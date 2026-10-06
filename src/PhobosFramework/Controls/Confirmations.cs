using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Press twice to go ahead (Framework 0.125.0; owner rule, 6 October 2026). A change or button that needs other
/// steps first (pausing a machine or its intake, removing an old link, applying a draft, stopping work, cancelling a
/// batch) is never refused for that reason. The first press says what will be done; a second press on the same choice
/// does it. One channel serves every host, so a service decides once:
/// <list type="bullet">
/// <item>A service works out the steps, then calls <see cref="Ask"/> with a warning naming them. Ask returns true when
/// the player has already confirmed; otherwise it records an offer and gives the message to refuse with.</item>
/// <item>A panel runs each press through <see cref="Press"/> (or a <see cref="PressGuard"/>), passing the warning the
/// player saw on the first press. A second press whose warning has changed is a new offer, never a blind go-ahead.</item>
/// <item>F3 and other text commands confirm with a trailing word (<see cref="TakeWord"/>), or an action carrying the
/// <see cref="Prefix"/>, which the service strips with <see cref="Split"/>.</item>
/// </list>
/// Conditions the service cannot clear itself (heat, contents, faults, damage, a full link bank) are still refused, with
/// a reason; they never offer. Nothing here is saved.</summary>
public static class Confirmations
{
    /// <summary>The prefix of an action already confirmed (F3, crew paths).</summary>
    public const string Prefix = "confirm:";
    /// <summary>The word that confirms a text command when it ends the line.</summary>
    public const string Word = "confirm";

    /// <summary>Strips <see cref="Prefix"/> from an action; true when it was there.</summary>
    public static bool Split(ref string action)
    {
        if (action == null || !action.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        action = action.Substring(Prefix.Length); return true;
    }
    /// <summary>The confirmed form of an action.</summary>
    public static string Confirmed(string action) => Prefix + action;
    /// <summary>Removes a trailing <see cref="Word"/> from a text command's arguments (case-insensitive); true when it was there.</summary>
    public static string[] TakeWord(IReadOnlyList<string> args, out bool confirmed)
    {
        confirmed = args.Count > 0 && string.Equals(args[args.Count - 1], Word, StringComparison.OrdinalIgnoreCase);
        return (confirmed ? args.Take(args.Count - 1) : args).ToArray();
    }

    // The press in progress, set by a panel around one service call.
    private static string? seen, offered;
    private static bool panel;

    /// <summary>Runs one panel press. <paramref name="seenWarning"/> is the warning the player saw on the first press of
    /// this same choice, or null on a first press. <paramref name="offer"/> is the warning when the service offered to
    /// do the steps on a second press, or null. Returns the call's own result.</summary>
    public static bool Press(string? seenWarning, Func<bool> call, out string? offer)
    {
        string? oldSeen = seen, oldOffered = offered; bool oldPanel = panel;
        seen = seenWarning; offered = null; panel = true;
        try { bool done = call(); offer = done ? null : offered; return done; }
        finally { seen = oldSeen; offered = oldOffered; panel = oldPanel; }
    }

    /// <summary>For a service: whether to go ahead with the steps <paramref name="warning"/> names. True when the player
    /// confirmed: a panel's second press on the same warning, or a text command confirmed with the word or prefix.
    /// Otherwise records the offer and sets <paramref name="message"/>: the warning and how to go ahead.</summary>
    public static bool Ask(string warning, bool confirmedByCommand, out string message)
    {
        if (string.IsNullOrWhiteSpace(warning)) throw new ArgumentException("A confirmation needs a warning that says what will be done.");
        if (Decide(seen, warning, confirmedByCommand)) { message = ""; return true; }
        offered = warning;
        message = warning.TrimEnd() + " " + ConsoleText.Get(panel ? "confirm_again" : "confirm_word");
        return false;
    }

    /// <summary>The rule behind <see cref="Ask"/>, pure for tests: a panel press goes ahead only on the warning the player
    /// saw; a text command goes ahead when it was confirmed.</summary>
    public static bool Decide(string? seenWarning, string warning, bool confirmedByCommand) =>
        seenWarning != null ? string.Equals(seenWarning, warning, StringComparison.Ordinal) : confirmedByCommand;

    /// <summary>For a knob or guarded switch, which cannot take a second press: runs the press, and on an offer shows a
    /// card with the warning, a Go ahead button that repeats it confirmed, and Not now. <paramref name="done"/> gets
    /// the final result and the service's message.</summary>
    public static void PressWithCard(Transform host, Func<(bool Done, string Message)> call, Action<bool, string> done)
    {
        (bool Done, string Message) result = default;
        bool ok = Press(null, () => { result = call(); return result.Done; }, out var offer);
        if (ok || offer == null) { done(ok, result.Message ?? ""); return; }
        string warning = offer;
        ChoiceCard.Show(host, warning, new[]
        {
            new ChoiceCard.Choice(ConsoleText.Get("go_ahead"), () =>
            {
                (bool Done, string Message) second = default;
                bool confirmed = Press(warning, () => { second = call(); return second.Done; }, out _);
                done(confirmed, second.Message ?? "");
            }, Tone.Attention),
            new ChoiceCard.Choice(ConsoleText.Get("not_now"), () => done(false, ConsoleText.Get("not_changed")))
        });
    }
}

/// <summary>The second-press state of one panel's buttons (Framework 0.125.0): which press waits for its second, and the
/// warning it showed. Presentation only. Any other press, a changed selection or closing the panel disarms it.</summary>
public sealed class PressGuard
{
    public string Key { get; private set; } = "";
    public string Warning { get; private set; } = "";
    public bool Armed(string key) => Key.Length > 0 && string.Equals(Key, key, StringComparison.Ordinal);
    /// <summary>The warning the player saw for this press, or null when it is a first press.</summary>
    public string? Seen(string key) => Armed(key) ? Warning : null;
    /// <summary>Runs a press of <paramref name="key"/> through <see cref="Confirmations.Press"/>; arms on an offer, otherwise disarms.</summary>
    public bool Press(string key, Func<bool> call)
    {
        bool done = Confirmations.Press(Seen(key), call, out var offer);
        if (offer != null) { Key = key; Warning = offer; } else Disarm();
        return done;
    }
    public void Disarm() { Key = ""; Warning = ""; }
    /// <summary>A button's label: "label again" while its press waits for the second.</summary>
    public string Label(string key, string label) => Armed(key) ? ConsoleText.Get("press_again", label) : label;
}

/// <summary>Holds machines paused while a change is made, then lets those that were working carry on (Framework 0.125.0).
/// The pause a service hands in must leave standing crew orders alone (never <c>CrewWork.ManualStop</c>): the player
/// asked for a change, not for the work to end. Resuming goes through the machine's own start checks; one that cannot
/// start stays stopped and the reason is returned.</summary>
public sealed class PausedChange
{
    private readonly List<(string Name, Func<string?> Resume)> held = new();
    /// <summary>How many machines are held.</summary>
    public int Count => held.Count;
    /// <summary>Pauses a machine that is working and remembers to resume it; does nothing for one that is not.</summary>
    public void Hold(string name, bool working, Action pause, Func<string?> resume)
    {
        if (!working) return;
        pause(); held.Add((name, resume));
    }
    /// <summary>Resumes everything held, last first. Returns what could not carry on, one sentence per machine, or "".</summary>
    public string Release()
    {
        var notes = new List<string>();
        for (int i = held.Count - 1; i >= 0; i--)
        {
            string? why;
            try { why = held[i].Resume(); }
            catch (Exception e) { why = ConsoleText.Get("resume_fault"); FrameworkLifecycle.Log(e.ToString()); }
            if (!string.IsNullOrWhiteSpace(why)) notes.Add(ConsoleText.Get("held_not_resumed", held[i].Name, why!.Trim()));
        }
        held.Clear();
        return string.Join(" ", notes);
    }
}
