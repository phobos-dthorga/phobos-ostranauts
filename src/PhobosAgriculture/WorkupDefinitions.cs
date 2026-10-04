using System;
using System.Linq;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;

namespace PhobosAgriculture;

internal static class WorkupDefinitions
{
    internal const string Bench = "PhobosVerdemorrowGroundworkB2", Residue = "PhobosVerdemorrowRecordedCropResidue",
        Concentrate = "PhobosVerdemorrowRecoveredConcentrate", Spent = "PhobosVerdemorrowSpentBiomass",
        Makeup = "PhobosVerdemorrowGroundworkMakeup", Mixture = "PhobosVerdemorrowGroundworkMixture";
    /// <summary>The straw bale (Agriculture 0.44.0) and the condition Phobos Manufacturing's V4 admits it by.</summary>
    internal const string Bale = "PhobosVerdemorrowStrawBale", BaleIdentity = Bale + "Identity";
    internal const double DryKg = 20;
    internal static bool IsBench(CondOwner co) => co.strCODef.StartsWith(Bench, StringComparison.Ordinal);
    /// <summary>The bench's crew work: each is a one-minute crew action at the B2 and nowhere else.</summary>
    internal static readonly string[] Work = { "recover-crop", "formulate-nutrients", "scutch-flax", "bale-straw" };
    internal static bool IsWork(string action) => Array.IndexOf(Work, action) >= 0;
    /// <summary>The workup job each queuing action prepares, and back.</summary>
    internal static string? ModeOf(string action) => action == "recover-crop" ? "recover" : action == "formulate-nutrients" ? "formulate" : action == "scutch-flax" ? "scutch" : null;
    internal static string ActionOf(string mode) => mode == "recover" ? "recover-crop" : mode == "formulate" ? "formulate-nutrients" : "scutch-flax";
    internal static readonly string[] Actions = Work.Concat(new[] { "start", "pause", "cancel-workup", "empty-press" }).ToArray();
    /// <summary>The B2's tray: one job's residue and supplement, its two unstackable products, and two cells to spare
    /// (six cells, where it had sixty-four).</summary>
    internal static readonly InventorySpec BenchInventory = InventorySpec.ProductTray(3, 2);
    internal static void Add(NativeDefinitions d)
    {
        ApplianceDefinitions.Add(d, Bench, Text.Get("workup_bench"), Text.Get("workup_bench_desc"), 2, DryKg, AgricultureEconomy.Price(Bench), "phobos/agriculture/Workup", Definitions.Controls, .02);
        EquipmentInventory.Apply(d, Bench, BenchInventory);
        // A water port (Agriculture 0.44.0): the straw dryer condenses its steam into a tank on the bench's line.
        var port = LinePorts.Water(2);
        LineDefinitions.AddPort(d, Bench, Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(), LinePorts.WaterPoint, port.X, port.Y, port.Socket);
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = Bench + form;
            if (form == "Installed") d.Objects[id].aInteractions = d.Objects[id].aInteractions.Concat(Work.Select(Definitions.WorkId)).ToArray();
        }
        foreach (var stock in new[] { (Residue, "recorded_residue"), (Concentrate, "concentrate"), (Spent, "spent_biomass"), (Makeup, "makeup"), (Mixture, "mixture"), (Bale, "straw_bale") })
            Definitions.Stock(d, stock.Item1, stock.Item2);
        // The bale's identity condition, which Manufacturing's refinery feed admits at the game level.
        d.Conditions[BaleIdentity] = new JsonCond { strName = BaleIdentity, strNameFriendly = Text.Get("straw_bale"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Objects[Bale].aStartingConds = d.Objects[Bale].aStartingConds.Concat(new[] { BaleIdentity + "=1x1" }).ToArray();
    }
}
