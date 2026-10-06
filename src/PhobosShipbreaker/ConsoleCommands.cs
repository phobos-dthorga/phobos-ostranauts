using HarmonyLib;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Same F3 entry point as Approach Assist; foreign commands pass through unchanged.
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsoleCommands
{
    /// <summary>A trailing confirm word goes ahead with the steps a refusal offered (Shipbreaker 0.85.0). It is stripped
    /// only for Shipbreaker's own commands; a foreign command passes through exactly as typed.</summary>
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var words = strInput.Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        bool confirmed = words.Length > 1 && words[words.Length - 1].Equals(Phobos.Ostranauts.Framework.Controls.Confirmations.Word, System.StringComparison.OrdinalIgnoreCase);
        if (!confirmed) return Dispatch(ref strInput, ref __result);
        string trimmed = string.Join(" ", words, 0, words.Length - 1);
        using (Overrides.Scope(true))
        {
            if (Dispatch(ref trimmed, ref __result)) return true;
            strInput = trimmed; return false;
        }
    }
    private static bool Dispatch(ref string strInput, ref bool __result)
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
