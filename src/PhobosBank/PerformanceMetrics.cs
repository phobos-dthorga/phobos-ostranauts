using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosBank;

internal static class PerformanceMetrics
{
    // 6 October 2026: one ledger read when the panel opens and every two seconds while it stays open.
    internal static PerformanceMetric? Read;
    internal static void Initialize() => Read = Performance.RegisterOperation("bank.read_debts", "processing");
}
