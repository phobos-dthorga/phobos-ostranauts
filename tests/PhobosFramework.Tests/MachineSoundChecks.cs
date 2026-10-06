using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Phobos.Ostranauts.Framework.Audio;

/// <summary>Framework 0.119.0 machine work sounds (owner request, 6 October 2026): the eight embedded loops read, measure
/// as the audio record says, and are levelled to one loudness; the nearest working machines are voiced; fades and
/// start offsets behave. Unity playback, the mix and loudness in the game are not tested here.</summary>
internal static class MachineSoundChecks
{
    // RMS of each prepared loop, dBFS, from assets/phobos-audio/washer-motor-pump-v1/manifest.json.
    private static readonly Dictionary<MachineLoop, double> RecordedRms = new()
    {
        [MachineLoop.A] = -26.745, [MachineLoop.B] = -22.275, [MachineLoop.C] = -25.795, [MachineLoop.D] = -25.797,
        [MachineLoop.E] = -25.153, [MachineLoop.F] = -22.803, [MachineLoop.G] = -23.111, [MachineLoop.H] = -25.541
    };
    internal static void Run(Action<bool, string> check)
    {
        var assembly = typeof(MachineSoundRules).Assembly;
        double reference = MachineSoundRules.FallbackReferenceLevel;
        foreach (MachineLoop loop in Enum.GetValues(typeof(MachineLoop)))
        {
            using var stream = assembly.GetManifestResourceStream(MachineSoundRules.Resource(loop));
            check(stream != null, "The plugin carries machine loop " + loop);
            var bytes = new MemoryStream(); stream!.CopyTo(bytes);
            var samples = CompletionCuePcm.Read(new MemoryStream(bytes.ToArray()), MachineSoundRules.MaxLoopSamples, "machine loop");
            check(samples.Length == 253575, "Loop " + loop + " is the prepared 5.75 s of mono samples");
            double rms = MachineSoundRules.Rms(samples);
            check(Math.Abs(MachineSoundRules.Dbfs(rms) - RecordedRms[loop]) < .02, "Loop " + loop + " measures as its audio record says: " + MachineSoundRules.Dbfs(rms));
            float gain = MachineSoundRules.Gain(reference, rms, 1);
            check(gain > 0 && gain < 1 && Math.Abs(gain * rms - reference) < 1e-6, "Loop " + loop + " is levelled to the vanilla reference");
            bool refused = false;
            try { CompletionCuePcm.Read(new MemoryStream(bytes.ToArray())); } catch (InvalidDataException) { refused = true; }
            check(refused, "The completion cue's half-second bound still refuses a loop");
        }
        check(MachineSoundRules.Gain(reference, .05, 0) == 0 && MachineSoundRules.Gain(reference, .05, .5) == MachineSoundRules.Gain(reference, .05, 1) / 2 &&
              MachineSoundRules.Gain(1, .0001, 1) == 1 && MachineSoundRules.Gain(double.NaN, .05, 1) == 0, "The player's volume scales the level, 0 mutes, and no source goes above full");

        var working = new[] { ("far", 900.0), ("near", 4.0), ("mid", 100.0), ("tie-b", 25.0), ("tie-a", 25.0), ("outside", 3000.0) };
        check(MachineSoundRules.Voiced(working, 3, 50).SequenceEqual(new[] { "near", "tie-a", "tie-b" }), "The nearest working machines are voiced, ties in a stable order");
        check(!MachineSoundRules.Voiced(working, 12, 50).Contains("outside") && MachineSoundRules.Voiced(working, 0, 50).Count == 0,
            "Machines beyond hearing are never voiced, and no voices means none");

        float up = 0; for (int i = 0; i < 8; i++) up = MachineSoundRules.Fade(up, .2f, .2f, .02f);
        check(Math.Abs(up - .2f) < 1e-6 && Math.Abs(MachineSoundRules.Fade(0, .2f, .2f, .075f) - .1f) < 1e-6, "A loop fades in linearly over 0.15 s and never overshoots");
        float down = .2f; for (int i = 0; i < 11; i++) down = MachineSoundRules.Fade(down, 0, .2f, .02f);
        check(down == 0 && Math.Abs(MachineSoundRules.Fade(.2f, 0, .2f, .1f) - .1f) < 1e-6, "A loop fades out linearly over 0.2 s and reaches silence");
        check(MachineSoundRules.Fade(.15f, 0, 0, .2f) == 0, "Muting mid-play still fades a loop out");
        check(MachineSoundRules.Fade(.1f, .1f, .1f, 1) == .1f && MachineSoundRules.Fade(.1f, .2f, .2f, 0) == .1f, "A steady loop is never faded at its repeat");

        int a = MachineSoundRules.StartSample("machine-1", 253575), b = MachineSoundRules.StartSample("machine-2", 253575);
        check(a == MachineSoundRules.StartSample("machine-1", 253575) && a >= 0 && a < 253575 && b != a, "Each machine starts its loop at its own stable place");

    }
}
