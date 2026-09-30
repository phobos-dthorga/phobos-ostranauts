using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosAgriculture;

/// <summary>Applies the economy data pack (<c>framework/economy.json</c>, Agriculture 0.24.0) to every Verdemorrow
/// machine: repair bills and work, dismantle work and salvage, with whatever the salvage leaves of the housing
/// returned as a retained housing remainder. Framework keeps native work and actual repair waste. The larger ladder
/// sizes (R4 and R5, E3 and E4) follow their small entry: bills and scrap grow with the footprint step, repair by 600 and
/// dismantling by 200.</summary>
internal static class EquipmentEconomy
{
    internal static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01" };
    private static readonly double[] UnitKg = { 1, 1, .5, .5 };
    private const int RepairStep = 600, DismantleStep = 200;

    internal static void Apply(NativeDefinitions d)
    {
        var pack = AgricultureEconomy.Pack;
        foreach (var machine in AgricultureEconomy.Machines)
        {
            var entry = pack.equipment[machine.Prefix];
            if (AgricultureEconomy.Ladder(machine.Prefix) is { } ladder)
                foreach (var (prefix, step, dryKg) in ladder)
                    ApplyForms(d, prefix, dryKg, Scale(entry.repairBill, 1 + step, except: "ItmPartsElecSmall01"), Scale(entry.salvage, 1 + step), Scale(entry.brokenSalvage, 1 + step),
                        entry.work.repair + RepairStep * step, entry.work.dismantle + DismantleStep * step);
            else ApplyForms(d, machine.Prefix, machine.MassKg, entry.repairBill, entry.salvage, entry.brokenSalvage, entry.work.repair, entry.work.dismantle);
        }
        EconomyStock.AddOffers(d, pack, AgricultureEconomy.Sales);
    }
    private static Dictionary<string, int> Scale(Dictionary<string, int> bill, int factor, string? except = null) =>
        bill.ToDictionary(p => p.Key, p => p.Key == except ? p.Value : p.Value * factor, System.StringComparer.Ordinal);

    private static void ApplyForms(NativeDefinitions d, string prefix, double massKg, Dictionary<string, int> repairBill, Dictionary<string, int> salvage, Dictionary<string, int> brokenSalvage,
        int repairWork, int dismantleWork)
    {
        foreach (string form in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool broken = form.EndsWith("Dmg"); string id = prefix + form;
            if (broken)
            {
                d.Installables[id + "Repair"].aInputs = EconomyStock.RepairInputs(repairBill);
                MaintenanceDefinitions.SetStat(d.Objects[id], "StatRepairProgressMax", repairWork);
            }
            var bill = broken ? brokenSalvage : salvage;
            string waste = prefix + (broken ? "Broken" : "") + "HousingWaste";
            double remainder = massKg - Materials.Select((m, i) => (bill.TryGetValue(m, out int n) ? n : 0) * UnitKg[i]).Sum();
            if (!d.Objects.ContainsKey(waste)) MaintenanceDefinitions.Remainder(d, waste, Text.Get("housing_waste"), remainder);
            var products = Materials.SelectMany(m => Enumerable.Repeat(m, bill.TryGetValue(m, out int n) ? n : 0)).Concat(new[] { waste }).ToArray();
            MaintenanceDefinitions.Dismantle(d, id, dismantleWork, products);
        }
    }
}
