# Quiet completion cues across Phobos mods

Owner-authorized expansion, 25 September 2026. Prepared versions: Framework
0.21.0, Shipbreaker 0.19.0, Agriculture 0.8.0 and Auto Nav 0.14.0. These are
source/package candidates, not installed or in-game listening-validated releases.

The unchanged original 280 ms tone has one playback channel in Framework.
All uses remain explicitly watched, brief and quiet. Proposed usefulness is
MEDIUM-HIGH: reducing repeated progress checking during a deliberate task.
That is a design judgment, not measured usability; mute or remove unwanted cues.

## Selected events

| Operation | Opt-in | Meaning |
| --- | --- | --- |
| Shipbreaker D4 / R4 | Notify on next batch completion, local panel/C1/F9 | One batch's products committed to its output tray; not queue completion or delivery to another machine. |
| Agriculture Hearth cooker | Start, then Notify when this meal or crop is ready | Meal physically delivered and state saved; blocked output is not success. |
| Agriculture Firstlight rack | Start a crop, then the same notification action | Whole cohort changed to harvest-ready after simulation and save. No automatic harvest. Includes the seed-producing cohort. |
| Auto Nav Approach / Rendezvous | Engage, then Details → Notify when this approach / rendezvous arrives | ARRIVED result saved. Arrival retains configured distance/speed meaning; it is not docking. |

Agriculture exposes the same actions through its C1 equipment provider and F3
interface. Auto Nav F3 adds `phobosnav watch`, `phobosnav unwatch` and
`phobosnav cue-volume`. Watches never start/resume operations. Each is consumed
once, even if sound is suppressed. Every use retains visible notification status.

**Intentionally silent:** native docking/clamping, Approach & Dock staging,
indefinite Follow, fire control, routine buttons, manual harvest interactions,
pipes/pumps/transfers, continuous queues and individual growth stages. The F6's
heating, cooling, equalization and manual release gates do not have a single
general success meaning, so this tone is not applied there. Manufacturing remains
a scaffold; retired Approach Assist is not extended.

**Observed native evidence:** local inspection of
[Blue Bottle Games' Ostranauts](https://bluebottlegames.com/games/ostranauts)
1.0.1.5 `Ship` docking code finds `ShipDockClamp` playback. We preserve that
feedback rather than layering a completion cue over it. `AudioManager` and the
native `UIGameplayClick` emitter identify the effects mixer used for our original
clip. The local decompilation stays outside Git. The inspected custom
meal/growth/arrival result paths have no equivalent completion cue; this is not
an exhaustive audio audit or an endorsement by Blue Bottle Games.

## Shared controls and restraint

**All Phobos completion cues: …%** changes the suite's single level. Agriculture's
**Change shared Phobos cue volume / mute** reports the level in its readout.
Levels cycle through 15%, 35%, 60% and mute. Default 35% scales the already quiet
sample before native effects volume. Configuration belongs to Framework:
`[Audio] CompletionCueVolume`, range 0–1. If no shared setting exists, an existing
first-trial Shipbreaker level/mute seeds it once without rewriting the old file.

One source and a three-real-second quiet interval apply across all mods. Nearby
events are dropped, never queued. Text remains available for each operation.
The requesting crew must be selected, awake and on the same ship; Auto Nav
watches belong to the piloting player. Loading, pause, unfocused play, locked UI
and unsuitable screen contexts suppress sound. Watches are never serialized.
Pause/Stop, faults, suspension and reload cancel the relevant watch. Ordinary
power shortage can retain it while the operation remains running.

Playback requires the native effects bus. Audio failure cannot interrupt jobs.
Low voice priority and the short quiet sample reduce interference, but do not
guarantee suppression during every native warning. No alarm suite is added.

## Implementation and checking

Framework owns `Audio.CompletionWatch`, the embedded WAV, player, volume, scope
checks and spacing. Content services establish completion facts and check access;
panels delegate actions and show results. Never infer completion from translated
text or panel refresh. No recipe, inventory, flight rule or save schema changes.

The original [audio records](../assets/phobos-shipbreaker/audio/README.md) retain
the waveform's provenance: ChatGPT-authored procedural synthesis under the project
MIT licence, not a recording or audio-model voice. Only Framework embeds it at
runtime. Documentation previews are not separate audio players. See the
[first-trial guide](shipbreaker-completion-cue.md).

Automated checks cover separate producers sharing one channel, consumed watches,
scope, mute, reload, malformed audio, the compiled provider's actual embedded
sample, and navigation persistence for arrival, abort and Stop. Existing
agriculture checks cover growth/readiness and saves. These do not execute Unity
playback or establish perceived loudness.

Owner checks: start/watch each selected task; confirm one cue and the correct
text. Test mute/native effects mute, simultaneous completion across mods,
fast-forward, reopened panels and reload. Confirm no docking-stage cue or
repeated pings from queues/mature racks, and that native warnings remain clear.
Keep each cue only if it reduces checking without distraction.
