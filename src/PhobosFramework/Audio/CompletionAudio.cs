using System;
using System.IO;
using BepInEx.Configuration;
using BepInEx;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>One optional presentation cue. No game-state writes or pending event queue.</summary>
internal sealed class CompletionAudio : IDisposable
{
    private readonly GameObject host;
    private readonly ConfigEntry<float> volume;
    private readonly Action<string> log;
    private readonly CompletionCueGate gate = new CompletionCueGate();
    private AudioClip? clip;
    private AudioSource? source;
    private bool failed;

    internal CompletionAudio(GameObject host, ConfigFile config, Action<string> log)
    {
        this.host = host; this.log = log;
        float initial = .35f;
        // Preserve a first-trial Shipbreaker mute/level once, without rewriting that file.
        if (!config.ContainsKey(new ConfigDefinition("Audio", "CompletionCueVolume")))
        {
            string legacyPath = Path.Combine(Paths.ConfigPath, "phobosgekko.ostranauts.shipbreaker.cfg");
            if (File.Exists(legacyPath))
            {
                var legacy = new ConfigFile(legacyPath, false) { SaveOnConfigSet = false };
                initial = legacy.Bind("Audio", "CompletionCueVolume", .35f).Value;
            }
        }
        volume = config.Bind("Audio", "CompletionCueVolume", initial,
            new ConfigDescription(Text.Get("Audio.volume_help"), new AcceptableValueRange<float>(0, 1)));
    }
    private float Volume => float.IsNaN(volume.Value) || float.IsInfinity(volume.Value) ? 0 : Mathf.Clamp01(volume.Value);
    internal string VolumeLabel => Text.Get("Audio.volume_label", Math.Round(Volume * 100));
    internal void CycleVolume()
    {
        try
        {
            float current = Volume;
            volume.Value = current == 0 ? .15f : current < .35f ? .35f : current < .6f ? .6f : 0;
            Poll();
        }
        catch (Exception ex) { Fail(ex); }
    }
    internal void Poll()
    {
        if (failed) return;
        try { if (source != null) source.volume = Volume; }
        catch (Exception ex) { Fail(ex); }
    }

    internal void Play(string actorId, string shipId)
    {
        try
        {
            var actor = CrewSim.GetSelectedCrew();
            if (actor == null || actor.bDestroyed || actor.HasCond("IsDead") || actor.HasCond("Unconscious") ||
                actor.strID != actorId || actor.ship?.strRegID != shipId) return;
            if (failed || Volume <= 0 || !Application.isFocused || CrewSim.objInstance == null ||
                !CrewSim.objInstance.FinishedLoading || AudioManager.am == null || CrewSim.bUILock || CrewSim.Paused ||
                CanvasManager.instance == null || CanvasManager.instance.State != CanvasManager.GUIState.SHIPGUI &&
                CanvasManager.instance.State != CanvasManager.GUIState.NORMAL) return;
            // Use the actual native effects bus, never an un-routed fallback which bypasses mute.
            string? mixer = DataHandler.GetAudioEmitter("UIGameplayClick")?.strMixerName;
            if (mixer == null || !AudioManager.MixerGroups.TryGetValue(mixer, out var group) || group == null) return;
            if (!gate.Take(Time.realtimeSinceStartup, Volume, source != null && source.isPlaying)) return;
            if (clip == null) clip = LoadClip();
            if (source == null)
            {
                source = host.AddComponent<AudioSource>();
                source.playOnAwake = false; source.loop = false;
                source.spatialBlend = 0; source.dopplerLevel = 0; source.priority = 255;
            }
            source.outputAudioMixerGroup = group; source.volume = Volume;
            source.clip = clip; source.Play();
        }
        catch (Exception ex) { Fail(ex); }
    }

    private void Fail(Exception ex)
    {
        if (failed) return;
        failed = true;
        // Audio is optional: never propagate into controls/material commits or flood the log.
        try { log("Phobos completion audio disabled for this session: " + ex.Message); } catch { }
    }

    private static AudioClip LoadClip()
    {
        using var stream = typeof(CompletionAudio).Assembly.GetManifestResourceStream("PhobosFramework.completion.wav")
            ?? throw new InvalidDataException("Missing completion cue.");
        var samples = CompletionCuePcm.Read(stream);
        var result = AudioClip.Create("Phobos completion", samples.Length, 1, CompletionCuePcm.SampleRate, false);
        if (!result.SetData(samples, 0)) { UnityEngine.Object.Destroy(result); throw new InvalidDataException("Cannot load completion cue."); }
        return result;
    }
    internal void Stop()
    {
        try { if (source != null) source.Stop(); }
        catch (Exception ex) { Fail(ex); }
    }
    public void Dispose()
    {
        Stop();
        try
        {
            if (source != null) UnityEngine.Object.Destroy(source);
            if (clip != null) UnityEngine.Object.Destroy(clip);
        }
        catch (Exception ex) { Fail(ex); }
    }
}
