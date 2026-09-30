using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

/// <summary>The S3 silo and T2 thaw unit rules: the immutable thaw recipe and its mass balance, the authored
/// energy budget, exact ice admission, vessel adjacency and whole-kilogram silo amounts.</summary>
internal static class SiloChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var recipe = ThawRules.Recipes.Current;
        check(recipe.InputKg == 24.7 && recipe.Revision == 1, "One block of the game's water ice, 24.7 kg, per batch");
        check(recipe.Products.Single(p => p.Id == ThawRules.Commodity).Kg == 22.7 && recipe.Products.Single(p => p.Id == ThawRules.Gangue).Kg == 2 &&
            ProcessRules.Balanced(recipe.InputKg, recipe.Products.Select(p => p.Kg * p.Count)), "Water plus native gangue conserve the whole block");
        check(ThawRules.Commodity == SiloRules.Commodity && SiloRules.Commodity == "water", "The thaw unit delivers the commodity the silo and the R3 reservoir hold");
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
        // Refining value (30 September 2026): the game's 20 cr block yields water and methane worth more at station prices; nothing sells them back.
        check(ThawRules.ClathrateWaterKg * SiloRules.WaterPricePerKg + ThawRules.MethaneKg * 2.2 > 20, "Thawing methane ice gains water and methane aboard against the game's own 20 cr block");
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

        check(SiloRules.ValidAmount(0) && SiloRules.ValidAmount(1000) && SiloRules.ValidAmount(250), "Whole kilograms within capacity are valid silo amounts");
        check(!SiloRules.ValidAmount(1000.5) && !SiloRules.ValidAmount(1001) && !SiloRules.ValidAmount(-1) && !SiloRules.ValidAmount(double.NaN), "Fractions, overflow and invalid amounts are refused");
        check(SiloRules.CapacityKg == 1000 && SiloRules.DryKg == 240 && SiloRules.Footprint == 3, "S3: 1,000 kg of water in a 240 kg 3 x 3 housing");
        check(SiloRules.ReserveChoices.All(SiloRules.ValidAmount) && SiloRules.TransferChoices.All(SiloRules.ValidAmount), "Every panel choice is a valid amount");
        check(SiloRules.PurchaseStepKg * SiloRules.PurchaseSteps == SiloRules.CapacityKg, "One station quote can fill an empty silo");
        // The S3, S4 and S5 ladder: the S3 unchanged, one tile wider per step, more water for less per kilogram.
        var sizes = SiloRules.Sizes;
        check(sizes.Count == 3 && sizes[0].Prefix == SiloRules.Prefix && sizes[0].Record == SiloRules.Record && sizes[0].CapacityKg == 1000 && sizes[0].DryKg == 240 && sizes[0].Price == 4800,
            "The S3 keeps its identity, record and ratings");
        check(sizes[1].Prefix == "PhobosProcessSiloMedium" && sizes[1].Footprint == 4 && sizes[1].CapacityKg == 1960 && sizes[1].DryKg == 365 &&
              sizes[2].Prefix == "PhobosProcessSiloLarge" && sizes[2].Footprint == 5 && sizes[2].CapacityKg == 3330 && sizes[2].DryKg == 465, "S4 holds 1,960 kg and S5 3,330 kg");
        check(sizes[1].CapacityKg / sizes[1].Price > sizes[0].CapacityKg / sizes[0].Price && sizes[2].CapacityKg / sizes[2].Price > sizes[1].CapacityKg / sizes[1].Price, "Bigger silos cost less per kilogram held");
        check(sizes.Select(s => s.Record).Concat(sizes.Select(s => s.Journal)).Concat(sizes.Select(s => s.Guard)).Distinct().Count() == 9, "Every size keeps its own records");
        check(SiloRules.IsFamily("PhobosProcessSiloLargeInstalledDmg") && SiloRules.For("PhobosProcessSiloMediumLoose") == sizes[1] && SiloRules.For("PhobosIceThawInstalled") == null,
            "Every form resolves to its own size; other machines do not");
        check(SiloRules.ReserveChoicesFor(1000).SequenceEqual(SiloRules.ReserveChoices) && sizes.All(s => SiloRules.ReserveChoicesFor(s.CapacityKg).All(k => SiloRules.ValidAmount(k, s.CapacityKg))),
            "Reserve steps scale with the silo and stay valid amounts; the S3's are unchanged");
        check(SiloRules.ValidAmount(3000, 3330) && !SiloRules.ValidAmount(3000, 1000), "Amounts are checked against the chosen silo's own capacity");

        var job = ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 1200, 1, 2400, 2400);
        check(job.Progress == 1200 && !job.Complete, "A saved half-thawed block resumes where it was");
        job.Advance("ice-1", 600, true, true); job.Advance("ice-1", 600, false, true);
        check(job.Progress == 1800, "Unpowered time thaws nothing");
        throws(() => ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 0, 2, 2400, 2400), "Unknown thaw revision rejected");
        throws(() => ProcessJob.CreateOrResume(ThawRules.Recipes, "ice-1", 100, 1, 0, 2400), "A thaw job without a saved duration is not guessed");
        check(IndustrialRules.Group(SiloRules.Installed) == "silo" && IndustrialRules.Group(ThawRules.Installed + "Dmg") == "thaw" &&
            IndustrialRules.Equipment(SiloRules.Installed) && IndustrialRules.Equipment(ThawRules.Installed), "Silos and thaw units are industrial equipment with their own console groups");
        check(ThawRules.OutPort != ThawRules.VesselPort && ThawRules.OutPort.StartsWith("PhobosShipbreaker.", StringComparison.Ordinal), "The thaw outlet and the vessel inlet are distinct namespaced ports");
    }
}
