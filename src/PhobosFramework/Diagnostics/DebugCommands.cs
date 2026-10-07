using System;
using Phobos.Ostranauts.Framework.Controls;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>The gate on test commands (Framework 0.128.0; owner rule, 7 October 2026, every Phobos mod). An F3 test command
/// that changes saved data, or how play goes on if the player carries on, works only after the game's own
/// <c>unlockdebug</c> (the game's <c>CrewSim.bEnableDebugCommands</c>), and even then warns plainly, on every use, that
/// the save then lies outside what the mod was built for and that no later version will put it back; it goes ahead
/// only with <c>confirm</c> at the end of the command. The mod then marks the save as test-changed in its own record and
/// calls <see cref="Record"/> for the log. Read-only readouts and ordinary player commands are never gated.</summary>
public static class DebugCommands
{
    public enum Decision { Locked, Warn, GoAhead }

    /// <summary>Whether the game's debug commands are unlocked this session (the game never saves it).</summary>
    public static bool Unlocked => CrewSim.bEnableDebugCommands;

    /// <summary>The rule, pure for tests: locked refuses, unlocked warns, unlocked and confirmed goes ahead.</summary>
    public static Decision Decide(bool unlocked, bool confirmed) => !unlocked ? Decision.Locked : confirmed ? Decision.GoAhead : Decision.Warn;

    /// <summary>For a test command: true when it may go ahead. Otherwise <paramref name="message"/> says why (the game's
    /// debug commands are locked) or gives the warning and how to confirm. <paramref name="mod"/> is the mod's name and
    /// <paramref name="what"/> a short description of what the command will do, both in the player's language.</summary>
    public static bool Gate(string mod, string what, bool confirmed, out string message)
    {
        switch (Decide(Unlocked, confirmed))
        {
            case Decision.Locked: message = Text.Get("Debug.locked"); return false;
            case Decision.Warn: message = Text.Get("Debug.warning", what, mod) + " " + ConsoleText.Get("confirm_word"); return false;
            default: message = ""; return true;
        }
    }

    /// <summary>What to tell the player after a test command went ahead.</summary>
    public static string Done(string what) => Text.Get("Debug.done", what);

    /// <summary>Writes the change to the log, so a bug report shows the save was test-changed.</summary>
    public static void Record(string owner, string what)
    {
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("A test change needs its owner.", nameof(owner));
        FrameworkLifecycle.Log(Text.Get("Debug.log", owner, what));
    }
}
