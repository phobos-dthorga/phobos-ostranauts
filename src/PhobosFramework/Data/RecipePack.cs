using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Processing;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>process-recipes</c> schema: what a machine turns a charge into. One entry per recipe, keyed by
/// recipe id; the entry names its machine and carries the revision a saved job binds to. Every recipe conserves
/// mass (inputs = products + off-gas) and breathes only the game's own gases; the owner ruling (30 September 2026)
/// enforces both on every file, shipped or player. Formula-defined catalogs (the D4 feed families) stay in code.</summary>
public sealed class RecipePack : DataPack
{
    public Dictionary<string, RecipeEntry> recipes = new(StringComparer.Ordinal);
}

public sealed class RecipeUnit
{
    public string id = "";
    public int count = 1;
    public double kg;
}

public sealed class ThermalEntry
{
    public double meltK, targetK, solidCp, liquidCp, latentKJ, holdSeconds;
}

public sealed class RecipeEntry
{
    public string? notes;
    /// <summary>Which catalog the recipe belongs to: <c>refinery</c>, <c>furnace</c>, <c>thaw</c>, <c>reclaimer</c>.</summary>
    public string machine = "";
    /// <summary>The saved identity of a bound job. Published revisions are frozen by hash; a change adds a revision.</summary>
    public int revision;
    public List<RecipeUnit> inputs = new();
    /// <summary>Items by definition id, or commodities (<c>water</c>, <c>methane</c>, <c>ammonia</c>) delivered to a vessel or store.</summary>
    public List<RecipeUnit> products = new();
    /// <summary>Native room species breathed into the room over the job, in kilograms.</summary>
    public Dictionary<string, double> offGas = new(StringComparer.Ordinal);
    /// <summary>Powered duration, where the recipe fixes it (a player setting or the machine may fix it instead).</summary>
    public double? seconds;
    /// <summary>Duration for saved jobs that predate a saved duration; only revisions actually shipped without one.</summary>
    public double? legacySeconds;
    /// <summary>A melt: a long interruption freezes it and the charge is lost.</summary>
    public bool melt;
    /// <summary>Feature keys the owner must satisfy before the recipe is offered (for example another mod's stock).</summary>
    public List<string> requires = new();
    public ThermalEntry? thermal;
    /// <summary>A working volume of a commodity the charge needs present and returns (a leach's circulating water):
    /// checked before and during the charge, never part of the mass balance.</summary>
    public Dictionary<string, double> circulates = new(StringComparer.Ordinal);
    /// <summary>Heat the reaction itself releases into the room over the charge, in kWh (negative when it absorbs
    /// heat from the machine's own electricity). Absent means none is modelled.</summary>
    public double? reactionKWh;
    public double InputKg => inputs.Sum(i => i.count * i.kg);
    public double ProductKg => products.Sum(p => p.count * p.kg);
    public double OffGasKg => offGas.Values.Sum();
}

/// <summary>What the owner knows that the file cannot: which machines and requirement keys exist, what each item
/// weighs (null for an unknown id, so an offline check skips it), and which product ids are commodities.</summary>
public sealed class RecipeContext
{
    public IReadOnlyCollection<string>? Machines { get; set; }
    public IReadOnlyCollection<string>? Requirements { get; set; }
    public Func<string, double?>? UnitMassOf { get; set; }
    public Func<string, bool>? IsCommodity { get; set; }
    public double MassToleranceKg { get; set; } = Units.MassToleranceKg;
    /// <summary>A thermal profile must melt above this temperature (the furnace's reference).</summary>
    public double ReferenceK { get; set; } = 0;
}

public static class RecipeSchema
{
    public const string Name = "process-recipes";
    /// <summary>Bound on the heat a single charge may declare, either sign.</summary>
    public const double MaximumReactionKWh = 1000;
    public static void Validate(RecipePack pack, RecipeContext context)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (pack.recipes.Count == 0) throw new ArgumentException(Text.Get("RecipeSchema.empty"));
        var revisions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pair in pack.recipes)
        {
            string id = pair.Key; var r = pair.Value;
            if (string.IsNullOrWhiteSpace(id) || id.Any(c => !(char.IsLetterOrDigit(c) || c == '-'))) throw new ArgumentException(Text.Get("RecipeSchema.id", id));
            if (string.IsNullOrWhiteSpace(r.machine)) throw new ArgumentException(Text.Get("RecipeSchema.machine", id));
            if (context.Machines != null && !context.Machines.Contains(r.machine)) throw new ArgumentException(Text.Get("RecipeSchema.unknown_machine", id, r.machine));
            if (r.revision < 1) throw new ArgumentException(Text.Get("RecipeSchema.revision", id));
            if (!revisions.Add(r.machine + "@" + r.revision)) throw new ArgumentException(Text.Get("RecipeSchema.duplicate_revision", id, r.machine, r.revision));
            if (r.inputs.Count == 0 || r.products.Count == 0) throw new ArgumentException(Text.Get("RecipeSchema.units", id));
            foreach (var unit in r.inputs.Concat(r.products))
            {
                if (string.IsNullOrWhiteSpace(unit.id) || unit.count < 1 || !Finite(unit.kg) || unit.kg <= 0) throw new ArgumentException(Text.Get("RecipeSchema.unit", id, unit.id));
                bool commodity = context.IsCommodity?.Invoke(unit.id) == true;
                double? known = commodity ? null : context.UnitMassOf?.Invoke(unit.id);
                if (known is double kg && Math.Abs(kg - unit.kg) > context.MassToleranceKg) throw new ArgumentException(Text.Get("RecipeSchema.unit_mass", id, unit.id, unit.kg, kg));
            }
            if (r.inputs.Select(u => u.id).Distinct(StringComparer.Ordinal).Count() != r.inputs.Count || r.products.Select(u => u.id).Distinct(StringComparer.Ordinal).Count() != r.products.Count)
                throw new ArgumentException(Text.Get("RecipeSchema.duplicate_unit", id));
            foreach (var pair2 in r.circulates)
                if (context.IsCommodity?.Invoke(pair2.Key) != true || !Finite(pair2.Value) || pair2.Value <= 0) throw new ArgumentException(Text.Get("RecipeSchema.circulates", id, pair2.Key));
            if (r.reactionKWh is double reaction && (!Finite(reaction) || Math.Abs(reaction) > MaximumReactionKWh)) throw new ArgumentException(Text.Get("RecipeSchema.reaction", id, MaximumReactionKWh));
            foreach (var gas in r.offGas)
            {
                if (!NativeGasCanister.IsRoomSpecies(gas.Key)) throw new ArgumentException(Text.Get("RecipeSchema.species", id, gas.Key));
                if (!Finite(gas.Value) || gas.Value <= 0) throw new ArgumentException(Text.Get("RecipeSchema.gas_kg", id, gas.Key));
            }
            double inKg = r.InputKg, outKg = r.ProductKg + r.OffGasKg;
            if (Math.Abs(inKg - outKg) > context.MassToleranceKg) throw new ArgumentException(Text.Get("RecipeSchema.mass", id, inKg, outKg));
            foreach (var (label, value) in new[] { ("seconds", r.seconds), ("legacySeconds", r.legacySeconds) })
                if (value is double s && (!Finite(s) || s < ProcessJob.MinSeconds || s > ProcessJob.MaxSeconds)) throw new ArgumentException(Text.Get("RecipeSchema.seconds", id, label, ProcessJob.MinSeconds, ProcessJob.MaxSeconds));
            foreach (string requirement in r.requires)
                if (string.IsNullOrWhiteSpace(requirement) || (context.Requirements != null && !context.Requirements.Contains(requirement))) throw new ArgumentException(Text.Get("RecipeSchema.requirement", id, requirement));
            if (r.thermal is ThermalEntry t)
            {
                foreach (double v in new[] { t.meltK, t.targetK, t.solidCp, t.liquidCp, t.latentKJ, t.holdSeconds }) if (!Finite(v) || v <= 0) throw new ArgumentException(Text.Get("RecipeSchema.thermal", id));
                if (t.meltK <= context.ReferenceK || t.targetK <= t.meltK) throw new ArgumentException(Text.Get("RecipeSchema.thermal", id));
            }
        }
    }
    private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}

/// <summary>Published recipe revisions are immutable: a saved job stores only its revision and reads the products
/// back from the catalog. The freeze file (<c>frozen-process-recipes.json</c>, embedded beside the pack and written by
/// <c>scripts/freeze-recipes.py</c>) records a hash of every published <c>machine@revision</c>; a shipped or player
/// entry whose revision is frozen must hash the same, and a frozen revision may never disappear. Changing a recipe
/// therefore means adding a revision. The hash is over the entry's JSON with <c>notes</c> removed, keys sorted and
/// no whitespace, so the Python and C# sides agree.</summary>
public static class RecipeFreeze
{
    public sealed class Frozen
    {
        public int schemaVersion;
        public Dictionary<string, string> revisions = new(StringComparer.Ordinal);
    }
    public static Frozen Read(System.Reflection.Assembly assembly, string resourceName)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(Text.Get("DataPacks.missing_resource", resourceName));
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return Parse(reader.ReadToEnd());
    }
    public static Frozen Parse(string json)
    {
        var frozen = JsonConvert.DeserializeObject<Frozen>(json, new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error, MaxDepth = 4 })
            ?? throw new FormatException(Text.Get("RecipeFreeze.unreadable"));
        if (frozen.schemaVersion != 1) throw new FormatException(Text.Get("RecipeFreeze.unreadable"));
        return frozen;
    }
    /// <summary>The canonical text of one raw entry: <c>notes</c> removed at the top level, keys sorted at every level.</summary>
    public static string Canonical(JObject entry)
    {
        var copy = (JObject)entry.DeepClone();
        copy.Remove("notes");
        return Sorted(copy).ToString(Formatting.None);
    }
    private static JToken Sorted(JToken token) => token switch
    {
        JObject o => new JObject(o.Properties().OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => new JProperty(p.Name, Sorted(p.Value)))),
        JArray a => new JArray(a.Select(Sorted)),
        _ => token
    };
    public static string Hash(JObject entry)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(Canonical(entry))).Select(b => b.ToString("x2")));
    }
    /// <summary>Throws when a frozen revision is missing from the raw pack or hashes differently.</summary>
    public static void Enforce(JObject rawPack, Frozen frozen)
    {
        if (rawPack == null) throw new ArgumentNullException(nameof(rawPack));
        if (frozen == null) throw new ArgumentNullException(nameof(frozen));
        var entries = rawPack["recipes"] as JObject ?? new JObject();
        var byRevision = new Dictionary<string, (string Id, JObject Entry)>(StringComparer.Ordinal);
        foreach (var property in entries.Properties())
        {
            if (property.Value is not JObject entry) continue;
            string key = (string?)entry["machine"] + "@" + (entry["revision"]?.ToString() ?? "");
            byRevision[key] = (property.Name, entry);
        }
        foreach (var pair in frozen.revisions)
        {
            if (!byRevision.TryGetValue(pair.Key, out var found)) throw new ArgumentException(Text.Get("RecipeFreeze.missing", pair.Key));
            string hash = Hash(found.Entry);
            if (!string.Equals(hash, pair.Value, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException(Text.Get("RecipeFreeze.changed", found.Id, pair.Key));
        }
    }
}
