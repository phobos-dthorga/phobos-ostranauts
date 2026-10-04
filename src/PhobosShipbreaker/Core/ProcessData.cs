using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Shipbreaker's <c>materials</c> data pack (<c>framework/materials.json</c>, Shipbreaker 0.46.0): furnace
/// housing stock, ingots, terminal remainders and the reclaimer and feed-family reject packets. Identities stay in
/// code; the pack is checked against the masses the recipes and family budgets are written for.</summary>
public static class ShipbreakerMaterials
{
    public const string Schema = MaterialSchema.Name, ModFolder = "PhobosShipbreaker", Resource = "PhobosShipbreaker.materials.json";
    public const string FurnacePacket = "furnace-packet", FurnaceHousing = "furnace-housing", ReclaimerPacket = "reclaimer-packet", Reject = "reject";
    /// <summary>The kind of a material a data file adds (Shipbreaker 0.75.0): an ordinary loose item, cast by an F6 recipe.</summary>
    public const string Stock = "stock";
    public static readonly IReadOnlyList<string> Kinds = new[] { FurnacePacket, FurnaceHousing, ReclaimerPacket, Reject, Stock };
    /// <summary>Every material the code builds from the pack, in definition order.</summary>
    public static IReadOnlyList<string> Ids => new[] { FurnaceRules.Blank, FurnaceRules.Housing, FurnaceRules.Remainder, FurnaceRecipes.AluminiumIngot, FurnaceRecipes.SteelIngot,
        FurnaceRecipes.SteelRemainder, ReclaimerRules.Feedstock, ReclaimerRules.Reject }.Concat(FeedFamilies.RejectKg.Keys).ToArray();
    private static MaterialPack? pack;
    public static MaterialPack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ModFolder, Schema, typeof(ShipbreakerMaterials).Assembly, Resource);
    public static MaterialPack Load()
    {
        pack = DataPacks.Load<MaterialPack>(Source, Check);
        return pack;
    }
    /// <summary>The rules every materials file is held to, on top of the shared schema.</summary>
    public static void Check(MaterialPack p)
    {
        // Add-ons and players may add materials of their own (Shipbreaker 0.75.0), as plain stock; ours keep their kinds.
        var ids = Ids;
        MaterialSchema.Validate(p, new MaterialContext(ids) { Kinds = Kinds, AllowAdditions = true });
        foreach (var m in p.materials)
            if (ids.Contains(m.Key) == (m.Value.kind == Stock)) throw new ArgumentException(Text.Get("Materials.added_kind", m.Key));
        // Masses the code's formulas and recipes are written for: the feed-family rejects, the reclaimer packets and the furnace products.
        foreach (var bound in FeedFamilies.RejectKg.Concat(new[] {
            new KeyValuePair<string, double>(ReclaimerRules.Feedstock, ReclaimerRules.InputKg), new KeyValuePair<string, double>(ReclaimerRules.Reject, ReclaimerRules.RejectKg),
            new KeyValuePair<string, double>(FurnaceRules.Blank, FurnaceRules.BlankKg), new KeyValuePair<string, double>(FurnaceRules.Remainder, FurnaceRules.RemainderKg),
            new KeyValuePair<string, double>(FurnaceRecipes.SteelRemainder, FurnaceRules.RemainderKg) }))
            if (Math.Abs(p.materials[bound.Key].kg - bound.Value) > ProcessRules.MassTolerance) throw new ArgumentException(Text.Get("Materials.bound_mass", bound.Key, bound.Value));
    }
    public static MaterialEntry Entry(string id) => Pack.materials.TryGetValue(id, out var e) ? e : throw new InvalidOperationException("No materials entry for " + id);
    /// <summary>A material's unit mass, for recipe checks; null for an id that is not ours.</summary>
    public static double? KgOf(string? id) => id != null && Pack.materials.TryGetValue(id, out var e) ? e.kg : null;
    /// <summary>The materials data files added, after ours, in id order.</summary>
    public static IReadOnlyList<KeyValuePair<string, MaterialEntry>> Added
    {
        get { var ids = Ids; return Pack.materials.Where(p => !ids.Contains(p.Key)).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray(); }
    }
    public static bool IsAdded(string? id) => id != null && Pack.materials.ContainsKey(id) && !Ids.Contains(id);
}

/// <summary>Shipbreaker's fixed recipes from the <c>process-recipes</c> data pack (Shipbreaker 0.46.0): the F6 charges
/// with their thermal profiles, the T2 thaw recipes and the R4 budget. Published revisions are frozen by hash. The
/// D4 feed families are formulas over part mass and stay in <see cref="FeedFamilies"/>.</summary>
public static class ShipbreakerRecipes
{
    public const string Schema = RecipeSchema.Name, Resource = "PhobosShipbreaker.process-recipes.json", FrozenResource = "PhobosShipbreaker.frozen-process-recipes.json";
    public const string Furnace = "furnace", ThawWater = "thaw-water", ThawMethane = "thaw-methane", Reclaimer = "reclaimer";
    public static readonly IReadOnlyList<string> Machines = new[] { Furnace, ThawWater, ThawMethane, Reclaimer };
    private static RecipePack? pack; private static RecipePack? builtFrom;
    private static readonly Dictionary<string, ProcessRecipeCatalog> catalogs = new(StringComparer.Ordinal);
    public static RecipePack Pack => pack ??= Load();
    public static DataPackSource Source => new(Text.Owner, ShipbreakerMaterials.ModFolder, Schema, typeof(ShipbreakerRecipes).Assembly, Resource);
    public static RecipePack Load(Func<string, double?>? nativeMass = null)
    {
        var frozen = RecipeFreeze.Read(typeof(ShipbreakerRecipes).Assembly, FrozenResource);
        var context = new RecipeContext
        {
            Machines = Machines, Requirements = Array.Empty<string>(), ReferenceK = FurnaceRules.ReferenceK,
            UnitMassOf = id => ShipbreakerMaterials.KgOf(id) ?? nativeMass?.Invoke(id),
            IsCommodity = id => id == ThawRules.Commodity || id == ThawRules.MethaneCommodity
        };
        pack = DataPacks.Load<RecipePack>(Source, (p, raw) => { RecipeSchema.Validate(p, context); Check(p); RecipeFreeze.Enforce(raw, frozen); });
        return pack;
    }
    /// <summary>Shipbreaker's own rules on top of the shared schema.</summary>
    private static void Check(RecipePack p)
    {
        foreach (var pair in p.recipes)
        {
            string id = pair.Key; var r = pair.Value;
            if (r.inputs.Count != 1) throw new ArgumentException(Text.Get("Recipes.one_input", id));
            if (r.machine == Furnace)
            {
                if (r.thermal == null) throw new ArgumentException(Text.Get("Recipes.furnace_thermal", id));
                if (Math.Abs(r.InputKg - FurnaceRules.ChargeUnits * FurnaceRules.FeedUnitKg) > ProcessRules.MassTolerance) throw new ArgumentException(Text.Get("Recipes.furnace_charge", id, FurnaceRules.ChargeUnits * FurnaceRules.FeedUnitKg));
                // Revision 1 is the original housing: every batch saved before 0.38.0 reinterprets its heat with these numbers.
                if (r.revision == FurnaceRules.RecipeRevision && !FurnaceProfile.IsAluminium(r.thermal)) throw new ArgumentException(Text.Get("Recipes.aluminium_profile", id));
            }
            else if (r.machine == ThawWater || r.machine == ThawMethane)
            {
                if (r.seconds == null) throw new ArgumentException(Text.Get("Recipes.seconds_required", id));
            }
        }
        foreach (string machine in Machines)
            if (!p.recipes.Values.Any(r => r.machine == machine)) throw new ArgumentException(Text.Get("Recipes.machine_missing", machine));
    }
    public static IEnumerable<KeyValuePair<string, RecipeEntry>> Of(string machine) => Pack.recipes.Where(p => p.Value.machine == machine).OrderBy(p => p.Value.revision);
    public static RecipeEntry Entry(string id) => Pack.recipes.TryGetValue(id, out var e) ? e : throw new InvalidOperationException("No recipe " + id);
    /// <summary>The Framework catalog of one machine: every revision, the highest current.</summary>
    public static ProcessRecipeCatalog Catalog(string machine)
    {
        lock (catalogs)
        {
            if (!ReferenceEquals(builtFrom, Pack)) { catalogs.Clear(); builtFrom = Pack; }
            if (catalogs.TryGetValue(machine, out var found)) return found;
            var recipes = Of(machine).Select(p => new ProcessRecipe(p.Value.revision, p.Value.InputKg, p.Value.products.Select(u => new ProductSpec(u.id, u.count, u.kg)), p.Value.legacySeconds)).ToArray();
            var catalog = new ProcessRecipeCatalog(recipes.Max(r => r.Revision), recipes);
            catalogs[machine] = catalog;
            return catalog;
        }
    }
}
