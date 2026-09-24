using System;
using System.Globalization;

namespace PhobosAutoNav.Core;

// Original Phobos policy. Distances here are kilometres, not game AU.
public readonly struct ApproachPlan
{
    public double RangeKM { get; }
    public double RequestedArrivalKM { get; }
    public double EffectiveArrivalKM { get; }
    public bool InsideArrivalBand => RangeKM <= EffectiveArrivalKM * ApproachRules.ArrivalBandMultiplier;

    internal ApproachPlan(double rangeKM, double requestedKM, double effectiveKM)
    { RangeKM = rangeKM; RequestedArrivalKM = requestedKM; EffectiveArrivalKM = effectiveKM; }
}

public static class ApproachRules
{
    public const float DefaultArrivalKM = 1f;
    public const double MinimumArrivalKM = 0.1;
    public const double MaximumArrivalKM = 100;
    public const double HullClearanceMultiplier = 1.5;
    public const double ArrivalBandMultiplier = 1.05;

    public static bool ValidArrival(double km) => ArrivalBrake.Finite(km)
        && km >= MinimumArrivalKM && km <= MaximumArrivalKM;

    public static bool TryParseArrival(string text, out float km)
    {
        km = default;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            || !ValidArrival(value)) return false;
        km = (float)value;
        return true;
    }

    public static bool TryPlan(double rangeKM, double requestedKM, double hullContactKM, out ApproachPlan plan)
    {
        plan = default;
        if (!ArrivalBrake.Finite(rangeKM) || rangeKM < 0 || !ValidArrival(requestedKM)
            || !ArrivalBrake.Finite(hullContactKM) || hullContactKM < 0) return false;
        double effective = Math.Max(requestedKM, hullContactKM * HullClearanceMultiplier);
        if (!ArrivalBrake.Finite(effective)) return false;
        // No minimum engagement range or artificial 5,000 km cut-off. Inside the
        // arrival band we brake relative motion; we do not burn toward the hull.
        plan = new ApproachPlan(rangeKM, requestedKM, effective);
        return true;
    }
}
