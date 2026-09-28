using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

/// <summary>The declared, mass-conserving budgets for the game's structural part families. Native base prices
/// pinned here come from condowners.json; the native suite audits every cosmetic variant against live data.</summary>
internal static class FeedFamilyChecks
{
    private static readonly Dictionary<string, double> UnitPrice = new(StringComparer.Ordinal) {
        ["ItmScrapSteel"] = 3.6, ["ItmScrapAluminum"] = 1.1, ["ItmScrapCarbonFiber"] = .12, ["ItmScrapPlastic"] = 46, ["ItmPartsMechSmall01"] = 5,
        ["PhobosPanelResidueR2"] = .01, ["PhobosShipbreakerResidue"] = .01 };
    private static readonly Dictionary<string, double> WholePrice = new(StringComparer.Ordinal) {
        ["floor"] = 15, ["durawal"] = 210, ["window"] = 330, ["whipple"] = 88, ["aero"] = 500 };

    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        double Value(IEnumerable<ProductSpec> products) => products.Sum(p => p.Count * (UnitPrice.TryGetValue(p.Id, out var price) ? price : .01));
        check(FeedFamilies.All.Select(f => f.Key).SequenceEqual(new[] { "wall", "floor", "durawal", "window", "whipple", "aero" }), "Six feed families in a fixed order");
        check(FeedFamilies.All.SelectMany(f => f.Bases).Distinct().Count() == FeedFamilies.All.Sum(f => f.Bases.Count), "No base definition belongs to two families");
        foreach (var family in FeedFamilies.All)
        {
            foreach (double kg in family.AcceptedMasses())
            {
                var catalog = family.Catalog(kg);
                check(catalog.Current.Revision == family.Revision && ProcessRules.MassMatches(catalog.Current.InputKg, kg), $"{family.Key} {kg} kg catalog is the family's current revision at that mass");
                check(ProcessRules.Balanced(kg, catalog.Current.Products.SelectMany(p => Enumerable.Repeat(p.Kg, p.Count))), $"{family.Key} {kg} kg budget conserves the whole part");
                check(catalog.Current.Products.Select(p => p.Id).Distinct().Count() == catalog.Current.Products.Count, $"{family.Key} {kg} kg products are distinct identities");
                check(ReferenceEquals(catalog, family.Catalog(kg)), $"{family.Key} {kg} kg: one catalog per accepted mass");
                check(catalog.Current.Products.Sum(p => p.Count) <= 64, $"{family.Key} {kg} kg batch fits an empty 8 x 8 tray unstacked");
                if (family.Key != "wall")
                {
                    check(catalog.Current.Products.Count(p => FeedFamilies.IsReject(p.Id)) <= 1 && catalog.Current.Products.All(p => !p.Id.StartsWith("Phobos", StringComparison.Ordinal) || FeedFamilies.IsReject(p.Id)),
                        $"{family.Key} {kg} kg: native identities for everything recoverable, one terminal reject at most");
                    check(catalog.Recipes.Count == 1, $"{family.Key} has a single shipped revision");
                }
            }
            foreach (double kg in new[] { family.MinKg - family.StepKg, family.MaxKg + family.StepKg, family.MinKg + family.StepKg / 2, 0, -1, double.NaN, double.PositiveInfinity })
            {
                check(!family.Accepts(kg), $"{family.Key} refuses {kg} kg");
                throws(() => family.Catalog(kg), $"{family.Key} has no catalog for {kg} kg");
            }
            check(family.Accepts(family.MinKg + 1e-9) && !family.Accepts(family.MinKg + .01), $"{family.Key} snaps to its step within the shared mass tolerance only");
        }
        // The ordinary wall keeps its shipped catalogs exactly.
        check(ReferenceEquals(FeedFamilies.Wall.Catalog(24), ProcessRecipes.WallPanels) && ReferenceEquals(FeedFamilies.Wall.Catalog(48), ProcessRecipes.ForWallMass(48)),
            "The wall family delegates to the shipped wall catalogs");
        check(FeedFamilies.Find(ProcessRules.Wall) == FeedFamilies.Wall && FeedFamilies.Find("ItmFloorGrate01Loose") == FeedFamilies.Floor &&
            FeedFamilies.Find("ItmWallPlastic1x1Loose") == FeedFamilies.DuraWal && FeedFamilies.Find("ItmWallWindow1x1SqLoose") == FeedFamilies.Window &&
            FeedFamilies.Find("ItmWallThin1x1Loose") == FeedFamilies.Whipple && FeedFamilies.Find("ItmWallAero1x3SlantBLoose") == FeedFamilies.Aero &&
            FeedFamilies.Find("ItmDoor01ClosedLoose") == null && FeedFamilies.Find("ItmConduit00Loose") == null && FeedFamilies.Find("ItmFloorGrate4x401Loose") == null && FeedFamilies.Find(null) == null,
            "Families are found by loose base definition; doors, conduit and the turbine lifter belong to none");
        // Exact budgets.
        (string, int, double)[] Products(FeedFamily f, double kg) => f.Catalog(kg).Current.Products.Select(p => (p.Id, p.Count, p.Kg)).ToArray();
        check(Products(FeedFamilies.Floor, 6.5).SequenceEqual(new[] { ("ItmScrapSteel", 2, 1.0), ("ItmScrapAluminum", 1, 1.0), ("ItmPartsMechSmall01", 1, .5), (FeedFamilies.FloorReject, 3, 1.0) }),
            "A plain 6.5 kg floor grate: two steel, one aluminium, one parts unit, three reject units");
        check(Products(FeedFamilies.Floor, 3).SequenceEqual(new[] { ("ItmScrapSteel", 2, 1.0), ("ItmScrapAluminum", 1, 1.0) }), "The lightest 3 kg floor leaves no reject");
        check(Products(FeedFamilies.Floor, 13).Single(p => p.Item1 == FeedFamilies.FloorReject).Item2 == 10, "A 13 kg Langdon-Phillips floor leaves ten reject units");
        check(Products(FeedFamilies.DuraWal, 14).SequenceEqual(new[] { ("ItmScrapPlastic", 3, .3), ("ItmScrapAluminum", 2, 1.0), ("ItmPartsMechSmall01", 1, .5), ("ItmScrapSteel", 1, 1.0), (FeedFamilies.DuraWalReject, 1, 9.6) }), "DuraWal budget");
        check(Products(FeedFamilies.Window, 10).SequenceEqual(new[] { ("ItmScrapAluminum", 2, 1.0), ("ItmPartsMechSmall01", 2, .5), ("ItmScrapSteel", 1, 1.0), (FeedFamilies.WindowReject, 1, 6.0) }), "Window budget");
        check(Products(FeedFamilies.Whipple, 5).SequenceEqual(new[] { ("ItmScrapAluminum", 3, 1.0), ("ItmPartsMechSmall01", 1, .5), (FeedFamilies.WhippleReject, 1, 1.5) }), "Whipple budget");
        check(Products(FeedFamilies.Aero, 4).SequenceEqual(new[] { ("ItmScrapPlastic", 3, .3), ("ItmScrapAluminum", 1, 1.0), (FeedFamilies.AeroReject, 1, 2.1) }) &&
            Products(FeedFamilies.Aero, 6).Single(p => p.Item1 == "ItmScrapAluminum").Item2 == 3, "Aero budget: one aluminium per kilogram above the skin");
        // Dismantling loses value against selling the part whole (owner rule); the plain base prices are pinned here.
        foreach (var family in FeedFamilies.All.Where(f => f.Key != "wall"))
            check(Value(family.Catalog(family.MinKg).Current.Products) < WholePrice[family.Key] && Value(family.Catalog(family.MaxKg).Current.Products) < WholePrice[family.Key],
                $"{family.Key} products are worth less than the whole part");
        check(FeedFamilies.AllProducts().Select(p => p.Id).Distinct().Count() == 12 && FeedFamilies.AllProducts().Any(p => p.Id == ProcessRules.Residue) &&
            FeedFamilies.AllProducts().Any(p => p.Id == ReclaimerRules.Feedstock) && FeedFamilies.RejectKg.Keys.All(FeedFamilies.IsProduct),
            "Every family's products, historic wall revisions included, are known products");
        check(FeedFamilies.RejectKg.Count == 5 && FeedFamilies.RejectFamily.Keys.SequenceEqual(FeedFamilies.RejectKg.Keys) && !FeedFamilies.IsReject(ReclaimerRules.Reject) && !FeedFamilies.IsReject("ItmScrapTrash"),
            "Five terminal reject identities, distinct from the R4 reject and from native trash");
    }
}
