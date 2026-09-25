using HarmonyLib;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Same F3 entry point as Approach Assist; foreign commands pass through unchanged.
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsoleCommands
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        if (FurnaceService.F3(strInput, out bool furnaceResult, out string furnaceResponse))
        { __result = furnaceResult; strInput += "\n" + furnaceResponse; return false; }
        if (IndustryCommands.Handle(strInput, out bool industryResult, out string industryResponse))
        { __result = industryResult; strInput += "\n" + industryResponse; return false; }
        var route = RoutingCommand.Parse(strInput);
        if (route.Action != "foreign")
        {
            __result = RoutingCommands.Run(route, out string routeResponse);
            strInput += "\n" + routeResponse; return false;
        }
        if (ReclaimerPanel.Command(strInput, out bool reclaimerResult, out string reclaimerResponse))
        { __result = reclaimerResult; strInput += "\n" + reclaimerResponse; return false; }
        var collector = CollectorCommand.Parse(strInput);
        if (collector.Action != CollectorAction.Foreign)
        {
            __result = CollectorCommands.Run(collector, out string collectorResponse);
            strInput += "\n" + collectorResponse; return false;
        }
        var command = Command.Parse(strInput);
        if (command.Action == CommandAction.Foreign) return true;
        __result = Plugin.Service.RunCommand(command, out string response);
        strInput += "\n" + response;
        return false;
    }
}
