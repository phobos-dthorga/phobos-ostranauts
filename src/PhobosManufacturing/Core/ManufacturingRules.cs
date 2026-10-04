using System;

namespace PhobosManufacturing.Core;

/// <summary>Identities and rules shared by every Manufacturing machine: the saved conditions the power
/// override reads, the commodities the vessels hold, access reach and the one-tile adjacency rule.</summary>
public static class ManufacturingRules
{
    public const string Owner = "phobosgekko.ostranauts.manufacturing";
    public const string Working = "PhobosManufacturingWorking", Electrolysing = "PhobosManufacturingElectrolysing", Reacting = "PhobosManufacturingReacting", Filling = "PhobosManufacturingFilling", Bottling = "PhobosManufacturingBottling", Grinding = "PhobosManufacturingGrinding",
        Content = "PhobosManufacturingContent";
    /// <summary>The commodity every registered water vessel holds (Shipbreaker's S3, Agriculture's R3) and ours.</summary>
    public const string Water = "water", Hydrogen = "hydrogen", Methane = "methane", Oxygen = "oxygen", Nitrogen = "nitrogen", CarbonDioxide = "carbon dioxide", Ammonia = "ammonia";
    /// <summary>Agriculture's hopper commodity (Agriculture 0.27.0 <c>HopperRules.Commodity</c>), named as a string only:
    /// the LC-3's complete formulation deposits into a linked hopper. A native check keeps the two equal.</summary>
    public const string CropNutrients = "crop nutrients";
    public const double LocalAccessTiles = 2.5, ConsoleAccessTiles = 2.5;
    public const double VesselRecheckSeconds = 5;
    /// <summary>How often a started machine with a product store sends one finished unit there (real seconds;
    /// Manufacturing 0.49.0, Framework StoreDelivery).</summary>
    public const double DeliverySeconds = 1;
    /// <summary>How far through a cycle a machine is, for its panel (Manufacturing 0.48.0; owner request, 5 October 2026:
    /// a meter in kWh read like a consumption countdown). A cycle is a fixed amount of delivered electricity, so the
    /// share delivered is the share done: 0 to 100, never 100 before the cycle has actually finished.</summary>
    public static double PercentDone(double doneKWh, double cycleKWh)
    {
        if (!Finite(doneKWh) || !Finite(cycleKWh) || cycleKWh <= 0 || doneKWh <= 0) return 0;
        return doneKWh >= cycleKWh ? 100 : System.Math.Min(99, System.Math.Floor(doneKWh / cycleKWh * 100));
    }
    /// <summary>Whole minutes of work left in a cycle at the machine's full draw, never less than one while any is left.</summary>
    public static double MinutesLeft(double doneKWh, double cycleKWh, double workingKW)
    {
        if (!Finite(doneKWh) || !Finite(cycleKWh) || !Finite(workingKW) || workingKW <= 0 || cycleKWh <= 0) return 0;
        double left = System.Math.Max(0, cycleKWh - System.Math.Max(0, doneKWh));
        return left <= 0 ? 0 : System.Math.Max(1, System.Math.Ceiling(left / workingKW * 60));
    }
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
