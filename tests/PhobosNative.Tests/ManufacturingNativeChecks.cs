using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing;
using PhobosManufacturing.Core;

/// <summary>Manufacturing 0.1.0 against the game's own data: the three Fennmark machines, the five materials,
/// the feed rule at the game level, the native feed masses the chemistry depends on, the mining tables the clay
/// chunk joins, the native oxygen canister the electrolyser fills, the hydrogen vessel, the economy contract,
/// and the native fire and explosion machinery the hazards rely on.</summary>
internal static class ManufacturingNativeChecks
{
    internal static void Run(NativeDefinitions d, NativeDefinitions withoutShipbreaker, string game, string repo, Action<bool, string> check, Action<Action, string> throws)
    {
        double Stat(JsonCondOwner co, string key) => EquipmentSaveUpgrade.Amount(co.aStartingConds, key);
        bool Has(JsonCondOwner co, string key) => co.aStartingConds.Any(s => s.Split('=')[0] == key);
        foreach (string state in Definitions.Forms)
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            var refinery = d.Objects[RefineryRules.Prefix + state]; var refineryItem = d.Items[refinery.strItemDef];
            check(refineryItem.nCols == 4 && refineryItem.aSocketAdds.Length == 16 && refinery.inventoryWidth == 4 && refinery.inventoryHeight == 4, "V4 occupies four by four native tiles: " + state);
            check(refinery.nContainerWidth == 8 && refinery.nContainerHeight == 8 && refinery.aSlotsWeHave.Contains(RefineryRules.InputSlot) && Stat(refinery, "StatMass") == RefineryRules.MachineKg, "V4 has an eight by eight tray, its charge feed and weighs 180 kg: " + state);
            check(refinery.strNameFriendly.StartsWith("Phobos' Fennmark V4 ", StringComparison.Ordinal), "V4 carries the Fennmark V4 name: " + state);
            check(Stat(refinery, "StatBasePrice") == (damaged ? RefineryRules.Price / 4 : RefineryRules.Price), "V4 authored prices: " + state);
            check((refinery.jsonPI == RefineryRules.Prefix + "Power") == (installed && !damaged), "V4 draws power only when installed and intact: " + state);
            var processor = d.Objects[ProcessorRules.Prefix + state]; var processorItem = d.Items[processor.strItemDef];
            check(processorItem.nCols == 2 && processorItem.aSocketAdds.Length == 4 && processor.inventoryWidth == 2 && processor.inventoryHeight == 2, "X2 occupies two by two native tiles: " + state);
            check(processor.nContainerWidth == 0 && processor.aSlotsWeHave.Length == 0 && !Has(processor, "IsContainer") && Stat(processor, "StatMass") == ProcessorRules.MachineKg, "X2 carries no cargo: its water and hydrogen are records: " + state);
            check(processor.strNameFriendly.StartsWith("Phobos' Fennmark X2 ", StringComparison.Ordinal), "X2 carries the Fennmark X2 name: " + state);
            var store = d.Objects[HydrogenRules.Prefix + state]; var storeItem = d.Items[store.strItemDef];
            check(storeItem.nCols == 2 && store.jsonPI == null && store.aTickers.Length == 0 && store.nContainerWidth == 0 && !Has(store, "IsContainer") && Stat(store, "StatMass") == HydrogenRules.DryKg,
                "H2 store is a passive two by two vessel at its dry mass: " + state);
            check(store.strNameFriendly.StartsWith("Phobos' Fennmark H2 ", StringComparison.Ordinal), "H2 store carries the Fennmark H2 name: " + state);
            if (installed)
                foreach (var co in new[] { refinery, processor, store })
                    check(co.aInteractions.Count(i => i == Definitions.Controls) == 1, "Installed machine offers one Control Panel: " + co.strName);
            foreach (var prefix in new[] { RefineryRules.Prefix, ProcessorRules.Prefix, HydrogenRules.Prefix })
            {
                check(d.Installables.ContainsKey(prefix + state + "Dismantle") && d.Installables.ContainsKey(prefix + state + (installed ? "Uninstall" : "Install")), "Native removal and dismantle jobs exist: " + prefix + state);
                check(damaged ? d.Installables.ContainsKey(prefix + state + "Repair") : d.Installables.ContainsKey(prefix + state + "Restore"), "Repair on damaged forms, Restore on intact ones: " + prefix + state);
            }
        }
        // The feed at the game level: any ore or our stock through native containment; the exact rule narrows it.
        var feed = d.Objects[RefineryRules.InputBin]; var trigger = DataHandler.dictCTs[feed.strContainerCT];
        check(feed.nContainerWidth * feed.nContainerHeight == RefineryRules.FeedCapacity && d.Slots[RefineryRules.InputSlot].bHide, "The charge feed holds six units as a hidden slot with its own window");
        foreach (string id in new[] { RefineryRules.Hydrates, RefineryRules.Iron, RefineryRules.Carbides, "ItmMineral02" })
            check(trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[id]), false), "Every native ore enters the feed at the game level: " + id);
        foreach (string id in new[] { Materials.ClayHydrates, Materials.NickelIronIngot, Materials.CarbonStock })
            check(trigger.TriggeredDataCO(new DataCO(d.Objects[id]), false), "Our chunk and stock enter the feed at the game level: " + id);
        foreach (string outside in new[] { "ItmIce01", "ItmMineralStone01", RefineryRules.Gangue, "ItmScrapSteel", "ItmCanisterLH02Loose" })
            check(!trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[outside]), false), "Ice, regolith, gangue, scrap and canisters never enter the feed: " + outside);
        foreach (string outside in new[] { Materials.RefinerySlag, Materials.AnhydrousResidue })
            check(!trigger.TriggeredDataCO(new DataCO(d.Objects[outside]), false), "Terminal remainders never enter the feed: " + outside);
        check(!RefineryRules.ValidFeed("ItmMineral02", 10, true, true, true, true), "Olivine passes the game-level ore rule and is refused by identity");
        check(Stat(DataHandler.dictCOs[RefineryRules.Hydrates], "StatMass") == RefineryRules.HydratesKg && Stat(DataHandler.dictCOs[RefineryRules.Iron], "StatMass") == RefineryRules.IronKg &&
            Stat(DataHandler.dictCOs[RefineryRules.Carbides], "StatMass") == RefineryRules.CarbidesKg && Stat(DataHandler.dictCOs[RefineryRules.Gangue], "StatMass") == RefineryRules.GangueKg,
            "The chemistry uses the game's own masses: hydrates 10, iron 20, carbides 10, gangue 3 kg");
        check(DataHandler.dictCOs[RefineryRules.Hydrates].nStackLimit > 1 && DataHandler.dictCOs[RefineryRules.Iron].nStackLimit > 1, "Ores stack natively, so the feed refuses merging");
        check(DataHandler.dictCOs[RefineryRules.Iron].aStartingConds.Any(s => s.StartsWith("StatDamage=", StringComparison.Ordinal)), "Ores spawn with damage, which the feed rule ignores");
        var power = d.Power[RefineryRules.Prefix + "Power"];
        check(Math.Abs(power.fAmount - RefineryRules.IdleKW / Units.SecondsPerHour) < 1e-12 && power.strOverrideCond == ManufacturingRules.Working && Math.Abs(power.fOverrideAmount - RefineryRules.WorkingKW / Units.SecondsPerHour) < 1e-12,
            "V4 draws 0.1 kW idle and 24 kW working");
        var cell = d.Power[ProcessorRules.Prefix + "Power"];
        check(Math.Abs(cell.fAmount - ProcessorRules.IdleKW / Units.SecondsPerHour) < 1e-12 && cell.strOverrideCond == ManufacturingRules.Electrolysing && Math.Abs(cell.fOverrideAmount - ProcessorRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && cell.aInputPts.SequenceEqual(new[] { "PowerA" }),
            "X2 draws 0.02 kW idle and 6 kW electrolysing through one input point");
        check(!d.Power.ContainsKey(HydrogenRules.Prefix + "Power"), "The hydrogen store has no power info");

        // Materials: clones of the game's own scrap and hydrates with our identity, mass, price and category.
        foreach (var m in Materials.All)
        {
            var co = d.Objects[m.Id];
            check(Stat(co, "StatMass") == m.Kg && Stat(co, "StatBasePrice") == m.Price && co.nStackLimit == m.Stack && Has(co, m.Category) && Has(co, m.Id + "Identity"),
                "Material carries its mass, price, stack and category: " + m.Id);
            check(co.strNameFriendly.StartsWith("Phobos' Fennmark ", StringComparison.Ordinal), "Material is branded: " + m.Id);
            check(d.Items.ContainsKey(co.strItemDef), "Material item definition exists: " + m.Id);
        }
        var clay = new DataCO(d.Objects[Materials.ClayHydrates]);
        check(clay.HasCond("IsOre") && clay.HasCond("IsMineral") && clay.HasCond("IsCategoryOre") && DataHandler.dictCTs["TIsMiningOutput"].TriggeredDataCO(clay, false), "The clay chunk is an ore the government kiosks buy like other minerals");
        check(d.Items[Materials.ClayHydrates].strImg == DataHandler.dictItemDefs[DataHandler.dictCOs[RefineryRules.Hydrates].strItemDef].strImg, "The clay chunk uses the game's hydrate art at runtime until its own master exists");
        check(new DataCO(d.Objects[Materials.RefinerySlag]).HasCond("IsCategoryTrash") && new DataCO(d.Objects[Materials.AnhydrousResidue]).HasCond("IsCategoryTrash"), "Terminal remainders are native trash");
        check(new DataCO(d.Objects[Materials.NickelIronIngot]).HasCond("IsCategoryMetals"), "Nickel-iron trades as a metal");
        foreach (string id in new[] { Materials.NickelIronIngot, Materials.CarbonStock, Materials.ClayHydrates, Materials.RefinerySlag })
            check(DataHandler.dictCTs["TIsBarterVORBScrapKiosk"].TriggeredDataCO(new DataCO(d.Objects[id]), false), "A native buyer accepts the material: " + id);
        check(!DataHandler.dictCTs["TIsBarterFlotillaScrapKiosk"].TriggeredDataCO(new DataCO(d.Objects[Materials.NickelIronIngot]), false), "Scrap kiosks do not buy ingots: they are not IsScrap, so they are not feed either");

        // With and without Shipbreaker: the same definitions, a different available catalog.
        check(RefineryRecipes.Available(true).Count() == 5 && RefineryRecipes.Available(false).Count() == 4 && withoutShipbreaker.Objects.Count == d.Objects.Count, "Shipbreaker's presence changes only the available charges, never the definitions");
        check(ShipbreakerStock.Definitions(), "Shipbreaker's steel ingot and remainder are published at the masses the steel charge expects");

        // The clay chunk joins the game's own mining tables once each, and no shop.
        foreach (string table in MiningLoot.Tables)
        {
            string branch = "PhobosManufacturingClay_" + table;
            check(DataHandler.dictLoot.ContainsKey(table) && DataHandler.dictLoot[table].strType == "item", "The native mining table exists: " + table);
            check(d.Loot.ContainsKey(branch) && d.Loot[branch].aCOs.Single().StartsWith(Materials.ClayHydrates + "=", StringComparison.Ordinal) && d.LootBranches[table].Count(b => b == branch) == 1,
                "The clay chunk is one bounded choice in " + table);
        }
        check(!d.Loot.Values.Any(l => l.strName.StartsWith("PhobosStock_", StringComparison.Ordinal) && l.aCOs.Any(c => Materials.All.Any(m => c.StartsWith(m.Id + "=", StringComparison.Ordinal)))), "No material is sold in a shop: ores and stock are mined or made");

        // The native oxygen canister the electrolyser fills, by the game's own rating.
        var rta = DataHandler.dictCOs["ItmRTAO2"];
        double capacity = NativeGasCanister.CapacityMoles(Stat(rta, "StatVolume"), Stat(rta, "StatGasPressureMax"), Stat(rta, "StatGasTemp"));
        check(Math.Abs(capacity - Stat(rta, "StatGasMolO2")) / Stat(rta, "StatGasMolO2") < 0.001, "The rated capacity matches the game's full RTA O2 canister within 0.1%");
        check(NativeGasCanister.Species(null) == null, "No canister, no species");
        var installedTrigger = DataHandler.dictCTs[ProcessorRules.CanisterTrigger];
        check(installedTrigger.TriggeredDataCO(new DataCO(rta), false) && !installedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmRTAO2Loose"]), false) && !installedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmRTACO2"]), false),
            "The game's own trigger names installed O2 canisters only");
        check(Math.Abs(ProcessorRules.OxygenMolesPerCycle * 428 - capacity) / capacity < 0.01, "About 428 cycles fill an empty canister");

        // The hydrogen vessel.
        var spec = BulkVessels.SpecFor(HydrogenRules.Installed);
        check(spec != null && spec.Commodity == "hydrogen" && spec.CapacityKg == 24 && spec.DryKg == 160 && spec.DamagePolicy == VesselDamagePolicy.Leak && spec.Owner == Plugin.Id, "The H2 store is a registered leaking hydrogen vessel");
        check(BulkVessels.SpecFor("PhobosProcessSiloInstalled")?.Commodity == "water" && BulkVessels.SpecFor(HydrogenRules.Installed) != BulkVessels.SpecFor("PhobosProcessSiloInstalled"), "Shipbreaker's water silo and our hydrogen store coexist in the registry");

        // Economy: mass-balanced salvage that loses value, repair bills in half-kilogram packs, restore rates.
        foreach (var e in EquipmentEconomy.Machines)
        foreach (string state in Definitions.Forms)
        {
            string id = e.Prefix + state;
            var dismantle = d.Installables[id + "Dismantle"];
            double outKg = dismantle.aLootCOs.Sum(p => Stat(DataHandler.dictCOs[p], "StatMass")), outValue = dismantle.aLootCOs.Sum(p => Stat(DataHandler.dictCOs[p], "StatBasePrice"));
            check(Math.Abs(outKg - Stat(d.Objects[id], "StatMass")) < 1e-6, "Whole equipment salvage conserves mass: " + id);
            check(outValue < Stat(d.Objects[id], "StatBasePrice") * .25, "Buying equipment for immediate scrap is not profitable: " + id);
            if (state.EndsWith("Dmg", StringComparison.Ordinal))
            {
                double inputKg = e.RepairBill.Select((count, i) => count * Stat(DataHandler.dictCOs[EquipmentEconomy.Materials[i]], "StatMass")).Sum();
                check(MaintenanceSafety.SpentPartUnits(inputKg) * .5 == inputKg, "Repair bill fits retained half-kg service packs: " + id);
            }
            else
            {
                var restore = d.Installables[id + "Restore"]; var effect = d.Loot[restore.strAllowLootCTsThem];
                double removal = double.Parse(effect.aCOs.Single().Split('x').Last(), System.Globalization.CultureInfo.InvariantCulture);
                check(Math.Abs(Stat(d.Objects[id], "StatDamageMax") / removal * restore.fDuration * 60 - e.RestoreMinutes) < .0001, "Restore rate matches the authored minutes: " + id);
            }
        }
        foreach (var e in EquipmentEconomy.Machines)
        foreach (string merchant in new[] { "ItmOKLGSupplyKioskInv", "ItmOKLGFixer", "ItmTraderSanDiegoHalvorsonInv", "ItmVORBScrapKioskInv" })
            check(d.LootBranches.TryGetValue(merchant, out var branches) && branches.Any(b => d.Loot[b].aCOs.Any(c => c.StartsWith(e.Prefix + "Loose", StringComparison.Ordinal))), "Every general merchant offers the machine: " + e.Prefix + " at " + merchant);
        foreach (var e in EquipmentEconomy.Machines)
        {
            var loose = new DataCO(d.Objects[e.Prefix + "Loose"]);
            check(DataHandler.dictCTs["TIsBarterSanDiegoHalvorsonSell"].TriggeredDataCO(loose, false) && DataHandler.dictCTs["TIsBarterVORBScrapKiosk"].TriggeredDataCO(loose, false), "Native sell and buy filters accept the machine: " + e.Prefix);
        }

        // The hazard model's native foundations: the fire gas-respire, the spread trigger, the explosion entries.
        string data = Path.Combine(game, "Ostranauts_Data", "StreamingAssets", "data");
        check(Directory.GetFiles(Path.Combine(data, "gasrespires"), "*.json", SearchOption.AllDirectories).Any(f => File.ReadAllText(f).Contains("\"Fire\"")), "The game's own fire gas-respire exists");
        check(DataHandler.dictCTs.ContainsKey("TIsFireSpreadable") && DataHandler.dictCOs.ContainsKey("SysFire"), "The game's own fire object and spread rule exist");
        check(Directory.GetFiles(data, "*.json", SearchOption.AllDirectories).Any(f => File.ReadAllText(f).Contains("\"AModeExplosionTradSmall\"")), "The native explosion attack modes exist");
        var explosions = JArray.Parse(File.ReadAllText(Path.Combine(repo, "mods/PhobosManufacturing/data/explosions/phobos_manufacturing.json")));
        var nativeExplosion = JArray.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(data, "explosions"), "*.json", SearchOption.AllDirectories).First())).First();
        foreach (string size in new[] { "Small", "Medium", "Large" })
        {
            var entry = explosions.FirstOrDefault(e => (string?)e["strName"] == "PhobosDeflagration" + size);
            check(entry != null && ((JObject)nativeExplosion).Properties().All(p => entry[p.Name] != null), "The deflagration entry has every field the game's own entries have: " + size);
            var sys = d.Objects[HydrogenRules.DeflagrationPrefix + size];
            check(sys.aUpdateCommands.Contains("Explosion,PhobosDeflagration" + size) && new DataCO(sys).HasCond("IsSystem") && d.Items.ContainsKey(sys.strItemDef), "The deflagration object carries the native explosion command: " + size);
        }
        check(DataHandler.dictLoot.ContainsKey("ItmRTAO2Dmg") || DataHandler.dictCOs["ItmRTAO2Dmg"].aUpdateCommands.Any(c => c.StartsWith("GasExchange,", StringComparison.Ordinal)), "A damaged native canister leaks through the game's own gas exchange");
        throws(() => new Material("PhobosBad", 0, 1, 1, "IsCategoryTrash", true, 1, ""), "A material without mass is refused");
    }
}
