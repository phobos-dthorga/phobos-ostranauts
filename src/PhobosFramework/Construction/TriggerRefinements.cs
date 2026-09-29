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
    // The game evaluates about 400,000 triggers a second at speed 8 (30 September 2026 capture) and nearly none are
    // ours. Two reads (length, first character) rule a name out before any hashing; the table still decides the rest.
    private static readonly NameFilter names = new();
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
        names.Clear();
        foreach (var key in refinements.Keys) names.Add(key);
    }
    /// <summary>A true native result refined by the registered selector for <paramref name="trigger"/>, if any.</summary>
    public static bool Refine(string? trigger, CondOwner? owner, bool result)
    {
        if (!result || trigger == null || refinements.Count == 0 || !names.MayContain(trigger)) return result;
        return refinements.TryGetValue(trigger, out var refine) ? refine(owner) : result;
    }
}

/// <summary>An exact negative test for a set of strings: false means the name is certainly not in the set. It compares
/// only the length and the first character, so it costs two reads and never hashes.</summary>
public sealed class NameFilter
{
    private ulong lengths, firstLow, firstHigh;
    private bool firstOther, empty;
    public void Clear() { lengths = firstLow = firstHigh = 0; firstOther = empty = false; }
    public void Add(string name)
    {
        if (name == null) return;
        if (name.Length == 0) { empty = true; return; }
        lengths |= 1UL << Math.Min(name.Length, 63);
        char c = name[0];
        if (c < 64) firstLow |= 1UL << c; else if (c < 128) firstHigh |= 1UL << (c - 64); else firstOther = true;
    }
    public bool MayContain(string name)
    {
        int length = name.Length;
        if (length == 0) return empty;
        if ((lengths & (1UL << Math.Min(length, 63))) == 0) return false;
        char c = name[0];
        return c < 64 ? (firstLow & (1UL << c)) != 0 : c < 128 ? (firstHigh & (1UL << (c - 64))) != 0 : firstOther;
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
