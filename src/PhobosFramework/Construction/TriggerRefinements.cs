using System;
using System.Collections.Generic;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>The one hook on the game's condition-trigger check, the most frequently called native method our mods
/// touch. Registered selector triggers (construction ingredients, station selectors, section assembly units) refine
/// a true native result to false when the object does not qualify; everything else returns before any lookup. Gating
/// stays on registered names, never on name patterns, because an unknown trigger name resolves to the game's own
/// always-true Blank trigger (vanilla-precedence rule).</summary>
public static class TriggerRefinements
{
    private static readonly Dictionary<string, Func<CondOwner?, bool>> refinements = new(StringComparer.Ordinal);
    static TriggerRefinements() { SectionAssembly.SelectorsChanged = Rebuild; Rebuild(); }
    public static int Count => refinements.Count;
    internal static void Clear() => refinements.Clear();
    /// <summary>Rebuilds the table from every registry; called after each registration transaction.</summary>
    internal static void Rebuild()
    {
        refinements.Clear();
        foreach (var pair in ConstructionRegistry.Selectors)
        {
            var ingredient = pair.Value;
            refinements[pair.Key] = owner => owner != null && !owner.bDestroyed && owner.strCODef == ingredient.item
                && RecipeRules.MassMatches(owner.GetCondAmount("StatMass"), ingredient.unitMassKg) && ConstructionHooks.HasNoContents(owner)
                && (!ingredient.requireEmpty || owner.coStackHead == null && (owner.aStack == null || owner.aStack.Count == 0));
        }
        foreach (var pair in ConstructionRegistry.StationSelectors)
        {
            var entry = pair.Value;
            refinements[pair.Key] = owner => owner != null && ConstructionRegistry.Ready(entry.Owner) && entry.Stations.Contains(owner.strCODef);
        }
        SectionAssembly.AddRefinements(refinements);
    }
    /// <summary>A true native result refined by the registered selector for <paramref name="trigger"/>, if any.</summary>
    public static bool Refine(string? trigger, CondOwner? owner, bool result)
    {
        if (!result || trigger == null || refinements.Count == 0) return result;
        return refinements.TryGetValue(trigger, out var refine) ? refine(owner) : result;
    }
}

[HarmonyPatch(typeof(CondTrigger), "Triggered", new[] { typeof(CondOwner), typeof(string), typeof(bool) })]
internal static class TriggerRefinementPatch
{
    private static void Postfix(CondTrigger __instance, CondOwner objOwner, ref bool __result)
    {
        if (!__result || TriggerRefinements.Count == 0 || __instance == null) return;
        __result = TriggerRefinements.Refine(__instance.strName, objOwner, true);
    }
}
