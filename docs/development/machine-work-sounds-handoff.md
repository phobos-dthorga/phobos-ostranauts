# Machine work sounds: eight washer/pump loops and Claude handoff

Updated **6 October 2026** for phobosgekko. **Audio files and handoff only:**
no runtime wiring, installation, Unity playback or Ostranauts mix test by Codex.

## Current owner direction

The owner selected the ElevenLabs **washing machine in good working order,
blending an electric motor with a liquid pump** direction after the earlier
ratchet and pink-noise/bubbling auditions. A–D were accepted as a palette;
no individual winner was selected. The owner then requested **four additional
variations in the same likeness**, explicitly required **loopable audio**, and
confirmed a total of **eight distinct samples**. E–H are the new four.

The loop requirement supersedes the earlier occasional-snippet direction,
the proposed 18–45 second scheduling and the old operating-loop exclusion.
Use continuous local playback while eligible equipment actually works.
Codex supplies audio and this handoff; **Claude handles all game wiring**.
New E–H, prepared A–H loop playback, consumer assignments and gain await owner
listening. General ambience, alarm suites and routine per-action audio are
outside this delivery.

## Saved assets and loop preparation

Saved in [washer-motor-pump-v1](../../assets/phobos-audio/washer-motor-pump-v1/README.md):

- [Listen page](../../assets/phobos-audio/washer-motor-pump-v1/listen.html):
  eight prepared WAVs with repeating playback; compare several cycles.
- [loops/](../../assets/phobos-audio/washer-motor-pump-v1/loops): **runtime
  candidates A–H**, 5.75 seconds each, mono 44,100 Hz signed PCM16 RIFF WAV.
- [originals/](../../assets/phobos-audio/washer-motor-pump-v1/originals):
  eight untouched six-second provider MP3s, not lossless source masters.
- The previous six-second A–D **one-shot** exports under `wav/` were deleted
  at the owner's request. The manifest records their old paths and hashes.
- [Manifest](../../assets/phobos-audio/washer-motor-pump-v1/manifest.json):
  prompts/settings, all provider IDs, source/export hashes, measured levels,
  joins, credit costs and the complete export recipe.

| Variation | Loop file for Claude | Peak dBFS | RMS dBFS |
| --- | --- | --- | --- |
| A | [washer-motor-pump-a.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-a.wav) | -17.270 | -26.745 |
| B | [washer-motor-pump-b.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-b.wav) | -12.569 | -22.275 |
| C | [washer-motor-pump-c.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-c.wav) | -19.918 | -25.795 |
| D | [washer-motor-pump-d.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-d.wav) | -19.152 | -25.797 |
| E | [washer-motor-pump-e.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-e.wav) | -18.242 | -25.153 |
| F | [washer-motor-pump-f.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-f.wav) | -12.841 | -22.803 |
| G | [washer-motor-pump-g.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-g.wav) | -14.194 | -23.111 |
| H | [washer-motor-pump-h.wav](../../assets/phobos-audio/washer-motor-pump-v1/loops/washer-motor-pump-h.wav) | -17.084 | -25.541 |

A–D's provider requests used loop = false; E–H used **loop = true**, as supported
by ElevenLabs' [Sound Effects documentation](https://elevenlabs.io/docs/overview/capabilities/sound-effects).
Every prepared loop is decoded from its untouched original, using an explicit
left/right average, **250 ms circular raised-cosine crossfade**, uniform DC
removal and PCM16 export. The overlap reduces six seconds to **253,575 samples
(5.75 seconds)**. No silence, per-cycle fade, loudness normalisation, pitch
change or synthesized sound is added. All originals remain intact. The old
one-shot exports have been deleted. Python prepares files only; it creates no
new sound.

The circular crossfade places both joins on originally adjacent source samples.
Offline checks find zero clipped samples, no silent boundary and wrap steps
below each file's ordinary 99th percentile sample step. The 40 ms boundary RMS
is within 1.4 dB of whole-clip RMS in all eight. These are signal checks, **not
proof of an inaudible perceived repeat**. Review several cycles for a recurring
swell, tonal shift or motif. B, F and G have higher measured RMS than some
others; equal gain will not give equal apparent loudness.

These PCM WAVs have no MP3 padding at the wrap. Endpoints need not be zero or
identical; forcing them to zero adds a dip. Fade **playback gain at activation
and stopping only**, never at each repeat. Browser repeating playback is a
review aid, not evidence of gap-free native game playback.

## Provenance, cost and earlier removal

Provider: **ElevenLabs Sound Effects v2**, model eleven_text_to_sound_v2.
This is model-generated fictional machinery, not a washing-machine field
recording or a scientific model of our equipment. Exact prompt for both rounds:

> A well-maintained washing machine running smoothly, steady low electric motor
> hum blended with a soft liquid circulation pump and muted water flow, smooth,
> even and subdued.

Both rounds: duration_seconds = 6, prompt_influence = 0.75. Original A–D node
MqQuYiyCJ0I7WawkJYL9 used loop = false; new E–H node Qhn6H67S5fQYW6p5fOnm used
loop = true. Shared flow P3PdhUE0PUsdogoEqt3H. Each round cost **240 credits**:
**480 for the eight washer variants**, **680 for this chat** including ratchet
(80) and pink-noise (120). Local export/preparation adds no generation charge.

Attribution: **ElevenLabs — elevenlabs.io**. Preserve the
[licensing record](../../assets/phobos-audio/washer-motor-pump-v1/LICENSING.md)
and IDs/hashes. ElevenLabs' [publication guidance](https://help.elevenlabs.io/hc/en-us/articles/13313564601361-Can-I-publish-the-content-I-generate-on-the-platform)
distinguishes generation-time plans; the owner reported a subscription but the
plugin does not expose plan/agreement evidence. These files are excluded from
the repository's general MIT grant; do not apply the procedural pack's licence.

The owner explicitly removed only the Python audio from earlier in this chat:
18 candidate WAVs, 18 PCM24 masters and two review tracks, **38 total**, under
assets/phobos-audio/work-sounds-v1. Each was introduced by commit **b8331d5e**
and matched its manifest hash before deletion. The
[removal record](../../assets/phobos-audio/washer-motor-pump-v1/retired-session-audio.json)
preserves those paths/hashes. They remain removed; the older Shipbreaker
completion cue is unchanged. Historical text/recipes remain labelled retired,
and their generator requires an explicit restoration flag. No ratchet or
pink-noise files were downloaded or deleted from ElevenLabs.

In a later owner request on **6 October 2026**, Codex also removed the four
faded one-shot WAV exports from `wav/`. These are separate from the 38 earlier
procedural files. The manifest keeps the former one-shot paths, hashes and
export recipe as history; `scripts/export-audio-candidates.py` now writes and
checks **only** the eight files in `loops/`.

## Proposed first consumers

**Agent proposal:** start with wet processing or a powered pump whose operation
fits the motor-and-liquid texture. The owner selected a sound direction, not
all equipment assignments. Inspect current services before choosing.

| Possible consumer | Source boundary for Claude to inspect | Qualification |
| --- | --- | --- |
| Manufacturing X2 wet processing | [ProcessorService.FinishPower](../../src/PhobosManufacturing/ProcessorService.cs) | Positive received energy credited to a working batch. Check the current recipe; audio invents no chemical reaction or hardware. |
| Manufacturing LC-3 | [ChargeMachine.FinishPower](../../src/PhobosManufacturing/ChargeMachine.cs) | Confirm the machine family and positive credited wet-process work. |
| Agriculture Groundwork W2 | [Service.Pump](../../src/PhobosAgriculture/IrrigationService.cs) | Actual solution movement/dosing. Emit at the supply, not every rack. A brief dose does not justify endless running sound. |
| Powered cooling | [FurnaceService.FinishPower](../../src/PhobosShipbreaker/FurnaceService.cs) | Positive motor energy and actual coolant handling. Passive radiators and instrument-only draw stay quiet. |

This is source-oriented advice, not an in-game listening audit. Do not attach
a liquid texture to a dry dismantler, heating element, tank or pipe simply
because an audio candidate exists. The
[original survey at b8331d5e](https://github.com/phobos-dthorga/phobos-ostranauts/blob/b8331d5e/docs/development/machine-work-sounds-handoff.md)
is history; its procedural assets and nine-family delivery are superseded.
Later quiet suspension/upkeep/completion cues remain proposals, not delivered
or newly authorised medical/navigation/warning sounds.

## Claude integration brief

Read [AGENTS.md](../../AGENTS.md), this record, the current source and the
[completion-cue guide](../shared-completion-cues.md). Retain completion audio's
separate purpose and controls. No new generation is needed for this first slice.

1. Use **loops/washer-motor-pump-a.wav through -h.wav** as runtime candidates.
   Preserve all originals, the removed-export record, IDs, hashes and terms;
   record the adopted variants and gain. Package only selected loop WAVs, not provider
   MP3s, listen pages or removed procedural samples.
2. Framework owns shared loading, mixing and emitter lifetime when the first
   consumer needs them. Content services identify actual work. Panels and UI
   refresh never trigger audio or mutate job state. Audio failure must not
   affect work, inventory, electricity, heat or saved records.
3. Establish eligibility after **positive received electricity is credited to
   actual work**, or a measured pump transfer. Running flags, requested power
   and text are insufficient. Maintain the loop only while that validated
   operation continues; do not latch one old positive event forever. Inspect
   service cadence so intermittent checks neither restart sound each tick nor
   imply continued work during idle, starvation, blocked output or faults.
4. Use one persistent source per audible eligible machine with **loop = true
   and playOnAwake = false**. Start once, rather than PlayOneShot every update
   or manually scheduling each 5.75-second repeat. Select a variant once per
   activation; never hard-switch files at each wrap. Variety is presentation,
   with no gameplay RNG or new saved authority.
5. Smooth source gain at activation and Stop/power loss/air loss/unload only.
   **Agent trial proposal:** 150 ms attack and 200 ms release, shortened if
   shutdown/disposal requires it. Never fade the file every cycle. Dispose or
   stop promptly when the machine unloads. Load and panel opening must not
   auto-play; resume only after current work passes its own checks.
6. Keep playback local to the selected crew's ship, nearby machinery and an
   air-filled room. Verify native listener/emitter placement and attenuation;
   camera coordinates are not assumed to be crew coordinates. Keep unrelated
   ships and vacuum machinery silent. No G4/ML-2 acoustic routing is approved.
7. Keep machinery volume/mute separate from watched completion cues, and route
   through the native effects mixer with its mute respected. **Agent trial
   proposal:** 0.1 linear gain (about -20 dB) before attenuation; adjust per
   variant after owner mix review. Text/status reasons remain available.
   Native alerts and completion must stay audible over operating machinery.
8. Avoid a room of identical phased copies. **Agent proposal:** bound audible
   voices initially to the two nearest eligible machines, with stable selection
   and smooth gain changes; stagger the starting playback position for copies
   of the same loop. The voice limit is provisional, not an owner requirement.
   Cache clips once. Do not queue missed audio, change pitch with game speed
   or play a backlog after pause, focus change, reload or time-skip. Check the
   native pause behaviour and real-time work state before resuming.
9. [CompletionCuePcm.Read](../../src/PhobosFramework/Audio/CompletionCuePcm.cs)
   rejects files longer than **0.5 seconds**. Use a separately bounded reader
   or shared explicit limit for 5.75-second loops, with meaningful format and
   length tests; retain the cue's bounds. [Unity's AudioSource documentation](https://docs.unity3d.com/2019.4/Documentation/Manual/class-AudioSource.html)
   describes looping, mixer routing, priority and distance controls, but their
   Ostranauts integration is untested here. Max Distance is not universally
   hard silence; use verified native attenuation or explicit audibility checks.
   Eight decoded mono float clips alone take about 7.74 MiB; measure actual
   loaded/active memory and CPU on the owner's best-case PC under repo rules.
10. This presentation needs **no new saved structures or migrations**. Keep
    variant/source state transient and read existing work records. If wiring
    changes that, document it and test old records. Claude's runtime feature
    takes the middle-number bump through maintained constants, with owning
    changelog, Workshop draft, guides, localisation and audit updates. This
    assets-only delivery changes no mod version or release readiness.

## Verification and owner listening

Rebuild with `python scripts/export-audio-candidates.py --write --ffmpeg <executable>`;
check with `python scripts/export-audio-candidates.py --check`.
The latter verifies all original hashes, format, levels, boundary metrics,
export hashes and exporter hash. The eight loop files have 253,575 samples
at 44,100 Hz, no clipped samples or silent wrap. All eight MP3 originals,
loop WAVs and the older completion cue are unchanged. The four one-shot WAVs
are absent. Export is reproducible with the
recorded FFmpeg build. No Unity/game listening or performance test was run.

Owner checks after Claude wires the first consumer:

- Compare A–H on speakers/headphones for several repeats, then in the game.
  Choose variants/gain; reject a noticeable cyclical swell, click, gap or motif.
- Work sounds continuously while eligible work continues, with no fade/dip
  added each cycle. Stop, idle, starvation, blocked output, power/air loss and
  unload end it smoothly and promptly. Short dosing should not sound perpetual.
- Many machines remain tolerable, without a phased chorus. Leaving the room
  or ship gives the intended attenuation/silence. Native alerts remain clear.
- Machinery mute, completion mute and native effects mute work as intended;
  mute never changes machine operation or displayed status.
- Pause, focus changes, speed changes, time-skip, reload and panel reopening
  neither produce a backlog nor imply work that is not occurring.

The owner performs perceptual and gameplay tests. Audio can be replaced without
changing recipes, work progress or saved records.
