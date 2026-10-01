using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Effects;
using Phobos.Ostranauts.Framework.Observations;

internal static class BeamGeometryChecks
{
    internal static void Run(Action<bool, string> check)
    {
        // A sector of 60 degrees and 24 tiles, at each cardinal mounting.
        foreach (int facing in new[] { 0, 90, 180, 270 })
        {
            var ahead = BeamGeometry.Rotate(0, 10, facing);
            check(BeamGeometry.Sector(5, 7, facing, 5 + ahead.X, 7 + ahead.Y, 60, 24, out double bearing, out double distance) &&
                Math.Abs(bearing) < 1e-6 && Math.Abs(distance - 10) < 1e-6, "A point straight ahead is in the arc at every mounting");
            var behind = BeamGeometry.Rotate(0, -10, facing);
            check(!BeamGeometry.Sector(5, 7, facing, 5 + behind.X, 7 + behind.Y, 60, 24, out _, out _), "A point behind the emitter is never in the arc");
            foreach (double edge in new[] { -30.0, 30.0 })
            {
                var onEdge = BeamGeometry.Rotate(0, 10, facing - edge);
                check(BeamGeometry.Sector(5, 7, facing, 5 + onEdge.X, 7 + onEdge.Y, 60, 24, out double b, out _) && Math.Abs(b - edge) < 1e-6,
                    "The arc edge is inside, and a bearing to the emitter's right is positive");
                var outside = BeamGeometry.Rotate(0, 10, facing - edge * 31 / 30);
                check(!BeamGeometry.Sector(5, 7, facing, 5 + outside.X, 7 + outside.Y, 60, 24, out _, out _), "One degree past the edge is outside");
            }
            var far = BeamGeometry.Rotate(0, 24, facing); var beyond = BeamGeometry.Rotate(0, 24.01, facing);
            check(BeamGeometry.Sector(0, 0, facing, far.X, far.Y, 60, 24, out _, out _) && !BeamGeometry.Sector(0, 0, facing, beyond.X, beyond.Y, 60, 24, out _, out _),
                "The range limit is inclusive and nothing beyond it is reached");
            var outward = BeamGeometry.Rotate(0, 1, facing);
            check(Math.Abs(Math.IEEERemainder(BeamGeometry.Facing(outward.X, outward.Y) - facing, 360)) < 1e-6, "Facing is recovered from the outward vector");
        }
        check(!BeamGeometry.Sector(0, 0, 0, 0, 0, 60, 24, out _, out _) && !BeamGeometry.Sector(0, 0, 0, double.NaN, 1, 60, 24, out _, out _) &&
            !BeamGeometry.Sector(0, 0, 0, 0, 1, 0, 24, out _, out _) && !BeamGeometry.Sector(0, 0, 0, 0, 1, 60, 0, out _, out _),
            "The emitter itself, invalid numbers, an empty arc and no range are refused");

        // Sweep order: from the cursor across to the far edge, then round from the near edge; nearest first in a step.
        var targets = new[] { (Id: "d", Bearing: 20.0, Distance: 4.0), (Id: "a", Bearing: -25.0, Distance: 9.0), (Id: "c", Bearing: 5.2, Distance: 8.0),
            (Id: "b", Bearing: 5.0, Distance: 3.0), (Id: "e", Bearing: 5.0, Distance: 3.0) };
        string Order(double cursor) => string.Concat(BeamGeometry.SweepOrder(targets, t => t.Bearing, t => t.Distance, t => t.Id, cursor).Select(t => t.Id));
        check(Order(-30) == "abecd", "A sweep from the near edge runs across the arc, nearest first within a degree, ties by id");
        check(Order(5) == "becda", "A sweep resumes at the cursor and wraps to what it passed");
        check(Order(21) == "abecd" && Order(double.NaN) == "abecd", "A cursor past everything, or none, starts from the near edge");

        // The sampled walk reports the first solid thing, and whose it is.
        var samples = BeamGeometry.Samples(0, 0, 0, 2.2, 0.5).ToArray();
        check(samples.Length == 5 && Math.Abs(samples[0].Y - 0.5) < 1e-9 && Math.Abs(samples[4].Y - 2.2) < 1e-9, "Samples start one step out and end on the target");
        check(!BeamGeometry.Samples(0, 0, 0, 0, 0.5).Any() && !BeamGeometry.Samples(0, 0, 1, 1, 0).Any(), "No walk without a distance or a step");
        BeamHit Probe(double x, double y) => y > 5.9 && y < 6.6 ? BeamHit.Own : y > 3.9 && y < 4.6 ? BeamHit.Other : BeamHit.None;
        check(BeamGeometry.Walk(0, 0, 0, 10, 0.5, Probe, out _, out double hitY) == BeamHit.Other && Math.Abs(hitY - 4) < 1e-9, "The nearer obstacle is met first");
        check(BeamGeometry.Walk(0, 5, 0, 10, 0.5, Probe, out _, out hitY) == BeamHit.Own && Math.Abs(hitY - 6) < 1e-9, "Our own hull in the way is reported as ours");
        check(BeamGeometry.Walk(0, 0, 0, 3, 0.5, Probe, out _, out hitY) == BeamHit.None && Math.Abs(hitY - 3) < 1e-9, "A clear path reaches its target");

        check(Math.Abs(BeamGeometry.DistanceToSegment(1, 5, 0, 0, 0, 10) - 1) < 1e-9 && Math.Abs(BeamGeometry.DistanceToSegment(0, 12, 0, 0, 0, 10) - 2) < 1e-9 &&
            Math.Abs(BeamGeometry.DistanceToSegment(3, -4, 0, 0, 0, 10) - 5) < 1e-9, "Distance to a beam path is measured to the segment, not the endless line");
        check(double.IsPositiveInfinity(BeamGeometry.DistanceToSegment(double.NaN, 0, 0, 0, 0, 1)), "An unknown position is never near the path");

        // The beam quad under a 2 x 2 item (parent scale 2): emitter on the top edge, target six tiles straight out.
        check(BeamTransform.Solve(0, 16, 2, 2, 0, 3.5, 0.25, out var pose) && Math.Abs(pose.LengthTiles - 6) < 1e-9 && Math.Abs(pose.X) < 1e-9 &&
            Math.Abs(pose.Y - 2) < 1e-9 && Math.Abs(pose.AngleDegrees - 90) < 1e-9 && Math.Abs(pose.ScaleX - 3) < 1e-9 && Math.Abs(pose.ScaleY - 0.125) < 1e-9,
            "A beam straight out is centred between emitter and target, turned along it and as long as the gap");
        check(BeamTransform.Solve(0, 16, 2, 2, 2, 2.5, 0.25, out pose) && Math.Abs(pose.AngleDegrees - 45) < 1e-9 && Math.Abs(pose.LengthTiles - Math.Sqrt(32)) < 1e-9,
            "A beam to one side turns towards its target");
        check(!BeamTransform.Solve(0, 16, 3, 2, 0, 3, 0.25, out _) && !BeamTransform.Solve(0, 16, 2, 2, 0, 0.5, 0.25, out _) &&
            !BeamTransform.Solve(0, 16, 2, 2, double.NaN, 3, 0.25, out _) && !BeamTransform.Solve(0, 16, 2, 2, 0, 3, 0, out _),
            "A non-square item, a target on the emitter, an unknown target and no thickness draw nothing");
    }
}
