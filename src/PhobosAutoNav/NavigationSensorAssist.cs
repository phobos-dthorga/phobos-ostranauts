using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Owner direction (28 September 2026): Auto Nav may switch on the fewest sensors needed to keep its
// target and nearby hazards tracked, non-emitting ones first, and warns the player each time. When
// its work ends it switches off only what it switched on, unless the player has switched it since.
// Sensors stay on while a flight is suspended so Resume can reacquire. Displays never switch sensors.
internal sealed partial class NavigationService
{
    // SensorSettleChecks (the settle budget) lives in NavigationSensors.cs, which every consumer compiles.
    // Game seconds without Auto Nav work before its sensors are switched off; covers controller handoffs.
    internal const double SensorReleaseGraceSeconds = 5;
    // Game seconds between hazard surveys; weak hazards keep their wider clearance meanwhile.
    internal const double HazardSurveySeconds = 2;

    private sealed class SensorHolder { internal CondOwner? Console; internal Ship Ship = null!; internal double IdleSince = double.NaN; }
    private readonly Dictionary<string, SensorHolder> sensorHolders = new(StringComparer.Ordinal);
    private string? settlingShip;
    private int settleChecks;
    private double nextHazardSurvey = double.NegativeInfinity;

    private static bool Restorable(ContactState state) => state == ContactState.Weak || state == ContactState.NoSensors;

    partial void RestoreTargetSensors(CondOwner? co, string? targetId, ref ContactReading reading)
    {
        if (co?.ship == null || string.IsNullOrEmpty(targetId) || !Restorable(reading.State)) return;
        if (EngageSensors(co, new[] { targetId! }, "target")) reading = NativeContactReader.Read(co.ship, targetId);
    }

    partial void RestoreHazardSensors(CondOwner co, IReadOnlyList<string> weakIds)
    {
        if (weakIds.Count == 0 || StarSystem.fEpoch < nextHazardSurvey) return;
        nextHazardSurvey = StarSystem.fEpoch + HazardSurveySeconds;
        EngageSensors(co, weakIds, "hazard");
    }

    private bool EngageSensors(CondOwner co, IReadOnlyList<string> ids, string reason)
    {
        var mode = Plugin.AutoEngageSensors.Value;
        var own = co.ship;
        if (mode == SensorAutoEngage.Off || own == null) return false;
        try
        {
            NativeSensorControl.Survey(own, ids, co.strID, out var needs, out var options);
            var chosen = SensorSelection.Choose(options, needs, mode);
            if (chosen.Count == 0) return false;
            var engaged = NativeSensorControl.Engage(own, chosen, co.strID);
            if (engaged.Count == 0) return false;
            // Readings shared within this step were taken before the switch.
            NativeContactReader.Invalidate();
            Track(co, own);
            settlingShip = own.strRegID; settleChecks = SensorSettleChecks;
            string names = SensorNames(engaged.Select(o => (o.Name, o.Emits)));
            bool emits = engaged.Any(o => o.Emits);
            string message = Text.Get(emits ? "SensorAssist.engaged_emitting" : "SensorAssist.engaged", names, Text.Get("SensorAssist.reason_" + reason));
            NativeSensorControl.Notify(own, true, message, Text.Get(emits ? "SensorAssist.banner_emitting" : "SensorAssist.banner", names));
            log(message);
            return true;
        }
        catch (Exception ex) { log(ex.ToString()); return false; }
    }

    private static string SensorNames(IEnumerable<(string Name, bool Emits)> sensors) =>
        string.Join(", ", sensors.Select(s => s.Emits ? Text.Get("SensorAssist.emitting_name", s.Name) : s.Name));

    private void Track(CondOwner co, Ship ship)
    {
        if (sensorHolders.TryGetValue(co.strID, out var known)) { known.Console = co; known.Ship = ship; known.IdleSince = double.NaN; }
        else sensorHolders[co.strID] = new SensorHolder { Console = co, Ship = ship };
    }

    partial void SettleSensors(Ship own, bool hold, ref bool settling)
    {
        if (settleChecks <= 0 || settlingShip != own.strRegID) return;
        settling = true;
        if (!hold) return;
        settleChecks--;
        HoldThrust(own, Text.Get("SensorAssist.settling"));
    }

    // Resumable flights keep their sensors while suspended. Departure reports its own saved work.
    private bool SensorWorkPending(SensorHolder holder)
    {
        var co = holder.Console;
        if (co == null || co.bDestroyed || co.ship != holder.Ship) return false;
        if (AutoNavCore.Engaged && console == co || industrial?.Console == co) return true;
        bool pending = DisplaySnapshot(co) != null;
        ReadSensorWork(co, ref pending);
        return pending;
    }

    // Real seconds between holder reconciliations: deciding whether Auto Nav still needs its sensors decodes the
    // console's flight record, which ran every frame while a flight was suspended (29 September 2026 pass).
    internal const double SensorReconcileSeconds = 0.5;
    private readonly Phobos.Ostranauts.Framework.Cadence sensorCadence = new(SensorReconcileSeconds);
    private readonly List<string> releasedHolders = new();

    partial void ReconcileSensors()
    {
        if (sensorHolders.Count == 0 || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.Paused || !sensorCadence.Due(RealClock())) return;
        releasedHolders.Clear();
        foreach (var pair in sensorHolders)
        {
            var holder = pair.Value;
            if (holder.Ship == null || holder.Ship.bDestroyed) { releasedHolders.Add(pair.Key); continue; }
            if (SensorWorkPending(holder)) { holder.IdleSince = double.NaN; continue; }
            if (double.IsNaN(holder.IdleSince) || StarSystem.fEpoch < holder.IdleSince) { holder.IdleSince = StarSystem.fEpoch; continue; }
            if (StarSystem.fEpoch - holder.IdleSince < SensorReleaseGraceSeconds) continue;
            releasedHolders.Add(pair.Key);
        }
        foreach (string key in releasedHolders)
        {
            var holder = sensorHolders[key];
            sensorHolders.Remove(key);
            if (holder.Ship != null && !holder.Ship.bDestroyed) ReleaseSensors(holder.Ship, key);
        }
        releasedHolders.Clear();
    }

    private void ReleaseSensors(Ship ship, string holder)
    {
        try
        {
            var off = NativeSensorControl.Release(ship, holder);
            if (off.Count == 0) return;
            NativeContactReader.Invalidate();
            string message = Text.Get("SensorAssist.released", SensorNames(off));
            NativeSensorControl.Notify(ship, false, message, null);
            log(message);
        }
        catch (Exception ex) { log(ex.ToString()); }
    }

    // After loading: notes left on this ship by an earlier session are reconciled like current work.
    partial void DiscoverSensorHolders(Ship ship)
    {
        try
        {
            var holders = NativeSensorControl.Holders(ship).Where(h => !sensorHolders.ContainsKey(h)).ToArray();
            if (holders.Length == 0) return;
            var consoles = ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true).ToArray();
            foreach (string holder in holders)
                sensorHolders[holder] = new SensorHolder { Console = consoles.FirstOrDefault(c => c.strID == holder), Ship = ship };
        }
        catch (Exception ex) { log(ex.ToString()); }
    }

    // World teardown drops references only; saved notes are reconciled after the next load.
    partial void ForgetSensorWork()
    {
        sensorHolders.Clear(); settlingShip = null; settleChecks = 0; nextHazardSurvey = double.NegativeInfinity;
    }

    partial void ReadSensorsInUse(CondOwner co, ref string line)
    {
        if (co.ship == null) return;
        var inUse = NativeSensorControl.InUse(co.ship, co.strID);
        if (inUse.Count > 0) line = Text.Get("SensorAssist.in_use", SensorNames(inUse));
    }

    /// <summary>Someone else switched a sensor type off while Auto Nav was working on that ship:
    /// Auto Nav leaves it off until that work ends.</summary>
    internal void SensorSwitchedOff(Ship ship, string condition)
    {
        try
        {
            var consoles = new[] { console, industrial?.Console }.Concat(sensorHolders.Values.Select(h => h.Console)).Distinct();
            foreach (var co in consoles)
            {
                if (co == null || co.ship != ship || !SensorWorkPending(new SensorHolder { Console = co, Ship = ship })) continue;
                if (NativeSensorControl.Decline(ship, condition, co.strID)) Track(co, ship);
            }
        }
        catch (Exception ex) { log(ex.ToString()); }
    }
}
