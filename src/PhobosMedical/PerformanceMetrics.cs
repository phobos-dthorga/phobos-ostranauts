using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosMedical;

internal static class PerformanceMetrics
{
    // 4 October 2026: the two-second pass over Ward-3 beds, the occasional sweep for care no bed still claims, and the
    // offer gate the game runs for every sleep, rest and lay offer on any object.
    internal static PerformanceMetric? BedTick, CareSweep, OfferGate;
    internal static void Initialize()
    {
        BedTick = Performance.RegisterOperation("medical.bed_tick", "processing");
        CareSweep = Performance.RegisterOperation("medical.care_sweep", "processing");
        OfferGate = Performance.RegisterOperation("medical.offer_gate", "native_hook");
    }
}
