using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ostranauts.Ships.Sensors;
using Phobos.Ostranauts.Framework.Sensors;

// Native members that selective sensor engagement and its player notices rely on.
internal static class SensorNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        check(typeof(ShipSensor).GetField("_coId", flags)?.FieldType == typeof(string), "Sensor unit identity resolves for lease notes");
        check(typeof(ShipSensor).GetField("SensorState")?.FieldType == typeof(int), "The in-memory sensor switch remains a public field");
        var setState = typeof(ShipSensor).GetMethod("SetState", new[] { typeof(bool) });
        check(setState != null, "The native Sensors-page switch remains SetState(bool)");
        var calls = PlaceholderLoadChecks.Calls(setState!);
        check(calls.Any(m => m.Name == "GetCondAmount") && calls.Any(m => m.Name == "SetCondAmount") && calls.Any(m => m.Name == "ZeroCondAmount"),
            "The switch still re-reads and writes the saved per-type condition, so a refresh never flips it");
        var cycle = typeof(Ostranauts.ShipGUIs.MFD.MFDSensors).GetMethod("CycleModes", flags);
        check(cycle != null && PlaceholderLoadChecks.Calls(cycle).Any(m => m == setState),
            "The native Sensors page switches through the same method Framework observes");
        check(typeof(ShipSensor).GetMethod("GetSignalStrength", new[] { typeof(ShipSignature), typeof(double), typeof(float) }) != null,
            "Per-sensor native signal remains available for predictions");
        check(typeof(ElectronicSystems).GetField("aElectronicSystems")?.FieldType == typeof(List<ShipSensor>), "The ship's sensor registry is still a sensor list");
        check(typeof(DataHandler).GetField("mapCOs", BindingFlags.Static | BindingFlags.Public)?.FieldType == typeof(Dictionary<string, CondOwner>),
            "Sensor units still resolve by ID after native mode switches");
        check(typeof(GUIOrbitDraw).GetMethod("CGClampWarning", new[] { typeof(string), typeof(bool) }) != null, "The native nav-map warning banner resolves");
        check(typeof(CondOwner).GetMethod("LogMessage", new[] { typeof(string), typeof(string), typeof(string), typeof(string) }) != null,
            "The native crew message log resolves");
        var patch = typeof(SensorLeases).Assembly.GetType("Phobos.Ostranauts.Framework.Sensors.SensorSwitchPatch");
        check(patch?.GetCustomAttributesData().Any(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch" &&
            a.ConstructorArguments.Any(c => Equals(c.Value, typeof(ShipSensor)))) == true, "Framework observes the native sensor switch");
    }
}
