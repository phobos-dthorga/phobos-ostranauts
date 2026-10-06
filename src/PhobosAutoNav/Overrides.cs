using System;

namespace PhobosAutoNav;

/// <summary>Press twice to go ahead (Auto Nav 0.35.0, Framework 0.125.0; owner rule, 6 October 2026). A new flight,
/// dock or departure no longer refuses because another flight holds the console: the first press says that flight will be
/// stopped (as Stop does, so the ship coasts with thrust cut), the second stops it and starts the new one. The new
/// target is checked first, so a press never stops a flight for nothing. This file holds only F3's confirmation; the
/// steps live in the navigation service.</summary>
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
}
