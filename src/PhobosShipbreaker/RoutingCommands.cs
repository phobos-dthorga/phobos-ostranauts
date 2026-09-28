using System;
using System.Linq;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class RoutingCommands
{
    internal static bool Run(RoutingCommand command, out string response)
    {
        response = Text.Get("Routing.help");
        try
        {
            if (command.Action == "help") return true;
            if (command.Action == "invalid") return false;
            var all = CollectorService.Sources().Concat(CollectorService.Receivers()).Distinct().ToArray();
            var selected = all.Where(c => command.ObjectId == null || c.strID.Equals(command.ObjectId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (command.Action == "unlink" && selected.Length == 0 && command.ObjectId != null)
            {
                var orphan = CollectorService.Resolve(command.ObjectId);
                if (orphan != null && RoutingRules.IsSender(orphan.strCODef)) selected = new[] { orphan };
            }
            if (command.Action == "status")
            {
                response = selected.Length == 0 ? Text.Get("Routing.none") : string.Join("\n\n", selected.Select(c =>
                    CollectorService.Label(c) + "\n" + (RoutingRules.IsSender(c.strCODef) ? CollectorService.LinkIds(c, true) + "\n" + CollectorService.DescribeLink(c, true) + "\n" : "") +
                    (ProcessingService.IsReclaimer(c) ? Text.Get("Routing.metals_port") + "\n" + CollectorService.LinkIds(c, true, true) + "\n" + CollectorService.DescribeLink(c, true, true) + "\n" : "") +
                    (RoutingRules.IsReceiver(c.strCODef) ? CollectorService.LinkIds(c, false) + "\n" + Plugin.Collectors.Describe(c) : "") +
                    (StorageService.Supported(c) ? "\n" + Plugin.Storage.Describe(c) : "")));
                return true;
            }
            if (selected.Length != 1) { response = Text.Get("Routing.select_one"); return false; }
            var target = selected[0]; bool success;
            if (new[] { "store", "stores", "unstore", "unload", "pause-unload" }.Contains(command.Action) && !StorageService.Supported(target))
            { response = Text.Get("Storage.unsupported"); return false; }
            switch (command.Action)
            {
                case "stores":
                    var stores = StorageService.Candidates(target).ToArray();
                    response = stores.Length == 0 ? Text.Get("Storage.no_candidates") : string.Join("\n", stores.Select(c => c.strNameFriendly + " — " + c.strID));
                    return true;
                case "store": return Plugin.Storage.Link(target, command.Argument, null, out response);
                case "unstore": return Plugin.Storage.Unlink(target, null, out response);
                case "unload": return Plugin.Storage.Start(target, null, out response);
                case "pause-unload": return Plugin.Storage.Pause(target, null, out response);
                case "link":
                    var receiver = all.SingleOrDefault(c => c.strID.Equals(command.Argument, StringComparison.OrdinalIgnoreCase));
                    if (receiver == null || receiver == target || !RoutingRules.CanConnect(target.strCODef, receiver.strCODef))
                    { response = Text.Get("Routing.invalid_source"); return false; }
                    success = Plugin.Collectors.Bind(receiver, target); response = Plugin.Collectors.Describe(receiver); return success;
                case "unlink": return Plugin.Collectors.Unlink(target, out response, command.Argument != "receive", metals: command.Argument == "metals");
                case "filter": return Plugin.Collectors.SetFilter(target, command.Argument!, out response);
                case "controls":
                    success = command.Argument != "receive" ? Plugin.CollectorControls.ShowSource(target, command.Argument == "metals") : Plugin.CollectorControls.Show(target);
                    response = success ? Text.Get("Routing.controls_open") : CollectorService.EndpointAccess(target) ?? Text.Get("Routing.invalid_source"); return success;
                case "start":
                case "pause":
                    if (!RoutingRules.IsReceiver(target.strCODef)) { response = Text.Get("Routing.receiver_required"); return false; }
                    success = command.Action == "start" ? Plugin.Collectors.Start(target) : Plugin.Collectors.Pause(target);
                    response = Plugin.Collectors.Describe(target); return success;
                default: return false;
            }
        }
        catch (Exception ex) { Plugin.Log(ex.ToString()); response = Text.Get("Routing.fault"); return false; }
    }
}
