using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Observations;
using UnityEngine;

namespace PhobosShipbreaker;

/// <summary>Content-owned instruments and shared, checked access for panel and F3. No machinery mutations.</summary>
internal static class IndustryObservations
{
    internal const double ProbeMaximumAgeSeconds = 5;
    private const float DiscoveryIntervalSeconds = 1;
    private sealed class Roster { internal float Next; internal CondOwner[] Sources = Array.Empty<CondOwner>(); }
    private sealed class Evidence
    {
        internal Observation[] Probes = Array.Empty<Observation>();
        internal string StopReason = "", StopShip = "";
        internal double StoppedAt;
        internal Observation[] AtStop = Array.Empty<Observation>();
    }
    private static ConditionalWeakTable<Ship, Roster> rosters = new ConditionalWeakTable<Ship, Roster>();
    private static ConditionalWeakTable<CondOwner, Evidence> evidence = new ConditionalWeakTable<CondOwner, Evidence>();

    internal static bool TryRead(ConsoleBinding binding, out EquipmentCard[] cards, out string problem)
    {
        cards = Array.Empty<EquipmentCard>();
        var console = CollectorService.Resolve(binding.ConsoleId);
        problem = console == null ? Text.Get("Industry.missing") : ControlAuthority.Check(console, binding) ?? "";
        if (problem.Length != 0) return false;
        var ship = console!.ship;
        var roster = rosters.GetValue(ship, _ => new Roster());
        if (Time.unscaledTime >= roster.Next)
        {
            roster.Sources = ship.GetCOs(null, false, false, true).Where(c => c != null && !c.bDestroyed && c.ship == ship &&
                c.objCOParent == null && c.HasCond("IsInstalled") && (NativeRoomAlarms.Kind(c) != null || ProcessingService.IsReclaimer(c))).ToArray();
            roster.Next = Time.unscaledTime + DiscoveryIntervalSeconds;
        }
        var result = new List<EquipmentCard>();
        foreach (var source in roster.Sources)
        {
            // Revalidate cached membership on every read; docking never enlarges this roster.
            if (source == null || source.bDestroyed || source.ship != ship || source.objCOParent != null || !source.HasCond("IsInstalled")) continue;
            try
            {
                if (ProcessingService.IsReclaimer(source)) result.AddRange(ReadProbes(source).Select(o => Card(source, o)));
                else if (NativeRoomAlarms.Kind(source) != null) result.Add(Card(source, NativeRoomAlarms.Read(source)));
            }
            catch (Exception)
            {
                // Unsupported provider changes remain an instrument fault, not a broken console.
                result.Add(Card(source, new Observation(source.strID, ship.strRegID, "", "adapter", "instrument_adapter", "", null, null, null,
                    ObservationValidity.Faulty, "adapter_fault")));
            }
        }
        cards = result.OrderBy(c => c.Group, StringComparer.Ordinal).ThenBy(c => c.Id, StringComparer.Ordinal).ToArray();
        return true;
    }

    internal static Observation[] ReadProbes(CondOwner source)
    {
        if (!ProcessingService.IsReclaimer(source)) return Array.Empty<Observation>();
        var state = evidence.GetValue(source, _ => new Evidence());
        var room = NativeRoomAlarms.MonitoredRoom(source, "use", false);
        string ship = source.ship?.strRegID ?? "unknown", subject = room?.strID ?? "";
        string? problem = NativeRoomAlarms.HardwareProblem(source);
        if (room?.GasContainer == null) problem = "room_scope";
        double now = StarSystem.fEpoch;
        var readings = new List<Observation>();
        foreach (string kind in new[] { "room_temperature", "room_pressure" })
        {
            string unit = kind == "room_temperature" ? "degC" : "kPa";
            if (problem == null)
            {
                double value = kind == "room_temperature" ? room!.GetCondAmount("StatGasTemp") - Units.CelsiusToKelvin : room!.GetCondAmount("StatGasPressure");
                if (Observation.Finite(value) && Observation.Finite(now))
                { readings.Add(new Observation(source.strID, ship, subject, kind, "reclaimer_cooling_probe", unit, value, null, now, ObservationValidity.Current)); continue; }
                problem = "invalid_value";
            }
            var last = state.Probes.FirstOrDefault(o => o.Kind == kind && o.ShipId == ship && o.SubjectId == subject && o.ObservedAt <= now);
            var validity = problem == "damaged" || problem == "invalid_value" ? ObservationValidity.Faulty : ObservationValidity.Unavailable;
            readings.Add(last != null ? last.WithValidity(validity, problem!) : new Observation(source.strID, ship, subject, kind,
                "reclaimer_cooling_probe", unit, null, null, null, validity, problem!));
        }
        state.Probes = readings.ToArray();
        return state.Probes;
    }

    internal static void RecordStop(CondOwner source, string reason)
    {
        try
        {
            var state = evidence.GetValue(source, _ => new Evidence());
            // A continuing identical interlock must not refresh the timestamp or erase its first evidence.
            if (state.StopReason == reason && state.StopShip == source.ship?.strRegID) return;
            state.StopReason = reason; state.StopShip = source.ship?.strRegID ?? ""; state.StoppedAt = StarSystem.fEpoch;
            state.AtStop = ReadProbes(source).ToArray();
        }
        catch { /* Diagnostics must not prevent the actual stop or change material/heat state. */ }
    }
    internal static void ClearStop(CondOwner source)
    {
        if (evidence.TryGetValue(source, out var state)) { state.StopReason = ""; state.AtStop = Array.Empty<Observation>(); }
    }
    internal static string ExplainStop(CondOwner source)
    {
        if (!evidence.TryGetValue(source, out var state) || state.StopReason.Length == 0 || state.StopShip != source.ship?.strRegID || StarSystem.fEpoch < state.StoppedAt) return "";
        return "\n\n" + Text.Get("Observations.last_stop", state.StopReason, StarSystem.fEpoch - state.StoppedAt) +
            string.Concat(state.AtStop.Select(o => "\n" + Text.Get("Observations.stop_probe", Text.Get("Observations.kind_" + o.Kind), Value(o), Text.Get("Observations.validity_" + o.Validity), o.SourceId, o.SubjectId) +
                (o.ObservedAt.HasValue ? Text.Get("Observations.evidence_age", Math.Max(0, state.StoppedAt - o.ObservedAt.Value)) : ""))) + "\n" + Text.Get("Observations.stop_note");
    }
    internal static string ProbeDetails(CondOwner source) => string.Join("\n\n", ReadProbes(source).Select(o => Describe(source, o)));

    private static EquipmentCard Card(CondOwner source, Observation observation) => new EquipmentCard
    {
        Id = source.strID + "/" + observation.Kind, Instrument = true,
        Name = Text.Get("Observations.kind_" + observation.Kind) + " — " + source.strNameFriendly,
        Group = observation.Capability == "native_alarm_output" ? "alarm" : "probe", Detail = Describe(source, observation),
        InstrumentStatus = Text.Get("Observations.validity_" + observation.Validity) + (observation.Validity == ObservationValidity.Current ? " — " + Value(observation) : ""),
        SearchScope = observation.SubjectId,
        Attention = observation.NeedsAttention,
        State = observation.Validity != ObservationValidity.Current ? EquipmentState.Unavailable : observation.NeedsAttention ? EquipmentState.Blocked : EquipmentState.Ready
    };
    private static string Value(Observation o) => o.Value.HasValue ? Text.Get("Observations.number", o.Value.Value, o.Unit == "degC" ? "°C" : o.Unit) :
        o.Alarm.HasValue ? Text.Get("Observations.alarm_" + o.Alarm) : Text.Get("Observations.unknown");
    internal static string Describe(CondOwner source, Observation o)
    {
        var checkedReading = o.Assess(source.ship?.strRegID ?? "", o.SubjectId, StarSystem.fEpoch,
            o.Capability == "native_alarm_output" ? NativeRoomAlarms.MaximumAgeSeconds : ProbeMaximumAgeSeconds);
        string status = Text.Get("Observations.validity_" + checkedReading.Validity);
        string value = checkedReading.Validity == ObservationValidity.Current ? Value(checkedReading) :
            Text.Get("Observations.unknown") + (checkedReading.ObservedAt.HasValue ? Text.Get("Observations.last_known", Value(checkedReading)) : "");
        return Text.Get("Observations.detail", Text.Get("Observations.kind_" + o.Kind), value, status, source.strNameFriendly, o.SourceId,
            o.ShipId, o.SubjectId.Length == 0 ? Text.Get("Observations.unknown") : o.SubjectId,
            checkedReading.ObservedAt.HasValue ? Text.Get("Observations.age", Math.Max(0, StarSystem.fEpoch - checkedReading.ObservedAt.Value)) : Text.Get("Observations.never")) +
            "\n" + Text.Get("Observations.capability_" + o.Capability) +
            (checkedReading.Reason.Length == 0 ? "" : "\n" + Text.Get("Observations.reason_" + checkedReading.Reason));
    }
    internal static void Reset()
    { rosters = new ConditionalWeakTable<Ship, Roster>(); evidence = new ConditionalWeakTable<CondOwner, Evidence>(); }
}
