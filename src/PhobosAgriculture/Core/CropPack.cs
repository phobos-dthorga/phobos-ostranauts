using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture.Core;

/// <summary>The <c>crops</c> schema (Agriculture 0.40.0; owner decision, 4 October 2026: crops and cooker recipes are
/// data before any crop is added). One entry per crop, keyed by the name a saved planting stores, plus the crop items
/// the code builds. Every figure the growth model used to hold in code is here; the model itself stays in
/// <see cref="CropState"/>.</summary>
public sealed class CropPack : DataPack
{
    public Dictionary<string, CropEntry> crops = new(StringComparer.Ordinal);
    public Dictionary<string, CropItemEntry> items = new(StringComparer.Ordinal);
}

public sealed class CropEntry
{
    public string? notes;
    /// <summary>A plain name for a crop the mod has no wording for (one a player file adds); shipped crops use the
    /// translation catalogue and leave this out.</summary>
    public string? name;
    /// <summary>Lit hours to harvest at ordinary pace, and the rack's electrical demand while growing.</summary>
    public double hours, kw;
    /// <summary>Planted mass and harvest-ready mass of the cohort, in kilograms.</summary>
    public double seedKg, finalKg;
    /// <summary>Carbon fixed over the cycle (as CH2O), nutrient and water taken up, and water transpired.</summary>
    public double carbonKg, nutrientKg, waterKg, vapourKg;
    /// <summary>Carbon the planting stock already holds.</summary>
    public double seedCarbonKg;
    /// <summary>Of a full healthy cohort: the edible mass, planting stock kept back from it, and one portion.</summary>
    public double edibleKg, keptStockKg, portionKg;
    /// <summary>The item planted, and the item each portion becomes.</summary>
    public string stock = "", produce = "";
    /// <summary>The feed formulation a W2 mixes for this crop (a saved name), and the name that feed is held under in
    /// irrigation conduit and drain canisters (also saved).</summary>
    public string feed = "", feedCommodity = "";
    /// <summary>The artwork family of its growth stages (<c>Rack-&lt;art&gt;-&lt;stage&gt;</c>).</summary>
    public string art = "";
}

public sealed class CropItemEntry
{
    public string? notes;
    /// <summary>The translation key of the item's name; its description is that key plus <c>_desc</c>.</summary>
    public string text = "";
    /// <summary>What eating one gives, for food: hunger relieved and satiety gained, in the game's own units.</summary>
    public int? hunger, satiety;
}

/// <summary>What the owner knows that the file cannot: which items exist, and the rack's limits.</summary>
public sealed class CropContext
{
    /// <summary>The unit mass of a loose item, or null for an id that is not known (an offline check skips it).</summary>
    public Func<string, double?>? UnitMassOf { get; set; }
    public Func<string, bool>? KnownItem { get; set; }
    public Func<string, bool>? KnownText { get; set; }
    /// <summary>Whether growth-stage artwork ships under this name. A crop a player adds borrows a shipped crop's stages.</summary>
    public Func<string, bool>? KnownArt { get; set; }
}

public static class CropSchema
{
    public const string Name = "crops";
    public const double MassTolerance = 1e-9, MaxHours = 10000, MaxKW = 1.5;
    /// <summary>Carbon dioxide taken in less oxygen given off, per kilogram of carbon fixed as CH2O: (44 - 32) / 30.</summary>
    public const double NetGasPerCarbon = 12d / 30;
    public static void Validate(CropPack pack, CropContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (pack.crops.Count == 0) throw new ArgumentException(Text.Get("crops_empty"));
        var feeds = new HashSet<string>(StringComparer.Ordinal); var commodities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in pack.crops)
        {
            string id = pair.Key; var c = pair.Value;
            if (string.IsNullOrWhiteSpace(id) || id.Any(ch => !(char.IsLower(ch) || char.IsDigit(ch) || ch == '-')) || id == "empty") throw new ArgumentException(Text.Get("crops_id", id));
            foreach (double v in new[] { c.hours, c.kw, c.seedKg, c.finalKg, c.carbonKg, c.nutrientKg, c.waterKg, c.portionKg, c.edibleKg })
                if (!CropState.Finite(v) || v <= 0) throw new ArgumentException(Text.Get("crops_positive", id));
            foreach (double v in new[] { c.vapourKg, c.seedCarbonKg, c.keptStockKg })
                if (!CropState.Finite(v) || v < 0) throw new ArgumentException(Text.Get("crops_positive", id));
            if (c.hours > MaxHours || c.kw > MaxKW) throw new ArgumentException(Text.Get("crops_limits", id, MaxHours, MaxKW));
            if (c.name != null && (string.IsNullOrWhiteSpace(c.name) || c.name.Length > 40 || c.name.Any(char.IsControl))) throw new ArgumentException(Text.Get("crops_name", id));
            if (c.seedKg >= c.finalKg || c.seedCarbonKg > c.seedKg || c.seedCarbonKg + c.carbonKg > c.finalKg + MassTolerance)
                throw new ArgumentException(Text.Get("crops_carbon", id));
            // Mass closes: what the cohort gains is the water and nutrient it takes up and the net gas it fixes, less what it transpires.
            double gained = c.waterKg + c.nutrientKg + c.carbonKg * NetGasPerCarbon - c.vapourKg, grown = c.finalKg - c.seedKg;
            if (Math.Abs(gained - grown) > MassTolerance) throw new ArgumentException(Text.Get("crops_mass", id, gained, grown));
            if (c.waterKg > CropState.ReservoirKg || c.nutrientKg > CropState.NutrientCapacityKg) throw new ArgumentException(Text.Get("crops_rack", id, CropState.ReservoirKg, CropState.NutrientCapacityKg));
            if (c.edibleKg > c.finalKg + MassTolerance || c.keptStockKg > c.edibleKg || c.portionKg > c.edibleKg) throw new ArgumentException(Text.Get("crops_harvest", id));
            foreach (string item in new[] { c.stock, c.produce })
            {
                if (string.IsNullOrWhiteSpace(item) || context.KnownItem?.Invoke(item) == false) throw new ArgumentException(Text.Get("crops_item", id, item));
            }
            // What is planted is one unit of stock, and each portion is one unit of produce.
            if (context.UnitMassOf?.Invoke(c.stock) is double stockKg && Math.Abs(stockKg - c.seedKg) > MassTolerance) throw new ArgumentException(Text.Get("crops_unit", id, c.stock, c.seedKg, stockKg));
            if (context.UnitMassOf?.Invoke(c.produce) is double produceKg && Math.Abs(produceKg - c.portionKg) > MassTolerance) throw new ArgumentException(Text.Get("crops_unit", id, c.produce, c.portionKg, produceKg));
            if (c.keptStockKg > 0 && Math.Abs(c.keptStockKg - c.seedKg) > MassTolerance) throw new ArgumentException(Text.Get("crops_kept", id));
            if (string.IsNullOrWhiteSpace(c.feed) || c.feed == NutrientSolution.None || !feeds.Add(c.feed)) throw new ArgumentException(Text.Get("crops_feed", id, c.feed));
            if (string.IsNullOrWhiteSpace(c.feedCommodity) || c.feedCommodity == "water" || !commodities.Add(c.feedCommodity)) throw new ArgumentException(Text.Get("crops_feed", id, c.feedCommodity));
            if (string.IsNullOrWhiteSpace(c.art) || c.art.Any(ch => !char.IsLetterOrDigit(ch)) || context.KnownArt?.Invoke(c.art) == false) throw new ArgumentException(Text.Get("crops_art", id));
        }
        foreach (var pair in pack.items)
        {
            var item = pair.Value;
            if (context.KnownItem?.Invoke(pair.Key) == false || string.IsNullOrWhiteSpace(item.text) || context.KnownText?.Invoke(item.text) == false)
                throw new ArgumentException(Text.Get("crops_item_entry", pair.Key));
            if ((item.hunger == null) != (item.satiety == null) || item.hunger is int h && (h < 1 || h > 20) || item.satiety is int s && (s < 1 || s > 20))
                throw new ArgumentException(Text.Get("crops_food", pair.Key));
        }
        foreach (var c in pack.crops)
            foreach (string item in new[] { c.Value.stock, c.Value.produce })
                if (!pack.items.ContainsKey(item)) throw new ArgumentException(Text.Get("crops_item", c.Key, item));
    }
}

/// <summary>Published crops are immutable: a saved planting stores only its crop's name and reads everything else
/// back from the pack. <c>frozen-crops.json</c> (written by <c>scripts/freeze-recipes.py</c>) holds a hash of every
/// published crop entry, in the same canonical form as recipe revisions; a shipped or player entry whose crop is
/// frozen must hash the same, and a frozen crop may never disappear. Changing a crop means adding one beside it.</summary>
public static class CropFreeze
{
    public static void Enforce(JObject rawPack, RecipeFreeze.Frozen frozen)
    {
        if (rawPack == null) throw new ArgumentNullException(nameof(rawPack));
        if (frozen == null) throw new ArgumentNullException(nameof(frozen));
        var entries = rawPack["crops"] as JObject ?? new JObject();
        foreach (var pair in frozen.revisions)
        {
            if (entries[pair.Key] is not JObject entry) throw new ArgumentException(Text.Get("crops_frozen_missing", pair.Key));
            if (!string.Equals(RecipeFreeze.Hash(entry), pair.Value, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(Text.Get("crops_frozen_changed", pair.Key));
        }
    }
}

/// <summary>Agriculture's crops, from the <c>crops</c> data pack (<c>framework/crops.json</c>) and player files in
/// <c>BepInEx/config/PhobosAgriculture/crops</c>. Loaded on first use (offline checks) and again at each content load.</summary>
public static class Crops
{
    public const string ModFolder = "PhobosAgriculture", Resource = "PhobosAgriculture.crops.json", FrozenResource = "PhobosAgriculture.frozen-crops.json";
    private static CropPack? pack;
    private static Dictionary<string, Crop> byId = new(StringComparer.Ordinal), byFeed = new(StringComparer.Ordinal);
    private static Crop[] all = Array.Empty<Crop>();
    public static CropPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, CropSchema.Name, typeof(Crops).Assembly, Resource);
    /// <summary>Every crop, in the pack's order (the order of planting actions, feeds and crew recipes).</summary>
    public static IReadOnlyList<Crop> All { get { _ = Pack; return all; } }
    /// <summary>The crop item ids of the shipped pack, from its text alone. A player file cannot add an item (the
    /// materials pack would not know it), so these are all the crop items there can be.</summary>
    public static IReadOnlyList<string> ShippedItemIds() =>
        (DataPacks.Parse(DataPacks.ShippedText(Source), CropSchema.Name, shipped: true)["items"] as JObject)?.Properties().Select(p => p.Name).ToArray() ?? Array.Empty<string>();
    /// <summary>What the rest of the mod knows about a crop's items: the materials pack's masses and the text catalogue.</summary>
    public static CropContext Known() => new()
    {
        UnitMassOf = AgricultureMaterials.KgOf, KnownItem = id => AgricultureMaterials.KgOf(id) != null,
        KnownText = key => Text.Has(key) && Text.Has(key + "_desc"),
        KnownArt = new HashSet<string>(ShippedArt(), StringComparer.Ordinal).Contains
    };
    /// <summary>The artwork families the shipped crops use; their growth-stage images are in the package.</summary>
    public static IReadOnlyList<string> ShippedArt() =>
        (DataPacks.Parse(DataPacks.ShippedText(Source), CropSchema.Name, shipped: true)["crops"] as JObject)?.Properties()
            .Select(p => (string?)p.Value["art"] ?? "").Where(a => a.Length > 0).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
    public static CropPack Load(CropContext? context = null)
    {
        var frozen = RecipeFreeze.Read(typeof(Crops).Assembly, FrozenResource);
        var use = context ?? Known();
        var loaded = DataPacks.Load<CropPack>(Source, (p, raw) => { CropSchema.Validate(p, use); CropFreeze.Enforce(raw, frozen); });
        Use(loaded);
        return loaded;
    }
    /// <summary>Adopts a validated pack (the loader, and checks that build one from text).</summary>
    public static void Use(CropPack loaded)
    {
        var crops = loaded.crops.Select(p => new Crop(p.Key, p.Value)).ToArray();
        pack = loaded; all = crops;
        byId = crops.ToDictionary(c => c.Id, StringComparer.Ordinal);
        byFeed = crops.ToDictionary(c => c.Feed, StringComparer.Ordinal);
    }
    public static Crop? Find(string? id) { _ = Pack; return id != null && byId.TryGetValue(id, out var c) ? c : null; }
    public static Crop? ByFeed(string? feed) { _ = Pack; return feed != null && byFeed.TryGetValue(feed, out var c) ? c : null; }
    /// <summary>The crop items in the pack's order, with their text and food values.</summary>
    public static IEnumerable<KeyValuePair<string, CropItemEntry>> Items => Pack.items;
    public static bool IsStock(string? id) => id != null && All.Any(c => c.Stock == id);
    public static bool IsProduce(string? id) => id != null && All.Any(c => c.Produce == id);
}
