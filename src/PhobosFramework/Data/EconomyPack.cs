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
    /// <summary>Machines, other equipment families and assembly sections, keyed by definition prefix (the small size for a vessel ladder) or item id.</summary>
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
    /// <summary>Finite lot per successful offer, by lot name. Entries name their lot; the default is their kind.</summary>
    public Dictionary<string, int> lots = new(StringComparer.Ordinal);
    /// <summary>Minimum offer probability before the Framework availability multiplier, by floor name (<c>equipment</c>, <c>supplies</c>).</summary>
    public Dictionary<string, double> chanceFloors = new(StringComparer.Ordinal);
    /// <summary>Rare world finds in the game's own loot tables.</summary>
    public List<WorldLootEntry> worldLoot = new();

    public int Lot(string name, int fallback) => lots.TryGetValue(name, out int v) ? v : fallback;
    public double Floor(string name, double fallback) => chanceFloors.TryGetValue(name, out double v) ? v : fallback;
}

public sealed class WorkEntry
{
    public int install, uninstall, repair, dismantle;
}

public sealed class EquipmentEconomyEntry
{
    public string? notes;
    /// <summary><c>equipment</c> (a machine with the four forms) or <c>section</c> (an assembly part: no repair, sold whole).</summary>
    public string kind = EconomySchema.Equipment;
    /// <summary><c>machine</c>: Installed, Loose, InstalledDmg and LooseDmg forms under the key prefix; <c>item</c>: the key
    /// and the key plus Dmg; <c>single</c>: the key alone.</summary>
    public string forms = EconomySchema.MachineForms;
    public double price;
    /// <summary>Broken-form price; a quarter of the price when omitted.</summary>
    public double? brokenPrice;
    /// <summary>Work-progress targets. Zero install or uninstall keeps the definition's own value.</summary>
    public WorkEntry work = new();
    public Dictionary<string, int> repairBill = new(StringComparer.Ordinal);
    public Dictionary<string, int> salvage = new(StringComparer.Ordinal);
    public Dictionary<string, int> brokenSalvage = new(StringComparer.Ordinal);
    /// <summary>Restore wear-removal pace; zero keeps the native rate.</summary>
    public int restoreMinutes;
    /// <summary>Internal compartment emptied before dismantling, if any.</summary>
    public string? internalBin;
    /// <summary>Whether a loose unit may turn up in world loot.</summary>
    public bool loot = true;
    /// <summary>Whether the family carries the game's high-salvage mark.</summary>
    public bool salvageValueHigh = true;
    /// <summary>Whether the offer templates apply to this family.</summary>
    public bool offers = true;
    /// <summary>Multiplies template and regional offer chances (small, cheap parts sell more often).</summary>
    public double offerScale = 1;
    /// <summary>Regional offer chance before the region factor; null uses the regional base chance.</summary>
    public double? regionalChance;
    /// <summary>The owner adds a remainder item carrying whatever the salvage bills leave of the mass.</summary>
    public bool salvageRemainder;
    /// <summary>Lot and floor names; the kind's own names when omitted.</summary>
    public string? lot;
    public string? floor;
    public double BrokenPriceOrDefault => brokenPrice ?? price / 4;
    public string LotName => lot ?? kind;
    public string FloorName => floor ?? EconomySchema.Equipment;
    public bool IsSection => kind == EconomySchema.Section;
}

public sealed class SupplyEconomyEntry
{
    public string? notes;
    public string kind = EconomySchema.Supplies;
    public double price;
    public int repairWork, dismantleWork;
    public Dictionary<string, int> repairBill = new(StringComparer.Ordinal);
    /// <summary>Retained remainder definition returned by dismantling (its mass is the item's own).</summary>
    public string? remainder;
    public List<string> merchants = new();
    public double chance = 1;
    /// <summary>Lot and floor names; <c>supplies</c> when omitted.</summary>
    public string? lot;
    public string? floor;
    /// <summary>Whether the regional expanded merchants also carry it.</summary>
    public bool expanded;
    /// <summary>Regional offer chance before the region factor; null means no regional offer.</summary>
    public double? regionalChance;
    /// <summary><c>regional</c> follows the refurbished-region rule; otherwise a fixed stock condition.</summary>
    public string regionalCondition = EconomySchema.RegionalCondition;
    public string LotName => lot ?? EconomySchema.Supplies;
    public string FloorName => floor ?? EconomySchema.Supplies;
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
    /// <summary>Lot and floor names when the item is not one of the pack's own entries.</summary>
    public string? lot;
    public string? floor;
}

public sealed class RegionalStock
{
    public string? notes;
    public double baseChance;
    public List<string> refurbished = new();
    public List<string> expandedMerchants = new();
    /// <summary>Loose commodities offered in every region (and by the expanded merchants), keyed by item id.</summary>
    public Dictionary<string, RegionalItemEntry> items = new(StringComparer.Ordinal);
}

public sealed class RegionalItemEntry
{
    public string? notes;
    public double chance;
    public string condition = "Pristine";
    public string lot = EconomySchema.Supplies;
    public string floor = EconomySchema.Supplies;
    public bool expanded = true;
}

public sealed class WorldLootEntry
{
    public string? notes;
    public string table = "";
    /// <summary>Several tables sharing one rule (each gets its own branch, the branch id plus an underscore and the table).</summary>
    public List<string> tables = new();
    public string branch = "";
    public double chance;
    public double brokenShare;
    /// <summary>Explicit item chances instead of the spread over the loot-eligible equipment.</summary>
    public Dictionary<string, double>? items;
    public IEnumerable<string> Tables => tables.Count > 0 ? tables : new[] { table };
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
/// numbers, known materials, merchants, regions, lots and floors, and salvage that weighs what the machine weighs
/// (or no more, where the owner adds a remainder). Pricing rules (value bands, the dismantling rule) are authoring
/// rules for shipped data and are not enforced here.</summary>
public static class EconomySchema
{
    public const string Name = "economy";
    public const string Equipment = "equipment", Supplies = "supplies", Section = "section";
    public const string MachineForms = "machine", ItemForms = "item", SingleForm = "single";
    public const string RegionalCondition = "regional";
    public const int MaximumQuantity = MarketStock.MaximumOfferQuantity;
    public const double MaximumOfferScale = 4;
    public static readonly IReadOnlyList<string> Kinds = new[] { Equipment, Supplies, Section };
    public static readonly IReadOnlyList<string> Forms = new[] { MachineForms, ItemForms, SingleForm };

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
            if (e.kind != Equipment && e.kind != Section) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", pair.Key, e.kind));
            if (!Forms.Contains(e.forms)) throw new ArgumentException(Text.Get("EconomySchema.forms", pair.Key, e.forms));
            Positive(e.price, pair.Key, "price");
            if (e.brokenPrice is double broken && (!Finite(broken) || broken <= 0 || broken >= e.price)) throw new ArgumentException(Text.Get("EconomySchema.broken_price", pair.Key));
            if (e.work.install < 0 || e.work.uninstall < 0 || e.restoreMinutes < 0) throw new ArgumentException(Text.Get("EconomySchema.work", pair.Key, "install/uninstall/restoreMinutes"));
            if (e.work.dismantle <= 0 || (e.work.repair <= 0 && !e.IsSection) || e.work.repair < 0) throw new ArgumentException(Text.Get("EconomySchema.work", pair.Key, "repair/dismantle"));
            if (!Finite(e.offerScale) || e.offerScale <= 0 || e.offerScale > MaximumOfferScale) throw new ArgumentException(Text.Get("EconomySchema.offer_scale", pair.Key, MaximumOfferScale));
            if (e.regionalChance is double regional && (!Finite(regional) || regional < 0 || regional > 1)) throw new ArgumentException(Text.Get("EconomySchema.chance", pair.Key, "regionalChance"));
            bool singleForm = e.forms == SingleForm;
            Bill(e.repairBill, pair.Key, "repairBill", context, allowEmpty: e.IsSection);
            Bill(e.salvage, pair.Key, "salvage", context, allowEmpty: e.salvageRemainder);
            Bill(e.brokenSalvage, pair.Key, "brokenSalvage", context, allowEmpty: e.salvageRemainder || singleForm);
            LotAndFloor(pack, e.LotName, e.FloorName, pair.Key);
            double? mass = context.MassOf?.Invoke(pair.Key);
            if (mass is double kg && context.MaterialMassOf != null)
            {
                foreach (var (label, bill) in new[] { ("salvage", e.salvage), ("brokenSalvage", e.brokenSalvage) })
                {
                    if (bill.Count == 0) continue;
                    double total = bill.Sum(b => b.Value * (context.MaterialMassOf(b.Key) ?? 0));
                    bool wrong = e.salvageRemainder ? total > kg + context.MassToleranceKg : Math.Abs(total - kg) > context.MassToleranceKg;
                    if (wrong) throw new ArgumentException(Text.Get("EconomySchema.salvage_mass", pair.Key, label, total, kg));
                }
            }
        }
        foreach (string prefix in context.Supplies)
            if (!pack.supplies.ContainsKey(prefix)) throw new ArgumentException(Text.Get("EconomySchema.missing_equipment", prefix));
        foreach (var pair in pack.supplies)
        {
            if (!context.Supplies.Contains(pair.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_equipment", pair.Key));
            var s = pair.Value;
            if (s.kind != Supplies) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", pair.Key, s.kind));
            Positive(s.price, pair.Key, "price");
            if (s.repairWork <= 0 || s.dismantleWork <= 0) throw new ArgumentException(Text.Get("EconomySchema.work", pair.Key, "repairWork/dismantleWork"));
            Bill(s.repairBill, pair.Key, "repairBill", context, allowEmpty: false);
            if (!Finite(s.chance) || s.chance < 0 || s.chance > 1) throw new ArgumentException(Text.Get("EconomySchema.supply_chance", pair.Key));
            if (s.merchants.Count == 0 || s.merchants.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(Text.Get("EconomySchema.merchants", pair.Key));
            foreach (string merchant in s.merchants) Merchant(merchant, pair.Key, context);
            if (s.regionalChance is double regional && (!Finite(regional) || regional < 0 || regional > 1)) throw new ArgumentException(Text.Get("EconomySchema.chance", pair.Key, "regionalChance"));
            if (s.regionalCondition != RegionalCondition) Condition(s.regionalCondition, pair.Key);
            LotAndFloor(pack, s.LotName, s.FloorName, pair.Key);
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
            if (o.lot != null || o.floor != null) LotAndFloor(pack, o.lot ?? Equipment, o.floor ?? Equipment, pair.Key);
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
            foreach (var pair in pack.regional.items)
            {
                string where = "regional/items/" + pair.Key;
                if (string.IsNullOrWhiteSpace(pair.Key)) throw new ArgumentException(Text.Get("EconomySchema.offer_item", where));
                Chance(pair.Value.chance, where, "chance");
                Condition(pair.Value.condition, where);
                LotAndFloor(pack, pair.Value.lot, pair.Value.floor, where);
            }
        }
        foreach (var lot in pack.lots)
        {
            if (string.IsNullOrWhiteSpace(lot.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", "lots", lot.Key));
            if (lot.Value < 1 || lot.Value > MaximumQuantity) throw new ArgumentException(Text.Get("EconomySchema.quantity", "lots/" + lot.Key));
        }
        foreach (var floor in pack.chanceFloors)
        {
            if (string.IsNullOrWhiteSpace(floor.Key)) throw new ArgumentException(Text.Get("EconomySchema.unknown_kind", "chanceFloors", floor.Key));
            if (!Finite(floor.Value) || floor.Value < 0 || floor.Value > 1) throw new ArgumentException(Text.Get("EconomySchema.chance", "chanceFloors/" + floor.Key, "value"));
        }
        foreach (var loot in pack.worldLoot)
        {
            if (loot.Tables.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException(Text.Get("EconomySchema.loot_table", loot.branch));
            if (!loot.branch.StartsWith("Phobos", StringComparison.Ordinal)) throw new ArgumentException(Text.Get("EconomySchema.offer_id", loot.branch));
            if (loot.items != null)
            {
                double sum = 0;
                foreach (var item in loot.items)
                {
                    if (string.IsNullOrWhiteSpace(item.Key)) throw new ArgumentException(Text.Get("EconomySchema.offer_item", loot.branch));
                    Chance(item.Value, loot.branch + "/" + item.Key, "chance"); sum += item.Value;
                }
                if (loot.items.Count == 0 || sum > 1 + 1e-9) throw new ArgumentException(Text.Get("EconomySchema.loot_items", loot.branch));
            }
            else Chance(loot.chance, loot.branch, "chance");
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
    private static void LotAndFloor(EconomyPack pack, string lot, string floor, string where)
    {
        if (!pack.lots.ContainsKey(lot)) throw new ArgumentException(Text.Get("EconomySchema.unknown_lot", where, lot));
        if (!pack.chanceFloors.ContainsKey(floor)) throw new ArgumentException(Text.Get("EconomySchema.unknown_floor", where, floor));
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
