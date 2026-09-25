using System;

namespace PhobosAutoNav.Core;

// Authored permission/aim policy; native weapons still own projectile creation and costs.
internal static class FireRules
{
    internal const double MaximumStep = 1, LockMarginDegrees = 2.5, NativeLeadRelativeTolerance = 1e-10;
    internal static double Turn(double error, double spin, double dt, double throttle, double acceleration, double speed)
    {
        if (!TorchRules.Finite(error, spin, dt, throttle, acceleration, speed) || dt <= 0 || dt > MaximumStep || throttle <= 0 || acceleration <= 0 || speed <= 0) return 0;
        double limit = Math.Min(1, throttle) * acceleration;
        double desired = Math.Sign(error) * Math.Min(speed, Math.Sqrt(1.6 * limit * Math.Abs(error)));
        return Math.Max(-limit, Math.Min(limit, (desired - spin) / (2 * dt)));
    }
    internal static double Aim(double previous, double arc, double rate, double dt, bool ready)
    {
        if (!ready || !TorchRules.Finite(previous, arc, rate, dt) || arc <= 0 || rate <= 0 || dt <= 0 || dt > MaximumStep) return 0;
        return Math.Min(arc, Math.Max(0, previous) + rate * dt);
    }
    internal static bool Locked(double progress, double arc) =>
        TorchRules.Finite(progress, arc) && arc > 0 && progress + LockMarginDegrees >= arc;
    internal static NavVector Lead(NavVector offset, NavVector targetRelativeVelocity, double speed)
        => TryLead(offset, targetRelativeVelocity, speed, out var lead) ? lead : offset;
    internal static bool TryLead(NavVector offset, NavVector targetRelativeVelocity, double speed, out NavVector lead)
    {
        lead = offset;
        if (!offset.Finite || offset.Length <= 0 || !targetRelativeVelocity.Finite || !ArrivalBrake.Finite(speed) || speed <= 0) return false;
        double a = targetRelativeVelocity.Dot(targetRelativeVelocity) - speed * speed;
        // Native GetExactFiringAngle divides by 2a, including the linear case.
        // Do not grant a solution which that projectile implementation cannot use.
        if (Math.Abs(a) <= NativeLeadRelativeTolerance * speed * speed) return false;
        double b = 2 * offset.Dot(targetRelativeVelocity), c = offset.Dot(offset);
        double t = 0;
        double d = b * b - 4 * a * c;
        if (d >= 0)
        {
            double first = (-b - Math.Sqrt(d)) / (2 * a), second = (-b + Math.Sqrt(d)) / (2 * a);
            t = first > 0 && second > 0 ? Math.Min(first, second) : Math.Max(first, second);
        }
        if (t <= 0 || !ArrivalBrake.Finite(t)) return false;
        lead = offset + targetRelativeVelocity * t;
        return lead.Finite;
    }
}
