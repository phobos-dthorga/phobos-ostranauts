# Occasional machine work sounds: audio and Claude handoff

Prepared **6 October 2026** for phobosgekko. **Audio candidates only:** nothing
has been wired into a mod, installed, published or tested in Ostranauts.

## Owner direction and delivered files

The owner asked Codex to explore where sound would improve the mods, create the
sound files, and prepare a handoff for **Claude to do the wiring**. The owner's
selected direction is **occasional mechanical sounds while equipment works**.
This extends the earlier brief-cue policy to occasional local machinery sounds;
it does not select operating loops, general ambience, an alarm suite or routine
button sounds. Older exclusions in the [25 September direction](animation-and-sound-direction.md)
and [completion guide](../shared-completion-cues.md) retain their historical scope.

Delivered: **18 original one-shot sounds in nine families**, each with variations
A and B, plus a comparison reel and a sparse workshop sketch. Individual sounds
last **0.9 to 1.6 seconds**. These durations, textures, family assignments and
suggested spacing are **agent proposals**, open to the owner's listening judgment.
They represent fictional machinery; they are not scientific acoustic models or
recordings of real equipment.

- [Listen page](../../assets/phobos-audio/work-sounds-v1/listen.html): individual
  samples, two variations per family, and both review tracks. Open in a browser.
- [Comparison reel](../../assets/phobos-audio/work-sounds-v1/review-reel.wav):
  44.5 seconds, families in the table below, A then B, with silence between clips.
- [Sparse workshop sketch](../../assets/phobos-audio/work-sounds-v1/sparse-workshop.wav):
  60 seconds with six separated sounds; an authored timing illustration without
  the game's background audio, not an in-game recording or mix test.
- [PCM16 candidates](../../assets/phobos-audio/work-sounds-v1/wav): potential
  runtime files, 44,100 Hz, mono, signed PCM16 little-endian RIFF WAV.
- [PCM24 masters](../../assets/phobos-audio/work-sounds-v1/masters): retain these
  lossless exports with the [recipes](../../assets/phobos-audio/work-sounds-v1/recipes.json)
  and [manifest](../../assets/phobos-audio/work-sounds-v1/manifest.json).

The listen page starts playback at 35%; the WAV files retain their authored
amplitude. This browser setting is a review convenience, not a proposed game
setting. None of the files loops. The existing watched completion tone is retained.

## Where sound helps most

The owner reports that many machines are quiet relative to the rest of the game.
The source inspection below identifies work boundaries where a sound can describe
real progress. **This was not an in-game listening audit** and does not establish
that a given native operation is silent.

Start with **D4 and R4**: their different textures could let the player recognize
which salvage stage is working without opening a panel. Then consider the W2 and
wet processors, where a short pump stroke can make the ship's support equipment
feel active. Furnace, bottler and hull-mounted machinery can follow if their
samples fit the game's mix. Do not add every candidate merely because it exists.

Filenames below have `-a.wav` and `-b.wav` variants in `wav/`.

| File stem | Sound proposal and intended use | Source boundary to inspect before wiring |
| --- | --- | --- |
| dismantler-work | D4: short loaded motor and rounded ratchet contacts. | [Shipbreaker ProcessingService.AfterPower](../../src/PhobosShipbreaker/ProcessingService.cs): compare actual job progress before/after its powered advance. |
| reclaimer-work | R4: lower loaded drive and granular grinding pass. | Same ProcessingService; select by its existing machine family. Not a new recipe or job clock. |
| furnace-work | F6: subdued electrical load and cushioned contactor. A possible later V4 heating reuse. No flame roar or safe-to-open cue. | [FurnaceService.FinishPower](../../src/PhobosShipbreaker/FurnaceService.cs): distinguish received heating energy from instrument/cooling draw. [Manufacturing ChargeMachine.FinishPower](../../src/PhobosManufacturing/ChargeMachine.cs) for V4; verify its actual recipe and heating context. |
| coolant-pump-work | Powered cooling: enclosed pump stroke. **Passive radiators stay silent.** | FurnaceService's measured motor energy and routed coolant handling. Recheck exact transfer/heat result; positive instrument draw or a warm sink alone does not qualify. |
| wet-processor-work | X2 and LC-3: small recirculation pass. A machine texture, not the sound of a chemical reaction. | [ProcessorService.FinishPower](../../src/PhobosManufacturing/ProcessorService.cs) for X2; ChargeMachine.FinishPower for LC-3. Check positive credited work and the current consumer family. |
| crop-pump-work | Groundwork W2: light pump stroke and valve seat. Emit at the supply, not every connected rack. Firstlight misting is a separate later possibility. | [Agriculture Service.Pump](../../src/PhobosAgriculture/IrrigationService.cs): positive actual movement/dosing, not the demanded budget alone. [MistStep](../../src/PhobosAgriculture/MistingService.cs): positive water consumption, not a growing crop alone. |
| bottler-work | Alembrine Corker-2: compact drive and damped press contact. No pop for each bottle. | [BottlerService.FinishPower](../../src/PhobosManufacturing/BottlerService.cs): positive received energy credited to a working batch. Completion/delivery remains its separate settlement. |
| grabber-work | G4: geared movement heard from an interior service position. No space collision or capture-success signal. | [IntakeService](../../src/PhobosShipbreaker/IntakeService.cs) and [CaptureService](../../src/PhobosShipbreaker/CaptureService.cs): choose the actual powered movement/cutting phase. **Hold if an interior audible position cannot be verified.** |
| laser-cabinet-work | Ablatine ML-2: interior electrical cabinet pulse. No beam zap or weapon shot. | [LaserService.FinishPower](../../src/PhobosShipbreaker/LaserPower.cs): positive credited energy in the working phase. **Hold if an interior audible position cannot be verified.** |

The pump, actuator, contactor and cabinet textures are proposed sound design, not
claims that the game models those components individually. Do not invent a work
phase, transfer, fan, valve or consumption just to justify a sample.

## Other worthwhile possibilities

- **A watched job unexpectedly stops:** a separate soft needs-attention cue could
  reduce missed blockages. Keep the exact reason in text, exclude deliberate Stop
  and ordinary waits, and group simultaneous events. This is a later proposal;
  no new attention or alarm sample is delivered here.
- **Hearth cooker:** first try the existing watched completion cue. A quiet
  heater-load texture might fit later, but the furnace sample must not suggest
  that a meal has finished or that a hot appliance is safe to handle.
- **Crew upkeep:** one occasional tool adjustment during real work may add life,
  provided the native action has no suitable sound. Avoid a sound for every skill
  tick, haul or item moved. No upkeep sample or native coverage audit is delivered.
- **Medical and Auto Nav:** leave the current information and game feedback to
  carry these initially. A heartbeat requires a real, supported timing source;
  treatment and arrival sounds require genuine completed results. Do not imply
  healing, docking or clinical measurements with decorative beeps.
- **Storage, pipes and idle machinery:** silence is useful here. Do not attach
  sounds to passive tanks, line segments, each transfer unit or every crop stage.

These are agent judgments about potential gameplay value, not measured usability
results or directions attributed to the owner.

## Production route and provenance

**Selected route: original procedural synthesis authored with Codex/ChatGPT.**
The [generator](../../scripts/synthesize-machine-audio.py) combines filtered noise,
short motor loads and damped inharmonic contacts. It uses Python's standard
library only, specified xorshift32 seeds and explicit recipes. No samples,
recordings, voices, game assets, external API calls or paid generation were used.
The project [MIT licence](../../LICENSE) covers this original source and audio;
keep the copyright notice with redistribution. The manifest retains source,
recipe, master and export hashes and the measured audio properties.

**PixelLab:** its [official MCP documentation](https://api.pixellab.ai/mcp/docs#create_vocal_animation),
checked 6 October 2026, describes mouth-position images, talking GIFs and sprite
frame plans. Those vocal tools do not create audio. Its
[documented API workflows](https://www.pixellab.ai/docs/ways-to-use-pixellab)
are image and animation workflows; no sound-generation tool was found in the
installed PixelLab tool inventory. This is a documented-capability check, not a
claim about every private or future PixelLab feature.

**OpenAI speech:** OpenAI's [text-to-speech documentation](https://developers.openai.com/api/docs/guides/text-to-speech)
describes converting text into spoken audio. It is a different route from this
nonverbal effects pack. No speech API or audio model was used here.

**If the owner wants a more recorded-machine texture:** ElevenLabs documents a
[dedicated sound-effects generator](https://elevenlabs.io/docs/overview/capabilities/sound-effects)
that creates effects from text descriptions. It is a possible later production
route, not a selected service, evaluated quality claim or purchase. Check account
access, allowance, cost and the actual output's distribution terms before using
it; its outputs must have separate provenance and must not inherit this pack's
MIT statement automatically. Self-recorded mechanical sounds are another option
if the owner supplies original recordings and permission. Neither is needed to
review this first pack.

## Claude integration brief

Read [AGENTS.md](../../AGENTS.md), this document, the current source, and the
[existing completion guide](../shared-completion-cues.md). The owner assigned
implementation to Claude; **Codex's delivery stops at sound files and handoff**.
The pack is ready for audition, not evidence the owner selected every sample.
Preserve rejected/unselected candidates and their records rather than deleting
or silently replacing them. Keep the selected master's hash in the implementation
record when a sample is accepted.

1. Start with the D4/R4 working slice and whichever variations the owner retains.
   Register reusable loading, gain, distance and spacing behaviour in Framework;
   content services identify real work. UI/panel refresh must never trigger audio
   or mutate job state. Keep the implementation proportional to these consumers.
2. Observe **actual positive work after received electricity was accounted for**.
   A Running flag, an installed machine, a power request or a panel saying Working
   is insufficient. Stop, no supply, missing feed, blocked output, faults and waits
   remain quiet. Audio failures must not change progress, inventory or saves.
3. Use sparse real-time spacing, not simulation ticks. Each family's suggested
   interval range is in recipes/manifest; these are starting values for listening,
   not a promised engine rate. Vary spacing and alternate A/B to avoid a metronome.
   Give a newly eligible machine a silent initial delay, stagger machines, allow
   at most one machinery snippet suite-wide, and start with at least six real
   seconds between snippets. Drop competing or overdue sounds; never queue them.
4. Recheck eligibility when a sound would play. Maintain timing state only for
   loaded relevant machines, clear it on unload/stop/dispose, and reset after
   pause, focus loss or reload. Fast-forward/time-skip never schedules accumulated
   events or raises pitch. Loading and reopening a panel are silent. Any random
   audio variation must be isolated from gameplay RNG and has no saved authority.
5. Keep sound local to the selected crew's ship and nearby air-filled machinery.
   Respect pause, focus and loaded gameplay context. Prefer the game's established
   positional emitter behaviour after inspecting it; do not assume camera/listener
   coordinates equal crew position. G4/ML-2 files refer to interior machinery;
   they are not permission to transmit external sounds through vacuum. Hold those
   consumers until their audible service position is established.
6. Keep separate machinery volume/mute and the existing watched completion level.
   Route through the native effects mixer, so native effects mute still works.
   No unrouted fallback. Retain all visible status/reasons. No sounds for button
   clicks, per-item transfers or native docking, and no new warning suite. Give
   game alerts and completion notifications priority; if suppression would require
   guessing an unavailable native alarm API, reduce/omit machine playback instead.
7. Reuse the **format knowledge**, not the current completion reader unchanged:
   [CompletionCuePcm.Read](../../src/PhobosFramework/Audio/CompletionCuePcm.cs)
   rejects payloads longer than **0.5 seconds**. These candidates are longer.
   Add a separately bounded reader or shared explicit length limit with tests;
   preserve completion's existing bounds. Use the PCM16 candidates, not PCM24
   masters or review reels. Load/cache once, not on each work step; dispose clips
   and sources on shutdown. Never play the review tracks at runtime.
8. [Unity's AudioSource documentation](https://docs.unity3d.com/2019.4/Documentation/Manual/class-AudioSource.html)
   documents looping, mixer output, priority and distance controls. These are
   Unity capabilities, not proof of Ostranauts integration. In particular,
   Max Distance does not guarantee silence outside that distance for every
   rolloff mode: use verified native attenuation or explicit audibility checks.
   Author one-shots with loop disabled and no automatic play-on-awake.
9. Audio presentation needs **no new saved structures or migration**. Reuse the
   current job records read-only; do not write audio timing or variants to saves.
   If implementation changes this, document it and add old-record fixtures.
   Record real loaded/active audio costs and memory under the performance rules.
10. When runtime integration adds player-facing functionality, classify it as a
    middle-number feature bump, claim any shared version with other sessions,
    and use the maintained-constants tool. Update the owning changelogs, Workshop
    drafts, generated notes, guides, localisation and language ledger then. This
    unintegrated pack does not change any mod version or release claim. Follow the
    normal build/install boundaries; the owner performs in-game listening tests.

## Offline verification and owner listening checks

Rebuild with `python scripts/synthesize-machine-audio.py`. Verify every generated
file without writing with `python scripts/synthesize-machine-audio.py --check`.
The generator reports obsolete WAVs without deleting them. Keep candidates under
their current names while reviewing; retain the old master if a new revision is
authored. The manifest is generated; edit recipes/source, not its measured fields.

Checked offline: both WAV encodings reopen at the intended rate/channel/length;
all 18 work exports have silent first/last samples, no clipped samples, small DC
offset, documented peaks/RMS and reproducible bytes. Raw work-sample peaks range
from approximately **-25.5 to -20 dBFS** and RMS from **-37 to -32.2 dBFS**. These
are authoring measurements, not perceived volume, hearing-safety guarantees or
proof that native alerts remain audible. **No perceptual listening or Unity
playback test was performed by Codex.** The owner should select, reject or request
revision after audition, then listen in the game's mix following Claude's wiring.

For that first runtime slice, the owner checks:

- Hear occasional D4/R4 work sounds at ordinary speed; distinguish them without
  repeated panel checking. Try speakers and headphones at normal game volume.
- Confirm stopped, idle, starved and blocked machines stay quiet. If a step is
  very short, skipping its sound is preferable to replaying it afterwards.
- Check a room full of machines: no chorus, no regular ticking and enough quiet.
  Leave the room/ship and confirm the intended falloff or silence.
- Test machinery mute, completion mute and native effects mute independently.
  Native warnings and watched completion should remain understandable.
- Pause, change focus, fast-forward, time-skip, reload and reopen panels; there
  must be no backlog or apparent work from a stopped machine.
- Test loss of room air and any approved hull-mounted placement; unsupported
  acoustic routing stays unwired. Confirm native sounds are not doubled.

Keep a sound only if it gives the ship useful life without becoming an annoyance.
The owner can revise the sound palette independently of recipes or game state.
