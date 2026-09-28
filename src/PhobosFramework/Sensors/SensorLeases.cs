using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Ostranauts.Ships.Sensors;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Sensors;

/// <summary>Automation that switches native ship sensors on through the same switch as the native
/// Sensors page, and later switches off only what it switched on. A sensor type the player already
/// has on is never claimed, and a player's own switch always wins. Main thread only.</summary>
public static class SensorLeases
{
    private static readonly AccessTools.FieldRef<ShipSensor, string> UnitId = AccessTools.FieldRefAccess<ShipSensor, string>("_coId");
    private static bool issuing;
    public static Action<string> Log { get; set; } = _ => { };

    /// <summary>Someone else (usually the player on the Sensors page) switched off a sensor type that
    /// carries no lease note. Arguments: the ship and the native sensor condition, such as IsSensorIR.</summary>
    public static event Action<Ship, string>? SwitchedOffByOthers;

    public static CondOwner? UnitOf(ShipSensor sensor)
    {
        string id = UnitId(sensor);
        return !string.IsNullOrEmpty(id) && DataHandler.mapCOs != null && DataHandler.mapCOs.TryGetValue(id, out var unit) &&
            unit != null && !unit.bDestroyed ? unit : null;
    }

    /// <summary>Fitted sensors in the ship's native registry, grouped by type as the Sensors page groups its buttons.</summary>
    public static IEnumerable<IGrouping<string, ShipSensor>> Types(Ship? ship) =>
        (ship?.ElectronicSystems?.aElectronicSystems?.Where(s => s != null && !string.IsNullOrEmpty(s.CondId)).ToArray() ??
            Array.Empty<ShipSensor>()).GroupBy(s => s.CondId, StringComparer.Ordinal);

    /// <summary>What a switched-off sensor would add to a native signal. The native formula runs with the
    /// in-memory switch briefly set and then restored; no condition or saved state is written.</summary>
    public static double IfOn(ShipSensor sensor, ShipSignature signature, double rangeKm, float visibility)
    {
        if (sensor.On) return sensor.GetSignalStrength(signature, rangeKm, visibility);
        int prior = sensor.SensorState;
        try { sensor.SensorState = 1; return sensor.GetSignalStrength(signature, rangeKm, visibility); }
        finally { sensor.SensorState = prior; }
    }

    private static ObjectStateStore Store(CondOwner unit, string condition) =>
        SensorLeaseRecord.Store(unit.mapGUIPropMaps, unit.strID, condition);

    // The native flip reads the saved switch first; refresh so decisions never use a stale in-memory state.
    private static bool Refresh(ShipSensor sensor, out CondOwner unit)
    {
        unit = UnitOf(sensor)!;
        if (unit == null) return false;
        sensor.SetState();
        return true;
    }

    private static void Flip(IEnumerable<ShipSensor> sensors)
    {
        issuing = true;
        try { foreach (var sensor in sensors) sensor.SetState(toggle: true); }
        finally { issuing = false; }
    }

    /// <summary>Switches one sensor type on for a holder, notes the lease on every unit, and returns true
    /// only when every sensor of that type is now on. Refuses when any is already on (it is the player's),
    /// when the player declined it during this holder's work, or when a note cannot be written.</summary>
    public static bool Engage(Ship ship, string condition, string automation, string holder)
    {
        var group = Types(ship).FirstOrDefault(g => g.Key == condition)?.ToArray();
        if (group == null || group.Length == 0) return false;
        var units = new List<CondOwner>();
        foreach (var sensor in group)
        {
            if (!Refresh(sensor, out var unit)) return false;
            if (!units.Contains(unit)) units.Add(unit);
        }
        if (group.Any(s => s.On)) return false;
        foreach (var unit in units)
        {
            var store = Store(unit, condition);
            var state = store.Read(out _);
            var record = SensorLeaseRecord.Read(store);
            // Someone else's note, a decline or unreadable data leaves the switch with the player.
            if (state != SavedStateStatus.Missing && (record == null || !record.BelongsTo(automation, holder) || record.State != SensorLeaseState.Leased))
                return false;
        }
        var written = new List<ObjectStateStore>();
        var lease = new SensorLeaseRecord(automation, holder, SensorLeaseState.Leased).Encode();
        foreach (var unit in units)
        {
            var store = Store(unit, condition);
            bool fresh = store.Read(out _) == SavedStateStatus.Missing;
            if (!store.TryWrite(lease)) { foreach (var added in written) added.Clear(); return false; }
            if (fresh) written.Add(store);
        }
        Flip(group);
        return group.All(s => s.On);
    }

    /// <summary>Switches off the sensor types this holder switched on and that are still on, then forgets
    /// all of the holder's notes on this ship. Returns the conditions it switched off.</summary>
    public static IReadOnlyList<string> Release(Ship ship, string automation, string holder)
    {
        var switchedOff = new List<string>();
        foreach (var group in Types(ship).ToArray())
        {
            var flip = new List<ShipSensor>();
            var notes = new List<ObjectStateStore>();
            foreach (var sensor in group)
            {
                var unit = UnitOf(sensor);
                if (unit == null) continue;
                var store = Store(unit, group.Key);
                var record = SensorLeaseRecord.Read(store);
                if (record == null || !record.BelongsTo(automation, holder)) continue;
                notes.Add(store);
                sensor.SetState();
                // Never flip a sensor that is already off: the native switch would turn it on.
                if (record.State == SensorLeaseState.Leased && sensor.On) flip.Add(sensor);
            }
            Flip(flip);
            foreach (var note in notes) note.Clear();
            if (flip.Count > 0 && flip.All(s => !s.On)) switchedOff.Add(group.Key);
        }
        return switchedOff;
    }

    private static IEnumerable<(string Condition, ShipSensor Sensor, SensorLeaseRecord Record)> Notes(Ship? ship, string automation)
    {
        foreach (var group in Types(ship))
            foreach (var sensor in group)
            {
                var unit = UnitOf(sensor);
                var record = unit == null ? null : SensorLeaseRecord.Read(Store(unit, group.Key));
                if (record != null && record.Automation == automation) yield return (group.Key, sensor, record);
            }
    }

    /// <summary>Sensor types a holder (or any holder, when null) switched on and that are still on.</summary>
    public static IReadOnlyList<string> InUse(Ship? ship, string automation, string? holder = null) =>
        Notes(ship, automation).Where(n => n.Record.State == SensorLeaseState.Leased && n.Sensor.On && (holder == null || n.Record.Holder == holder))
            .Select(n => n.Condition).Distinct().ToArray();

    /// <summary>Sensor types the player switched off again during this holder's work.</summary>
    public static IReadOnlyCollection<string> Declined(Ship? ship, string automation, string holder) =>
        new HashSet<string>(Notes(ship, automation).Where(n => n.Record.State == SensorLeaseState.Declined && n.Record.Holder == holder)
            .Select(n => n.Condition), StringComparer.Ordinal);

    /// <summary>Holders with any note on this ship, including notes left by an earlier session.</summary>
    public static IReadOnlyCollection<string> Holders(Ship? ship, string automation) =>
        new HashSet<string>(Notes(ship, automation).Select(n => n.Record.Holder), StringComparer.Ordinal);

    /// <summary>Records that the player switched a sensor type off during a holder's work, so the
    /// automation does not switch it back on before that work ends. Other notes stay untouched.</summary>
    public static bool Decline(Ship ship, string condition, string automation, string holder)
    {
        bool any = false;
        var declined = new SensorLeaseRecord(automation, holder, SensorLeaseState.Declined).Encode();
        foreach (var sensor in Types(ship).FirstOrDefault(g => g.Key == condition) ?? Enumerable.Empty<ShipSensor>())
        {
            var unit = UnitOf(sensor);
            if (unit == null) continue;
            var store = Store(unit, condition);
            var state = store.Read(out _);
            var record = SensorLeaseRecord.Read(store);
            if (state == SavedStateStatus.Missing || record != null && record.BelongsTo(automation, holder)) any |= store.TryWrite(declined);
        }
        return any;
    }

    // Native Sensors page, other mods or scripts: any flip not issued here is someone else taking control.
    internal static void Observe(ShipSensor sensor, bool toggle)
    {
        if (!toggle || issuing) return;
        try
        {
            var unit = UnitOf(sensor);
            if (unit == null) return;
            var store = Store(unit, sensor.CondId);
            var record = SensorLeaseRecord.Read(store);
            if (sensor.On) { if (record != null) store.Clear(); return; }
            if (record != null) { if (record.State == SensorLeaseState.Leased) store.TryWrite(record.With(SensorLeaseState.Declined).Encode()); return; }
            if (unit.ship != null) SwitchedOffByOthers?.Invoke(unit.ship, sensor.CondId);
        }
        catch (Exception ex) { Log("Sensor switch observation failed: " + ex.Message); }
    }
}

[HarmonyPatch(typeof(ShipSensor), nameof(ShipSensor.SetState))]
internal static class SensorSwitchPatch
{
    private static void Postfix(ShipSensor __instance, bool toggle) => SensorLeases.Observe(__instance, toggle);
}
