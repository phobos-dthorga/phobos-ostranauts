using System;

namespace Phobos.Ostranauts.Framework.Observations;

public enum ObservationValidity { Current, Stale, Unavailable, Faulty }
public enum AlarmState { Clear, Warning, Alert, Cold, Hot }

/// <summary>Immutable evidence, not permission to control equipment. Times are native simulation seconds.</summary>
public sealed class Observation
{
    public string SourceId { get; }
    public string ShipId { get; }
    public string SubjectId { get; }
    public string Kind { get; }
    public string Capability { get; }
    public string Unit { get; }
    public double? Value { get; }
    public AlarmState? Alarm { get; }
    public double? ObservedAt { get; }
    public ObservationValidity Validity { get; }
    public string Reason { get; }
    public bool NeedsAttention => Validity != ObservationValidity.Current || Alarm.HasValue && Alarm != AlarmState.Clear;

    public Observation(string sourceId, string shipId, string subjectId, string kind, string capability,
        string unit, double? value, AlarmState? alarm, double? observedAt, ObservationValidity validity, string reason = "")
    {
        if (string.IsNullOrEmpty(sourceId) || string.IsNullOrEmpty(shipId) || string.IsNullOrEmpty(kind) || string.IsNullOrEmpty(capability))
            throw new ArgumentException("Observation identity and capability are required.");
        if (value.HasValue && !Finite(value.Value) || observedAt.HasValue && !Finite(observedAt.Value) || value.HasValue && alarm.HasValue)
            throw new ArgumentException("Observation values and times must be finite, with only one value type.");
        if (validity == ObservationValidity.Current && (string.IsNullOrEmpty(subjectId) || !observedAt.HasValue || !value.HasValue && !alarm.HasValue))
            throw new ArgumentException("Current observations require a subject, value and observation time.");
        SourceId = sourceId; ShipId = shipId; SubjectId = subjectId; Kind = kind; Capability = capability;
        Unit = unit; Value = value; Alarm = alarm; ObservedAt = observedAt; Validity = validity; Reason = reason;
    }

    /// <summary>Never lets aging, moving a source, or a reversed clock turn old evidence into a live reading.</summary>
    public Observation Assess(string shipId, string subjectId, double now, double maximumAgeSeconds)
    {
        if (!Finite(maximumAgeSeconds) || maximumAgeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(maximumAgeSeconds));
        if (shipId != ShipId || subjectId != SubjectId || !Finite(now) || ObservedAt.HasValue && now < ObservedAt.Value)
            return WithoutValue("scope_or_clock_changed");
        if (Validity == ObservationValidity.Current && now - ObservedAt!.Value > maximumAgeSeconds)
            return WithValidity(ObservationValidity.Stale, "expired");
        return this;
    }
    public Observation WithValidity(ObservationValidity validity, string reason) =>
        new Observation(SourceId, ShipId, SubjectId, Kind, Capability, Unit, Value, Alarm, ObservedAt, validity, reason);
    public Observation WithoutValue(string reason) => new Observation(SourceId, ShipId, SubjectId, Kind, Capability,
        Unit, null, null, null, ObservationValidity.Unavailable, reason);
    public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
