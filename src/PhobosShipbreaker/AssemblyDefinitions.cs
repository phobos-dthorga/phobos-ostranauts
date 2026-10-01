using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>The D4, R4 and F6 are bought or found whole (owner direction, 1 October 2026: assembling a machine from
/// several identical sections was arbitrary and confusing). Each machine's own Install job owns INSTALL, APPS and shows
/// the construction stages; the section jobs stay registered only so saved section sites finish or cancel, and saved
/// sections convert through Framework's legacy sweep: complete sets into the whole machine, leftovers into the exact
/// bill of their section recipe.</summary>
internal static class AssemblyDefinitions
{
    // Keep the original table contracts for already-queued jobs. New work uses native sites.
    internal static readonly string[] Legacy = { "PhobosBuildShipbreaker", "PhobosBuildReclaimer", "PhobosBuildFurnace" };
    // Shipbreaker 0.60.0: sections are no longer made; saved queues keep their exact contracts.
    internal static readonly string[] RetiredSectionRecipes = { "PhobosBuildShipbreakerSection", "PhobosBuildShipbreakerSectionCast",
        "PhobosBuildReclaimerSection", "PhobosBuildReclaimerSectionCast", "PhobosBuildFurnaceSection" };
    /// <summary>Each retired section, its count per machine and the standard recipe whose bill a leftover returns to.</summary>
    internal static readonly (string Prefix, string Section, int Count, double SectionKg, double MachineKg, int TableSeconds, string Recipe)[] Specs =
    {
        (Content.Prefix, ProcessRules.AssemblySection, 2, ProcessRules.AssemblySectionKg, ProcessRules.MachineKg, 1800, "PhobosBuildShipbreakerSection"),
        (ReclaimerRules.Prefix, ReclaimerRules.Section, 2, ReclaimerRules.SectionKg, ReclaimerRules.MachineKg, 2700, "PhobosBuildReclaimerSection"),
        (FurnaceRules.Prefix, FurnaceRules.Section, 3, FurnaceRules.SectionKg, FurnaceRules.MachineKg, 2400, "PhobosBuildFurnaceSection")
    };

    internal static void Add(NativeDefinitions d)
    {
        foreach (var spec in Specs)
        {
            var economy = EquipmentEconomy.Machines.Single(s => s.Prefix == spec.Prefix);
            // Native five progress units per .001 game hour, plus the existing mounting target.
            // Native skill/tool modifiers still apply; this is not a wall-clock promise.
            double progress = economy.Install + spec.TableSeconds * 5 / 3.6;
            string materialTrigger = spec.Prefix == Content.Prefix ? "PhobosShipbreakerTSection" :
                spec.Prefix == ReclaimerRules.Prefix ? "PhobosScrapReclaimerTSection" : "PhobosFurnaceSectionTrigger";
            SectionAssembly.Add(d, spec.Prefix + "SectionAssembly", spec.Section, materialTrigger, spec.Prefix + "Installed",
                spec.Count, spec.SectionKg, progress, new[] { "TIsToolMortorq", "TIsToolSoldering" });
            var appearance = Appearance(spec.Prefix);
            SectionAssembly.SetAppearance(spec.Prefix + "SectionAssembly", appearance);
            // The stage art follows the whole machine's own Install site, so it stays in ordinary play.
            SectionAssembly.SetWholeAppearance(spec.Prefix + "LooseInstall", spec.Prefix + "Loose", spec.Prefix + "Installed", appearance);
        }
    }

    internal static SectionAssemblyAppearance Appearance(string prefix)
    {
        const string art = "phobos/shipbreaker/";
        return prefix == Content.Prefix
            ? new SectionAssemblyAppearance(art + "PhobosShipbreakerSection", art + "ConstructionD4Mid", 64)
            : prefix == ReclaimerRules.Prefix
                ? new SectionAssemblyAppearance(art + "PhobosReclaimerSectionDedicated", art + "ConstructionR4Mid", 64)
                : new SectionAssemblyAppearance(art + "ConstructionF6Early", art + "ConstructionF6Mid", 96);
    }

    internal static void FinishRegistration(Action<string> log)
    {
        SectionAssembly.RetireFromMenu();
        SectionAssembly.RetireTableOffers(Legacy.Concat(RetiredSectionRecipes).ToArray());
        foreach (var spec in Specs)
        {
            try
            {
                var recipe = ConstructionRegistry.Find(spec.Recipe) ?? throw new InvalidOperationException(Text.Get("Assembly.recipe_missing", spec.Recipe));
                LegacyItemConversions.Register(spec.Section, spec.SectionKg, spec.Count, spec.Prefix + "Loose", spec.MachineKg,
                    recipe.ingredients.Select(i => new LegacyItemConversions.Material(i.item, i.count, i.unitMassKg)).ToArray());
            }
            // Saved sections then simply stay as they are; nothing else depends on the conversion.
            catch (Exception e) { log(Text.Get("Assembly.conversion_unavailable", spec.Section, e.Message)); }
        }
    }
}
