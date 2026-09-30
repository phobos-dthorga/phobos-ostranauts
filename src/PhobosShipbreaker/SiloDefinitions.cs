using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

/// <summary>Native definitions for the S3 process-water silo (passive vessel) and the T2 ice thaw unit
/// (powered, one feed bin, one gangue tray). Both use the shared machine family contract.</summary>
internal static class SiloDefinitions
{
    internal const string SiloArt = "PhobosProcessSilo", ThawArt = "PhobosIceThaw";
    internal static void Add(NativeDefinitions d)
    {
        foreach (var spec in SiloService.Specs) BulkVessels.Register(spec);
        foreach (var size in SiloRules.Sizes) AddSilo(d, size);
        AddThaw(d);
    }
    private static void AddSilo(NativeDefinitions d, SiloSize size)
    {
        string p = size.Prefix; int footprint = size.Footprint;
        MachineDefinitions.AddFamily(d, p);
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            co.strNameFriendly = co.strNameShort = Text.Get(size.NameKey) + (damaged ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Silo.description", size.DryKg, size.CapacityKg, footprint);
            // A passive vessel: no container, no feed, no electricity, no tickers. Its water is a saved record.
            co.strLoot = "Blank"; co.aSlotsWeHave = Array.Empty<string>(); co.strContainerCT = null;
            co.nContainerWidth = co.nContainerHeight = 0;
            co.aStartingConds = co.aStartingConds.Where(s => !s.StartsWith("IsContainer=", StringComparison.Ordinal)).ToArray();
            co.mapGUIPropMaps = Array.Empty<string>();
            co.jsonPI = null; co.aTickers = Array.Empty<string>();
            co.aInteractions = Array.Empty<string>();
            co.inventoryWidth = co.inventoryHeight = footprint;
            Content.SetStat(co, "StatMass", size.DryKg);
            co.mapPoints = new[] { "use,0," + (-8 * footprint - 8) };
            item.nCols = footprint; item.fZScale = 0.5f;
            item.aSocketAdds = Enumerable.Repeat(installed ? "TILFixtureAdds" : "TILItemAdds", footprint * footprint).ToArray();
            item.aSocketReqs = Border(footprint, installed ? "TILFloor" : "Blank");
            item.aSocketForbids = Border(footprint, installed ? "TILObstruction" : "TILItemForbids");
            // One dedicated overhead sprite for every form, as the inventory portrait too; damaged forms use the
            // game's damage tint. The master and its provenance are in assets/artwork-completion.
            Content.ApplyArtwork(co, item, size.Art, size.Art);
            co.strPortraitImg = item.strImg;
        }
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
    /// <summary>A socket grid one tile wider than the footprint on every side: the interior value inside, Blank on the rim.</summary>
    internal static string[] Border(int footprint, string interior)
    {
        int side = footprint + 2;
        return Enumerable.Range(0, side * side).Select(i =>
            i % side > 0 && i % side < side - 1 && i / side > 0 && i / side < side - 1 ? interior : "Blank").ToArray();
    }
}
