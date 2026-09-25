using System;
using System.Linq;
using System.Reflection;

internal static class FlightHubNativeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        check(typeof(GUIOrbitDraw).GetField("ledWLock", flags)?.FieldType == typeof(GUILamp),
            "Native held-thrust lamp adapter resolves against the installed engine");
        check(typeof(GUIOrbitDraw).GetProperty("HoldingThrustActive", flags)?.PropertyType == typeof(bool),
            "Native held-thrust live latch remains observable");
        var rescue = typeof(GUIOrbitDraw).GetMethod("ToggleInnerPanel", flags)!;
        var calls = PlaceholderLoadChecks.Calls(rescue);
        check(calls.Any(m => m.DeclaringType == typeof(CanvasManager) && m.Name == "ShowCanvasGroup") &&
            calls.Any(m => m.DeclaringType == typeof(CanvasManager) && m.Name == "HideCanvasGroup"),
            "Native Rescue switches canvas visibility rather than unloading flight modules");
        check(typeof(AIShipManager).GetMethod("GetAIShipByRegID")!.ReturnType.GetProperty("ActiveCommandName", flags)?.PropertyType == typeof(string),
            "Explicit override can distinguish standard native pilot commands");
    }
}
