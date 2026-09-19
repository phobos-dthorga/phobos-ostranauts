using System;

namespace PhobosApproachAssist.Core;

public enum DebugAction { Foreign, Invalid, Help, Status, Spawn, SpawnDamaged, Pulse, Stop }

public static class DebugCommands
{
    public const string Keyword = "phobosapproach";
    public const string Help = "Phobos Approach Assist commands:\n"
        + "phobosapproach help - show this list\n"
        + "phobosapproach status - report setup and first pulse blocker\n"
        + "phobosapproach spawn [damaged] - add one module to the open nav console\n"
        + "phobosapproach pulse - run the capped two-second test; no braking\n"
        + "phobosapproach stop - release Approach Assist thrust (does not stop the ship)\n"
        + "Spawn and pulse require a separate PhobosApproachAssistTest save. F8 also opens test tools.";

    public static DebugAction Parse(string? input)
    {
        var words = (input ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !string.Equals(words[0], Keyword, StringComparison.OrdinalIgnoreCase))
            return DebugAction.Foreign;
        if (words.Length == 1) return DebugAction.Help;
        if (words.Length == 3 && string.Equals(words[1], "spawn", StringComparison.OrdinalIgnoreCase)
            && string.Equals(words[2], "damaged", StringComparison.OrdinalIgnoreCase)) return DebugAction.SpawnDamaged;
        if (words.Length != 2) return DebugAction.Invalid;
        switch (words[1].ToLowerInvariant())
        {
            case "help": return DebugAction.Help;
            case "status": return DebugAction.Status;
            case "spawn": return DebugAction.Spawn;
            case "pulse": return DebugAction.Pulse;
            case "stop": return DebugAction.Stop;
            default: return DebugAction.Invalid;
        }
    }
}
