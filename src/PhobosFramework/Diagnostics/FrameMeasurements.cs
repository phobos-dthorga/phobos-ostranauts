using System;
using System.Diagnostics;

namespace Phobos.Ostranauts.Framework.Diagnostics;

// One main-thread poll per rendered frame, not a timer or simulation tick.
// No samples, clock/GC calls or allocations while recording is disabled.
internal sealed class FrameMeasurements
{
    private readonly Func<long> clock;
    private readonly Func<int, int> collections;
    private Func<long>? allocated;
    private readonly Func<long>? heap;
    private readonly double millisecondsPerTick;
    private readonly PerformanceMetric? interval, gc0, gc1, gc2, bytes, afterCollection;
    /// <summary>Frame-time buckets (Framework 0.104.0): each frame counts once in the first bucket whose upper bound it
    /// does not exceed, the last bucket open-ended. Summary captures keep counter totals rather than every frame, so
    /// these counts are what frame percentiles and long-frame counts are read from: a percentile is reported as its
    /// bucket's upper bound, never interpolated. The bounds are the common refresh rates and the long-frame limits the
    /// comparison script reports (33.3, 50 and 100 ms).</summary>
    internal static readonly (double UpperMs, string Name)[] Buckets =
    {
        (1000d / 120, "game.frame.upto_8_3ms"), (1000d / 90, "game.frame.upto_11_1ms"), (1000d / 60, "game.frame.upto_16_7ms"),
        (20, "game.frame.upto_20ms"), (25, "game.frame.upto_25ms"), (1000d / 30, "game.frame.upto_33_3ms"), (50, "game.frame.upto_50ms"),
        (1000d / 15, "game.frame.upto_66_7ms"), (100, "game.frame.upto_100ms"), (200, "game.frame.upto_200ms"), (500, "game.frame.upto_500ms"),
        (double.PositiveInfinity, "game.frame.over_500ms")
    };
    private readonly PerformanceMetric?[] buckets = new PerformanceMetric?[Buckets.Length];
    private readonly PerformanceMetric? collectionFrame;
    private int sequence = -1, previous0, previous1, previous2;
    private long previousTick, previousBytes;
    internal bool AllocationSupported => allocated != null;

    internal FrameMeasurements() : this(Stopwatch.GetTimestamp, Stopwatch.Frequency, GC.CollectionCount, AllocationReader(), () => GC.GetTotalMemory(false)) { }
    internal FrameMeasurements(Func<long> clock, long frequency, Func<int, int> collections, Func<long>? allocated, Func<long>? heap = null)
    {
        this.clock = clock; this.collections = collections; this.allocated = allocated; this.heap = heap;
        millisecondsPerTick = 1000d / frequency;
        interval = Performance.Session?.RegisterGauge("game.frame.interval", "frames", "ms");
        gc0 = Performance.RegisterIncrement("game.gc.gen0", "memory", "collections");
        gc1 = Performance.RegisterIncrement("game.gc.gen1", "memory", "collections");
        gc2 = Performance.RegisterIncrement("game.gc.gen2", "memory", "collections");
        bytes = Performance.RegisterIncrement("game.allocations.main_thread", "memory", "bytes");
        // Framework 0.104.0: the managed heap at the first frame after any collection. Its floor rising over a long
        // session is the plainest sign that something holds on to memory.
        if (heap != null) afterCollection = Performance.Session?.RegisterGauge("memory.managed_heap_after_collection", "memory", "bytes");
        for (int i = 0; i < Buckets.Length; i++) buckets[i] = Performance.RegisterIncrement(Buckets[i].Name, "frames", "frames");
        // Framework 0.133.0 (L102): the length of each frame in which a collection happened, which holds its pause. On
        // the game's runtime every collection counts in all three generations, so the first is enough. Its count,
        // total and worst against the whole frame total show how much of a capture went to collections.
        collectionFrame = Performance.Session?.RegisterGauge("game.gc.frame_ms", "memory", "ms");
    }
    private static Func<long>? AllocationReader()
    {
        try
        {
            var method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);
            if (method == null) return null;
            var read = (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
            // Some Mono builds expose the method but return a constant zero.
            // Establish that an actual allocation advances it before claiming support.
            long before = read(); var probe = new byte[256]; long after = read(); GC.KeepAlive(probe);
            return after - before >= probe.Length ? read : null;
        }
        catch { return null; }
    }
    private long ReadAllocated()
    {
        try { return allocated?.Invoke() ?? 0; }
        catch { allocated = null; return 0; } // Unsupported Mono API is unavailable, never zero allocation.
    }
    internal void Poll()
    {
        var session = Performance.Session;
        if (session == null || !session.IsRecording) { sequence = -1; return; }
        try
        {
            long tick = clock(), memory = ReadAllocated();
            int n0 = collections(0), n1 = collections(1), n2 = collections(2);
            if (sequence == session.CaptureSequence)
            {
                if (tick > previousTick)
                {
                    double ms = (tick - previousTick) * millisecondsPerTick; int bucket = 0;
                    while (ms > Buckets[bucket].UpperMs) bucket++;
                    Performance.Increment(interval, ms); Performance.Increment(buckets[bucket], 1);
                    if (n0 > previous0) Performance.Increment(collectionFrame, ms);
                }
                if (n0 > previous0) Performance.Increment(gc0, n0 - previous0);
                if (n0 > previous0 && heap != null) Performance.Increment(afterCollection, heap());
                if (n1 > previous1) Performance.Increment(gc1, n1 - previous1);
                if (n2 > previous2) Performance.Increment(gc2, n2 - previous2);
                if (AllocationSupported && memory >= previousBytes) Performance.Increment(bytes, memory - previousBytes);
            }
            sequence = session.CaptureSequence;
            previousTick = tick; previousBytes = memory;
            previous0 = n0; previous1 = n1; previous2 = n2;
        }
        catch (Exception error) { session.Fault(error); }
    }
}
