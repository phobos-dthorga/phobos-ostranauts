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
}
