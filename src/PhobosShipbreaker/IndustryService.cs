using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal sealed class EquipmentCard
{
    internal string Id = "", Name = "", Group = "", Detail = "", InstrumentStatus = "", SearchScope = "";
    internal EquipmentState State;
    internal bool Attention, Instrument;
}

/// <summary>One command boundary for local panels, central console and F3. No ambient remote bypass.</summary>
internal static class IndustryService
{
    internal static CondOwner[] Discover(Ship? ship) => ship == null || (int)ship.LoadState < 2 ? Array.Empty<CondOwner>() :
        ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
            .Where(c => c != null && !c.bDestroyed && c.ship == ship && c.objCOParent == null && c.HasCond("IsInstalled") && IndustrialRules.Equipment(c.strCODef))
            .OrderBy(c => IndustrialRules.Group(c.strCODef), StringComparer.Ordinal).ThenBy(c => c.strID, StringComparer.Ordinal).ToArray();
    internal static EquipmentCard[] SnapshotShip(Ship? ship)
    {
        var equipment = Discover(ship); var intake = Plugin.Service.IntakeActivities(equipment);
        return equipment.Select(c => Snapshot(c, intake.TryGetValue(c.strID, out var activity) ? activity : (EquipmentActivity?)null)).ToArray();
    }
    internal static EquipmentCard Snapshot(CondOwner co, EquipmentActivity? intake = null)
    {
        var group = IndustrialRules.Group(co.strCODef);
        var process = ProcessingService.IsProcessor(co.strCODef) ? Plugin.Service.Activity(co) :
            group == "collector" ? Plugin.Collectors.Activity(co) : intake ?? Plugin.Service.IntakeActivity(co);
        bool attention = process.NeedsAttention;
        string detail = process.Detail;
        if (group == "reclaimer")
        {
            var receiving = Plugin.Collectors.Activity(co); attention |= receiving.NeedsAttention;
            detail += "\n\n" + Text.Get("Industry.receiving") + ": " + StateName(receiving.State) + "\n" + receiving.Detail;
            detail += "\n\n" + IndustryObservations.ProbeDetails(co);
        }
        if (group == "fixture") detail += "\n\n" + (intake?.Detail ?? Plugin.Service.DescribeIntake(co));
        double demand = group == "fixture" ? Plugin.Options.WorkingKW : group == "reclaimer" ? Plugin.Options.ReclaimerKW : group == "collector" ? Plugin.Options.CollectorKW : group == "grabber" ? IntakeRules.WorkingKW : 0;
        if (demand > 0) detail += "\n\n" + Text.Get("Industry.demand", demand) + (group == "reclaimer" ? Text.Get("Industry.feed_demand", Plugin.Options.FeederKW) : "");
        if (co.objContainer != null) detail += "\n" + Text.Get("Industry.stored", co.objContainer.ContainedCOs.Count, co.objContainer.ContainedCOs.Sum(c => c.GetTotalMass()));
        if (RoutingRules.IsSender(co.strCODef)) detail += "\n\n" + CollectorService.DescribeLink(co, true);
        if (RoutingRules.IsReceiver(co.strCODef)) detail += "\n\n" + CollectorService.DescribeLink(co, false) + "\n" + CollectorService.FilterLabel(co);
        return new EquipmentCard { Id = co.strID, Name = co.strNameFriendly + " [" + Phobos.Ostranauts.Framework.Inventory.PortPairing.ShortId(co.strID) + "]",
            Group = group, State = process.State, Attention = attention, Detail = detail + IndustryObservations.ExplainStop(co) };
    }
    internal static string StateName(EquipmentState state) => Text.Get("Industry.state_" + state);
    internal static bool Run(ConsoleBinding? binding, string targetId, string action, string? value, out string message)
    {
        var target = CollectorService.Resolve(targetId);
        message = Text.Get("Industry.missing");
        if (target == null || !IndustrialRules.Equipment(target.strCODef)) return false;
        if (binding != null)
        {
            message = ControlAuthority.Check(target, binding) ?? "";
            if (message.Length != 0) return false;
        }
        bool processor = ProcessingService.IsProcessor(target.strCODef), receiver = RoutingRules.IsReceiver(target.strCODef);
        bool result;
        switch (action)
        {
            case "start" when processor: result = Plugin.Service.Start(target, binding); message = Plugin.Service.Describe(target); return result;
            case "pause" when processor: result = Plugin.Service.Pause(target, false, binding); message = Plugin.Service.Describe(target); return result;
            case "cancel" when processor: result = Plugin.Service.Pause(target, true, binding); message = Plugin.Service.Describe(target); return result;
            case "receive" when receiver: result = Plugin.Collectors.Start(target, binding); message = Plugin.Collectors.Describe(target); return result;
            case "pause-receive" when receiver: result = Plugin.Collectors.Pause(target, binding); message = Plugin.Collectors.Describe(target); return result;
            case "filter" when receiver: return Plugin.Collectors.SetFilter(target, value ?? "", out message, binding);
            case "unlink-input" when receiver: return Plugin.Collectors.Unlink(target, out message, false, binding);
            case "unlink-output" when RoutingRules.IsSender(target.strCODef): return Plugin.Collectors.Unlink(target, out message, true, binding);
            case "link-input" when receiver:
            case "link-output" when RoutingRules.IsSender(target.strCODef):
                var peer = value == null ? null : CollectorService.Resolve(value);
                if (peer == null) return false;
                var source = action == "link-input" ? peer : target; var dest = action == "link-input" ? target : peer;
                result = Plugin.Collectors.Bind(dest, source, binding); message = Plugin.Collectors.Describe(dest); return result;
            case "feed" when binding == null && processor: result = Plugin.Service.OpenInventory(target, true); message = Plugin.Service.Describe(target); return result;
            case "products" when binding == null && processor: result = Plugin.Service.OpenInventory(target, false); message = Plugin.Service.Describe(target); return result;
            case "inventory" when binding == null && receiver: result = Plugin.Collectors.OpenInventory(target); message = Plugin.Collectors.Describe(target); return result;
            default: message = Text.Get("Industry.unsupported_action"); return false;
        }
    }
    internal static string PauseAll(ConsoleBinding binding)
    {
        var console = CollectorService.Resolve(binding.ConsoleId);
        string? problem = console == null ? Text.Get("Industry.missing") : ControlAuthority.Check(console, binding);
        if (problem != null) return problem;
        var results = new List<string>();
        foreach (var target in Discover(console!.ship))
        {
            if (ProcessingService.IsProcessor(target.strCODef)) { bool ok = Run(binding, target.strID, "pause", null, out string info); results.Add(target.strNameFriendly + ": " + Text.Get(ok ? "Industry.success" : "Industry.rejected", info)); }
            if (RoutingRules.IsReceiver(target.strCODef)) { bool ok = Run(binding, target.strID, "pause-receive", null, out string info); results.Add(target.strNameFriendly + ": " + Text.Get(ok ? "Industry.success" : "Industry.rejected", info)); }
        }
        return results.Count == 0 ? Text.Get("Industry.none") : string.Join("\n\n", results);
    }
}
