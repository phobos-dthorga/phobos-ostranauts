using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosAgriculture.Core;

/// <summary>One Hearth-2 cooking recipe: one portion in, one portion of the same mass out, after a fixed energy.</summary>
public sealed class HearthRecipe
{
    public readonly string Id, Input, Product;
    public readonly int Revision;
    public readonly double Kg, Seconds;
    /// <summary>Electricity the portion takes at the cooker's fixed draw.</summary>
    public double KWh => HearthRecipes.CookerKW * Seconds / 3600;
    public HearthRecipe(string id, RecipeEntry e)
    { Id = id; Revision = e.revision; Input = e.inputs[0].id; Product = e.products[0].id; Kg = e.inputs[0].kg; Seconds = e.seconds ?? 0; }
}

/// <summary>The Hearth-2's recipes, from Agriculture's <c>process-recipes</c> data pack (Agriculture 0.40.0; the one
/// potato recipe was code until then). A portion bound in the cooker is cooked by the recipe that names its item, so
/// the pack may hold one recipe per input item; published revisions are frozen by hash.</summary>
public static class HearthRecipes
{
    public const string Machine = "hearth", Resource = "PhobosAgriculture.process-recipes.json", FrozenResource = "PhobosAgriculture.frozen-process-recipes.json";
    /// <summary>The cooker's electrical draw while cooking, in kW.</summary>
    public const double CookerKW = 2;
    private static RecipePack? pack;
    private static HearthRecipe[] all = Array.Empty<HearthRecipe>();
    public static RecipePack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, Crops.ModFolder, RecipeSchema.Name, typeof(HearthRecipes).Assembly, Resource);
    public static IReadOnlyList<HearthRecipe> All { get { _ = Pack; return all; } }
    /// <summary>The most energy any recipe takes: the bound on a saved cooking progress.</summary>
    public static double MaxKWh => All.Count == 0 ? 0 : All.Max(r => r.KWh);
    public static HearthRecipe? ForInput(string? item) { foreach (var r in All) if (r.Input == item) return r; return null; }
    public static bool IsInput(string? item) => ForInput(item) != null;
    public static bool IsProduct(string? item) => All.Any(r => r.Product == item);
    public static RecipePack Load()
    {
        var frozen = RecipeFreeze.Read(typeof(HearthRecipes).Assembly, FrozenResource);
        var context = new RecipeContext { Machines = new[] { Machine }, Requirements = Array.Empty<string>(), UnitMassOf = AgricultureMaterials.KgOf, IsCommodity = _ => false };
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
            if (r.inputs.Count != 1 || r.products.Count != 1 || r.inputs[0].count != 1 || r.products[0].count != 1 || r.offGas.Count != 0 || r.circulates.Count != 0 ||
                r.thermal != null || r.melt || r.reactionKWh != null || r.supersedes.Count != 0 || r.seconds == null)
                throw new ArgumentException(Text.Get("hearth_recipe_shape", pair.Key));
            if (AgricultureMaterials.KgOf(r.inputs[0].id) == null || AgricultureMaterials.KgOf(r.products[0].id) == null) throw new ArgumentException(Text.Get("hearth_recipe_item", pair.Key));
            if (!inputs.Add(r.inputs[0].id)) throw new ArgumentException(Text.Get("hearth_recipe_input", pair.Key, r.inputs[0].id));
        }
    }
}
