using System;
using System.Collections.Generic;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using UnityEngine;

namespace PhobosShipbreaker;

// Native 1.0.1.5 spawns 1x1 markers, assigns saved room/zone indices, and only
// then restores full construction footprints. Preserve the serialized grid
// before either index consumer runs. Full placeholder restoration remains native.
[HarmonyPatch(typeof(Ship), "SpawnItems")]
internal static class PlaceholderLoadRepair
{
    internal static void Postfix(Ship __instance, bool bTemplateOnly, Ship.Loaded nLoad,
        Dictionary<string, CondOwner> dictPlaceholders, GameObject ___goTiles)
    {
        if (bTemplateOnly || nLoad <= Ship.Loaded.Shallow || !Content.Ready ||
            __instance.LoadState > Ship.Loaded.Shallow || ___goTiles == null) return;
        var saved = __instance.json;
        if (saved?.aPlaceholders == null || saved.aRooms == null || saved.aRooms.Length == 0 ||
            dictPlaceholders == null || __instance.aTiles == null ||
            (long)__instance.nCols * __instance.nRows != __instance.aTiles.Count) return;

        int largestFootprint = 0;
        foreach (var marker in saved.aPlaceholders)
        {
            if (marker == null || string.IsNullOrEmpty(marker.strName) || !Content.OwnsInstalledDefinition(marker.strInstalledCO) ||
                !dictPlaceholders.TryGetValue(marker.strName, out var spawned) || spawned == null ||
                !spawned.HasCond("IsPlaceholder") ||
                !DataHandler.dictCOs.TryGetValue(marker.strInstalledCO, out var co) ||
                !DataHandler.dictItemDefs.TryGetValue(co.strItemDef, out var item) ||
                item.nCols <= 0 || item.aSocketAdds == null) continue;
            largestFootprint = Math.Max(largestFootprint,
                Math.Max(item.nCols, item.aSocketAdds.Length / item.nCols));
        }
        if (largestFootprint <= 1) return;
        if (!SavedGridBounds.TryPlan(__instance.nCols, __instance.nRows,
            __instance.vShipPos.x, __instance.vShipPos.y, saved.nCols, saved.nRows,
            saved.vShipPos.x, saved.vShipPos.y, largestFootprint, out var padding))
        {
            Plugin.Log(Text.Get("PlaceholderLoadRepair.unsupported", __instance.strRegID));
            return;
        }
        if (!padding.Required) return;
        int columns = __instance.nCols, rows = __instance.nRows;
        TileUtils.PadTilemap(__instance, ___goTiles, padding.Left, padding.Right, padding.Top, padding.Bottom);
        Plugin.Log(Text.Get("PlaceholderLoadRepair.restored", __instance.strRegID,
            columns, rows, __instance.nCols, __instance.nRows));
    }
}
