using System;

namespace PhobosShipbreaker.Core;

public enum CommandAction { Foreign, Invalid, Help, Status, Settings, Start, Pause, Cancel, Dependencies, Feed, Products }

public readonly struct Command
{
    public CommandAction Action { get; }
    public string? TargetId { get; }
    private Command(CommandAction action, string? targetId = null) { Action = action; TargetId = targetId; }

    public static string Help => Text.Get("Commands.phobos_shipbreaker_commands_phobosshipbreaker_help_show_commands");

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
            case "feed": action = CommandAction.Feed; break;
            case "products": action = CommandAction.Products; break;
            default: return new Command(CommandAction.Invalid);
        }
        if (words.Length == 3 && (action == CommandAction.Help || action == CommandAction.Settings || action == CommandAction.Dependencies))
            return new Command(CommandAction.Invalid);
        return new Command(action, words.Length == 3 ? words[2] : null);
    }
}
