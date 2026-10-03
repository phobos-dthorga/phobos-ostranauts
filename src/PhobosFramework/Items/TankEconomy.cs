using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Items;

/// <summary>Applies Framework's economy pack to the water tanks (Framework 0.58.0), on the pattern the tanks had under
/// Shipbreaker: authored price, work-progress targets, repair bill and mass-balanced salvage for the S3, with the other
/// sizes derived through the ladder (work and repair growing with each tile, salvage keeping the S3's fittings and
/// filling the rest of the housing in its proportions), merchant offers, and the S3's own share of world finds.</summary>
public static class TankEconomy
{
    public sealed class Spec
    {
        public string Prefix = "";
        public int Price, Install, Uninstall, Repair, Dismantle, RestoreMinutes;
        public int[] RepairBill = Array.Empty<int>(), Salvage = Array.Empty<int>(), BrokenSalvage = Array.Empty<int>();
    }
    // Native work-progress targets, not wall-clock seconds. Bills are steel, aluminium, mechanical parts, electronic
    // parts and retained trash: 1, 1, 0.5, 0.5 and 1 kg units, the game's own items.
    public static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01", "ItmScrapTrash" };
    private static readonly string[] Triggers = { "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall" };
    private static readonly double[] UnitKg = { 1, 1, .5, .5, 1 };
    /// <summary>Shipbreaker's increments per tile for the silo ladder: install, uninstall, repair, dismantle, Restore minutes.</summary>
    private const int InstallStep = 400, UninstallStep = 300, RepairStep = 600, DismantleStep = 200, RestoreStep = 10;

    public static IReadOnlyList<Spec> Specs => WaterTanks.All.Select(SpecOf).ToArray();
    /// <summary>Every saleable tank size; the S3 alone turns up in salvage.</summary>
    public static IReadOnlyList<EquipmentSale> Sales
    {
        get
        {
            var entry = ItemEconomy.Pack.equipment[WaterTanks.BasePrefix];
            return WaterTanks.All.Select(t => t.Step == 0 ? EquipmentSale.Of(t.Prefix, entry) : EquipmentSale.Size(t.Prefix, entry)).ToArray();
        }
    }
    private static int[] Bill(Dictionary<string, int> bill, int columns) => Materials.Take(columns).Select(id => bill.TryGetValue(id, out int count) ? count : 0).ToArray();
    public static Spec SpecOf(WaterTank tank)
    {
        var small = ItemEconomy.Pack.equipment[WaterTanks.BasePrefix];
        int step = tank.Step;
        double smallDry = WaterTanks.BaseDryKg, dry = tank.DryKg;
        int[] Split(int[] smallBill)
        {
            if (step == 0) return smallBill;
            var fittings = smallBill.Select((count, i) => i is 0 or 1 or 4 ? 0 : count).ToArray();
            double fittingsKg = fittings.Select((count, i) => count * UnitKg[i]).Sum(), smallRest = smallDry - fittingsKg, rest = dry - fittingsKg;
            int steel = (int)Math.Round(rest * smallBill[0] / smallRest), aluminium = (int)Math.Round(rest * smallBill[1] / smallRest);
            fittings[0] = steel; fittings[1] = aluminium; fittings[4] = (int)Math.Round(rest - steel - aluminium);
            return fittings;
        }
        var repair = Bill(small.repairBill, Triggers.Length);
        repair = new[] { repair[0] + step, repair[1] + step, repair[2] + 2 * step, repair[3] }.Select(n => Math.Max(0, n)).ToArray();
        return new Spec
        {
            Prefix = tank.Prefix, Price = (int)tank.Price, Install = small.work.install + InstallStep * step, Uninstall = small.work.uninstall + UninstallStep * step,
            Repair = small.work.repair + RepairStep * step, Dismantle = small.work.dismantle + DismantleStep * step, RepairBill = repair,
            Salvage = Split(Bill(small.salvage, Materials.Length)), BrokenSalvage = Split(Bill(small.brokenSalvage, Materials.Length)),
            RestoreMinutes = small.restoreMinutes + RestoreStep * step
        };
    }
    public static string[] Products(int[] bill) => bill.SelectMany((count, i) => Enumerable.Repeat(Materials[i], count)).ToArray();

    internal static void Apply(NativeDefinitions d)
    {
        foreach (var spec in Specs)
        foreach (string state in MachineFamilies.Forms)
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
                repair.aInputs = spec.RepairBill.Select((count, i) => Triggers[i] + "=1x" + count).Where((s, i) => spec.RepairBill[i] > 0).ToArray();
                // The first output is the repaired tank; every replaced kilogram then returns as spent parts.
                repair.aLootCOs = new[] { spec.Prefix + state.Replace("Dmg", "") };
                repair.aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" };
                MaintenanceDefinitions.Repair(d, repair);
            }
            else
            {
                MaintenanceDefinitions.Restore(d, id);
                SetRestoreRate(d, id, spec.Prefix + "RestoreProgress", spec.RestoreMinutes);
            }
            MaintenanceDefinitions.Dismantle(d, id, spec.Dismantle, Products(damaged ? spec.BrokenSalvage : spec.Salvage));
            var mount = d.Installables[spec.Prefix + state + (state.StartsWith("Installed", StringComparison.Ordinal) ? "Uninstall" : "Install")];
            mount.aToolCTsUse = new[] { "TIsToolMortorq" };
            mount.strCTThemMultCondTools = "IsToolMortorq";
            co.strDesc += Text.Get("WaterTanks.dismantle_note");
            EquipmentSaveUpgrade.Register(d, id, id);
        }
        EconomyStock.AddOffers(d, ItemEconomy.Pack, Sales);
        EconomyStock.AddWorldLoot(d, ItemEconomy.Pack, Sales);
    }
    private static void SetRestoreRate(NativeDefinitions d, string id, string effect, int minutes)
    {
        var job = d.Installables[id + "Restore"];
        // Native work duration is hours. Scale only our wear-removal effect; damage capacity, saved wear, tool and
        // skill modifiers and the action identity stay intact.
        const double MinutesPerHour = 60;
        double maximum = double.Parse(d.Objects[id].aStartingConds.Single(s => s.StartsWith("StatDamageMax=", StringComparison.Ordinal)).Split('x').Last(), CultureInfo.InvariantCulture);
        double removal = maximum * job.fDuration * MinutesPerHour / minutes;
        d.Loot[effect] = new Loot { strName = effect, strType = "trigger", aCOs = new[] { "TDnStatDamage=1x" + removal.ToString("R", CultureInfo.InvariantCulture) }, aLoots = Array.Empty<string>() };
        job.strAllowLootCTsThem = effect;
    }
}
