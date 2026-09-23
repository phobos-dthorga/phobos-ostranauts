// Native construction integration follows OCF 0.8.71; see THIRD-PARTY.md and
// licenses/CraftingFramework-MIT.md for the upstream notice and adaptation scope.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Phobos.Ostranauts.Framework.Construction;

public sealed class RecipePack
{
    public int schemaVersion;
    public Recipe[] recipes = Array.Empty<Recipe>();
}

public sealed class Ingredient
{
    public string trigger = "";
    public string item = "";
    public int count;
    public double unitMassKg;
    public bool requireEmpty;
}

public sealed class Product
{
    public string item = "";
    public int count;
    public double unitMassKg;
}

public sealed class Recipe
{
    public string id = "";
    public string name = "";
    public string description = "";
    public string[] stationIds = Array.Empty<string>();
    public string[] optionalStationIds = Array.Empty<string>();
    public string[] legacyActionIds = Array.Empty<string>();
    public Ingredient[] ingredients = Array.Empty<Ingredient>();
    public Product[] outputs = Array.Empty<Product>();
    public double workSeconds;
    public float range;
}

/// <summary>Strict, finite, mass-balanced construction; no probabilistic outputs.</summary>
public static class RecipeRules
{
    public const int MaxUnits = 100;
    public const string ActionPrefix = "PhobosCraft_";
    public static string ActionId(string recipeId) => ActionPrefix + recipeId;
    public static bool MassMatches(double actual, double expected) =>
        IsPositive(actual) && IsPositive(expected) && Math.Abs(actual - expected) <= 0.000001;
    private static bool IsPositive(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    public static void Identifier(string value)
    {
        if (value == null || !Regex.IsMatch(value, "^[A-Za-z][A-Za-z0-9_]{0,95}$"))
            throw new ArgumentException("Use an author-prefixed identifier containing letters, digits and underscores.");
    }
    public static void Validate(Recipe recipe)
    {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        Identifier(recipe.id);
        if (string.IsNullOrWhiteSpace(recipe.name) || recipe.name.Length > 160)
            throw new ArgumentException("A recipe needs a readable name of at most 160 characters.");
        if (!IsPositive(recipe.workSeconds) || recipe.workSeconds > 86400 ||
            !IsPositive(recipe.range) || recipe.range > 8)
            throw new ArgumentException("Work must take 0..86400 seconds and access range must be 0..8 tiles (exclusive of zero).");
        if (recipe.stationIds == null || recipe.stationIds.Length == 0 || recipe.stationIds.Length > 32 ||
            recipe.optionalStationIds == null || recipe.optionalStationIds.Length > 32 || recipe.legacyActionIds == null || recipe.legacyActionIds.Length > 16)
            throw new ArgumentException("Provide required stations, with bounded optional stations and legacy aliases.");
        var stations = recipe.stationIds.Concat(recipe.optionalStationIds).ToArray();
        foreach (string id in stations.Concat(recipe.legacyActionIds)) Identifier(id);
        if (stations.Distinct(StringComparer.Ordinal).Count() != stations.Length ||
            recipe.legacyActionIds.Distinct(StringComparer.Ordinal).Count() != recipe.legacyActionIds.Length ||
            recipe.legacyActionIds.Any(id => id.StartsWith(ActionPrefix, StringComparison.Ordinal)))
            throw new ArgumentException("Duplicate station/alias or an alias using the current action namespace.");
        if (recipe.ingredients == null || recipe.ingredients.Length == 0 || recipe.ingredients.Length > MaxUnits ||
            recipe.outputs == null || recipe.outputs.Length == 0 || recipe.outputs.Length > MaxUnits)
            throw new ArgumentException("A recipe needs bounded, nonempty inputs and outputs.");
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null) throw new ArgumentException("Null ingredient.");
            Identifier(ingredient.item); Identifier(ingredient.trigger);
            CheckAmount(ingredient.count, ingredient.unitMassKg);
        }
        foreach (var product in recipe.outputs)
        {
            if (product == null) throw new ArgumentException("Null output.");
            Identifier(product.item); CheckAmount(product.count, product.unitMassKg);
        }
        if (recipe.ingredients.Select(i => i.item).Distinct(StringComparer.Ordinal).Count() != recipe.ingredients.Length ||
            recipe.outputs.Select(p => p.item).Distinct(StringComparer.Ordinal).Count() != recipe.outputs.Length)
            throw new ArgumentException("Combine repeated ingredient/output identities into one entry.");
        if (recipe.ingredients.Sum(i => i.count) > MaxUnits || recipe.outputs.Sum(p => p.count) > MaxUnits)
            throw new ArgumentException("Construction is limited to 100 input/output units per job.");
        if (!MassMatches(recipe.ingredients.Sum(i => i.unitMassKg * i.count), recipe.outputs.Sum(p => p.unitMassKg * p.count)))
            throw new ArgumentException("Construction inputs and outputs must conserve mass, including any residue.");
    }
    private static void CheckAmount(int count, double mass)
    {
        if (count < 1 || count > MaxUnits || !IsPositive(mass) || mass > 1000000)
            throw new ArgumentException("Invalid count or unit mass.");
    }

    /// <summary>One snapshot per unit in the native removal contract, including units from stacks.</summary>
    public static bool MatchesInputs(Recipe recipe, IReadOnlyList<InputUnit> units)
    {
        if (units.Count != recipe.ingredients.Sum(i => i.count)) return false;
        foreach (var ingredient in recipe.ingredients)
        {
            var selected = units.Where(u => u.ItemId == ingredient.item).ToArray();
            if (selected.Length != ingredient.count || selected.Any(u => !MassMatches(u.MassKg, ingredient.unitMassKg)
                || !u.Empty || (ingredient.requireEmpty && !u.Separate))) return false;
        }
        return true;
    }
}

public readonly struct InputUnit
{
    public string ItemId { get; }
    public double MassKg { get; }
    public bool Empty { get; }
    public bool Separate { get; }
    public InputUnit(string itemId, double massKg, bool empty, bool separate)
    { ItemId = itemId; MassKg = massKg; Empty = empty; Separate = separate; }
}

/// <summary>Prevents replay after native effects start, even when they throw.</summary>
public sealed class CompletionGate
{
    public bool Attempted { get; private set; }
    public bool TryBegin(bool cancelled, Func<bool> validate)
    {
        if (cancelled || Attempted || !validate()) return false;
        Attempted = true;
        return true;
    }
}
