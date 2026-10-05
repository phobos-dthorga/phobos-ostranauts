using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>When a ship's cached pipe layout must be read again (Framework 0.106.0; owner performance review, 5 October
/// 2026). The hooks drop a layout on every physical change: a segment laid, damaged, repaired or removed, a floor or
/// wall, a participant installed or taken away, a line drained or returned. What they cannot see is readiness changing
/// with nothing physical happening: a machine locked or unlocked, a ship changing hands, a ship finishing loading. Until
/// 0.106.0 a full rescan of every object aboard covered that every two seconds, about 8 ms a time on a large ship. Now
/// the snapshot remembers the objects that took part and whether each was ready; every two seconds only those are
/// checked, and a full rescan stays as a safety net every <see cref="FullSeconds"/>. Pure: the caller reads readiness.</summary>
internal static class RouteRecheck
{
    /// <summary>The safety-net rescan, for anything neither the hooks nor the readiness check can see.</summary>
    internal const double FullSeconds = 30;

    /// <summary>Whether a full rebuild is due: never built, the safety net has run out, or a watched object is gone or
    /// changed readiness. <paramref name="readyNow"/> answers null for an object that is gone or left the ship.</summary>
    internal static bool RebuildDue<T>(bool built, double sinceFull, IReadOnlyList<(T Item, bool Ready)> watched, Func<T, bool?> readyNow)
    {
        if (!built || !(sinceFull < FullSeconds)) return true;
        for (int i = 0; i < watched.Count; i++)
            if (readyNow(watched[i].Item) is not bool ready || ready != watched[i].Ready) return true;
        return false;
    }
}
