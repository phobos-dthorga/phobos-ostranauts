using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>A family of hand-disassembly parts the D4 takes: the loose base definitions that belong to it (the game's
/// cosmetic overlays resolve to their base), the masses it accepts and the declared, mass-conserving budget for each
/// mass. Every recoverable output is a native identity; the remainder is a terminal reject identity priced at the
/// technical minimum. These are authored gameplay budgets, not assays of the game's cosmetic materials.</summary>
public sealed class FeedFamily
{
    public string Key { get; }
    public IReadOnlyList<string> Bases { get; }
    public double MinKg { get; }
    public double MaxKg { get; }
    /// <summary>Mass resolution: 1 for whole kilograms, 0.5 where the game's variants weigh half kilograms.</summary>
    public double StepKg { get; }
    public int Revision { get; }
    private readonly Func<double, ProcessRecipeCatalog> catalogs;
    private readonly Dictionary<double, ProcessRecipeCatalog> cache = new Dictionary<double, ProcessRecipeCatalog>();

    public FeedFamily(string key, IEnumerable<string> bases, double minKg, double maxKg, double stepKg, int revision,
        Func<double, IReadOnlyList<ProductSpec>> budget, Func<double, ProcessRecipeCatalog>? catalogs = null)
    {
        if (string.IsNullOrWhiteSpace(key) || minKg <= 0 || maxKg < minKg || stepKg <= 0 || revision < 1) throw new ArgumentException("Invalid feed family.");
        Key = key; Bases = Array.AsReadOnly(bases.ToArray()); MinKg = minKg; MaxKg = maxKg; StepKg = stepKg; Revision = revision;
        this.catalogs = catalogs ?? (kg => new ProcessRecipeCatalog(revision, new[] { new ProcessRecipe(revision, kg, budget(kg)) }));
    }

    public double Snap(double kg) => Math.Round(kg / StepKg) * StepKg;
    /// <summary>A mass the family has a recipe for: finite, on the family's step and inside its range.</summary>
    public bool Accepts(double kg)
    {
        if (double.IsNaN(kg) || double.IsInfinity(kg)) return false;
        double snapped = Snap(kg);
        return ProcessRules.MassMatches(kg, snapped) && snapped >= MinKg - ProcessRules.MassTolerance && snapped <= MaxKg + ProcessRules.MassTolerance;
    }
    /// <summary>The catalog for a part of this mass; one immutable catalog per accepted mass.</summary>
    public ProcessRecipeCatalog Catalog(double kg)
    {
        if (!Accepts(kg)) throw new ArgumentException(Text.Get("ProcessRecipes.unsupported_family_mass", Key, kg, MinKg, MaxKg));
        double snapped = Snap(kg);
        lock (cache)
        {
            if (cache.TryGetValue(snapped, out var existing)) return existing;
            var catalog = catalogs(snapped);
            cache[snapped] = catalog;
            return catalog;
        }
    }
    /// <summary>Every mass the family accepts, on its step.</summary>
    public IEnumerable<double> AcceptedMasses()
    {
        int steps = (int)Math.Round((MaxKg - MinKg) / StepKg);
        for (int i = 0; i <= steps; i++) yield return Math.Round(MinKg + i * StepKg, 6);
    }
}

public static class FeedFamilies
{
    // Terminal remainders: one identity per family, never re-processed, technical minimum price.
    public const string FloorReject = "PhobosFloorRejectR1", DuraWalReject = "PhobosDuraWalRejectR1", WindowReject = "PhobosWindowRejectR1";
    public const string WhippleReject = "PhobosWhippleRejectR1", AeroReject = "PhobosAeroRejectR1";
    public const double FloorRejectKg = 1, DuraWalRejectKg = 9.6, WindowRejectKg = 6, WhippleRejectKg = 1.5, AeroRejectKg = 2.1;
    public const int FloorRejectStack = 10;
    public const string Plastic = "ItmScrapPlastic", Parts = "ItmPartsMechSmall01", Aluminium = "ItmScrapAluminum", Steel = "ItmScrapSteel";
    public const double PlasticKg = .3, PartsKg = .5;

    /// <summary>The ordinary wall: revisions 1 and 2 exactly as shipped (ProcessRecipes owns that catalog).</summary>
    public static readonly FeedFamily Wall = new FeedFamily("wall", new[] { ProcessRules.Wall }, ProcessRules.MinimumWallKg, ProcessRules.MaximumWallKg, 1, 2,
        kg => ProcessRecipes.WallProducts((int)Math.Round(kg)), ProcessRecipes.ForWallMass);
    /// <summary>Floor grates, 3 to 13 kg by half kilograms across the game's variants: two steel, one aluminium, a
    /// parts unit when the mass has a half, and one-kilogram reject units for the rest.</summary>
    public static readonly FeedFamily Floor = new FeedFamily("floor", new[] { "ItmFloorGrate01Loose" }, 3, 13, .5, 1, kg =>
    {
        var products = new List<ProductSpec> { new ProductSpec(Steel, 2, 1), new ProductSpec(Aluminium, 1, 1) };
        double remaining = kg - 3;
        if (Math.Abs(remaining - Math.Floor(remaining) - .5) < 1e-9) { products.Add(new ProductSpec(Parts, 1, PartsKg)); remaining -= PartsKg; }
        int rejects = (int)Math.Round(remaining);
        if (rejects > 0) products.Add(new ProductSpec(FloorReject, rejects, FloorRejectKg));
        return products;
    });
    /// <summary>DuraWal plastic interior wall, 14 kg: a little plastic, aluminium, parts and steel; most of it is
    /// laminated cladding the fixture cannot separate.</summary>
    public static readonly FeedFamily DuraWal = new FeedFamily("durawal", new[] { "ItmWallPlastic1x1Loose" }, 14, 14, 1, 1, _ => new[] {
        new ProductSpec(Plastic, 3, PlasticKg), new ProductSpec(Aluminium, 2, 1), new ProductSpec(Parts, 1, PartsKg),
        new ProductSpec(Steel, 1, 1), new ProductSpec(DuraWalReject, 1, DuraWalRejectKg) });
    /// <summary>Window panels, 10 kg: frame metal and fittings; glazing and seals are the remainder (the game has no glass item).</summary>
    public static readonly FeedFamily Window = new FeedFamily("window", new[] { "ItmWallWindow1x1Loose", "ItmWallWindow1x1SqLoose" }, 10, 10, 1, 1, _ => new[] {
        new ProductSpec(Aluminium, 2, 1), new ProductSpec(Parts, 2, PartsKg), new ProductSpec(Steel, 1, 1), new ProductSpec(WindowReject, 1, WindowRejectKg) });
    /// <summary>Whipple shielding framework, 5 kg: mostly aluminium.</summary>
    public static readonly FeedFamily Whipple = new FeedFamily("whipple", new[] { "ItmWallThin1x1Loose" }, 5, 5, 1, 1, _ => new[] {
        new ProductSpec(Aluminium, 3, 1), new ProductSpec(Parts, 1, PartsKg), new ProductSpec(WhippleReject, 1, WhippleRejectKg) });
    /// <summary>Aerodynamic panels, 4 to 6 kg: plastic skin, aluminium for every kilogram above the skin, a fixed remainder.</summary>
    public static readonly FeedFamily Aero = new FeedFamily("aero", new[] { "ItmWallAero1x1Loose", "ItmWallAero1x1SqLoose", "ItmWallAero1x2SlantLoose",
        "ItmWallAero1x2SlantBLoose", "ItmWallAero1x3SlantLoose", "ItmWallAero1x3SlantBLoose" }, 4, 6, 1, 1, kg => new[] {
        new ProductSpec(Plastic, 3, PlasticKg), new ProductSpec(Aluminium, (int)Math.Round(kg) - 3, 1), new ProductSpec(AeroReject, 1, AeroRejectKg) });

    public static readonly IReadOnlyList<FeedFamily> All = Array.AsReadOnly(new[] { Wall, Floor, DuraWal, Window, Whipple, Aero });
    public static readonly IReadOnlyDictionary<string, double> RejectKg = new Dictionary<string, double>(StringComparer.Ordinal) {
        [FloorReject] = FloorRejectKg, [DuraWalReject] = DuraWalRejectKg, [WindowReject] = WindowRejectKg, [WhippleReject] = WhippleRejectKg, [AeroReject] = AeroRejectKg };
    public static readonly IReadOnlyDictionary<string, string> RejectFamily = new Dictionary<string, string>(StringComparer.Ordinal) {
        [FloorReject] = "floor", [DuraWalReject] = "durawal", [WindowReject] = "window", [WhippleReject] = "whipple", [AeroReject] = "aero" };
    public static bool IsReject(string? id) => id != null && RejectKg.ContainsKey(id);
    public static FeedFamily? Find(string? baseDefinition) => baseDefinition == null ? null : All.FirstOrDefault(f => f.Bases.Contains(baseDefinition));
    private static IReadOnlyList<ProductSpec>? products;
    /// <summary>Every product any family can deliver at any accepted mass, including historic wall revisions, once per (id, kg).</summary>
    public static IReadOnlyList<ProductSpec> AllProducts()
    {
        if (products != null) return products;
        var all = All.SelectMany(f => f.AcceptedMasses().SelectMany(kg => f.Catalog(kg).Recipes).SelectMany(r => r.Products))
            .GroupBy(p => (p.Id, p.Kg)).Select(g => g.First()).OrderBy(p => p.Id, StringComparer.Ordinal).ThenBy(p => p.Kg).ToArray();
        products = Array.AsReadOnly(all);
        return products;
    }
    public static bool IsProduct(string? id) => id != null && AllProducts().Any(p => p.Id == id);
}
