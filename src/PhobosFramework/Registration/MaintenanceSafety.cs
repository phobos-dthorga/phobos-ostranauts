using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Registration;

internal static class MaintenanceSafety
{
    internal static readonly Dictionary<string, string?> Actions = new Dictionary<string, string?>(StringComparer.Ordinal);
    internal static readonly Dictionary<(string Definition, string Action), string> LegacyFinishes = new Dictionary<(string, string), string>();
    internal static string ResolveFinish(string definition, string action) =>
        LegacyFinishes.TryGetValue((definition, action), out var replacement) ? replacement : action;
    internal static void Register(string id, string? internalBin)
    {
        foreach (string action in new[] { "ACT" + id, "ACT" + id + "Allow", "MS" + id }) Actions[action] = internalBin;
    }
    // An internal bin's own mass is a rounded native stat; compare within the shared mass tolerance.
    internal static bool Empty(CondOwner? item, string? internalBin) => item != null && !item.bDestroyed &&
        item.GetLotCOs(true).Count == 0 && item.GetCOsSafe(true).All(child =>
            internalBin != null && child.strCODef == internalBin && Math.Abs(child.GetTotalMass()) <= Units.MassToleranceKg &&
            child.GetCOsSafe(true).Count == 0 && child.GetLotCOs(true).Count == 0);
    internal static string? Reason(CondOwner? item, string? internalBin)
    {
        if (item == null || item.bDestroyed) return Text.Get("MaintenanceInfo.gone");
        if (item.coStackHead != null || item.aStack?.Count > 0) return Text.Get("MaintenanceInfo.stack");
        if (item.GetLotCOs(true).Count != 0) return Text.Get("MaintenanceInfo.lot");
        return Empty(item, internalBin) ? null : Text.Get("MaintenanceInfo.contents");
    }
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
        // Runs for every completed interaction in the game: nothing registered, or no name, is the common case.
        if (MaintenanceSafety.LegacyFinishes.Count == 0 || __instance.strName == null) return;
        var item = __instance.objUs;
        if (item == null) return;
        string replacement = MaintenanceSafety.ResolveFinish(item.strCODef, __instance.strName);
        if (replacement == __instance.strName) return;
        if (!DataHandler.dictInteractions.TryGetValue(replacement, out var definition) || definition.objLootModeSwitch == null) return;
        __instance.strName = replacement;
        __instance.objLootModeSwitch = DataHandler.GetLoot(definition.objLootModeSwitch);
    }
}

// Repairs finish the game's way since Framework 0.74.0: the native mode switch consumes the parts and returns the
// repaired item. The former finish hook that turned the parts into Spent Service Parts is gone.

[HarmonyPatch(typeof(Interaction), "TriggeredInternal")]
internal static class DismantleEligibilityPatch
{
    private static void Postfix(Interaction __instance, CondOwner objUs, CondOwner objThem, ref bool __result)
    {
        if (!__result || __instance.strName == null || !MaintenanceSafety.Actions.TryGetValue(__instance.strName, out var bin)) return;
        var item = __instance.strName.StartsWith("MS", StringComparison.Ordinal) ? objUs : objThem;
        if (MaintenanceSafety.Empty(item, bin)) return;
        __instance.AddFailReason("main", MaintenanceSafety.Reason(item, bin) ?? Text.Get("MaintenanceSafety.empty_the_equipment_its_feed_and_any"));
        __result = false;
    }
}

// Recheck at effects time, including the destruction handler's self-targeted
// finish action. Cargo may have been inserted since the worker started. A refusal
// still closes the game's task for the action, as native effects would.
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class DismantleCompletionPatch
{
    private static bool Prefix(Interaction __instance)
    {
        if (__instance.strName == null || !MaintenanceSafety.Actions.TryGetValue(__instance.strName, out var bin)) return true;
        bool finish = __instance.strName.StartsWith("MS", StringComparison.Ordinal);
        var item = finish ? __instance.objUs : __instance.objThem;
        if (!MaintenanceSafety.Empty(item, bin))
            return NativeEffects.Refuse(__instance, MaintenanceSafety.Reason(item, bin) ?? Text.Get("MaintenanceSafety.empty_the_equipment_its_feed_and_any"));
        if (finish && bin != null)
            foreach (var child in item.GetCOsSafe(true).Where(c => c.strCODef == bin).ToArray()) child.Destroy();
        return true;
    }
}
