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
    internal const double DryKg = 20, Price = 250;
    internal static bool IsBench(CondOwner co) => co.strCODef.StartsWith(Bench, StringComparison.Ordinal);
    internal static readonly string[] Actions = { "recover-crop", "formulate-nutrients", "start", "pause", "cancel-workup" };
    internal static void Add(NativeDefinitions d)
    {
        ApplianceDefinitions.Add(d, Bench, Text.Get("workup_bench"), Text.Get("workup_bench_desc"), 2, DryKg, Price, "phobos/agriculture/Workup", Definitions.Controls, .02);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool broken = form.EndsWith("Dmg"); string id = Bench + form;
            if (broken)
            {
                d.Installables[id + "Repair"].aInputs = new[] { "TIsPartsMechSmall=1x1", "TIsPartsElecSmall=1x1", "TIsScrapAluminum=1x1" };
                MaintenanceDefinitions.SetStat(d.Objects[id], "StatRepairProgressMax", 1800);
            }
            int steel = broken ? 1 : 3;
            string waste = Bench + (broken ? "Broken" : "") + "HousingWaste";
            if (!d.Objects.ContainsKey(waste)) MaintenanceDefinitions.Remainder(d, waste, Text.Get("housing_waste"), DryKg - steel);
            MaintenanceDefinitions.Dismantle(d, id, 600, Enumerable.Repeat("ItmScrapSteel", steel).Concat(new[] { waste }).ToArray());
            if (form == "Installed") d.Objects[id].aInteractions = d.Objects[id].aInteractions.Concat(new[] { "recover-crop", "formulate-nutrients" }.Select(Definitions.WorkId)).ToArray();
        }
        foreach (var stock in new[] { (Residue,"recorded_residue","residue",.5,.01), (Concentrate,"concentrate","nutrients",.004,.01),
            (Spent,"spent_biomass","recovery_reject",.5,.01), (Makeup,"makeup","nutrients",NutrientRecovery.MakeupKg,NutrientRecovery.MakeupPrice),
            (Mixture,"mixture","nutrients",.008,.01) })
            Definitions.Stock(d, stock.Item1, stock.Item4, stock.Item5, stock.Item2, false, stock.Item3);
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv" })
        foreach (string item in new[] { Bench + "Loose", Makeup })
            MarketStock.Add(d, merchant, "PhobosAgricultureStock_" + merchant + "_" + item, item, .65, StockCondition.Pristine, StockQuantities.For(item));
    }
}
