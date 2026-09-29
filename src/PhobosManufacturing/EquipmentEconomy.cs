using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Authored prices, work, repair bills and mass-balanced salvage for the three machines, with the same
/// merchant and regional routes as their Shipbreaker siblings. Owner direction (29 September 2026): Manufacturing
/// equipment is mid-to-late-game plant, priced and maintained like the game's own late-game kit (IC fusion reactor
/// $141,000, heavy lift rotor $56,774, radars $30,000-42,000, towing brace $23,944): component repair bills with
/// motors, mainboards, heat sinks and screens, long work, the game's high-salvage mark, and rare world finds.
/// Salvage bills equal each machine's mass.</summary>
internal static class EquipmentEconomy
{
    internal sealed class Spec
    {
        internal string Prefix;
        internal int Price, Install, Uninstall, Repair, Dismantle, RestoreMinutes;
        internal int[] RepairBill, Salvage, BrokenSalvage;
        internal string? InternalBin;
        internal Spec(string prefix, int price, int install, int uninstall, int repair, int dismantle, int[] repairBill, int[] salvage, int[] brokenSalvage, int restoreMinutes, string? internalBin = null)
        {
            if (repairBill.Length != Triggers.Length || salvage.Length != Materials.Length || brokenSalvage.Length != Materials.Length)
                throw new ArgumentException("Bill length does not match the material list: " + prefix);
            Prefix = prefix; Price = price; Install = install; Uninstall = uninstall; Repair = repair; Dismantle = dismantle; RepairBill = repairBill; Salvage = salvage; BrokenSalvage = brokenSalvage; RestoreMinutes = restoreMinutes; InternalBin = internalBin;
        }
    }
    // Bills are steel, aluminium, mechanical parts, electronic parts, motors, mainboards, heat sinks, screens and
    // (salvage only) retained trash: 1, 1, 0.5, 0.5, 2.5, 0.5, 1.5, 6 and 1 kg units, the game's own items.
    internal static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01",
        "ItmComponentMotor01", "ItmComponentMobo01", "ItmHeatSink01", "ItmPartsScreen01", "ItmScrapTrash" };
    private static readonly string[] Triggers = { "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall",
        "TIsMotor", "TIsMobo", "TIsHeatSink", "TIsScreen" };
    /// <summary>The game's own mark on its late-game equipment: the K-Leg fixer buys it intact, the ordinary
    /// supplies kiosk does not; Venus buys either. Every native loose item above $20,000 carries it.</summary>
    internal const string HighSalvageMark = "IsSalvageValueHigh";
    internal static readonly Spec[] Machines = {
        //                                                                                                          repair: St Al Me El Mo Mb HS Sc     salvage:  St  Al Me El Mo Mb HS Sc Tr          broken: St Al Me El Mo Mb HS Sc Tr
        new Spec(RefineryRules.Prefix, price: (int)RefineryRules.Price, install: 2000, uninstall: 1600, repair: 6000, dismantle: 1600, new[]{4,2,6,8,2,2,2,1}, new[]{100,40,20,10,2,2,2,1,10}, new[]{90,32,10,4,1,0,1,0,47}, restoreMinutes: 150, RefineryRules.InputBin),
        new Spec(ProcessorRules.Prefix, price: (int)ProcessorRules.Price, install: 1200, uninstall: 1000, repair: 3600, dismantle: 900, new[]{2,2,3,8,1,3,2,0}, new[]{70,26,16,12,1,3,2,0,13}, new[]{34,10,4,0,0,1,1,0,82}, restoreMinutes: 90),
        new Spec(HydrogenRules.Prefix, price: (int)HydrogenRules.Price, install: 1200, uninstall: 1000, repair: 3000, dismantle: 900, new[]{6,2,6,2,0,1,0,0}, new[]{110,30,12,3,0,1,0,0,12}, new[]{40,10,4,0,0,0,0,0,108}, restoreMinutes: 60)
    };
    internal static string[] Products(int[] bill) => bill.SelectMany((count, i) => Enumerable.Repeat(Materials[i], count)).ToArray();
    internal static void Apply(NativeDefinitions d)
    {
        foreach (var spec in Machines)
        foreach (string state in Definitions.Forms)
        {
            string id = spec.Prefix + state;
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[id];
            if (!co.aStartingConds.Any(s => s.StartsWith(HighSalvageMark + "=", StringComparison.Ordinal)))
                co.aStartingConds = co.aStartingConds.Concat(new[] { HighSalvageMark + "=1x1" }).ToArray();
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? spec.Price / 4 : spec.Price);
            MaintenanceDefinitions.SetStat(co, "StatInstallProgressMax", spec.Install);
            MaintenanceDefinitions.SetStat(co, "StatUninstallProgressMax", spec.Uninstall);
            if (damaged)
            {
                MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", spec.Repair);
                var repair = d.Installables[id + "Repair"];
                repair.aInputs = spec.RepairBill.Select((count, i) => Triggers[i] + "=1x" + count).Where((s, i) => spec.RepairBill[i] > 0).ToArray();
                repair.aLootCOs = new[] { spec.Prefix + state.Replace("Dmg", "") };
                repair.aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" };
                MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
            }
            else SetRestoreRate(d, id, spec.Prefix + "RestoreProgress", spec.RestoreMinutes);
            MaintenanceDefinitions.Dismantle(d, id, spec.Dismantle, Products(damaged ? spec.BrokenSalvage : spec.Salvage), emptyInternalBin: spec.InternalBin);
            var mount = d.Installables[spec.Prefix + state + (state.StartsWith("Installed", StringComparison.Ordinal) ? "Uninstall" : "Install")];
            mount.aToolCTsUse = new[] { "TIsToolMortorq" };
            mount.strCTThemMultCondTools = "IsToolMortorq";
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        AddStock(d);
        AdditiveLoot.SetItemChoice(d, "ItmLootSpawnEngineering", "PhobosManufacturingMachinerySalvage", SalvageChances());
    }
    /// <summary>Late-game plant is a rare find: one engineering roll in twenty yields a machine, three times in four
    /// a broken one, split evenly across the three families.</summary>
    internal const double MachinerySalvageChance = 0.05, BrokenSalvageShare = 0.75;
    internal static Dictionary<string, double> SalvageChances() => Machines
        .SelectMany(m => new[] { (m.Prefix + "LooseDmg", MachinerySalvageChance * BrokenSalvageShare / Machines.Length),
                                 (m.Prefix + "Loose", MachinerySalvageChance * (1 - BrokenSalvageShare) / Machines.Length) })
        .ToDictionary(p => p.Item1, p => p.Item2);
    /// <summary>Restore removes wear at an equipment-specific rate; the native Restore job already exists.</summary>
    internal static void SetRestoreRate(NativeDefinitions d, string id, string effect, int minutes)
    {
        var job = d.Installables[id + "Restore"];
        const double MinutesPerHour = 60;
        double maximum = double.Parse(d.Objects[id].aStartingConds.Single(s => s.StartsWith("StatDamageMax=", StringComparison.Ordinal)).Split('x').Last(), CultureInfo.InvariantCulture);
        double removal = maximum * job.fDuration * MinutesPerHour / minutes;
        d.Loot[effect] = new Loot { strName = effect, strType = "trigger", aCOs = new[] { "TDnStatDamage=1x" + removal.ToString("R", CultureInfo.InvariantCulture) }, aLoots = Array.Empty<string>() };
        job.strAllowLootCTsThem = effect;
    }
    private static void AddStock(NativeDefinitions d)
    {
        foreach (var spec in Machines)
        {
            Offer("ItmOKLGSupplyKioskInv", "Scrap", spec.Prefix + "LooseDmg", .20, StockCondition.Broken);
            Offer("ItmOKLGFixer", "Fixer", spec.Prefix + "Loose", .10, StockCondition.Worn);
            Offer("ItmTraderSanDiegoHalvorsonInv", "Industrial", spec.Prefix + "Loose", .40, StockCondition.Pristine);
            Offer("ItmVORBScrapKioskInv", "VenusScrap", spec.Prefix + "LooseDmg", .15, StockCondition.Broken);
            Offer("ItmVORBScrapKioskInv", "VenusRefurb", spec.Prefix + "Loose", .20, StockCondition.Refurbished);
        }
        void Offer(string merchant, string tag, string item, double chance, StockCondition condition) =>
            MarketStock.Add(d, merchant, "PhobosStock_" + tag + "_" + merchant + "_" + item, item, StockQuantities.Chance(item, chance), condition, StockQuantities.For(item));
    }
}

/// <summary>Wholesale lots per successful offer and the shared availability floor, as the sibling mods use.</summary>
internal static class StockQuantities
{
    internal const double EquipmentChance = 0.85;
    internal const int Machines = 8;
    internal static double Chance(string item, double original) => Math.Min(1, Math.Max(EquipmentChance, original));
    internal static int For(string item) => Machines;
}

/// <summary>Regional availability across the vanilla solar system, with Shipbreaker's industrial factors.</summary>
internal static class RegionalEconomy
{
    internal static readonly (string Region, double Factor)[] Profiles = {
        ("BCER", 1), ("BCRS", 1.25), ("EJDR", 0.75), ("HQCH", 0.75), ("JATL", 0.75), ("JFTS", 1), ("MHNG", 0.5), ("MSUZ", 1),
        ("MTRS", 1.5), ("MVOL", 1.5), ("OFLT", 0.5), ("SVIR", 1.25), ("VCBR", 1), ("VENC", 0.75), ("VNCA", 1.25)
    };
    internal static void Apply(NativeDefinitions d)
    {
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv" })
        foreach (var machine in EquipmentEconomy.Machines)
            MarketStock.AddMissing(d, merchant, "PhobosExpanded_Manufacturing_" + merchant + "_" + machine.Prefix + "Loose", machine.Prefix + "Loose",
                StockQuantities.Chance(machine.Prefix + "Loose", 0), StockCondition.Pristine, StockQuantities.For(machine.Prefix + "Loose"));
        foreach (var profile in Profiles)
        {
            var condition = profile.Region == "OFLT" ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (var machine in EquipmentEconomy.Machines)
                RegionalMarkets.Add(d, profile.Region, machine.Prefix + "Loose", StockQuantities.Chance(machine.Prefix + "Loose", .20 * profile.Factor), condition, StockQuantities.For(machine.Prefix + "Loose"));
        }
    }
}
