using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Phobos.Ostranauts.Framework.Data;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>The <c>story</c> schema (Framework 0.107.0; owner request, 6 October 2026): news, adverts and story arcs a
/// mod, an add-on or a player adds to the game. Every mod may ship one pack; Framework's own pack holds the settings.
/// Text is inline English, replaceable through a translation catalogue by <c>Story.&lt;id&gt;.&lt;field&gt;</c>. Nothing here
/// uses the game's plots, pledges or social interactions, so no name of ours enters a save beyond one record on the
/// player and the completion test named by each story goal (see docs/development/story-system-design.md).</summary>
public sealed class StoryPack : DataPack
{
    /// <summary>Framework's pack only: how much of the TV and how many arcs story content may take.</summary>
    public StorySettings? settings;
    /// <summary>TV news items by id.</summary>
    public Dictionary<string, StoryBroadcast> broadcasts = new(StringComparer.Ordinal);
    /// <summary>TV adverts by id.</summary>
    public Dictionary<string, StoryAdvert> adverts = new(StringComparer.Ordinal);
    /// <summary>Story arcs by id: ordered steps the player is given, each with goals that Framework checks.</summary>
    public Dictionary<string, StoryArc> arcs = new(StringComparer.Ordinal);
}

public sealed class StorySettings
{
    /// <summary>Share of TV news picks given to an eligible story broadcast (0 to 1); the rest stay the game's own.</summary>
    public double broadcastShare = 0.3;
    /// <summary>Share of TV advert picks given to an eligible story advert (0 to 1).</summary>
    public double advertShare = 0.3;
    /// <summary>Real seconds between story checks (goals, arc starts and the news pool).</summary>
    public double checkSeconds = 30;
    /// <summary>How many arcs may start by themselves at once; F3 starts are not limited.</summary>
    public int maxActiveArcs = 2;
}

/// <summary>When an entry may appear. Every part is optional and every part given must hold.</summary>
public sealed class StoryRequires
{
    /// <summary>Mods that must be installed: a Phobos mod by folder name (PhobosManufacturing) or any BepInEx plugin id.</summary>
    public List<string> mods = new();
    /// <summary>Game conditions the player must have.</summary>
    public List<string> playerConditions = new();
    /// <summary>Game conditions the player must not have.</summary>
    public List<string> forbidConditions = new();
    /// <summary>Item definitions that must be on one of the player's ships.</summary>
    public List<string> owns = new();
    /// <summary>Station registration ids the player must be docked at or aboard (any one of them); <c>any</c> for any station.</summary>
    public List<string> dockedAt = new();
    /// <summary>Arcs the player must have finished.</summary>
    public List<string> arcsDone = new();
    /// <summary>Arcs the player must never have started.</summary>
    public List<string> arcsNotStarted = new();
}

public sealed class StoryBroadcast
{
    /// <summary>For authors; the game never shows a headline's title.</summary>
    public string? title;
    public string? notes;
    /// <summary>Shown above the item as "Region News:".</summary>
    public string region = "";
    public string text = "";
    /// <summary>How often it is picked against other story broadcasts (1 to 100).</summary>
    public int weight = 1;
    /// <summary>Shown once in a save, then never again.</summary>
    public bool once;
    public StoryRequires? requires;
}

public sealed class StoryAdvert
{
    public string? title;
    public string? notes;
    public string text = "";
    public int weight = 1;
    public bool once;
    public StoryRequires? requires;
}

public sealed class StoryArc
{
    /// <summary>For authors and the F3 list; the player sees each step's goal title.</summary>
    public string title = "";
    public string? notes;
    public StoryRequires? requires;
    /// <summary>Chance per check that an eligible arc starts by itself (0 to 1); 0 starts only from F3.</summary>
    public double chance;
    /// <summary>May start again after it is finished.</summary>
    public bool repeatable;
    public List<StoryStep> steps = new();
}

public sealed class StoryStep
{
    public string id = "";
    /// <summary>What the player is told when the step begins.</summary>
    public StoryDelivery? delivery;
    /// <summary>A goal in the game's GOALS list; a step without one waits on its tests unseen.</summary>
    public StoryObjective? objective;
    /// <summary>All must pass for the step to finish.</summary>
    public List<StoryTest> tests = new();
    public StoryOutcome? onComplete;
}

public sealed class StoryDelivery
{
    public StoryMessage? message;
    /// <summary>A broadcast id that the next TV news pick shows.</summary>
    public string? bulletin;
}

public sealed class StoryMessage
{
    /// <summary>Who it is from, shown before the text in the crew log.</summary>
    public string from = "";
    public string text = "";
}

public sealed class StoryObjective
{
    public string title = "";
    public string description = "";
}

/// <summary>One goal test, from a fixed list of kinds interpreted in code.</summary>
public sealed class StoryTest
{
    /// <summary><c>dock-at</c>, <c>have-item</c>, <c>install</c> or <c>wait</c>.</summary>
    public string kind = "";
    /// <summary>dock-at: a station registration id, or <c>any</c>.</summary>
    public string? station;
    /// <summary>have-item and install: an item definition id.</summary>
    public string? item;
    /// <summary>have-item and install: how many.</summary>
    public int count = 1;
    /// <summary>have-item: the items are taken from the player when the step finishes.</summary>
    public bool consume;
    /// <summary>wait: game hours since the step began.</summary>
    public double hours;
}

public sealed class StoryOutcome
{
    public StoryMessage? message;
    /// <summary>Items given to the player, or put at their feet when their hands and bags are full.</summary>
    public List<StoryReward> items = new();
}

public sealed class StoryReward
{
    public string item = "";
    public int count = 1;
}

/// <summary>The checks every story file passes, shipped, add-on or player. Names of the game's items and conditions
/// and references between packs are checked when the packs are merged (<see cref="StoryLibrary"/>).</summary>
public static class StorySchema
{
    public const string Name = "story";
    public const string DockedAnywhere = "any";
    public const string DockAt = "dock-at", HaveItem = "have-item", Install = "install", Wait = "wait";
    public static readonly IReadOnlyList<string> TestKinds = new[] { DockAt, HaveItem, Install, Wait };
    public static readonly IReadOnlyList<string> Placeholders = new[] { "[player]", "[player-first]", "[ship]" };
    public const int MaxIdLength = 48, MaxStepIdLength = 32, MaxRegion = 40, MaxBroadcast = 700, MaxAdvert = 400,
        MaxMessage = 400, MaxFrom = 40, MaxObjectiveTitle = 60, MaxObjectiveDescription = 300, MaxTitle = 80,
        MaxSteps = 12, MaxTests = 4, MaxRewards = 5, MaxRewardCount = 20, MaxItemCount = 100, MaxWeight = 100,
        MaxListEntries = 16, MaxActiveArcs = 10;
    public const double MaxWaitHours = 720, MinCheckSeconds = 5, MaxCheckSeconds = 600;
    private static readonly Regex Id = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex GameName = new("^[A-Za-z0-9_]+$", RegexOptions.CultureInvariant);
    private static readonly Regex Station = new("^[A-Za-z0-9_|-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex PhobosMod = new("^Phobos[A-Za-z]+$", RegexOptions.CultureInvariant);
    private static readonly Regex PluginId = new("^[A-Za-z0-9_-]+(\\.[A-Za-z0-9_-]+)+$", RegexOptions.CultureInvariant);

    public static bool IsId(string? id, int max = MaxIdLength) => id != null && id.Length <= max && Id.IsMatch(id);

    /// <summary>Validates a pack. Only Framework's own pack may carry settings.</summary>
    public static void Validate(StoryPack pack, bool frameworkPack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        if (pack.settings != null)
        {
            if (!frameworkPack) throw new ArgumentException(Text.Get("StorySchema.settings_elsewhere"));
            var s = pack.settings;
            Range(s.broadcastShare, 0, 1, "settings.broadcastShare"); Range(s.advertShare, 0, 1, "settings.advertShare");
            Range(s.checkSeconds, MinCheckSeconds, MaxCheckSeconds, "settings.checkSeconds");
            if (s.maxActiveArcs < 0 || s.maxActiveArcs > MaxActiveArcs) throw new ArgumentException(Text.Get("StorySchema.range", "settings.maxActiveArcs", 0, MaxActiveArcs));
        }
        foreach (var pair in pack.broadcasts)
        {
            string where = "broadcasts." + pair.Key; var b = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(b.title, b.notes, where);
            if (string.IsNullOrWhiteSpace(b.region) || b.region.Length > MaxRegion || Plain(b.region) != null) throw new ArgumentException(Text.Get("StorySchema.region", where, MaxRegion));
            Words(b.text, MaxBroadcast, where + ".text");
            Weight(b.weight, where); Requires(b.requires, where);
        }
        foreach (var pair in pack.adverts)
        {
            string where = "adverts." + pair.Key; var a = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(a.title, a.notes, where);
            Words(a.text, MaxAdvert, where + ".text");
            Weight(a.weight, where); Requires(a.requires, where);
        }
        foreach (var pair in pack.arcs) Arc(pair.Key, pair.Value);
    }

    private static void Arc(string id, StoryArc? arc)
    {
        string where = "arcs." + id;
        if (arc == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        EntryId(id, where);
        if (string.IsNullOrWhiteSpace(arc.title) || arc.title.Length > MaxTitle) throw new ArgumentException(Text.Get("StorySchema.title", where, MaxTitle));
        Author(null, arc.notes, where);
        Range(arc.chance, 0, 1, where + ".chance");
        Requires(arc.requires, where);
        if (arc.steps == null || arc.steps.Count == 0 || arc.steps.Count > MaxSteps) throw new ArgumentException(Text.Get("StorySchema.steps", where, MaxSteps));
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < arc.steps.Count; i++)
        {
            var step = arc.steps[i] ?? throw new ArgumentException(Text.Get("StorySchema.empty", where + ".steps." + i));
            string at = where + ".steps." + (IsId(step.id, MaxStepIdLength) ? step.id : i.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (!IsId(step.id, MaxStepIdLength)) throw new ArgumentException(Text.Get("StorySchema.id", at, MaxStepIdLength));
            if (!ids.Add(step.id)) throw new ArgumentException(Text.Get("StorySchema.step_twice", where, step.id));
            if (step.delivery != null)
            {
                if (step.delivery.message == null && step.delivery.bulletin == null) throw new ArgumentException(Text.Get("StorySchema.empty", at + ".delivery"));
                Message(step.delivery.message, at + ".delivery.message");
                if (step.delivery.bulletin != null && !IsId(step.delivery.bulletin)) throw new ArgumentException(Text.Get("StorySchema.id", at + ".delivery.bulletin", MaxIdLength));
            }
            if (step.objective != null)
            {
                Words(step.objective.title, MaxObjectiveTitle, at + ".objective.title");
                if (step.objective.description.Length > 0) Words(step.objective.description, MaxObjectiveDescription, at + ".objective.description");
            }
            if (step.tests == null || step.tests.Count == 0 || step.tests.Count > MaxTests) throw new ArgumentException(Text.Get("StorySchema.tests", at, MaxTests));
            foreach (var test in step.tests) Test(test, at + ".tests");
            if (step.onComplete != null)
            {
                Message(step.onComplete.message, at + ".onComplete.message");
                if (step.onComplete.items == null || step.onComplete.items.Count > MaxRewards) throw new ArgumentException(Text.Get("StorySchema.rewards", at, MaxRewards));
                foreach (var reward in step.onComplete.items)
                {
                    if (reward == null || !GameName.IsMatch(reward.item ?? "")) throw new ArgumentException(Text.Get("StorySchema.game_name", at + ".onComplete.items", reward?.item ?? ""));
                    if (reward.count < 1 || reward.count > MaxRewardCount) throw new ArgumentException(Text.Get("StorySchema.range", at + ".onComplete.items." + reward.item, 1, MaxRewardCount));
                }
            }
        }
    }

    private static void Test(StoryTest? test, string where)
    {
        if (test == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        where += "." + test.kind;
        bool station = test.station != null, item = test.item != null, hours = test.hours != 0;
        switch (test.kind)
        {
            case DockAt:
                if (!station || item || hours || test.consume || test.count != 1) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, "station"));
                if (!StationId(test.station!)) throw new ArgumentException(Text.Get("StorySchema.station", where, test.station!));
                break;
            case HaveItem: case Install:
                if (!item || station || hours || test.consume && test.kind == Install) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, test.kind == Install ? "item, count" : "item, count, consume"));
                if (!GameName.IsMatch(test.item!)) throw new ArgumentException(Text.Get("StorySchema.game_name", where, test.item!));
                if (test.count < 1 || test.count > MaxItemCount) throw new ArgumentException(Text.Get("StorySchema.range", where + ".count", 1, MaxItemCount));
                break;
            case Wait:
                if (station || item || test.consume || test.count != 1) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, "hours"));
                if (!(test.hours > 0) || test.hours > MaxWaitHours) throw new ArgumentException(Text.Get("StorySchema.wait_hours", where, MaxWaitHours));
                break;
            default: throw new ArgumentException(Text.Get("StorySchema.test_kind", where, string.Join(", ", TestKinds)));
        }
    }

    private static void Requires(StoryRequires? r, string where)
    {
        if (r == null) return;
        where += ".requires";
        List(r.mods, where + ".mods", m => PhobosMod.IsMatch(m) || PluginId.IsMatch(m));
        List(r.playerConditions, where + ".playerConditions", GameName.IsMatch);
        List(r.forbidConditions, where + ".forbidConditions", GameName.IsMatch);
        List(r.owns, where + ".owns", GameName.IsMatch);
        List(r.dockedAt, where + ".dockedAt", StationId);
        List(r.arcsDone, where + ".arcsDone", s => IsId(s));
        List(r.arcsNotStarted, where + ".arcsNotStarted", s => IsId(s));
    }

    private static void List(List<string>? values, string where, Func<string, bool> valid)
    {
        if (values == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        if (values.Count > MaxListEntries) throw new ArgumentException(Text.Get("StorySchema.list_long", where, MaxListEntries));
        foreach (var value in values) if (value == null || !valid(value)) throw new ArgumentException(Text.Get("StorySchema.game_name", where, value ?? ""));
    }

    public static bool StationId(string value) => value == DockedAnywhere || value.Length <= 32 && Station.IsMatch(value);

    private static void EntryId(string id, string where)
    {
        if (!IsId(id)) throw new ArgumentException(Text.Get("StorySchema.id", where, MaxIdLength));
    }
    private static void Author(string? title, string? notes, string where)
    {
        if (title != null && (title.Length > MaxTitle || Plain(title) != null)) throw new ArgumentException(Text.Get("StorySchema.title", where, MaxTitle));
        if (notes != null && notes.Length > 2000) throw new ArgumentException(Text.Get("StorySchema.notes", where));
    }
    private static void Weight(int weight, string where)
    {
        if (weight < 1 || weight > MaxWeight) throw new ArgumentException(Text.Get("StorySchema.range", where + ".weight", 1, MaxWeight));
    }
    private static void Range(double value, double min, double max, string where)
    {
        if (double.IsNaN(value) || value < min || value > max) throw new ArgumentException(Text.Get("StorySchema.range", where, min, max));
    }
    private static void Message(StoryMessage? message, string where)
    {
        if (message == null) return;
        if (string.IsNullOrWhiteSpace(message.from) || message.from.Length > MaxFrom || Plain(message.from) != null) throw new ArgumentException(Text.Get("StorySchema.from", where, MaxFrom));
        Words(message.text, MaxMessage, where + ".text");
    }

    /// <summary>Player text: not blank, within its length, no markup, and no bracketed token but the placeholders.</summary>
    public static void Words(string? text, int max, string where)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException(Text.Get("StorySchema.text_blank", where));
        if (text!.Length > max) throw new ArgumentException(Text.Get("StorySchema.text_long", where, max, text.Length));
        string? problem = Plain(text);
        if (problem != null) throw new ArgumentException(Text.Get("StorySchema.text_token", where, problem, string.Join(" ", Placeholders)));
    }

    /// <summary>The first thing in a text that is not allowed (a control character other than a line break, angle
    /// brackets, or a square-bracket token other than a placeholder), or null.</summary>
    public static string? Plain(string text)
    {
        foreach (char c in text) if (c == '<' || c == '>' || char.IsControl(c) && c != '\n') return c == '\n' ? "\\n" : c.ToString();
        string rest = text;
        foreach (var token in Placeholders) rest = rest.Replace(token, "");
        int open = rest.IndexOf('['), close = rest.IndexOf(']');
        if (open < 0 && close < 0) return null;
        if (open >= 0) { int end = rest.IndexOf(']', open); return end > open ? rest.Substring(open, end - open + 1) : "["; }
        return "]";
    }

    /// <summary>Fills the placeholders. Missing values read as a plain description, never as the token.</summary>
    public static string Fill(string text, string player, string firstName, string ship) =>
        text.Replace("[player-first]", firstName).Replace("[player]", player).Replace("[ship]", ship);
}
