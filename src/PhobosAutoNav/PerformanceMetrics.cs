using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosAutoNav;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? Guidance, Docking, Contact, PanelRefresh, PanelRead, Fire, Background;
    // 29 September 2026 pass: the guard sweep, hazard scan, record settlement, the foreign-controller check and the
    // count of native contact reads (one per target per step once shared).
    internal static PerformanceMetric? Guard, Hazards, Persist, ForeignController, ContactReads;
    internal static void Initialize()
    {
        Guidance = Performance.RegisterOperation("autonav.guidance.update", "navigation");
        Docking = Performance.RegisterOperation("autonav.docking.update", "navigation");
        Contact = Performance.RegisterOperation("autonav.contact.read", "observations");
        PanelRefresh = Performance.RegisterOperation("autonav.panel.refresh", "presentation");
        PanelRead = Performance.RegisterOperation("autonav.panel.read", "presentation");
        Fire = Performance.RegisterOperation("autonav.fire.update", "navigation");
        Background = Performance.RegisterOperation("autonav.system.update", "navigation");
        Guard = Performance.RegisterOperation("autonav.guard.update", "navigation");
        Hazards = Performance.RegisterOperation("autonav.hazards.scan", "observations");
        Persist = Performance.RegisterOperation("autonav.persist.write", "persistence");
        ForeignController = Performance.RegisterOperation("autonav.foreign_controller.check", "observations");
        ContactReads = Performance.RegisterIncrement("autonav.contact.reads", "observations", "reads");
    }
}
