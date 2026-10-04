using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phobos.Ostranauts.Framework.Data;

/// <summary>The <c>outcomes</c> schema (Framework 0.88.0; owner direction, 4 October 2026: chance tables are data a
/// player may tune or add to, apart from the code). A table belongs to one base recipe, the one a player chooses or a
/// machine matches, and lists the recipes a charge of it may turn out to be, each with a whole-number weight. Every
/// outcome is an ordinary exact recipe in the owner's process-recipes pack, frozen and mass-balanced like any other;
/// this pack holds only the odds, which are balance and are not frozen.</summary>
public sealed class OutcomePack : DataPack
{
    /// <summary>Tables by base recipe id.</summary>
    public Dictionary<string, OutcomeTable> tables = new(StringComparer.Ordinal);
}

public sealed class OutcomeTable
{
    public string? notes;
    /// <summary>Weight by outcome recipe id. The base recipe is one of its own outcomes; a weight of 0 switches an
    /// outcome off.</summary>
    public Dictionary<string, int> outcomes = new(StringComparer.Ordinal);
}

/// <summary>What the owner of the recipes tells the validator about one recipe: its machine, and a signature that is
/// equal for two recipes exactly when a player loads and waits the same for both (item inputs, drawn commodities,
/// circulating volumes and duration).</summary>
public sealed class OutcomeRecipe
{
    public string Machine { get; }
    public string Signature { get; }
    public OutcomeRecipe(string machine, string signature) { Machine = machine ?? ""; Signature = signature ?? ""; }
}

public static class OutcomeSchema
{
    public const string Name = "outcomes";
    public const int MaxWeight = 10000;

    /// <summary>The checks every file passes, shipped or player. Whether a table is generous is an authoring rule for
    /// shipped data and is not enforced here.</summary>
    public static void Validate(OutcomePack pack, Func<string, OutcomeRecipe?> recipe)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (recipe == null) throw new ArgumentNullException(nameof(recipe));
        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in pack.tables)
        {
            string id = pair.Key; var table = pair.Value;
            if (table == null) throw new ArgumentException(Text.Get("OutcomeSchema.table_empty", id));
            var baseRecipe = recipe(id) ?? throw new ArgumentException(Text.Get("OutcomeSchema.unknown_recipe", id, id));
            if (!table.outcomes.ContainsKey(id)) throw new ArgumentException(Text.Get("OutcomeSchema.base_missing", id));
            long total = 0;
            foreach (var outcome in table.outcomes)
            {
                if (outcome.Value < 0 || outcome.Value > MaxWeight) throw new ArgumentException(Text.Get("OutcomeSchema.weight", id, outcome.Key, MaxWeight));
                var other = recipe(outcome.Key) ?? throw new ArgumentException(Text.Get("OutcomeSchema.unknown_recipe", id, outcome.Key));
                if (other.Machine != baseRecipe.Machine || other.Signature != baseRecipe.Signature) throw new ArgumentException(Text.Get("OutcomeSchema.different_charge", id, outcome.Key));
                if (seen.TryGetValue(outcome.Key, out var owner)) throw new ArgumentException(Text.Get("OutcomeSchema.shared", outcome.Key, owner, id));
                seen[outcome.Key] = id;
                total += outcome.Value;
            }
            if (total < 1) throw new ArgumentException(Text.Get("OutcomeSchema.no_weight", id));
        }
    }
}

/// <summary>The pick itself, with no game types. The same units always give the same outcome, in whatever order they
/// are named, so saving, reloading, cancelling and starting again can never reroll a charge; a different set of units is
/// a different draw, which can only be seen by using the units up.</summary>
public static class Outcomes
{
    /// <summary>FNV-1a over the unit ids, sorted and joined: stable across runs, machines and runtimes (unlike a
    /// string's own hash code).</summary>
    public static uint Hash(IEnumerable<string> unitIds)
    {
        var ids = (unitIds ?? throw new ArgumentNullException(nameof(unitIds))).ToArray();
        Array.Sort(ids, StringComparer.Ordinal);
        uint hash = 2166136261;
        foreach (byte b in Encoding.UTF8.GetBytes(string.Join("\n", ids))) { hash ^= b; hash *= 16777619; }
        // A final avalanche, so ids that differ only in their last characters spread over the whole range.
        hash ^= hash >> 16; hash *= 0x85ebca6b; hash ^= hash >> 13; hash *= 0xc2b2ae35; hash ^= hash >> 16;
        return hash;
    }
    /// <summary>The outcome for these units: outcomes are walked in ordinal id order and the hash, modulo the total
    /// weight, falls in exactly one of them. Outcomes with a weight of 0 are never picked.</summary>
    public static string Pick(IReadOnlyDictionary<string, int> weights, IEnumerable<string> unitIds)
    {
        if (weights == null) throw new ArgumentNullException(nameof(weights));
        long total = 0;
        foreach (var w in weights.Values) { if (w < 0) throw new ArgumentException("A weight cannot be negative."); total += w; }
        if (total < 1) throw new ArgumentException("An outcome table needs some weight.");
        long point = Hash(unitIds) % total;
        foreach (var pair in weights.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (point < pair.Value) return pair.Key;
            point -= pair.Value;
        }
        throw new InvalidOperationException("The pick fell outside the table.");
    }
}
