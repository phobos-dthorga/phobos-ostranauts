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
    /// <summary>The B2's tray: one job's inputs and its two products.</summary>
    internal static readonly InventorySpec BenchInventory = InventorySpec.ProductTray(8, 8);
    internal static void Add(NativeDefinitions d)
    {
        ApplianceDefinitions.Add(d, Bench, Text.Get("workup_bench"), Text.Get("workup_bench_desc"), 2, DryKg, AgricultureEconomy.Price(Bench), "phobos/agriculture/Workup", Definitions.Controls, .02);
        EquipmentInventory.Apply(d, Bench, BenchInventory);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = Bench + form;
            if (form == "Installed") d.Objects[id].aInteractions = d.Objects[id].aInteractions.Concat(new[] { "recover-crop", "formulate-nutrients" }.Select(Definitions.WorkId)).ToArray();
        }
        foreach (var stock in new[] { (Residue, "recorded_residue"), (Concentrate, "concentrate"), (Spent, "spent_biomass"), (Makeup, "makeup"), (Mixture, "mixture") })
            Definitions.Stock(d, stock.Item1, stock.Item2);
    }
}
