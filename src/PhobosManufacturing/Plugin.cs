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
    public const string Version = "0.2.0";
    public const string MinimumFrameworkVersion = "0.41.0";
    internal static Action<string> Log = _ => { };
    private Harmony? harmony;
    private float nextScan;
    private void Awake()
    {
        Log = x => Logger.LogInfo(x); Text.EnsureLoaded();
        harmony = new Harmony(Id); harmony.PatchAll(typeof(Plugin).Assembly);
        ShipbreakerStock.Detect();
        FrameworkLifecycle.ContentLoading += Load;
        EquipmentProviders.Register(new Provider());
        Log(Text.Get("Plugin.loaded", Version, ShipbreakerStock.PluginPresent ? Text.Get("Plugin.with_shipbreaker") : Text.Get("Plugin.without_shipbreaker")));
    }
    private static void Load() { ResetServices(); Content.Register(Log); }
    internal static void ResetServices() { RefineryService.Reset(); ProcessorService.Reset(); SabatierService.Reset(); StoreService.Reset(); }
    /// <summary>A damaged fuel store has no native tick of its own: every couple of seconds its leak advances.</summary>
    private void Update()
    {
        if (UnityEngine.Time.unscaledTime < nextScan) return;
        nextScan = UnityEngine.Time.unscaledTime + 2;
        if (!Content.Ready || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || DataHandler.mapCOs == null) return;
        foreach (var co in DataHandler.mapCOs.Values.Where(c => c != null && FuelStores.IsFamily(c.strCODef) && c.HasCond("IsDamaged") && c.HasCond("IsInstalled")).ToArray())
            StoreService.Tick(co);
    }
    private void OnDestroy()
    {
        FrameworkLifecycle.ContentLoading -= Load;
        EquipmentProviders.Unregister(Id); Phobos.Ostranauts.Framework.Liquids.BulkVessels.Unregister(Id);
        ResetServices(); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(Powered), "UsePower", new[] { typeof(CondOwner), typeof(double) })]
internal static class PowerPatch
{
    internal sealed class PowerState { internal bool Finished; internal RefineryService.Transfer? Refinery; internal ProcessorService.Transfer? Processor; internal SabatierService.Transfer? Reactor; }
    private static bool Prefix(Powered __instance, CondOwner __0, ref double __1, out PowerState __state)
    {
        __state = new PowerState();
        if (__0 == null) return true;
        if (RefineryRules.IsFamily(__0.strCODef))
        {
            try { return RefineryService.BeginPower(__instance, __0, ref __1, out __state.Refinery); }
            catch (Exception ex) { RefineryService.Fault(__0, ex); return false; }
        }
        if (ProcessorRules.IsFamily(__0.strCODef))
        {
            try { return ProcessorService.BeginPower(__instance, __0, ref __1, out __state.Processor); }
            catch (Exception ex) { ProcessorService.Fault(__0, ex); return false; }
        }
        if (SabatierRules.IsFamily(__0.strCODef))
        {
            try { return SabatierService.BeginPower(__instance, __0, ref __1, out __state.Reactor); }
            catch (Exception ex) { SabatierService.Fault(__0, ex); return false; }
        }
        return true;
    }
    private static void Postfix(Powered __instance, CondOwner __0, PowerState __state)
    {
        if (__0 == null) return;
        if (RefineryRules.IsFamily(__0.strCODef))
        {
            try { RefineryService.FinishPower(__instance, __0, __state.Refinery); RefineryService.AfterPower(__0); }
            catch (Exception ex) { RefineryService.Fault(__0, ex); }
            finally { __state.Finished = true; }
        }
        else if (ProcessorRules.IsFamily(__0.strCODef))
        {
            try { ProcessorService.FinishPower(__instance, __0, __state.Processor); }
            catch (Exception ex) { ProcessorService.Fault(__0, ex); }
            finally { __state.Finished = true; }
        }
        else if (SabatierRules.IsFamily(__0.strCODef))
        {
            try { SabatierService.FinishPower(__instance, __0, __state.Reactor); }
            catch (Exception ex) { SabatierService.Fault(__0, ex); }
            finally { __state.Finished = true; }
        }
    }
    private static void Finalizer(Powered __instance, CondOwner __0, PowerState? __state)
    {
        // An exceptional native call can already have debited electricity: account its witnessed delivery once.
        try
        {
            if (__state != null && !__state.Finished && __0 != null)
            {
                if (RefineryRules.IsFamily(__0.strCODef)) RefineryService.FinishPower(__instance, __0, __state.Refinery);
                else if (ProcessorRules.IsFamily(__0.strCODef)) ProcessorService.FinishPower(__instance, __0, __state.Processor);
                else if (SabatierRules.IsFamily(__0.strCODef)) SabatierService.FinishPower(__instance, __0, __state.Reactor);
            }
        }
        catch (Exception ex) { Plugin.Log(ex.Message); }
        finally
        {
            if (__0 != null && (RefineryRules.IsFamily(__0.strCODef) || ProcessorRules.IsFamily(__0.strCODef) || SabatierRules.IsFamily(__0.strCODef)))
            { RefineryService.Forget(__instance); ProcessorService.Forget(__instance); SabatierService.Forget(__instance); }
        }
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
        if (RefineryRules.IsFamily(machine.strCODef)) { try { RefineryService.BeforePower(machine); } catch (Exception ex) { RefineryService.Fault(machine, ex); } }
        else if (ProcessorRules.IsFamily(machine.strCODef)) { try { ProcessorService.BeforePower(machine); } catch (Exception ex) { ProcessorService.Fault(machine, ex); } }
        else if (SabatierRules.IsFamily(machine.strCODef)) { try { SabatierService.BeforePower(machine); } catch (Exception ex) { SabatierService.Fault(machine, ex); } }
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

// Removal work is refused when it is offered, never by blocking native destruction; a refused finish still
// closes the game's task (owner decision, 28 September 2026).
[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class MaintenanceOffer
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        var reason = __result ? MaintenanceFinish.Reason(__instance.strName, objUs, objThem) : null;
        if (reason != null) { __instance.AddFailReason("main", reason); __result = false; }
    }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class MaintenanceFinish
{
    internal static string? Reason(string action, CondOwner? us, CondOwner? them)
    {
        bool dismantle = action.IndexOf("Dismantle", StringComparison.OrdinalIgnoreCase) >= 0;
        bool removal = dismantle || action.IndexOf("Uninstall", StringComparison.OrdinalIgnoreCase) >= 0;
        if (!removal) return null;
        var machine = new[] { us, them }.FirstOrDefault(Content.Machine);
        return machine == null ? null : Content.MaintenanceReason(machine, dismantle);
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
        if (FuelStores.IsFamily(coNew.strCODef)) StoreService.Damaged(coNew);
        else if (SabatierRules.IsFamily(coNew.strCODef)) SabatierService.Damaged(coNew);
    }
}
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.Destroy))]
internal static class StoreDestroyPatch
{
    private static void Prefix(CondOwner __instance)
    {
        if (__instance == null) return;
        if (FuelStores.IsFamily(__instance.strCODef)) StoreService.Destroying(__instance);
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
        string action = parts.Length == 4 && new[] { "link", "water", "store", "canister", "vent", "hydrogen", "methane" }.Contains(parts[1]) ? parts[1] + ":" + parts[3] : parts[1];
        var provider = new Provider();
        __result = Content.Machine(co) && provider.Command(co!, null, action, out message); strInput += "\n" + message; return false;
    }
}
