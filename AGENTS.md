# Phobos Ostranauts contributor instructions

## Localization and constants

- Keep player-facing text in per-mod UTF-8 translation catalogs, with embedded
  English fallbacks and Framework's shared lookup. See `docs/localization.md`.
- Use stable translation keys and complete messages with placeholders. Never
  translate native IDs, saved-state keys, command names or configuration keys,
  and never use translated status text to drive gameplay decisions.
- Name meaningful limits, unit conversions and tolerances; derive displayed
  capacities and yields from authoritative rules. Keep balance in the owning
  content mod and preserve historic recipe contracts. Do not generalize every
  layout coordinate or expose every constant as a setting.

## Working style

- Keep this a practical, small-team project. Prefer a working slice over a
  speculative framework or extensive process.
- Use observed progress to plan rounds; do not invent hour estimates.
- Explain what works, what was checked and what remains uncertain.
- Do not control the owner's mouse or keyboard. Inspect files and use command-line
  tools; give the owner instructions for interactive steps unless they explicitly
  request hands-on control again.
- The owner's overarching direction is longer-term habitation and survival in
  hostile space, potentially indefinitely (clarified 2026-09-23). Prefer features
  that extend endurance, support crew health and make the ship maintainable away
  from stations. Judge industrial outputs by their usefulness to those needs.
  Treat indefinite operation as an ambition supported by recovery, maintenance
  and replenishment of losses, not a claim of perfect recycling or unlimited
  matter. Research each need as it arises in play; reuse existing mods first.
- For any Ostranauts mod idea, candidly recommend simplifying, setting it aside
  or changing direction when gameplay value, engine limits or maintenance costs
  make further work unconvincing. Do not continue merely because effort was spent.
- Repository visibility stays private until the owner explicitly requests a change.
- While this repository is private, use ordinary commits and direct pushes to
  `main` for requested checkpoints. Do not create PRs or run PR review/merge
  workflows unless the owner explicitly asks. The owner deferred PR formalities
  until the project is public (2026-09-23); this preference overrides a skill's
  default PR lifecycle. Keep appropriate checks and normal Git protections;
  never force-push or change visibility to simplify delivery.
- Public releases of these mods are the intended destination (2026-09-23).
  Preserve original authorship, provenance and adaptation notes from the outset;
  distinguish verified third-party terms from the owner's permissive working
  assumption. This future intent does not authorise changing visibility today.
- Research industrial ideas as the owner encounters relevant gameplay and can
  test them. Current priority: powered shipbreaking, onboard processing first,
  external cutting and its positioning/autopilot needs later. See
  `docs/fusion-industry-roadmap.md` and `docs/powered-shipbreaking-research.md`.
- The owner expanded industrial research on 2026-09-24 to shredders, material
  recyclers and asteroid feedstocks, including new ore types that replenish life
  support. Use native tethered asteroid mining as the acquisition baseline; do
  not assume purchased ore or add a second mining system. Reuse native water ice,
  methane ice, hydrates and carbon-bearing ore first. Investigate new nitrogen-
  and phosphate/salt-bearing feeds where they fill a concrete endurance gap.
  Keep proposed assays/yields distinct from native evidence, preserve existing
  residue and saved-job meaning, and account for every product and remainder.
  See `docs/shipbreaking-material-processing-research.md` and
  `docs/asteroid-life-support-research.md`. The combined scrap reclaimer is now
  implemented as described below; ore and life-support processing remain research,
  not implemented features or verified integrations.
- Future industrial chemical storage is documented in
  `docs/chemical-storage-and-process-fluids.md` (owner direction, 2026-09-24).
  Preserve solvent/reagent reservoirs, quantity-based station refuelling like
  Ship's Water, required inputs for suitable chemical processes, optional salvage
  improvements and contents-driven leaks/ruptures/hazards as later ideas, not current
  features. Use native gases first; custom species need code-level mass, atmosphere
  and persistence research. Shared storage/accounting, fluid transfers, station
  services, optional adapters and diagnostics belong in Phobos Framework as
  concrete consumers need them; chemistry, equipment art, balance and specific
  hazards remain content-owned. Preserve other providers' terminal entries and
  potable-water state. Existing solid transfers and equipment-stock helpers are
  not already fluid/refuelling APIs. Do not impose chemical dependencies on current
  mechanical processing or build a speculative parallel atmosphere simulation.
- Use `docs/player-guide.md` as the current player-facing starting point and
  `docs/equipment-economy.md` for current prices, bills and work times. Label older
  inventory/build snapshots as historical; do not turn them into ordinary-save
  restrictions or claim prepared packages are installed. Include the player guide
  and its direct equipment links through the shared packaging helper.
- Follow `docs/residue-material-contract.md` and `docs/scrap-reclaimer.md` for
  the implemented 0.8.0 chain. Legacy 13 kg residue remains unclassified, and
  started revision-1 wall jobs keep their exact outputs. Fresh revision-2 wall
  jobs produce identified 13 kg feed; the combined 4 x 4 reclaimer returns
  3 kg steel + 1 kg aluminium + 9 kg terminal rejects. These are authored
  budgets, not native chemical assays. Never reroll rejects or convert old cargo.
  Framework now owns shared immutable recipes, saved-job binding and mass checks;
  content owns native keys, identities and balance. Default reclaimer operation
  is 120 seconds / 12 kW, delivering heat into native room gas. Require enough
  atmosphere/thermal headroom; do not treat vacuum as free cooling. Use explicit
  paired output collectors. Version 0.9.0 adds automatic reclaimer feed from
  fixtures or collector buffers, independent input/output pairs and saved exact-ID
  filters through Framework. Follow `docs/automatic-material-routing.md`. Keep
  receiving and processing permissions separate and paused after reload. Full
  destinations retain cargo at the sender; no virtual inventories or silent disposal.
  Preserve shared staged delivery and the native powered-job pause on reload.
- Prefer extending existing mods over duplicating their systems. Steam Workshop
  dependencies are welcome. Refresh the inventory when it matters; see
  `docs/mod-extension-survey.md`. Use permissive mod licensing as the owner's
  working assumption unless restrictions are explicitly stated; record verified
  terms separately, follow them and preserve attribution. Game assets remain
  subject to the game/repository boundaries below.
- Plan for dependencies that remain incompatible or unavailable without treating
  release age alone as failure. Follow `docs/dependency-contingencies.md`: prefer
  a working combination, a narrow compatibility fix or a maintained successor;
  fork only where justified, preserving provenance and applicable terms. Protect
  saved identities, inventories and progress before removing a required provider.
  Document inexpensive contingencies now rather than building speculative
  replacement systems or recurring version monitors (owner preference, 2026-09-23).
- Implement concrete low-cost dependency safeguards early, before saves rely on
  our content (owner follow-up, 2026-09-23). Keep Shipbreaker's provider contract and
  native checks in `NativeAdapter`/`DependencyContract`, prepare definitions before
  publication, and preserve rollback plus startup recipe checks. This is not a
  promise of save recovery when a required provider or the loader is absent.
- Create or extend reusable scripts when repeated work makes them worthwhile,
  especially builds, packaging, installation and verification. Prefer existing
  scripts over repeating ad hoc commands; keep automation proportional to the
  task. Support safe repeat runs and keep local paths/configuration out of Git.
- Use `scripts/install-mods.ps1` for local installation and updates, including
  agent-run delivery. The owner requested a reusable installer usable by both
  them and Codex (2026-09-23). Build the selected package first when its source
  changes; use `-WhatIf` for previews and `-VerifyOnly` for installed-file checks.
  Default selection is AutoNav and Shipbreaker; the older Approach Assist is
  opt-in. Keep the game-closed guard and leave gameplay tests to the owner.
  See `docs/installing-mods.md`; do not repeat manual file-copy/load-order edits.

## Architecture

- Auto Nav's immediate goal is short-range ship/station approach **below 5,000 km**
  (owner direction, 2026-09-24; vanilla threshold owner-reported). Do not confuse
  starting range with arrival distance or add an artificial engagement minimum.
  The owner selected **1 km default arrival**, adjustable closer. Version 0.4.0
  accepts 0.1–100 km arrival requests with native hull clearance; preserve existing
  preferences and capture overrides per flight. Keep diagnostics and panel/F3
  actions on the same service. Docking, obstacle avoidance and continuous working
  position control remain separate future features. See `docs/auto-navigate-adaptation.md`.
- The owner reports successful Auto Nav flight behaviour but wants less RCS waste
  from excessive precision (2026-09-24). Version 0.4.3 captures configurable cruise
  hysteresis per flight, corrects only to the acceptable band and stops chasing
  heading during coasting. Preserve sideways-drift and braking overrides plus the
  separate final-arrival tolerance. Numerical delta-v comparisons are not measured
  game fuel savings; UI 0.4.2 and coasting 0.4.3 still need owner validation.
- The owner requests Polaris in Auto Nav's equipment name to identify its
  navigation-station use. Use **Phobos Polaris Auto Nav Module** for the item,
  localized damaged form, construction and panel branding. Retain saved IDs and
  the Phobos Auto Nav package identity. Follow `docs/auto-nav-economy.md` for its
  native trade, repair, Restore and mass-balanced salvage. Reuse Framework services;
  do not invent extra scrap mass or duplicate economic machinery. Existing approved
  sprites/faceplate remain suitable because names are rendered as live text.

- Use native JSON definitions for suitable content and existing behaviours.
- Navigation panels must explicitly use
  `Ostranauts.ShipGUIs.NavStation.Draggable`, not the game's same-named global
  object-hauling component. Bind `NavModBase.DraggableRef`; the package build
  checks the compiled references. See the Auto Nav 0.4.1 Edit-mode fix notes.
- Match navigation placement bounds to visible artwork. Auto Nav 0.4.2 uses the
  native 20% board-height row and a physical 2:1 container; normalize saved/default
  sizes before native fit checks without moving other modules or bypassing overlap
  rules. See `docs/auto-nav-panel-layout-audit.md` for the vanilla measurements.
- Use C# extensions for behaviour the native data system cannot express cleanly.
- UI code presents state and delegates actions; gameplay services own mutations.
- Extract shared code when concrete features establish a shared need. Avoid
  duplicated business logic and premature generalisation.
- The owner selected our own shareable Ostranauts framework **instead of OCF**
  on 2026-09-24, explicitly correcting an earlier misuse of "in lieu of".
  Follow `docs/phobos-framework.md`: reusable services belong in Phobos Framework;
  machines, artwork and balance remain content mods. Other authors should be
  able to use the framework without Shipbreaker. Framework/Shipbreaker 0.2.0
  implement independent construction and native machinery; OCF/SWB are optional.
  Use native Bar/Dining Tables for obtainable assembly surfaces; the native
  Workbench has unconfirmed normal acquisition. Support an existing Workshop
  bench optionally. Preserve IDs and queued-job meaning, prevent duplicate recipe
  ownership, and retain attribution. The empty legacy OCF recipe file is an
  intentional migration stub; active recipes live in `framework/recipes.json`.
  Do not uninstall foreign providers from the owner's save or claim crash-atomic
  native construction. In-game migration remains owner-tested work.
  Framework 0.4.0 adds exact-ID filters, transfer clocks and bounded grid search;
  Framework 0.5.0 adds reciprocal saved sender/receiver pairing in namespaced
  native property maps. Use full object IDs plus stable logical port IDs, not
  nearest-machine matching or short display IDs. Keep routing configuration
  distinct from permission to resume work after reload. See `docs/material-port-pairing.md`.
  Shipbreaker's collector is one paired floor route, not a general conveyor network.
  Framework/Shipbreaker 0.6.0 and Auto Nav 0.2.0 add ordinary merchant acquisition,
  maintenance, tool requirements and save-compatible economy upgrades. Reuse the
  shared additive stock and native maintenance helpers; keep balance in content
  mods. Follow `docs/equipment-economy.md`; retain actual repair waste mass and
  protect cargo from dismantling.
- Prefix new game identifiers with `Phobos` and keep them stable once saved games
  can contain them. Document migrations for incompatible changes.
- Distinguish observed engine behaviour from proposed designs and untested assumptions.
- Build machinery at its intended physical footprint and storage capacity from the
  first usable implementation. Shipbreaker's current baseline is 4 x 4 tiles,
  one active panel, a four-panel feed and a separate 8 x 8 output tray.
- Expose reasonable player preferences and balance adjustments as documented
  settings. Preserve saved-job meaning when settings change; keep item identities,
  physical dimensions and mass-balanced recipes stable rather than making every
  internal value configurable.
- Provide useful F3 console commands alongside the normal controls, following
  Approach Assist's ConsoleResolver integration. Route gameplay actions through
  the same service and report actionable status; do not bypass gameplay checks.
- The owner now requests equipment **Control Panel** interfaces and a central
  industrial console, with research/text mockups before finished graphics
  (2026-09-24). This supersedes the earlier wait-for-testing instruction for that
  design work. Follow `docs/industrial-control-console.md` and its text mockups;
  the 0.10.0 implementation follows these designs. Use a shared panel family and
  current machinery portraits; the recommended new workstation is 3 x 3 with a
  seat. Target clicked objects by full ID, retain local controls, F9 and F3, and
  route all actions through checked services. Remote console commands must keep
  machine interlocks and same-authorized-ship access; physical inventory handling
  and maintenance stay local. Preserve separate processing/receiving permissions.
  Use Framework for concrete shared access/snapshot/dispatch/UI needs; keep item
  art and balance content-owned. Do not inherit nav flight behaviour, simulate
  telemetry we do not have, or present passive chutes as powered machinery.

## Artwork

- Owner requested a separate 1 x 4 hull chute between the existing feeder and a
  matching-width exterior grabber (2026-09-24). Read this as four tiles along the
  hull, one deep; the 0.3.0 connected candidate uses a 4-wide x 3-deep grabber. Use two static
  side clamps and a restrained central cutting head, with coarse readable pixel
  art. See `docs/shipbreaker-hull-intake.md`; the connected candidate implements
  wall-supported exterior mounting and detached-panel transfer. Backing walls
  provide the native pressure barrier; attached-hull cutting and a simulated
  airlock remain future work. Preserve the existing fixture.
  The owner approved the generated intact visual direction on 2026-09-24; retain
  the masters and pixel scale when deriving subsequent production forms.

- Before generating Shipbreaker artwork, follow
  `docs/ship-equipment-art-study.md` (owner-requested equipment study, 2026-09-23).
  Installed machinery needs its own reference set; do not simply enlarge the
  AutoNav faceplate or assume all equipment is blue-grey. Plan the 4 x 4 fixture
  around a 64 x 64 world texture, a matching normal map and appropriate damage
  and loose forms. The six owner screenshots reviewed on 2026-09-23 are sufficient
  for an initial concept; final appearance still needs checking in-game.
  Game-derived research sheets stay in ignored `.local/`, never in mod packages.
- In screenshot analysis, distinguish view obscuration from lighting. The owner
  clarified that dark regions generally mean areas obscured from view, not absent
  electrical lighting; non-electric sources such as fires also illuminate areas.
  Do not infer power state from brightness or bake visibility wedges into artwork.
- Keep machinery and separately placed ship infrastructure distinct in artwork.
  The owner identified the yellow/striped conduit surrounding the fusion core as
  a separately built and placed entity needed for electrical input/output
  (2026-09-23). Do not copy
  that network as integral machine framing or paint a connected conduit loop into
  Shipbreaker; provide readable connection points for native conduit instead.
- Prefer simple shapes, deliberate contrast and purposeful details over dense
  surface detail (owner clarification, 2026-09-23). Well-matched community-mod
  equipment is also useful visual reference; record origins where known without
  requiring the owner to classify every screenshot. Both cramped ship scenes and
  spacious station scenes are useful; do not ask for rearrangement or staged damage.
- The owner liked Shipbreaker concept v1's design but requested much more
  pixelation to match the game (2026-09-23). Preserve the layout while using coarse
  pixel clusters, stepped edges and simpler shading. Do not treat a smooth large
  concept or a blurred reduction as the final pixel-art target; inspect at 64 x 64.
- The owner subsequently approved pixelated Shipbreaker v2 as shown in the
  64 x 64 preview enlarged without smoothing (2026-09-23). Preserve its layout,
  palette and pixel scale for matching artwork. The unchanged master and previews
  are recorded in `assets/phobos-shipbreaker/README.md`. Shipbreaker 0.1.4 now
  includes the six colour/normal/portrait sets, exported by
  `scripts/export-shipbreaker-art.ps1` with source hashes and exact prompts retained.
  Remaining forms were authorised for production, not individually approved.
  In-game appearance and lighting remain pending owner testing. Preserve the
  separate conduit and full-size footprint when revising this family.

- The owner approved the exact slate-grey Phobos Auto Nav faceplate at
  `mods/PhobosAutoNav/images/phobos/autonav/PhobosAutoNavPanel.png` on 2026-09-23.
  Earlier apparently conflicting feedback was clarified as crossed replies:
  this specific image is approved, including its subtle edges and fasteners.
  Do not replace it with the weathered concept or redesign it as flat geometry.
- Match the original game's restrained navigation-panel language. Keep labels
  and controls live; Phobos identity comes through the name and layout. Preserve
  the approved faceplate's proportions. See `assets/phobos-autonav/README.md`.
- The owner also approved both pickup/item images on 2026-09-23: the intact
  cassette and its cracked, scorched damaged version. Their unchanged masters are
  `assets/phobos-autonav/source/PhobosAutoNavModule-approved.png` and
  `PhobosAutoNavModuleDmg-approved.png`. Use the shared crop in
  `scripts/export-autonav-art.ps1` for game-sized exports; keep the two states
  registered and preserve the original masters. Do not redesign these selections
  or distribute the game's sprites.

## Game and repository boundaries

- Develop in mod folders; do not overwrite the game's original files.
- Ordinary saves are the baseline for current equipment, including Auto Nav
  (owner direction, 2026-09-24). Do not reintroduce named-test-save gates or require
  disposable saves for normal play. The owner runs gameplay checks. Do not directly
  edit, replace or delete save files without explicit authorisation.
- Do not commit saves, decompiled game source, game assemblies, extracted game
  assets, credentials or personal machine paths.
- Resolve game references from a local path or configuration, not a committed
  machine-specific location. Document third-party licences and asset provenance.
- For private-key generation, conversion or credential entry, provide manual
  instructions to the owner; do not automate it or inspect populated secret fields.

## Verification

- Shipbreaker 0.3.0's first connected intake moves already-detached ordinary walls:
  4 x 3 exterior grabber -> 4 x 1 chute over intact supporting walls -> existing
  4 x 4 processor. The backing walls remain the native pressure barrier; no open
  portal or simulated airlock cycle. External cutting and routed conveyors remain
  future work. Normal grabber Inventory is input, normal processor Inventory is
  products. Preserve the saved internal feed as an explicit F9/console fallback.
  Framework 0.3.0 supplies shared physical-item transfer; Shipbreaker owns layout,
  eligibility, power and timing. Reuse those paths rather than duplicating them.

- The owner authorised research of a reusable hull disposal port with future
  filters on 2026-09-24. See `docs/material-disposal-port-research.md`. Native
  jettison destroys items, and bare exterior drops do not establish persistent
  independent cargo. The owner then authorised implementation: Framework and
  Shipbreaker 0.4.0 provide a 2 x 1, 20 kg wall collector, four-packet inventory,
  exact residue filter and one explicit processor link over structural flooring.
  See `docs/residue-collector.md`. This is an intermediate transport endpoint,
  not completed disposal or reduced ship mass. Unsupported inputs stay intact.
  Version 0.5.0 retains cargo and the selected pair after reload, resets the short
  timer and pauses. Link/Unlink is available from either endpoint; a missing or
  mismatched peer blocks transfer. Recoverable
  release needs separate ownership/motion/save handling and remains unimplemented.

- The owner chose to perform Approach Assist's in-game tests personally. Prepare
  builds and test instructions; leave the running game and test execution to the
  owner unless they later request hands-on assistance.
- Scale checks to the change. Add useful tests for gameplay rules and persistence;
  do not create tests that merely repeat static documentation.
- Treat well-established patterns in existing mods as sufficient evidence for
  reusing that behaviour, especially when corroborated across multiple reputable,
  widely used mods. Do not require isolated proof tests such as observing basic
  module power consumption, or a diagnostic-only prototype, before building a
  useful feature. Briefly record the relevant precedent and proceed.
- Focus verification on our new logic, meaningful integration differences and
  concrete suspected failures. Revisit an established pattern only when a relevant
  change, conflicting evidence or an actual fault warrants it; do not turn reuse
  into another prerequisite research or testing phase.
- Equipment dismantling should lose monetary value against selling the whole
  item (owner clarification, 2026-09-24). Audit actual native material prices,
  damaged definitions and wear tiers; show combined output value alongside whole
  value rather than presenting material counts alone. Distinguish definition
  value from merchant quotes and do not claim every regional market is identical.
  Use `docs/equipment-value-audit.md` and `docs/vanilla-economy-audit.md`; preserve
  mass without assuming it implies conservation of monetary value.
- For new machinery, check our material accounting, progress, interruption and
  persistence where we introduce or change that behaviour. Cover power or
  fast-forward interactions when they pose a specific integration risk. Record
  the game and plugin versions tested.
- Never call a successful build an in-game test. Do not claim untested compatibility.

## Industrial control direction (2026-09-24)

- The owner approved implementation of screen overflow, strict per-console ship
  scope and management of large equipment lists. Use automatic type grouping,
  independent scroll areas, name/ID search, status filters and Attention.
- Shipbreaker/Framework 0.10.0 implements a 3 x 3, 40 kg seated console and local
  equipment panels. Remote commands require the console's player-owned host ship;
  docked/moored/towed neighbours remain separate. Check fresh facts per command.
  Physical inventory access and maintenance remain local. Do not add a console
  dependency to existing autonomous jobs or resume jobs merely by viewing them.
- PDA/visor connection display is an idea only this round: logical saved links,
  all or selected-machine view, reusable snapshot/pair services. No PDA controls
  or overlays were authorised for implementation yet. See
  `docs/industrial-console-player-guide.md` and the original text mockups.
