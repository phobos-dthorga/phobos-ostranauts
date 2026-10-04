using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosManufacturing.Core;

/// <summary>Manufacturing's <c>outcomes</c> data pack (<c>framework/outcomes.json</c>, Manufacturing 0.44.0; owner
/// direction, 4 October 2026): which charges have more than one possible result, and the odds of each. The results are
/// ordinary frozen recipes in the process-recipes pack; this pack holds only the tables, which a player may retune or
/// add to in <c>BepInEx/config/PhobosManufacturing/outcomes</c>. No code names a table, an outcome or a weight: the
/// charge engine asks <see cref="Resolve"/> at bind, and the picked recipe's revision is what the charge saves, so a
/// reload resumes exactly that result.</summary>
public static class ChargeOutcomes
{
    public const string Resource = "PhobosManufacturing.outcomes.json";
    private static OutcomePack? pack; private static RecipePack? builtFor;
    private static readonly Dictionary<string, string> baseOf = new(StringComparer.Ordinal);
    public static DataPackSource Source => new(ManufacturingRules.Owner, Economy.ModFolder, OutcomeSchema.Name, typeof(ChargeOutcomes).Assembly, Resource);
    /// <summary>The tables for the current recipe pack; reloaded whenever the recipes are.</summary>
    public static OutcomePack Pack
    {
        get
        {
            var recipes = ChargeCatalog.Pack;
            if (pack != null && ReferenceEquals(builtFor, recipes)) return pack;
            return Load();
        }
    }
    public static OutcomePack Load()
    {
        var recipes = ChargeCatalog.Pack;
        var facts = Facts(recipes);
        pack = DataPacks.Load<OutcomePack>(Source, p => OutcomeSchema.Validate(p, id => facts.TryGetValue(id, out var fact) ? fact : null));
        builtFor = recipes;
        baseOf.Clear();
        foreach (var table in pack.tables)
            foreach (string outcome in table.Value.outcomes.Keys) baseOf[outcome] = table.Key;
        return pack;
    }
    /// <summary>What the validator needs of each recipe: its machine, and a signature equal for two recipes exactly when
    /// a player loads and waits the same for both.</summary>
    public static Dictionary<string, OutcomeRecipe> Facts(RecipePack recipes) => recipes.recipes.ToDictionary(p => p.Key, p => new OutcomeRecipe(p.Value.machine, Signature(p.Value)), StringComparer.Ordinal);
    private static string Signature(RecipeEntry r)
    {
        string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        var inputs = r.inputs.OrderBy(i => i.id, StringComparer.Ordinal).Select(i => i.id + "*" + i.count + "@" + N(i.kg));
        var loop = (r.circulates ?? new Dictionary<string, double>()).OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => c.Key + "~" + N(c.Value));
        return string.Join(";", inputs) + "|" + string.Join(";", loop) + "|" + N(r.seconds ?? 0);
    }
    /// <summary>The recipe a player sees for this one: an outcome's base, or the recipe itself.</summary>
    public static string BaseOf(string recipeId) { _ = Pack; return baseOf.TryGetValue(recipeId, out var id) ? id : recipeId; }
    /// <summary>A recipe that is only ever reached as an outcome: never offered, matched or selected by itself.</summary>
    public static bool IsHidden(string recipeId) { _ = Pack; return baseOf.TryGetValue(recipeId, out var id) && id != recipeId; }
    /// <summary>The recipe a charge of <paramref name="recipe"/> made of these units turns out to be. A recipe with no
    /// table is itself.</summary>
    public static ChargeRecipe Resolve(ChargeRecipe recipe, IEnumerable<string> unitIds)
    {
        if (!Pack.tables.TryGetValue(recipe.Id, out var table)) return recipe;
        string picked = Outcomes.Pick(table.outcomes, unitIds);
        return ChargeCatalog.For(recipe.Machine).ById(picked) ?? throw new InvalidOperationException("Outcome recipe " + picked + " is missing.");
    }
}
