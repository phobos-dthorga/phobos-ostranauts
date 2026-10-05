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
    /// <summary>How the room's carbon dioxide speeds growth (Agriculture 0.43.0); absent means no response.</summary>
    public Co2ResponseEntry? co2Response;
    /// <summary>The growing room, stress rules and W2 feeding figures (Agriculture 0.55.0); absent means the defaults.</summary>
    public GrowthEntry? growth;
}

public sealed class Co2ResponseEntry
{
    public string? notes;
    /// <summary>Points of (carbon dioxide partial pressure in kPa, growth factor), in rising pressure. Between points the
    /// factor is interpolated; below the first and above the last it holds the end value.</summary>
    public List<double[]> points = new();
}

/// <summary>The growth factor a room's carbon dioxide gives (Agriculture 0.43.0). The factor multiplies how much a crop
/// grows per hour and per kWh; every budget per unit of growth stays the crop's own, so mass is conserved and a richer
/// room only shortens the cycle. All shipped crops are C3 plants, so one curve serves them all.</summary>
public static class Co2Response
{
    public const double MinFactor = .5, MaxFactor = 2, MaxKPa = 10;
    /// <summary>The factor at <paramref name="kPa"/> on <paramref name="points"/>; 1 with no curve or no reading.</summary>
    public static double Factor(IReadOnlyList<double[]>? points, double kPa)
    {
        if (points == null || points.Count == 0 || !CropState.Finite(kPa) || kPa < 0) return 1;
        if (kPa <= points[0][0]) return points[0][1];
        for (int i = 1; i < points.Count; i++)
            if (kPa <= points[i][0])
            {
                double t = (kPa - points[i - 1][0]) / (points[i][0] - points[i - 1][0]);
                return points[i - 1][1] + t * (points[i][1] - points[i - 1][1]);
            }
        return points[points.Count - 1][1];
    }
    public static double Factor(double kPa) => Factor(Crops.Pack.co2Response?.points, kPa);
    public static void Validate(Co2ResponseEntry? entry)
    {
        if (entry == null) return;
        if (entry.points.Count < 1 || entry.points.Count > 16) throw new ArgumentException(Text.Get("crops_co2"));
        double last = -1;
        foreach (var p in entry.points)
        {
            if (p == null || p.Length != 2 || !CropState.Finite(p[0]) || !CropState.Finite(p[1]) || p[0] <= last || p[0] < 0 || p[0] > MaxKPa || p[1] < MinFactor || p[1] > MaxFactor)
                throw new ArgumentException(Text.Get("crops_co2"));
            last = p[0];
        }
    }
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
    /// <summary>Picks a ripe plant allows before its final harvest (Agriculture 0.42.0); zero or absent for a crop
    /// harvested once. Each pick takes up to <see cref="pickKg"/> of whole portions and leaves the plant growing.</summary>
    public int picks;
    public double pickKg;
    /// <summary>The item planted, and the item each portion becomes.</summary>
    public string stock = "", produce = "";
    /// <summary>Old names, kept to read old saves (Agriculture 0.55.0): the feed a W2 mixed for this crop until 0.54.0,
    /// and the name that feed was held under in irrigation conduit and drain canisters. Nothing mixes a feed any more;
    /// a new crop leaves both out.</summary>
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
    /// <summary>Whether an item is one a data file added to the materials pack (Agriculture 0.48.0): it carries its own
    /// name there, so its crop item entry needs no text key.</summary>
    public Func<string, bool>? AddedItem { get; set; }
}

public static class CropSchema
{
    public const string Name = "crops";
    public const double MassTolerance = 1e-9, MaxHours = 10000, MaxKW = 1.5;
    public const int MaxPicks = 10;
    /// <summary>Carbon dioxide taken in less oxygen given off, per kilogram of carbon fixed as CH2O: (44 - 32) / 30.</summary>
    public const double NetGasPerCarbon = 12d / 30;
    public static void Validate(CropPack pack, CropContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (pack.crops.Count == 0) throw new ArgumentException(Text.Get("crops_empty"));
        Co2Response.Validate(pack.co2Response);
        Growth.Validate(pack.growth, pack.crops.Keys);
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
            // A pick takes whole portions, less than one cycle's growth, so the plant can regrow what was picked.
            if (c.picks < 0 || c.picks > MaxPicks || (c.picks == 0) != (c.pickKg == 0) ||
                c.picks > 0 && (!CropState.Finite(c.pickKg) || c.pickKg < c.portionKg || c.pickKg > c.edibleKg || c.pickKg >= c.finalKg - c.seedKg))
                throw new ArgumentException(Text.Get("crops_picks", id, MaxPicks));
            // The old feed names are optional since 0.55.0; one that is given must still be its crop's alone.
            if (c.feed == null || c.feed.Length > 0 && (string.IsNullOrWhiteSpace(c.feed) || c.feed == LegacyFeed.Water || !feeds.Add(c.feed))) throw new ArgumentException(Text.Get("crops_feed", id, c.feed ?? ""));
            if (c.feedCommodity == null || c.feedCommodity.Length > 0 && (string.IsNullOrWhiteSpace(c.feedCommodity) || c.feedCommodity == LegacyFeed.Water || !commodities.Add(c.feedCommodity))) throw new ArgumentException(Text.Get("crops_feed", id, c.feedCommodity ?? ""));
            if (string.IsNullOrWhiteSpace(c.art) || c.art.Any(ch => !char.IsLetterOrDigit(ch)) || context.KnownArt?.Invoke(c.art) == false) throw new ArgumentException(Text.Get("crops_art", id));
        }
        foreach (var pair in pack.items)
        {
            var item = pair.Value;
            bool added = context.AddedItem?.Invoke(pair.Key) == true;
            if (context.KnownItem?.Invoke(pair.Key) == false || (added ? item.text.Length != 0 : string.IsNullOrWhiteSpace(item.text) || context.KnownText?.Invoke(item.text) == false))
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
    private static Dictionary<string, Crop> byId = new(StringComparer.Ordinal), byFeed = new(StringComparer.Ordinal), byFeedCommodity = new(StringComparer.Ordinal);
    private static Crop[] all = Array.Empty<Crop>();
    public static CropPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, CropSchema.Name, typeof(Crops).Assembly, Resource);
    /// <summary>Every crop, in the pack's order (the order of planting actions, feeds and crew recipes).</summary>
    public static IReadOnlyList<Crop> All { get { _ = Pack; return all; } }
    /// <summary>The crop item ids of the shipped pack, from its text alone: the crop items that are the mod's own. An
    /// item a data file adds (Agriculture 0.48.0) is in the materials pack as an addition, with its own name.</summary>
    public static IReadOnlyList<string> ShippedItemIds() =>
        (DataPacks.Parse(DataPacks.ShippedText(Source), CropSchema.Name, shipped: true)["items"] as JObject)?.Properties().Select(p => p.Name).ToArray() ?? Array.Empty<string>();
    /// <summary>What the rest of the mod knows about a crop's items: the materials pack's masses and the text catalogue.</summary>
    public static CropContext Known() => new()
    {
        UnitMassOf = AgricultureMaterials.KgOf, KnownItem = id => AgricultureMaterials.KgOf(id) != null,
        KnownText = key => Text.Has(key) && Text.Has(key + "_desc"), AddedItem = AgricultureMaterials.IsAdded,
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
        byFeed = crops.Where(c => c.Feed.Length > 0).ToDictionary(c => c.Feed, StringComparer.Ordinal);
        byFeedCommodity = crops.Where(c => c.FeedCommodity.Length > 0).ToDictionary(c => c.FeedCommodity, StringComparer.Ordinal);
    }
    public static Crop? Find(string? id) { _ = Pack; return id != null && byId.TryGetValue(id, out var c) ? c : null; }
    public static Crop? ByFeed(string? feed) { _ = Pack; return feed != null && byFeed.TryGetValue(feed, out var c) ? c : null; }
    public static Crop? ByFeedCommodity(string? name) { _ = Pack; return name != null && byFeedCommodity.TryGetValue(name, out var c) ? c : null; }
    /// <summary>The crop items in the pack's order, with their text and food values.</summary>
    public static IEnumerable<KeyValuePair<string, CropItemEntry>> Items => Pack.items;
    public static bool IsStock(string? id) => id != null && All.Any(c => c.Stock == id);
    public static bool IsProduce(string? id) => id != null && All.Any(c => c.Produce == id);
}
