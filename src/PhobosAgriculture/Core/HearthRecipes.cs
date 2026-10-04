using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture.Core;

/// <summary>One Hearth-2 cooking recipe: one bound portion (the first input), at most one supply consumed with it
/// when the cooking finishes (a water ration for bread), and what it becomes, after a fixed energy. Mass in equals
/// mass out; cooking moisture loss is not modelled, because the game's air has no water vapour to receive it.</summary>
public sealed class HearthRecipe
{
    public readonly string Id, Input;
    public readonly int Revision;
    /// <summary>The bound portion's unit mass.</summary>
    public readonly double Kg, Seconds;
    /// <summary>A supply used up with the portion when the cooking finishes, or none.</summary>
    public readonly (string Id, double Kg)? Extra;
    public readonly IReadOnlyList<(string Id, double Kg)> Products;
    /// <summary>Electricity the portion takes at the cooker's fixed draw.</summary>
    public double KWh => HearthRecipes.CookerKW * Seconds / 3600;
    public HearthRecipe(string id, RecipeEntry e)
    {
        Id = id; Revision = e.revision; Input = e.inputs[0].id; Kg = e.inputs[0].kg; Seconds = e.seconds ?? 0;
        Extra = e.inputs.Count > 1 ? (e.inputs[1].id, e.inputs[1].kg) : null;
        Products = e.products.SelectMany(p => Enumerable.Repeat((p.id, p.kg), p.count)).ToArray();
    }
}

/// <summary>The Hearth-2's recipes, from Agriculture's <c>process-recipes</c> data pack (Agriculture 0.40.0; the one
/// potato recipe was code until then). A portion bound in the cooker is cooked by the recipe that names its item, so
/// the pack may hold one recipe per bound item; published revisions are frozen by hash. Agriculture 0.41.0 allows a
/// second input, the supply used with it (wheat grain bakes with a water ration).</summary>
public static class HearthRecipes
{
    public const string Machine = "hearth", Resource = "PhobosAgriculture.process-recipes.json", FrozenResource = "PhobosAgriculture.frozen-process-recipes.json";
    /// <summary>The cooker's electrical draw while cooking, in kW.</summary>
    public const double CookerKW = 2;
    /// <summary>Game items a recipe may use as its supply, with the unit mass the rest of the mod already uses for them.</summary>
    public static readonly IReadOnlyDictionary<string, double> GameSupplies = new Dictionary<string, double>(StringComparer.Ordinal) { ["LiquidWater"] = .25 };
    private static RecipePack? pack;
    private static HearthRecipe[] all = Array.Empty<HearthRecipe>();
    public static RecipePack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, Crops.ModFolder, RecipeSchema.Name, typeof(HearthRecipes).Assembly, Resource);
    public static IReadOnlyList<HearthRecipe> All { get { _ = Pack; return all; } }
    /// <summary>The most energy any recipe takes: the bound on a saved cooking progress.</summary>
    public static double MaxKWh => All.Count == 0 ? 0 : All.Max(r => r.KWh);
    public static HearthRecipe? ForInput(string? item) { foreach (var r in All) if (r.Input == item) return r; return null; }
    public static bool IsInput(string? item) => ForInput(item) != null;
    /// <summary>Whether an item is the supply some recipe uses with its portion.</summary>
    public static bool IsExtra(string? item) => item != null && All.Any(r => r.Extra?.Id == item);
    public static bool IsProduct(string? item) => All.Any(r => r.Products.Any(p => p.Id == item));
    /// <summary>A unit mass for a recipe check: one of the mod's materials, or a game supply a recipe may use.</summary>
    public static double? UnitKg(string? id) => AgricultureMaterials.KgOf(id) ?? (id != null && GameSupplies.TryGetValue(id, out double kg) ? kg : null);
    public static RecipePack Load()
    {
        var frozen = RecipeFreeze.Read(typeof(HearthRecipes).Assembly, FrozenResource);
        var context = new RecipeContext { Machines = new[] { Machine }, Requirements = Array.Empty<string>(), UnitMassOf = UnitKg, IsCommodity = _ => false };
        var loaded = DataPacks.Load<RecipePack>(Source, (p, raw) => { RecipeSchema.Validate(p, context); Check(p); RecipeFreeze.Enforce(raw, frozen); });
        pack = loaded; all = loaded.recipes.Select(p => new HearthRecipe(p.Key, p.Value)).ToArray();
        return loaded;
    }
    /// <summary>The cooker's own rules on top of the shared schema.</summary>
    private static void Check(RecipePack p)
    {
        var inputs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in p.recipes)
        {
            var r = pair.Value;
            // One bound portion, at most one supply, one unit of each; products are the mod's own items.
            if (r.inputs.Count < 1 || r.inputs.Count > 2 || r.inputs.Any(i => i.count != 1) || r.products.Count < 1 || r.offGas.Count != 0 || r.circulates.Count != 0 ||
                r.thermal != null || r.melt || r.reactionKWh != null || r.supersedes.Count != 0 || r.seconds == null)
                throw new ArgumentException(Text.Get("hearth_recipe_shape", pair.Key));
            if (AgricultureMaterials.KgOf(r.inputs[0].id) == null || r.inputs.Skip(1).Any(i => UnitKg(i.id) == null) || r.products.Any(u => AgricultureMaterials.KgOf(u.id) == null))
                throw new ArgumentException(Text.Get("hearth_recipe_item", pair.Key));
            if (!inputs.Add(r.inputs[0].id)) throw new ArgumentException(Text.Get("hearth_recipe_input", pair.Key, r.inputs[0].id));
        }
    }
}
