using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Observations;

/// <summary>Read-only native alarm-output adapter. Does not run sensors, switch alarms or expose gas concentrations.</summary>
public static class NativeRoomAlarms
{
    public const double MaximumAgeSeconds = 5;
    private sealed class Witness
    {
        internal string Ship = "", Room = "", Point = "";
        internal double Epoch;
        internal int Frame;
        internal bool Valid;
        internal Observation? Last;
    }
    private static ConditionalWeakTable<CondOwner, Witness> witnesses = new ConditionalWeakTable<CondOwner, Witness>();
    private static readonly string[] Families = { "O2", "N2", "CO2", "Smoke", "Contaminants", "Temp" };
    public static string? Kind(CondOwner source) => Families.FirstOrDefault(f => source.HasCond("IsAlarm" + f));

    public static string? HardwareProblem(CondOwner source)
    {
        if (source.bDestroyed || source.objCOParent != null || !source.HasCond("IsInstalled") || source.ship == null || (int)source.ship.LoadState < 2) return "not_installed";
        if (source.HasCond("IsDamaged")) return "damaged";
        if (source.HasCond("IsLocked") || source.HasCond("IsOff") || source.HasCond("IsSignalOff") || source.HasCond("IsOverrideOff")) return "disabled";
        return source.HasCond("IsPowered") ? null : "no_power";
    }

    /// <summary>Also checks the dock-inclusive query used by native gas alarms; ambiguous geometry fails closed.</summary>
    public static CondOwner? MonitoredRoom(CondOwner source, string point, bool checkNeighbours)
    {
        var ship = source.ship;
        if (ship == null) return null;
        var room = ship.GetRoomAtWorldCoords1(source.GetPos(point), false)?.CO;
        if (room == null || room.ship != ship) return null;
        if (checkNeighbours)
        {
            var objects = new List<CondOwner>();
            ship.GetCOsAtWorldCoords1(source.GetPos(point), null, true, false, objects);
            if (objects.Any(c => c != null && c.ship != null && c.ship != ship)) return null;
        }
        return room;
    }

    // A witness is made only by a real native evaluation, including when our panel is closed.
    internal static void Evaluated(CondOwner source, string point)
    {
        try
        {
            if (source == null || Kind(source) == null) return;
            if (HardwareProblem(source) != null) { if (witnesses.TryGetValue(source, out var previous)) previous.Valid = false; return; }
            var room = MonitoredRoom(source, point, true);
            if (room == null) { witnesses.Remove(source); return; }
            var w = witnesses.GetValue(source, _ => new Witness());
            if (w.Ship != source.ship.strRegID || w.Room != room.strID || StarSystem.fEpoch < w.Epoch) w.Last = null;
            else CompletePending(source, w);
            w.Ship = source.ship.strRegID; w.Room = room.strID; w.Point = point;
            w.Epoch = StarSystem.fEpoch; w.Frame = Time.frameCount; w.Valid = true;
        }
        catch { witnesses.Remove(source); } // An instrumentation failure never interrupts native sensing.
    }

    public static Observation Read(CondOwner source)
    {
        string kind = Kind(source) ?? throw new ArgumentException("Unsupported native room alarm.");
        string ship = source.ship?.strRegID ?? "unknown";
        witnesses.TryGetValue(source, out var w);
        var room = MonitoredRoom(source, w?.Point ?? "RoomA", true);
        string subject = room?.strID ?? "";
        Observation Missing(string reason, bool faulty = false) => new Observation(source.strID, ship, subject, kind,
            "native_alarm_output", "", null, null, null, faulty ? ObservationValidity.Faulty : ObservationValidity.Unavailable, reason);
        string? problem = HardwareProblem(source);
        if (room == null) { if (w != null) w.Valid = false; return Missing("room_scope"); }
        if (w != null && (w.Ship != ship || w.Room != subject || StarSystem.fEpoch < w.Epoch)) { witnesses.Remove(source); w = null; }
        if (problem != null)
        {
            if (w != null) w.Valid = false;
            return w?.Last != null ? w.Last.WithValidity(problem == "damaged" ? ObservationValidity.Faulty : ObservationValidity.Unavailable, problem) : Missing(problem, problem == "damaged");
        }
        if (w == null || !w.Valid) return Missing("awaiting_native");
        // Native Run queues transitions; do not read the previous lamp in that same frame.
        CompletePending(source, w);
        return w.Last?.Assess(ship, subject, StarSystem.fEpoch, MaximumAgeSeconds) ?? Missing("awaiting_native");
    }
    private static void CompletePending(CondOwner source, Witness w)
    {
        if (!w.Valid || w.Frame >= Time.frameCount) return;
        string? kind = Kind(source);
        AlarmState? alarm = source.HasCond("IsRed") ? kind == "Temp" ? AlarmState.Hot : AlarmState.Alert :
            source.HasCond("IsYellow") ? AlarmState.Warning : source.HasCond("IsBlue") && kind == "Temp" ? AlarmState.Cold :
            source.HasCond("IsGreen") || source.HasCond("IsWhite") && kind == "Temp" ? AlarmState.Clear : (AlarmState?)null;
        if (kind == null) return;
        if (w.Last?.ObservedAt == w.Epoch) return;
        w.Last = new Observation(source.strID, w.Ship, w.Room, kind, "native_alarm_output", "", null, alarm, w.Epoch,
            alarm.HasValue ? ObservationValidity.Current : ObservationValidity.Faulty, alarm.HasValue ? "" : "unknown_output");
    }

    public static void Reset() => witnesses = new ConditionalWeakTable<CondOwner, Witness>();
}

[HarmonyPatch(typeof(GasPressureSense), nameof(GasPressureSense.Run))]
internal static class PressureObservationPatch
{
    private static void Prefix(CondOwner ___co, string ___strSignalCond, out bool __state) => __state = ___co != null && ___co.HasCond(___strSignalCond);
    private static void Postfix(CondOwner ___co, string ___strPoint, bool __state) { if (__state) NativeRoomAlarms.Evaluated(___co, ___strPoint); }
}
[HarmonyPatch(typeof(Sensor), nameof(Sensor.Run))]
internal static class SensorObservationPatch
{
    private static void Postfix(CondOwner ___coUs, string ___strPoint) => NativeRoomAlarms.Evaluated(___coUs, ___strPoint);
}
[HarmonyPatch]
internal static class ObservationReloadPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods().Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => NativeRoomAlarms.Reset();
}
