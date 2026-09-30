using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Joint art for equipment with ports on more than one line family (Framework 0.56.0). The game redraws a
/// placed item's neighbouring sheet pieces only for the item's own sprite-sheet trigger (one per item); this postfix
/// does the same for the further families registered by <see cref="LineDefinitions.AddPort"/>, with the game's own
/// neighbour rule (cardinal tiles, no docked ships). Presentation only, guarded, never throws into the game.</summary>
public static class LineJoints
{
    private static readonly Dictionary<string, List<string>> extra = new(StringComparer.Ordinal);
    private static readonly List<CondOwner> buffer = new();
    private static bool failed;
    internal static void Register(string definition, string spriteTrigger)
    {
        if (!extra.TryGetValue(definition, out var list)) extra[definition] = list = new List<string>();
        if (!list.Contains(spriteTrigger)) list.Add(spriteTrigger);
    }
    public static IReadOnlyList<string> Extra(string definition) => extra.TryGetValue(definition, out var list) ? list : (IReadOnlyList<string>)Array.Empty<string>();
    internal static void Reset() => extra.Clear();

    [HarmonyPatch(typeof(Ship), "UpdateTiles")]
    private static class RefreshPatch
    {
        private static void Postfix(Ship __instance, CondOwner objICO)
        {
            if (failed || objICO?.strCODef == null || !extra.TryGetValue(objICO.strCODef, out var triggers) || objICO.Item == null || __instance == null) return;
            try
            {
                var item = objICO.Item; var centre = objICO.tf.position;
                var seen = new HashSet<Tile>();
                for (int i = 0; i < Math.Max(1, item.nWidthInTiles); i++)
                    for (int j = 0; j < Math.Max(1, item.nHeightInTiles); j++)
                    {
                        var point = new Vector2(centre.x + i - (item.nWidthInTiles - 1) / 2f, centre.y + j - (item.nHeightInTiles - 1) / 2f);
                        int index = __instance.GetTileIndexAtWorldCoords1(point);
                        var tile = index >= 0 ? __instance.GetTileByIndex(index) : null;
                        if (tile == null) continue;
                        seen.Add(tile);
                        foreach (var near in TileUtils.GetSurroundingTiles(tile, true, false)) if (near != null) seen.Add(near);
                    }
                foreach (string name in triggers)
                {
                    if (DataHandler.dictCTs == null || !DataHandler.dictCTs.TryGetValue(name, out var trigger)) continue;
                    foreach (var tile in seen)
                    {
                        if (tile.coProps == null || !trigger.Triggered(tile.coProps)) continue;
                        buffer.Clear();
                        __instance.GetCOsAtWorldCoords1(tile.tf.position, null, false, true, buffer);
                        foreach (var co in buffer)
                            if (co?.Item?.ctSpriteSheet?.strName == name) co.Item.SetSpriteSheetIndex(TileUtils.GetSurroundingTiles(tile, true, false));
                    }
                }
            }
            catch (Exception e) { failed = true; FrameworkLifecycle.Log(Text.Get("LineJoints.failed", e.Message)); }
            finally { buffer.Clear(); }
        }
    }
}

/// <summary>The PDA's paint filter (Framework 0.56.0): our line segments count as Conduits, like the game's own
/// power conduit, and not as Equipment, so painting Uninstall on a tile with pipe under a machine takes only what
/// the player's filter selects. The game rebuilds its filter trigger from the toggles; this postfix adds our lines
/// under Conduits and excludes them from the rest, by each family's own machine condition (which lines saved
/// before this version already carry). The game's conduit trigger itself is never changed: it also drives the
/// power conduit's own install and repair jobs.</summary>
public static class LineJobFilter
{
    public const string AnyLine = "TIsPhobosLineAny", InstalledLine = "TIsPhobosLineInstalled";
    private static readonly List<string> machineConditions = new();
    internal static void RegisterFamily(string prefix) { string cond = prefix + "Machine"; if (!machineConditions.Contains(cond)) machineConditions.Add(cond); }
    public static IReadOnlyList<string> MachineConditions => machineConditions;
    internal static void Reset() => machineConditions.Clear();
    /// <summary>Our two triggers: any line segment (any family's machine condition), and an installed one.</summary>
    public static CondTrigger[] Triggers() => new[]
    {
        new CondTrigger { strName = AnyLine, fChance = 1, fCount = 1, bAND = false, aReqs = machineConditions.ToArray(), aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() },
        new CondTrigger { strName = InstalledLine, fChance = 1, fCount = 1, bAND = true, aReqs = new[] { "IsInstalled" }, aForbids = Array.Empty<string>(), aTriggers = new[] { AnyLine } }
    };
    /// <summary>Adjusts the game's composite filter: with Conduits on, our installed lines join it; otherwise they are
    /// excluded even when another toggle (Equipment, Loose) would take them.</summary>
    public static void Apply(CondTrigger filter, bool conduits)
    {
        if (filter == null || machineConditions.Count == 0) return;
        if (conduits) filter.aTriggers = (filter.aTriggers ?? Array.Empty<string>()).Concat(new[] { InstalledLine }).Distinct().ToArray();
        else filter.aForbids = (filter.aForbids ?? Array.Empty<string>()).Concat(machineConditions).Distinct().ToArray();
    }
    [HarmonyPatch(typeof(GUIPDA), "UpdateFilterCT")]
    private static class FilterPatch
    {
        private static void Postfix(GUIPDA __instance)
        {
            try
            {
                if (machineConditions.Count == 0 || DataHandler.dictCTs == null) return;
                foreach (var trigger in Triggers()) DataHandler.dictCTs[trigger.strName] = trigger;
                var filter = Traverse.Create(typeof(GUIPDA)).Field("ctJobFilter").GetValue<CondTrigger>();
                var toggle = Traverse.Create(__instance).Field("chkFilterConduits").GetValue<UnityEngine.UI.Toggle>();
                Apply(filter, toggle != null && toggle.isOn);
            }
            catch (Exception e) { FrameworkLifecycle.Log(Text.Get("LineJoints.filter_failed", e.Message)); }
        }
    }
}
