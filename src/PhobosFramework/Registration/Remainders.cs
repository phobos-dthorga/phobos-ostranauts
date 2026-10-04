using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>The terminal remainders every Phobos mod makes (Framework 0.87.0; owner rule, 4 October 2026: no trash-like
/// object is left to pile up without a consumer). A remainder is an item no recipe takes: slag, cakes, rejects, retained
/// waste. Content declares each one by definition id when it prepares its definitions, and Framework's own
/// <see cref="MaintenanceDefinitions.Remainder"/> declares the waste it creates. A consumer (Manufacturing's reaction
/// mass feeder) admits exactly the declared ids, so it can never eat ore, scrap or a product, and the native checks
/// refuse a Phobos trash item that is not declared. Identity is by definition id, never by a condition on the item, so
/// remainders already lying in a save count without any conversion.</summary>
public static class Remainders
{
    private static readonly HashSet<string> declared = new(StringComparer.Ordinal);
    /// <summary>Declares remainders by definition id. Declaring one twice is harmless.</summary>
    public static void Declare(params string[] ids)
    {
        if (ids == null) return;
        lock (declared)
            foreach (string id in ids)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A remainder needs a definition id.");
                declared.Add(id);
            }
    }
    public static void Declare(IEnumerable<string> ids) { foreach (string id in ids) Declare(id); }
    public static bool IsDeclared(string? id) { if (id == null) return false; lock (declared) return declared.Contains(id); }
    public static IReadOnlyList<string> All { get { lock (declared) { var all = new List<string>(declared); all.Sort(StringComparer.Ordinal); return all; } } }
}
