using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Applies the economy data pack (<c>framework/economy.json</c>, Shipbreaker 0.48.0): authored prices,
/// work-progress targets, repair bills and mass-balanced salvage for every machine and section, the coolant conduit,
/// merchant offers and world finds. The S4, S5, Y3 and Y4 follow their small entry through the Framework ladder.</summary>
internal static class EquipmentEconomy
{
    internal sealed class Spec
    {
        internal string Prefix;
        internal int Price, Install, Uninstall, Repair, Dismantle, RestoreMinutes;
        internal int[] RepairBill, Salvage, BrokenSalvage;
        /// <summary>Whether a loose unit can turn up in engineering salvage; the larger silos and bins are too big to.</summary>
        internal bool Loot;
        internal string? InternalBin;
        internal EquipmentEconomyEntry Entry;
        internal Spec(string prefix, EquipmentEconomyEntry entry, int price, int install, int uninstall, int repair, int dismantle,
            int[] repairBill, int[] salvage, int[] brokenSalvage, int restoreMinutes, bool loot, string? internalBin)
        { Prefix = prefix; Entry = entry; Price = price; Install = install; Uninstall = uninstall; Repair = repair;
          Dismantle = dismantle; RepairBill = repairBill; Salvage = salvage; BrokenSalvage = brokenSalvage;
          RestoreMinutes = restoreMinutes; Loot = loot; InternalBin = internalBin; }
    }
    // Native work-progress targets, not wall-clock seconds. Bills are steel, aluminium, mechanical parts,
    // electronic parts, retained trash: 1, 1, 0.5, 0.5 and 1 kg units, the game's own items.
    internal static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01", "ItmScrapTrash" };
    private static readonly string[] Triggers = { "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall" };
    private static readonly double[] UnitKg = { 1, 1, .5, .5, 1 };
    private static Spec[]? machines; private static EquipmentSale[]? sales; private static EconomyPack? builtFrom;
    /// <summary>Every machine family and size in application order: the small entries, then the S4 and S5, then the Y3 and Y4.</summary>
    internal static IReadOnlyList<Spec> Machines => machines != null && ReferenceEquals(builtFrom, ShipbreakerEconomy.Pack) ? machines : Build().Machines;
    /// <summary>Every saleable machine size, for offers, regional stock and world finds.</summary>
    internal static IReadOnlyList<EquipmentSale> Sales => sales != null && ReferenceEquals(builtFrom, ShipbreakerEconomy.Pack) ? sales : Build().Sales;
    internal static double MachinerySalvageChance => ShipbreakerEconomy.Pack.worldLoot.First(l => l.items == null).chance;
    internal static double AluminiumIngotSalvageChance => IngotSalvageChance(FurnaceRecipes.AluminiumIngot);
    internal static double SteelIngotSalvageChance => IngotSalvageChance(FurnaceRecipes.SteelIngot);
    private static double IngotSalvageChance(string ingot) => ShipbreakerEconomy.Pack.worldLoot.Where(l => l.items != null && l.items.ContainsKey(ingot)).Sum(l => l.items![ingot]);

    private static (Spec[] Machines, EquipmentSale[] Sales) Build()
    {
        var pack = builtFrom = ShipbreakerEconomy.Pack;
        var list = new List<Spec>(); var saleList = new List<EquipmentSale>();
        foreach (var machine in ShipbreakerEconomy.Machines)
        {
            var entry = pack.equipment[machine.Prefix];
            list.Add(FromEntry(machine.Prefix, entry)); saleList.Add(EquipmentSale.Of(machine.Prefix, entry));
        }
        var bin = pack.equipment[BinRules.Prefix];
        foreach (var size in BinRules.Sizes.Skip(1))
        {
            list.Add(LadderSpec(size.Prefix, size.Footprint - BinRules.Footprint, size.DryKg, BinRules.DryKg, size.Price, bin, 300, 200, 500, 150, 5));
            saleList.Add(EquipmentSale.Size(size.Prefix, bin));
        }
        // Sections are retired (Shipbreaker 0.60.0): never offered or found; held copies keep price and dismantling.
        machines = list.ToArray(); sales = saleList.ToArray();
        return (machines, sales);
    }
    private static int[] Bill(Dictionary<string, int> bill, int columns) => Materials.Take(columns).Select(id => bill.TryGetValue(id, out int count) ? count : 0).ToArray();
    private static Spec FromEntry(string prefix, EquipmentEconomyEntry e) => new(prefix, e, (int)e.price, e.work.install, e.work.uninstall, e.work.repair, e.work.dismantle,
        Bill(e.repairBill, Triggers.Length), Bill(e.salvage, Materials.Length), Bill(e.brokenSalvage, Materials.Length), e.restoreMinutes, e.loot, e.internalBin);
    /// <summary>A larger size on its small entry's pattern: work and repair grow with the footprint step, and salvage
    /// keeps the small size's fittings and fills the rest of the housing with steel, aluminium and retained trash in
    /// the small size's proportions. The step increments are the family's own.</summary>
    internal static Spec LadderSpec(string prefix, int step, double dryKg, double smallDryKg, double price, EquipmentEconomyEntry small,
        int installStep, int uninstallStep, int repairStep, int dismantleStep, int restoreStep)
    {
        int[] Split(int[] smallBill)
        {
            var fittings = smallBill.Select((count, i) => i is 0 or 1 or 4 ? 0 : count).ToArray();
            double fittingsKg = fittings.Select((count, i) => count * UnitKg[i]).Sum(), smallRest = smallDryKg - fittingsKg, rest = dryKg - fittingsKg;
            int steel = (int)Math.Round(rest * smallBill[0] / smallRest), aluminium = (int)Math.Round(rest * smallBill[1] / smallRest);
            fittings[0] = steel; fittings[1] = aluminium; fittings[4] = (int)Math.Round(rest - steel - aluminium);
            return fittings;
        }
        var repair = Bill(small.repairBill, Triggers.Length);
        repair = new[] { repair[0] + step, repair[1] + step, repair[2] + 2 * step, repair[3] };
        return new Spec(prefix, small, (int)price, small.work.install + installStep * step, small.work.uninstall + uninstallStep * step, small.work.repair + repairStep * step,
            small.work.dismantle + dismantleStep * step, repair, Split(Bill(small.salvage, Materials.Length)), Split(Bill(small.brokenSalvage, Materials.Length)),
            small.restoreMinutes + restoreStep * step, loot: false, small.internalBin);
    }

    internal static string[] Products(int[] bill) => bill.SelectMany((count, i) => Enumerable.Repeat(Materials[i], count)).ToArray();
    internal static void Apply(NativeDefinitions d)
    {
        var pack = ShipbreakerEconomy.Pack;
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
                // The repaired machine is the only output: the parts are consumed, as in the game's own repairs.
                repair.aLootCOs = new[] { spec.Prefix + state.Replace("Dmg", "") };
                repair.aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" };
                MaintenanceDefinitions.Repair(d, repair);
            }
            else Restore(d, id, spec);
            MaintenanceDefinitions.Dismantle(d, id, spec.Dismantle, Products(damaged ? spec.BrokenSalvage : spec.Salvage), emptyInternalBin: spec.InternalBin);
            var mount = d.Installables[spec.Prefix + state + (state.StartsWith("Installed") ? "Uninstall" : "Install")];
            mount.aToolCTsUse = new[] { "TIsToolMortorq" };
            mount.strCTThemMultCondTools = "IsToolMortorq";
            co.strDesc += Text.Get(BinRules.IsFamily(spec.Prefix) ? "EquipmentEconomy.empty_the_bin_before_dismantling" : "EquipmentEconomy.empty_the_machine_and_feed_before_dismantling");
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        foreach (var section in ShipbreakerEconomy.Sections)
        {
            var entry = pack.equipment[section.Id]; var co = d.Objects[section.Id];
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", entry.price);
            var marks = new List<string> { "IsCategoryIndustrialProducts=1x1" };
            if (entry.salvageValueHigh) marks.Add("IsSalvageValueHigh=1x1");
            co.aStartingConds = co.aStartingConds.Concat(marks).ToArray();
            MaintenanceDefinitions.Dismantle(d, section.Id, entry.work.dismantle, Products(Bill(entry.salvage, Materials.Length)));
            EquipmentSaveUpgrade.Register(d, section.Id, section.Id);
        }
        MaintenanceDefinitions.SetStat(d.Objects[ProcessRules.Residue], "StatBasePrice", .01);
        EquipmentSaveUpgrade.Register(d, ProcessRules.Residue, ProcessRules.Residue);
        foreach (string reject in FeedFamilies.RejectKg.Keys)
        {
            MaintenanceDefinitions.SetStat(d.Objects[reject], "StatBasePrice", .01);
            EquipmentSaveUpgrade.Register(d, reject, reject);
        }
        EconomyStock.AddOffers(d, pack, Sales);
        EconomyStock.AddWorldLoot(d, pack, Sales);
    }

    private static void Restore(NativeDefinitions d, string id, Spec spec)
    {
        MaintenanceDefinitions.Restore(d, id);
        SetRestoreRate(d, id, spec.Prefix + "RestoreProgress", spec.RestoreMinutes);
    }
    internal static void SetRestoreRate(NativeDefinitions d, string id, string effect, int minutes)
    {
        var job = d.Installables[id + "Restore"];
        // Native work duration is hours. Scale only our wear-removal effect;
        // damage capacity, saved wear, tool/skill modifiers and action identity stay intact.
        const double MinutesPerHour = 60;
        double maximum = double.Parse(d.Objects[id].aStartingConds.Single(s =>
            s.StartsWith("StatDamageMax=", StringComparison.Ordinal)).Split('x').Last(), CultureInfo.InvariantCulture);
        double removal = maximum * job.fDuration * MinutesPerHour / minutes;
        d.Loot[effect] = new Loot { strName = effect, strType = "trigger",
            aCOs = new[] { "TDnStatDamage=1x" + removal.ToString("R", CultureInfo.InvariantCulture) },
            aLoots = Array.Empty<string>() };
        job.strAllowLootCTsThem = effect;
    }
    /// <summary>The repair inputs of a supply entry, as native trigger requirements in bill-material order.</summary>
    internal static string[] RepairInputs(SupplyEconomyEntry supply)
    {
        var bill = Bill(supply.repairBill, Triggers.Length);
        return bill.Select((count, i) => Triggers[i] + "=1x" + count).Where((s, i) => bill[i] > 0).ToArray();
    }
}
