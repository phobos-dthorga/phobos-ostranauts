using System;
using System.Collections.Generic;
using System.Linq;

namespace PhobosAutoNav.Core;

// A switched-off sensor type and what it would add to each needed contact's native signal.
internal sealed class SensorOption
{
    internal readonly string Key, Name;
    internal readonly bool Emits;
    internal readonly double[] Gains;
    internal SensorOption(string key, string name, bool emits, double[] gains) { Key = key; Name = name; Emits = emits; Gains = gains; }
}

// A contact below the signal navigation needs.
internal readonly struct SensorNeed
{
    internal readonly double Current, Required;
    internal SensorNeed(double current, double required) { Current = current; Required = required; }
}

internal enum SensorAutoEngage { All, Passive, Off }

internal static class SensorSelection
{
    // Authored headroom above the native detection threshold, so a contact near the edge is
    // restored firmly rather than flickering back to a weak reading.
    internal const double TrackMargin = 1.2;
    // Options are native sensor types (five in 1.0.1.5), so the exhaustive search stays tiny.
    private const int MaximumOptions = 8;

    internal static SensorNeed Need(double signal, double threshold) =>
        new(ArrivalBrake.Finite(signal) && signal > 0 ? signal : 0, threshold * TrackMargin);

    /// <summary>The fewest sensor types that lift every contact some combination can lift, using
    /// non-emitting sensors wherever they are enough. Contacts no combination can reach are not a
    /// reason to switch anything on. Returns nothing when nothing would help.</summary>
    internal static IReadOnlyList<SensorOption> Choose(IReadOnlyList<SensorOption> options, IReadOnlyList<SensorNeed> needs, SensorAutoEngage mode)
    {
        if (mode == SensorAutoEngage.Off || needs.Count == 0) return Array.Empty<SensorOption>();
        var usable = options.Where(o => o.Gains.Length == needs.Count && o.Gains.All(g => ArrivalBrake.Finite(g) && g >= 0) &&
            (mode == SensorAutoEngage.All || !o.Emits)).Take(MaximumOptions).ToArray();
        var reachable = Enumerable.Range(0, needs.Count).Where(i => ArrivalBrake.Finite(needs[i].Required) && needs[i].Required > 0 &&
            needs[i].Current < needs[i].Required && needs[i].Current + usable.Sum(o => o.Gains[i]) >= needs[i].Required).ToArray();
        if (reachable.Length == 0) return Array.Empty<SensorOption>();
        SensorOption[]? best = null;
        (int Emitting, int Count, double Gain) bestKey = default;
        for (int mask = 1; mask < 1 << usable.Length; mask++)
        {
            var chosen = usable.Where((_, index) => (mask & 1 << index) != 0).ToArray();
            if (!reachable.All(i => needs[i].Current + chosen.Sum(o => o.Gains[i]) >= needs[i].Required)) continue;
            // Ties go to the larger gain on contacts that can actually be restored.
            (int Emitting, int Count, double Gain) key = (chosen.Count(o => o.Emits), chosen.Length, chosen.Sum(o => reachable.Sum(i => o.Gains[i])));
            if (best == null || key.Emitting < bestKey.Emitting || key.Emitting == bestKey.Emitting &&
                (key.Count < bestKey.Count || key.Count == bestKey.Count && key.Gain > bestKey.Gain))
            { best = chosen; bestKey = key; }
        }
        return best ?? Array.Empty<SensorOption>();
    }
}
