using System;

namespace Phobos.Ostranauts.Framework;

/// <summary>The game's calendar (Framework 0.127.0), as its own <c>MathUtils</c> counts it: hours of 3,600 seconds, a
/// day of 87,658.125 seconds (<c>CrewSim.SEC_PER_DAY</c>, about 24.35 hours, so the game has an hour 24), four shifts a
/// day of six hours each with the last running long, and a year of 31,556,926 seconds. No game types, so offline checks
/// use it; a native check compares it with the game.</summary>
public static class GameClock
{
    public const double HourSeconds = 3600, ShiftSeconds = 21600, DaySeconds = 87658.125, YearSeconds = 31556926, MonthSeconds = 2629743.75;
    public const int ShiftsPerDay = 4, DaysPerYear = 360;

    /// <summary>Game days in a span of game seconds.</summary>
    public static double Days(double seconds) => seconds / DaySeconds;
    /// <summary>Game seconds in a number of game days.</summary>
    public static double Seconds(double days) => days * DaySeconds;

    /// <summary>The shift of the day an epoch falls in (1 to 4), as the game's <c>MathUtils.GetShiftFromS</c> numbers it.</summary>
    public static int Shift(double epoch)
    {
        int hour = (int)(epoch % YearSeconds % DaySeconds / HourSeconds);
        return Math.Min(ShiftsPerDay - 1, hour / 6) + 1;
    }

    /// <summary>A count that rises by one at every shift change the game makes: the day number times four plus the
    /// shift. Two epochs whose counts differ by <c>n</c> are <c>n</c> shift changes apart, the game's own boundaries
    /// (where it raises instalments and late fees), whatever the clock did in between.</summary>
    public static long ShiftCount(double epoch)
    {
        if (double.IsNaN(epoch) || double.IsInfinity(epoch) || epoch < 0) return 0;
        long year = (long)(epoch / YearSeconds);
        double inYear = epoch - year * YearSeconds;
        long day = (long)(inYear / DaySeconds);
        // A game year is 360 days and one second. That last second is day 361 to the game, in shift 1, and the new
        // year's first shift follows with no shift change between them, so the second counts as the next year's first
        // shift: 360 days of four shifts a year, the 361st day landing on the next year's count.
        return (year * DaysPerYear + day) * ShiftsPerDay + Shift(epoch) - 1;
    }
}
