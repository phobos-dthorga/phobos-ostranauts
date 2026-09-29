using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Registration;

/// <summary>Definition-id lookups for family registrations (a bulk vessel spec, a gas store size, a machine kind).
/// Families are declared by prefix and matched through <see cref="EquipmentIdentity.IsFamily"/>; the answer for each
/// definition id is remembered, so the hot paths that classify every appliance in the world pay one dictionary probe
/// instead of a scan over every registered family with string comparisons. This indexes immutable definitions, never
/// live membership: which objects exist and where is always read fresh (audit P4).</summary>
public sealed class DefinitionIndex<T> where T : class
{
    /// <summary>Distinct definition ids are finite; the bound only guards against runaway garbage strings.</summary>
    public const int MemoLimit = 65536;
    private readonly List<(string Prefix, T Value)> families = new();
    private readonly Dictionary<string, T?> resolved = new(StringComparer.Ordinal);
    public int Count => families.Count;
    public IEnumerable<T> Values { get { foreach (var f in families) yield return f.Value; } }
    /// <summary>Registers a family by its definition prefix; later registrations of the same prefix win.</summary>
    public void Add(string prefix, T value)
    {
        if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("A family needs a definition prefix.");
        if (value == null) throw new ArgumentNullException(nameof(value));
        families.RemoveAll(f => f.Prefix == prefix);
        families.Add((prefix, value)); resolved.Clear();
    }
    public int Remove(Func<T, bool> predicate)
    {
        int removed = families.RemoveAll(f => predicate(f.Value));
        if (removed > 0) resolved.Clear();
        return removed;
    }
    public void Clear() { families.Clear(); resolved.Clear(); }
    /// <summary>The family value for a definition id, or null. Allocation-free after the first look at each id.</summary>
    public T? Get(string? definition)
    {
        if (definition == null) return null;
        if (resolved.TryGetValue(definition, out var found)) return found;
        T? value = null;
        foreach (var f in families) if (EquipmentIdentity.IsFamily(definition, f.Prefix)) { value = f.Value; break; }
        if (resolved.Count < MemoLimit) resolved[definition] = value;
        return value;
    }
    public bool Contains(string? definition) => Get(definition) != null;
}
