using System;
using Phobos.Ostranauts.Framework;

/// <summary>Framework 0.127.0: the shared game calendar against the game's own <c>CrewSim</c> and <c>MathUtils</c>.</summary>
internal static class GameClockNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(Math.Abs(CrewSim.SEC_PER_DAY - GameClock.DaySeconds) < 1e-6, "GameClock's day is the game's SEC_PER_DAY");
        int mismatches = 0; long previous = GameClock.ShiftCount(0); int changes = 0, counted = 0;
        for (double t = 0; t < 2.2 * GameClock.YearSeconds; t += 977)
        {
            if (MathUtils.GetShiftFromS(t) != GameClock.Shift(t)) mismatches++;
            long count = GameClock.ShiftCount(t);
            if (MathUtils.GetShiftFromS(t) != MathUtils.GetShiftFromS(t - 977) && t > 0) changes++;
            counted += (int)(count - previous); previous = count;
        }
        check(mismatches == 0, "GameClock numbers every shift as MathUtils.GetShiftFromS does: " + mismatches + " differ");
        check(counted == changes, "The shift count rises exactly as often as the game's shift number changes: " + counted + " against " + changes);
        foreach (double t in new[] { 0.0, 50000, GameClock.DaySeconds * 17.5, GameClock.YearSeconds * 1.3 })
            check(MathUtils.GetHourFromS(t) == (int)(t % GameClock.YearSeconds % GameClock.DaySeconds / GameClock.HourSeconds) &&
                  MathUtils.GetYearFromS(t) == (int)(t / GameClock.YearSeconds), "Hours and years follow the game at " + t);
    }
}
