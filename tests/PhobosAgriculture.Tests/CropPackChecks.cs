using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Data;
using PhobosAgriculture.Core;

/// <summary>Agriculture 0.40.0: crops and cooker recipes are data packs. The shipped packs must give exactly the
/// figures the code held until then, records saved before must still load, and the rules a player file is held to
/// (mass balance, known items, frozen crops) must refuse what they should.</summary>
internal static class CropPackChecks
{
    internal static void Run(Action<bool, string> check)
    {
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch (ArgumentException) { failed = true; } catch (FormatException) { failed = true; } catch (InvalidOperationException) { failed = true; } check(failed, label); }

        // The three crops, in the order of their planting actions, with the figures the code held before 0.40.0.
        check(Crops.All.Select(c => c.Id).SequenceEqual(new[] { "potato", "lettuce", "lettuce-seed", "wheat", "tomato", "soybean", "flax", "sugar-beet" }), "The shipped crops keep their order, new ones after");
        var historic = new Dictionary<string, double[]>
        {
            ["potato"] = new[] { 96, .75, .2, 5, .84, .04, 4.624, .2, .04 },
            ["lettuce"] = new[] { 48, .4, .005, 1.2, .0605, .005, 1.2658, .1, .0045 },
            ["lettuce-seed"] = new[] { 96, .4, .005, 1.2, .0725, .01, 1.356, .2, .0045 }
        };
        foreach (var pair in historic)
        {
            var c = Crop.Get(pair.Key);
            check(new[] { c.Hours, c.KW, c.Seed, c.Final, c.Carbon, c.Nutrient, c.Water, c.Vapour, c.SeedCarbon }.SequenceEqual(pair.Value), "The pack holds the historic budget exactly: " + pair.Key);
        }
        check(Crop.Get("potato").EdibleKg / Crop.Get("potato").Final == 4.2 / 5 && Crop.Get("lettuce").EdibleKg / Crop.Get("lettuce").Final == 1 / 1.2 &&
              Crop.Get("lettuce-seed").EdibleKg / Crop.Get("lettuce-seed").Final == .02 / 1.2, "Edible shares are the historic fractions, bit for bit");
        Reject(() => Crop.Get("rye"), "An unknown crop is refused, as before");

        // A full healthy harvest of each, as the code gave it before.
        (double Seed, int Portions, double Portion, double Residue) Full(string id)
        {
            var c = Crop.Get(id); var s = new CropState { CropId = id, Cohort = new string('a', 32), Progress = 1, Biomass = c.Final, Carbon = c.SeedCarbon + c.Carbon, RecoveryRevision = 1 };
            var h = s.Harvest(); return (h.SeedKg, h.Portions, h.PortionKg, h.ResidueKg);
        }
        var potato = Full("potato"); var lettuce = Full("lettuce"); var seed = Full("lettuce-seed");
        check(potato.Seed == .2 && potato.Portions == 10 && potato.Portion == .4 && Math.Abs(potato.Residue - .8) < 1e-9, "A potato harvest keeps one seed potato and gives ten portions and 0.8 kg of residue");
        check(lettuce.Seed == 0 && lettuce.Portions == 4 && lettuce.Portion == .25 && Math.Abs(lettuce.Residue - .2) < 1e-9, "A lettuce harvest gives four portions and 0.2 kg of residue");
        check(seed.Seed == 0 && seed.Portions == 4 && seed.Portion == .005 && Math.Abs(seed.Residue - 1.18) < 1e-9, "A seed harvest gives four packets and 1.18 kg of residue");
        check(Crop.Get("potato").Stock == "PhobosVerdemorrowContinuancePotato" && Crop.Get("potato").Produce == "PhobosVerdemorrowRawPotatoes" &&
              Crop.Get("lettuce").Stock == "PhobosVerdemorrowContinuanceLettuce" && Crop.Get("lettuce").Produce == "PhobosVerdemorrowLettuce" &&
              Crop.Get("lettuce-seed").Produce == Crop.Get("lettuce-seed").Stock, "Each crop is planted from and harvested into the items it always was");

        // Feeds: the saved names and conduit names, and each feed as its crop's own water and nutrient.
        check(Crops.All.Take(3).Select(c => c.Feed).SequenceEqual(new[] { "potato-v1", "lettuce-v1", "lettuce-seed-v1" }) &&
              Crops.All.Take(3).Select(c => c.FeedCommodity).SequenceEqual(new[] { "potato feed", "lettuce feed", "lettuce seed feed" }), "Feed and conduit names are the saved ones");
        check(NutrientSolution.CropId("water") == "" && NutrientSolution.CropId("potato-v1") == "potato" && NutrientSolution.Ratio("potato-v1").CarrierKg == 4.624 && NutrientSolution.Ratio("potato-v1").SoluteKg == .04 &&
              NutrientSolution.Ratio("lettuce-v1").CarrierKg == 1.2658 && NutrientSolution.Ratio("lettuce-v1").SoluteKg == .005, "A feed is its crop's water and nutrient budget");
        Reject(() => NutrientSolution.CropId("rye-v1"), "An unknown feed is refused, as before");
        check(CropAppearance.PlantKey(new CropState { CropId = "lettuce-seed", Cohort = new string('a', 32), Biomass = .005, Carbon = .0045 }) == "LettuceSeed-sprout", "Artwork keys come from the pack");

        // Records saved before 0.40.0 load unchanged: the three cohorts, and a potato half cooked.
        Dictionary<string, string> Saved(string crop, string biomass, string carbon, string cookerInput = "none", string cooker = "0") => new()
        {
            ["crop"] = crop, ["cohort"] = crop == "empty" ? "none" : new string('b', 32), ["cookerInput"] = cookerInput, ["progress"] = crop == "empty" ? "0" : "0.5", ["health"] = "1", ["water"] = "3.25",
            ["nutrients"] = "0.02", ["biomass"] = biomass, ["carbon"] = carbon, ["pace"] = "1", ["dark"] = "0", ["cooker"] = cooker, ["recoveryRevision"] = crop == "empty" ? "0" : "1"
        };
        foreach (var (crop, biomass, carbon) in new[] { ("potato", "2.6", "0.46"), ("lettuce", "0.6025", "0.03475"), ("lettuce-seed", "0.6025", "0.04075") })
        {
            var read = CropState.Read(Saved(crop, biomass, carbon));
            check(read.CropId == crop && read.Save()["biomass"] == biomass && read.Save()["crop"] == crop, "A planting saved before the crops pack loads and saves unchanged: " + crop);
        }
        var cooking = CropState.Read(Saved("empty", "0", "0", "raw-potato-item-id", "0.02"));
        check(cooking.CookerInput == "raw-potato-item-id" && cooking.CookerProgress == .02, "A half-cooked portion saved before the recipes pack loads unchanged");
        Reject(() => CropState.Read(Saved("empty", "0", "0", "raw-potato-item-id", "0.6")), "Cooking progress beyond any recipe's energy is still refused");

        // The cooker: the potato recipe with the energy the code held, and (0.41.0) flatbread from grain and a water ration.
        var hearth = HearthRecipes.ForInput("PhobosVerdemorrowRawPotatoes");
        check(hearth != null && hearth.Revision == 1 && hearth.Products.SequenceEqual(new[] { ("PhobosVerdemorrowHearthPotatoes", .4) }) && hearth.Extra == null && hearth.Kg == .4 && hearth.KWh == .05 && HearthRecipes.CookerKW == 2,
            "The Hearth-2 cooks a 0.4 kg potato portion with 0.05 kWh at 2 kW, as before");
        var bread = HearthRecipes.ForInput("PhobosVerdemorrowWheatGrain");
        check(bread != null && bread.Revision == 2 && bread.Kg == .4 && bread.Extra == ("LiquidWater", .25) && bread.Products.SequenceEqual(new[] { ("PhobosVerdemorrowHearthFlatbread", .65) }) &&
              Math.Abs(bread.KWh - 1d / 3) < 1e-12, "Flatbread bakes 0.4 kg of grain and one water ration into a 0.65 kg loaf over ten minutes");
        var stew = HearthRecipes.ForInput("PhobosVerdemorrowSoybeans");
        check(stew != null && stew.Revision == 3 && stew.Extra == ("LiquidWater", .25) && stew.Products.SequenceEqual(new[] { ("PhobosVerdemorrowHearthSoybeans", .5) }) && stew.KWh == .5 && HearthRecipes.MaxKWh == .5,
            "Soybean stew cooks 0.25 kg of beans and one water ration into a 0.5 kg bowl over fifteen minutes, the longest recipe");
        check(HearthRecipes.IsInput("PhobosVerdemorrowRawPotatoes") && HearthRecipes.IsProduct("PhobosVerdemorrowHearthPotatoes") && !HearthRecipes.IsInput("PhobosVerdemorrowLettuce") &&
              HearthRecipes.IsExtra("LiquidWater") && !HearthRecipes.IsInput("LiquidWater"), "Only what a recipe names is cookable; water is a supply, never a portion");
        check(HearthRecipes.All.All(r => Math.Abs(r.Kg + (r.Extra?.Kg ?? 0) - r.Products.Sum(p => p.Kg)) < 1e-12), "Every cooking recipe conserves mass");

        // Wheat (0.41.0): a full healthy harvest keeps one packet of seed wheat, gives one portion of grain, and leaves the straw.
        var wheat = Full("wheat");
        check(wheat.Seed == .05 && wheat.Portions == 1 && wheat.Portion == .4 && Math.Abs(wheat.Residue - .95) < 1e-9, "A wheat harvest keeps a seed packet, gives one 0.4 kg portion of grain and 0.95 kg of straw");
        var w = Crop.Get("wheat");
        check(w.Stock == "PhobosVerdemorrowContinuanceWheat" && w.Produce == "PhobosVerdemorrowWheatGrain" && w.Feed == "wheat-v1" && w.Art == "Wheat" && w.KW > Crop.Get("potato").KW && w.Hours < Crop.Get("potato").Hours,
            "Wheat takes more light and a slightly shorter cycle than potato, in the source ratios");
        check((Crop.Get("wheat").EdibleKg / Crop.Get("wheat").Final) < .4 && (Crop.Get("potato").EdibleKg / Crop.Get("potato").Final) > .8, "Wheat leaves far more of the plant as residue than potato");

        // Soybean (0.42.0): one portion of dry beans, the packet back, the rest straw.
        var soy = Full("soybean");
        check(soy.Seed == .03 && soy.Portions == 1 && soy.Portion == .25 && Math.Abs(soy.Residue - .62) < 1e-9, "A soybean harvest keeps a seed packet, gives one 0.25 kg portion of beans and 0.62 kg of straw");
        // Flax (0.45.0): four 0.25 kg straw bundles for the B2, the packet back, the rest residue; scutching conserves mass.
        var fx = Full("flax");
        check(fx.Seed == .01 && fx.Portions == 4 && fx.Portion == .25 && Math.Abs(fx.Residue - .59) < 1e-9 && Crop.Get("flax").Produce == FlaxScutching.Straw,
            "A flax harvest keeps a seed packet, gives four 0.25 kg straw bundles and 0.59 kg of residue");
        check(Math.Abs(FlaxScutching.ClothCount * FlaxScutching.ClothKg + FlaxScutching.ShivesKg - FlaxScutching.StrawKg) < 1e-12 &&
              FlaxScutching.ShivesMineralsKg + FlaxScutching.ShivesOrganicKg < FlaxScutching.ShivesKg && Math.Abs(FlaxScutching.ClothCount * FlaxScutching.ClothKg / FlaxScutching.StrawKg - .2) < 1e-12,
            "Scutching one bundle gives two 25 g clean cloth (20% of the straw) and shives with the rest of its mass");
        // Sugar beet (0.46.0): nine 0.5 kg roots and the packet back; B2 sugar extraction conserves each root's mass.
        var sb = Full("sugar-beet");
        check(sb.Seed == .02 && sb.Portions == 9 && sb.Portion == .5 && Math.Abs(sb.Residue - 1.48) < 1e-9 && Crop.Get("sugar-beet").Produce == SugarExtraction.Beet,
            "A sugar beet harvest keeps a seed packet, gives nine 0.5 kg roots and 1.48 kg of leaves and crowns");
        foreach (var c in BenchConversions.All)
            check(Math.Abs(c.ProductCount * c.ProductKg + c.ResidueKg - c.InputKg) < 1e-12 && c.ResidueMineralsKg + c.ResidueOrganicKg <= c.ResidueKg + 1e-12 &&
                  BenchConversions.ForMode(c.Mode) == c && BenchConversions.ForAction(c.Action) == c, "A bench conversion conserves its input's mass: " + c.Mode);
        check(Math.Abs(SugarExtraction.SugarKg / SugarExtraction.BeetSucroseKg - .8235) < 1e-3 && SugarExtraction.BeetSucroseKg / SugarExtraction.BeetKg >= .15 &&
              SugarExtraction.BeetSucroseKg / SugarExtraction.BeetKg <= .2 && SugarExtraction.BeetWaterKg / SugarExtraction.BeetKg >= .75,
            "A beet is 75% water and 17% sucrose, of which the bench crystallises 82%, within the cited ranges");
        check(Crops.All.All(c => c.Picks == 0 || c.Id == "tomato") && Crop.Get("tomato").Picks == 3 && Crop.Get("tomato").PickKg == .75, "Only tomato is picked repeatedly: three picks of up to 0.75 kg");

        // Tomato (0.42.0): picking takes whole portions from a ripe plant, leaves it growing and sets growth back by
        // exactly the share of a cycle it took; regrowing it restores the plant, so mass is conserved across picks.
        var t = Crop.Get("tomato"); double grownKg = t.Final - t.Seed;
        var vine = new CropState { CropId = "tomato", Cohort = new string('c', 32), Progress = 1, Biomass = t.Final, Carbon = t.SeedCarbon + t.Carbon, RecoveryRevision = 1, Water = 20, Nutrients = .5, Running = true };
        check(vine.PickPortions() == 3, "A ripe tomato plant gives three portions a pick");
        double before = vine.Biomass, carbonBefore = vine.Carbon;
        vine.Pick(3);
        check(Math.Abs(before - vine.Biomass - .75) < 1e-12 && Math.Abs(vine.Progress - (1 - .75 / grownKg)) < 1e-12 && vine.Picks == 1 && !vine.Ready && vine.PickPortions() == 0,
            "A pick removes exactly the picked fruit and sets growth back by the share of a cycle it was");
        check(Math.Abs(carbonBefore - vine.Carbon - .75 * t.Carbon / grownKg) < 1e-12, "The fruit takes its share of the plant's carbon with it");
        var saved = vine.Save();
        check(saved["picks"] == "1" && CropState.Read(saved).Picks == 1, "Picks taken are saved and read back");
        // Regrow in hour steps with ample light, CO2 and oxygen: the plant returns to its picked-from state.
        double energy = 0;
        for (int hour = 0; hour < 40 && !vine.Ready; hour++) { double h = Math.Min(1, (1 - vine.Progress) * t.Hours); vine.Step(h, h * t.KW, 10, 10, true); energy += h * t.KW; }
        check(vine.Ready && Math.Abs(vine.Biomass - t.Final) < 1e-9 && Math.Abs(vine.Carbon - (t.SeedCarbon + t.Carbon)) < 1e-9 && Math.Abs(energy / t.KW - .75 / grownKg * t.Hours) < 1e-9,
            "Regrowing the picked fruit takes the same share of a cycle and restores the plant");
        vine.Pick(3); vine.Progress = 1; vine.Biomass = t.Final; vine.Pick(3); vine.Progress = 1; vine.Biomass = t.Final;
        check(vine.Picks == 3 && vine.PickPortions() == 0, "After its three picks the plant gives no more; only harvest is left");
        Reject(() => vine.Pick(1), "A spent plant cannot be picked");
        var last = vine.Harvest();
        check(last.SeedKg == .005 && last.Portions == 14 && Math.Abs(last.ResidueKg - (t.Final - .005 - 14 * .25)) < 1e-9, "The final harvest takes the rest of the fruit, a seed packet and the vine");
        var unpicked = Saved("tomato", "2.5", "0.26");
        check(!unpicked.ContainsKey("picks") && CropState.Read(unpicked).Picks == 0, "A planting saved with no picks reads as none");
        var overPicked = Saved("tomato", "2.5", "0.26"); overPicked["picks"] = "4";
        Reject(() => CropState.Read(overPicked), "More picks than the crop allows are refused");
        var pickedPotato = Saved("potato", "2.6", "0.46"); pickedPotato["picks"] = "1";
        Reject(() => CropState.Read(pickedPotato), "A crop harvested once cannot have picks");

        // Agriculture 0.43.0: room carbon dioxide speeds growth per hour and per kWh, with the same budget per unit of growth.
        check(Co2Response.Factor(0) == 1 && Co2Response.Factor(.04) == 1 && Math.Abs(Co2Response.Factor(.1) - 1.2) < 1e-12 && Math.Abs(Co2Response.Factor(.125) - 1.225) < 1e-12 &&
              Math.Abs(Co2Response.Factor(.15) - 1.25) < 1e-12 && Co2Response.Factor(.5) == 1 && Math.Abs(Co2Response.Factor(5) - .85) < 1e-12 && Co2Response.Factor(double.NaN) == 1,
            "The curve: no change at ambient, fastest near 0.15 kPa, back to the historic rate by 0.5 kPa and slower above");
        (double Hours, double KWh, CropState State) GrowAt(string id, double factor)
        {
            var c = Crop.Get(id); var st = new CropState { Water = 20, Nutrients = .5 }; st.Plant(c, 1); double hours = 0, kwh = 0;
            for (int i = 0; i < 400 && !st.Ready; i++) { double h = Math.Min(1, (1 - st.Progress) * c.Hours / factor); st.Step(h, h * c.KW, 10, 10, true, null, factor); hours += h; kwh += h * c.KW; }
            return (hours, kwh, st);
        }
        var plain = GrowAt("wheat", 1); var rich = GrowAt("wheat", 1.25);
        check(plain.State.Ready && rich.State.Ready && Math.Abs(plain.Hours - 84) < 1e-9 && Math.Abs(rich.Hours - 84 / 1.25) < 1e-9 && Math.Abs(rich.KWh - plain.KWh / 1.25) < 1e-9,
            "At 0.15 kPa wheat ripens in 1/1.25 of the time on 1/1.25 of the energy");
        check(Math.Abs(rich.State.Biomass - plain.State.Biomass) < 1e-9 && Math.Abs(rich.State.Carbon - plain.State.Carbon) < 1e-9 && Math.Abs(rich.State.Water - plain.State.Water) < 1e-9 &&
              Math.Abs(rich.State.Nutrients - plain.State.Nutrients) < 1e-9, "Enrichment changes the time, not what the crop takes or gives");
        Reject(() => new CropState().Step(1, 1, 1, 1, true, null, 3), "A growth factor outside 0.5 to 2 is refused");

        // The rules every crops file is held to.
        string shipped = DataPacks.ShippedText(Crops.Source);
        var frozen = RecipeFreeze.Read(typeof(Crops).Assembly, Crops.FrozenResource);
        CropPack Try(Action<JObject> change)
        {
            var raw = DataPacks.Parse(shipped, CropSchema.Name, shipped: true); change(raw);
            return DataPacks.LoadText<CropPack>(raw.ToString(), "", "test", CropSchema.Name, (p, r) => { CropSchema.Validate(p, Crops.Known()); CropFreeze.Enforce(r, frozen); });
        }
        check(Try(_ => { }).crops.Count == 8, "The shipped crops file passes its own rules");
        check(frozen.revisions.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(new[] { "flax", "lettuce", "lettuce-seed", "potato", "soybean", "sugar-beet", "tomato", "wheat" }), "Every shipped crop is frozen");
        Reject(() => Try(r => r["crops"]!["potato"]!["hours"] = 48), "A published crop cannot be changed");
        Reject(() => Try(r => ((JObject)r["crops"]!).Remove("lettuce")), "A published crop cannot be removed");
        // A new crop beside the shipped ones, using the mod's own items: the way a player file adds one.
        JObject Fast() { var c = (JObject)DataPacks.Parse(shipped, CropSchema.Name, true)["crops"]!["lettuce"]!.DeepClone(); c["hours"] = 36; c["feed"] = "fast-lettuce-v1"; c["feedCommodity"] = "fast lettuce feed"; return c; }
        check(Try(r => r["crops"]!["fast-lettuce"] = Fast()).crops.Count == 9, "A new crop that balances and uses known items is accepted");
        Reject(() => Try(r => { var c = Fast(); c["picks"] = 2; r["crops"]!["fast-lettuce"] = c; }), "A picked crop needs a pick mass");
        Reject(() => Try(r => { var c = Fast(); c["picks"] = 2; c["pickKg"] = .1; r["crops"]!["fast-lettuce"] = c; }), "A pick takes at least one whole portion");
        var named = Fast(); named["name"] = "Quick lettuce";
        var added = new Crop("fast-lettuce", Try(r => r["crops"]!["fast-lettuce"] = named).crops["fast-lettuce"]);
        check(added.Name == "Quick lettuce" && Crop.Get("potato").Name == "Potatoes", "An added crop reads by its plain name; a shipped crop by the catalogue");
        Reject(() => Try(r => { var c = Fast(); c["waterKg"] = 2; r["crops"]!["fast-lettuce"] = c; }), "A crop that does not conserve mass is refused");
        Reject(() => Try(r => { var c = Fast(); c["feed"] = "lettuce-v1"; r["crops"]!["fast-lettuce"] = c; }), "Two crops cannot share a feed name");
        Reject(() => Try(r => { var c = Fast(); c["produce"] = "ItmScrapSteel"; r["crops"]!["fast-lettuce"] = c; }), "A crop cannot name an item the mod does not have");
        Reject(() => Try(r => { var c = Fast(); c["portionKg"] = .3; r["crops"]!["fast-lettuce"] = c; }), "A portion must weigh what its item weighs");
        Reject(() => Try(r => { var c = Fast(); c["edibleKg"] = 2; r["crops"]!["fast-lettuce"] = c; }), "A harvest cannot give more than it grows");
        Reject(() => Try(r => r["crops"]!["Fast Lettuce"] = Fast()), "A crop name is lower-case letters, digits and hyphens");
        Reject(() => Try(r => { var c = Fast(); c["art"] = "Rye"; r["crops"]!["fast-lettuce"] = c; }), "A crop borrows the growth artwork of a crop that ships");
        Reject(() => Try(r => { var c = Fast(); c["yield"] = 3; r["crops"]!["fast-lettuce"] = c; }), "An unknown field is refused");
        Reject(() => Try(r => r["items"]!["PhobosVerdemorrowLettuce"]!["hunger"] = 40), "Food values stay within bounds");
        // Checks above built packs from text; the live table is still the shipped one.
        check(Crops.All.Count == 8, "Checking a candidate file does not change the crops in use");
    }
}
