# Phobos Framework changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Shared native INSTALL category constants and pre-publication validation prevent unreachable visible installation entries.

## [0.25.0] - 2026-09-26 - Draft

### Crew automation

- Added default-disabled standing orders, native task publication, exact worker context, equipment/input/capacity reservations, per-crew permissions and shared roster/equipment/C1 controls.
- Added persistent specialities and powered-terminal study; completed practice and study combine toward authored 20-hour/10-hour thresholds, with a 20% hands-on duration benefit. Existing terminal actions are retained.
- Added bounded native time-skip coordination, checked travel/handling budgets, measured machine ticks and repair-time sharing. Native rest, care, events, payroll and fuel remain native; exterior operations suspend.
- Saved manual stops and routine resume preferences remain authoritative. Unknown saved records are retained and blocked.

### Compatibility and limits

- Native method/definition and offline checks are not in-game validation. Common Sense integration uses native tasks; live compatibility and UI review remain owner-run.
- See [crew controls, sources and owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/crew-automation.md).

## [0.24.2] - 2026-09-26 - Draft

### Fixed

- Share a presentation-only command-feedback rule across Agriculture and Shipbreaker panels: omit successful complete-line or paragraph echoes already shown in fresh live status. Keep rejection explanations, distinct notices and other-endpoint responses visible. No gameplay, saved state or console command responses change.

### Compatibility and limits

- Agriculture 0.11.1 and Shipbreaker 0.24.1 require this shared helper. Offline regression checks cover full-status echoes, notices, line endings, errors and substring coincidences; native visual confirmation remains pending.

## [0.24.1] - 2026-09-26 - Draft

### Fixed

- Correct outgoing ship dimensions after native save trimming. Blue Bottle Games' inspected Ostranauts 1.0.1.5 writes dimensions before trimming but room/zone indices afterwards; removal of edge objects can therefore produce an internally inconsistent save.
- Load an affected save using a uniquely validated smaller grid, requiring the full exterior boundary and all saved room positions to agree. Correct only in-memory dimensions before Shipbreaker's padding guard. Preserve room IDs, atmosphere, zones, items, wear and construction progress; reject missing or ambiguous evidence. Original archives are not edited.
- The supplied later autosave resolves from its stale 64-by-44 header to 63 by 44, restoring all eight room-position lookups offline. The earlier save remains 64 by 44. Both earlier guards ran; trusting the stale header was the remaining Phobos contribution. See the [evidence and reload check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-room-load-mitigation.md).

### Compatibility and limits

- Framework-only update retaining Shipbreaker's existing grid guard, including 0.23.0 from the failure log and 0.24.0 present at delivery. The owner associates the recurrence with torch use and a six-hour time skip; those triggers are not independently established. The earlier marker-health fix was owner-confirmed, while this third fix awaits gameplay confirmation. No lost gas is invented, and no drive/time controls, saved identities or recipe contracts are changed.

## [0.24.0] - 2026-09-26 - Draft

### Changed

- Quantity-aware merchant and regional-stock overloads request bounded physical lots while retaining the old single-unit API. Probability and stock condition remain independent; native and third-party branches and existing inventories are preserved.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.23.1] - 2026-09-26 - Draft

### Fixed

- Retain living saved construction markers whose recorded damage is below their saved health limit when the native loader would compare that damage against the generic marker's zero health. Applies to verified vanilla and available mod targets during saved item spawning; templates, finished equipment, dead/exhausted markers and ambiguous records keep native behavior. Damage, construction progress and save files are not rewritten.
- Addresses the second pending-construction room-load trigger found in Blue Bottle Games' Ostranauts 1.0.1.5: lightly worn G4/H4 markers were discarded before Shipbreaker's existing grid guard could run. The supplied earlier autosave has no damage overrides; the later save has 69 affected markers, including 67 vanilla wall/floor markers. Read-only native-data audits retain all 69 with the patch.

### Compatibility and limits

- Keep Shipbreaker 0.19.1 or newer for its separate saved-grid protection. Framework's health correction does not replace it, restore already-lost gas or recover missing providers. Shipbreaker 0.21.0 was installed for this Framework-only update. Automated checks cover native hook boundaries and both supplied saves; the owner subsequently confirmed the reported reload worked. The separate stale-header recurrence is addressed in 0.24.1. See the [investigation and owner check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-room-load-mitigation.md).

## [0.23.0] - 2026-09-26 - Draft

### Added

- Shared verified vanilla retail endpoint lookup and additive regional offers. Missing optional kiosks are reported and skipped without redirecting stock or disabling content. Consumers retain ownership of balance; native market pricing and inventories remain authoritative.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Content using the regional helper requires Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

## [0.22.0] - 2026-09-26 - Draft

### Added

- Version-scoped opt-in reject reservations around the inspected [Valtora Ship's Water 0.16.1 Recycler](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189). Bound processing to finite destination space and measure waste debit/potable credit using same-ship tank lists. Unlinked recyclers retain provider behavior.
- Shared admission registry for content-owned collector cargo and the owning collector's mount validator. Existing capacity and native container checks remain authoritative; this does not enable industrial routing of new cargo.

### Known limits

- The adapter reports wet remainder under an authored water-mass convention, not a nutrient assay. The content consumer owns access, pairing, journals and explicit post-load resume. No provider files are modified. Unity hook execution remains an owner check.

## [0.21.2] - 2026-09-26 - Draft

### Fixed

- Resolve native instrument donor audits from BepInEx's managed directory when its byte-loaded game assembly has an empty Location. Keep the pinned SHA-256 check and component/callback isolation; changed or missing donors remain unavailable.
- Supply fixed-field text layout that preserves glyph size, normalizes native-font baseline spacing and delegates clipping to the owning mask. Auto Nav uses it to correct blank compact labels and clipped telemetry without modifying shared font assets.
- Add regressions for memory-loaded assemblies, mismatched/missing donor files and native font metrics. No saved-state changes; native interaction and visual confirmation remain owner checks.

## [0.21.1] - 2026-09-26 - Draft

### Added

- Shared saved-grid padding planner for Shipbreaker's pending-construction load mitigation. Validate integral origins, containment and bounded expansion without changing saved records or shrinking live geometry.
- Regression coverage for the reported room-index mismatch, all grid edges, origin changes and invalid bounds. Framework alone does not patch ship loading; Shipbreaker owns activation. The owner reported a successful affected-save reload with Shipbreaker 0.19.1 on 26 September 2026; broader coverage remains unverified.

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
