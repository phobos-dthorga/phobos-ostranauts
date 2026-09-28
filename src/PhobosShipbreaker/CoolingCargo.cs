using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Explicit local access to cargo admitted by the old cooling definitions.</summary>
internal static class CoolingCargo
{
    private static readonly List<(CondOwner Target, HashSet<CondOwner> Cargo)> Transitions = new();
    internal const string RejectAll = "PhobosCoolingNoNewCargo", Action = "PhobosCoolingRecoverCargo";
    internal static void Add(NativeDefinitions d)
    {
        d.Triggers[RejectAll] = new CondTrigger { strName = RejectAll, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { "IsSolid" }, aForbids = new[] { "IsSolid" }, aTriggers = Array.Empty<string>() };
        var action = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        action.strName = Action; action.strTitle = Text.Get("Maintenance.recover_cargo");
        action.strDesc = action.strTooltip = Text.Get("Maintenance.recovery_help");
        action.strRaiseUI = null; action.CTTestThem = null; action.fTargetPointRange = 2;
        d.Interactions[Action] = action;
        foreach (var co in d.Objects.Values.Where(co => FurnaceRules.Cooling(co.strName)))
            co.aInteractions = co.aInteractions.Concat(new[] { Action }).ToArray();
    }
    internal static bool HasCargo(CondOwner? co) => co != null && !co.bDestroyed && FurnaceRules.Cooling(co.strCODef) &&
        (co.objContainer?.ContainedCOs.Count > 0 || co.GetLotCOs(false).Count > 0);
    internal static int BeginTransition(CondOwner source, CondOwner target)
    {
        int depth = Transitions.Count;
        if (source != null && target != null && FurnaceRules.Cooling(source.strCODef) && FurnaceRules.Cooling(target.strCODef) &&
            FurnaceRules.Underside(source.strCODef) == FurnaceRules.Underside(target.strCODef) && source.objContainer != null)
            Transitions.Add((target, new HashSet<CondOwner>(source.objContainer.ContainedCOs)));
        return depth;
    }
    internal static void EndTransition(int depth) { if (Transitions.Count > depth) Transitions.RemoveRange(depth, Transitions.Count - depth); }
    internal static bool Restoring(Container destination, CondOwner item)
    {
        // AllowedCO is a native hot path: no closures or enumeration allocations when idle.
        for (int i = Transitions.Count - 1; i >= 0; i--)
            if (ReferenceEquals(Transitions[i].Target, destination.CO) && Transitions[i].Cargo.Contains(item)) return true;
        return false;
    }
    internal static bool Open(CondOwner co, out string reason)
    {
        reason = ProcessingService.AccessProblem(co) ?? "";
        if (reason.Length != 0) return false;
        if (!HasCargo(co) || co.objContainer == null || CrewSim.inventoryGUI == null)
        { reason = Text.Get("Maintenance.no_cargo"); return false; }
        // Never turn construction/service lots into free inventory. Native cancellation owns them.
        if (co.HasCond("IsLocked") || co.GetLotCOs(true).Count != 0 || co.GetInteractionCurrent() != null)
        { reason = Text.Get("Maintenance.reserved"); return false; }
        reason = FurnaceService.MaintenanceReason(co, true) ?? "";
        if (reason.Length != 0) return false;
        var cargo = co.objContainer.ContainedCOs.ToArray();
        if (cargo.Any(item => item == null || item.bDestroyed || item.objCOParent != co || item.GetLotCOs(true).Count != 0 || item.GetInteractionCurrent() != null ||
            item.GetCOsSafe(true).Any(child => child.GetLotCOs(true).Count != 0 || child.GetInteractionCurrent() != null)))
        { reason = Text.Get("Maintenance.reserved"); return false; }
        // Older native mode switches can leave IsLotItem on ordinary container children.
        // Only clear this stale marker after proving no actual lot or running item action exists.
        foreach (var item in cargo)
        {
            item.ZeroCondAmount("IsLotItem");
            foreach (var child in item.GetCOsSafe(true)) child.ZeroCondAmount("IsLotItem");
        }
        CrewSim.inventoryGUI.SpawnInventoryWindow(co, global::Ostranauts.Inventory.InventoryWindowType.Container, null);
        reason = Text.Get("Maintenance.recovery_help"); return true;
    }
}

// Native damage/repair switches copy physical cargo through AddCO, which normally
// applies the admission filter. Permit only the captured old cargo and exact new
// cooling object during that synchronous transition, including failure cleanup.
[HarmonyPatch(typeof(CondOwner), nameof(CondOwner.ModeSwitch))]
internal static class CoolingCargoTransition
{
    private static void Prefix(CondOwner __instance, CondOwner coNew, out int __state) => __state = CoolingCargo.BeginTransition(__instance, coNew);
    private static Exception? Finalizer(Exception? __exception, int __state)
    { CoolingCargo.EndTransition(__state); return __exception; }
}
[HarmonyPatch(typeof(Container), nameof(Container.AllowedCO))]
internal static class CoolingCargoAdmission
{
    private static void Postfix(Container __instance, CondOwner coIn, ref bool __result)
    { if (!__result && CoolingCargo.Restoring(__instance, coIn)) __result = true; }
}

[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class CoolingCargoOffer
{
    private static void Postfix(Interaction __instance, CondOwner objThem, ref bool __result)
    { if (__instance.strName == CoolingCargo.Action) __result &= CoolingCargo.HasCargo(objThem); }
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class CoolingCargoAction
{
    private static bool Prefix(Interaction __instance, bool isCancelIa)
    {
        if (__instance.strName != CoolingCargo.Action) return true;
        if (!isCancelIa && __instance.objUs == CrewSim.GetSelectedCrew() && __instance.objThem != null)
        {
            CoolingCargo.Open(__instance.objThem, out string reason);
            __instance.objUs.LogMessage(reason, "Neutral", __instance.objThem.strID);
        }
        return false;
    }
}
