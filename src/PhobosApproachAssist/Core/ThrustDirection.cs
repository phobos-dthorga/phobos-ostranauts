using System;

namespace PhobosApproachAssist.Core;

public readonly struct ThrustDirection
{
    public const double MaximumThrottle = 0.1;
    public double X { get; }
    public double Y { get; }
    private ThrustDirection(double x, double y) { X = x; Y = y; }

    public double AvailableAcceleration(double fullAcceleration) =>
        fullAcceleration * MaximumThrottle / (Math.Abs(X) + Math.Abs(Y));

    public static bool TryCreate(double dx, double dy, double rotation, out ThrustDirection direction)
    {
        direction = default;
        if (!BurnPlan.Finite(dx) || !BurnPlan.Finite(dy) || !BurnPlan.Finite(rotation)) return false;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (!BurnPlan.Finite(length) || length <= 0) return false;
        double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
        direction = new ThrustDirection((dx * cos + dy * sin) / length, (-dx * sin + dy * cos) / length);
        return true;
    }
}
