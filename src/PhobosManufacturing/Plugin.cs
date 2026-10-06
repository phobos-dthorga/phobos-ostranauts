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
[BepInDependency(AgricultureStock.PluginId, BepInDependency.DependencyFlags.SoftDependency)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = ManufacturingRules.Owner;
    public const string Version = "0.58.1";
    public const string MinimumFrameworkVersion = "0.127.2";
    internal static Action<string> Log = _ => { };
    private Harmony? harmony;
    private float nextScan;
    internal const string UpkeepSkill = "SkillEngMechanical";
    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        PerformanceMetrics.Initialize();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        ShipbreakerStock.Detect();
        AgricultureStock.Detect();
        FrameworkLifecycle.ContentLoading += Load;
        EquipmentProviders.Register(new Provider());
        Panel.Register();
        foreach (string group in Provider.Groups) { string g = group; EquipmentProviders.RegisterGroup(g, () => Text.Get("Group." + g)); }
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new FillerCrewProvider());
        // "Load feed by crew" on the charge machines and the RM-1 (0.56.0).
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new FeedCrewProvider());
        // Working sounds (0.57.0): each machine's loop, played by Framework.
        MachineSoundTable.Register();
        // Crew upkeep (0.55.0): every working machine can be tuned and inspected; the A2 regulator is inspected only,
        // because it holds a set point and has no work rate to raise. The game's mechanical skill counts as skilled.
        foreach (var kind in new[] { MachineKind.Charge, MachineKind.Processor, MachineKind.Sabatier, MachineKind.Filler, MachineKind.Cracker, MachineKind.Bottler, MachineKind.Feeder })
        {
            var k = kind;
            Phobos.Ostranauts.Framework.Crew.Upkeep.Register("manufacturing." + k.ToString().ToLowerInvariant(), id => MachineKinds.Classify(id) == k,
                UpkeepSkill, Phobos.Ostranauts.Framework.Crew.CrewRole.Industry, tunable: true);
        }
        Phobos.Ostranauts.Framework.Crew.Upkeep.Register("manufacturing.regulator", id => id == RegulatorRules.Installed, UpkeepSkill, Phobos.Ostranauts.Framework.Crew.CrewRole.Industry, tunable: false);
        Phobos.Ostranauts.Framework.Propulsion.RcsPropellant.Register(ManifoldService.Instance);
        Phobos.Ostranauts.Framework.Propulsion.RcsPropellant.Register(FeederService.Instance);
        Phobos.Ostranauts.Framework.Trading.BulkSupplies.Register(StoreService.Supplies);
        Phobos.Ostranauts.Framework.Trading.BulkSupplies.RegisterBuyback(StoreService.Buyback);
        Log(Text.Get("Plugin.loaded", Version, ShipbreakerStock.PluginPresent ? Text.Get("Plugin.with_shipbreaker") : Text.Get("Plugin.without_shipbreaker")));
    }
    private static void Load() { ResetServices(); Content.Register(Log); }
    internal static void ResetServices() { MachineKinds.Reset(); ProcessorService.Reset(); SabatierService.Reset(); CrackerService.Reset(); StoreService.Reset(); ManifoldService.Reset(); FillerService.Reset(); RegulatorService.Reset(); BottlerService.Reset(); FeederService.Reset(); }
    private readonly List<CondOwner> damagedStores = new(), regulators = new(), members = new(), classedStores = new();
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
        damagedStores.Clear(); regulators.Clear(); classedStores.Clear();
        machines.Members(members);
        foreach (var c in members)
        {
            if (c.strCODef == RegulatorRules.Installed) regulators.Add(c);
            else if (GasStores.IsFamily(c.strCODef) && c.HasCond("IsDamaged") && c.HasCond("IsInstalled")) damagedStores.Add(c);
            else if (c.ship != null && GasStores.For(c.strCODef) is GasStore g && Phobos.Ostranauts.Framework.Liquids.GasNetworkSafety.ClassOf(g.Commodity) != Phobos.Ostranauts.Framework.Liquids.GasHazardClass.None)
                classedStores.Add(c);
        }
        foreach (var co in damagedStores) StoreService.Tick(co);
        // Oxygen and fuel stores sharing one gas line: advice through one crew-log line per ship, never a block.
        foreach (var ship in classedStores.GroupBy(c => c.ship))
        {
            try { Phobos.Ostranauts.Framework.Liquids.GasNetworkSafety.Review(ship.Key, ship); } catch (Exception ex) { Log(ex.ToString()); }
        }
        foreach (var co in regulators)
        {
            using var tick = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.RegulatorTick);
            try { RegulatorService.Tick(co); } catch (Exception ex) { Log(ex.ToString()); }
        }
    }
    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        EquipmentProviders.Unregister(Id); Phobos.Ostranauts.Framework.Crew.CrewWork.Unregister(Id); Phobos.Ostranauts.Framework.Crew.CrewWork.Unregister(Id + ".feed"); Phobos.Ostranauts.Framework.Liquids.BulkVessels.Unregister(Id); Phobos.Ostranauts.Framework.Trading.BulkSupplies.Unregister(Id);
        Phobos.Ostranauts.Framework.Propulsion.RcsPropellant.Unregister(ManifoldService.Instance.Id);
        ResetServices(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    // The game calls these for every powered object in the world; an appliance that is not ours is classified by one
    // dictionary probe and leaves no state behind (29 September 2026 performance pass, FF3).
    internal sealed class PowerState { internal MachineKind Kind; internal bool Finished; internal ChargeMachine? Engine; internal ChargeMachine.Transfer? Charge; internal ProcessorService.Transfer? Processor; internal SabatierService.Transfer? Reactor; internal CrackerService.Transfer? Cracker; internal FillerService.Transfer? Filler; internal BottlerService.Transfer? Bottler; internal FeederService.Transfer? Feeder; }
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
            case MachineKind.Charge:
                state.Engine = ChargeMachines.For(__0.strCODef);
                if (state.Engine == null) return true;
                try { return state.Engine.BeginPower(__instance, __0, ref __1, out state.Charge); }
                catch (Exception ex) { state.Engine.Fault(__0, ex); return false; }
            case MachineKind.Processor:
                try { return ProcessorService.BeginPower(__instance, __0, ref __1, out state.Processor); }
                catch (Exception ex) { ProcessorService.Fault(__0, ex); return false; }
            case MachineKind.Sabatier:
                try { return SabatierService.BeginPower(__instance, __0, ref __1, out state.Reactor); }
                catch (Exception ex) { SabatierService.Fault(__0, ex); return false; }
            case MachineKind.Cracker:
                try { return CrackerService.BeginPower(__instance, __0, ref __1, out state.Cracker); }
                catch (Exception ex) { CrackerService.Fault(__0, ex); return false; }
            case MachineKind.Filler:
                try { return FillerService.BeginPower(__instance, __0, ref __1, out state.Filler); }
                catch (Exception ex) { FillerService.Fault(__0, ex); return false; }
            case MachineKind.Bottler:
                try { return BottlerService.BeginPower(__instance, __0, ref __1, out state.Bottler); }
                catch (Exception ex) { BottlerService.Fault(__0, ex); return false; }
            case MachineKind.Feeder:
                try { return FeederService.BeginPower(__instance, __0, ref __1, out state.Feeder); }
                catch (Exception ex) { FeederService.Fault(__0, ex); return false; }
            default: return true;
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
                case MachineKind.Charge: __state.Engine?.FinishPower(__instance, __0, __state.Charge); __state.Engine?.AfterPower(__0); break;
                case MachineKind.Processor: ProcessorService.FinishPower(__instance, __0, __state.Processor); break;
                case MachineKind.Sabatier: SabatierService.FinishPower(__instance, __0, __state.Reactor); break;
                case MachineKind.Cracker: CrackerService.FinishPower(__instance, __0, __state.Cracker); break;
                case MachineKind.Filler: FillerService.FinishPower(__instance, __0, __state.Filler); break;
                case MachineKind.Bottler: BottlerService.FinishPower(__instance, __0, __state.Bottler); break;
                case MachineKind.Feeder: FeederService.FinishPower(__instance, __0, __state.Feeder); break;
            }
        }
        catch (Exception ex) { Fault(__state, __0, ex); }
        finally { __state.Finished = true; }
    }
    private static void Fault(PowerState state, CondOwner co, Exception ex)
    {
        switch (state.Kind)
        {
            case MachineKind.Charge: state.Engine?.Fault(co, ex); break;
            case MachineKind.Processor: ProcessorService.Fault(co, ex); break;
            case MachineKind.Sabatier: SabatierService.Fault(co, ex); break;
            case MachineKind.Cracker: CrackerService.Fault(co, ex); break;
            case MachineKind.Filler: FillerService.Fault(co, ex); break;
            case MachineKind.Bottler: BottlerService.Fault(co, ex); break;
            case MachineKind.Feeder: FeederService.Fault(co, ex); break;
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
                    case MachineKind.Charge: __state.Engine?.FinishPower(__instance, __0, __state.Charge); break;
                    case MachineKind.Processor: ProcessorService.FinishPower(__instance, __0, __state.Processor); break;
                    case MachineKind.Sabatier: SabatierService.FinishPower(__instance, __0, __state.Reactor); break;
                    case MachineKind.Cracker: CrackerService.FinishPower(__instance, __0, __state.Cracker); break;
                    case MachineKind.Filler: FillerService.FinishPower(__instance, __0, __state.Filler); break;
                    case MachineKind.Bottler: BottlerService.FinishPower(__instance, __0, __state.Bottler); break;
                    case MachineKind.Feeder: FeederService.FinishPower(__instance, __0, __state.Feeder); break;
                }
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
        finally { foreach (var engine in ChargeMachines.All) engine.Forget(__instance); ProcessorService.Forget(__instance); SabatierService.Forget(__instance); CrackerService.Forget(__instance); FillerService.Forget(__instance); BottlerService.Forget(__instance); FeederService.Forget(__instance); }
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
            case MachineKind.Charge:
                var engine = ChargeMachines.For(machine.strCODef);
                if (engine != null) try { engine.BeforePower(machine); } catch (Exception ex) { engine.Fault(machine, ex); }
                break;
            case MachineKind.Processor: try { ProcessorService.BeforePower(machine); } catch (Exception ex) { ProcessorService.Fault(machine, ex); } break;
            case MachineKind.Sabatier: try { SabatierService.BeforePower(machine); } catch (Exception ex) { SabatierService.Fault(machine, ex); } break;
            case MachineKind.Cracker: try { CrackerService.BeforePower(machine); } catch (Exception ex) { CrackerService.Fault(machine, ex); } break;
            case MachineKind.Filler: try { FillerService.BeforePower(machine); } catch (Exception ex) { FillerService.Fault(machine, ex); } break;
            case MachineKind.Bottler: try { BottlerService.BeforePower(machine); } catch (Exception ex) { BottlerService.Fault(machine, ex); } break;
            case MachineKind.Feeder: try { FeederService.BeforePower(machine); } catch (Exception ex) { FeederService.Fault(machine, ex); } break;
        }
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class FeedPatch
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    {
        if (!__result) return;
        string? id = __instance.CO?.strCODef;
        if (ChargeMachines.ForBin(id) is ChargeMachine machine) __result = machine.CanFeed(__instance.CO!, coIn);
        // The reaction mass feeder takes declared remainders only (Manufacturing 0.43.0), in every form.
        else if (FeederRules.IsFamily(id)) __result = FeederService.CanFeed(coIn);
    }
}

// Ores and our stock stack natively; the feed bin holds individual units bound to a charge, so no merging there.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.CanStackOnItem))]
internal static class FeedStackPatch
{
    private static void Postfix(CondOwner __instance, CondOwner objIncoming, ref int __result)
    {
        if (ChargeMachines.IsBin(__instance.objCOParent?.strCODef) || ChargeMachines.IsBin(objIncoming?.objCOParent?.strCODef)) __result = 0;
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

// The crew-order switches, like the game's own Toggle Power: "Keep suit bottles charged" on the intact installed L2,
// and "Load feed by crew" on the charge machines and the RM-1 (0.56.0).
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class CrewOrderPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.objThem == null || __instance.objUs != CrewSim.GetSelectedCrew()) return;
        bool bottles = __instance.strName == FillerRules.BottleOrder;
        if (!bottles && __instance.strName != CrewFeedRules.FeedOrder) return;
        bool done = bottles ? FillerCrewProvider.Toggle(__instance.objThem, out string message) : FeedCrewProvider.Toggle(__instance.objThem, out message);
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
        if (!__result || __instance.strName == null || !Content.Maintained(objUs) && !Content.Maintained(objThem)) return;
        var reason = MaintenanceFinish.Reason(__instance.strName, objUs, objThem);
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class MaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        var machine = Content.Maintained(us) ? us : Content.Maintained(them) ? them : null;
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
        else if (CrackerRules.IsFamily(coNew.strCODef)) CrackerService.Damaged(coNew);
    }
}
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class StoreDestroyPatch
{
    private static void Prefix(CondOwner __instance)
    {
        // A ship being unloaded is not destroyed in play: the save keeps what its stores and reactors held, so nothing
        // is vented or logged as lost on a reload (Manufacturing 0.32.0).
        if (__instance == null || Phobos.Ostranauts.Framework.FrameworkLifecycle.Unloading(__instance)) return;
        if (GasStores.IsFamily(__instance.strCODef)) StoreService.Destroying(__instance);
        else if (LiquidStores.IsFamily(__instance.strCODef) && !__instance.HasCond("IsModeSwitching", false)) LiquidStoreService.Hazard(__instance, "destroyed_log");
        else if (SabatierRules.IsFamily(__instance.strCODef)) SabatierService.Destroying(__instance);
        else if (CrackerRules.IsFamily(__instance.strCODef)) CrackerService.Destroying(__instance);
    }
}

// Uninstall and reinstall of the X2, K2 and AX-2 (Manufacturing 0.56.1): the game copies the machine's record onto the
// new form; the mass it holds follows here, as Framework does for bulk vessels.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class HoldCarriedPatch
{
    private static void Postfix(CondOwner coNew)
    {
        if (coNew == null) return;
        if (ProcessorRules.IsFamily(coNew.strCODef)) ProcessorService.Carried(coNew);
        else if (SabatierRules.IsFamily(coNew.strCODef)) SabatierService.Carried(coNew);
        else if (CrackerRules.IsFamily(coNew.strCODef)) CrackerService.Carried(coNew);
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        // A trailing confirm word goes ahead with the steps a refusal offered (0.58.0).
        var parts = Phobos.Ostranauts.Framework.Controls.Confirmations.TakeWord(strInput.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries), out bool confirmed);
        if (parts.Length == 0 || !parts[0].Equals("phobosmanufacturing", StringComparison.OrdinalIgnoreCase)) return true;
        if (parts.Length == 1 || parts[1] == "list")
        { strInput += "\n" + string.Join("\n", CrewSim.GetSelectedCrew()?.ship?.GetCOs(null, false, false, true).Where(Content.Machine).Select(c => c.strNameFriendly + " " + c.strID) ?? Array.Empty<string>()); __result = true; return false; }
        var co = parts.Length >= 3 ? Content.Resolve(parts[2]) : null;
        string message = Text.Get("Console.help");
        // The right-click "Load feed by crew" switch, through the same service (0.56.0).
        if (parts[1] == "crew-load") { __result = FeedCrewProvider.Toggle(co!, out message); strInput += "\n" + message; return false; }
        string action = parts.Length == 4 && new[] { "link", "water", "store", "canister", "vent", "hydrogen", "methane", "feed", "order", "source-on", "source-off", "unlink",
            "mode", "target", "draw", "transfer", "o2", "pressure", "oxygen", "nitrogen", "recipe", "ammonia", "gas-link", "acid", "pour", "nutrients" }.Contains(parts[1]) ? parts[1] + ":" + parts[3] : parts[1];
        if (confirmed) action = Phobos.Ostranauts.Framework.Controls.Confirmations.Confirmed(action);
        var provider = new Provider();
        __result = Content.Machine(co) && provider.Command(co!, null, action, out message); strInput += "\n" + message; return false;
    }
}
