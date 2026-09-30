using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native definitions for the T2 ice thaw unit (powered, one feed bin, one gangue tray) on the shared machine
/// family contract. The S3 to S5 process water silos it fills moved to Framework's water tanks in Shipbreaker 0.54.0,
/// with their ids, records and names unchanged.</summary>
internal static class ThawDefinitions
{
    internal const string ThawArt = "PhobosIceThaw";
    internal static void Add(NativeDefinitions d)
    {
        AddThaw(d);
        AddLinePorts(d);
    }
    /// <summary>Ports on Framework's shared lines (Shipbreaker 0.53.0): the T2 has a process-water port on its local -X
    /// side and a gas port on its +X side for methane, both in the middle row. Methane is carried by the gas line.
    /// Points are rebuilt from definitions on load.</summary>
    private static void AddLinePorts(NativeDefinitions d)
    {
        LineFamilies.Assign(ThawRules.MethaneCommodity, LineFamilies.Gas);
        var water = Phobos.Ostranauts.Framework.Items.SharedLines.ProcessWaterSpec(); var gas = Phobos.Ostranauts.Framework.Items.SharedLines.GasSpec();
        var tw = LinePorts.Water(ThawRules.Footprint); var tg = LinePorts.Gas(ThawRules.Footprint);
        LineDefinitions.AddPort(d, ThawRules.Prefix, water, LinePorts.WaterPoint, tw.X, tw.Y, tw.Socket);
        LineDefinitions.AddPort(d, ThawRules.Prefix, gas, LinePorts.GasPoint, tg.X, tg.Y, tg.Socket);
    }
    private static void AddThaw(NativeDefinitions d)
    {
        string p = ThawRules.Prefix;
        MachineDefinitions.AddFamily(d, p);
        MachineDefinitions.AddFeed(d, p, "IsIce");
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get("Thaw.name") + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Thaw.description", ThawRules.MachineKg, ThawRules.IceKg, ThawRules.WaterKg, ThawRules.GangueKg, ThawRules.WorkingKW, ThawRules.CycleSeconds / 60,
                ThawRules.MethaneIceKg, ThawRules.ClathrateWaterKg, ThawRules.MethaneKg, ThawRules.MethaneCycleSeconds / 60);
            Content.SetStat(co, "StatMass", ThawRules.MachineKg);
            // The gangue tray is the ordinary Inventory; the ice feed opens as its own titled window.
            co.nContainerWidth = ThawRules.TrayCells; co.nContainerHeight = 1;
            co.inventoryWidth = co.inventoryHeight = ThawRules.Footprint;
            co.dictSlotsLayout = new Dictionary<string, UnityEngine.Vector3> { ["self"] = UnityEngine.Vector3.zero };
            co.mapPoints = new[] { "use,0,-24", "PowerA,0,8" };
            item.nCols = ThawRules.Footprint; item.fZScale = 0.5f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", ThawRules.Footprint * ThawRules.Footprint).ToArray();
            item.aSocketReqs = Border(ThawRules.Footprint, installed ? "TILFloor" : "Blank");
            item.aSocketForbids = Border(ThawRules.Footprint, installed ? "TILObstruction" : "TILItemForbids");
            // One dedicated overhead sprite for every form, as the inventory portrait too; damaged forms use the
            // game's damage tint. The master and its provenance are in assets/artwork-completion.
            Content.ApplyArtwork(co, item, ThawArt, ThawArt);
            co.strPortraitImg = item.strImg;
        }
        var feed = d.Objects[ThawRules.InputBin];
        feed.strNameFriendly = feed.strNameShort = Text.Get("Thaw.feed_name");
        feed.strDesc = Text.Get("Thaw.feed_description", ThawRules.FeedCapacity, ThawRules.IceKg, ThawRules.MethaneIceKg);
        // Two one-cell blocks. FeedPatch also enforces the exact identity, mass and count.
        feed.nContainerWidth = ThawRules.FeedCapacity; feed.nContainerHeight = 1;
        d.Slots[ThawRules.InputSlot].bHide = true;
        d.Slots[ThawRules.InputSlot].strNameFriendly = feed.strNameFriendly;
        var power = d.Power[p + "Power"];
        power.aInputPts = new[] { "PowerA" };
        power.fAmount = ThawRules.IdleKW / Units.SecondsPerHour;
        power.strOverrideCond = ProcessRules.Working;
        power.fOverrideAmount = ThawRules.WorkingKW / Units.SecondsPerHour;
    }
    /// <summary>A socket grid one tile wider than the footprint on every side (Framework's shared rule).</summary>
    internal static string[] Border(int footprint, string interior) => MachineFamilies.Border(footprint, interior);
}
