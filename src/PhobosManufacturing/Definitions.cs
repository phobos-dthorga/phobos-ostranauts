using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Items;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Native definitions for the Fennmark and Lixivar machines, every size of gas store, the gas line, the materials, and
/// the three deflagration objects. Machines use Framework's appliance contract; materials clone the game's own scrap and hydrate
/// definitions so pickup, stacking, damage and market behaviour stay vanilla.</summary>
internal static class Definitions
{
    internal const string Controls = "PhobosManufacturingControls", ImagePath = "phobos/manufacturing/";
    internal const string RefineryArt = "PhobosVolatilesRefinery", ProcessorArt = "PhobosChemicalProcessor", StoreArt = "PhobosHydrogenStore",
        ReactorArt = "PhobosSabatierReactor", MethaneArt = "PhobosMethaneStore", ManifoldArt = "PhobosPropellantManifold",
        FillerArt = "PhobosCanisterFiller", RegulatorArt = "PhobosCabinAirRegulator", CrackerArt = "PhobosAmmoniaCracker", LeachArt = "PhobosLeachUnit", AcidPlantArt = "PhobosAcidPlant", FermenterArt = "PhobosFermenterStill", BottlerArt = "PhobosBottlingUnit", FeederArt = "PhobosReactionMassFeeder", ElectrolysisCellArt = "PhobosElectrolysisCell", CarbothermalArt = "PhobosCarbothermalReactor";
    internal static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    /// <summary>The game's named colour for each stored gas's contents row; hydrogen, which the game has no gas for, takes its cryogenic blue.</summary>
    internal static readonly IReadOnlyDictionary<string, string> ContentsColors = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["H2"] = "CryoBlue", ["CH4"] = "CH4BlueGreen", ["O2"] = "O2Green", ["N2"] = "N2Blue", ["CO2"] = "CO2White", ["NH3"] = "NH3Beige",
        // The game shows its own carbon monoxide readings in the carbon dioxide white.
        ["CO"] = "CO2White"
    };
    internal static void Add(NativeDefinitions d)
    {
        foreach (string condition in new[] { ManufacturingRules.Working, ManufacturingRules.Electrolysing, ManufacturingRules.Reacting, ManufacturingRules.Filling, ManufacturingRules.Grinding, ManufacturingRules.Content })
            d.Conditions[condition] = new JsonCond { strName = condition, strNameFriendly = Text.Get("Condition." + condition), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("Panel.controls"); controls.strDesc = controls.strTooltip = Text.Get("Panel.controls_tooltip");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        AddMaterials(d);
        foreach (var machine in ChargeMachines.All) AddChargeMachine(d, machine);
        AddProcessor(d);
        AddBottler(d);
        AddReactor(d);
        AddCracker(d);
        // Every size of every gas store; each size's art is named after its own definition prefix.
        foreach (var store in GasStores.All)
            AddStore(d, store, Text.Get(store.TextPrefix + ".details", store.DryKg, store.CapacityKg, store.LeakKgPerHour, store.Footprint), store.Prefix);
        // Every size of every liquid store (Manufacturing 0.19.0); each size's art is named after its own definition prefix.
        foreach (var store in LiquidStores.All)
            AddLiquidStore(d, store);
        // What each store holds, on the game's right-click card (Framework 0.79.0), in the game's own gas tints.
        foreach (var family in GasStores.Families)
            VesselContentsDisplay.Declare(d, family.Commodity, Text.Get(family.TextPrefix + ".contents"), ContentsColors[family.Species]);
        foreach (var family in LiquidStores.Families)
            VesselContentsDisplay.Declare(d, family.Commodity, Text.Get(family.TextPrefix + ".contents"), family.ContentsColor);
        AddManifold(d);
        AddFeeder(d);
        AddRegolithFloor(d);
        // Owner rule (4 October 2026): every terminal remainder is declared, so the feeder is its consumer.
        Remainders.Declare(Materials.All.Where(m => m.Terminal).Select(m => m.Id));
        AddFiller(d);
        AddRegulator(d);
        AddLinePorts(d);
        // The Lixivar acid line (Manufacturing 0.24.0) and the Alembrine ethanol line (0.38.0), with their ports.
        LiquidLine.AddAll(d);
        AddDeflagrations(d);
    }

    /// <summary>A charge machine from its spec and its equipment entry: the appliance, its game-level feed rule (own stock
    /// identities, plus native ore where the machine takes it), the hidden feed bin, the power override and use points.</summary>
    private static void AddChargeMachine(NativeDefinitions d, ChargeMachine machine)
    {
        var spec = machine.Spec; string p = spec.Prefix; var shape = Equipment.Entry(p);
        string name = Text.Get(spec.Text("name"));
        ApplianceDefinitions.Add(d, p, name, Text.Get(spec.Text("description"), shape.massKg, shape.workingKW, shape.footprint),
            shape.footprint, shape.massKg, Economy.Price(p), ImagePath + (shape.art ?? spec.Art), Controls, shape.idleKW, shape.installTab);
        // Feed at the game level: native ore (where the machine takes it) or our own stock; the container patch then
        // applies the exact identity, mass and count rule.
        d.Triggers[spec.StockTrigger] = new CondTrigger { strName = spec.StockTrigger, fChance = 1, fCount = 1, bAND = false,
            aReqs = (spec.AdmitsOre ? new[] { "IsOre" } : Array.Empty<string>()).Concat(spec.FeedConditions).Concat(spec.StockFeed.Select(id => id + "Identity"))
                // Added materials a recipe of this machine takes (0.45.0) are admitted by their own identity.
                .Concat(ChargeCatalog.For(spec.MachineKey).FeedIds.Where(Materials.IsAdded).Select(id => id + "Identity")).Distinct().ToArray(), aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Triggers[spec.FeedTrigger] = new CondTrigger { strName = spec.FeedTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = Array.Empty<string>(), aForbids = Array.Empty<string>(), aTriggers = new[] { "TIsFitContainerSolid", spec.StockTrigger } };
        ApplianceDefinitions.AddFeedBin(d, p, spec.FeedTrigger, shape.feedCells, Text.Get(spec.Text("feed_name")));
        EquipmentInventory.Apply(d, p, InventorySpec.ProductTray(ChargeTrayWidth, ChargeTrayHeight));
        d.Objects[spec.InputBin].strDesc = Text.Get(spec.Text("feed_description"), shape.feedCells);
        // Only Power points are electrical inputs; line ports and use points never are.
        ApplianceDefinitions.SetPowerOverride(d, p, shape.idleKW, shape.workingKW, spec.WorkingCondition, shape.points.Keys.Where(k => k.StartsWith("Power", StringComparison.Ordinal)).ToArray());
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = name + (damaged ? Text.Get("Content.damaged") : "");
            co.mapPoints = EquipmentSchema.MapPoints(shape);
            co.strPortraitImg = item.strImg;
        }
    }

    private static void AddProcessor(NativeDefinitions d)
    {
        string p = ProcessorRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Processor.name"), Text.Get("Processor.description", ProcessorRules.MachineKg, ProcessorRules.WorkingKW, ProcessorRules.WaterKgPerCycle, ProcessorRules.OxygenKgPerCycle, ProcessorRules.HydrogenKgPerCycle),
            ProcessorRules.Footprint, ProcessorRules.MachineKg, Economy.Price(ProcessorRules.Prefix), ImagePath + ProcessorArt, Controls, ProcessorRules.IdleKW, InstallMenu.Appliances);
        ApplianceDefinitions.SetPowerOverride(d, p, ProcessorRules.IdleKW, ProcessorRules.WorkingKW, ManufacturingRules.Electrolysing, "PowerA");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Processor.name") + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.mapPoints = new[] { "use,0,-24", "PowerA,0," + ApplianceDefinitions.WallRowY(ProcessorRules.Footprint), "PhobosGasOut,8,0" };
            co.strPortraitImg = item.strImg;
        }
    }

    /// <summary>The Alembrine Corker-2 bottling unit (Manufacturing 0.40.0): a powered 2 x 2 appliance whose tray takes only
    /// its own spirit servings, with an ethanol port and a water port and no gas port.</summary>
    private static void AddBottler(NativeDefinitions d)
    {
        string p = BottlerRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Bottler.name"), Text.Get("Bottler.description", BottlerRules.MachineKg, BottlerRules.WorkingKW, BottlerRules.ServingsPerBatch),
            BottlerRules.Footprint, BottlerRules.MachineKg, Economy.Price(p), ImagePath + BottlerArt, Controls, BottlerRules.IdleKW, InstallMenu.Appliances);
        ApplianceDefinitions.SetPowerOverride(d, p, BottlerRules.IdleKW, BottlerRules.WorkingKW, ManufacturingRules.Bottling, "PowerA");
        d.Triggers[BottlerRules.SpiritTrigger] = new CondTrigger { strName = BottlerRules.SpiritTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { BottlerRules.Spirit + "Identity" }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Triggers[BottlerRules.TrayTrigger] = new CondTrigger { strName = BottlerRules.TrayTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = Array.Empty<string>(), aForbids = new[] { "IsInstalled" }, aTriggers = new[] { BottlerRules.SpiritTrigger } };
        // A product tray of four cells: one batch is one stack of seven, so it holds four batches before it waits.
        EquipmentInventory.Apply(d, p, InventorySpec.ProductTray(2, 2, BottlerRules.TrayTrigger));
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Bottler.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.mapPoints = new[] { "use,0,-24", "PowerA,0," + ApplianceDefinitions.WallRowY(BottlerRules.Footprint) };
            co.strPortraitImg = item.strImg;
        }
        var water = LinePorts.Water(BottlerRules.Footprint);
        LineDefinitions.AddPort(d, p, Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(), LinePorts.WaterPoint, water.X, water.Y, water.Socket);
    }

    /// <summary>The K2 Sabatier reactor: a powered 2 x 2 appliance with no container; its gases are a saved record.</summary>
    private static void AddReactor(NativeDefinitions d)
    {
        string p = SabatierRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Sabatier.name"), Text.Get("Sabatier.description", SabatierRules.MachineKg, SabatierRules.WorkingKW, SabatierRules.HydrogenKgPerCycle,
                SabatierRules.CarbonDioxideKgPerCycle, SabatierRules.WaterKgPerCycle, SabatierRules.MethaneKgPerCycle),
            SabatierRules.Footprint, SabatierRules.MachineKg, Economy.Price(SabatierRules.Prefix), ImagePath + ReactorArt, Controls, SabatierRules.IdleKW, InstallMenu.Appliances);
        ApplianceDefinitions.SetPowerOverride(d, p, SabatierRules.IdleKW, SabatierRules.WorkingKW, ManufacturingRules.Reacting, "PowerA");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Sabatier.name") + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.mapPoints = new[] { "use,0,-24", "PowerA,0," + ApplianceDefinitions.WallRowY(SabatierRules.Footprint), "PhobosGasIn,-8,0", "PhobosGasOut,8,0" };
            co.strPortraitImg = item.strImg;
        }
    }

    /// <summary>The AX-2 ammonia cracker: a powered 2 x 2 appliance with no container; its gases are a saved record.</summary>
    private static void AddCracker(NativeDefinitions d)
    {
        string p = CrackerRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Cracker.name"), Text.Get("Cracker.description", CrackerRules.MachineKg, CrackerRules.WorkingKW, CrackerRules.AmmoniaKgPerCycle,
                CrackerRules.NitrogenKgPerCycle, CrackerRules.HydrogenKgPerCycle),
            CrackerRules.Footprint, CrackerRules.MachineKg, Economy.Price(CrackerRules.Prefix), ImagePath + CrackerArt, Controls, CrackerRules.IdleKW, InstallMenu.Appliances);
        ApplianceDefinitions.SetPowerOverride(d, p, CrackerRules.IdleKW, CrackerRules.WorkingKW, ManufacturingRules.Reacting, "PowerA");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Cracker.name") + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.mapPoints = new[] { "use,0,-24", "PowerA,0," + ApplianceDefinitions.WallRowY(CrackerRules.Footprint), "PhobosGasIn,-8,0", "PhobosGasOut,8,0" };
            co.strPortraitImg = item.strImg;
        }
    }

    private static void AddStore(NativeDefinitions d, GasStore fuel, string description, string art)
    {
        string p = fuel.Prefix, name = Text.Get(fuel.NameKey);
        BulkVessels.Register(fuel.Spec);
        ApplianceDefinitions.Add(d, p, name, description, fuel.Footprint, fuel.DryKg, fuel.Price, ImagePath + art, Controls, 0, InstallMenu.Appliances);
        // A passive vessel: no electricity, no tickers, no container. Its contents are a saved record.
        d.Power.Remove(p + "Power"); d.Interactions.Remove(p + "PowerChange");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = name + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.mapPoints = new[] { "use,0," + (-8 * fuel.Footprint - 8) };
            co.strPortraitImg = item.strImg;
        }
    }
    /// <summary>A liquid store: a passive bunded vessel with no electricity, no container and no gas-line port.</summary>
    private static void AddLiquidStore(NativeDefinitions d, LiquidStore store)
    {
        string p = store.Prefix, name = Text.Get(store.NameKey);
        BulkVessels.Register(store.Spec);
        ApplianceDefinitions.Add(d, p, name, Text.Get(store.Family.TextPrefix + ".details", store.DryKg, store.CapacityKg, 0, store.Footprint), store.Footprint, store.DryKg, store.Price,
            ImagePath + store.Prefix, Controls, 0, InstallMenu.Appliances);
        d.Power.Remove(p + "Power"); d.Interactions.Remove(p + "PowerChange");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = name + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.mapPoints = new[] { "use,0," + (-8 * store.Footprint - 8) };
            co.strPortraitImg = item.strImg;
        }
        // A canister rack in place of a general inventory (Manufacturing 0.25.0): a drain canister of acid hauled here
        // pours into the tank, and the empty canister waits in the rack. Nothing else fits.
        ApplianceDefinitions.SetRack(d, p, DrainCanisterDefinitions.RackTrigger, DrainCanisterDefinitions.RackWidth, DrainCanisterDefinitions.RackHeight);
    }
    /// <summary>The L2 filling station: a powered 2 x 2 appliance whose ordinary tray becomes a four-cell rack for suit
    /// O2 bottles (the game's own Inventory window). It is not airtight and not a gas container itself: the gas it
    /// moves goes straight from a store or a canister into the vessel being filled.</summary>
    private static void AddFiller(NativeDefinitions d)
    {
        string p = FillerRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Filler.name"), Text.Get("Filler.description", FillerRules.MachineKg, FillerRules.WorkingKW, FillerRules.RackCells, FillerRules.FillFraction * 100),
            FillerRules.Footprint, FillerRules.MachineKg, Economy.Price(FillerRules.Prefix), ImagePath + FillerArt, Controls, FillerRules.IdleKW, InstallMenu.Hvac);
        ApplianceDefinitions.SetPowerOverride(d, p, FillerRules.IdleKW, FillerRules.WorkingKW, ManufacturingRules.Filling, "PowerA");
        // The rack admits only the game's handheld O2 bottles.
        d.Triggers[FillerRules.RackTrigger] = new CondTrigger { strName = FillerRules.RackTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { "IsVesselO2", "IsHandheld" }, aForbids = new[] { "IsInstalled" }, aTriggers = Array.Empty<string>() };
        ApplianceDefinitions.SetRack(d, p, FillerRules.RackTrigger, FillerRules.RackCells, 1);
        var order = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        order.strName = FillerRules.BottleOrder; order.strTitle = Text.Get("Filler.crew_order_title");
        order.strDesc = Text.Get("Filler.crew_order_action"); order.strTooltip = Text.Get("Filler.crew_order_tooltip");
        order.strRaiseUI = null; order.fTargetPointRange = 2;
        d.Interactions[order.strName] = order;
        d.Objects[FillerRules.Installed].aInteractions = d.Objects[FillerRules.Installed].aInteractions.Concat(new[] { FillerRules.BottleOrder }).Distinct().ToArray();
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Filler.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsAirtight=", StringComparison.Ordinal)).ToArray();
            // Power on the local -X side, the gas line port on the neighbouring tile of the local +X side.
            co.mapPoints = new[] { "use,0,-24", "PowerA,-8," + ApplianceDefinitions.WallRowY(FillerRules.Footprint), FillerRules.Inlet + ",24,8" };
            co.strPortraitImg = item.strImg;
        }
    }
    /// <summary>The A2 cabin air regulator: a powered 2 x 2 valve and sensor unit on the native air-pump pattern, with
    /// the gas line port beside it. It holds no gas and no inventory: gas goes straight from the linked store into the room.</summary>
    private static void AddRegulator(NativeDefinitions d)
    {
        string p = RegulatorRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Regulator.name"), Text.Get("Regulator.description", RegulatorRules.MachineKg, RegulatorRules.WorkingKW,
                RegulatorRules.OxygenKgPerHour, RegulatorRules.NitrogenKgPerHour, RegulatorRules.MinRoomKPa, RegulatorRules.MaxOxygenFraction * 100),
            RegulatorRules.Footprint, RegulatorRules.MachineKg, Economy.Price(RegulatorRules.Prefix), ImagePath + RegulatorArt, Controls, RegulatorRules.WorkingKW, InstallMenu.Hvac);
        EquipmentInventory.Apply(d, p, RegulatorInventory);
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Regulator.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsAirtight=", StringComparison.Ordinal)).ToArray();
            co.mapPoints = new[] { "use,0,-24", "PowerA,-8," + ApplianceDefinitions.WallRowY(RegulatorRules.Footprint), RegulatorRules.Inlet + ",24,8" };
            co.strPortraitImg = item.strImg;
        }
    }
    /// <summary>The Slingwright RM-1 reaction mass feeder (Manufacturing 0.43.0): a powered 1 x 1 machine for a regulator's
    /// gas-input tile, like the P1. Its four-cell inventory admits declared remainders only (the admission hook); the
    /// ground mass is a bulk-vessel record shown on the right-click card. Not airtight and not a gas container, so the
    /// game never treats it as a canister.</summary>
    private static void AddFeeder(NativeDefinitions d)
    {
        string p = FeederRules.Prefix;
        BulkVessels.Register(new BulkVesselSpec(p, FeederRules.Commodity, FeederRules.CapacityKg, FeederRules.MachineKg, ManufacturingRules.Owner,
            FeederRules.MassRecord, FeederRules.MassJournal, FeederRules.MassGuard));
        VesselContentsDisplay.Declare(d, FeederRules.Commodity, Text.Get("Feeder.contents"), "CO2White");
        ApplianceDefinitions.Add(d, p, Text.Get("Feeder.name"), Text.Get("Feeder.description", FeederRules.MachineKg, FeederRules.CapacityKg, FeederRules.WorkingKW),
            FeederRules.Footprint, FeederRules.MachineKg, Economy.Price(p), ImagePath + FeederArt, Controls, FeederRules.IdleKW, InstallMenu.Hvac);
        ApplianceDefinitions.SetPowerOverride(d, p, FeederRules.IdleKW, FeederRules.WorkingKW, ManufacturingRules.Grinding, "PowerA");
        EquipmentInventory.Apply(d, p, InventorySpec.ServiceRack(FeederRules.TrayWidth, FeederRules.TrayHeight));
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Feeder.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsAirtight=", StringComparison.Ordinal)).ToArray();
            co.mapPoints = new[] { "use,0,0", "PowerA,0," + ApplianceDefinitions.WallRowY(FeederRules.Footprint) };
            co.strPortraitImg = item.strImg;
        }
    }

    /// <summary>The P1 manifold: a passive 1 x 1 valve block for a regulator's gas-input tile. Deliberately not
    /// airtight and not a gas container, so the game never treats it as a canister (no refuelling into it); the
    /// Framework RCS patch finds it on the tile and asks it for remass.</summary>
    private static void AddManifold(NativeDefinitions d)
    {
        string p = ManifoldRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Manifold.name"), Text.Get("Manifold.description", ManifoldRules.MachineKg),
            ManifoldRules.Footprint, ManifoldRules.MachineKg, Economy.Price(ManifoldRules.Prefix), ImagePath + ManifoldArt, Controls, 0, InstallMenu.Hvac);
        d.Power.Remove(p + "Power"); d.Interactions.Remove(p + "PowerChange");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Manifold.name") + (damaged ? Text.Get("Content.damaged") : "");
            StripContainer(co);
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            co.aInteractions = co.aInteractions.Where(i => i != "Inventory").ToArray();
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsAirtight=", StringComparison.Ordinal)).ToArray();
            // The line port is the neighbouring tile on the manifold's local -Y side, rotating with it.
            co.mapPoints = new[] { "use,0,0", ManifoldRules.Inlet + ",0,-16" };
            co.strPortraitImg = item.strImg;
        }
    }

    /// <summary>Ports on Framework's shared lines (Manufacturing 0.23.0; the gas line itself moved to Framework with its
    /// identities unchanged). Every gas store keeps its port on the neighbouring tile of its local +X side; the P1, L2
    /// and A2 keep their inlets; every machine gains a process-water port on its local -X side and a gas port on its +X
    /// side where it uses them, both in the middle row (Framework's <see cref="LinePorts"/> rule). Points are rebuilt
    /// from definitions on load, so machines saved before gain their ports with no rewrite. The gas commodities are
    /// carried by the gas line, and oxygen and the fuels are classed for the shared-line caution.</summary>
    private static void AddLinePorts(NativeDefinitions d)
    {
        var gas = SharedLines.GasSpec(); var water = SharedLines.ProcessWaterSpec();
        foreach (string commodity in new[] { ManufacturingRules.Hydrogen, ManufacturingRules.Methane, ManufacturingRules.Oxygen, ManufacturingRules.Nitrogen,
                     ManufacturingRules.CarbonDioxide, ManufacturingRules.Ammonia, ManufacturingRules.CarbonMonoxide })
            LineFamilies.Assign(commodity, LineFamilies.Gas);
        GasNetworkSafety.Classify(ManufacturingRules.Oxygen, GasHazardClass.Oxidiser);
        foreach (string fuel in new[] { ManufacturingRules.Hydrogen, ManufacturingRules.Methane, ManufacturingRules.Ammonia, ManufacturingRules.CarbonMonoxide }) GasNetworkSafety.Classify(fuel, GasHazardClass.Fuel);
        foreach (var fuel in GasStores.All)
        {
            var outlet = fuel.Outlet;
            LineDefinitions.AddPort(d, fuel.Prefix, gas, ManifoldRules.StoreOutlet, outlet.X, outlet.Y, outlet.SocketIndex);
        }
        LineDefinitions.AddPort(d, ManifoldRules.Prefix, gas, ManifoldRules.Inlet, 0, -16, 0);
        foreach (string prefix in new[] { FillerRules.Prefix, RegulatorRules.Prefix })
        {
            var port = LinePorts.Gas(FillerRules.Footprint);
            LineDefinitions.AddPort(d, prefix, gas, prefix == FillerRules.Prefix ? FillerRules.Inlet : RegulatorRules.Inlet, port.X, port.Y, port.Socket);
        }
        void Machine(string prefix, int footprint, bool usesWater)
        {
            var g = LinePorts.Gas(footprint);
            LineDefinitions.AddPort(d, prefix, gas, LinePorts.GasPoint, g.X, g.Y, g.Socket);
            if (!usesWater) return;
            var w = LinePorts.Water(footprint);
            LineDefinitions.AddPort(d, prefix, water, LinePorts.WaterPoint, w.X, w.Y, w.Socket);
        }
        foreach (var machine in ChargeMachines.All) Machine(machine.Spec.Prefix, Equipment.Entry(machine.Spec.Prefix).footprint, usesWater: true);
        Machine(ProcessorRules.Prefix, ProcessorRules.Footprint, usesWater: true);
        Machine(SabatierRules.Prefix, SabatierRules.Footprint, usesWater: true);
        Machine(CrackerRules.Prefix, CrackerRules.Footprint, usesWater: false);
    }

    /// <summary>The V4, LC-3 and SA-3 product trays in cells (Manufacturing 0.30.0; they were 8 x 8): two charges of any
    /// recipe delivered into stacks, including two of the 2 x 2 residues side by side.</summary>
    internal const int ChargeTrayWidth = 4, ChargeTrayHeight = 3;
    /// <summary>The A2 stores nothing. It keeps a hidden grid of its old size that admits nothing, so anything a save
    /// left in it loads attached and is put on the deck (Framework ContainerFit); it has no Inventory action.</summary>
    internal static readonly InventorySpec RegulatorInventory = InventorySpec.LegacyReceptacle(8, 8);
    private static void StripContainer(JsonCondOwner co)
    {
        co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>(); co.strContainerCT = null;
        co.nContainerWidth = co.nContainerHeight = 0;
        co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsContainer=", StringComparison.Ordinal)).ToArray();
        co.mapGUIPropMaps = Array.Empty<string>();
    }

    /// <summary>Stock and remainders clone the game's scrap steel (its pickup, stacking, wear and destruction
    /// behaviour); mined chunks (clay hydrates, the ammonium salt crust, the evaporite crust) clone the game's hydrates
    /// so they mine, break and sell like them.</summary>
    private static void AddMaterials(NativeDefinitions d)
    {
        foreach (var m in Materials.All)
        {
            // The Alembrine spirit (Manufacturing 0.40.0) is a clone of the game's own vodka serving, so the game's liquor
            // drinking and effects apply; it drops the vodka's brand marker, which the game's Bismertnaya lines key off.
            bool spirit = m.Id == BottlerRules.Spirit;
            // Two regolith items keep a game picture by cloning its owner (0.51.0): nothing is copied, the art is
            // referenced at run time. The conditions each keeps are listed; everything else of the donor is dropped.
            bool look = NativeLook.TryGetValue(m.Id, out var native0);
            string source = spirit ? BottlerRules.SpiritDonor : look ? native0.Donor : m.Mined ? RefineryRules.Hydrates : "ItmScrapSteel";
            var native = DataHandler.dictCOs[source];
            var co = NativeDefinitions.Clone(native); var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[native.strItemDef]);
            co.strName = co.strItemDef = item.strName = m.Id;
            // An added material (0.45.0) carries its own name and text, which an add-on's translation may replace.
            co.strNameFriendly = co.strNameShort = m.Added ? Phobos.Ostranauts.Framework.Localization.Translations.Get(Text.Owner, "Material." + m.Id, m.Name) : Text.Get("Material." + m.Id);
            co.strDesc = m.Added ? Phobos.Ostranauts.Framework.Localization.Translations.Get(Text.Owner, "Material." + m.Id + "_description", m.Description) : Text.Get("Material." + m.Id + "_description");
            co.nStackLimit = m.Stack; co.mapChargeProfiles = Array.Empty<string>();
            string identity = m.Id + "Identity";
            d.Conditions[identity] = new JsonCond { strName = identity, strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
            var keep = look ? native0.Keep : m.Mined ? new[] { "IsNonHighlightable", "IsSolid", "IsMineral", "IsEdged", "IsTough", "IsImmuneAnnihilation", "IsOre", "StatDamage", "StatDamageMax" }
                : new[] { "IsRigid", "IsSolid", "StatDamageMax" };
            // A donor's damage rule would turn the item into something else (regolith into gangue): ours has none.
            if (look) co.aUpdateCommands = Array.Empty<string>();
            co.aStartingConds = co.aStartingConds.Where(s => spirit ? BottlerRules.KeepFromDonor(s.Split('=')[0]) : keep.Contains(s.Split('=')[0])).Concat(new[] { identity + "=1x1", m.Category + "=1x1",
                "StatMass=1x" + m.Kg.ToString(CultureInfo.InvariantCulture), "StatBasePrice=1x" + m.Price.ToString(CultureInfo.InvariantCulture) }).ToArray();
            if (m.Art.Length > 0 || m.Added)
            {
                // Ours live under this mod's image folder; an added material names its own picture, which the game
                // finds in whichever enabled mod's images folder holds it.
                string image = m.Added ? m.Image : ImagePath + m.Art;
                item.strImg = image; item.strImgNorm = image + "Normal"; item.strImgDamaged = "";
                co.strPortraitImg = item.strImg;
                item.nCols = m.Side; item.aSocketAdds = Enumerable.Repeat("TILItemAdds", m.Side * m.Side).ToArray();
                item.aSocketReqs = Border(m.Side, "Blank"); item.aSocketForbids = Border(m.Side, "TILItemForbids");
                co.inventoryWidth = co.inventoryHeight = m.Side;
            }
            d.Objects[m.Id] = co; d.Items[m.Id] = item;
            d.Loot[m.Id] = new Loot { strName = m.Id, strType = "item", aCOs = new[] { m.Id + "=1x1" }, aLoots = Array.Empty<string>() };
            EquipmentSaveUpgrade.Register(d, m.Id, m.Id);
            // Refining as a business (Manufacturing 0.26.0, owner decision of 1 October 2026): saved stock takes the
            // pack's current price on every load, so the retroactive prices reach ingots and salts already aboard.
            EquipmentSaveUpgrade.FollowPrice(m.Id);
        }
    }
    /// <summary>Materials that show a game item's own picture (0.51.0): the donor cloned, and the donor conditions kept.
    /// The paver must not keep IsFloorGrate, or the game's own floor job would lay it as a plain floor.</summary>
    private static readonly Dictionary<string, (string Donor, string[] Keep)> NativeLook = new(StringComparer.Ordinal)
    {
        [Materials.BakedRegolith] = (RefineryRules.Regolith, new[] { "IsSolid", "IsEdged", "IsTough" }),
        [Materials.RegolithPaver] = (RegolithFloor.LooseDonor, new[] { "IsSolid", "StatDamageMax", "StatInstallProgressMax", "StatUninstallProgressMax" })
    };

    /// <summary>The regolith floor (Manufacturing 0.51.0; owner decision, 5 October 2026): a Phobos twin of the game's
    /// Polished Regolith Floor that a crew member lays from one paver and can lift again. The twin clones the game's
    /// object and keeps its item definition, so the picture, sockets and stats are the game's own, referenced at run
    /// time; it keeps IsFloorGrate, so machines, pipes and belts treat it as floor. The two jobs are clones of the
    /// game's own floor jobs (welder to lay, structure cutter to lift), pointed at our paver and twin. The game's tile
    /// itself is never changed: it has no uninstall, and giving it one would break every tile already laid.</summary>
    private static void AddRegolithFloor(NativeDefinitions d)
    {
        string paver = Materials.RegolithPaver, floor = RegolithFloor.Installed;
        var co = NativeDefinitions.Clone(DataHandler.dictCOs[RegolithFloor.NativeTile]);
        co.strName = floor;
        co.strNameFriendly = Text.Get("RegolithFloor.name"); co.strDesc = Text.Get("RegolithFloor.description");
        co.aStartingConds = co.aStartingConds.Concat(new[] { RegolithFloor.Identity + "=1x1", "StatInstallProgressMax=1x200", "StatUninstallProgressMax=1x200" }).ToArray();
        d.Objects[floor] = co;
        d.Conditions[RegolithFloor.Identity] = new JsonCond { strName = RegolithFloor.Identity, strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[RegolithFloor.PaverTrigger] = new CondTrigger { strName = RegolithFloor.PaverTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { paver + "Identity" }, aForbids = new[] { "IsInstalled", "IsDamaged" }, aTriggers = Array.Empty<string>() };
        d.Triggers[RegolithFloor.InstalledTrigger] = new CondTrigger { strName = RegolithFloor.InstalledTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { RegolithFloor.Identity, "IsInstalled" }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        // The two jobs carry the same figures as the game's FloorGrate01Install and FloorGrate01Uninstall (welder to
        // lay, structure cutter to lift, hull work rates), written out so nothing depends on the game's job table.
        d.Installables[RegolithFloor.InstallJob] = new JsonInstallable
        {
            strName = RegolithFloor.InstallJob, strActionCO = paver, strActionGroup = "Work",
            strInteractionTemplate = "ACTInstallTEMP", CTThem = RegolithFloor.PaverTrigger, aInputs = new[] { RegolithFloor.PaverTrigger + "=1.0x1" },
            fTargetPointRange = 2, fDuration = 0.001f, aToolCTsUse = new[] { "TIsToolWelding" }, aLootCOs = new[] { floor },
            strStartInstall = floor, strBuildType = InstallMenu.Hull, strJobType = "install", bNoJobMenu = false,
            strAllowLootCTsUs = "CTWorkProgressHULL", strAllowLootCTsThem = "CONDInstallProgressx5", strProgressStat = "StatInstallProgress",
            strCTThemMultCondTools = "IsToolWelding", strCTThemMultCondUs = "StatInstallRateHULL"
        };
        d.Installables[RegolithFloor.UninstallJob] = new JsonInstallable
        {
            strName = RegolithFloor.UninstallJob, strActionCO = floor, strActionGroup = "Work",
            strInteractionTemplate = "ACTUninstallTEMP", CTThem = RegolithFloor.InstalledTrigger, aInputs = Array.Empty<string>(),
            fTargetPointRange = 2.5f, fDuration = 0.001f, aToolCTsUse = new[] { "TIsToolStructureCutter" }, aLootCOs = new[] { paver },
            strJobType = "uninstall", strAllowLootCTsUs = "CTWorkProgressHULL", strAllowLootCTsThem = "CONDUninstallProgressx5", strProgressStat = "StatUninstallProgress",
            strCTThemMultCondTools = "IsToolStructureCutter", strCTThemMultCondUs = "StatInstallRateHULL"
        };
    }
    internal static string[] Border(int side, string interior)
    {
        int width = side + 2;
        return Enumerable.Range(0, width * width).Select(i => i % width > 0 && i % width < width - 1 && i / width > 0 && i / width < width - 1 ? interior : "Blank").ToArray();
    }

    /// <summary>Three sizes of the game's own explosion object; the `Explosion,<name>` entries live in this mod's
    /// data/explosions file and the native component does everything else.</summary>
    private static void AddDeflagrations(NativeDefinitions d)
    {
        foreach (string size in new[] { "Small", "Medium", "Large" })
        {
            string id = GasStores.DeflagrationPrefix + size;
            d.Objects[id] = new JsonCondOwner { strName = id, strItemDef = id, strType = "Item", strNameFriendly = Text.Get("Store.deflagration"), strNameShort = Text.Get("Store.deflagration"),
                aStartingConds = new[] { "IsNonHighlightable=1x1", "IsSystem=1x1" }, aUpdateCommands = new[] { "Explosion,PhobosDeflagration" + size }, strPortraitImg = "blank",
                aInteractions = Array.Empty<string>(), mapSlotEffects = Array.Empty<string>(), mapPoints = Array.Empty<string>() };
            d.Items[id] = new JsonItemDef { strName = id, strImg = "blank", strImgNorm = "blank", strImgDamaged = "", nCols = 1, fZScale = .5f,
                aSocketAdds = new[] { "TILItemAdds" }, aSocketReqs = Border(1, "Blank"), aSocketForbids = Border(1, "Blank") };
            d.Loot[id] = new Loot { strName = id, strType = "item", aCOs = new[] { id + "=1x1" }, aLoots = Array.Empty<string>() };
        }
    }
}

/// <summary>The clay hydrate chunk takes a share of the game's C-class mining roll, carved from silicates
/// (owner loot policy, 30 September 2026: a new chunk replaces part of the native yield, never adds a roll).
/// Clays are hydrated phyllosilicates, so the chunk relabels part of what the game calls silicates. C-class
/// deposits roll this table directly, and dark-regolith walls reach it through their own nested C-class roll,
/// so one carve serves every route without counting twice. Never a shop.</summary>
internal static class MiningLoot
{
    internal const double DefaultChance = 0.10;
    /// <summary>The ammonium salt crust's share of the C-class roll, also from silicates (Manufacturing 0.9.0): Ceres,
    /// where Dawn found ammonium salts, is a dark carbonaceous body, and bright salt deposits are local, not bulk rock.</summary>
    internal const double CrustChance = 0.05;
    /// <summary>The evaporite crust's share (Manufacturing 0.18.0), also from silicates: a dried brine vein of the kind
    /// NASA's OSIRIS-REx team found in Bennu samples is a local vein, not bulk rock.</summary>
    internal const double EvaporiteChance = 0.05;
    /// <summary>The sulfide nodule's share of the M-class and S-class rolls (Manufacturing 0.19.0), carved from meteoric
    /// iron: troilite and schreibersite nodules sit inside iron meteorites, so a tenth of each iron share becomes one.</summary>
    internal const double NoduleMChance = 0.04, NoduleSChance = 0.02;
    internal const string MTable = "ItmRandomMineralMClass", STable = "ItmRandomMineralSClass", IronDonor = "ItmMineral01";
    internal const string Table = "ItmRandomMineralCClass", Donor = "ItmMineral04";
    internal static double Chance = DefaultChance;
    internal static void Add(NativeDefinitions d)
    {
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.ContainsKey(Table)) { Plugin.Log(Text.Get("Content.missing_table", Table)); return; }
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.ClayHydrates, Chance);
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.AmmoniumSaltCrust, CrustChance);
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.EvaporiteCrust, EvaporiteChance);
        foreach (var (table, share) in new[] { (MTable, NoduleMChance), (STable, NoduleSChance) })
            if (DataHandler.dictLoot.ContainsKey(table)) AdditiveLoot.CarveChoice(d, table, IronDonor, Materials.SulfideNodule, share);
            else Plugin.Log(Text.Get("Content.missing_table", table));
    }
}
