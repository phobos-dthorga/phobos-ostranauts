namespace PhobosManufacturing.Core;

/// <summary>When removal work on a Manufacturing machine is refused at offer time. Manufacturing 0.56.1 (owner report,
/// 6 October 2026: Uninstall had vanished from the X2 and K2) let a machine that holds water or gas be uninstalled
/// whole. Manufacturing 0.56.2 (owner rule, same day): choosing Uninstall accepts what stopping mid-reaction brings, so
/// a working machine, or one with a batch under way, is never made to wait either. Its saved work, its hold and any
/// bound charge travel with the loose machine and wait for it to be installed again. Only a record that cannot be read
/// refuses both; Dismantle, which would delete what is inside, still waits for an empty machine. No game types, so the
/// offline checks run it.</summary>
public static class RemovalRules
{
    public const string Protected = "Maintenance.protected";
    /// <summary>The text key of the refusal, or null when the work may be offered. <paramref name="inside"/> is what
    /// dismantling would destroy: kilograms held, or 1 for a batch under way.</summary>
    public static string? Reason(bool recordProtected, double inside, bool dismantle, string insideKey)
    {
        if (recordProtected) return Protected;
        return dismantle && inside > 1e-8 ? insideKey : null;
    }
}
