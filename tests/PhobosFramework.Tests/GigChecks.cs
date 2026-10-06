using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Flight;
using Phobos.Ostranauts.Framework.Trading;

/// <summary>Framework 0.123.0 fair gig deadlines (owner direction, 6 October 2026): torch trip times on the game's own
/// planner, and the allowance from the fleet's average, never shorter than the game's. The game's gig code, ships and
/// kiosk text are not exercised here.</summary>
internal static class GigChecks
{
    private const double G = 9.81 / (TorchTrip.KilometresPerAu * 1000); // 1 g in AU/s²

    internal static void Run(Action<bool, string> check)
    {
        // 0.7 AU at 1 g peaks near 1,000 km/s, far below the torch limit (a tenth of light speed): accelerate halfway,
        // brake the rest, about 57 hours, where the game allows 49.
        double au = .7, limit = TorchTrip.SpeedLimitAuPerSecond;
        double trip = TorchTrip.Seconds(au, G);
        check(Math.Abs(trip - 2 * Math.Sqrt(au / G)) < 1e-6 && G * trip / 2 < limit, "A real trip is accelerate-then-brake: " + (trip / 3600).ToString("0.0") + " h");
        check(trip / 3600 > au * GigTimeRules.GameHoursPerAu, "At 1 g a 0.7 AU trip takes longer than the game's gig allowance");
        // An artificially strong drive reaches the limit and coasts in the middle, as the game's planner does.
        double strong = 1e-6;
        double ramp = limit / strong, coast = 2 * ramp + (1 - strong * ramp * ramp) / limit;
        check(Math.Abs(TorchTrip.Seconds(1, strong) - coast) < 1e-6 && TorchTrip.Seconds(1, strong) > 2 * Math.Sqrt(1 / strong), "A drive that reaches the limit coasts at it");
        double hop = .001;
        check(TorchTrip.Seconds(hop, 4 * G) < TorchTrip.Seconds(hop, G) && TorchTrip.Seconds(au, 0) == double.PositiveInfinity && TorchTrip.Seconds(0, G) == 0,
            "More thrust is quicker; no thrust never arrives; no distance takes no time");

        double game = au * GigTimeRules.GameHoursPerAu;
        check(GigTimeRules.Allowance(game, Array.Empty<double>(), 1.25, 4) == game, "With no torch ship the game's own time stands");
        double one = GigTimeRules.Allowance(game, new[] { trip }, 1.25, 4);
        check(Math.Abs(one - Math.Max(game, trip / 3600 * 1.25 + 4)) < 1e-9, "One ship: its trip, a quarter more, and four hours to dock and turn in");
        double slow = TorchTrip.Seconds(au, G / 10);
        double two = GigTimeRules.Allowance(game, new[] { trip, slow }, 1.25, 4);
        check(Math.Abs(two - Math.Max(game, (trip + slow) / 2 / 3600 * 1.25 + 4)) < 1e-9, "Several ships: their average trip");
        check(GigTimeRules.Allowance(500, new[] { 3600.0 }, 1.25, 4) == 500, "Never shorter than the game's own time");
        check(GigTimeRules.Allowance(1, new[] { 3600.0, double.PositiveInfinity, double.NaN, -5 }, 1.25, 4) == 1 * 1.25 + 4, "Trips that cannot be made do not count");
        check(GigTimeRules.Allowance(1, new[] { 3600.0 }, 99, -3) == 3 + 0 && GigTimeRules.Allowance(1, new[] { 3600.0 }, double.NaN, double.NaN) == 1.25 + 4,
            "Settings out of range are held to 1 to 3 times and 0 to 48 hours; nonsense falls back to the defaults");
        check(GigTimeRules.FerryHours(.7) == .7 * 80 && GigTimeRules.GameHoursPerAu < GigTimeRules.FerryHoursPerAu, "The game's own ferry is slower than its gig allowance");
    }
}
