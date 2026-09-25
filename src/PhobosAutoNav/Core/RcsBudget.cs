using System;

namespace PhobosAutoNav.Core;

// Native RCS gas demand adds absolute translation and rotation inputs.
internal static class RcsBudget
{
    internal const double CombinedRotationShare = .25;
    internal const double MinimumTranslationShare = 1 - CombinedRotationShare;
    internal const double TwoAxisCost = 1.4142135623730951;

    internal static bool TryLimit(double x, double y, double turn, double throttle,
        double rotationShare, out RcsCommand command)
    {
        command = default;
        if (!TorchRules.Finite(x, y, turn, throttle, rotationShare) || throttle < 0 || throttle > 1 ||
            rotationShare < 0 || rotationShare > 1) return false;
        double rotation = Math.Max(-throttle * rotationShare, Math.Min(throttle * rotationShare, turn));
        double demand = Math.Abs(x) + Math.Abs(y), budget = throttle - Math.Abs(rotation);
        if (!ArrivalBrake.Finite(demand)) return false;
        if (demand > budget) { x *= budget / demand; y *= budget / demand; }
        command = new RcsCommand(x, y, rotation);
        return true;
    }
}

internal readonly struct RcsCommand
{
    internal readonly double X, Y, Turn;
    internal RcsCommand(double x, double y, double turn) { X = x; Y = y; Turn = turn; }
}
