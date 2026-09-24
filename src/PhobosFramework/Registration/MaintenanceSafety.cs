using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Registration;

internal static class MaintenanceSafety
{
    internal static readonly Dictionary<string, string?> Actions = new Dictionary<string, string?>(StringComparer.Ordinal);
    internal static readonly Dictionary<string, string> Repairs = new Dictionary<string, string>(StringComparer.Ordinal);
    internal static readonly Dictionary<(string Definition, string Action), string> LegacyFinishes = new Dictionary<(string, string), string>();
    internal static string ResolveFinish(string definition, string action) =>
        LegacyFinishes.TryGetValue((definition, action), out var replacement) ? replacement : action;
    internal static int SpentPartUnits(double kg)
    {
        double units = kg * 2;
        return double.IsNaN(units) || double.IsInfinity(units) || units < 0 || units > 200 ||
            Math.Abs(units - Math.Round(units)) > .000001 ? -1 : (int)Math.Round(units);
    }
    internal static void Register(string id, string? internalBin)
    {
        foreach (string action in new[] { "ACT" + id, "ACT" + id + "Allow", "MS" + id }) Actions[action] = internalBin;
    }
    internal static bool Empty(CondOwner? item, string? internalBin) => item != null && !item.bDestroyed &&
        item.GetLotCOs(true).Count == 0 && item.GetCOsSafe(true).All(child =>
            internalBin != null && child.strCODef == internalBin && child.GetTotalMass() == 0 &&
            child.GetCOsSafe(true).Count == 0 && child.GetLotCOs(true).Count == 0);
}

// A saved pre-economy overlay may still have a generic native finish queued.
// Resolve only an explicitly registered object/action pair, before maintenance
// guards run. Vanilla equipment using the same old action remains unchanged.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class LegacyMaintenanceFinishPatch
{
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Interaction __instance)
    {
        var item = __instance.objUs;
        if (item == null) return;
        string replacement = MaintenanceSafety.ResolveFinish(item.strCODef, __instance.strName);
        if (replacement == __instance.strName) return;
        if (!DataHandler.dictInteractions.TryGetValue(replacement, out var definition) || definition.objLootModeSwitch == null) return;
        __instance.strName = replacement;
        __instance.objLootModeSwitch = DataHandler.GetLoot(definition.objLootModeSwitch);
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class RepairRemainderPatch
{
    private static bool Prefix(Interaction __instance)
    {
        if (!MaintenanceSafety.Repairs.TryGetValue(__instance.strName, out var output)) return true;
        var item = __instance.objUs;
        if (item == null || item.bDestroyed) return false;
        // Native ModeSwitch destroys the repair lot. Return exactly that mass,
        // not a guessed bill. Reject unfamiliar fractional/huge lots before effects.
        var lot = item.GetLotCOs(false);
        if (lot.Any(co => co == null || co.bDestroyed || !Construction.ConstructionHooks.HasNoContents(co) || co.GetLotCOs(true).Count != 0))
        { FrameworkLifecycle.Log(Text.Get("MaintenanceSafety.repair_paused_service_material_contains_cargo_for", item.strID)); return false; }
        double kg = lot.Sum(co => co.GetTotalMass());
        int units = MaintenanceSafety.SpentPartUnits(kg);
        if (units < 0)
        { FrameworkLifecycle.Log(Text.Get("MaintenanceSafety.repair_paused_unsupported_service_material_mass_for", item.strID)); return false; }
        __instance.objLootModeSwitch = new Loot { strName = "PhobosRepairReturn", strType = "item",
            aCOs = new[] { output + "=1x1" }.Concat(Enumerable.Repeat(MaintenanceDefinitions.SpentParts + "=1x1", units)).ToArray(),
            aLoots = Array.Empty<string>() };
        return true;
    }
}

[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class DismantleEligibilityPatch
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (!__result || !MaintenanceSafety.Actions.TryGetValue(__instance.strName, out var bin)) return;
        var item = __instance.strName.StartsWith("MS", StringComparison.Ordinal) ? objUs : objThem;
        if (MaintenanceSafety.Empty(item, bin)) return;
        __instance.AddFailReason("main", Text.Get("MaintenanceSafety.empty_the_equipment_its_feed_and_any"));
        __result = false;
    }
}

// Recheck at effects time, including the destruction handler's self-targeted
// finish action. Cargo may have been inserted since the worker started.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class DismantleCompletionPatch
{
    private static bool Prefix(Interaction __instance)
    {
        if (!MaintenanceSafety.Actions.TryGetValue(__instance.strName, out var bin)) return true;
        bool finish = __instance.strName.StartsWith("MS", StringComparison.Ordinal);
        var item = finish ? __instance.objUs : __instance.objThem;
        if (!MaintenanceSafety.Empty(item, bin)) return false;
        if (finish && bin != null)
            foreach (var child in item.GetCOsSafe(true).Where(c => c.strCODef == bin).ToArray()) child.Destroy();
        return true;
    }
}
