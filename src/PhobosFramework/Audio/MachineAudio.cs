using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Plays the machine loops (Framework 0.119.0). Each voiced machine gets one positional looping source on a
/// child of its own object, set up exactly as the game's own appliance loops are (the vanilla scrubber's emitter: its
/// mixer bus, distance falloff curve and range), so ours fade with distance, follow the game's volume settings and
/// muffle in thin air as its own machines do. Each clip is levelled at load to the scrubber's measured loudness, so a
/// Phobos machine sits in the mix like a vanilla appliance. A loop starts once when its machine starts work and fades
/// out when the work stops; it is never restarted or faded at each repeat. Only the nearest few working machines are
/// voiced, so a room of machines does not become a chorus. No game-state writes and no queued sound.</summary>
internal sealed class MachineAudio : IDisposable
{
    private sealed class Voice
    {
        internal CondOwner Machine = null!;
        internal GameObject Host = null!;
        internal AudioSource Source = null!;
        internal MachineLoop Loop;
        internal float Gain;
        internal bool Wanted;
    }
    private readonly ConfigEntry<float> volume;
    private readonly ConfigEntry<int> voices;
    private readonly Action<string> log;
    private readonly Dictionary<MachineLoop, (AudioClip Clip, double Rms)> clips = new();
    private readonly HashSet<MachineLoop> unreadable = new();
    private readonly Dictionary<string, Voice> playing = new(StringComparer.Ordinal);
    private readonly List<CondOwner> scratch = new();
    private Discovery.WorldFamily? family;
    private AudioListener? listener;
    private UnityEngine.Audio.AudioMixerGroup? mixer;
    private AnimationCurve? falloff;
    private float minDistance = 20, maxDistance = 50, nextSelect;
    private double referenceLevel;
    private bool prepared, failed;
    private const float SelectSeconds = .25f;

    internal MachineAudio(ConfigFile config, Action<string> log)
    {
        this.log = log;
        volume = config.Bind("Audio", "MachineSoundVolume", 1f,
            new ConfigDescription(Text.Get("Audio.machine_volume_help"), new AcceptableValueRange<float>(0, 1)));
        voices = config.Bind("Audio", "MachineSoundVoices", MachineSoundRules.DefaultVoices,
            new ConfigDescription(Text.Get("Audio.machine_voices_help"), new AcceptableValueRange<int>(0, MachineSoundRules.MaxVoices)));
        family = Discovery.WorldFamilies.Register(FrameworkInfo.PluginId + ".machine-sounds", MachineSounds.Has);
    }
    private float Volume => float.IsNaN(volume.Value) || float.IsInfinity(volume.Value) ? 0 : Mathf.Clamp01(volume.Value);
    private int Voices => Math.Max(0, Math.Min(MachineSoundRules.MaxVoices, voices.Value));

    /// <summary>Called every frame: chooses the voiced machines a few times a second, and moves each fade every frame.</summary>
    internal void Poll()
    {
        if (failed) return;
        try
        {
            if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || AudioManager.am == null) { if (playing.Count > 0) Stop(); return; }
            float now = Time.unscaledTime;
            if (now >= nextSelect) { nextSelect = now + SelectSeconds; Select(); }
            Step(Time.unscaledDeltaTime);
        }
        catch (Exception ex) { Fail(ex); }
    }

    /// <summary>The mixer bus, falloff and loudness of the game's own appliance loop, read once.</summary>
    private bool Prepare()
    {
        if (prepared) return mixer != null;
        prepared = true;
        var reference = DataHandler.GetAudioEmitter(MachineSoundRules.ReferenceEmitter);
        if (reference == null || string.IsNullOrEmpty(reference.strMixerName) || !AudioManager.MixerGroups.TryGetValue(reference.strMixerName, out var group) || group == null)
        { log("Machine sounds are off: the game's appliance sound settings could not be read."); return false; }
        mixer = group;
        if (reference.fMinDistance > 0) minDistance = reference.fMinDistance;
        if (reference.fMaxDistance > minDistance) maxDistance = reference.fMaxDistance;
        if (!string.IsNullOrEmpty(reference.strFalloffCurve)) falloff = Resources.Load<AnimationCurveAsset>("Curves/" + reference.strFalloffCurve)?.curve;
        referenceLevel = MachineSoundRules.FallbackReferenceLevel;
        try
        {
            var clip = Resources.Load<AudioClip>("Audio/" + reference.strClipSteady);
            if (clip != null && clip.samples > 0)
            {
                var data = new float[clip.samples * clip.channels];
                if (clip.GetData(data, 0))
                {
                    double rms = MachineSoundRules.Rms(data);
                    if (rms > 0) referenceLevel = reference.fVolumeSteady * rms;
                }
            }
        }
        catch { /* the fallback level stands */ }
        log("Machine sounds levelled to the game's " + MachineSoundRules.ReferenceEmitter + " at " + MachineSoundRules.Dbfs(referenceLevel).ToString("0.0") + " dBFS.");
        return true;
    }
    /// <summary>A loop's clip, read once from its file (0.120.0). A loop that cannot be read stays silent for the
    /// session, said once in the log; every other loop still plays.</summary>
    private (AudioClip Clip, double Rms)? Clip(MachineLoop loop)
    {
        if (clips.TryGetValue(loop, out var known)) return known;
        if (unreadable.Contains(loop)) return null;
        try
        {
            var pcm = SoundFiles.Load(SoundFiles.MachineLoop(loop), MachineSoundRules.MaxLoopSeconds, "machine loop " + loop);
            var clip = AudioClip.Create("Phobos machine " + loop, pcm.Samples.Length, 1, pcm.SampleRate, false);
            if (!clip.SetData(pcm.Samples, 0)) { UnityEngine.Object.Destroy(clip); throw new System.IO.InvalidDataException("Unity refused its samples."); }
            return clips[loop] = (clip, MachineSoundRules.Rms(pcm.Samples));
        }
        catch (System.IO.InvalidDataException ex)
        {
            unreadable.Add(loop);
            log("Machine loop " + loop + " is silent for this session: " + ex.Message);
            return null;
        }
    }

    private void Select()
    {
        foreach (var voice in playing.Values) voice.Wanted = false;
        if (Volume <= 0 || Voices == 0 || family == null || !Prepare()) return;
        // The game hears from its main camera (its listener lives there; cloned cameras have theirs switched off).
        if (listener == null || !listener.isActiveAndEnabled) listener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
        if (listener == null) return;
        var here = listener.transform.position;
        scratch.Clear(); family.Members(scratch);
        var working = new List<(string Id, double DistanceSquared)>();
        var byId = new Dictionary<string, CondOwner>(StringComparer.Ordinal);
        foreach (var co in scratch)
        {
            if (co == null || co.bDestroyed || co.ship == null || (int)co.ship.LoadState < 2 || co.objCOParent != null || !MachineSounds.Working(co)) continue;
            var position = co.transform.position;
            double dx = position.x - here.x, dy = position.y - here.y;
            working.Add((co.strID, dx * dx + dy * dy)); byId[co.strID] = co;
        }
        scratch.Clear();
        foreach (string id in MachineSoundRules.Voiced(working, Voices, maxDistance))
        {
            if (!playing.TryGetValue(id, out var voice) || voice.Source == null) voice = Start(byId[id]);
            if (voice != null) voice.Wanted = true;
        }
    }
    private Voice? Start(CondOwner machine)
    {
        if (!MachineSounds.Entries.TryGetValue(machine.strCODef, out var entry)) return null;
        var loaded = Clip(entry.Loop);
        if (loaded == null) return null;
        var clip = loaded.Value.Clip;
        var host = new GameObject("Phobos machine sound");
        host.transform.SetParent(machine.transform, false);
        var source = host.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = true; source.clip = clip;
        source.outputAudioMixerGroup = mixer; source.spatialBlend = 1; source.dopplerLevel = 0;
        source.minDistance = minDistance; source.maxDistance = maxDistance; source.pitch = entry.Pitch; source.volume = 0;
        if (falloff != null) { source.rolloffMode = AudioRolloffMode.Custom; source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, falloff); }
        source.timeSamples = MachineSoundRules.StartSample(machine.strID, clip.samples);
        source.Play();
        var voice = new Voice { Machine = machine, Host = host, Source = source, Loop = entry.Loop };
        playing[machine.strID] = voice;
        return voice;
    }
    private void Step(float delta)
    {
        if (playing.Count == 0) return;
        List<string>? gone = null;
        foreach (var pair in playing)
        {
            var voice = pair.Value;
            if (voice.Source == null || voice.Machine == null || voice.Machine.bDestroyed) { (gone ??= new()).Add(pair.Key); continue; }
            float full = MachineSoundRules.Gain(referenceLevel, clips[voice.Loop].Rms, Volume);
            voice.Gain = MachineSoundRules.Fade(voice.Gain, voice.Wanted ? full : 0, full, delta);
            if (voice.Source.volume != voice.Gain) voice.Source.volume = voice.Gain;
            if (!voice.Wanted && voice.Gain <= 0) { UnityEngine.Object.Destroy(voice.Host); (gone ??= new()).Add(pair.Key); }
        }
        if (gone != null) foreach (string id in gone) playing.Remove(id);
    }

    /// <summary>Silences everything at once (a game load or exit); nothing is queued to play later.</summary>
    internal void Stop()
    {
        foreach (var voice in playing.Values) if (voice.Host != null) UnityEngine.Object.Destroy(voice.Host);
        playing.Clear(); listener = null; prepared = false; mixer = null; falloff = null;
    }
    private void Fail(Exception ex)
    {
        if (failed) return;
        failed = true;
        try { Stop(); } catch { }
        log("Machine sounds are off for this session: " + ex.Message);
    }
    public void Dispose()
    {
        Stop();
        foreach (var clip in clips.Values) if (clip.Clip != null) UnityEngine.Object.Destroy(clip.Clip);
        clips.Clear();
    }
}
