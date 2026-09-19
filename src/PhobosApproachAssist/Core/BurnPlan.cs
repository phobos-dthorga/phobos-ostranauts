using System;

namespace PhobosApproachAssist.Core;

/// <summary>A finite integration-test pulse, not an approach or braking controller.</summary>
public sealed class BurnPlan
{
    public const double DurationSeconds = 2;
    public const double MaxAcceleration = 0.05;
    public const double MaxDeltaV = 0.1;
    public const double MaxStepSeconds = 2;
    public double Elapsed { get; private set; }
    public double CommandedDeltaV { get; private set; }
    public bool Active { get; private set; } = true;
    public string Status { get; private set; } = "Test pulse armed";

    public double Step(double seconds, double availableAcceleration)
    {
        if (!Active) return 0;
        if (!Finite(seconds) || seconds < 0 || seconds > MaxStepSeconds)
        {
            Cancel("Simulation step outside prototype limit");
            return 0;
        }
        if (seconds == 0) return 0;
        if (!Finite(availableAcceleration) || availableAcceleration <= 0)
        {
            Cancel("No usable RCS acceleration");
            return 0;
        }
        double activeTime = Math.Min(seconds, DurationSeconds - Elapsed);
        double dv = Math.Min(Math.Min(MaxAcceleration, availableAcceleration) * activeTime,
            MaxDeltaV - CommandedDeltaV);
        Elapsed += activeTime;
        CommandedDeltaV += dv;
        if (Elapsed >= DurationSeconds - 1e-9 || CommandedDeltaV >= MaxDeltaV - 1e-9)
        {
            Active = false;
            Status = "Test pulse complete; ship is still moving";
        }
        else Status = "Test pulse running";
        // Native physics integrates the whole step. Average a fractional final pulse over it.
        return dv / seconds;
    }

    public void Cancel(string reason) { Active = false; Status = reason; }
    public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

public static class TestSavePolicy
{
    public const string Prefix = "PhobosApproachAssistTest";
    public static bool Allows(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (name!.StartsWith("autosave_", StringComparison.Ordinal))
        {
            int split = name.IndexOf('_', 9);
            if (split < 0 || !int.TryParse(name.Substring(9, split - 9), out int count) || count < 0)
                return false;
            name = name.Substring(split + 1);
        }
        return name == Prefix || name.StartsWith(Prefix + "-", StringComparison.Ordinal);
    }
}
