using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

[BepInPlugin(Id, "Phobos Manufacturing", Version)]
[BepInDependency(FrameworkInfo.PluginId, MinimumFrameworkVersion)]
[BepInDependency(ShipbreakerStock.PluginId, BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = ManufacturingRules.Owner;
    public const string Version = "0.7.0";
    public const string MinimumFrameworkVersion = "0.46.0";
    internal static Action<string> Log = _ => { };
    private Harmony? harmony;
    private float nextScan;
    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        ShipbreakerStock.Detect();
        FrameworkLifecycle.ContentLoading += Load;
        EquipmentProviders.Register(new Provider());
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new FillerCrewProvider());
        Phobos.Ostranauts.Framework.Propulsion.RcsPropellant.Register(ManifoldService.Instance);
        Phobos.Ostranauts.Framework.Trading.BulkSupplies.Register(StoreService.GasSupplies);
        Log(Text.Get("Plugin.loaded", Version, ShipbreakerStock.PluginPresent ? Text.Get("Plugin.with_shipbreaker") : Text.Get("Plugin.without_shipbreaker")));
    }
    private static void Load() { ResetServices(); Content.Register(Log); }
    internal static void ResetServices() { MachineKinds.Reset(); RefineryService.Reset(); ProcessorService.Reset(); SabatierService.Reset(); StoreService.Reset(); ManifoldService.Reset(); FillerService.Reset(); RegulatorService.Reset(); }
    private readonly List<CondOwner> damagedStores = new(), regulators = new(), members = new();
    // Stage 8: regulators and gas stores come from Framework's shared world sweep, not a pass over every world object.
    private static readonly Phobos.Ostranauts.Framework.Discovery.WorldFamily machines =
        Phobos.Ostranauts.Framework.Discovery.WorldFamilies.Register(Id + ".scanned", id => id == RegulatorRules.Installed || GasStores.IsFamily(id));
    /// <summary>A damaged fuel store has no native tick of its own: every couple of seconds its leak advances, and every
    /// A2 regulator checks its room. One plain pass over the world, one dictionary probe per object.</summary>
    private void Update()
    {
        if (UnityEngine.Time.unscaledTime < nextScan) return;
        nextScan = UnityEngine.Time.unscaledTime + 2;
        if (!Content.Ready || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || DataHandler.mapCOs == null) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Scan);
        damagedStores.Clear(); regulators.Clear();
        machines.Members(members);
        foreach (var c in members)
        {
            if (c.strCODef == RegulatorRules.Installed) regulators.Add(c);
            else if (GasStores.IsFamily(c.strCODef) && c.HasCond("IsDamaged") && c.HasCond("IsInstalled")) damagedStores.Add(c);
        }
        foreach (var co in damagedStores) StoreService.Tick(co);
        foreach (var co in regulators)
        {
            using var tick = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.RegulatorTick);
            try { RegulatorService.Tick(co); } catch (Exception ex) { Log(ex.ToString()); }
        }
    }
    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        EquipmentProviders.Unregister(Id); Phobos.Ostranauts.Framework.Crew.CrewWork.Unregister(Id); Phobos.Ostranauts.Framework.Liquids.BulkVessels.Unregister(Id); Phobos.Ostranauts.Framework.Trading.BulkSupplies.Unregister(Id);
        Phobos.Ostranauts.Framework.Propulsion.RcsPropellant.Unregister(ManifoldService.Instance.Id);
        ResetServices(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    // The game calls these for every powered object in the world; an appliance that is not ours is classified by one
    // dictionary probe and leaves no state behind (29 September 2026 performance pass, FF3).
    internal sealed class PowerState { internal MachineKind Kind; internal bool Finished; internal RefineryService.Transfer? Refinery; internal ProcessorService.Transfer? Processor; internal SabatierService.Transfer? Reactor; internal FillerService.Transfer? Filler; }
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out PowerState? __state)
    {
        __state = null;
        if (__0 == null) return true;
        var kind = MachineKinds.Classify(__0.strCODef);
        if (kind == MachineKind.None) return true;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PowerHook);
        var state = __state = new PowerState { Kind = kind };
        switch (kind)
        {
            case MachineKind.Refinery:
                try { return RefineryService.BeginPower(__instance, __0, ref __1, out state.Refinery); }
                catch (Exception ex) { RefineryService.Fault(__0, ex); return false; }
            case MachineKind.Processor:
                try { return ProcessorService.BeginPower(__instance, __0, ref __1, out state.Processor); }
                catch (Exception ex) { ProcessorService.Fault(__0, ex); return false; }
            case MachineKind.Sabatier:
                try { return SabatierService.BeginPower(__instance, __0, ref __1, out state.Reactor); }
                catch (Exception ex) { SabatierService.Fault(__0, ex); return false; }
            default:
                try { return FillerService.BeginPower(__instance, __0, ref __1, out state.Filler); }
                catch (Exception ex) { FillerService.Fault(__0, ex); return false; }
        }
    }
    private static void Postfix(Powered __instance, CondOwner __0, PowerState? __state)
    {
        if (__state == null || __0 == null) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PowerHook);
        try
        {
            switch (__state.Kind)
            {
                case MachineKind.Refinery: RefineryService.FinishPower(__instance, __0, __state.Refinery); RefineryService.AfterPower(__0); break;
                case MachineKind.Processor: ProcessorService.FinishPower(__instance, __0, __state.Processor); break;
                case MachineKind.Sabatier: SabatierService.FinishPower(__instance, __0, __state.Reactor); break;
                default: FillerService.FinishPower(__instance, __0, __state.Filler); break;
            }
        }
        catch (Exception ex) { Fault(__state.Kind, __0, ex); }
        finally { __state.Finished = true; }
    }
    private static void Fault(MachineKind kind, CondOwner co, Exception ex)
    {
        switch (kind)
        {
            case MachineKind.Refinery: RefineryService.Fault(co, ex); break;
            case MachineKind.Processor: ProcessorService.Fault(co, ex); break;
            case MachineKind.Sabatier: SabatierService.Fault(co, ex); break;
            default: FillerService.Fault(co, ex); break;
        }
    }
    private static void Finalizer(Powered __instance, CondOwner __0, PowerState? __state)
    {
        if (__state == null || __0 == null) return;
        // An exceptional native call can already have debited electricity: account its witnessed delivery once.
        try
        {
            if (!__state.Finished)
                switch (__state.Kind)
                {
                    case MachineKind.Refinery: RefineryService.FinishPower(__instance, __0, __state.Refinery); break;
                    case MachineKind.Processor: ProcessorService.FinishPower(__instance, __0, __state.Processor); break;
                    case MachineKind.Sabatier: SabatierService.FinishPower(__instance, __0, __state.Reactor); break;
                    default: FillerService.FinishPower(__instance, __0, __state.Filler); break;
                }
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
        finally { RefineryService.Forget(__instance); ProcessorService.Forget(__instance); SabatierService.Forget(__instance); FillerService.Forget(__instance); }
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
        var kind = MachineKinds.Classify(machine.strCODef);
        if (kind == MachineKind.None) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.MachineStep);
        switch (kind)
        {
            case MachineKind.Refinery: try { RefineryService.BeforePower(machine); } catch (Exception ex) { RefineryService.Fault(machine, ex); } break;
            case MachineKind.Processor: try { ProcessorService.BeforePower(machine); } catch (Exception ex) { ProcessorService.Fault(machine, ex); } break;
            case MachineKind.Sabatier: try { SabatierService.BeforePower(machine); } catch (Exception ex) { SabatierService.Fault(machine, ex); } break;
            default: try { FillerService.BeforePower(machine); } catch (Exception ex) { FillerService.Fault(machine, ex); } break;
        }
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class FeedPatch
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    {
        if (__result && __instance.CO?.strCODef == RefineryRules.InputBin) __result = RefineryService.CanFeed(__instance.CO, coIn);
    }
}

// Ores and our stock stack natively; the feed bin holds individual units bound to a charge, so no merging there.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.CanStackOnItem))]
internal static class FeedStackPatch
{
    private static void Postfix(CondOwner __instance, CondOwner objIncoming, ref int __result)
    {
        if (__instance.objCOParent?.strCODef == RefineryRules.InputBin || objIncoming?.objCOParent?.strCODef == RefineryRules.InputBin) __result = 0;
    }
}

[HarmonyPatch]
internal static class ReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Plugin.ResetServices();
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class ControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.strName != Definitions.Controls || !Content.Machine(__instance.objThem)) return;
        if (__instance.objUs == CrewSim.GetSelectedCrew()) Panel.Show(__instance.objThem);
    }
}

// "Keep suit bottles charged": a toggle like the game's own Toggle Power, on the intact installed L2.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class BottleOrderPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.strName != FillerRules.BottleOrder || __instance.objThem == null || __instance.objUs != CrewSim.GetSelectedCrew()) return;
        bool done = FillerCrewProvider.Toggle(__instance.objThem, out string message);
        var actor = __instance.objUs;
        if (actor != null && !actor.bDestroyed && actor.HasCond("IsHuman")) actor.LogMessage(message, done ? "Neutral" : "Bad", "Game");
    }
}

// Removal work is refused when it is offered, never by blocking native destruction; a refused finish still
// closes the game's task (owner decision, 28 September 2026).
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class MaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        // This runs for every offer the game evaluates: our machines are recognised before any name search.
        if (!__result || __instance.strName == null || !Content.Machine(objUs) && !Content.Machine(objThem)) return;
        var reason = MaintenanceFinish.Reason(__instance.strName, objUs, objThem);
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class MaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        var machine = Content.Machine(us) ? us : Content.Machine(them) ? them : null;
        if (machine == null || action == null) return null;
        bool dismantle = action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0;
        bool removal = dismantle || action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
        return removal ? Content.MaintenanceReason(machine, dismantle) : null;
    }
    private static bool Prefix(Interaction __instance)
    {
        var reason = Reason(__instance.strName, __instance.objUs, __instance.objThem);
        return reason == null || Phobos.Ostranauts.Framework.Registration.NativeEffects.Refuse(__instance, reason);
    }
}

// The stores' and the reactor's hazards follow the native damage switch and destruction; nothing is blocked.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class StoreDamagePatch
{
    private static void Postfix(CondOwner coNew)
    {
        if (coNew == null || !coNew.HasCond("IsDamaged")) return;
        if (GasStores.IsFamily(coNew.strCODef)) StoreService.Damaged(coNew);
        else if (SabatierRules.IsFamily(coNew.strCODef)) SabatierService.Damaged(coNew);
    }
}
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class StoreDestroyPatch
{
    private static void Prefix(CondOwner __instance)
    {
        if (__instance == null) return;
        if (GasStores.IsFamily(__instance.strCODef)) StoreService.Destroying(__instance);
        else if (SabatierRules.IsFamily(__instance.strCODef)) SabatierService.Destroying(__instance);
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var parts = strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts[0].Equals("phobosmanufacturing", StringComparison.OrdinalIgnoreCase)) return true;
        if (parts.Length == 1 || parts[1] == "list")
        { strInput += "\n" + string.Join("\n", CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, false, false, true).Where(Content.Machine).Select(c => c.strNameFriendly + " " + c.strID) ?? Array.Empty<string>()); __result = true; return false; }
        var co = parts.Length >= 3 ? Content.Resolve(parts[2]) : null;
        string message = Text.Get("Console.help");
        string action = parts.Length == 4 && new[] { "link", "water", "store", "canister", "vent", "hydrogen", "methane", "feed", "order", "source-on", "source-off", "unlink",
            "mode", "target", "draw", "transfer", "o2", "pressure", "oxygen", "nitrogen" }.Contains(parts[1]) ? parts[1] + ":" + parts[3] : parts[1];
        var provider = new Provider();
        __result = Content.Machine(co) && provider.Command(co!, null, action, out message); strInput += "\n" + message; return false;
    }
}
