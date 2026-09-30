using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Liquids;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Conveyor belts (Framework 0.61.0; owner decision, 30 September 2026: working belts this round, and the
/// open-floor item routes then require belts). A belt is a 1 x 1 segment on Framework's shared line pattern in the
/// lowest lane; a run of intact installed belts over sound floor joins any two pieces of equipment whose own tiles it
/// lies on or beside. Belts hold nothing: an item moves straight from one container to the other through the checked
/// physical transfer, driven and paid for by the machine that sends or receives it, as before. Equipment that touches
/// (footprints meeting or one tile apart) needs no belt.</summary>
public static class BeltNetwork
{
    public const string FamilyId = "PhobosFramework.Belt", Prefix = "PhobosConveyorBelt";
    public const string Present = Prefix + "Present", Intact = Prefix + "Intact", Installed = Prefix + "Installed";
    public static readonly FluidSegmentFamily Family = new(FamilyId, c => c.strCODef == Installed) { Label = () => Text.Get("BeltNetwork.label") };

    /// <summary>Whether a belt run joins the cells of one piece of equipment to the cells of another (on or beside).</summary>
    public static bool Joins(Ship? ship, IEnumerable<int> starts, IEnumerable<int> goals) =>
        ship != null && ship.nCols > 0 && ship.nRows > 0 && FluidRouteCache.Topology(ship, Family).JoinsNear(starts, goals);

    /// <summary>The belt cells an item travels along from one piece of equipment to the other (Framework 0.62.0), for the
    /// moving-item display only; null when no belt run joins them. Reach itself is decided by <see cref="Joins"/>.</summary>
    public static int[]? PathCells(Ship? ship, IEnumerable<int> starts, IEnumerable<int> goals)
    {
        if (ship == null || ship.nCols <= 0 || ship.nRows <= 0) return null;
        var topology = FluidRouteCache.Topology(ship, Family);
        if (topology.Overflow) return null;
        var from = Near(ship, topology, starts); var to = Near(ship, topology, goals);
        foreach (int a in from)
            foreach (int b in to)
                if (topology.ComponentOf(a) == topology.ComponentOf(b)) return topology.Path(a, b);
        return null;
    }
    private static List<int> Near(Ship ship, FluidTopology topology, IEnumerable<int> cells)
    {
        var found = new List<int>(); int count = ship.nCols * ship.nRows;
        foreach (int cell in cells)
        {
            if (cell < 0 || cell >= count) continue;
            int column = cell % ship.nCols;
            foreach (int c in new[] { cell, column > 0 ? cell - 1 : -1, column + 1 < ship.nCols ? cell + 1 : -1, cell - ship.nCols, cell + ship.nCols })
                if (c >= 0 && c < count && topology.Allowed(c) && !found.Contains(c)) found.Add(c);
        }
        return found;
    }
    /// <summary>Every tile an installed object covers (Framework 0.62.0), so a belt on or beside any edge of it joins it.
    /// The game swaps an item's width and height itself when it is rotated, so the live values are used as they are.</summary>
    public static int[] FootprintCells(CondOwner co)
    {
        var ship = co?.ship; var item = co?.Item;
        if (ship == null || item == null) return System.Array.Empty<int>();
        int w = System.Math.Max(1, item.nWidthInTiles), h = System.Math.Max(1, item.nHeightInTiles);
        var centre = co!.GetPos(); var cells = new List<int>(w * h);
        for (int i = 0; i < w; i++)
            for (int j = 0; j < h; j++)
            {
                int index = ship.GetTileIndexAtWorldCoords1(centre + new UnityEngine.Vector2(i - (w - 1) / 2f, j - (h - 1) / 2f));
                if (index >= 0 && !cells.Contains(index)) cells.Add(index);
            }
        return cells.ToArray();
    }
    /// <summary>The owner's link rule for items: the two touch, or a belt joins their cells. Never across open floor.</summary>
    public static bool Reaches(CondOwner a, CondOwner b, IEnumerable<int> aCells, IEnumerable<int> bCells) =>
        a != null && b != null && a.ship != null && a.ship == b.ship && (BulkVessels.Adjacent(a, b) || Joins(a.ship, aCells, bCells));
}
