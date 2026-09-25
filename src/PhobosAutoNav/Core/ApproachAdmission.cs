using System;

namespace PhobosAutoNav.Core;

internal readonly struct BrakingRoom
{
    internal readonly double RequiredM, AvailableM;
    internal bool Safe => RequiredM <= AvailableM;
    internal BrakingRoom(double required, double available) { RequiredM = required; AvailableM = available; }
}

// New-flight admission only. Never revoke an ongoing brake because its margin shrank.
internal static class ApproachAdmission
{
    internal static bool TryEvaluate(double dx, double dy, double vx, double vy,
        double contactM, double arrivalM, double arrivalMS, double fullAcceleration,
        double throttle, double reactionSeconds, out BrakingRoom room)
    {
        room = default;
        if (!TorchRules.Finite(dx, dy, vx, vy, contactM, arrivalM, arrivalMS, fullAcceleration, throttle, reactionSeconds) ||
            contactM < 0 || arrivalM <= 0 || arrivalMS < 0 || fullAcceleration <= 0 ||
            throttle <= 0 || throttle > 1 || reactionSeconds <= 0) return false;
        double range = Math.Sqrt(dx * dx + dy * dy), speed = Math.Sqrt(vx * vx + vy * vy);
        if (!TorchRules.Finite(range, speed) || range <= contactM) return false;
        double closing = Math.Max(0, (vx * dx + vy * dy) / range);
        double acceleration = CoastRules.BrakingAcceleration(fullAcceleration * throttle);
        // Outside the arrival band, allow room to meet the selected arrival speed.
        // Inside it, retain a hull margin without an artificial engagement minimum.
        double boundary = range > arrivalM * ApproachRules.ArrivalBandMultiplier ?
            arrivalM * ApproachRules.ArrivalBandMultiplier : contactM * ApproachRules.HullClearanceMultiplier;
        double available = Math.Max(0, range - boundary);
        // Projection of the stopping segment along the initial line of sight is
        // conservative: sideways motion only adds distance during this brake.
        double stopping = Math.Max(0, speed * speed - arrivalMS * arrivalMS) / (2 * acceleration);
        double required = closing * reactionSeconds + (speed > 0 ? closing / speed * stopping : 0);
        if (!TorchRules.Finite(acceleration, required, available) || acceleration <= 0) return false;
        room = new BrakingRoom(required, available);
        return true;
    }
}
