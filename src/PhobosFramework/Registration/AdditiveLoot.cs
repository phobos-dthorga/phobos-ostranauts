using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Add one optional item choice to future native loot rolls; never touch inventories.
/// The choice is our own loot definition; the native table gains (or loses) one link to it in
/// place when the set publishes, so the game's own table object and every other entry stay.</summary>
public static class AdditiveLoot
{
    public static void SetItemChoice(NativeDefinitions definitions, string tableId, string branchId,
        IReadOnlyDictionary<string, double> chances)
    {
        if (!SafeId(branchId) || branchId == tableId || !branchId.StartsWith("Phobos", StringComparison.Ordinal) ||
            chances == null || chances.Count == 0 || chances.Any(p => !SafeId(p.Key) ||
                double.IsNaN(p.Value) || double.IsInfinity(p.Value) || p.Value < 0 || p.Value > 1) ||
            chances.Sum(p => p.Value) > 1)
            throw new ArgumentException(Text.Get("AdditiveLoot.invalid_choice"));
        if (!DataHandler.dictLoot.TryGetValue(tableId, out var table) || table.strType != "item")
            throw new ArgumentException(Text.Get("AdditiveLoot.missing_table", tableId));
        var enabled = chances.Where(p => p.Value > 0).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
        // One native cumulative-choice expression means at most one new item per roll.
        definitions.Loot[branchId] = new Loot { strName = branchId, strType = "item",
            aCOs = enabled.Length == 0 ? Array.Empty<string>() : new[] { string.Join("|", enabled.Select(p =>
                p.Key + "=" + p.Value.ToString("R", CultureInfo.InvariantCulture) + "x1")) },
            aLoots = Array.Empty<string>() };
        if (!definitions.LootBranches.TryGetValue(tableId, out var branches))
            definitions.LootBranches[tableId] = branches = new HashSet<string>(StringComparer.Ordinal);
        if (enabled.Length == 0) branches.Remove(branchId); else branches.Add(branchId);
        definitions.Amend(() => Link(tableId, branchId, enabled.Length > 0));
    }

    /// <summary>Carve <paramref name="share"/> of one native unit's probability into a new choice, placed
    /// immediately after that donor in the same cumulative expression, so no other unit's odds change and
    /// the table yields no extra rolls (owner loot policy, 30 September 2026). Works on native
    /// <c>item</c> tables and on <c>ship</c> tables such as the asteroid-field pickers, where an appended
    /// branch could never be chosen. Several sets may carve one donor; they compose order-independently.
    /// A zero share restores the donor. The live table is amended in place at publication.</summary>
    public static void CarveChoice(NativeDefinitions definitions, string tableId, string donorId, string choiceId, double share)
    {
        if (definitions == null) throw new ArgumentNullException(nameof(definitions));
        if (!SafeId(tableId) || !SafeId(donorId) || !SafeId(choiceId) || donorId == choiceId ||
            double.IsNaN(share) || double.IsInfinity(share) || share < 0 || share > 1)
            throw new ArgumentException(Text.Get("LootCarves.invalid", tableId, donorId, choiceId));
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.TryGetValue(tableId, out var table) ||
            (table.strType != "item" && table.strType != "ship") || definitions.Loot.ContainsKey(tableId))
            throw new ArgumentException(Text.Get("LootCarves.missing_table", tableId, choiceId));
        bool known = table.strType == "ship"
            ? DataHandler.dictShips?.ContainsKey(choiceId) == true || DataHandler.dictAsteroidClusterBlueprints?.ContainsKey(choiceId) == true
            : definitions.Objects.ContainsKey(choiceId) || DataHandler.dictCOs.ContainsKey(choiceId) ||
              definitions.Loot.ContainsKey(choiceId) || DataHandler.dictLoot.ContainsKey(choiceId);
        if (!known) throw new ArgumentException(Text.Get("LootCarves.unknown_choice", tableId, choiceId));
        // Fail at preparation when the donor cannot carry this share even alone; composition with other
        // sets' carves on the same donor is settled, and reported, when the table renders.
        var native = LootCarveRegistry.Original(tableId, table) ?? table.aCOs ?? Array.Empty<string>();
        var donor = LootCarveMath.Locate(native, donorId, out string reason);
        if (donor == null) throw new ArgumentException(Text.Get("LootCarves.refused", tableId, choiceId, donorId, reason));
        if (share > donor.Value.Chance + LootCarveMath.Epsilon)
            throw new ArgumentException(Text.Get("LootCarves.refused", tableId, choiceId, donorId, "insufficient"));
        var carve = new LootCarve(donorId, share);
        if (!definitions.LootCarves.TryGetValue(tableId, out var carves))
            definitions.LootCarves[tableId] = carves = new Dictionary<string, LootCarve>(StringComparer.Ordinal);
        carves[choiceId] = carve;
        definitions.Amend(() => LootCarveRegistry.Apply(tableId, choiceId, carve));
    }

    /// <summary>Set or clear our standalone link on the live native table. Composite foreign
    /// expressions and every other entry stay; assigning the list re-parses the native units.</summary>
    internal static void Link(string tableId, string branchId, bool enabled)
    {
        if (!DataHandler.dictLoot.TryGetValue(tableId, out var table)) return;
        var current = table.aLoots ?? Array.Empty<string>();
        var retained = current.Where(entry =>
            entry == null || !entry.StartsWith(branchId + "=", StringComparison.Ordinal) || entry.Contains("|")).ToArray();
        var next = enabled ? retained.Concat(new[] { branchId + "=1x1" }).ToArray() : retained;
        if (next.Length == current.Length && !next.Except(current).Any() && !current.Except(next).Any()) return;
        table.aLoots = next;
    }

    private static bool SafeId(string id) => !string.IsNullOrWhiteSpace(id) &&
        id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
}
