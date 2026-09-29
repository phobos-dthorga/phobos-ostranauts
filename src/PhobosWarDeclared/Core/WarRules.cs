namespace PhobosWarDeclared.Core;

/// <summary>Identities and defaults for Phobos' War Has Been Declared.</summary>
public static class WarRules
{
    public const string Owner = "phobosgekko.ostranauts.wardeclared";
    public const string Prefix = "PhobosWar";
    public const string BattleStations = Prefix + "BattleStations";
    public const string StandDown = Prefix + "StandDown";
    public const string LayHeld = Prefix + "LayHeld";
    /// <summary>Saved-state record on each ship's own ShipCO.</summary>
    public const string StateName = "PhobosWarDeclared.Ledger";
    public const int StateVersion = 1;

    public const double DefaultQuietMinutes = 5;
    public const double MinimumQuietMinutes = 1;
    public const double MaximumQuietMinutes = 60;
    public const string DefaultSchematic = "safe";
    /// <summary>How often ships are checked for combat facts and due work (real seconds).</summary>
    public const float PollSeconds = 2;
    /// <summary>Real seconds before build sites that could not be laid (an item in hand, the ship not yet editable,
    /// an unexpected failure with attempts left) are tried again. They stay pending; a Lay held order or a fresh
    /// stand-down tries at once.</summary>
    public const double RetrySeconds = 10;
    /// <summary>A damage switch the game queued but never ran is forgotten after this much game time.</summary>
    public const double PendingDamageSeconds = 300;
    /// <summary>Attempts before a part that keeps failing for an unexpected reason is held for the player.</summary>
    public const int MaximumLayAttempts = 3;
    /// <summary>The saved ledger keeps at most this many parts per ship; later losses are still reported.</summary>
    public const int MaximumEntries = 400;
}
