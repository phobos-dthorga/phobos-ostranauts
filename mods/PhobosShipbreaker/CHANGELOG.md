# Phobos Shipbreaker changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Document the third room-load recurrence: the grid guard trusted dimensions recorded before native save trimming. Framework 0.24.1 corrects validated stale headers before padding and synchronizes future outgoing saves after trimming. The earlier marker-health fix was owner-confirmed; the new before/after archive checks are offline, with gameplay confirmation pending.
- Document the wear-related save-load recurrence and Framework 0.23.1 correction. Native marker rejection could remove the pending G4/H4 before Shipbreaker's grid guard ran; retain the existing grid protection and update Framework. The earlier successful owner test remains valid for its original save, not proof of all later reloads.
- Correct the F6 operating guide's direct versus powered-pipe cooling behavior, circulation/feed demand and damaged-pump limits. Point current-version checks to maintained references rather than historical installation notes.
- Clarify that Manufacturing and autonomous G4 reclamation are separate implementation tracks; current material receipt does not authorize repeated furnace batches or replace motion interlocks.
- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Research and planning

- Earlier capture-only research is superseded for supported ordinary walls by the implementation below. Broader structural recipes, repeated furnace batches and whole-wreck completion remain outside this round.
- Keep [NASA Goddard's Raven research](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/) and [ESA's LIRIS experiment by Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before) as sensing context, separate from Blue Bottle Games native evidence and authored gameplay choices. No institutional endorsement or gameplay validation is implied.

## [0.24.1] - 2026-09-26 - Draft

### Fixed

- C2 residue collectors can be installed on two structural floor tiles with a clear two-tile service walkway, including damaged installation forms. Native placement checks still reject occupied footprints; the body blocks pedestrian overlap. Existing wall installations retain their support and exterior-mouth rules.
- Agriculture 0.11.1 can align the collector's full two-tile pocket against the optional Recycler on open floor, including in front of it. The collector's opposite service side remains accessible. Floor support and access are rechecked during operation.
- Collector routing windows and local/C1 furnace instruments suppress successful status or notice echoes already present in live status, using Framework 0.24.2. Furnace actions refresh their status immediately; rejection explanations and responses concerning another endpoint remain visible.

### Compatibility

- Retains equipment IDs, inventories, saved pairs, construction inputs, finite payload and explicit resume after reload. No pressure boundary is created or removed. Prepared offline; gameplay confirmation remains with the owner.
- Requires Framework 0.24.2 or newer for shared panel feedback.

## [0.24.0] - 2026-09-26 - Draft

### Added

- Add explicitly enabled G4 reclamation: choose an exposed work wall and separate protected anchor, capture, cut/feed, release, retreat, traverse and recapture another admissible work window. Reuse existing G4/H4/D4, N1/N2 and artwork; require Auto Nav 0.18.0 and Framework 0.24.0.
- Cut only empty, unstacked, undamaged native 24 kg ordinary walls on the exact owned unoccupied target. Retain floors, anchor supports, other equipment and target registration. Unreachable remnants are reported; whole-wreck deletion and new structural recipes are excluded.
- Capture authored 120 powered seconds / 12 kW defaults per started cut. Credit actual received electricity and reject its heat into the connected D4 service room, requiring 10 kPa and a 40 C ceiling. This cooling connection is an authored abstraction; vacuum is not free cooling. Cutting and existing 2 kW transfer run sequentially.
- Add G4/C1/F3 Start, Resume, Pause, Stop and phase/blocker/completion/remnant status. Start authorizes the bound G4/D4 chain; full capacity waits under that same mission. Downstream permissions and furnace motion restrictions remain unchanged.

### Persistence and limits

- Journal uninstall and transfer separately before mutation; re-resolve exact native item identity and resulting position. Preserve paid work and evidence on interruption, without reconstructing cargo or repeating outputs. Framework general transfers remain same-ship.
- Reload, lost tracking and manual takeover suspend authority. Stop retains a current capture; Release stays explicit. Older captures remain releasable but require a newly planned protected support before automatic cutting.
- Update maintained defaults/dependency minima, generated equipment operating tables and the [reclamation guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-reclamation.md). Native API evidence is from Blue Bottle Games' locally inspected Ostranauts 1.0.1.5; offline checks are separate from owner gameplay evaluation. No repeated furnace batches or Steam publication.

## [0.23.0] - 2026-09-26 - Draft

### Changed

- Increase successful local and regional offers to wholesale lots for all machinery, assembly sections, coolant conduit and service charges. Uses Framework 0.24.0; prices, engineering salvage and existing inventories are unchanged.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.22.0] - 2026-09-26 - Draft

### Added

- Selected-G4 capture and explicit release through local industrial panels, C1 and F3. Bind full native ship, target, G4/chute/D4 and N1/N2 console/module IDs; shorten and disambiguate display labels only. Auto Nav owns the terminal approach; native mooring establishes deck contact after a hull-fit check.
- Write-ahead native anchor records and a post-capture wall-contact check. Manual takeover, tracking/power loss and reload require explicit restart. Unknown state and uncertain native mutations are retained; no target cargo is deleted or copied.

### Requirements and limits

- Phobos Auto Nav 0.16.0+ is now mandatory alongside Framework 0.23.0+. Builder and installer include the dependency. Existing equipment, cargo, recipe and hot-job identities remain unchanged.
- First stage accepts owned, unoccupied targets only. Cutting, automatic repositioning, repeated furnace cycles and whole-wreck completion remain future work. Stop/release do not brake. Prepared offline, not installed, gameplay-validated or published.
- See the [capture guide and native evidence](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-capture.md). Blue Bottle Games supplies the native mooring/fit precedent; this authored arrangement is not scientific validation.

## [0.21.0] - 2026-09-26 - Draft

### Added

- Industrial equipment, assembly sections, coolant conduits and finite coolant charges gain bounded offers at 15 additional placed vanilla retail markets. Industrial centres receive higher authored availability; Flotilla machinery is refurbished. Clean coolant now uses the industrial category and retained coolant uses trash, never potable water. Base values, material budgets and jobs are preserved.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Requires Phobos Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

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
