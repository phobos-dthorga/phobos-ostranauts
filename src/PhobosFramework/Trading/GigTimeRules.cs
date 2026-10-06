using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Trading;

/// <summary>Fair gig deadlines (Framework 0.123.0; owner direction, 6 October 2026), with no game types, so the
/// offline checks run them. The game gives a far gig 70 hours per AU in a straight line, whatever ship takes it, which
/// is quicker than its own long-range ferry (80 hours per AU). Here the time allowed is what the player's own
/// torch-equipped ships would need: each ship's trip time on the game's planner, averaged across them, times a margin,
/// plus time to undock, dock and turn in. It is a minimum: the game's own allowance stands when it is longer, and a
/// player with no torch ship keeps the game's allowance.</summary>
public static class GigTimeRules
{
    /// <summary>The game's allowance per AU for a far gig, and its own long-range ferry's (both read from its code).</summary>
    public const double GameHoursPerAu = 70, FerryHoursPerAu = 80;
    /// <summary>The game counts a gig as far beyond 5,000 km; nearer ones keep their template's own time.</summary>
    public const double FarAu = 3.342293712194078E-05;
    public const double DefaultMargin = 1.25, MinMargin = 1, MaxMargin = 3;
    public const double DefaultDockingHours = 4, MinDockingHours = 0, MaxDockingHours = 48;

    /// <summary>The hours a gig allows: the game's own hours, or the fleet's average trip with margin and docking time
    /// when that is longer. Trips that cannot be made (no acceleration) do not count.</summary>
    public static double Allowance(double gameHours, IEnumerable<double> tripSeconds, double margin, double dockingHours)
    {
        var trips = (tripSeconds ?? Enumerable.Empty<double>()).Where(t => t > 0 && !double.IsInfinity(t) && !double.IsNaN(t)).ToList();
        if (trips.Count == 0 || !(gameHours > 0)) return gameHours;
        double fair = trips.Average() / 3600 * Clamp(margin, MinMargin, MaxMargin, DefaultMargin) + Clamp(dockingHours, MinDockingHours, MaxDockingHours, DefaultDockingHours);
        return Math.Max(gameHours, fair);
    }

    /// <summary>The game's long-range ferry time for a distance, for the player with no torch ship.</summary>
    public static double FerryHours(double au) => Math.Max(0, au) * FerryHoursPerAu;

    private static double Clamp(double value, double min, double max, double fallback) => double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Min(max, Math.Max(min, value));
}
