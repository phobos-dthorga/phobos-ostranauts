using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Where a line segment counts as laid, read from Framework's <c>lines</c> data pack
/// (<c>framework/lines.json</c>, Framework 0.100.0) with player and add-on files over it. The decision itself has no
/// game types, so the offline checks run it on the shipped rules. A rule named after a line family id applies to that
/// family; every other line, and the irrigation and coolant conduits, use <c>default</c>.</summary>
public static class LinePlacement
{
    public const string ModFolder = "PhobosFramework", Resource = "PhobosFramework.lines.json";
    private static LinePack? pack;
    public static LinePack Pack => pack ??= Load();
    public static DataPackSource Source => new(FrameworkInfo.PluginId, ModFolder, LineSchema.Name, typeof(LinePlacement).Assembly, Resource);
    public static LinePack Load() { pack = DataPacks.Load<LinePack>(Source, LineSchema.Validate); return pack; }

    /// <summary>The rule for a line family, or the default.</summary>
    public static LinePlacementEntry For(string? familyId) =>
        familyId != null && Pack.placement.TryGetValue(familyId, out var own) ? own : Pack.placement[LineSchema.Default];

    /// <summary>One installed, intact object on the segment's tile: whether the game counts it as a floor, and the
    /// conditions it carries.</summary>
    public readonly struct Standing
    {
        public bool Floor { get; }
        public Func<string, bool> Has { get; }
        public Standing(bool floor, Func<string, bool> has) { Floor = floor; Has = has ?? (_ => false); }
    }

    /// <summary>Whether a tile may carry a segment at all under a rule: none of its forbidden conditions, and at least
    /// one condition a support asks for. Cheap, and asked before the tile's objects are read.</summary>
    public static bool TileCarries(LinePlacementEntry rule, Func<string, bool> tileHas)
    {
        foreach (string forbidden in rule.forbiddenTiles) if (tileHas(forbidden)) return false;
        foreach (var support in rule.supports) if (tileHas(support.tile)) return true;
        return false;
    }
    /// <summary>The whole decision, pure: the tile carries a segment, and some support's tile condition is present
    /// with a matching object standing on it.</summary>
    public static bool Supported(LinePlacementEntry rule, Func<string, bool> tileHas, IReadOnlyList<Standing> standing)
    {
        if (rule == null || tileHas == null || standing == null || !TileCarries(rule, tileHas)) return false;
        foreach (var support in rule.supports)
        {
            if (!tileHas(support.tile)) continue;
            bool floor = support.@object == LineSchema.Floor;
            for (int i = 0; i < standing.Count; i++)
                if (floor ? standing[i].Floor : standing[i].Has(support.@object)) return true;
        }
        return false;
    }
}
