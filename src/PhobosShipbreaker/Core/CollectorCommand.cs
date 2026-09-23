using System;

namespace PhobosShipbreaker.Core;

public enum CollectorAction { Foreign, Invalid, Help, Status, Link, Unlink, Start, Pause, Controls }
public readonly struct CollectorCommand
{
    public CollectorAction Action { get; }
    public string? PortId { get; }
    public string? SourceId { get; }
    private CollectorCommand(CollectorAction action, string? port = null, string? source = null)
    { Action = action; PortId = port; SourceId = source; }
    public const string Help = "phoboscollector status [collector-ID]\nphoboscollector controls [collector-ID]\n"
        + "phoboscollector link <collector-ID> <processor-ID>\nphoboscollector unlink [endpoint-ID]\nphoboscollector start [collector-ID]\nphoboscollector pause [collector-ID]\n"
        + "Use full IDs from phoboscollector status or phobosshipbreaker status. Link/unlink beside either endpoint; Start beside the collector.\n"
        + "Unlink accepts a collector or processor ID. Without an ID, commands require one collector. Links survive reload; press Start to resume.\n"
        + "Test-save setup: spawn PhobosResidueCollectorLoose; install over two exterior walls, pocket outward.";
    public static CollectorCommand Parse(string? input)
    {
        var w = (input ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (w.Length == 0 || !string.Equals(w[0], "phoboscollector", StringComparison.OrdinalIgnoreCase)) return new CollectorCommand(CollectorAction.Foreign);
        if (w.Length == 1 || w.Length == 2 && w[1].Equals("help", StringComparison.OrdinalIgnoreCase)) return new CollectorCommand(CollectorAction.Help);
        if (w.Length < 2 || w.Length > 4) return new CollectorCommand(CollectorAction.Invalid);
        if (w[1].Equals("link", StringComparison.OrdinalIgnoreCase)) return w.Length == 4 ? new CollectorCommand(CollectorAction.Link, w[2], w[3]) : new CollectorCommand(CollectorAction.Invalid);
        if (w.Length > 3) return new CollectorCommand(CollectorAction.Invalid);
        CollectorAction action;
        switch (w[1].ToLowerInvariant())
        {
            case "status": action = CollectorAction.Status; break;
            case "start": action = CollectorAction.Start; break;
            case "unlink": action = CollectorAction.Unlink; break;
            case "pause": action = CollectorAction.Pause; break;
            case "controls": action = CollectorAction.Controls; break;
            default: action = CollectorAction.Invalid; break;
        }
        return new CollectorCommand(action, w.Length == 3 ? w[2] : null);
    }
}
