using System;
using System.Collections.Generic;
using System.Linq;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

// Native sensor-control boundary. Fitted switched-off sensor types add their gain to the
// observer's native contribution list when switched; notes follow Framework's lease rules.
internal static class NativeSensorControl
{
    internal sealed class FakeSensor
    {
        internal string Key = "", Name = "";
        internal bool Emits, On;
        internal double Gain;
        internal string? LeaseHolder, DeclinedBy;
    }
    internal static readonly List<FakeSensor> Sensors = new();
    internal static readonly List<(bool Caution, string Log, string? Banner)> Notices = new();
    internal static bool RefreshAfterSwitch;
    internal static int Surveys;

    internal static void Reset() { Sensors.Clear(); Notices.Clear(); RefreshAfterSwitch = false; Surveys = 0; }
    internal static FakeSensor Fit(string key, string name, double gain, bool emits = false)
    {
        var sensor = new FakeSensor { Key = key, Name = name, Gain = gain, Emits = emits };
        Sensors.Add(sensor); return sensor;
    }

    internal static void Survey(Ship own, IReadOnlyList<string> ids, string holder, out SensorNeed[] needs, out SensorOption[] options)
    {
        Surveys++;
        var found = new List<SensorNeed>();
        foreach (string id in ids)
        {
            if (!NativeContactReader.TryInputs(own, id, out var signature, out double rangeKM, out float visibility, out double threshold)) continue;
            double current = own.ElectronicSystems.GetSignatureStrength(signature!, rangeKM, visibility);
            bool beyondTrack = CrewSim.system.GetShipByRegID(id) == null && rangeKM > ContactRules.StellarRangeKM;
            found.Add(beyondTrack ? new SensorNeed(current, double.PositiveInfinity) : SensorSelection.Need(current, threshold));
        }
        needs = found.ToArray();
        int count = needs.Length;
        options = Sensors.Where(s => !s.On && s.DeclinedBy != holder)
            .Select(s => new SensorOption(s.Key, s.Name, s.Emits, Enumerable.Repeat(s.Gain, count).ToArray())).ToArray();
    }

    internal static IReadOnlyList<SensorOption> Engage(Ship own, IReadOnlyList<SensorOption> chosen, string holder)
    {
        var engaged = new List<SensorOption>();
        foreach (var option in chosen)
        {
            var sensor = Sensors.FirstOrDefault(s => s.Key == option.Key);
            if (sensor == null || sensor.On || sensor.DeclinedBy == holder) continue;
            sensor.On = true; sensor.LeaseHolder = holder;
            own.ElectronicSystems.aElectronicSystems.Add(sensor.Gain);
            engaged.Add(option);
        }
        if (engaged.Count > 0 && RefreshAfterSwitch) own.bCheckSensors = true;
        return engaged;
    }

    internal static IReadOnlyList<(string Name, bool Emits)> Release(Ship own, string holder)
    {
        var off = new List<(string, bool)>();
        foreach (var sensor in Sensors)
        {
            if (sensor.LeaseHolder == holder && sensor.On)
            {
                sensor.On = false; own.ElectronicSystems.aElectronicSystems.Remove(sensor.Gain);
                off.Add((sensor.Name, sensor.Emits));
            }
            if (sensor.LeaseHolder == holder) sensor.LeaseHolder = null;
            if (sensor.DeclinedBy == holder) sensor.DeclinedBy = null;
        }
        return off;
    }

    internal static IReadOnlyList<(string Name, bool Emits)> InUse(Ship? own, string holder) =>
        Sensors.Where(s => s.On && s.LeaseHolder == holder).Select(s => (s.Name, s.Emits)).ToArray();

    internal static IReadOnlyCollection<string> Holders(Ship own) =>
        Sensors.SelectMany(s => new[] { s.LeaseHolder, s.DeclinedBy }).Where(h => h != null).Select(h => h!).Distinct().ToArray();

    internal static bool Decline(Ship own, string condition, string holder)
    {
        bool any = false;
        foreach (var sensor in Sensors.Where(s => s.Key == condition && (s.LeaseHolder == null || s.LeaseHolder == holder)))
        { sensor.LeaseHolder = null; sensor.DeclinedBy = holder; any = true; }
        return any;
    }

    internal static void Notify(Ship own, bool caution, string logText, string? banner) => Notices.Add((caution, logText, banner));

    // The player's own switch on the native Sensors page, as Framework's switch observer sees it.
    internal static void PlayerSwitch(Ship own, string key, bool on)
    {
        var sensor = Sensors.First(s => s.Key == key);
        if (sensor.On == on) return;
        sensor.On = on;
        if (on) { own.ElectronicSystems.aElectronicSystems.Add(sensor.Gain); sensor.LeaseHolder = sensor.DeclinedBy = null; return; }
        own.ElectronicSystems.aElectronicSystems.Remove(sensor.Gain);
        if (sensor.LeaseHolder != null) { sensor.DeclinedBy = sensor.LeaseHolder; sensor.LeaseHolder = null; return; }
        if (sensor.DeclinedBy == null) Plugin.Service.SensorSwitchedOff(own, key);
    }
}
