using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

public static class ProcessRecipes
{
    private static readonly Dictionary<int, ProcessRecipeCatalog> byMass = new Dictionary<int, ProcessRecipeCatalog>();
    // Keep the exact revision-1 identity, counts and masses for every old job. Revision 1 only ever
    // took the plain 24 kg wall. Revision 2 is enabled together with its usable reclaimer.
    private static readonly ProcessRecipe RevisionOne = new ProcessRecipe(1, ProcessRules.StandardWallKg, new[] {
        new ProductSpec("ItmPartsMechSmall01", 2, 0.5),
        new ProductSpec("ItmScrapAluminum", 2, 1),
        new ProductSpec("ItmScrapCarbonFiber", 2, 1),
        new ProductSpec("ItmScrapSteel", 6, 1),
        new ProductSpec(ProcessRules.Residue, 1, 13)
    }, legacySeconds: 60);

    /// <summary>Revision 2 for a wall of the given whole-kilogram mass: the 13 kg identified residue
    /// packet, then the parts, aluminium and carbon fibre, then steel for whatever is left. The plain
    /// 24 kg wall gives exactly the products shipped with revision 2; the game's cosmetic wall variants
    /// (14 to 48 kg) give more or less steel. Mass is conserved; the reclaimer's packet stays 13 kg.</summary>
    public static IReadOnlyList<ProductSpec> WallProducts(int kg)
    {
        if (kg < ProcessRules.MinimumWallKg || kg > ProcessRules.MaximumWallKg)
            throw new ArgumentException(Text.Get("ProcessRecipes.unsupported_wall_mass", kg, ProcessRules.MinimumWallKg, ProcessRules.MaximumWallKg));
        var products = new List<ProductSpec>();
        double remaining = kg - ReclaimerRules.InputKg;
        products.Add(new ProductSpec("ItmPartsMechSmall01", 2, .5)); remaining -= 1;
        foreach (var metal in new[] { "ItmScrapAluminum", "ItmScrapCarbonFiber" })
        {
            int count = (int)Math.Min(2, Math.Floor(remaining));
            if (count > 0) { products.Add(new ProductSpec(metal, count, 1)); remaining -= count; }
        }
        if (remaining >= 1) products.Add(new ProductSpec("ItmScrapSteel", (int)Math.Round(remaining), 1));
        products.Add(new ProductSpec(ReclaimerRules.Feedstock, 1, ReclaimerRules.InputKg));
        return products;
    }

    /// <summary>The catalog for a wall of this mass. Revision 2 exists for every accepted whole-kilogram
    /// mass; revision 1 is registered only for the plain 24 kg wall it was ever stamped on.</summary>
    public static ProcessRecipeCatalog ForWallMass(double kg)
    {
        if (!ProcessRules.AcceptedWallKg(kg))
            throw new ArgumentException(Text.Get("ProcessRecipes.unsupported_wall_mass", kg, ProcessRules.MinimumWallKg, ProcessRules.MaximumWallKg));
        int whole = (int)Math.Round(kg);
        lock (byMass)
        {
            if (byMass.TryGetValue(whole, out var cached)) return cached;
            var recipes = new List<ProcessRecipe>();
            if (whole == (int)ProcessRules.StandardWallKg) recipes.Add(RevisionOne);
            recipes.Add(new ProcessRecipe(2, whole, WallProducts(whole)));
            var catalog = new ProcessRecipeCatalog(2, recipes);
            byMass[whole] = catalog;
            return catalog;
        }
    }

    public static readonly ProcessRecipeCatalog WallPanels = ForWallMass(ProcessRules.StandardWallKg);
}
