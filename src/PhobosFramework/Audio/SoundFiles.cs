using System;
using System.Collections.Generic;
using System.IO;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Where Framework's sounds come from (Framework 0.120.0; owner decision, 6 October 2026): loose WAV files in
/// the plugin's own <c>sounds</c> folder, not resources inside the assembly, so a player can see and replace them. A
/// file of the same name in <c>BepInEx/config/PhobosFramework/sounds/</c> is played instead of the shipped one; a
/// replacement that cannot be read is reported and the shipped sound plays. A shipped file that is missing or damaged
/// leaves that one sound silent and says so. Replacements are levelled like the shipped loops, so none plays louder
/// than the game's own appliances. No game types, so the offline checks run it.</summary>
public static class SoundFiles
{
    /// <summary>The folder name, beside the plugin and under the player's config folder alike.</summary>
    public const string Folder = "sounds";
    public const string Completion = "completion.wav";
    /// <summary>The longest completion cue: half a second.</summary>
    public const double CompletionMaxSeconds = .5;
    /// <summary>The shipped cue's quiet peak; a louder replacement is turned down to it.</summary>
    public const float CompletionPeak = .1f;

    /// <summary>Scales samples down so none is louder than <paramref name="peak"/>; quieter sounds are left alone.</summary>
    public static float[] LimitPeak(float[] samples, float peak)
    {
        float highest = 0;
        foreach (float s in samples) highest = Math.Max(highest, Math.Abs(s));
        if (!(highest > peak) || !(peak > 0)) return samples;
        float scale = peak / highest;
        var result = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++) result[i] = samples[i] * scale;
        return result;
    }

    public static string MachineLoop(MachineLoop loop) => "machine-loop-" + char.ToLowerInvariant(loop.ToString()[0]) + ".wav";

    /// <summary>Every file Framework ships in its sounds folder.</summary>
    public static IEnumerable<string> All
    {
        get
        {
            yield return Completion;
            foreach (MachineLoop loop in Enum.GetValues(typeof(MachineLoop))) yield return MachineLoop(loop);
        }
    }

    /// <summary>The plugin's own sounds folder; set at start-up.</summary>
    public static string ShippedDirectory { get; set; } = "";
    /// <summary>The player's replacement folder; empty for none.</summary>
    public static string OverrideDirectory { get; set; } = "";
    public static Action<string> Log { get; set; } = _ => { };

    internal static PcmClip Load(string name, double maxSeconds, string what) => Load(name, maxSeconds, what, OverrideDirectory, ShippedDirectory, Log);

    /// <summary>The player's replacement when it reads, otherwise the shipped file. Throws InvalidDataException when
    /// the shipped file is missing or damaged.</summary>
    internal static PcmClip Load(string name, double maxSeconds, string what, string overrideDirectory, string shippedDirectory, Action<string> log)
    {
        if (!string.IsNullOrEmpty(overrideDirectory))
        {
            string replacement = Path.Combine(overrideDirectory, name);
            if (File.Exists(replacement))
            {
                try
                {
                    var clip = ReadFile(replacement, maxSeconds, what);
                    log("Playing the replacement " + what + " " + replacement + ".");
                    return clip;
                }
                catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
                {
                    log("Replacement " + what + " " + replacement + " ignored: " + ex.Message + " The shipped sound plays instead.");
                }
            }
        }
        string shipped = Path.Combine(shippedDirectory ?? "", name);
        if (!File.Exists(shipped)) throw new InvalidDataException("The " + what + " file " + shipped + " is missing; reinstall Phobos Framework.");
        try { return ReadFile(shipped, maxSeconds, what); }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        { throw new InvalidDataException("The " + what + " file " + shipped + " cannot be read: " + ex.Message); }
    }

    private static PcmClip ReadFile(string path, double maxSeconds, string what)
    {
        if (new FileInfo(path).Length > PcmWav.MaxFileBytes(maxSeconds)) throw new InvalidDataException("The " + what + " file is longer than " + maxSeconds + " seconds.");
        using var stream = File.OpenRead(path);
        return PcmWav.Read(stream, maxSeconds, what);
    }
}
