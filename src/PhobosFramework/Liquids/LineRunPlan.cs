using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>What the two-second top-up knows about one run since it last read it (Framework 0.133.0, L103).</summary>
internal enum LineRunState
{
    /// <summary>Not read since its layout was built or one of its segments was written: read every record.</summary>
    Unknown,
    /// <summary>Every open segment was full: nothing to do until a segment is written or the layout changes.</summary>
    Full,
    /// <summary>A segment wanted more but its stores gave nothing: read again only when what they offer changes.</summary>
    Wanting
}

/// <summary>What one pass does with a run.</summary>
internal enum LineRunStep { Skip, Sources, Read }

/// <summary>When the top-up reads a run again (Framework 0.133.0, L103; owner performance review, 8 October 2026). Until
/// then every pass read the saved record of every segment on every run with a store, every two real seconds, about 1.1 ms
/// a pass on the owner's save, although a full run stays full: a segment's contents change only when Framework writes
/// its record (a top-up, a pump, a drain, a vent, damage), and each write marks its run Unknown. A run whose stores had
/// nothing to give is not read again until they offer something different; that also settles a run its stores cannot
/// fill any further, such as a gap smaller than the planner's smallest draw. The layout is the route cache's, so a
/// segment laid, removed, damaged or repaired builds the runs afresh, and a full read of every run every
/// <see cref="RouteRecheck.FullSeconds"/> catches anything else. Pure: the adapter reads the records and the stores.</summary>
internal static class LineRunPlan
{
    internal static LineRunStep Next(LineRunState state) => state switch
    {
        LineRunState.Full => LineRunStep.Skip,
        LineRunState.Wanting => LineRunStep.Sources,
        _ => LineRunStep.Read
    };

    /// <summary>Where a run stands after a read: full, or filled (read again, since its records changed), or wanting with
    /// nothing drawn.</summary>
    internal static LineRunState After(bool wanting, bool drew) =>
        !wanting ? LineRunState.Full : drew ? LineRunState.Unknown : LineRunState.Wanting;

    /// <summary>Whether the runs must be built afresh and every run read: never built, or the safety net has run out
    /// (an unreadable clock counts as run out).</summary>
    internal static bool FullDue(double now, double builtAt) => !(now - builtAt < RouteRecheck.FullSeconds);

    /// <summary>Whether a wanting run's stores offer something other than when it was last read: a commodity appeared or
    /// went, or an amount moved by more than <paramref name="tolerance"/> kilograms.</summary>
    internal static bool Changed(IReadOnlyDictionary<string, double>? before, IReadOnlyDictionary<string, double> now, double tolerance)
    {
        if (before == null) return true;
        int counted = 0;
        foreach (var pair in now)
        {
            if (!(pair.Value > tolerance)) continue;
            counted++;
            if (!before.TryGetValue(pair.Key, out var was) || !(Math.Abs(was - pair.Value) <= tolerance)) return true;
        }
        int held = 0;
        foreach (var pair in before) if (pair.Value > tolerance) held++;
        return held != counted;
    }
}
