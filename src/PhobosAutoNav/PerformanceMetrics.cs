using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosAutoNav;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? Guidance, Docking, Contact, PanelRefresh, PanelRead, Fire, Background;
    internal static void Initialize()
    {
        Guidance = Performance.RegisterOperation("autonav.guidance.update", "navigation");
        Docking = Performance.RegisterOperation("autonav.docking.update", "navigation");
        Contact = Performance.RegisterOperation("autonav.contact.read", "observations");
        PanelRefresh = Performance.RegisterOperation("autonav.panel.refresh", "presentation");
        PanelRead = Performance.RegisterOperation("autonav.panel.read", "presentation");
        Fire = Performance.RegisterOperation("autonav.fire.update", "navigation");
        Background = Performance.RegisterOperation("autonav.system.update", "navigation");
    }
}
