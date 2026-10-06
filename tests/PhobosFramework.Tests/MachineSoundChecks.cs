using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Phobos.Ostranauts.Framework.Audio;

/// <summary>Framework 0.119.0 machine work sounds (owner request, 6 October 2026): the eight loops read, measure as the
/// audio record says, and are levelled to one loudness; the nearest working machines are voiced; fades and start
/// offsets behave. Since 0.120.0 the sounds are loose files a player may replace: the shipped names match the build's
/// file map, a readable replacement plays, a bad one falls back to the shipped file, and a missing shipped file is
/// refused. Unity playback, the mix and loudness in the game are not tested here.</summary>
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
        string repo = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var map = ((JObject)JObject.Parse(File.ReadAllText(Path.Combine(repo, "config/framework-sounds.json")))["files"]!).Properties()
            .ToDictionary(p => p.Name, p => (string)p.Value!);
        check(map.Keys.OrderBy(k => k).SequenceEqual(SoundFiles.All.Select(n => SoundFiles.Folder + "/" + n).OrderBy(k => k)),
            "The build's sound map ships exactly the files the code asks for");
        string root = Path.Combine(Path.GetTempPath(), "phobos-sounds-" + Guid.NewGuid().ToString("N"));
        string shipped = Path.Combine(root, "plugin", SoundFiles.Folder), replacements = Path.Combine(root, "config", SoundFiles.Folder);
        Directory.CreateDirectory(shipped); Directory.CreateDirectory(replacements);
        try
        {
            foreach (var pair in map) File.Copy(Path.Combine(repo, pair.Value), Path.Combine(root, "plugin", pair.Key));
            var log = new List<string>();
            PcmClip Load(string name, double seconds) => SoundFiles.Load(name, seconds, "test sound", replacements, shipped, log.Add);

            double reference = MachineSoundRules.FallbackReferenceLevel;
            foreach (MachineLoop loop in Enum.GetValues(typeof(MachineLoop)))
            {
                var clip = Load(SoundFiles.MachineLoop(loop), MachineSoundRules.MaxLoopSeconds);
                check(clip.Samples.Length == 253575 && clip.SampleRate == 44100, "Loop " + loop + " is the prepared 5.75 s of mono samples");
                double rms = MachineSoundRules.Rms(clip.Samples);
                check(Math.Abs(MachineSoundRules.Dbfs(rms) - RecordedRms[loop]) < .02, "Loop " + loop + " measures as its audio record says: " + MachineSoundRules.Dbfs(rms));
                float gain = MachineSoundRules.Gain(reference, rms, 1);
                check(gain > 0 && gain < 1 && Math.Abs(gain * rms - reference) < 1e-6, "Loop " + loop + " is levelled to the vanilla reference");
                bool refused = false;
                try { Load(SoundFiles.MachineLoop(loop), SoundFiles.CompletionMaxSeconds); } catch (InvalidDataException) { refused = true; }
                check(refused, "The completion cue's half-second bound still refuses a loop");
            }
            check(log.Count == 0, "Shipped sounds load without a log line");

            // A player's replacement: stereo at 48 kHz with an editor's notes, mixed to mono and levelled like any loop.
            string name = SoundFiles.MachineLoop(MachineLoop.C);
            var stereo = Enumerable.Range(0, 48000).SelectMany(i => new short[] { (short)(i % 200 * 40), (short)-(i % 200 * 40) }).ToArray();
            File.WriteAllBytes(Path.Combine(replacements, name), Wav(2, 48000, stereo, notes: true));
            var replaced = Load(name, MachineSoundRules.MaxLoopSeconds);
            check(replaced.SampleRate == 48000 && replaced.Samples.Length == 48000 && replaced.Samples.All(s => s == 0),
                "A stereo replacement plays at its own rate, mixed to mono");
            check(log.Count == 1 && log[0].Contains("Playing the replacement"), "Using a replacement is said in the log");
            var mono = Enumerable.Range(0, 22050).Select(i => (short)(i % 100 * 100)).ToArray();
            File.WriteAllBytes(Path.Combine(replacements, name), Wav(1, 22050, mono, notes: false));
            check(MachineSoundRules.Gain(reference, MachineSoundRules.Rms(Load(name, MachineSoundRules.MaxLoopSeconds).Samples), 1) < 1,
                "A loud replacement is turned down to the game's appliance level");

            // A replacement that cannot be read: reported, and the shipped loop plays.
            foreach (var bad in new[] { new byte[] { 1, 2, 3 }, Wav(1, 44100, mono, notes: false).Take(60).ToArray(), Wav(1, 4000, mono, notes: false),
                         Wav(1, 8000, new short[8000 * 11], notes: false) })
            {
                log.Clear();
                File.WriteAllBytes(Path.Combine(replacements, name), bad);
                var fallback = Load(name, MachineSoundRules.MaxLoopSeconds);
                check(fallback.Samples.Length == 253575 && log.Count == 1 && log[0].Contains("ignored") && log[0].Contains("shipped sound plays"),
                    "A replacement that cannot be read is reported and the shipped loop plays (" + bad.Length + " bytes)");
            }
            File.Delete(Path.Combine(replacements, name));

            // A shipped file that is missing is refused with its path, never played as silence or garbage.
            File.Delete(Path.Combine(shipped, SoundFiles.MachineLoop(MachineLoop.H)));
            bool missing = false;
            try { Load(SoundFiles.MachineLoop(MachineLoop.H), MachineSoundRules.MaxLoopSeconds); }
            catch (InvalidDataException ex) { missing = ex.Message.Contains("missing") && ex.Message.Contains("machine-loop-h.wav"); }
            check(missing, "A missing shipped loop is refused and named");

            var loud = new[] { .5f, -.8f, .2f };
            var limited = SoundFiles.LimitPeak(loud, SoundFiles.CompletionPeak);
            check(Math.Abs(limited.Max(Math.Abs) - SoundFiles.CompletionPeak) < 1e-6 && Math.Abs(limited[0] / limited[1] - loud[0] / loud[1]) < 1e-6,
                "A loud replacement cue is turned down to the shipped cue's peak, its shape kept");
            var quiet = new[] { .05f, -.02f };
            check(ReferenceEquals(SoundFiles.LimitPeak(quiet, SoundFiles.CompletionPeak), quiet), "A quiet cue is left alone");
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }

        check(MachineSoundRules.Gain(MachineSoundRules.FallbackReferenceLevel, .05, 0) == 0 &&
              MachineSoundRules.Gain(MachineSoundRules.FallbackReferenceLevel, .05, .5) == MachineSoundRules.Gain(MachineSoundRules.FallbackReferenceLevel, .05, 1) / 2 &&
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

    /// <summary>A 16-bit PCM WAV as an editor writes it, optionally with a LIST notes section after the samples.</summary>
    internal static byte[] Wav(int channels, int rate, short[] interleaved, bool notes)
    {
        using var memory = new MemoryStream();
        using var w = new BinaryWriter(memory);
        byte[] list = notes ? new byte[] { (byte)'I', (byte)'N', (byte)'F', (byte)'O', (byte)'I', (byte)'S', (byte)'F', (byte)'T', 5, 0, 0, 0, (byte)'e', (byte)'d', (byte)'i', (byte)'t', 0, 0 } : Array.Empty<byte>();
        int data = interleaved.Length * 2;
        w.Write("RIFF".ToCharArray()); w.Write(4 + 24 + 8 + data + (notes ? 8 + list.Length : 0)); w.Write("WAVE".ToCharArray());
        w.Write("fmt ".ToCharArray()); w.Write(16); w.Write((short)1); w.Write((short)channels); w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
        w.Write("data".ToCharArray()); w.Write(data); foreach (short s in interleaved) w.Write(s);
        if (notes) { w.Write("LIST".ToCharArray()); w.Write(list.Length); w.Write(list); }
        w.Flush();
        return memory.ToArray();
    }
}
