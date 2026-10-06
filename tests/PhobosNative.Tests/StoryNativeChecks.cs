using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Story;

/// <summary>Story packs over the game's real data (Framework 0.107.0, Agriculture 0.60.0): every item and condition the
/// shipped packs name exists once the mods have prepared, every goal test is a valid trigger that only Framework can
/// complete, and the game's members the story code relies on are where it expects them. Nothing is published.</summary>
internal static class StoryNativeChecks
{
    internal static void Run(string repo, Action<bool, string> check)
    {
        StoryPack Load(string mod, bool framework) =>
            DataPacks.LoadText<StoryPack>(File.ReadAllText(Path.Combine(repo, "mods", mod, "framework", "story.json")), "", mod, StorySchema.Name, p => StorySchema.Validate(p, framework));
        var frameworkPack = Load("PhobosFramework", true);
        var agriculture = Load("PhobosAgriculture", false);
        check(frameworkPack.settings != null && frameworkPack.broadcasts.Count == 0 && frameworkPack.arcs.Count == 0, "Framework's story pack holds the settings and no stories");
        var library = StoryLibrary.Build(new[] { ("framework", frameworkPack), ("agriculture", agriculture) }, frameworkPack.settings, _ => true,
            id => DataHandler.dictCOs.ContainsKey(id), c => DataHandler.dictConds.ContainsKey(c));
        check(library.Problems.Count == 0, "Every name in the shipped story packs exists in the game's data: " + string.Join("; ", library.Problems));
        check(library.Broadcasts.Count == 3 && library.Adverts.Count == 2 && library.Arcs.ContainsKey("verdemorrow-grain-sample"), "Agriculture's seed loads whole");
        foreach (var item in new[] { "PhobosVerdemorrowWheatGrain", "PhobosVerdemorrowContinuanceWheat", "PhobosVerdemorrowGroundworkNutrients" })
            check(DataHandler.dictCOs.TryGetValue(item, out var co) && co.nStackLimit > 1, "A seed arc item is a stackable loose item: " + item);
        check(DataHandler.dictCOs.ContainsKey("PhobosVerdemorrowFirstlight4Installed") && DataHandler.dictCOs.ContainsKey("PhobosVerdemorrowHearth2Installed"),
            "The seed's ownership requirements name the installed rack and cooker");

        var d = new NativeDefinitions();
        StoryArcs.AddDefinitions(d, library);
        check(d.Conditions.TryGetValue(StoryArcs.Never, out var never) && never.nDisplaySelf == 2 && never.nDisplayOther == 2, "The goal condition is hidden");
        var tests = d.Triggers.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        check(tests.SequenceEqual(new[] { "PhobosStory.verdemorrow-grain-sample.deliver", "PhobosStory.verdemorrow-grain-sample.grow" }), "One goal test per step with a goal: " + string.Join(", ", tests));
        foreach (var trigger in d.Triggers.Values)
        {
            NativeDefinitions.Validate(trigger);
            check(trigger.aReqs.SequenceEqual(new[] { StoryArcs.Never }) && trigger.fChance == 1 && !DataHandler.dictCTs.ContainsKey(trigger.strName),
                "A goal test requires only the condition no one has, and names no trigger of the game's: " + trigger.strName);
            check(!trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["PhobosVerdemorrowWheatGrain"]), false), "A goal test never passes by itself: " + trigger.strName);
        }
        check(!DataHandler.dictConds.ContainsKey(StoryArcs.Never), "The goal condition is Framework's own name");

        // The game members the runner and the TV postfixes use.
        check(typeof(Ostranauts.Objectives.ObjectiveTracker).GetMethod("RemoveObjective")?.GetParameters().Select(p => p.Name).SequenceEqual(new[] { "objective", "strReason", "bCompleted" }) == true,
            "RemoveObjective's parameters are named as the dismissal postfix expects");
        check(typeof(DataHandler).GetMethod("GetHeadline")?.ReturnType == typeof(JsonHeadline) && typeof(DataHandler).GetMethod("GetAd")?.ReturnType == typeof(JsonAd),
            "The TV asks the game for one headline and one advert at a time");

        // Framework 0.108.0: small talk, loading tips and encyclopedia articles.
        // Framework 0.117.0 adds the Phobos operations section and its five help articles.
        check(library.Chatter.Count == 6 && library.Tips.Count == 2 && library.Sections.Count == 3 && library.Articles.Count == 7 && library.Lines.Count == 9,
            "The seed's small talk (six lines and three news mentions), tips and articles load, with Framework's five help articles");
        foreach (var pair in StoryMoments.Interactions)
            foreach (var name in pair.Value)
                check(DataHandler.dictInteractions.TryGetValue(name, out var interaction) && !string.IsNullOrEmpty(interaction.strDesc), "A moment's small talk is the game's own: " + name);
        // Every grammar token a lead-in uses already appears in the game's own social lines, so the game inflects it.
        var tokens = new System.Text.RegularExpressions.Regex(@"\[[^\]]+\]");
        var vanilla = new System.Collections.Generic.HashSet<string>(DataHandler.dictInteractions.Values.SelectMany(i => tokens.Matches(i.strDesc ?? "").Cast<System.Text.RegularExpressions.Match>().Select(m => m.Value)));
        var catalog = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(Path.Combine(repo, "translations", "PhobosFramework", "en.json")));
        foreach (var moment in StoryMoments.Interactions.Keys)
        {
            string? leadIn = (string?)catalog["Story.moment." + moment];
            check(leadIn != null && leadIn.Contains("{0}"), "Each moment has a lead-in with a place for the line: " + moment);
            foreach (System.Text.RegularExpressions.Match token in tokens.Matches(leadIn ?? ""))
                check(vanilla.Contains(token.Value), "A lead-in uses only grammar tokens the game's own lines use: " + moment + " " + token.Value);
        }
        var nodes = StoryLore.Nodes(library, _ => true, (owner, key, inline) => inline);
        check(nodes.Count == 10 && nodes.All(n => n.Parent == null || nodes.Any(p => p.Name == n.Parent)) && nodes.All(n => n.Name.StartsWith(StoryLore.NodePrefix, StringComparison.Ordinal)),
            "Both shared sections show with Agriculture's articles, and the Phobos operations section with its five; every parent is ours and no name is the game's");
        // Framework 0.117.0: every About button names an article that becomes a node under the operations section, and
        // the encyclopedia still opens a node by its name (Info.OpenToNode looks it up in mapNodes, keyed by strName).
        foreach (string article in StoryLore.OperationsArticles)
            check(nodes.Any(n => n.Name == StoryLore.NodePrefix + article && n.Parent == StoryLore.NodePrefix + StoryLore.OperationsSection),
                "An About button's article is in the encyclopedia under Phobos operations: " + article);
        foreach (string article in new[] { "operations-standing-orders", "operations-upkeep", "operations-time-skips", Phobos.Ostranauts.Framework.Registration.MaintenanceInformation.Article })
            check(StoryLore.OperationsArticles.Contains(article), "The panel's About button opens a listed article: " + article);
        check(typeof(Info).GetMethod("OpenToNode", new[] { typeof(string) }) != null && typeof(Info).GetField("mapNodes")?.FieldType == typeof(System.Collections.Generic.Dictionary<string, InfoNode>) &&
              typeof(Info).GetField("instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) != null,
            "The encyclopedia still opens a node by name through its own instance");
        check(typeof(DataHandler).GetMethod("GetTip")?.ReturnType == typeof(JsonTip) && typeof(Info).GetMethod("BuildHierarchyFromJSON") != null,
            "Loading tips come one at a time, and the encyclopedia builds its tree in the method we prepare for");

        // Framework 0.110.0: story data files on the game's own data cards.
        check(library.Files.ContainsKey("trial-notes") && library.Arcs["verdemorrow-grain-sample"].Value.steps[1].onComplete!.files.SequenceEqual(new[] { "trial-notes" }),
            "Agriculture's grain-sample reward carries its data file");
        check(DataHandler.dictCOs.TryGetValue(StoryFiles.Card, out var card) && card.strLoot == "ItmCardDataStorageEmpty" && DataHandler.dictLoot.ContainsKey(card.strLoot) &&
              Newtonsoft.Json.JsonConvert.SerializeObject(DataHandler.dictLoot[card.strLoot]).Contains(StoryFiles.Store),
            "The game's data card comes with its data store, from its own loot");
        check(d.Objects.TryGetValue(StoryFiles.Definition, out var file) && file.strItemDef == DataHandler.dictCOs[StoryFiles.VanillaFile].strItemDef &&
              file.aStartingConds.Any(c => c.StartsWith("IsDataItem", StringComparison.Ordinal)), "Framework's story file is the game's data file under our name");
        var storeCt = DataHandler.dictCOs[StoryFiles.Store].strContainerCT;
        check(storeCt != null && DataHandler.dictCTs[storeCt].TriggeredDataCO(new DataCO(file), false), "A data store accepts the story file");
        check(typeof(GUIComputer2).GetField("strStorageRun", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)?.FieldType == typeof(string),
            "The computer names the file it opens in the field the story postfix reads");

        // Phobos Spacer Stories (data-only add-on): with every Phobos mod's items published, the whole collection loads
        // beside the shipped packs with no entry left out, so every item and condition it names exists in the game.
        var folder = Path.Combine(repo, "mods", "PhobosSpacerStories", "phobos", "PhobosFramework", "story");
        var stories = Directory.GetFiles(folder, "*.json").OrderBy(f => f, StringComparer.Ordinal)
            .Select(f => (Owner: "spacer-stories", Pack: DataPacks.LoadText<StoryPack>(File.ReadAllText(f), "", "spacer-stories", StorySchema.Name, p => StorySchema.Validate(p, false)))).ToList();
        // Auto Nav's definitions are prepared but not published in this harness; the game publishes them in play.
        var autoNav = PhobosAutoNav.EquipmentContent.Prepare();
        var all = StoryLibrary.Build(new[] { ("framework", frameworkPack), ("agriculture", agriculture) }.Concat(stories), frameworkPack.settings, _ => true,
            id => DataHandler.dictCOs.ContainsKey(id) || autoNav.Objects.ContainsKey(id), c => DataHandler.dictConds.ContainsKey(c));
        check(stories.Count > 0 && all.Problems.Count == 0, "Phobos Spacer Stories loads whole against the game's data: " + string.Join("; ", all.Problems.Take(10)));
        // Counted from the add-on's own files, so a new volume needs no edit here (Spacer Stories 0.3.0 has 13 overlays).
        int Added<T>(Func<StoryPack, Dictionary<string, T>> table) => stories.Sum(s => table(s.Pack).Count);
        check(all.Arcs.Count == library.Arcs.Count + Added(p => p.arcs) && all.Files.Count == library.Files.Count + Added(p => p.files) &&
              all.Broadcasts.Count == library.Broadcasts.Count + Added(p => p.broadcasts) && all.Adverts.Count == library.Adverts.Count + Added(p => p.adverts) &&
              all.Chatter.Count == library.Chatter.Count + Added(p => p.chatter) && all.Tips.Count == library.Tips.Count + Added(p => p.tips) &&
              all.Articles.Count == library.Articles.Count + Added(p => p.articles) && all.People.Count == library.People.Count + Added(p => p.people) &&
              all.Threads.Count == library.Threads.Count + Added(p => p.threads),
            "Every chain, file, news item, advert, line, tip, article, person and thread it authors loads, none dropped or merged away");
        check(all.Articles.Values.All(a => all.Sections.ContainsKey(a.Value.section)), "Every article sits in a known section");
        check(stories.SelectMany(s => s.Pack.broadcasts.Keys.Concat(s.Pack.adverts.Keys).Concat(s.Pack.chatter.Keys).Concat(s.Pack.tips.Keys).Concat(s.Pack.articles.Keys)
            .Concat(s.Pack.arcs.Keys).Concat(s.Pack.files.Keys)).All(id => id.StartsWith("spacertales-", StringComparison.Ordinal)), "Every id carries the add-on's own prefix");

        Places(check, frameworkPack, library);
    }

    /// <summary>Framework 0.114.0: the shipped places are the game's own stations. Read from the game's star system
    /// file, as DataHandler loads it: every place's station exists, a regional place is one the game marks regional,
    /// a part lies within the region the game's nearest regional station would give it, and the factions match.</summary>
    private static void Places(Action<bool, string> check, StoryPack frameworkPack, StoryLibrary library)
    {
        string file = Path.Combine(StoryNativeData.Native, "star_systems", "star_system.json");
        var systems = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonStarSystemSave[]>(File.ReadAllText(file))!;
        var system = systems.First(s => s.strName == "NewGame");
        var stations = system.aSpawnStations.ToDictionary(s => s.strName, StringComparer.Ordinal);
        var places = library.Places;
        check(frameworkPack.places.Count >= 12 && places.Count == frameworkPack.places.Count, "Framework ships its places and all of them merged");
        int regional = 0;
        foreach (var pair in frameworkPack.places)
        {
            var place = pair.Value;
            check(stations.TryGetValue(place.station, out var station), "A shipped place is a station in the game's star system: " + pair.Key + " = " + place.station);
            if (station == null) continue;
            if (place.within == null)
            {
                regional++;
                check(station.bIsRegion, "A regional place is a station the game marks as a region: " + pair.Key);
                check(place.body == station.strNameParent, "A regional place names the game's body: " + pair.Key);
            }
            else
            {
                check(!station.bIsRegion && places.IsRegional(place.within), "A part lies within a regional place and is not itself regional: " + pair.Key);
                check(station.strPublicName == null || place.name == station.strPublicName, "A part keeps the game's public name: " + pair.Key + " = " + station.strPublicName);
            }
            check(place.factions.SequenceEqual(station.aFactions ?? Array.Empty<string>()), "A place lists the game's factions at home there: " + pair.Key);
            check(places.Find(place.station) == pair.Key && places.Find(place.station + "_X") == pair.Key, "A station id and its parts find their place: " + pair.Key);
        }
        check(regional == system.aSpawnStations.Count(s => s.bIsRegion), "Every regional station of the game has a place: " + regional);
        // The game members the grounding code reads exist where it expects them.
        check(typeof(CollisionManager).GetField("strATCClosest") != null && typeof(AIShipManager).GetField("strATCLast") != null, "The game's current-region fields exist");
        check(typeof(MathUtils).GetMethod("GetYearFromS") != null && typeof(MathUtils).GetMethod("GetMonthFromS") != null && typeof(MathUtils).GetMethod("GetDayOfMonthFromS") != null,
            "The game's calendar helpers exist");
        // Framework 0.115.0: the standing members and tiers the gates read, over the game's own factions.
        check(typeof(JsonFaction).GetMethod("GetFactionScore", new[] { typeof(string) }) != null && typeof(JsonFaction).GetMethod("ApplyFactionRep") != null &&
              typeof(JsonFaction).GetMethod("GetReputation") != null && typeof(CondOwner).GetMethod("GetAllFactions") != null, "The game's faction score, apply and tier members exist");
        foreach (var (score, tier) in new[] { (100f, "Honored"), (99.9f, "Trusted"), (75f, "Trusted"), (50f, "Friendly"), (25f, "Warm"), (0f, "Neutral"), (-49.9999f, "Neutral"), (-50f, "Dislikes") })
            check(JsonFaction.GetReputation(score).ToString() == tier && string.Equals(StoryRules.Tier(score), tier, StringComparison.OrdinalIgnoreCase), "The tier thresholds agree with the game's at " + score + ": " + JsonFaction.GetReputation(score));
        var factions = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(file)).First(s => (string?)s["strName"] == "NewGame")["aFactions"];
        var names = new HashSet<string>(factions!.Select(f => (string)f["strName"]!), StringComparer.Ordinal);
        foreach (var pair in frameworkPack.places) foreach (var faction in pair.Value.factions) check(names.Contains(faction), "A place's faction is one of the game's: " + faction);
        check(MathUtils.GetYearFromS(system.dfEpoch) >= 2070 && MathUtils.GetMonthFromS(system.dfEpoch) is >= 1 and <= 12 && MathUtils.GetDayOfMonthFromS(system.dfEpoch) is >= 1 and <= 31,
            "The game's start date reads as a calendar date: " + MathUtils.GetYearFromS(system.dfEpoch));
    }
}

/// <summary>Where the game's data folder is, for story checks that read it directly.</summary>
internal static class StoryNativeData
{
    internal static string Native = "";
}
