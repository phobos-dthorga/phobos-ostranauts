using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class AssemblyDefinitions
{
    // Keep the original table contracts for already-queued jobs. New work uses native sites.
    internal static readonly string[] Legacy = { "PhobosBuildShipbreaker", "PhobosBuildReclaimer", "PhobosBuildFurnace" };
    internal static void Add(NativeDefinitions d)
    {
        foreach (var spec in new[] {
            (Content.Prefix, ProcessRules.AssemblySection, 2, ProcessRules.AssemblySectionKg, 1800),
            (ReclaimerRules.Prefix, ReclaimerRules.Section, 2, ReclaimerRules.SectionKg, 2700),
            (FurnaceRules.Prefix, FurnaceRules.Section, 3, FurnaceRules.SectionKg, 2400) })
        {
            var economy = EquipmentEconomy.Machines.Single(s => s.Prefix == spec.Item1);
            // Native five progress units per .001 game hour, plus the existing mounting target.
            // Native skill/tool modifiers still apply; this is not a wall-clock promise.
            double progress = economy.Install + spec.Item5 * 5 / 3.6;
            string materialTrigger = spec.Item1 == Content.Prefix ? "PhobosShipbreakerTSection" :
                spec.Item1 == ReclaimerRules.Prefix ? "PhobosScrapReclaimerTSection" : "PhobosFurnaceSectionTrigger";
            SectionAssembly.Add(d, spec.Item1 + "SectionAssembly", spec.Item2, materialTrigger, spec.Item1 + "Installed",
                spec.Item3, spec.Item4, progress, new[] { "TIsToolMortorq", "TIsToolSoldering" });
            int count = spec.Item3;
            ItemInformation.Register(d, spec.Item1 + "AssemblyInformation", Text.Get("Assembly.title"), new[] { spec.Item2 },
                _ => Text.Get("Assembly.instructions", count, spec.Item4, count * spec.Item4));
        }
    }
    internal static void FinishRegistration()
    {
        SectionAssembly.PreferAssemblyMenu();
        SectionAssembly.RetireTableOffers(Legacy);
    }
}
