using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Story;

public enum ArcState { Active, Done, Abandoned }

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
    private const string ArcPrefix = "arc.", SeenPrefix = "seen.", QueueKey = "queue";
    public Dictionary<string, ArcProgress> Arcs { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Seen { get; } = new(StringComparer.Ordinal);
    public List<string> Queue { get; } = new();
    private readonly Dictionary<string, string> kept = new(StringComparer.Ordinal);

    public static StoryRecord Decode(IReadOnlyDictionary<string, string> fields)
    {
        var record = new StoryRecord();
        foreach (var pair in fields ?? new Dictionary<string, string>())
        {
            if (pair.Key.StartsWith(ArcPrefix, StringComparison.Ordinal) && TryArc(pair.Value, out var arc)) record.Arcs[pair.Key.Substring(ArcPrefix.Length)] = arc;
            else if (pair.Key.StartsWith(SeenPrefix, StringComparison.Ordinal) && pair.Value == "1") record.Seen.Add(pair.Key.Substring(SeenPrefix.Length));
            else if (pair.Key == QueueKey) record.Queue.AddRange(pair.Value.Split('|').Where(id => id.Length > 0).Take(MaxQueue));
            else record.kept[pair.Key] = pair.Value;
        }
        return record;
    }

    public Dictionary<string, string> Encode()
    {
        var fields = new Dictionary<string, string>(kept, StringComparer.Ordinal);
        foreach (var pair in Arcs)
            fields[ArcPrefix + pair.Key] = string.Join("|", State(pair.Value.State), pair.Value.Step.ToString(CultureInfo.InvariantCulture), pair.Value.StepId,
                pair.Value.StepStart.ToString("R", CultureInfo.InvariantCulture), pair.Value.Completions.ToString(CultureInfo.InvariantCulture));
        foreach (var id in Seen) fields[SeenPrefix + id] = "1";
        if (Queue.Count > 0) fields[QueueKey] = string.Join("|", Queue.Take(MaxQueue));
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
}

/// <summary>The story rules with no game types: eligibility, goal tests and weighted picks.</summary>
public static class StoryRules
{
    /// <summary>Every story goal test name starts with this; the game resolves an unknown one to its always-true Blank.</summary>
    public const string TestPrefix = "PhobosStory.";
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
        return null;
    }

    public static bool Passed(StoryTest test, IStoryFacts facts, double stepStart) => test.kind switch
    {
        StorySchema.DockAt => facts.DockedAt(test.station!),
        StorySchema.HaveItem => facts.Carried(test.item!) >= test.count,
        StorySchema.Install => facts.Installed(test.item!) >= test.count,
        StorySchema.Wait => facts.Epoch - stepStart >= test.hours * 3600,
        _ => false
    };

    public static bool Passed(StoryStep step, IStoryFacts facts, double stepStart) => step.tests.All(t => Passed(t, facts, stepStart));

    /// <summary>One test and how far it has got, for the F3 report.</summary>
    public static string Describe(StoryTest test, IStoryFacts facts, double stepStart) => test.kind switch
    {
        StorySchema.DockAt => Text.Get("Story.test_dock", test.station!, Mark(Passed(test, facts, stepStart))),
        StorySchema.HaveItem => Text.Get("Story.test_have", test.item!, facts.Carried(test.item!), test.count, test.consume ? Text.Get("Story.test_taken") : ""),
        StorySchema.Install => Text.Get("Story.test_install", test.item!, facts.Installed(test.item!), test.count),
        StorySchema.Wait => Text.Get("Story.test_wait", Math.Max(0, (facts.Epoch - stepStart) / 3600).ToString("0.0", CultureInfo.InvariantCulture), test.hours.ToString("0.0", CultureInfo.InvariantCulture)),
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
