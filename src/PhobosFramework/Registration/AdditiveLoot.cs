using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Add one optional item choice to future native loot rolls; never touch inventories.</summary>
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
        if (!definitions.Loot.TryGetValue(tableId, out var table))
        {
            if (!DataHandler.dictLoot.TryGetValue(tableId, out var source) || source.strType != "item")
                throw new ArgumentException(Text.Get("AdditiveLoot.missing_table", tableId));
            table = NativeDefinitions.Clone(source);
            definitions.Loot.Add(tableId, table);
        }
        if (table.strType != "item")
            throw new ArgumentException(Text.Get("AdditiveLoot.missing_table", tableId));
        // Remove only our standalone branch. Composite foreign expressions stay intact.
        var retained = (table.aLoots ?? Array.Empty<string>()).Where(entry =>
            entry == null || !entry.StartsWith(branchId + "=", StringComparison.Ordinal) || entry.Contains("|")).ToArray();
        var enabled = chances.Where(p => p.Value > 0).OrderBy(p => p.Key, StringComparer.Ordinal).ToArray();
        table.aLoots = enabled.Length == 0 ? retained : retained.Concat(new[] { branchId + "=1x1" }).ToArray();
        // One native cumulative-choice expression means at most one new item per roll.
        definitions.Loot[branchId] = new Loot { strName = branchId, strType = "item",
            aCOs = enabled.Length == 0 ? Array.Empty<string>() : new[] { string.Join("|", enabled.Select(p =>
                p.Key + "=" + p.Value.ToString("R", CultureInfo.InvariantCulture) + "x1")) },
            aLoots = Array.Empty<string>() };
    }

    private static bool SafeId(string id) => !string.IsNullOrWhiteSpace(id) &&
        id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.');
}
