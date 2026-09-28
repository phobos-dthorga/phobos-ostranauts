using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Thermal properties of one charge metal in the F6 model: the lining, chamber, sink and radiator
/// stay the same, so a heavier metal is a hotter, longer cycle through the same hardware.</summary>
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
    /// <summary>The original aluminium numbers, unchanged: every saved batch before 0.38.0 uses them.</summary>
    public static readonly FurnaceProfile Aluminium = new("aluminium", FurnaceRules.MeltK, FurnaceRules.TargetK, FurnaceRules.SolidCp, FurnaceRules.LiquidCp, FurnaceRules.LatentKJ, FurnaceRules.HoldSeconds);
    /// <summary>Iron's melting point (1811 K) and enthalpy of fusion (13.81 kJ/mol, 247 kJ/kg) are from the NIST
    /// Chemistry WebBook (National Institute of Standards and Technology). The 40 K superheat, the averaged
    /// solid heat capacity over the whole range and the liquid value are our simplification; the game's steel
    /// scrap is not pure iron, and no alloy chemistry is modelled.</summary>
    public static readonly FurnaceProfile Steel = new("steel", 1811, 1851, .60, .82, 247, FurnaceRules.HoldSeconds);
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

/// <summary>The F6 recipe catalog. Revision 1 is the original housing and stays exactly as shipped; the
/// ingots are the custom raw stock the owner approved for Manufacturing (29 September 2026).</summary>
public static class FurnaceRecipes
{
    public const string AluminiumIngot = "PhobosAluminiumIngot", SteelIngot = "PhobosSteelIngot", SteelRemainder = "PhobosSteelMeltRemainder";
    public const string SteelScrap = "ItmScrapSteel";
    public const double IngotKg = 4, AluminiumIngotPrice = 12, SteelIngotPrice = 25;
    public const int IngotStack = 10, IngotsPerCharge = 4, GatesPerCharge = 3;
    /// <summary>The charge chamber admits either metal at the game level; the selected recipe decides.</summary>
    public static readonly string[] FeedConditions = { "IsAluminum", "IsSteel" };
    public static readonly FurnaceRecipe Housing = new("housing", FurnaceRules.RecipeRevision, FurnaceMaterialRules.Aluminium, FurnaceProfile.Aluminium,
        new[] { new ProductSpec(FurnaceRules.Blank, 1, FurnaceRules.BlankKg), new ProductSpec(FurnaceRules.Remainder, 1, FurnaceRules.RemainderKg) });
    public static readonly FurnaceRecipe AluminiumIngots = new("aluminium-ingots", 2, FurnaceMaterialRules.Aluminium, FurnaceProfile.Aluminium,
        new[] { new ProductSpec(AluminiumIngot, IngotsPerCharge, IngotKg), new ProductSpec(FurnaceMaterialRules.Aluminium, GatesPerCharge, FurnaceRules.FeedUnitKg), new ProductSpec(FurnaceRules.Remainder, 1, FurnaceRules.RemainderKg) });
    public static readonly FurnaceRecipe SteelIngots = new("steel-ingots", 3, SteelScrap, FurnaceProfile.Steel,
        new[] { new ProductSpec(SteelIngot, IngotsPerCharge, IngotKg), new ProductSpec(SteelScrap, GatesPerCharge, FurnaceRules.FeedUnitKg), new ProductSpec(SteelRemainder, 1, FurnaceRules.RemainderKg) });
    public static readonly IReadOnlyList<FurnaceRecipe> All = Array.AsReadOnly(new[] { Housing, AluminiumIngots, SteelIngots });
    public static readonly int MaxRevision = All.Max(r => r.Revision);
    public static readonly string[] Ingots = { AluminiumIngot, SteelIngot };
    public static readonly IReadOnlyList<string> ProductIds = Array.AsReadOnly(All.SelectMany(r => r.Products).Select(p => p.Id).Distinct(StringComparer.Ordinal).ToArray());
    public static FurnaceRecipe? ByRevision(int revision) => All.FirstOrDefault(r => r.Revision == revision);
    public static FurnaceRecipe? ById(string? id) => id == null ? null : All.FirstOrDefault(r => r.Id == id);
    public static string FeedLabelKey(FurnaceRecipe recipe) => recipe.FeedId == FurnaceMaterialRules.Aluminium ? "Furnace.feed_aluminium" : "Furnace.feed_steel";
}
