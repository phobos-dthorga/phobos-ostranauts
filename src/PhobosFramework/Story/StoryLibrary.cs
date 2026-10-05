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

/// <summary>A line people may say in small talk (Framework 0.108.0): a chatter entry, or a broadcast's mention, which
/// is said in the headline moment while its news is eligible. <see cref="Key"/> is its translation key.</summary>
public sealed class StoryLine
{
    public string Id { get; }
    public string Owner { get; }
    public string Moment { get; }
    public string Text { get; }
    public string Key { get; }
    public string Speakers { get; }
    public int Weight { get; }
    public StoryRequires? Requires { get; }
    public StoryLine(string id, string owner, string moment, string text, string key, string speakers, int weight, StoryRequires? requires)
    { Id = id; Owner = owner; Moment = moment; Text = text; Key = key; Speakers = speakers; Weight = weight; Requires = requires; }
}

/// <summary>Every loaded story pack merged into one library, with no game types. Ids are shared by every table across
/// all packs: a second use of an id is refused with a problem, the first kept. An entry is also refused when it names
/// a game item or condition that does not exist (unless it needs a mod that is not installed, when it simply never
/// appears), or a broadcast, arc or section that no pack provides.</summary>
public sealed class StoryLibrary
{
    public StorySettings Settings { get; private set; } = new();
    public IReadOnlyDictionary<string, StoryEntry<StoryBroadcast>> Broadcasts => broadcasts;
    public IReadOnlyDictionary<string, StoryEntry<StoryAdvert>> Adverts => adverts;
    public IReadOnlyDictionary<string, StoryEntry<StoryArc>> Arcs => arcs;
    public IReadOnlyDictionary<string, StoryEntry<StoryChatterLine>> Chatter => chatter;
    public IReadOnlyDictionary<string, StoryEntry<StoryTip>> Tips => tips;
    public IReadOnlyDictionary<string, StoryEntry<StorySection>> Sections => sections;
    public IReadOnlyDictionary<string, StoryEntry<StoryArticle>> Articles => articles;
    public IReadOnlyDictionary<string, StoryEntry<StoryFile>> Files => files;
    /// <summary>Every small-talk line: the chatter entries, then the broadcasts' mentions.</summary>
    public IReadOnlyList<StoryLine> Lines { get; private set; } = Array.Empty<StoryLine>();
    public IReadOnlyList<string> Problems => problems;
    public static StoryLibrary Empty { get; } = new();

    private readonly Dictionary<string, StoryEntry<StoryBroadcast>> broadcasts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryAdvert>> adverts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryArc>> arcs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryChatterLine>> chatter = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryTip>> tips = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StorySection>> sections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryArticle>> articles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryFile>> files = new(StringComparer.Ordinal);
    private readonly List<string> problems = new();

    /// <param name="packs">In load order: Framework's pack first, then each mod's in registration order.</param>
    /// <param name="modInstalled">Whether a mod named in <c>requires.mods</c> is installed.</param>
    /// <param name="itemExists">Whether an item definition exists; null skips the check (offline tools).</param>
    /// <param name="conditionExists">Whether a condition definition exists; null skips the check.</param>
    public static StoryLibrary Build(IEnumerable<(string Owner, StoryPack Pack)> packs, StorySettings? settings, Func<string, bool> modInstalled,
        Func<string, bool>? itemExists, Func<string, bool>? conditionExists)
    {
        var library = new StoryLibrary { Settings = settings ?? new StorySettings() };
        var problems = library.problems;
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
        void Add<T>(Dictionary<string, StoryEntry<T>> table, string id, string owner, T value, StoryRequires? requires, IEnumerable<string>? extraItems = null,
            IEnumerable<string>? extraConditions = null)
        {
            if (!Claim(id, owner)) return;
            var problem = Names(requires, Owned(requires).Concat(extraItems ?? Enumerable.Empty<string>()), Conditions(requires).Concat(extraConditions ?? Enumerable.Empty<string>()));
            if (problem != null) problems.Add(Text.Get("Story.refused", id, owner, problem));
            else table[id] = new(id, owner, value);
        }
        foreach (var (owner, pack) in packs)
        {
            foreach (var pair in pack.broadcasts) Add(library.broadcasts, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.adverts) Add(library.adverts, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.arcs)
            {
                // Every test and outcome of the arc: each step's own and each branch's.
                var tests = pair.Value.steps.SelectMany(s => s.tests.Concat((s.branches ?? new List<StoryBranch>()).SelectMany(b => b.tests))).ToList();
                var outcomes = pair.Value.steps.SelectMany(s => new[] { s.onComplete }.Concat((s.branches ?? new List<StoryBranch>()).Select(b => b.onComplete)));
                Add(library.arcs, pair.Key, owner, pair.Value, pair.Value.requires,
                    tests.Where(t => t.item != null).Select(t => t.item!).Concat(outcomes.SelectMany(o => o?.items ?? new List<StoryReward>()).Select(r => r.item)),
                    tests.Where(t => t.condition != null).Select(t => t.condition!));
            }
            foreach (var pair in pack.chatter) Add(library.chatter, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.tips) Add(library.tips, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.sections) Add(library.sections, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.articles) Add(library.articles, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.files) Add(library.files, pair.Key, owner, pair.Value, null);
        }
        // References between entries, repeated until nothing more is refused: an arc may need another arc that was itself refused.
        string? Reference(StoryRequires? r)
        {
            if (r == null) return null;
            foreach (var id in r.arcsDone.Concat(r.arcsNotStarted)) if (!library.arcs.ContainsKey(id)) return Text.Get("Story.unknown_arc", id);
            foreach (var id in r.filesRead) if (!library.files.ContainsKey(id)) return Text.Get("Story.unknown_file", id);
            return null;
        }
        bool Refuse<T>(Dictionary<string, StoryEntry<T>> table, Func<StoryEntry<T>, string?> problem)
        {
            bool any = false;
            foreach (var entry in table.Values.ToArray())
                if (problem(entry) is string p) { problems.Add(Text.Get("Story.refused", entry.Id, entry.Owner, p)); table.Remove(entry.Id); any = true; }
            return any;
        }
        bool changed = true;
        while (changed)
        {
            changed = Refuse(library.broadcasts, e => Reference(e.Value.requires));
            changed |= Refuse(library.adverts, e => Reference(e.Value.requires));
            changed |= Refuse(library.chatter, e => Reference(e.Value.requires));
            changed |= Refuse(library.arcs, e => Reference(e.Value.requires) ??
                e.Value.steps.Select(s => s.delivery?.bulletin).Where(b => b != null && !library.broadcasts.ContainsKey(b)).Select(b => Text.Get("Story.unknown_bulletin", b!)).FirstOrDefault() ??
                e.Value.steps.SelectMany(s => new[] { s.onComplete }.Concat((s.branches ?? new List<StoryBranch>()).Select(b => b.onComplete)))
                    .SelectMany(o => o?.files ?? new List<string>()).Where(f => !library.files.ContainsKey(f)).Select(f => Text.Get("Story.unknown_file", f)).FirstOrDefault());
            changed |= Refuse(library.files, e => e.Value.startsArc != null && !library.arcs.ContainsKey(e.Value.startsArc) ? Text.Get("Story.unknown_arc", e.Value.startsArc) : null);
        }
        Refuse(library.articles, e => library.sections.ContainsKey(e.Value.section) ? null : Text.Get("Story.unknown_section", e.Value.section));
        library.Lines = library.chatter.Values.Select(c => new StoryLine(c.Id, c.Owner, c.Value.moment, c.Value.line, c.Id + ".line", c.Value.speakers, c.Value.weight, c.Value.requires))
            .Concat(library.broadcasts.Values.Where(b => b.Value.mention != null)
                .Select(b => new StoryLine(b.Id, b.Owner, StoryMoments.Headline, b.Value.mention!, b.Id + ".mention", StorySchema.Anyone, b.Value.weight, b.Value.requires)))
            .ToList();
        return library;
    }

    /// <summary>Total entries, for status lines.</summary>
    public int Count => broadcasts.Count + adverts.Count + arcs.Count + chatter.Count + tips.Count + sections.Count + articles.Count + files.Count;
}
