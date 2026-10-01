using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

/// <summary>The T2 thaw unit rules: the immutable thaw recipe and its mass balance, the authored energy budget, exact
/// ice admission and vessel adjacency. The silos it fills are Framework's water tanks since Shipbreaker 0.54.0.</summary>
internal static class SiloChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var recipe = ThawRules.Recipes.Current;
        check(recipe.InputKg == 24.7 && recipe.Revision == 1, "One block of the game's water ice, 24.7 kg, per batch");
        check(recipe.Products.Single(p => p.Id == ThawRules.Commodity).Kg == 22.7 && recipe.Products.Single(p => p.Id == ThawRules.Gangue).Kg == 2 &&
            ProcessRules.Balanced(recipe.InputKg, recipe.Products.Select(p => p.Kg * p.Count)), "Water plus native gangue conserve the whole block");
        check(ThawRules.Commodity == "water", "The thaw unit delivers the commodity every water tank holds");
        check(!recipe.Products.Any(p => p.Id == ThawRules.Ice), "No output can be rerolled as feed");
        check(Math.Abs(ThawRules.ThermalNeedKWh - 24.7 * 525 / 3600) < 1e-9 && ThawRules.ThermalNeedKWh < ThawRules.CycleEnergyKWh && ThawRules.CycleEnergyKWh == 4,
            "The 6 kW, 40 minute cycle covers the authored 3.6 kWh thermal need with a margin for the drive and the room");
        check(ThawRules.FusionKJPerKg < ThawRules.MeltKJPerKg, "The authored need exceeds the enthalpy of fusion alone (warming allowance)");
        check(Math.Abs(ThawRules.RoomHeatKW(true) - .9) < 1e-9 && ThawRules.RoomHeatKW(false) == ThawRules.IdleKW, "Fifteen percent of working power warms the room; the idle draw is all heat");
        check(ReclaimerRules.CoolingBudget(10000, 290, 0, 100, ThawRules.RoomHeatKW(true), 60, out double rise) && rise > 0, "Thaw heat uses the same room budget rule as the R4");
        check(!ReclaimerRules.CoolingBudget(0, 290, 0, 0, ThawRules.RoomHeatKW(true), 60, out _), "Vacuum is not free cooling for the thaw unit either");

        check(ThawRules.ValidIce("ItmIce01", 24.7, true, true, true), "Exact water ice is accepted");
        check(ThawRules.ValidIce("ItmIce02", 24.84, true, true, true) && !ThawRules.ValidIce("ItmIce02", 24.7, true, true, true), "Methane ice is accepted at its own mass only");

        // Methane ice as methane clathrate, CH4.6H2O (Circone et al. 2005): mass and energy.
        var clathrate = ThawRules.MethaneRecipes.Current;
        check(clathrate.InputKg == 24.84 && ProcessRules.Balanced(clathrate.InputKg, clathrate.Products.Select(p => p.Kg * p.Count)) &&
            clathrate.Products.Single(p => p.Id == ThawRules.Gangue).Kg == ThawRules.GangueKg, "Methane ice: water, methane and the same native gangue conserve the whole block");
        double hydrateKg = ThawRules.MethaneIceKg - ThawRules.GangueKg, mol = hydrateKg / (ThawRules.MethaneMolarKg + 6 * ThawRules.WaterMolarKg);
        check(Math.Abs(mol * ThawRules.MethaneMolarKg - ThawRules.MethaneKg) < .005 && Math.Abs(mol * 6 * ThawRules.WaterMolarKg - ThawRules.ClathrateWaterKg) < .005,
            "Methane and water follow n = 6 on 22.84 kg of hydrate, rounded to 0.01 kg");
        double idealFraction = ThawRules.MethaneMolarKg / (ThawRules.MethaneMolarKg + 5.75 * ThawRules.WaterMolarKg);
        check(Math.Abs(idealFraction - .1341) < .0005 && ThawRules.MethaneKg < hydrateKg * idealFraction, "Methane stays below the full-occupancy ceiling (13.4 wt%)");
        check(Math.Abs(ThawRules.ClathrateThermalNeedKWh - 4.09) < .01 && ThawRules.ClathrateThermalNeedKWh < ThawRules.WorkingKW * ThawRules.MethaneCycleSeconds / 3600 * (1 - ThawRules.RoomHeatFraction),
            "The 50 minute methane cycle delivers 4.25 kWh to the block after the room's share, above the 4.09 kWh dissociation and warming need");
        check(!clathrate.Products.Any(p => ThawRules.IsFeed(p.Id)) && ThawRules.RecipesFor("ItmIce02") == ThawRules.MethaneRecipes && ThawRules.RecipesFor("ItmIce01") == ThawRules.Recipes &&
            ThawRules.CycleSecondsFor("ItmIce02") == 3000 && ThawRules.CycleSecondsFor("ItmIce01") == ThawRules.CycleSeconds, "Each feed identity reads its own recipe catalog and cycle");
        check(ThawRules.MethaneOutPort != ThawRules.OutPort && ThawRules.MethaneInPort != ThawRules.VesselPort, "Methane has its own port pair, apart from water's");
        // Refining as a business (1 October 2026): the re-priced 100 cr block yields water and methane worth about twice it,
        // at the station water price and the game's 2.2 cr/kg methane (the native checks use the live price).
        double thawed = ThawRules.ClathrateWaterKg * Phobos.Ostranauts.Framework.Items.WaterTanks.WaterPricePerKg + ThawRules.MethaneKg * 2.2;
        check(thawed >= 1.5 * ThawRules.MethaneIcePrice && thawed <= 2.5 * ThawRules.MethaneIcePrice, "Thawing a methane ice block earns 1.5 to 2.5 times the re-priced block");
        check(!ThawRules.ValidIce("ItmIceTrash01", 2, true, true, true), "Ice gangue is not feed");
        check(!ThawRules.ValidIce("ItmIce01", 24, true, true, true) && !ThawRules.ValidIce("ItmIce01", 24.7, false, true, true) &&
            !ThawRules.ValidIce("ItmIce01", 24.7, true, false, true) && !ThawRules.ValidIce("ItmIce01", 24.7, true, true, false), "Wrong mass, installed, loaded or stacked blocks are refused");

        // Adjacency: within one tile of the vessel on the longer axis, never overlapping. Tile coordinates.
        check(ThawRules.Adjacent(0, 0, 2.5, 0, 3) && ThawRules.Adjacent(0, 0, 3.5, 0, 3) && ThawRules.Adjacent(0, 0, -2.5, 1, 3) && ThawRules.Adjacent(0, 0, 2.5, 2.5, 3),
            "A 3 x 3 silo touching the 2 x 2 unit, one tile away or diagonal is within reach");
        check(!ThawRules.Adjacent(0, 0, 4.5, 0, 3) && !ThawRules.Adjacent(0, 0, 0, 4.5, 3), "Two tiles away is out of reach");
        check(!ThawRules.Adjacent(0, 0, 1, 0, 3) && !ThawRules.Adjacent(0, 0, 0, 0, 3), "Overlapping placements cannot pair");
        check(ThawRules.Adjacent(10, 10, 12, 10, 2) && !ThawRules.Adjacent(10, 10, 14, 10, 2), "A 2 x 2 vessel reaches from touching to one tile away");
        check(!ThawRules.Adjacent(double.NaN, 0, 2.5, 0, 3) && !ThawRules.Adjacent(0, 0, 2.5, 0, 0), "Invalid geometry cannot pair");

        // The S3 to S5 silos (and the new S2) are Framework's water tanks since Shipbreaker 0.54.0; Framework's own checks
        // cover their ladder, amounts and records.

        var job = ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 1200, 1, 2400, 2400);
        check(job.Progress == 1200 && !job.Complete, "A saved half-thawed block resumes where it was");
        job.Advance("ice-1", 600, true, true); job.Advance("ice-1", 600, false, true);
        check(job.Progress == 1800, "Unpowered time thaws nothing");
        throws(() => ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 0, 2, 2400, 2400), "Unknown thaw revision rejected");
        throws(() => ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 100, 1, 0, 2400), "A thaw job without a saved duration is not guessed");
        check(IndustrialRules.Group("PhobosProcessSiloInstalled") == "" && IndustrialRules.Group(ThawRules.Installed + "Dmg") == "thaw" && IndustrialRules.Equipment(ThawRules.Installed),
            "Thaw units are industrial equipment with their own console group; the silos are Framework's tanks now");
        // Shipbreaker 0.63.0 (owner decision, 1 October 2026): the T2 and the ML-2 open Framework's shared Control Panel;
        // the machines with routing, furnace or capture pages keep the industrial panel.
        check(new[] { ThawRules.Installed, ThawRules.Installed + "Dmg", LaserRules.Installed, LaserRules.Installed + "Dmg" }.All(IndustrialRules.SharedPanel),
            "The thaw unit and the mining laser open the shared Control Panel, damaged or not");
        check(new[] { "PhobosShipbreakerInstalled", ReclaimerRules.Installed, FurnaceRules.Prefix + "Installed", IntakeRules.Grabber + "Installed", CollectorRules.Installed,
                FurnaceRules.Radiator + "Installed", IndustrialRules.Prefix + "Installed", "PhobosProcessSiloInstalled", "", null! }.All(id => !IndustrialRules.SharedPanel(id)),
            "The D4, R4, F6, G4, C2, the radiator and the C1 keep the industrial panel");
        check(ThawRules.OutPort != ThawRules.VesselPort && ThawRules.OutPort.StartsWith("PhobosShipbreaker.", StringComparison.Ordinal), "The thaw outlet and the vessel inlet are distinct namespaced ports");
    }
}
