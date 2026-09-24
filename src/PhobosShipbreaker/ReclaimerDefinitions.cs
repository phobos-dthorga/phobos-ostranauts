using System;
using System.Linq;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class ReclaimerDefinitions
{
    internal static void Add(NativeDefinitions d, double workingKW)
    {
        string p = ReclaimerRules.Prefix;
        MachineDefinitions.AddFamily(d, p);
        MachineDefinitions.AddFeed(d, p, ReclaimerRules.FeedCondition);
        foreach (string id in new[] { ReclaimerRules.FeedCondition, ReclaimerRules.SectionCondition })
            d.Conditions[id] = new JsonCond { strName = id, strNameFriendly = id, strColor = "Neutral", nDisplaySelf = 2, nDisplayOther = 2 };
        foreach (string state in new[] { "Installed", "Loose", "InstalledDmg", "LooseDmg" })
        {
            var co = d.Objects[p + state]; var item = d.Items[p + state];
            var source = d.Objects[Content.Prefix + state]; var sourceItem = d.Items[Content.Prefix + state];
            co.strNameFriendly = co.strNameShort = Text.Get("Reclaimer.name") + (state.EndsWith("Dmg") ? Text.Get("Content.damaged") : "");
            co.strDesc = Text.Get("Reclaimer.description", ReclaimerRules.MachineKg, ReclaimerRules.InputKg, ReclaimerRules.FeedCapacity, ReclaimerRules.RejectKg);
            Content.SetStat(co, "StatMass", ReclaimerRules.MachineKg);
            co.mapPoints = source.mapPoints.ToArray();
            co.dictSlotsLayout = new System.Collections.Generic.Dictionary<string, UnityEngine.Vector3> { ["self"] = UnityEngine.Vector3.zero };
            co.nContainerWidth = co.nContainerHeight = ReclaimerRules.OutputSize;
            co.inventoryWidth = co.inventoryHeight = ReclaimerRules.Footprint;
            co.aInteractions = state.StartsWith("Installed") ? new[] { "Inventory", ReclaimerRules.Controls } : new[] { "Inventory" };
            item.nCols = ReclaimerRules.Footprint;
            item.aSocketAdds = sourceItem.aSocketAdds.ToArray(); item.aSocketReqs = sourceItem.aSocketReqs.ToArray(); item.aSocketForbids = sourceItem.aSocketForbids.ToArray();
            // Closed transportable housing uses one original sprite and native damage tint.
            Content.ApplyArtwork(co, item, p, p);
        }
        var feed = d.Objects[ReclaimerRules.InputBin];
        feed.strNameFriendly = feed.strNameShort = Text.Get("Reclaimer.feed");
        feed.strDesc = Text.Get("Reclaimer.feed_description", ReclaimerRules.FeedCapacity, ReclaimerRules.InputKg);
        // Four one-cell packets. FeedPatch also enforces exact ID, mass and emptiness.
        feed.nContainerWidth = feed.nContainerHeight = 2;
        d.Slots[ReclaimerRules.InputSlot].bHide = true;
        d.Slots[ReclaimerRules.InputSlot].strNameFriendly = feed.strNameFriendly;
        var power = d.Power[p + "Power"];
        power.fAmount = ReclaimerRules.IdleKW / Units.SecondsPerHour;
        power.strOverrideCond = ProcessRules.Working;
        power.fOverrideAmount = workingKW / Units.SecondsPerHour;
        var controls = NativeDefinitions.Clone(DataHandler.dictInteractions["Inventory"]);
        controls.strName = ReclaimerRules.Controls; controls.strTitle = Text.Get("CollectorDefinitions.control_panel");
        controls.strDesc = Text.Get("Reclaimer.controls_action"); controls.strTooltip = Text.Get("Reclaimer.controls_tooltip");
        controls.strRaiseUI = null; controls.fTargetPointRange = 2; d.Interactions[controls.strName] = controls;
        Packet(d, ReclaimerRules.Feedstock, ReclaimerRules.InputKg, "Reclaimer.residue_name", "Reclaimer.residue_description", ReclaimerRules.FeedCondition);
        Packet(d, ReclaimerRules.Reject, ReclaimerRules.RejectKg, "Reclaimer.reject_name", "Reclaimer.reject_description", null);
        var section = NativeDefinitions.Clone(d.Objects[ProcessRules.AssemblySection]);
        var sectionItem = NativeDefinitions.Clone(d.Items[ProcessRules.AssemblySection]);
        section.strName = section.strItemDef = sectionItem.strName = ReclaimerRules.Section;
        section.strNameFriendly = section.strNameShort = Text.Get("Reclaimer.section_name");
        section.strDesc = Text.Get("Reclaimer.section_description", ReclaimerRules.SectionKg, ReclaimerRules.MachineKg);
        section.aStartingConds = section.aStartingConds.Where(s => !s.StartsWith(ProcessRules.AssemblySectionCondition + "=")).Concat(new[] { ReclaimerRules.SectionCondition + "=1x1" }).ToArray();
        Content.SetStat(section, "StatMass", ReclaimerRules.SectionKg);
        d.Objects[section.strName] = section; d.Items[sectionItem.strName] = sectionItem;
        d.Triggers[ReclaimerRules.SectionTrigger] = new CondTrigger { strName = ReclaimerRules.SectionTrigger, fChance = 1, fCount = 1, bAND = true,
            aReqs = new[] { ReclaimerRules.SectionCondition }, aForbids = Array.Empty<string>(), aTriggers = Array.Empty<string>() };
    }
    private static void Packet(NativeDefinitions d, string id, double kg, string name, string description, string? condition)
    {
        var co = NativeDefinitions.Clone(d.Objects[ProcessRules.Residue]);
        var item = NativeDefinitions.Clone(d.Items[ProcessRules.Residue]);
        co.strName = co.strItemDef = item.strName = id;
        co.strNameFriendly = co.strNameShort = Text.Get(name); co.strDesc = Text.Get(description);
        co.inventoryWidth = co.inventoryHeight = item.nCols = 1;
        item.aSocketAdds = new[] { "TILItemAdds" };
        item.aSocketReqs = Enumerable.Repeat("Blank", 9).ToArray();
        item.aSocketForbids = Enumerable.Range(0, 9).Select(i => i == 4 ? "TILItemForbids" : "Blank").ToArray();
        Content.SetStat(co, "StatMass", kg); Content.SetStat(co, "StatBasePrice", .01);
        if (condition != null) co.aStartingConds = co.aStartingConds.Concat(new[] { condition + "=1x1" }).ToArray();
        d.Objects[id] = co; d.Items[id] = item;
    }
}
