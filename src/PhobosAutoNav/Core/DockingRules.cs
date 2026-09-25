using System;

namespace PhobosAutoNav.Core;

// Original Phobos terminal guidance. Metres, seconds and radians throughout.
internal static class DockingRules
{
    internal const double MaximumHullGapM = 10000, MaximumSeconds = 1800, MaximumStep = 1;
    internal const double CruiseMS = 20, ClampSpeedMS = .2, ClampHeadingRadians = .004,
        ClampSpinRadians = .002, VelocityDeadbandMS = .03;
    internal const double NativeClampRadius = 1.1, StandOffRadius = 1.05, MinimumRadius = 1.005;
    private const double BrakingReserve = .45, RotationAcceleration = .15, RotationSpeed = .1;

    internal static double Wrap(double angle) => Math.Atan2(Math.Sin(angle), Math.Cos(angle));

    internal static bool TryGuide(double dx, double dy, double vx, double vy, double rotation, double spin,
        double contactM, double acceleration, double throttle, double dt, out DockingCommand command, NavVector targetAcceleration = default)
    {
        command = default;
        foreach (double value in new[] { dx, dy, vx, vy, rotation, spin, contactM, acceleration, throttle, dt })
            if (!ArrivalBrake.Finite(value)) return false;
        if (!targetAcceleration.Finite) return false;
        if (contactM <= 0 || acceleration <= 0 || throttle <= 0 || throttle > 1 || dt <= 0 || dt > MaximumStep) return false;
        double range = Math.Sqrt(dx * dx + dy * dy), speed = Math.Sqrt(vx * vx + vy * vy);
        if (!ArrivalBrake.Finite(range) || !ArrivalBrake.Finite(speed) || range < contactM * MinimumRadius || range - contactM > MaximumHullGapM) return false;
        double nx = dx / range, ny = dy / range, closing = vx * nx + vy * ny;
        double heading = Wrap(-Math.Atan2(dx, dy) - rotation);
        double available = Math.Max(acceleration * throttle * .05, acceleration * throttle * BrakingReserve - Math.Max(0, -targetAcceleration.Dot(new NavVector(nx, ny))));
        // Refuse an already unsafe intercept; this controller does not promise recovery or obstacle avoidance.
        if (closing > 0 && closing * closing / (2 * available) + closing * dt >= range - contactM) return false;
        bool ready = range <= contactM * NativeClampRadius && speed <= ClampSpeedMS &&
            Math.Abs(heading) <= ClampHeadingRadians && Math.Abs(spin) <= ClampSpinRadians;
        double gap = Math.Max(0, range - contactM * StandOffRadius);
        double plannedBraking = available * .5; // Keep room for deadband, sampled control and sideways correction.
        double desired = Math.Min(CruiseMS, Math.Min(gap / (3 * dt),
            Math.Sqrt(plannedBraking * plannedBraking * dt * dt + 2 * plannedBraking * gap) - plannedBraking * dt));
        // Brake sideways drift and face the target before advancing, rather than sweeping across it.
        double lateral = Math.Abs(vx * ny - vy * nx);
        if (Math.Abs(heading) > .15 || lateral > Math.Max(1, desired * .25)) desired = 0;
        double ex = nx * desired - vx + targetAcceleration.X * dt, ey = ny * desired - vy + targetAcceleration.Y * dt;
        double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
        double x = (ex * cos + ey * sin) / (acceleration * dt), y = (-ex * sin + ey * cos) / (acceleration * dt);
        if (Math.Sqrt(ex * ex + ey * ey) < VelocityDeadbandMS) x = y = 0;
        double wantedSpin = Math.Sign(heading) * Math.Min(RotationSpeed, Math.Min(Math.Abs(heading) / (4 * dt),
            Math.Sqrt(RotationAcceleration * Math.Abs(heading))));
        double turn = Math.Max(-RotationAcceleration, Math.Min(RotationAcceleration, (wantedSpin - spin) / (2 * dt)));
        if (Math.Abs(heading) < ClampHeadingRadians * .5 && Math.Abs(spin) < ClampSpinRadians * .5) turn = 0;
        // Translation uses the native aggregate axis budget. Rotation consumes fuel too.
        if (!RcsBudget.TryLimit(x, y, turn, throttle, RcsBudget.CombinedRotationShare, out var bounded)) return false;
        command = new DockingCommand(bounded.X, bounded.Y, bounded.Turn, ready, range - contactM, speed, heading);
        return true;
    }
    internal static bool TryHold(NavVector offset, NavVector velocity, NavVector targetAcceleration,
        double rotation, double spin, double hull, double acceleration, double throttle, double dt, out DockingCommand command)
    {
        command = default;
        if (!offset.Finite || !velocity.Finite || !targetAcceleration.Finite ||
            !TorchRules.Finite(rotation, spin, hull, acceleration, throttle, dt) || acceleration <= 0 || throttle <= 0 ||
            throttle > 1 || hull <= 0 || dt <= 0 || dt > MaximumStep) return false;
        NavVector demand;
        double separation = Math.Max(hull * 1.5, hull + 100);
        if (PredictiveGuidance.TryPlan(offset, velocity, targetAcceleration, default, acceleration * throttle,
            CruiseMS, 0, separation, hull, dt, 2, true, out var plan)) demand = plan.Acceleration;
        else demand = (targetAcceleration - velocity / dt).Limit(acceleration * throttle * .5);
        double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
        double turn = Math.Max(-RotationAcceleration, Math.Min(RotationAcceleration, -spin / (2 * dt)));
        if (!RcsBudget.TryLimit((demand.X * cos + demand.Y * sin) / acceleration,
            (-demand.X * sin + demand.Y * cos) / acceleration, turn, throttle, RcsBudget.CombinedRotationShare, out var bounded)) return false;
        command = new DockingCommand(bounded.X, bounded.Y, bounded.Turn, false, offset.Length - hull, velocity.Length, 0);
        return true;
    }

}

internal readonly struct DockingCommand
{
    internal readonly double X, Y, Turn, HullGapM, SpeedMS, HeadingRadians;
    internal readonly bool Ready;
    internal DockingCommand(double x, double y, double turn, bool ready, double gap, double speed, double heading)
    { X = x; Y = y; Turn = turn; Ready = ready; HullGapM = gap; SpeedMS = speed; HeadingRadians = heading; }
}
