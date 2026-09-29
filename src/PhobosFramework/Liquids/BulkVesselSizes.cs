using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>The three sizes every bulk family offers (owner direction, 29 September 2026): small, medium and large,
/// unless its commodity is niche or high-value. Each size is one tile wider than the one below it.</summary>
public enum VesselSize { Small, Medium, Large }

/// <summary>The shared size ladder for bulk vessel families. The small size keeps the family's original definition
/// prefix and record names, so existing saves and IDs are untouched; medium and large append the size name to both.
/// Capacity, dry mass and price scale from the small size by one authored rule shared by every Phobos mod:
/// capacity with floor area plus a 10% housing-efficiency gain per step (walls and fittings take a smaller share of
/// a bigger vessel), dry mass with floor area less 15% per step, and price with floor area to the power 0.6 (an
/// economy of scale). Content rounds the results and owns every small-size value.</summary>
public static class BulkVesselSizes
{
    public static readonly IReadOnlyList<VesselSize> All = new[] { VesselSize.Small, VesselSize.Medium, VesselSize.Large };
    public const double EfficiencyGainPerStep = 0.10, DryReductionPerStep = 0.15, PriceAreaExponent = 0.6;

    /// <summary>The definition prefix of one size: the family's own prefix for small, suffixed otherwise.</summary>
    public static string Prefix(string smallPrefix, VesselSize size) => Suffixed(smallPrefix, size);
    /// <summary>A record, journal or guard name for one size, suffixed the same way.</summary>
    public static string Name(string smallName, VesselSize size) => Suffixed(smallName, size);
    public static int Footprint(int smallFootprint, VesselSize size) =>
        smallFootprint < 1 ? throw new ArgumentException("A footprint is at least one tile.") : smallFootprint + Step(size);
    /// <summary>The size a definition belongs to within one family ladder, or null when it is not in the ladder.</summary>
    public static VesselSize? SizeOf(string? definition, string smallPrefix)
    {
        if (definition == null) return null;
        foreach (var size in All) if (EquipmentIdentity.IsFamily(definition, Prefix(smallPrefix, size))) return size;
        return null;
    }
    public static bool InLadder(string? definition, string smallPrefix) => SizeOf(definition, smallPrefix) != null;

    public static double AreaRatio(int smallFootprint, VesselSize size)
    {
        double side = Footprint(smallFootprint, size);
        return side * side / (smallFootprint * (double)smallFootprint);
    }
    public static double CapacityFactor(int smallFootprint, VesselSize size) => AreaRatio(smallFootprint, size) * (1 + EfficiencyGainPerStep * Step(size));
    public static double DryFactor(int smallFootprint, VesselSize size) => AreaRatio(smallFootprint, size) * (1 - DryReductionPerStep * Step(size));
    public static double PriceFactor(int smallFootprint, VesselSize size) => Math.Pow(AreaRatio(smallFootprint, size), PriceAreaExponent);
    /// <summary>Rounds a scaled quantity to a readable step: whole kilograms below 100, fives below 1,000, tens above.</summary>
    public static double Round(double value)
    {
        if (!BulkVesselSpec.Finite(value) || value <= 0) throw new ArgumentException("Invalid scaled quantity.");
        double step = value < 100 ? 1 : value < 1000 ? 5 : 10;
        return Math.Max(step, Math.Round(value / step, MidpointRounding.AwayFromZero) * step);
    }

    /// <summary>One size's declaration, scaled from the family's small values and rounded.</summary>
    public static BulkVesselSpec Spec(string smallPrefix, int smallFootprint, VesselSize size, string commodity, double smallCapacityKg, double smallDryKg,
        string owner, string smallRecord, string smallJournal, string smallGuard, VesselDamagePolicy policy = VesselDamagePolicy.Isolate, double leakKgPerHour = 0) =>
        new(Prefix(smallPrefix, size), commodity, Scale(smallCapacityKg, CapacityFactor(smallFootprint, size)), Scale(smallDryKg, DryFactor(smallFootprint, size)), owner,
            Name(smallRecord, size), Name(smallJournal, size), Name(smallGuard, size), policy, leakKgPerHour);
    /// <summary>The small value itself for the small size (never re-rounded), the rounded scaled value otherwise.</summary>
    public static double Scale(double smallValue, double factor) => factor == 1 ? smallValue : Round(smallValue * factor);

    private static int Step(VesselSize size) => size switch { VesselSize.Small => 0, VesselSize.Medium => 1, VesselSize.Large => 2, _ => throw new ArgumentException("Unknown vessel size.") };
    private static string Suffixed(string smallName, VesselSize size)
    {
        if (string.IsNullOrWhiteSpace(smallName)) throw new ArgumentException("A family needs a name.");
        return size == VesselSize.Small ? smallName : smallName + size;
    }
}
