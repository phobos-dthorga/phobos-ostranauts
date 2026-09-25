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
    public const string Version = "0.23.0";
    internal static ProcessingService Service { get; private set; } = null!;
    internal static Action<string> Log { get; private set; } = null!;
    internal static Settings Options { get; private set; } = null!;
    internal static CollectorService Collectors { get; private set; } = null!;
    internal static CollectorPanel CollectorControls { get; private set; } = null!;
    internal static ReclaimerPanel ReclaimerControls { get; private set; } = null!;
    private Harmony? harmony;
    private FixturePanel panel = null!;

    private void Awake()
    {
        Log = text => Logger.LogInfo(text);
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.SetEndpointValidator(co => Core.CollectorRules.IsFamily(co.strCODef) && CollectorRoute.MountProblem(co) == null);
        PerformanceMetrics.Initialize();
        Options = new Settings(Config);
        Service = new ProcessingService(Log, Options);
        Collectors = new CollectorService(Log, Options);
        CollectorControls = new CollectorPanel(Collectors);
        ReclaimerControls = new ReclaimerPanel();
        panel = new FixturePanel(Service, Options);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += LoadContent;
        FrameworkLifecycle.ContentLoaded += ConfirmContent;
        Log(Text.Get("Plugin.shipbreaker_loaded_with_independent_phobos_framework_construction", Options.ControlsKey));
    }
    private void Update() { panel.Update(); FurnaceService.Update(); CaptureService.Update(); }
    private void OnGUI() { panel.Draw(); CollectorControls.Draw(); ReclaimerControls.Draw(); }
    internal static void ResetServices() { CaptureService.Reset(); Service.Reset(); Collectors.Reset(); CollectorControls.Reset(); ReclaimerControls.Reset(); IndustryObservations.Reset(); FurnaceService.Reset(); }
    private static void LoadContent() { ResetServices(); Content.Register(Log); }
    private static void ConfirmContent() => Content.ConfirmRecipes(Log);
    private void OnDestroy()
    {
        CaptureService.Shutdown();
        Phobos.Ostranauts.Framework.Inventory.CollectorCargo.SetEndpointValidator(null);
        FrameworkLifecycle.ContentLoading -= LoadContent; FrameworkLifecycle.ContentLoaded -= ConfirmContent;
        Service?.Reset(); Collectors?.Reset(); IndustryObservations.Reset(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    internal sealed class PowerState { internal bool Working, Feeding, Finished; internal ReclaimerHeat.Transfer? Heat; internal FurnaceService.PowerTransfer? Furnace; }
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out PowerState __state)
    {
        __state = new PowerState { Working = __0 != null && (ProcessingService.IsProcessor(__0.strCODef) && __0.HasCond(Core.ProcessRules.Working) ||
            ProcessingService.IsGrabber(__0) && __0.HasCond(Core.IntakeRules.Working) ||
            Core.CollectorRules.IsFamily(__0.strCODef) && __0.HasCond(Core.CollectorRules.Working)) };
        __state.Feeding = __0 != null && ProcessingService.IsReclaimer(__0) && __0.HasCond(Core.RoutingRules.Feeding);
        if (__0 != null && Core.FurnaceRules.Machine(__0.strCODef))
        {
            try { return FurnaceService.BeginPower(__instance, __0, ref __1, out __state.Furnace); }
            catch (Exception ex) { FurnaceService.Fault(__0, ex); return false; }
        }
        if (__state.Feeding)
        {
            double baseKW = __state.Working ? Plugin.Options.ReclaimerKW : Core.ReclaimerRules.IdleKW;
            __1 *= Core.RoutingRules.DemandKW(__state.Working, true, Plugin.Options.ReclaimerKW, Plugin.Options.FeederKW) / baseKW;
        }
        return __0 == null || ReclaimerHeat.Begin(__instance, __0, __1, out __state.Heat);
    }
    private static void Postfix(Powered __instance, CondOwner __0, PowerState __state)
    {
        if (__0 == null) return;
        if (Core.FurnaceRules.Machine(__0.strCODef))
        {
            try { FurnaceService.FinishPower(__instance, __0, __state.Furnace); }
            catch (Exception ex) { FurnaceService.Fault(__0, ex); }
            finally { __state.Finished = true; }
            return;
        }
        try { ReclaimerHeat.Finish(__instance, __0, __state.Heat); }
        catch (Exception ex) { Plugin.Service.Fault(__0, ex); Plugin.Collectors.Fault(__0, ex); return; }
        finally { __state.Finished = true; }
        if (Core.CollectorRules.IsFamily(__0.strCODef)) Plugin.Collectors.AfterPower(__0, __state.Working);
        else if (ProcessingService.IsGrabber(__0)) Plugin.Service.AfterIntakePower(__0, __state.Working);
        else
        {
            Plugin.Service.AfterPower(__0, __state.Working, __state.Heat?.WorkSeconds);
            if (ProcessingService.IsReclaimer(__0)) Plugin.Collectors.AfterPower(__0, __state.Feeding, __state.Heat?.WorkSeconds);
        }
    }
    private static void Finalizer(Powered __instance, CondOwner __0, PowerState? __state)
    {
        // An exceptional native call can already have debited electricity. Account
        // its witnessed partial delivery once before discarding the session receipt.
        try
        {
            if (__state != null && !__state.Finished && __0 != null)
            {
                if (Core.FurnaceRules.Machine(__0.strCODef))
                { FurnaceService.FinishPower(__instance, __0, __state.Furnace); FurnaceService.Get(__0).State.Batch.Armed = false; }
                else ReclaimerHeat.Finish(__instance, __0, __state.Heat);
            }
        }
        catch (Exception ex) { if (__0 != null && Core.FurnaceRules.Machine(__0.strCODef)) FurnaceService.Fault(__0, ex); else Plugin.Log(ex.Message); }
        finally { ReclaimerHeat.Forget(__instance); }
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
        if (Core.CollectorRules.IsFamily(machine.strCODef))
        {
            try { Plugin.Collectors.BeforePower(machine); }
            catch (Exception ex) { Plugin.Collectors.Fault(machine, ex); }
            return;
        }
        if (ProcessingService.IsGrabber(machine))
        {
            try { Plugin.Service.BeforeIntakePower(machine); }
            catch (Exception ex) { Plugin.Service.IntakeFault(machine, ex); }
            return;
        }
        if (!ProcessingService.IsProcessor(machine.strCODef)) return;
        try
        {
            Plugin.Service.BeforePower(machine);
            if (ProcessingService.IsReclaimer(machine)) Plugin.Collectors.BeforePower(machine);
        }
        catch (Exception ex)
        { Plugin.Service.Fault(machine, ex); if (ProcessingService.IsReclaimer(machine)) Plugin.Collectors.Fault(machine, ex); }
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class FeedPatch
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    {
        if (__result && __instance.CO?.strCODef == Core.FurnaceRules.Feed) __result = FurnaceService.CanFeed(__instance.CO, coIn);
        if (__result && __instance.CO != null && (__instance.CO.strCODef == Content.InputBin || __instance.CO.strCODef == Core.ReclaimerRules.InputBin))
            __result = ProcessingService.CanFeed(__instance.CO, coIn);
        if (__result && __instance.CO != null && Core.CollectorRules.IsFamily(__instance.CO.strCODef))
            __result = CollectorService.CanAccept(__instance.CO, coIn);
    }
}

// Native aluminium normally auto-stacks on insertion. Captive batches require
// individual persistent identities, so disable stacking only across this bin.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.CanStackOnItem))]
internal static class FurnaceFeedStackPatch
{
    private static void Postfix(CondOwner __instance, CondOwner objIncoming, ref int __result)
    {
        if (__instance.objCOParent?.strCODef == Core.FurnaceRules.Feed || objIncoming?.objCOParent?.strCODef == Core.FurnaceRules.Feed) __result = 0;
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() =>
        typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Plugin.ResetServices();
}
