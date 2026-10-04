using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;
using PhobosMedical.Core;

namespace PhobosMedical;

/// <summary>Applies the economy pack to the Ward-3: price, work, a repair bill like the Infirmaway's (with clean
/// cloth for the mattress), mass-balanced salvage, a native Dismantle job, offers, regions, world finds and the faction
/// kiosks. The figures live in <c>framework/economy.json</c>; this class turns them into native definitions.</summary>
internal static class EquipmentEconomy
{
    /// <summary>The game's requirement triggers for the bill materials: the shared list plus clean scrap cloth, which
    /// the game's own medical bed repair takes for its upholstery.</summary>
    private static readonly IReadOnlyDictionary<string, string> Triggers = new Dictionary<string, string>(EconomyStock.MaterialTriggers, StringComparer.Ordinal)
    {
        ["ItmScrapClothClean"] = "TIsScrapClothClean"
    };
    internal const string HighSalvageMark = "IsSalvageValueHigh";
    internal static IReadOnlyList<EquipmentSale> Sales => Economy.Machines.Select(m => EquipmentSale.Of(m.Prefix, Economy.Entry(m.Prefix))).ToArray();

    internal static string[] Products(Dictionary<string, int> bill) => bill.Where(b => b.Value > 0).SelectMany(b => Enumerable.Repeat(b.Key, b.Value)).ToArray();
    internal static string[] RepairInputs(Dictionary<string, int> bill) => bill.Where(b => b.Value > 0)
        .Select(b => (Triggers.TryGetValue(b.Key, out var t) ? t : throw new ArgumentException("No requirement trigger for " + b.Key)) + "=1x" + b.Value).ToArray();

    internal static void Apply(NativeDefinitions d)
    {
        foreach (var (prefix, _) in Economy.Machines)
        {
            var e = Economy.Entry(prefix);
            foreach (string form in MedicalRules.Forms)
            {
                string id = prefix + form;
                bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
                var co = d.Objects[id];
                if (e.salvageValueHigh && !co.aStartingConds.Any(s => s.StartsWith(HighSalvageMark + "=", StringComparison.Ordinal)))
                    co.aStartingConds = co.aStartingConds.Concat(new[] { HighSalvageMark + "=1x1" }).ToArray();
                MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? e.BrokenPriceOrDefault : e.price);
                if (e.work.install > 0) MaintenanceDefinitions.SetStat(co, "StatInstallProgressMax", e.work.install);
                if (e.work.uninstall > 0) MaintenanceDefinitions.SetStat(co, "StatUninstallProgressMax", e.work.uninstall);
                if (damaged)
                {
                    MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", e.work.repair);
                    var repair = d.Installables[id + "Repair"];
                    repair.aInputs = RepairInputs(e.repairBill);
                    repair.aLootCOs = new[] { prefix + form.Replace("Dmg", "") };
                    repair.aToolCTsUse = new[] { "TIsToolMortorq", "TIsToolSoldering" };
                    MaintenanceDefinitions.Repair(d, repair);
                }
                else if (e.restoreMinutes > 0) SetRestoreRate(d, id, prefix + "RestoreProgress", e.restoreMinutes);
                MaintenanceDefinitions.Dismantle(d, id, e.work.dismantle, Products(damaged ? e.brokenSalvage : e.salvage));
                var mount = d.Installables[id + (form.StartsWith("Installed", StringComparison.Ordinal) ? "Uninstall" : "Install")];
                mount.aToolCTsUse = new[] { "TIsToolMortorq" };
                mount.strCTThemMultCondTools = "IsToolMortorq";
                EquipmentSaveUpgrade.Register(d, id, id);
            }
        }
        var sales = Sales;
        EconomyStock.AddOffers(d, Economy.Pack, sales);
        EconomyStock.AddWorldLoot(d, Economy.Pack, sales);
        EconomyStock.ApplyRegional(d, Economy.Pack, "Medical", sales);
        EconomyStock.ApplyFactionKiosks(d, Economy.Pack, "Medical", sales);
    }

    /// <summary>Restore removes wear at the pack's pace; the native Restore job already exists.</summary>
    private static void SetRestoreRate(NativeDefinitions d, string id, string effect, int minutes)
    {
        var job = d.Installables[id + "Restore"];
        const double MinutesPerHour = 60;
        double maximum = double.Parse(d.Objects[id].aStartingConds.Single(s => s.StartsWith("StatDamageMax=", StringComparison.Ordinal)).Split('x').Last(), CultureInfo.InvariantCulture);
        double removal = maximum * job.fDuration * MinutesPerHour / minutes;
        d.Loot[effect] = new Loot { strName = effect, strType = "trigger", aCOs = new[] { "TDnStatDamage=1x" + removal.ToString("R", CultureInfo.InvariantCulture) }, aLoots = Array.Empty<string>() };
        job.strAllowLootCTsThem = effect;
    }
}
