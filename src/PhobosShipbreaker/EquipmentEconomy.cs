using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class EquipmentEconomy
{
    internal sealed class Spec
    {
        internal string Prefix;
        internal int Price, Install, Uninstall, Repair, Dismantle;
        internal int[] RepairBill, Salvage, BrokenSalvage;
        internal Spec(string prefix, int price, int install, int uninstall, int repair, int dismantle,
            int[] repairBill, int[] salvage, int[] brokenSalvage)
        { Prefix = prefix; Price = price; Install = install; Uninstall = uninstall; Repair = repair;
          Dismantle = dismantle; RepairBill = repairBill; Salvage = salvage; BrokenSalvage = brokenSalvage; }
    }
    // Bills are steel, aluminium, mechanical parts, electronic parts, retained trash.
    internal static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01", "ItmScrapTrash" };
    private static readonly string[] Triggers = { "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall" };
    internal static readonly Spec[] Machines = {
        new Spec(Content.Prefix, 12000, 1500, 1000, 3600, 1000, new[]{4,2,4,4}, new[]{92,40,16,4,18}, new[]{80,32,8,0,44}),
        new Spec(IntakeRules.Grabber, 6400, 1000, 800, 2400, 650, new[]{2,1,4,2}, new[]{42,16,12,2,15}, new[]{34,12,6,0,31}),
        new Spec(IntakeRules.Chute, 1800, 500, 500, 1500, 300, new[]{2,1,2,0}, new[]{20,8,8,2,7}, new[]{16,6,4,0,16}),
        new Spec(CollectorRules.Prefix, 2400, 600, 500, 1800, 350, new[]{0,1,2,2}, new[]{10,3,4,2,4}, new[]{8,2,2,0,9})
    };

    internal static string[] Products(int[] bill) => bill.SelectMany((count, i) => Enumerable.Repeat(Materials[i], count)).ToArray();
    internal static void Apply(NativeDefinitions d)
    {
        foreach (var spec in Machines)
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            string id = spec.Prefix + state;
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[id];
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? spec.Price / 4 : spec.Price);
            MaintenanceDefinitions.SetStat(co, "StatInstallProgressMax", spec.Install);
            MaintenanceDefinitions.SetStat(co, "StatUninstallProgressMax", spec.Uninstall);
            if (damaged)
            {
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", spec.Repair);
                var repair = d.Installables[spec.Prefix + state.Replace("Dmg", "") + "Repair"];
                repair.aInputs = spec.RepairBill.Select((count,i) => Triggers[i] + "=1x" + count)
                    .Where((s,i) => spec.RepairBill[i] > 0).ToArray();
                // First output is the persistent replacement machine. Every replaced
                // kilogram is then returned as loose spent parts, not deleted matter.
                repair.aLootCOs = new[] { spec.Prefix + state.Replace("Dmg", "") };
                repair.aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" };
                MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
            }
            else MaintenanceDefinitions.Restore(d, id);
            MaintenanceDefinitions.Dismantle(d, id, spec.Dismantle, Products(damaged ? spec.BrokenSalvage : spec.Salvage),
                emptyInternalBin: spec.Prefix == Content.Prefix ? Content.InputBin : null);
            var mount = d.Installables[spec.Prefix + state + (state.StartsWith("Installed") ? "Uninstall" : "Install")];
            mount.aToolCTsUse = new[] { "TIsToolMortorq" };
            mount.strCTThemMultCondTools = "IsToolMortorq";
            co.strDesc += " Empty the machine and feed before dismantling. Repair replaces failed parts; Restore treats wear using tools.";
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        var section = d.Objects[ProcessRules.AssemblySection];
        MaintenanceDefinitions.SetStat(section, "StatBasePrice", 4800);
        section.aStartingConds = section.aStartingConds.Concat(new[] { "IsCategoryIndustrialProducts=1x1", "IsSalvageValueHigh=1x1" }).ToArray();
        MaintenanceDefinitions.Dismantle(d, section.strName, 400, Products(new[]{46,20,8,2,9}));
        MaintenanceDefinitions.SetStat(d.Objects[ProcessRules.Residue], "StatBasePrice", .01);
        EquipmentSaveUpgrade.Register(d, section.strName, section.strName);
        EquipmentSaveUpgrade.Register(d, ProcessRules.Residue, ProcessRules.Residue);
        AddStock(d);
    }

    private static void AddStock(NativeDefinitions d)
    {
        // OKLG scrap supplies already sells broken high-end hardware; the fixer
        // carries rare usable equipment. San Diego's Halvorson is an industrial trader.
        foreach (var spec in Machines)
        {
            double small = spec.Prefix == IntakeRules.Chute || spec.Prefix == CollectorRules.Prefix ? 1.5 : 1;
            Offer("ItmOKLGSupplyKioskInv", "Scrap", spec.Prefix + "LooseDmg", .20 * small, StockCondition.Broken);
            Offer("ItmOKLGFixer", "Fixer", spec.Prefix + "Loose", .10 * small, StockCondition.Worn);
            Offer("ItmTraderSanDiegoHalvorsonInv", "Industrial", spec.Prefix + "Loose", .40 * small, StockCondition.Pristine);
            Offer("ItmVORBScrapKioskInv", "VenusScrap", spec.Prefix + "LooseDmg", .15 * small, StockCondition.Broken);
        }
        Offer("ItmOKLGSupplyKioskInv", "Section", ProcessRules.AssemblySection, .30, StockCondition.Refurbished);
        Offer("ItmTraderSanDiegoHalvorsonInv", "Section", ProcessRules.AssemblySection, .50, StockCondition.Refurbished);
        // Modest repaired stock at K-Leg offers a usable path without requiring a
        // journey to a specific system. Separate chances can produce both offers.
        Offer("ItmOKLGFixer", "Refurb", Content.Loose, .10, StockCondition.Refurbished);

        void Offer(string merchant, string tag, string item, double chance, StockCondition condition) =>
            MarketStock.Add(d, merchant, "PhobosStock_" + tag + "_" + merchant + "_" + item, item, chance, condition);
    }
}
