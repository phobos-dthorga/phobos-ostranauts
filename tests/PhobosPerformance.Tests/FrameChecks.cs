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
        check(!frames.AllocationSupported && Capture().GetProperty("counters").GetArrayLength() == 1, "Unsupported allocation reading remains unavailable, without fabricated zero samples");
    }
}
