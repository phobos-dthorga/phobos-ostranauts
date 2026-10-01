using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Where each line family meets a piece of equipment (Framework 0.57.0), and which equipment has which
/// ports. One rule for every machine and store, so players learn it once: the process-water port is the neighbouring
/// tile on the local -X side, the gas port the neighbouring tile on the local +X side, both in the middle row (the
/// upper of the two middle rows on an even footprint), and the acid port on the +X side one row below the gas port.
/// Ports rotate with the equipment. The footprint tile beside the port draws the pipe's joint. Since Framework
/// 0.69.0 (owner decision, 1 October 2026) a port no longer says where the pipe must lie: it marks the equipment as
/// taking part in the family, and a pipe of the family under the equipment or directly beside it on any side joins
/// it (<see cref="FluidRouteCache"/>). The port tile is one of those tiles, so older layouts keep working. Content
/// registers ports through <c>LineDefinitions.AddPort</c>; points are rebuilt from definitions when a save loads,
/// so equipment saved before a port existed gains it with no rewrite.</summary>
public static class LinePorts
{
    public const string WaterPoint = "PhobosWaterPort", GasPoint = "PhobosGasPort", AcidPoint = "PhobosAcidPort";
    private static readonly Dictionary<string, Dictionary<string, List<string>>> ports = new(StringComparer.Ordinal);
    private static readonly IReadOnlyList<string> none = Array.Empty<string>();

    /// <summary>The middle row's index from the top (the upper of the two middle rows on an even footprint).</summary>
    public static int MiddleRow(int footprint) => footprint % 2 == 0 ? footprint / 2 - 1 : (footprint - 1) / 2;
    /// <summary>A row's point height in the game's map-point units (16 per tile, +Y towards the top row).</summary>
    public static int RowY(int footprint, int row) => 8 * (footprint - 1) - 16 * row;
    public static (int X, int Y, int Socket) Water(int footprint) => Side(footprint, MiddleRow(footprint), plusX: false);
    public static (int X, int Y, int Socket) Gas(int footprint) => Side(footprint, MiddleRow(footprint), plusX: true);
    public static (int X, int Y, int Socket) Acid(int footprint)
    {
        if (footprint < 2) throw new ArgumentOutOfRangeException(nameof(footprint), "An acid port needs a footprint of two tiles or more.");
        return Side(footprint, MiddleRow(footprint) + 1, plusX: true);
    }
    private static (int X, int Y, int Socket) Side(int footprint, int row, bool plusX)
    {
        if (footprint < 1) throw new ArgumentOutOfRangeException(nameof(footprint));
        int x = 8 * footprint + 8;
        return (plusX ? x : -x, RowY(footprint, row), row * footprint + (plusX ? footprint - 1 : 0));
    }

    /// <summary>Records that a definition carries a port of a family at a named map point.</summary>
    public static void Register(string family, string definition, string point)
    {
        if (string.IsNullOrEmpty(family) || string.IsNullOrEmpty(definition) || string.IsNullOrEmpty(point)) throw new ArgumentException("A line port needs a family, a definition and a point.");
        if (!ports.TryGetValue(family, out var byDefinition)) ports[family] = byDefinition = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (!byDefinition.TryGetValue(definition, out var points)) byDefinition[definition] = points = new List<string>();
        if (!points.Contains(point)) points.Add(point);
    }
    /// <summary>A definition's port points for a family, or an empty list.</summary>
    public static IReadOnlyList<string> Points(string family, string? definition) =>
        definition != null && ports.TryGetValue(family, out var byDefinition) && byDefinition.TryGetValue(definition, out var points) ? points : none;
    public static bool Has(string family, string? definition) => Points(family, definition).Count > 0;
    /// <summary>Every definition with a port of a family (for offline checks).</summary>
    public static IEnumerable<string> Definitions(string family) => ports.TryGetValue(family, out var byDefinition) ? byDefinition.Keys : Array.Empty<string>();
}
