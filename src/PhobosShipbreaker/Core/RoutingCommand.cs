using System;
using System.Linq;

namespace PhobosShipbreaker.Core;

public readonly struct RoutingCommand
{
    public string Action { get; }
    public string? ObjectId { get; }
    public string? Argument { get; }
    private RoutingCommand(string action, string? id = null, string? argument = null)
    { Action = action; ObjectId = id; Argument = argument; }
    public static RoutingCommand Parse(string? input)
    {
        var words = (input ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosroute", StringComparison.OrdinalIgnoreCase)) return new RoutingCommand("foreign");
        string action = words.Length > 1 ? words[1].ToLowerInvariant() : "help";
        if (action == "help" && words.Length <= 2) return new RoutingCommand(action);
        if (action == "status" && words.Length <= 3) return new RoutingCommand(action, words.Length == 3 ? words[2] : null);
        if ((action == "start" || action == "pause") && words.Length == 3) return new RoutingCommand(action, words[2]);
        if (words.Length == 4 && new[] { "link", "unlink", "filter", "controls" }.Contains(action))
        {
            string argument = action == "link" ? words[3] : words[3].ToLowerInvariant();
            if ((action == "unlink" || action == "controls") && argument != "send" && argument != "receive") return new RoutingCommand("invalid");
            if (action == "filter" && RoutingRules.FilterIds(argument).Length == 0) return new RoutingCommand("invalid");
            return new RoutingCommand(action, words[2], argument);
        }
        return new RoutingCommand("invalid");
    }
}
