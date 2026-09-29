using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Native definitions for the six Fennmark machines, the propellant line, the five materials, and the three deflagration
/// objects. Machines use Framework's appliance contract; materials clone the game's own scrap and hydrate
/// definitions so pickup, stacking, damage and market behaviour stay vanilla.</summary>
internal static class Definitions
{
    internal const string Controls = "PhobosManufacturingControls", ImagePath = "phobos/manufacturing/";
    internal const string RefineryArt = "PhobosVolatilesRefinery", ProcessorArt = "PhobosChemicalProcessor", StoreArt = "PhobosHydrogenStore",
        ReactorArt = "PhobosSabatierReactor", MethaneArt = "PhobosMethaneStore", ManifoldArt = "PhobosPropellantManifold", LineArt = "PropellantPipe";
    internal static readonly string[] Forms = { "Installed", "Loose", "InstalledDmg", "LooseDmg" };
    internal static void Add(NativeDefinitions d, bool steelStock)
    {
        foreach (string condition in new[] { ManufacturingRules.Working, ManufacturingRules.Electrolysing, ManufacturingRules.Reacting, ManufacturingRules.Content })
            d.Conditions[condition] = new JsonCond { strName = condition, strNameFriendly = Text.Get("Condition." + condition), strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = Controls; controls.strTitle = Text.Get("Panel.controls"); controls.strDesc = controls.strTooltip = Text.Get("Panel.controls_tooltip");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2;
        d.Interactions[Controls] = controls;
        AddMaterials(d);
        AddRefinery(d, steelStock);
        AddProcessor(d);
        AddReactor(d);
        AddStore(d, FuelStores.Hydrogen, Text.Get("Store.description", HydrogenRules.DryKg, HydrogenRules.CapacityKg, HydrogenRules.LeakKgPerHour), StoreArt);
        AddStore(d, FuelStores.Methane, Text.Get("Methane.description", MethaneRules.DryKg, MethaneRules.CapacityKg, MethaneRules.LeakKgPerHour), MethaneArt);
        AddPropellantLine(d);
        AddManifold(d);
        AddDeflagrations(d);
    }

    private static void AddRefinery(NativeDefinitions d, bool steelStock)
    {
        string p = RefineryRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Refinery.name"), Text.Get("Refinery.description", RefineryRules.MachineKg, RefineryRules.WorkingKW, RefineryRules.Footprint),
            RefineryRules.Footprint, RefineryRules.MachineKg, RefineryRules.Price, ImagePath + RefineryArt, Controls, RefineryRules.IdleKW, InstallMenu.Appliances);
        // Feed at the game level: any ore (the native TIsOre rule) or our own stock; the container patch then
        // applies the exact identity, mass and count rule.
        d.Triggers[RefineryRules.StockTrigger] = new CondTrigger { strName = RefineryRules.StockTrigger, fChance = 1, fCount = 1, bAND = false,
            aReqs = new[] { "IsOre" }.Concat(RefineryRules.StockFeed.Select(id => id + "Identity")).ToArray(), aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Triggers[RefineryRules.FeedTrigger] = new CondTrigger { strName = RefineryRules.FeedTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = Array.Empty<string>(), aForbids = Array.Empty<string>(), aTriggers = new[] { "TIsFitContainerSolid", RefineryRules.StockTrigger } };
        ApplianceDefinitions.AddFeedBin(d, p, RefineryRules.FeedTrigger, RefineryRules.FeedCapacity, Text.Get("Refinery.feed_name"));
        d.Objects[RefineryRules.InputBin].strDesc = Text.Get("Refinery.feed_description", RefineryRules.FeedCapacity);
        ApplianceDefinitions.SetPowerOverride(d, p, RefineryRules.IdleKW, RefineryRules.WorkingKW, ManufacturingRules.Working, "PowerA", "PowerB");
        foreach (string form in Forms)
        {
            var co = d.Objects[p + form]; var item = d.Items[p + form];
            bool damaged = form.EndsWith("Dmg", StringComparison.Ordinal);
            co.strNameFriendly = co.strNameShort = Text.Get("Refinery.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.mapPoints = new[] { "use,0,-40", "PowerA,-24,24", "PowerB,24,24" };
            co.strPortraitImg = item.strImg;
        }
    }

    private static void AddProcessor(NativeDefinitions d)
    {
        string p = ProcessorRules.Prefix;
        ApplianceDefinitions.Add(d, p, Text.Get("Processor.name"), Text.Get("Processor.description", ProcessorRules.MachineKg, ProcessorRules.WorkingKW, ProcessorRules.WaterKgPerCycle, ProcessorRules.OxygenKgPerCycle, ProcessorRules.HydrogenKgPerCycle),
            ProcessorRules.Footprint, ProcessorRules.MachineKg, ProcessorRules.Price, ImagePath + ProcessorArt, Controls, ProcessorRules.IdleKW, InstallMenu.Appliances);
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
            SabatierRules.Footprint, SabatierRules.MachineKg, SabatierRules.Price, ImagePath + ReactorArt, Controls, SabatierRules.IdleKW, InstallMenu.Appliances);
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

    private static void AddStore(NativeDefinitions d, FuelStore fuel, string description, string art)
    {
        string p = fuel.Prefix, name = Text.Get(fuel.TextPrefix + ".name");
        BulkVessels.Register(fuel.Spec);
        ApplianceDefinitions.Add(d, p, name, description, 2, fuel.DryKg, fuel.Price, ImagePath + art, Controls, 0, InstallMenu.Appliances);
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
            co.mapPoints = new[] { "use,0,-24" };
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
            ManifoldRules.Footprint, ManifoldRules.MachineKg, ManifoldRules.Price, ImagePath + ManifoldArt, Controls, 0, InstallMenu.Hvac);
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
        ApplianceDefinitions.Add(d, pipe, Text.Get("Line.name"), Text.Get("Line.description"), 1, PropellantLineRules.Kg, PropellantLineRules.Price, ImagePath + LineArt, Controls, 0, InstallMenu.Hvac);
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
        // The fuel stores gain a line port on the neighbouring tile of their local +X side, with pipe art joining them.
        foreach (var fuel in FuelStores.All)
            foreach (string form in Forms)
            {
                var co = d.Objects[fuel.Prefix + form];
                co.mapPoints = co.mapPoints.Concat(new[] { ManifoldRules.StoreOutlet + ",24,8" }).ToArray();
                if (!form.StartsWith("Installed", StringComparison.Ordinal)) continue;
                var item = d.Items[fuel.Prefix + form];
                item.aSocketAdds[1] = pipe + "FixturePort"; item.ctSpriteSheet = pipe + "Sprite";
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
    /// behaviour); the clay chunk clones the game's hydrates so it mines, breaks and sells like them.</summary>
    private static void AddMaterials(NativeDefinitions d)
    {
        foreach (var m in Materials.All)
        {
            string source = m == Materials.Clay ? RefineryRules.Hydrates : "ItmScrapSteel";
            var native = DataHandler.dictCOs[source];
            var co = NativeDefinitions.Clone(native); var item = NativeDefinitions.Clone(DataHandler.dictItemDefs[native.strItemDef]);
            co.strName = co.strItemDef = item.strName = m.Id;
            co.strNameFriendly = co.strNameShort = Text.Get("Material." + m.Id); co.strDesc = Text.Get("Material." + m.Id + "_description");
            co.nStackLimit = m.Stack; co.mapChargeProfiles = Array.Empty<string>();
            string identity = m.Id + "Identity";
            d.Conditions[identity] = new JsonCond { strName = identity, strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
            var keep = m == Materials.Clay ? new[] { "IsNonHighlightable", "IsSolid", "IsMineral", "IsEdged", "IsTough", "IsImmuneAnnihilation", "IsOre", "StatDamage", "StatDamageMax" }
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
            string id = FuelStores.DeflagrationPrefix + size;
            d.Objects[id] = new JsonCondOwner { strName = id, strItemDef = id, strType = "Item", strNameFriendly = Text.Get("Store.deflagration"), strNameShort = Text.Get("Store.deflagration"),
                aStartingConds = new[] { "IsNonHighlightable=1x1", "IsSystem=1x1" }, aUpdateCommands = new[] { "Explosion,PhobosDeflagration" + size }, strPortraitImg = "blank",
                aInteractions = Array.Empty<string>(), mapSlotEffects = Array.Empty<string>(), mapPoints = Array.Empty<string>() };
            d.Items[id] = new JsonItemDef { strName = id, strImg = "blank", strImgNorm = "blank", strImgDamaged = "", nCols = 1, fZScale = .5f,
                aSocketAdds = new[] { "TILItemAdds" }, aSocketReqs = Border(1, "Blank"), aSocketForbids = Border(1, "Blank") };
            d.Loot[id] = new Loot { strName = id, strType = "item", aCOs = new[] { id + "=1x1" }, aLoots = Array.Empty<string>() };
        }
    }
}

/// <summary>The clay hydrate chunk joins the game's own mining tables as one bounded choice: the dark-regolith
/// rock salvage outputs and the C-class deposit table. Never a shop.</summary>
internal static class MiningLoot
{
    internal const double DefaultChance = 0.10;
    internal static readonly string[] Tables = { "ItmRock05SalvageOutput", "ItmRock06SalvageOutput", "ItmRandomMineralCClass" };
    internal static double Chance = DefaultChance;
    internal static void Add(NativeDefinitions d)
    {
        foreach (string table in Tables)
        {
            if (DataHandler.dictLoot == null || !DataHandler.dictLoot.ContainsKey(table)) { Plugin.Log(Text.Get("Content.missing_table", table)); continue; }
            AdditiveLoot.SetItemChoice(d, table, "PhobosManufacturingClay_" + table, new Dictionary<string, double> { [Materials.ClayHydrates] = Chance });
        }
    }
}
