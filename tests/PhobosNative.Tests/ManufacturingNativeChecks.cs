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

/// <summary>Manufacturing against the game's own data: the five Fennmark machines, the five materials,
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
            check(Stat(refinery, "StatBasePrice") == (damaged ? Economy.Price(RefineryRules.Prefix) / 4 : Economy.Price(RefineryRules.Prefix)), "V4 authored prices: " + state);
            check((refinery.jsonPI == RefineryRules.Prefix + "Power") == (installed && !damaged), "V4 draws power only when installed and intact: " + state);
            var processor = d.Objects[ProcessorRules.Prefix + state]; var processorItem = d.Items[processor.strItemDef];
            check(processorItem.nCols == 2 && processorItem.aSocketAdds.Length == 4 && processor.inventoryWidth == 2 && processor.inventoryHeight == 2, "X2 occupies two by two native tiles: " + state);
            check(processor.nContainerWidth == 0 && processor.aSlotsWeHave.Length == 0 && !Has(processor, "IsContainer") && Stat(processor, "StatMass") == ProcessorRules.MachineKg, "X2 carries no cargo: its water and hydrogen are records: " + state);
            check(processor.strNameFriendly.StartsWith("Phobos' Fennmark X2 ", StringComparison.Ordinal), "X2 carries the Fennmark X2 name: " + state);
            var store = d.Objects[HydrogenRules.Prefix + state]; var storeItem = d.Items[store.strItemDef];
            check(storeItem.nCols == 2 && store.jsonPI == null && store.aTickers.Length == 0 && store.nContainerWidth == 0 && !Has(store, "IsContainer") && Stat(store, "StatMass") == HydrogenRules.DryKg,
                "H2 store is a passive two by two vessel at its dry mass: " + state);
            check(store.strNameFriendly.StartsWith("Phobos' Fennmark H2 ", StringComparison.Ordinal), "H2 store carries the Fennmark H2 name: " + state);
            var reactor = d.Objects[SabatierRules.Prefix + state]; var reactorItem = d.Items[reactor.strItemDef];
            check(reactorItem.nCols == 2 && reactorItem.aSocketAdds.Length == 4 && reactor.nContainerWidth == 0 && !Has(reactor, "IsContainer") && Stat(reactor, "StatMass") == SabatierRules.MachineKg,
                "K2 is a two by two reactor with no cargo: its gases are records: " + state);
            check(reactor.strNameFriendly.StartsWith("Phobos' Fennmark K2 ", StringComparison.Ordinal) && (reactor.jsonPI == SabatierRules.Prefix + "Power") == (installed && !damaged),
                "K2 carries its Fennmark name and draws power only when installed and intact: " + state);
            var cracker = d.Objects[CrackerRules.Prefix + state]; var crackerItem = d.Items[cracker.strItemDef];
            check(crackerItem.nCols == 2 && crackerItem.aSocketAdds.Length == 4 && cracker.nContainerWidth == 0 && !Has(cracker, "IsContainer") && Stat(cracker, "StatMass") == CrackerRules.MachineKg,
                "AX-2 is a two by two cracker with no cargo: its gases are records: " + state);
            check(cracker.strNameFriendly.StartsWith("Phobos' Tolvane AX-2 ", StringComparison.Ordinal) && (cracker.jsonPI == CrackerRules.Prefix + "Power") == (installed && !damaged),
                "AX-2 carries its Tolvane name and draws power only when installed and intact: " + state);
            check(Stat(cracker, "StatBasePrice") == (damaged ? (int)Economy.Price(CrackerRules.Prefix) / 4 : (int)Economy.Price(CrackerRules.Prefix)) && Has(cracker, EquipmentEconomy.HighSalvageMark),
                "AX-2 carries its late-game price and the high-salvage mark: " + state);
            var methane = d.Objects[MethaneRules.Prefix + state];
            check(d.Items[methane.strItemDef].nCols == 2 && methane.jsonPI == null && methane.aTickers.Length == 0 && !Has(methane, "IsContainer") && Stat(methane, "StatMass") == MethaneRules.DryKg,
                "M2 store is a passive two by two vessel at its dry mass: " + state);
            check(methane.strNameFriendly.StartsWith("Phobos' Fennmark M2 ", StringComparison.Ordinal), "M2 store carries the Fennmark M2 name: " + state);
            var manifold = d.Objects[ManifoldRules.Prefix + state]; var manifoldItem = d.Items[manifold.strItemDef];
            check(manifoldItem.nCols == 1 && manifold.jsonPI == null && !Has(manifold, "IsAirtight") && !Has(manifold, "IsContainer") && Stat(manifold, "StatMass") == ManifoldRules.MachineKg,
                "P1 is a passive one-tile valve block, not airtight and not a container: " + state);
            check(manifold.strNameFriendly.StartsWith("Phobos' Fennmark P1 ", StringComparison.Ordinal) && manifold.mapPoints.Contains(ManifoldRules.Inlet + ",0,-16"),
                "P1 carries the Fennmark P1 name and a line port on its neighbouring tile: " + state);
            check(!DataHandler.dictCTs["TIsRCSValidInput"].TriggeredDataCO(new DataCO(manifold), false),
                "The game's own RCS input rule refuses the manifold, so vanilla refuels never pour nitrogen into it: " + state);
            var line = d.Objects[PropellantLineRules.Prefix + state];
            check(Stat(line, "StatMass") == PropellantLineRules.Kg && (damaged ? Stat(line, "StatBasePrice") < Economy.SupplyPrice(PropellantLineRules.Prefix) : Stat(line, "StatBasePrice") == Economy.SupplyPrice(PropellantLineRules.Prefix)) &&
                d.Installables.ContainsKey(PropellantLineRules.Prefix + state + "Dismantle"), "The propellant line is ordinary pipe supply: " + state);
            foreach (var fuel in GasStores.All)
            {
                var gas = d.Objects[fuel.Prefix + state]; var gasItem = d.Items[gas.strItemDef]; var port = fuel.Outlet;
                check(gas.mapPoints.Contains(ManifoldRules.StoreOutlet + "," + port.X + "," + port.Y), "Gas stores carry a gas-line port beside their middle row: " + fuel.Prefix + state);
                check(gasItem.nCols == fuel.Footprint && gasItem.aSocketAdds.Length == fuel.Footprint * fuel.Footprint && gas.inventoryWidth == fuel.Footprint && gas.jsonPI == null &&
                    !Has(gas, "IsContainer") && Stat(gas, "StatMass") == fuel.DryKg, "Each size is a passive vessel of its own footprint at its dry mass: " + fuel.Prefix + state);
                check(gas.strNameFriendly.StartsWith("Phobos' Fennmark " + fuel.Family.Model + fuel.Footprint + " ", StringComparison.Ordinal), "Each size carries its Fennmark model: " + fuel.Prefix + state);
                check(Stat(gas, "StatBasePrice") == (damaged ? (int)fuel.Price / 4 : (int)fuel.Price) && Has(gas, EquipmentEconomy.HighSalvageMark), "Each size carries its late-game price: " + fuel.Prefix + state);
                check(installed ? gasItem.aSocketAdds[port.SocketIndex] == PropellantLineRules.Prefix + "FixturePort" : true, "The pipe joint sits on the footprint tile beside the port: " + fuel.Prefix + state);
                check(d.Installables.ContainsKey(fuel.Prefix + state + "Dismantle") && (damaged ? d.Installables.ContainsKey(fuel.Prefix + state + "Repair") : d.Installables.ContainsKey(fuel.Prefix + state + "Restore")),
                    "Every size has native dismantle, repair and Restore jobs: " + fuel.Prefix + state);
                check(BulkVessels.SpecFor(fuel.Prefix + state) == fuel.Spec, "Every form resolves to its own registered size: " + fuel.Prefix + state);
            }
            var filler = d.Objects[FillerRules.Prefix + state]; var fillerItem = d.Items[filler.strItemDef];
            check(fillerItem.nCols == 2 && filler.inventoryWidth == 2 && filler.nContainerWidth == FillerRules.RackCells && filler.nContainerHeight == 1 && filler.strContainerCT == FillerRules.RackTrigger &&
                !Has(filler, "IsAirtight") && Stat(filler, "StatMass") == FillerRules.MachineKg && filler.aInteractions.Contains("Inventory"),
                "L2 is a two by two station with a four-cell bottle rack behind the game's own Inventory window: " + state);
            check(filler.strNameFriendly.StartsWith("Phobos' Fennmark L2 ", StringComparison.Ordinal) && filler.mapPoints.Contains(FillerRules.Inlet + ",24,8") &&
                (filler.jsonPI == FillerRules.Prefix + "Power") == (installed && !damaged), "L2 carries its Fennmark name, a gas-line port and power only when installed and intact: " + state);
            check(!DataHandler.dictCTs["TIsRCSValidInput"].TriggeredDataCO(new DataCO(filler), false), "The game's own RCS input rule refuses the station: " + state);
            check(filler.aInteractions.Contains(FillerRules.BottleOrder) == (installed && !damaged), "Only the intact installed L2 offers Keep suit bottles charged: " + state);
            var regulator = d.Objects[RegulatorRules.Prefix + state]; var regulatorItem = d.Items[regulator.strItemDef];
            check(regulatorItem.nCols == 2 && regulator.inventoryWidth == 2 && !Has(regulator, "IsAirtight") && Stat(regulator, "StatMass") == RegulatorRules.MachineKg &&
                regulator.strNameFriendly.StartsWith("Phobos' Fennmark A2 ", StringComparison.Ordinal) && regulator.mapPoints.Contains(RegulatorRules.Inlet + ",24,8") &&
                (regulator.jsonPI == RegulatorRules.Prefix + "Power") == (installed && !damaged),
                "A2 is a two by two Fennmark unit with a gas-line port, not airtight, powered only when installed and intact: " + state);
            check(Stat(regulator, "StatBasePrice") == (damaged ? (int)Economy.Price(RegulatorRules.Prefix) / 4 : (int)Economy.Price(RegulatorRules.Prefix)) && Has(regulator, EquipmentEconomy.HighSalvageMark),
                "A2 carries its late-game price and the high-salvage mark: " + state);
            check(installed ? regulatorItem.aSocketAdds[1] == PropellantLineRules.Prefix + "FixturePort" : true, "A2 pipe joint sits on its port tile: " + state);
            if (installed)
                foreach (var co in new[] { refinery, processor, store, reactor, cracker, methane, manifold, filler, regulator })
                    check(co.aInteractions.Count(i => i == Definitions.Controls) == 1, "Installed machine offers one Control Panel: " + co.strName);
            foreach (var prefix in new[] { RefineryRules.Prefix, ProcessorRules.Prefix, HydrogenRules.Prefix, SabatierRules.Prefix, CrackerRules.Prefix, MethaneRules.Prefix, ManifoldRules.Prefix, FillerRules.Prefix, RegulatorRules.Prefix })
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
        check(!d.Power.ContainsKey(HydrogenRules.Prefix + "Power") && !d.Power.ContainsKey(MethaneRules.Prefix + "Power") && !d.Power.ContainsKey(ManifoldRules.Prefix + "Power") &&
            !d.Power.ContainsKey(PropellantLineRules.Prefix + "Power"), "The fuel stores, the manifold and the line have no power info");
        // The propellant line is its own pipe family: its own segment condition and sprite trigger, never coolant or irrigation.
        var lineTrigger = d.Triggers[PropellantLineRules.Prefix + "Sprite"];
        check(lineTrigger.aReqs.SequenceEqual(new[] { PropellantLineRules.Segment }) && d.Items[PropellantLineRules.Installed].ctSpriteSheet == PropellantLineRules.Prefix + "Sprite" &&
            d.Items[PropellantLineRules.Installed].bHasSpriteSheet, "The propellant line joins only its own segments");
        check(new[] { PropellantLineRules.Segment, "PhobosFurnaceCoolantSegment", "PhobosWaterConduitPresent" }.Distinct().Count() == 3, "Propellant, coolant and irrigation segments are distinct");
        // Both regulators put their gas inputs on neighbouring tiles, where a one-tile canister or manifold sits.
        foreach (string regulator in new[] { "ItmRCSDistro01", "ItmRCSDistro02" })
            check(DataHandler.dictCOs[regulator].mapPoints.Any(m => m.StartsWith("GasInput", StringComparison.Ordinal)) && DataHandler.dictItemDefs[DataHandler.dictCOs["ItmRTAN2"].strItemDef].nCols == 1,
                "The regulator has gas-input points and a canister is one tile: " + regulator);
        var bed = d.Power[SabatierRules.Prefix + "Power"];
        check(Math.Abs(bed.fAmount - SabatierRules.IdleKW / Units.SecondsPerHour) < 1e-12 && bed.strOverrideCond == ManufacturingRules.Reacting && Math.Abs(bed.fOverrideAmount - SabatierRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && bed.aInputPts.SequenceEqual(new[] { "PowerA" }),
            "K2 draws 0.02 kW idle and 1.2 kW reacting through one input point");
        var crackerPower = d.Power[CrackerRules.Prefix + "Power"];
        check(Math.Abs(crackerPower.fAmount - CrackerRules.IdleKW / Units.SecondsPerHour) < 1e-12 && crackerPower.strOverrideCond == ManufacturingRules.Reacting &&
            Math.Abs(crackerPower.fOverrideAmount - CrackerRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && crackerPower.aInputPts.SequenceEqual(new[] { "PowerA" }),
            "AX-2 draws 0.02 kW idle and 2 kW cracking through one input point");
        check(CrackerRules.Balanced() && NativeGasCanister.IsRoomSpecies(CrackerRules.AmmoniaSpecies) && NativeGasCanister.IsRoomSpecies(CrackerRules.NitrogenSpecies),
            "The cracker conserves mass with the game's molar masses, and NH3 and N2 are game gases");
        // Sabatier with the game's own molar masses, and the game's own CO2 canister rule.
        check(SabatierRules.Balanced() && NativeGasCanister.IsRoomSpecies(SabatierRules.CarbonDioxide) && NativeGasCanister.IsRoomSpecies(SabatierRules.MethaneSpecies),
            "The reactor conserves mass with the game's molar masses, and CO2 and CH4 are game gases");
        var co2Trigger = DataHandler.dictCTs[SabatierRules.CanisterTrigger];
        check(co2Trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmRTACO2"]), false) && !co2Trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmRTACO2Loose"]), false) &&
            !co2Trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmRTAO2"]), false), "The game's own trigger names installed CO2 canisters only");
        check(NativeGasCanister.Species(null) == null && DataHandler.dictCOs["ItmRTACO2"].aStartingConds.Any(s => s.StartsWith("IsVesselCO2=", StringComparison.Ordinal)),
            "The native CO2 canister is rated for CO2, the species the reactor draws");
        var methaneSpec = BulkVessels.SpecFor(MethaneRules.Installed);
        check(methaneSpec != null && methaneSpec.Commodity == "methane" && methaneSpec.CapacityKg == MethaneRules.CapacityKg && methaneSpec.DamagePolicy == VesselDamagePolicy.Leak && methaneSpec.Owner == Plugin.Id,
            "The M2 store is a registered leaking methane vessel");
        check(new[] { BulkVessels.SpecFor(HydrogenRules.Installed), methaneSpec, BulkVessels.SpecFor("PhobosProcessSiloInstalled") }.Select(s => s?.Commodity).Distinct().Count() == 3,
            "Water, hydrogen and methane vessels coexist in the registry");
        check(GasStores.All.All(s => BulkVessels.SpecFor(s.Installed) == s.Spec) && BulkVessels.All.Count(s => s.Owner == Plugin.Id) == GasStores.All.Count,
            "Every gas store size is registered once, by this mod");
        // The L2 rack admits the game's suit O2 bottles only; the power override follows the filling condition.
        var rack = DataHandler.dictCTs[FillerRules.RackTrigger];
        check(rack.TriggeredDataCO(new DataCO(DataHandler.dictCOs["ItmCanisterO2Small"]), false), "The rack admits the game's suit O2 bottle");
        foreach (string outside in new[] { "ItmRTAO2Loose", "ItmRTAN2", "ItmScrapSteel", "ItmCanisterLH02Loose" })
            check(!rack.TriggeredDataCO(new DataCO(DataHandler.dictCOs[outside]), false), "The rack refuses anything else: " + outside);
        var booster = d.Power[FillerRules.Prefix + "Power"];
        check(Math.Abs(booster.fAmount - FillerRules.IdleKW / Units.SecondsPerHour) < 1e-12 && booster.strOverrideCond == ManufacturingRules.Filling &&
            Math.Abs(booster.fOverrideAmount - FillerRules.WorkingKW / Units.SecondsPerHour) < 1e-12, "L2 draws 0.05 kW idle and 3 kW filling");
        // The game's own vessels the station fills: rated O2, N2 and CO2 canisters and the suit bottle.
        foreach (string vessel in new[] { "ItmRTAO2", "ItmRTAN2", "ItmRTACO2", "ItmCanisterO2Small" })
            check(NativeGasCanister.Species(null) == null && DataHandler.dictCOs[vessel].aStartingConds.Any(s => s.StartsWith("StatGasPressureMax=", StringComparison.Ordinal)) &&
                DataHandler.dictCOs[vessel].aStartingConds.Any(s => s.StartsWith("IsVessel", StringComparison.Ordinal)), "A native vessel carries its rated pressure and species: " + vessel);
        check(DataHandler.dictCOs["ItmCanisterO2Small"].aStartingConds.Any(s => s.StartsWith("IsHandheld=", StringComparison.Ordinal)), "The suit bottle is handheld, which the rack and the fill rule use");

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
        check(RefineryRecipes.Available(true).Count() == 6 && RefineryRecipes.Available(false).Count() == 5 && withoutShipbreaker.Objects.Count == d.Objects.Count, "Shipbreaker's presence changes only the available charges, never the definitions");
        check(ShipbreakerStock.Definitions(), "Shipbreaker's steel ingot and remainder are published at the masses the steel charge expects");

        // Refining value guardrails (agent proposal under the owner's 30 September 2026 direction, in place of the
        // retired every-charge-loses-value rule), at live prices with Shipbreaker's steel ingot included:
        // 1. sellable products of any charge are worth at most one and a half times its inputs (a gain reflects real
        //    work, never a windfall); 2. a charge whose inputs are all station-bought stock gains at most a quarter
        //    at base prices, well inside the game's buy/sell spread, so no repeatable trade loop pays; 3. commodity
        //    records (water, stored gases) are valued at the station price for information only, because they have
        //    no sell route. Off-gas has no value.
        double Price(string id) => id == ManufacturingRules.Water ? PhobosShipbreaker.Core.SiloRules.WaterPricePerKg
            : Stat(d.Objects.TryGetValue(id, out var own) ? own : DataHandler.dictCOs[id], "StatBasePrice");
        // Stored gases (ammonia) are valued at the game's own gas price per kilogram, read from its GasPrices table (the
        // table GasContainer.GetGasPrice reads in a running game).
        double GasPrice(string species) => double.Parse(DataHandler.dictLoot["GasPrices"].aCOs.Single(e => e.StartsWith(species + "=", StringComparison.Ordinal)).Split('x')[1],
            System.Globalization.CultureInfo.InvariantCulture);
        double ProductValue(ProductSpec p) => p.Id == ManufacturingRules.Water ? p.Count * p.Kg * Price(p.Id) :
            GasStores.FamilyOf(p.Id) is GasFamily gas ? p.Count * p.Kg * GasPrice(gas.Species) : p.Count * Price(p.Id);
        bool Sellable(string id) => id != ManufacturingRules.Water && GasStores.FamilyOf(id) == null;
        bool Bought(string id) => d.Objects.ContainsKey(id) && Economy.Pack.regional != null && Economy.Pack.regional.items.ContainsKey(id)
            || PhobosShipbreaker.Core.ShipbreakerEconomy.Pack.regional?.items.ContainsKey(id) == true;
        foreach (var recipe in RefineryRecipes.All)
        {
            double inValue = recipe.Inputs.Sum(i => i.Count * Price(i.Id));
            double sellable = recipe.Products.Where(p => Sellable(p.Id)).Sum(ProductValue);
            double commodities = recipe.Products.Where(p => !Sellable(p.Id)).Sum(ProductValue);
            check(sellable <= 1.5 * inValue, $"The {recipe.Id} charge's sellable products stay within half again its inputs: {sellable:F2} out of {inValue:F2} in ({commodities:F2} of commodities aside)");
            if (recipe.Inputs.All(i => Bought(i.Id)))
                check(sellable <= 1.25 * inValue, $"The {recipe.Id} charge, fed from bought stock, gains at most a quarter at base prices: {sellable:F2} out of {inValue:F2}");
        }
        check(!RefineryRecipes.All.SelectMany(r => r.Inputs).Any(i => Bought(i.Id)), "No V4 charge is fed from bought stock: ores and chunks are mined, and nickel-iron ingots and carbon come only from the V4 (the bought-stock guardrail is ready for a future charge)");
        check(GasPrice("NH3") > 0 && RefineryRecipes.Ammonium.StoredGases.All(p => GasStores.FamilyOf(p.Id) != null),
            "The game prices ammonia, and the salt crust charge's ammonia goes to a store family");

        // The clay chunk takes a carved share of the game's C-class roll, taken from silicates, and no shop.
        var cClass = DataHandler.dictLoot[MiningLoot.Table];
        var carve = d.LootCarves[MiningLoot.Table][Materials.ClayHydrates];
        check(cClass.strType == "item" && carve.Donor == MiningLoot.Donor && carve.Share == MiningLoot.Chance, "The clay chunk is carved from the C-class silicates share");
        var units = cClass.aCOs.Single().Split('|');
        int silicates = Array.FindIndex(units, u => u.StartsWith(MiningLoot.Donor + "=", StringComparison.Ordinal));
        int clayAt = Array.FindIndex(units, u => u == Materials.ClayHydrates + "=0.1x1");
        check(units.Count(u => u.StartsWith(Materials.ClayHydrates + "=", StringComparison.Ordinal)) == 1 && silicates >= 0 && clayAt > silicates &&
              units.Skip(silicates + 1).Take(clayAt - silicates - 1).All(u => d.LootCarves[MiningLoot.Table].ContainsKey(u.Split('=')[0]) || u.StartsWith("ItmIce01=", StringComparison.Ordinal)),
            "The live C-class table holds the clay chunk once, among the carves that follow silicates");
        foreach (string wall in new[] { "ItmRock05SalvageOutput", "ItmRock06SalvageOutput" })
            check(!DataHandler.dictLoot[wall].aCOs.Any(c => c.Contains(Materials.ClayHydrates)) && !DataHandler.dictLoot[wall].aLoots.Any(l => l.StartsWith("PhobosManufacturingClay_", StringComparison.Ordinal)),
                "Dark walls reach clay only through their nested C-class roll, never a second time: " + wall);
        check(!d.Loot.Keys.Any(k => k.StartsWith("PhobosManufacturingClay_", StringComparison.Ordinal)) && !d.LootBranches.ContainsKey(MiningLoot.Table), "The retired additive clay branches are gone");
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
            var loose = new DataCO(d.Objects[e.Prefix + "Loose"]); var broken = new DataCO(d.Objects[e.Prefix + "LooseDmg"]);
            check(DataHandler.dictCTs["TIsBarterSanDiegoHalvorsonSell"].TriggeredDataCO(loose, false) && DataHandler.dictCTs["TIsBarterVORBScrapKiosk"].TriggeredDataCO(loose, false), "Native sell and buy filters accept the machine: " + e.Prefix);
            // Late-game kit trades like the game's own: the fixer buys it intact, the ordinary supplies kiosk does not, Venus buys either.
            check(Definitions.Forms.All(s => Has(d.Objects[e.Prefix + s], EquipmentEconomy.HighSalvageMark)) && Has(DataHandler.dictCOs["ItmReactorIC03OffLoose"], EquipmentEconomy.HighSalvageMark),
                "Every form carries the game's high-salvage mark, as the IC fusion reactor does: " + e.Prefix);
            check(DataHandler.dictCTs["TIsBarterOKLGFixerBuy"].TriggeredDataCO(loose, false) && !DataHandler.dictCTs["TIsBarterOKLGFixerBuy"].TriggeredDataCO(broken, false) &&
                !DataHandler.dictCTs["TIsBarterOKLGSupplyKiosk"].TriggeredDataCO(loose, false) && DataHandler.dictCTs["TIsBarterVORBScrapKiosk"].TriggeredDataCO(broken, false),
                "The K-Leg fixer buys the intact machine, the supplies kiosk does not, Venus buys the broken one: " + e.Prefix);
            // Priced among the game's late-game equipment: above the towing brace's loose half, below the IC fusion reactor.
            double price = Stat(d.Objects[e.Prefix + "Loose"], "StatBasePrice");
            check(price > Stat(DataHandler.dictCOs["ItmTowingBrace01Loose"], "StatBasePrice") && price < Stat(DataHandler.dictCOs["ItmReactorIC03OffLoose"], "StatBasePrice"),
                "Late-game price band: " + e.Prefix);
        }
        // World finds are rare and mostly broken.
        var salvage = d.Loot["PhobosManufacturingMachinerySalvage"].aCOs.Single().Split('|')
            .ToDictionary(c => c.Split('=')[0], c => double.Parse(c.Split('=')[1].Split('x')[0], System.Globalization.CultureInfo.InvariantCulture));
        check(Math.Abs(salvage.Values.Sum() - EquipmentEconomy.MachinerySalvageChance) < 1e-9 && EquipmentEconomy.MachinerySalvageChance <= 0.05 &&
            Math.Abs(salvage.Where(p => p.Key.EndsWith("Dmg", StringComparison.Ordinal)).Sum(p => p.Value) - 0.75 * EquipmentEconomy.MachinerySalvageChance) < 1e-9 && salvage.Count == 2 * EquipmentEconomy.Machines.Count(m => m.Loot) && !salvage.Keys.Any(k => k.Contains("Medium") || k.Contains("Large")),
            "At most one engineering roll in twenty yields a machine, three in four of those broken; medium and large stores never turn up in salvage");

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
