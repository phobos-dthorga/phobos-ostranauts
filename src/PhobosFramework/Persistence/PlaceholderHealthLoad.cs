using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Persistence;

// Scope the correction to native saved-item spawning. Templates and unrelated
// destruction checks keep their normal behavior; nested/failed calls unwind.
[HarmonyPatch(typeof(Ship), "SpawnItems")]
internal static class PlaceholderHealthLoad
{
    internal sealed class Scope
    {
        internal readonly Ship Ship;
        internal readonly Dictionary<string, JsonPlaceholder> Markers;
        internal int Retained;
        internal Scope(Ship ship)
        {
            Ship = ship;
            Markers = UniqueMarkers(ship.json.aPlaceholders);
        }
    }

    [ThreadStatic] internal static Scope? Current;

    internal static void Prefix(Ship __instance, bool bTemplateOnly, out Scope? __state)
    {
        __state = Current;
        Current = !bTemplateOnly && __instance.json?.aPlaceholders != null
            ? new Scope(__instance) : null;
    }

    internal static Exception? Finalizer(Exception? __exception, Scope? __state)
    {
        var finished = Current;
        Current = __state;
        if (finished?.Retained > 0)
            FrameworkLifecycle.Log(Text.Get("PlaceholderHealthLoad.retained", finished.Ship.strRegID, finished.Retained));
        return __exception;
    }

    internal static Dictionary<string, JsonPlaceholder> UniqueMarkers(IEnumerable<JsonPlaceholder> markers) => markers
        .Where(m => m != null && !string.IsNullOrEmpty(m.strName))
        .GroupBy(m => m.strName, StringComparer.Ordinal).Where(g => g.Count() == 1)
        .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    internal static bool CanRetain(JsonItem item, JsonPlaceholder marker, JsonCondOwnerSave saved, double genericMaximum)
    {
        if (genericMaximum != 0 || item.strName != "Placeholder" || string.IsNullOrEmpty(item.strID) ||
            item.strID != marker.strName || saved.strID != item.strID || saved.strCODef != "Placeholder" ||
            item.strParentID != null || item.strSlotParentID != null ||
            !saved.bAlive || string.IsNullOrEmpty(marker.strInstalledCO) || string.IsNullOrEmpty(marker.strActionCO) ||
            saved.aCondZeroes?.Any(c => c == "StatDamageMax" || c == "IsPlaceholder") == true) return false;
        return SavedPlaceholderHealth.HasRemainingHealth(item.GetCondAmountOverride("StatDamage"), saved.aConds);
    }
}

[HarmonyPatch(typeof(Ship), "IsItemDestroyed")]
internal static class PlaceholderDamageCheck
{
    internal static bool Prefix(Ship __instance, JsonItem objItem, ref bool __result)
    {
        var scope = PlaceholderHealthLoad.Current;
        if (scope == null || !ReferenceEquals(scope.Ship, __instance) || objItem?.strName != "Placeholder" ||
            string.IsNullOrEmpty(objItem.strID) || !scope.Markers.TryGetValue(objItem.strID, out var marker) ||
            DataHandler.dictCOSaves == null || !DataHandler.dictCOSaves.TryGetValue(objItem.strID, out var saved)) return true;
        if (!KnownDefinition(marker.strInstalledCO) || !KnownDefinition(marker.strActionCO)) return true;
        var generic = DataHandler.GetDataCO("Placeholder");
        if (generic == null || !PlaceholderHealthLoad.CanRetain(objItem, marker, saved, generic.GetMaxHealth())) return true;
        __result = false;
        scope.Retained++;
        return false;
    }

    private static bool KnownDefinition(string id) => !string.IsNullOrEmpty(id) &&
        (DataHandler.dictCOs.ContainsKey(id) || DataHandler.dictCOOverlays.ContainsKey(id));
}
