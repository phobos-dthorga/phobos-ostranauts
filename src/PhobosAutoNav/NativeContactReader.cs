using System;
using Ostranauts.Ships.Sensors;
using Phobos.Ostranauts.Framework.Processing;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Selected-target reads only: no native visibility UI, sensor toggles, list
// rebuilds, shared threshold writes, ship TimeAdvance or world ship scans.
internal static class NativeContactReader
{
    // 29 September 2026 pass (FF5): within one native physics step the guard, the guidance tick, fire control and
    // the hazard sweep all ask about the same contacts. One reading per (observer, target) is kept for that step
    // only; the service opens and closes the step, and Auto Nav's own sensor switches invalidate it. Reads outside
    // a step (panels, the torch controller, other mods) are always fresh.
    private static readonly StepMemo<(string Observer, string Target), ContactReading> readings = new();
    private static long step;
    private static bool sharing;
    internal static bool Sharing => sharing;
    internal static void BeginStep() { step++; sharing = true; readings.Invalidate(); }
    internal static void EndStep() { sharing = false; readings.Invalidate(); }
    internal static void Invalidate() => readings.Invalidate();

    internal static ContactReading Read(Ship? observer, string? targetId)
    {
        if (!sharing || observer == null || string.IsNullOrEmpty(targetId)) return ReadNow(observer, targetId);
        var key = (observer.strRegID ?? "", targetId!);
        if (readings.TryGet(step, key, out var known)) return known;
        var reading = ReadNow(observer, targetId);
        readings.Set(step, key, reading);
        return reading;
    }

    private static ContactReading ReadNow(Ship? observer, string? targetId)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Contact);
        Phobos.Ostranauts.Framework.Diagnostics.Performance.Increment(PerformanceMetrics.ContactReads);
        try
        {
            var system = CrewSim.system;
            if (system == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ||
                observer == null || observer.bDestroyed || observer.objSS == null || string.IsNullOrEmpty(targetId) ||
                system.GetShipByRegID(observer.strRegID) != observer)
                return new ContactReading(ContactState.Unavailable);
            var target = system.GetShipByRegID(targetId!);
            // An asteroid remains a native stellar marker until tethered; it then becomes a ship
            // with the same ID, so one identity covers both forms.
            if (target == null) return ReadStellar(system, observer, targetId!);
            if (target == observer || target.bDestroyed || target.objSS == null ||
                target.HideFromSystem || target.IsStationHidden()) return new ContactReading(ContactState.Unavailable);
            // What the game's own navigation station shows without a signal test (GUIOrbitDraw.VisibleFromNavStation):
            // this ship's docked partners, signal beacons and the tutorial derelict. Known stations need the
            // station's own record and are still read through the signal test.
            if (target.IsDockedWith(observer) || target.Classification == Ship.TypeClassification.SignalBeacon || target.ShipCO?.HasCond("IsTutorialDerelict") == true)
                return new ContactReading(ContactState.Ready, 1, ContactRules.DefaultThreshold);
            var problem = SensorProblem(observer);
            if (problem != null) return problem.Value;
            if (Occluded(system, observer, target.objSS, out bool fault))
                return new ContactReading(fault ? ContactState.Fault : ContactState.Occluded);

            if (!TryInputs(observer, targetId!, out var signature, out double rangeKM, out float visibility, out double threshold))
                return new ContactReading(ContactState.Fault);
            return ContactRules.Evaluate(observer.ElectronicSystems.GetSignatureStrength(signature!, rangeKM, visibility), threshold);
        }
        catch
        {
            // Drawing diagnostics must not throw into the game's UI. Guidance
            // treats the same unreadable state as a loss of authority to track.
            return new ContactReading(ContactState.Fault);
        }
    }

    // Native stellar-object sensing: the same combined signal, the observer's visibility only,
    // a fixed threshold and a 1,000 km live-track limit. Celestial occlusion matches ship reads.
    private static ContactReading ReadStellar(StarSystem system, Ship observer, string id)
    {
        if (system.dictStellarObjects == null || !system.dictStellarObjects.TryGetValue(id, out var stellar) || stellar?.objSS == null)
            return new ContactReading(ContactState.Unavailable);
        var problem = SensorProblem(observer);
        if (problem != null) return problem.Value;
        RefreshStellar(stellar.objSS);
        if (Occluded(system, observer, stellar.objSS, out bool fault))
            return new ContactReading(fault ? ContactState.Fault : ContactState.Occluded);
        if (!TryInputs(observer, id, out var signature, out double rangeKM, out float visibility, out _, refresh: false))
            return new ContactReading(ContactState.Fault);
        return ContactRules.EvaluateStellar(observer.ElectronicSystems.GetSignatureStrength(signature!, rangeKM, visibility), rangeKM);
    }

    /// <summary>The native inputs every contact read uses: signature, range, visibility and detection
    /// threshold. False when the object or its geometry cannot be read. Occlusion is checked separately.</summary>
    internal static bool TryInputs(Ship observer, string id, out ShipSignature? signature, out double rangeKM, out float visibility,
        out double threshold, bool refresh = true)
    {
        signature = null; rangeKM = double.NaN; visibility = observer.fVisibilityRangeMod; threshold = ContactRules.DefaultThreshold;
        var system = CrewSim.system;
        if (system == null || observer.objSS == null || string.IsNullOrEmpty(id)) return false;
        var ship = system.GetShipByRegID(id);
        if (ship != null)
        {
            if (ship == observer || ship.bDestroyed || ship.objSS == null) return false;
            rangeKM = observer.GetRangeTo(ship) / AutoNavCore.KM_TO_AU;
            visibility = Math.Min(visibility, ship.fVisibilityRangeMod);
            var selected = CrewSim.GetSelectedCrew();
            bool skilled = selected != null && !selected.bDestroyed && selected.ship == observer && selected.HasCond("SkillOpsSensors");
            threshold = skilled ? ContactRules.SkilledThreshold : ContactRules.DefaultThreshold;
            if (!Finite(rangeKM, visibility)) return false;
            signature = new ShipSignature(ship, observer);
            return true;
        }
        // Stellar markers: the observer's visibility only and a fixed threshold.
        if (system.dictStellarObjects == null || !system.dictStellarObjects.TryGetValue(id, out var stellar) || stellar?.objSS == null) return false;
        if (refresh) RefreshStellar(stellar.objSS);
        rangeKM = observer.objSS.GetRangeTo(stellar.objSS) / AutoNavCore.KM_TO_AU;
        if (!Finite(rangeKM, visibility)) return false;
        signature = new ShipSignature(stellar, observer);
        return true;
    }

    private static bool Finite(double rangeKM, float visibility) =>
        ArrivalBrake.Finite(rangeKM) && rangeKM >= 0 && ArrivalBrake.Finite(visibility) && visibility >= 0;

    private static ContactReading? SensorProblem(Ship observer)
    {
        if (observer.bCheckSensors || observer.bCheckPower) return new ContactReading(ContactState.Updating);
        var sensors = observer.ElectronicSystems;
        return sensors?.aElectronicSystems == null || !sensors.HasAnySensorOn() ? new ContactReading(ContactState.NoSensors) : null;
    }

    // The native segment/body test without the nav screen's current main occluder.
    // Nonphysical placeholders and asteroid-field markers are not bodies that block sight.
    private static bool Occluded(StarSystem system, Ship observer, ShipSitu target, out bool fault)
    {
        fault = false;
        if (system.aBOs == null) { fault = true; return true; }
        foreach (var body in system.aBOs.Values)
            if (body != null && body.nDrawFlagsBody != ContactRules.PlaceholderBodyDrawFlag && !body.IsAsteroidField &&
                StarSystem.IsLOSBlockedByBO(body, observer, target))
                return true;
        return false;
    }

    /// <summary>Native field-locked asteroids hold a cached position recomputed from their field
    /// by a zero-length advance, as native collision checks do. No other state changes.</summary>
    internal static void RefreshStellar(ShipSitu situ)
    {
        if (situ.bBOLocked && !situ.bOrbitLocked && situ.strBOPORShip != null) situ.TimeAdvance(0.0);
    }
}
