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
        // Framework 0.104.0: the ledgers the mod keeps by key, read once a second while recording.
        Performance.RegisterFootprint("war.records", "footprint", () => WarService.RecordCount);
        Performance.RegisterFootprint("war.tallies", "footprint", () => WarService.TallyCount);
        Performance.RegisterFootprint("war.damaged", "footprint", () => WarService.DamagedCount);
        Performance.RegisterFootprint("war.part_facts", "footprint", () => WarService.PartFactCount);
    }
}
