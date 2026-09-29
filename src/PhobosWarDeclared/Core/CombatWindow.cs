using System;

namespace PhobosWarDeclared.Core;

public enum WindowChange { None, Opened, Closed }

/// <summary>One ship's battle-stations state. The game has no combat mode, so the window opens on native
/// combat facts (an enemy engaged with the ship, the ship's own weapons target, recent damage) or on the
/// player's Battle stations order, and closes after a quiet period or on Stand down. A manual order keeps
/// the window open until the player stands down. After Stand down, a fight still listed by the game does
/// not reopen the window until that listing clears; fresh damage does.</summary>
public sealed class CombatWindow
{
    public bool Open { get; private set; }
    public bool Manual { get; private set; }
    public double OpenedAt { get; private set; }
    public double LastActivity { get; private set; }
    public double? StoodDownAt { get; private set; }
    public bool SuppressOngoing { get; private set; }

    /// <param name="now">Game time in seconds.</param>
    /// <param name="ongoing">The game currently lists this ship as engaged or targeting.</param>
    /// <param name="lastDamage">Game time of the last damage event this session, if any.</param>
    /// <param name="quietSeconds">Game seconds without activity before an automatic window closes.</param>
    public WindowChange Observe(double now, bool ongoing, double? lastDamage, double quietSeconds)
    {
        if (quietSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(quietSeconds));
        if (SuppressOngoing && !ongoing) SuppressOngoing = false;
        bool activeOngoing = ongoing && !SuppressOngoing;
        bool activeDamage = lastDamage.HasValue && lastDamage.Value <= now && now - lastDamage.Value < quietSeconds &&
            (!StoodDownAt.HasValue || lastDamage.Value > StoodDownAt.Value);
        if (activeDamage) LastActivity = Math.Max(LastActivity, lastDamage!.Value);
        if (activeOngoing) LastActivity = Math.Max(LastActivity, now);
        if (!Open && (activeDamage || activeOngoing))
        {
            Open = true; OpenedAt = now;
            return WindowChange.Opened;
        }
        if (Open && !Manual && now - LastActivity >= quietSeconds)
        {
            Open = false; StoodDownAt = now;
            return WindowChange.Closed;
        }
        return WindowChange.None;
    }

    public WindowChange Declare(double now)
    {
        Manual = true; SuppressOngoing = false;
        LastActivity = Math.Max(LastActivity, now);
        if (Open) return WindowChange.None;
        Open = true; OpenedAt = now;
        return WindowChange.Opened;
    }

    public WindowChange Stand(double now)
    {
        if (!Open) return WindowChange.None;
        Open = false; Manual = false; StoodDownAt = now; SuppressOngoing = true;
        return WindowChange.Closed;
    }

    internal void Restore(bool open, bool manual, double openedAt, double lastActivity, double? stoodDownAt, bool suppressOngoing)
    {
        Open = open; Manual = open && manual; OpenedAt = openedAt; LastActivity = lastActivity;
        StoodDownAt = stoodDownAt; SuppressOngoing = suppressOngoing;
    }
}
