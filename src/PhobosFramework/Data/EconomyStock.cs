using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>What an economy pack says about selling one item: which lot it ships in and which availability floor
/// applies, resolved from the pack's own entries (exact ids first, then family prefixes, longest first). Every
/// Phobos mod's stock quantities and regional offers go through here instead of their own item tests.</summary>
public static class EconomyStock
{
    public const int DefaultLot = 8;
    public const double DefaultFloor = 0.85;

    /// <summary>The sale ids of one equipment entry's small size: the intact form, then the damaged form (none for a single item).</summary>
    public static IEnumerable<string> SaleIds(string key, EquipmentEconomyEntry entry)
    {
        yield return IntactSaleId(key, entry);
        string? damaged = DamagedSaleId(key, entry);
        if (damaged != null) yield return damaged;
    }
    public static string IntactSaleId(string key, EquipmentEconomyEntry entry) => entry.forms == EconomySchema.MachineForms ? key + "Loose" : key;
    public static string? DamagedSaleId(string key, EquipmentEconomyEntry entry) => entry.forms switch
    {
        EconomySchema.MachineForms => key + "LooseDmg",
        EconomySchema.ItemForms => key + "Dmg",
        _ => null
    };
    /// <summary>The definition forms an entry has, as suffixes on the key.</summary>
    public static IEnumerable<string> FormSuffixes(EquipmentEconomyEntry entry) => entry.forms switch
    {
        EconomySchema.MachineForms => new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" },
        EconomySchema.ItemForms => new[] { "", "Dmg" },
        _ => new[] { "" }
    };
    /// <summary>Whether an item id belongs to the entry (any size of a machine family, either form of an item).</summary>
    public static bool Matches(string key, EquipmentEconomyEntry entry, string item) => entry.forms switch
    {
        EconomySchema.MachineForms => item.StartsWith(key, StringComparison.Ordinal),
        EconomySchema.ItemForms => item == key || item == key + "Dmg",
        _ => item == key
    };

    /// <summary>The lot and floor names for an item: an exact regional item or single/item-form entry first, then a
    /// supply prefix, then the longest matching machine prefix; the pack's defaults when nothing matches.</summary>
    public static (string Lot, string Floor) Classify(EconomyPack pack, string item)
    {
        if (pack.regional != null && pack.regional.items.TryGetValue(item, out var regional)) return (regional.lot, regional.floor);
        foreach (var pair in pack.equipment)
            if (pair.Value.forms != EconomySchema.MachineForms && Matches(pair.Key, pair.Value, item)) return (pair.Value.LotName, pair.Value.FloorName);
        SupplyEconomyEntry? supply = null; int supplyLength = -1;
        foreach (var pair in pack.supplies)
            if (item.StartsWith(pair.Key, StringComparison.Ordinal) && pair.Key.Length > supplyLength) { supply = pair.Value; supplyLength = pair.Key.Length; }
        EquipmentEconomyEntry? machine = null; int machineLength = -1;
        foreach (var pair in pack.equipment)
            if (pair.Value.forms == EconomySchema.MachineForms && Matches(pair.Key, pair.Value, item) && pair.Key.Length > machineLength) { machine = pair.Value; machineLength = pair.Key.Length; }
        if (supply != null && supplyLength >= machineLength) return (supply.LotName, supply.FloorName);
        if (machine != null) return (machine.LotName, machine.FloorName);
        return (EconomySchema.Equipment, EconomySchema.Equipment);
    }
    /// <summary>The finite lot one successful offer of the item delivers.</summary>
    public static int Quantity(EconomyPack pack, string item) => pack.Lot(Classify(pack, item).Lot, DefaultLot);
    /// <summary>The offer chance after the item's availability floor, capped at certainty.</summary>
    public static double Chance(EconomyPack pack, string item, double original) => Math.Min(1, Math.Max(pack.Floor(Classify(pack, item).Floor, DefaultFloor), original));

    /// <summary>Adds the pack's explicit offers (keyed by their loot id) and the template offers for every equipment
    /// sale id the owner lists, with the owner's id pattern <c>PhobosStock_&lt;tag&gt;_&lt;merchant&gt;_&lt;item&gt;</c>.</summary>
    public static void AddOffers(NativeDefinitions d, EconomyPack pack, IReadOnlyList<EquipmentSale> equipment)
    {
        foreach (var pair in pack.offers) Known(d, pair.Value.item, pair.Key);
        foreach (var template in pack.offerTemplates)
        foreach (var sale in equipment)
        {
            if (!sale.Entry.offers) continue;
            string? item = template.form == "Loose" ? sale.Intact : sale.Damaged;
            if (item == null) continue;
            MarketStock.Add(d, template.merchant, "PhobosStock_" + template.tag + "_" + template.merchant + "_" + item, item,
                Chance(pack, item, template.chance * sale.Entry.offerScale), Parse(template.condition), Quantity(pack, item));
        }
        foreach (var pair in pack.offers)
        {
            var offer = pair.Value;
            int quantity = offer.quantity ?? (offer.lot != null ? pack.Lot(offer.lot, DefaultLot) : Quantity(pack, offer.item));
            double floor = offer.floor != null ? pack.Floor(offer.floor, DefaultFloor) : pack.Floor(Classify(pack, offer.item).Floor, DefaultFloor);
            MarketStock.Add(d, offer.merchant, pair.Key, offer.item, Math.Min(1, Math.Max(floor, offer.chance)), Parse(offer.condition), quantity);
        }
        foreach (var pair in pack.supplies)
        foreach (string merchant in pair.Value.merchants)
        {
            string item = pair.Key + "Loose";
            MarketStock.Add(d, merchant, "PhobosStock_Supply_" + merchant + "_" + item, item, Chance(pack, item, pair.Value.chance), StockCondition.Pristine, Quantity(pack, item));
        }
    }

    /// <summary>Applies the pack's regional section: the expanded merchants carry every equipment sale id, every
    /// expanded supply and every expanded regional item at the floor chance; each region offers equipment at its
    /// regional chance (refurbished in the listed regions), supplies at theirs and the regional items at theirs.</summary>
    public static void ApplyRegional(NativeDefinitions d, EconomyPack pack, string ownerTag, IReadOnlyList<EquipmentSale> equipment)
    {
        var regional = pack.regional;
        if (regional == null) return;
        foreach (var pair in regional.items) Known(d, pair.Key, "regional/items");
        var expanded = equipment.Select(s => s.Intact)
            .Concat(pack.supplies.Where(p => p.Value.expanded).Select(p => p.Key + "Loose"))
            .Concat(regional.items.Where(p => p.Value.expanded).Select(p => p.Key)).ToArray();
        foreach (string merchant in regional.expandedMerchants)
        foreach (string item in expanded)
            MarketStock.AddMissing(d, merchant, "PhobosExpanded_" + ownerTag + "_" + merchant + "_" + item, item, Chance(pack, item, 0), StockCondition.Pristine, Quantity(pack, item));
        foreach (var region in pack.regions)
        {
            var condition = regional.refurbished.Contains(region.Key) ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (var sale in equipment)
                RegionalMarkets.Add(d, region.Key, sale.Intact, Chance(pack, sale.Intact, (sale.Entry.regionalChance ?? regional.baseChance) * sale.Entry.offerScale * region.Value), condition, Quantity(pack, sale.Intact));
            foreach (var pair in pack.supplies)
            {
                if (pair.Value.regionalChance is not double chance) continue;
                string item = pair.Key + "Loose";
                var supplyCondition = pair.Value.regionalCondition == EconomySchema.RegionalCondition ? condition : Parse(pair.Value.regionalCondition);
                RegionalMarkets.Add(d, region.Key, item, Chance(pack, item, chance * region.Value), supplyCondition, Quantity(pack, item));
            }
            foreach (var pair in regional.items)
                RegionalMarkets.Add(d, region.Key, pair.Key, Chance(pack, pair.Key, pair.Value.chance * region.Value), Parse(pair.Value.condition), Quantity(pack, pair.Key));
        }
    }

    /// <summary>The spread of one world-loot entry: explicit items as written, or the entry's chance divided over the
    /// loot-eligible equipment sale ids, the broken share to the damaged forms.</summary>
    public static Dictionary<string, double> LootChances(WorldLootEntry loot, IReadOnlyList<EquipmentSale> equipment)
    {
        if (loot.items != null) return new Dictionary<string, double>(loot.items, StringComparer.Ordinal);
        var found = equipment.Where(s => s.Loot).ToArray();
        var chances = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var sale in found)
        {
            if (sale.Damaged != null) { chances[sale.Damaged] = loot.chance * loot.brokenShare / found.Length; chances[sale.Intact] = loot.chance * (1 - loot.brokenShare) / found.Length; }
            else chances[sale.Intact] = loot.chance / found.Length;
        }
        return chances;
    }
    /// <summary>Adds every world-loot entry of the pack as an additive item choice.</summary>
    public static void AddWorldLoot(NativeDefinitions d, EconomyPack pack, IReadOnlyList<EquipmentSale> equipment, double scale = 1)
    {
        foreach (var loot in pack.worldLoot)
        {
            // Loot items are not checked here: owners may prepare loot before the items themselves (the native checks catch a wrong id).
            var chances = LootChances(loot, equipment);
            if (scale != 1) chances = chances.ToDictionary(p => p.Key, p => p.Value * scale, StringComparer.Ordinal);
            foreach (string table in loot.Tables)
                AdditiveLoot.SetItemChoice(d, table, loot.tables.Count > 0 ? loot.branch + "_" + table : loot.branch, chances);
        }
    }

    public static StockCondition Parse(string condition) => (StockCondition)Enum.Parse(typeof(StockCondition), condition);
    /// <summary>An offer or find may only name a definition the owner is registering or one the game already holds
    /// (a loot-only preparation after the items were published); a typo in a file is refused here, not at the shop.</summary>
    private static void Known(NativeDefinitions d, string item, string where)
    {
        if (d.Objects.ContainsKey(item)) return;
        var native = DataHandler.dictCOs;
        if (native != null && native.ContainsKey(item)) return;
        throw new ArgumentException(Text.Get("EconomyStock.unknown_item", where, item));
    }

    /// <summary>The game's requirement triggers for the ordinary bill materials.</summary>
    public static readonly IReadOnlyDictionary<string, string> MaterialTriggers = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["ItmScrapSteel"] = "TIsScrapSteel", ["ItmScrapAluminum"] = "TIsScrapAluminum", ["ItmPartsMechSmall01"] = "TIsPartsMechSmall", ["ItmPartsElecSmall01"] = "TIsPartsElecSmall",
        ["ItmComponentMotor01"] = "TIsMotor", ["ItmComponentMobo01"] = "TIsMobo", ["ItmHeatSink01"] = "TIsHeatSink", ["ItmPartsScreen01"] = "TIsScreen"
    };
    /// <summary>A repair bill as native job inputs, in the bill's own order, skipping zero counts.</summary>
    public static string[] RepairInputs(IEnumerable<KeyValuePair<string, int>> bill) => bill.Where(b => b.Value > 0)
        .Select(b => (MaterialTriggers.TryGetValue(b.Key, out string? trigger) ? trigger : throw new ArgumentException("No requirement trigger for " + b.Key)) + "=1x" + b.Value).ToArray();
}

/// <summary>One saleable size of an equipment entry: its intact sale id, its damaged sale id (if any) and the entry
/// whose offers, scale and lot it follows. Owners list every size of a ladder against the small entry.</summary>
public sealed class EquipmentSale
{
    public string Intact { get; }
    public string? Damaged { get; }
    public EquipmentEconomyEntry Entry { get; }
    /// <summary>Whether this size may turn up in world loot (larger sizes of a ladder do not).</summary>
    public bool Loot { get; }
    public EquipmentSale(string intact, string? damaged, EquipmentEconomyEntry entry, bool? loot = null) { Intact = intact; Damaged = damaged; Entry = entry; Loot = loot ?? entry.loot; }
    public static EquipmentSale Of(string key, EquipmentEconomyEntry entry) => new(EconomyStock.IntactSaleId(key, entry), EconomyStock.DamagedSaleId(key, entry), entry);
    /// <summary>A larger size of a machine family under its own prefix.</summary>
    public static EquipmentSale Size(string prefix, EquipmentEconomyEntry small) => new(prefix + "Loose", prefix + "LooseDmg", small, loot: false);
}
