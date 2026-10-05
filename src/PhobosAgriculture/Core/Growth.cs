using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosAgriculture.Core;

/// <summary>The crops pack's <c>growth</c> section (Agriculture 0.55.0; owner direction, 5 October 2026: all crop data
/// is data, so players and add-ons tune it or give their own crops their own limits). It sits beside the crop
/// entries, outside what is frozen, like <c>co2Response</c>. Every figure may be left out; what is left out takes the
/// default in <see cref="Growth"/>.</summary>
public sealed class GrowthEntry
{
    public string? notes;
    /// <summary>The room every crop grows in, unless its own entry under <see cref="crops"/> says otherwise.</summary>
    public RoomEntry? room;
    public StressEntry? stress;
    /// <summary>The nutrient a W2 keeps in each rack it feeds, in kilograms.</summary>
    public double? nutrientTargetKg;
    /// <summary>The nutrient each kilogram of water the pump moves can carry, in kilograms.</summary>
    public double? feedStrengthKgPerKg;
    /// <summary>Limits for one crop, by crop name. Only what is given replaces the shared room.</summary>
    public Dictionary<string, CropGrowthEntry> crops = new(StringComparer.Ordinal);
}
public sealed class RoomEntry { public string? notes; public double? minC, maxC, minKPa, maxKPa; }
public sealed class StressEntry
{
    public string? notes;
    /// <summary>Hours of stress a crop shrugs off, then the health it loses per further hour in a room that suits it,
    /// in one that does not, and in one with no air; and the water a rack needs before a crew order plants in it.</summary>
    public double? graceHours, healthLossPerHour, healthLossPerHourOutside, healthLossPerHourNoAir, plantWaterKg;
}
public sealed class CropGrowthEntry { public string? notes; public RoomEntry? room; }

/// <summary>A growing room's limits, resolved for one crop.</summary>
public readonly struct GrowthRoom
{
    public double MinK { get; }
    public double MaxK { get; }
    public double MinKPa { get; }
    public double MaxKPa { get; }
    public GrowthRoom(double minC, double maxC, double minKPa, double maxKPa) { MinK = minC + Growth.ZeroC; MaxK = maxC + Growth.ZeroC; MinKPa = minKPa; MaxKPa = maxKPa; }
    public double MinC => MinK - Growth.ZeroC;
    public double MaxC => MaxK - Growth.ZeroC;
    public bool Suits(double tempK, double kPa) => tempK >= MinK && tempK <= MaxK && kPa >= MinKPa && kPa <= MaxKPa;
    /// <summary>The room for a planted or ordered crop; the shared room for none or an unknown name.</summary>
    public static GrowthRoom For(string? cropId) => Growth.Room(Crops.Pack.growth, cropId);
}

/// <summary>How stress wears a crop down (see <see cref="StressEntry"/>).</summary>
public sealed class StressRules
{
    public double GraceHours = Growth.DefaultGraceHours, HealthLossPerHour = Growth.DefaultLoss, HealthLossPerHourOutside = Growth.DefaultLossOutside,
        HealthLossPerHourNoAir = Growth.DefaultLossNoAir, PlantWaterKg = Growth.DefaultPlantWaterKg;
}

/// <summary>The defaults, the resolved values and the validation of the <c>growth</c> section. The defaults are the
/// figures the code held until 0.55.0, with one owner decision: crops grow up to 31 C (26 C until 0.54.0, then 30 C),
/// on the presumption that crops of the game's day are engineered ones and that a ship's rooms sit at about 25 C or a
/// little above. Authored gameplay limits, not plant physiology.</summary>
public static class Growth
{
    public const double ZeroC = 273.15;
    public const double DefaultMinC = 18, DefaultMaxC = 31, DefaultMinKPa = 70, DefaultMaxKPa = 110;
    public const double DefaultGraceHours = 2, DefaultLoss = .01, DefaultLossOutside = .1, DefaultLossNoAir = .1, DefaultPlantWaterKg = .25;
    public const double DefaultNutrientTargetKg = .1, DefaultFeedStrength = .01;
    public const double LowestC = -50, HighestC = 100, HighestKPa = 500, MaxGraceHours = 1000;

    public static GrowthRoom Room(GrowthEntry? g, string? cropId)
    {
        var shared = g?.room; RoomEntry? own = cropId != null && g != null && g.crops.TryGetValue(cropId, out var c) ? c.room : null;
        return new GrowthRoom(own?.minC ?? shared?.minC ?? DefaultMinC, own?.maxC ?? shared?.maxC ?? DefaultMaxC,
            own?.minKPa ?? shared?.minKPa ?? DefaultMinKPa, own?.maxKPa ?? shared?.maxKPa ?? DefaultMaxKPa);
    }
    public static StressRules StressOf(GrowthEntry? g)
    {
        var s = g?.stress;
        return new StressRules
        {
            GraceHours = s?.graceHours ?? DefaultGraceHours, HealthLossPerHour = s?.healthLossPerHour ?? DefaultLoss,
            HealthLossPerHourOutside = s?.healthLossPerHourOutside ?? DefaultLossOutside, HealthLossPerHourNoAir = s?.healthLossPerHourNoAir ?? DefaultLossNoAir,
            PlantWaterKg = s?.plantWaterKg ?? DefaultPlantWaterKg
        };
    }
    private static StressRules? stress; private static CropPack? stressOf;
    /// <summary>The loaded pack's stress rules, resolved once per pack.</summary>
    public static StressRules Stress
    {
        get { var pack = Crops.Pack; if (stress == null || !ReferenceEquals(stressOf, pack)) { stress = StressOf(pack.growth); stressOf = pack; } return stress; }
    }
    public static double NutrientTargetKg => Crops.Pack.growth?.nutrientTargetKg ?? DefaultNutrientTargetKg;
    public static double FeedStrength => Crops.Pack.growth?.feedStrengthKgPerKg ?? DefaultFeedStrength;

    public static void Validate(GrowthEntry? g, ICollection<string> cropIds)
    {
        if (g == null) return;
        void Room(RoomEntry? r, string where, GrowthRoom resolved)
        {
            foreach (double? v in new[] { r?.minC, r?.maxC, r?.minKPa, r?.maxKPa })
                if (v is double x && !CropState.Finite(x)) throw new ArgumentException(Text.Get("crops_room", where));
            if (resolved.MinC < LowestC || resolved.MaxC > HighestC || resolved.MinC >= resolved.MaxC || resolved.MinKPa <= 0 || resolved.MaxKPa > HighestKPa || resolved.MinKPa >= resolved.MaxKPa)
                throw new ArgumentException(Text.Get("crops_room", where));
        }
        Room(g.room, "growth.room", Growth.Room(g, null));
        foreach (var pair in g.crops)
        {
            if (!cropIds.Contains(pair.Key) || pair.Value == null) throw new ArgumentException(Text.Get("crops_room_crop", pair.Key));
            Room(pair.Value.room, "growth.crops." + pair.Key, Growth.Room(g, pair.Key));
        }
        var s = StressOf(g);
        foreach (double v in new[] { s.GraceHours, s.HealthLossPerHour, s.HealthLossPerHourOutside, s.HealthLossPerHourNoAir, s.PlantWaterKg })
            if (!CropState.Finite(v) || v < 0) throw new ArgumentException(Text.Get("crops_stress"));
        if (s.GraceHours > MaxGraceHours || s.HealthLossPerHour > 1 || s.HealthLossPerHourOutside > 1 || s.HealthLossPerHourNoAir > 1 || s.PlantWaterKg > CropState.ReservoirKg)
            throw new ArgumentException(Text.Get("crops_stress"));
        double target = g.nutrientTargetKg ?? DefaultNutrientTargetKg, strength = g.feedStrengthKgPerKg ?? DefaultFeedStrength;
        if (!CropState.Finite(target) || target <= 0 || target > CropState.NutrientCapacityKg || !CropState.Finite(strength) || strength <= 0 || strength > 1)
            throw new ArgumentException(Text.Get("crops_feed_rule", CropState.NutrientCapacityKg));
    }
}

/// <summary>How a W2 feeds nutrients (Agriculture 0.55.0; owner direction, 5 October 2026: one nutrient, one clear
/// choice). Every kilogram of water the pump moves, fresh into a rack or recirculated through a full one, carries up to
/// a set dose, and a rack is kept topped up to a target. Pure; the pack gives the two figures.</summary>
public static class NutrientFeed
{
    /// <summary>The nutrient a rack still wants: up to the target, never past its own capacity.</summary>
    public static double Need(double rackNutrients, double targetKg) =>
        Math.Max(0, Math.Min(targetKg, CropState.NutrientCapacityKg) - rackNutrients);
    /// <summary>The water to recirculate through a rack this step, beyond the <paramref name="freshKg"/> already
    /// delivered, so the stream can carry what the rack needs: bounded by what the pump may still move.</summary>
    public static double Recirculate(double needKg, double available, double freshKg, double strength, double budgetKg)
    {
        if (strength <= 0 || !CropState.Finite(budgetKg) || budgetKg <= 0) return 0;
        double wanted = Math.Min(needKg, available) - freshKg * strength;
        return wanted <= 0 ? 0 : Math.Min(budgetKg, wanted / strength);
    }
    /// <summary>The nutrient that moves from a W2 holding <paramref name="available"/> to a rack needing
    /// <paramref name="needKg"/> when the pump moved <paramref name="movedKg"/> of water to or through it.</summary>
    public static double Send(double needKg, double available, double movedKg, double strength) =>
        Math.Max(0, Math.Min(Math.Min(needKg, available), movedKg * strength));
    /// <summary>What a W2 takes into its own nutrient store from its source in a step.</summary>
    public static double Dose(double held, double sourceKg, double budgetKg) =>
        Math.Max(0, Math.Min(CropState.NutrientCapacityKg - held, Math.Min(sourceKg, budgetKg)));
}
