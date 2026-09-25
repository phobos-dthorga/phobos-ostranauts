using System;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Construction;

namespace Phobos.Ostranauts.Framework;

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class FrameworkConsole
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        var words = (strInput ?? "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !string.Equals(words[0], "phobosframework", StringComparison.OrdinalIgnoreCase)) return true;
        if (words.Length >= 2 && words[1].Equals("perf", StringComparison.OrdinalIgnoreCase))
        {
            __result = Diagnostics.NativePerformance.Command(words, out var performanceResponse);
            strInput += "\n" + performanceResponse;
            return false;
        }
        string command = words.Length == 1 ? "help" : words[1].ToLowerInvariant();
        __result = words.Length <= 2 && (command == "help" || command == "status" || command == "recipes");
        string response = Text.Get("FrameworkConsole.phobos_framework", FrameworkInfo.Version);
        if (!__result || command == "help") response += Text.Get("FrameworkConsole.commands_phobosframework_status_recipes_help_read_only");
        else response += ConstructionRegistry.Describe(command == "recipes");
        strInput += "\n" + response;
        return false;
    }
}
