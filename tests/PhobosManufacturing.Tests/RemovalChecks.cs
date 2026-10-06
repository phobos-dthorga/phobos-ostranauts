using System;
using PhobosManufacturing.Core;

/// <summary>Manufacturing 0.56.1 (owner report, 6 October 2026: Uninstall had vanished from the X2 and K2): removal of a
/// gas or liquid machine is refused only for a reason the player can act on. Uninstall carries a hold; Dismantle waits
/// for an empty one.</summary>
internal static class RemovalChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const string hold = "Maintenance.cycle";
        check(RemovalRules.Reason(false, false, 1.125, false, hold) == null, "An idle machine holding water can be uninstalled: the water goes with it");
        check(RemovalRules.Reason(false, false, 1.125, true, hold) == hold, "A machine holding water cannot be dismantled: that would delete the water");
        check(RemovalRules.Reason(false, false, 0, true, hold) == null && RemovalRules.Reason(false, false, 0, false, hold) == null, "An empty idle machine can be uninstalled or dismantled");
        check(RemovalRules.Reason(false, true, 0, false, hold) == RemovalRules.Running && RemovalRules.Reason(false, true, 1, true, hold) == RemovalRules.Running,
            "A working machine is paused first, whatever it holds");
        check(RemovalRules.Reason(true, false, 0, false, hold) == RemovalRules.Protected && RemovalRules.Reason(true, true, 1, true, hold) == RemovalRules.Protected,
            "A record that cannot be read is left alone, before any other reason");
        check(RemovalRules.Reason(false, false, 1e-9, true, hold) == null, "A trace below the accounting tolerance does not block dismantling");
    }
}
