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
    internal static string TypeName(SensorType type)
    {
        foreach (var (known, key) in Types) if (known == type) return Text.Get("SensorSuite.type_" + key);
        return type.ToString();
    }

    /// <summary>Presentation only: native per-sensor signal reads, no toggles or saved state.</summary>
    internal static string Describe(Ship? observer, string? targetId)
    {
        try
        {
            var sensors = observer?.ElectronicSystems?.aElectronicSystems;
            if (observer?.objSS == null || sensors == null || sensors.Count == 0) return Text.Get("SensorSuite.none");
            if (observer.bCheckSensors || observer.bCheckPower) return Text.Get("Sensors.Updating");
            ShipSignature? signature = null; double rangeKM = double.NaN; float visibility = 0;
            bool located = targetId != null && NativeContactReader.TryInputs(observer, targetId, out signature, out rangeKM, out visibility, out _);
            var leased = new HashSet<string>(Phobos.Ostranauts.Framework.Sensors.SensorLeases.InUse(observer, NativeSensorControl.Automation), StringComparer.Ordinal);
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
                string line = located
                    ? Text.Get("SensorSuite.signal", name, on.Sum(s => s.GetSignalStrength(signature!, rangeKM, visibility)), on.Length, group.Length)
                    : Text.Get("SensorSuite.on", name, on.Length, group.Length);
                lines.Add(on.Any(s => leased.Contains(s.CondId)) ? Text.Get("SensorSuite.by_auto_nav", line) : line);
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
