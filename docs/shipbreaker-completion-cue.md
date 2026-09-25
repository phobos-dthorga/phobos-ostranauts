# Quiet watched-batch completion cue

**Current update:** Shipbreaker 0.19.0 requires Framework 0.21.0, which owns the
shared sample, volume/mute and burst limit. See [shared cues](shared-completion-cues.md).
The 0.18.0 trial below records the original scope; its Shipbreaker-specific audio
configuration/ownership are superseded. The waveform and D4/R4 semantics remain.

Shipbreaker **0.18.0**, prepared 25 September 2026. Owner-authorized first audio
trial; not installed or evaluated in the game's actual mix by this change.

## Using it

On a D4 dismantling fixture or R4 reclaimer's Control Panel, choose **Notify on
next batch completion**. The C1 equipment page and fallback F9/reclaimer panels
offer the same action. Start/resume processing separately: watching never starts
machinery or changes its queue. Closing the panel does not cancel the watch.

One short tone may play when that machine's next batch has physically committed
its products to the output tray. The watch then turns off, even if the queue
continues. The status retains **Watched batch completed**; products may subsequently
move through an existing collector route. This is a batch receipt, not a claim
that the whole queue finished or cargo reached a remote destination.

**Cancel completion notification**, processing Pause/Cancel, a processing fault
or loading a save clears the watch. Already-complete work recovered at Start is
silent. Ordinary waiting for electricity retains the watch. Native item IDs,
inventories, job progress, recipes and saved data are unchanged.

**Completion cue: …%** cycles through 15%, 35%, 60% and mute (0%). The initial
level is 35% of the already quiet sample, additionally scaled by native effects
volume. The persistent BepInEx setting is `[Audio] CompletionCueVolume`, range
0–1. Each watch is optional, even with a nonzero volume. The text works when muted.

At C1, F3 also accepts `phobosindustry watch <console-ID> <equipment-ID>` and
`phobosindustry unwatch <console-ID> <equipment-ID>`, with the normal access checks.

## Restraint and limits

- Only the crew member who requested the watch can hear it while selected, awake
  and still on the machine's own ship. No global announcements across vessels.
- Background/unfocused play, pause, locked UI, loading, social scenes and game-over
  contexts suppress audio. Suppressed events are consumed, never queued for later.
- One source prevents overlapping cues. Completions within three real seconds of
  a played cue are dropped; fast-forward cannot produce a delayed chorus.
- The native effects mixer must be available. Missing audio or a playback error
  leaves production intact; an audio error disables the cue for that session.
- The source uses a low Unity voice priority. There is no verified native
  critical-alarm gate: this does **not** promise suppression whenever an alarm
  sounds. Keep the sample quiet and judge coexistence during owner play.
- No furnace, navigation, cooker, ambient machinery or repeating transfer cues
  are added. The proposed usefulness is **MEDIUM-HIGH**, pending owner judgment.

## Sample and native evidence

The original [completion WAV](../assets/phobos-shipbreaker/audio/completion.wav)
is a 280 ms mono PCM16 waveform at 44.1 kHz: a gentle 440/660 Hz blend under a
smooth envelope, no sampled recording, voice, pitch sweep or impact. The measured
file peak is about -20.3 dBFS before runtime attenuation; this measurement does
not establish perceived loudness or comfort. At default 35% its signal peak is
about -29.4 dBFS before the native mixer. Headphone/speaker gain still matters.

[Generation script](../scripts/synthesize-completion-cue.py) and
[provenance/measurements](../assets/phobos-shipbreaker/audio/completion.json)
retain synthesis parameters and SHA-256 hashes. This is ChatGPT-authored procedural
synthesis, not an audio-model recording. Original source and sample use the
project's MIT licence; no game audio was copied. See the
[audio provenance note](../assets/phobos-shipbreaker/audio/README.md).

**Observed native implementation:** local inspection of
[Blue Bottle Games' Ostranauts](https://bluebottlegames.com/games/ostranauts)
1.0.1.5 `AudioManager.CreateAudioEmitter`, `AudioManager.MixerGroups` and
`audioemitters/audioemitters.json` shows native UI sounds such as `UIGameplayClick`
routed through `PrefsEffects`. Our source resolves that emitter's current mixer
group and uses it for the original clip; it never replaces the emitter or copies
its sample. The local decompilation stays outside Git. No separate native
completion sound was found on the inspected custom D4/R4 batch-commit path; this
is narrower than an exhaustive audit of the game's audio. This is native-code
evidence and our implementation decision, not endorsement or gameplay validation
by Blue Bottle Games.

This single consumer stays within Shipbreaker. Move common presentation support
into Framework if a second justified consumer needs it, following the
[shared animation/sound direction](animation-and-sound-direction.md).

## Verification and owner listening check

Automated checks exercise one-watch consumption, cancelled watches, foreign
crew/ships, restored completed inputs, fresh sessions, mute, real-time spacing,
overlap and a burst of completions. They also decode the shipped WAV, reject
changed/truncated headers and check duration, amplitude and smooth sample edges.
The build checks that waveform and provenance exactly reproduce from the script.
These checks do not execute Unity playback or hear the game's mix.

For owner evaluation, use ordinary listening volume on speakers/headphones:

1. Watch one D4/R4 batch and start it. Confirm the status and one soft cue after
   products appear. Allow the next batch to finish; it should stay silent.
2. Repeat muted, with native sound effects muted, and after reopening the panel.
   Muted completion should remain visible and never replay after unmuting.
3. Pause/cancel or reload before completion. Resume without rearming; expect silence.
4. Watch two machines and use accelerated time. Close completions should yield
   at most one cue within three real seconds, with no deferred playback.
5. Check that native warnings remain clear and the cue reduces progress checking.
   Reduce volume or omit the sound if its usefulness does not justify it.

Prepared packages are not installed automatically. See [installation](installing-mods.md).
