using System;
using HarmonyLib;
using Ostranauts.ShipGUIs.NavStation;

namespace PhobosAutoNav;

// Native UI has live latches in addition to persisted GPM switches. This adapter is
// called only by the checked explicit Disengage service, never by display refresh.
internal static class NativeFlightPanel
{
    internal static bool Holding(Ship ship) => GUIOrbitDraw.IsOpen() && GUIOrbitDraw.Instance != null &&
        GUIOrbitDraw.Instance.COSelfBase()?.ship == ship && GUIOrbitDraw.Instance.HoldingThrustActive;

    internal static void Release(Ship ship)
    {
        var panel = GUIOrbitDraw.IsOpen() ? GUIOrbitDraw.Instance : null;
        if (panel == null || panel.COSelfBase()?.ship != ship) return;
        var lamp = AccessTools.Field(typeof(GUIOrbitDraw), "ledWLock")?.GetValue(panel) as GUILamp;
        if (panel.HoldingThrustActive && lamp == null)
            throw new InvalidOperationException("Native held-thrust latch unavailable; use native navigation controls.");
        if (lamp != null) lamp.State = 0;
        if (panel.chkStationKeeping != null) panel.chkStationKeeping.SetIsOnWithoutNotify(false);
        foreach (var course in panel.GetComponentsInChildren<NavModCoursePlot>(true))
            if (course.chkEngage != null) course.chkEngage.SetIsOnWithoutNotify(false);
    }
}
