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

        // Automatic engagement: fewest sensor types, non-emitting first, nothing for unreachable contacts.
        SensorOption Option(string key, bool emits, params double[] gains) => new(key, key, emits, gains);
        string Keys(System.Collections.Generic.IReadOnlyList<SensorOption> chosen) => string.Join(",", System.Linq.Enumerable.Select(chosen, o => o.Key));
        var need = SensorSelection.Need(.1, ContactRules.DefaultThreshold);
        check(Math.Abs(need.Required - ContactRules.DefaultThreshold * SensorSelection.TrackMargin) < 1e-12 && need.Current == .1,
            "A restored track needs headroom above the native threshold");
        check(SensorSelection.Need(double.NaN, .3).Current == 0, "An unreadable current signal counts as none");
        var passive = new[] { Option("ir", false, .2), Option("optical", false, .1), Option("radar", true, 5) };
        check(Keys(SensorSelection.Choose(passive, new[] { need }, SensorAutoEngage.All)) == "ir,optical",
            "Two passive sensors are preferred over one emitting sensor");
        check(Keys(SensorSelection.Choose(new[] { Option("ir", false, .3), Option("optical", false, .3), Option("em", false, .1) }, new[] { need }, SensorAutoEngage.All)) == "ir",
            "The fewest sensors that suffice are chosen");
        var weakPassive = new[] { Option("ir", false, .05), Option("radar", true, 1), Option("lidar", true, .5) };
        check(Keys(SensorSelection.Choose(weakPassive, new[] { need }, SensorAutoEngage.All)) == "radar",
            "An emitting sensor is used alone when passive sensors cannot help, preferring the stronger one on ties");
        check(SensorSelection.Choose(weakPassive, new[] { need }, SensorAutoEngage.Passive).Count == 0, "Passive never selects radar or LiDAR");
        check(SensorSelection.Choose(passive, new[] { need }, SensorAutoEngage.Off).Count == 0, "Off selects nothing");
        check(SensorSelection.Choose(new[] { Option("ir", false, .01) }, new[] { need }, SensorAutoEngage.All).Count == 0,
            "Nothing is switched on when no combination restores the contact");
        var beyond = new SensorNeed(.1, double.PositiveInfinity);
        check(SensorSelection.Choose(passive, new[] { beyond }, SensorAutoEngage.All).Count == 0, "A contact beyond native tracking range is never a reason to switch");
        var two = new[] { need, SensorSelection.Need(.2, ContactRules.DefaultThreshold) };
        var split = new[] { Option("ir", false, .3, 0), Option("em", false, 0, .2), Option("optical", false, .3, .2) };
        check(Keys(SensorSelection.Choose(split, two, SensorAutoEngage.All)) == "optical", "One sensor covering several contacts beats two single-purpose ones");
        check(Keys(SensorSelection.Choose(split, new[] { need, beyond }, SensorAutoEngage.All)) == "ir",
            "Reachable contacts are served even when another is out of reach");
        check(SensorSelection.Choose(new[] { Option("ir", false, double.NaN) }, new[] { need }, SensorAutoEngage.All).Count == 0 &&
            SensorSelection.Choose(new[] { Option("ir", false, 1, 1) }, new[] { need }, SensorAutoEngage.All).Count == 0,
            "Invalid or mismatched predictions are ignored");
        check(SensorSelection.Choose(passive, new[] { new SensorNeed(.5, .36) }, SensorAutoEngage.All).Count == 0, "A contact already strong enough needs nothing");
    }
}
