using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>One story entry and the mod whose pack it came from (its translation catalogue).</summary>
public sealed class StoryEntry<T>
{
    public string Id { get; }
    public string Owner { get; }
    public T Value { get; }
    public StoryEntry(string id, string owner, T value) { Id = id; Owner = owner; Value = value; }
}

/// <summary>Every loaded story pack merged into one library, with no game types. Ids are shared by broadcasts, adverts
/// and arcs across all packs: a second use of an id is refused with a problem, the first kept. An entry is also
/// refused when it names a game item or condition that does not exist (unless it needs a mod that is not installed,
/// when it simply never appears), or a broadcast or arc that no pack provides.</summary>
public sealed class StoryLibrary
{
    public StorySettings Settings { get; }
    public IReadOnlyDictionary<string, StoryEntry<StoryBroadcast>> Broadcasts { get; }
    public IReadOnlyDictionary<string, StoryEntry<StoryAdvert>> Adverts { get; }
    public IReadOnlyDictionary<string, StoryEntry<StoryArc>> Arcs { get; }
    public IReadOnlyList<string> Problems { get; }
    public static StoryLibrary Empty { get; } = new(new StorySettings(), new(), new(), new(), new());

    private StoryLibrary(StorySettings settings, Dictionary<string, StoryEntry<StoryBroadcast>> broadcasts, Dictionary<string, StoryEntry<StoryAdvert>> adverts,
        Dictionary<string, StoryEntry<StoryArc>> arcs, List<string> problems)
    { Settings = settings; Broadcasts = broadcasts; Adverts = adverts; Arcs = arcs; Problems = problems; }

    /// <param name="packs">In load order: Framework's pack first, then each mod's in registration order.</param>
    /// <param name="modInstalled">Whether a mod named in <c>requires.mods</c> is installed.</param>
    /// <param name="itemExists">Whether an item definition exists; null skips the check (offline tools).</param>
    /// <param name="conditionExists">Whether a condition definition exists; null skips the check.</param>
    public static StoryLibrary Build(IEnumerable<(string Owner, StoryPack Pack)> packs, StorySettings? settings, Func<string, bool> modInstalled,
        Func<string, bool>? itemExists, Func<string, bool>? conditionExists)
    {
        var problems = new List<string>();
        var broadcasts = new Dictionary<string, StoryEntry<StoryBroadcast>>(StringComparer.Ordinal);
        var adverts = new Dictionary<string, StoryEntry<StoryAdvert>>(StringComparer.Ordinal);
        var arcs = new Dictionary<string, StoryEntry<StoryArc>>(StringComparer.Ordinal);
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        bool Claim(string id, string owner)
        {
            if (owners.TryGetValue(id, out var first)) { problems.Add(Text.Get("Story.id_taken", id, owner, first)); return false; }
            owners[id] = owner;
            return true;
        }
        string? Names(StoryRequires? requires, IEnumerable<string> items, IEnumerable<string> conditions)
        {
            if (requires != null && !requires.mods.All(modInstalled)) return null;
            if (itemExists != null) foreach (var item in items) if (!itemExists(item)) return Text.Get("Story.unknown_item", item);
            if (conditionExists != null) foreach (var c in conditions) if (!conditionExists(c)) return Text.Get("Story.unknown_condition", c);
            return null;
        }
        IEnumerable<string> Owned(StoryRequires? r) => r?.owns ?? Enumerable.Empty<string>();
        IEnumerable<string> Conditions(StoryRequires? r) => r == null ? Enumerable.Empty<string>() : r.playerConditions.Concat(r.forbidConditions);
        foreach (var (owner, pack) in packs)
        {
            foreach (var pair in pack.broadcasts)
            {
                if (!Claim(pair.Key, owner)) continue;
                var problem = Names(pair.Value.requires, Owned(pair.Value.requires), Conditions(pair.Value.requires));
                if (problem != null) problems.Add(Text.Get("Story.refused", pair.Key, owner, problem));
                else broadcasts[pair.Key] = new(pair.Key, owner, pair.Value);
            }
            foreach (var pair in pack.adverts)
            {
                if (!Claim(pair.Key, owner)) continue;
                var problem = Names(pair.Value.requires, Owned(pair.Value.requires), Conditions(pair.Value.requires));
                if (problem != null) problems.Add(Text.Get("Story.refused", pair.Key, owner, problem));
                else adverts[pair.Key] = new(pair.Key, owner, pair.Value);
            }
            foreach (var pair in pack.arcs)
            {
                if (!Claim(pair.Key, owner)) continue;
                var arc = pair.Value;
                var items = Owned(arc.requires).Concat(arc.steps.SelectMany(s => s.tests).Where(t => t.item != null).Select(t => t.item!))
                    .Concat(arc.steps.SelectMany(s => s.onComplete?.items ?? new List<StoryReward>()).Select(r => r.item));
                var problem = Names(arc.requires, items, Conditions(arc.requires));
                if (problem != null) problems.Add(Text.Get("Story.refused", pair.Key, owner, problem));
                else arcs[pair.Key] = new(pair.Key, owner, arc);
            }
        }
        // References between entries, repeated until nothing more is refused: an arc may need another arc that was itself refused.
        string? Reference(StoryRequires? r)
        {
            if (r == null) return null;
            foreach (var id in r.arcsDone.Concat(r.arcsNotStarted)) if (!arcs.ContainsKey(id)) return Text.Get("Story.unknown_arc", id);
            return null;
        }
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (var entry in broadcasts.Values.ToArray())
                if (Reference(entry.Value.requires) is string p) { problems.Add(Text.Get("Story.refused", entry.Id, entry.Owner, p)); broadcasts.Remove(entry.Id); changed = true; }
            foreach (var entry in adverts.Values.ToArray())
                if (Reference(entry.Value.requires) is string p) { problems.Add(Text.Get("Story.refused", entry.Id, entry.Owner, p)); adverts.Remove(entry.Id); changed = true; }
            foreach (var entry in arcs.Values.ToArray())
            {
                string? p = Reference(entry.Value.requires);
                if (p == null)
                    foreach (var step in entry.Value.steps)
                        if (step.delivery?.bulletin is string b && !broadcasts.ContainsKey(b)) { p = Text.Get("Story.unknown_bulletin", b); break; }
                if (p != null) { problems.Add(Text.Get("Story.refused", entry.Id, entry.Owner, p)); arcs.Remove(entry.Id); changed = true; }
            }
        }
        return new StoryLibrary(settings ?? new StorySettings(), broadcasts, adverts, arcs, problems);
    }

    /// <summary>Total entries, for status lines.</summary>
    public int Count => Broadcasts.Count + Adverts.Count + Arcs.Count;
}
