using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The Oxsmith EC-4 and ferrosilicon (Manufacturing 0.52.0): both electrolysis charges against the reductions
/// they state (IUPAC 2013 molar masses, NIST-JANAF formation enthalpies), the ferrosilicon unit's make-up, and the
/// LC-3's hydrogen charge against the silicon it holds.</summary>
internal static class ElectrolysisChecks
{
    private const double Fe = 0.055845, Si = 0.0280855, O2 = 0.031998, H2O = 0.01801528, H2 = 0.00201588, SiO2 = 0.0600843;
    private const double FeOKJ = 272.04, QuartzKJ = 910.86, WaterKJ = 285.83;
    internal static void Run(Action<bool, string> check)
    {
        var lump = ElectrolysisRecipes.Regolith; var ore = ElectrolysisRecipes.Silicates;
        check(ElectrolysisRecipes.All.Count == 2 && lump.Revision == 1 && ore.Revision == 2 && lump.Machine == ChargeCatalog.ElectrolysisCell &&
              ChargeCatalog.PrefixOf(ChargeCatalog.ElectrolysisCell) == ElectrolysisRules.Prefix && lump.Requires.Count == 0 && ore.Requires.Count == 0,
            "The EC-4 has two charges of its own, neither needing another mod");
        var unit = Materials.ById(Materials.Ferrosilicon)!;
        check(Math.Abs(ElectrolysisRules.FerrosiliconIronKg + ElectrolysisRules.FerrosiliconSiliconKg - unit.Kg) < 1e-9 && !unit.Terminal && unit.Stack >= 3,
            "A ferrosilicon unit is 1.414 kg of iron and 0.786 kg of silicon, and a lump's three stack in one cell");
        foreach (var recipe in new[] { lump, ore })
        {
            int units = recipe.Products.Single(p => p.Id == Materials.Ferrosilicon).Count;
            double iron = units * ElectrolysisRules.FerrosiliconIronKg / Fe, silicon = units * ElectrolysisRules.FerrosiliconSiliconKg / Si;
            double oxygen = (iron / 2 + silicon) * O2;
            check(Math.Abs(recipe.Deposits.Single(p => p.Id == ManufacturingRules.Oxygen).Kg - oxygen) < .005,
                "The oxygen a charge stores is what its ferrosilicon's iron and silicon gave up: " + recipe.Id);
            check(Math.Abs(recipe.ReactionKWh - -(iron * FeOKJ + silicon * QuartzKJ) / 3600) < .05 && -recipe.ReactionKWh < recipe.EnergyKWh,
                "The reductions absorb less than the charge draws: " + recipe.Id);
            check(Math.Abs(recipe.ChargeKg - (recipe.Products.Sum(p => p.Kg * p.Count) + recipe.OffGasKg)) < 1e-9 && !recipe.Melt && recipe.Units == 1 && recipe.Draws.Count == 0 &&
                  recipe.Products.Single(p => p.Id == Materials.RefinerySlag).Kg == Materials.SlagKg,
                "An electrolysis charge conserves mass, is one unit, draws nothing and leaves slag in kilogram units: " + recipe.Id);
        }
        check(lump.ItemInputs.Single().Id == RefineryRules.Regolith && lump.ItemInputs.Single().Kg == RefineryRules.RegolithKg && lump.Seconds == ProcessJob.MaxSeconds &&
              Math.Abs(lump.EnergyKWh - 60) < 1e-9 && lump.OffGas["CO2"] == .1 && lump.Deposits.Single(p => p.Id == ManufacturingRules.Water).Kg == .4 &&
              Math.Abs(lump.Deposits.Single(p => p.Id == ManufacturingRules.Oxygen).Kg / lump.ChargeKg - .195) < 1e-9,
            "A regolith lump gives 19.5 percent of its mass as oxygen in an hour at 60 kW, with the bake's water and carbon dioxide");
        check(ore.ItemInputs.Single().Id == ElectrolysisRules.Silicates && ore.ItemInputs.Single().Kg == ElectrolysisRules.SilicatesKg && ore.OffGas.Count == 0 && ore.Seconds == 2400 &&
              ore.Deposits.Single().Id == ManufacturingRules.Oxygen,
            "A Silicates chunk is dry: oxygen, ferrosilicon and slag in forty minutes");
        check(ElectrolysisRecipes.Match(new[] { RefineryRules.Regolith }) == lump && ElectrolysisRecipes.Match(new[] { ElectrolysisRules.Silicates }) == ore &&
              ElectrolysisRecipes.Match(new[] { RefineryRules.Gangue }) == null && ElectrolysisRules.FeedCapacity == 2 && ElectrolysisRules.WorkingKW == 60 && ElectrolysisRules.Footprint == 4,
            "The cell chooses its charge from the feed and takes nothing else");
        // What the cell does not absorb warms the room: the equipment pack's share is the regolith charge's own balance.
        check(Math.Abs(ElectrolysisRules.RoomHeatFraction - (lump.EnergyKWh + lump.ReactionKWh) / lump.EnergyKWh) < .001, "The room takes the 55 percent of a regolith charge its reactions do not");

        // Hydrogen from ferrosilicon on the LC-3: Si + 2 H2O -> SiO2 + 2 H2 on the silicon of three units.
        var h = LeachRecipes.FerrosiliconHydrogen;
        double mol = 3 * ElectrolysisRules.FerrosiliconSiliconKg / Si;
        check(h.Revision == 15 && h.Machine == ChargeCatalog.Leach && h.ItemInputs.Single().Id == Materials.Ferrosilicon && h.ItemInputs.Single().Count == 3 && h.Requires.Count == 0,
            "The LC-3's hydrogen charge takes three ferrosilicon and needs no other mod");
        check(Math.Abs(h.Draws.Single(i => i.Id == ManufacturingRules.Water).Kg - 2 * mol * H2O) < .005 && Math.Abs(h.Deposits.Single().Kg - 2 * mol * H2) < .001 && h.Deposits.Single().Id == ManufacturingRules.Hydrogen,
            "It draws 3.024 kg of water and stores 0.339 kg of hydrogen for 83.96 mol of silicon");
        var spent = h.Products.Single(p => p.Id == Materials.SpentFerrosilicon);
        check(spent.Count == 3 && Math.Abs(spent.Kg - (ElectrolysisRules.FerrosiliconIronKg + ElectrolysisRules.FerrosiliconSiliconKg / Si * SiO2)) < .002 && Materials.IsTerminal(Materials.SpentFerrosilicon),
            "Each unit leaves its iron and its silicon as silica: 3.095 kg of spent ferrosilicon, terminal");
        check(Math.Abs(h.ChargeKg - h.Products.Sum(p => p.Kg * p.Count)) < 1e-9 && h.OffGas.Count == 0 && Math.Abs(h.ReactionKWh - mol * (QuartzKJ - 2 * WaterKJ) / 3600) < .05,
            "The hydrogen charge conserves mass, breathes nothing into the room and releases about 7.9 kWh");
        check(LeachRules.StockFeed.Contains(Materials.Ferrosilicon) && LeachRules.FeedCapacity >= 3, "Three ferrosilicon fit the LC-3's feed");
        // Against the X2: a kilogram of water gives up the same hydrogen either way; here its oxygen stays in the silica.
        check(h.Deposits.Single().Kg / h.Draws.Single().Kg > ProcessorRules.HydrogenKgPerCycle / ProcessorRules.WaterKgPerCycle * .99 &&
              h.EnergyKWh / h.Deposits.Single().Kg < .3 * ProcessorRules.CycleKWh / ProcessorRules.HydrogenKgPerCycle, "Silicon frees the same hydrogen from the water as the X2, for under a third of the electricity");
        var ports = new[] { ElectrolysisRules.OxygenPort, ElectrolysisRules.WaterPort, ElectrolysisRules.VesselPort, LeachRules.HydrogenPort, LeachRules.VesselPort, AcidPlantRules.VesselPort, AcidPlantRules.OxygenPort };
        check(ports.Distinct().Count() == ports.Length, "The EC-4's ports and the LC-3's hydrogen port are their own");
    }
}
