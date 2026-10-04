using System;
using System.Collections.Generic;
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
            check(refinery.nContainerWidth == 4 && refinery.nContainerHeight == 3 && refinery.aSlotsWeHave.Contains(RefineryRules.InputSlot) && Stat(refinery, "StatMass") == RefineryRules.MachineKg, "V4 has an eight by eight tray, its charge feed and weighs 180 kg: " + state);
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
            check(!d.Objects.ContainsKey(PropellantLineRules.Prefix + state), "The gas line is Framework's since Manufacturing 0.23.0, not defined twice: " + state);
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
        // The Lixivar LC-3: a 3 x 3 charge machine with its own feed rule, power and name.
        foreach (string state in Definitions.Forms)
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            var leach = d.Objects[LeachRules.Prefix + state]; var leachItem = d.Items[leach.strItemDef];
            check(leachItem.nCols == 3 && leachItem.aSocketAdds.Length == 9 && leach.inventoryWidth == 3 && leach.inventoryHeight == 3 && Stat(leach, "StatMass") == LeachRules.MachineKg,
                "LC-3 occupies three by three native tiles and weighs 220 kg: " + state);
            check(leach.nContainerWidth == 4 && leach.nContainerHeight == 3 && leach.aSlotsWeHave.Contains(LeachRules.Prefix + "Input"), "LC-3 has a four by three tray and its charge feed: " + state);
            check(leach.strNameFriendly.StartsWith("Phobos' Lixivar LC-3 Leach and Crystallise Unit", StringComparison.Ordinal), "LC-3 carries the Lixivar LC-3 name: " + state);
            check(Stat(leach, "StatBasePrice") == (damaged ? (int)Economy.Price(LeachRules.Prefix) / 4 : (int)Economy.Price(LeachRules.Prefix)) && Has(leach, EquipmentEconomy.HighSalvageMark),
                "LC-3 carries its late-game price and the high-salvage mark: " + state);
            check((leach.jsonPI == LeachRules.Prefix + "Power") == (installed && !damaged), "LC-3 draws power only when installed and intact: " + state);
            check(d.Installables.ContainsKey(LeachRules.Prefix + state + "Dismantle") && (damaged ? d.Installables.ContainsKey(LeachRules.Prefix + state + "Repair") : d.Installables.ContainsKey(LeachRules.Prefix + state + "Restore")),
                "LC-3 has native dismantle, and Repair or Restore: " + state);
            if (installed) check(leach.aInteractions.Count(i => i == Definitions.Controls) == 1 && leach.mapPoints.Contains("PowerA,0,32"), "Installed LC-3 offers one Control Panel and its power point, in the wall row behind it");
        }
        var leachFeed = d.Objects[LeachRules.Prefix + "InputBin"]; var leachTrigger = DataHandler.dictCTs[leachFeed.strContainerCT];
        check(leachFeed.nContainerWidth * leachFeed.nContainerHeight == LeachRules.FeedCapacity, "The LC-3 feed holds four units");
        foreach (string id in LeachRules.StockFeed)
            check(leachTrigger.TriggeredDataCO(new DataCO(d.Objects[id]), false), "The LC-3 feed admits its own feed at the game level: " + id);
        // From 0.20.0 the LC-3 takes the game's olivine: native ore passes the game-level rule, as on the V4, and the
        // container rule admits only the selected recipe's exact charge.
        foreach (string ore in new[] { LeachRules.Olivine, RefineryRules.Hydrates })
            check(leachTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[ore]), false), "Native ore enters the LC-3 feed at the game level: " + ore);
        check(Stat(DataHandler.dictCOs[LeachRules.Olivine], "StatMass") == LeachRecipes.Epsom.ItemInputs.Single().Kg && LeachRecipes.Epsom.ItemInputs.Single().Id == LeachRules.Olivine,
            "The Epsom salt charge takes one whole 10 kg olivine chunk");
        check(LeachRecipes.FeedKg(RefineryRules.Hydrates, LeachRecipes.Epsom, true, true) == null && LeachRecipes.FeedKg(LeachRules.Olivine, LeachRecipes.Leach, true, true) == null,
            "Other ore is refused by identity, and olivine only for the Epsom salt charge");
        foreach (string outside in new[] { "ItmIce01", "ItmScrapSteel" })
            check(!leachTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[outside]), false), "The LC-3 feed takes no ice or scrap: " + outside);
        foreach (string outside in new[] { Materials.NickelIronIngot, Materials.LeachedResidue, Materials.BrineSaltCake, Materials.CausticRemainder, Materials.OlivineLeachCake })
            check(!leachTrigger.TriggeredDataCO(new DataCO(d.Objects[outside]), false), "The LC-3 feed takes no other Phobos material: " + outside);
        var leachPower = d.Power[LeachRules.Prefix + "Power"];
        check(Math.Abs(leachPower.fAmount - LeachRules.IdleKW / Units.SecondsPerHour) < 1e-12 && leachPower.strOverrideCond == ManufacturingRules.Working &&
              Math.Abs(leachPower.fOverrideAmount - LeachRules.WorkingKW / Units.SecondsPerHour) < 1e-12 && leachPower.aInputPts.SequenceEqual(new[] { "PowerA" }),
            "LC-3 draws 0.1 kW idle and 12 kW working through one input point");
        check(DataHandler.dictCOs[LeachRules.MakeupPacket] is var packet && Stat(packet, "StatMass") == LeachRules.MakeupPacketKg, "Agriculture's makeup packet weighs the 40 g the formulation makes");

        // Every message the shared engine builds from a spec's text prefix exists for every charge machine (spoil
        // messages only where a charge can spoil, selection messages only where the crew selects), and every link's
        // texts resolve, including the name of each gas a charge stores.
        var engineSource = File.ReadAllText(Path.Combine(repo, "src/PhobosManufacturing/ChargeMachine.cs"));
        var engineKeys = System.Text.RegularExpressions.Regex.Matches(engineSource, @"\bT\(([^;]*?)\)").Cast<System.Text.RegularExpressions.Match>()
            .SelectMany(m => System.Text.RegularExpressions.Regex.Matches(m.Groups[1].Value, "\"([a-z_]+)\"").Cast<System.Text.RegularExpressions.Match>().Select(k => k.Groups[1].Value))
            .Concat(new[] { "name", "description", "feed_name", "feed_description" }).Distinct().ToArray();
        check(engineKeys.Length > 40 && engineKeys.Contains("no_selection") && engineKeys.Contains("spoiled_log"), "The engine's message keys are read from its source");
        foreach (var machine in ChargeMachines.All)
        {
            var chargeSpec = machine.Spec;
            foreach (string key in engineKeys)
            {
                if (key.StartsWith("spoiled", StringComparison.Ordinal) && chargeSpec.Spoiled == null) continue;
                if ((key.StartsWith("select", StringComparison.Ordinal) || key == "no_selection") && chargeSpec.Selection != RecipeSelection.Explicit) continue;
                if (key == "heat_note" && !chargeSpec.HeatNote) continue;
                check(!PhobosManufacturing.Text.Get(chargeSpec.Text(key), 0, 0, 0, 0, 0, 0, 0).StartsWith("[", StringComparison.Ordinal), "Engine message exists: " + chargeSpec.Text(key));
            }
            check(!PhobosManufacturing.Text.Get(chargeSpec.MaintenanceChargeKey).StartsWith("[", StringComparison.Ordinal), "Removal refusal exists: " + chargeSpec.MaintenanceChargeKey);
            foreach (var link in machine.Links)
                foreach (string text in Enum.GetValues(typeof(LinkProblem)).Cast<LinkProblem>().Select(problem => link.Reason(problem, 1, 2))
                             .Concat(new[] { link.FieldLabel(), link.Linked(), link.Unlinked(), link.Missing() }))
                    check(!text.Contains("["), "Link text resolves on " + chargeSpec.Prefix + " for " + link.Commodity + ": " + text);
            check(Provider.Groups.Contains(chargeSpec.SnapshotKind), "The charge machine's console group is one we name: " + chargeSpec.SnapshotKind);
        }
        foreach (string group in Provider.Groups)
            check(!PhobosManufacturing.Text.Get("Group." + group).StartsWith("[", StringComparison.Ordinal), "Console group name exists: " + group);
        check(!PhobosManufacturing.Text.Get("Store.accept_done").StartsWith("[", StringComparison.Ordinal), "Acid tanks and gas stores share the accept notice");
        check(ChargeMachines.Refinery.Links.Any(l => l.Commodity == ManufacturingRules.CarbonDioxide) && ChargeMachines.Leach.Links.Select(l => l.Commodity).SequenceEqual(new[] { ManufacturingRules.Water, ManufacturingRules.Ammonia, LiquidStores.SulfuricAcid, ManufacturingRules.CropNutrients }),
            "The V4 links a carbon dioxide store for the calcine; the LC-3 links water, ammonia, an acid tank and a nutrient hopper");
        check(ManufacturingRules.CropNutrients == PhobosAgriculture.Core.HopperRules.Commodity && AgricultureStock.HopperMinimum == new Version(0, 27, 0) &&
              LeachRecipes.CropNutrients.Deposits.Single().Id == PhobosAgriculture.Core.HopperRules.Commodity,
            "The complete formulation deposits exactly the commodity Agriculture's hoppers hold, from the version that has them");
        var leachPorts = ChargeMachines.Leach.Links.Select(l => l.MachinePort).ToArray();
        check(leachPorts.Distinct().Count() == 4 && ChargeMachines.Leach.Links.All(l => l.PeerPort == LeachRules.VesselPort), "The LC-3's four links have their own ports and one vessel-side port");

        // The feed at the game level: any ore or our stock through native containment; the exact rule narrows it.
        var feed = d.Objects[RefineryRules.InputBin]; var trigger = DataHandler.dictCTs[feed.strContainerCT];
        check(feed.nContainerWidth * feed.nContainerHeight == RefineryRules.FeedCapacity && d.Slots[RefineryRules.InputSlot].bHide, "The charge feed holds six units as a hidden slot with its own window");
        foreach (string id in new[] { RefineryRules.Hydrates, RefineryRules.Iron, RefineryRules.Carbides, "ItmMineral02" })
            check(trigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[id]), false), "Every native ore enters the feed at the game level: " + id);
        foreach (string id in new[] { Materials.ClayHydrates, Materials.NickelIronIngot, Materials.CarbonStock, Materials.LeachedResidue })
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
        check(!d.Power.ContainsKey(HydrogenRules.Prefix + "Power") && !d.Power.ContainsKey(MethaneRules.Prefix + "Power") && !d.Power.ContainsKey(ManifoldRules.Prefix + "Power"),
            "The fuel stores and the manifold have no power info");
        check(PropellantLineRules.Segment == Phobos.Ostranauts.Framework.Liquids.LineFamilies.GasPresent && PropellantLineRules.Prefix == Phobos.Ostranauts.Framework.Liquids.LineFamilies.GasPrefix &&
            new[] { PropellantLineRules.Segment, "PhobosFurnaceCoolantSegment", "PhobosWaterConduitPresent", Phobos.Ostranauts.Framework.Liquids.LineFamilies.ProcessWaterPresent }.Distinct().Count() == 4,
            "The moved gas line keeps its saved identities, and gas, coolant, irrigation and process-water segments stay distinct");
        foreach (string prefix in ChargeMachines.All.Select(m => m.Spec.Prefix).Concat(new[] { ProcessorRules.Prefix, SabatierRules.Prefix, CrackerRules.Prefix }))
            check(d.Objects[prefix + "Installed"].mapPoints.Any(p => p.StartsWith(Phobos.Ostranauts.Framework.Liquids.LinePorts.GasPoint + ",", StringComparison.Ordinal)) &&
                (prefix == CrackerRules.Prefix) != d.Objects[prefix + "Installed"].mapPoints.Any(p => p.StartsWith(Phobos.Ostranauts.Framework.Liquids.LinePorts.WaterPoint + ",", StringComparison.Ordinal)),
                "Every machine has a gas port, and a water port unless it never handles water: " + prefix);
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
        check(GasStores.All.All(s => BulkVessels.SpecFor(s.Installed) == s.Spec) && LiquidStores.All.All(s => BulkVessels.SpecFor(s.Installed) == s.Spec) &&
              BulkVessels.All.Count(s => s.Owner == Plugin.Id) == GasStores.All.Count + LiquidStores.All.Count,
            "Every gas and liquid store size is registered once, by this mod");

        // The Lixivar acid tanks (Manufacturing 0.19.0): bunded liquid stores, never gas stores.
        foreach (var store in LiquidStores.All)
        foreach (string state in Definitions.Forms)
        {
            var tank = d.Objects[store.Prefix + state]; var tankItem = d.Items[tank.strItemDef];
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            check(tankItem.nCols == store.Footprint && tank.inventoryWidth == store.Footprint && tank.jsonPI == null && tank.aTickers.Length == 0 && Stat(tank, "StatMass") == store.DryKg,
                "Each acid tank is a passive vessel of its own footprint at its dry mass: " + store.Prefix + state);
            string model = store.Family == LiquidStores.AcidFamily ? "Phobos' Lixivar AT-" + store.Footprint + " Sulfuric Acid Tank" : "Phobos' Alembrine Cask-" + store.Footprint + " Ethanol Tank";
            check(tank.strNameFriendly.StartsWith(model, StringComparison.Ordinal), "Each liquid tank carries its own brand and model: " + store.Prefix + state);
            check(Stat(tank, "StatBasePrice") == (damaged ? (int)store.Price / 4 : (int)store.Price) && Has(tank, EquipmentEconomy.HighSalvageMark), "Each acid tank carries its price and the high-salvage mark: " + store.Prefix + state);
            check(!GasStores.IsFamily(tank.strName) && LiquidStores.IsFamily(tank.strName) && !tank.mapPoints.Any(p => p.StartsWith("PhobosGas", StringComparison.Ordinal)),
                "An acid tank is no gas store and has no gas-line port: " + store.Prefix + state);
        }
        var acidSpec = BulkVessels.SpecFor(LiquidStores.AcidFamily.Small.Installed)!;
        check(acidSpec.Commodity == LiquidStores.SulfuricAcid && acidSpec.DamagePolicy == VesselDamagePolicy.Isolate && acidSpec.CapacityKg == 1150 && acidSpec.DryKg == 240 &&
              Math.Abs(LiquidStores.AcidCapacityKg - 1156) < 1 && LiquidStores.AcidFamily.Sizes.Select(s => s.CapacityKg).SequenceEqual(new double[] { 1150, 2850, 5520 }),
            "The AT-2 holds 1,150 kg (98% acid at 1,836 kg/m3 in 0.787 m3 at 80%), isolates on damage, and the AT-3 and AT-4 follow the ladder");
        // The kiosk prices acid through NativeGasVessel.PricePerKg, which reads this same GasPrices table in a running game.
        check(NativeGasCanister.IsRoomSpecies("H2SO4") && GasPrice("H2SO4") == 3.1 && Math.Abs(LiquidStores.MistKg(1150) - 0.115) < 1e-9,
            "Acid mist is the game's own H2SO4, which the game prices at 3.1 cr/kg; a full AT-2 mists 115 g when damaged");
        // The Alembrine ethanol casks (Manufacturing 0.38.0).
        var ethanolSpec = BulkVessels.SpecFor(LiquidStores.EthanolFamily.Small.Installed)!;
        check(ethanolSpec.Commodity == LiquidStores.Ethanol && ethanolSpec.DamagePolicy == VesselDamagePolicy.Isolate && ethanolSpec.CapacityKg == 495 && ethanolSpec.DryKg == 240 &&
              Math.Abs(LiquidStores.EthanolDensityKgPerM3 * LiquidStores.VesselVolumeM3 * LiquidStores.FillFraction - 497) < 1,
            "The Cask-2 holds 495 kg (ethanol at 789 kg/m3 in 0.787 m3 at 80%) and isolates on damage");
        check(StoreService.LiquidPricePerKg(LiquidStores.EthanolFamily) == LiquidStores.EthanolPricePerKg && LiquidStores.AcidFamily.PricePerKg == null,
            "Ethanol carries its authored station price; acid keeps the game's own (read from GasPrices in a running game)");

        // The Lixivar SA-3 acid plant.
        foreach (string state in Definitions.Forms)
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            var plant = d.Objects[AcidPlantRules.Prefix + state];
            check(d.Items[plant.strItemDef].nCols == 3 && Stat(plant, "StatMass") == AcidPlantRules.MachineKg && plant.strNameFriendly.StartsWith("Phobos' Lixivar SA-3 Sulfuric Acid Plant", StringComparison.Ordinal),
                "SA-3 is a 260 kg three by three Lixivar plant: " + state);
            check((plant.jsonPI == AcidPlantRules.Prefix + "Power") == (installed && !damaged) && Stat(plant, "StatBasePrice") == (damaged ? (int)Economy.Price(AcidPlantRules.Prefix) / 4 : (int)Economy.Price(AcidPlantRules.Prefix)),
                "SA-3 draws power only when installed and intact, at its late-game price: " + state);
        }
        var plantPower = d.Power[AcidPlantRules.Prefix + "Power"];
        check(Math.Abs(plantPower.fOverrideAmount - 4 / Units.SecondsPerHour) < 1e-12 && plantPower.strOverrideCond == ManufacturingRules.Working, "SA-3 draws 4 kW working");
        var plantTrigger = DataHandler.dictCTs[d.Objects[AcidPlantRules.Prefix + "InputBin"].strContainerCT];
        check(plantTrigger.TriggeredDataCO(new DataCO(d.Objects[Materials.SulfideNodule]), false) && !plantTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[RefineryRules.Iron]), false) &&
              !plantTrigger.TriggeredDataCO(new DataCO(d.Objects[Materials.EvaporiteCrust]), false), "The SA-3 feed takes the sulfide nodule and nothing else");
        check(ChargeMachines.AcidPlant.Links.Select(l => l.Commodity).SequenceEqual(new[] { ManufacturingRules.Oxygen, ManufacturingRules.Water, LiquidStores.SulfuricAcid }),
            "The SA-3 links an oxygen store, a water vessel and an acid tank");
        // The Alembrine Copperhead-3 fermenter-still (Manufacturing 0.39.0).
        foreach (string state in Definitions.Forms)
        {
            bool damaged = state.EndsWith("Dmg", StringComparison.Ordinal), installed = state.StartsWith("Installed", StringComparison.Ordinal);
            var still = d.Objects[FermenterRules.Prefix + state];
            check(d.Items[still.strItemDef].nCols == 3 && Stat(still, "StatMass") == 320 && still.strNameFriendly.StartsWith("Phobos' Alembrine Copperhead-3 Fermenter-Still", StringComparison.Ordinal),
                "The fermenter-still is a 320 kg three by three Alembrine machine: " + state);
            check((still.jsonPI == FermenterRules.Prefix + "Power") == (installed && !damaged) && Stat(still, "StatBasePrice") == (damaged ? (int)Economy.Price(FermenterRules.Prefix) / 4 : (int)Economy.Price(FermenterRules.Prefix)),
                "The fermenter-still draws power only when installed and intact, at its price: " + state);
        }
        var stillPower = d.Power[FermenterRules.Prefix + "Power"];
        check(Math.Abs(stillPower.fOverrideAmount - 3 / Units.SecondsPerHour) < 1e-12 && stillPower.strOverrideCond == ManufacturingRules.Working, "The fermenter-still draws 3 kW working");
        var stillTrigger = DataHandler.dictCTs[d.Objects[FermenterRules.Prefix + "InputBin"].strContainerCT];
        check(stillTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[FermenterRules.Beet]), false) && stillTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[FermenterRules.Sugar]), false) &&
              !stillTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs["PhobosVerdemorrowRawPotatoes"]), false) && !stillTrigger.TriggeredDataCO(new DataCO(d.Objects[Materials.SulfideNodule]), false),
            "The fermenter-still's feed takes Agriculture's beets and sugar and nothing else");
        check(ChargeMachines.Fermenter.Links.Select(l => l.Commodity).SequenceEqual(new[] { ManufacturingRules.Water, ManufacturingRules.CarbonDioxide, LiquidStores.Ethanol }) &&
              ChargeMachines.Fermenter.Spec.IgnitionSource && AgricultureStock.SugarCropsDefinitions() && Economy.Pack.factionKiosks?.tiers[FermenterRules.Prefix] == "Friendly",
            "The fermenter-still links a water vessel, a CO2 store and an ethanol cask, is an ignition source while working, and finds Agriculture's beets and sugar at their masses");
        var ethanolPort = LinePorts.Ethanol(3);
        check(d.Objects[FermenterRules.Prefix + "Installed"].mapPoints.Contains(LinePorts.EthanolPoint + "," + ethanolPort.X + "," + ethanolPort.Y), "The fermenter-still has an ethanol port");
        foreach (var (table, share) in new[] { (MiningLoot.MTable, MiningLoot.NoduleMChance), (MiningLoot.STable, MiningLoot.NoduleSChance) })
        {
            var nodule = d.LootCarves[table][Materials.SulfideNodule];
            check(nodule.Donor == MiningLoot.IronDonor && nodule.Share == share && DataHandler.dictLoot[table].aCOs.Single().Split('|').Count(u => u.StartsWith(Materials.SulfideNodule + "=", StringComparison.Ordinal)) == 1,
                "The sulfide nodule is carved once from the meteoric-iron share: " + table);
        }
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
            check(co.strNameFriendly.StartsWith("Phobos' Fennmark ", StringComparison.Ordinal) || co.strNameFriendly.StartsWith("Phobos' Lixivar ", StringComparison.Ordinal) || co.strNameFriendly.StartsWith("Phobos' Alembrine ", StringComparison.Ordinal), "Material is branded Fennmark or Lixivar: " + m.Id);
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
        check(AcidPlantRecipes.All.Count == 1 && AcidPlantRecipes.Match(new[] { Materials.SulfideNodule }) == AcidPlantRecipes.Roast, "The SA-3 has its one roast recipe");
        check(RefineryRecipes.Available(true).Count() == 11 && RefineryRecipes.Available(false).Count() == 11 && withoutShipbreaker.Objects.Count == d.Objects.Count, "Shipbreaker never changes the definitions, and nickel steel is offered with or without it");
        check(AgricultureStock.Definitions(), "Agriculture's makeup packet is published at the 40 g the formulation expects");
        check(AgricultureStock.StrawDefinitions(), "Agriculture's straw bale is published at the 1 kg the straw charges expect");
        check(RefineryRecipes.Available(true, true).Count() == 13 && RefineryRecipes.Available(false, true).Select(r => r.Id).Contains("straw-char") && !RefineryRecipes.Available(true).Any(r => r.Requires.Contains(ChargeCatalog.StrawRequirement)),
            "The straw charges are offered only with Agriculture's bale");
        check(ShipbreakerStock.Definitions(), "Shipbreaker's steel ingot and remainder are published at the masses the steel charge expects");

        // Refining as a business (owner approval, 1 October 2026; docs/development/refining-business-and-interdependencies.md,
        // "The rules now in force"), at live prices with Shipbreaker's steel ingot included. Products are valued as the
        // station values them: items at their price, water at the station price, stored gases and acid at the game's gas price.
        // 1. A charge fed by mined ore whose products include a finished item earns 1.5 to 2.5 times that ore.
        // 2. A step inside a chain (fed only by stock made aboard) never loses and gains at most half again.
        // 3. A supply or disposal charge (no finished item: only gangue, terminal remainders and bulk) makes no profit claim.
        // 4. Fertiliser formulations carry Agriculture's own price: the owner's exception, as fertiliser is rare in the game's world.
        // 5. A superseded revision is kept only so a bound job settles; it is never offered and not priced.
        // 6. A charge fed only from bought stock earns at most 1.25 times its inputs. Off-gas has no value.
        // 7. A service charge on the game's own consumables (reactivating spent CO2 filters, Manufacturing 0.27.0) never gains:
        //    the game prices spent and ready cartridges alike, so it saves purchases rather than making money.
        double Price(string id) => id == ManufacturingRules.Water ? Phobos.Ostranauts.Framework.Items.WaterTanks.WaterPricePerKg
            : Stat(d.Objects.TryGetValue(id, out var own) ? own : DataHandler.dictCOs[id], "StatBasePrice");
        // Stored gases are valued at the game's own gas price per kilogram, read from its GasPrices table (the table
        // GasContainer.GetGasPrice reads in a running game).
        double GasPrice(string species) => double.Parse(DataHandler.dictLoot["GasPrices"].aCOs.Single(e => e.StartsWith(species + "=", StringComparison.Ordinal)).Split('x')[1],
            System.Globalization.CultureInfo.InvariantCulture);
        double UnitValue(string id, int count, double kg) => id == ManufacturingRules.Water ? count * kg * Price(id) :
            GasStores.FamilyOf(id) is GasFamily gas ? count * kg * GasPrice(gas.Species) :
            LiquidStores.FamilyOf(id) is LiquidFamily liquid ? count * kg * (liquid.PricePerKg ?? GasPrice(liquid.MistSpecies!)) : count * Price(id);
        bool Mined(string id) => Materials.ById(id)?.Mined == true || !d.Objects.ContainsKey(id) && DataHandler.dictCOs.TryGetValue(id, out var native) && new DataCO(native).HasCond("IsOre");
        bool Finished(string id) => !ChargeCommodities.Is(id) && !Materials.IsTerminal(id) && id != RefineryRules.Gangue;
        bool Bought(string id) => d.Objects.ContainsKey(id) && Economy.Pack.regional != null && Economy.Pack.regional.items.ContainsKey(id)
            || PhobosShipbreaker.Core.ShipbreakerEconomy.Pack.regional?.items.ContainsKey(id) == true;
        var superseded = new HashSet<(string, int)>(ChargeCatalog.All.SelectMany(r => r.Supersedes.Select(e => (r.Machine, e))));
        int business = 0, steps = 0, services = 0;
        bool NativeItem(string id) => !ChargeCommodities.Is(id) && !Mined(id) && !d.Objects.ContainsKey(id) && Materials.ById(id) == null && id.StartsWith("Itm", StringComparison.Ordinal);
        foreach (var recipe in ChargeCatalog.All)
        {
            if (superseded.Contains((recipe.Machine, recipe.Revision))) continue;
            // The owner's exception (30 September 2026, reaffirmed 1 October): the formulations into Agriculture's makeup
            // packets and crop nutrients carry Agriculture's own price, from salts no merchant sells.
            if (recipe.Requires.Contains(ChargeCatalog.CropNutrientsRequirement))
            {
                check(recipe.Products.All(p => p.Id == ManufacturingRules.CropNutrients) && !recipe.ItemInputs.Any(i => Bought(i.Id)) && recipe.Solids(recipe.Products).Count() == 0,
                    $"The {recipe.Id} charge deposits only crop nutrients into a hopper (Agriculture's own price), from salts no merchant sells");
                continue;
            }
            // The owner's exception (4 October 2026, crop waste both ways): straw is baled crop waste, which no merchant sells
            // and which is priced as waste, so charring it gains and burning it supplies; neither can be bought into a loop.
            if (recipe.Requires.Contains(ChargeCatalog.StrawRequirement))
            {
                check(recipe.ItemInputs.All(i => i.Id == RefineryRules.StrawBale) && !recipe.ItemInputs.Any(i => Bought(i.Id)) && Price(RefineryRules.StrawBale) <= 1,
                    $"The {recipe.Id} charge takes only Agriculture's straw bales, which no merchant sells and which are priced as waste");
                continue;
            }
            if (recipe.Requires.Contains(ChargeCatalog.MakeupRequirement))
            {
                check(recipe.Products.All(p => p.Id == LeachRules.MakeupPacket) && Stat(DataHandler.dictCOs[LeachRules.MakeupPacket], "StatBasePrice") == PhobosAgriculture.Core.NutrientRecovery.MakeupPrice &&
                    !recipe.ItemInputs.Any(i => Bought(i.Id)), $"The {recipe.Id} charge yields only Agriculture's makeup packet at Agriculture's own price, from salts no merchant sells");
                continue;
            }
            double inValue = recipe.Inputs.Sum(i => UnitValue(i.Id, i.Count, i.Kg));
            double outValue = recipe.Products.Sum(p => UnitValue(p.Id, p.Count, p.Kg));
            if (recipe.ItemInputs.All(i => Bought(i.Id)))
                check(outValue <= 1.25 * inValue, $"The {recipe.Machine} {recipe.Id} charge, fed from bought stock, earns at most a quarter more at base prices: {outValue:F2} out of {inValue:F2}");
            if (!recipe.Products.Any(p => Finished(p.Id))) continue;
            if (recipe.ItemInputs.All(i => NativeItem(i.Id)))
            {
                check(outValue <= inValue, $"The {recipe.Machine} {recipe.Id} service charge never gains on the game's own items: {outValue:F2} out of {inValue:F2}");
                services++;
                continue;
            }
            if (recipe.ItemInputs.Any(i => Mined(i.Id)))
            {
                double ore = recipe.ItemInputs.Where(i => Mined(i.Id)).Sum(i => UnitValue(i.Id, i.Count, i.Kg));
                check(outValue >= 1.5 * ore && outValue <= 2.5 * ore, $"The {recipe.Machine} {recipe.Id} charge earns 1.5 to 2.5 times its ore: {outValue:F2} from {ore:F2} of ore ({inValue - ore:F2} of reagents)");
                business++;
            }
            else
            {
                check(outValue >= inValue && outValue <= 1.5 * inValue, $"The {recipe.Machine} {recipe.Id} step neither loses nor gains more than half again: {outValue:F2} out of {inValue:F2}");
                steps++;
            }
        }
        check(business == 5 && steps == 4 && services == 2, $"Five business charges (carbon, nickel-iron, evaporite, olivine, sulfide), four steps (struvite twice, nickel steel, methane cracking) and two reactivation services are priced: {business}, {steps} and {services}");
        // Methane cracking from bought stock: water and CO2 through the X2 and K2 make the methane; selling the oxygen and water
        // back at the kiosk's share and the carbon black at a generous 1.2 times its price must not repay what was bought.
        var crackBed = RefineryRecipes.ById("methane-pyrolysis")!;
        double methaneMol = crackBed.Draws.Single(i => i.Id == ManufacturingRules.Methane).Kg / 0.016043, hydrogenNeeded = methaneMol * 4 * 0.002016;
        double cycles = hydrogenNeeded / ProcessorRules.HydrogenKgPerCycle, boughtWater = cycles * ProcessorRules.WaterKgPerCycle, boughtCO2 = methaneMol * 0.044009;
        double bought = boughtWater * Price(ManufacturingRules.Water) + boughtCO2 * GasPrice("CO2") + Price(Materials.CarbonStock);
        double buyback = Phobos.Ostranauts.Framework.Trading.BulkSupplies.BuybackShare;
        double sold = buyback * (cycles * ProcessorRules.OxygenKgPerCycle * GasPrice("O2") + methaneMol * 2 * 0.018015 * Price(ManufacturingRules.Water) + crackBed.Deposits.Single().Kg * GasPrice("H2")) +
            1.2 * crackBed.Products.Where(p => !ChargeCommodities.Is(p.Id)).Sum(p => p.Count * Price(p.Id));
        check(sold < bought, $"Cracking methane made from bought water and CO2 never repays the purchase: {sold:F2} back from {bought:F2}");
        // The game's own CO2 filters enter the V4's feed at the game level, spent or ready; the exact rule then takes only spent ones.
        var feedTrigger = DataHandler.dictCTs[RefineryRules.FeedTrigger];
        foreach (string filter in new[] { "ItmFilterCO201Dmg", "ItmFilterCO202Dmg" })
            check(feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[filter]), false) && Stat(DataHandler.dictCOs[filter], "StatMass") == 2.5, "A spent CO2 filter passes the V4 feed's game-level rule at 2.5 kg: " + filter);
        check(RefineryRules.FeedKg("ItmFilterCO201", true) == null && RefineryRules.FeedKg("ItmFilterCO201Dmg", true) == 2.5, "Only spent cartridges are feed; ready ones are refused by the exact rule");
        // Agriculture's straw bale enters at the game level by its identity condition, and by the exact rule only with the straw gate.
        check(feedTrigger.TriggeredDataCO(new DataCO(DataHandler.dictCOs[RefineryRules.StrawBale]), false) && RefineryRules.FeedKg(RefineryRules.StrawBale, true, true) == 1 && RefineryRules.FeedKg(RefineryRules.StrawBale, true) == null,
            "A straw bale passes the V4 feed's game-level rule, and is feed only while Agriculture's bale is available");
        // The whole iron chain: a meteoric iron block and a fifth of a carbon ore block end as four nickel steel ingots.
        double ironChain = 4 * Price(Materials.NickelSteelIngot), ironOre = Price(RefineryRules.Iron) + Price(RefineryRules.Carbides) / RefineryRecipes.Carbon.Products.Single(p => p.Id == Materials.CarbonStock).Count;
        check(ironChain >= 1.5 * ironOre && ironChain <= 2.5 * ironOre, $"Meteoric iron and carbon ore end as nickel steel worth 1.5 to 2.5 times the ores: {ironChain:F2} from {ironOre:F2}");
        check(Price(Materials.NickelSteelIngot) > 2 * Price(RefineryRules.SteelIngot), "Nickel steel is the mined chain's own product, kept apart from Shipbreaker's plain steel ingot cast from scrap");
        check(!ChargeCatalog.All.Where(r => !superseded.Contains((r.Machine, r.Revision))).SelectMany(r => r.Inputs).Any(i => Bought(i.Id)),
            "No V4, LC-3 or SA-3 charge is fed from bought stock: ores and chunks are mined, and every intermediate is made aboard");
        // The kiosk buys bulk back at its share of its own selling price, so a buy-and-sell loop always loses.
        check(Phobos.Ostranauts.Framework.Trading.BulkSupplies.BuybackShare >= .4 && Phobos.Ostranauts.Framework.Trading.BulkSupplies.BuybackShare <= .5,
            "The kiosk's buy-back share sits inside the game's own kiosk buying range, below its selling price");
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
        var evaporite = d.LootCarves[MiningLoot.Table][Materials.EvaporiteCrust];
        check(evaporite.Donor == MiningLoot.Donor && evaporite.Share == MiningLoot.EvaporiteChance && units.Count(u => u.StartsWith(Materials.EvaporiteCrust + "=", StringComparison.Ordinal)) == 1 &&
              Array.FindIndex(units, u => u.StartsWith(Materials.EvaporiteCrust + "=", StringComparison.Ordinal)) > silicates, "The evaporite crust is carved once from the C-class silicates share");
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

        // Economy: mass-balanced salvage that loses value, repairs that return only the machine, restore rates.
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
                var repair = d.Installables[id + "Repair"];
                check(repair.aLootCOs.SequenceEqual(new[] { e.Prefix + state.Replace("Dmg", "") }) && repair.aInputs.Length > 0,
                    "Repair consumes its bill and returns only the repaired machine, as the game's repairs do: " + id);
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
