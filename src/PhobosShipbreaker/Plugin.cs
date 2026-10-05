using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Phobos.Ostranauts.Framework;

namespace PhobosShipbreaker;

[BepInPlugin(Id, "Phobos Shipbreaker", Version)]
[BepInProcess("Ostranauts.exe")]
[BepInDependency(FrameworkInfo.PluginId, Core.DependencyContract.MinimumFramework)]
[BepInDependency(PhobosAutoNav.Plugin.Id, Core.DependencyContract.MinimumAutoNav)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.shipbreaker";
    public const string Version = "0.83.0";
    internal static ProcessingService Service { get; private set; } = null!;
    internal static Action<string> Log { get; private set; } = null!;
    internal static Settings Options { get; private set; } = null!;
    internal static CollectorService Collectors { get; private set; } = null!;
    internal static StorageService Storage { get; private set; } = null!;
    internal static CollectorPanel CollectorControls { get; private set; } = null!;
    internal static ReclaimerPanel ReclaimerControls { get; private set; } = null!;
    private Harmony? harmony;
    private FixturePanel panel = null!;

    private void Awake()
    {
        Log = text => Logger.LogInfo(text);
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.SetEndpointValidator(co => Core.CollectorRules.IsFamily(co.strCODef) && CollectorRoute.MountProblem(co) == null);
        PhobosAutoNav.IndustrialNavigation.ManualTakeover += ReclamationService.ManualTakeover;
        PerformanceMetrics.Initialize();
        Options = new Settings(Config);
        Service = new ProcessingService(Log, Options);
        Collectors = new CollectorService(Log, Options);
        Storage = new StorageService(Log, Options);
        CollectorControls = new CollectorPanel(Collectors);
        ReclaimerControls = new ReclaimerPanel();
        panel = new FixturePanel(Service, Options);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += LoadContent;
        FrameworkLifecycle.ContentLoaded += ConfirmContent;
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new IndustrialCrewProvider());
        var vessels = new VesselProvider();
        Phobos.Ostranauts.Framework.Controls.EquipmentProviders.Register(vessels);
        VesselPanel.Register(vessels);
        Phobos.Ostranauts.Framework.Crew.CrewSpecialities.Register(new("IndustrialProcessing",Text.Get("Crew.skill"),Id,Phobos.Ostranauts.Framework.Crew.CrewRole.Industry));
        // Crew upkeep (0.82.0): the D4, R4, T2 and ML-2 can be tuned and inspected. The F6 is inspected only: its melt
        // hold is a minute, so a tune would gain seconds, and its hot-batch physics stay as they are.
        foreach (var (key, kind, tunable) in new[] { ("d4", PowerKind.Processor, true), ("r4", PowerKind.Reclaimer, true), ("t2", PowerKind.Thaw, true),
            ("ml2", PowerKind.Laser, true), ("f6", PowerKind.Furnace, false) })
        {
            var k = kind;
            Phobos.Ostranauts.Framework.Crew.Upkeep.Register("shipbreaker." + key, id => PowerKinds.Classify(id) == k, "IndustrialProcessing",
                Phobos.Ostranauts.Framework.Crew.CrewRole.Industry, tunable);
        }
        // Housekeeping (0.83.0): crew may put away what a Rivetline Y bin takes (mined ore, regolith, gangue, ice) in one.
        Phobos.Ostranauts.Framework.Crew.Upkeep.RegisterTidyStore(Core.BinRules.IsFamily);
        Log(Text.Get("Plugin.shipbreaker_loaded_with_independent_phobos_framework_construction", Options.ControlsKey));
    }
    private void Update() { panel.Update(); FurnaceService.Update(); CaptureService.Update(); ReclamationService.Update(); LaserService.Update(); }
    internal static void ResetServices() { PowerKinds.Reset(); ReclamationService.Reset(); LaserService.Reset(); CaptureService.Reset(); ThawService.Reset(); Service.Reset(); Collectors.Reset(); Storage.Reset(); CollectorControls.Reset(); ReclaimerControls.Reset(); IndustryObservations.Reset(); FurnaceService.Reset(); }
    private static void LoadContent() { ResetServices(); Content.Register(Log); }
    private static void ConfirmContent() => Content.ConfirmRecipes(Log);
    private void OnDestroy()
    {
        PhobosAutoNav.IndustrialNavigation.ManualTakeover -= ReclamationService.ManualTakeover;
        ReclamationService.Reset(); LaserService.Reset(); CaptureService.Shutdown();
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.SetEndpointValidator(null);
        FrameworkLifecycle.ContentLoading -= LoadContent; FrameworkLifecycle.ContentLoaded -= ConfirmContent;
        // Shipbreaker registers no bulk vessels since 0.54.0: the water silos are Framework's, under this mod's id, and
        // unregistering by that id here would take Framework's tanks away.
        Phobos.Ostranauts.Framework.Controls.EquipmentProviders.Unregister(Id); Phobos.Ostranauts.Framework.Trading.BulkSupplies.Unregister(Id);
        Service?.Reset(); Collectors?.Reset(); Storage?.Reset(); ThawService.Reset(); IndustryObservations.Reset(); harmony?.UnpatchSelf();
    }
}

// The game calls these for every powered object in the world; an appliance that is not ours is classified by one
// dictionary probe and leaves no state behind (29 September 2026 performance pass, FF3).
[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    internal sealed class PowerState { internal double Tune = 1; internal PowerKind Kind; internal bool Working, Feeding, Unloading, Finished, Cutting; internal ReclamationService.PowerTransfer? Cutter; internal ReclaimerHeat.Transfer? Heat; internal FurnaceService.PowerTransfer? Furnace; internal ThawService.Transfer? Thaw; internal LaserService.PowerTransfer? Laser; internal bool Lasing; }
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out PowerState? __state)
    {
        __state = null;
        if (__0 == null) return true;
        var kind = PowerKinds.Classify(__0.strCODef);
        if (kind == PowerKind.None) return true;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PowerHook);
        var state = __state = new PowerState { Kind = kind };
        switch (kind)
        {
            case PowerKind.Furnace:
                try { return FurnaceService.BeginPower(__instance, __0, ref __1, out state.Furnace); }
                catch (Exception ex) { FurnaceService.Fault(__0, ex); return false; }
            case PowerKind.Thaw:
                try { return ThawService.BeginPower(__instance, __0, ref __1, out state.Thaw); }
                catch (Exception ex) { ThawService.Fault(__0, ex); return false; }
            case PowerKind.Laser:
                try { return LaserService.BeginPower(__instance, __0, ref __1, out state.Laser, out state.Lasing); }
                catch (Exception ex) { LaserService.Fault(__0, ex); return false; }
            case PowerKind.Grabber:
                state.Working = __0.HasCond(Core.IntakeRules.Working);
                try { return ReclamationService.BeginPower(__instance,__0,ref __1,out state.Cutter,out state.Cutting); }
                catch(Exception ex) { ReclamationService.Fault(__0,ex); return false; }
        }
        state.Working = kind == PowerKind.Collector ? __0.HasCond(Core.CollectorRules.Working) : __0.HasCond(Core.ProcessRules.Working);
        state.Feeding = kind == PowerKind.Reclaimer && __0.HasCond(Core.RoutingRules.Feeding);
        state.Unloading = ProcessingService.IsInstalledProcessor(__0) && __0.HasCond(Core.StorageRules.Unloading);
        if (state.Feeding || state.Unloading)
        {
            bool reclaimer = kind == PowerKind.Reclaimer;
            double work = reclaimer ? Plugin.Options.ReclaimerKW : Plugin.Options.WorkingKW, idle = reclaimer ? Core.ReclaimerRules.IdleKW : Plugin.Options.IdleKW;
            __1 *= Core.RoutingRules.DemandKW(state.Working, state.Feeding, state.Unloading, work, idle, Plugin.Options.FeederKW) / (state.Working ? work : idle);
        }
        // Crew upkeep (0.82.0): a tuned D4 or R4 asks for more power while it works and makes that much more progress.
        if (state.Working && kind != PowerKind.Collector)
        {
            bool reclaiming = kind == PowerKind.Reclaimer;
            double demandKW = Core.RoutingRules.DemandKW(true, state.Feeding, state.Unloading, reclaiming ? Plugin.Options.ReclaimerKW : Plugin.Options.WorkingKW,
                reclaiming ? Core.ReclaimerRules.IdleKW : Plugin.Options.IdleKW, Plugin.Options.FeederKW);
            if (demandKW > 0) state.Tune = Phobos.Ostranauts.Framework.Crew.Upkeep.Draw(__0, ref __1, __1 * Phobos.Ostranauts.Framework.Units.SecondsPerHour / demandKW);
        }
        return kind != PowerKind.Reclaimer || ReclaimerHeat.Begin(__instance, __0, __1, out state.Heat);
    }
    private static void Postfix(Powered __instance, CondOwner __0, PowerState? __state)
    {
        if (__state == null || __0 == null) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PowerHook);
        if (__state.Cutting)
        {
            try { ReclamationService.FinishPower(__instance,__0,__state.Cutter); }
            catch(Exception ex) { ReclamationService.Fault(__0,ex); }
            finally { __state.Finished=true; }
            return;
        }
        if (__state.Kind == PowerKind.Laser)
        {
            try { LaserService.FinishPower(__instance, __0, __state.Laser); }
            catch (Exception ex) { LaserService.Fault(__0, ex); }
            finally { __state.Finished = true; }
            return;
        }
        if (__state.Kind == PowerKind.Thaw)
        {
            try { ThawService.FinishPower(__instance, __0, __state.Thaw); ThawService.AfterPower(__0, __0.HasCond(Core.ProcessRules.Working), __state.Thaw?.WorkSeconds, __state.Thaw?.Tune ?? 1); }
            catch (Exception ex) { ThawService.Fault(__0, ex); }
            finally { __state.Finished = true; }
            return;
        }
        if (__state.Kind == PowerKind.Furnace)
        {
            try { FurnaceService.FinishPower(__instance, __0, __state.Furnace); }
            catch (Exception ex) { FurnaceService.Fault(__0, ex); }
            finally { __state.Finished = true; }
            return;
        }
        if (__state.Kind == PowerKind.Reclaimer)
        {
            try { ReclaimerHeat.Finish(__instance, __0, __state.Heat); }
            catch (Exception ex) { Plugin.Service.Fault(__0, ex); Plugin.Collectors.Fault(__0, ex); __state.Finished = true; return; }
        }
        __state.Finished = true;
        if (__state.Kind == PowerKind.Collector) Plugin.Collectors.AfterPower(__0, __state.Working);
        else if (__state.Kind == PowerKind.Grabber) Plugin.Service.AfterIntakePower(__0, __state.Working);
        else
        {
            Plugin.Service.AfterPower(__0, __state.Working, __state.Heat?.WorkSeconds, __state.Tune);
            if (__state.Kind == PowerKind.Reclaimer) Plugin.Collectors.AfterPower(__0, __state.Feeding, __state.Heat?.WorkSeconds);
            if (ProcessingService.IsInstalledProcessor(__0)) Plugin.Storage.AfterPower(__0, __state.Unloading, __state.Heat?.WorkSeconds);
        }
    }
    private static void Finalizer(Powered __instance, CondOwner __0, PowerState? __state)
    {
        if (__state == null || __0 == null) return;
        // An exceptional native call can already have debited electricity. Account
        // its witnessed partial delivery once before discarding the session receipt.
        try
        {
            if (!__state.Finished)
            {
                if (__state.Kind == PowerKind.Furnace)
                { FurnaceService.FinishPower(__instance, __0, __state.Furnace); FurnaceService.Get(__0).State.Batch.Armed = false; }
                else if (__state.Cutting) { ReclamationService.FinishPower(__instance,__0,__state.Cutter); ReclamationService.Fault(__0,new InvalidOperationException("Interrupted cutter power delivery.")); }
                else if (__state.Lasing) { LaserService.FinishPower(__instance, __0, __state.Laser); LaserService.Fault(__0, new InvalidOperationException("Interrupted laser power delivery.")); }
                else if (__state.Kind == PowerKind.Thaw) ThawService.FinishPower(__instance, __0, __state.Thaw);
                else if (__state.Kind == PowerKind.Reclaimer) ReclaimerHeat.Finish(__instance, __0, __state.Heat);
            }
        }
        catch (Exception ex) { if (__state.Kind == PowerKind.Furnace) FurnaceService.Fault(__0, ex); else Plugin.Log(ex.Message); }
        finally { ReclaimerHeat.Forget(__instance); ThawService.Forget(__instance); }
    }
}

// Clear stale work demand before the native path selects its active/idle coefficient.
[HarmonyPatch(typeof(Powered), "Run")]
internal static class PowerDemandPatch
{
    private static void Prefix(Powered __instance)
    {
        var machine = __instance.CO;
        if (machine == null) return;
        var kind = PowerKinds.Classify(machine.strCODef);
        switch (kind)
        {
            case PowerKind.None: case PowerKind.Furnace: return;
            case PowerKind.Collector:
                try { Plugin.Collectors.BeforePower(machine); }
                catch (Exception ex) { Plugin.Collectors.Fault(machine, ex); }
                return;
            case PowerKind.Grabber:
                try { if (!ReclamationService.PreparePower(machine)) Plugin.Service.BeforeIntakePower(machine); }
                catch (Exception ex) { Plugin.Service.IntakeFault(machine, ex); }
                return;
            case PowerKind.Thaw:
                try { ThawService.BeforePower(machine); }
                catch (Exception ex) { ThawService.Fault(machine, ex); }
                return;
            case PowerKind.Laser:
                try { LaserService.PreparePower(machine); }
                catch (Exception ex) { LaserService.Fault(machine, ex); }
                return;
        }
        try
        {
            Plugin.Service.BeforePower(machine);
            if (kind == PowerKind.Reclaimer) Plugin.Collectors.BeforePower(machine);
        }
        catch (Exception ex)
        { Plugin.Service.Fault(machine, ex); if (kind == PowerKind.Reclaimer) Plugin.Collectors.Fault(machine, ex); }
        // Storage unloading has its own permission and fault state; it never stops processing.
        if (!ProcessingService.IsInstalledProcessor(machine)) return;
        try { Plugin.Storage.BeforePower(machine); }
        catch (Exception ex) { Plugin.Storage.Fault(machine, ex); }
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class FeedPatch
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    {
        // Runs for every container admission in the game: the container and its id are read once, and a container
        // that is none of ours leaves after a few string comparisons.
        if (!__result) return;
        var bin = __instance.CO; string? id = bin?.strCODef;
        if (id == null) return;
        if (id == Core.FurnaceRules.Feed) __result = FurnaceService.CanFeed(bin!, coIn);
        else if (id == Core.ThawRules.InputBin) __result = ThawService.CanFeed(bin!, coIn);
        else if (id == Content.InputBin || id == Core.ReclaimerRules.InputBin) __result = ProcessingService.CanFeed(bin!, coIn);
        else if (Core.CollectorRules.IsFamily(id)) __result = CollectorService.CanAccept(bin!, coIn);
    }
}

// Native items normally auto-stack on insertion (aluminium, floor grates, Whipple panels, water ice). Feed bins
// hold individual units with their own saved job state, so disable stacking only across the four feed bins.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.CanStackOnItem))]
internal static class FurnaceFeedStackPatch
{
    private static bool FeedBin(string? id) => id == Core.FurnaceRules.Feed || id == Content.InputBin || id == Core.ReclaimerRules.InputBin || id == Core.ThawRules.InputBin;
    private static void Postfix(CondOwner __instance, CondOwner objIncoming, ref int __result)
    {
        if (__result != 0 && (FeedBin(__instance.objCOParent?.strCODef) || FeedBin(objIncoming?.objCOParent?.strCODef))) __result = 0;
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Plugin.ResetServices();
}
