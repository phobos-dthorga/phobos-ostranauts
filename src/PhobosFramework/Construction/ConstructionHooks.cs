// Exact selection, native material fetching and completion gating are adapted
// from OCF 0.8.71. Phobos adds ownership, mass checks and explicit legacy mapping.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Construction;

internal static class ConstructionHooks
{
    internal static ConditionalWeakTable<Interaction, CompletionGate> Completions = new ConditionalWeakTable<Interaction, CompletionGate>();
    internal static void ResetAll() => Completions = new ConditionalWeakTable<Interaction, CompletionGate>();
    // Native enumeration includes stack neighbours. They are separate material
    // units, not contents that would be destroyed inside the selected item.
    internal static bool HasNoContents(CondOwner co)
    {
        var stack = new HashSet<CondOwner>(co.aStack == null ? Enumerable.Empty<CondOwner>() : co.aStack);
        return co.GetCOsSafe(true).All(stack.Contains);
    }
    internal static void Translate(ref string name, ref JsonInteractionSave save)
    {
        string resolved = ConstructionRegistry.ResolveAction(name);
        if (resolved == name) return;
        name = resolved;
        if (save == null) return;
        save = save.Clone(); // Never edit the source save DTO or files.
        save.strName = resolved;
        save.strChainStart = ConstructionRegistry.ResolveAction(save.strChainStart);
    }

    internal static bool Validate(Interaction action, ConstructionRegistry.Registered entry)
    {
        if (!ConstructionRegistry.Ready(entry.Owner) || action.objUs == null || action.objThem == null ||
            !entry.Stations.Contains(action.objThem.strCODef) || !action.objThem.HasCond("IsInstalled") || action.objThem.HasCond("IsDamaged")) return false;
        bool fetch = action.bGetItemBefore;
        try
        {
            // At completion, all materials must actually have arrived. Never credit planned hauling.
            action.bGetItemBefore = false;
            if (!action.Triggered(action.objUs, action.objThem, false, false, true, false, null)) return false;
        }
        finally { action.bGetItemBefore = fetch; }
        var contract = action.aLootItemRemoveContract;
        if (contract == null || contract.Distinct().Count() != contract.Count || contract.Any(co => co == null || co.bDestroyed)) return false;
        var units = contract.Select(co => new InputUnit(co.strCODef, co.GetCondAmount("StatMass"),
            HasNoContents(co), co.coStackHead == null && (co.aStack == null || co.aStack.Count == 0))).ToArray();
        return RecipeRules.MatchesInputs(entry.Recipe, units);
    }
}

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetInteraction))]
internal static class LegacyConstructionLookup
{
    private static void Prefix(ref string __0, ref JsonInteractionSave __1) => ConstructionHooks.Translate(ref __0, ref __1);
}

[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.TryGetInteraction))]
internal static class LegacyConstructionTryLookup
{
    private static void Prefix(ref string __0, ref JsonInteractionSave __2) => ConstructionHooks.Translate(ref __0, ref __2);
}

[HarmonyPatch(typeof(CondTrigger), "Triggered", new[] { typeof(CondOwner), typeof(string), typeof(bool) })]
internal static class ConstructionSelection
{
    private static void Postfix(CondTrigger __instance, CondOwner objOwner, ref bool __result)
    {
        if (!__result || __instance?.strName == null) return;
        if (ConstructionRegistry.Selectors.TryGetValue(__instance.strName, out var ingredient))
        {
            __result = objOwner != null && !objOwner.bDestroyed && objOwner.strCODef == ingredient.item
                && RecipeRules.MassMatches(objOwner.GetCondAmount("StatMass"), ingredient.unitMassKg)
                && ConstructionHooks.HasNoContents(objOwner);
            if (__result && objOwner != null && ingredient.requireEmpty)
                __result = objOwner.coStackHead == null && (objOwner.aStack == null || objOwner.aStack.Count == 0);
        }
        else if (ConstructionRegistry.StationSelectors.TryGetValue(__instance.strName, out var entry))
            __result = objOwner != null && ConstructionRegistry.Ready(entry.Owner) && entry.Stations.Contains(objOwner.strCODef);
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ResetObject))]
internal static class ConstructionReset
{
    private static void Postfix(Interaction __instance) => ConstructionHooks.Completions.Remove(__instance);
}

[HarmonyPatch(typeof(Interaction), "ApplyEffects")]
internal static class ConstructionCompletion
{
    private static bool Prefix(Interaction __instance, bool isCancelIa)
    {
        if (__instance.strName == null || !__instance.strName.StartsWith(RecipeRules.ActionPrefix, StringComparison.Ordinal)) return true;
        if (!ConstructionRegistry.Recipes.TryGetValue(__instance.strName, out var entry)) return false;
        try
        {
            bool result = ConstructionHooks.Completions.GetOrCreateValue(__instance)
                .TryBegin(isCancelIa, () => ConstructionHooks.Validate(__instance, entry));
            if (!result && !isCancelIa) FrameworkLifecycle.Log("Construction blocked or already applied: " + __instance.strName + ". Check station, arrived materials, contents and unit mass.");
            return result;
        }
        catch (Exception ex) { FrameworkLifecycle.Log("Construction blocked before native effects: " + ex); return false; }
    }

    private static Exception? Finalizer(Interaction __instance, Exception? __exception)
    {
        if (__exception != null && __instance.strName?.StartsWith(RecipeRules.ActionPrefix, StringComparison.Ordinal) == true)
            FrameworkLifecycle.Log("Native construction failed after effects may have started; replay is blocked. Inspect the test save before retrying: " + __exception);
        return __exception;
    }
}
