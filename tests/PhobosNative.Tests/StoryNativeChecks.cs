using System;
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
        check(library.Chatter.Count == 6 && library.Tips.Count == 2 && library.Sections.Count == 2 && library.Articles.Count == 2 && library.Lines.Count == 9,
            "The seed's small talk (six lines and three news mentions), tips and articles load");
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
        check(nodes.Count == 4 && nodes.All(n => n.Parent == null || nodes.Any(p => p.Name == n.Parent)) && nodes.All(n => n.Name.StartsWith(StoryLore.NodePrefix, StringComparison.Ordinal)),
            "Both shared sections show with Agriculture's articles; every parent is ours and no name is the game's");
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
        check(stories.Count == 9 && all.Problems.Count == 0, "Phobos Spacer Stories loads whole against the game's data: " + string.Join("; ", all.Problems.Take(10)));
        check(all.Arcs.Count == library.Arcs.Count + 9 && all.Files.Count == library.Files.Count + 9 && all.Articles.Values.All(a => all.Sections.ContainsKey(a.Value.section)),
            "Its nine chains and nine files load, and every article sits in a known section");
        check(stories.SelectMany(s => s.Pack.broadcasts.Keys.Concat(s.Pack.adverts.Keys).Concat(s.Pack.chatter.Keys).Concat(s.Pack.tips.Keys).Concat(s.Pack.articles.Keys)
            .Concat(s.Pack.arcs.Keys).Concat(s.Pack.files.Keys)).All(id => id.StartsWith("spacertales-", StringComparison.Ordinal)), "Every id carries the add-on's own prefix");
    }
}
