using HarmonyLib;
using PhobosApproachAssist.Core;

namespace PhobosApproachAssist;

// The player's F3 console uses ConsoleResolver, not the separate DevConsole UI.
[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsoleCommands
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var action = DebugCommands.Parse(strInput);
        if (action == DebugAction.Foreign) return true;
        __result = Plugin.Service.RunDebugCommand(action, out string response);
        strInput += "\n" + response;
        return false;
    }
}
