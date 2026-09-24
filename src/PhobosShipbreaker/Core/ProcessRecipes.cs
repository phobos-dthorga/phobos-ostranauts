using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosShipbreaker.Core;

/// <summary>Immutable wall-processing outputs. Published revisions must never be rewritten.</summary>
public sealed class ProcessRecipe
{
    public int Revision { get; }
    public IReadOnlyList<ProductSpec> Products { get; }
    // Only revisions actually shipped without a saved duration may supply this fallback.
    public double? LegacySeconds { get; }

    public ProcessRecipe(int revision, IEnumerable<ProductSpec> products, double? legacySeconds = null)
    {
        var copy = products.ToArray();
        if (revision < 1 || copy.Length == 0 || copy.Any(p => p == null ||
            string.IsNullOrWhiteSpace(p.Id) || p.Count <= 0 || p.Kg <= 0 || double.IsNaN(p.Kg) || double.IsInfinity(p.Kg)) ||
            copy.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != copy.Length ||
            !ProcessRules.Balanced(ProcessRules.InputKg, copy.Select(p => p.Kg * p.Count)) ||
            (legacySeconds.HasValue && (double.IsNaN(legacySeconds.Value) ||
                double.IsInfinity(legacySeconds.Value) || legacySeconds < ProcessRules.MinJobSeconds || legacySeconds > ProcessRules.MaxJobSeconds)))
            throw new ArgumentException(Text.Get("ProcessRecipes.invalid_wall_recipe_or_material_balance"));
        Revision = revision; Products = Array.AsReadOnly(copy); LegacySeconds = legacySeconds;
    }
}

/// <summary>Current selection is for fresh panels; saved revisions resolve independently.</summary>
public sealed class ProcessRecipeCatalog
{
    private readonly Dictionary<double, ProcessRecipe> revisions;
    public ProcessRecipe Current { get; }
    public IReadOnlyList<ProcessRecipe> Recipes { get; }

    public ProcessRecipeCatalog(int currentRevision, IEnumerable<ProcessRecipe> recipes)
    {
        var copy = recipes.ToArray();
        revisions = copy.ToDictionary(r => (double)r.Revision);
        if (!revisions.TryGetValue(currentRevision, out var current))
            throw new ArgumentException(Text.Get("ProcessRecipes.current_wall_recipe_is_not_registered"));
        Current = current; Recipes = Array.AsReadOnly(copy);
    }

    public bool TryGet(double revision, out ProcessRecipe? recipe) => revisions.TryGetValue(revision, out recipe);
}

public static class ProcessRecipes
{
    // Keep the exact revision-1 identity, counts and masses for every old job.
    // Do not enable another revision until its residue consumer is playable.
    public static readonly ProcessRecipeCatalog WallPanels = new ProcessRecipeCatalog(1, new[] {
        new ProcessRecipe(1, new[] {
            new ProductSpec("ItmPartsMechSmall01", 2, 0.5),
            new ProductSpec("ItmScrapAluminum", 2, 1),
            new ProductSpec("ItmScrapCarbonFiber", 2, 1),
            new ProductSpec("ItmScrapSteel", 6, 1),
            new ProductSpec(ProcessRules.Residue, 1, 13)
        }, legacySeconds: 60)
    });
}
