using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosAutoNav;

internal static class PerformanceMetrics
{
    internal static PerformanceMetric? Guidance, Docking, Contact;
    internal static void Initialize()
    {
        Guidance = Performance.RegisterOperation("autonav.guidance.update", "navigation");
        Docking = Performance.RegisterOperation("autonav.docking.update", "navigation");
        Contact = Performance.RegisterOperation("autonav.contact.read", "observations");
    }
}
