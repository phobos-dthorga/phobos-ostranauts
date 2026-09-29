using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Discovery;

/// <summary>Which world objects belong to registered definition families, found by a sweep that can be spread across
/// frames. Membership is only ever grown by the sweep and read back through the live test, so an object that left the
/// world (destroyed, unregistered, renamed away) drops out on the next read, never later. Pure logic: the native
/// adapter <see cref="WorldFamilies"/> supplies the world and the live test.</summary>
public sealed class WorldIndex<T> where T : class
{
    public const int MaximumFamilies = 32;
    /// <summary>Distinct definition ids are finite; the bound only guards against runaway garbage strings.</summary>
    public const int MemoLimit = 65536;
    private readonly Func<T, string?> definition;
    private readonly Func<T, bool> live;
    private readonly List<(string Key, Func<string, bool> Member)> families = new();
    private readonly Dictionary<string, int> masks = new(StringComparer.Ordinal);
    private readonly List<List<T>> members = new();
    private readonly List<HashSet<T>> sets = new();
    // The sweep's snapshot of the world, taken by one bulk copy of references rather than an enumeration.
    private T[] sweep = Array.Empty<T>();
    private int sweepCount, cursor;

    public WorldIndex(Func<T, string?> definition, Func<T, bool> live)
    {
        this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        this.live = live ?? throw new ArgumentNullException(nameof(live));
    }

    public int FamilyCount => families.Count;
    /// <summary>A full sweep has completed since the last reset.</summary>
    public bool Primed { get; private set; }
    public bool Sweeping => cursor < sweepCount;
    /// <summary>Objects of the current sweep not yet examined.</summary>
    public int Pending => sweepCount - cursor;

    /// <summary>Registers a family by a definition-id predicate; the same key replaces its predicate. The index then
    /// needs a fresh sweep before that family is complete; other families keep their members.</summary>
    public int Register(string key, Func<string, bool> member)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("A family needs a key.");
        if (member == null) throw new ArgumentNullException(nameof(member));
        int id = families.FindIndex(f => f.Key == key);
        if (id >= 0) families[id] = (key, member);
        else
        {
            if (families.Count >= MaximumFamilies) throw new InvalidOperationException("Too many world families.");
            families.Add((key, member)); members.Add(new List<T>()); sets.Add(new HashSet<T>(ReferenceComparer.Instance));
            id = families.Count - 1;
        }
        Forget(id);
        return id;
    }

    /// <summary>A family that is no longer wanted keeps its slot but matches nothing.</summary>
    public void Unregister(string key)
    {
        int id = families.FindIndex(f => f.Key == key);
        if (id < 0) return;
        families[id] = (key, _ => false);
        Forget(id);
    }

    // A family's predicate changed: its members and every remembered answer go, and a full sweep is needed again.
    private void Forget(int family)
    {
        members[family].Clear(); sets[family].Clear();
        masks.Clear(); EndSweep(); Primed = false;
    }

    /// <summary>Forgets every member, remembered definition answer and sweep in progress (world or content change).</summary>
    public void Reset()
    {
        foreach (var list in members) list.Clear();
        foreach (var set in sets) set.Clear();
        masks.Clear(); EndSweep(); Primed = false;
    }

    /// <summary>Starts a sweep over a snapshot of the world, replacing any sweep in progress. The collection's own bulk
    /// copy takes the snapshot; missing entries are skipped as the sweep reaches them.</summary>
    public void Begin(ICollection<T> world)
    {
        EndSweep();
        int count = world.Count;
        if (sweep.Length < count) sweep = new T[Math.Max(count, sweep.Length * 2)];
        world.CopyTo(sweep, 0);
        sweepCount = count;
        if (count == 0) Primed = true;
    }

    // Releases the snapshot so objects that left the world are not kept alive by it.
    private void EndSweep() { if (sweepCount > 0) Array.Clear(sweep, 0, sweepCount); sweepCount = 0; cursor = 0; }

    /// <summary>Examines up to <paramref name="count"/> objects of the current sweep; returns how many were examined.</summary>
    public int Advance(int count)
    {
        if (count <= 0 || cursor >= sweepCount) return 0;
        int end = count >= sweepCount - cursor ? sweepCount : cursor + count, examined = end - cursor;
        for (int i = cursor; i < end; i++) Offer(sweep[i]);
        cursor = end;
        if (cursor >= sweepCount) { EndSweep(); Primed = true; }
        return examined;
    }

    /// <summary>Adds the object to every family its definition belongs to.</summary>
    public void Offer(T item)
    {
        if (item == null) return;
        var id = definition(item);
        if (id == null) return;
        int mask = Mask(id);
        for (int f = 0; mask != 0; f++, mask >>= 1)
            if ((mask & 1) != 0 && sets[f].Add(item)) members[f].Add(item);
    }

    /// <summary>The family's live members, in the order they were found. Objects that fail the live test are dropped.</summary>
    public void Members(int family, List<T> into)
    {
        into.Clear();
        if (family < 0 || family >= members.Count) return;
        var list = members[family]; var set = sets[family];
        int kept = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (!live(item)) { set.Remove(item); continue; }
            list[kept++] = item; into.Add(item);
        }
        list.RemoveRange(kept, list.Count - kept);
    }

    public bool Belongs(int family, string? id) => id != null && family >= 0 && family < families.Count && (Mask(id) & (1 << family)) != 0;

    private int Mask(string id)
    {
        if (masks.TryGetValue(id, out int mask)) return mask;
        mask = 0;
        for (int f = 0; f < families.Count; f++) if (families[f].Member(id)) mask |= 1 << f;
        if (masks.Count < MemoLimit) masks[id] = mask;
        return mask;
    }

    private sealed class ReferenceComparer : IEqualityComparer<T>
    {
        internal static readonly ReferenceComparer Instance = new();
        public bool Equals(T? a, T? b) => ReferenceEquals(a, b);
        public int GetHashCode(T item) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(item);
    }
}
