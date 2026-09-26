using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Persistence;

internal static class SavedGridHeader
{
    internal static bool TryResolve(JsonShip saved, out int columns, out int rows)
    {
        columns = rows = 0;
        if (saved?.aRooms == null || saved.aRooms.Length == 0 || saved.aItems == null) return false;
        // Correct only the observed stale-size case; ordinary loads do no work.
        long count = (long)saved.nCols * saved.nRows;
        int maximum = -1;
        foreach (var room in saved.aRooms)
        {
            if (room?.aTiles == null || room.aTiles.Length == 0) return false;
            foreach (int tile in room.aTiles) maximum = Math.Max(maximum, tile);
        }
        if (count <= 0 || maximum < 0 || (long)maximum + 1 >= count) return false;
        var roomIds = new HashSet<string>(saved.aRooms.Select(r => r.strID), StringComparer.Ordinal);
        var markers = new Dictionary<string, JsonItem>(StringComparer.Ordinal);
        foreach (var item in saved.aItems)
        {
            if (item == null || item.strID == null || !roomIds.Contains(item.strID)) continue;
            if (item.strName != "Compartment" || item.strParentID != null || item.strSlotParentID != null ||
                markers.ContainsKey(item.strID)) return false;
            markers.Add(item.strID, item);
        }
        var shapes = new List<SavedRoomShape>();
        foreach (var room in saved.aRooms)
        {
            if (string.IsNullOrEmpty(room.strID) || !markers.TryGetValue(room.strID, out var marker)) return false;
            shapes.Add(new SavedRoomShape(room.strID, room.bVoid, room.aTiles, marker.fX, marker.fY));
        }
        if (saved.aZones?.Any(z => z == null || z.aTiles == null) == true) return false;
        return SavedRoomGrid.TryResolve(saved.nCols, saved.nRows, saved.vShipPos.x, saved.vShipPos.y,
            shapes, saved.aZones?.SelectMany(z => z.aTiles) ?? Enumerable.Empty<int>(), out columns, out rows);
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.InitShip))]
internal static class SavedGridHeaderLoad
{
    internal static void Prefix(Ship __instance, bool bTemplateOnly, Ship.Loaded nLoad)
    {
        if (bTemplateOnly || nLoad <= Ship.Loaded.Shallow || __instance.LoadState > Ship.Loaded.Shallow ||
            !SavedGridHeader.TryResolve(__instance.json, out int columns, out int rows)) return;
        var saved = __instance.json;
        int oldColumns = saved.nCols, oldRows = saved.nRows;
        saved.nCols = columns; saved.nRows = rows;
        FrameworkLifecycle.Log(Text.Get("SavedGridHeader.load", saved.strRegID, oldColumns, oldRows, columns, rows));
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.GetJSON))]
internal static class SavedGridHeaderSave
{
    // The native method has now trimmed the live grid and serialized its room
    // and zone indices. Correct the outgoing DTO, never the live geometry.
    internal static void Postfix(Ship __instance, bool bSaveGame, JsonShip __result)
    {
        if (!bSaveGame || __instance.LoadState < Ship.Loaded.Edit || __result == null ||
            __instance.nCols <= 0 || __instance.nRows <= 0 || __instance.aTiles == null ||
            (long)__instance.nCols * __instance.nRows != __instance.aTiles.Count ||
            __result.nCols < __instance.nCols || __result.nRows < __instance.nRows ||
            __result.vShipPos.x != __instance.vShipPos.x || __result.vShipPos.y != __instance.vShipPos.y ||
            (__result.nCols == __instance.nCols && __result.nRows == __instance.nRows)) return;
        int oldColumns = __result.nCols, oldRows = __result.nRows;
        __result.nCols = __instance.nCols; __result.nRows = __instance.nRows;
        FrameworkLifecycle.Log(Text.Get("SavedGridHeader.save", __instance.strRegID, oldColumns, oldRows, __result.nCols, __result.nRows));
    }
}
