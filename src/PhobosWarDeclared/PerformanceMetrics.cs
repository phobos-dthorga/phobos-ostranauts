using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosWarDeclared;

internal static class PerformanceMetrics
{
    // 29 September 2026 pass: the two-second poll over player ships and the laying pass it runs when due.
    internal static PerformanceMetric? Poll, LayPending;
    internal static void Initialize()
    {
        Poll = Performance.RegisterOperation("war.poll", "processing");
        LayPending = Performance.RegisterOperation("war.lay_pending", "processing");
    }
}
