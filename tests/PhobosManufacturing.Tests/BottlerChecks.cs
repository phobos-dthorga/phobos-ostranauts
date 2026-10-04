using System;
using System.Linq;
using PhobosManufacturing.Core;

/// <summary>The Alembrine Corker-2 bottler (Manufacturing 0.40.0): a serving's make-up at 40% alcohol by volume, the batch
/// balance, the saved record, the donor's kept conditions, and the spirit's price in the refining band.</summary>
internal static class BottlerChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        double ethanolMass = 0.4 * 789.3, waterMass = 0.6 * 998.2, fraction = ethanolMass / (ethanolMass + waterMass);
        check(BottlerRules.Balanced() && Math.Abs(BottlerRules.EthanolPerServingKg / BottlerRules.ServingKg - fraction) < .003,
            "A 35 g serving at 40% by volume is 12.1 g of ethanol and 22.9 g of water");
        check(Math.Abs(BottlerRules.EthanolPerBatchKg + BottlerRules.WaterPerBatchKg - BottlerRules.ServingsPerBatch * BottlerRules.ServingKg) < 1e-12 && BottlerRules.ServingsPerBatch == 7,
            "A batch draws exactly the mass of its seven servings");
        var saved = BottlerState.Read(new BottlerState { BatchKWh = .1, Batches = 3 }.Save());
        check(Math.Abs(saved.BatchKWh - .1) < 1e-12 && saved.Batches == 3, "The bottler's record survives reload");
        throws(() => new BottlerState { BatchKWh = 1 }.Save(), "A batch cannot hold more energy than it needs");
        var (credited, complete) = BottlerRules.Advance(.15, .1);
        check(complete && Math.Abs(credited - BottlerRules.BatchKWh) < 1e-12, "A batch completes at 0.2 kWh");
        check(!BottlerRules.KeepFromDonor("IsBismertnaya") && !BottlerRules.KeepFromDonor("StatMass") && !BottlerRules.KeepFromDonor("IsCategoryIntoxicants") &&
              BottlerRules.KeepFromDonor("IsLiquor") && BottlerRules.KeepFromDonor("IsLiquid") && BottlerRules.KeepFromDonor("IsDrug"),
            "The spirit keeps the vodka's liquor, liquid and drug marks and drops its brand, mass, price and category");
        // Refining as a business: a beet rack's spirit earns 1.5 to 2.5 times the crop's nutrients and water at base prices.
        var spirit = Materials.ById(Materials.Spirit)!;
        double ethanolPerRack = 9.0 / 6 * FermenterRecipes.BeetMash.Deposits.Single(p => p.Id == LiquidStores.Ethanol).Kg;
        double servings = ethanolPerRack / BottlerRules.EthanolPerServingKg, value = servings * spirit.Price;
        double inputs = 50 * 1.5 + 5.862 * 10;
        check(value >= 1.5 * inputs && value <= 2.5 * inputs, $"A beet rack's spirit is worth 1.5 to 2.5 times its nutrients and water: {value:F0} from {inputs:F0}");
        check(LiquidStores.EthanolPricePerKg * 0.45 < spirit.Price / BottlerRules.EthanolPerServingKg, "Bottling ethanol always beats selling it back");
        check(spirit.Kg == BottlerRules.ServingKg && spirit.Stack == BottlerRules.ServingsPerBatch && !Materials.IsTerminal(Materials.Spirit), "The spirit is a 35 g serving that stacks seven to a batch");
    }
}
