using System;

namespace Phobos.Ostranauts.Framework.Flight;

/// <summary>How long a torch-drive trip takes (Framework 0.123.0), with no game types, so the offline checks run it. It
/// follows the game's own trip planner (<c>PlanTrip4Sub</c>): accelerate at the drive's thrust, coast at the torch speed
/// limit when the trip is long enough to reach it, and brake to arrive at rest. A straight line between the two ends at
/// the moment of asking: orbits, docking and fuel are left to the callers that need them.</summary>
public static class TorchTrip
{
    /// <summary>The game's own torch speed limit (its planner's <c>fTorchSpeedLimit</c> default, a tenth of light speed), in AU a second.</summary>
    public const double SpeedLimitAuPerSecond = 0.00020039887409959505;
    /// <summary>Kilometres in one AU, as the game counts them.</summary>
    public const double KilometresPerAu = 149597872.0;

    /// <summary>Seconds to cover <paramref name="au"/> from rest to rest at <paramref name="acceleration"/> (AU/s²),
    /// coasting at <paramref name="speedLimit"/>; infinity when the ship cannot accelerate.</summary>
    public static double Seconds(double au, double acceleration, double speedLimit = SpeedLimitAuPerSecond)
    {
        if (!(au > 0)) return 0;
        if (!(acceleration > 0) || double.IsInfinity(acceleration) || double.IsNaN(au)) return double.PositiveInfinity;
        double halfway = Math.Sqrt(au / acceleration);
        if (!(speedLimit > 0) || acceleration * halfway <= speedLimit) return 2 * halfway;
        // Reaching the limit: up to speed, coast the rest, then down again.
        double ramp = speedLimit / acceleration;
        double rampDistance = acceleration * ramp * ramp;
        return 2 * ramp + (au - rampDistance) / speedLimit;
    }
}
