using System;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Story;

/// <summary>Story news on the game's TVs (Framework 0.107.0). The TV asks the game for one headline or advert at a
/// time; a queued bulletin goes first, and otherwise a share of picks (30% by default) comes from the story entries
/// eligible at the last check. The rest stay the game's own, so its news is never drowned out. Nothing about a pick
/// is saved by the game.</summary>
internal static class StoryNews
{
    internal static JsonHeadline Headline(JsonHeadline original)
    {
        var library = StoryContent.Library;
        if (library.Broadcasts.Count == 0) return original;
        string? id = StoryArcs.NextBulletin();
        bool fromPool = false;
        if (id == null && StoryArcs.BroadcastPool.Count > 0 && StoryArcs.Roll() < library.Settings.broadcastShare)
        { id = StoryRules.Pick(StoryArcs.BroadcastPool, StoryArcs.Roll()); fromPool = true; }
        if (id == null || !library.Broadcasts.TryGetValue(id, out var entry)) return original;
        StoryArcs.Shown(id, entry.Value.once || entry.Value.onceEach, fromPool ? StoryArcs.BroadcastPool : null);
        // One of its variants (Framework 0.132.0), never the same as last time when it has more than one.
        int variant = VariantPicks.Next("news:" + id, entry.Value.text.Count, StoryArcs.Roll());
        // The "Region News:" label: the item's own, else its place's (Framework 0.114.0).
        string? place = library.PlaceOf(entry.Value.thread, entry.Value.place);
        string region = entry.Value.region != null ? StoryContent.Words(entry.Owner, id + ".region", entry.Value.region) : library.Places.Region(place) ?? Text.Get("Story.these_parts");
        return new JsonHeadline
        {
            strName = StoryRules.TestPrefix + id,
            strRegion = region,
            strDesc = StoryArcs.Fill(StoryContent.Words(entry.Owner, id + ".text", entry.Value.text, variant), place)
        };
    }

    internal static JsonAd Advert(JsonAd original)
    {
        var library = StoryContent.Library;
        if (StoryArcs.AdvertPool.Count == 0 || !(StoryArcs.Roll() < library.Settings.advertShare)) return original;
        string? id = StoryRules.Pick(StoryArcs.AdvertPool, StoryArcs.Roll());
        if (id == null || !library.Adverts.TryGetValue(id, out var entry)) return original;
        StoryArcs.Shown(id, entry.Value.once || entry.Value.onceEach, StoryArcs.AdvertPool);
        int variant = VariantPicks.Next("advert:" + id, entry.Value.text.Count, StoryArcs.Roll());
        return new JsonAd { strName = StoryRules.TestPrefix + id, strDesc = StoryArcs.Fill(StoryContent.Words(entry.Owner, id + ".text", entry.Value.text, variant), library.PlaceOf(entry.Value.thread, entry.Value.place)) };
    }
}

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetHeadline))]
internal static class StoryHeadlinePatch
{
    private static void Postfix(ref JsonHeadline __result)
    {
        try { __result = StoryNews.Headline(__result); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetAd))]
internal static class StoryAdvertPatch
{
    private static void Postfix(ref JsonAd __result)
    {
        try { __result = StoryNews.Advert(__result); }
        catch (Exception ex) { FrameworkLifecycle.Log(Text.Get("Story.check_failed", ex.Message)); }
    }
}
