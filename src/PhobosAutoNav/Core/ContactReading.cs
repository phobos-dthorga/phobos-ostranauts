using System;

namespace PhobosAutoNav.Core;

// A qualification result, never a position cache or a claim of measured accuracy.
internal enum ContactState { Unavailable, Updating, NoSensors, Occluded, Weak, Fault, Ready }

internal readonly struct ContactReading
{
    internal readonly ContactState State;
    internal readonly double Signal, Threshold;
    internal bool Usable => State == ContactState.Ready;
    internal string MessageKey => "Sensors." + State;
    internal ContactReading(ContactState state, double signal = double.NaN, double threshold = double.NaN)
    { State = state; Signal = signal; Threshold = threshold; }
}

internal static class ContactRules
{
    // Native 1.0.1.5 ElectronicSystems.UpdateDetectionThreshold, read without
    // changing shared native state or retaining a previous operator's benefit.
    internal const float DefaultThreshold = 0.3f, SkilledThreshold = 0.25f;
    internal const int PlaceholderBodyDrawFlag = 1;
    internal static ContactReading Evaluate(double signal, double threshold) =>
        !ArrivalBrake.Finite(signal) || signal < 0 || !ArrivalBrake.Finite(threshold) || threshold <= 0
            ? new ContactReading(ContactState.Fault)
            : new ContactReading(signal >= threshold ? ContactState.Ready : ContactState.Weak, signal, threshold);

    // Native 1.0.1.5 GUIOrbitDraw.StellarObjectVisible: asteroid markers beyond this range are
    // only partial contacts, and their fixed threshold has no operator-skill benefit.
    internal const double StellarRangeKM = 1000;
    internal static ContactReading EvaluateStellar(double signal, double rangeKM)
    {
        if (!ArrivalBrake.Finite(rangeKM) || rangeKM < 0) return new ContactReading(ContactState.Fault);
        var reading = Evaluate(signal, DefaultThreshold);
        return reading.State == ContactState.Ready && rangeKM > StellarRangeKM
            ? new ContactReading(ContactState.Weak, reading.Signal, reading.Threshold) : reading;
    }
}

// Which sensed objects local avoidance treats as hazards. A weak contact is still sensed:
// the native map draws it up to one fifth of its range from its true position.
internal static class HazardRules
{
    internal const double PartialErrorFraction = .2;
    // Authored reach: beyond this a weak contact's native position error exceeds 20 km,
    // too coarse to place usefully in local avoidance.
    internal const double WeakHazardRangeKM = 100;
    internal static bool Tracked(ContactState state) => state == ContactState.Ready || state == ContactState.Weak;
    // Extra clearance in metres, or null when the object is not a local hazard.
    internal static double? UncertaintyM(ContactState state, double rangeKM) =>
        !ArrivalBrake.Finite(rangeKM) || rangeKM < 0 ? null :
        state == ContactState.Ready ? 0 :
        state == ContactState.Weak && rangeKM <= WeakHazardRangeKM ? rangeKM * 1000 * PartialErrorFraction : null;
}

// Explicit sensor switch-on from the navigation console. Active sensors emit and are opt-in.
internal static class SensorSuiteRules
{
    internal static bool ShouldSwitchOn(bool on, bool active, bool includeActive) => !on && (includeActive || !active);
}
