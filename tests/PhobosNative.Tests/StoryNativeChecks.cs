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
    }
}
