using System;

namespace PhobosAutoNav.Core;

// Authored permission/aim policy; native weapons still own projectile creation and costs.
internal static class FireRules
{
    internal const double MaximumStep = 1, LockMarginDegrees = 2.5;
    internal static double Aim(double previous, double arc, double rate, double dt, bool ready)
    {
        if (!ready || !TorchRules.Finite(previous, arc, rate, dt) || arc <= 0 || rate <= 0 || dt <= 0 || dt > MaximumStep) return 0;
        return Math.Min(arc, Math.Max(0, previous) + rate * dt);
    }
    internal static bool Locked(double progress, double arc) =>
        TorchRules.Finite(progress, arc) && arc > 0 && progress + LockMarginDegrees >= arc;
    internal static NavVector Lead(NavVector offset, NavVector targetRelativeVelocity, double speed)
    {
        if (!offset.Finite || !targetRelativeVelocity.Finite || !ArrivalBrake.Finite(speed) || speed <= 0) return offset;
        double a = targetRelativeVelocity.Dot(targetRelativeVelocity) - speed * speed;
        double b = 2 * offset.Dot(targetRelativeVelocity), c = offset.Dot(offset);
        double t = 0;
        if (Math.Abs(a) < 1e-9) { if (b < 0) t = -c / b; }
        else
        {
            double d = b * b - 4 * a * c;
            if (d >= 0)
            {
                double first = (-b - Math.Sqrt(d)) / (2 * a), second = (-b + Math.Sqrt(d)) / (2 * a);
                t = first > 0 && second > 0 ? Math.Min(first, second) : Math.Max(first, second);
            }
        }
        return t > 0 && ArrivalBrake.Finite(t) ? offset + targetRelativeVelocity * t : offset;
    }
}
