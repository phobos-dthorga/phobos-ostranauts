using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;
using Newtonsoft.Json;
using Ostranauts.Trading;

// Use the game's data-only valuation implementation, including native wear tiers.
// An optional report is generated from the same definitions used by these checks.
internal static class EquipmentValueAudit
{
    private static double Price(string id, double wear = 0, bool pristine = false)
    {
        var co = NativeDefinitions.Clone(DataHandler.dictCOs[id]);
        double maximum = EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatDamageMax");
        MaintenanceDefinitions.SetStat(co, "StatDamage", maximum * wear);
        // DataCO tests condition presence, so a literal IsPristine=0 is still a flag.
        co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsPristine=", StringComparison.Ordinal)).ToArray();
        if (pristine) MaintenanceDefinitions.SetStat(co, "IsPristine", 1);
        return new DataCO(co).GetBasePrice();
    }

    private static (double low, double high) Discount(string id)
    {
        string expression = DataHandler.dictLoot[id].aCOs.Single().Split('x').Last();
        var values = expression.Split('-').Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        return (values[0], values.Last());
    }

    internal static void Run(string repo, Action<bool,string> check, string? report)
    {
        // VORB's native buyer accepts both empty loose machinery and these parts.
        // Use adverse ends of its range, not an invented universal shop price.
        var buy = Discount("CONDTraderDiscountBuyKioskScrapVORB");
        var sell = Discount("CONDTraderDiscountSellKioskScrapVORB");
        var buyer = DataHandler.dictCTs["TIsBarterVORBScrapKiosk"];
        var rows = new List<string> {
            "# Phobos equipment value audit", "",
            "Generated from current Phobos candidate definitions and Ostranauts 1.0.1.5's `DataCO.GetBasePrice` (2026-09-24). Includes every implemented equipment family, both functional and broken forms, and the assembly section. No game session or live merchant quote was sampled.", "",
            "All dollar figures below are **whole-object values**, not prices per kilogram or shop purchase quotes. Recovered parts are valued at full condition without a retail pristine flag. Work, power and tool costs are excluded.", "",
            "| Item/state | Whole base | Whole at maximum wear tier | All dismantling outputs | Output / whole base | VORB adverse comparison: whole / scrap |",
            "|---|---:|---:|---:|---:|---:|" };
        check(Price(Content.Loose, 0, true) > Price(Content.Loose), "Pristine premium is distinct from ordinary full condition");
        check(Price(Content.Loose, .99) < Price(Content.Loose), "Native wear reduces whole-equipment value");
        var items = EquipmentEconomy.Machines.SelectMany(s => new[] { s.Prefix + "Loose", s.Prefix + "LooseDmg" })
            .Concat(new[] { ProcessRules.AssemblySection, ReclaimerRules.Section, PhobosAutoNav.EquipmentContent.Base, PhobosAutoNav.EquipmentContent.Base + "Dmg" });
        foreach (string id in items)
        {
            string[] outputs = DataHandler.dictInstallables[id + "Dismantle"].aLootCOs;
            double recovered = outputs.Sum(p => Price(p));
            double full = Price(id), worn = Price(id, .99);
            check(buyer.TriggeredDataCO(new DataCO(DataHandler.dictCOs[id]), false), "Audited buyer accepts loose equipment: " + id);
            check(outputs.All(p => buyer.TriggeredDataCO(new DataCO(DataHandler.dictCOs[p]), false)), "Audited buyer accepts dismantling products: " + id);
            foreach (double wear in new[] { 0, .049, .05, .15, .34, .67, .99 })
            foreach (bool pristine in new[] { false, true })
            {
                double whole = Price(id, wear, pristine);
                check(recovered < whole, "Dismantling loses value at native wear/pristine tier: " + id);
                check(recovered * buy.high < whole * buy.low, "Dismantling loses even across adverse VORB buyer endpoints: " + id);
                check(recovered * buy.high < whole * sell.low, "VORB purchase then dismantle/resell is a loss: " + id);
            }
            rows.Add($"| {DataHandler.dictCOs[id].strNameFriendly} | ${full:N2} | ${worn:N2} | ${recovered:N2} | {recovered/full:P2} | ${worn*buy.low:N2} / ${recovered*buy.high:N2} |");
        }
        rows.AddRange(new[] { "", "The VORB column compares the **lowest whole-item value at the lowest native buyer multiplier** against **fresh output at the highest buyer multiplier**. It excludes supply/demand, negotiation, travel and labour. An assembly section has no wear stat. Broken equipment has its own lower base price; additional wear can reduce that again.", "",
            "This is a conservative vanilla baseline, not a guarantee across different regions, category shortages, blockades or mods which alter prices. VORB's broad buyer is checked against every item above. Other merchants can refuse a whole machine or its scrap entirely.", "",
            "## Construction and repair material values", "",
            "| Recipe output | Construction inputs, base value | Dismantling outputs, base value |", "|---|---:|---:|" });
        double sectionInputs = 0;
        foreach (string mod in new[] { "PhobosShipbreaker", "PhobosAutoNav" })
        {
            var pack = JsonConvert.DeserializeObject<RecipePack>(File.ReadAllText(Path.Combine(repo, "mods", mod, "framework", "recipes.json")))!;
            foreach (var recipe in pack.recipes)
            {
                string id = recipe.outputs[0].item;
                if (id == "PhobosNavModAutoNav") id = PhobosAutoNav.EquipmentContent.Base;
                double inputs = recipe.ingredients.Sum(i => i.count * Price(i.item));
                if (id == ProcessRules.AssemblySection) sectionInputs = inputs;
                double scrap = DataHandler.dictInstallables[id + "Dismantle"].aLootCOs.Sum(p => Price(p));
                // Includes unfinished section construction; no craft/scrap material loop.
                check(scrap < inputs, "Construct then dismantle never increases ingredient value: " + recipe.id);
                rows.Add($"| {recipe.name} | ${inputs:N2} | ${scrap:N2} |");
            }
        }
        double processorSalvage = DataHandler.dictInstallables[Content.Loose + "Dismantle"].aLootCOs.Sum(p => Price(p));
        check(processorSalvage < sectionInputs * 2, "Processor salvage is below original raw materials, not just priced sections");
        rows.AddRange(new[] { "", $"The processor's final assembly consumes two priced sections. Raw materials for both sections total ${sectionInputs*2:N2}; its ${processorSalvage:N2} dismantling yield is also below that original raw-material bill. Construction creates a usable machine through labour; this is separate from the dismantling comparison.", "",
            "| Repair target | Replacement inputs, base value |", "|---|---:|" });
        foreach (var spec in EquipmentEconomy.Machines)
            rows.Add($"| {DataHandler.dictCOs[spec.Prefix + "Loose"].strNameFriendly} | ${spec.RepairBill.Select((count,i) => count * Price(EquipmentEconomy.Materials[i])).Sum():N2} |");
        rows.AddRange(new[] { $"| Phobos Auto Nav | ${2*Price("ItmPartsElecSmall01"):N2} |", "", "Repair values exclude purchasing markups, reusable tools, work and subsequent Restore. Repair returns equal-mass spent material; it does not mint fresh valuable components.", "" });
        if (report != null) File.WriteAllText(report, string.Join("\n", rows));
    }
}
