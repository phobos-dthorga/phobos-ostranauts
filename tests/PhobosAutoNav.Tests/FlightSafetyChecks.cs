using System;
using PhobosAutoNav.Core;

internal static class FlightSafetyChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (double throttle in new[] { 0, .01, .1, .25, 1d })
        foreach (double x in new[] { -2, -.1, 0, .3, 1d })
        foreach (double y in new[] { -1, 0, .7 })
        foreach (double turn in new[] { -.5, 0, .5 })
        {
            check(RcsBudget.TryLimit(x, y, turn, throttle, RcsBudget.CombinedRotationShare, out var command), "Finite maneuver can be budgeted");
            check(Math.Abs(command.X) + Math.Abs(command.Y) + Math.Abs(command.Turn) <= throttle + 1e-12,
                "All axes including rotation fit the native throttle budget");
            check(Math.Abs(command.X * y - command.Y * x) < 1e-12, "Clamping retains the requested translation direction");
            check(Math.Abs(command.Turn) <= throttle * RcsBudget.CombinedRotationShare + 1e-12,
                "Combined turn leaves the promised translation reserve");
        }
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            check(!RcsBudget.TryLimit(bad, 0, 0, .5, .25, out _), "Invalid maneuver cannot reach native control");
        check(!RcsBudget.TryLimit(1, 1, 1, -1, .25, out _) && !RcsBudget.TryLimit(1, 1, 1, 1.1, .25, out _), "Invalid throttle rejected");
        check(RcsBudget.TryLimit(0, 0, -.5, .1, 1, out var rotation) && Math.Abs(rotation.Turn + .1) < 1e-12,
            "Standalone alignment is bounded by low throttle");

        bool Admit(double range, double vx, double vy, double arrival, double throttle, out BrakingRoom room) =>
            ApproachAdmission.TryEvaluate(0, range, vx, vy, 10, 1000, arrival, .5, throttle, 10, out room);
        check(Admit(2000, 0, 100, 0, 1, out var unsafeRoom) && !unsafeRoom.Safe,
            "Audit regression: ample fuel cannot make a 2 km 100 m/s intercept safe");
        check(unsafeRoom.RequiredM > 10000 && unsafeRoom.AvailableM == 950, "Admission reserves reaction, turn and diagonal braking costs");
        check(Admit(85000, 0, 100, 0, 1, out var far) && far.Safe, "Ample braking distance is accepted");
        check(Admit(500, 0, .2, 0, .1, out var near) && near.Safe, "Slow approach inside arrival band is still usable");
        check(Admit(500, 0, 100, 0, 1, out var fastNear) && !fastNear.Safe, "Inside-ring speed does not bypass hull protection");
        check(Admit(500, 0, -100, 0, .1, out var receding) && receding.Safe, "Receding motion is not treated as closing speed");
        check(Admit(500, 100, 0, 0, .1, out var tangent) && tangent.Safe, "Tangent motion initially moves away from the target hull");
        check(Admit(500, 0, 0, 0, .1, out var stopped) && stopped.Safe, "Already stopped close target is accepted");
        check(Admit(30000, 0, 100, 0, 1, out var full) && Admit(30000, 0, 100, 0, .1, out var low) &&
            full.Safe && !low.Safe, "Selected throttle controls admission capability");
        check(Admit(2000, 0, 20, 20, 1, out var drift) && drift.Safe, "Nonzero arrival uses captured requested speed");
        check(!Admit(10, 0, 0, 0, 1, out _) && !Admit(0, 0, 0, 0, 1, out _), "Overlapping hulls cannot be admitted");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, -1d })
            check(!ApproachAdmission.TryEvaluate(0, 2000, 0, 20, 10, 1000, 0, .5, 1, bad, out _), "Invalid timing cannot establish braking room");
        check(!Admit(2000, 0, 100, 0, 0, out _), "No throttle means no braking capability");
    }
}
