using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>The eight washer/pump loops (Framework 0.119.0; ElevenLabs Sound Effects v2, see
/// <c>assets/phobos-audio/washer-motor-pump-v1</c>). Each machine family is given one, hard-coded by its mod; each loop
/// is a file in Framework's sounds folder (<see cref="SoundFiles.MachineLoop"/>) a player may replace.</summary>
public enum MachineLoop { A, B, C, D, E, F, G, H }

/// <summary>The machine-sound rules with no game types, so the offline checks run them: loudness matching, which
/// working machines are voiced, fades and start offsets. Nothing here touches work, power or saved records.</summary>
public static class MachineSoundRules
{
    /// <summary>The longest loop the reader accepts, shipped or a player's replacement.</summary>
    public const double MaxLoopSeconds = 10;
    /// <summary>Fade in and out (handoff trial values), in real seconds; never at each repeat of the loop.</summary>
    public const float AttackSeconds = .15f, ReleaseSeconds = .2f;
    /// <summary>The vanilla appliance loop ours are levelled to, and its volume and level if it cannot be measured.</summary>
    public const string ReferenceEmitter = "ItmAtmoScrubberOnSound";
    /// <summary>Level used when the vanilla reference cannot be measured: the scrubber's 0.1 volume on a -24 dBFS loop.</summary>
    public const double FallbackReferenceLevel = .1 * 0.0630957344;
    public const int DefaultVoices = 4, MaxVoices = 12;
    /// <summary>A machine's own pitch is kept within the game's own variation for its loops (0.95-1.05), widened a
    /// little for size: big machines lower, small ones higher. Never changed with game speed.</summary>
    public const float MinPitch = .75f, MaxPitch = 1.25f;

    /// <summary>The root-mean-square level of a clip, 0 to 1 of full scale.</summary>
    public static double Rms(IReadOnlyList<float> samples)
    {
        if (samples == null || samples.Count == 0) return 0;
        double sum = 0;
        for (int i = 0; i < samples.Count; i++) sum += samples[i] * (double)samples[i];
        return Math.Sqrt(sum / samples.Count);
    }
    public static double Dbfs(double level) => level > 0 ? 20 * Math.Log10(level) : double.NegativeInfinity;
    /// <summary>The source volume that makes a clip as loud as the vanilla reference (its volume times its level),
    /// times the player's setting; never above 1.</summary>
    public static float Gain(double referenceLevel, double clipRms, double playerVolume)
    {
        if (!(referenceLevel > 0) || !(clipRms > 0) || !(playerVolume > 0) || double.IsInfinity(referenceLevel)) return 0;
        return (float)Math.Min(1, referenceLevel / clipRms * Math.Min(1, playerVolume));
    }
    /// <summary>The ids of the closest working machines to voice, nearest first; ties keep a stable order by id so the
    /// choice never flickers between equals.</summary>
    public static List<string> Voiced(IEnumerable<(string Id, double DistanceSquared)> working, int voices, double maxDistance)
    {
        if (working == null || voices <= 0) return new List<string>();
        double limit = maxDistance > 0 ? maxDistance * maxDistance : double.PositiveInfinity;
        return working.Where(w => w.DistanceSquared <= limit && !double.IsNaN(w.DistanceSquared))
            .OrderBy(w => w.DistanceSquared).ThenBy(w => w.Id, StringComparer.Ordinal).Take(voices).Select(w => w.Id).ToList();
    }
    /// <summary>One step of a linear fade toward the target, at a fixed rate set by the voice's full level: rising from
    /// silence to full over the attack, falling from full to silence over the release.</summary>
    public static float Fade(float current, float target, float fullLevel, float deltaSeconds)
    {
        if (!(deltaSeconds > 0) || current == target) return current;
        float scale = Math.Max(fullLevel, Math.Max(current, target));
        float rate = scale / (target > current ? AttackSeconds : ReleaseSeconds);
        if (!(rate > 0)) return target;
        float step = rate * deltaSeconds;
        return target > current ? Math.Min(target, current + step) : Math.Max(target, current - step);
    }
    /// <summary>Where a machine's loop starts, stable for that machine (FNV-1a of its id), so two machines on the same
    /// loop are not in phase. Presentation only: no game randomness.</summary>
    public static int StartSample(string id, int samples)
    {
        if (samples <= 0) return 0;
        uint h = 2166136261;
        foreach (char c in id ?? "") { h ^= c; h *= 16777619; }
        return (int)(h % (uint)samples);
    }
}
