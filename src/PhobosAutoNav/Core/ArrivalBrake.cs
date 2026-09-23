using System;
using System.Text.RegularExpressions;

namespace PhobosAutoNav.Core;

// Original Phobos logic. Units here are metres and seconds, not game AU.
public readonly struct BrakeCommand
{
    public double X { get; }
    public double Y { get; }
    public BrakeCommand(double x, double y) { X = x; Y = y; }
}

public static class ArrivalBrake
{
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

    public static bool NeedsBrake(double range, double radius, double speed, double arrival, double tolerance) =>
        range <= radius * 1.05 && speed > arrival + tolerance;

    public static bool TryCommand(double vx, double vy, double rotation, double fullAcceleration,
        double throttle, double arrival, double dt, out BrakeCommand command)
    {
        command = default;
        foreach (double value in new[] { vx, vy, rotation, fullAcceleration, throttle, arrival, dt })
            if (!Finite(value)) return false;
        if (fullAcceleration <= 0 || throttle <= 0 || throttle > 1 || arrival < 0 || dt <= 0) return false;
        double speed = Math.Sqrt(vx * vx + vy * vy);
        if (!Finite(speed)) return false;
        if (speed <= arrival) return true;
        double cos = Math.Cos(rotation), sin = Math.Sin(rotation);
        double x = -(vx * cos + vy * sin) / speed;
        double y = -(-vx * sin + vy * cos) / speed;
        // Native fuel use adds absolute axis inputs; honour an aggregate throttle cap.
        double cap = throttle / (Math.Abs(x) + Math.Abs(y));
        double demand = (speed - arrival) / dt / fullAcceleration;
        double fraction = Math.Min(cap, demand);
        command = new BrakeCommand(x * fraction, y * fraction);
        return true;
    }

    public static bool TestSaveAllowed(string? name) => name != null && Regex.IsMatch(name,
        @"^(?:autosave_\d+_)?PhobosAutoNavTest(?:$|-)", RegexOptions.CultureInvariant);
}
