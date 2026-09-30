using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosManufacturing.Core;

/// <summary>Every charge machine's recipes, read once from the <c>process-recipes</c> data pack
/// (<c>framework/process-recipes.json</c>) and frozen by revision. Each machine has its own revision space
/// (<c>refinery@1</c> and a later machine's <c>@1</c> are different recipes). The loader enforces the Framework schema
/// (mass conservation and the game's gases on every file) and the machines' own rules: every recipe names its
/// seconds, a feed identity has one unit mass per machine, a charge fits the machine's feed bin, and a melt draws
/// nothing from a vessel.</summary>
public static class ChargeCatalog
{
    public const string Schema = RecipeSchema.Name, Resource = "PhobosManufacturing.process-recipes.json", FrozenResource = "PhobosManufacturing.frozen-process-recipes.json";
    public const string Refinery = "refinery", Leach = "leach", AcidPlant = "acid-plant";
    public const string SteelStockRequirement = "shipbreaker-steel-stock", MakeupRequirement = "agriculture-makeup";
    /// <summary>Each catalog machine key and the definition prefix of the machine it runs on.</summary>
    public static readonly IReadOnlyDictionary<string, string> MachinePrefixes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Refinery] = RefineryRules.Prefix,
        [Leach] = LeachRules.Prefix,
        [AcidPlant] = AcidPlantRules.Prefix
    };
    /// <summary>Feature keys a recipe may require; the owner resolves each at load.</summary>
    public static readonly IReadOnlyList<string> Requirements = new[] { SteelStockRequirement, MakeupRequirement };
    public static string PrefixOf(string machine) => MachinePrefixes.TryGetValue(machine, out var prefix) ? prefix : throw new InvalidOperationException("Unknown charge machine: " + machine);
    private static RecipePack? pack; private static IReadOnlyList<ChargeRecipe>? all; private static RecipePack? builtFrom;
    private static readonly Dictionary<string, ChargeRecipeView> views = new(StringComparer.Ordinal);
    public static RecipePack Pack => pack ??= Load();
    public static DataPackSource Source => new(ManufacturingRules.Owner, Economy.ModFolder, Schema, typeof(ChargeCatalog).Assembly, Resource);
    /// <summary>Reads the shipped pack and any player files. Unit masses are checked against Manufacturing's own
    /// materials, Shipbreaker's steel stock, Agriculture's makeup packet and, when the game's data is loaded, the
    /// native items.</summary>
    public static RecipePack Load(Func<string, double?>? nativeMass = null)
    {
        var frozen = RecipeFreeze.Read(typeof(ChargeCatalog).Assembly, FrozenResource);
        var context = new RecipeContext
        {
            Machines = MachinePrefixes.Keys.ToArray(), Requirements = Requirements,
            UnitMassOf = id => Materials.KgOf(id) ?? (id == RefineryRules.SteelIngot ? RefineryRules.SteelIngotKg : id == RefineryRules.SteelRemainder ? RefineryRules.SteelRemainderKg :
                id == LeachRules.MakeupPacket ? LeachRules.MakeupPacketKg : nativeMass?.Invoke(id)),
            IsCommodity = ChargeCommodities.Is
        };
        pack = DataPacks.Load<RecipePack>(Source, (p, raw) =>
        {
            RecipeSchema.Validate(p, context);
            foreach (var pair in p.recipes) if (pair.Value.seconds == null) throw new ArgumentException("Recipe " + pair.Key + ": a charge needs its seconds.");
            Check(Build(p));
            RecipeFreeze.Enforce(raw, frozen);
        });
        return pack;
    }
    private static IReadOnlyList<ChargeRecipe> Build(RecipePack source) => Array.AsReadOnly(source.recipes
        .OrderBy(p => p.Value.machine, StringComparer.Ordinal).ThenBy(p => p.Value.revision)
        .Select(p => new ChargeRecipe(p.Value.machine, p.Key, p.Value.revision, p.Value.inputs.Select(u => new ChargeInput(u.id, u.count, u.kg)),
            p.Value.products.Select(u => new ProductSpec(u.id, u.count, u.kg)), p.Value.offGas, p.Value.seconds ?? 0, p.Value.melt, p.Value.requires, p.Value.circulates, p.Value.reactionKWh ?? 0))
        .ToArray());
    /// <summary>The machines' own rules over a whole catalog: one unit mass per feed identity per machine, and every
    /// charge fits its machine's feed bin.</summary>
    public static void Check(IEnumerable<ChargeRecipe> recipes)
    {
        foreach (var machine in recipes.GroupBy(r => r.Machine, StringComparer.Ordinal))
        {
            int cells = Equipment.Entry(PrefixOf(machine.Key)).feedCells;
            foreach (var recipe in machine)
                if (recipe.Units > cells) throw new ArgumentException("Recipe " + recipe.Id + ": its charge of " + recipe.Units + " units does not fit a feed bin of " + cells + ".");
            foreach (var feed in machine.SelectMany(r => r.ItemInputs).GroupBy(i => i.Id, StringComparer.Ordinal))
                if (feed.Select(i => i.Kg).Distinct().Count() > 1) throw new ArgumentException("Feed " + feed.Key + " has more than one unit mass on machine " + machine.Key + ".");
        }
    }
    /// <summary>Every recipe of every machine, by machine then revision.</summary>
    public static IReadOnlyList<ChargeRecipe> All
    {
        get
        {
            if (all != null && ReferenceEquals(builtFrom, Pack)) return all;
            views.Clear();
            builtFrom = Pack;
            return all = Build(builtFrom);
        }
    }
    /// <summary>One machine's recipes, in their own revision space.</summary>
    public static ChargeRecipeView For(string machine)
    {
        var recipes = All;
        if (!views.TryGetValue(machine, out var view)) views[machine] = view = new ChargeRecipeView(machine, recipes.Where(r => r.Machine == machine).ToArray());
        return view;
    }
}

/// <summary>One machine's recipes: lookups, availability under the owner's requirement gates, the automatic match
/// and the unit mass each feed identity must carry.</summary>
public sealed class ChargeRecipeView
{
    public string Machine { get; }
    public IReadOnlyList<ChargeRecipe> All { get; }
    public ChargeRecipeView(string machine, IReadOnlyList<ChargeRecipe> recipes) { Machine = machine; All = recipes; }
    public ChargeRecipe? ByRevision(int revision) => All.FirstOrDefault(r => r.Revision == revision);
    public ChargeRecipe? ById(string? id) => id == null ? null : All.FirstOrDefault(r => r.Id == id);
    /// <summary>Recipes whose every requirement is met.</summary>
    public IEnumerable<ChargeRecipe> Available(Func<string, bool> met) => All.Where(r => r.Requires.All(met));
    /// <summary>The available recipe whose whole item charge is present among the feed identities, preferring the
    /// largest charge, then the lowest revision; or null. The caller binds the exact units.</summary>
    public ChargeRecipe? Match(IEnumerable<string> feedIds, Func<string, bool> met)
    {
        var counts = feedIds.GroupBy(id => id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return Available(met).OrderByDescending(r => r.Units).ThenBy(r => r.Revision)
            .FirstOrDefault(r => r.ItemInputs.All(i => counts.TryGetValue(i.Id, out int n) && n >= i.Count));
    }
    /// <summary>The unit mass a feed identity must carry among the available recipes (or only <paramref name="selected"/>,
    /// when its requirements are met, for a machine that binds an explicitly selected recipe), or null when it is not feed.</summary>
    public double? FeedKg(string? id, Func<string, bool> met, ChargeRecipe? selected = null)
    {
        if (id == null || selected != null && !selected.Requires.All(met)) return null;
        var recipes = selected != null ? new[] { selected } : Available(met);
        foreach (var recipe in recipes)
            foreach (var input in recipe.ItemInputs)
                if (input.Id == id) return input.Kg;
        return null;
    }
    /// <summary>Every feed identity any recipe of this machine takes.</summary>
    public IReadOnlyList<string> FeedIds => All.SelectMany(r => r.ItemInputs).Select(i => i.Id).Distinct(StringComparer.Ordinal).ToArray();
}
