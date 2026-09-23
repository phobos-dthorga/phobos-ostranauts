using System;
using System.Linq;
using HarmonyLib;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class CollectorCommands
{
    internal static bool Run(CollectorCommand command, out string response)
    {
        try
        {
            if (command.Action == CollectorAction.Help) { response = CollectorCommand.Help; return true; }
            if (command.Action == CollectorAction.Invalid) { response = "Use phoboscollector help."; return false; }
            var ports = CollectorService.Find().Where(p => command.PortId == null || p.strID.Equals(command.PortId, StringComparison.OrdinalIgnoreCase)).ToArray();
            if (command.Action == CollectorAction.Status)
            {
                response = "Phobos Residue Collector " + Plugin.Version + "\n" + (ports.Length == 0 ? "No matching installed collector on this ship." :
                    string.Join("\n\n", ports.Select(p => CollectorService.LinkIds(p) + "\n" + Plugin.Collectors.Describe(p)))); return true;
            }
            if (command.Action == CollectorAction.Unlink && command.PortId != null)
            {
                // Also permit clearing an orphaned sender when its old receiver no longer exists.
                var endpoint = DataHandler.mapCOs?.Values.SingleOrDefault(p => p != null && !p.bDestroyed &&
                    p.strID.Equals(command.PortId, StringComparison.OrdinalIgnoreCase) &&
                    (Content.IsMachine(p.strCODef) || CollectorRules.IsFamily(p.strCODef)));
                if (endpoint == null) { response = "No loaded material endpoint with that full ID."; return false; }
                return Plugin.Collectors.Unlink(endpoint, out response);
            }
            if (ports.Length != 1) { response = "Select exactly one installed collector using its ID from phoboscollector status."; return false; }
            var port = ports[0]; bool success;
            switch (command.Action)
            {
                case CollectorAction.Link:
                    var source = ProcessingService.FindMachines().SingleOrDefault(p => p.strID.Equals(command.SourceId, StringComparison.OrdinalIgnoreCase));
                    if (source == null) { response = "Processor not found on this ship. Use phobosshipbreaker status."; return false; }
                    success = Plugin.Collectors.Bind(port, source); break;
                case CollectorAction.Start: success = Plugin.Collectors.Start(port); break;
                case CollectorAction.Pause: success = Plugin.Collectors.Pause(port); break;
                case CollectorAction.Unlink: return Plugin.Collectors.Unlink(port, out response);
                case CollectorAction.Controls:
                    string? problem = CollectorService.AccessProblem(port);
                    if (problem != null) { response = problem; return false; }
                    success = Plugin.CollectorControls.Show(port); break;
                default: response = CollectorCommand.Help; return false;
            }
            response = port.strID + "\n" + Plugin.Collectors.Describe(port); return success;
        }
        catch (Exception ex) { Plugin.Log(ex.ToString()); response = "Collector command failed; inspect the BepInEx log."; return false; }
    }
}

[HarmonyPatch(typeof(Interaction), nameof(Interaction.ApplyEffects))]
internal static class CollectorControlsPatch
{
    private static void Postfix(Interaction __instance, bool isCancelIa)
    {
        if (isCancelIa || __instance.strName != CollectorRules.Controls || __instance.objUs != CrewSim.GetSelectedCrew() ||
            __instance.objThem == null || !CollectorRules.IsFamily(__instance.objThem.strCODef)) return;
        Plugin.CollectorControls.Show(__instance.objThem);
    }
}
