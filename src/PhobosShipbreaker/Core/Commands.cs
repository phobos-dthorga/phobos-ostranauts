using System;

namespace PhobosShipbreaker.Core;

public enum CommandAction { Foreign, Invalid, Help, Status, Settings, Start, Pause, Cancel, Dependencies }

public readonly struct Command
{
    public CommandAction Action { get; }
    public string? TargetId { get; }
    private Command(CommandAction action, string? targetId = null) { Action = action; TargetId = targetId; }

    public const string Help = "Phobos Shipbreaker commands:\n"
        + "phobosshipbreaker help - show commands\n"
        + "phobosshipbreaker status [fixture-ID] - show fixtures, IDs and processing state\n"
        + "phobosshipbreaker settings - show active settings and the config filename\n"
        + "phobosshipbreaker dependencies - show loaded versions, template checks and recipe registration\n"
        + "phobosshipbreaker start [fixture-ID] - start or resume processing\n"
        + "phobosshipbreaker pause [fixture-ID] - pause, keeping progress\n"
        + "phobosshipbreaker cancel [fixture-ID] - clear queued work; keep panels, no energy refund\n"
        + "Omit the ID only when there is one fixture on the selected crew member's ship. Stand beside it for controls.\n"
        + "Test-save setup uses native commands: spawn PhobosShipbreakerLoose / spawn ItmWall1x1Loose. Install normally.";

    public static Command Parse(string? input)
    {
        var words = (input ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !string.Equals(words[0], "phobosshipbreaker", StringComparison.OrdinalIgnoreCase))
            return new Command(CommandAction.Foreign);
        if (words.Length == 1) return new Command(CommandAction.Help);
        if (words.Length > 3) return new Command(CommandAction.Invalid);
        CommandAction action;
        switch (words[1].ToLowerInvariant())
        {
            case "help": action = CommandAction.Help; break;
            case "status": action = CommandAction.Status; break;
            case "settings": action = CommandAction.Settings; break;
            case "dependencies": action = CommandAction.Dependencies; break;
            case "start": action = CommandAction.Start; break;
            case "pause": action = CommandAction.Pause; break;
            case "cancel": action = CommandAction.Cancel; break;
            default: return new Command(CommandAction.Invalid);
        }
        if (words.Length == 3 && (action == CommandAction.Help || action == CommandAction.Settings || action == CommandAction.Dependencies))
            return new Command(CommandAction.Invalid);
        return new Command(action, words.Length == 3 ? words[2] : null);
    }
}
