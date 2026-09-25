using System;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Audio;

internal static class CompletionCueChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws)
    {
        var watch = new CompletionWatch();
        check(!watch.Commit("actor", "ship", true), "Unwatched production stays silent");
        watch.Arm("actor", "ship");
        check(watch.Commit("actor", "ship", true) && watch.Completed && !watch.Armed, "Confirmed delivery consumes one watch and retains text");
        for (int i = 0; i < 100; i++) check(!watch.Commit("actor", "ship", true), "Continuous queues cannot replay consumed watch");
        watch.Arm("actor", "ship");
        check(!watch.Completed, "Explicit rearm clears previous completion text");
        watch.Cancel();
        check(!watch.Commit("actor", "ship", true), "Pause/cancel/fault does not announce success");
        watch.Arm("actor", "ship");
        check(!watch.Commit("other", "ship", true) && watch.Completed, "Other selected crew cannot receive cue but completion remains visible");
        watch.Arm("actor", "ship");
        check(!watch.Commit("actor", "neighbour", true), "No cue across ship scope");
        watch.Arm("actor", "ship");
        check(!watch.Commit("actor", "ship", false) && !watch.Armed, "Recovered already-complete input consumes watch silently");
        watch.Arm("actor", "ship");
        watch = new CompletionWatch();
        check(!watch.Armed && !watch.Completed && !watch.Commit("actor", "ship", true), "Fresh load has neither old watch nor old completion");
        watch.Arm("", "ship");
        check(!watch.Armed, "No anonymous watch");

        var gate = new CompletionCueGate();
        var meal = new CompletionWatch(); var flight = new CompletionWatch();
        meal.Arm("actor", "ship"); flight.Arm("actor", "ship");
        var shared = new CompletionCueGate();
        check(meal.Commit("actor", "ship", true) && shared.Take(1, .35, false), "First mod admitted to the shared audio channel");
        check(flight.Commit("actor", "ship", true) && !shared.Take(1.1, .35, false), "Another mod's simultaneous completion is consumed but silent");
        check(meal.Completed && flight.Completed && !flight.Commit("actor", "ship", true), "Both mods retain visual completion without a delayed replay");
        check(!gate.Take(10, 0, false), "Muted event dropped");
        check(!gate.Take(10, double.NaN, false) && !gate.Take(double.NaN, .35, false), "Invalid audio inputs rejected");
        check(gate.Take(10, .35, false), "Eligible event admitted");
        for (int i = 0; i < 100; i++) check(!gate.Take(10 + i / 100.0, .35, false), "Fast-forward burst dropped without queue");
        check(!gate.Take(14, .35, true), "Never overlap another completion");
        check(gate.Take(14, .35, false), "New event admitted after real-time spacing");
        check(!gate.Take(1, .35, false) && gate.Take(4, .35, false), "Clock reset handled without rapid replay");
        watch.Arm("actor", "ship");
        bool completed = watch.Commit("actor", "ship", true);
        check(completed && !gate.Take(20, 0, false) && !watch.Commit("actor", "ship", true), "Unmuting does not replay a muted completion");

        using var audio = typeof(CompletionWatch).Assembly.GetManifestResourceStream("PhobosFramework.completion.wav")!;
        using var copy = new MemoryStream(); audio.CopyTo(copy);
        var bytes = copy.ToArray();
        var samples = CompletionCuePcm.Read(new MemoryStream(bytes));
        check(samples.Length / (double)CompletionCuePcm.SampleRate == .28, "Owned cue stays brief");
        check(samples[0] == 0 && samples[samples.Length - 1] == 0 && samples.Max(s => Math.Abs(s)) <= .101, "Quiet peak and silent endpoints");
        check(samples.Zip(samples.Skip(1), (a,b) => Math.Abs(a-b)).Max() < .01, "No abrupt sample discontinuity");
        foreach (int offset in new[] { 0, 4, 8, 12, 16, 20, 22, 24, 28, 32, 34, 36, 40 })
        {
            var broken = (byte[])bytes.Clone(); broken[offset] ^= 0x40;
            throws(() => CompletionCuePcm.Read(new MemoryStream(broken)), "Changed PCM header fails closed at " + offset);
        }
        throws(() => CompletionCuePcm.Read(new MemoryStream(bytes.Take(43).ToArray())), "Truncated export rejected");
    }
}
