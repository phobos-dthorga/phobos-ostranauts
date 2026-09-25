using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAgriculture;

// Authored probabilities per native contents-table roll, not per ship or search.
// Keep commodities in one optional choice so adding stock cannot flood containers.
internal static class LootContent
{
    internal const double DefaultMultiplier = 1, MaximumMultiplier = 3;
    internal const string FridgeTable = "ItmFridge01Contents", CrateTable = "ItmRandomCrateLockedContents";
    // Preserve our original fridge branch when expanding it beyond seeds.
    internal const string FridgeBranch = "PhobosAgricultureStoredSeeds", CrateBranch = "PhobosAgricultureCrateSupplies";

    internal static void Add(NativeDefinitions definitions, bool enabled, double multiplier)
    {
        if (double.IsNaN(multiplier) || double.IsInfinity(multiplier) || multiplier < 0 || multiplier > MaximumMultiplier)
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        double scale = enabled ? multiplier : 0;
        AddChoice(FridgeTable, FridgeBranch, new Dictionary<string, double>
        {
            [Definitions.PotatoSeed] = .03, [Definitions.LettuceSeed] = .04,
            [Definitions.Raw] = .06, [Definitions.Leaves] = .05, [Definitions.Meal] = .04
        });
        AddChoice(CrateTable, CrateBranch, new Dictionary<string, double>
        {
            [Definitions.PotatoSeed] = .03, [Definitions.LettuceSeed] = .04,
            [Definitions.Nutrient] = .08, [Definitions.Irrigation] = .06,
            [Service.RecoveryCartridge] = .05, [IrrigationDefinitions.Pipe + "Loose"] = .04
        });

        void AddChoice(string parent, string branch, Dictionary<string, double> chances) =>
            AdditiveLoot.SetItemChoice(definitions, parent, branch,
                chances.ToDictionary(pair => pair.Key, pair => pair.Value * scale, StringComparer.Ordinal));
    }
}
