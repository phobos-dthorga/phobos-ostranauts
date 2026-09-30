using System;

namespace PhobosManufacturing.Core;

/// <summary>Identities and rules shared by every Manufacturing machine: the saved conditions the power
/// override reads, the commodities the vessels hold, access reach and the one-tile adjacency rule.</summary>
public static class ManufacturingRules
{
    public const string Owner = "phobosgekko.ostranauts.manufacturing";
    public const string Working = "PhobosManufacturingWorking", Electrolysing = "PhobosManufacturingElectrolysing", Reacting = "PhobosManufacturingReacting", Filling = "PhobosManufacturingFilling",
        Content = "PhobosManufacturingContent";
    /// <summary>The commodity every registered water vessel holds (Shipbreaker's S3, Agriculture's R3) and ours.</summary>
    public const string Water = "water", Hydrogen = "hydrogen", Methane = "methane", Oxygen = "oxygen", Nitrogen = "nitrogen", CarbonDioxide = "carbon dioxide", Ammonia = "ammonia";
    /// <summary>Agriculture's hopper commodity (Agriculture 0.27.0 <c>HopperRules.Commodity</c>), named as a string only:
    /// the LC-3's complete formulation deposits into a linked hopper. A native check keeps the two equal.</summary>
    public const string CropNutrients = "crop nutrients";
    public const double LocalAccessTiles = 2.5, ConsoleAccessTiles = 2.5;
    public const double VesselRecheckSeconds = 5;
    /// <summary>Two square footprints lie within one tile of each other: the distance between centres, on the
    /// longer axis, is at least half of both footprints (no overlap) and at most that plus one tile.</summary>
    public static bool Adjacent(double ax, double ay, int aFootprint, double bx, double by, int bFootprint) =>
        Phobos.Ostranauts.Framework.Liquids.BulkVessels.WithinOneTile(ax, ay, aFootprint, bx, by, bFootprint);
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    /// <summary>Credits supplied energy to a cycle of <paramref name="cycleKWh"/>; never more than one cycle completes
    /// per step, and energy beyond a completion is refused rather than banked.</summary>
    public static (double CycleKWh, bool Complete) AdvanceCycle(double creditedKWh, double suppliedKWh, double cycleKWh)
    {
        if (!Finite(creditedKWh) || !Finite(suppliedKWh) || !Finite(cycleKWh) || cycleKWh <= 0 || creditedKWh < 0 || suppliedKWh < 0 || creditedKWh >= cycleKWh)
            throw new ArgumentException("Invalid cycle energy.");
        double credited = Math.Min(creditedKWh + suppliedKWh, cycleKWh);
        return (credited, credited >= cycleKWh - 1e-9);
    }
}
