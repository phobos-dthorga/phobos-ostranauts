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

    /// <summary>The owner's link rule for items: the two touch, or a belt joins their cells. Never across open floor.</summary>
    public static bool Reaches(CondOwner a, CondOwner b, IEnumerable<int> aCells, IEnumerable<int> bCells) =>
        a != null && b != null && a.ship != null && a.ship == b.ship && (BulkVessels.Adjacent(a, b) || Joins(a.ship, aCells, bCells));
}
