using System;

namespace PhobosAutoNav.Core;

// Original Phobos rules. All arguments are SI, except explicitly named angles.
internal static class TorchRules
{
    internal const double StandardGravity = 9.81;
    internal const double NativeNoWakeRadiusM = 300000;
    internal const double ZoneMarginM = 1000;
    internal const double ZoneRefreshSeconds = 1;
    internal const double MaximumHeadingRadians = Math.PI / 180;
    internal const double MaximumBurnSpin = 0.002;
    internal const double NativeCoreTemperature = 0.7250000238418579;
    internal const double CoreTemperatureTolerance = 0.2;
    internal const int LimiterSearchIterations = 24;

    // Distance to the entire relative-velocity segment, inflated by a bound on
    // acceleration. Checking only endpoints misses fast crossings through a zone.
    internal static bool ZoneClear(double dx, double dy, double vx, double vy,
        double accelerationBound, double horizon, double positionUncertainty = 0)
    {
        if (!Finite(dx, dy, vx, vy, accelerationBound, horizon, positionUncertainty) || accelerationBound < 0 || horizon <= 0 || positionUncertainty < 0) return false;
        double vv = vx * vx + vy * vy;
        double time = vv > 0 ? Math.Max(0, Math.Min(horizon, -(dx * vx + dy * vy) / vv)) : 0;
        double x = dx + vx * time, y = dy + vy * time;
        double clearance = NativeNoWakeRadiusM + ZoneMarginM + positionUncertainty + accelerationBound * horizon * horizon / 2;
        double distance2 = x * x + y * y;
        return Finite(vv, distance2, clearance, clearance * clearance) && distance2 > clearance * clearance;
    }

    internal static bool Aligned(double headingError, double spin, double dt) =>
        Finite(headingError, spin, dt) && dt > 0 && Math.Abs(spin) <= MaximumBurnSpin &&
        Math.Abs(headingError) + Math.Abs(spin) * dt <= MaximumHeadingRadians;

    // End speed whose RCS stopping distance still fits after this WHOLE step.
    // The envelope does not assume the torch will remain usable for braking.
    internal static double SafeSpeed(double gap, double currentSpeed, double arrivalSpeed, double rcsAcceleration, double dt)
    {
        if (!Finite(gap, currentSpeed, arrivalSpeed, rcsAcceleration, dt) || gap < 0 || currentSpeed < 0 ||
            arrivalSpeed < 0 || rcsAcceleration <= 0 || dt <= 0) return double.NaN;
        double a = CoastRules.BrakingAcceleration(rcsAcceleration);
        double remaining = Math.Max(0, gap - currentSpeed * dt);
        // Reserve another end-speed step as well as the current-speed step.
        double at = a * dt;
        return Math.Max(arrivalSpeed, Math.Sqrt(at * at + arrivalSpeed * arrivalSpeed + 2 * a * remaining) - at);
    }

    internal static double BurnAcceleration(double errorX, double errorY, double heading, double maxAcceleration, double dt)
    {
        if (!Finite(errorX, errorY, heading, maxAcceleration, dt) || maxAcceleration <= 0 || dt <= 0) return 0;
        // Project onto the actual thrust axis. Never burn through the desired
        // velocity, even when native reactor temperature temporarily increases thrust.
        double along = -Math.Sin(heading) * errorX + Math.Cos(heading) * errorY;
        return Math.Max(0, Math.Min(maxAcceleration, along / dt));
    }

    internal static bool Finite(params double[] values)
    {
        foreach (double value in values) if (!ArrivalBrake.Finite(value)) return false;
        return true;
    }
}
