using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The Lixivar LC-3 (Manufacturing 0.18.0, acid consumers 0.20.0): its shape, the six recipes against their reactions (IUPAC 2013
/// molar masses), one terminal remainder per feed, the settlement each recipe asks of its links, feed admission by the
/// selected recipe, prices under the refining guardrails, and its saved record with the selection.</summary>
internal static class LeachChecks
{
    // Molar masses, kg/mol (IUPAC 2013 conventional atomic weights).
    private const double KCl = 0.074551, Na2SO4 = 0.142036, K2SO4 = 0.174252, NaCl = 0.05844, NaMgPO4 = 0.142265, NH3 = 0.017031, H2O = 0.018015,
        Struvite = 0.245404, NaOH = 0.039997, H2SO4 = 0.098072, H3PO4 = 0.097994, Epsom = 0.246466, AmmoniumSulfate = 0.132134, Melanterite = 0.278006,
        SiO2 = 0.060083, Olivine = 0.158984, N = 0.014007, K = 0.039098;
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        check(LeachRules.Footprint == 3 && LeachRules.FeedCapacity == 4 && LeachRules.MachineKg == 220 && LeachRules.WorkingKW == 12 && LeachRules.IdleKW == 0.1 && LeachRules.RoomHeatFraction == .15,
            "The LC-3 is 3 x 3, 220 kg, 12 kW working and 0.1 kW idle, 15 percent into the room, four feed cells");
        check(ChargeCatalog.PrefixOf(ChargeCatalog.Leach) == LeachRules.Prefix && Equipment.Entry(LeachRules.Prefix).installTab == "APPS", "The leach catalog runs on the LC-3, installed from APPS");
        double Sum(ChargeRecipe r) => r.Products.Sum(p => p.Kg * p.Count) + r.OffGasKg;
        foreach (var recipe in LeachRecipes.All)
        {
            check(recipe.Machine == ChargeCatalog.Leach && Math.Abs(recipe.ChargeKg - Sum(recipe)) < 1e-9, "Charge mass equals products plus off-gas: " + recipe.Id);
            check(recipe.Units <= LeachRules.FeedCapacity && recipe.Seconds >= ProcessJob.MinSeconds && recipe.Seconds <= ProcessJob.MaxSeconds && !recipe.Melt && recipe.OffGas.Count == 0,
                "Fits the feed, one processing hour, no melt and nothing into the room: " + recipe.Id);
            check(recipe.Products.All(p => recipe.Inputs.All(i => i.Id != p.Id)), "No recipe yields its own feed: " + recipe.Id);
            check(recipe.Solids(recipe.Products).Count(p => Materials.IsTerminal(p.Id)) <= 1, "At most one terminal remainder per feed: " + recipe.Id);
        }
        check(LeachRecipes.All.Select(r => r.Revision).OrderBy(r => r).SequenceEqual(Enumerable.Range(1, 14)), "Fourteen revisions of its own, 1 to 14 (the gangue wash and its outcomes are 7 to 10, the regolith leach and its outcomes 11 to 14)");
        check(LeachRecipes.Available(false).Count() == 6 && LeachRecipes.Available(true).Count() == 7 && LeachRecipes.Available(true, true).Count() == 8 &&
              !LeachRecipes.Available(false).Contains(LeachRecipes.Makeup) && !LeachRecipes.Available(true).Contains(LeachRecipes.CropNutrients),
            "Without Agriculture neither formulation is available, and crop nutrients need its hoppers");

        // Evaporite leach: 2 KCl + Na2SO4 -> K2SO4 + 2 NaCl, KCl limiting, in 20 kg of circulating water.
        var leach = LeachRecipes.Leach;
        double kcl = 0.600 / KCl, sulfate = 0.600 / Na2SO4;
        check(kcl / 2 < sulfate, "Potassium chloride limits the glaserite route");
        double k2so4 = kcl / 2 * K2SO4, cake = 1.200 + kcl * NaCl + (sulfate - kcl / 2) * Na2SO4 + 0.500 + 0.050;
        check(Math.Abs(leach.Products.Single(p => p.Id == Materials.PotassiumSulfate).Kg - k2so4) < .002 && Math.Abs(leach.Products.Single(p => p.Id == Materials.BrineSaltCake).Kg - cake) < .003 &&
              leach.Products.Single(p => p.Id == Materials.PhosphateConcentrate).Kg == .25 && leach.Products.Single(p => p.Id == Materials.LeachedResidue).Kg == 6.8,
            "Leach: 0.70 kg potassium sulfate, 0.25 kg phosphate, 6.80 kg leached residue (matrix and magnesite) and 2.25 kg of brine salt cake per crust");
        check(leach.ItemInputs.Single().Id == Materials.EvaporiteCrust && leach.Draws.Count == 0 && leach.Circulates[ManufacturingRules.Water] == 20 && Math.Abs(leach.EnergyKWh - 12) < 1e-9,
            "Leach: one crust, 20 kg of water on hand and returned, 12 kWh");
        var leachNeeds = SettlementPlan.Build(leach.Circulates.Select(c => new SettlementLeg(c.Key, SettlementRole.Circulate, c.Value)));
        check(leachNeeds.Single().NeedAvailableKg == 20 && !leachNeeds.Single().Changes, "A leach needs 20 kg of water on hand and changes the vessel by nothing");

        // Struvite: NaMgPO4 + NH3 + 7 H2O -> MgNH4PO4.6H2O + NaOH on the concentrate's 1.757 mol.
        var st = LeachRecipes.Struvite;
        double phosphate = 0.250 / NaMgPO4;
        check(Math.Abs(st.Draws.Single(i => i.Id == ManufacturingRules.Ammonia).Kg - phosphate * NH3) < .001 && Math.Abs(st.Draws.Single(i => i.Id == ManufacturingRules.Water).Kg - 7 * phosphate * H2O) < .003 &&
              Math.Abs(st.Products.Single(p => p.Id == Materials.Struvite).Kg - phosphate * Struvite) < .002 && Math.Abs(st.Products.Single(p => p.Id == Materials.CausticRemainder).Kg - phosphate * NaOH) < .001,
            "Struvite: 30 g of ammonia and 0.22 kg of water with the concentrate give 0.43 kg of struvite and 0.07 kg of caustic remainder");
        check(st.ItemInputs.Single().Id == Materials.PhosphateConcentrate && st.Deposits.Count == 0 && Math.Abs(st.EnergyKWh - 1) < 1e-9, "Struvite: one concentrate, both reagents drawn, nothing deposited, 1 kWh");
        var stNeeds = SettlementPlan.Build(st.Draws.Select(i => new SettlementLeg(i.Id, SettlementRole.Draw, i.Kg * i.Count)));
        check(stNeeds.Count == 2 && stNeeds.All(n => n.Changes && n.NetKg < 0), "Struvite draws from two linked vessels");
        check(Math.Abs(RefineryRecipes.CrustAmmoniaKg / st.Draws.Single(i => i.Id == ManufacturingRules.Ammonia).Kg - 31.8) < .1, "One ammonium salt crust's ammonia feeds about thirty struvite charges");

        // Makeup formulation: one potassium sulfate and two struvite make 39 Groundwork makeup packets, nothing left.
        var mk = LeachRecipes.Makeup;
        check(mk.Inputs.Single(i => i.Id == Materials.PotassiumSulfate).Count == 1 && mk.Inputs.Single(i => i.Id == Materials.Struvite).Count == 2 &&
              mk.Products.Single().Id == LeachRules.MakeupPacket && mk.Products.Single().Count == 39 && mk.Products.Single().Kg == LeachRules.MakeupPacketKg,
            "Formulation: one potassium sulfate and two struvite give 39 makeup packets of 40 g, with no remainder");
        check(mk.Requires.SequenceEqual(new[] { ChargeCatalog.MakeupRequirement }) && Math.Abs(mk.EnergyKWh - .5) < 1e-9 && mk.Units == 3, "Formulation needs Agriculture, 0.5 kWh, three units");
        check(mk.Products.Single().Count * 30 > mk.ItemInputs.Sum(i => i.Count * Materials.ById(i.Id)!.Price), "The formulation makes value, capped at Agriculture's own packet price (owner decision)");


        // Epsom salt from olivine (0.20.0): (Mg0.71Fe0.29)2SiO4 + 2 H2SO4 + 12 H2O -> 1.42 MgSO4.7H2O + 0.58 FeSO4.7H2O + SiO2
        // on the chunk's authored 7.000 kg of Fa29 olivine; 32 Epsom units crystallise, the rest stays in the cake's liquor.
        var ep = LeachRecipes.Epsom;
        double olivine = 7.000 / Olivine, mg = 1.42 * olivine, fe = 0.58 * olivine, epsomMol = 32 * .432 / Epsom;
        check(Math.Abs(ep.Draws.Single(i => i.Id == LiquidStores.SulfuricAcid).Kg - 2 * olivine * H2SO4) < .005 && Math.Abs(ep.Draws.Single(i => i.Id == ManufacturingRules.Water).Kg - 12 * olivine * H2O) < .005,
            "Epsom salt: 8.64 kg of acid and 9.52 kg of water for 44.03 mol of olivine");
        check(ep.Products.Single(p => p.Id == Materials.EpsomSalt).Count == 32 && epsomMol < mg && epsomMol / mg > .85 &&
              Math.Abs(ep.Products.Single(p => p.Id == Materials.OlivineLeachCake).Kg - (3.000 + olivine * SiO2 + fe * Melanterite + (mg - epsomMol) * Epsom)) < .005,
            "Epsom salt: 32 units (about nine-tenths of the magnesium) and a 14.33 kg leach cake of silica, iron sulfate, rock and liquor");
        // Formation enthalpies, kJ/mol: forsterite -2173.0, fayalite -1478.2, epsomite -3388.7, melanterite -3014.6, amorphous silica -903.5.
        double olivineKJ = (1.42 * -3388.7 + 0.58 * -3014.6 + -903.5) - (0.71 * -2173.0 + 0.29 * -1478.2 + 2 * -814.0 + 12 * -285.83);
        check(Math.Abs(ep.ReactionKWh - -olivine * olivineKJ / 3600) < .05 && Math.Abs(ep.EnergyKWh - 12) < 1e-9 && ep.Units == 1,
            "Epsom salt: one chunk, 12 kWh of electricity and about 5.3 kWh of reaction heat into the room");

        // The acid route to struvite: H3PO4 + MgSO4.7H2O + 3 NH3 -> MgNH4PO4.6H2O + (NH4)2SO4 + H2O on the flask.
        var ab = LeachRecipes.AcidStruvite;
        double flask = .515 / H3PO4, stMol = 3 * .43 / Struvite;
        check(Math.Abs(flask / stMol - 1) < .001 && Math.Abs(3 * .432 / Epsom / stMol - 1) < .001, "One flask and three Epsom salt carry the phosphorus and magnesium of three struvite units");
        check(Math.Abs(ab.Draws.Single(i => i.Id == ManufacturingRules.Ammonia).Kg - 3 * stMol * NH3) < .001 &&
              Math.Abs(ab.Products.Single(p => p.Id == Materials.AmmoniumSulfate).Kg * 3 - stMol * AmmoniumSulfate) < .002 &&
              Math.Abs(ab.Deposits.Single(p => p.Id == ManufacturingRules.Water).Kg - stMol * H2O) < .001 && ab.Products.Single(p => p.Id == Materials.Struvite).Count == 3,
            "Acid route: 0.269 kg of ammonia gives three struvite, three ammonium sulfate and 94 g of water back to the vessel");
        check(ab.Units == 4 && Math.Abs(ab.EnergyKWh - 2) < 1e-9 && ab.Solids(ab.Products).All(p => !Materials.IsTerminal(p.Id)), "Acid route: four units, 2 kWh, no remainder");
        double abIn = Materials.ById(Materials.PhosphoricAcidFlask)!.Price + 3 * Materials.ById(Materials.EpsomSalt)!.Price;
        double abOut = ab.Solids(ab.Products).Sum(p => p.Count * Materials.ById(p.Id)!.Price);
        check(abOut > abIn && abOut <= 1.5 * abIn, "The acid route, a step inside the nodule and olivine chains, gains a little on the flask and Epsom salt, before the ammonia");

        // The complete formulation: one of each salt, with ammonia neutralised by acid in the mixer to top up nitrogen.
        var cn = LeachRecipes.CropNutrients;
        double nh3 = cn.Draws.Single(i => i.Id == ManufacturingRules.Ammonia).Kg / NH3, acid = cn.Draws.Single(i => i.Id == LiquidStores.SulfuricAcid).Kg / H2SO4;
        check(Math.Abs(nh3 / acid - 2) < .01 && cn.Units == 4 && cn.ItemInputs.All(i => i.Count == 1) && cn.Products.Single().Id == ManufacturingRules.CropNutrients && cn.Deposits.Count == 1,
            "Formulation: one of each of the four salts, and ammonia and acid in the ratio of ammonium sulfate, into crop nutrients for a hopper");
        double n = .43 * N / Struvite + .232 * 2 * N / AmmoniumSulfate + nh3 * N, k = .7 * 2 * K / K2SO4;
        check(Math.Abs(n / k - 210.0 / 235) < .01, "Formulation: nitrogen to potassium at Hoagland solution's 210 to 235");
        check(Math.Abs(cn.ReactionKWh - acid * 274.98 / 3600) < .01 && Math.Abs(cn.EnergyKWh - 1) < 1e-9 && cn.Requires.SequenceEqual(new[] { ChargeCatalog.CropNutrientsRequirement }),
            "Formulation: 1 kWh, about 0.56 kWh of neutralisation heat, and it needs Agriculture's hoppers");
        var cnNeeds = SettlementPlan.Build(cn.Draws.Select(i => new SettlementLeg(i.Id, SettlementRole.Draw, i.Kg * i.Count)).Concat(cn.Deposits.Select(p => new SettlementLeg(p.Id, SettlementRole.Deposit, p.Kg * p.Count))));
        check(cnNeeds.Count == 3 && cnNeeds.Single(x => x.Commodity == ManufacturingRules.CropNutrients).NetKg > 0, "Formulation draws from two linked vessels and fills a hopper");
        check(ChargeCommodities.Is(ManufacturingRules.CropNutrients) && !ChargeCommodities.Is(Materials.EpsomSalt), "Crop nutrients are a commodity, the salts items");

        // Feed admission follows the selected recipe.
        check(LeachRecipes.FeedKg(Materials.EvaporiteCrust, leach, false) == 10 && LeachRecipes.FeedKg(Materials.PotassiumSulfate, leach, true) == null &&
              LeachRecipes.FeedKg(Materials.PotassiumSulfate, mk, true) == .7 && LeachRecipes.FeedKg(Materials.PotassiumSulfate, mk, false) == null && LeachRecipes.FeedKg("ItmMineral11", leach, true) == null,
            "Only the selected recipe's feed enters, and the formulation's only with Agriculture");
        check(LeachRules.FeedIds.All(id => LeachRecipes.All.SelectMany(r => r.ItemInputs).Any(i => i.Id == id)) && LeachRecipes.All.SelectMany(r => r.ItemInputs).All(i => LeachRules.FeedIds.Contains(i.Id)) &&
              !LeachRules.StockFeed.Contains(LeachRules.Olivine),
            "The feed rule names exactly the LC-3's feed identities; the olivine comes in by the game's ore rule");

        // Materials and prices (refining as a business, owner approval 1 October 2026): the crust's salts are worth 1.5 to 2.5
        // times the crust at base prices; struvite gains a little on its concentrate.
        check(Materials.ById(Materials.EvaporiteCrust)!.Mined && Materials.ById(Materials.EvaporiteCrust)!.Price == 150, "The crust is mined and sells like the salt crust");
        double salts = leach.Solids(leach.Products).Sum(p => p.Count * Materials.ById(p.Id)!.Price), crust = Materials.ById(Materials.EvaporiteCrust)!.Price;
        check(salts >= 1.5 * crust && salts <= 2.5 * crust, "Leaching a crust earns 1.5 to 2.5 times the crust: " + salts / crust);
        check(Materials.ById(Materials.Struvite)!.Price > Materials.ById(Materials.PhosphateConcentrate)!.Price && Materials.ById(Materials.Struvite)!.Price <= 1.5 * Materials.ById(Materials.PhosphateConcentrate)!.Price,
            "Struvite gains a little on its concentrate, before the reagents' value");
        foreach (string id in new[] { Materials.BrineSaltCake, Materials.CausticRemainder, Materials.CalcinedResidue, Materials.OlivineLeachCake })
            check(Materials.IsTerminal(id) && Materials.ById(id)!.Price == Materials.TerminalPrice, "Terminal at the technical minimum: " + id);
        check(!Materials.IsTerminal(Materials.PhosphateConcentrate) && !Materials.IsTerminal(Materials.LeachedResidue), "Intermediates are feed, not terminal");

        // Ports, records and the saved selection.
        var ports = new[] { LeachRules.WaterPort, LeachRules.AmmoniaPort, LeachRules.AcidPort, LeachRules.NutrientPort, LeachRules.VesselPort, AcidPlantRules.AcidPort, AcidPlantRules.VesselPort, RefineryRules.OutPort, RefineryRules.VesselPort, RefineryRules.GasInPort, ProcessorRules.WaterInPort };
        check(ports.Distinct().Count() == ports.Length && ports.All(p => p.StartsWith("PhobosManufacturing.")), "The LC-3's ports are its own and namespaced");
        check(LeachRules.Record != RefineryRules.Record, "The LC-3 keeps its own record");
        var state = new ChargeState(true) { Selected = 2, RecipeId = "struvite", Revision = 2, ProgressSeconds = 12, Charge = new List<string> { "c-1" } };
        var read = ChargeState.Read(state.Save(), true);
        check(read.Selected == 2 && read.Revision == 2 && read.Charge.Single() == "c-1" && state.Save().ContainsKey("selected"), "The LC-3 record keeps the selection");
        read.Clear();
        check(read.Selected == 2 && !read.Bound, "Clearing a charge keeps the selection for the next");
        throws(() => RefineryState.Read(state.Save()), "A V4 record never carries a selection");
    }
}
