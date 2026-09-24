using System;

namespace PhobosAutoNav.Core;

// Original Phobos cruise policy; metres, seconds and degrees, never game AU.
internal readonly struct CoastSettings
{
    public double MinimumToleranceMS { get; }
    public double SpeedTolerancePercent { get; }
    public double EnterFraction { get; }
    public double BurnHeadingToleranceDegrees { get; }

    public CoastSettings(double minimumToleranceMS, double speedTolerancePercent,
        double enterFraction, double burnHeadingToleranceDegrees)
    {
        MinimumToleranceMS = minimumToleranceMS;
        SpeedTolerancePercent = speedTolerancePercent;
        EnterFraction = enterFraction;
        BurnHeadingToleranceDegrees = burnHeadingToleranceDegrees;
    }

    public bool IsValid => InRange(MinimumToleranceMS, 0.1, 20)
        && InRange(SpeedTolerancePercent, 0, 25) && InRange(EnterFraction, 0.2, 0.9)
        && InRange(BurnHeadingToleranceDegrees, 0.1, 10);

    private static bool InRange(double value, double min, double max) =>
        ArrivalBrake.Finite(value) && value >= min && value <= max;
}

internal readonly struct CoastDecision
{
    public bool Coasting { get; }
    public double CorrectionFraction { get; }
    public double ResumeToleranceMS { get; }
    public double CrossTrackToleranceMS { get; }

    internal CoastDecision(bool coasting, double correctionFraction, double resume, double crossTrack)
    { Coasting = coasting; CorrectionFraction = correctionFraction; ResumeToleranceMS = resume; CrossTrackToleranceMS = crossTrack; }
}

internal static class CoastRules
{
    public const float DefaultSpeedTolerancePercent = 10;
    public const float DefaultEnterFraction = 0.75f;
    public const float DefaultBurnHeadingToleranceDegrees = 2;
    public const double DriftLookaheadSeconds = 30;
    public const double DriftRadiusFraction = 0.25;
    public const double DegreesToRadians = Math.PI / 180;
    public const double BrakingReserve = 0.85;
    private const double TwoAxisThrottleCost = 1.4142135623730951;
    private const double SpinDeadbandRadiansPerSecond = 0.002;
    private const double RotationResponseSteps = 2;
    private const double BoundaryEpsilon = 1e-9;

    // Guard the next coast step using actual range, not the farther predicted
    // intercept point. The /sqrt(2) allowance matches the arrival brake's
    // aggregate two-axis throttle budget in its least favourable orientation.
    public static bool TryBrakingSpeedLimit(double gapM, double closingMS, double arrivalMS,
        double accelerationMS2, double dt, out double speedMS)
    {
        speedMS = 0;
        if (!Nonnegative(gapM) || !ArrivalBrake.Finite(closingMS) || !Nonnegative(arrivalMS)
            || !Nonnegative(accelerationMS2) || accelerationMS2 == 0 || !Nonnegative(dt) || dt == 0) return false;
        double nextGap = Math.Max(0, gapM - Math.Max(0, closingMS) * dt);
        double budget = arrivalMS * arrivalMS + 2 * accelerationMS2 * BrakingReserve / TwoAxisThrottleCost * nextGap;
        if (!ArrivalBrake.Finite(budget)) return false;
        speedMS = Math.Sqrt(budget);
        return true;
    }

    public static bool TryDecide(bool wasCoasting, double cruiseMS, double errorMS, double crossTrackMS,
        double arrivalRadiusM, bool braking, CoastSettings settings, out CoastDecision decision)
    {
        decision = default;
        if (!settings.IsValid || !Nonnegative(cruiseMS) || cruiseMS == 0 || !Nonnegative(errorMS)
            || !Nonnegative(crossTrackMS) || !Nonnegative(arrivalRadiusM) || arrivalRadiusM == 0) return false;
        double resume = Math.Max(settings.MinimumToleranceMS, cruiseMS * settings.SpeedTolerancePercent / 100);
        double crossTrack = Math.Min(resume, arrivalRadiusM * DriftRadiusFraction / DriftLookaheadSeconds);
        if (!ArrivalBrake.Finite(resume) || crossTrack <= 0) return false;
        // Cruise economy must never suppress braking or final-arrival handling.
        if (braking) { decision = new CoastDecision(false, 1, resume, crossTrack); return true; }
        double demand = Math.Max(errorMS / resume, crossTrackMS / crossTrack);
        if (!ArrivalBrake.Finite(demand)) return false;
        bool coast = demand <= (wasCoasting ? 1 : settings.EnterFraction) + BoundaryEpsilon;
        // A correction ends at the inner band, rather than chasing zero error.
        double fraction = coast ? 0 : Math.Max(0, 1 - settings.EnterFraction / demand);
        decision = new CoastDecision(coast, fraction, resume, crossTrack);
        return true;
    }

    // Translational RCS is already transformed into body axes by the controller.
    // Coasting needs no target-facing attitude: damp a spin, don't chase heading.
    public static double CoastRotation(double angularSpeed, double dt, double maximumCommand)
    {
        if (!ArrivalBrake.Finite(angularSpeed) || !Nonnegative(dt) || dt == 0
            || !Nonnegative(maximumCommand) || Math.Abs(angularSpeed) <= SpinDeadbandRadiansPerSecond) return 0;
        return Math.Max(-maximumCommand, Math.Min(maximumCommand, -angularSpeed / (RotationResponseSteps * dt)));
    }

    private static bool Nonnegative(double value) => ArrivalBrake.Finite(value) && value >= 0;
}
