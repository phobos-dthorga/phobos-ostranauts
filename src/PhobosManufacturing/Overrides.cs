using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;

namespace PhobosManufacturing;

/// <summary>Press twice to go ahead (Manufacturing 0.58.0, Framework 0.125.0; owner rule, 6 October 2026). A link, mode or
/// recipe change on a working machine no longer refuses with "Stop the machine first": the first press says the machine
/// will pause for it (and, where the batch must go, what is lost), the second does it and lets the machine carry on
/// through its own start checks. Standing crew orders stay on: the pause here never calls <c>CrewWork.ManualStop</c>.</summary>
internal static class Overrides
{
    [ThreadStatic] private static bool confirmed;
    /// <summary>Whether the command in progress was confirmed with F3's trailing word.</summary>
    internal static bool Confirmed => confirmed;
    /// <summary>Runs one command with its F3 confirmation.</summary>
    internal static T With<T>(bool value, Func<T> call)
    {
        bool old = confirmed; confirmed = value;
        try { return call(); } finally { confirmed = old; }
    }

    /// <summary>Makes a change around a machine that may be working. <paramref name="firstStep"/> is any other step,
    /// already worded (a batch cancelled). Asks first when there is a step or the machine is working; then pauses it,
    /// makes the change (null when it worked, or the refusal), and lets it carry on. The message is
    /// <paramref name="success"/>'s, followed by the reason if the machine could not carry on.</summary>
    internal static bool HoldAround(CondOwner co, bool working, Action pause, Func<string?> resume, Func<string?> change, Func<string> success, out string message, string? firstStep = null)
    {
        string name = ObjectPresentation.Name(co);
        var steps = new List<string>();
        if (!string.IsNullOrEmpty(firstStep)) steps.Add(firstStep!);
        if (working) steps.Add(Text.Get("Content.hold_for_change", name));
        if (steps.Count > 0 && !Confirmations.Ask(string.Join(" ", steps), Confirmed, out message)) return false;
        var held = new PausedChange();
        held.Hold(name, working, pause, resume);
        string? failure; string notes;
        try { failure = change(); }
        finally { notes = held.Release(); }
        message = failure ?? success();
        if (notes.Length > 0) message = message.TrimEnd() + "\n" + notes;
        return failure == null;
    }
}
