using System;

namespace PhobosAutoNav.Core;

// Original Phobos guidance. All public-to-the-adapter values use metres and seconds.
internal readonly struct NavVector
{
    internal readonly double X, Y;
    internal NavVector(double x, double y) { X = x; Y = y; }
    internal double Length => Math.Sqrt(X * X + Y * Y);
    internal bool Finite => TorchRules.Finite(X, Y, Length);
    internal NavVector Unit => Length > 1e-12 ? this / Length : default;
    internal double Dot(NavVector b) => X * b.X + Y * b.Y;
    internal NavVector Limit(double limit) => Length > limit ? Unit * limit : this;
    public static NavVector operator +(NavVector a, NavVector b) => new(a.X + b.X, a.Y + b.Y);
    public static NavVector operator -(NavVector a, NavVector b) => new(a.X - b.X, a.Y - b.Y);
    public static NavVector operator -(NavVector a) => new(-a.X, -a.Y);
    public static NavVector operator *(NavVector a, double b) => new(a.X * b, a.Y * b);
    public static NavVector operator /(NavVector a, double b) => new(a.X / b, a.Y / b);
}

// Session-only observations; never reads target actuators, crew or AI intent.
internal sealed class MotionTrack
{
    internal const double MaximumGap = 10, MaximumAcceleration = 40;
    private NavVector velocity;
    private double epoch;
    internal int Samples { get; private set; }
    internal NavVector Acceleration { get; private set; }
    internal double ErrorMS { get; private set; }
    internal double Age(double now) => Samples == 0 ? double.PositiveInfinity : Math.Max(0, now - epoch);
    internal double Horizon => Math.Max(2, Math.Min(20, 20 / (1 + ErrorMS)));
    internal void Reset() { Samples = 0; Acceleration = default; ErrorMS = 0; epoch = 0; velocity = default; }
    internal bool Observe(NavVector measuredVelocity, double now, NavVector knownInput = default)
    {
        if (!measuredVelocity.Finite || !knownInput.Finite || !ArrivalBrake.Finite(now)) { Reset(); return false; }
        double dt = now - epoch;
        if (Samples > 0 && dt == 0) return true; // A UI/post-physics read is not another observation.
        if (Samples == 0 || dt <= 0 || dt > MaximumGap)
        { Reset(); velocity = measuredVelocity; epoch = now; Samples = 1; return true; }
        var measured = (measuredVelocity - velocity) / dt + knownInput;
        ErrorMS = (measuredVelocity - velocity - (Acceleration - knownInput) * dt).Length;
        // Reject discontinuities, rather than treating a teleport/load as a real burn.
        if (!measured.Finite || measured.Length > MaximumAcceleration)
        { Reset(); velocity = measuredVelocity; epoch = now; Samples = 1; return true; }
        double weight = 1 - Math.Exp(-dt / .5);
        Acceleration = Samples == 1 ? measured : Acceleration * (1 - weight) + measured * weight;
        velocity = measuredVelocity; epoch = now; Samples++; return true;
    }
}

internal readonly struct GuidancePlan
{
    internal readonly NavVector Acceleration, Correction, RequestedAcceleration;
    internal readonly double Horizon, ClosingLimit;
    internal readonly bool Braking, Holding, Limited;
    internal GuidancePlan(NavVector acceleration, NavVector correction, NavVector requested, double horizon, double limit, bool braking, bool holding, bool limited)
    { Acceleration = acceleration; Correction = correction; RequestedAcceleration = requested; Horizon = horizon; ClosingLimit = limit; Braking = braking; Holding = holding; Limited = limited; }
}

// Bounded receding-horizon search: four feedback trajectories, two target models,
// eight rollout steps each. This is an authored game controller, not a certified MPC solver.
internal static class PredictiveGuidance
{
    internal const int RolloutSteps = 8;
    internal const double HoldFraction = .03, MaximumHoldBandM = 250, MinimumHoldBandM = 5;
    internal const double RotationReserve = .75, DirectionReserve = .7071067811865475;
    private static readonly double[] Responses = { 1, 2, 4, 8 };
    internal static double Band(double stop) => Math.Max(MinimumHoldBandM, Math.Min(MaximumHoldBandM, stop * HoldFraction));

    internal static bool TryPlan(NavVector offset, NavVector relativeVelocity, NavVector targetAcceleration,
        NavVector previous, double rcsAcceleration, double cruise, double arrival, double stop, double hull,
        double dt, double trustedHorizon, bool follow, out GuidancePlan plan)
    {
        plan = default;
        if (!offset.Finite || !relativeVelocity.Finite || !targetAcceleration.Finite || !previous.Finite ||
            !TorchRules.Finite(rcsAcceleration, cruise, arrival, stop, hull, dt, trustedHorizon) ||
            rcsAcceleration <= 0 || cruise <= 0 || arrival < 0 || stop <= hull || hull < 0 || dt <= 0 || dt > 60 || offset.Length <= hull) return false;
        double authority = rcsAcceleration * RotationReserve * DirectionReserve;
        double horizon = Math.Max(dt, Math.Min(20, trustedHorizon));
        double best = double.PositiveInfinity, response = 1;
        NavVector selected = default;
        foreach (double candidate in Responses)
        {
            double tau = Math.Max(dt, candidate);
            var command = Demand(offset, relativeVelocity, targetAcceleration, authority, cruise, arrival, stop, dt, tau, follow);
            double score = 0;
            for (int model = 0; model < 2; model++)
            {
                var r = offset; var v = relativeVelocity; var last = previous;
                double time = 0;
                for (int i = 0; i < RolloutSteps; i++)
                {
                    double h = i == 0 ? dt : (horizon - dt) / (RolloutSteps - 1);
                    if (h <= 0) break;
                    var a = targetAcceleration * (model == 0 ? 1 : Math.Exp(-(time + .5 * h) / 2));
                    time += h;
                    var u = (i == 0 ? command : Demand(r, v, a, authority, cruise, arrival, stop, dt, tau, follow)).Limit(authority);
                    var delta = u - a;
                    var before = r;
                    r -= v * h + delta * (.5 * h * h); v += delta * h;
                    var segment = r - before;
                    double fraction = segment.Dot(segment) > 1e-12 ? Math.Max(0, Math.Min(1, -before.Dot(segment) / segment.Dot(segment))) : 0;
                    double closest = (before + segment * fraction).Length - delta.Length * h * h / 8;
                    if (closest <= hull * ApproachRules.HullClearanceMultiplier) score += 1e12;
                    double distanceError = follow ? Math.Max(0, Math.Abs(r.Length - stop) - Band(stop)) : Math.Max(0, r.Length - stop);
                    var wanted = DesiredVelocity(r, v, a, authority, cruise, arrival, stop, dt, follow);
                    score += distanceError * .002 + (v - wanted).Length * (v - wanted).Length +
                        .1 * u.Length * h + .2 * (u - last).Length;
                    last = u;
                }
            }
            if (score < best) { best = score; response = tau; selected = command; }
        }
        if (!ArrivalBrake.Finite(best)) return false;
        bool threatened = best >= 1e12;
        if (threatened)
        { selected = (targetAcceleration - relativeVelocity / Math.Max(dt, 1)).Limit(authority); response = dt; }
        var desired = DesiredVelocity(offset, relativeVelocity, targetAcceleration, authority, cruise, arrival, stop, dt, follow);
        double closing = relativeVelocity.Dot(offset.Unit);
        bool braking = closing > Math.Max(0, desired.Dot(offset.Unit)) + .1 || offset.Length < stop;
        bool holding = follow && Math.Abs(offset.Length - stop) <= Band(stop) && relativeVelocity.Length < .5;
        plan = new GuidancePlan(selected.Limit(authority), selected * response, selected, horizon, desired.Length,
            braking || threatened, holding && !threatened, threatened || targetAcceleration.Length >= authority);
        return true;
    }

    private static NavVector Demand(NavVector r, NavVector v, NavVector a, double authority, double cruise,
        double arrival, double stop, double dt, double response, bool follow) =>
        a + (DesiredVelocity(r, v, a, authority, cruise, arrival, stop, dt, follow) - v) / response;

    private static NavVector DesiredVelocity(NavVector r, NavVector v, NavVector a, double authority,
        double cruise, double arrival, double stop, double dt, bool follow)
    {
        double gap = r.Length - stop;
        if (follow) gap = Math.Sign(gap) * Math.Max(0, Math.Abs(gap) - Band(stop) * .5);
        else gap = Math.Max(0, gap);
        // Reserve room for a changed target burn and a complete next simulation step.
        double braking = Math.Max(authority * .1, authority * .65 - Math.Max(0, -a.Dot(r.Unit)));
        double distance = Math.Max(0, Math.Abs(gap) - Math.Max(0, v.Dot(r.Unit)) * dt);
        double speed = Math.Min(cruise, Math.Sqrt(arrival * arrival + 2 * braking * distance));
        speed = Math.Min(speed, arrival + Math.Abs(gap) / Math.Max(4, 3 * dt));
        return r.Unit * (gap < 0 ? -speed : speed);
    }

    // Turn/startup time is part of the propulsion choice. A rapidly rotating
    // correction should use RCS until a useful, sustained torch leg exists.
    internal static bool TorchWorthwhile(NavVector correction, double heading, double spin, double turnAcceleration,
        double turnSpeed, double torchAcceleration, double rcsAcceleration, double trustedHorizon, double minimumCorrection,
        out double delay)
    {
        delay = double.PositiveInfinity;
        if (!correction.Finite || !TorchRules.Finite(heading, spin, turnAcceleration, turnSpeed, torchAcceleration,
            rcsAcceleration, trustedHorizon, minimumCorrection) || correction.Length < minimumCorrection ||
            turnAcceleration <= 0 || turnSpeed <= 0 || torchAcceleration <= rcsAcceleration) return false;
        double angle = Math.Abs(DockingRules.Wrap(-Math.Atan2(correction.X, correction.Y) - heading));
        delay = Math.Max(angle / turnSpeed, 2 * Math.Sqrt(angle / turnAcceleration)) + Math.Abs(spin) / turnAcceleration + TorchRules.ZoneRefreshSeconds;
        return delay < trustedHorizon && delay + correction.Length / torchAcceleration < correction.Length / rcsAcceleration;
    }

    // Conservative turn / one burn interval / RCS braking envelope. Evaluate both
    // continued thrust and coasting; do not assume the next torch burn will be available.
    internal static bool TorchSequenceSafe(NavVector offset, NavVector velocity, NavVector targetAcceleration,
        NavVector requested, double hull, double rcs, double torch, double turnDelay, double dt)
    {
        if (!offset.Finite || !velocity.Finite || !targetAcceleration.Finite || !requested.Finite ||
            !TorchRules.Finite(hull, rcs, torch, turnDelay, dt) || offset.Length <= hull || rcs <= 0 || torch <= 0 || turnDelay < 0 || dt <= 0) return false;
        double closing = Math.Max(0, velocity.Dot(offset.Unit));
        bool retrograde = requested.Dot(velocity) < 0;
        foreach (double continued in new[] { 0d, 1d })
        {
            double targetInward = Math.Max(0, -targetAcceleration.Dot(offset.Unit)) * continued;
            double turnTravel = closing * turnDelay + .5 * (rcs + targetInward) * turnDelay * turnDelay;
            double speedAfterTurn = closing + (rcs + targetInward) * turnDelay;
            double forward = retrograde ? 0 : Math.Min(torch, Math.Max(0, requested.Dot(offset.Unit)));
            double burnTravel = speedAfterTurn * dt + .5 * (forward + targetInward) * dt * dt;
            double endSpeed = speedAfterTurn + (forward + targetInward) * dt;
            double braking = rcs * RotationReserve * DirectionReserve - targetInward;
            if (braking <= 0 || turnTravel + burnTravel + endSpeed * endSpeed / (2 * braking) >= offset.Length - hull) return false;
        }
        return true;
    }
}
