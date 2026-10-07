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
/// is said in the headline moment for a while after its news was shown. <see cref="Key"/> is its translation key.
/// Since Framework 0.114.0 a line may belong to a place (its own, or its thread's) and is said only there.</summary>
public sealed class StoryLine
{
    public string Id { get; }
    public string Owner { get; }
    public string Moment { get; }
    /// <summary>What is said: one text or its variants (Framework 0.132.0).</summary>
    public TextVariants Text { get; }
    public string Key { get; }
    public string Speakers { get; }
    public int Weight { get; }
    public StoryRequires? Requires { get; }
    public string? Place { get; }
    public string? Thread { get; }
    /// <summary>For a mention: the broadcast it quotes, which must have been shown recently.</summary>
    public string? Broadcast { get; }
    /// <summary>Game factions the speaker must belong to one of (Framework 0.115.0); empty for anyone.</summary>
    public IReadOnlyList<string> SpeakerFactions { get; }
    public StoryLine(string id, string owner, string moment, TextVariants text, string key, string speakers, int weight, StoryRequires? requires,
        string? place = null, string? thread = null, string? broadcast = null, IReadOnlyList<string>? speakerFactions = null)
    { Id = id; Owner = owner; Moment = moment; Text = text; Key = key; Speakers = speakers; Weight = weight; Requires = requires; Place = place; Thread = thread; Broadcast = broadcast; SpeakerFactions = speakerFactions ?? Array.Empty<string>(); }
}

/// <summary>Every loaded story pack merged into one library, with no game types. Ids are shared by every table across
/// all packs: a second use of an id is refused with a problem, the first kept. An entry is also refused when it names
/// a game item or condition that does not exist (unless it needs a mod that is not installed, when it simply never
/// appears), or a broadcast, arc, section, place, person or thread that no pack provides.</summary>
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
    /// <summary>The places, people and threads (Framework 0.114.0).</summary>
    public IReadOnlyDictionary<string, StoryEntry<StoryPlace>> PlaceEntries => places;
    public IReadOnlyDictionary<string, StoryEntry<StoryPerson>> People => people;
    public IReadOnlyDictionary<string, StoryEntry<StoryThread>> Threads => threads;
    /// <summary>The merged places as one lookup from station ids.</summary>
    public StoryPlaces Places { get; private set; } = StoryPlaces.Empty;
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
    private readonly Dictionary<string, StoryEntry<StoryPlace>> places = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryPerson>> people = new(StringComparer.Ordinal);
    private readonly Dictionary<string, StoryEntry<StoryThread>> threads = new(StringComparer.Ordinal);
    private readonly List<string> problems = new();

    /// <summary>The place an entry belongs to: its own, else its thread's, else none.</summary>
    public string? PlaceOf(string? thread, string? place) => place ?? (thread != null && threads.TryGetValue(thread, out var t) ? t.Value.place : null);
    /// <summary>The requirements an entry inherits from its thread, or null.</summary>
    public StoryRequires? ThreadRequires(string? thread) => thread != null && threads.TryGetValue(thread, out var t) ? t.Value.requires : null;
    /// <summary>A person's display name, "Name, role", through the owner's translations.</summary>
    public string? PersonName(string? key, Func<string, string, string, string> words)
    {
        if (key == null || !people.TryGetValue(key, out var person)) return null;
        string name = words(person.Owner, key + ".name", person.Value.name);
        return person.Value.role == null ? name : name + ", " + words(person.Owner, key + ".role", person.Value.role);
    }
    /// <summary>The ids of every entry in a thread, by table, for the F3 thread report.</summary>
    public IEnumerable<(string Table, string Id)> Members(string thread) =>
        broadcasts.Values.Where(e => e.Value.thread == thread).Select(e => ("broadcasts", e.Id))
        .Concat(adverts.Values.Where(e => e.Value.thread == thread).Select(e => ("adverts", e.Id)))
        .Concat(arcs.Values.Where(e => e.Value.thread == thread).Select(e => ("arcs", e.Id)))
        .Concat(chatter.Values.Where(e => e.Value.thread == thread).Select(e => ("chatter", e.Id)))
        .Concat(tips.Values.Where(e => e.Value.thread == thread).Select(e => ("tips", e.Id)))
        .Concat(sections.Values.Where(e => e.Value.thread == thread).Select(e => ("sections", e.Id)))
        .Concat(articles.Values.Where(e => e.Value.thread == thread).Select(e => ("articles", e.Id)))
        .Concat(files.Values.Where(e => e.Value.thread == thread).Select(e => ("files", e.Id)));

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
            foreach (var pair in pack.places) Add(library.places, pair.Key, owner, pair.Value, null);
            foreach (var pair in pack.people) Add(library.people, pair.Key, owner, pair.Value, null);
            foreach (var pair in pack.threads) Add(library.threads, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.broadcasts) Add(library.broadcasts, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.adverts) Add(library.adverts, pair.Key, owner, pair.Value, pair.Value.requires);
            foreach (var pair in pack.arcs)
            {
                // Every test and outcome of the arc: each step's own, each branch's and each reply's.
                var tests = Tests(pair.Value).ToList();
                var outcomes = pair.Value.steps.SelectMany(Outcomes);
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
        string? Place(string? id) => library.UnknownPlace(id);
        string? Thread(string? id) => id != null && !library.threads.ContainsKey(id) ? Text.Get("Story.unknown_thread", id) : null;
        string? Person(string? id, string? thread)
        {
            if (id == null) return null;
            if (!library.people.ContainsKey(id)) return Text.Get("Story.unknown_person", id);
            // A thread with a cast keeps its letters in the family.
            if (thread != null && library.threads.TryGetValue(thread, out var t) && t.Value.people.Count > 0 && !t.Value.people.Contains(id)) return Text.Get("Story.not_in_cast", id, thread);
            return null;
        }
        // Every variant of a text (Framework 0.132.0) must name only people the library has.
        string? Mentioned(IEnumerable<string> named, string? thread) => named.Select(p => Person(p, thread)).FirstOrDefault(p => p != null);
        string? Reference(StoryRequires? r) => library.UnknownReference(r);
        string? Grounding(string? thread, string? place) => Thread(thread) ?? Place(place);
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
            library.Places = new StoryPlaces(library.places.ToDictionary(p => p.Key, p => p.Value.Value, StringComparer.Ordinal));
            // A place lies within a regional place only; a regional place has a region label (checked by the schema).
            changed = Refuse(library.places, e => e.Value.within == null ? null : Place(e.Value.within) ??
                (library.places.TryGetValue(e.Value.within, out var within) && within.Value.within != null ? Text.Get("Story.not_regional", e.Value.within) : null));
            changed |= Refuse(library.people, e => Place(e.Value.home));
            changed |= Refuse(library.threads, e => Place(e.Value.place) ?? e.Value.people.Select(p => Person(p, null)).FirstOrDefault(p => p != null) ?? Reference(e.Value.requires));
            changed |= Refuse(library.broadcasts, e => Reference(e.Value.requires) ?? Grounding(e.Value.thread, e.Value.place) ??
                (e.Value.region == null && library.Places.Region(library.PlaceOf(e.Value.thread, e.Value.place)) == null ? Text.Get("Story.no_region", e.Id) : null) ??
                Mentioned(StorySchema.People(e.Value.text), e.Value.thread) ?? Mentioned(StorySchema.People(e.Value.mention), e.Value.thread));
            changed |= Refuse(library.adverts, e => Reference(e.Value.requires) ?? Grounding(e.Value.thread, e.Value.place) ?? Mentioned(StorySchema.People(e.Value.text), e.Value.thread));
            changed |= Refuse(library.chatter, e => Reference(e.Value.requires) ?? Grounding(e.Value.thread, e.Value.place) ?? Mentioned(StorySchema.People(e.Value.line), e.Value.thread));
            changed |= Refuse(library.arcs, e => Reference(e.Value.requires) ?? Grounding(e.Value.thread, e.Value.place) ??
                e.Value.steps.Select(s => s.delivery?.bulletin).Where(b => b != null && !library.broadcasts.ContainsKey(b)).Select(b => Text.Get("Story.unknown_bulletin", b!)).FirstOrDefault() ??
                e.Value.steps.SelectMany(Outcomes)
                    .SelectMany(o => o?.files ?? new List<string>()).Where(f => !library.files.ContainsKey(f)).Select(f => Text.Get("Story.unknown_file", f)).FirstOrDefault() ??
                Messages(e.Value).Select(m => Person(m.person, e.Value.thread) ?? Mentioned(StorySchema.People(m.text), e.Value.thread)).FirstOrDefault(p => p != null) ??
                e.Value.steps.Select(s => Person(s.objective?.person, e.Value.thread)).FirstOrDefault(p => p != null) ??
                e.Value.steps.SelectMany(s => new[] { s.objective?.title, s.objective?.description }).Select(t => Mentioned(StorySchema.People(t), e.Value.thread)).FirstOrDefault(p => p != null) ??
                // A dock-at test with no station needs the arc's place.
                (library.PlaceOf(e.Value.thread, e.Value.place) == null && Tests(e.Value).Any(t => t.kind == StorySchema.DockAt && t.station == null) ? Text.Get("Story.no_place", e.Id) : null));
            changed |= Refuse(library.files, e => (e.Value.startsArc != null && !library.arcs.ContainsKey(e.Value.startsArc) ? Text.Get("Story.unknown_arc", e.Value.startsArc) : null) ??
                Grounding(e.Value.thread, e.Value.place) ?? Person(e.Value.person, e.Value.thread) ?? Mentioned(StorySchema.People(e.Value.text), e.Value.thread));
            changed |= Refuse(library.tips, e => Thread(e.Value.thread));
            changed |= Refuse(library.sections, e => Thread(e.Value.thread));
            changed |= Refuse(library.articles, e => Thread(e.Value.thread));
        }
        Refuse(library.articles, e => library.sections.ContainsKey(e.Value.section) ? null : Text.Get("Story.unknown_section", e.Value.section));
        library.Lines = library.chatter.Values.Select(c => new StoryLine(c.Id, c.Owner, c.Value.moment, c.Value.line, c.Id + ".line", c.Value.speakers, c.Value.weight, c.Value.requires,
                library.PlaceOf(c.Value.thread, c.Value.place), c.Value.thread, null, c.Value.speakerFactions))
            .Concat(library.broadcasts.Values.Where(b => b.Value.mention != null)
                .Select(b => new StoryLine(b.Id, b.Owner, StoryMoments.Headline, b.Value.mention!, b.Id + ".mention", StorySchema.Anyone, b.Value.weight, b.Value.requires,
                    library.PlaceOf(b.Value.thread, b.Value.place), b.Value.thread, b.Id)))
            .ToList();
        return library;
    }

    /// <summary>The first entry a requirement block names that this library does not have (an arc, an arc's step, a
    /// file, a news item, a place, or a region that is not a regional place), or null. Other packs that carry a story
    /// <c>requires</c> block (Framework 0.127.0) check it against the built library with this.</summary>
    public string? UnknownReference(StoryRequires? r)
    {
        if (r == null) return null;
        foreach (var id in r.arcsDone.Concat(r.arcsNotStarted).Concat(r.arcsActive)) if (!arcs.ContainsKey(id)) return Text.Get("Story.unknown_arc", id);
        foreach (var at in r.arcsAtStep)
            if (!StorySchema.TryArcStep(at, out var arc, out var step) || !arcs.TryGetValue(arc, out var entry) || entry.Value.steps.All(s => s.id != step)) return Text.Get("Story.unknown_step", at);
        foreach (var id in r.filesRead) if (!files.ContainsKey(id)) return Text.Get("Story.unknown_file", id);
        foreach (var id in r.newsSeen) if (!broadcasts.ContainsKey(id)) return Text.Get("Story.unknown_bulletin", id);
        foreach (var id in r.places) if (UnknownPlace(id) is string p) return p;
        foreach (var id in r.regions) if (UnknownPlace(id) is string p) return p; else if (!Places.IsRegional(id)) return Text.Get("Story.not_regional", id);
        return null;
    }
    /// <summary>Why a place key is not one this library knows, or null when it is (or none is given).</summary>
    public string? UnknownPlace(string? id) => id != null && !places.ContainsKey(id) ? Text.Get("Story.unknown_place", id) : null;
    /// <summary>Why a person key is not one this library knows, or null when it is (or none is given).</summary>
    public string? UnknownPerson(string? id) => id != null && !people.ContainsKey(id) ? Text.Get("Story.unknown_person", id) : null;

    /// <summary>Every outcome a step can have: its own, its branches' and its replies' (Framework 0.122.0).</summary>
    internal static IEnumerable<StoryOutcome?> Outcomes(StoryStep s) => new[] { s.onComplete }
        .Concat((s.branches ?? new List<StoryBranch>()).Select(b => b.onComplete)).Concat((s.choices ?? new List<StoryChoice>()).Select(c => c.onComplete));
    private static IEnumerable<StoryMessage> Messages(StoryArc arc) => arc.steps.SelectMany(s =>
        new[] { s.delivery?.message }.Concat(Outcomes(s).Select(o => o?.message))).Where(m => m != null)!;
    private static IEnumerable<StoryTest> Tests(StoryArc arc) => arc.steps.SelectMany(s => s.tests
        .Concat((s.branches ?? new List<StoryBranch>()).SelectMany(b => b.tests)).Concat((s.choices ?? new List<StoryChoice>()).SelectMany(c => c.tests)));

    /// <summary>Total entries, for status lines.</summary>
    public int Count => broadcasts.Count + adverts.Count + arcs.Count + chatter.Count + tips.Count + sections.Count + articles.Count + files.Count;
}
