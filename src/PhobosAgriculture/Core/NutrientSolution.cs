using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture.Core;

/// <summary>The per-crop feed of Agriculture 0.5.0 to 0.54.0, kept only to read what old saves hold. Since 0.55.0
/// (owner direction, 5 October 2026: one nutrient for every crop) a rack and a W2 each hold plain water and plain
/// nutrients, and nothing mixes or matches a feed. An old feed is water and nutrients in its crop's own proportion,
/// so it converts exactly: its water joins the water store and its nutrients the nutrient store, with no mass
/// changed. The feed names live on in the frozen crop entries (<c>feed</c>, <c>feedCommodity</c>).</summary>
public static class LegacyFeed
{
    /// <summary>The plain-water profile name of the old solution record, and what an emptied record is saved as.</summary>
    public const string Water = "water";
    public const double Tolerance = 1e-9;
    /// <summary>An old solution record's contents: water and nutrients in kilograms. A record of plain water holds
    /// none. Unknown fields, bad numbers and negative amounts are refused, as before.</summary>
    public static LiquidMixture ReadSolution(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Count != 3 || !fields.ContainsKey("profile")) throw new ArgumentException("Unknown solution fields.");
        double water = double.Parse(fields["water"], CultureInfo.InvariantCulture), nutrients = double.Parse(fields["nutrients"], CultureInfo.InvariantCulture);
        if (!CropState.Finite(water) || !CropState.Finite(nutrients) || water < 0 || nutrients < 0) throw new ArgumentException("Invalid solution contents.");
        return new LiquidMixture(water, nutrients);
    }
    /// <summary>The record every machine saves now: plain water, nothing dissolved.</summary>
    public static Dictionary<string, string> EmptySolution() => new() { ["profile"] = Water, ["water"] = "0", ["nutrients"] = "0" };
    /// <summary>Folds an old solution into the plain stores. False, changing nothing, when it would not fit (a record
    /// the old rules could not have written).</summary>
    public static bool Fold(CropState state, LiquidMixture solution)
    {
        if (state.Water + solution.CarrierKg > CropState.ReservoirKg + Tolerance || state.Nutrients + solution.SoluteKg > CropState.NutrientCapacityKg + Tolerance) return false;
        state.Water = Math.Min(CropState.ReservoirKg, state.Water + solution.CarrierKg);
        state.Nutrients = Math.Min(CropState.NutrientCapacityKg, state.Nutrients + solution.SoluteKg);
        return true;
    }
    /// <summary>What <paramref name="kg"/> of an old feed is made of, by the name it was mixed under
    /// (<see cref="Crop.Feed"/>) or held under in a pipe or canister (<see cref="Crop.FeedCommodity"/>); null for a
    /// name no shipped or added crop carries.</summary>
    public static LiquidMixture? Split(string? name, double kg)
    {
        if (string.IsNullOrEmpty(name) || !CropState.Finite(kg) || kg < 0) return null;
        var crop = Crops.ByFeed(name) ?? Crops.ByFeedCommodity(name);
        if (crop == null) return null;
        double total = crop.Water + crop.Nutrient;
        return new LiquidMixture(kg * crop.Water / total, kg * crop.Nutrient / total);
    }
    /// <summary>The old feed names pipes and canisters may still hold.</summary>
    public static IEnumerable<string> Commodities()
    {
        foreach (var crop in Crops.All) if (crop.FeedCommodity.Length > 0) yield return crop.FeedCommodity;
    }
}
