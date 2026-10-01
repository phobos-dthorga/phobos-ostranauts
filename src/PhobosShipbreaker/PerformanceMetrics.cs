using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosShipbreaker;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? ProcessCheck, ProcessAdvance, RouteCheck, RouteAdvance, PanelRefresh, RouteCandidates, Furnace, Capture, Reclamation, FurnaceCandidates,
        PowerHook, FurnaceRoute, FurnaceSaves, Laser;
    internal static void Initialize()
    {
        ProcessCheck = Performance.RegisterOperation("shipbreaker.processing.check", "processing");
        ProcessAdvance = Performance.RegisterOperation("shipbreaker.processing.advance", "processing");
        RouteCheck = Performance.RegisterOperation("shipbreaker.routing.check", "routing");
        RouteAdvance = Performance.RegisterOperation("shipbreaker.routing.advance", "routing");
        PanelRefresh = Performance.RegisterOperation("shipbreaker.panel.refresh", "presentation");
        RouteCandidates = Performance.RegisterIncrement("shipbreaker.routing.candidate_items", "routing", "items");
        Furnace = Performance.RegisterOperation("shipbreaker.furnace.update", "processing");
        Capture = Performance.RegisterOperation("shipbreaker.capture.update", "processing");
        Reclamation = Performance.RegisterOperation("shipbreaker.reclamation.update", "processing");
        FurnaceCandidates = Performance.RegisterIncrement("shipbreaker.furnace.scan_objects", "discovery", "items");
        // 29 September 2026 pass: the power hooks on our own machines, the coolant route and furnace record settlements.
        PowerHook = Performance.RegisterOperation("shipbreaker.power.hook", "processing");
        FurnaceRoute = Performance.RegisterOperation("shipbreaker.furnace.route", "routing");
        FurnaceSaves = Performance.RegisterIncrement("shipbreaker.furnace.saves", "persistence", "saves");
        Laser = Performance.RegisterOperation("shipbreaker.laser.update", "processing");
        Performance.RegisterContext("shipbreaker.industrial_panel_visible", () =>
            CrewSim.goUI != null && CrewSim.goUI.GetComponent<IndustrialPanel>()?.bActive == true ? "true" : "false");
    }
}
