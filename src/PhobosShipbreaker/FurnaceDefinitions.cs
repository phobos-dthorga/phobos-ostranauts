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
        MachineDefinitions.AddFamily(d, FurnaceRules.Prefix);
        MachineDefinitions.AddFeed(d, FurnaceRules.Prefix, "IsAluminum");
        MachineDefinitions.AddFamily(d, FurnaceRules.Radiator);
        foreach (string p in new[] { FurnaceRules.Prefix, FurnaceRules.Radiator })
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool furnace = p == FurnaceRules.Prefix, installed = state.StartsWith("Installed"), damaged = state.EndsWith("Dmg");
            int height = furnace ? 6 : 4;
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get(furnace ? "Furnace.name" : "Furnace.radiator_name") + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get(furnace ? "Furnace.description" : "Furnace.radiator_description");
            Content.SetStat(co, "StatMass", furnace ? FurnaceRules.MachineKg : FurnaceRules.RadiatorKg);
            co.inventoryWidth = item.nCols = 6; co.inventoryHeight = height;
            co.nContainerWidth = co.nContainerHeight = furnace ? 8 : 0;
            co.dictSlotsLayout = new Dictionary<string, Vector3> { ["self"] = Vector3.zero };
            co.mapPoints = new[] { "use,0,-56", "PowerA,-40,-40", "PowerB,40,-40" };
            if (!furnace)
            {
                co.aStartingConds = co.aStartingConds.Where(c => !c.StartsWith("IsContainer=")).ToArray();
                co.aSlotsWeHave = Array.Empty<string>(); co.strLoot = "Blank";
                co.jsonPI = null; co.aTickers = Array.Empty<string>(); co.aInteractions = Array.Empty<string>();
            }
            Content.ApplyArtwork(co, item, p);
            item.aSocketAdds = Enumerable.Repeat(installed ? furnace ? "TILFixtureAdds" : "TILExtFixtureAdds" : "TILItemAdds", 6 * height).ToArray();
            item.aSocketReqs = Grid(height, installed && furnace ? "TILFloor" : "Blank");
            item.aSocketForbids = Grid(height, installed ? "TILObstruction" : "TILItemForbids");
            if (installed && !furnace)
                for (int x = 1; x <= 6; x++) item.aSocketReqs[(height + 1) * 8 + x] = "TILWall";
        }
        var bin = d.Objects[FurnaceRules.Feed];
        bin.strNameFriendly = bin.strNameShort = Text.Get("Furnace.feed_name"); bin.strDesc = Text.Get("Furnace.feed_description");
        bin.nContainerWidth = 10; bin.nContainerHeight = 8;
        d.Slots[FurnaceRules.Slot].bHide = true; d.Slots[FurnaceRules.Slot].strNameFriendly = bin.strNameFriendly;
        // A one-kW clock coefficient; the checked service substitutes each interval's
        // real bounded demand before native UsePower. No free IsPowered receipt.
        d.Power[FurnaceRules.Prefix + "Power"].fAmount = 1.0 / 3600;
        Packet(d, FurnaceRules.Blank, 19, 55, "Furnace.blank_name", "Furnace.blank_description", "PhobosFurnaceHousing");
        Packet(d, FurnaceRules.Housing, 18, 60, "Furnace.housing_name", "Furnace.housing_description", "PhobosFurnaceHousing");
        Packet(d, FurnaceRules.Remainder, 1, .01, "Furnace.remainder_name", "Furnace.remainder_description", ProcessRules.Residue);
        Packet(d, FurnaceRules.Section, FurnaceRules.SectionKg, 6500, "Furnace.section_name", "Furnace.section_description", ProcessRules.AssemblySection);
    }
    private static string[] Grid(int height, string interior) => Enumerable.Range(0, (height + 2) * 8)
        .Select(i => i % 8 > 0 && i % 8 < 7 && i / 8 > 0 && i / 8 <= height ? interior : "Blank").ToArray();
    private static void Packet(NativeDefinitions d, string id, double mass, double price, string name, string description, string art)
    {
        var co = NativeDefinitions.Clone(d.Objects[ProcessRules.Residue]); var item = NativeDefinitions.Clone(d.Items[ProcessRules.Residue]);
        co.strName = co.strItemDef = item.strName = id;
        co.strNameFriendly = co.strNameShort = Text.Get(name); co.strDesc = Text.Get(description);
        int side = id == FurnaceRules.Section ? 4 : id == FurnaceRules.Remainder ? 1 : 2;
        co.inventoryWidth = co.inventoryHeight = item.nCols = side;
        co.aStartingConds = new[] { "IsSolid=1x1", "IsRigid=1x1", id + "Identity=1x1" };
        Content.SetStat(co, "StatMass", mass); Content.SetStat(co, "StatBasePrice", price);
        item.aSocketAdds = Enumerable.Repeat("TILItemAdds", side * side).ToArray();
        item.aSocketReqs = Enumerable.Repeat("Blank", (side + 2) * (side + 2)).ToArray();
        item.aSocketForbids = Enumerable.Range(0, (side + 2) * (side + 2)).Select(i => i % (side + 2) > 0 && i % (side + 2) <= side && i / (side + 2) > 0 && i / (side + 2) <= side ? "TILItemForbids" : "Blank").ToArray();
        Content.ApplyArtwork(co, item, art);
        d.Conditions[id + "Identity"] = new JsonCond { strName = id + "Identity", strNameFriendly = co.strNameFriendly, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        d.Triggers[id + "Trigger"] = new CondTrigger { strName = id + "Trigger", fChance = 1, fCount = 1, bAND = true, aReqs = new[] { id + "Identity" }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
        d.Objects[id] = co; d.Items[id] = item;
    }
}
