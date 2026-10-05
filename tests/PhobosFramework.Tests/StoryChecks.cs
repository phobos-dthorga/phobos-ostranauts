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
        public HashSet<string> Mods = new(), Conditions = new(), Docked = new();
        public Dictionary<string, int> Ship = new(), Hands = new();
        public double Epoch { get; set; }
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
        Refused(With("[ship]", "[captain]"), "Only the three placeholders may be bracketed");
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
