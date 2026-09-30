using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Trading;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>economy</c> schema: what a content mod's equipment costs, how long it takes to install, repair and
/// dismantle, what repairs need and dismantling returns, and where it is sold. Balance stays content-owned in the
/// file; the shape and the checks are shared. Every dictionary is keyed by the identifier the code already uses, so a
/// player file can tune an entry by naming it. Ids stay in code; a player cannot add equipment, only offers.</summary>
public sealed class EconomyPack : DataPack
{
    /// <summary>Machines and other equipment families, keyed by definition prefix (the small size for a vessel ladder).</summary>
    public Dictionary<string, EquipmentEconomyEntry> equipment = new(StringComparer.Ordinal);
    /// <summary>Ordinary supplies (pipes, lines, charges), keyed by definition prefix.</summary>
    public Dictionary<string, SupplyEconomyEntry> supplies = new(StringComparer.Ordinal);
    /// <summary>Offers made for every equipment entry that accepts offers; the offer id is derived by the owner.</summary>
    public List<OfferTemplate> offerTemplates = new();
    /// <summary>Explicit offers, keyed by their loot branch id (must start with Phobos). Players may add these.</summary>
    public Dictionary<string, OfferEntry> offers = new(StringComparer.Ordinal);
    /// <summary>Regional availability factors by the game's region code.</summary>
    public Dictionary<string, double> regions = new(StringComparer.Ordinal);
    public RegionalStock? regional;
    /// <summary>Finite lot per successful offer, by kind (<c>equipment</c>, <c>supplies</c>).</summary>
    public Dictionary<string, int> lots = new(StringComparer.Ordinal);
    /// <summary>Minimum offer probability before the Framework availability multiplier, by kind.</summary>
    public Dictionary<string, double> chanceFloors = new(StringComparer.Ordinal);
    /// <summary>Rare world finds in the game's own loot tables.</summary>
    public List<WorldLootEntry> worldLoot = new();
}

public sealed class WorkEntry
{
    public int install, uninstall, repair, dismantle;
}

public sealed class EquipmentEconomyEntry
{
    public string? notes;
    public string kind = "equipment";
    public double price;
    /// <summary>Broken-form price; a quarter of the price when omitted.</summary>
    public double? brokenPrice;
    public WorkEntry work = new();
    public Dictionary<string, int> repairBill = new(StringComparer.Ordinal);
    public Dictionary<string, int> salvage = new(StringComparer.Ordinal);
    public Dictionary<string, int> brokenSalvage = new(StringComparer.Ordinal);
    public int restoreMinutes;
    /// <summary>Internal compartment emptied before dismantling, if any.</summary>
    public string? internalBin;
    /// <summary>Whether a loose unit may turn up in world loot.</summary>
    public bool loot = true;
    /// <summary>Whether the family carries the game's high-salvage mark.</summary>
    public bool salvageValueHigh = true;
    /// <summary>Whether the offer templates apply to this family.</summary>
    public bool offers = true;
    public double BrokenPriceOrDefault => brokenPrice ?? price / 4;
}

public sealed class SupplyEconomyEntry
{
    public string? notes;
    public string kind = "supplies";
    public double price;
    public int repairWork, dismantleWork;
    public Dictionary<string, int> repairBill = new(StringComparer.Ordinal);
    /// <summary>Retained remainder definition returned by dismantling (its mass is the item's own).</summary>
    public string? remainder;
    public List<string> merchants = new();
    public double chance = 1;
}

public sealed class OfferTemplate
{
    public string? notes;
    public string merchant = "";
    public string tag = "";
    /// <summary>Which form the offer sells: <c>Loose</c> or <c>LooseDmg</c>.</summary>
    public string form = "Loose";
    public string condition = "Pristine";
    public double chance;
}

public sealed class OfferEntry
{
    public string? notes;
    public string merchant = "";
    public string item = "";
    public string condition = "Pristine";
    public double chance;
    public int? quantity;
}

public sealed class RegionalStock
{
    public string? notes;
    public double baseChance;
    public List<string> refurbished = new();
    public List<string> expandedMerchants = new();
}

public sealed class WorldLootEntry
{
    public string? notes;
    public string table = "";
    public string branch = "";
    public double chance;
    public double brokenShare;
}

/// <summary>What the owner knows that the file cannot: which families exist in code and how much they weigh, which
/// material ids and merchants exist. Null members skip their check (pure offline tests).</summary>
public sealed class EconomyContext
{
    public IReadOnlyCollection<string> Equipment { get; }
    public IReadOnlyCollection<string> Supplies { get; }
    public Func<string, double?>? MassOf { get; set; }
    public Func<string, double?>? MaterialMassOf { get; set; }
    public Func<string, bool>? MerchantExists { get; set; }
    public double MassToleranceKg { get; set; } = Units.MassToleranceKg;
    public EconomyContext(IReadOnlyCollection<string> equipment, IReadOnlyCollection<string> supplies)
    {
        Equipment = equipment ?? throw new ArgumentNullException(nameof(equipment)); Supplies = supplies ?? Array.Empty<string>();
    }
}

/// <summary>The economy checks every file passes, shipped or player: complete coverage of the code's families, sane
/// numbers, known materials, merchants and regions, and salvage that weighs what the machine weighs. Pricing rules
/// (value bands, the dismantling rule) are authoring rules for shipped data and are not enforced here.</summary>
public static class EconomySchema
{
    public const string Name = "economy";
    public const int MaximumQuantity = MarketStock.MaximumOfferQuantity;
    public static readonly IReadOnlyList<string> Kinds = new[] { "equipment", "supplies" };

    public static void Validate(EconomyPack pack, EconomyContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        foreach (string prefix in context.Equipment)
            if (!pack.equipment.ContainsKey(prefix)) throw new ArgumentException(Text.Get("EconomySchema.missing_equipment", prefix));
        foreach (var pair in pack.equipment)
        {
            if (!context.Equipment.Contains(pair.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_equipment", pair.Key));
            var e = pair.Value;
            if (!Kinds.Contains(e.kind)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", pair.Key, e.kind));
            Positive(e.price, pair.Key, "price");
            if (e.brokenPrice is double broken && (!Finite(broken) || broken <= 0 || broken >= e.price)) throw new ArgumentException(Text.Get("EconomySchema.broken_price", pair.Key));
            foreach (var (label, value) in new[] { ("install", e.work.install), ("uninstall", e.work.uninstall), ("repair", e.work.repair), ("dismantle", e.work.dismantle), ("restoreMinutes", e.restoreMinutes) })
                if (value <= 0) throw new ArgumentException(Text.Get("EconomySchema.work", pair.Key, label));
            Bill(e.repairBill, pair.Key, "repairBill", context, allowEmpty: false);
            Bill(e.salvage, pair.Key, "salvage", context, allowEmpty: false);
            Bill(e.brokenSalvage, pair.Key, "brokenSalvage", context, allowEmpty: false);
            double? mass = context.MassOf?.Invoke(pair.Key);
            if (mass is double kg && context.MaterialMassOf != null)
            {
                foreach (var (label, bill) in new[] { ("salvage", e.salvage), ("brokenSalvage", e.brokenSalvage) })
                {
                    double total = bill.Sum(b => b.Value * (context.MaterialMassOf(b.Key) ?? 0));
                    if (Math.Abs(total - kg) > context.MassToleranceKg) throw new ArgumentException(Text.Get("EconomySchema.salvage_mass", pair.Key, label, total, kg));
                }
            }
        }
        foreach (string prefix in context.Supplies)
            if (!pack.supplies.ContainsKey(prefix)) throw new ArgumentException(Text.Get("EconomySchema.missing_equipment", prefix));
        foreach (var pair in pack.supplies)
        {
            if (!context.Supplies.Contains(pair.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_equipment", pair.Key));
            var s = pair.Value;
            if (!Kinds.Contains(s.kind)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", pair.Key, s.kind));
            Positive(s.price, pair.Key, "price");
            if (s.repairWork <= 0 || s.dismantleWork <= 0) throw new ArgumentException(Text.Get("EconomySchema.work", pair.Key, "repairWork/dismantleWork"));
            Bill(s.repairBill, pair.Key, "repairBill", context, allowEmpty: false);
            if (!Finite(s.chance) || s.chance < 0 || s.chance > 1) throw new ArgumentException(Text.Get("EconomySchema.supply_chance", pair.Key));
            if (s.merchants.Count == 0 || s.merchants.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(Text.Get("EconomySchema.merchants", pair.Key));
            foreach (string merchant in s.merchants) Merchant(merchant, pair.Key, context);
        }
        foreach (var template in pack.offerTemplates)
        {
            string where = "offerTemplates/" + template.tag;
            if (string.IsNullOrWhiteSpace(template.tag) || template.tag.Any(c => !char.IsLetterOrDigit(c))) throw new ArgumentException(Text.Get("EconomySchema.offer_tag", template.tag));
            if (template.form != "Loose" && template.form != "LooseDmg") throw new ArgumentException(Text.Get("EconomySchema.offer_form", where, template.form));
            Condition(template.condition, where);
            Chance(template.chance, where, "chance");
            Merchant(template.merchant, where, context);
        }
        foreach (var pair in pack.offers)
        {
            if (!pair.Key.StartsWith("Phobos", StringComparison.Ordinal)) throw new ArgumentException(Text.Get("EconomySchema.offer_id", pair.Key));
            var o = pair.Value;
            if (string.IsNullOrWhiteSpace(o.item)) throw new ArgumentException(Text.Get("EconomySchema.offer_item", pair.Key));
            Condition(o.condition, pair.Key);
            Chance(o.chance, pair.Key, "chance");
            if (o.quantity is int q && (q < 1 || q > MaximumQuantity)) throw new ArgumentException(Text.Get("EconomySchema.quantity", pair.Key));
            Merchant(o.merchant, pair.Key, context);
        }
        foreach (var region in pack.regions)
        {
            if (!RegionalMarkets.Known(region.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_region", region.Key));
            if (!Finite(region.Value) || region.Value < 0 || region.Value > 4) throw new ArgumentException(Text.Get("EconomySchema.region_factor", region.Key));
        }
        if (pack.regional != null)
        {
            Chance(pack.regional.baseChance, "regional", "baseChance");
            foreach (string code in pack.regional.refurbished) if (!RegionalMarkets.Known(code)) throw new ArgumentException(Text.Get("EconomySchema.unknown_region", code));
            foreach (string merchant in pack.regional.expandedMerchants) Merchant(merchant, "regional", context);
        }
        foreach (var lot in pack.lots)
        {
            if (!Kinds.Contains(lot.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", "lots", lot.Key));
            if (lot.Value < 1 || lot.Value > MaximumQuantity) throw new ArgumentException(Text.Get("EconomySchema.quantity", "lots/" + lot.Key));
        }
        foreach (var floor in pack.chanceFloors)
        {
            if (!Kinds.Contains(floor.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", "chanceFloors", floor.Key));
            if (!Finite(floor.Value) || floor.Value < 0 || floor.Value > 1) throw new ArgumentException(Text.Get("EconomySchema.chance", "chanceFloors/" + floor.Key, "value"));
        }
        foreach (var loot in pack.worldLoot)
        {
            if (string.IsNullOrWhiteSpace(loot.table)) throw new ArgumentException(Text.Get("EconomySchema.loot_table", loot.branch));
            if (!loot.branch.StartsWith("Phobos", StringComparison.Ordinal)) throw new ArgumentException(Text.Get("EconomySchema.offer_id", loot.branch));
            Chance(loot.chance, loot.branch, "chance");
            if (!Finite(loot.brokenShare) || loot.brokenShare < 0 || loot.brokenShare > 1) throw new ArgumentException(Text.Get("EconomySchema.chance", loot.branch, "brokenShare"));
        }
    }

    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    private static void Positive(double v, string where, string field) { if (!Finite(v) || v <= 0) throw new ArgumentException(Text.Get("EconomySchema.positive", where, field)); }
    private static void Chance(double v, string where, string field) { if (!Finite(v) || v <= 0 || v > 1) throw new ArgumentException(Text.Get("EconomySchema.chance", where, field)); }
    private static void Condition(string value, string where) { if (!Enum.TryParse<StockCondition>(value, false, out _)) throw new ArgumentException(Text.Get("EconomySchema.condition", where, value)); }
    private static void Merchant(string merchant, string where, EconomyContext context)
    {
        if (string.IsNullOrWhiteSpace(merchant)) throw new ArgumentException(Text.Get("EconomySchema.merchants", where));
        if (context.MerchantExists != null && !context.MerchantExists(merchant)) throw new ArgumentException(Text.Get("EconomySchema.unknown_merchant", where, merchant));
    }
    private static void Bill(Dictionary<string, int> bill, string where, string field, EconomyContext context, bool allowEmpty)
    {
        if (!allowEmpty && bill.Count == 0) throw new ArgumentException(Text.Get("EconomySchema.empty_bill", where, field));
        foreach (var item in bill)
        {
            if (item.Value < 0 || item.Value > 10000) throw new ArgumentException(Text.Get("EconomySchema.bill_count", where, field, item.Key));
            if (context.MaterialMassOf != null && context.MaterialMassOf(item.Key) == null) throw new ArgumentException(Text.Get("EconomySchema.unknown_material", where, field, item.Key));
        }
    }
}
