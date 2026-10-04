using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The carbothermal route (Manufacturing 0.53.0): the CR-4's two charges against the reductions they state,
/// the K2's carbon monoxide mode and its saved record, the carbon monoxide stores, and the loop that turns a lump's
/// carbon monoxide into oxygen with the methane and hydrogen netting to nothing.</summary>
internal static class CarbothermalChecks
{
    private const double Fe = 0.055845, Si = 0.0280855, CH4 = 0.016043, CO = 0.02801, H2 = 0.00201588;
    private const double FeOKJ = 272.04, QuartzKJ = 910.86, MethaneKJ = 74.87, MonoxideKJ = 110.53;
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var lump = CarbothermalRecipes.Regolith; var ore = CarbothermalRecipes.Silicates;
        check(CarbothermalRecipes.All.Count == 2 && lump.Revision == 1 && ore.Revision == 2 && lump.Machine == ChargeCatalog.CarbothermalReactor &&
              ChargeCatalog.PrefixOf(ChargeCatalog.CarbothermalReactor) == CarbothermalRules.Prefix && lump.Requires.Count == 0 && ore.Requires.Count == 0,
            "The CR-4 has two charges of its own, neither needing another mod");
        foreach (var recipe in new[] { lump, ore })
        {
            int units = recipe.Products.Single(p => p.Id == Materials.Ferrosilicon).Count;
            double iron = units * ElectrolysisRules.FerrosiliconIronKg / Fe, silicon = units * ElectrolysisRules.FerrosiliconSiliconKg / Si, oxygenAtoms = iron + 2 * silicon;
            check(Math.Abs(recipe.Draws.Single(i => i.Id == ManufacturingRules.Methane).Kg - oxygenAtoms * CH4) < .005 &&
                  Math.Abs(recipe.Deposits.Single(p => p.Id == ManufacturingRules.CarbonMonoxide).Kg - oxygenAtoms * CO) < .005 &&
                  Math.Abs(recipe.Deposits.Single(p => p.Id == ManufacturingRules.Hydrogen).Kg - 2 * oxygenAtoms * H2) < .005,
                "One methane per oxygen atom removed, giving one carbon monoxide and two hydrogen: " + recipe.Id);
            double absorbed = iron * (FeOKJ + MethaneKJ - MonoxideKJ) + silicon * (QuartzKJ + 2 * MethaneKJ - 2 * MonoxideKJ);
            check(Math.Abs(recipe.ReactionKWh - -absorbed / 3600) < .05 && -recipe.ReactionKWh < recipe.EnergyKWh, "The reductions absorb less than the charge draws: " + recipe.Id);
            check(Math.Abs(recipe.ChargeKg - (recipe.Products.Sum(p => p.Kg * p.Count) + recipe.OffGasKg)) < 1e-9 && !recipe.Melt && recipe.Units == 1,
                "A carbothermal charge conserves mass and is one unit: " + recipe.Id);
            // The same rock gives the same metal and slag by either route.
            var twin = recipe == lump ? ElectrolysisRecipes.Regolith : ElectrolysisRecipes.Silicates;
            check(twin.Products.Single(p => p.Id == Materials.Ferrosilicon).Count == units && twin.Products.Single(p => p.Id == Materials.RefinerySlag).Count == recipe.Products.Single(p => p.Id == Materials.RefinerySlag).Count &&
                  twin.ItemInputs.Single().Id == recipe.ItemInputs.Single().Id && twin.Seconds == recipe.Seconds,
                "Both routes leave the same ferrosilicon and slag of the same rock in the same time: " + recipe.Id);
        }
        check(Math.Abs(lump.EnergyKWh - 30) < 1e-9 && CarbothermalRules.WorkingKW == 30 && CarbothermalRules.WorkingKW * 2 == ElectrolysisRules.WorkingKW && lump.OffGas["CO2"] == .1 &&
              lump.Deposits.Single(p => p.Id == ManufacturingRules.Water).Kg == .4 && CarbothermalRules.FeedCapacity == 2 && CarbothermalRules.Footprint == 4,
            "The CR-4 draws 30 kW, half the EC-4, and a lump gives the bake's water and carbon dioxide");
        check(Math.Abs(CarbothermalRules.RoomHeatFraction - (lump.EnergyKWh + lump.ReactionKWh) / lump.EnergyKWh) < .001, "The room takes the 18 percent of a regolith charge its reactions do not");
        check(CarbothermalRecipes.Match(new[] { RefineryRules.Regolith }) == lump && CarbothermalRecipes.Match(new[] { ElectrolysisRules.Silicates }) == ore && CarbothermalRecipes.Match(new[] { RefineryRules.Gangue }) == null,
            "The reactor chooses its charge from the feed and takes nothing else");

        // The K2's second mode: CO + 3 H2 -> CH4 + H2O on the same hydrogen charge.
        check(SabatierRules.MonoxideBalanced() && Math.Abs(SabatierRules.MonoxideMolesPerCycle * 3 - SabatierRules.HydrogenMolesPerCycle) < 1e-9, "A carbon monoxide cycle takes a third as many moles as its hydrogen and makes the stoichiometric water");
        check(Math.Abs(SabatierRules.MonoxideKgPerCycle - 0.5789) < .001 && Math.Abs(SabatierRules.MonoxideMethaneKgPerCycle - 0.3316) < .001 && Math.Abs(SabatierRules.MonoxideWaterKgPerCycle - 0.3723) < .001,
            "Each carbon monoxide cycle uses 0.579 kg and makes 0.332 kg of methane and 0.372 kg of water");
        check(Math.Abs(SabatierRules.MonoxideReactionKJPerMol - 250.17) < 1e-9 && Math.Abs(SabatierRules.MonoxideReactionKWhPerCycle - 1.436) < .002 &&
              Math.Abs(SabatierRules.RoomHeatKW(true, true) - (SabatierRules.WorkingKW + SabatierRules.MonoxideReactionKWhPerCycle)) < 1e-12 && SabatierRules.RoomHeatKW(true, false) == SabatierRules.RoomHeatKW(true) &&
              SabatierRules.RoomHeatKW(false, true) == SabatierRules.IdleKW,
            "The carbon monoxide reaction releases 250.17 kJ per mole (NIST), 1.44 kWh a cycle, on top of the reactor's electricity");
        check(SabatierRules.CarbonKg(false) == SabatierRules.CarbonDioxideKgPerCycle && SabatierRules.WaterKg(false) == SabatierRules.WaterKgPerCycle && SabatierRules.MethaneKg(false) == SabatierRules.MethaneKgPerCycle &&
              SabatierRules.ReactionKWh(false) == SabatierRules.ReactionKWhPerCycle, "The first mode's figures are unchanged");
        // The saved record: an old record reads and writes exactly as before; carbon monoxide adds two optional fields.
        var old = new SabatierState { HydrogenKg = 0.1, CarbonDioxideKg = 0.3, Canister = "ItmRTACO2abc", Cycles = 4, ConsumedCarbonDioxideKg = 2.7 };
        var saved = old.Save();
        check(saved.Count == 10 && !saved.ContainsKey("co") && !saved.ContainsKey("used_co"), "A reactor that never held carbon monoxide writes the ten fields it always wrote");
        var read = SabatierState.Read(saved);
        check(read.CarbonMonoxideKg == 0 && !read.HoldsMonoxide && read.HoldsDioxide && read.CarbonDioxideKg == .3 && read.Save().OrderBy(p => p.Key).SequenceEqual(saved.OrderBy(p => p.Key)), "An old record reads unchanged and round-trips to the same fields");
        var s = new SabatierState { HydrogenKg = SabatierRules.HydrogenKgPerCycle, CarbonMonoxideKg = SabatierRules.MonoxideKgPerCycle, CycleKWh = 0.4 };
        check(s.Charged && s.HoldsMonoxide && Math.Abs(s.HeldKg - (SabatierRules.HydrogenKgPerCycle + SabatierRules.MonoxideKgPerCycle)) < 1e-12, "A carbon monoxide charge counts toward the held mass and the cycle");
        double before = s.HeldKg;
        s.Convert();
        check(Math.Abs(s.HeldKg - before) < 1e-12 && s.CarbonMonoxideKg == 0 && s.HydrogenKg == 0 && Math.Abs(s.WaterKg - SabatierRules.MonoxideWaterKgPerCycle) < 1e-12 &&
              Math.Abs(s.MethaneKg - SabatierRules.MonoxideMethaneKgPerCycle) < 1e-12 && Math.Abs(s.ConsumedCarbonMonoxideKg - SabatierRules.MonoxideKgPerCycle) < 1e-12 && s.ConsumedCarbonDioxideKg == 0 && s.Cycles == 1,
            "A carbon monoxide cycle converts mass for mass into its own products and counts its own total");
        var round = SabatierState.Read(s.Save());
        check(round.MethaneKg == s.MethaneKg && round.WaterKg == s.WaterKg && round.ConsumedCarbonMonoxideKg == s.ConsumedCarbonMonoxideKg && s.Save().ContainsKey("used_co") && !s.Save().ContainsKey("co"),
            "The second mode's product hold and total round-trip; an empty hold is not written");
        var half = new SabatierState { HydrogenKg = SabatierRules.HydrogenKgPerCycle, CarbonMonoxideKg = 0.2 };
        check(!half.Charged && SabatierState.Read(half.Save()).CarbonMonoxideKg == 0.2, "A part-filled carbon monoxide hold is not a charge, and is kept");
        foreach (var bad in new[] {
            new Dictionary<string, string> { ["co"] = "0.9" }, new Dictionary<string, string> { ["co"] = "-0.1" }, new Dictionary<string, string> { ["co"] = "0.2", ["co2"] = "0.2" },
            new Dictionary<string, string> { ["used_co"] = "-1" }, new Dictionary<string, string> { ["ch4"] = "0.4" } })
            throws(() => SabatierState.Read(bad), "A corrupt carbon monoxide record is refused: " + string.Join(",", bad.Select(p => p.Key + "=" + p.Value)));

        // The loop on one lump: the K2 uses every kilogram of carbon monoxide, the X2 makes up the hydrogen from the K2's
        // own water, and the methane comes back. What is left over is the oxygen.
        double monoxide = lump.Deposits.Single(p => p.Id == ManufacturingRules.CarbonMonoxide).Kg, cycles = monoxide / SabatierRules.MonoxideKgPerCycle;
        double methaneBack = cycles * SabatierRules.MonoxideMethaneKgPerCycle, hydrogenNeeded = cycles * SabatierRules.HydrogenKgPerCycle;
        double hydrogenShort = hydrogenNeeded - lump.Deposits.Single(p => p.Id == ManufacturingRules.Hydrogen).Kg, x2Cycles = hydrogenShort / ProcessorRules.HydrogenKgPerCycle;
        double oxygen = x2Cycles * ProcessorRules.OxygenKgPerCycle, waterUsed = x2Cycles * ProcessorRules.WaterKgPerCycle, waterBack = cycles * SabatierRules.MonoxideWaterKgPerCycle;
        check(Math.Abs(methaneBack - lump.Draws.Single(i => i.Id == ManufacturingRules.Methane).Kg) < .01, "The K2 returns the methane the reactor drew");
        check(Math.Abs(oxygen - ElectrolysisRecipes.Regolith.Deposits.Single(p => p.Id == ManufacturingRules.Oxygen).Kg) < .08 && Math.Abs(waterUsed - waterBack) < .08,
            "Splitting the K2's water makes up the hydrogen and leaves the oxygen the EC-4 would have made, about 3.9 kg");
        check(cycles > 11 && cycles < 12.5 && x2Cycles > 3.5 && x2Cycles < 4.5, "A lump keeps one K2 busy for about twelve hours and one X2 for about four");

        // The carbon monoxide stores.
        var family = GasStores.CarbonMonoxideFamily; var fuel = family.Fuel!;
        double moles = NativeGasCanister.CapacityMoles(0.787, 41400, 293) * GasStores.UsableMolesFraction;
        check(family.Species == "CO" && family.Model == "Z" && family.Commodity == ManufacturingRules.CarbonMonoxide && family.LeaksIntoRoom && family.Small.LeakSpecies == "CO" && family.Small.IsFuel &&
              GasStores.FamilyOf("carbon monoxide") == family && ChargeCommodities.Is(ManufacturingRules.CarbonMonoxide) && NativeGasCanister.IsRoomSpecies("CO"),
            "The Z stores hold the game's own carbon monoxide, which leaks into the room and burns");
        check(Math.Abs(family.SmallCapacityKg - NativeGasCanister.Kilograms("CO", moles)) / family.SmallCapacityKg < .01 && family.SmallCapacityKg == 300 && family.Small.Spec.DamagePolicy == VesselDamagePolicy.Leak,
            "The small store holds 80% of the canister vessel's ideal moles, 300 kg");
        check(Math.Abs(fuel.OxygenPerFuel - 0.5712) < .001 && Math.Abs(fuel.RoomProductsPerKg["CO2"] - 1.5712) < .001 && Math.Abs(fuel.HeatingKJPerKg / 1000 - 10.10) < .02,
            "CO + 1/2 O2 -> CO2: 0.57 kg of oxygen and 1.57 kg of CO2 per kg, 10.1 MJ/kg (NIST)");
        var burn = family.Small.Burn(10, 2);
        check(Math.Abs(burn.BurnedKg - 2 / fuel.OxygenPerFuel) < 1e-9 && Math.Abs(burn.BurnedKg + burn.LostKg - 10) < 1e-12 && Math.Abs(burn.RoomProductsKg["CO2"] - burn.BurnedKg - burn.OxygenKg) < 1e-4,
            "A carbon monoxide burn is bounded by the room's oxygen and leaves exactly the carbon dioxide its mass makes");
        check(!FillerRules.Species.Contains("CO") && Math.Abs(ManifoldRules.Ratio("carbon monoxide") - 1) < .01,
            "The L2 does not bottle carbon monoxide; a P1 values it like nitrogen, should a player choose to burn it");
        var ports = new[] { CarbothermalRules.MethanePort, CarbothermalRules.MonoxidePort, CarbothermalRules.HydrogenPort, CarbothermalRules.WaterPort, CarbothermalRules.VesselPort,
            ElectrolysisRules.VesselPort, LeachRules.VesselPort, RefineryRules.VesselPort };
        check(ports.Distinct().Count() == ports.Length, "The CR-4's ports are its own");
    }
}
