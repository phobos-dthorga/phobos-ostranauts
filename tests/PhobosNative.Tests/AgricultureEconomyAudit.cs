using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using PhobosAgriculture.Core;

// Read-only audit of actual content definitions through the native evaluator.
// Findings deliberately do not assert that authored price choices are balanced.
internal static class AgricultureEconomyAudit
{
    internal static void Write(NativeDefinitions definitions, string repo, string report)
    {
        JsonCondOwner Object(string id) => definitions.Objects.TryGetValue(id, out var co) ? co : DataHandler.dictCOs[id];
        double Price(string id) => EquipmentValueAudit.Price(Object(id));
        string Money(double value) => value.ToString("N2", CultureInfo.InvariantCulture);
        string rack = PhobosAgriculture.Definitions.Rack, cooker = PhobosAgriculture.Definitions.Cooker;
        var buy = EquipmentValueAudit.Discount("CONDTraderDiscountBuyKioskScrapVORB");
        var sell = EquipmentValueAudit.Discount("CONDTraderDiscountSellKioskScrapVORB");
        var recipes = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(Path.Combine(repo, "mods/PhobosAgriculture/framework/recipes.json")))!;
        var comparisons = new List<string>();
        var rows = new List<string> { "# Agriculture economy: generated evidence", "",
            "25 September 2026. Current Agriculture definitions plus installed Ostranauts 1.0.1.5, evaluated with Blue Bottle Games' native DataCO.GetBasePrice and trade triggers. Values are per object, in credits, before merchant/market adjustments. No game session or live quote was sampled; this report reflects the current authored economic balance.", "",
            "## Equipment and construction", "",
            "| Machine | Base | Pristine | Broken base | Worn broken | Raw construction inputs | Work minutes | Dismantle outputs |", "|---|---:|---:|---:|---:|---:|---:|---:|" };
        foreach (string prefix in new[] { rack, cooker, PhobosAgriculture.IrrigationDefinitions.Supply, PhobosAgriculture.IrrigationDefinitions.Pipe })
        {
            string id = prefix + "Loose";
            var recipe = recipes.recipes.Single(r => r.outputs.Any(o => o.item == id));
            double ingredients = recipe.ingredients.Sum(i => Price(i.item) * i.count);
            double scrap = definitions.Installables[id + "Dismantle"].aLootCOs.Sum(Price);
            rows.Add($"| {Object(id).strNameFriendly} | {Money(Price(id))} | {Money(EquipmentValueAudit.Price(Object(id), pristine: true))} | {Money(Price(id + "Dmg"))} | {Money(EquipmentValueAudit.Price(Object(id + "Dmg"), .99))} | {Money(ingredients)} | {recipe.workSeconds / 60d:G} | {Money(scrap)} |");
            // Explicit like-for-like adverse comparison, not a claim about a live deal.
            double purchase = ingredients * 1.25 * sell.high, resale = Price(id) * buy.low;
            comparisons.Add($"{Object(id).strNameFriendly}: VORB endpoint illustration: pristine ingredients at the highest sell multiplier cost {Money(purchase)}; a full-condition constructed machine at the lowest buy multiplier returns {Money(resale)}. Difference {Money(resale - purchase)} before tools, labour, hauling, availability and market category effects.\n");
        }
        rows.Add(""); rows.AddRange(comparisons);
        rows.AddRange(new[] { "", "## Native service definitions", "", "| Target | Repair inputs | Repair outputs | Restore inputs | Dismantle outputs |", "|---|---|---|---|---|" });
        foreach (string prefix in new[] { rack, cooker, PhobosAgriculture.IrrigationDefinitions.Supply, PhobosAgriculture.IrrigationDefinitions.Pipe })
        {
            var repair = definitions.Installables[prefix + "LooseDmgRepair"];
            var restore = definitions.Installables[prefix + "LooseRestore"];
            rows.Add($"| {Object(prefix + "Loose").strNameFriendly} | {string.Join(", ", repair.aInputs)} | {string.Join(", ", repair.aLootCOs)} + Framework actual spent materials | {(restore.aInputs.Length == 0 ? "None; native in-place wear work" : string.Join(", ", restore.aInputs))} | {string.Join(", ", definitions.Installables[prefix + "LooseDismantle"].aLootCOs)} |");
        }
        rows.Add("\nRepair bills and work differ by machine; actual consumed repair mass returns as spent material. Restore removes wear in place and does not award pristine condition. See the current player guide for bills and timings.\n");
        rows.AddRange(new[] { "## Supply and food values / buyer acceptance", "",
            "These are native data-trigger results for empty loose definitions. 'Buy' means the merchant buys from the player. Stock insertion does not establish buy-back eligibility.", "",
            "| Item | Base | Category flags | Supply buyer | VORB buyer | Fixer generic buyer | Halvorson buyer |", "|---|---:|---|---|---|---|---|" });
        foreach (var co in definitions.Objects.Values.Where(c => !c.strName.EndsWith("Installed", StringComparison.Ordinal) && !c.strName.EndsWith("InstalledDmg", StringComparison.Ordinal)))
        {
            var data = new DataCO(co);
            var flags = co.aStartingConds.Where(c => c.StartsWith("IsCategory", StringComparison.Ordinal)).Select(c => c.Split('=')[0]);
            var accepted = new[] { "TIsBarterOKLGSupplyKiosk", "TIsBarterVORBScrapKiosk", "TIsBarterOKLGFixerBuy", "TIsBarterSanDiegoHalvorsonBuy" }
                .Select(t => DataHandler.dictCTs[t].TriggeredDataCO(data, false) ? "Yes" : "No");
            rows.Add($"| {co.strNameFriendly} | {Money(EquipmentValueAudit.Price(co))} | {string.Join(", ", flags)} | {string.Join(" | ", accepted)} |");
        }
        rows.AddRange(new[] { "", "## Ideal crop economics with purchased native water", "",
            $"Native LiquidWater: {Money(Price("LiquidWater"))} per 0.25 kg = {Money(Price("LiquidWater") / .25)} per kg. Native acceptable algae meal: {Money(Price("ItmTrencherAcceptableAlgae"))} per portion. Water prices here describe native inventory rations, not Ship's Water tank-refill tariffs.", "",
            "| Cycle | Consumed water value | Consumed nutrient value | Whole harvested output value | Propagation stock |", "|---|---:|---:|---:|---:|" });
        foreach (var crop in new[] { Crop.Potato, Crop.Lettuce, Crop.LettuceSeed })
        {
            var state = new CropState { Water = 20, Nutrients = .5 }; state.Plant(crop, 1);
            for (int hour = 0; hour < crop.Hours; hour++) state.Step(1, crop.KW, 10, 10, true);
            var harvest = state.Harvest();
            string food = crop == Crop.Potato ? PhobosAgriculture.Definitions.Meal : crop == Crop.LettuceSeed ? PhobosAgriculture.Definitions.LettuceSeed : PhobosAgriculture.Definitions.Leaves;
            rows.Add($"| {crop.Id} | {Money(crop.Water / .25 * Price("LiquidWater"))} | {Money(crop.Nutrient / .04 * Price(PhobosAgriculture.Definitions.Nutrient))} | {Money(harvest.Portions * Price(food))} | {(crop == Crop.LettuceSeed ? "Consumes one packet; harvests four seed packets, no edible leaves" : harvest.SeedKg > 0 ? Money(Price(PhobosAgriculture.Definitions.PotatoSeed)) + " retained seed potato" : "Consumes one " + Money(Price(PhobosAgriculture.Definitions.LettuceSeed)) + " packet; no seed return")} |");
        }
        rows.Add($"\nGroundwork irrigation charges: {Money(Price(PhobosAgriculture.Definitions.Irrigation))} per {PhobosAgriculture.Definitions.IrrigationKg:G} kg. Consumed root-water cost: potato {Money(Crop.Potato.Water / PhobosAgriculture.Definitions.IrrigationKg * Price(PhobosAgriculture.Definitions.Irrigation))}; lettuce {Money(Crop.Lettuce.Water / PhobosAgriculture.Definitions.IrrigationKg * Price(PhobosAgriculture.Definitions.Irrigation))}. Purchase whole charges; remaining water stays available. No potable conversion.\n");
        rows.AddRange(new[] { "", "The ideal potato row assumes cooking all nominal whole portions. It excludes delayed-harvest respiration/rounding, electricity, cooker work, losses and equipment amortization. Retained potato seed is not both sold and replanted. Lettuce consumes only 5 g of a 40 g nutrient packet; unused nutrient remains inventory, not an eightfold recurring expense.", "",
            "Conclusions and proposed changes are in [Agriculture economy review](agriculture-economy-review.md). These prices are authored game balance; NASA/ESA research does not establish fictional prices, profit margins or repair bills.", "" });
        rows.Add($"New treatment jobs: {TreatmentCartridge.FullPrice:G} cr / {TreatmentCartridge.CapacityKg:G} kg drainage capacity = {TreatmentCartridge.FullPrice/TreatmentCartridge.CapacityKg:G} cr per kg at base value. A 0.25 kg batch uses {TreatmentCartridge.Mass(.25):G} kg medium and {TreatmentCartridge.Price(.25):G} cr of capacity; a 20 kg batch uses {TreatmentCartridge.Mass(20):G} kg and {TreatmentCartridge.Price(20):G} cr. Unused medium returns with proportional base value. Historic bound jobs retain whole-cartridge consumption. Electricity remains {DrainageRecovery.KWhPerKg:G} kWh/kg, excluding crew/equipment.\n");
        AgricultureOperatingAudit.Append(rows, Price);
        File.WriteAllText(report, string.Join("\n", rows));
    }
}
