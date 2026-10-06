namespace PhobosManufacturing.Core;

/// <summary>When removal work on a gas or liquid machine (X2, K2, AX-2) is refused at offer time (Manufacturing
/// 0.56.1; owner report, 6 October 2026: Uninstall had vanished from the X2 and K2). These machines refill their hold
/// as soon as it runs low, so "wait until it is empty" never came. Now: a machine whose record cannot be read is left
/// alone; a working machine is paused first; Uninstall is allowed while it merely holds water or gas, and the loose
/// machine carries that mass along (as the water silos do); only Dismantle, which would delete the mass, waits for an
/// empty hold. No game types, so the offline checks run it.</summary>
public static class RemovalRules
{
    public const string Protected = "Maintenance.protected", Running = "Maintenance.running";
    /// <summary>The text key of the refusal, or null when the work may be offered.</summary>
    public static string? Reason(bool recordProtected, bool running, double heldKg, bool dismantle, string holdKey)
    {
        if (recordProtected) return Protected;
        if (running) return Running;
        return dismantle && heldKg > 1e-8 ? holdKey : null;
    }
}
