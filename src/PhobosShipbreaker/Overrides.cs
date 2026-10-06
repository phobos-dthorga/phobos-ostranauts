using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;

namespace PhobosShipbreaker;

/// <summary>Press twice to go ahead (Shipbreaker 0.85.0, Framework 0.125.0; owner rule, 6 October 2026). A link, filter,
/// cooling or capture change that needs work paused or an old link removed no longer refuses for that: the first press
/// says what will be done, the second does it and lets paused work carry on through its own start checks. The F6's
/// heating is never restarted for the player: its resume stays explicit, so a change that needs it off says so and
/// leaves it off. Standing crew orders stay on: no pause here calls <c>CrewWork.ManualStop</c>.</summary>
internal static class Overrides
{
    [ThreadStatic] private static bool confirmed;
    /// <summary>Whether the command in progress was confirmed with F3's trailing word.</summary>
    internal static bool Confirmed => confirmed;
    /// <summary>Marks the commands run inside it as confirmed (or not) until disposed.</summary>
    internal static IDisposable Scope(bool value) => new Restore(value);
    private sealed class Restore : IDisposable
    {
        private readonly bool old;
        internal Restore(bool value) { old = confirmed; confirmed = value; }
        public void Dispose() => confirmed = old;
    }
    /// <summary>Runs one command with its F3 confirmation.</summary>
    internal static T With<T>(bool value, Func<T> call) { using (Scope(value)) return call(); }

    /// <summary>Makes a change around equipment that may be working. <paramref name="firstStep"/> is any other step, already
    /// worded. Asks first when there is a step or the equipment is working; then pauses it, makes the change (null when it
    /// worked, or the refusal) and lets it carry on. The message is <paramref name="success"/>'s, followed by the reason
    /// if it could not carry on.</summary>
    internal static bool HoldAround(CondOwner co, bool working, Action pause, Func<string?> resume, Func<string?> change, Func<string> success, out string message, string? firstStep = null)
    {
        string name = ObjectPresentation.Name(co);
        var steps = new List<string>();
        if (!string.IsNullOrEmpty(firstStep)) steps.Add(firstStep!);
        if (working) steps.Add(Text.Get("Overrides.hold", name));
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
