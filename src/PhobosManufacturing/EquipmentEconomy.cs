using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using Phobos.Ostranauts.Framework.Trading;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Applies the economy data pack: authored prices, work, repair bills and mass-balanced salvage for the
/// machines, with the same merchant and regional routes as their Shipbreaker siblings. Owner direction (29 September
/// 2026): Manufacturing equipment is mid-to-late-game plant, priced and maintained like the game's own late-game kit
/// (IC fusion reactor $141,000, heavy lift rotor $56,774, radars $30,000-42,000, towing brace $23,944): component
/// repair bills, long work, the game's high-salvage mark, and rare world finds. The figures live in
/// <c>framework/economy.json</c> (Manufacturing 0.11.0); this class turns them into native definitions.</summary>
internal static class EquipmentEconomy
{
    internal sealed class Spec
    {
        internal string Prefix;
        internal int Price, BrokenPrice, Install, Uninstall, Repair, Dismantle, RestoreMinutes;
        internal int[] RepairBill, Salvage, BrokenSalvage;
        internal string? InternalBin;
        /// <summary>Whether a loose unit can turn up in engineering salvage (only machines and small stores fit a container).</summary>
        internal bool Loot;
        internal bool Offers, SalvageValueHigh;
        internal Spec(string prefix, int price, int brokenPrice, int install, int uninstall, int repair, int dismantle, int[] repairBill, int[] salvage, int[] brokenSalvage, int restoreMinutes,
            string? internalBin, bool loot, bool offers, bool salvageValueHigh)
        {
            if (repairBill.Length != Triggers.Length || salvage.Length != Materials.Length || brokenSalvage.Length != Materials.Length)
                throw new ArgumentException("Bill length does not match the material list: " + prefix);
            Prefix = prefix; Price = price; BrokenPrice = brokenPrice; Install = install; Uninstall = uninstall; Repair = repair; Dismantle = dismantle; RepairBill = repairBill; Salvage = salvage;
            BrokenSalvage = brokenSalvage; RestoreMinutes = restoreMinutes; InternalBin = internalBin; Loot = loot; Offers = offers; SalvageValueHigh = salvageValueHigh;
        }
    }
    // Bills are steel, aluminium, mechanical parts, electronic parts, motors, mainboards, heat sinks, screens and
    // (salvage only) retained trash: 1, 1, 0.5, 0.5, 2.5, 0.5, 1.5, 6 and 1 kg units, the game's own items.
    internal static readonly string[] Materials = { "ItmScrapSteel", "ItmScrapAluminum", "ItmPartsMechSmall01", "ItmPartsElecSmall01",
        "ItmComponentMotor01", "ItmComponentMobo01", "ItmHeatSink01", "ItmPartsScreen01", "ItmScrapTrash" };
    private static readonly string[] Triggers = { "TIsScrapSteel", "TIsScrapAluminum", "TIsPartsMechSmall", "TIsPartsElecSmall",
        "TIsMotor", "TIsMobo", "TIsHeatSink", "TIsScreen" };
    /// <summary>The game's own unit masses of the bill materials, used to carry the small stores' salvage up the size
    /// ladder (the pack's own bills are checked against the native definitions when the game is loaded).</summary>
    private static readonly double[] UnitKg = { 1, 1, .5, .5, 2.5, .5, 1.5, 6, 1 };
    /// <summary>The game's own mark on its late-game equipment: the K-Leg fixer buys it intact, the ordinary
    /// supplies kiosk does not; Venus buys either. Every native loose item above $20,000 carries it.</summary>
    internal const string HighSalvageMark = "IsSalvageValueHigh";
    /// <summary>Loose propellant line stacks like the other mods' pipes.</summary>
    internal const int LineStack = 10;
    private static Spec[]? machines; private static EconomyPack? builtFrom;
    /// <summary>Every family and size in application order: the machines, then every gas store size. Rebuilt when the
    /// pack is reloaded (a new game load, a second offline preparation).</summary>
    internal static IReadOnlyList<Spec> Machines => machines != null && ReferenceEquals(builtFrom, Economy.Pack) ? machines : machines = Build();
    /// <summary>Chance per engineering-loot roll of one Manufacturing machine, from the pack's world-loot entry.</summary>
    internal static double MachinerySalvageChance => Economy.Pack.worldLoot.Count > 0 ? Economy.Pack.worldLoot[0].chance : 0;
    internal static double BrokenSalvageShare => Economy.Pack.worldLoot.Count > 0 ? Economy.Pack.worldLoot[0].brokenShare : 0;

    private static Spec[] Build()
    {
        var pack = builtFrom = Economy.Pack;
        var list = new List<Spec>();
        foreach (var machine in Economy.Machines) list.Add(FromEntry(machine.Prefix, pack.equipment[machine.Prefix]));
        foreach (var store in GasStores.All) list.Add(StoreSpec(store, pack.equipment[store.Family.SmallPrefix]));
        return list.ToArray();
    }
    private static int[] Bill(Dictionary<string, int> bill, string[] columns) => columns.Select(id => bill.TryGetValue(id, out int count) ? count : 0).ToArray();
    private static Spec FromEntry(string prefix, EquipmentEconomyEntry e) => new(prefix, (int)e.price, (int)e.BrokenPriceOrDefault, e.work.install, e.work.uninstall, e.work.repair, e.work.dismantle,
        Bill(e.repairBill, Materials.Take(Triggers.Length).ToArray()), Bill(e.salvage, Materials), Bill(e.brokenSalvage, Materials), e.restoreMinutes, e.internalBin, e.loot, e.offers, e.salvageValueHigh);
    /// <summary>Every gas store size on the same pattern: late-game price from the size ladder, work and repair growing
    /// with the footprint, and mass-balanced salvage whose steel, aluminium and retained trash fill the dry mass
    /// around the small store's fittings (the small entry's own bills exactly).</summary>
    internal static Spec StoreSpec(GasStore store, EquipmentEconomyEntry small)
    {
        int step = store.Footprint - store.Family.SmallFootprint;
        double dry = store.DryKg, smallDry = store.Family.SmallDryKg;
        int[] Split(int[] smallBill)
        {
            // Fittings are everything but steel, aluminium and trash; the rest of the housing splits as the small store's does.
            var fittings = smallBill.Select((count, i) => i is 0 or 1 or 8 ? 0 : count).ToArray();
            double fittingsKg = fittings.Select((count, i) => count * UnitKg[i]).Sum(), smallRest = smallDry - fittingsKg, rest = dry - fittingsKg;
            int steel = (int)Math.Round(rest * smallBill[0] / smallRest), aluminium = (int)Math.Round(rest * smallBill[1] / smallRest), trash = (int)Math.Round(rest - steel - aluminium);
            fittings[0] = steel; fittings[1] = aluminium; fittings[8] = trash;
            return fittings;
        }
        var repair = Bill(small.repairBill, Materials.Take(Triggers.Length).ToArray());
        repair = new[] { repair[0] + 2 * step, repair[1] + step, repair[2] + 2 * step, repair[3] + step, repair[4], repair[5] + step / 2, repair[6], repair[7] };
        double price = store.Price, broken = small.brokenPrice is double b ? BulkVesselSizes.Scale(b, BulkVesselSizes.PriceFactor(store.Family.SmallFootprint, store.Size)) : price / 4;
        return new Spec(store.Prefix, (int)price, (int)broken, small.work.install + 400 * step, small.work.uninstall + 300 * step, small.work.repair + 900 * step, small.work.dismantle + 300 * step,
            repair, Split(Bill(small.salvage, Materials)), Split(Bill(small.brokenSalvage, Materials)), small.restoreMinutes + 15 * step, small.internalBin,
            loot: small.loot && store.Size == VesselSize.Small, offers: small.offers, salvageValueHigh: small.salvageValueHigh);
    }
    internal static string[] Products(int[] bill) => bill.SelectMany((count, i) => Enumerable.Repeat(Materials[i], count)).ToArray();
    internal static void Apply(NativeDefinitions d)
    {
        foreach (var spec in Machines)
        foreach (string state in Definitions.Forms)
        {
            string id = spec.Prefix + state;
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[id];
            if (spec.SalvageValueHigh && !co.aStartingConds.Any(s => s.StartsWith(HighSalvageMark + "=", StringComparison.Ordinal)))
                co.aStartingConds = co.aStartingConds.Concat(new[] { HighSalvageMark + "=1x1" }).ToArray();
            MaintenanceDefinitions.SetStat(co, "StatBasePrice", damaged ? spec.BrokenPrice : spec.Price);
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
        AddSupplies(d);
        foreach (var loot in Economy.Pack.worldLoot)
            AdditiveLoot.SetItemChoice(d, loot.table, loot.branch, SalvageChances(loot));
    }
    /// <summary>Late-game plant is a rare find: one engineering roll in twenty yields a machine, three times in four
    /// a broken one, split evenly across the families that may be found.</summary>
    internal static Dictionary<string, double> SalvageChances() => Economy.Pack.worldLoot.Count > 0 ? SalvageChances(Economy.Pack.worldLoot[0]) : new Dictionary<string, double>(StringComparer.Ordinal);
    private static Dictionary<string, double> SalvageChances(WorldLootEntry loot)
    {
        var found = Machines.Where(m => m.Loot).ToArray();
        return found.SelectMany(m => new[] { (m.Prefix + "LooseDmg", loot.chance * loot.brokenShare / found.Length),
                                             (m.Prefix + "Loose", loot.chance * (1 - loot.brokenShare) / found.Length) })
            .ToDictionary(p => p.Item1, p => p.Item2, StringComparer.Ordinal);
    }
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
    /// <summary>Supplies (the propellant line) are ordinary stock, not late-game plant: priced and stocked like the
    /// other mods' pipes, repaired with a little aluminium, dismantled to their own retained waste.</summary>
    private static void AddSupplies(NativeDefinitions d)
    {
        foreach (var pair in Economy.Pack.supplies)
        {
            string prefix = pair.Key; var supply = pair.Value;
            string waste = supply.remainder ?? prefix + "Waste";
            double kg = prefix == PropellantLineRules.Prefix ? PropellantLineRules.Kg : 1;
            MaintenanceDefinitions.Remainder(d, waste, Text.Get("Line.waste"), kg);
            var bill = Bill(supply.repairBill, Materials.Take(Triggers.Length).ToArray());
            foreach (string state in Definitions.Forms)
            {
                string id = prefix + state; var co = d.Objects[id];
                if (state.EndsWith("Dmg", StringComparison.Ordinal))
                {
                    MaintenanceDefinitions.SetStat(co, "StatRepairProgressMax", supply.repairWork);
                    var repair = d.Installables[id + "Repair"];
                    repair.aInputs = bill.Select((count, i) => Triggers[i] + "=1x" + count).Where((s, i) => bill[i] > 0).ToArray();
                    MaintenanceDefinitions.ReturnRepairMaterials(d, repair);
                }
                MaintenanceDefinitions.Dismantle(d, id, supply.dismantleWork, new[] { waste });
                EquipmentSaveUpgrade.Register(d, id, id);
            }
            foreach (string merchant in supply.merchants)
                MarketStock.Add(d, merchant, "PhobosStock_Line_" + merchant, prefix + "Loose", StockQuantities.Chance(prefix + "Loose", supply.chance), StockCondition.Pristine, StockQuantities.For(prefix + "Loose"));
        }
    }
    private static void AddStock(NativeDefinitions d)
    {
        foreach (var spec in Machines)
        {
            if (!spec.Offers) continue;
            foreach (var template in Economy.Pack.offerTemplates)
                Offer(template.merchant, template.tag, spec.Prefix + template.form, template.chance, (StockCondition)Enum.Parse(typeof(StockCondition), template.condition));
        }
        foreach (var pair in Economy.Pack.offers)
        {
            var offer = pair.Value;
            MarketStock.Add(d, offer.merchant, pair.Key, offer.item, StockQuantities.Chance(offer.item, offer.chance), (StockCondition)Enum.Parse(typeof(StockCondition), offer.condition),
                offer.quantity ?? StockQuantities.For(offer.item));
        }
        void Offer(string merchant, string tag, string item, double chance, StockCondition condition) =>
            MarketStock.Add(d, merchant, "PhobosStock_" + tag + "_" + merchant + "_" + item, item, StockQuantities.Chance(item, chance), condition, StockQuantities.For(item));
    }
}

/// <summary>Wholesale lots per successful offer and the shared availability floor, as the sibling mods use.</summary>
internal static class StockQuantities
{
    internal static double EquipmentChance => Floor("equipment", 0.85);
    internal static double SupplyChance => Floor("supplies", 0.95);
    internal static int Machines => Lot("equipment", 8);
    internal static int Pipes => Lot("supplies", 128);
    private static double Floor(string kind, double fallback) => Economy.Pack.chanceFloors.TryGetValue(kind, out double v) ? v : fallback;
    private static int Lot(string kind, int fallback) => Economy.Pack.lots.TryGetValue(kind, out int v) ? v : fallback;
    private static bool Supply(string item) => Economy.SupplyKeys.Any(prefix => item.StartsWith(prefix, StringComparison.Ordinal));
    internal static double Chance(string item, double original) => Math.Min(1, Math.Max(Supply(item) ? SupplyChance : EquipmentChance, original));
    internal static int For(string item) => Supply(item) ? Pipes : Machines;
}

/// <summary>Regional availability across the vanilla solar system, from the pack's region factors.</summary>
internal static class RegionalEconomy
{
    internal static (string Region, double Factor)[] Profiles => Economy.Pack.regions.Select(p => (p.Key, p.Value)).ToArray();
    internal static void Apply(NativeDefinitions d)
    {
        var regional = Economy.Pack.regional;
        if (regional == null) return;
        foreach (string merchant in regional.expandedMerchants)
        foreach (var machine in EquipmentEconomy.Machines)
            MarketStock.AddMissing(d, merchant, "PhobosExpanded_Manufacturing_" + merchant + "_" + machine.Prefix + "Loose", machine.Prefix + "Loose",
                StockQuantities.Chance(machine.Prefix + "Loose", 0), StockCondition.Pristine, StockQuantities.For(machine.Prefix + "Loose"));
        foreach (var profile in Profiles)
        {
            var condition = regional.refurbished.Contains(profile.Region) ? StockCondition.Refurbished : StockCondition.Pristine;
            foreach (var machine in EquipmentEconomy.Machines)
                RegionalMarkets.Add(d, profile.Region, machine.Prefix + "Loose", StockQuantities.Chance(machine.Prefix + "Loose", regional.baseChance * profile.Factor), condition, StockQuantities.For(machine.Prefix + "Loose"));
        }
    }
}
