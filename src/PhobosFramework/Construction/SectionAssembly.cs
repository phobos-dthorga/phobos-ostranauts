using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Construction;

/// <summary>Finite section bills on native placeholders: native hauling, lots, cancellation and saves. Since Framework
/// 0.67.0 (owner direction, 1 October 2026: machines come whole, never from several identical sections) section jobs
/// stay registered only so saved sites load, finish or cancel; <see cref="RetireFromMenu"/> hands INSTALL back to the
/// whole machine's own job, and the construction stages follow that job instead (<see cref="SetWholeAppearance"/>).</summary>
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

    /// <summary>The same construction stages on a whole machine's own Install site (Framework 0.67.0): early until the
    /// loose machine is delivered and work has started, intermediate after. Appearance only: the native job, its input
    /// and its completion are untouched.</summary>
    public static void SetWholeAppearance(string jobId, string whole, string installed, SectionAssemblyAppearance appearance)
    {
        if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(whole) || string.IsNullOrEmpty(installed) || Jobs.ContainsKey(jobId))
            throw new ArgumentException("Invalid whole-machine construction appearance: " + jobId);
        Wholes[jobId] = new Contract { Section = whole, Output = installed, Count = 1,
            Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance)) };
    }

    internal static bool TryAppearance(Placeholder marker, out SectionAssemblyAppearance? appearance, out bool intermediate)
    {
        appearance = null;
        intermediate = false;
        if (marker == null || marker.strInstallIA == null || !marker.strInstallIA.StartsWith("ACT", StringComparison.Ordinal)) return false;
        string jobId = marker.strInstallIA.Substring(3);
        bool whole = !Jobs.TryGetValue(jobId, out var contract);
        if (whole && !Wholes.TryGetValue(jobId, out contract)) return false;
        if (marker.strInstalledCO != contract!.Output || contract.Appearance == null) return false;
        var site = marker.GetComponent<CondOwner>();
        if (site == null || site.bDestroyed || site.Item == null || !site.Item.bPlaceholder) return false;
        appearance = contract.Appearance;
        // The native lot and work conditions survive reload. Do not save a parallel visual state.
        double progress = site.GetCondAmount("StatInstallProgress");
        intermediate = progress > 0 && !double.IsNaN(progress) && !double.IsInfinity(progress) &&
            (whole ? WholeDelivered(contract, site) : HasCompleteBill(contract, site));
        return true;
    }

    // A whole machine may carry its own inventory; only its identity and intact loose form matter to the stage.
    private static bool WholeDelivered(Contract contract, CondOwner site)
    {
        var parts = site.GetLotCOs(false);
        if (parts.Count != 1) return false;
        var part = parts[0];
        return part != null && !part.bDestroyed && part.objCOParent == site && part.strCODef == contract.Section &&
            !part.HasCond("IsInstalled") && !part.HasCond("IsDamaged");
    }

    /// <summary>Whether a construction site is a saved section site, and whether it can still finish: its full, valid
    /// bill is delivered. The legacy-item sweep leaves complete sites to the crew and cancels the rest.</summary>
    internal static bool IsSectionSite(CondOwner site, out bool complete)
    {
        complete = false;
        if (site == null || site.bDestroyed || site.Item == null || !site.Item.bPlaceholder) return false;
        var marker = site.GetComponent<Placeholder>();
        if (marker == null || marker.strInstallIA == null || !marker.strInstallIA.StartsWith("ACT", StringComparison.Ordinal) ||
            !Jobs.TryGetValue(marker.strInstallIA.Substring(3), out var contract) || marker.strInstalledCO != contract.Output) return false;
        complete = HasCompleteBill(contract, site);
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
    // The jobs by their finishing action's name ("MS" and the job id), so the effects hook, which sees every finished
    // action in the game, finds ours without building a string for each one (Framework 0.133.0, L105).
    private static readonly Dictionary<string, Contract> Finishes = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Contract> Wholes = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Contract> Selectors = new(StringComparer.Ordinal);
    private static ConditionalWeakTable<Interaction, CompletionGate> finishes = new();
    /// <summary>Raised when the selector table changes; the shared trigger hook rebuilds its table from it. A
    /// delegate rather than a direct call keeps this file compilable against the test doubles on its own.</summary>
    internal static Action SelectorsChanged = () => { };
    internal static void Reset() { Jobs.Clear(); Finishes.Clear(); Wholes.Clear(); Selectors.Clear(); finishes = new(); SelectorsChanged(); }
    /// <summary>Our assembly selectors, for the shared trigger hook: a true native result stands only for a valid unit.</summary>
    internal static void AddRefinements(Dictionary<string, Func<CondOwner?, bool>> into)
    {
        foreach (var pair in Selectors) { var c = pair.Value; into[pair.Key] = item => item != null && ValidUnit(c, item); }
    }

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
        Finishes["MS" + jobId] = contract;
        Selectors[selector] = contract;
        SelectorsChanged();
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

    /// <summary>Retires every section job from new work (Framework 0.67.0): INSTALL lists the whole machine's own job
    /// for the same installed form, whatever the generation order, and a section no longer offers Install. The job and
    /// its interactions stay registered, so saved sites load, finish and cancel exactly as before.</summary>
    public static void RetireFromMenu()
    {
        foreach (var pair in Jobs)
        {
            if (!DataHandler.dictInstallables.TryGetValue(pair.Key, out var job)) continue;
            var whole = DataHandler.dictInstallables.Values.Where(j => j != null && j.strName != pair.Key && !Jobs.ContainsKey(j.strName) &&
                j.strStartInstall == job.strStartInstall && j.strBuildType == job.strBuildType)
                .OrderBy(j => j.strName, StringComparer.Ordinal).FirstOrDefault();
            foreach (var menu in new[] { Installables.dictJobBuildOptions, Installables.dictJobBuildOptionsListed })
            {
                if (menu == null || job.strBuildType == null || !menu.TryGetValue(job.strBuildType, out var tab) || tab == null) continue;
                if (whole != null) tab[job.strStartInstall] = whole;
                else if (tab.TryGetValue(job.strStartInstall, out var listed) && listed?.strName == pair.Key) tab.Remove(job.strStartInstall);
            }
            if (DataHandler.dictCOs.TryGetValue(pair.Value.Section, out var section) && section.aInteractions != null)
                section.aInteractions = section.aInteractions.Where(a => a != "ACT" + pair.Key).ToArray();
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
    /// <summary>The assembly selector rule on its own (the live hook is <see cref="TriggerRefinements"/>): native
    /// callers also use unnamed, inline conditions, and only registered assembly selectors belong to us, so every
    /// other native result is left alone.</summary>
    internal static bool Select(string? trigger, CondOwner item, bool previous)
    {
        if (string.IsNullOrEmpty(trigger)) return previous;
        return !Selectors.TryGetValue(trigger, out var c) ? previous : previous && ValidUnit(c, item);
    }
    private static bool ValidUnit(Contract c, CondOwner item) => item != null && !item.bDestroyed &&
        item.strCODef == c.Section && RecipeRules.MassMatches(item.GetCondAmount("StatMass"), c.Mass) &&
        !item.HasCond("IsInstalled") && !item.HasCond("IsDamaged") && ConstructionHooks.HasNoContents(item) &&
        item.GetLotCOs(true).Count == 0 && item.coStackHead == null && item.aStack.Count == 0;
    internal static void Initialize(Placeholder? placeholder)
    {
        if (placeholder == null || placeholder.strInstallIA == null || !placeholder.strInstallIA.StartsWith("ACT", StringComparison.Ordinal)) return;
        // A whole machine's site keeps its native work target; it only gains the construction stages.
        if (Wholes.ContainsKey(placeholder.strInstallIA.Substring(3))) { SectionAssemblyView.Attach(placeholder); return; }
        if (!Jobs.TryGetValue(placeholder.strInstallIA.Substring(3), out var c)) return;
        // An old section may have no install-max condition. Change only its new native placeholder.
        var site = placeholder.GetComponent<CondOwner>();
        if (site == null) return;
        site.SetCondAmount("StatInstallProgressMax", c.Progress);
        if (c.Appearance != null) SectionAssemblyView.Attach(placeholder);
    }
    internal static bool Finish(Interaction action, bool cancelled)
    {
        if (action.strName == null || !Finishes.TryGetValue(action.strName, out var c)) return true;
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
