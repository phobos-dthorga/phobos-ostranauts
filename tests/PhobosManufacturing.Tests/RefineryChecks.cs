using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The V4 catalog and its charge model: every recipe conserves mass, matches only its exact charge,
/// admits only the exact feed units, and the saved record round-trips.</summary>
internal static class RefineryChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        double Sum(ChargeRecipe r) => r.Products.Sum(p => p.Kg * p.Count) + r.OffGasKg;
        foreach (var recipe in RefineryRecipes.All)
        {
            check(Math.Abs(recipe.ChargeKg - Sum(recipe)) < 1e-9, "Charge mass equals products plus off-gas: " + recipe.Id);
            check(recipe.Seconds >= ProcessJob.MinSeconds && recipe.Seconds <= ProcessJob.MaxSeconds, "Duration fits one processing hour: " + recipe.Id);
            check(recipe.Products.All(p => recipe.Inputs.All(i => i.Id != p.Id)), "No recipe yields its own feed: " + recipe.Id);
            check(RefineryRecipes.ByRevision(recipe.Revision) == recipe && RefineryRecipes.ById(recipe.Id) == recipe, "Revision and id resolve: " + recipe.Id);
        }
        check(RefineryRecipes.All.Select(r => r.Revision).Distinct().Count() == 5, "Five distinct revisions");
        // The chemistry table, line by line.
        var h = RefineryRecipes.Hydrates;
        check(h.ChargeKg == 10 && h.Products.Single(p => p.Id == "water").Kg == 1 && h.Products.Single(p => p.Id == "ItmMiningTrash").Count == 3 && h.OffGasKg == 0 && !h.Melt,
            "Hydrates: 10 kg gives 1 kg of water and three 3 kg gangue, no gas, no melt");
        var c = RefineryRecipes.Clay;
        check(c.ChargeKg == 10 && c.Products.Single(p => p.Id == "water").Kg == 2 && c.Products.Single(p => p.Id == Materials.AnhydrousResidue).Kg == 8,
            "Clay hydrates: 10 kg gives 2 kg of water and one 8 kg residue");
        var k = RefineryRecipes.Carbon;
        check(k.Products.Single(p => p.Id == Materials.CarbonStock).Count == 5 && k.Products.Single(p => p.Id == "water").Kg == 1 && k.Products.Single(p => p.Id == "ItmMiningTrash").Count == 1 &&
            Math.Abs(k.OffGasKg - 1) < 1e-12 && k.OffGas["CO2"] == .6 && k.OffGas["CO"] == .3 && k.OffGas["Smoke"] == .1,
            "Carbon: 10 kg gives five carbon, 1 kg water, one gangue and 1 kg of CO2, CO and smoke");
        var n = RefineryRecipes.NickelIron;
        check(n.ChargeKg == 20 && n.Products.Single(p => p.Id == Materials.NickelIronIngot).Count == 4 && n.Products.Single(p => p.Id == "ItmMiningTrash").Count == 1 &&
            n.Products.Single(p => p.Id == Materials.RefinerySlag).Kg == 1 && n.Melt && !n.NeedsVessel && !n.NeedsSteelStock,
            "Nickel-iron: 20 kg gives four 4 kg ingots, one gangue and 1 kg of slag; a melt needing no vessel");
        var s = RefineryRecipes.Steel;
        check(s.Inputs.Count == 2 && s.Inputs.Single(i => i.Id == Materials.NickelIronIngot).Count == 4 && s.Inputs.Single(i => i.Id == Materials.CarbonStock).Count == 1 &&
            s.Products.Single(p => p.Id == "PhobosSteelIngot").Count == 4 && s.Products.Single(p => p.Id == "PhobosSteelMeltRemainder").Kg == 1 && s.Melt && s.NeedsSteelStock,
            "Steel: four nickel-iron and one carbon give four steel ingots and Shipbreaker's remainder; a melt needing Shipbreaker");
        check(Math.Abs(n.EnergyKWh - 16) < 1e-9 && Math.Abs(h.EnergyKWh - 4) < 1e-9 && Math.Abs(k.EnergyKWh - 12) < 1e-9, "Energy per charge follows 24 kW and the duration");
        check(Math.Abs(RefineryRules.RoomHeatKW(true) - 3.6) < 1e-9 && RefineryRules.RoomHeatKW(false) == RefineryRules.IdleKW, "Fifteen percent of 24 kW warms the room while working");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 9) }, null, 600, false, false), "An unbalanced recipe is refused");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 9) }, new Dictionary<string, double> { ["H2"] = 1 }, 600, false, false), "Hydrogen can never be off-gas");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 10) }, null, 3601, false, false), "A duration over an hour is refused");

        // Matching binds the largest exact charge present; nothing else.
        check(RefineryRecipes.Match(new[] { "ItmMineral11" }, false) == h && RefineryRecipes.Match(new[] { "ItmMineral11", "ItmMineral11" }, false) == h, "One or two hydrate blocks match the hydrate charge");
        check(RefineryRecipes.Match(new[] { Materials.ClayHydrates }, false) == c && RefineryRecipes.Match(new[] { "ItmMineral03" }, false) == k && RefineryRecipes.Match(new[] { "ItmMineral01" }, false) == n, "Clay, carbide and iron blocks match their charges");
        var steelCharge = Enumerable.Repeat(Materials.NickelIronIngot, 4).Concat(new[] { Materials.CarbonStock });
        check(RefineryRecipes.Match(steelCharge, true) == s && RefineryRecipes.Match(steelCharge, false) == null, "The steel charge matches only with Shipbreaker's stock");
        check(RefineryRecipes.Match(Enumerable.Repeat(Materials.NickelIronIngot, 4), true) == null && RefineryRecipes.Match(Enumerable.Repeat(Materials.NickelIronIngot, 3).Concat(new[] { Materials.CarbonStock }), true) == null,
            "Four ingots without carbon, or three with, is no charge");
        check(RefineryRecipes.Match(steelCharge.Concat(new[] { "ItmMineral11" }), true) == s, "With a hydrate block beside it the larger steel charge is preferred");
        foreach (string outside in new[] { "ItmIce01", "ItmMineralStone01", "ItmMineral02", "ItmMiningTrash", "ItmScrapSteel", Materials.RefinerySlag })
            check(RefineryRecipes.Match(new[] { outside }, true) == null, "Never a charge: " + outside);
        check(RefineryRecipes.Available(false).Count() == 4 && RefineryRecipes.Available(true).Count() == 5, "Without Shipbreaker four recipes are available; with it five");

        // Feed admission is exact.
        check(RefineryRules.ValidFeed("ItmMineral11", 10, true, true, true, false) && RefineryRules.ValidFeed("ItmMineral01", 20, true, true, true, false) && RefineryRules.ValidFeed("ItmMineral03", 10, true, true, true, false),
            "The three native ores enter at their unit masses");
        check(RefineryRules.ValidFeed(Materials.ClayHydrates, 10, true, true, true, false), "The clay chunk enters");
        check(RefineryRules.ValidFeed(Materials.NickelIronIngot, 4, true, true, true, true) && !RefineryRules.ValidFeed(Materials.NickelIronIngot, 4, true, true, true, false), "Nickel-iron enters only when steel can be made");
        check(!RefineryRules.ValidFeed("ItmMineral11", 9.9, true, true, true, true) && !RefineryRules.ValidFeed("ItmMineral01", 20.5, true, true, true, true), "A wrong mass is refused");
        check(!RefineryRules.ValidFeed("ItmMineral11", 10, false, true, true, true) && !RefineryRules.ValidFeed("ItmMineral11", 10, true, false, true, true) && !RefineryRules.ValidFeed("ItmMineral11", 10, true, true, false, true),
            "Installed, loaded or stacked units are refused");
        check(!RefineryRules.ValidFeed("ItmMineral02", 10, true, true, true, true) && !RefineryRules.ValidFeed("ItmIce01", 24.7, true, true, true, true), "Olivine and ice are not feed");

        // Off-gas is released with progress and never twice.
        check(Math.Abs(k.OffGasDueKg(.5, 0) - .5) < 1e-9 && Math.Abs(k.OffGasDueKg(.5, .3) - .2) < 1e-9 && k.OffGasDueKg(.5, .6) == 0 && Math.Abs(k.OffGasDueKg(1.5, 0) - 1) < 1e-9, "Off-gas due follows progress, capped at the recipe's share");
        check(h.OffGasDueKg(1, 0) == 0, "A recipe without off-gas releases nothing");
        var split = k.Split(.5).ToDictionary(p => p.Key, p => p.Value);
        check(Math.Abs(split["CO2"] - .3) < 1e-12 && Math.Abs(split["CO"] - .15) < 1e-12 && Math.Abs(split["Smoke"] - .05) < 1e-12, "Off-gas splits in the declared proportions");
        throws(() => k.OffGasDueKg(double.NaN, 0), "An invalid progress is refused");

        // A frozen melt yields slag with mass conserved; drying never spoils.
        check(!RefineryRecipes.Spoiled(h, 100000) && !RefineryRecipes.Spoiled(n, 2400) && RefineryRecipes.Spoiled(n, 2401), "Only a melt spoils, and only after waiting longer than its own duration");
        var spoiledIron = RefineryRecipes.SpoiledProducts(n);
        check(spoiledIron.Sum(p => p.Kg * p.Count) == 20 && spoiledIron.Single(p => p.Id == Materials.RefinerySlag).Count == 17 && spoiledIron.Single(p => p.Id == "ItmMiningTrash").Count == 1, "A frozen iron melt is 17 kg of slag and its rock");
        var spoiledSteel = RefineryRecipes.SpoiledProducts(s);
        check(spoiledSteel.Sum(p => p.Kg * p.Count) == 17 && spoiledSteel.Single().Count == 17, "A frozen steel melt is 17 kg of slag");
        throws(() => RefineryRecipes.SpoiledProducts(h), "Drying cannot spoil");

        // Prices keep the recovery rule.
        check(4 * Materials.Ingot.Price < 450 && 5 * Materials.Carbon.Price + 10 < 99 && Materials.Slag.Price == .01 && Materials.Residue.Price == .01 && Materials.Clay.Price == 180,
            "Four ingots are worth less than the iron block, five carbon plus water less than the carbide ore; remainders are trash; the clay chunk sells like hydrates");
        check(Materials.IsTerminal(Materials.RefinerySlag) && Materials.IsTerminal(Materials.AnhydrousResidue) && !Materials.IsTerminal(Materials.CarbonStock) && Materials.IsStock(Materials.NickelIronIngot), "Terminal and stock identities are distinct");

        // The saved record.
        var state = new RefineryState { RecipeId = "steel", Revision = 5, ProgressSeconds = 123.5, WaitSeconds = 10, EmittedKg = 0, Cycles = 2, Running = true, Charge = new List<string> { "a-1", "a-2" } };
        var read = RefineryState.Read(state.Save());
        check(read.RecipeId == "steel" && read.Revision == 5 && read.ProgressSeconds == 123.5 && read.WaitSeconds == 10 && read.Cycles == 2 && read.Running && read.Charge.SequenceEqual(new[] { "a-1", "a-2" }), "The refinery record round-trips");
        var empty = RefineryState.Read(new RefineryState().Save());
        check(!empty.Bound && empty.Charge.Count == 0 && empty.RecipeId == "", "An empty record is unbound");
        throws(() => RefineryState.Read(new Dictionary<string, string> { ["revision"] = "-1" }), "A negative revision is refused");
        throws(() => RefineryState.Read(new Dictionary<string, string> { ["revision"] = "3" }), "A revision without a charge is refused");
        throws(() => RefineryState.Read(new Dictionary<string, string> { ["progress"] = "NaN" }), "An invalid progress is refused");
        throws(() => RefineryState.Read(new Dictionary<string, string> { ["mystery"] = "1" }), "An unknown field is refused");
        check(RefineryState.SafeId("abc-123") && !RefineryState.SafeId("a;b") && !RefineryState.SafeId("a,b") && !RefineryState.SafeId(""), "Bound ids are safe for the record");
        check(ManufacturingRules.Adjacent(0, 0, 4, 3.5, 0, 3) && ManufacturingRules.Adjacent(0, 0, 4, 4.5, 0, 3) && ManufacturingRules.Adjacent(0, 0, 4, 4.5, 4.5, 3),
            "A silo touching, one tile away, or diagonal from the refinery is adjacent");
        check(!ManufacturingRules.Adjacent(0, 0, 4, 5.5, 0, 3) && !ManufacturingRules.Adjacent(0, 0, 4, 2, 0, 3) && !ManufacturingRules.Adjacent(0, 0, 4, double.NaN, 0, 3), "Two tiles away, overlapping, or nowhere is not adjacent");
        check(new[] { RefineryRules.OutPort, RefineryRules.VesselPort, ProcessorRules.WaterInPort, ProcessorRules.VesselOutPort, ProcessorRules.HydrogenOutPort, ProcessorRules.StoreInPort }.Distinct().Count() == 6 &&
            new[] { RefineryRules.OutPort, RefineryRules.VesselPort, ProcessorRules.WaterInPort, ProcessorRules.VesselOutPort, ProcessorRules.HydrogenOutPort, ProcessorRules.StoreInPort }.All(p => p.StartsWith("PhobosManufacturing.")),
            "Ports are distinct and namespaced");
        check(new[] { RefineryRules.Record, ProcessorRules.Record, ProcessorRules.Guard, HydrogenRules.Record, HydrogenRules.Journal, HydrogenRules.Guard }.Distinct().Count() == 6, "Records, journals and guards are distinct");
    }
}
