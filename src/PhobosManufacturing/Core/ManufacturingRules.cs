using System;

namespace PhobosManufacturing.Core;

/// <summary>Identities and rules shared by every Manufacturing machine: the saved conditions the power
/// override reads, the commodities the vessels hold, access reach and the one-tile adjacency rule.</summary>
public static class ManufacturingRules
{
    public const string Owner = "phobosgekko.ostranauts.manufacturing";
    public const string Working = "PhobosManufacturingWorking", Electrolysing = "PhobosManufacturingElectrolysing", Content = "PhobosManufacturingContent";
    /// <summary>The commodity every registered water vessel holds (Shipbreaker's S3, Agriculture's R3) and ours.</summary>
    public const string Water = "water", Hydrogen = "hydrogen";
    public const double LocalAccessTiles = 2.5, ConsoleAccessTiles = 2.5;
    public const double VesselRecheckSeconds = 5;
    /// <summary>Two square footprints lie within one tile of each other: the distance between centres, on the
    /// longer axis, is at least half of both footprints (no overlap) and at most that plus one tile.</summary>
    public static bool Adjacent(double ax, double ay, int aFootprint, double bx, double by, int bFootprint)
    {
        foreach (double v in new[] { ax, ay, bx, by }) if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        if (aFootprint < 1 || bFootprint < 1) return false;
        double reach = (aFootprint + bFootprint) / 2.0, distance = Math.Max(Math.Abs(ax - bx), Math.Abs(ay - by));
        return distance + 1e-6 >= reach && distance <= reach + 1 + 1e-6;
    }
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
}
