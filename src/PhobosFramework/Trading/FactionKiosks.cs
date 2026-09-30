using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>The reputation a faction kiosk asks before it sells an item, in the game's own order.</summary>
public enum FactionTier { Neutral, Warm, Friendly, Trusted, Honored }

/// <summary>Reputation gates for registered offers at the game's faction kiosks, which sell for faction scrip.
/// The native kiosk lists each item under the first of its tier triggers that matches (Neutral, Warm, Friendly,
/// Trusted, Honored) and locks tiers above the buyer's standing. A Phobos tier mark is required by its own tier's
/// trigger and forbidden by every lower one, so a marked item appears exactly at its tier, even when a native tag
/// (a nav board's control-systems category) would otherwise place it lower. The triggers are amended in place;
/// vanilla items carry no mark and keep their tiers. Scrip prices stay native: the item's credit price at the
/// faction's own conversion rate.</summary>
public static class FactionKiosks
{
    /// <summary>The game's tier triggers, in the order its kiosk tests them.</summary>
    public static readonly IReadOnlyList<string> NativeTriggers = new[]
        { "TIsFactionTierNeutral", "TIsFactionTierWarm", "TIsFactionTierFriendly", "TIsFactionTierTrusted", "TIsFactionTierHonored" };
    public const string MarkPrefix = "IsPhobosFactionTier";

    /// <summary>The hidden condition an offer stamps on its stock for a tier, or null for Neutral (no gate).</summary>
    public static string? Mark(FactionTier tier) => tier == FactionTier.Neutral ? null : MarkPrefix + tier;
    public static FactionTier Parse(string tier) => (FactionTier)Enum.Parse(typeof(FactionTier), tier);
    public static bool Known(string tier) => Enum.TryParse<FactionTier>(tier, false, out var parsed) && Enum.IsDefined(typeof(FactionTier), parsed) && parsed.ToString() == tier;

    /// <summary>The additions to one native tier trigger: its own mark joins its requirements (any one suffices), and
    /// every higher tier's mark joins its exclusions. Neutral has no mark of its own and excludes every mark.</summary>
    public static (string[] Requires, string[] Forbids) Amendment(FactionTier tier)
    {
        var mark = Mark(tier);
        var higher = Enum.GetValues(typeof(FactionTier)).Cast<FactionTier>().Where(t => t > tier).Select(t => Mark(t)!).ToArray();
        return (mark == null ? Array.Empty<string>() : new[] { mark }, higher);
    }

    // ContentLoaded: after every mod's data. The marks are Framework definitions; the native triggers are amended in place.
    internal static void Definitions()
    {
        var d = new NativeDefinitions();
        foreach (FactionTier tier in Enum.GetValues(typeof(FactionTier)))
        {
            string? mark = Mark(tier);
            if (mark == null) continue;
            d.Conditions[mark] = new JsonCond { strName = mark, strNameFriendly = Text.Get("FactionKiosks.mark_name", tier), strDesc = Text.Get("FactionKiosks.mark_description", tier),
                strColor = "Neutral", nDisplaySelf = 0, nDisplayOther = 0 };
        }
        d.Amend(AmendTriggers);
        d.Publish();
    }

    private static void AmendTriggers()
    {
        foreach (FactionTier tier in Enum.GetValues(typeof(FactionTier)))
        {
            // The loaded trigger itself: the game's lookup hands out clones, and amending one would change nothing.
            if (DataHandler.dictCTs == null || !DataHandler.dictCTs.TryGetValue(NativeTriggers[(int)tier], out var trigger) || trigger == null) { FrameworkLifecycle.Log(Text.Get("FactionKiosks.missing_trigger", NativeTriggers[(int)tier])); continue; }
            var (requires, forbids) = Amendment(tier);
            // The native tiers accept any one requirement. Appending to an empty list (anything not forbidden) or to an
            // all-of list would change what vanilla items the tier shows, so such a trigger is left alone.
            if (requires.Length > 0 && ((trigger.aReqs?.Length ?? 0) == 0 || trigger.bAND)) { FrameworkLifecycle.Log(Text.Get("FactionKiosks.unexpected_trigger", trigger.strName)); continue; }
            bool changed = Append(trigger, requires, forbids);
            if (changed) ForgetRelevantConditions(trigger);
        }
    }

    /// <summary>Appends missing names through the native setters, which flag the trigger as changed. Idempotent.</summary>
    public static bool Append(CondTrigger trigger, IEnumerable<string> requires, IEnumerable<string> forbids)
    {
        var reqs = trigger.aReqs ?? Array.Empty<string>(); var bans = trigger.aForbids ?? Array.Empty<string>();
        var newReqs = requires.Where(n => !reqs.Contains(n)).ToArray(); var newBans = forbids.Where(n => !bans.Contains(n)).ToArray();
        if (newReqs.Length > 0) trigger.aReqs = reqs.Concat(newReqs).ToArray();
        if (newBans.Length > 0) trigger.aForbids = bans.Concat(newBans).ToArray();
        return newReqs.Length > 0 || newBans.Length > 0;
    }

    // The loaded trigger caches the conditions it reads on first use; clones (what kiosks receive) start without it.
    // Plain reflection, resolved only when amending: no Harmony type initialisation for callers that only read marks.
    private static void ForgetRelevantConditions(CondTrigger trigger) =>
        typeof(CondTrigger).GetField("_aCondsRelevant", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(trigger, null);
}
