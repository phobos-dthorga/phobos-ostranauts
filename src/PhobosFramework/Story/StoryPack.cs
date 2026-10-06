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
    /// <summary>Lines people may say in the game's own small talk, by id (Framework 0.108.0).</summary>
    public Dictionary<string, StoryChatterLine> chatter = new(StringComparer.Ordinal);
    /// <summary>Lore tips for loading screens, by id (Framework 0.108.0).</summary>
    public Dictionary<string, StoryTip> tips = new(StringComparer.Ordinal);
    /// <summary>Top-level encyclopedia entries, by id (Framework 0.108.0); shown only while one of their articles is.</summary>
    public Dictionary<string, StorySection> sections = new(StringComparer.Ordinal);
    /// <summary>Encyclopedia articles, by id, each under a section (Framework 0.108.0).</summary>
    public Dictionary<string, StoryArticle> articles = new(StringComparer.Ordinal);
    /// <summary>Data files, by id (Framework 0.110.0): read on any computer or PDA from the data card that carries them.</summary>
    public Dictionary<string, StoryFile> files = new(StringComparer.Ordinal);
    /// <summary>Places content is grounded in (Framework 0.114.0): stations by registration id. Framework ships the
    /// game's regional stations; add-ons add their own.</summary>
    public Dictionary<string, StoryPlace> places = new(StringComparer.Ordinal);
    /// <summary>Named recurring people with a home place (Framework 0.114.0): the senders of letters and the cast of threads.</summary>
    public Dictionary<string, StoryPerson> people = new(StringComparer.Ordinal);
    /// <summary>Threads (Framework 0.114.0): a story's home place, cast and shared requirements, which every entry
    /// declaring <c>thread</c> inherits.</summary>
    public Dictionary<string, StoryThread> threads = new(StringComparer.Ordinal);
}

/// <summary>A place (Framework 0.114.0): a station, or a part of one, that content can belong to.</summary>
public sealed class StoryPlace
{
    public string? notes;
    /// <summary>The station's registration id or prefix (OKLG, VORB_HAB); its parts count as it does.</summary>
    public string station = "";
    /// <summary>The regional place this one lies within (one level); a place without one is a region of its own.</summary>
    public string? within;
    /// <summary>The "Region News:" label for news from here; required on a regional place, inherited by its parts.</summary>
    public string? region;
    /// <summary>The body it orbits or stands on, for authors and the [body] placeholder.</summary>
    public string? body;
    /// <summary>The game's faction names at home here, for authors.</summary>
    public List<string> factions = new();
    /// <summary>What people call it: the [place] placeholder.</summary>
    public string name = "";
}

/// <summary>A named recurring person (Framework 0.114.0), shown as "Name, role" where a letter names its sender.</summary>
public sealed class StoryPerson
{
    public string? notes;
    public string name = "";
    public string? role;
    /// <summary>The place they belong to.</summary>
    public string home = "";
    /// <summary>The game's faction name they belong to, for authors.</summary>
    public string? faction;
    /// <summary>The look of the face the game makes for them (Framework 0.121.0): <c>masculine</c>, <c>feminine</c> or
    /// <c>any</c> (the default). The face itself is rolled once per save by the game's own face roll.</summary>
    public string? face;
}

/// <summary>A thread (Framework 0.114.0): the entries that declare it share its place, cast and requirements.</summary>
public sealed class StoryThread
{
    public string title = "";
    public string? notes;
    /// <summary>The place its members belong to unless they name their own.</summary>
    public string? place;
    /// <summary>Its cast: the people its letters may come from. Empty leaves the cast open.</summary>
    public List<string> people = new();
    /// <summary>Requirements every member must also meet.</summary>
    public StoryRequires? requires;
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
    /// <summary>Share of matching small talk that uses a story line when one is eligible (0 to 1).</summary>
    public double chatterShare = 0.4;
    /// <summary>Share of loading-screen tips taken from story tips (0 to 1).</summary>
    public double tipShare = 0.3;
    /// <summary>How much more often news and adverts of the place the player is at are picked (Framework 0.114.0); an
    /// unplaced entry counts 2, and one placed elsewhere counts <see cref="farWeight"/>. 0 hides far news.</summary>
    public double localWeight = 4, farWeight = 1;
    /// <summary>Game days after a news item was shown during which people still mention it (Framework 0.114.0).</summary>
    public double mentionDays = 10;
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
    /// <summary>Story data files the player must have opened (Framework 0.110.0).</summary>
    public List<string> filesRead = new();
    /// <summary>Only once this many game days have passed since the player's story record began (Framework 0.109.0).</summary>
    public double? afterDays;
    /// <summary>Only until this many game days have passed since the player's story record began.</summary>
    public double? beforeDays;
    /// <summary>Story flags set by an arc outcome (Framework 0.114.0): all must be set.</summary>
    public List<string> flags = new();
    /// <summary>Story flags none of which may be set.</summary>
    public List<string> notFlags = new();
    /// <summary>Arcs that must be under way.</summary>
    public List<string> arcsActive = new();
    /// <summary>Arcs at a given step, as <c>arc.step</c>: each must be under way at that step.</summary>
    public List<string> arcsAtStep = new();
    /// <summary>Places the player must be at (any one): docked at it, or in its region when it is a regional place.</summary>
    public List<string> places = new();
    /// <summary>Regional places the player must be in the region of (any one).</summary>
    public List<string> regions = new();
    /// <summary>News items that must have been shown on a TV.</summary>
    public List<string> newsSeen = new();
    /// <summary>How the game's factions regard the player (Framework 0.115.0): each must hold.</summary>
    public List<StoryStanding> standing = new();
    /// <summary>Game conditions (skills among them) that someone aboard other than the player must have, one person per condition.</summary>
    public List<string> crewWith = new();
    /// <summary>How many crew the player has, the player not counted.</summary>
    public StoryCount? crewCount;
    /// <summary>Phobos machines, by installed definition id, that must be running on one of the player's ships.</summary>
    public List<string> running = new();
    /// <summary>Calendar months (1 to 12) the entry is for.</summary>
    public List<int> months = new();
    /// <summary>A window of the day in UTC hours (0 to 23); <c>from</c> after <c>to</c> wraps midnight.</summary>
    public StoryHours? hours;
}

/// <summary>A faction's standing with the player (Framework 0.115.0), by the game's own tiers.</summary>
public sealed class StoryStanding
{
    /// <summary>The game's faction name, such as OKLGCorp.</summary>
    public string faction = "";
    /// <summary>The lowest tier that holds (<c>dislikes</c>, <c>neutral</c>, <c>warm</c>, <c>friendly</c>, <c>trusted</c>, <c>honored</c>).</summary>
    public string? atLeast;
    /// <summary>The highest tier that holds.</summary>
    public string? atMost;
}

public sealed class StoryCount
{
    public int atLeast;
    public int? atMost;
}

public sealed class StoryHours
{
    public int from, to;
}

/// <summary>A change to a faction's standing with the player (Framework 0.115.0), through the game's own scores.</summary>
public sealed class StoryStandingChange
{
    public string faction = "";
    /// <summary>Points added to the faction's view of the player, up to 10 either way; a tier is 25.</summary>
    public double change;
}

public sealed class StoryBroadcast
{
    /// <summary>For authors; the game never shows a headline's title.</summary>
    public string? title;
    public string? notes;
    /// <summary>Shown above the item as "Region News:"; taken from the place when left out (Framework 0.114.0).</summary>
    public string? region;
    public string text = "";
    /// <summary>How often it is picked against other story broadcasts (1 to 100).</summary>
    public int weight = 1;
    /// <summary>Shown once in a save, then never again.</summary>
    public bool once;
    public StoryRequires? requires;
    /// <summary>What people say when they bring the news up in small talk (the headline moment), for a while after it was shown.</summary>
    public string? mention;
    /// <summary>The thread it belongs to and the place it is news of (Framework 0.114.0).</summary>
    public string? thread, place;
}

public sealed class StoryAdvert
{
    public string? title;
    public string? notes;
    public string text = "";
    public int weight = 1;
    public bool once;
    public StoryRequires? requires;
    public string? thread, place;
}

/// <summary>A line someone says in one of the game's own small-talk moments.</summary>
public sealed class StoryChatterLine
{
    public string? title;
    public string? notes;
    /// <summary>One of <see cref="StoryMoments"/>: which kind of small talk carries the line.</summary>
    public string moment = "";
    public string line = "";
    /// <summary><c>anyone</c>, <c>crew</c> (the speaker is aboard one of the player's ships), <c>others</c>, or
    /// <c>locals</c> (others, at the line's place; Framework 0.114.0).</summary>
    public string speakers = StorySchema.Anyone;
    public int weight = 1;
    public StoryRequires? requires;
    /// <summary>The thread it belongs to, and the place it is said at: crew say it while the player is there, others
    /// only when they are there themselves (Framework 0.114.0).</summary>
    public string? thread, place;
    /// <summary>Game faction names the speaker must belong to one of (Framework 0.115.0), such as OKLGLEO for AyoSec.</summary>
    public List<string> speakerFactions = new();
}

/// <summary>A lore tip shown while the game loads. No player exists then, so only mods may be required.</summary>
public sealed class StoryTip
{
    public string? title;
    public string? notes;
    public string text = "";
    public int weight = 1;
    public StoryRequires? requires;
    /// <summary>The thread it belongs to, for authors and the F3 thread report; a tip inherits only the thread's mods.</summary>
    public string? thread;
}

/// <summary>A top-level entry in the game's encyclopedia.</summary>
public sealed class StorySection
{
    public string? notes;
    /// <summary>The name in the encyclopedia's list.</summary>
    public string label = "";
    public string title = "";
    public string? body;
    /// <summary>A picture beside the page (Framework 0.110.0): a path under a mod's images folder, without .png.</summary>
    public string? image;
    public StoryRequires? requires;
    public string? thread;
}

/// <summary>An encyclopedia article under a section.</summary>
public sealed class StoryArticle
{
    public string? notes;
    /// <summary>A section id from any loaded pack.</summary>
    public string section = "";
    public string label = "";
    public string title = "";
    public string body = "";
    /// <summary>A picture beside the page (Framework 0.110.0): a path under a mod's images folder, without .png.</summary>
    public string? image;
    public StoryRequires? requires;
    public string? thread;
}

/// <summary>The small-talk moments story lines may use (Framework 0.108.0): each is a set of the game's own social
/// interactions, whose text is swapped for one use, and a lead-in in Framework's catalogue
/// (<c>Story.moment.&lt;moment&gt;</c>) that uses only grammar tokens the game's own lines use.</summary>
public static class StoryMoments
{
    public static readonly IReadOnlyDictionary<string, string[]> Interactions = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["headline"] = new[] { "SOCMentionHeadline" },
        ["joke"] = new[] { "SOCMildFunny", "SOCDarkJoke", "SOCCraftDarkJoke" },
        ["complaint"] = new[] { "SOCComplainAboutLAs" },
        ["story"] = new[] { "SOCReminisce", "SOCShareAnotherStory" },
        ["jargon"] = new[] { "SOCTechJargon" },
        ["superstition"] = new[] { "SOCWarnSuperstition" },
        ["worry"] = new[] { "SOCAdmitWorries" },
        ["question"] = new[] { "SOCMetaphysicalQuandary" },
        ["small-talk"] = new[] { "SOCShootBreeze" },
    };
    public const string Headline = "headline";
    private static readonly Dictionary<string, string> byInteraction =
        Interactions.SelectMany(m => m.Value.Select(i => (Interaction: i, Moment: m.Key))).ToDictionary(p => p.Interaction, p => p.Moment, StringComparer.Ordinal);
    /// <summary>The moment a game interaction belongs to, or null.</summary>
    public static string? Of(string? interaction) => interaction != null && byInteraction.TryGetValue(interaction, out var moment) ? moment : null;
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
    /// <summary>The thread it belongs to, and its place (Framework 0.114.0): it starts by itself only while the player
    /// is there, and a dock-at test without a station means this place.</summary>
    public string? thread, place;
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
    /// <summary>The step that follows (Framework 0.109.0): a step id of the same arc, or <c>end</c>; by default the next in order.</summary>
    public string? next;
    /// <summary>Other ways the step can finish, each with its own tests, outcome and next step, checked after the step's own tests.</summary>
    public List<StoryBranch>? branches;
}

/// <summary>Another way a step can finish (Framework 0.109.0): the first branch whose tests all pass decides what happens.</summary>
public sealed class StoryBranch
{
    public string? notes;
    public List<StoryTest> tests = new();
    public StoryOutcome? onComplete;
    /// <summary>A step id of the same arc, or <c>end</c>.</summary>
    public string next = "";
}

public sealed class StoryDelivery
{
    public StoryMessage? message;
    /// <summary>A broadcast id that the next TV news pick shows.</summary>
    public string? bulletin;
}

public sealed class StoryMessage
{
    /// <summary>Who it is from, shown before the text in the crew log; left out when <see cref="person"/> names them.</summary>
    public string? from;
    public string text = "";
    /// <summary>The person it is from (Framework 0.114.0), shown as "Name, role".</summary>
    public string? person;
}

public sealed class StoryObjective
{
    public string title = "";
    public string description = "";
    /// <summary>Whose face the goal shows and who it says it is from (Framework 0.121.0); left out, the sender of the
    /// step's letter, else the arc's last sender before it.</summary>
    public string? person;
}

/// <summary>One goal test, from a fixed list of kinds interpreted in code.</summary>
public sealed class StoryTest
{
    /// <summary><c>dock-at</c>, <c>have-item</c>, <c>install</c> or <c>wait</c>.</summary>
    public string kind = "";
    /// <summary>dock-at: a station registration id, or <c>any</c>; left out, the arc's own place (Framework 0.114.0).</summary>
    public string? station;
    /// <summary>have-item and install: an item definition id.</summary>
    public string? item;
    /// <summary>have-item and install: how many.</summary>
    public int count = 1;
    /// <summary>have-item: the items are taken from the player when the step finishes.</summary>
    public bool consume;
    /// <summary>wait: game hours since the step began.</summary>
    public double hours;
    /// <summary>credits: how many the player holds (taken when the step finishes, with consume).</summary>
    public double amount;
    /// <summary>condition: a game condition the player has, such as a skill.</summary>
    public string? condition;
}

public sealed class StoryOutcome
{
    public StoryMessage? message;
    /// <summary>Items given to the player, or put at their feet when their hands and bags are full.</summary>
    public List<StoryReward> items = new();
    /// <summary>Credits paid to the player, entered in the game's ledger (Framework 0.109.0).</summary>
    public int credits;
    /// <summary>Story data files given on one data card (Framework 0.110.0).</summary>
    public List<string> files = new();
    /// <summary>Story flags set and cleared on the player's record (Framework 0.114.0), for other entries' requirements.</summary>
    public List<string> setFlags = new(), clearFlags = new();
    /// <summary>Changes to factions' standing with the player (Framework 0.115.0; owner choice: small), at most two.</summary>
    public List<StoryStandingChange> standing = new();
}

/// <summary>A data file (Framework 0.110.0), carried on a data card and read on a computer or PDA like the game's own.</summary>
public sealed class StoryFile
{
    public string? notes;
    /// <summary>The file name a computer lists, such as TRIAL_NOTES.TXT.</summary>
    public string name = "";
    public string text = "";
    /// <summary>An arc that starts when the file is first opened, if it has not started yet.</summary>
    public string? startsArc;
    /// <summary>The thread it belongs to, the place it is about and the person who wrote it (Framework 0.114.0), for
    /// the placeholders and the F3 thread report.</summary>
    public string? thread, place, person;
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
    public const string Anyone = "anyone", Crew = "crew", Others = "others", Locals = "locals";
    public static readonly IReadOnlyList<string> Speakers = new[] { Anyone, Crew, Others, Locals };
    public const string DockAt = "dock-at", HaveItem = "have-item", Install = "install", Wait = "wait", Credits = "credits", Condition = "condition";
    public static readonly IReadOnlyList<string> TestKinds = new[] { DockAt, HaveItem, Install, Wait, Credits, Condition };
    public const string End = "end";
    public const int MaxBranches = 4, MaxCreditReward = 50000, MaxFileName = 32, MaxFileText = 3000, MaxFiles = 5, MaxImage = 100;
    private static readonly Regex FileName = new("^[A-Za-z0-9_.-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex ImagePath = new("^[A-Za-z0-9_-]+(/[A-Za-z0-9_-]+)*$", RegexOptions.CultureInvariant);
    public const double MaxCreditTest = 1000000, MaxDays = 3650, MaxWeightFactor = 100, MaxMentionDays = 365;
    /// <summary>The bracketed tokens player text may hold. <c>[person:key]</c> names a person (Framework 0.114.0).</summary>
    public static readonly IReadOnlyList<string> Placeholders = new[] { "[player]", "[player-first]", "[ship]", "[place]", "[region]", "[station]", "[body]", "[date]", "[crew]" };
    public const int MaxPlaces = 64, MaxPeople = 64, MaxThreads = 32, MaxCast = 8, MaxFactions = 8, MaxFlags = 4, MaxName = 40;
    /// <summary>The game's standing tiers, lowest first (Framework 0.115.0).</summary>
    public static readonly string[] Tiers = { "dislikes", "neutral", "warm", "friendly", "trusted", "honored" };
    public const double MaxStandingChange = 10;
    public const int MaxStandingChanges = 2, MaxCrewCount = 50, MaxSpeakerFactions = 4;
    private static readonly Regex PersonToken = new("\\[person:[a-z0-9]+(-[a-z0-9]+)*\\]", RegexOptions.CultureInvariant);
    private static readonly Regex Token = new("\\[([a-z-]+(?::[a-z0-9-]+)?)\\]", RegexOptions.CultureInvariant);
    public const int MaxIdLength = 48, MaxStepIdLength = 32, MaxRegion = 40, MaxBroadcast = 700, MaxAdvert = 400,
        MaxMessage = 400, MaxFrom = 40, MaxObjectiveTitle = 60, MaxObjectiveDescription = 300, MaxTitle = 80,
        MaxSteps = 12, MaxTests = 4, MaxRewards = 5, MaxRewardCount = 20, MaxItemCount = 100, MaxWeight = 100,
        MaxListEntries = 16, MaxActiveArcs = 10, MaxLine = 200, MaxTip = 450, MaxLabel = 40, MaxArticle = 4000;
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
            Range(s.chatterShare, 0, 1, "settings.chatterShare"); Range(s.tipShare, 0, 1, "settings.tipShare");
            if (s.maxActiveArcs < 0 || s.maxActiveArcs > MaxActiveArcs) throw new ArgumentException(Text.Get("StorySchema.range", "settings.maxActiveArcs", 0, MaxActiveArcs));
            Range(s.localWeight, 0, MaxWeightFactor, "settings.localWeight"); Range(s.farWeight, 0, MaxWeightFactor, "settings.farWeight");
            Range(s.mentionDays, 0, MaxMentionDays, "settings.mentionDays");
        }
        if (pack.places.Count > MaxPlaces) throw new ArgumentException(Text.Get("StorySchema.table_long", "places", MaxPlaces));
        foreach (var pair in pack.places)
        {
            string where = "places." + pair.Key; var p = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, p.notes, where);
            if (string.IsNullOrEmpty(p.station) || p.station == DockedAnywhere || !StationId(p.station)) throw new ArgumentException(Text.Get("StorySchema.station", where + ".station", p.station ?? ""));
            Key(p.within, where + ".within");
            if (p.region != null && (string.IsNullOrWhiteSpace(p.region) || p.region.Length > MaxRegion || Plain(p.region) != null)) throw new ArgumentException(Text.Get("StorySchema.region", where, MaxRegion));
            if (p.within == null && p.region == null) throw new ArgumentException(Text.Get("StorySchema.region", where, MaxRegion));
            Short(p.body, MaxName, where + ".body", false);
            Short(p.name, MaxName, where + ".name", true);
            if (p.factions == null || p.factions.Count > MaxFactions) throw new ArgumentException(Text.Get("StorySchema.list_long", where + ".factions", MaxFactions));
            foreach (var f in p.factions) if (f == null || !GameName.IsMatch(f)) throw new ArgumentException(Text.Get("StorySchema.game_name", where + ".factions", f ?? ""));
        }
        if (pack.people.Count > MaxPeople) throw new ArgumentException(Text.Get("StorySchema.table_long", "people", MaxPeople));
        foreach (var pair in pack.people)
        {
            string where = "people." + pair.Key; var p = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, p.notes, where);
            Short(p.name, MaxName, where + ".name", true); Short(p.role, MaxName, where + ".role", false);
            if (!IsId(p.home)) throw new ArgumentException(Text.Get("StorySchema.id", where + ".home", MaxIdLength));
            if (p.faction != null && !GameName.IsMatch(p.faction)) throw new ArgumentException(Text.Get("StorySchema.game_name", where + ".faction", p.faction));
            if (!Social.PortraitRules.IsLook(p.face)) throw new ArgumentException(Text.Get("StorySchema.face", where + ".face", string.Join(", ", Social.PortraitRules.Looks)));
        }
        if (pack.threads.Count > MaxThreads) throw new ArgumentException(Text.Get("StorySchema.table_long", "threads", MaxThreads));
        foreach (var pair in pack.threads)
        {
            string where = "threads." + pair.Key; var t = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, t.notes, where);
            if (string.IsNullOrWhiteSpace(t.title) || t.title.Length > MaxTitle || Plain(t.title) != null) throw new ArgumentException(Text.Get("StorySchema.title", where, MaxTitle));
            Key(t.place, where + ".place");
            if (t.people == null || t.people.Count > MaxCast || t.people.Any(p => !IsId(p)) || t.people.Distinct().Count() != t.people.Count) throw new ArgumentException(Text.Get("StorySchema.list_long", where + ".people", MaxCast));
            Requires(t.requires, where);
        }
        foreach (var pair in pack.broadcasts)
        {
            string where = "broadcasts." + pair.Key; var b = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(b.title, b.notes, where);
            if (b.region != null && (string.IsNullOrWhiteSpace(b.region) || b.region.Length > MaxRegion || Plain(b.region) != null)) throw new ArgumentException(Text.Get("StorySchema.region", where, MaxRegion));
            // A broadcast is news of somewhere: its own region label, or a place (checked when the packs merge).
            if (b.region == null && b.place == null && b.thread == null) throw new ArgumentException(Text.Get("StorySchema.region", where, MaxRegion));
            Words(b.text, MaxBroadcast, where + ".text");
            if (b.mention != null) Words(b.mention, MaxLine, where + ".mention");
            Weight(b.weight, where); Requires(b.requires, where); Key(b.thread, where + ".thread"); Key(b.place, where + ".place");
        }
        foreach (var pair in pack.adverts)
        {
            string where = "adverts." + pair.Key; var a = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(a.title, a.notes, where);
            Words(a.text, MaxAdvert, where + ".text");
            Weight(a.weight, where); Requires(a.requires, where); Key(a.thread, where + ".thread"); Key(a.place, where + ".place");
        }
        foreach (var pair in pack.arcs) Arc(pair.Key, pair.Value);
        foreach (var pair in pack.chatter)
        {
            string where = "chatter." + pair.Key; var c = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(c.title, c.notes, where);
            if (!StoryMoments.Interactions.ContainsKey(c.moment ?? "")) throw new ArgumentException(Text.Get("StorySchema.moment", where, string.Join(", ", StoryMoments.Interactions.Keys)));
            Words(c.line, MaxLine, where + ".line");
            if (!Speakers.Contains(c.speakers ?? "")) throw new ArgumentException(Text.Get("StorySchema.speakers", where, string.Join(", ", Speakers)));
            Weight(c.weight, where); Requires(c.requires, where); Key(c.thread, where + ".thread"); Key(c.place, where + ".place");
            if (c.speakerFactions == null || c.speakerFactions.Count > MaxSpeakerFactions) throw new ArgumentException(Text.Get("StorySchema.list_long", where + ".speakerFactions", MaxSpeakerFactions));
            foreach (var f in c.speakerFactions) if (f == null || !GameName.IsMatch(f)) throw new ArgumentException(Text.Get("StorySchema.game_name", where + ".speakerFactions", f ?? ""));
        }
        foreach (var pair in pack.tips)
        {
            string where = "tips." + pair.Key; var t = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(t.title, t.notes, where);
            Lore(t.text, MaxTip, where + ".text");
            Weight(t.weight, where); ModsOnly(t.requires, where); Key(t.thread, where + ".thread");
        }
        foreach (var pair in pack.sections)
        {
            string where = "sections." + pair.Key; var s = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, s.notes, where);
            Lore(s.label, MaxLabel, where + ".label"); Lore(s.title, MaxObjectiveTitle, where + ".title");
            if (s.body != null) Lore(s.body, MaxArticle, where + ".body");
            Image(s.image, where);
            ModsOnly(s.requires, where); Key(s.thread, where + ".thread");
        }
        foreach (var pair in pack.articles)
        {
            string where = "articles." + pair.Key; var a = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, a.notes, where);
            if (!IsId(a.section)) throw new ArgumentException(Text.Get("StorySchema.id", where + ".section", MaxIdLength));
            Lore(a.label, MaxLabel, where + ".label"); Lore(a.title, MaxObjectiveTitle, where + ".title"); Lore(a.body, MaxArticle, where + ".body");
            Image(a.image, where);
            ModsOnly(a.requires, where); Key(a.thread, where + ".thread");
        }
        foreach (var pair in pack.files)
        {
            string where = "files." + pair.Key; var f = pair.Value ?? throw new ArgumentException(Text.Get("StorySchema.empty", where));
            EntryId(pair.Key, where); Author(null, f.notes, where);
            if (string.IsNullOrEmpty(f.name) || f.name.Length > MaxFileName || !FileName.IsMatch(f.name)) throw new ArgumentException(Text.Get("StorySchema.file_name", where, MaxFileName));
            Words(f.text, MaxFileText, where + ".text");
            Key(f.startsArc, where + ".startsArc"); Key(f.thread, where + ".thread"); Key(f.place, where + ".place"); Key(f.person, where + ".person");
        }
    }

    /// <summary>An optional reference to another entry, by id.</summary>
    private static void Key(string? id, string where)
    {
        if (id != null && !IsId(id)) throw new ArgumentException(Text.Get("StorySchema.id", where, MaxIdLength));
    }
    /// <summary>A short plain label, such as a name or a role.</summary>
    private static void Short(string? text, int max, string where, bool required)
    {
        if (text == null) { if (required) throw new ArgumentException(Text.Get("StorySchema.text_blank", where)); return; }
        if (string.IsNullOrWhiteSpace(text) || text.Length > max || Plain(text) != null) throw new ArgumentException(Text.Get("StorySchema.text_long", where, max, text.Length));
    }

    private static void Image(string? image, string where)
    {
        if (image != null && (image.Length > MaxImage || !ImagePath.IsMatch(image))) throw new ArgumentException(Text.Get("StorySchema.image", where, MaxImage));
    }

    /// <summary>Text shown with no player at hand (tips, the encyclopedia): plain, and no placeholders.</summary>
    private static void Lore(string? text, int max, string where)
    {
        Words(text, max, where);
        if (text!.IndexOf('[') >= 0) throw new ArgumentException(Text.Get("StorySchema.no_placeholders", where));
    }

    /// <summary>Tips and the encyclopedia exist before any player does, so only installed mods can be required.</summary>
    private static void ModsOnly(StoryRequires? r, string where)
    {
        if (r == null) return;
        Requires(r, where);
        if (r.playerConditions.Count + r.forbidConditions.Count + r.owns.Count + r.dockedAt.Count + r.arcsDone.Count + r.arcsNotStarted.Count + r.filesRead.Count +
            r.flags.Count + r.notFlags.Count + r.arcsActive.Count + r.arcsAtStep.Count + r.places.Count + r.regions.Count + r.newsSeen.Count +
            r.standing.Count + r.crewWith.Count + r.running.Count + r.months.Count > 0 ||
            r.afterDays != null || r.beforeDays != null || r.crewCount != null || r.hours != null)
            throw new ArgumentException(Text.Get("StorySchema.mods_only", where));
    }

    private static void Arc(string id, StoryArc? arc)
    {
        string where = "arcs." + id;
        if (arc == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        EntryId(id, where);
        if (string.IsNullOrWhiteSpace(arc.title) || arc.title.Length > MaxTitle) throw new ArgumentException(Text.Get("StorySchema.title", where, MaxTitle));
        Author(null, arc.notes, where);
        Range(arc.chance, 0, 1, where + ".chance");
        Requires(arc.requires, where); Key(arc.thread, where + ".thread"); Key(arc.place, where + ".place");
        // A dock-at test may leave its station out only when the arc has a place of its own (or a thread that may give one).
        bool placed = arc.place != null || arc.thread != null;
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
                if (step.objective.person != null && !IsId(step.objective.person)) throw new ArgumentException(Text.Get("StorySchema.id", at + ".objective.person", MaxIdLength));
            }
            Tests(step.tests, at, placed);
            Outcome(step.onComplete, at);
            if (step.branches != null)
            {
                if (step.branches.Count == 0 || step.branches.Count > MaxBranches) throw new ArgumentException(Text.Get("StorySchema.branches", at, MaxBranches));
                for (int b = 0; b < step.branches.Count; b++)
                {
                    var branch = step.branches[b] ?? throw new ArgumentException(Text.Get("StorySchema.empty", at + ".branches." + b));
                    string bat = at + ".branches." + b;
                    Tests(branch.tests, bat, placed);
                    Outcome(branch.onComplete, bat);
                    if (branch.notes != null && branch.notes.Length > 2000) throw new ArgumentException(Text.Get("StorySchema.notes", bat));
                }
            }
        }
        // Where each step goes next must be a step of the same arc, or the end.
        foreach (var step in arc.steps)
        {
            string at = where + ".steps." + step.id;
            if (step.next != null && step.next != End && !ids.Contains(step.next)) throw new ArgumentException(Text.Get("StorySchema.next", at, step.next));
            foreach (var branch in step.branches ?? new List<StoryBranch>())
                if (branch.next != End && !ids.Contains(branch.next ?? "")) throw new ArgumentException(Text.Get("StorySchema.next", at + ".branches", branch.next ?? ""));
        }
    }

    private static void Tests(List<StoryTest>? tests, string at, bool placed)
    {
        if (tests == null || tests.Count == 0 || tests.Count > MaxTests) throw new ArgumentException(Text.Get("StorySchema.tests", at, MaxTests));
        foreach (var test in tests) Test(test, at + ".tests", placed);
    }

    private static void Outcome(StoryOutcome? outcome, string at)
    {
        if (outcome == null) return;
        Message(outcome.message, at + ".onComplete.message");
        if (outcome.items == null || outcome.items.Count > MaxRewards) throw new ArgumentException(Text.Get("StorySchema.rewards", at, MaxRewards));
        foreach (var reward in outcome.items)
        {
            if (reward == null || !GameName.IsMatch(reward.item ?? "")) throw new ArgumentException(Text.Get("StorySchema.game_name", at + ".onComplete.items", reward?.item ?? ""));
            if (reward.count < 1 || reward.count > MaxRewardCount) throw new ArgumentException(Text.Get("StorySchema.range", at + ".onComplete.items." + reward.item, 1, MaxRewardCount));
        }
        if (outcome.credits < 0 || outcome.credits > MaxCreditReward) throw new ArgumentException(Text.Get("StorySchema.range", at + ".onComplete.credits", 0, MaxCreditReward));
        if (outcome.files == null || outcome.files.Count > MaxFiles || outcome.files.Any(f => !IsId(f)) || outcome.files.Distinct().Count() != outcome.files.Count)
            throw new ArgumentException(Text.Get("StorySchema.files", at + ".onComplete.files", MaxFiles));
        foreach (var (flags, name) in new[] { (outcome.setFlags, "setFlags"), (outcome.clearFlags, "clearFlags") })
            if (flags == null || flags.Count > MaxFlags || flags.Any(f => !IsId(f)) || flags.Distinct().Count() != flags.Count)
                throw new ArgumentException(Text.Get("StorySchema.flags", at + ".onComplete." + name, MaxFlags));
        if (outcome.setFlags.Intersect(outcome.clearFlags).Any()) throw new ArgumentException(Text.Get("StorySchema.flags", at + ".onComplete.clearFlags", MaxFlags));
        if (outcome.standing == null || outcome.standing.Count > MaxStandingChanges || outcome.standing.Select(s => s?.faction).Distinct().Count() != outcome.standing.Count)
            throw new ArgumentException(Text.Get("StorySchema.standing_change", at + ".onComplete.standing", MaxStandingChanges, MaxStandingChange));
        foreach (var s in outcome.standing)
            if (s == null || !GameName.IsMatch(s.faction ?? "") || double.IsNaN(s.change) || s.change == 0 || Math.Abs(s.change) > MaxStandingChange)
                throw new ArgumentException(Text.Get("StorySchema.standing_change", at + ".onComplete.standing", MaxStandingChanges, MaxStandingChange));
    }

    private static void Test(StoryTest? test, string where, bool placed = false)
    {
        if (test == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        where += "." + test.kind;
        bool station = test.station != null, item = test.item != null, hours = test.hours != 0, amount = test.amount != 0, condition = test.condition != null;
        if ((amount && test.kind != Credits) || (condition && test.kind != Condition))
            throw new ArgumentException(Text.Get("StorySchema.test_fields", where, Fields(test.kind)));
        switch (test.kind)
        {
            case Credits:
                if (station || item || hours || test.count != 1) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, Fields(Credits)));
                if (!(test.amount > 0) || test.amount > MaxCreditTest) throw new ArgumentException(Text.Get("StorySchema.range", where + ".amount", 1, MaxCreditTest));
                break;
            case Condition:
                if (station || item || hours || test.consume || test.count != 1 || !condition) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, Fields(Condition)));
                if (!GameName.IsMatch(test.condition!)) throw new ArgumentException(Text.Get("StorySchema.game_name", where, test.condition!));
                break;
            case DockAt:
                if (!(station || placed) || item || hours || test.consume || test.count != 1) throw new ArgumentException(Text.Get("StorySchema.test_fields", where, "station"));
                if (station && !StationId(test.station!)) throw new ArgumentException(Text.Get("StorySchema.station", where, test.station!));
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

    private static string Fields(string kind) => kind switch
    {
        DockAt => "station", HaveItem => "item, count, consume", Install => "item, count", Wait => "hours",
        Credits => "amount, consume", Condition => "condition", _ => string.Join(", ", TestKinds)
    };

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
        List(r.filesRead, where + ".filesRead", s => IsId(s));
        List(r.flags, where + ".flags", s => IsId(s)); List(r.notFlags, where + ".notFlags", s => IsId(s));
        List(r.arcsActive, where + ".arcsActive", s => IsId(s));
        List(r.arcsAtStep, where + ".arcsAtStep", IsArcStep);
        List(r.places, where + ".places", s => IsId(s)); List(r.regions, where + ".regions", s => IsId(s));
        List(r.newsSeen, where + ".newsSeen", s => IsId(s));
        List(r.crewWith, where + ".crewWith", GameName.IsMatch); List(r.running, where + ".running", GameName.IsMatch);
        if (r.standing == null || r.standing.Count > MaxListEntries) throw new ArgumentException(Text.Get("StorySchema.list_long", where + ".standing", MaxListEntries));
        foreach (var s in r.standing)
        {
            if (s == null || !GameName.IsMatch(s.faction ?? "")) throw new ArgumentException(Text.Get("StorySchema.game_name", where + ".standing", s?.faction ?? ""));
            if (s.atLeast == null && s.atMost == null || s.atLeast != null && !Tiers.Contains(s.atLeast) || s.atMost != null && !Tiers.Contains(s.atMost) ||
                s.atLeast != null && s.atMost != null && Array.IndexOf(Tiers, s.atLeast) > Array.IndexOf(Tiers, s.atMost))
                throw new ArgumentException(Text.Get("StorySchema.standing", where + ".standing." + s.faction, string.Join(", ", Tiers)));
        }
        if (r.crewCount != null && (r.crewCount.atLeast < 0 || r.crewCount.atLeast > MaxCrewCount || r.crewCount.atMost is int most && (most < r.crewCount.atLeast || most > MaxCrewCount)))
            throw new ArgumentException(Text.Get("StorySchema.count", where + ".crewCount", MaxCrewCount));
        if (r.months == null || r.months.Count > 12 || r.months.Any(m => m < 1 || m > 12) || r.months.Distinct().Count() != r.months.Count) throw new ArgumentException(Text.Get("StorySchema.months", where + ".months"));
        if (r.hours != null && (r.hours.from < 0 || r.hours.from > 23 || r.hours.to < 0 || r.hours.to > 23)) throw new ArgumentException(Text.Get("StorySchema.hours", where + ".hours"));
        if (r.afterDays is double after) Range(after, 0, MaxDays, where + ".afterDays");
        if (r.beforeDays is double before) Range(before, 0, MaxDays, where + ".beforeDays");
        if (r.afterDays is double a && r.beforeDays is double b && !(a < b)) throw new ArgumentException(Text.Get("StorySchema.days", where));
    }

    private static void List(List<string>? values, string where, Func<string, bool> valid)
    {
        if (values == null) throw new ArgumentException(Text.Get("StorySchema.empty", where));
        if (values.Count > MaxListEntries) throw new ArgumentException(Text.Get("StorySchema.list_long", where, MaxListEntries));
        foreach (var value in values) if (value == null || !valid(value)) throw new ArgumentException(Text.Get("StorySchema.game_name", where, value ?? ""));
    }

    public static bool StationId(string value) => value == DockedAnywhere || value.Length <= 32 && Station.IsMatch(value);
    /// <summary><c>arc.step</c>, as a goal test names them.</summary>
    public static bool IsArcStep(string? value) => TryArcStep(value, out _, out _);
    public static bool TryArcStep(string? value, out string arc, out string step)
    {
        arc = step = "";
        if (value == null) return false;
        int dot = value.IndexOf('.');
        if (dot <= 0 || dot == value.Length - 1 || value.IndexOf('.', dot + 1) >= 0) return false;
        arc = value.Substring(0, dot); step = value.Substring(dot + 1);
        return IsId(arc) && IsId(step, MaxStepIdLength);
    }

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
        // A sender: free text, or a person from the people table (Framework 0.114.0), or both (the text is shown).
        if (message.from != null && (string.IsNullOrWhiteSpace(message.from) || message.from.Length > MaxFrom || Plain(message.from) != null)) throw new ArgumentException(Text.Get("StorySchema.from", where, MaxFrom));
        Key(message.person, where + ".person");
        if (message.from == null && message.person == null) throw new ArgumentException(Text.Get("StorySchema.from", where, MaxFrom));
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
        string rest = PersonToken.Replace(text, "");
        foreach (var token in Placeholders) rest = rest.Replace(token, "");
        int open = rest.IndexOf('['), close = rest.IndexOf(']');
        if (open < 0 && close < 0) return null;
        if (open >= 0) { int end = rest.IndexOf(']', open); return end > open ? rest.Substring(open, end - open + 1) : "["; }
        return "]";
    }

    /// <summary>Fills the placeholders. Missing values read as a plain description, never as the token.</summary>
    public static string Fill(string text, string player, string firstName, string ship) =>
        Fill(text, name => name switch { "player" => player, "player-first" => firstName, "ship" => ship, _ => null });

    /// <summary>Fills every placeholder from <paramref name="value"/>, by the token's name (<c>player</c>, <c>place</c>,
    /// <c>person:key</c> and so on); a token with no value is left as it is.</summary>
    public static string Fill(string text, Func<string, string?> value)
    {
        if (text.IndexOf('[') < 0) return text;
        return Token.Replace(text, m => value(m.Groups[1].Value) ?? m.Value);
    }
    /// <summary>The person keys a text names through <c>[person:key]</c>.</summary>
    public static IEnumerable<string> People(string? text)
    {
        if (text == null || text.IndexOf("[person:", StringComparison.Ordinal) < 0) yield break;
        foreach (Match m in PersonToken.Matches(text)) yield return m.Value.Substring(8, m.Value.Length - 9);
    }
}
