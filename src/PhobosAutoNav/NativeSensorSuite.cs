using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Ships.Sensors;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

/// <summary>The ship's whole native sensor suite as seen from the navigation console:
/// what each fitted sensor type contributes to the current target, and an explicit,
/// player-issued switch-on through the same toggle as the native Sensors page.</summary>
internal static class NativeSensorSuite
{
    private static readonly (SensorType Type, string Key)[] Types =
    { (SensorType.Optical, "optical"), (SensorType.IR, "ir"), (SensorType.EM, "em"), (SensorType.Radar, "radar"), (SensorType.Lidar, "lidar") };

    /// <summary>Presentation only: native per-sensor signal reads, no toggles or saved state.</summary>
    internal static string Describe(Ship? observer, string? targetId)
    {
        try
        {
            var sensors = observer?.ElectronicSystems?.aElectronicSystems;
            if (observer?.objSS == null || sensors == null || sensors.Count == 0) return Text.Get("SensorSuite.none");
            if (observer.bCheckSensors || observer.bCheckPower) return Text.Get("Sensors.Updating");
            bool located = TryTarget(observer, targetId, out var signature, out double rangeKM, out float visibility);
            var lines = new List<string> { Text.Get("SensorSuite.heading") };
            bool offFitted = false, offActive = false;
            foreach (var (type, key) in Types)
            {
                var group = sensors.Where(s => s != null && s.SensorType == type).ToArray();
                string name = Text.Get("SensorSuite.type_" + key);
                if (group.Length == 0) { lines.Add(Text.Get("SensorSuite.not_fitted", name)); continue; }
                var on = group.Where(s => s.On).ToArray();
                bool active = group.Any(s => s.IsActiveSensor);
                if (on.Length == 0)
                {
                    offFitted = true; offActive |= active;
                    lines.Add(Text.Get(active ? "SensorSuite.off_active" : "SensorSuite.off", name));
                    continue;
                }
                lines.Add(located
                    ? Text.Get("SensorSuite.signal", name, on.Sum(s => s.GetSignalStrength(signature!, rangeKM, visibility)), on.Length, group.Length)
                    : Text.Get("SensorSuite.on", name, on.Length, group.Length));
            }
            if (located)
            {
                var reading = NativeContactReader.Read(observer, targetId);
                double threshold = ArrivalBrake.Finite(reading.Threshold) ? reading.Threshold : ContactRules.DefaultThreshold;
                lines.Add(Text.Get("SensorSuite.total", observer.ElectronicSystems.GetSignatureStrength(signature!, rangeKM, visibility), threshold, Text.Get(reading.MessageKey)));
            }
            if (offFitted) lines.Add(Text.Get(offActive ? "SensorSuite.hint_active" : "SensorSuite.hint"));
            return string.Join("\n", lines);
        }
        catch { return Text.Get("Sensors.Fault"); }
    }

    // The same target signature, range and visibility the contact reader uses.
    private static bool TryTarget(Ship observer, string? targetId, out ShipSignature? signature, out double rangeKM, out float visibility)
    {
        signature = null; rangeKM = double.NaN; visibility = observer.fVisibilityRangeMod;
        if (string.IsNullOrEmpty(targetId) || CrewSim.system == null) return false;
        var ship = CrewSim.system.GetShipByRegID(targetId!);
        if (ship != null)
        {
            if (ship == observer || ship.bDestroyed || ship.objSS == null) return false;
            signature = new ShipSignature(ship, observer);
            rangeKM = observer.GetRangeTo(ship) / AutoNavCore.KM_TO_AU;
            visibility = Math.Min(visibility, ship.fVisibilityRangeMod);
        }
        else if (CrewSim.system.dictStellarObjects != null && CrewSim.system.dictStellarObjects.TryGetValue(targetId!, out var stellar) && stellar?.objSS != null)
        {
            NativeContactReader.RefreshStellar(stellar.objSS);
            signature = new ShipSignature(stellar, observer);
            rangeKM = observer.objSS.GetRangeTo(stellar.objSS) / AutoNavCore.KM_TO_AU;
        }
        return signature != null && ArrivalBrake.Finite(rangeKM) && rangeKM >= 0;
    }

    /// <summary>F3: phobosnav sensors [passive|all]. Switching on needs the player's local
    /// navigation console. Active sensors emit and are switched on only when asked for.</summary>
    internal static bool Command(string[] words, out string response)
    {
        var co = NavigationService.OpenConsole;
        var own = co?.ship;
        string? targetId = AutoNavCore.Engaged && AutoNavCore.EngagedPlayer == own ? AutoNavCore.EngagedTarget?.ShipId :
            GUIOrbitDraw.CrossHairTarget?.Ship?.strRegID ?? GUIOrbitDraw.CrossHairTarget?.stellarObj?.strID;
        if (words.Length == 2) { response = own == null ? Text.Get("SensorSuite.console_required") : Describe(own, targetId); return own != null; }
        string mode = words.Length == 3 ? words[2].ToLowerInvariant() : "";
        if (mode != "passive" && mode != "all") { response = Text.Get("SensorSuite.usage"); return false; }
        if (co == null || !NavigationService.IsLocalConsole(co) || own?.ElectronicSystems?.aElectronicSystems == null)
        { response = Text.Get("SensorSuite.console_required"); return false; }
        if (own.bCheckSensors || own.bCheckPower) { response = Text.Get("Sensors.Updating"); return false; }
        bool includeActive = mode == "all";
        var switched = new List<string>();
        try
        {
            foreach (var sensor in own.ElectronicSystems.aElectronicSystems.ToArray())
            {
                if (sensor == null || !SensorSuiteRules.ShouldSwitchOn(sensor.On, sensor.IsActiveSensor, includeActive)) continue;
                sensor.SetState(toggle: true);
                if (sensor.On) switched.Add(DataHandler.GetCond(sensor.CondId)?.strNameFriendly ?? sensor.Name);
            }
        }
        finally { if (switched.Count > 0) GUIOrbitDraw.Instance?.InvalidateAllShips(); }
        response = (switched.Count == 0 ? Text.Get("SensorSuite.nothing_to_switch") :
            Text.Get(includeActive ? "SensorSuite.switched_active" : "SensorSuite.switched", string.Join(", ", switched.Distinct())))
            + "\n\n" + Describe(own, targetId);
        return true;
    }
}
