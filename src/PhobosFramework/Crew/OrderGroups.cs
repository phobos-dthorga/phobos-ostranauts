using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>How the Orders list groups equipment (Framework 0.117.0; owner direction, 6 October 2026: groups by state
/// with the ones that need the player open and the rest folded). No game types, so the offline checks run it.</summary>
public static class OrderGroups
{
    /// <summary>Orders waiting on the player: a stop (manual, fault, reload, changed settings, completed, a skip) or a
    /// record that cannot be read. Resume or a look at the machine is the next step.</summary>
    public const string NeedsYou = "needs_you";
    /// <summary>Orders running, or waiting for feed, space or a free crew member: the crew's business.</summary>
    public const string Working = "working";
    /// <summary>Orders never switched on or never given work: most of a big ship, so folded.</summary>
    public const string Off = "off";
    public static readonly IReadOnlyList<string> Order = new[] { NeedsYou, Working, Off };
    public static readonly IReadOnlyList<string> DefaultFolded = new[] { Working, Off };

    public static string Group(OrderState state) => state switch
    {
        OrderState.Blocked or OrderState.Stopped => NeedsYou,
        OrderState.Running or OrderState.Waiting => Working,
        _ => Off
    };
    /// <summary>A list row: the name and state, then the work and the one thing it waits for (or who is on it).</summary>
    public static string RowText(string name, OrderStatus status, string unassigned)
    {
        if (status == null) throw new ArgumentNullException(nameof(status));
        string second = status.Detail.Length > 0 ? status.Detail : status.Worker == unassigned ? "" : status.Worker;
        string line = status.Work + (second.Length > 0 ? " · " + second : "");
        return name + " — " + status.Label + (line.Length > 0 ? "\n" + line : "");
    }
    /// <summary>How a machine without crew orders is fed and emptied: by hand, plus any feed or product store it has.</summary>
    public static string LoadingText(string byHand, string feedStore, string productStore)
    {
        var lines = new List<string> { byHand ?? "" };
        if (!string.IsNullOrEmpty(feedStore)) lines.Add(feedStore);
        if (!string.IsNullOrEmpty(productStore)) lines.Add(productStore);
        return string.Join("\n", lines);
    }
}
