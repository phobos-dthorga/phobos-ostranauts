using System;
using System.Linq;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Construction;

namespace Phobos.Ostranauts.Framework;

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class FrameworkConsole
{
    /// <summary>Further status lines appended to <c>phobosframework status</c> (the data packs); set by the plugin so
    /// the console can be compiled on its own.</summary>
    internal static Func<string>? ExtraStatus { get; set; }
    /// <summary>The add-ons in use and the data files skipped, for <c>phobosframework addons</c> (Framework 0.90.0).</summary>
    internal static Func<string>? AddOns { get; set; }
    /// <summary>The <c>loot [table]</c> report (the loot carve registry), set by the plugin for the same reason.</summary>
    internal static Func<string?, string>? Loot { get; set; }
    /// <summary>The <c>story ...</c> commands (Framework 0.107.0), set by the plugin for the same reason.</summary>
    internal static Func<string[], string>? Story { get; set; }
    /// <summary>The <c>upkeep ...</c> commands (Framework 0.111.0), set by the plugin for the same reason.</summary>
    internal static Func<string[], string>? Upkeep { get; set; }
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
        if (command == "loot" && words.Length <= 3 && Loot != null)
        {
            __result = true;
            strInput += "\n" + Loot(words.Length == 3 ? words[2] : null);
            return false;
        }
        if (command == "crew" && words.Length <= 3)
        {
            __result = true;
            strInput += "\n" + Crew.CrewDiagnostics.Describe(words.Length == 3 ? words[2] : null);
            return false;
        }
        if (command == "upkeep" && Upkeep != null)
        {
            __result = true;
            strInput += "\n" + Upkeep(words);
            return false;
        }
        if (command == "story" && Story != null)
        {
            __result = true;
            strInput += "\n" + Story(words);
            return false;
        }
        if (command == "addons" && words.Length == 2 && AddOns != null)
        {
            __result = true;
            strInput += "\n" + AddOns();
            return false;
        }
        __result = words.Length <= 2 && (command == "help" || command == "status" || command == "recipes");
        string response = Text.Get("FrameworkConsole.phobos_framework", FrameworkInfo.Version);
        if (!__result || command == "help") response += Text.Get("FrameworkConsole.commands_phobosframework_status_recipes_help_read_only");
        else response += ConstructionRegistry.Describe(command == "recipes");
        if (command == "status" && ExtraStatus != null) response += "\n" + ExtraStatus();
        strInput += "\n" + response;
        return false;
    }
}
