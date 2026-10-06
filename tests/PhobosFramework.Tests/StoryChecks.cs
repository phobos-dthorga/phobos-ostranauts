using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Story;

/// <summary>Story packs (Framework 0.107.0): the schema, the merged library, the player's record and the rules.</summary>
internal static class StoryChecks
{
    private const string Pack = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""broadcasts"": {
        ""greens-report"": { ""title"": ""Greens"", ""region"": ""Outer System"", ""text"": ""Crews report fresh lettuce aboard the [ship]."", ""weight"": 2 },
        ""seed-call"": { ""region"": ""Shipping & Inner System"", ""text"": ""Seed wheat wanted.\nAsk at any station."", ""once"": true }
      },
      ""adverts"": { ""hearth-advert"": { ""text"": ""Hearth-2: a hot meal, [player-first]."", ""requires"": { ""owns"": [ ""Cooker"" ] } } },
      ""arcs"": {
        ""first-harvest"": {
          ""title"": ""First harvest"", ""chance"": 0.1,
          ""requires"": { ""mods"": [ ""PhobosAgriculture"" ], ""arcsNotStarted"": [ ""second-harvest"" ] },
          ""steps"": [
            { ""id"": ""call"", ""delivery"": { ""message"": { ""from"": ""Verdemorrow"", ""text"": ""Hello [player]."" }, ""bulletin"": ""seed-call"" },
              ""objective"": { ""title"": ""Dock at a station"", ""description"": ""Any one will do."" },
              ""tests"": [ { ""kind"": ""dock-at"", ""station"": ""any"" } ] },
            { ""id"": ""deliver"", ""objective"": { ""title"": ""Bring seed wheat"" },
              ""tests"": [ { ""kind"": ""have-item"", ""item"": ""Seed"", ""count"": 3, ""consume"": true }, { ""kind"": ""dock-at"", ""station"": ""VORB"" } ],
              ""onComplete"": { ""message"": { ""from"": ""Verdemorrow"", ""text"": ""Thanks."" }, ""items"": [ { ""item"": ""Packet"", ""count"": 2 } ] } },
            { ""id"": ""rest"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 2 } ] }
          ]
        },
        ""second-harvest"": { ""title"": ""Second"", ""repeatable"": true, ""requires"": { ""arcsDone"": [ ""first-harvest"" ] },
          ""steps"": [ { ""id"": ""grow"", ""objective"": { ""title"": ""Install a rack"" }, ""tests"": [ { ""kind"": ""install"", ""item"": ""Rack"", ""count"": 2 } ] } ] }
      }
    }";

    private sealed class Facts : IStoryFacts
    {
        public HashSet<string> Mods = new(), Conditions = new(), Docked = new(), At = new();
        public Dictionary<string, int> Ship = new(), Hands = new();
        public double Epoch { get; set; }
        public double Credits { get; set; }
        public string? Region { get; set; }
        public bool Near(string place) => At.Contains(place) || Region == place;
        public Dictionary<string, double> Standings = new();
        public HashSet<string> CrewConditions = new(), RunningMachines = new();
        public int CrewCount { get; set; }
        public int Month { get; set; } = 1;
        public int Hour { get; set; }
        public double? Standing(string faction) => Standings.TryGetValue(faction, out var s) ? s : null;
        public bool CrewWith(string condition) => CrewConditions.Contains(condition);
        public int Running(string item) => RunningMachines.Contains(item) ? 1 : 0;
        public bool ModInstalled(string mod) => Mods.Contains(mod);
        public bool PlayerHas(string condition) => Conditions.Contains(condition);
        public int Installed(string item) => Ship.TryGetValue(item, out int n) ? n : 0;
        public int Carried(string item) => Hands.TryGetValue(item, out int n) ? n : 0;
        public bool DockedAt(string station) => station == StorySchema.DockedAnywhere ? Docked.Count > 0 : Docked.Contains(station);
    }

    internal static void Run(Action<bool, string> check)
    {
        void Refused(string json, string message, bool framework = false)
        {
            bool refused = false;
            try { DataPacks.LoadText<StoryPack>(json, "", "test", StorySchema.Name, p => StorySchema.Validate(p, framework)); }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException) { refused = true; }
            check(refused, message);
        }
        StoryPack Load(string json, bool framework = false) => DataPacks.LoadText<StoryPack>(json, "", "test", StorySchema.Name, p => StorySchema.Validate(p, framework));
        string With(string find, string replace) { check(Pack.Contains(find), "Test fixture has " + find); return Pack.Replace(find, replace); }

        var pack = Load(Pack);
        check(pack.broadcasts.Count == 2 && pack.adverts.Count == 1 && pack.arcs["first-harvest"].steps.Count == 3, "A full story pack loads");
        check(pack.arcs["first-harvest"].steps[1].tests[0].consume && pack.arcs["first-harvest"].steps[2].tests[0].hours == 2, "Tests keep their fields");

        // The schema refuses what the game could not show or the runner could not check.
        Refused(With(@"""weight"": 2", @"""weight"": 2, ""colour"": ""red"""), "An unknown field is refused");
        Refused(With("greens-report", "Greens_Report"), "Ids are lower-case and hyphenated");
        Refused(With("[ship]", "[captain]"), "Only the placeholders may be bracketed");
        Refused(With("Hello [player].", "Hello <b>[player]</b>."), "Markup is refused");
        Refused(With(@"""region"": ""Outer System"", ", ""), "A broadcast needs a region");
        Refused(With(@"""station"": ""any""", @"""station"": ""any"", ""item"": ""Seed"""), "A dock-at test takes only a station");
        Refused(With(@"""kind"": ""wait"", ""hours"": 2", @"""kind"": ""wait"", ""hours"": 0"), "A wait needs some hours");
        Refused(With(@"""kind"": ""wait""", @"""kind"": ""sleep"""), "Unknown test kinds are refused");
        Refused(With(@"""id"": ""rest""", @"""id"": ""call"""), "A step id is used once per arc");
        Refused(With(@"""tests"": [ { ""kind"": ""wait"", ""hours"": 2 } ]", @"""tests"": []"), "A step needs a test");
        Refused(With(@"""chance"": 0.1", @"""chance"": 1.5"), "Chance is from 0 to 1");
        Refused(With(@"""mods"": [ ""PhobosAgriculture"" ]", @"""mods"": [ ""Agri culture"" ]"), "A mod is a Phobos folder name or a plugin id");
        Refused(With("Crews report fresh lettuce aboard the [ship].", new string('x', StorySchema.MaxBroadcast + 1)), "Broadcast text has a length limit");
        Refused(Pack.Replace(@"""schema"": ""story"",", @"""schema"": ""story"", ""settings"": { ""checkSeconds"": 30 },"), "Only Framework's pack holds settings");
        var settings = Load(Pack.Replace(@"""schema"": ""story"",", @"""schema"": ""story"", ""settings"": { ""checkSeconds"": 60, ""maxActiveArcs"": 1 },"), framework: true);
        check(settings.settings!.checkSeconds == 60 && settings.settings.broadcastShare == 0.3, "Framework's pack sets what it names and keeps the other defaults");
        Refused(Pack.Replace(@"""schema"": ""story"",", @"""schema"": ""story"", ""settings"": { ""broadcastShare"": 2 },"), "Shares are from 0 to 1", framework: true);
        check(StorySchema.Plain("Line one\nline two [player]") == null && StorySchema.Plain("a ] b") == "]" && StorySchema.Plain("[x]") == "[x]", "Plain text allows line breaks and placeholders only");
        check(StorySchema.Fill("[player-first] of [ship], [player]", "Ada Kerr", "Ada", "Tern") == "Ada of Tern, Ada Kerr", "Placeholders fill");

        // The merged library: first id wins, unknown game names and references leave an entry out.
        var other = Load(@"{ ""schemaVersion"": 1, ""schema"": ""story"", ""broadcasts"": { ""greens-report"": { ""region"": ""Tharsis"", ""text"": ""Second copy."" },
            ""rack-news"": { ""region"": ""Tharsis"", ""text"": ""Racks."", ""requires"": { ""owns"": [ ""NoSuchRack"" ] } },
            ""late-news"": { ""region"": ""Tharsis"", ""text"": ""Late."", ""requires"": { ""arcsDone"": [ ""no-such-arc"" ] } },
            ""manufacturing-news"": { ""region"": ""Tharsis"", ""text"": ""Stills."", ""requires"": { ""mods"": [ ""PhobosManufacturing"" ], ""owns"": [ ""Still"" ] } } } }");
        var items = new HashSet<string> { "Seed", "Packet", "Rack", "Cooker" };
        var library = StoryLibrary.Build(new[] { ("agriculture", pack), ("player", other) }, null, m => m == "PhobosAgriculture", items.Contains, _ => true);
        check(library.Broadcasts["greens-report"].Owner == "agriculture" && library.Broadcasts["greens-report"].Value.text.StartsWith("Crews"), "The first pack keeps a shared id");
        check(!library.Broadcasts.ContainsKey("rack-news") && !library.Broadcasts.ContainsKey("late-news"), "Unknown items and arcs leave an entry out");
        check(library.Broadcasts.ContainsKey("manufacturing-news"), "An entry for a mod that is not installed is kept unchecked (it never shows)");
        check(library.Problems.Count == 3 && library.Arcs.Count == 2 && library.Settings.maxActiveArcs == 2, "Three problems are listed and both arcs load");
        var noSeed = StoryLibrary.Build(new[] { ("agriculture", pack) }, null, _ => true, i => i != "Seed", _ => true);
        check(!noSeed.Arcs.ContainsKey("first-harvest") && !noSeed.Arcs.ContainsKey("second-harvest"), "An arc naming a missing item is left out, and so is the arc that needs it finished");
        var noBulletin = pack.broadcasts["seed-call"]; pack.broadcasts.Remove("seed-call");
        check(!StoryLibrary.Build(new[] { ("a", pack) }, null, _ => true, null, null).Arcs.ContainsKey("first-harvest"), "A bulletin must name a loaded broadcast");
        pack.broadcasts["seed-call"] = noBulletin;

        // The record: a round trip that keeps what it does not know.
        var record = new StoryRecord();
        record.Arcs["first-harvest"] = new ArcProgress { State = ArcState.Active, Step = 1, StepId = "deliver", StepStart = 12345.25, Completions = 0 };
        record.Arcs["second-harvest"] = new ArcProgress { State = ArcState.Done, StepId = "grow", Completions = 3 };
        record.Seen.Add("seed-call"); record.Enqueue("greens-report"); record.Enqueue("greens-report");
        var fields = record.Encode();
        fields["future.field"] = "kept"; fields["arc.broken"] = "sideways|x";
        check(fields.All(f => f.Key.Length <= 100 && Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(f.Value)), "Every field fits a Phobos record");
        var back = StoryRecord.Decode(fields);
        check(back.Arcs["first-harvest"].StepId == "deliver" && back.Arcs["first-harvest"].StepStart == 12345.25 && back.Arcs["second-harvest"].Completions == 3, "Arc progress round-trips");
        check(back.Seen.Contains("seed-call") && back.Queue.SequenceEqual(new[] { "greens-report" }), "Seen news and the queue round-trip; a bulletin is queued once");
        var again = back.Encode();
        check(again["future.field"] == "kept" && again["arc.broken"] == "sideways|x", "Unknown and unreadable fields are kept as they were");
        for (int i = 0; i < 12; i++) back.Enqueue("news-" + i);
        check(back.Queue.Count == StoryRecord.MaxQueue && back.Queue.Last() == "news-11", "The queue keeps the newest bulletins");

        // Eligibility and tests.
        var facts = new Facts { Epoch = 12345.25 + 3600 };
        var arc = pack.arcs["first-harvest"];
        var fresh = new StoryRecord();
        check(StoryRules.Blocked(arc.requires, facts, fresh) != null, "Without its mod an arc is not available");
        facts.Mods.Add("PhobosAgriculture");
        check(StoryRules.Blocked(arc.requires, facts, fresh) == null, "With its mod and before the second arc it is");
        fresh.Arcs["second-harvest"] = new ArcProgress();
        check(StoryRules.Blocked(arc.requires, facts, fresh) != null, "arcsNotStarted closes it once the other arc started");
        check(StoryRules.Blocked(pack.arcs["second-harvest"].requires, facts, record) != null, "arcsDone needs the first arc finished");
        check(StoryRules.Blocked(pack.adverts["hearth-advert"].requires, facts, fresh) != null, "An advert that needs an owned cooker waits for one");
        facts.Ship["Cooker"] = 1;
        check(StoryRules.Blocked(pack.adverts["hearth-advert"].requires, facts, fresh) == null, "and shows once one is aboard");
        var deliver = arc.steps[1];
        check(!StoryRules.Passed(deliver, facts, 0), "Delivery needs the seed and the station");
        facts.Hands["Seed"] = 3; facts.Docked.Add("VORB");
        check(StoryRules.Passed(deliver, facts, 0) && StoryRules.Passed(arc.steps[0], facts, 0), "Three seed while docked at VORB pass, and so does any station");
        check(!StoryRules.Passed(arc.steps[2], facts, facts.Epoch - 3600) && StoryRules.Passed(arc.steps[2], facts, facts.Epoch - 7200), "A wait counts game hours since the step began");
        facts.Ship["Rack"] = 1;
        check(!StoryRules.Passed(pack.arcs["second-harvest"].steps[0], facts, 0), "An install test counts what is aboard");

        // Picks, steps and goal names.
        var pool = new List<(string, int)> { ("a", 1), ("b", 3) };
        check(StoryRules.Pick(pool, 0) == "a" && StoryRules.Pick(pool, 0.24) == "a" && StoryRules.Pick(pool, 0.26) == "b" && StoryRules.Pick(pool, 1) == "b", "Weighted picks fall by weight");
        check(StoryRules.Pick(new List<(string, int)>(), 0.5) == null, "An empty pool picks nothing");
        check(StoryRules.Resolve(arc, new ArcProgress { Step = 0, StepId = "deliver" }) == 1, "A saved step is found by id first");
        check(StoryRules.Resolve(arc, new ArcProgress { Step = 2, StepId = "renamed" }) == 2 && StoryRules.Resolve(arc, new ArcProgress { Step = 9, StepId = "gone" }) == -1, "then by position, else not at all");
        check(StoryRules.GoalTest("first-harvest", "call") == "PhobosStory.first-harvest.call", "Goal tests are named after the arc and step");
        check(StoryRules.TryGoal("PhobosStory.first-harvest.call", out var a, out var s) && a == "first-harvest" && s == "call", "and read back");
        check(!StoryRules.TryGoal("PhobosStory.first.harvest.call", out _, out _) && !StoryRules.TryGoal("TIsSleeping", out _, out _), "Other names are not story goals");
        check(StoryContent.PluginId("PhobosAutoNav") == "phobosgekko.ostranauts.autonav" && StoryContent.PluginId("other.mod.id") == "other.mod.id", "Mods name their plugin id");

        Phase2(check, (json, message, framework) => Refused(json, message, framework), (json, framework) => Load(json, framework));
        Round3(check, (json, message) => Refused(json, message, false), json => Load(json, false));
        Round4(check, (json, message) => Refused(json, message, false), json => Load(json, false));
        Round5(check, (json, message, framework) => Refused(json, message, framework), (json, framework) => Load(json, framework));
        Round6(check, (json, message) => Refused(json, message, false), json => Load(json, false));
        Round7(check, (json, message) => Refused(json, message, false), json => Load(json, false));
        Round8(check, (json, message) => Refused(json, message, false), json => Load(json, false));
    }

    private const string Replies = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""arcs"": { ""offer"": { ""title"": ""An offer"", ""steps"": [
        { ""id"": ""letter"", ""delivery"": { ""message"": { ""from"": ""Dara"", ""text"": ""Will you carry it?"" } }, ""objective"": { ""title"": ""Answer Dara"" },
          ""choices"": [
            { ""id"": ""accept"", ""label"": ""I'll carry it."", ""tests"": [ { ""kind"": ""credits"", ""amount"": 100 } ], ""onComplete"": { ""message"": { ""from"": ""Dara"", ""text"": ""Thank you."" }, ""setFlags"": [ ""offer-accepted"" ] }, ""next"": ""carry"" },
            { ""id"": ""refuse"", ""label"": ""Not this time."", ""next"": ""end"" } ] },
        { ""id"": ""carry"", ""objective"": { ""title"": ""Carry it"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ], ""onComplete"": { ""message"": { ""from"": ""Dara"", ""text"": ""Arrived."" } } } ] } }
    }";

    /// <summary>Framework 0.122.0: replies the player chooses in the Letters window, and the letters kept for it.</summary>
    private static void Round8(Action<bool, string> check, Action<string, string> refused, Func<string, StoryPack> load)
    {
        string With(string find, string replace) { check(Replies.Contains(find), "Fixture has " + find); return Replies.Replace(find, replace); }
        var pack = load(Replies);
        var arc = pack.arcs["offer"];
        var letter = arc.steps[0];
        check(letter.choices!.Count == 2 && letter.tests.Count == 0 && letter.choices[0].next == "carry" && letter.choices[1].tests.Count == 0, "A step with two replies loads");
        refused(With("{ \"id\": \"refuse\", \"label\": \"Not this time.\", \"next\": \"end\" } ]", "]"), "A step offers at least two replies");
        refused(With("\"choices\": [", "\"tests\": [ { \"kind\": \"wait\", \"hours\": 1 } ], \"choices\": ["), "A step with replies has no tests of its own");
        refused(With("\"id\": \"refuse\"", "\"id\": \"accept\""), "A reply id is used once in its step");
        refused(With("\"next\": \"carry\" },", "\"next\": \"elsewhere\" },"), "A reply leads to a step of the same arc or the end");
        refused(With("\"label\": \"Not this time.\", ", ""), "A reply has a label");
        refused(With("Not this time.", "Not <b>this</b> time."), "A reply's label is plain text");
        refused(With("\"label\": \"Not this time.\"", "\"label\": \"" + new string('x', StorySchema.MaxChoiceLabel + 1) + "\""), "A reply's label has a length limit");

        var facts = new Facts { Epoch = 7200 };
        check(StoryRules.Outcome(letter, facts, 0) == null, "A step waiting for a reply never finishes by itself");
        check(StoryRules.ChoiceBlocked(letter.choices[0], facts, 0)?.kind == StorySchema.Credits && StoryRules.ChoiceBlocked(letter.choices[1], facts, 0) == null,
            "A reply with a test is locked until it passes; one without is open");
        facts.Credits = 150;
        check(StoryRules.ChoiceBlocked(letter.choices[0], facts, 0) == null, "Holding the credits unlocks it");

        // Letters: kept as they arrive, or reconstructed for an arc begun before letters were kept.
        var progress = new ArcProgress { State = ArcState.Active, Step = 1, StepId = "carry" };
        var rebuilt = StoryRules.Letters(arc, progress, null);
        check(rebuilt.Count == 1 && rebuilt[0].Step == "letter" && rebuilt[0].Kind == StoryLetter.Opening && rebuilt[0].Epoch == null,
            "An older arc shows the letters its progress implies, without dates (the reply itself is not known)");
        progress.State = ArcState.Done;
        check(StoryRules.Letters(arc, progress, null).Select(l => l.Step + l.Kind).SequenceEqual(new[] { "letterd", "carryc" }), "A finished arc shows every step's letters");
        var record = new StoryRecord();
        record.AddLetter("offer", new StoryLetter("letter", StoryLetter.Opening, null, 10));
        record.AddLetter("offer", new StoryLetter("letter", StoryLetter.Reply, "accept", 20.5));
        check(StoryRules.Letters(arc, progress, record.Letters["offer"]).Count == 2, "Kept letters are shown as kept");
        var fields = record.Encode();
        fields["letters.broken"] = "letter,x,,5";
        var back = StoryRecord.Decode(fields);
        check(back.Letters["offer"].Count == 2 && back.Letters["offer"][1].Choice == "accept" && back.Letters["offer"][1].Epoch == 20.5 && back.Letters["offer"][0].Choice == null,
            "Letters and replies survive a save");
        check(!back.Letters.ContainsKey("broken") && back.Encode()["letters.broken"] == "letter,x,,5", "A letters entry that is not ours is kept as written, never used");
        for (int i = 0; i < StoryRecord.MaxLetters + 5; i++) record.AddLetter("long", new StoryLetter("letter", StoryLetter.Opening, null, i));
        check(record.Letters["long"].Count == StoryRecord.MaxLetters && record.Letters["long"][0].Epoch == 5, "An arc keeps its newest letters, the oldest going first");
        check(StoryLetter.IsKind("b0") && StoryLetter.IsKind("a") && !StoryLetter.IsKind("bx") && !StoryLetter.IsKind("z"), "Only our letter kinds are read");
    }

    private const string Faces = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""people"": { ""orra-pell"": { ""name"": ""Orra Pell"", ""role"": ""broker"", ""home"": ""oklg"", ""face"": ""feminine"" }, ""kes-arven"": { ""name"": ""Kes Arven"", ""home"": ""oklg"" } },
      ""arcs"": { ""leaflets"": { ""title"": ""Leaflets"", ""steps"": [
        { ""id"": ""list"", ""delivery"": { ""message"": { ""person"": ""orra-pell"", ""text"": ""Four leaflets."" } }, ""objective"": { ""title"": ""Wait for the list"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 2 } ],
          ""onComplete"": { ""message"": { ""person"": ""kes-arven"", ""text"": ""Found it."" } } },
        { ""id"": ""quiet"", ""objective"": { ""title"": ""Wait again"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] },
        { ""id"": ""named"", ""objective"": { ""title"": ""Answer Orra"", ""person"": ""orra-pell"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] },
        { ""id"": ""free"", ""delivery"": { ""message"": { ""from"": ""The desk"", ""text"": ""Noted."" } }, ""objective"": { ""title"": ""Note it"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] } ] },
        ""alone"": { ""title"": ""Alone"", ""steps"": [ { ""id"": ""only"", ""objective"": { ""title"": ""Wait"" }, ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] } ] } }
    }";

    /// <summary>Framework 0.121.0: who a goal is from (its face and From line) and the faces kept in the record.</summary>
    private static void Round7(Action<bool, string> check, Action<string, string> refused, Func<string, StoryPack> load)
    {
        string With(string find, string replace) { check(Faces.Contains(find), "Fixture has " + find); return Faces.Replace(find, replace); }
        var pack = load(Faces);
        var arc = pack.arcs["leaflets"];
        check(pack.people["orra-pell"].face == "feminine" && pack.people["kes-arven"].face == null && arc.steps[2].objective!.person == "orra-pell", "A face look and a goal's person load");
        refused(With("\"face\": \"feminine\"", "\"face\": \"female\""), "A face look is masculine, feminine or any");
        refused(With("\"person\": \"orra-pell\" }, \"tests\"", "\"person\": \"Orra Pell\" }, \"tests\""), "A goal's person is a person id");

        check(StoryRules.GoalSender(arc, 0).Person == "orra-pell", "A goal is from the sender of its step's letter");
        check(StoryRules.GoalSender(arc, 1).Person == "kes-arven", "A goal with no letter is from the last sender before it, a completion's letter first");
        check(StoryRules.GoalSender(arc, 2).Person == "orra-pell", "A goal's own person comes first");
        var free = StoryRules.GoalSender(arc, 3);
        check(free.Person == null && free.From?.from == "The desk", "A sender given as free text comes back as text");
        check(StoryRules.GoalSender(pack.arcs["alone"], 0) == (null, null) && StoryRules.GoalSender(arc, 9) == (null, null), "A goal with no sender is from no one in particular");

        var looks = Phobos.Ostranauts.Framework.Social.PortraitRules.Looks;
        check(looks.SequenceEqual(new[] { "any", "masculine", "feminine" }) && Phobos.Ostranauts.Framework.Social.PortraitRules.Flags("masculine") == (true, false) &&
              Phobos.Ostranauts.Framework.Social.PortraitRules.Flags("feminine") == (false, true) && Phobos.Ostranauts.Framework.Social.PortraitRules.Flags(null) == (true, true),
            "A look picks the game's male, female or nonbinary face pool");
        string a = Phobos.Ostranauts.Framework.Social.PortraitRules.ImageName("pbaseA|pbaseB"), b = Phobos.Ostranauts.Framework.Social.PortraitRules.ImageName("pbaseA|pbaseC");
        check(a == Phobos.Ostranauts.Framework.Social.PortraitRules.ImageName("pbaseA|pbaseB") && a != b && a.StartsWith("PhobosFace") && a.IndexOf('.') < 0 && a.IndexOf('/') < 0,
            "A face's picture name is stable for its parts and has no path or extension");
        check(!Phobos.Ostranauts.Framework.Social.PortraitRules.ValidParts(new[] { "../x" }) && !Phobos.Ostranauts.Framework.Social.PortraitRules.ValidParts(Array.Empty<string>()) &&
              Phobos.Ostranauts.Framework.Social.PortraitRules.ValidParts(new[] { "pbaseHairA01", "pbaseFaceB02" }), "Saved face parts are plain portrait names");

        var record = new StoryRecord();
        record.Faces["orra-pell"] = new[] { "pbaseHairA01", "pbaseFaceB02" };
        var fields = record.Encode();
        fields["face.bad"] = "../../evil|x";
        var back = StoryRecord.Decode(fields);
        check(back.Faces["orra-pell"].SequenceEqual(new[] { "pbaseHairA01", "pbaseFaceB02" }), "A correspondent's face survives a save");
        check(!back.Faces.ContainsKey("bad") && back.Encode()["face.bad"] == "../../evil|x", "A face entry that is not plain parts is kept as written, never used");
    }

    private const string Standing = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""adverts"": { ""ayosec-ad"": { ""text"": ""AyoSec hiring."", ""requires"": { ""standing"": [ { ""faction"": ""OKLGLEO"", ""atLeast"": ""warm"" }, { ""faction"": ""OKLGCrim"", ""atMost"": ""neutral"" } ],
                     ""crewWith"": [ ""SkillBotany"" ], ""crewCount"": { ""atLeast"": 1, ""atMost"": 3 }, ""running"": [ ""Still"" ], ""months"": [ 12, 1 ], ""hours"": { ""from"": 22, ""to"": 5 } } } },
      ""chatter"": { ""cop-talk"": { ""moment"": ""complaint"", ""speakers"": ""others"", ""line"": ""Paperwork, [crew]."", ""speakerFactions"": [ ""OKLGLEO"" ] } },
      ""arcs"": { ""favour"": { ""title"": ""A favour"", ""steps"": [ { ""id"": ""do"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ],
                   ""onComplete"": { ""standing"": [ { ""faction"": ""OKLGCorp"", ""change"": 5 }, { ""faction"": ""OKLGCrim"", ""change"": -2.5 } ] } } ] } }
    }";

    /// <summary>Framework 0.115.0: standing, crew and clock gates, speaker factions and standing changes.</summary>
    private static void Round6(Action<bool, string> check, Action<string, string> refused, Func<string, StoryPack> load)
    {
        string With(string find, string replace) { check(Standing.Contains(find), "Fixture has " + find); return Standing.Replace(find, replace); }
        var pack = load(Standing);
        var r = pack.adverts["ayosec-ad"].requires!;
        check(r.standing.Count == 2 && r.crewCount!.atMost == 3 && r.hours!.from == 22 && pack.arcs["favour"].steps[0].onComplete!.standing[1].change == -2.5 && pack.chatter["cop-talk"].speakerFactions.SequenceEqual(new[] { "OKLGLEO" }),
            "Standing, crew and clock gates load");
        refused(With("\"atLeast\": \"warm\"", "\"atLeast\": \"liked\""), "A tier is one of the game's");
        refused(With("{ \"faction\": \"OKLGCrim\", \"atMost\": \"neutral\" }", "{ \"faction\": \"OKLGCrim\" }"), "A standing gate names a tier");
        refused(With("\"atLeast\": \"warm\"", "\"atLeast\": \"trusted\", \"atMost\": \"warm\""), "atLeast is not above atMost");
        refused(With("\"atLeast\": 1, \"atMost\": 3", "\"atLeast\": 4, \"atMost\": 3"), "A crew count's atMost is not below atLeast");
        refused(With("\"months\": [ 12, 1 ]", "\"months\": [ 13 ]"), "Months are 1 to 12");
        refused(With("\"from\": 22", "\"from\": 24"), "Hours are 0 to 23");
        refused(With("\"change\": 5", "\"change\": 11"), "A standing change is at most 10");
        refused(With("\"change\": 5", "\"change\": 0"), "A standing change is not 0");
        refused(With("\"faction\": \"OKLGCrim\", \"change\": -2.5", "\"faction\": \"OKLGCorp\", \"change\": -2.5"), "Each faction is changed once");
        refused(Standing.Replace("\"chatter\": {", "\"tips\": { \"t\": { \"text\": \"Lore.\", \"requires\": { \"months\": [ 1 ] } } }, \"chatter\": {"), "Lore cannot take a clock gate");

        check(StoryRules.Tier(100) == "honored" && StoryRules.Tier(99.9) == "trusted" && StoryRules.Tier(75) == "trusted" && StoryRules.Tier(50) == "friendly" && StoryRules.Tier(25) == "warm" &&
              StoryRules.Tier(0) == "neutral" && StoryRules.Tier(-49.9999) == "neutral" && StoryRules.Tier(-50) == "dislikes" && StoryRules.Tier(double.NaN) == "dislikes",
            "Tiers follow the game's thresholds: 100, 75, 50, 25, -50");
        check(StoryRules.HourIn(9, 17, 12) && !StoryRules.HourIn(9, 17, 18) && StoryRules.HourIn(22, 5, 23) && StoryRules.HourIn(22, 5, 2) && !StoryRules.HourIn(22, 5, 12), "An hour window wraps midnight");

        var facts = new Facts { Month = 12, Hour = 23, CrewCount = 2 };
        var record = new StoryRecord();
        check(StoryRules.Blocked(r, facts, record) != null, "An unknown faction blocks");
        facts.Standings["OKLGLEO"] = 30; facts.Standings["OKLGCrim"] = 0;
        check(StoryRules.Blocked(r, facts, record) != null && StoryRules.Blocked(r, facts, record)!.Contains("SkillBotany"), "Then the crew skill blocks: " + StoryRules.Blocked(r, facts, record));
        facts.CrewConditions.Add("SkillBotany");
        check(StoryRules.Blocked(r, facts, record) != null && StoryRules.Blocked(r, facts, record)!.Contains("Still"), "Then the running machine");
        facts.RunningMachines.Add("Still");
        check(StoryRules.Blocked(r, facts, record) == null, "With warm AyoSec, neutral criminals, a botanist, a still running, in December at 23:00 the advert shows");
        facts.Standings["OKLGLEO"] = 24;
        check(StoryRules.Blocked(r, facts, record) != null, "Standing below the tier blocks");
        facts.Standings["OKLGLEO"] = 30; facts.Standings["OKLGCrim"] = 25;
        check(StoryRules.Blocked(r, facts, record) != null, "Standing above atMost blocks");
        facts.Standings["OKLGCrim"] = -60; facts.CrewCount = 4;
        check(StoryRules.Blocked(r, facts, record) != null, "Too many crew blocks");
        facts.CrewCount = 0;
        check(StoryRules.Blocked(r, facts, record) != null, "Too few crew blocks");
        facts.CrewCount = 1; facts.Month = 6;
        check(StoryRules.Blocked(r, facts, record) != null, "The wrong month blocks");
        facts.Month = 1; facts.Hour = 12;
        check(StoryRules.Blocked(r, facts, record) != null, "An hour outside the window blocks");
        facts.Hour = 3;
        check(StoryRules.Blocked(r, facts, record) == null, "January at 03:00, inside a window over midnight, passes");

        var library = StoryLibrary.Build(new[] { ("x", pack) }, null, _ => true, null, null);
        var line = library.Lines.Single(l => l.Id == "cop-talk");
        check(StoryRules.Voices(line, false, false, false, new[] { "OKLGLEO", "OKLGCorp" }) && !StoryRules.Voices(line, false, false, false, new[] { "OKLGCiv" }) && !StoryRules.Voices(line, false, false, false, Array.Empty<string>()),
            "A line with speaker factions is said only by a member of one");
        check(StorySchema.Plain("Ask [crew].") == null, "[crew] is a placeholder");
    }

    private const string Grounded = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""places"": {
        ""oklg"": { ""station"": ""OKLG"", ""name"": ""OKLG"", ""body"": ""1036 Ganymed"", ""region"": ""Outer System"", ""factions"": [ ""OKLGCorp"" ] },
        ""oklg-res"": { ""station"": ""OKLG_RES"", ""name"": ""the residential level"", ""within"": ""oklg"" },
        ""vnca"": { ""station"": ""VNCA"", ""name"": ""Long Beach Terminal"", ""region"": ""Shipping & Inner System"" },
        ""vorb"": { ""station"": ""VORB"", ""name"": ""Venus Orbital"", ""within"": ""vnca"" }
      },
      ""people"": {
        ""neri"": { ""name"": ""Neri Vale"", ""role"": ""receiving clerk"", ""home"": ""oklg-res"", ""faction"": ""OKLGCorp"" },
        ""orra"": { ""name"": ""Orra Pell"", ""home"": ""vorb"" },
        ""lost"": { ""name"": ""Nobody"", ""home"": ""no-such-place"" }
      },
      ""threads"": {
        ""ledger"": { ""title"": ""The second-shift ledger"", ""place"": ""oklg"", ""people"": [ ""neri"" ], ""requires"": { ""mods"": [ ""PhobosShipbreaker"" ] } },
        ""venus"": { ""title"": ""Venus"", ""place"": ""vorb"" },
        ""loose"": { ""title"": ""No place"" }
      },
      ""broadcasts"": {
        ""ledger-news"": { ""thread"": ""ledger"", ""text"": ""A clerk at [place] on [body] wants the ledger checked, [person:neri] says."", ""weight"": 2, ""mention"": ""That ledger business at [place]."" },
        ""venus-news"": { ""thread"": ""venus"", ""region"": ""Venus"", ""text"": ""Rain again."", ""requires"": { ""flags"": [ ""ledger-closed"" ] } },
        ""anywhere-news"": { ""region"": ""Outer System"", ""text"": ""Prices up."" },
        ""step-news"": { ""region"": ""Outer System"", ""text"": ""Someone is asking about a can."", ""requires"": { ""arcsAtStep"": [ ""misfiled-can.letter"" ] } },
        ""nowhere-news"": { ""thread"": ""loose"", ""text"": ""No region, and a thread with no place."" },
        ""stranger-news"": { ""thread"": ""ledger"", ""text"": ""[person:orra] is not in this cast."" }
      },
      ""chatter"": {
        ""dock-talk"": { ""thread"": ""ledger"", ""moment"": ""complaint"", ""speakers"": ""locals"", ""line"": ""Paperwork at [place] again."" },
        ""crew-talk"": { ""place"": ""oklg"", ""moment"": ""small-talk"", ""speakers"": ""crew"", ""line"": ""Glad to be back at [station]."" },
        ""free-talk"": { ""moment"": ""joke"", ""line"": ""Anywhere, anyone."" }
      },
      ""arcs"": {
        ""misfiled-can"": { ""title"": ""The misfiled can"", ""thread"": ""ledger"", ""chance"": 0.5,
          ""steps"": [
            { ""id"": ""letter"", ""delivery"": { ""message"": { ""person"": ""neri"", ""text"": ""Come by, [player-first]."" } }, ""tests"": [ { ""kind"": ""dock-at"" } ],
              ""onComplete"": { ""setFlags"": [ ""ledger-open"" ] } },
            { ""id"": ""close"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ], ""onComplete"": { ""setFlags"": [ ""ledger-closed"" ], ""clearFlags"": [ ""ledger-open"" ] } }
          ] },
        ""far-arc"": { ""title"": ""Far"", ""place"": ""vorb"", ""chance"": 1, ""steps"": [ { ""id"": ""go"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] } ] },
        ""no-home"": { ""title"": ""No home"", ""thread"": ""loose"", ""steps"": [ { ""id"": ""dock"", ""tests"": [ { ""kind"": ""dock-at"" } ] } ] }
      },
      ""files"": { ""note"": { ""name"": ""NOTE.TXT"", ""text"": ""Written at [place]."", ""thread"": ""ledger"", ""person"": ""neri"" } }
    }";

    /// <summary>Framework 0.114.0: places, people, threads, flags, arc progress gates, placed news and small talk.</summary>
    private static void Round5(Action<bool, string> check, Action<string, string, bool> refused, Func<string, bool, StoryPack> load)
    {
        string With(string find, string replace) { check(Grounded.Contains(find), "Fixture has " + find); return Grounded.Replace(find, replace); }
        var pack = load(Grounded, false);
        check(pack.places.Count == 4 && pack.people.Count == 3 && pack.threads.Count == 3 && pack.arcs["misfiled-can"].steps[0].onComplete!.setFlags.SequenceEqual(new[] { "ledger-open" }),
            "Places, people, threads and flags load");
        refused(With("\"station\": \"OKLG\"", "\"station\": \"any\""), "A place is a station, not any", false);
        refused(With("\"region\": \"Outer System\", \"factions\"", "\"factions\""), "A regional place needs a region label", false);
        refused(With("\"home\": \"vorb\"", "\"home\": \"Venus Orbital\""), "A home is a place key", false);
        refused(With("\"people\": [ \"neri\" ]", "\"people\": [ \"neri\", \"neri\" ]"), "A cast names each person once", false);
        refused(With("\"message\": { \"person\": \"neri\", ", "\"message\": { "), "A message names a sender or a person", false);
        refused(With("\"setFlags\": [ \"ledger-closed\" ], \"clearFlags\": [ \"ledger-open\" ]", "\"setFlags\": [ \"ledger-open\" ], \"clearFlags\": [ \"ledger-open\" ]"), "A flag is not both set and cleared", false);
        refused(With("\"arcsAtStep\": [ \"misfiled-can.letter\" ]", "\"arcsAtStep\": [ \"misfiled-can\" ]"), "arcsAtStep names arc.step", false);
        refused(With("\"speakers\": \"locals\"", "\"speakers\": \"neighbours\""), "Speakers may be locals, not anything else new", false);
        refused(With("[person:neri]", "[person:Neri Vale]"), "A person token holds a key", false);
        refused(Grounded.Replace("\"schema\": \"story\",", "\"schema\": \"story\", \"settings\": { \"localWeight\": 101 },"), "The local weight has a ceiling", true);
        var tips = load(Grounded.Replace("\"files\": {", "\"tips\": { \"t\": { \"text\": \"Plain lore.\", \"thread\": \"ledger\" } }, \"files\": {"), false);
        check(tips.tips["t"].thread == "ledger", "A tip may name a thread for grouping");
        refused(Grounded.Replace("\"files\": {", "\"tips\": { \"t\": { \"text\": \"Lore at [place].\" } }, \"files\": {"), "Lore still takes no placeholder, the new ones included", false);
        check(StorySchema.Plain("See [person:neri] at [place] on [date]") == null && StorySchema.Plain("[person:]") == "[person:]", "Plain text allows person tokens with a key");
        check(StorySchema.People("[person:neri] and [person:orra]").SequenceEqual(new[] { "neri", "orra" }), "The people a text names are read back");
        check(StorySchema.Fill("[player] at [place], [person:neri], [nothing]", t => t == "player" ? "Ada" : t == "place" ? "OKLG" : t == "person:neri" ? "Neri" : null) == "Ada at OKLG, Neri, [nothing]",
            "Fill answers each token by name and leaves an unknown one");

        // The merged library: references between the new tables, and what they refuse.
        var library = StoryLibrary.Build(new[] { ("x", pack) }, null, _ => true, null, null);
        string problems = string.Join("; ", library.Problems);
        check(!library.People.ContainsKey("lost"), "A person with no home place is left out: " + problems);
        check(!library.Broadcasts.ContainsKey("nowhere-news"), "A news item with no region, in a thread with no place, is left out");
        refused(With("\"region\": \"Outer System\", \"text\": \"Prices up.\"", "\"text\": \"Prices up.\""), "A news item with neither region, place nor thread is refused by the file check", false);
        check(!library.Broadcasts.ContainsKey("stranger-news") && library.Broadcasts.ContainsKey("ledger-news"), "A thread with a cast refuses a person outside it, and keeps one inside");
        check(!library.Arcs.ContainsKey("no-home") && library.Arcs.ContainsKey("misfiled-can"), "A dock-at test with no station needs the arc's place, which a thread without one does not give");
        refused(With("\"thread\": \"loose\", \"steps\": [ { \"id\": \"dock\"", "\"steps\": [ { \"id\": \"dock\""), "A dock-at test with no station in an arc with no place or thread is refused by the file check", false);
        check(library.PlaceOf("ledger", null) == "oklg" && library.PlaceOf("ledger", "vorb") == "vorb" && library.PlaceOf(null, null) == null, "An entry's place is its own, else its thread's");
        check(library.ThreadRequires("ledger")!.mods.SequenceEqual(new[] { "PhobosShipbreaker" }) && library.ThreadRequires("venus") == null, "A thread's requirements are read by key");
        check(library.PersonName("neri", (o, k, inline) => inline) == "Neri Vale, receiving clerk" && library.PersonName("orra", (o, k, inline) => inline) == "Orra Pell", "People show as name and role");
        check(library.Members("ledger").Select(m => m.Id).OrderBy(i => i, StringComparer.Ordinal).SequenceEqual(new[] { "dock-talk", "ledger-news", "misfiled-can", "note" }), "A thread knows its members");
        var lines = library.Lines.ToDictionary(l => l.Id);
        check(lines["dock-talk"].Place == "oklg" && lines["crew-talk"].Place == "oklg" && lines["free-talk"].Place == null && lines["ledger-news"].Broadcast == "ledger-news" && lines["ledger-news"].Place == "oklg",
            "Lines carry their place, from the thread or their own, and a mention its broadcast");

        // Places: ids map to places by the longest station prefix; sub-places roll up to their region.
        var places = library.Places;
        check(places.Find("OKLG_RES") == "oklg-res" && places.Find("OKLG") == "oklg" && places.Find("OKLG_BIZ") == "oklg" && places.Find("VORB|Aux") == "vorb" && places.Find("ZZZZ") == null && places.Find(null) == null,
            "Station ids find their place, parts included");
        check(places.Root("oklg-res") == "oklg" && places.Root("oklg") == "oklg" && places.IsRegional("vnca") && !places.IsRegional("vorb") && places.Covers("oklg", "OKLG_RES") && !places.Covers("oklg-res", "OKLG"),
            "A sub-place lies within its region; a region covers its parts");
        check(places.Region("oklg-res") == "Outer System" && places.Body("vorb") == null && places.Name("oklg") == "OKLG", "Labels come from the place or its region");

        // Gates: flags, arcs under way, places and regions.
        var facts = new Facts { Epoch = 1000 };
        var record = new StoryRecord { Began = 0 };
        check(StoryRules.Blocked(pack.broadcasts["venus-news"].requires, facts, record) != null, "A flag not yet set blocks");
        record.SetFlag("ledger-closed", 500);
        check(StoryRules.Blocked(pack.broadcasts["venus-news"].requires, facts, record) == null, "and holds once set");
        check(StoryRules.Blocked(new StoryRequires { notFlags = new() { "ledger-closed" } }, facts, record) != null, "notFlags blocks while the flag is set");
        check(StoryRules.Blocked(pack.broadcasts["step-news"].requires, facts, record) != null, "arcsAtStep waits for the arc");
        record.Arcs["misfiled-can"] = new ArcProgress { State = ArcState.Active, StepId = "letter" };
        check(StoryRules.Blocked(pack.broadcasts["step-news"].requires, facts, record) == null && StoryRules.Blocked(new StoryRequires { arcsActive = new() { "misfiled-can" } }, facts, record) == null, "and holds at that step, as does arcsActive");
        record.Arcs["misfiled-can"].StepId = "close";
        check(StoryRules.Blocked(pack.broadcasts["step-news"].requires, facts, record) != null, "A later step no longer counts as the earlier one");
        check(StoryRules.Blocked(new StoryRequires { places = new() { "oklg" } }, facts, record) != null, "A place gate needs the player there");
        facts.At.Add("oklg");
        check(StoryRules.Blocked(new StoryRequires { places = new() { "oklg" } }, facts, record) == null, "and holds when docked at it");
        facts.At.Clear(); facts.Region = "oklg";
        check(StoryRules.Blocked(new StoryRequires { places = new() { "oklg" } }, facts, record) == null && StoryRules.Blocked(new StoryRequires { regions = new() { "oklg" } }, facts, record) == null &&
              StoryRules.Blocked(new StoryRequires { regions = new() { "vnca" } }, facts, record) != null, "A regional place is near from anywhere in its region");
        check(StoryRules.Blocked(new StoryRequires { newsSeen = new() { "ledger-news" } }, facts, record) != null, "newsSeen waits for the broadcast");
        record.MarkSeen("ledger-news", 900);
        check(StoryRules.Blocked(new StoryRequires { newsSeen = new() { "ledger-news" } }, facts, record) == null, "and holds once shown");
        check(StoryRules.Blocked(null, pack.threads["ledger"].requires, facts, record) != null && StoryRules.Blocked(null, pack.threads["ledger"].requires, new Facts { Mods = { "PhobosShipbreaker" } }, record) == null,
            "A thread's requirements gate its members");

        // Weights, voices and the mention window.
        var settings = new StorySettings();
        check(StoryRules.PlaceWeight(2, "oklg", true, settings) == 8 && StoryRules.PlaceWeight(2, "oklg", false, settings) == 2 && StoryRules.PlaceWeight(2, null, false, settings) == 4,
            "Local news weighs four times, unplaced twice, far once");
        check(StoryRules.PlaceWeight(2, "oklg", false, new StorySettings { farWeight = 0 }) == 0, "A far weight of 0 hides far news");
        check(StoryRules.Voices(lines["dock-talk"], false, true, false) && !StoryRules.Voices(lines["dock-talk"], false, false, true) && !StoryRules.Voices(lines["dock-talk"], true, true, true),
            "A locals line is said by others at the place only");
        check(StoryRules.Voices(lines["crew-talk"], true, false, true) && !StoryRules.Voices(lines["crew-talk"], true, true, false) && !StoryRules.Voices(lines["crew-talk"], false, true, true),
            "A placed crew line is said by crew while the player is there");
        check(StoryRules.Voices(lines["free-talk"], true, false, false) && StoryRules.Voices(lines["free-talk"], false, false, false), "An unplaced line is said anywhere");
        check(StoryRules.Voices(StorySchema.Locals, false) && !StoryRules.Voices(StorySchema.Locals, true), "locals on an unplaced line means others");
        check(StoryRules.MentionFresh(900, 1000, 10) && !StoryRules.MentionFresh(null, 1000, 10) && !StoryRules.MentionFresh(0, 11 * 86400, 10) && StoryRules.MentionFresh(0, 9 * 86400, 10),
            "A mention is fresh for the mention days after its news was shown");
        check(record.SeenEpoch("ledger-news") == 900 && record.SeenEpoch("never") == null, "When news was shown is remembered");
        var old = StoryRecord.Decode(new Dictionary<string, string> { ["seen.old-news"] = "1", ["began"] = "250" });
        check(old.Seen.Contains("old-news") && old.SeenEpoch("old-news") == 250, "News seen before times were kept reads as seen when the record began");

        // The record: flags and seen times round-trip; an older record is unchanged.
        record.SetFlag("ledger-open", 700); record.SetFlag("ledger-open", 800);
        var fields = record.Encode();
        check(fields["flag.ledger-open"] == "700" && fields["flag.ledger-closed"] == "500" && fields["seen.ledger-news"] == "900", "Flags keep their first time; seen news keeps its time");
        check(fields.All(f => Phobos.Ostranauts.Framework.Persistence.ObjectStateStore.SafeValue(f.Value)), "The new fields fit a Phobos record");
        var back = StoryRecord.Decode(fields);
        check(back.Flags["ledger-open"] == 700 && back.SeenAt["ledger-news"] == 900 && back.Seen.Contains("ledger-news"), "Flags and seen times round-trip");
        back.ClearFlag("ledger-open");
        check(!back.Encode().ContainsKey("flag.ledger-open") && back.Encode().ContainsKey("flag.ledger-closed"), "A cleared flag leaves the record");
        check(StorySchema.TryArcStep("misfiled-can.letter", out var a, out var st) && a == "misfiled-can" && st == "letter" && !StorySchema.IsArcStep("a.b.c") && !StorySchema.IsArcStep("nodot"), "arc.step reads back");
    }

    private const string Files = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""files"": { ""old-log"": { ""name"": ""OLD_LOG.TXT"", ""text"": ""Found aboard the [ship]."", ""startsArc"": ""follow-up"" },
                   ""orphan"": { ""name"": ""ORPHAN.TXT"", ""text"": ""Starts nothing real."", ""startsArc"": ""no-such-arc"" } },
      ""arcs"": {
        ""giver"": { ""title"": ""Giver"", ""steps"": [ { ""id"": ""give"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ], ""onComplete"": { ""files"": [ ""old-log"" ] } } ] },
        ""follow-up"": { ""title"": ""Follow-up"", ""requires"": { ""filesRead"": [ ""old-log"" ] }, ""steps"": [ { ""id"": ""go"", ""tests"": [ { ""kind"": ""dock-at"", ""station"": ""any"" } ] } ] },
        ""bad-gift"": { ""title"": ""Bad gift"", ""steps"": [ { ""id"": ""give"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ], ""onComplete"": { ""files"": [ ""missing-file"" ] } } ] }
      },
      ""sections"": { ""lore"": { ""label"": ""Lore"", ""title"": ""Lore"", ""image"": ""phobos/agriculture/Counter"" } },
      ""articles"": { ""page"": { ""section"": ""lore"", ""label"": ""Page"", ""title"": ""Page"", ""body"": ""Words."", ""image"": ""phobos/agriculture/Cooker"" } }
    }";

    /// <summary>Framework 0.110.0: data files, files given by outcomes, filesRead and encyclopedia pictures.</summary>
    private static void Round4(Action<bool, string> check, Action<string, string> refused, Func<string, StoryPack> load)
    {
        string With(string find, string replace) { check(Files.Contains(find), "Fixture has " + find); return Files.Replace(find, replace); }
        var pack = load(Files);
        check(pack.files.Count == 2 && pack.arcs["giver"].steps[0].onComplete!.files.SequenceEqual(new[] { "old-log" }) && pack.articles["page"].image == "phobos/agriculture/Cooker", "Files, file rewards and pictures load");
        refused(With("\"OLD_LOG.TXT\"", "\"OLD LOG.TXT\""), "A file name has no spaces");
        refused(With("\"image\": \"phobos/agriculture/Cooker\"", "\"image\": \"phobos/agriculture/Cooker.png\""), "A picture path leaves out .png");
        refused(With("\"files\": [ \"old-log\" ]", "\"files\": [ \"old-log\", \"old-log\" ]"), "A reward names each file once");
        refused(With("\"label\": \"Lore\"", "\"label\": \"Lore\", \"requires\": { \"filesRead\": [ \"old-log\" ] }"), "The encyclopedia cannot require a file read: there is no player when it is built");

        var library = StoryLibrary.Build(new[] { ("x", pack) }, null, _ => true, null, null);
        check(library.Files.ContainsKey("old-log") && !library.Files.ContainsKey("orphan") && !library.Arcs.ContainsKey("bad-gift") && library.Arcs.ContainsKey("giver"),
            "A file that starts an unknown arc, and an arc that gives an unknown file, are left out: " + string.Join("; ", library.Problems));
        var facts = new Facts();
        var record = new StoryRecord();
        check(StoryRules.Blocked(library.Arcs["follow-up"].Value.requires, facts, record) != null, "filesRead waits for the file to be opened");
        record.Read.Add("old-log");
        check(StoryRules.Blocked(library.Arcs["follow-up"].Value.requires, facts, record) == null, "and holds once it has been");
        check(StoryRecord.Decode(record.Encode()).Read.SetEquals(new[] { "old-log" }), "Opened files round-trip in the record");
        var nodes = StoryLore.Nodes(library, _ => true, (o, k, inline) => inline);
        check(nodes.Single(n => n.Name == "PhobosStory.page").Image == "phobos/agriculture/Cooker" && nodes.Single(n => n.Name == "PhobosStory.lore").Image == "phobos/agriculture/Counter", "Pictures reach the encyclopedia nodes");
    }

    private const string Branching = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""arcs"": { ""debt-run"": { ""title"": ""A debt to clear"", ""requires"": { ""afterDays"": 2, ""beforeDays"": 30 },
        ""steps"": [
          { ""id"": ""offer"", ""tests"": [ { ""kind"": ""credits"", ""amount"": 500, ""consume"": true } ],
            ""onComplete"": { ""credits"": 50 }, ""next"": ""thanks"",
            ""branches"": [ { ""tests"": [ { ""kind"": ""condition"", ""condition"": ""SkillHacking"" } ], ""next"": ""hacked"", ""onComplete"": { ""credits"": 900 } },
                            { ""tests"": [ { ""kind"": ""wait"", ""hours"": 48 } ], ""next"": ""end"" } ] },
          { ""id"": ""hacked"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ], ""next"": ""end"" },
          { ""id"": ""thanks"", ""tests"": [ { ""kind"": ""wait"", ""hours"": 1 } ] }
        ] } }
    }";

    /// <summary>Framework 0.109.0: credits and condition tests, branches and next steps, credit rewards, story time.</summary>
    private static void Round3(Action<bool, string> check, Action<string, string> refused, Func<string, StoryPack> load)
    {
        string With(string find, string replace) { check(Branching.Contains(find), "Fixture has " + find); return Branching.Replace(find, replace); }
        var pack = load(Branching);
        var arc = pack.arcs["debt-run"];
        check(arc.steps[0].branches!.Count == 2 && arc.steps[0].onComplete!.credits == 50 && arc.requires!.afterDays == 2, "Branches, credit rewards and story time load");
        refused(With("\"next\": \"thanks\"", "\"next\": \"nowhere\""), "next must name a step of the arc or end");
        refused(With("\"next\": \"hacked\"", "\"next\": \"elsewhere\""), "A branch's next must name a step of the arc or end");
        refused(With("\"amount\": 500", "\"amount\": 0"), "A credits test needs an amount");
        refused(With("\"amount\": 500", "\"amount\": 500, \"item\": \"Seed\""), "A credits test takes only amount and consume");
        refused(With("\"condition\": \"SkillHacking\"", "\"condition\": \"SkillHacking\", \"consume\": true"), "A condition test cannot consume");
        refused(With("{ \"kind\": \"wait\", \"hours\": 48 }", "{ \"kind\": \"wait\", \"hours\": 48, \"amount\": 3 }"), "Only a credits test takes an amount");
        refused(With("\"credits\": 50 }", "\"credits\": 50001 }"), "A credit reward has a ceiling");
        refused(With("\"beforeDays\": 30", "\"beforeDays\": 1"), "afterDays must come before beforeDays");
        refused(With("\"branches\": [", "\"branches\": [ { \"tests\": [ { \"kind\": \"wait\", \"hours\": 1 } ], \"next\": \"end\" }, { \"tests\": [ { \"kind\": \"wait\", \"hours\": 2 } ], \"next\": \"end\" }, { \"tests\": [ { \"kind\": \"wait\", \"hours\": 3 } ], \"next\": \"end\" },"), "At most four branches");
        var library = StoryLibrary.Build(new[] { ("x", pack) }, null, _ => true, _ => true, c => c == "SkillHacking");
        check(library.Arcs.ContainsKey("debt-run"), "Conditions named by tests are checked against the game: known ones pass");
        check(!StoryLibrary.Build(new[] { ("x", pack) }, null, _ => true, _ => true, _ => false).Arcs.ContainsKey("debt-run"), "and an unknown condition in a branch test leaves the arc out");

        var facts = new Facts { Epoch = 10 * 86400 };
        var record = new StoryRecord { Began = 9 * 86400 };
        check(StoryRules.Days(record, facts) == 1 && StoryRules.Blocked(arc.requires, facts, record) != null, "One day of story time is too early for afterDays 2");
        facts.Epoch = 12 * 86400;
        check(StoryRules.Blocked(arc.requires, facts, record) == null, "Three days is inside the window");
        facts.Epoch = 40 * 86400;
        check(StoryRules.Blocked(arc.requires, facts, record) != null, "Thirty-one days is past beforeDays");
        check(StoryRules.Days(new StoryRecord(), facts) == 0, "A record that has not begun counts no days");
        var round = StoryRecord.Decode(record.Encode());
        check(round.Began == 9 * 86400 && StoryRecord.Decode(new StoryRecord().Encode()).Began == null, "Story time's start round-trips, and an older record has none");

        var offer = arc.steps[0];
        facts.Epoch = 0;
        check(StoryRules.Outcome(offer, facts, 0) == null, "With nothing met, the step waits");
        facts.Credits = 600;
        check(StoryRules.Outcome(offer, facts, 0) == -1, "Its own test (500 credits) finishes it");
        facts.Credits = 0; facts.Conditions.Add("SkillHacking");
        check(StoryRules.Outcome(offer, facts, 0) == 0, "The first branch whose tests pass decides");
        facts.Conditions.Clear(); facts.Epoch = 48 * 3600;
        check(StoryRules.Outcome(offer, facts, 0) == 1, "A later branch when only its tests pass");
        facts.Credits = 600; facts.Conditions.Add("SkillHacking");
        check(StoryRules.Outcome(offer, facts, 0) == -1, "The step's own tests come before its branches");
        check(StoryRules.NextStep(arc, 0, "thanks") == 2 && StoryRules.NextStep(arc, 0, "end") == -1 && StoryRules.NextStep(arc, 1, null) == 2 && StoryRules.NextStep(arc, 2, null) == -1,
            "Next steps: named, the end, or the next in order until the last");
        check(StoryRules.Passed(new StoryTest { kind = StorySchema.Credits, amount = 600 }, facts, 0) && !StoryRules.Passed(new StoryTest { kind = StorySchema.Credits, amount = 601 }, facts, 0), "A credits test compares what the player holds");
    }

    private const string Talk = @"{
      ""schemaVersion"": 1, ""schema"": ""story"",
      ""broadcasts"": { ""wheat-news"": { ""region"": ""Tharsis"", ""text"": ""Wheat is short."", ""weight"": 3, ""mention"": ""Wheat's short, [player-first]."" },
                        ""quiet-news"": { ""region"": ""Tharsis"", ""text"": ""Nothing to say."" } },
      ""chatter"": {
        ""rack-hum"": { ""moment"": ""complaint"", ""speakers"": ""crew"", ""line"": ""The rack hums all night."", ""requires"": { ""owns"": [ ""Rack"" ] } },
        ""kiosk-gossip"": { ""moment"": ""small-talk"", ""speakers"": ""others"", ""line"": ""Potatoes at the kiosk."" },
        ""crop-name"": { ""moment"": ""superstition"", ""line"": ""Never name a crop."", ""weight"": 2 }
      },
      ""tips"": { ""first-lettuce"": { ""text"": ""Fresh lettuce tastes better."", ""requires"": { ""mods"": [ ""PhobosAgriculture"" ] } } },
      ""sections"": { ""makers"": { ""label"": ""Makers"", ""title"": ""Makers and brands"" }, ""empty-section"": { ""label"": ""Empty"", ""title"": ""Nothing here"" } },
      ""articles"": {
        ""verdemorrow"": { ""section"": ""makers"", ""label"": ""Verdemorrow"", ""title"": ""Verdemorrow Agronomics"", ""body"": ""Grow racks.\n\nAnd cookers."" },
        ""manufacturing-only"": { ""section"": ""makers"", ""label"": ""Fennmark"", ""title"": ""Fennmark"", ""body"": ""Refineries."", ""requires"": { ""mods"": [ ""PhobosManufacturing"" ] } },
        ""lost-article"": { ""section"": ""no-such-section"", ""label"": ""Lost"", ""title"": ""Lost"", ""body"": ""Nowhere."" }
      }
    }";

    /// <summary>Framework 0.108.0: small talk, loading tips and encyclopedia articles.</summary>
    private static void Phase2(Action<bool, string> check, Action<string, string, bool> refused, Func<string, bool, StoryPack> load)
    {
        string With(string find, string replace) { check(Talk.Contains(find), "Fixture has " + find); return Talk.Replace(find, replace); }
        var pack = load(Talk, false);
        check(pack.chatter.Count == 3 && pack.tips.Count == 1 && pack.sections.Count == 2 && pack.articles.Count == 3 && pack.broadcasts["wheat-news"].mention != null, "Small talk, tips and articles load");
        refused(With("\"moment\": \"complaint\"", "\"moment\": \"gossip\""), "An unknown moment is refused", false);
        refused(With("\"speakers\": \"crew\"", "\"speakers\": \"everyone\""), "Speakers are anyone, crew or others", false);
        refused(With("The rack hums all night.", new string('x', StorySchema.MaxLine + 1)), "A line has a length limit", false);
        refused(With("Fresh lettuce tastes better.", "Fresh lettuce, [player]."), "A tip cannot use placeholders: there is no player at a loading screen", false);
        refused(With("\"mods\": [ \"PhobosAgriculture\" ] } } },", "\"owns\": [ \"Rack\" ] } } },"), "A tip may require only mods", false);
        refused(With("Grow racks.", "Grow racks for [ship]."), "An article cannot use placeholders", false);
        refused(With("\"label\": \"Verdemorrow\"", "\"label\": \"" + new string('x', StorySchema.MaxLabel + 1) + "\""), "A list label has a length limit", false);
        refused(Talk.Replace("\"schema\": \"story\",", "\"schema\": \"story\", \"settings\": { \"chatterShare\": 1.5 },"), "The chatter share is from 0 to 1", true);

        var library = StoryLibrary.Build(new[] { ("agriculture", pack) }, null, m => m == "PhobosAgriculture", i => i == "Rack", _ => true);
        check(!library.Articles.ContainsKey("lost-article") && library.Problems.Count == 1, "An article in an unknown section is left out with a problem: " + string.Join("; ", library.Problems));
        var lines = library.Lines.ToDictionary(l => l.Id);
        check(lines.Count == 4 && lines["wheat-news"].Moment == StoryMoments.Headline && lines["wheat-news"].Key == "wheat-news.mention" && lines["wheat-news"].Weight == 3,
            "A broadcast's mention is a headline line with the broadcast's weight; a broadcast without one adds none");
        check(lines["rack-hum"].Key == "rack-hum.line" && lines["crop-name"].Speakers == StorySchema.Anyone, "Chatter lines keep their translation key and default to anyone");

        // Moments are the game's own small talk.
        check(StoryMoments.Of("SOCMentionHeadline") == "headline" && StoryMoments.Of("SOCDarkJoke") == "joke" && StoryMoments.Of("SOCShareAnotherStory") == "story", "Game interactions map to moments");
        check(StoryMoments.Of("SOCInsult") == null && StoryMoments.Of(null) == null, "Other interactions carry no story line");
        check(StoryMoments.Interactions.Values.SelectMany(v => v).Distinct().Count() == StoryMoments.Interactions.Values.Sum(v => v.Length), "Each game interaction belongs to one moment only");

        // Pools by moment, then speakers.
        var noRack = StoryRules.ChatterPools(library.Lines, r => r == null || r.owns.Count == 0);
        check(!noRack.ContainsKey("complaint") && noRack["headline"].Count == 1 && noRack["small-talk"].Count == 1, "Pools hold the lines eligible now, by moment");
        var all = StoryRules.ChatterPools(library.Lines, _ => true);
        check(StoryRules.PickLine(all["complaint"], speakerIsCrew: true, 0.5)?.Id == "rack-hum" && StoryRules.PickLine(all["complaint"], speakerIsCrew: false, 0.5) == null, "A crew line is said only aboard the player's ships");
        check(StoryRules.PickLine(all["small-talk"], true, 0.5) == null && StoryRules.PickLine(all["small-talk"], false, 0.5)?.Id == "kiosk-gossip", "An others line is said only away from them");
        check(StoryRules.PickLine(all["superstition"], true, 0.5) != null && StoryRules.PickLine(all["superstition"], false, 0.5) != null, "An anyone line is said by anyone");
        check(StoryRules.PickLine(null, true, 0.5) == null, "No pool, no line");
        check(StoryRules.Voices(StorySchema.Crew, true) && !StoryRules.Voices(StorySchema.Others, true) && StoryRules.Voices(StorySchema.Anyone, false), "Speakers match as written");

        // The encyclopedia: sections only with an article to show, articles under their section, mods respected.
        string Words(string owner, string key, string inline) => inline;
        var nodes = StoryLore.Nodes(library, m => m == "PhobosAgriculture", Words);
        check(nodes.Select(n => n.Name).SequenceEqual(new[] { "PhobosStory.makers", "PhobosStory.verdemorrow" }), "One section with its one shown article: " + string.Join(", ", nodes.Select(n => n.Name)));
        check(nodes[0].Parent == null && nodes[1].Parent == "PhobosStory.makers" && nodes[1].Body.Contains("\n\n"), "Sections hang under the index and articles under their section, paragraphs kept");
        var withManufacturing = StoryLore.Nodes(library, _ => true, Words);
        check(withManufacturing.Count == 3 && withManufacturing.All(n => n.Parent == null || withManufacturing.Any(p => p.Name == n.Parent)), "With its mod installed the other article shows too, and every parent is shown");
        check(StoryLore.Nodes(StoryLibrary.Empty, _ => true, Words).Count == 0, "No articles, no sections");
    }
}
