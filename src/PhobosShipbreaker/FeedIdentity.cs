using Phobos.Ostranauts.Framework.Crew;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Which feed family a loose part belongs to, by the game's base definition (cosmetic overlays resolve to it,
/// see WallIdentity), and whether this particular part can go into the D4 now.</summary>
internal static class FeedIdentity
{
    internal static FeedFamily? Family(string? definition) => FeedFamilies.Find(WallIdentity.Base(definition));
    internal static FeedFamily? Family(CondOwner? part) => part == null ? null : Family(part.strCODef);
    /// <summary>A detached, single, empty part of an accepted family at a mass the family has a recipe for.</summary>
    internal static bool ValidFeed(CondOwner? part) => part != null && !part.bDestroyed && Family(part) is FeedFamily family &&
        !part.HasCond("IsInstalled") && part.coStackHead == null && (part.aStack == null || part.aStack.Count == 0) &&
        part.GetCOsSafe(true).Count == 0 && family.Accepts(part.GetTotalMass());
    /// <summary>Crew admission of one unit before it is detached from its stack: its own mass, not the stack's.</summary>
    internal static bool UnitFeed(CondOwner part) => CrewLogistics.Loose(part) && Family(part) is FeedFamily family && family.Accepts(part.GetCondAmount("StatMass"));
    internal static string Label(FeedFamily family) => Text.Get("Feed.family_" + family.Key);
    internal static string Problem(CondOwner part)
    {
        var family = Family(part);
        if (family == null) return Text.Get("ProcessingService.is_not_a_supported_ordinary_wall", part.strNameFriendly, part.strCODef);
        if (part.HasCond("IsInstalled")) return Text.Get("ProcessingService.detach_the_wall_first");
        if (part.coStackHead != null || part.aStack.Count != 0) return Text.Get("ProcessingService.separate_the_wall_stack_first");
        if (part.GetCOsSafe(true).Count != 0) return Text.Get("ProcessingService.remove_anything_attached_to_or_stored_in");
        return Text.Get("ProcessingService.no_recipe_for_family_mass", part.strNameFriendly, part.GetTotalMass(), Label(family), family.MinKg, family.MaxKg,
            Text.Get(family.StepKg < 1 ? "Feed.step_half" : "Feed.step_whole"));
    }
}
