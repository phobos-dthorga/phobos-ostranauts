using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Thermal properties of one charge metal in the F6 model: the lining, chamber, sink and radiator
/// stay the same, so a heavier metal is a hotter, longer cycle through the same hardware. Profiles come from the
/// recipe pack's thermal entries; equal values share one instance.</summary>
public sealed class FurnaceProfile
{
    public string Id { get; }
    public double MeltK { get; }
    public double TargetK { get; }
    public double SolidCp { get; }
    public double LiquidCp { get; }
    public double LatentKJ { get; }
    public double HoldSeconds { get; }
    /// <summary>The heating interlock pressure at this profile's target: the same evacuated residual gas as the
    /// aluminium rule (0.5 kPa at 973 K) reads proportionally higher when hotter, so a hotter recipe is not
    /// refused for the gas its own pump already left behind.</summary>
    public double HotPressureKPa => FurnaceRules.HotPressureKPa * TargetK / FurnaceRules.TargetK;
    public FurnaceProfile(string id, double meltK, double targetK, double solidCp, double liquidCp, double latentKJ, double holdSeconds)
    {
        foreach (double v in new[] { meltK, targetK, solidCp, liquidCp, latentKJ, holdSeconds }) if (!ThermalMath.Finite(v) || v <= 0) throw new ArgumentException("Invalid furnace profile.");
        if (string.IsNullOrWhiteSpace(id) || meltK <= FurnaceRules.ReferenceK || targetK <= meltK) throw new ArgumentException("Invalid furnace profile.");
        Id = id; MeltK = meltK; TargetK = targetK; SolidCp = solidCp; LiquidCp = liquidCp; LatentKJ = latentKJ; HoldSeconds = holdSeconds;
    }
    private static readonly Dictionary<(double, double, double, double, double, double), FurnaceProfile> interned = new();
    /// <summary>Whether a thermal entry is the original aluminium profile in <see cref="FurnaceRules"/>: every saved
    /// batch before 0.38.0 uses those numbers, so revision 1 must keep them.</summary>
    public static bool IsAluminium(ThermalEntry t) => t.meltK == FurnaceRules.MeltK && t.targetK == FurnaceRules.TargetK && t.solidCp == FurnaceRules.SolidCp &&
        t.liquidCp == FurnaceRules.LiquidCp && t.latentKJ == FurnaceRules.LatentKJ && t.holdSeconds == FurnaceRules.HoldSeconds;
    /// <summary>The profile for a recipe's thermal entry; recipes with equal thermal values share the instance.</summary>
    public static FurnaceProfile From(ThermalEntry t, string recipeId)
    {
        var key = (t.meltK, t.targetK, t.solidCp, t.liquidCp, t.latentKJ, t.holdSeconds);
        lock (interned)
        {
            if (interned.TryGetValue(key, out var found)) return found;
            var profile = new FurnaceProfile(IsAluminium(t) ? "aluminium" : recipeId, t.meltK, t.targetK, t.solidCp, t.liquidCp, t.latentKJ, t.holdSeconds);
            interned[key] = profile;
            return profile;
        }
    }
    /// <summary>The original aluminium numbers, unchanged: every saved batch before 0.38.0 uses them.</summary>
    public static FurnaceProfile Aluminium => FurnaceRecipes.Housing.Profile;
    /// <summary>Iron's melting point (1811 K) and enthalpy of fusion (13.81 kJ/mol, 247 kJ/kg) are from the NIST
    /// Chemistry WebBook (National Institute of Standards and Technology). The 40 K superheat, the averaged
    /// solid heat capacity over the whole range and the liquid value are our simplification; the game's steel
    /// scrap is not pure iron, and no alloy chemistry is modelled.</summary>
    public static FurnaceProfile Steel => FurnaceRecipes.SteelIngots.Profile;
}

/// <summary>One immutable F6 recipe: the metal a twenty-piece charge is made of, its thermal profile and the
/// released products, which conserve the charge mass exactly. The revision is the saved identity of a batch.</summary>
public sealed class FurnaceRecipe
{
    public string Id { get; }
    public int Revision { get; }
    public string FeedId { get; }
    public FurnaceProfile Profile { get; }
    public IReadOnlyList<ProductSpec> Products { get; }
    public FurnaceRecipe(string id, int revision, string feedId, FurnaceProfile profile, IEnumerable<ProductSpec> products)
    {
        var copy = products.ToArray();
        if (string.IsNullOrWhiteSpace(id) || revision < 1 || string.IsNullOrWhiteSpace(feedId) || profile == null || copy.Length == 0 ||
            copy.Any(p => p == null || string.IsNullOrWhiteSpace(p.Id) || p.Count <= 0 || !ThermalMath.Finite(p.Kg) || p.Kg <= 0) ||
            copy.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length ||
            !ProcessMaterial.Balanced(FurnaceRules.ChargeUnits * FurnaceRules.FeedUnitKg, copy.Select(p => p.Kg * p.Count)))
            throw new ArgumentException("Invalid furnace recipe.");
        Id = id; Revision = revision; FeedId = feedId; Profile = profile; Products = Array.AsReadOnly(copy);
    }
}

/// <summary>The F6 recipe catalog, read from the <c>process-recipes</c> data pack. Revision 1 is the original
/// housing and stays exactly as shipped (frozen by hash); the ingots are the custom raw stock the owner approved
/// for Manufacturing (29 September 2026). Ingot masses, prices and stacks come from the <c>materials</c> pack.</summary>
public static class FurnaceRecipes
{
    public const string AluminiumIngot = "PhobosAluminiumIngot", SteelIngot = "PhobosSteelIngot", SteelRemainder = "PhobosSteelMeltRemainder";
    public const string SteelScrap = "ItmScrapSteel";
    public static double IngotKg => ShipbreakerMaterials.Entry(AluminiumIngot).kg;
    public static double AluminiumIngotPrice => ShipbreakerMaterials.Entry(AluminiumIngot).price;
    public static double SteelIngotPrice => ShipbreakerMaterials.Entry(SteelIngot).price;
    public static int IngotStack => ShipbreakerMaterials.Entry(AluminiumIngot).stack;
    public static int IngotsPerCharge => AluminiumIngots.Products.Single(p => p.Id == AluminiumIngot).Count;
    public static int GatesPerCharge => AluminiumIngots.Products.Single(p => p.Id == FurnaceMaterialRules.Aluminium).Count;
    /// <summary>The charge chamber admits either metal at the game level; the selected recipe decides.</summary>
    public static readonly string[] FeedConditions = { "IsAluminum", "IsSteel" };
    private static IReadOnlyList<FurnaceRecipe>? all; private static RecipePack? builtFrom;
    public static IReadOnlyList<FurnaceRecipe> All
    {
        get
        {
            var pack = ShipbreakerRecipes.Pack;
            if (all != null && ReferenceEquals(builtFrom, pack)) return all;
            builtFrom = pack;
            return all = Array.AsReadOnly(ShipbreakerRecipes.Of(ShipbreakerRecipes.Furnace).Select(p => new FurnaceRecipe(p.Key, p.Value.revision, p.Value.inputs[0].id,
                FurnaceProfile.From(p.Value.thermal!, p.Key), p.Value.products.Select(u => new ProductSpec(u.id, u.count, u.kg)))).ToArray());
        }
    }
    public static FurnaceRecipe Housing => ById("housing")!;
    public static FurnaceRecipe AluminiumIngots => ById("aluminium-ingots")!;
    public static FurnaceRecipe SteelIngots => ById("steel-ingots")!;
    public static int MaxRevision => All.Max(r => r.Revision);
    public static readonly string[] Ingots = { AluminiumIngot, SteelIngot };
    public static IReadOnlyList<string> ProductIds => Array.AsReadOnly(All.SelectMany(r => r.Products).Select(p => p.Id).Distinct(StringComparer.Ordinal).ToArray());
    public static FurnaceRecipe? ByRevision(int revision) => All.FirstOrDefault(r => r.Revision == revision);
    public static FurnaceRecipe? ById(string? id) => id == null ? null : All.FirstOrDefault(r => r.Id == id);
    /// <summary>A recipe's name on the panel: ours from the catalogue; one a data file added (Shipbreaker 0.75.0) from
    /// <c>Recipe.&lt;id&gt;</c>, which an add-on's translation file supplies, or its id.</summary>
    public static string Label(FurnaceRecipe recipe) => Text.Has("Furnace.recipe_" + recipe.Id) ? Text.Get("Furnace.recipe_" + recipe.Id) :
        Text.Has("Recipe." + recipe.Id) ? Text.Get("Recipe." + recipe.Id) : recipe.Id;
    public static string FeedLabelKey(FurnaceRecipe recipe) => recipe.FeedId == FurnaceMaterialRules.Aluminium ? "Furnace.feed_aluminium" : "Furnace.feed_steel";
}
