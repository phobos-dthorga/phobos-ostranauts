using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Phobos.Ostranauts.Framework.Diagnostics;
using Phobos.Scope.Recording;

/// <summary>Framework 0.104.0: memory levels and footprint counts read once a real second while recording.</summary>
internal static class PeriodicChecks
{
    internal static void Run(Action<bool, string> check, string directory)
    {
        long tick = 0; int logs = 0, heapReads = 0, entries = 5;
        double? level = 100;
        var session = new PerformanceSession(directory, () => true, () => new Dictionary<string, string>(), _ => logs++, () => tick, 1000);
        Performance.Session = session;
        Performance.RegisterPeriodic("test.heap", "memory", "bytes", () => { heapReads++; return level; });
        Performance.RegisterFootprint("test.sessions", "footprint", () => entries);
        Performance.RegisterPeriodic("test.broken", "memory", "bytes", () => throw new NotSupportedException());
        bool Command(string text) => session.Command(("phobosframework perf " + text).Split(' '), out _);
        JsonElement Capture()
        {
            using var stream = new MemoryStream(); session.Snapshot!.WriteJson(stream);
            using var json = JsonDocument.Parse(stream.ToArray()); return json.RootElement.Clone();
        }
        JsonElement Total(JsonElement data, int metric) => data.GetProperty("counter_aggregates").EnumerateArray().Single(a => a.GetProperty("metric").GetInt32() == metric);

        for (int i = 0; i < 50; i++) session.Poll();
        check(heapReads == 0, "Nothing is read while recording is off");
        check(Command("start summary 30 100"), "Periodic fixture starts");
        check(heapReads == 1, "A window starts with a reading");
        for (int i = 0; i < 50; i++) { tick += 10; session.Poll(); }
        check(heapReads == 1, "No second reading inside the same real second");
        tick += 500; session.Poll();
        check(heapReads == 2, "The next reading comes a real second later");
        level = null; tick += 1000; session.Poll(); level = 300; entries = 9; tick += 1000; session.Poll();
        session.Stop(StopReason.Manual);
        var data = Capture(); var heap = Total(data, 0); var footprint = Total(data, 1);
        check(heap.GetProperty("samples").GetInt64() == 3 && heap.GetProperty("max").GetDouble() == 300 && heap.GetProperty("last").GetDouble() == 300,
            "An unavailable second records nothing, never a zero");
        check(footprint.GetProperty("samples").GetInt64() == 4 && footprint.GetProperty("min").GetDouble() == 5 && footprint.GetProperty("last").GetDouble() == 9,
            "A footprint count is a level sampled on the same cadence");
        check(Total(data, 2).GetProperty("samples").GetInt64() == 0 && logs == 1 && session.Snapshot!.RejectedMeasurements == 0,
            "A broken reading is switched off once, with one log line, and the capture carries on unmarked");
        check(Command("export") && Command("start summary 30 100") && heapReads == 5, "A new window reads again at once");
        session.Stop(StopReason.Manual);
        check(Total(Capture(), 2).GetProperty("samples").GetInt64() == 0 && logs == 1, "A switched-off reading stays off for the session");
    }
}
