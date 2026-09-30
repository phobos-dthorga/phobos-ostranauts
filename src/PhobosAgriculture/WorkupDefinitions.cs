using System;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal static class WorkupDefinitions
{
    internal const string Bench = "PhobosVerdemorrowGroundworkB2", Residue = "PhobosVerdemorrowRecordedCropResidue",
        Concentrate = "PhobosVerdemorrowRecoveredConcentrate", Spent = "PhobosVerdemorrowSpentBiomass",
        Makeup = "PhobosVerdemorrowGroundworkMakeup", Mixture = "PhobosVerdemorrowGroundworkMixture";
    internal const double DryKg = 20;
    internal static bool IsBench(CondOwner co) => co.strCODef.StartsWith(Bench, StringComparison.Ordinal);
    internal static readonly string[] Actions = { "recover-crop", "formulate-nutrients", "start", "pause", "cancel-workup" };
    internal static void Add(NativeDefinitions d)
    {
        ApplianceDefinitions.Add(d, Bench, Text.Get("workup_bench"), Text.Get("workup_bench_desc"), 2, DryKg, AgricultureEconomy.Price(Bench), "phobos/agriculture/Workup", Definitions.Controls, .02);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = Bench + form;
            if (form == "Installed") d.Objects[id].aInteractions = d.Objects[id].aInteractions.Concat(new[] { "recover-crop", "formulate-nutrients" }.Select(Definitions.WorkId)).ToArray();
        }
        foreach (var stock in new[] { (Residue,"recorded_residue","recorded_residue",.5,.01), (Concentrate,"concentrate","concentrate",.004,.01),
            (Spent,"spent_biomass","spent_biomass",.5,.01), (Makeup,"makeup","makeup",NutrientRecovery.MakeupKg,NutrientRecovery.MakeupPrice),
            (Mixture,"mixture","mixture",.008,.01) })
            Definitions.Stock(d, stock.Item1, stock.Item4, stock.Item5, stock.Item2, false, stock.Item3);
    }
}
