using System;

namespace PhobosAutoNav.Core;

// Discrete, bounded controls; custom F3/config values are displayed without rounding them.
internal static class InstrumentRules
{
    private static readonly double[] ArrivalStops = { .1, .25, .5, 1, 2, 5, 10, 25, 50, 100 };
    internal static double StepArrival(double current, int direction)
    {
        if (!ApproachRules.ValidArrival(current) || direction == 0) return current;
        if (direction > 0)
        {
            foreach (double stop in ArrivalStops) if (stop > current + 0.00001) return stop;
        }
        else for (int i = ArrivalStops.Length - 1; i >= 0; i--)
            if (ArrivalStops[i] < current - 0.00001) return ArrivalStops[i];
        return current; // Never wrap from maximum to a near-hull arrival.
    }

    internal static float ArrivalAngle(double km) => ApproachRules.ValidArrival(km)
        ? (float)(120 - 240 * Math.Log10(km / ApproachRules.MinimumArrivalKM) /
            Math.Log10(ApproachRules.MaximumArrivalKM / ApproachRules.MinimumArrivalKM)) : 0;

    internal static bool CanSetArrival(bool active, bool saved) => !active && !saved;
    internal static bool CanEnableTorch(bool activeOrSaved, bool capturedPreference) => !activeOrSaved || capturedPreference;
}
