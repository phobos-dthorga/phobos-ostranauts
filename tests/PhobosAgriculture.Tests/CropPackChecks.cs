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
        void Reject(Action action, string label) { bool failed = false; try { action(); } catch (ArgumentException) { failed = true; } catch (FormatException) { failed = true; } check(failed, label); }

        // The three crops, in the order of their planting actions, with the figures the code held before 0.40.0.
        check(Crops.All.Select(c => c.Id).SequenceEqual(new[] { "potato", "lettuce", "lettuce-seed", "wheat" }), "The shipped crops keep their order, new ones after");
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
        Reject(() => CropState.Read(Saved("empty", "0", "0", "raw-potato-item-id", "0.4")), "Cooking progress beyond any recipe's energy is still refused");

        // The cooker: the potato recipe with the energy the code held, and (0.41.0) flatbread from grain and a water ration.
        var hearth = HearthRecipes.ForInput("PhobosVerdemorrowRawPotatoes");
        check(hearth != null && hearth.Revision == 1 && hearth.Products.SequenceEqual(new[] { ("PhobosVerdemorrowHearthPotatoes", .4) }) && hearth.Extra == null && hearth.Kg == .4 && hearth.KWh == .05 && HearthRecipes.CookerKW == 2,
            "The Hearth-2 cooks a 0.4 kg potato portion with 0.05 kWh at 2 kW, as before");
        var bread = HearthRecipes.ForInput("PhobosVerdemorrowWheatGrain");
        check(bread != null && bread.Revision == 2 && bread.Kg == .4 && bread.Extra == ("LiquidWater", .25) && bread.Products.SequenceEqual(new[] { ("PhobosVerdemorrowHearthFlatbread", .65) }) &&
              Math.Abs(bread.KWh - 1d / 3) < 1e-12 && HearthRecipes.MaxKWh == bread.KWh, "Flatbread bakes 0.4 kg of grain and one water ration into a 0.65 kg loaf over ten minutes");
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

        // The rules every crops file is held to.
        string shipped = DataPacks.ShippedText(Crops.Source);
        var frozen = RecipeFreeze.Read(typeof(Crops).Assembly, Crops.FrozenResource);
        CropPack Try(Action<JObject> change)
        {
            var raw = DataPacks.Parse(shipped, CropSchema.Name, shipped: true); change(raw);
            return DataPacks.LoadText<CropPack>(raw.ToString(), "", "test", CropSchema.Name, (p, r) => { CropSchema.Validate(p, Crops.Known()); CropFreeze.Enforce(r, frozen); });
        }
        check(Try(_ => { }).crops.Count == 4, "The shipped crops file passes its own rules");
        check(frozen.revisions.Keys.OrderBy(k => k, StringComparer.Ordinal).SequenceEqual(new[] { "lettuce", "lettuce-seed", "potato", "wheat" }), "Every shipped crop is frozen");
        Reject(() => Try(r => r["crops"]!["potato"]!["hours"] = 48), "A published crop cannot be changed");
        Reject(() => Try(r => ((JObject)r["crops"]!).Remove("lettuce")), "A published crop cannot be removed");
        // A new crop beside the shipped ones, using the mod's own items: the way a player file adds one.
        JObject Fast() { var c = (JObject)DataPacks.Parse(shipped, CropSchema.Name, true)["crops"]!["lettuce"]!.DeepClone(); c["hours"] = 36; c["feed"] = "fast-lettuce-v1"; c["feedCommodity"] = "fast lettuce feed"; return c; }
        check(Try(r => r["crops"]!["fast-lettuce"] = Fast()).crops.Count == 5, "A new crop that balances and uses known items is accepted");
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
        check(Crops.All.Count == 4, "Checking a candidate file does not change the crops in use");
    }
}
