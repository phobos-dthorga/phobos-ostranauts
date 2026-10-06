using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Story;

public enum ArcState { Active, Done, Abandoned }

/// <summary>One letter the player received in an arc, or a reply they sent (Framework 0.122.0). Only where it came from
/// is kept: its text is read from the packs when shown, so a translation or a corrected letter shows as it is now.</summary>
public sealed class StoryLetter
{
    /// <summary>A step's opening letter, its completion letter, a branch's (<c>b0</c> to <c>b3</c>) or the player's reply.</summary>
    public const string Opening = "d", Completion = "c", Reply = "a";
    public StoryLetter(string step, string kind, string? choice, double? epoch) { Step = step; Kind = kind; Choice = choice; Epoch = epoch; }
    public string Step { get; }
    public string Kind { get; }
    /// <summary>The reply's choice id, for <see cref="Reply"/>.</summary>
    public string? Choice { get; }
    /// <summary>When it arrived (game epoch, seconds); null for one reconstructed from an older record.</summary>
    public double? Epoch { get; }
    public static bool IsKind(string kind) => kind == Opening || kind == Completion || kind == Reply || kind.Length == 2 && kind[0] == 'b' && kind[1] >= '0' && kind[1] <= '9';
}

/// <summary>Where the player is in one arc.</summary>
public sealed class ArcProgress
{
    public ArcState State;
    /// <summary>The current step's position and id; the id wins when a pack's steps are reordered.</summary>
    public int Step;
    public string StepId = "";
    /// <summary>Game time (the game's epoch, in seconds) when the current step began.</summary>
    public double StepStart;
    /// <summary>How many times the arc has been finished.</summary>
    public int Completions;
}

/// <summary>The player's story record, kept in one Phobos record on the player (<c>PhobosState.PhobosStory</c>): arc
/// progress, the once-only news already shown and the bulletins waiting for a TV. The game never reads it. Fields this
/// version does not understand, and arcs no loaded pack knows, are kept exactly as they were.</summary>
public sealed class StoryRecord
{
    public const string Name = "PhobosStory";
    public const int Version = 1, MaxQueue = 8;
    private const string ArcPrefix = "arc.", SeenPrefix = "seen.", QueueKey = "queue", BeganKey = "began", ReadPrefix = "read.", FlagPrefix = "flag.", FacePrefix = "face.";
    /// <summary>The game face parts each correspondent was given (Framework 0.121.0), rolled once by the game's own face
    /// roll and kept, so a person looks the same for the whole save. The game's roll cannot be repeated from a seed.</summary>
    public Dictionary<string, string[]> Faces { get; } = new(StringComparer.Ordinal);
    private const string LettersPrefix = "letters.";
    /// <summary>The most letters kept for one arc; the oldest go first.</summary>
    public const int MaxLetters = 64;
    /// <summary>The letters each arc delivered and the replies sent (Framework 0.122.0), oldest first.</summary>
    public Dictionary<string, List<StoryLetter>> Letters { get; } = new(StringComparer.Ordinal);

    public void AddLetter(string arc, StoryLetter letter)
    {
        if (!Letters.TryGetValue(arc, out var list)) Letters[arc] = list = new List<StoryLetter>();
        list.Add(letter);
        while (list.Count > MaxLetters) list.RemoveAt(0);
    }

    /// <summary>"step|kind|choice|epoch" entries joined by ";" (Framework 0.124.1; 0.122.0 wrote commas, which the save
    /// store refuses, so no such record was ever saved; its form is still read). Null when any part is not ours.</summary>
    private static List<StoryLetter>? TryLetters(string value)
    {
        var list = new List<StoryLetter>();
        foreach (string entry in value.Split(';'))
        {
            var parts = entry.Split('|');
            if (parts.Length != 4) parts = entry.Split(',');
            if (parts.Length != 4 || !StorySchema.IsId(parts[0], StorySchema.MaxStepIdLength) || !StoryLetter.IsKind(parts[1]) ||
                parts[2].Length > 0 && !StorySchema.IsId(parts[2], StorySchema.MaxStepIdLength)) return null;
            double? epoch = parts[3].Length == 0 ? null : Epoch(parts[3]);
            if (parts[3].Length > 0 && epoch == null) return null;
            list.Add(new StoryLetter(parts[0], parts[1], parts[2].Length == 0 ? null : parts[2], epoch));
        }
        return list.Count is > 0 and <= MaxLetters ? list : null;
    }
    private static string Encode(StoryLetter l) => l.Step + "|" + l.Kind + "|" + (l.Choice ?? "") + "|" + (l.Epoch is double e ? e.ToString("R", CultureInfo.InvariantCulture) : "");
    /// <summary>An arc's letters as the store takes them: runs of ";"-joined entries, each within the store's value
    /// length, the first under <c>letters.&lt;arc&gt;</c> and the rest under <c>letters.&lt;arc&gt;.1</c>, <c>.2</c> and on.</summary>
    private static IEnumerable<(string Suffix, string Value)> EncodeLetters(IEnumerable<StoryLetter> letters)
    {
        var run = new System.Text.StringBuilder(); int index = 0;
        foreach (var letter in letters)
        {
            string entry = Encode(letter);
            if (run.Length > 0 && run.Length + 1 + entry.Length > Persistence.ObjectStateStore.MaxValueLength)
            { yield return (index == 0 ? "" : "." + index, run.ToString()); run.Clear(); index++; }
            if (run.Length > 0) run.Append(';');
            run.Append(entry);
        }
        if (run.Length > 0) yield return (index == 0 ? "" : "." + index, run.ToString());
    }
    /// <summary>Splits <c>letters.&lt;arc&gt;</c> or <c>letters.&lt;arc&gt;.&lt;n&gt;</c> into the arc and the run's place.</summary>
    private static (string Arc, int Index)? LettersKey(string key)
    {
        string rest = key.Substring(LettersPrefix.Length);
        int dot = rest.LastIndexOf('.');
        if (dot > 0 && int.TryParse(rest.Substring(dot + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int index)) return (rest.Substring(0, dot), index);
        return StorySchema.IsId(rest) ? (rest, 0) : null;
    }
    /// <summary>Story data files the player has opened (Framework 0.110.0).</summary>
    public HashSet<string> Read { get; } = new(StringComparer.Ordinal);
    /// <summary>Story flags set by arc outcomes (Framework 0.114.0), with the game time each was set.</summary>
    public Dictionary<string, double> Flags { get; } = new(StringComparer.Ordinal);
    /// <summary>When each once-shown or ever-shown news item was first shown (Framework 0.114.0); an item seen before
    /// this version is in <see cref="Seen"/> with no time here.</summary>
    public Dictionary<string, double> SeenAt { get; } = new(StringComparer.Ordinal);
    /// <summary>Game time (the game's epoch, in seconds) when this record began (Framework 0.109.0): the start of the
    /// player's story time for <c>afterDays</c> and <c>beforeDays</c>. A record from an earlier version gains it when the
    /// game is next loaded.</summary>
    public double? Began { get; set; }
    public Dictionary<string, ArcProgress> Arcs { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Seen { get; } = new(StringComparer.Ordinal);
    public List<string> Queue { get; } = new();
    private readonly Dictionary<string, string> kept = new(StringComparer.Ordinal);

    public static StoryRecord Decode(IReadOnlyDictionary<string, string> fields)
    {
        var record = new StoryRecord();
        // Letters arrive in runs under letters.<arc>, letters.<arc>.1 and on (0.124.1); gathered, then read in order.
        var runs = new Dictionary<string, SortedDictionary<int, string>>(StringComparer.Ordinal);
        foreach (var pair in fields ?? new Dictionary<string, string>())
        {
            if (pair.Key.StartsWith(LettersPrefix, StringComparison.Ordinal))
            {
                var place = LettersKey(pair.Key);
                if (place != null)
                {
                    if (!runs.TryGetValue(place.Value.Arc, out var parts)) runs[place.Value.Arc] = parts = new SortedDictionary<int, string>();
                    parts[place.Value.Index] = pair.Value;
                    continue;
                }
            }
            if (pair.Key.StartsWith(ArcPrefix, StringComparison.Ordinal) && TryArc(pair.Value, out var arc)) record.Arcs[pair.Key.Substring(ArcPrefix.Length)] = arc;
            else if (pair.Key.StartsWith(SeenPrefix, StringComparison.Ordinal) && pair.Value == "1") record.Seen.Add(pair.Key.Substring(SeenPrefix.Length));
            else if (pair.Key.StartsWith(SeenPrefix, StringComparison.Ordinal) && Epoch(pair.Value) is double seenAt)
            { string id = pair.Key.Substring(SeenPrefix.Length); record.Seen.Add(id); record.SeenAt[id] = seenAt; }
            else if (pair.Key.StartsWith(FlagPrefix, StringComparison.Ordinal) && Epoch(pair.Value) is double setAt) record.Flags[pair.Key.Substring(FlagPrefix.Length)] = setAt;
            else if (pair.Key.StartsWith(ReadPrefix, StringComparison.Ordinal) && pair.Value == "1") record.Read.Add(pair.Key.Substring(ReadPrefix.Length));
            else if (pair.Key.StartsWith(FacePrefix, StringComparison.Ordinal) && Social.PortraitRules.ValidParts(pair.Value.Split('|'))) record.Faces[pair.Key.Substring(FacePrefix.Length)] = pair.Value.Split('|');
            else if (pair.Key == QueueKey) record.Queue.AddRange(pair.Value.Split('|').Where(id => id.Length > 0).Take(MaxQueue));
            else if (pair.Key == BeganKey && double.TryParse(pair.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double began) && !double.IsNaN(began) && !double.IsInfinity(began)) record.Began = began;
            else record.kept[pair.Key] = pair.Value;
        }
        foreach (var pair in runs)
        {
            // A run that is not ours keeps every piece as written, so a later version can still read it.
            if (TryLetters(string.Join(";", pair.Value.Values)) is List<StoryLetter> letters) record.Letters[pair.Key] = letters;
            else foreach (var piece in pair.Value) record.kept[LettersPrefix + pair.Key + (piece.Key == 0 ? "" : "." + piece.Key)] = piece.Value;
        }
        return record;
    }

    public Dictionary<string, string> Encode()
    {
        var fields = new Dictionary<string, string>(kept, StringComparer.Ordinal);
        foreach (var pair in Arcs)
            fields[ArcPrefix + pair.Key] = string.Join("|", State(pair.Value.State), pair.Value.Step.ToString(CultureInfo.InvariantCulture), pair.Value.StepId,
                pair.Value.StepStart.ToString("R", CultureInfo.InvariantCulture), pair.Value.Completions.ToString(CultureInfo.InvariantCulture));
        foreach (var id in Seen) fields[SeenPrefix + id] = SeenAt.TryGetValue(id, out var at) ? at.ToString("R", CultureInfo.InvariantCulture) : "1";
        foreach (var id in Read) fields[ReadPrefix + id] = "1";
        foreach (var pair in Flags) fields[FlagPrefix + pair.Key] = pair.Value.ToString("R", CultureInfo.InvariantCulture);
        foreach (var pair in Faces) fields[FacePrefix + pair.Key] = string.Join("|", pair.Value);
        foreach (var pair in Letters)
        {
            if (pair.Value.Count == 0) continue;
            var runs = EncodeLetters(pair.Value).ToList();
            // A run the store would refuse would refuse the whole record: that arc's letters are left out instead, and said.
            if (runs.Any(r => !Persistence.ObjectStateStore.SafeValue(r.Value))) { FrameworkLifecycle.Log(Text.Get("Story.letters_unsaved", pair.Key)); continue; }
            foreach (var (suffix, value) in runs) fields[LettersPrefix + pair.Key + suffix] = value;
        }
        if (Queue.Count > 0) fields[QueueKey] = string.Join("|", Queue.Take(MaxQueue));
        if (Began is double b) fields[BeganKey] = b.ToString("R", CultureInfo.InvariantCulture);
        return fields;
    }

    /// <summary>Queues a bulletin for the next TV news pick, once.</summary>
    public void Enqueue(string id)
    {
        if (!Queue.Contains(id)) Queue.Add(id);
        while (Queue.Count > MaxQueue) Queue.RemoveAt(0);
    }
    public bool Started(string arc) => Arcs.ContainsKey(arc);
    public bool Finished(string arc) => Arcs.TryGetValue(arc, out var p) && (p.State == ArcState.Done || p.Completions > 0);
    public bool Active(string arc) => Arcs.TryGetValue(arc, out var p) && p.State == ArcState.Active;
    public bool AtStep(string arc, string step) => Arcs.TryGetValue(arc, out var p) && p.State == ArcState.Active && p.StepId == step;
    /// <summary>A news item was shown: remembered with the time, the first time.</summary>
    public void MarkSeen(string id, double epoch) { if (Seen.Add(id) || !SeenAt.ContainsKey(id)) SeenAt[id] = epoch; }
    /// <summary>When a news item was first shown: its time, or the record's start for one seen before times were kept, or null.</summary>
    public double? SeenEpoch(string id) => SeenAt.TryGetValue(id, out var at) ? at : Seen.Contains(id) ? Began : null;
    public void SetFlag(string flag, double epoch) { if (!Flags.ContainsKey(flag)) Flags[flag] = epoch; }
    public void ClearFlag(string flag) => Flags.Remove(flag);
    private static double? Epoch(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double epoch) && !double.IsNaN(epoch) && !double.IsInfinity(epoch) ? epoch : null;
    public int ActiveCount(Func<string, bool> known) => Arcs.Count(a => a.Value.State == ArcState.Active && known(a.Key));

    private static string State(ArcState state) => state switch { ArcState.Active => "active", ArcState.Done => "done", _ => "abandoned" };
    private static bool TryArc(string value, out ArcProgress arc)
    {
        arc = new ArcProgress();
        var parts = value.Split('|');
        if (parts.Length != 5) return false;
        switch (parts[0]) { case "active": arc.State = ArcState.Active; break; case "done": arc.State = ArcState.Done; break; case "abandoned": arc.State = ArcState.Abandoned; break; default: return false; }
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out arc.Step) || !StorySchema.IsId(parts[2], StorySchema.MaxStepIdLength) ||
            !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out arc.StepStart) || double.IsNaN(arc.StepStart) || double.IsInfinity(arc.StepStart) ||
            !int.TryParse(parts[4], NumberStyles.None, CultureInfo.InvariantCulture, out arc.Completions)) return false;
        arc.StepId = parts[2];
        return true;
    }
}

/// <summary>What the story rules ask of the game, so they can be checked without it.</summary>
public interface IStoryFacts
{
    bool ModInstalled(string mod);
    bool PlayerHas(string condition);
    /// <summary>Objects of this definition on the player's loaded ships.</summary>
    int Installed(string item);
    /// <summary>Units of this definition the player carries, bags included.</summary>
    int Carried(string item);
    /// <summary>Docked at or aboard this station (or one of its parts), or any station for <c>any</c>.</summary>
    bool DockedAt(string station);
    /// <summary>Game time in seconds.</summary>
    double Epoch { get; }
    /// <summary>The credits the player holds (Framework 0.109.0).</summary>
    double Credits { get; }
    /// <summary>The regional place the player is in (Framework 0.114.0): the nearest regional station, as the game's own
    /// traffic control sees it; null where no place is known.</summary>
    string? Region { get; }
    /// <summary>Whether the player is at a place: docked at or aboard it (or a part of it), or, for a regional place,
    /// anywhere in its region.</summary>
    bool Near(string place);
    /// <summary>A faction's score for the player as the game's FACTIONS app sums it (Framework 0.115.0), or null for a
    /// faction the game does not have.</summary>
    double? Standing(string faction);
    /// <summary>Whether someone aboard other than the player has the condition.</summary>
    bool CrewWith(string condition);
    /// <summary>How many crew the player has, the player not counted.</summary>
    int CrewCount { get; }
    /// <summary>Phobos machines of this definition running on the player's loaded ships.</summary>
    int Running(string item);
    /// <summary>The calendar month (1 to 12) and the UTC hour (0 to 23).</summary>
    int Month { get; }
    int Hour { get; }
}

/// <summary>The story rules with no game types: eligibility, goal tests and weighted picks.</summary>
public static class StoryRules
{
    /// <summary>Every story goal test name starts with this; the game resolves an unknown one to its always-true Blank.</summary>
    public const string TestPrefix = "PhobosStory.";
    /// <summary>An entry's own requirements and its thread's (Framework 0.114.0): the first that does not hold, or null.</summary>
    public static string? Blocked(StoryRequires? own, StoryRequires? thread, IStoryFacts facts, StoryRecord record) =>
        Blocked(own, facts, record) ?? Blocked(thread, facts, record);
    /// <summary>Null when every requirement holds; otherwise the first that does not, for the F3 report.</summary>
    public static string? Blocked(StoryRequires? r, IStoryFacts facts, StoryRecord record)
    {
        if (r == null) return null;
        foreach (var mod in r.mods) if (!facts.ModInstalled(mod)) return Text.Get("Story.needs_mod", mod);
        foreach (var c in r.playerConditions) if (!facts.PlayerHas(c)) return Text.Get("Story.needs_condition", c);
        foreach (var c in r.forbidConditions) if (facts.PlayerHas(c)) return Text.Get("Story.forbids_condition", c);
        foreach (var item in r.owns) if (facts.Installed(item) < 1) return Text.Get("Story.needs_owned", item);
        if (r.dockedAt.Count > 0 && !r.dockedAt.Any(facts.DockedAt)) return Text.Get("Story.needs_docked", string.Join(", ", r.dockedAt));
        foreach (var arc in r.arcsDone) if (!record.Finished(arc)) return Text.Get("Story.needs_arc_done", arc);
        foreach (var arc in r.arcsNotStarted) if (record.Started(arc)) return Text.Get("Story.needs_arc_unstarted", arc);
        foreach (var file in r.filesRead) if (!record.Read.Contains(file)) return Text.Get("Story.needs_file_read", file);
        foreach (var flag in r.flags) if (!record.Flags.ContainsKey(flag)) return Text.Get("Story.needs_flag", flag);
        foreach (var flag in r.notFlags) if (record.Flags.ContainsKey(flag)) return Text.Get("Story.forbids_flag", flag);
        foreach (var arc in r.arcsActive) if (!record.Active(arc)) return Text.Get("Story.needs_arc_active", arc);
        foreach (var at in r.arcsAtStep) if (!StorySchema.TryArcStep(at, out var arc, out var step) || !record.AtStep(arc, step)) return Text.Get("Story.needs_arc_step", at);
        if (r.places.Count > 0 && !r.places.Any(facts.Near)) return Text.Get("Story.needs_place", string.Join(", ", r.places));
        if (r.regions.Count > 0 && !r.regions.Contains(facts.Region ?? "")) return Text.Get("Story.needs_region", string.Join(", ", r.regions));
        foreach (var news in r.newsSeen) if (!record.Seen.Contains(news)) return Text.Get("Story.needs_news", news);
        foreach (var s in r.standing)
        {
            double? score = facts.Standing(s.faction);
            if (score == null) return Text.Get("Story.unknown_faction", s.faction);
            string tier = Tier(score.Value);
            if (s.atLeast != null && TierRank(tier) < TierRank(s.atLeast)) return Text.Get("Story.needs_standing", s.faction, s.atLeast, tier);
            if (s.atMost != null && TierRank(tier) > TierRank(s.atMost)) return Text.Get("Story.standing_too_high", s.faction, s.atMost, tier);
        }
        foreach (var c in r.crewWith) if (!facts.CrewWith(c)) return Text.Get("Story.needs_crew_with", c);
        if (r.crewCount is StoryCount count && (facts.CrewCount < count.atLeast || count.atMost is int most && facts.CrewCount > most)) return Text.Get("Story.needs_crew_count", count.atLeast, count.atMost?.ToString(CultureInfo.InvariantCulture) ?? "", facts.CrewCount);
        foreach (var item in r.running) if (facts.Running(item) < 1) return Text.Get("Story.needs_running", item);
        if (r.months.Count > 0 && !r.months.Contains(facts.Month)) return Text.Get("Story.needs_month", string.Join(", ", r.months), facts.Month);
        if (r.hours is StoryHours hours && !HourIn(hours.from, hours.to, facts.Hour)) return Text.Get("Story.needs_hour", hours.from, hours.to, facts.Hour);
        double days = Days(record, facts);
        if (r.afterDays is double after && days < after) return Text.Get("Story.needs_after_days", after.ToString("0.#", CultureInfo.InvariantCulture), days.ToString("0.#", CultureInfo.InvariantCulture));
        if (r.beforeDays is double before && days >= before) return Text.Get("Story.needs_before_days", before.ToString("0.#", CultureInfo.InvariantCulture));
        return null;
    }

    /// <summary>The game's standing tier for a score (Framework 0.115.0), as JsonFaction.GetReputation maps it:
    /// Honored from 100, Trusted from 75, Friendly from 50, Warm from 25, Neutral from -50, else Dislikes.</summary>
    public static string Tier(double score) =>
        double.IsNaN(score) ? "dislikes" : score >= 100 ? "honored" : score >= 75 ? "trusted" : score >= 50 ? "friendly" : score >= 25 ? "warm" : score >= -49.9999f ? "neutral" : "dislikes";
    public static int TierRank(string tier) => Math.Max(0, Array.IndexOf(StorySchema.Tiers, tier));
    /// <summary>Whether an hour lies in a window of the day; a window whose start is after its end wraps midnight.</summary>
    public static bool HourIn(int from, int to, int hour) => from <= to ? hour >= from && hour <= to : hour >= from || hour <= to;

    /// <summary>Game days since the player's story record began; 0 before it has. A game day is the game's own
    /// (<see cref="GameClock.DaySeconds"/>, Framework 0.127.0; earlier versions counted 86,400 seconds).</summary>
    public static double Days(StoryRecord record, IStoryFacts facts) => record.Began is double began ? Math.Max(0, GameClock.Days(facts.Epoch - began)) : 0;

    public static bool Passed(StoryTest test, IStoryFacts facts, double stepStart) => test.kind switch
    {
        StorySchema.DockAt => facts.DockedAt(test.station!),
        StorySchema.HaveItem => facts.Carried(test.item!) >= test.count,
        StorySchema.Install => facts.Installed(test.item!) >= test.count,
        StorySchema.Wait => facts.Epoch - stepStart >= test.hours * 3600,
        StorySchema.Credits => facts.Credits >= test.amount,
        StorySchema.Condition => facts.PlayerHas(test.condition!),
        _ => false
    };

    /// <summary>What finishes a step now (Framework 0.109.0): -1 for its own tests, the index of the first branch whose
    /// tests all pass, or null when nothing does yet.</summary>
    public static int? Outcome(StoryStep step, IStoryFacts facts, double stepStart)
    {
        // A choice step (Framework 0.122.0) waits for the player's answer; nothing finishes it by itself.
        if (step.choices != null) return null;
        if (Passed(step, facts, stepStart)) return -1;
        if (step.branches == null) return null;
        for (int i = 0; i < step.branches.Count; i++)
            if (step.branches[i].tests.All(t => Passed(t, facts, stepStart))) return i;
        return null;
    }

    /// <summary>The step index an outcome leads to, or -1 for the end of the arc: the named step, the end, or by
    /// default the next step in order.</summary>
    public static int NextStep(StoryArc arc, int index, string? next)
    {
        if (next == StorySchema.End) return -1;
        if (next != null) return arc.steps.FindIndex(s => s.id == next);
        return index + 1 < arc.steps.Count ? index + 1 : -1;
    }

    /// <summary>Who a step's goal is from (Framework 0.121.0): the goal's own person, else the sender of the step's
    /// letter, else the last sender before it in the arc (a completion's letter after its step's opening letter). A
    /// sender given only as free text comes back as <c>From</c>; neither, and the goal is from no one in particular.</summary>
    public static (string? Person, StoryMessage? From) GoalSender(StoryArc arc, int index)
    {
        if (index < 0 || index >= arc.steps.Count) return (null, null);
        var step = arc.steps[index];
        if (step.objective?.person != null) return (step.objective.person, null);
        if (step.delivery?.message is StoryMessage own) return (own.person, own.person == null ? own : null);
        for (int i = index - 1; i >= 0; i--)
            foreach (var message in new[] { arc.steps[i].onComplete?.message, arc.steps[i].delivery?.message })
                if (message != null) return (message.person, message.person == null ? message : null);
        return (null, null);
    }

    public static bool Passed(StoryStep step, IStoryFacts facts, double stepStart) => step.tests.All(t => Passed(t, facts, stepStart));

    /// <summary>The first test keeping a reply locked (Framework 0.122.0), or null when it can be sent.</summary>
    public static StoryTest? ChoiceBlocked(StoryChoice choice, IStoryFacts facts, double stepStart) => choice.tests.FirstOrDefault(t => !Passed(t, facts, stepStart));

    /// <summary>The letters of an arc for the Letters window (Framework 0.122.0): those recorded as they arrived, or, for
    /// an arc begun before letters were kept, what its progress shows it must have received, in step order and without
    /// dates. A finished arc shows every step's letters; an open or set-aside one, those before its current step and
    /// the current step's opening letter.</summary>
    public static List<StoryLetter> Letters(StoryArc arc, ArcProgress progress, IReadOnlyList<StoryLetter>? recorded)
    {
        if (recorded != null && recorded.Count > 0) return recorded.ToList();
        var letters = new List<StoryLetter>();
        int current = progress.State == ArcState.Done ? arc.steps.Count : Math.Max(0, Resolve(arc, progress));
        for (int i = 0; i < arc.steps.Count && i <= current; i++)
        {
            var step = arc.steps[i];
            if (step.delivery?.message != null) letters.Add(new StoryLetter(step.id, StoryLetter.Opening, null, null));
            if (i < current && step.onComplete?.message != null) letters.Add(new StoryLetter(step.id, StoryLetter.Completion, null, null));
        }
        return letters;
    }

    /// <summary>One test and how far it has got, for the F3 report.</summary>
    public static string Describe(StoryTest test, IStoryFacts facts, double stepStart) => test.kind switch
    {
        StorySchema.DockAt => Text.Get("Story.test_dock", test.station!, Mark(Passed(test, facts, stepStart))),
        StorySchema.HaveItem => Text.Get("Story.test_have", test.item!, facts.Carried(test.item!), test.count, test.consume ? Text.Get("Story.test_taken") : ""),
        StorySchema.Install => Text.Get("Story.test_install", test.item!, facts.Installed(test.item!), test.count),
        StorySchema.Wait => Text.Get("Story.test_wait", Math.Max(0, (facts.Epoch - stepStart) / 3600).ToString("0.0", CultureInfo.InvariantCulture), test.hours.ToString("0.0", CultureInfo.InvariantCulture)),
        StorySchema.Credits => Text.Get("Story.test_credits", Math.Floor(facts.Credits).ToString("0", CultureInfo.InvariantCulture), test.amount.ToString("0", CultureInfo.InvariantCulture), test.consume ? Text.Get("Story.test_paid") : ""),
        StorySchema.Condition => Text.Get("Story.test_condition", test.condition!, Mark(facts.PlayerHas(test.condition!))),
        _ => test.kind
    };
    private static string Mark(bool done) => done ? Text.Get("Story.yes") : Text.Get("Story.no");

    /// <summary>A weighted pick: <paramref name="roll"/> from 0 (inclusive) to 1 (exclusive) falls in exactly one entry.</summary>
    public static string? Pick(IReadOnlyList<(string Id, int Weight)> pool, double roll)
    {
        long total = 0;
        foreach (var entry in pool) total += Math.Max(0, entry.Weight);
        if (total < 1) return null;
        double point = Math.Min(Math.Max(roll, 0), 0.999999999) * total;
        foreach (var entry in pool)
        {
            if (entry.Weight <= 0) continue;
            if (point < entry.Weight) return entry.Id;
            point -= entry.Weight;
        }
        return pool.Last(e => e.Weight > 0).Id;
    }

    /// <summary>Whether a line may be voiced by this speaker (Framework 0.108.0): <c>crew</c> lines only by someone aboard
    /// one of the player's ships, <c>others</c> and <c>locals</c> lines only by someone who is not.</summary>
    public static bool Voices(string speakers, bool speakerIsCrew) =>
        speakers == StorySchema.Anyone || speakers == StorySchema.Crew && speakerIsCrew || (speakers == StorySchema.Others || speakers == StorySchema.Locals) && !speakerIsCrew;
    /// <summary>A placed line (Framework 0.114.0) is said only where it belongs: by crew while the player is at the
    /// place, by others only when they are there themselves. <c>locals</c> on an unplaced line means others.</summary>
    public static bool Voices(StoryLine line, bool speakerIsCrew, bool speakerAtPlace, bool playerNearPlace) =>
        Voices(line.Speakers, speakerIsCrew) && (line.Place == null || (speakerIsCrew ? playerNearPlace : speakerAtPlace));
    /// <summary>With the speaker's factions too (Framework 0.115.0): a line with <c>speakerFactions</c> needs one of them.</summary>
    public static bool Voices(StoryLine line, bool speakerIsCrew, bool speakerAtPlace, bool playerNearPlace, IEnumerable<string> speakerFactions) =>
        Voices(line, speakerIsCrew, speakerAtPlace, playerNearPlace) && (line.SpeakerFactions.Count == 0 || speakerFactions.Any(line.SpeakerFactions.Contains));

    /// <summary>How much a news item or advert weighs in the TV pool (Framework 0.114.0): more at its place, a little
    /// anywhere when it has none, and far from its place as little as the settings say. 0 leaves it out.</summary>
    public static int PlaceWeight(int weight, string? place, bool near, StorySettings settings)
    {
        double factor = place == null ? UnplacedFactor : near ? settings.localWeight : settings.farWeight;
        if (double.IsNaN(factor) || factor <= 0) return 0;
        return (int)Math.Max(0, Math.Min(int.MaxValue / 2, Math.Round(weight * factor)));
    }
    public const double UnplacedFactor = 2;

    /// <summary>Whether people still mention a news item: within the mention days of when it was first shown.</summary>
    public static bool MentionFresh(double? seenEpoch, double epoch, double mentionDays) =>
        seenEpoch is double seen && epoch - seen >= 0 && epoch - seen <= GameClock.Seconds(mentionDays);

    /// <summary>The eligible small-talk lines, by moment.</summary>
    public static Dictionary<string, List<StoryLine>> ChatterPools(IEnumerable<StoryLine> lines, Func<StoryRequires?, bool> eligible)
    {
        var pools = new Dictionary<string, List<StoryLine>>(StringComparer.Ordinal);
        foreach (var line in lines)
        {
            if (!eligible(line.Requires)) continue;
            if (!pools.TryGetValue(line.Moment, out var pool)) pools[line.Moment] = pool = new List<StoryLine>();
            pool.Add(line);
        }
        return pools;
    }

    /// <summary>A weighted pick among the lines this speaker may voice, or null when there are none.</summary>
    public static StoryLine? PickLine(IReadOnlyList<StoryLine>? pool, bool speakerIsCrew, double roll) =>
        PickLine(pool, l => Voices(l.Speakers, speakerIsCrew), roll);
    public static StoryLine? PickLine(IReadOnlyList<StoryLine>? pool, Func<StoryLine, bool> voices, double roll)
    {
        if (pool == null) return null;
        var voiced = pool.Where(voices).ToList();
        string? id = Pick(voiced.Select(l => (l.Id, l.Weight)).ToList(), roll);
        return id == null ? null : voiced.First(l => l.Id == id);
    }

    /// <summary>The step a saved arc is at: by id, else by position, else none (the pack changed too much).</summary>
    public static int Resolve(StoryArc arc, ArcProgress progress)
    {
        int byId = arc.steps.FindIndex(s => s.id == progress.StepId);
        if (byId >= 0) return byId;
        return progress.Step >= 0 && progress.Step < arc.steps.Count ? progress.Step : -1;
    }

    /// <summary>The goal test name a story step's objective carries in the save.</summary>
    public static string GoalTest(string arc, string step) => TestPrefix + arc + "." + step;
    /// <summary>The arc and step a goal test name points to, or false when it is not ours.</summary>
    public static bool TryGoal(string? name, out string arc, out string step)
    {
        arc = step = "";
        if (name == null || !name.StartsWith(TestPrefix, StringComparison.Ordinal)) return false;
        var parts = name.Substring(TestPrefix.Length).Split('.');
        if (parts.Length != 2 || !StorySchema.IsId(parts[0]) || !StorySchema.IsId(parts[1], StorySchema.MaxStepIdLength)) return false;
        arc = parts[0]; step = parts[1];
        return true;
    }
}
