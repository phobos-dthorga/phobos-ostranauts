using System;
using System.Globalization;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private static float ReadThrottle(CondOwner? co) => ReadThrottleReading(co) ?? 0;

    // Guidance fails closed to zero; instruments retain the distinction from a measured zero. The slider
    // value is mapped the way the game maps it for the pilot's own RCS commands (MathUtils.ExpMap).
    private static float? ReadThrottleReading(CondOwner? co)
    {
        if (co != null && co.mapGUIPropMaps.TryGetValue("Panel A", out var props) &&
            props.TryGetValue("slidThrottle", out var text) &&
            float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && ArrivalBrake.Finite(value))
            return MathUtils.ExpMap(Math.Max(0, Math.Min(1, value)));
        return null;
    }

    /// <summary>The last guidance step the game actually ran, for admission checks; the configured cap until one is known.</summary>
    internal double LastStepSeconds { get; private set; } = double.NaN;
    private double AdmissionStep => ArrivalBrake.Finite(LastStepSeconds) && LastStepSeconds > 0 ? Math.Min(LastStepSeconds, Plugin.MaximumStepSeconds.Value) : Plugin.MaximumStepSeconds.Value;

    private string? AdmissionProblem(CondOwner co, TargetRef target, double arrivalKM, double arrivalMS)
    {
        if (!AutoNavCore.TryReadAdmission(co.ship, target, arrivalKM, arrivalMS, ReadThrottle(co),
            AdmissionStep, out var room)) return Text.Get("Approach.unavailable");
        return room.Safe ? null : Text.Get("Approach.braking_room", room.RequiredM / 1000, room.AvailableM / 1000);
    }
}
