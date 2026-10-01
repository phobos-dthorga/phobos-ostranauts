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
        check(RefineryRecipes.All.Select(r => r.Revision).Distinct().Count() == 12, "Twelve distinct revisions");
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
        var ns = RefineryRecipes.NickelSteel;
        check(ns.Inputs.Select(i => (i.Id, i.Count, i.Kg)).SequenceEqual(s.Inputs.Select(i => (i.Id, i.Count, i.Kg))) && ns.Products.Single(p => p.Id == Materials.NickelSteelIngot).Count == 4 &&
              ns.Products.Single(p => p.Id == Materials.RefinerySlag).Kg == 1 && ns.Melt && !ns.NeedsSteelStock && ns.Seconds == s.Seconds && ns.Supersedes.SequenceEqual(new[] { s.Revision }),
            "Nickel steel takes the steel charge's exact items, gives four nickel steel ingots and a kilogram of slag, needs no other mod and supersedes the steel charge");
        check(Math.Abs(n.EnergyKWh - 16) < 1e-9 && Math.Abs(h.EnergyKWh - 4) < 1e-9 && Math.Abs(k.EnergyKWh - 12) < 1e-9, "Energy per charge follows 24 kW and the duration");
        check(Math.Abs(RefineryRules.RoomHeatKW(true) - 3.6) < 1e-9 && RefineryRules.RoomHeatKW(false) == RefineryRules.IdleKW, "Fifteen percent of 24 kW warms the room while working");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 9) }, null, 600, false, false), "An unbalanced recipe is refused");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 9) }, new Dictionary<string, double> { ["H2"] = 1 }, 600, false, false), "Hydrogen can never be off-gas");
        throws(() => new ChargeRecipe("bad", 9, new[] { new ChargeInput("x", 1, 10) }, new[] { new ProductSpec("y", 1, 10) }, null, 3601, false, false), "A duration over an hour is refused");

        // Matching binds the largest exact charge present; nothing else.
        check(RefineryRecipes.Match(new[] { "ItmMineral11" }, false) == h && RefineryRecipes.Match(new[] { "ItmMineral11", "ItmMineral11" }, false) == h, "One or two hydrate blocks match the hydrate charge");
        check(RefineryRecipes.Match(new[] { Materials.ClayHydrates }, false) == c && RefineryRecipes.Match(new[] { "ItmMineral03" }, false) == k && RefineryRecipes.Match(new[] { "ItmMineral01" }, false) == n, "Clay, carbide and iron blocks match their charges");
        var steelCharge = Enumerable.Repeat(Materials.NickelIronIngot, 4).Concat(new[] { Materials.CarbonStock });
        check(RefineryRecipes.Match(steelCharge, true) == ns && RefineryRecipes.Match(steelCharge, false) == ns, "New charges of four nickel-iron and one carbon make nickel steel, with or without Shipbreaker");
        check(RefineryRecipes.ByRevision(s.Revision) == s && !RefineryRecipes.Available(true).Contains(s), "The superseded steel charge still resolves by its revision for a bound job, but is never offered");
        check(RefineryRecipes.Match(Enumerable.Repeat(Materials.NickelIronIngot, 4), true) == null, "Four ingots without carbon is no charge");
        var bed = RefineryRecipes.ById("methane-pyrolysis")!;
        var threeAndCarbon = Enumerable.Repeat(Materials.NickelIronIngot, 3).Concat(new[] { Materials.CarbonStock }).ToArray();
        check(RefineryRecipes.Match(threeAndCarbon, true) == bed && ChargeCatalog.For(ChargeCatalog.Refinery).Match(threeAndCarbon, RefineryRecipes.Met(true), r => r.Draws.Count == 0) == null,
            "Three ingots and a carbon offer only the carbon as a methane-cracking bed, which a machine binds only with a methane store linked");
        check(RefineryRecipes.Match(steelCharge.Concat(new[] { "ItmMineral11" }), true) == ns, "With a hydrate block beside it the larger nickel steel charge is preferred");
        foreach (string outside in new[] { "ItmIce01", "ItmMineralStone01", "ItmMineral02", "ItmMiningTrash", "ItmScrapSteel", Materials.RefinerySlag })
            check(RefineryRecipes.Match(new[] { outside }, true) == null, "Never a charge: " + outside);
        check(RefineryRecipes.Available(false).Count() == 11 && RefineryRecipes.Available(true).Count() == 11, "Eleven recipes are available with or without Shipbreaker: nickel steel replaces the plain steel charge");

        // Feed admission is exact.
        check(RefineryRules.ValidFeed("ItmMineral11", 10, true, true, true, false) && RefineryRules.ValidFeed("ItmMineral01", 20, true, true, true, false) && RefineryRules.ValidFeed("ItmMineral03", 10, true, true, true, false),
            "The three native ores enter at their unit masses");
        check(RefineryRules.ValidFeed(Materials.ClayHydrates, 10, true, true, true, false), "The clay chunk enters");
        check(RefineryRules.ValidFeed(Materials.NickelIronIngot, 4, true, true, true, true) && RefineryRules.ValidFeed(Materials.NickelIronIngot, 4, true, true, true, false), "Nickel-iron enters with or without Shipbreaker, for nickel steel");
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
        var spoiledNickelSteel = RefineryRecipes.SpoiledProducts(ns);
        check(spoiledNickelSteel.Sum(p => p.Kg * p.Count) == 17 && spoiledNickelSteel.Single().Count == 17, "A frozen nickel steel melt is 17 kg of slag");
        throws(() => RefineryRecipes.SpoiledProducts(h), "Drying cannot spoil");

        // Ammonium salt crust: 2 NH4Cl + Na2CO3 -> 2 NH3 + CO2 + H2O + 2 NaCl on an authored 3.000 kg of NH4Cl.
        var a = RefineryRecipes.Ammonium;
        double nh4cl = 3.0 / 0.053491, reactions = nh4cl / 2;
        check(Math.Abs(a.StoredGases.Single(p => p.Id == "ammonia").Kg - nh4cl * 0.017031) < .001 && Math.Abs(a.OffGas["CO2"] - reactions * 0.04401) < .001 &&
              Math.Abs(a.Products.Single(p => p.Id == "water").Kg - reactions * 0.018015) < .001, "Ammonia, carbon dioxide and water follow the reaction on 56.08 mol of ammonium chloride");
        double sodaKg = reactions * 0.105988, saltKg = nh4cl * 0.05844, remainderKg = 10 - 3.0 - sodaKg;
        check(Math.Abs(a.Products.Single(p => p.Id == Materials.SpentSaltCake).Kg - (saltKg + remainderKg)) < .001 && Math.Abs(a.ChargeKg - Sum(a)) < 1e-9,
            "The salt cake is the sodium chloride plus the crust's clay; the charge conserves mass");
        check(a.StoredGases.Count == 1 && a.Solids(a.Products).Single().Id == Materials.SpentSaltCake && RefineryRules.StoredGasFamilies.Contains(GasStores.AmmoniaFamily),
            "Ammonia goes to a store, the cake to the tray, water to the vessel, CO2 into the room");
        // Standard enthalpies of formation (kJ/mol): NH4Cl -314.4, Na2CO3 -1130.7, NaCl -411.2, NH3 -45.9, CO2 -393.5, H2O(g) -241.8.
        double reactionKJ = (2 * -411.2 + 2 * -45.9 + -393.5 + -241.8) - (2 * -314.4 + -1130.7);
        check(Math.Abs(reactionKJ - 210) < 1 && a.EnergyKWh * (1 - RefineryRules.RoomHeatFraction) > (reactions * reactionKJ + 10 * 0.9 * 330) / 3600,
            "The 15 minute charge covers the reaction's +210 kJ per mole and heating the crust, after the room's share");
        check(RefineryRules.ValidFeed(Materials.AmmoniumSaltCrust, 10, true, true, true, false) && RefineryRecipes.Match(new[] { Materials.AmmoniumSaltCrust }, false) == a, "The salt crust enters and matches its charge");
        check(Materials.Crust.Mined && Materials.Clay.Mined && !Materials.Ingot.Mined && Materials.IsTerminal(Materials.SpentSaltCake) && !Materials.IsTerminal(Materials.AmmoniumSaltCrust),
            "Mined chunks and terminal remainders are declared on the material");
        check(Materials.Crust.Price > a.StoredGases.Single().Kg * 3.40 + a.OffGas["CO2"] * 1.3 + a.Products.Single(p => p.Id == "water").Kg * 10 + Materials.SaltCake.Price,
            "The crust is worth more than its ammonia and carbon dioxide at the game's gas prices, its water and the cake");

        // Calcining the LC-3's leached residue (Manufacturing 0.18.0): MgCO3 -> MgO + CO2 on 0.50 kg of magnesite.
        var cal = RefineryRecipes.Calcine;
        double magnesite = 0.500 / 0.084313;
        check(Math.Abs(cal.StoredGases.Single(p => p.Id == ManufacturingRules.CarbonDioxide).Kg - magnesite * 0.044009) < .002 &&
              Math.Abs(cal.Products.Single(p => p.Id == Materials.CalcinedResidue).Kg - (6.30 + magnesite * 0.040304)) < .002 && Math.Abs(cal.ChargeKg - Sum(cal)) < 1e-9,
            "Calcining gives the magnesite's carbon dioxide to a store and leaves the matrix and magnesia as the calcined residue");
        // Formation enthalpies (kJ/mol): MgO -601.6 and CO2 -393.5 (CODATA key values), magnesite -1113.3 (Robie and Hemingway 1995).
        double calcineKJ = -601.6 + -393.5 - -1113.3;
        check(Math.Abs(cal.ReactionKWh + magnesite * calcineKJ / 3600) < .01 && cal.ReactionKWh < 0 && Math.Abs(cal.EnergyKWh - 3) < 1e-9 && !cal.Melt,
            "The calcine absorbs its reaction heat (about 118 kJ per mole) within a 3 kWh charge; not a melt");
        check(RefineryRules.StoredGasFamilies.Count() == 3 && RefineryRules.StoredGasFamilies.Contains(GasStores.CarbonDioxideFamily) && RefineryRules.StoredGasFamilies.Contains(GasStores.HydrogenFamily),
            "The V4 sends ammonia, carbon dioxide and hydrogen to stores");
        check(RefineryRules.GasFamilies.Count() == 5 && RefineryRules.GasFamilies.Contains(GasStores.MethaneFamily) && RefineryRules.GasFamilies.Contains(GasStores.OxygenFamily) &&
              !RefineryRules.Stores(GasStores.MethaneFamily) && RefineryRules.Stores(GasStores.HydrogenFamily),
            "The V4 links five gas stores: methane and oxygen it draws from, ammonia, carbon dioxide and hydrogen it fills");

        // The interdependency first slice (Manufacturing 0.27.0), with IUPAC 2013 molar masses.
        const double C = 0.012011, H2 = 0.002016, CH4 = 0.016043, O2 = 0.031998, CO2 = 0.044009;
        double cracked = 3.0 / C;
        check(Math.Abs(bed.Draws.Single(i => i.Id == ManufacturingRules.Methane).Kg - cracked * CH4) < .001 && Math.Abs(bed.Deposits.Single(p => p.Id == ManufacturingRules.Hydrogen).Kg - cracked * 2 * H2) < .001 &&
              bed.Products.Single(p => p.Id == Materials.CarbonBlack).Count == 4 && bed.ItemInputs.Single().Id == Materials.CarbonStock && Math.Abs(bed.ChargeKg - Sum(bed)) < 1e-9 && !bed.Melt,
            "Methane cracking: CH4 -> C + 2 H2 on 249.77 mol; the carbon stock bed and the 3 kg of new carbon leave as four carbon black");
        check(Math.Abs(bed.ReactionKWh + cracked * 74.87 / 3600) < .01 && bed.ReactionKWh < 0 && bed.EnergyKWh > -bed.ReactionKWh,
            "Methane cracking absorbs 74.87 kJ a mole, 5.19 kWh, within the charge's electricity");
        var burn = RefineryRecipes.ById("carbon-burn")!;
        double burned = 1.0 / C;
        check(Math.Abs(burn.Draws.Single(i => i.Id == ManufacturingRules.Oxygen).Kg - burned * O2) < .001 && Math.Abs(burn.Deposits.Single(p => p.Id == ManufacturingRules.CarbonDioxide).Kg - burned * CO2) < .001 &&
              burn.ItemInputs.Single().Id == Materials.CarbonBlack && burn.OffGasKg == 0 && Math.Abs(burn.ReactionKWh - burned * 393.51 / 3600) < .01,
            "Carbon burning: C + O2 -> CO2 on 83.26 mol, all of it to a store, releasing 9.10 kWh");
        foreach (var (id, spent, ready) in new[] { ("scrubber-reactivation", "ItmFilterCO201Dmg", "ItmFilterCO201"), ("eva-filter-reactivation", "ItmFilterCO202Dmg", "ItmFilterCO202") })
        {
            var r = RefineryRecipes.ById(id)!;
            check(r.ItemInputs.Single().Id == spent && r.ItemInputs.Single().Count == 4 && r.Products.Single(p => p.Id == ready).Count == 3 &&
                  r.Products.Single(p => p.Id == Materials.ExhaustedSorbent).Count == 1 && r.OffGasKg == 0 && r.Draws.Count == 0 && Math.Abs(r.ChargeKg - Sum(r)) < 1e-9,
                "Reactivation: four spent give three ready and one exhausted sorbent, nothing into the air: " + id);
        }
        check(Materials.IsTerminal(Materials.ExhaustedSorbent) && !Materials.IsTerminal(Materials.CarbonBlack) && RefineryRules.FeedConditions.Contains("IsFilterCO2") && RefineryRules.StockFeed.Contains(Materials.CarbonBlack),
            "The sorbent is terminal; carbon black and the game's CO2 filters enter the V4's feed");
        check(RefineryRules.ValidFeed(Materials.LeachedResidue, 6.8, true, true, true, false) && RefineryRecipes.Match(new[] { Materials.LeachedResidue }, false) == cal && Materials.IsTerminal(Materials.CalcinedResidue) && !Materials.IsTerminal(Materials.LeachedResidue),
            "The leached residue enters and matches the calcine; the calcined residue is terminal");

        // Refining as a business (owner approval, 1 October 2026): a chain from mined ore to finished items earns 1.5 to 2.5
        // times the ore at base prices; remainders are trash; the clay chunk sells like hydrates. The native checks repeat
        // this at live prices with the game's own ore and gangue values.
        check(Materials.Slag.Price == .01 && Materials.Residue.Price == .01 && Materials.Clay.Price == 180, "Remainders are trash; the clay chunk sells like hydrates");
        double nickelSteel = Materials.ById(Materials.NickelSteelIngot)!.Price;
        check(4 * nickelSteel > 4 * Materials.Ingot.Price + Materials.Carbon.Price && 4 * nickelSteel <= 1.5 * (4 * Materials.Ingot.Price + Materials.Carbon.Price),
            "Carburising into nickel steel gains a little on its four ingots and carbon, as a step inside the iron chain");
        check(4 * nickelSteel > 4 * 25 * 2, "Nickel steel is worth well over Shipbreaker's plain steel ingot, which the F6 casts from scrap");
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
