using System;
using System.Globalization;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static float ReadThrottle(CondOwner? co) => ReadThrottleReading(co) ?? 0;

    // Guidance fails closed to zero; instruments retain the distinction from a measured zero.
    private static float? ReadThrottleReading(CondOwner? co)
    {
        if (co != null && co.mapGUIPropMaps.TryGetValue("Panel A", out var props) &&
            props.TryGetValue("slidThrottle", out var text) &&
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && ArrivalBrake.Finite(value))
            return Math.Max(0, Math.Min(1, value));
        return null;
    }

    private static string? AdmissionProblem(CondOwner co, TargetRef target, double arrivalKM, double arrivalMS)
    {
        if (!AutoNavCore.TryReadAdmission(co.ship, target, arrivalKM, arrivalMS, ReadThrottle(co),
            Plugin.MaximumStepSeconds.Value, out var room)) return Text.Get("Approach.unavailable");
        return room.Safe ? null : Text.Get("Approach.braking_room", room.RequiredM / 1000, room.AvailableM / 1000);
    }
}
