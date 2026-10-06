using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Controls;

/// <summary>Framework 0.125.0: press twice to go ahead, and machines held paused around a change.</summary>
internal static class ConfirmationChecks
{
    internal static void Run(Action<bool, string> check)
    {
        string action = "confirm:link-water:abc";
        check(Confirmations.Split(ref action) && action == "link-water:abc", "The confirm prefix is stripped and reported");
        check(!Confirmations.Split(ref action) && action == "link-water:abc", "A plain action is left alone");
        check(Confirmations.Confirmed("pause") == "confirm:pause", "The confirmed form carries the prefix");
        var args = Confirmations.TakeWord(new[] { "link-water", "abc", "CONFIRM" }, out bool worded);
        check(worded && args.Length == 2 && args[1] == "abc", "A trailing confirm word confirms a text command");
        args = Confirmations.TakeWord(new[] { "link-water", "confirm-me" }, out worded);
        check(!worded && args.Length == 2, "Only the whole word confirms");

        check(Confirmations.Decide("Pause both.", "Pause both.", false), "A second press on the same warning goes ahead");
        check(!Confirmations.Decide("Pause both.", "Pause all three.", true), "A second press whose warning changed does not");
        check(Confirmations.Decide(null, "Pause both.", true) && !Confirmations.Decide(null, "Pause both.", false), "A text command goes ahead only when confirmed");

        // A service inside panel presses.
        string message = "";
        bool Service(string warning) => Confirmations.Ask(warning, false, out message);
        bool done = Confirmations.Press(null, () => Service("Pauses the W2 first."), out var offer);
        check(!done && offer == "Pauses the W2 first." && message.StartsWith("Pauses the W2 first. ", StringComparison.Ordinal), "A first press offers and says what will be done");
        done = Confirmations.Press(offer, () => Service("Pauses the W2 first."), out offer);
        check(done && offer == null && message.Length == 0, "The second press goes ahead");
        done = Confirmations.Press("Pauses the W2 first.", () => Service("Pauses two W2s first."), out offer);
        check(!done && offer == "Pauses two W2s first.", "A changed situation offers again instead of acting");
        done = Confirmations.Press(null, () => false, out offer);
        check(!done && offer == null, "A plain refusal offers nothing");
        // Outside a press: a text command.
        check(!Confirmations.Ask("Stops the flight.", false, out message) && message.StartsWith("Stops the flight. ", StringComparison.Ordinal), "An unconfirmed text command is told how to go ahead");
        check(Confirmations.Ask("Stops the flight.", true, out message) && message.Length == 0, "A confirmed text command goes ahead");
        bool refused = false; try { Confirmations.Ask(" ", true, out _); } catch (ArgumentException) { refused = true; }
        check(refused, "An offer must say what it will do");

        var guard = new PressGuard();
        check(!guard.Press("link", () => Service("Unlinks the old W2 first.")) && guard.Armed("link") && guard.Seen("link") == "Unlinks the old W2 first.", "A guard arms on an offer");
        check(guard.Label("link", "Apply") != "Apply" && guard.Label("other", "Apply") == "Apply", "Only the armed button is relabelled");
        check(!guard.Press("other", () => false) && !guard.Armed("link"), "Any other press disarms it");
        guard.Press("link", () => Service("Unlinks the old W2 first."));
        check(guard.Press("link", () => Service("Unlinks the old W2 first.")) && !guard.Armed("link"), "The second press acts and disarms");

        var order = new List<string>();
        var change = new PausedChange();
        bool rackRunning = true, oldW2Running = true, newW2Running = false;
        change.Hold("rack", rackRunning, () => { rackRunning = false; order.Add("pause rack"); }, () => { rackRunning = true; order.Add("resume rack"); return null; });
        change.Hold("old W2", oldW2Running, () => { oldW2Running = false; order.Add("pause old"); }, () => "it is damaged");
        change.Hold("new W2", newW2Running, () => order.Add("pause new"), () => { order.Add("resume new"); return null; });
        change.Hold("broken", true, () => order.Add("pause broken"), () => throw new InvalidOperationException("boom"));
        check(change.Count == 3 && !rackRunning && !oldW2Running && !order.Contains("pause new"), "Only working machines are paused and held");
        string notes = change.Release();
        check(order[order.Count - 1] == "resume rack" && rackRunning && !order.Contains("resume new"), "Held machines resume last first; idle ones are left alone");
        check(notes.Contains("old W2") && notes.Contains("it is damaged") && notes.Contains("broken"), "A machine that cannot carry on is named with its reason, and a failure is reported");
        check(change.Count == 0 && change.Release().Length == 0, "Release empties the hold");
    }
}
