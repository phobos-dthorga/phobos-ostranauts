using System;
using PhobosAutoNav.Core;

internal static class SensorRuleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        // Native stellar-object rule: fixed threshold, partial beyond 1,000 km.
        check(ContactRules.EvaluateStellar(ContactRules.DefaultThreshold, 999).State == ContactState.Ready, "A signal at the native threshold inside 1,000 km is a live track");
        check(ContactRules.EvaluateStellar(.29, 10).State == ContactState.Weak, "Asteroid markers need the fixed native threshold");
        check(ContactRules.EvaluateStellar(50, 1000.001).State == ContactState.Weak, "Beyond 1,000 km an asteroid is only a partial contact");
        check(ContactRules.EvaluateStellar(double.NaN, 10).State == ContactState.Fault && ContactRules.EvaluateStellar(1, double.NaN).State == ContactState.Fault &&
            ContactRules.EvaluateStellar(1, -1).State == ContactState.Fault, "Unreadable stellar signal or range is a fault");

        // Hazard admission: ready exact, weak with native partial-contact error, others unknown.
        check(HazardRules.UncertaintyM(ContactState.Ready, 5000) == 0, "Ready contacts are exact at any range");
        check(HazardRules.UncertaintyM(ContactState.Weak, 10) == 2000, "Weak contacts carry one fifth of their range as clearance");
        check(HazardRules.UncertaintyM(ContactState.Weak, HazardRules.WeakHazardRangeKM) == 20000 &&
            HazardRules.UncertaintyM(ContactState.Weak, HazardRules.WeakHazardRangeKM + .001) == null, "Weak contacts beyond local reach are not guessed");
        foreach (var state in new[] { ContactState.Unavailable, ContactState.Updating, ContactState.NoSensors, ContactState.Occluded, ContactState.Fault })
            check(HazardRules.UncertaintyM(state, 1) == null && !HazardRules.Tracked(state), $"{state} is not a sensed hazard");
        check(HazardRules.Tracked(ContactState.Weak) && HazardRules.Tracked(ContactState.Ready), "Weak and ready contacts remain tracked");
        check(HazardRules.UncertaintyM(ContactState.Weak, double.NaN) == null && HazardRules.UncertaintyM(ContactState.Ready, -1) == null, "Invalid range is never a hazard guess");

        // Explicit switch-on: passive by default, active only when asked, never switches anything off.
        for (int mask = 0; mask < 8; mask++)
        {
            bool on = (mask & 1) != 0, active = (mask & 2) != 0, all = (mask & 4) != 0;
            check(SensorSuiteRules.ShouldSwitchOn(on, active, all) == (!on && (all || !active)), "Sensor switch-on selection " + mask);
        }
    }
}
