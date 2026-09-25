# Phobos Shipbreaker changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Correct the F6 operating guide's direct versus powered-pipe cooling behavior, circulation/feed demand and damaged-pump limits. Point current-version checks to maintained references rather than historical installation notes.
- Clarify that Manufacturing and autonomous G4 reclamation are separate future implementation tracks; current material receipt does not authorize repeated furnace batches or replace motion interlocks.
- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Research and planning

- Document selected-G4 autonomous reclamation after the player's manual valuables pass, with exact native IDs, supported leftover processing and finite storage/reject accounting. This is a specification, not delivered cutting or automation.
- Select Auto Nav with existing N1/N2 hardware as a required dependency for the future implementation; record loader, packaging and installer work without changing current candidate requirements or saved identities.
- Prioritize active G4 positioning, holding/cutting and movement along a short ordinary-wall section; docking/capture remains optional. Record unresolved collision-compatible reach, native uninstall/transfer risks and a proposed review of Phobos' any-thrust furnace pause for concurrent processing within checked motion/power/thermal limits. Existing runtime policy is unchanged; repeated batches and whole-wreck completion remain later gates.
- Record [NASA Goddard's Raven research](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/) and [ESA's LIRIS experiment by Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before) as sensing context, separate from Blue Bottle Games engine evidence and authored gameplay choices. No endorsement or gameplay validation is implied.
- See the [research](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-autopilot-research.md) and [staged implementation handover](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-autopilot-handover.md). No installation or publication in this round.

## [0.20.0] - 2026-09-26 - Draft

### Added

- Allow registered content-owned cargo in the existing Residue Collector under its unchanged four-slot and 52 kg limits. Expose the existing hull/exterior/floor mount check for optional attachments.
- Agriculture 0.9.0 can attach Ship's Water 0.16.1 Recycler wet-reject capture to the same exclusive inlet. Collector hardware, industrial residue identities, default filters and automatic industrial routing retain their meaning. Remove wet packets manually.

### Requirements

Phobos Framework 0.22.0 or newer. Agriculture and [Valtora's Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189) remain optional. No new Shipbreaker equipment artwork or industrial recipe; live attachment behavior awaits owner testing.

## [0.19.1] - 2026-09-26 - Draft

### Fixed

- Mitigate saved-room misassignment triggered by pending Shipbreaker construction at a ship's grid edge. Restore checked saved grid bounds before native room and zone loading; retain native construction restoration, saved atmosphere, identities and progress.
- The owner's G4 marker explains the observed 63-to-62-column mismatch and all four wrong tile indices. Apply only with spawned Shipbreaker installation markers; unrelated ships, matching grids and unsupported geometry are left unchanged.

### Requirements

Phobos Framework 0.21.1 or newer. No saved-state migration or save-file editing. This cannot recover gas already lost before saving. Offline regression and native loader-contract checks passed; the owner reported a successful affected-save reload on 26 September 2026. Broader save/reload coverage remains unverified. See the [investigation and owner check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-room-load-mitigation.md).

## [0.19.0] - 2026-09-25 - Draft

### Changed

- Use Framework's common completion channel and volume/mute. Preserve the original D4/R4 one-batch meaning; simultaneous events from other Phobos mods cannot overlap.

### Requirements

Phobos Framework 0.21.0 or newer. No recipe, inventory or save changes. Listening evaluation remains pending.

## [0.18.0] - 2026-09-25 - Draft

### Added

- Optional notification for one explicitly watched D4/R4 batch, after products are committed to the output tray. Further queued batches remain silent; completion also remains visible in text.
- Original 280 ms quiet procedural completion tone through the native sound-effects mixer, with volume/mute in the panels and configuration. Same selected crew and ship only; pause, fault or reload clears watches. Suppressed events never replay; nearby completion bursts are dropped.
- Local Control Panel, C1, F9/reclaimer fallback and C1 F3 watch/unwatch controls. Reclaimer fallback content scrolls to keep controls reachable.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.20.0 or newer. No new mod dependency or saved-state migration.

### Known limits

- Prepared first audio trial; owner listening and gameplay evaluation remain pending. No furnace, cooker, navigation or ambient cues. Low playback priority is not a native critical-alarm suppression guarantee.

### Fixed

- Fixed unreachable INSTALL entries; machinery appears under APPS, cooling hardware and conduits under HVAC, and the C1 console under CTRL, including damaged forms. Existing inputs, placement and saved IDs are preserved.

## [0.17.0] - 2026-09-25 - Draft

### Added

- Route exact aluminium feed from a separate R4 metals output to the F6. Stop at twenty pieces; receiving never starts or releases a batch.
- Collect released, cold housing blanks and melt remainders with an explicit furnace-products collector filter. Existing residue routes, default filters and tray limits retain their meaning.
- Account furnace feed motors through the existing electrical receipt, including partial supply and retained motor heat. Preserve cargo on full destinations and pause receiving on reload or failed interlocks.
- Expose material pairing and receiving through local panels, C1 and F3, with rotated front-corner installation markers.
- Add optional finite coolant servicing: fill/drain and captured leakage, with a conservative lumped heat model. Existing sealed cooling installations remain supported.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.20.0 or newer. Agriculture and Auto Nav remain optional.

### Known limits

- Prepared candidate; owner gameplay and appearance checks remain outstanding.
- Individual aluminium feed only; crew separate native stacks. No automatic Seal, Start, Equalize or Release.
- A housing blank fills the existing collector grid; haul it away before the next item can fit.
- No new finished-part recipe, arbitrary container routing or generated artwork. Manufacturing remains separate.

### References

- [Furnace routing guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/furnace-material-routing.md)
- [Cooling scope](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/furnace-coolant-conduits.md)

## [0.16.0] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Process manually supplied detached wall panels; connect a grabber, wall chute and indoor dismantling fixture.
- Recover steel and aluminium from identified processing residue, retaining finite rejects.
- Pair collectors and material routes explicitly; manage machines through local Control Panels, C1 or F3.
- Cast aluminium housings with the electrically heated F6 furnace and finite cooling.
- Choose existing direct exterior or underside cooling, or optional F6-C sealed conduits to a remote F6-R radiator.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.19.0 or newer. Auto Nav and Agriculture are optional. No Crafting Framework or Salvage Workshop dependency.

### Known limits

- Experimental candidate; current machinery and artwork still need in-game evaluation.
- No attached-hull cutting, persistent cargo ejection or perfect recycling. Recipe yields are authored gameplay budgets.
- Processing and receiving are separate permissions and pause after reload. Saved links are retained.
- Coolant conduits use a simplified sealed thermal circuit, not transferable water or a fill/drain/leak simulation. Existing direct installations retain their saved meaning.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/player-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
