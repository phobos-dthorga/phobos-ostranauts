# Phobos Framework changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Audit linked player documentation and correct generated placement wording for section assembly while preserving direct installation of complete machinery. No gameplay or save changes.

- Extend maintained item evidence to actual native maintenance generation, attachment and fresh/worn/contained target checks. Document shared dismantling restrictions for cargo, lots and stacks; add a read-only aggregate save audit. Runtime behaviour is unchanged.

- Investigated additive bulk-storage and station-purchase contracts for Agriculture, including native refuelling and Ship's Water coexistence, custody, reservations and UI isolation. Published a research blueprint; the investigation itself registered no runtime API, equipment or service. Framework 0.27.0 below later added the shared bulk-supply storage and station purchase services.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Shared native INSTALL category constants and pre-publication validation prevent unreachable visible installation entries.

## [0.37.0] - 2026-09-28 - Draft

### Added

- Appliance definitions name a self-targeted power-change action in their power info, the native pattern that makes the game itself set and clear IsPowered. Agriculture machines therefore show their real power state to the game, the crew console and the panel.
- NativeDefinitions.Trigger returns a native trigger by name or null; the game's own lookup returns its always-true Blank trigger for an unknown name, which must never gate an optional provider.
- StackUnits enumerates the units in a container counting each native stack's members separately, since the game stacks matching items dropped into a container; machines take one member at a time and leave the head in place.
- LiquidTransferGuard.Resolve closes an interrupted-transfer journal on the owner's say-so, for the new accept-contents commands.

### Compatibility and limits

- Additive API. Required by Agriculture 0.17.0 and Shipbreaker 0.34.0. Offline checks pass; owner play-testing is pending.

## [0.36.0] - 2026-09-28 - Draft

### Fixed

- Any crew member the game admits can take a standing-order step. Tasks used to name your own character as their owner, and the game forbids everyone not on an owner list, so no other crew member ever took one.
- Several orders can feed one store. The game keeps one task per target and action unless told otherwise, so later orders on the same store were silently dropped.
- Crew who are merely not rested, sated or slaked, or in moderate pain, are no longer refused work. The game's own pledges put eating, drinking and rest first, as they do for painted jobs; unconsciousness, combat and emergencies still block.
- A step is no longer refused because the equipment's contents changed after the claim (a tidied stack, for example); the provider checks the actual contents when it completes.
- Training credit is no longer lost when the game reuses a pooled action object for a later action.
- Time-skip repairs keep the game's own repair allowance, scaled by the share of on-shift crew time our jobs left free, instead of a replacement count that was up to six times smaller. A skip suspends only orders on the skipping crew's ships that it cannot advance.
- A refused dismantle, repair or construction finish now closes the game's task for it, as native effects do, instead of leaving that task listed forever. The reason goes to the actor's crew log.
- An internal bin counts as empty within the shared mass tolerance rather than at exactly zero.
- Transfer clocks, pump budgets and processing jobs accept long intervals such as a time-skip or a reload gap and catch up like native machines; the electricity actually received bounds the work. Content mods' own "time gap" pauses no longer trigger and are removed in their next versions.
- Without Ship's Water, a missing tank rule no longer counts as always true (the game returns its always-true Blank trigger for an unknown name).
- The unsaved-changes guard applies only to the panel hosting a Phobos shell; closing any other panel proceeds natively.
- Escape closes a store picker before its panel, as with native sub-windows, and pause, time-scale, console and screenshot keys stay live while picking in the world.

### Changed

- Native loot tables and construction stations are amended in place instead of being cloned and republished under their own names; the game's own objects and every other mod's entries stay.
- The standing-order task action is cloned from the vanilla Toggle Power job (Operate duty, tooling animation) rather than from the inventory panel action.
- The F3 command phobosframework crew also names who could take each order step right now.

### Compatibility and limits

- Additive API: NativeDefinitions.Amend and LootBranches, NativeEffects.Refuse, ShipsWaterSupply.Rule and CrewBalance.RepairShare. TransferClock.MaximumStepSeconds is replaced by MaximumCycleSeconds: steps are unbounded, cycles stay 1 to 60 seconds. No saved identities or records change. Content mods built against 0.35.0 keep working; Agriculture and Shipbreaker updates that remove their own time-gap and finish-time workarounds follow. Offline checks pass; owner play-testing is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.35.0] - 2026-09-28 - Draft

### Fixed

- Standing orders no longer interrupt crew study. The game cancels every on-shift crew member's study whenever its task list grows, and each completed order step used to add a new task. An order now announces one task when it is enabled, like a painted job, and later steps are added quietly.
- A failed order step is retried after 30 seconds, then 1, 2, 5 and 10 minutes, instead of every 2 seconds. The order stays enabled and shows the wait and the reason; the worker is free for native tasks, study and rest in between.
- A step a crew member cannot take (no route, nothing to carry, a skilled colleague available, or a reservation held) is withheld from that crew member's task search instead of ending it, so lower-priority native tasks stay reachable.
- Terminal definitions are amended in place rather than cloned and republished, so PDA job painting on terminals keeps its native actions. The retired 15-minute study action no longer carries a work duty, so an old queued one cannot turn a direct order into a task.

### Changed

- Specialities are studied through the game's own study chain: powered terminals offer Study Agriculture, Cooking and Industrial Processing with the vanilla stages, refusals, tablet animation, work-shift interruption, Stop and time-skip continuation. Each completed study step credits the speciality; a skipped hour credits an hour. The 10-hour study balance is unchanged.
- AutoTask crew can choose Phobos study on their own. The game only picks actions present in a crew member's AI history, so on load the vanilla construction-study entries are copied for each speciality into every crew member's history and the new-crew template, only where absent. Nothing is overwritten or removed.
- The retired 15-minute study action is no longer offered on terminals; its definition stays registered so older saves load.
- New F3 command phobosframework crew, optionally followed by a crew member's name, explains per crew member AutoTask, shift, current action and study eligibility, each terminal's study admission and users, AI-history entries for Phobos study, order retry waits and the game's task count.
- Registration refuses a trigger whose chance is zero and restores missing lists before publishing.

### Compatibility and limits

- Additive changes to saved crew AI history and to live terminal action lists; no saved identities change. Existing saved terminals qualify for study through the vanilla rule without a power cycle. Content mods need no update. Offline checks pass; owner play-testing of study, interruption and retry behaviour is pending.

## [0.34.0] - 2026-09-28 - Draft

### Added

- SensorLeases lets automation switch native ship sensor types on through the native Sensors page switch, noting a lease on each sensor unit, and later switch off only what it switched on. Sensors already on are never claimed. Any other switch clears the lease, or turns a switch-off into a decline for that work. Notes use the existing object-state store, so they follow power mode switches, repairs and saves. IfOn predicts a sensor's contribution with the native formula without writing state, and SwitchedOffByOthers reports other switch-offs.
- PlayerNotices posts one crew message-log line and, while that ship's navigation station is open, the native nav-map warning banner with its tone, limited to once every 20 seconds per notice kind.

### Compatibility and limits

- Additive services; existing saves and consumers are unaffected. Content mods own their switching policy and wording. Offline checks pass; banner and log appearance in Unity remain owner-tested.

## [0.33.0] - 2026-09-28 - Draft

### Added

- A public single-container check for crew storage eligibility: finite, unlocked, not a person and not provider-owned equipment. Crew store lists keep the same rule, and Shipbreaker storage outputs now share it instead of copying it.

### Compatibility

- Additive only; saved orders, stores and equipment are unchanged. Required by Shipbreaker 0.33.0. Offline checks are not in-game validation.

## [0.32.0] - 2026-09-28 - Draft

### Added

- Optional unfinished construction images for section-based machinery. Stages follow delivered parts and saved work; native completion still creates the finished machine.
- Construction views preserve native geometry and selection effects, limit routine refreshes to ten per second per site, and release their private materials when removed. Missing images fall back without changing the job.

### Compatibility

- Existing assembly calls, material bills, saved IDs and work rules remain unchanged. No new saved appearance fields. Offline assembly, renderer-adapter and native-boundary checks pass; Unity visual approval remains pending.

### Fixed

- Keep documentation packaging status out of the returned package path so archive creation succeeds after the guide-directory split.

## [0.31.1] - 2026-09-28 - Draft

### Fixed

- Leave unnamed native condition triggers unchanged instead of throwing during section selection. The item-information hook also ignores unnamed native interactions. Registered assembly material and completion checks remain intact.

### Validation

- Reproduced the owner's null-key exception before the fix. Regression checks cover null, empty and unrelated names with both native outcomes, including the compiled hooks. Live Unity confirmation remains pending.

## [0.31.0] - 2026-09-28 - Draft

### Added

- Shared native construction-site section contracts with exact input validation, staged native lots and preserved cancellation/save handling.
- Read-only item and maintenance information panels for content-owned instructions.

### Fixed

- Explain split-stack, retained cargo and pending-work restrictions without weakening dismantle guards.

### Compatibility

- Existing public APIs and table action identities remain valid. Native Unity hauling and information-panel interaction await owner checks.

## [0.30.3] - 2026-09-27 - Draft

### Changed

- Add a shared stock-coverage helper that fills omitted item offers without duplicating an already prepared lot or replacing its condition. Existing merchant inventories, native pricing and other providers remain untouched.
- Applies to future native stock and loot generation; no forced restocks, saved-cargo changes, price changes or live gameplay validation. See [merchant availability and salvage](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/merchant-stock.md).

## [0.30.2] - 2026-09-27 - Draft

### Fixed

- Add opt-in native transport-action normalization shared by content mods: retain useful actions, add missing pickup/drop, match stack actions to stack limits and keep installed machinery out of carry slots.
- Restore declared cumbersome flags in detached load data for explicitly registered bulky cargo. Preserve saved hand placement until successful native release, then retire the legacy hand attachment. No save files, contents, mass, wear or progress are rewritten.
- Audited all 118 implemented item definitions and checked native action/slot contracts offline. Live menus and loaded inventory handling still require owner testing; see the [item handling audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/item-handling-audit.md).

## [0.30.1] - 2026-09-27 - Draft

### Fixed

- Correct washed-out Polaris buttons by using explicit dark colours for every button state and resetting inherited brightness. Native artwork and other panel defaults remain unchanged.

### Known limits

- Native palette contrast and offline regressions are checked; corrected Unity appearance and scrolling still require owner playtesting. No gameplay values or save formats change.


## [0.30.0] - 2026-09-27 - Draft

### Changed

- Add opt-in Polaris button styling using native artwork, retained bindings and live selected markers. Existing panel styles remain available.
- Add guarded secondary-click handling and wrapped Polaris console navigation/actions. No gameplay policy or saved-state format changes.

### Requirements

Additive Framework API update; normal game/loader requirements remain unchanged.

### Known limits

- Offline regression and browser-layout checks are separate from owner-run Unity interaction tests. No additional performance captures or Steam publication are claimed.
- Existing faceplates and suitable vanilla graphics are reused after review; no new artwork generation was warranted.


## [0.29.0] - 2026-09-27 - Draft

### Added

- Add shared presentation pacing, change-only widget updates, retained native-widget bindings, fresh ship-scoped equipment discovery and allocation-free equipment-family matching for existing mod consumers.
- Extend disabled-by-default Scope recording with bounded frame intervals, collection counts, calibrated allocation support and crew/discovery timings. Export stays explicit; world changes stop recording.

### Fixed

- Avoid allocating empty state copies and copying prior saved data solely to validate a write. Preserve fresh validation and detached read snapshots.

### Compatibility and limits

- Public helper additions are additive; save formats and gameplay rules are unchanged. See docs/development/performance-audit.md for the six baseline captures, complete source ledger and offline checks. Follow-up captures and measured improvement targets are deferred by owner direction; Unity interaction and performance remain unverified.

## [0.28.1] - 2026-09-27 - Draft

### Documentation

- Review English controls, warnings, descriptions and help for practical player language; retain precise diagnostics and established equipment names. Update current guides, item-reference inputs and the Workshop draft.
- Follow the retrospective language rule and glossary in docs/development/player-language.md, informed by Blue Bottle Games' official Ostranauts description and Daniel Fedor's developer AMA. This is an interest-based audience interpretation, not measured demographic data.

### Compatibility and limits

- Wording only: translation keys, placeholders, commands, saved identities, resource values and gameplay rules are unchanged. This is an unpublished development candidate; Unity text layout remains unverified.

## [0.28.0] - 2026-09-27 - Draft

### Added

- Add shared native appliance state-art binding for dedicated damaged, loose and loose-damaged imagery. Content mods own sprites; the helper changes image and portrait references while preserving physical definitions and saved identities.

### Compatibility and limits

- Agriculture 0.15.0 and Shipbreaker 0.28.0 use the new helper. Existing appliance registration signatures remain available. Native definition checks verify that geometry, conditions, actions and economic fields survive repeat binding. No installation, Steam publication or gameplay validation is claimed.

## [0.27.0] - 2026-09-27 - Draft

### Bulk custody and purchasing

- Add validated commodity/catch/reserve storage and operation-owned endpoint reservations, without changing existing scalar/mixture transfer interfaces.
- Add an independent station Bulk supplies view, exact quotes and measured payment/delivery settlement. Known partial receipts refund missing quantity; uncertain journals block retry and retain evidence. Native fuel and optional Ship’s Water services remain separate.
- Add optional structured equipment selectors for checked C1 configuration drafts. No content-owned chemistry or resources move into UI callbacks.
- Retain the Agriculture-first research and native Blue Bottle Games / Valtora attribution. Offline accounting/native checks are not Unity validation; no Steam publication.


## [0.26.1] - 2026-09-27 - Draft

### Panel corrections

- Keep compact text inside its actual element bounds, shorten the roster shortcut, respect fixed picture/button widths and draw both stock-stepper symbols without relying on font glyph coverage.
- Make Details & diagnostics a reusable expand/collapse block. Show status once, highlight the selected view/equipment, and explain empty time-skip sections.
- Disable unavailable Locate/Clear controls. Locate centres a temporary ship view; Clear explicitly reports a pending draft change, which still needs Apply.
- Add a visible ship-selection banner, dimmed background with candidate openings, object brackets and a cyan line that animates over native-valid hits. Right/middle drag pans, scroll zooms, and Cancel/Escape restores the previous view and draft. Overlaps open a short choice list.
- Isolate native shortcut commands as well as mouse/keyboard world handlers while picking. Do not enter native signal-connection mode or change crew selection, target authorization, object lights or layers.
- Bound multi-line confirmations by both width and height, so notices cannot overflow the footer.
- Preserve saved orders, manual stops, names, resource accounting and existing presentation APIs. Automated geometry, compiled-wiring and build checks are separate from pending owner-run Unity evaluation.

## [0.26.0] - 2026-09-26 - Draft

### Control panels

- Added a shared compact console shell with fixed navigation/actions, independent viewports and a narrow-screen Back layout. Existing panels and native instrument dimensions are not globally resized.
- Separated Orders, Crew & Training and Time-skip. Equipment-specific forms use drafts, complete form validation, stale rejection, Apply/Discard and explicit Resume after changing enabled orders. Disabled and manual-stop states remain intact.
- Added searchable storage/connection pickers, opt-in nicknames, native artwork with neutral placeholders, and an input-isolated ship picker with overlap disambiguation. Native names, object IDs and physical inventories are unchanged.
- Reused the existing original Phobos console frame. Native components remain behind audited adapters with standard-widget fallbacks. Browser previews and automated checks are not Unity gameplay validation.

## [0.25.1] - 2026-09-26 - Draft

### Fixed

- Resolve crew through the native company roster instead of the unused legacy crew-list field, fixing repeated CrewWork.Poll null exceptions and the same fault in crew controls and time-skip preview.
- Skip unresolved, destroyed and uninitialized workers during work discovery. Departure checks use the same native roster but block if any member is unresolved or away.
- Keep saved orders, manual stops, training and native roster entries intact. Automated regression checks are not in-game validation.

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
- The supplied later autosave resolves from its stale 64-by-44 header to 63 by 44, restoring all eight room-position lookups offline. The earlier save remains 64 by 44. Both earlier guards ran; trusting the stale header was the remaining Phobos contribution. See the [evidence and reload check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-room-load-mitigation.md).

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

- Keep Shipbreaker 0.19.1 or newer for its separate saved-grid protection. Framework's health correction does not replace it, restore already-lost gas or recover missing providers. Shipbreaker 0.21.0 was installed for this Framework-only update. Automated checks cover native hook boundaries and both supplied saves; the owner subsequently confirmed the reported reload worked. The separate stale-header recurrence is addressed in 0.24.1. See the [investigation and owner check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-room-load-mitigation.md).

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

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/framework-author-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
