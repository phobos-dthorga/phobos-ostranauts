# Phobos Framework changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Shared native INSTALL category constants and pre-publication validation prevent unreachable visible installation entries.

## [0.22.0] - 2026-09-26 - Draft

### Added

- Version-scoped opt-in reject reservations around the inspected [Valtora Ship's Water 0.16.1 Recycler](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189). Bound processing to finite destination space and measure waste debit/potable credit using same-ship tank lists. Unlinked recyclers retain provider behavior.
- Shared admission registry for content-owned collector cargo and the owning collector's mount validator. Existing capacity and native container checks remain authoritative; this does not enable industrial routing of new cargo.

### Known limits

- The adapter reports wet remainder under an authored water-mass convention, not a nutrient assay. The content consumer owns access, pairing, journals and explicit post-load resume. No provider files are modified. Unity hook execution remains an owner check.

## [0.21.0] - 2026-09-25 - Draft

### Added

- Share the original quiet completion cue across content mods with one native-effects player, volume/mute and real-time burst suppression. Transient watches retain visible outcomes; dropped events never replay.
- Seed the shared volume from an existing first-trial Shipbreaker setting only when no shared setting exists. Audio failure remains isolated from game operations.

### Known limits

- No save migration. Unity playback and listening evaluation remain owner checks. Low playback priority does not guarantee suppression during native alarms.

## [0.20.0] - 2026-09-25 - Draft

### Added

- Bounded multi-receiver port banks retain the original single-pair identity.
- Reusable retained two-component fluid lines preserve cargo, route binding and transit clocks across reloads.
- Bounded hydraulic resistance and shared-budget allocation support Agriculture and optional furnace coolant servicing.

## [0.19.0] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Native construction, maintenance, additive merchant stock and material accounting.
- Versioned saved state, physical transfers, explicit endpoint pairing and ship-scoped equipment controls.
- Measured power and liquid delivery, shared conduit routes, finite solution accounting and optional provider adapters.
- Translation lookup, reusable native instruments and opt-in Phobos Scope performance recording.

### Requirements

Ostranauts 1.0.1.5 and BepInEx 5 (inspected baseline 5.4.23.5). No Shipbreaker, Crafting Framework or Salvage Workshop dependency.

### Known limits

- This is an experimental shared library. Successful offline checks do not establish in-game compatibility.
- Phobos Scope recording is disabled by default. The recorder is bundled; no Rust process is needed during play.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/framework-author-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
