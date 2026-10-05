using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Profiling;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Memory levels read once a real second while recording (Framework 0.104.0; owner request, 5 October 2026).
/// Each source is tried once at start-up: one that throws or reads nothing is left out and named as unavailable, never
/// recorded as zero. These are whole-game figures; our own share shows in the footprint counts and by comparing captures
/// with and without the mods.</summary>
internal static class MemoryMeasurements
{
    internal static string Available { get; private set; } = "";
    internal static string Unavailable { get; private set; } = "";

    internal static void Register()
    {
        var available = new List<string>(); var unavailable = new List<string>();
        void Source(string key, Func<long> read)
        {
            bool ok;
            try { ok = read() > 0; } catch { ok = false; }
            (ok ? available : unavailable).Add(key);
            if (ok) Performance.RegisterPeriodic("memory." + key, "memory", "bytes", () => read());
        }
        // The managed heap in use, without forcing a collection.
        Source("managed_heap", () => GC.GetTotalMemory(false));
        // Unity's own accounting: its scripting heap (used and reserved) and its native allocations.
        Source("unity.mono_used", () => Profiler.GetMonoUsedSizeLong());
        Source("unity.mono_heap", () => Profiler.GetMonoHeapSizeLong());
        Source("unity.allocated", () => Profiler.GetTotalAllocatedMemoryLong());
        Source("unity.reserved", () => Profiler.GetTotalReservedMemoryLong());
        // What Task Manager shows for the game.
        Process? process = null;
        try { process = Process.GetCurrentProcess(); } catch { }
        if (process != null)
        {
            Source("process.working_set", () => { process.Refresh(); return process.WorkingSet64; });
            Source("process.private", () => { process.Refresh(); return process.PrivateMemorySize64; });
        }
        else unavailable.Add("process");
        Available = available.Count == 0 ? "none" : string.Join(",", available);
        Unavailable = unavailable.Count == 0 ? "none" : string.Join(",", unavailable);
    }
}
