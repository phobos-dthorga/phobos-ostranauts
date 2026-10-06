using System;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Story lines in the game's own small talk (Framework 0.108.0). The game turns every social interaction into
/// text through <c>GrammarUtils.GenerateDescription</c>, from the interaction's own description. When the interaction
/// belongs to a story moment (<see cref="StoryMoments"/>), a share of uses (<c>chatterShare</c>) says an eligible story
/// line instead, behind the moment's lead-in, and that choice is kept for the same use, so the social log and the
/// conversation screen agree. Only the text changes: the game's interaction, its effects and the name in character
/// history stay its own, so nothing new is saved.</summary>
internal static class StoryChatter
{
    private sealed class Choice
    {
        public string Key = "";
        public string? Text;
    }
    private static ConditionalWeakTable<Interaction, Choice> memo = new();
    private static string? forced;

    internal static void Reset() { memo = new ConditionalWeakTable<Interaction, Choice>(); forced = null; }

    internal static string Describe(Interaction interaction, string original)
    {
        string? moment = StoryMoments.Of(interaction.strName);
        if (moment == null || StoryContent.Library.Lines.Count == 0) return original;
        var us = interaction.objUs;
        string key = interaction.strName + "|" + us?.strID + "|" + interaction.objThem?.strID;
        if (memo.TryGetValue(interaction, out var choice) && choice.Key == key) return choice.Text ?? original;
        memo.Remove(interaction);
        using (Diagnostics.Performance.Measure(Diagnostics.Performance.StoryChatter))
            choice = new Choice { Key = key, Text = Choose(moment, interaction, us) };
        memo.Add(interaction, choice);
        return choice.Text ?? original;
    }

    private static string? Choose(string moment, Interaction interaction, CondOwner? us)
    {
        var library = StoryContent.Library;
        StoryLine? line = null;
        if (forced != null && (line = library.Lines.FirstOrDefault(l => l.Id == forced && l.Moment == moment)) != null) forced = null;
        if (line == null)
        {
            if (!StoryArcs.ChatterPools.TryGetValue(moment, out var pool) || !(StoryArcs.Roll() < library.Settings.chatterShare)) return null;
            // A placed line (Framework 0.114.0) is said where it belongs: by crew while the player is there, by others there.
            bool crew = StoryArcs.Crew(us);
            var factions = us?.GetAllFactions() ?? new System.Collections.Generic.List<string>();
            line = StoryRules.PickLine(pool, l => StoryRules.Voices(l, crew, l.Place != null && StoryArcs.SpeakerAt(us, l.Place), l.Place != null && StoryArcs.NearPlace(l.Place), factions), StoryArcs.Roll());
            if (line == null) return null;
        }
        string said = StoryArcs.Fill(StoryContent.Words(line.Owner, line.Key, line.Text), line.Place);
        return GrammarUtils.GetInflectedString(Text.Get("Story.moment." + moment, said), interaction);
    }

    /// <summary>F3: the next use of this line's moment says it, whatever the share, speakers and requirements.</summary>
    internal static string Force(string id)
    {
        var line = StoryContent.Library.Lines.FirstOrDefault(l => l.Id == id);
        if (line == null) return Text.Get("Story.unknown_line", id);
        forced = id;
        return Text.Get("Story.line_forced", id, line.Moment, string.Join(", ", StoryMoments.Interactions[line.Moment]));
    }

    /// <summary>F3: the lines each moment may use now.</summary>
    internal static string Describe()
    {
        var pools = StoryArcs.ChatterPools;
        var lines = StoryMoments.Interactions.Keys.Select(m => Text.Get("Story.moment_line", m,
            pools.TryGetValue(m, out var pool) && pool.Count > 0 ? string.Join(", ", pool.Select(l => l.Id + (l.Speakers == StorySchema.Anyone ? "" : " (" + l.Speakers + ")"))) : Text.Get("Story.none")));
        return Text.Get("Story.chatter_title", StoryContent.Library.Settings.chatterShare) + "\n" + string.Join("\n", lines) +
            (forced != null ? "\n" + Text.Get("Story.forced_waiting", forced) : "");
    }
}

[HarmonyPatch(typeof(GrammarUtils), nameof(GrammarUtils.GenerateDescription), new[] { typeof(Interaction) })]
internal static class StoryChatterPatch
{
    private static void Postfix(Interaction interaction, ref string __result)
    {
        if (interaction == null) return;
        try { __result = StoryChatter.Describe(interaction, __result); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}

[HarmonyPatch(typeof(GrammarUtils), nameof(GrammarUtils.GenerateDescription), new[] { typeof(Interaction), typeof(bool) })]
internal static class StoryChatterLogPatch
{
    private static void Postfix(Interaction interaction, ref string __result)
    {
        if (interaction == null) return;
        try { __result = StoryChatter.Describe(interaction, __result); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}
