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
    public string nameKey = "";
    public string descriptionKey = "";
    public string[] stationIds = Array.Empty<string>();
    public string[] optionalStationIds = Array.Empty<string>();
    public string[] legacyActionIds = Array.Empty<string>();
    public string[] toolTriggers = Array.Empty<string>();
    public Ingredient[] ingredients = Array.Empty<Ingredient>();
    public Product[] outputs = Array.Empty<Product>();
    public double workSeconds;
    public float range;
}

/// <summary>Strict, finite, mass-balanced construction; no probabilistic outputs.</summary>
public static class RecipeRules
{
    public const int MaxUnits = 100;
    public const int MaxTools = 8, MaxNameCharacters = 160, MaxStations = 32, MaxAliases = 16;
    public const double MaxWorkSeconds = 86400, MaxAccessTiles = 8, MaxUnitMassKg = 1000000;
    public const string ActionPrefix = "PhobosCraft_";
    public static string ActionId(string recipeId) => ActionPrefix + recipeId;
    public static bool MassMatches(double actual, double expected) =>
        IsPositive(actual) && IsPositive(expected) && Math.Abs(actual - expected) <= Units.MassToleranceKg;
    private static bool IsPositive(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    public static void Identifier(string value)
    {
        if (value == null || !Regex.IsMatch(value, "^[A-Za-z][A-Za-z0-9_]{0,95}$"))
            throw new ArgumentException(Text.Get("Recipe.use_an_author_prefixed_identifier_containing_letters"));
    }
    public static void Validate(Recipe recipe)
    {
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        Identifier(recipe.id);
        if (recipe.toolTriggers == null || recipe.toolTriggers.Length > MaxTools || recipe.toolTriggers.Distinct().Count() != recipe.toolTriggers.Length)
            throw new ArgumentException(Text.Get("Recipe.use_up_to_eight_distinct_reusable_tool", MaxTools));
        foreach (string tool in recipe.toolTriggers) Identifier(tool);
        if (string.IsNullOrWhiteSpace(recipe.name) || recipe.name.Length > MaxNameCharacters)
            throw new ArgumentException(Text.Get("Recipe.a_recipe_needs_a_readable_name_of", MaxNameCharacters));
        if (!IsPositive(recipe.workSeconds) || recipe.workSeconds > MaxWorkSeconds ||
            !IsPositive(recipe.range) || recipe.range > MaxAccessTiles)
            throw new ArgumentException(Text.Get("Recipe.work_must_take_seconds_and_access_range", MaxWorkSeconds, MaxAccessTiles));
        if (recipe.stationIds == null || recipe.stationIds.Length == 0 || recipe.stationIds.Length > MaxStations ||
            recipe.optionalStationIds == null || recipe.optionalStationIds.Length > MaxStations || recipe.legacyActionIds == null || recipe.legacyActionIds.Length > MaxAliases)
            throw new ArgumentException(Text.Get("Recipe.provide_required_stations_with_bounded_optional_stations"));
        var stations = recipe.stationIds.Concat(recipe.optionalStationIds).ToArray();
        foreach (string id in stations.Concat(recipe.legacyActionIds)) Identifier(id);
        if (stations.Distinct(StringComparer.Ordinal).Count() != stations.Length ||
            recipe.legacyActionIds.Distinct(StringComparer.Ordinal).Count() != recipe.legacyActionIds.Length ||
            recipe.legacyActionIds.Any(id => id.StartsWith(ActionPrefix, StringComparison.Ordinal)))
            throw new ArgumentException(Text.Get("Recipe.duplicate_station_alias_or_an_alias_using"));
        if (recipe.ingredients == null || recipe.ingredients.Length == 0 || recipe.ingredients.Length > MaxUnits ||
            recipe.outputs == null || recipe.outputs.Length == 0 || recipe.outputs.Length > MaxUnits)
            throw new ArgumentException(Text.Get("Recipe.a_recipe_needs_bounded_nonempty_inputs_and"));
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient == null) throw new ArgumentException(Text.Get("Recipe.null_ingredient"));
            Identifier(ingredient.item); Identifier(ingredient.trigger);
            CheckAmount(ingredient.count, ingredient.unitMassKg);
        }
        foreach (var product in recipe.outputs)
        {
            if (product == null) throw new ArgumentException(Text.Get("Recipe.null_output"));
            Identifier(product.item); CheckAmount(product.count, product.unitMassKg);
        }
        if (recipe.ingredients.Select(i => i.item).Distinct(StringComparer.Ordinal).Count() != recipe.ingredients.Length ||
            recipe.outputs.Select(p => p.item).Distinct(StringComparer.Ordinal).Count() != recipe.outputs.Length)
            throw new ArgumentException(Text.Get("Recipe.combine_repeated_ingredient_output_identities_into_one"));
        if (recipe.ingredients.Sum(i => i.count) > MaxUnits || recipe.outputs.Sum(p => p.count) > MaxUnits)
            throw new ArgumentException(Text.Get("Recipe.construction_is_limited_to_input_output_units", MaxUnits));
        if (!MassMatches(recipe.ingredients.Sum(i => i.unitMassKg * i.count), recipe.outputs.Sum(p => p.unitMassKg * p.count)))
            throw new ArgumentException(Text.Get("Recipe.construction_inputs_and_outputs_must_conserve_mass"));
    }
    private static void CheckAmount(int count, double mass)
    {
        if (count < 1 || count > MaxUnits || !IsPositive(mass) || mass > MaxUnitMassKg)
            throw new ArgumentException(Text.Get("Recipe.invalid_count_or_unit_mass"));
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
