using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>lines</c> schema (Framework 0.100.0; owner direction, 5 October 2026: rules of this kind are data,
/// not code). It says where a pipe or conduit segment counts as laid: which tiles carry nothing at all, and what must
/// stand, installed and intact, on a tile for a segment there to join its line. One rule is named <c>default</c>;
/// a line family may have its own rule under its family id. Player and add-on files tune or add rules like any
/// other pack.</summary>
public sealed class LinePack : DataPack
{
    /// <summary>Placement rules by name: <c>default</c>, or a line family id.</summary>
    public Dictionary<string, LinePlacementEntry> placement = new(StringComparer.Ordinal);
}

public sealed class LinePlacementEntry
{
    public string? notes;
    /// <summary>Tile conditions on which a segment never counts (flex floor, outside tiles).</summary>
    public List<string> forbiddenTiles = new();
    /// <summary>What holds a segment up. One match is enough.</summary>
    public List<LineSupportEntry> supports = new();
}

public sealed class LineSupportEntry
{
    public string? notes;
    /// <summary>The condition the tile must carry (<c>IsFloor</c>, <c>IsWall</c>).</summary>
    public string tile = "";
    /// <summary>What must stand on that tile, installed and intact: the word <c>floor</c> for any of the game's floors,
    /// or a condition the object carries (<c>IsWall</c>).</summary>
    public string @object = "";
}

public static class LineSchema
{
    public const string Name = "lines", Default = "default", Floor = "floor";
    public const int MaxForbidden = 16, MaxSupports = 8;

    /// <summary>The checks every file passes, shipped or player.</summary>
    public static void Validate(LinePack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (!pack.placement.ContainsKey(Default)) throw new ArgumentException(Text.Get("LineSchema.default_missing"));
        foreach (var pair in pack.placement)
        {
            string id = pair.Key; var rule = pair.Value;
            if (string.IsNullOrWhiteSpace(id) || rule == null) throw new ArgumentException(Text.Get("LineSchema.rule_empty", id));
            if (rule.forbiddenTiles == null || rule.forbiddenTiles.Count > MaxForbidden || rule.forbiddenTiles.Any(c => !Identifier(c)) ||
                rule.forbiddenTiles.Distinct(StringComparer.Ordinal).Count() != rule.forbiddenTiles.Count)
                throw new ArgumentException(Text.Get("LineSchema.forbidden", id, MaxForbidden));
            if (rule.supports == null || rule.supports.Count < 1 || rule.supports.Count > MaxSupports) throw new ArgumentException(Text.Get("LineSchema.supports", id, MaxSupports));
            foreach (var support in rule.supports)
            {
                if (support == null || !Identifier(support.tile) || !Identifier(support.@object)) throw new ArgumentException(Text.Get("LineSchema.support", id));
                if (rule.forbiddenTiles.Contains(support.tile)) throw new ArgumentException(Text.Get("LineSchema.contradiction", id, support.tile));
            }
        }
    }
    /// <summary>A condition name as the game writes them: letters and digits, starting with a letter.</summary>
    public static bool Identifier(string? name) =>
        !string.IsNullOrEmpty(name) && name!.Length <= 64 && char.IsLetter(name[0]) && name.All(c => c < 128 && char.IsLetterOrDigit(c));
}
