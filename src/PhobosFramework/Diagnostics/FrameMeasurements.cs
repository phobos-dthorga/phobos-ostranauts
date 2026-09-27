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
    private readonly double millisecondsPerTick;
    private readonly PerformanceMetric? interval, gc0, gc1, gc2, bytes;
    private int sequence = -1, previous0, previous1, previous2;
    private long previousTick, previousBytes;
    internal bool AllocationSupported => allocated != null;

    internal FrameMeasurements() : this(Stopwatch.GetTimestamp, Stopwatch.Frequency, GC.CollectionCount, AllocationReader()) { }
    internal FrameMeasurements(Func<long> clock, long frequency, Func<int, int> collections, Func<long>? allocated)
    {
        this.clock = clock; this.collections = collections; this.allocated = allocated;
        millisecondsPerTick = 1000d / frequency;
        interval = Performance.Session?.RegisterGauge("game.frame.interval", "frames", "ms");
        gc0 = Performance.RegisterIncrement("game.gc.gen0", "memory", "collections");
        gc1 = Performance.RegisterIncrement("game.gc.gen1", "memory", "collections");
        gc2 = Performance.RegisterIncrement("game.gc.gen2", "memory", "collections");
        bytes = Performance.RegisterIncrement("game.allocations.main_thread", "memory", "bytes");
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
                if (tick > previousTick) Performance.Increment(interval, (tick - previousTick) * millisecondsPerTick);
                if (n0 > previous0) Performance.Increment(gc0, n0 - previous0);
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
