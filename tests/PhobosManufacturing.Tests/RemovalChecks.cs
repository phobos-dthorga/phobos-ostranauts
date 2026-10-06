using System;
using PhobosManufacturing.Core;

/// <summary>Manufacturing 0.56.1 and 0.56.2 (owner report and rule, 6 October 2026): choosing Uninstall accepts what
/// stopping mid-reaction brings. A machine holding water or gas, working, or with a batch under way can always be
/// uninstalled, its work travelling with it; Dismantle waits for an empty machine; an unreadable record blocks both.</summary>
internal static class RemovalChecks
{
    internal static void Run(Action<bool, string> check)
    {
        const string hold = "Maintenance.cycle", batch = "Maintenance.charge";
        check(RemovalRules.Reason(false, 1.125, false, hold) == null, "A machine holding water can be uninstalled: the water goes with it");
        check(RemovalRules.Reason(false, 1.125, true, hold) == hold, "A machine holding water cannot be dismantled: that would delete the water");
        check(RemovalRules.Reason(false, 1, false, batch) == null && RemovalRules.Reason(false, 1, true, batch) == batch,
            "A batch under way travels with an uninstall, and blocks only Dismantle");
        check(RemovalRules.Reason(false, 0, true, hold) == null && RemovalRules.Reason(false, 0, false, hold) == null, "An empty, idle machine can be uninstalled or dismantled");
        check(RemovalRules.Reason(true, 0, false, hold) == RemovalRules.Protected && RemovalRules.Reason(true, 1, true, hold) == RemovalRules.Protected,
            "A record that cannot be read is left alone, before any other reason");
        check(RemovalRules.Reason(false, 1e-9, true, hold) == null, "A trace below the accounting tolerance does not block dismantling");
    }
}
