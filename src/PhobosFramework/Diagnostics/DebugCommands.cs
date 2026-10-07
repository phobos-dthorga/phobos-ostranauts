using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>The gate on test commands (Framework 0.128.0; owner rule, 7 October 2026, every Phobos mod). An F3 test command
/// that changes saved data, or how play goes on if the player carries on, works only after the game's own
/// <c>unlockdebug</c> (the game's <c>CrewSim.bEnableDebugCommands</c>), and even then warns plainly, on every use, that
/// the save then lies outside what the mod was built for and that no later version will put it back; it goes ahead
/// only with <c>confirm</c> at the end of the command. After the change, the mod calls <see cref="Record"/>, which logs
/// it and marks the save as test-changed in one shared record on the player (Framework 0.128.1), so
/// <c>phobosframework status</c> and a bug report show it. Read-only readouts and ordinary player commands are never
/// gated.</summary>
public static class DebugCommands
{
    public enum Decision { Locked, Warn, GoAhead }
    /// <summary>The shared mark on the player: one field per mod, the count of test changes, when the last one was and
    /// what it was.</summary>
    public const string RecordName = "PhobosTests";
    private const int RecordVersion = 1;

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

    /// <summary>Writes the change to the log and marks the save as test-changed (the shared record on the player). A
    /// record this version cannot read is left untouched.</summary>
    public static void Record(string owner, string what)
    {
        if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("A test change needs its owner.", nameof(owner));
        FrameworkLifecycle.Log(Text.Get("Debug.log", owner, what));
        var player = CrewSim.coPlayer;
        if (player == null || player.bDestroyed) return;
        var store = Store(player);
        var status = store.Read(out var fields);
        if (status != SavedStateStatus.Ready && status != SavedStateStatus.Missing) return;
        var copy = new Dictionary<string, string>(fields.ToDictionary(p => p.Key, p => p.Value), StringComparer.Ordinal);
        copy.TryGetValue(owner, out var old);
        copy[owner] = Mark(old, StarSystem.fEpoch, what);
        store.TryWrite(copy);
    }

    /// <summary>The player's test-change marks by mod: how many, and the last one. Empty for an untouched save.</summary>
    public static IReadOnlyList<(string Owner, int Count, string Last)> Changes()
    {
        var player = CrewSim.coPlayer;
        if (player == null || Store(player).Read(out var fields) != SavedStateStatus.Ready) return Array.Empty<(string, int, string)>();
        return fields.Select(p => (p.Key, Count(p.Value), Last(p.Value))).Where(x => x.Item2 > 0).OrderBy(x => x.Key, StringComparer.Ordinal).ToList();
    }

    /// <summary>The status line for <c>phobosframework status</c>, or null for an untouched save.</summary>
    public static string? StatusLine()
    {
        var changes = Changes();
        if (changes.Count == 0) return null;
        return Text.Get("Debug.status", string.Join("; ", changes.Select(c => Text.Get("Debug.status_mod", c.Owner, c.Count, c.Last))));
    }

    private static ObjectStateStore Store(CondOwner player) => new(player.mapGUIPropMaps, RecordName, Text.Owner, RecordVersion);

    /// <summary>The saved mark: "count|epoch|what", pure for tests. A mark this version cannot read restarts its count.</summary>
    public static string Mark(string? old, double epoch, string what) =>
        (Count(old) + 1).ToString(CultureInfo.InvariantCulture) + "|" + epoch.ToString("R", CultureInfo.InvariantCulture) + "|" + Clean(what);

    public static int Count(string? mark) =>
        mark != null && int.TryParse(mark.Split('|')[0], NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 0;

    private static string Last(string mark) { var p = mark.Split('|'); return p.Length >= 3 ? p[2] : ""; }

    private static string Clean(string? text)
    {
        var chars = (text ?? "").Select(c => c == '|' ? '/' : c == ',' ? ';' : c == '=' ? '-' : char.IsControl(c) ? ' ' : c).ToArray();
        string cleaned = new string(chars).Trim();
        return cleaned.Length == 0 ? "-" : cleaned.Length > 200 ? cleaned.Substring(0, 200) : cleaned;
    }
}
