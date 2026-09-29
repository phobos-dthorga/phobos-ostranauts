using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosAgriculture;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? Scan, Tick, Panel, Candidates, Route, Saves;
    internal static void Initialize()
    {
        Scan = Performance.RegisterOperation("agriculture.scan", "discovery");
        Tick = Performance.RegisterOperation("agriculture.machine.update", "processing");
        Panel = Performance.RegisterOperation("agriculture.panel.refresh", "presentation");
        Candidates = Performance.RegisterIncrement("agriculture.scan_objects", "discovery", "items");
        Route = Performance.RegisterOperation("agriculture.irrigation.route", "routing");
        Saves = Performance.RegisterIncrement("agriculture.saves", "persistence", "saves");
    }
}
