# Machine work sounds: selected washer/pump audio and Claude handoff

Updated **6 October 2026** for phobosgekko. The owner selected the sound palette
after auditioning the ElevenLabs previews. **Audio files and handoff only:**
no runtime wiring, installation or Ostranauts mix test has been performed.

## Owner selection and saved files

The owner asked Codex to create sound files and Claude to do any wiring. The
first direction was occasional mechanical sounds while equipment works. After
ratchet and pink-noise/bubbling auditions, the owner requested a **washing machine
in good working order, mixing an electric motor with a liquid pump**, approved
the generation spend, then accepted this direction and asked to save all four.

Saved in [washer-motor-pump-v1](../../assets/phobos-audio/washer-motor-pump-v1/README.md):

- [Listen page](../../assets/phobos-audio/washer-motor-pump-v1/listen.html):
  four prepared WAVs, with links to the untouched MP3 downloads.
- [Originals](../../assets/phobos-audio/washer-motor-pump-v1/originals): provider
  MP3s, preserved byte-for-byte. These are not lossless source masters.
- [WAVs](../../assets/phobos-audio/washer-motor-pump-v1/wav): six seconds each,
  mono, 44,100 Hz, signed PCM16 little-endian RIFF.
- [Manifest](../../assets/phobos-audio/washer-motor-pump-v1/manifest.json):
  exact prompt/settings, provider IDs, hashes, export recipe, measured properties
  and costs. Variation letters follow the order of the four auditioned previews.

| Variation | Prepared file | Peak dBFS | RMS dBFS |
| --- | --- | --- | --- |
| A | [washer-motor-pump-a.wav](../../assets/phobos-audio/washer-motor-pump-v1/wav/washer-motor-pump-a.wav) | -17.183 | -26.847 |
| B | [washer-motor-pump-b.wav](../../assets/phobos-audio/washer-motor-pump-v1/wav/washer-motor-pump-b.wav) | -12.563 | -22.368 |
| C | [washer-motor-pump-c.wav](../../assets/phobos-audio/washer-motor-pump-v1/wav/washer-motor-pump-c.wav) | -19.933 | -25.776 |
| D | [washer-motor-pump-d.wav](../../assets/phobos-audio/washer-motor-pump-v1/wav/washer-motor-pump-d.wav) | -18.957 | -25.815 |

Export adds a **25 ms fade-in and 80 ms fade-out**, with no loudness normalisation,
pitch change or synthesis. The WAVs have zero first/last samples and no clipped
samples. B's measured RMS is higher, so do not assume one gain setting makes all
four equally loud. These measurements are not a game mix or perceived-volume test.

**The owner retained all four; no single variant has been selected.** The owner
auditioned the provider previews; the prepared WAVs differ only by mono decoding
and edge fades. Their exact playback level and consumer assignments still need
owner listening after Claude's implementation.

The six-second source captures are short review textures, generated with looping
disabled and faded for one-shot use. They are **not seamless loops**. Accepting a
background texture does not by itself change the earlier occasional-playback
direction. Continuous operating playback needs an explicit later owner direction.

## Session-only removal and provenance

The owner explicitly requested deletion of **only the Python audio samples made
earlier in this chat**. Removed: 18 candidate WAVs, 18 PCM24 masters and two review
tracks, **38 files total**, under assets/phobos-audio/work-sounds-v1.

Each removed path was introduced by this chat's commit **b8331d5e** and matched
the original manifest hash before deletion. The
[removal record](../../assets/phobos-audio/washer-motor-pump-v1/retired-session-audio.json)
retains those paths/hashes. The older Shipbreaker completion cue was left intact
and its hash checked. The old pack's text records remain labelled historical;
its generator now requires an explicit restoration flag, so routine invocation
cannot recreate the owner-discarded samples. No ratchet or pink-noise provider
files were downloaded or deleted from ElevenLabs.

ElevenLabs' [sound-effects documentation](https://elevenlabs.io/docs/overview/capabilities/sound-effects)
describes effects generated from text prompts. These are model-generated,
fictional machine textures, not real washing-machine recordings or a scientific
model of our equipment. Prompt:

> A well-maintained washing machine running smoothly, steady low electric motor
> hum blended with a soft liquid circulation pump and muted water flow, smooth,
> even and subdued.

Provider: ElevenLabs Sound Effects v2, model eleven_text_to_sound_v2.
Parameters: duration_seconds = 6, prompt_influence = 0.75, loop = false.
The selected round cost **240 credits**; preceding ratchet and pink-noise rounds
cost 80 and 120 respectively, **440 total**. Saving/exporting caused no new
generation or additional credits.

Attribution: **ElevenLabs — elevenlabs.io**. Preserve the pack's
[licensing record](../../assets/phobos-audio/washer-motor-pump-v1/LICENSING.md).
ElevenLabs' [publication guidance](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform)
distinguishes free and paid generation-time plans. The owner reported a
subscription, but the plugin exposes no account-plan or agreement evidence.
Do not give these files the procedural pack's MIT claim or infer commercial
rights from the credit balance. The audio directory is explicitly excluded
from the repository's general MIT grant.

## Proposed first consumers

**Agent proposal:** start with wet processing or a powered pump whose operation
fits the selected motor-and-liquid texture. This replaces the initial proposal
to start with a D4/R4 ratchet/grinder palette. The owner has chosen a sound
direction, not approved every equipment assignment.

| Possible consumer | Source boundary for Claude to inspect | Qualification |
| --- | --- | --- |
| Manufacturing X2 wet processing | [ProcessorService.FinishPower](../../src/PhobosManufacturing/ProcessorService.cs) | Positive received energy credited to an actual working batch. Check the current service and recipe; the sound does not invent a chemical reaction or new hardware. |
| Manufacturing LC-3 | [ChargeMachine.FinishPower](../../src/PhobosManufacturing/ChargeMachine.cs) | Confirm the current machine family and positive credited wet-process work. |
| Agriculture Groundwork W2 | [Service.Pump](../../src/PhobosAgriculture/IrrigationService.cs) | Positive actual solution movement/dosing. Emit at the supply, not every rack. |
| Powered cooling | [FurnaceService.FinishPower](../../src/PhobosShipbreaker/FurnaceService.cs) | Positive measured motor energy and actual coolant handling. Passive radiators and instrument-only draw stay quiet. |

This table is source-oriented handoff advice, **not an in-game listening audit**.
Do not attach the liquid texture to a dry dismantler, furnace heating, tank,
pipe segment or hull-mounted machine merely because a candidate exists.
The earlier broader survey is retained in
[the original handoff at b8331d5e](https://github.com/phobos-dthorga/phobos-ostranauts/blob/b8331d5e/docs/development/machine-work-sounds-handoff.md),
as history; its procedural file links and nine-family delivery are superseded.

Later possibilities remain proposals: a separate soft cue for an unexpectedly
stopped watched job, an occasional upkeep tool adjustment if native feedback is
missing, or the existing watched completion cue for the Hearth cooker.
No new warning/medical/navigation cue is approved or delivered by this pack.

## Claude integration brief

Read [AGENTS.md](../../AGENTS.md), this document, the current source, and the
[shared completion-cue guide](../shared-completion-cues.md). Codex creates the
audio and handoff; **Claude implements the game wiring**.

1. Use the prepared PCM16 WAVs. Preserve the originals, IDs, hashes and licence
   record. Record the adopted variant(s) and gain in the implementation record.
   Do not package the removed procedural samples, provider MP3s, or listen pages
   as runtime audio. No new sound generation is required for this first slice.
2. Reuse or add concrete shared audio loading/mixing/spacing in Framework when
   the first content consumer needs it. Content services identify real work.
   Panels and UI refresh must not trigger sound or mutate job state.
3. Observe actual positive work **after received electricity is accounted for**,
   or a measured pump transfer. A Running flag, installed machine, requested
   power or panel text is insufficient. Idle, Stop, faults, starvation and
   blocked output stay quiet. An audio failure never changes work or inventory.
4. Use sparse real-time scheduling. **Agent starting proposal:** 18–45 seconds
   between eligible snippets per machine, staggered; one machinery snippet
   suite-wide at a time, with at least six seconds of quiet after its end.
   Drop competing/overdue events; never queue them. The owner may revise this
   after listening. Six-second files do not justify uninterrupted playback.
5. Recheck work eligibility when playback starts and stop/fade the source if
   the machine stops, loses power/air or unloads. No apparent continued work.
   Reset scheduling after pause, focus loss and load; fast-forward/time-skip
   must not produce a backlog or change pitch. Panel opening and save load
   stay silent. Audio variation has no gameplay RNG or saved authority.
6. Keep it local to nearby machinery on the selected crew's ship and inside
   an air-filled room. Verify the native emitter/listener behaviour; do not
   assume camera coordinates equal crew position. Keep unrelated ships and
   external vacuum machinery silent. These files approve no acoustic routing
   for the G4 or ML-2.
7. Keep separate machinery volume/mute and the existing watched completion
   level. Route through the native effects mixer; respect its mute. **Agent
   starting proposal:** 0.1 linear clip gain (about -20 dB) before positional
   attenuation, then owner mix review. B may need extra attenuation. This is
   a conservative trial value, not a measured match to Ostranauts volume.
   Preserve text/status reasons, and give native alerts/completion priority.
8. [CompletionCuePcm.Read](../../src/PhobosFramework/Audio/CompletionCuePcm.cs)
   currently rejects audio longer than **0.5 seconds**. Do not pass these
   six-second WAVs through it unchanged. Add a separately bounded reader or
   a shared explicit limit with meaningful format/length tests; retain the
   completion cue's existing bounds. Cache once and dispose clips/sources on
   shutdown. Decoder success is not proof the game can play the sound.
9. [Unity's AudioSource documentation](https://docs.unity3d.com/2019.4/Documentation/Manual/class-AudioSource.html)
   supports mixer routing, priority and distance controls. These are engine
   capabilities, not verified Ostranauts integration. Max Distance is not a
   universal hard silence boundary for every rolloff mode; use verified native
   attenuation or an explicit audibility check. Set loop and play-on-awake false.
   Measure actual loaded/active memory and CPU cost under the repository rules.
10. This presentation needs **no new saved structures or migrations**. Read
    existing work records; keep timing/variants transient. If implementation
    changes that, document it and use old-record fixtures. A runtime feature
    takes the middle-number bump, with the maintained-constants tool and owning
    changelog, Workshop draft, localisation, guides and audit updates. This
    assets-only selection changes no mod version or release readiness.

## Verification and owner checks

Export the saved originals with
`python scripts/export-audio-candidates.py --write --ffmpeg <executable>`.
Check the retained MP3 hashes, WAV format/properties/hashes and exporter hash
with `python scripts/export-audio-candidates.py --check`.
This is decoding and edge preparation, not Python sound synthesis.

Checked offline: all originals decode; all four WAVs have 264,600 mono PCM16
samples at 44,100 Hz (six seconds), zero endpoint samples, no clipped samples,
documented peaks/RMS and recorded hashes. Original MP3 bytes are unchanged.
The current export reruns reproducibly with the recorded FFmpeg build.
**No Unity playback or in-game mix test was performed by Codex.**

The owner should check the first wired consumer:

- The texture feels like working equipment at ordinary speed and normal volume.
  Compare A–D on speakers/headphones; choose variants and gain in the game mix.
- Idle, stopped, starved and blocked machines are quiet. Playback ends when real
  work ends. A short operation can skip its sound rather than replay it later.
- A room full of machines has quiet gaps, no chorus and no steady metronome.
  Leaving the room/ship gives the intended attenuation or silence.
- Machinery mute, completion mute and native effects mute behave separately;
  native alerts and watched completion remain clear.
- Pause, focus changes, fast-forward, time-skip, reload and panel reopening
  produce no backlog. Loss of room air stops the machine sound.

Keep the palette useful and quiet. The sound can be revised without changing
recipes, work progress or saved records.
