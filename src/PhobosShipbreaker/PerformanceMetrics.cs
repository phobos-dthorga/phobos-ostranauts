using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosShipbreaker;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? ProcessCheck, ProcessAdvance, RouteCheck, RouteAdvance, PanelRefresh, RouteCandidates;
    internal static void Initialize()
    {
        ProcessCheck = Performance.RegisterOperation("shipbreaker.processing.check", "processing");
        ProcessAdvance = Performance.RegisterOperation("shipbreaker.processing.advance", "processing");
        RouteCheck = Performance.RegisterOperation("shipbreaker.routing.check", "routing");
        RouteAdvance = Performance.RegisterOperation("shipbreaker.routing.advance", "routing");
        PanelRefresh = Performance.RegisterOperation("shipbreaker.panel.refresh", "presentation");
        RouteCandidates = Performance.RegisterIncrement("shipbreaker.routing.candidate_items", "routing", "items");
        Performance.RegisterContext("shipbreaker.industrial_panel_visible", () =>
            CrewSim.goUI != null && CrewSim.goUI.GetComponent<IndustrialPanel>()?.bActive == true ? "true" : "false");
    }
}
