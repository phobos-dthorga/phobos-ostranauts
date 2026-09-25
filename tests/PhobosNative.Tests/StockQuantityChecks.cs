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
            (PhobosAutoNav.EquipmentContent.Prepare(), _ => PhobosAutoNav.StockQuantities.Boards)
        };
        foreach (var (d, quantity) in packs)
        {
            int count = 0;
            foreach (var branch in d.Loot.Values.Where(l => offers.Contains(l.strName)))
            {
                var parsed = Parsed(branch); int expected = quantity(parsed.strName);
                check(parsed.fMin == expected && parsed.fMax == expected && expected > 1 && expected <= MarketStock.MaximumOfferQuantity,
                    "Native merchant parser receives the content-owned lot, including regional offers: " + branch.strName);
                count++;
            }
            check(count > 0, "Every trading content mod is covered by bulk-stock checks");
        }

        const string parent = "ItmOKLGSupplyKioskInv", branchId = "PhobosQuantityTest";
        string[] original = DataHandler.dictLoot[parent].aLoots.ToArray();
        var batch = new NativeDefinitions();
        MarketStock.Add(batch, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, 128);
        MarketStock.Add(batch, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, 64);
        check(batch.Loot[parent].aLoots.Count(x => x == branchId + "=1x1") == 1 && Parsed(batch.Loot[branchId]).fMin == 64,
            "Repeat registration replaces the lot without adding a second stock branch");
        check(original.All(x => batch.Loot[parent].aLoots.Contains(x)) && DataHandler.dictLoot[parent].aLoots.SequenceEqual(original),
            "Bulk preparation preserves live native/foreign stock and changes no inventory");
        foreach (int invalid in new[] { 0, -1, MarketStock.MaximumOfferQuantity + 1, int.MaxValue })
        {
            var rejected = new NativeDefinitions();
            throws(() => MarketStock.Add(rejected, parent, branchId, "PhobosVerdemorrowWaterConduitLoose", .5, StockCondition.Pristine, invalid),
                "Reject unsafe quantity before publication");
            check(rejected.Loot.Count == 0, "Rejected quantity leaves no partial registration");
        }
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
