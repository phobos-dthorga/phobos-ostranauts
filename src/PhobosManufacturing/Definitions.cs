using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Data;
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
        ReactorArt = "PhobosSabatierReactor", MethaneArt = "PhobosMethaneStore", ManifoldArt = "PhobosPropellantManifold", LineArt = "PropellantPipe",
        FillerArt = "PhobosCanisterFiller", RegulatorArt = "PhobosCabinAirRegulator", CrackerArt = "PhobosAmmoniaCracker", LeachArt = "PhobosLeachUnit";
    internal static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    internal static void Add(NativeDefinitions d)
    {
        foreach (string condition in new[] { ManufacturingRules.Working, ManufacturingRules.Electrolysing, ManufacturingRules.Reacting, ManufacturingRules.Filling, ManufacturingRules.Content })
            d.Conditions[condition] = new JsonCond { strName = condition, strNameFriendly = Text.Get("Condition." + condition), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("Panel.controls"); controls.strDesc = controls.strTooltip = Text.Get("Panel.controls_tooltip");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        AddMaterials(d);
        foreach (var machine in ChargeMachines.All) AddChargeMachine(d, machine);
        AddProcessor(d);
        AddReactor(d);
        AddCracker(d);
        // Every size of every gas store; each size's art is named after its own definition prefix.
        foreach (var store in GasStores.All)
            AddStore(d, store, Text.Get(store.TextPrefix + ".details", store.DryKg, store.CapacityKg, store.LeakKgPerHour, store.Footprint), store.Prefix);
        AddPropellantLine(d);
        AddManifold(d);
        AddFiller(d);
        AddRegulator(d);
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
            aReqs = (spec.AdmitsOre ? new[] { "IsOre" } : Array.Empty<string>()).Concat(spec.StockFeed.Select(id => id + "Identity")).ToArray(), aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Triggers[spec.FeedTrigger] = new CondTrigger { strName = spec.FeedTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = Array.Empty<string>(), aForbids = Array.Empty<string>(), aTriggers = new[] { "TIsFitContainerSolid", spec.StockTrigger } };
        ApplianceDefinitions.AddFeedBin(d, p, spec.FeedTrigger, shape.feedCells, Text.Get(spec.Text("feed_name")));
        d.Objects[spec.InputBin].strDesc = Text.Get(spec.Text("feed_description"), shape.feedCells);
        ApplianceDefinitions.SetPowerOverride(d, p, shape.idleKW, shape.workingKW, spec.WorkingCondition, shape.points.Keys.Where(k => k != "use").ToArray());
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
            co.mapPoints = new[] { "use,0,-24", "PowerA,0,8", "PhobosGasOut,8,0" };
            co.strPortraitImg = item.strImg;
        }
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
            co.mapPoints = new[] { "use,0,-24", "PowerA,0,8", "PhobosGasIn,-8,0", "PhobosGasOut,8,0" };
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
            co.mapPoints = new[] { "use,0,-24", "PowerA,0,8", "PhobosGasIn,-8,0", "PhobosGasOut,8,0" };
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
            co.mapPoints = new[] { "use,0,-24", "PowerA,-8,8", FillerRules.Inlet + ",24,8" };
            co.strPortraitImg = item.strImg;
            if (form.StartsWith("Installed", StringComparison.Ordinal)) { item.aSocketAdds[1] = PropellantLineRules.Prefix + "FixturePort"; item.ctSpriteSheet = PropellantLineRules.Prefix + "Sprite"; }
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
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Regulator.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsAirtight=", StringComparison.Ordinal)).ToArray();
            co.mapPoints = new[] { "use,0,-24", "PowerA,-8,8", RegulatorRules.Inlet + ",24,8" };
            co.strPortraitImg = item.strImg;
            if (form.StartsWith("Installed", StringComparison.Ordinal)) { item.aSocketAdds[1] = PropellantLineRules.Prefix + "FixturePort"; item.ctSpriteSheet = PropellantLineRules.Prefix + "Sprite"; }
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
            if (form.StartsWith("Installed", StringComparison.Ordinal)) { item.aSocketAdds[0] = PropellantLineRules.Prefix + "FixturePort"; item.ctSpriteSheet = PropellantLineRules.Prefix + "Sprite"; }
        }
    }

    /// <summary>The Fennmark propellant line, on the shared conduit pattern with its own identity and sprite trigger,
    /// so it never joins coolant or irrigation lines. Its art is a recorded recolour of the shared pipe sheet.</summary>
    private static void AddPropellantLine(NativeDefinitions d)
    {
        string pipe = PropellantLineRules.Prefix;
        foreach (string key in new[] { PropellantLineRules.Segment, PropellantLineRules.WorkingSegment })
            d.Conditions[key] = new JsonCond { strName = key, strNameFriendly = Text.Get("Line.name"), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[pipe + "Sprite"] = new CondTrigger { strName = pipe + "Sprite", fChance = 1, fCount = 1, bAND = true, aReqs = new[] { PropellantLineRules.Segment }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        foreach (bool intact in new[] { true, false })
        {
            string key = pipe + (intact ? "Adds" : "Off");
            d.Loot[key] = new Loot { strName = key, strType = "condition", aCOs = intact ? new[] { PropellantLineRules.Segment + "=1x1", PropellantLineRules.WorkingSegment + "=1x1" } : new[] { PropellantLineRules.Segment + "=1x1" }, aLoots = Array.Empty<string>() };
        }
        d.Loot[pipe + "FixturePort"] = new Loot { strName = pipe + "FixturePort", strType = "condition", aCOs = new[] { PropellantLineRules.Segment + "=1x1" }, aLoots = new[] { "TILFixtureAdds=1x1" } };
        ApplianceDefinitions.Add(d, pipe, Text.Get("Line.name"), Text.Get("Line.description"), 1, PropellantLineRules.Kg, Economy.SupplyPrice(PropellantLineRules.Prefix), ImagePath + LineArt, Controls, 0, InstallMenu.Hvac);
        d.Power.Remove(pipe + "Power"); d.Interactions.Remove(pipe + "PowerChange");
        foreach (string form in Forms)
        {
            bool installed = form.StartsWith("Installed", StringComparison.Ordinal), damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[pipe + form]; var item = d.Items[co.strItemDef];
            co.strNameFriendly = co.strNameShort = Text.Get("Line.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.nStackLimit = installed ? 1 : EquipmentEconomy.LineStack;
            co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            co.mapPoints = new[] { "use,0,-16" };
            co.aStartingConds = co.aStartingConds.Where(x => !x.StartsWith("IsContainer=", StringComparison.Ordinal) && !x.StartsWith("IsCumbersome=", StringComparison.Ordinal)).Concat(new[] { "IsPocketable=1x1" }).ToArray();
            co.nContainerWidth = co.nContainerHeight = 0; co.mapGUIPropMaps = Array.Empty<string>(); co.strContainerCT = null;
            item.fZScale = 1.01f;
            if (installed)
            {
                item.strImg = ImagePath + LineArt + "Sheet"; item.strImgNorm = item.strImg + "Normal";
                item.bHasSpriteSheet = true; item.ctSpriteSheet = pipe + "Sprite";
                item.aSocketAdds = new[] { pipe + (damaged ? "Off" : "Adds") };
                item.aSocketForbids = Enumerable.Range(0, 9).Select(i => i == 4 ? pipe + "Off" : "Blank").ToArray();
                item.aSocketReqs = Enumerable.Range(0, 9).Select(i => i == 4 ? "TILFloor" : "Blank").ToArray();
            }
        }
        // Every gas store gains a line port on the neighbouring tile of its local +X side, in the middle row, with pipe
        // art joining it at the footprint tile beside the port.
        foreach (var fuel in GasStores.All)
            foreach (string form in Forms)
            {
                var co = d.Objects[fuel.Prefix + form];
                var outlet = fuel.Outlet;
                co.mapPoints = co.mapPoints.Concat(new[] { ManifoldRules.StoreOutlet + "," + outlet.X + "," + outlet.Y }).ToArray();
                if (!form.StartsWith("Installed", StringComparison.Ordinal)) continue;
                var item = d.Items[fuel.Prefix + form];
                item.aSocketAdds[outlet.SocketIndex] = pipe + "FixturePort"; item.ctSpriteSheet = pipe + "Sprite";
            }
    }

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
            string source = m.Mined ? RefineryRules.Hydrates : "ItmScrapSteel";
            var native = DataHandler.dictCOs[source];
            var co = NativeDefinitions.Clone(native); var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[native.strItemDef]);
            co.strName = co.strItemDef = item.strName = m.Id;
            co.strNameFriendly = co.strNameShort = Text.Get("Material." + m.Id); co.strDesc = Text.Get("Material." + m.Id + "_description");
            co.nStackLimit = m.Stack; co.mapChargeProfiles = Array.Empty<string>();
            string identity = m.Id + "Identity";
            d.Conditions[identity] = new JsonCond { strName = identity, strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
            var keep = m.Mined ? new[] { "IsNonHighlightable", "IsSolid", "IsMineral", "IsEdged", "IsTough", "IsImmuneAnnihilation", "IsOre", "StatDamage", "StatDamageMax" }
                : new[] { "IsRigid", "IsSolid", "StatDamageMax" };
            co.aStartingConds = co.aStartingConds.Where(s => keep.Contains(s.Split('=')[0])).Concat(new[] { identity + "=1x1", m.Category + "=1x1",
                "StatMass=1x" + m.Kg.ToString(CultureInfo.InvariantCulture), "StatBasePrice=1x" + m.Price.ToString(CultureInfo.InvariantCulture) }).ToArray();
            if (m.Art.Length > 0)
            {
                item.strImg = ImagePath + m.Art; item.strImgNorm = ImagePath + m.Art + "Normal"; item.strImgDamaged = "";
                co.strPortraitImg = item.strImg;
                item.nCols = m.Side; item.aSocketAdds = Enumerable.Repeat("TILItemAdds", m.Side * m.Side).ToArray();
                item.aSocketReqs = Border(m.Side, "Blank"); item.aSocketForbids = Border(m.Side, "TILItemForbids");
                co.inventoryWidth = co.inventoryHeight = m.Side;
            }
            d.Objects[m.Id] = co; d.Items[m.Id] = item;
            d.Loot[m.Id] = new Loot { strName = m.Id, strType = "item", aCOs = new[] { m.Id + "=1x1" }, aLoots = Array.Empty<string>() };
            EquipmentSaveUpgrade.Register(d, m.Id, m.Id);
        }
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
    internal const string Table = "ItmRandomMineralCClass", Donor = "ItmMineral04";
    internal static double Chance = DefaultChance;
    internal static void Add(NativeDefinitions d)
    {
        if (DataHandler.dictLoot == null || !DataHandler.dictLoot.ContainsKey(Table)) { Plugin.Log(Text.Get("Content.missing_table", Table)); return; }
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.ClayHydrates, Chance);
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.AmmoniumSaltCrust, CrustChance);
        AdditiveLoot.CarveChoice(d, Table, Donor, Materials.EvaporiteCrust, EvaporiteChance);
    }
}
