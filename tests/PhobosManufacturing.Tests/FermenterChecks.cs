using System;
using System.Linq;
using PhobosManufacturing.Core;

/// <summary>The Alembrine Copperhead-3 fermenter-still (Manufacturing 0.39.0): both charges against sucrose fermentation,
/// C12H22O11 + H2O -> 4 C2H5OH + 4 CO2 (IUPAC 2013 molar masses), mass balance to the gram, the Agriculture gate and the
/// feed rules.</summary>
internal static class FermenterChecks
{
    private const double Sucrose = 0.342297, Ethanol = 0.046068, CO2 = 0.044009, H2O = 0.018015;
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var beet = FermenterRecipes.BeetMash; var sugar = FermenterRecipes.SugarWash;
        check(FermenterRecipes.All.Count == 2 && beet.Revision == 1 && sugar.Revision == 2 && beet.Machine == ChargeCatalog.Fermenter &&
              beet.Requires.SequenceEqual(new[] { ChargeCatalog.SugarCropsRequirement }) && sugar.Requires.SequenceEqual(new[] { ChargeCatalog.SugarCropsRequirement }),
            "The fermenter-still has two charges, both needing Agriculture's sugar crops");
        foreach (var (recipe, sucroseKg) in new[] { (beet, 6 * 0.085), (sugar, 6 * 0.07) })
        {
            double mol = sucroseKg * FermenterRules.FermentedShare / Sucrose;
            check(Math.Abs(recipe.Deposits.Single(p => p.Id == LiquidStores.Ethanol).Kg - 4 * mol * Ethanol) < .001 &&
                  Math.Abs(recipe.Deposits.Single(p => p.Id == ManufacturingRules.CarbonDioxide).Kg - 4 * mol * CO2) < .001,
                "Fermentation gives four ethanol and four CO2 a sucrose, on 92% of the sugar: " + recipe.Id);
            check(Math.Abs(recipe.ChargeKg - recipe.Inputs.Sum(i => i.Count * i.Kg)) < 1e-9 && recipe.OffGasKg == 0 &&
                  recipe.Products.Single(p => p.Id == Materials.SpentMash).Count == 1 && recipe.Seconds == 3600,
                "A fermenter charge conserves mass, breathes nothing into the room and leaves one spent mash in an hour: " + recipe.Id);
            check(Math.Abs(recipe.ReactionKWh - mol * 172.5 / 3600) < .01 && recipe.ReactionKWh > 0, "Fermentation releases about 172.5 kJ a mole of sucrose: " + recipe.Id);
        }
        double water = 6 * 0.375 - 6 * 0.085 * FermenterRules.FermentedShare / Sucrose * H2O - (0.5 - 6 * (0.5 - 0.375 - 0.085 * FermenterRules.FermentedShare));
        check(Math.Abs(beet.Deposits.Single(p => p.Id == ManufacturingRules.Water).Kg - water) < .002 && beet.Circulates.Count == 0,
            "Beet mash returns the beets' water not bound in the spent mash to the water vessel");
        check(sugar.Draws.Single(i => i.Id == ManufacturingRules.Water).Kg == 0.487 && sugar.Circulates[ManufacturingRules.Water] == 2 && !sugar.Deposits.Any(p => p.Id == ManufacturingRules.Water),
            "Sugar wash circulates 2 kg of water and keeps 0.487 kg in its spent mash");
        check(Materials.IsTerminal(Materials.SpentMash) && FermenterRules.StockFeed.SequenceEqual(new[] { FermenterRules.Beet, FermenterRules.Sugar }) && FermenterRules.FermentedShare == .92,
            "The spent mash is terminal; beets and sugar are the stock feed");
        check(FermenterRecipes.Match(Enumerable.Repeat(FermenterRules.Beet, 6), false) == null && FermenterRecipes.Match(Enumerable.Repeat(FermenterRules.Beet, 6), true) == beet &&
              FermenterRecipes.Match(Enumerable.Repeat(FermenterRules.Sugar, 6), true) == sugar && !FermenterRecipes.Available(false).Any() && FermenterRecipes.Available(true).Count() == 2,
            "Six beets or six sugar packets match their charge only with Agriculture's sugar crops");
        var ports = new[] { FermenterRules.WaterPort, FermenterRules.CarbonDioxidePort, FermenterRules.EthanolPort, FermenterRules.VesselPort, AcidPlantRules.VesselPort, LeachRules.VesselPort, RefineryRules.VesselPort };
        check(ports.Distinct().Count() == ports.Length, "The fermenter-still's ports are its own");
    }
}
