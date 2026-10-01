using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Observations;

/// <summary>What a straight walk from an emitter met first.</summary>
public enum BeamHit { None, Own, Other }

/// <summary>Deck-plane geometry for a fixed emitter that sweeps a sector: which points lie in the arc, the order a
/// sweep visits them, a sampled straight walk that reports the first thing in the way, and how close a point stands
/// to the path. One unit is one deck tile, an emitter facing of zero degrees looks along +Y, and positive angles turn
/// counter-clockwise, as the game's item rotation does. Pure arithmetic: content supplies positions and what a
/// sample found.</summary>
public static class BeamGeometry
{
    public const double Tolerance = 1e-6;

    public static bool Finite(params double[] values)
    {
        foreach (double value in values) if (double.IsNaN(value) || double.IsInfinity(value)) return false;
        return true;
    }

    public static (double X, double Y) Rotate(double x, double y, double degrees)
    {
        double radians = degrees * Math.PI / 180;
        return (x * Math.Cos(radians) - y * Math.Sin(radians), x * Math.Sin(radians) + y * Math.Cos(radians));
    }

    /// <summary>The facing, in degrees, of an emitter whose outward direction is the vector (dx, dy).</summary>
    public static double Facing(double dx, double dy) => Math.Atan2(-dx, dy) * 180 / Math.PI;

    /// <summary>Whether the target lies inside the sector of <paramref name="arcDegrees"/> centred on the emitter's
    /// facing and within <paramref name="range"/>. The bearing is degrees off the centre line, negative to the
    /// emitter's left (its local -X) and positive to its right.</summary>
    public static bool Sector(double emitterX, double emitterY, double facingDegrees, double targetX, double targetY,
        double arcDegrees, double range, out double bearingDegrees, out double distance)
    {
        bearingDegrees = distance = 0;
        if (!Finite(emitterX, emitterY, facingDegrees, targetX, targetY, arcDegrees, range) || arcDegrees <= 0 || arcDegrees > 360 || range <= 0) return false;
        var local = Rotate(targetX - emitterX, targetY - emitterY, -facingDegrees);
        distance = Math.Sqrt(local.X * local.X + local.Y * local.Y);
        if (distance < Tolerance) return false;
        bearingDegrees = Math.Atan2(local.X, local.Y) * 180 / Math.PI;
        return Math.Abs(bearingDegrees) <= arcDegrees / 2 + Tolerance && distance <= range + Tolerance;
    }

    /// <summary>One-degree sweep steps: targets in the same step are taken nearest first.</summary>
    public static int Bucket(double bearingDegrees) => (int)Math.Round(bearingDegrees, MidpointRounding.AwayFromZero);

    /// <summary>The order a sweep takes its targets: from the cursor's step across to the far edge of the arc, then
    /// back round from the near edge; nearest first within a step, then by id so the order never depends on a scan.</summary>
    public static IEnumerable<T> SweepOrder<T>(IEnumerable<T> candidates, Func<T, double> bearing, Func<T, double> distance, Func<T, string> id, double cursorDegrees)
    {
        if (candidates == null) throw new ArgumentNullException(nameof(candidates));
        int cursor = Finite(cursorDegrees) ? Bucket(cursorDegrees) : int.MinValue;
        return candidates.OrderBy(c => Bucket(bearing(c)) < cursor ? 1 : 0).ThenBy(c => Bucket(bearing(c)))
            .ThenBy(distance).ThenBy(id, StringComparer.Ordinal);
    }

    /// <summary>Points along the straight line from the start towards the end, one every <paramref name="step"/>,
    /// starting one step out and finishing on the end point itself.</summary>
    public static IEnumerable<(double X, double Y)> Samples(double fromX, double fromY, double toX, double toY, double step)
    {
        if (!Finite(fromX, fromY, toX, toY, step) || step <= 0) yield break;
        double dx = toX - fromX, dy = toY - fromY, length = Math.Sqrt(dx * dx + dy * dy);
        if (length < Tolerance) yield break;
        for (double t = step; t < length - Tolerance; t += step) yield return (fromX + dx * t / length, fromY + dy * t / length);
        yield return (toX, toY);
    }

    /// <summary>Walks the samples and stops at the first one the probe reports as solid, giving where it was.</summary>
    public static BeamHit Walk(double fromX, double fromY, double toX, double toY, double step, Func<double, double, BeamHit> probe, out double hitX, out double hitY)
    {
        if (probe == null) throw new ArgumentNullException(nameof(probe));
        hitX = toX; hitY = toY;
        foreach (var sample in Samples(fromX, fromY, toX, toY, step))
        {
            var hit = probe(sample.X, sample.Y);
            if (hit == BeamHit.None) continue;
            hitX = sample.X; hitY = sample.Y;
            return hit;
        }
        return BeamHit.None;
    }

    /// <summary>The shortest distance from a point to the segment between two points.</summary>
    public static double DistanceToSegment(double x, double y, double fromX, double fromY, double toX, double toY)
    {
        if (!Finite(x, y, fromX, fromY, toX, toY)) return double.PositiveInfinity;
        double dx = toX - fromX, dy = toY - fromY, squared = dx * dx + dy * dy;
        double t = squared < Tolerance ? 0 : Math.Max(0, Math.Min(1, ((x - fromX) * dx + (y - fromY) * dy) / squared));
        double px = fromX + dx * t - x, py = fromY + dy * t - y;
        return Math.Sqrt(px * px + py * py);
    }
}
