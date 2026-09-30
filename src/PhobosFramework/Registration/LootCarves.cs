using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>One carved share: <paramref name="Share"/> of <paramref name="Donor"/>'s probability given to a choice.</summary>
public sealed class LootCarve
{
    public string Donor { get; }
    public double Share { get; }
    public LootCarve(string donor, double share) { Donor = donor; Share = share; }
}

/// <summary>Pure arithmetic for carved loot shares. A native cumulative choice
/// (<c>a=0.4x1|b=0.3x1</c>) rolls one random number against consecutive bands, so a new choice
/// inserted immediately after its donor, with the donor reduced by the same amount, leaves every
/// other unit's band exactly where it was and the table's total unchanged.</summary>
public static class LootCarveMath
{
    /// <summary>Tolerance for a donor that must still cover the shares carved from it.</summary>
    public const double Epsilon = 1e-9;

    /// <summary>The native expression index and unit index of the donor, or null when it is missing or
    /// appears more than once (a negative expression, or any second appearance, makes it ambiguous).</summary>
    public static (int Expression, int Unit, double Chance)? Locate(IReadOnlyList<string> expressions, string donor, out string reason)
    {
        (int, int, double)? found = null; reason = "";
        for (int e = 0; e < expressions.Count; e++)
        {
            string expression = expressions[e];
            if (string.IsNullOrEmpty(expression)) continue;
            string[] units = expression.Split('|');
            for (int u = 0; u < units.Length; u++)
            {
                string name = units[u].Split('=')[0];
                if (expression[0] == '-') name = name.TrimStart('-');
                if (name != donor) continue;
                if (found != null || expression[0] == '-') { reason = "ambiguous"; return null; }
                if (!TryChance(units[u], out double chance)) { reason = "unreadable"; return null; }
                found = (e, u, chance);
            }
        }
        if (found == null) reason = "missing";
        return found;
    }

    /// <summary>Render a table's expressions from its original native form and every carve on it.
    /// Carves apply in ordinal choice order; one that its donor can no longer cover, or whose donor is
    /// missing or ambiguous, is refused and left out. A zero share renders nothing, so the original
    /// returns unchanged when every share is zero.</summary>
    public static string[] Render(IReadOnlyList<string> original, IReadOnlyDictionary<string, LootCarve> carves,
        out IReadOnlyList<(string Choice, string Reason)> refused)
    {
        if (original == null) throw new ArgumentNullException(nameof(original));
        if (carves == null) throw new ArgumentNullException(nameof(carves));
        var rejected = new List<(string, string)>();
        var result = original.ToArray();
        // Per donor: the accepted choices in order, and the donor's remaining chance.
        var accepted = new Dictionary<string, List<(string Choice, double Share)>>(StringComparer.Ordinal);
        var located = new Dictionary<string, (int Expression, int Unit, double Chance)>(StringComparer.Ordinal);
        foreach (var pair in carves.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            string choice = pair.Key; var carve = pair.Value;
            if (!(carve.Share > 0)) continue;
            if (!located.TryGetValue(carve.Donor, out var at))
            {
                var found = Locate(original, carve.Donor, out string reason);
                if (found == null) { rejected.Add((choice, reason)); continue; }
                located[carve.Donor] = at = found.Value;
                accepted[carve.Donor] = new List<(string, double)>();
            }
            double taken = accepted[carve.Donor].Sum(c => c.Share);
            if (carve.Share > at.Chance - taken + Epsilon) { rejected.Add((choice, "insufficient")); continue; }
            accepted[carve.Donor].Add((choice, carve.Share));
        }
        foreach (var pair in accepted.Where(p => p.Value.Count > 0))
        {
            var at = located[pair.Key];
            var units = result[at.Expression].Split('|').ToList();
            string donorUnit = units[at.Unit];
            double remaining = Math.Max(0, Math.Round(at.Chance - pair.Value.Sum(c => c.Share), 6));
            int x = donorUnit.IndexOf('x', donorUnit.IndexOf('=') + 1);
            string count = x < 0 ? "" : donorUnit.Substring(x);
            units[at.Unit] = pair.Key + "=" + Format(remaining) + count;
            units.InsertRange(at.Unit + 1, pair.Value.Select(c => c.Choice + "=" + Format(c.Share) + "x1"));
            result[at.Expression] = string.Join("|", units);
        }
        refused = rejected;
        return result;
    }

    private static string Format(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static bool TryChance(string unit, out double chance)
    {
        chance = 0;
        int eq = unit.IndexOf('=');
        if (eq < 0) return false;
        string rest = unit.Substring(eq + 1);
        int x = rest.IndexOf('x');
        return double.TryParse(x < 0 ? rest : rest.Substring(0, x), NumberStyles.Float, CultureInfo.InvariantCulture, out chance) &&
               !double.IsNaN(chance) && !double.IsInfinity(chance) && chance >= 0;
    }
}

/// <summary>The live side of carved loot: each carved native table's original expressions and what we
/// last wrote, so every set's carves re-render together, order-independently, and a table that another
/// mod has since rewritten is left alone and reported rather than guessed at.</summary>
internal static class LootCarveRegistry
{
    private sealed class Entry
    {
        public Loot Table = null!;
        public string[] Original = Array.Empty<string>();
        public string[] Written = Array.Empty<string>();
        public readonly Dictionary<string, LootCarve> Carves = new Dictionary<string, LootCarve>(StringComparer.Ordinal);
    }
    private static readonly Dictionary<string, Entry> tables = new Dictionary<string, Entry>(StringComparer.Ordinal);
    internal static Action<string> Log = _ => { };

    /// <summary>A new data load builds new native tables; forget the previous load's.</summary>
    internal static void Reset() => tables.Clear();

    /// <summary>The table's native expressions before any carve, for a prepare-time donor check.</summary>
    internal static IReadOnlyList<string>? Original(string tableId, Loot table) =>
        tables.TryGetValue(tableId, out var e) && ReferenceEquals(e.Table, table) ? e.Original : null;

    /// <summary>Read-only report for <c>phobosframework loot [table]</c>: a table's live expressions as the game rolls
    /// them now and the Phobos shares carved into it, or, with no table, which tables carry shares.</summary>
    internal static string Describe(string? tableId)
    {
        if (string.IsNullOrWhiteSpace(tableId))
            return tables.Count == 0 ? Text.Get("LootConsole.none")
                : Text.Get("LootConsole.carved_tables", string.Join(", ", tables.Keys.OrderBy(k => k, StringComparer.Ordinal)));
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.TryGetValue(tableId!, out var table)) return Text.Get("LootConsole.missing", tableId!);
        var lines = new List<string> { Text.Get("LootConsole.table", tableId!) };
        lines.AddRange((table.aCOs ?? Array.Empty<string>()).Select(e => "  " + e));
        if (tables.TryGetValue(tableId!, out var entry) && ReferenceEquals(entry.Table, table) && entry.Carves.Count > 0)
        {
            lines.Add(Text.Get("LootConsole.carves"));
            lines.AddRange(entry.Carves.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => Text.Get("LootConsole.carve", c.Key, c.Value.Share, c.Value.Donor)));
        }
        else lines.Add(Text.Get("LootConsole.no_carves"));
        return string.Join("\n", lines);
    }
    internal static void Apply(string tableId, string choice, LootCarve carve)
    {
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.TryGetValue(tableId, out var table))
        {
            Log(Text.Get("LootCarves.missing_table", tableId, choice));
            return;
        }
        var current = table.aCOs ?? Array.Empty<string>();
        if (!tables.TryGetValue(tableId, out var entry) || !ReferenceEquals(entry.Table, table))
            tables[tableId] = entry = new Entry { Table = table, Original = current.ToArray(), Written = current.ToArray() };
        else if (!current.SequenceEqual(entry.Written))
        {
            Log(Text.Get("LootCarves.foreign_edit", tableId, choice));
            return;
        }
        entry.Carves[choice] = carve;
        var next = LootCarveMath.Render(entry.Original, entry.Carves, out var refused);
        foreach (var r in refused) Log(Text.Get("LootCarves.refused", tableId, r.Choice, entry.Carves[r.Choice].Donor, r.Reason));
        // Reassign, never mutate: the setter re-parses, and the game caches parsed units by expression.
        if (!next.SequenceEqual(current)) table.aCOs = next;
        entry.Written = next;
    }
}
