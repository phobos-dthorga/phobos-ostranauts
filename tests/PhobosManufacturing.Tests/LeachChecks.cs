using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using PhobosManufacturing.Core;

/// <summary>The Lixivar LC-3 (Manufacturing 0.18.0): its shape, the three recipes against their reactions (IUPAC 2013
/// molar masses), one terminal remainder per feed, the settlement each recipe asks of its links, feed admission by the
/// selected recipe, prices under the refining guardrails, and its saved record with the selection.</summary>
internal static class LeachChecks
{
    // Molar masses, kg/mol (IUPAC 2013 conventional atomic weights).
    private const double KCl = 0.074551, Na2SO4 = 0.142036, K2SO4 = 0.174252, NaCl = 0.05844, NaMgPO4 = 0.142265, NH3 = 0.017031, H2O = 0.018015,
        Struvite = 0.245404, NaOH = 0.039997;
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
        check(LeachRecipes.All.Select(r => r.Revision).OrderBy(r => r).SequenceEqual(new[] { 1, 2, 3 }), "Three revisions of its own, 1 to 3");
        check(LeachRecipes.Available(false).Count() == 2 && LeachRecipes.Available(true).Count() == 3 && !LeachRecipes.Available(false).Contains(LeachRecipes.Makeup),
            "Without Agriculture the makeup formulation is unavailable");

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

        // Feed admission follows the selected recipe.
        check(LeachRecipes.FeedKg(Materials.EvaporiteCrust, leach, false) == 10 && LeachRecipes.FeedKg(Materials.PotassiumSulfate, leach, true) == null &&
              LeachRecipes.FeedKg(Materials.PotassiumSulfate, mk, true) == .7 && LeachRecipes.FeedKg(Materials.PotassiumSulfate, mk, false) == null && LeachRecipes.FeedKg("ItmMineral11", leach, true) == null,
            "Only the selected recipe's feed enters, and the formulation's only with Agriculture");
        check(LeachRules.StockFeed.All(id => LeachRecipes.All.SelectMany(r => r.ItemInputs).Any(i => i.Id == id)) && LeachRecipes.All.SelectMany(r => r.ItemInputs).All(i => LeachRules.StockFeed.Contains(i.Id)),
            "The game-level feed rule names exactly the LC-3's feed identities");

        // Materials and prices: the salts lose value against the crust, struvite stays within half again its inputs.
        check(Materials.ById(Materials.EvaporiteCrust)!.Mined && Materials.ById(Materials.EvaporiteCrust)!.Price == 150, "The crust is mined and sells like the salt crust");
        check(leach.Solids(leach.Products).Sum(p => p.Count * Materials.ById(p.Id)!.Price) < Materials.ById(Materials.EvaporiteCrust)!.Price, "Leaching a crust loses value, as the salt crust does for its nitrogen");
        check(Materials.ById(Materials.Struvite)!.Price <= 1.5 * Materials.ById(Materials.PhosphateConcentrate)!.Price, "Struvite stays within half again its concentrate, before the reagents' value");
        foreach (string id in new[] { Materials.BrineSaltCake, Materials.CausticRemainder, Materials.CalcinedResidue })
            check(Materials.IsTerminal(id) && Materials.ById(id)!.Price == Materials.TerminalPrice, "Terminal at the technical minimum: " + id);
        check(!Materials.IsTerminal(Materials.PhosphateConcentrate) && !Materials.IsTerminal(Materials.LeachedResidue), "Intermediates are feed, not terminal");

        // Ports, records and the saved selection.
        var ports = new[] { LeachRules.WaterPort, LeachRules.AmmoniaPort, LeachRules.VesselPort, RefineryRules.OutPort, RefineryRules.VesselPort, RefineryRules.GasInPort, ProcessorRules.WaterInPort };
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
