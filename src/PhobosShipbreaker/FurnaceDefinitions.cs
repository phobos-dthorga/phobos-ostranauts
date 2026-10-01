using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;
using UnityEngine;

namespace PhobosShipbreaker;

internal static class FurnaceDefinitions
{
    internal static void Add(NativeDefinitions d)
    {
        FurnaceService.AddCoolantStock(d);
        MachineDefinitions.AddFamily(d, FurnaceRules.Prefix);
        MachineDefinitions.AddFeed(d, FurnaceRules.Prefix, "IsAluminum", FurnaceRecipes.FeedConditions);
        MachineDefinitions.AddFamily(d, FurnaceRules.Radiator, InstallMenu.Hvac);
        MachineDefinitions.AddFamily(d, FurnaceRules.ThermalPort, InstallMenu.Hvac);
        foreach (string p in new[] { FurnaceRules.Prefix, FurnaceRules.Radiator, FurnaceRules.ThermalPort })
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool furnace = p == FurnaceRules.Prefix, installed = state.StartsWith("Installed"), damaged = state.EndsWith("Dmg");
            bool port = p == FurnaceRules.ThermalPort;
            int width = port ? 1 : 6, height = port ? 1 : furnace ? 6 : 4;
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get(furnace ? "Furnace.name" : port ? "Furnace.port_name" : "Furnace.radiator_name") + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get(furnace ? "Furnace.description" : port ? "Furnace.port_description" : "Furnace.radiator_description");
            Content.SetStat(co, "StatMass", furnace ? FurnaceRules.MachineKg : FurnaceRules.RadiatorKg);
            co.inventoryWidth = item.nCols = width; co.inventoryHeight = height;
            if (furnace) EquipmentInventory.Declare(co, InventorySpec.ProductTray(FurnaceRules.TrayWidth, FurnaceRules.TrayHeight));
            co.dictSlotsLayout = new Dictionary<string, Vector3> { ["self"] = Vector3.zero };
            co.mapPoints = new[] { "use,0,-56", "PowerA,-40,-40", "PowerB,40,-40" };
            if (furnace) co.mapPoints = co.mapPoints.Concat(new[] { "CoolingLeft,-56,8", "CoolingRight,56,8", "CoolingRear,0,96", "MaterialIn,-40,-40", "MaterialOut,40,-40" }).ToArray();
            if (!furnace && !port) co.mapPoints = co.mapPoints.Concat(new[] { "CoolantIn,8,-56" }).ToArray();
            if (port) co.mapPoints = new[] { "use,0,0" };
            if (!furnace)
            {
                // Keep a reject-all receptacle solely to restore legacy saved cargo.
                // Removing it would strand children during native Ship.SpawnItems.
                EquipmentInventory.Declare(co, InventorySpec.LegacyReceptacle(8, 8, CoolingCargo.RejectAll));
                co.aStartingConds = co.aStartingConds.Where(c => !c.StartsWith("IsContainer=")).ToArray();
                co.aSlotsWeHave = Array.Empty<string>(); co.strLoot = "Blank";
                co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            }
            Content.ApplyArtwork(co, item, p);
            if (furnace && installed) item.strImg = "phobos/shipbreaker/PhobosFurnaceSockets";
            if (!furnace && !port && installed) item.strImg = "phobos/shipbreaker/PhobosFurnaceRadiatorSocket";
            item.aSocketAdds = Enumerable.Repeat(installed ? furnace || port ? "TILFixtureAdds" : "TILExtFixtureAdds" : "TILItemAdds", width * height).ToArray();
            item.aSocketReqs = Grid(width, height, installed && (furnace || port) ? "TILFloor" : "Blank");
            item.aSocketForbids = Grid(width, height, installed ? "TILObstruction" : "TILItemForbids");
            if (installed && port) item.aSocketForbids[4] = "PhobosFurnacePortForbids";
            if (installed && !furnace && !port)
                for (int x = 1; x <= 6; x++) item.aSocketReqs[(height + 1) * 8 + x] = "TILWall";
        }
        // Keep the native floor as the pressure barrier; damaged/EVA decking cannot support the assembly.
        var portForbids = NativeDefinitions.Clone(DataHandler.dictLoot["TILObstruction"]);
        portForbids.strName = "PhobosFurnacePortForbids";
        portForbids.aCOs = portForbids.aCOs.Concat(new[] { "IsEVATile=1x1", "IsWall=1x1" }).Distinct().ToArray();
        d.Loot[portForbids.strName] = portForbids;
        var bin = d.Objects[FurnaceRules.Feed];
        bin.strNameFriendly = bin.strNameShort = Text.Get("Furnace.feed_name"); bin.strDesc = Text.Get("Furnace.feed_description");
        EquipmentInventory.Declare(bin, InventorySpec.Feed(FurnaceRules.ChamberWidth, FurnaceRules.ChamberHeight, bin.strContainerCT));
        d.Slots[FurnaceRules.Slot].bHide = true; d.Slots[FurnaceRules.Slot].strNameFriendly = bin.strNameFriendly;
        // A one-kW clock coefficient; the checked service substitutes each interval's
        // real bounded demand before native UsePower. No free IsPowered receipt.
        d.Power[FurnaceRules.Prefix + "Power"].fAmount = 1.0 / 3600;
        // Housing stock, the ingots (Manufacturing raw stock, owner approval 29 September 2026) and the terminal remainders, from the materials pack.
        foreach (var (id, name) in new[] { (FurnaceRules.Blank, "blank"), (FurnaceRules.Housing, "housing"), (FurnaceRules.Remainder, "remainder"),
            (FurnaceRecipes.AluminiumIngot, "aluminium_ingot"), (FurnaceRecipes.SteelIngot, "steel_ingot"), (FurnaceRecipes.SteelRemainder, "steel_remainder") })
        {
            var m = ShipbreakerMaterials.Entry(id);
            Packet(d, id, m.kg, m.price, "Furnace." + name + "_name", "Furnace." + name + "_description", m.art ?? "", m.side, m.stack);
            if (m.category != null) d.Objects[id].aStartingConds = d.Objects[id].aStartingConds.Concat(new[] { m.category + "=1x1" }).ToArray();
        }
        Packet(d, FurnaceRules.Section, FurnaceRules.SectionKg, 6500, "Furnace.section_name", "Furnace.section_description", "PhobosFurnaceSectionDedicated");
        CoolingCargo.Add(d);
    }
    private static string[] Grid(int width, int height, string interior) => Enumerable.Range(0, (height + 2) * (width + 2))
        .Select(i => i % (width + 2) > 0 && i % (width + 2) <= width && i / (width + 2) > 0 && i / (width + 2) <= height ? interior : "Blank").ToArray();
    private static void Packet(NativeDefinitions d, string id, double mass, double price, string name, string description, string art, int side = 0, int stack = 1)
    {
        var co = NativeDefinitions.Clone(d.Objects[ProcessRules.Residue]); var item = NativeDefinitions.Clone(d.Items[ProcessRules.Residue]);
        co.strName = co.strItemDef = item.strName = id;
        co.strNameFriendly = co.strNameShort = Text.Get(name); co.strDesc = Text.Get(description);
        if (side == 0) side = id == FurnaceRules.Section ? 4 : id == FurnaceRules.Remainder ? 1 : 2;
        co.nStackLimit = stack;
        co.inventoryWidth = co.inventoryHeight = item.nCols = side;
        co.aStartingConds = new[] { "IsSolid=1x1", "IsRigid=1x1", id + "Identity=1x1" };
        Content.SetStat(co, "StatMass", mass); Content.SetStat(co, "StatBasePrice", price);
        item.aSocketAdds = Enumerable.Repeat("TILItemAdds", side * side).ToArray();
        item.aSocketReqs = Enumerable.Repeat("Blank", (side + 2) * (side + 2)).ToArray();
        item.aSocketForbids = Enumerable.Range(0, (side + 2) * (side + 2)).Select(i => i % (side + 2) > 0 && i % (side + 2) <= side && i / (side + 2) > 0 && i / (side + 2) <= side ? "TILItemForbids" : "Blank").ToArray();
        if (id == FurnaceRules.Housing) Content.ApplyArtwork(co, item, art);
        else Content.ApplyStockArtwork(co, item, art);
        d.Conditions[id + "Identity"] = new JsonCond { strName = id + "Identity", strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[id + "Trigger"] = new CondTrigger { strName = id + "Trigger", fChance = 1, fCount = 1, bAND = true, aReqs = new[] { id + "Identity" }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Objects[id] = co; d.Items[id] = item;
    }
}
