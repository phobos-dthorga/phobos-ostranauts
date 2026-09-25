using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosAutoNav.Core;

internal readonly struct ObstacleDisc
{
    internal readonly string Id;
    internal readonly NavVector Position, Velocity;
    internal readonly double Radius;
    internal ObstacleDisc(string id, NavVector position, NavVector velocity, double radius)
    { Id = id; Position = position; Velocity = velocity; Radius = radius; }
}

// Original bounded local planner. Coordinates are metres in the observer's frame.
// No native references, hidden contacts, collision exemptions or actuator writes.
internal sealed class ObstacleRoute
{
    internal const int MaximumObstacles = 32, RingPoints = 16, MaximumVisibilityTests = 65536;
    internal const double MinimumMarginM = 25, MaximumHorizonSeconds = 600;
    internal double LastCost { get; private set; }
    private string passing = "";
    private int side;
    internal void Reset() { passing = ""; side = 0; }
    internal static double Distance(NavVector a, NavVector b, NavVector point)
    {
        var delta = b - a;
        double length2 = delta.Dot(delta);
        double t = length2 <= 1e-12 ? 0 : Math.Max(0, Math.Min(1, (point - a).Dot(delta) / length2));
        return (point - a - delta * t).Length;
    }
    internal static bool Clear(NavVector a, NavVector b, IReadOnlyList<ObstacleDisc> obstacles) =>
        obstacles.All(o => Distance(a, b, o.Position) > o.Radius);

    internal bool Plan(NavVector goal, IReadOnlyList<ObstacleDisc> obstacles, out NavVector waypoint)
    {
        waypoint = default; LastCost=double.PositiveInfinity;
        if (!goal.Finite || obstacles.Count > MaximumObstacles || obstacles.Any(o => !o.Position.Finite ||
            !o.Velocity.Finite || !ArrivalBrake.Finite(o.Radius) || o.Radius <= 0)) return false;
        if (Clear(default, goal, obstacles)) { Reset(); waypoint = goal; LastCost=goal.Length; return true; }
        if (obstacles.Any(o => o.Position.Length <= o.Radius || (goal - o.Position).Length <= o.Radius)) return false;
        var nodes = new List<NavVector> { default, goal };
        var blockers = obstacles.Where(o => Distance(default, goal, o.Position) <= o.Radius).ToArray();
        var first = blockers.OrderBy(o => o.Position.Length).ThenBy(o => o.Id, StringComparer.Ordinal).First();
        if (passing != first.Id) { passing = first.Id; side = 0; }
        foreach (var o in obstacles)
            for (int n = 0; n < RingPoints; n++)
            {
                double angle = 2 * Math.PI * n / RingPoints;
                // Circumscribed ring: each adjacent straight chord remains outside the disc.
                var p = o.Position + new NavVector(Math.Cos(angle), Math.Sin(angle)) * ((o.Radius + 1) / Math.Cos(Math.PI / RingPoints));
                if (obstacles.All(d => (p - d.Position).Length > d.Radius)) nodes.Add(p);
            }
        var cost = Enumerable.Repeat(double.PositiveInfinity, nodes.Count).ToArray();
        var previous = Enumerable.Repeat(-1, nodes.Count).ToArray(); var done = new bool[nodes.Count];
        cost[0] = 0;
        int visibilityTests=0;
        for (int iteration = 0; iteration < nodes.Count; iteration++)
        {
            int at = -1;
            for (int i = 0; i < nodes.Count; i++) if (!done[i] && (at < 0 || cost[i] < cost[at])) at = i;
            if (at < 0 || double.IsPositiveInfinity(cost[at])) break;
            if (at == 1) break;
            done[at] = true;
            for (int j = 1; j < nodes.Count; j++)
            {
                if(done[j]) continue;
                if(++visibilityTests>MaximumVisibilityTests) return false;
                if(!Clear(nodes[at],nodes[j],obstacles)) continue;
                int candidateSide = Math.Sign(goal.X * nodes[j].Y - goal.Y * nodes[j].X);
                double penalty = at == 0 && side != 0 && candidateSide != 0 && side != candidateSide ? goal.Length : 0;
                double next = cost[at] + (nodes[j] - nodes[at]).Length + penalty;
                if (next < cost[j]) { cost[j] = next; previous[j] = at; }
            }
        }
        if (previous[1] < 0) return false;
        int hop = 1;
        while (previous[hop] > 0) hop = previous[hop];
        waypoint = nodes[hop]; LastCost=cost[1];
        side = Math.Sign(goal.X * waypoint.Y - goal.Y * waypoint.X);
        return true;
    }

    internal static bool BurnAndBrakeSafe(NavVector velocity, NavVector acceleration, double burnSeconds, double braking, IReadOnlyList<ObstacleDisc> obstacles)
    {
        if(!ArrivalBrake.Finite(braking)||braking<=0||!SweepSafe(velocity,acceleration,burnSeconds,obstacles)) return false;
        var end=velocity*burnSeconds+acceleration*(.5*burnSeconds*burnSeconds);
        var speed=velocity+acceleration*burnSeconds;
        double seconds=Math.Max(.001,speed.Length/braking);
        if(seconds>MaximumHorizonSeconds) return false;
        var moved=obstacles.Select(o=>new ObstacleDisc(o.Id,o.Position+o.Velocity*burnSeconds-end,o.Velocity,o.Radius)).ToArray();
        return SweepSafe(speed,-speed.Unit*braking,seconds,moved);
    }

    internal static bool SweepSafe(NavVector velocity, NavVector acceleration, double dt, IReadOnlyList<ObstacleDisc> obstacles)
    {
        if (!velocity.Finite || !acceleration.Finite || !ArrivalBrake.Finite(dt) || dt <= 0) return false;
        foreach (var o in obstacles)
        {
            var relative = velocity - o.Velocity;
            var end = relative * dt + acceleration * (.5 * dt * dt);
            // Curved path deviates from its chord by at most |a| dt squared / 8.
            if (Distance(default, end, o.Position) <= o.Radius + acceleration.Length * dt * dt / 8) return false;
        }
        return true;
    }
}
