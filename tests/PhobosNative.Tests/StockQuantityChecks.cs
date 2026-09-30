using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

internal static class StockQuantityChecks
{
    private static LootUnit Parsed(Loot loot) => ((List<List<LootUnit>>)typeof(Loot)
        .GetField("aCOLootUnits", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(loot)!).Single().Single();

    internal static void Run(Action<bool,string> check, Action<Action,string> throws)
    {
        var offers = (IDictionary)typeof(MarketStock).GetField("Offers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        var packs = new (NativeDefinitions Definitions, Func<string,int> Quantity)[] {
            (PhobosAgriculture.Definitions.Prepare(), PhobosAgriculture.StockQuantities.For),
            (PhobosShipbreaker.Content.Prepare(), PhobosShipbreaker.StockQuantities.For),
            (PhobosAutoNav.EquipmentContent.Prepare(), _ => PhobosAutoNav.StockQuantities.Boards),
            (PhobosManufacturing.Content.Prepare(true), PhobosManufacturing.StockQuantities.For),
            (Phobos.Ostranauts.Framework.Items.FrameworkItems.Prepare(), Phobos.Ostranauts.Framework.Items.ItemEconomy.Quantity)
        };
        foreach (var (d, quantity) in packs)
        {
            int count = 0;
            foreach (var branch in d.Loot.Values.Where(l => offers.Contains(l.strName)))
            {
                var parsed = Parsed(branch); int expected = quantity(parsed.strName);
                check(parsed.fMin == expected && parsed.fMax == expected && expected > 1 && expected <= MarketStock.MaximumOfferQuantity,
                    "Native merchant parser receives the content-owned lot, including regional offers: " + branch.strName);
                check(parsed.fChance >= .85f && parsed.fChance <= 1, "Every stock offer receives increased availability before optional configuration");
                count++;
            }
            check(count > 0, "Every trading content mod is covered by bulk-stock checks");
        }

        // Every implemented purchasable family has a functional route in each general local market.
        foreach (var (d, quantity) in packs)
        {
            var available = d.Loot.Values.Where(l => offers.Contains(l.strName)).Select(l => Parsed(l).strName)
                .Where(id => !id.EndsWith("Dmg", StringComparison.Ordinal)).ToHashSet();
            foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv" })
            {
                check(!d.Loot.ContainsKey(merchant) && d.LootBranches.ContainsKey(merchant), "Merchant tables are linked in place, never republished: " + merchant);
                var local = d.LootBranches[merchant].Where(id => d.Loot.ContainsKey(id) && offers.Contains(id))
                    .Select(id => Parsed(d.Loot[id]).strName).ToHashSet();
                check(available.IsSubsetOf(local), "No functional item silently omitted from general market: " + merchant);
            }
        }

        // Every machine family has the same second-hand routes as its siblings: used and refurbished
        // intact stock, and a broken offer of its damaged form.
        string ConditionOf(string branch) { object offer = offers[branch]!;
            return offer.GetType().GetField("Condition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(offer)!.ToString()!; }
        var families = new (NativeDefinitions Definitions, (string Intact, string Broken)[] Machines)[] {
            (packs[0].Definitions, PhobosAgriculture.Definitions.MachineFamilies.Select(p => (p + "Loose", p + "LooseDmg")).ToArray()),
            (packs[1].Definitions, PhobosShipbreaker.EquipmentEconomy.Machines.Select(m => (m.Prefix + "Loose", m.Prefix + "LooseDmg")).ToArray()),
            (packs[2].Definitions, new[] { PhobosAutoNav.NavigationService.ModuleId, PhobosAutoNav.NavigationService.PursuitId, PhobosAutoNav.NavigationService.FireControlId }
                .Select(id => (id, id + "Dmg")).ToArray()),
            (packs[3].Definitions, PhobosManufacturing.EquipmentEconomy.Machines.Select(m => (m.Prefix + "Loose", m.Prefix + "LooseDmg")).ToArray())
        };
        foreach (var (d, machines) in families)
        {
            var routes = d.Loot.Values.Where(l => offers.Contains(l.strName))
                .ToLookup(l => Parsed(l).strName, l => ConditionOf(l.strName));
            foreach (var (intact, broken) in machines)
            {
                check(routes[intact].Contains("Worn") && routes[intact].Contains("Refurbished"), "Used and refurbished stock exist for " + intact);
                check(routes[broken].Contains("Broken"), "A broken offer exists for " + broken);
            }
        }

        const string parent = "ItmOKLGSupplyKioskInv", branchId = "PhobosQuantityTest";
        string[] original = DataHandler.dictLoot[parent].aLoots.ToArray();
        var batch = new NativeDefinitions();
        MarketStock.Add(batch, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, 128);
        MarketStock.Add(batch, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, 64);
        check(batch.LootBranches[parent].Count(x => x == branchId) == 1 && Parsed(batch.Loot[branchId]).fMin == 64,
            "Repeat registration replaces the lot without adding a second stock branch");
        check(!batch.Loot.ContainsKey(parent) && DataHandler.dictLoot[parent].aLoots.SequenceEqual(original),
            "Bulk preparation preserves live native/foreign stock and changes no inventory");
        foreach (int invalid in new[] { 0, -1, MarketStock.MaximumOfferQuantity + 1, int.MaxValue })
        {
            var rejected = new NativeDefinitions();
            throws(() => MarketStock.Add(rejected, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, invalid),
                "Reject unsafe quantity before publication");
            check(rejected.Loot.Count == 0 && rejected.LootBranches.Count == 0, "Rejected quantity leaves no partial registration");
        }
        MarketStock.AddMissing(batch, parent, "PhobosShouldNotDuplicate", "PhobosVerdemorrowWaterConduitLoose", .95, StockCondition.Worn, 16);
        check(!batch.Loot.ContainsKey("PhobosShouldNotDuplicate") && Parsed(batch.Loot[branchId]).fMin == 64,
            "Coverage filling preserves an existing prepared offer and its physical lot");
        MarketStock.AddMissing(batch, parent, "PhobosMissingBoard", "PhobosNavModAutoNav", .85, StockCondition.Pristine, 16);
        check(batch.Loot.ContainsKey("PhobosMissingBoard"), "A distinct omitted identity receives an offer");
        var legacy = new NativeDefinitions();
        MarketStock.Add(legacy, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine);
        check(Parsed(legacy.Loot[branchId]).fMin == 1, "Old public API keeps its single-unit contract");
        double multiplier = MarketStock.AvailabilityMultiplier;
        try
        {
            MarketStock.AvailabilityMultiplier = 2;
            MarketStock.Add(legacy, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .4, StockCondition.Pristine, 128);
            var parsed = Parsed(legacy.Loot[branchId]);
            check(Math.Abs(parsed.fChance - .8) < 1e-6 && parsed.fMin == 128 && parsed.fMax == 128,
                "Availability multiplies the offer chance, not physical quantity");
        }
        finally { MarketStock.AvailabilityMultiplier = multiplier; }
    }
}
