using System;
using Ostranauts.Ships.Sensors;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Selected-target reads only: no native visibility UI, sensor toggles, list
// rebuilds, shared threshold writes, target TimeAdvance or world ship scans.
internal static class NativeContactReader
{
    internal static ContactReading Read(Ship? observer, string? targetId)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Contact);
        try
        {
            var system = CrewSim.system;
            if (system == null || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ||
                observer == null || observer.bDestroyed || observer.objSS == null || string.IsNullOrEmpty(targetId) ||
                system.GetShipByRegID(observer.strRegID) != observer)
                return new ContactReading(ContactState.Unavailable);
            var target = system.GetShipByRegID(targetId);
            if (target == null || target == observer || target.bDestroyed || target.objSS == null ||
                target.HideFromSystem || target.IsStationHidden()) return new ContactReading(ContactState.Unavailable);
            if (observer.bCheckSensors || observer.bCheckPower) return new ContactReading(ContactState.Updating);
            var sensors = observer.ElectronicSystems;
            if (sensors?.aElectronicSystems == null || !sensors.HasAnySensorOn())
                return new ContactReading(ContactState.NoSensors);

            // Use the native segment/body test without depending on the nav
            // screen's current main occluder. Ignore nonphysical placeholders.
            if (system.aBOs == null) return new ContactReading(ContactState.Fault);
            foreach (var body in system.aBOs.Values)
                if (body != null && body.nDrawFlagsBody != ContactRules.PlaceholderBodyDrawFlag && !body.IsAsteroidField &&
                    StarSystem.IsLOSBlockedByBO(body, observer, target.objSS))
                    return new ContactReading(ContactState.Occluded);

            double rangeKM = observer.GetRangeTo(target) / AutoNavCore.KM_TO_AU;
            float visibility = Math.Min(observer.fVisibilityRangeMod, target.fVisibilityRangeMod);
            if (!ArrivalBrake.Finite(rangeKM) || rangeKM < 0 || !ArrivalBrake.Finite(visibility) || visibility < 0)
                return new ContactReading(ContactState.Fault);
            var selected = CrewSim.GetSelectedCrew();
            bool skilled = selected != null && !selected.bDestroyed && selected.ship == observer && selected.HasCond("SkillOpsSensors");
            double threshold = skilled ? ContactRules.SkilledThreshold : ContactRules.DefaultThreshold;
            var signature = new ShipSignature(target, observer);
            return ContactRules.Evaluate(sensors.GetSignatureStrength(signature, rangeKM, visibility), threshold);
        }
        catch
        {
            // Drawing diagnostics must not throw into the game's UI. Guidance
            // treats the same unreadable state as a loss of authority to track.
            return new ContactReading(ContactState.Fault);
        }
    }
}
