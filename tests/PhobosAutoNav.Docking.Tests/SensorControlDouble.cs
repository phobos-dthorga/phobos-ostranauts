using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Native sensor-control boundary for the shared avoidance guard: when a sensor is offered,
// switching it on turns every surveyed contact into a firm track.
internal static class NativeSensorControl
{
    internal static bool Offer;
    internal static readonly List<string[]> Surveys = new();
    internal static readonly List<string> Notices = new();
    private static string[] surveyed = Array.Empty<string>();
    internal static void Reset() { Offer = false; Surveys.Clear(); Notices.Clear(); surveyed = Array.Empty<string>(); }

    internal static void Survey(Ship own, IReadOnlyList<string> ids, string holder, out SensorNeed[] needs, out SensorOption[] options)
    {
        surveyed = ids.ToArray(); Surveys.Add(surveyed);
        needs = ids.Select(_ => new SensorNeed(0, 1)).ToArray();
        options = Offer ? new[] { new SensorOption("IsSensorIR", "Infrared", false, ids.Select(_ => 2d).ToArray()) } : Array.Empty<SensorOption>();
    }
    internal static IReadOnlyList<SensorOption> Engage(Ship own, IReadOnlyList<SensorOption> chosen, string holder)
    {
        Offer = false;
        foreach (string id in surveyed) NativeContactReader.ById[id] = ContactState.Ready;
        return chosen;
    }
    internal static IReadOnlyList<(string Name, bool Emits)> Release(Ship own, string holder) => Array.Empty<(string, bool)>();
    internal static IReadOnlyList<(string Name, bool Emits)> InUse(Ship? own, string holder) => Array.Empty<(string, bool)>();
    internal static IReadOnlyCollection<string> Holders(Ship own) => Array.Empty<string>();
    internal static bool Decline(Ship own, string condition, string holder) => false;
    internal static void Notify(Ship own, bool caution, string logText, string? banner) => Notices.Add(logText);
}

internal static partial class Plugin { internal static Setting<SensorAutoEngage> AutoEngageSensors = new(SensorAutoEngage.All); }
