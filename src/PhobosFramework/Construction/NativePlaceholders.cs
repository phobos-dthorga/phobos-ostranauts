using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Inventory;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>Whether a placeholder for a part would let crew walk through its tiles.</summary>
public enum PlaceholderFootprint { Walkable, Blocks, Unknown }

public enum PlaceholderLayResult { Laid, UnknownPart, NoInstallJob, DoesNotFit, PlayerBusy, ShipUnavailable, Failed }

/// <summary>Lays the game's own construction placeholders (the ghost build site the PDA INSTALL tab paints)
/// from code, for any part with a registered install job, vanilla or modded. The crew then build it
/// through the native Construct duty with a real part; nothing is created for free.
/// <para>The laying path mirrors how the game rebuilds saved placeholders when a ship loads: a temporary
/// installed part at the spot, the job's action object, <c>DataHandler.GetCOPlaceholder</c>, then
/// <c>Ship.AddCO</c>. A placeholder carries its part's full tile footprint, so a wall or machine
/// placeholder blocks walking exactly as the finished part would; <see cref="Footprint"/> tells callers
/// which parts do.</para></summary>
public static class NativePlaceholders
{
    /// <summary>Tile conditions that make <c>Tile.IsWalkable</c> refuse a tile (a door placeholder is not a working door).</summary>
    public static readonly IReadOnlyCollection<string> BlockingTileConditions = new[] { "IsWall", "IsObstruction" };

    private static Dictionary<string, string>? intactForms;
    private static readonly Dictionary<string, PlaceholderFootprint> footprints = new(StringComparer.Ordinal);

    /// <summary>The native install job that places this installed definition from an INSTALL tab, or null when
    /// none is registered. Jobs without a tab (the game's re-install of damaged forms) are not offered.</summary>
    public static JsonInstallable? InstallJobFor(string installedDefinition)
    {
        if (string.IsNullOrEmpty(installedDefinition) || Installables.dictJobBuildOptions == null) return null;
        foreach (var pair in Installables.dictJobBuildOptions)
            if (!string.IsNullOrEmpty(pair.Key) && pair.Value != null && pair.Value.TryGetValue(installedDefinition, out var job) && job != null) return job;
        return null;
    }

    /// <summary>The INSTALL tab (build type) that lists this part, or null.</summary>
    public static string? MenuFor(string installedDefinition)
    {
        if (string.IsNullOrEmpty(installedDefinition) || Installables.dictJobBuildOptions == null) return null;
        foreach (var pair in Installables.dictJobBuildOptions)
            if (!string.IsNullOrEmpty(pair.Key) && pair.Value != null && pair.Value.ContainsKey(installedDefinition)) return pair.Key;
        return null;
    }

    /// <summary>The installed definition a build site should place so the crew rebuild what stood there, or null.
    /// Follows damage back to the intact form, then, for a working state with no install job of its own
    /// (a closed or locked door, an alarm showing red, a machine switched on), the game's own uninstall job:
    /// the loose part it yields and the INSTALL job that takes that loose part. The rebuilt part starts in
    /// the state its install job gives it, as when a player places it.</summary>
    public static string? RebuildTarget(string definition)
    {
        if (string.IsNullOrEmpty(definition)) return null;
        foreach (var candidate in new[] { IntactFormOf(definition), definition }.Distinct())
        {
            if (InstallJobFor(candidate) != null) return candidate;
            foreach (var loose in UninstallProducts(candidate))
            {
                var job = InstallJobTaking(loose);
                if (job != null) return job.strStartInstall;
            }
        }
        return null;
    }

    private static IEnumerable<string> UninstallProducts(string installedDefinition)
    {
        if (DataHandler.dictInstallables == null) yield break;
        foreach (var job in DataHandler.dictInstallables.Values)
        {
            if (job == null || job.strJobType != "uninstall" || job.strActionCO != installedDefinition) continue;
            foreach (var product in job.aLootCOs ?? Array.Empty<string>())
            {
                var name = EntryName(product);
                if (name.Length > 0) yield return name;
            }
            if (!string.IsNullOrEmpty(job.strLootOut)) foreach (var name in LootNames(job.strLootOut)) yield return name;
        }
    }

    private static JsonInstallable? InstallJobTaking(string looseDefinition)
    {
        if (Installables.dictJobBuildOptions == null) return null;
        // Deterministic across runs: tabs and parts in ordinal order.
        foreach (var menu in Installables.dictJobBuildOptions.Where(p => !string.IsNullOrEmpty(p.Key) && p.Value != null).OrderBy(p => p.Key, StringComparer.Ordinal))
            foreach (var job in menu.Value.Values.Where(j => j != null && j.strActionCO == looseDefinition).OrderBy(j => j.strStartInstall, StringComparer.Ordinal))
                return job;
        return null;
    }

    /// <summary>The intact definition a damaged form came from, following each definition's
    /// <c>Destructable,StatDamage,…</c> damage loot to the form its mode switch produces
    /// (for example <c>ItmWall1x1Dmg</c> to <c>ItmWall1x1</c>). Returns the input when it is not a damaged form.</summary>
    public static string IntactFormOf(string definition)
    {
        if (string.IsNullOrEmpty(definition)) return definition;
        intactForms ??= BuildIntactForms();
        // A cosmetic overlay (for example a branded conduit) is damaged through its base definition;
        // follow the base, then map back through the overlay's own base-to-overlay pairs.
        if (DataHandler.dictCOOverlays != null && DataHandler.dictCOOverlays.TryGetValue(definition, out var overlay) && overlay != null &&
            DataHandler.dictCOs?.ContainsKey(definition) != true)
        {
            var intactBase = FollowDamage(overlay.strCOBase);
            return OverlayPairs(overlay).TryGetValue(intactBase, out var intactOverlay) ? intactOverlay : definition;
        }
        return FollowDamage(definition);
    }

    private static string FollowDamage(string definition)
    {
        string current = definition;
        // Several damage stages may chain (intact to damaged to worse); follow back to the first form.
        for (int guard = 0; guard < 8 && intactForms!.TryGetValue(current, out var source); guard++) current = source;
        return current;
    }

    /// <summary>An overlay's <c>mapModeSwitches</c> list read as base-form to overlay-form pairs, as <c>COOverlay.ModeSwitch</c> uses it.</summary>
    private static Dictionary<string, string> OverlayPairs(JsonCOOverlay overlay)
    {
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        var list = overlay.mapModeSwitches ?? Array.Empty<string>();
        for (int i = 0; i + 1 < list.Length; i += 2) pairs[list[i]] = list[i + 1];
        return pairs;
    }

    /// <summary>The object definition behind a name: itself, or an overlay's base definition.</summary>
    private static JsonCondOwner? Definition(string name)
    {
        if (string.IsNullOrEmpty(name) || DataHandler.dictCOs == null) return null;
        if (DataHandler.dictCOs.TryGetValue(name, out var co)) return co;
        return DataHandler.dictCOOverlays != null && DataHandler.dictCOOverlays.TryGetValue(name, out var overlay) && overlay?.strCOBase != null &&
            DataHandler.dictCOs.TryGetValue(overlay.strCOBase, out var baseCo) ? baseCo : null;
    }

    /// <summary>Whether a placeholder of this installed definition would block crew movement on any tile.</summary>
    public static PlaceholderFootprint Footprint(string installedDefinition)
    {
        if (string.IsNullOrEmpty(installedDefinition)) return PlaceholderFootprint.Unknown;
        if (footprints.TryGetValue(installedDefinition, out var known)) return known;
        var tiles = TileConditions(installedDefinition);
        var result = tiles == null ? PlaceholderFootprint.Unknown :
            tiles.Any(BlockingTileConditions.Contains) ? PlaceholderFootprint.Blocks : PlaceholderFootprint.Walkable;
        footprints[installedDefinition] = result;
        return result;
    }

    /// <summary>The conditions this installed definition adds to the tiles it covers (for example <c>IsFloor</c>,
    /// <c>IsWall</c>), or null when the definition or its item is unknown.</summary>
    public static IReadOnlyCollection<string>? TileConditions(string installedDefinition)
    {
        var co = Definition(installedDefinition);
        if (co?.strItemDef == null || DataHandler.dictItemDefs == null || !DataHandler.dictItemDefs.TryGetValue(co.strItemDef, out var item) || item == null)
            return null;
        return (item.aSocketAdds ?? Array.Empty<string>()).SelectMany(LootNames).Distinct().ToArray();
    }

    /// <summary>The native conditions an installed definition starts with (for example <c>IsNavStation</c>).</summary>
    public static IReadOnlyCollection<string> StartingConditions(string definition)
    {
        var co = Definition(definition);
        if (co?.aStartingConds == null) return Array.Empty<string>();
        return co.aStartingConds.Select(EntryName).Where(n => n.Length > 0).Distinct().ToArray();
    }

    /// <summary>Lays one placeholder for <paramref name="installedDefinition"/> centred at <paramref name="position"/>
    /// with the given rotation, the way a player painting the INSTALL tab would. Never throws into the game.</summary>
    public static PlaceholderLayResult TryLay(Ship ship, string installedDefinition, Vector3 position, float rotation, out string detail)
    {
        detail = "";
        if (ship == null || ship.bDestroyed || ship.LoadState < Ship.Loaded.Edit) return PlaceholderLayResult.ShipUnavailable;
        if (Definition(installedDefinition) == null) return PlaceholderLayResult.UnknownPart;
        var job = InstallJobFor(installedDefinition);
        if (job == null || string.IsNullOrEmpty(job.strInteractionName) || string.IsNullOrEmpty(job.strActionCO)) return PlaceholderLayResult.NoInstallJob;
        // The native fit check measures from the selected inventory item when one is held; wait until it is put away.
        if (GUIInventory.instance != null && GUIInventory.instance.Selected != null) return PlaceholderLayResult.PlayerBusy;

        CondOwner? part = null, action = null;
        try
        {
            part = DataHandler.GetCondOwner(installedDefinition);
            if (part == null || part.Item == null) return PlaceholderLayResult.UnknownPart;
            part.tf.position = new Vector3(position.x, position.y, part.tf.position.z);
            part.Item.fLastRotation = rotation;
            if (!part.Item.CheckFit(part.tf.position, ship)) return PlaceholderLayResult.DoesNotFit;

            var install = DataHandler.GetInteraction(job.strInteractionName);
            if (install == null) return PlaceholderLayResult.NoInstallJob;
            action = DataHandler.GetCondOwner(job.strActionCO);
            if (action == null) return PlaceholderLayResult.NoInstallJob;
            action.tf.position = part.tf.position;
            action.strPersistentCO = job.strPersistentCO;
            action.strPersistentCT = install.CTTestThem?.strName;
            var placeholder = DataHandler.GetCOPlaceholder(part, action, install.strName);
            if (placeholder == null) return PlaceholderLayResult.Failed;
            ship.AddCO(placeholder, bTiles: true);
            detail = placeholder.strID;
            return PlaceholderLayResult.Laid;
        }
        catch (Exception ex)
        {
            detail = ex.Message;
            return PlaceholderLayResult.Failed;
        }
        finally
        {
            // Temporaries are never added to a ship; the placeholder copied what it needs (as the load path does).
            try { action?.Destroy(); } catch { }
            try { part?.Destroy(); } catch { }
        }
    }

    /// <summary>Drops cached lookups. Consumers call it from their <c>FrameworkLifecycle.ContentLoading</c> handler;
    /// the lookups rebuild lazily on first use after every mod has registered its definitions.</summary>
    public static void Reset() { intactForms = null; footprints.Clear(); }

    private static Dictionary<string, string> BuildIntactForms()
    {
        var sources = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (DataHandler.dictCOs == null || DataHandler.dictInteractions == null) return new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in DataHandler.dictCOs)
        {
            // Only installed forms that stay installed when damaged; scrap and loose products are not rebuilt from.
            var commands = pair.Value?.aUpdateCommands;
            if (commands == null || !Installed(pair.Value)) continue;
            foreach (var command in commands)
            {
                var parts = command?.Split(',');
                if (parts == null || parts.Length < 3 || parts[0] != "Destructable" || parts[1] != "StatDamage") continue;
                foreach (var interaction in LootNames(parts[2]))
                {
                    if (!DataHandler.dictInteractions.TryGetValue(interaction, out var ia) || string.IsNullOrEmpty(ia?.objLootModeSwitch)) continue;
                    foreach (var product in LootNames(ia!.objLootModeSwitch))
                        if (product != pair.Key && DataHandler.dictCOs.TryGetValue(product, out var made) && Installed(made))
                        {
                            if (!sources.TryGetValue(product, out var list)) sources[product] = list = new List<string>();
                            if (!list.Contains(pair.Key)) list.Add(pair.Key);
                        }
                }
            }
        }
        // Cosmetic variants can share one damaged form: prefer the source the damaged name extends
        // (ItmWall1x1 for ItmWall1x1Dmg), otherwise the first by ordinal name, so the choice never varies.
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in sources)
            map[pair.Key] = pair.Value.Where(s => pair.Key.StartsWith(s, StringComparison.Ordinal)).OrderByDescending(s => s.Length)
                .Concat(pair.Value.OrderBy(s => s, StringComparer.Ordinal)).First();
        // A form that is itself somebody's source is not also damaged from it (guards accidental cycles).
        foreach (var key in map.Keys.ToArray())
            if (map.TryGetValue(map[key], out var back) && back == key) map.Remove(key);
        return map;
    }

    private static bool Installed(JsonCondOwner? definition) =>
        definition?.aStartingConds != null && definition.aStartingConds.Any(c => c.StartsWith("IsInstalled=", StringComparison.Ordinal));

    /// <summary>Every name a loot table can yield, without the game's random roll (nested tables followed once each).</summary>
    internal static IEnumerable<string> LootNames(string? loot) => LootNames(loot, new HashSet<string>(StringComparer.Ordinal));
    private static IEnumerable<string> LootNames(string? loot, HashSet<string> seen)
    {
        if (string.IsNullOrEmpty(loot) || DataHandler.dictLoot == null || !seen.Add(loot!) || !DataHandler.dictLoot.TryGetValue(loot!, out var table) || table == null)
            yield break;
        foreach (var entry in table.aCOs ?? Array.Empty<string>())
            foreach (var option in entry.Split('|'))
            {
                var name = EntryName(option);
                if (name.Length > 0) yield return name;
            }
        foreach (var entry in table.aLoots ?? Array.Empty<string>())
            foreach (var option in entry.Split('|'))
                foreach (var nested in LootNames(EntryName(option), seen)) yield return nested;
    }

    /// <summary>"IsWall=1.0x1" and "-Name=…" to their bare names.</summary>
    internal static string EntryName(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry)) return "";
        var name = entry.Trim();
        int equals = name.IndexOf('=');
        if (equals >= 0) name = name.Substring(0, equals);
        return name.TrimStart('-').Trim();
    }
}
