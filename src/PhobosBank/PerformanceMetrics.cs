using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosBank;

internal static class PerformanceMetrics
{
    // 6 October 2026: one ledger read when the panel opens and every two seconds while it stays open.
    internal static PerformanceMetric? Read, Loans;
    internal static void Initialize()
    {
        Read = Performance.RegisterOperation("bank.read_debts", "processing");
        // 7 October 2026 (Banking 0.2.0): the five-second loan poll, which bills interest at shift changes.
        Loans = Performance.RegisterOperation("bank.poll_loans", "processing");
    }
}
