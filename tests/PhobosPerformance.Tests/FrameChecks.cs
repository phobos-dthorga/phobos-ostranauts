using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Phobos.Ostranauts.Framework.Diagnostics;
using Phobos.Scope.Recording;

internal static class FrameChecks
{
    internal static void Run(Action<bool, string> check, string directory)
    {
        var session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { });
        Performance.Session = session;
        long tick = 100, bytes = 1000; int gc = 0, reads = 0;
        var frames = new FrameMeasurements(() => { reads++; return tick; }, 1000, _ => gc, () => bytes);
        bool Command(string text) => session.Command(("phobosframework perf " + text).Split(' '), out _);
        JsonElement Capture()
        {
            using var stream = new MemoryStream(); session.Snapshot!.WriteJson(stream);
            using var json = JsonDocument.Parse(stream.ToArray()); return json.RootElement.Clone();
        }
        for (int i = 0; i < 100; i++) frames.Poll();
        check(reads == 0, "Disabled frame sampling does not read clock or GC");
        check(Command("start detailed 30 100"), "Frame fixture starts");
        frames.Poll(); tick += 16; bytes += 512; frames.Poll(); tick += 50; gc++; bytes += 256; frames.Poll();
        session.Stop(StopReason.Manual);
        var data = Capture();
        var samples = data.GetProperty("counters").EnumerateArray().ToArray();
        check(samples.Count(s => s.GetProperty("metric").GetInt32() == 0) == 2, "Only complete frame intervals are recorded");
        check(samples.Where(s => s.GetProperty("metric").GetInt32() == 0).Select(s => s.GetProperty("value").GetDouble()).SequenceEqual(new[] { 16d, 50d }), "Frame intervals retain spikes independently of simulation speed");
        check(samples.Where(s => s.GetProperty("metric").GetInt32() == 4).Sum(s => s.GetProperty("value").GetDouble()) == 768, "Allocation samples are deltas, not heap size");
        check(samples.Count(s => s.GetProperty("metric").GetInt32() >= 1 && s.GetProperty("metric").GetInt32() <= 3) == 3, "Only changed GC counts produce samples");
        check(Command("export") && Command("start detailed 30 100"), "Second frame capture starts");
        tick += 10000; frames.Poll(); session.Stop(StopReason.Manual);
        check(Capture().GetProperty("counters").GetArrayLength() == 0, "New capture without idle poll never includes time between captures");
        check(Command("export") && Command("start detailed 30 1"), "Bounded frame capture starts");
        frames.Poll(); tick++; bytes++; frames.Poll(); session.Stop(StopReason.WorldChange);
        check(session.Snapshot!.DroppedRecords > 0, "Frame records obey the shared memory cap and report loss");

        session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { });
        Performance.Session = session;
        frames = new FrameMeasurements(() => ++tick, 1000, _ => 0, () => throw new NotSupportedException());
        check(Command("start detailed 30 100"), "Unsupported allocation fixture starts");
        frames.Poll(); frames.Poll(); session.Stop(StopReason.Manual);
        check(!frames.AllocationSupported && Capture().GetProperty("counters").EnumerateArray().Count(s => s.GetProperty("metric").GetInt32() <= 4) == 1,
            "Unsupported allocation reading remains unavailable, without fabricated zero samples");

        // Framework 0.104.0: the managed heap is read at the first frame after a collection, and only then.
        session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { });
        Performance.Session = session;
        int collections = 0; long managed = 4000; int heapReads = 0;
        frames = new FrameMeasurements(() => ++tick, 1000, _ => collections, null, () => { heapReads++; return managed; });
        check(Command("start detailed 30 100"), "Heap-after-collection fixture starts");
        frames.Poll(); frames.Poll(); collections++; managed = 3000; frames.Poll(); frames.Poll(); session.Stop(StopReason.Manual);
        var after = Capture().GetProperty("counter_aggregates").EnumerateArray().Single(a => a.GetProperty("metric").GetInt32() == 5);
        check(heapReads == 1 && after.GetProperty("samples").GetInt64() == 1 && after.GetProperty("last").GetDouble() == 3000,
            "The heap is read once, at the frame after a collection");

        // Frame-time buckets: each frame counts once, by its upper bound; a summary capture keeps the counts.
        session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => { });
        Performance.Session = session;
        long clock = 0;
        frames = new FrameMeasurements(() => clock, 1000, _ => 0, null);
        check(Command("start summary 30 100"), "Frame-bucket fixture starts");
        frames.Poll();
        foreach (long ms in new long[] { 16, 17, 17, 50, 51, 600 }) { clock += ms; frames.Poll(); }
        session.Stop(StopReason.Manual);
        var totals = Capture().GetProperty("counter_aggregates").EnumerateArray().ToDictionary(a => a.GetProperty("metric").GetInt32(), a => a.GetProperty("sum").GetDouble());
        double Count(int bucket) => totals[5 + bucket];
        check(Count(2) == 1 && Count(3) == 2 && Count(6) == 1 && Count(7) == 1 && Count(11) == 1 && Enumerable.Range(0, FrameMeasurements.Buckets.Length).Sum(Count) == 6,
            "Each frame counts once in the bucket of its upper bound, with the open bucket above 500 ms");
        check(totals[0] == 751, "The frame interval total keeps the whole time, so the mean frame is exact without samples");
    }
}
