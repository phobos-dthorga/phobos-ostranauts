using HarmonyLib;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

// Same F3 entry point as Approach Assist; foreign commands pass through unchanged.
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsoleCommands
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var command = Command.Parse(strInput);
        if (command.Action == CommandAction.Foreign) return true;
        __result = Plugin.Service.RunCommand(command, out string response);
        strInput += "\n" + response;
        return false;
    }
}
