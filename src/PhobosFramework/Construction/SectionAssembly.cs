using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>Finite section bills on native placeholders: native hauling, lots, cancellation and saves.</summary>
public static class SectionAssembly
{
    private sealed class Contract
    {
        internal string Section = "", Output = "", Selector = "";
        internal int Count;
        internal double Mass, Progress;
        internal SectionAssemblyAppearance? Appearance;
    }

    /// <summary>Opt-in marker artwork only; neither stage grants construction permission.</summary>
    public static void SetAppearance(string jobId, SectionAssemblyAppearance appearance)
    {
        if (!Jobs.TryGetValue(jobId, out var contract)) throw new ArgumentException("Unknown section assembly: " + jobId);
        contract.Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
    }

    internal static bool TryAppearance(Placeholder marker, out SectionAssemblyAppearance? appearance, out bool intermediate)
    {
        appearance = null;
        intermediate = false;
        if (marker == null || marker.strInstallIA == null || !marker.strInstallIA.StartsWith("ACT", StringComparison.Ordinal) ||
            !Jobs.TryGetValue(marker.strInstallIA.Substring(3), out var contract) ||
            marker.strInstalledCO != contract.Output || contract.Appearance == null) return false;
        var site = marker.GetComponent<CondOwner>();
        if (site == null || site.bDestroyed || site.Item == null || !site.Item.bPlaceholder) return false;
        appearance = contract.Appearance;
        // The native lot and work conditions survive reload. Do not save a parallel visual state.
        double progress = site.GetCondAmount("StatInstallProgress");
        intermediate = progress > 0 && !double.IsNaN(progress) && !double.IsInfinity(progress) && HasCompleteBill(contract, site);
        return true;
    }

    private static bool HasCompleteBill(Contract contract, CondOwner site)
    {
        var parts = site.GetLotCOs(false);
        if (parts.Count != contract.Count) return false;
        for (int i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (part == null || part.objCOParent != site || !ValidUnit(contract, part)) return false;
            for (int j = 0; j < i; j++) if (ReferenceEquals(parts[j], part)) return false;
        }
        return site.GetCOsSafe(true).Count == 0;
    }
    private static readonly Dictionary<string, Contract> Jobs = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Contract> Selectors = new(StringComparer.Ordinal);
    private static ConditionalWeakTable<Interaction, CompletionGate> finishes = new();
    internal static void Reset() { Jobs.Clear(); Selectors.Clear(); finishes = new(); }

    public static void Add(NativeDefinitions d, string jobId, string section, string sectionTrigger, string installed, int count,
        double unitMass, double workProgress, string[] tools)
    {
        RecipeRules.Identifier(jobId);
        if (count < 2 || count > RecipeRules.MaxUnits || !RecipeRules.MassMatches(unitMass,
            EquipmentSaveUpgrade.Amount(d.Objects[section].aStartingConds, "StatMass")) || !RecipeRules.MassMatches(unitMass * count,
            EquipmentSaveUpgrade.Amount(d.Objects[installed].aStartingConds, "StatMass")) || workProgress <= 0 ||
            double.IsNaN(workProgress) || double.IsInfinity(workProgress)) throw new ArgumentException("Invalid section assembly bill: " + jobId);
        string selector = jobId + "Material";
        d.Triggers[selector] = ConstructionRegistry.Trigger(selector, Array.Empty<string>(), new[] { "IsInstalled", "IsDamaged" });
        d.Triggers[selector].aTriggers = new[] { sectionTrigger };
        var contract = new Contract { Section = section, Output = installed, Selector = selector,
            Count = count, Mass = unitMass, Progress = workProgress };
        Jobs[jobId] = contract;
        Selectors[selector] = contract;
        MaintenanceDefinitions.SetStat(d.Objects[section], "StatInstallProgressMax", workProgress);
        d.Installables[jobId] = new JsonInstallable {
            strName = jobId, strActionCO = section, strActionGroup = "Work", strJobType = "install",
            strInteractionName = "Install", strInteractionTemplate = "ACTInstallNoSparksTEMP",
            strStartInstall = installed, strBuildType = InstallMenu.Appliances, CTThem = selector,
            aInputs = new[] { selector + "=1x" + count }, aToolCTsUse = tools.ToArray(), aLootCOs = new[] { installed },
            fDuration = .001f, fTargetPointRange = 2, strAllowLootCTsUs = "CTWorkProgressMISC",
            strAllowLootCTsThem = "CONDInstallProgressx5", strProgressStat = "StatInstallProgress",
            strCTThemMultCondUs = "StatInstallRateMISC", strCTThemMultCondTools = "IsToolMortorq"
        };
    }

    /// <summary>Choose assembly in INSTALL deterministically; a whole machine keeps its direct Install action.</summary>
    public static void PreferAssemblyMenu()
    {
        foreach (var pair in Jobs)
        {
            if (!DataHandler.dictInstallables.TryGetValue(pair.Key, out var job)) continue;
            Installables.dictJobBuildOptions[job.strBuildType][job.strStartInstall] = job;
            Installables.dictJobBuildOptionsListed[job.strBuildType][job.strStartInstall] = job;
        }
    }
    /// <summary>Retain saved table actions and their exact old contracts, but retire new menu offers.</summary>
    public static void RetireTableOffers(params string[] recipeIds)
    {
        var actions = new HashSet<string>(recipeIds.Select(RecipeRules.ActionId), StringComparer.Ordinal);
        foreach (var definition in DataHandler.dictCOs.Values)
            if (definition.aInteractions?.Any(actions.Contains) == true)
                definition.aInteractions = definition.aInteractions.Where(a => !actions.Contains(a)).ToArray();
    }
    internal static bool Select(string? trigger, CondOwner item, bool previous)
    {
        // Native callers also use unnamed, inline conditions. Only registered
        // assembly selectors belong to us; leave every other native result alone.
        if (string.IsNullOrEmpty(trigger)) return previous;
        return !Selectors.TryGetValue(trigger, out var c) ? previous : previous && ValidUnit(c, item);
    }
    private static bool ValidUnit(Contract c, CondOwner item) => item != null && !item.bDestroyed &&
        item.strCODef == c.Section && RecipeRules.MassMatches(item.GetCondAmount("StatMass"), c.Mass) &&
        !item.HasCond("IsInstalled") && !item.HasCond("IsDamaged") && ConstructionHooks.HasNoContents(item) &&
        item.GetLotCOs(true).Count == 0 && item.coStackHead == null && item.aStack.Count == 0;
    internal static void Initialize(Placeholder? placeholder)
    {
        if (placeholder == null || placeholder.strInstallIA == null || !placeholder.strInstallIA.StartsWith("ACT", StringComparison.Ordinal) ||
            !Jobs.TryGetValue(placeholder.strInstallIA.Substring(3), out var c)) return;
        // An old section may have no install-max condition. Change only its new native placeholder.
        var site = placeholder.GetComponent<CondOwner>();
        if (site == null) return;
        site.SetCondAmount("StatInstallProgressMax", c.Progress);
        if (c.Appearance != null) SectionAssemblyView.Attach(placeholder);
    }
    internal static bool Finish(Interaction action, bool cancelled)
    {
        if (action.strName == null || !action.strName.StartsWith("MS", StringComparison.Ordinal) ||
            !Jobs.TryGetValue(action.strName.Substring(2), out var c)) return true;
        return finishes.GetOrCreateValue(action).TryBegin(cancelled, () => {
            var site = action.objUs;
            if (site == null || site.bDestroyed || site.ship == null || (int)site.ship.LoadState < 2) return false;
            var marker = site.GetComponent<Placeholder>();
            if (marker == null || marker.strInstalledCO != c.Output || marker.strInstallIA != "ACT" + action.strName.Substring(2)) return false;
            return HasCompleteBill(c, site);
        });
    }
    internal static void ResetInteraction(Interaction action) => finishes.Remove(action);
}

[HarmonyPatch(typeof(CondTrigger), "Triggered", new[] { typeof(CondOwner), typeof(string), typeof(bool) })]
internal static class SectionAssemblySelection
{
    private static void Postfix(CondTrigger __instance, CondOwner objOwner, ref bool __result) =>
        __result = SectionAssembly.Select(__instance.strName, objOwner, __result);
}
[HarmonyPatch(typeof(DataHandler), nameof(DataHandler.GetCOPlaceholder))]
internal static class SectionAssemblyPlaceholder
{
    private static void Postfix(CondOwner __result) => SectionAssembly.Initialize(__result == null ? null : __result.GetComponent<Placeholder>());
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class SectionAssemblyCompletion
{
    private static bool Prefix(Interaction __instance, bool isCancelIa) => SectionAssembly.Finish(__instance, isCancelIa);
}
[HarmonyPatch(typeof(Interaction), nameof(Interaction.ResetObject))]
internal static class SectionAssemblyReset
{
    private static void Postfix(Interaction __instance) => SectionAssembly.ResetInteraction(__instance);
}
