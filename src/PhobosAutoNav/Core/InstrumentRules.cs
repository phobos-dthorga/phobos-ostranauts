using System;

namespace PhobosAutoNav.Core;

// Discrete, bounded controls; custom F3/config values are displayed without rounding them.
internal static class InstrumentRules
{
    private static readonly double[] ArrivalStops = { .1, .25, .5, 1, 2, 5, 10, 25, 50, 100 };
    private static readonly double[] CruiseStops = { 10, 20, 50, 100, 200, 500, 1000, 2000, 5000 };
    private static readonly double[] SpeedStops = { 0, .1, .2, .5, 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
    internal static double StepSpeed(double current, int direction, bool arrival, double cruise)
    {
        double maximum = arrival ? Math.Min(cruise, FlightPreferences.MaximumArrivalMS) : FlightPreferences.MaximumCruiseMS;
        double minimum = arrival ? 0 : FlightPreferences.MinimumCruiseMS;
        if (!ArrivalBrake.Finite(current) || current < minimum || current > maximum || direction == 0) return current;
        var stops = arrival ? SpeedStops : CruiseStops;
        if (direction > 0)
        {
            foreach (double stop in stops) if (stop > current) return Math.Min(stop, maximum);
        }
        else for (int i = stops.Length - 1; i >= 0; i--)
            if (stops[i] < current) return Math.Max(stops[i], minimum);
        return current;
    }
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
