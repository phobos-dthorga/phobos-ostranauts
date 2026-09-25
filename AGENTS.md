# Phobos Ostranauts contributor instructions

## Localization and constants

- Owner direction (2026-09-25): `scripts/update-constants.py` is the standard
  maintenance route for versioning and frequently updated, appropriate constants
  across all Phobos Ostranauts mods. Use `config/maintained-constants.json` to
  register their authoritative copies and current summaries; do not maintain
  registered copies independently or repeat ad hoc search-and-replace edits.
- Preview with `--set Key=value`, apply with `--apply`, and verify with
  `--check --format json`. Use `--list` to discover supported fields and
  `--format json` for machine-readable reports. Follow `docs/updating-constants.md`,
  review the diff and reported follow-up items, and run affected builds/checks.
  Updating source values does not rebuild, install or publish packages.
- Maintain and extend the updater itself as needs arise. When a new recurring
  field or target format is appropriate, update the catalogue and, where needed,
  the script, validation, recovery tests and usage documentation together instead
  of working around the tool. Run `tests/test_update_constants.py` through unittest
  discovery and the catalogue consistency check after changing this mechanism.
- Keep this maintenance tooling proportional to actual needs. Preserve saved IDs,
  historic recipe contracts, compatibility thresholds and historical reports.
  Do not register migrations, interdependent material budgets or safety rules as
  routine tuning knobs; those still require their own design and validation.

- Keep player-facing text in per-mod UTF-8 translation catalogs, with embedded
  English fallbacks and Framework's shared lookup. See `docs/localization.md`.
- Use stable translation keys and complete messages with placeholders. Never
  translate native IDs, saved-state keys, command names or configuration keys,
  and never use translated status text to drive gameplay decisions.
- Name meaningful limits, unit conversions and tolerances; derive displayed
  capacities and yields from authoritative rules. Keep balance in the owning
  content mod and preserve historic recipe contracts. Do not generalize every
  layout coordinate or expose every constant as a setting.

## Equipment branding and model names

- Owner memorandum (2026-09-24): all our objects, machines and other equipment
  must use world-appropriate branding and model names invented for this project.
  Share brands and model families across related equipment where appropriate;
  keep names plausible for Ostranauts' utilitarian industrial setting.
- Every full equipment display name must begin with the literal `Phobos'`
  prefix, including the apostrophe. Preferred structure:
  `Phobos' <original brand> <model> <equipment type>`. Keep the functional type
  clear so players can identify the item's purpose. Assign model designations
  where meaningful; ordinary materials need no artificial machine model number.
- This policy covers existing equipment as well as future additions. The
  current implementation uses Asterel for electronics, Rivetline for Shipbreaker
  equipment and Verdemorrow Agronomics (short brand: Verdemorrow) for Agriculture.
  See `docs/equipment-branding.md` for assigned models and agricultural supply lines.
  When applying a name, update item/damaged forms, construction, shop labels,
  control-panel titles and current player documentation together. Historical
  reports may retain their original names when clearly identified as historical.
- Keep branding and model designations consistent across translations; localize
  equipment types and descriptions through the existing catalogs. Retain the
  `Phobos'` prefix in full translated equipment names too.
- Renaming is presentation only: preserve native definition IDs, saved-state
  keys, translation keys, recipe IDs, console commands and mod/package identities.
  Brand/model selection belongs to content mods. Framework 0.12.0 owns the shared
  `Localization.EquipmentNames` pattern, with content-owned embedded naming maps
  and translated type/variant descriptors. Retain existing translation keys.
- Agriculture exception (owner clarification, 2026-09-25): the mod has never been
  used, so its content identifiers may be replaced without migration aliases.
  Agriculture 0.1.1 adopts the PhobosVerdemorrow namespace for its equipment,
  supplies and recipes. This is not permission to change other mods' saved IDs.

## Supporting research and institutional attribution

- Owner memorandum (2026-09-25), effective immediately for all current and future
  Phobos Ostranauts mods, including Framework: explicitly name NASA, ESA or the
  relevant research organization whenever its work supports a claim, calculation
  or design decision. Do not leave that attribution implicit in a link or describe
  it only as generic "research".
- In research notes, design documents, explanations and relevant player-facing
  help, place a clearly labelled primary-source link beside the supported claim.
  Identify the organization and the document, experiment or mission; include its
  date/version where material. Attribute the actual researchers or institution,
  distinguishing authorship from a repository merely hosting a paper.
- Clearly separate the source's findings from our inference, simplified model,
  fictional equipment and authored gameplay balance. Explain material limits of
  applying the research; attribution must not imply institutional endorsement or
  validation of our mod. Mark unverified references as such rather than inventing
  citations. Correct missing attribution when encountered in existing material.
- Keep equipment branding original and controls readable. Put supporting research
  in the relevant explanatory text or documentation, with live/localized text
  where it appears in-game; do not bake citations into machinery artwork.
- Apply the same direct attribution to original researchers, game documentation
  and mod authors. Label accelerated growth, authored yields and simplified
  chemistry explicitly. Preserve references in relevant design and release
  documentation alongside separate artwork provenance and licensing records;
  repair or extend citations whenever related existing research is revised.

## Agriculture direction (2026-09-25)

- Owner follow-up: research shared fluid pipes for plant sustenance (water and
  nutrients), taking after vanilla conduits in part. This is distinct from
  Agriculture cooling. Follow `docs/fluid-conduits-and-irrigation-research.md`:
  reusable transport belongs in Framework, equipment/biology in content mods;
  native conduit placement and sprite patterns are candidates for reuse, while
  fluid accounting stays separate from electricity. Water-first/local nutrient
  dosing was the first recommendation, not an exclusion of later nutrient pipes.
  The owner subsequently authorized implementation and necessary artwork.
  Agriculture 0.4.0 / Framework 0.18.0 prepare one W2 supply-to-rack water route,
  finite manual/provider inlet, local nutrients, independent pipe sockets and
  guarded receipts. Follow `docs/agriculture-water-conduits.md`. Receiving/pumping
  pauses on reload; legacy refill remains explicit. Multi-rack allocation,
  nutrient mixtures and furnace coolant loops remain future work. No installation
  or gameplay validation is implied by prepared packages.

- Agriculture has a separate fictional manufacturer: **Verdemorrow Agronomics**,
  evoking verdant growth and humanity's tomorrow in space. Use Verdemorrow on
  item names, Firstlight for cultivation machinery, Hearth for cooking and
  prepared food, Continuance for planting stock, and Groundwork for nutrients.
  Current models are Firstlight-4 and Hearth-2. Ordinary produce and retained
  waste carry the brand without artificial model numbers. Keep the literal
  `Phobos'` prefix and recognizable functional types. These are fictional product
  families, not claims about real cultivars or research-institution endorsement.
- The owner selected a separate Phobos Agriculture content mod requiring Phobos
  Framework, beginning with potatoes and lettuce, automatic environmental control
  and crew planting, harvesting and maintenance. Short configurable growth cycles
  are authored gameplay balance. Follow `docs/agriculture-research.md`,
  `docs/agriculture-first-slice.md` and `docs/agriculture-roadmap.md`.
- The owner subsequently authorized implementation. Agriculture 0.2.0 supplies
  the Firstlight-4 rack, Hearth-2 portion cooker, potato/lettuce cohorts and finite manual inputs.
  Framework 0.17.0 adds equipment providers and measured liquid transfers;
  Shipbreaker 0.14.0 exposes agriculture through C1. The optional Ship's Water
  adapter is scoped to inspected 0.16.1, with manual fallback for other versions.
  Follow `docs/agriculture-player-guide.md` and `docs/agriculture-implementation.md`
  for delivered scope and owner checks. Prepared packages are not installed or
  gameplay-validated. Keep biology in Agriculture, preserving Framework fixed
  batches and their one-hour limit.
- Ship's Water irrigation and Shipbreaker's industrial-console/material links
  remain optional. Protect crew water reserves, same-ship isolation, finite manual
  supply and existing residue/reject identities. Future nutrient/asteroid recovery
  needs characterized products; cultivation does not grant perfect recycling.
- PixelLab is preferred for a small potato growth-stage pilot before broader
  plant production, following the shared asset and resolution policies. Separate
  plant and rack layers, preserve registration/provenance and measure cost per
  usable sprite. Research and planning do not initiate paid generation.
- Owner follow-up (2026-09-25): continue features before gameplay testing, selecting
  visible plant growth and lettuce artwork. Use ChatGPT for high-resolution grow-rack
  and galley furniture masters, with separate PixelLab plant/stove layers. Agriculture
  0.2.0 selects registered native-size compositions from saved crop state through
  native Item.SetAlt; the local panel shares the same stage policy. Preserve the
  4 x 4 / 2 x 2 footprints, four-tray single-cohort meaning and read-only rendering.
  Follow `docs/agriculture-living-visuals.md` and `assets/phobos-agriculture/layers.json`.

## Manufacturing direction (2026-09-25)

- The owner approved a separate **Phobos Manufacturing** content mod for dedicated
  machining and finished components, beginning with research for one enclosed
  milling machine/machining centre and heat-sink finishing. Follow
  `docs/manufacturing-handover.md` in its separate task. Mod creation and research
  are authorized; no Manufacturing equipment is implemented by the handover.
- Require **Phobos Framework only** as a mod dependency, alongside the normal
  game/loader prerequisites. Do not require Ostranauts Crafting Framework,
  Salvage Workshop or their associated content. Keep Shipbreaker and other
  Phobos integrations optional, with valid standalone stock acquisition.
- Shipbreaker owns recovery and rough casting processes; Manufacturing owns
  machining, tooling and finished components. Framework owns concrete shared
  services. Establish one provider per item and avoid circular dependencies.
- Dedicated machining supersedes ordinary-table finishing in the new heat-sink
  proposal. Preserve the already delivered housing recipe, saved IDs and hot
  jobs. New proposed heat-sink IDs and balance are still open to revision.
- Start with one useful machine/job; later lathes need concrete turned products.
  Research workholding, contained chips/swarf, power/heat, maintenance, finite
  outputs and interruption before expanding machinery or process fluids.
- The first research round and buildable Manufacturing 0.0.1 scaffold are in
  `docs/manufacturing-research.md` and `docs/manufacturing-implementation.md`.
  The proposed M4 enclosed mill, two-sink preform and finite machining cartridge
  remain unregistered designs. The scaffold has no operational machinery and
  is not installed. Keep the optional F6 recipe separate from historic housings;
  follow `assets/phobos-manufacturing/README.md` for layered artwork planning.

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
- Owner authorized public visibility and newcomer documentation on 2026-09-25,
  including the Phobos Scope dependency, and selected MIT for original Scope work.
  Never force-push or change other repositories' visibility implicitly.
- Owner follow-up (2026-09-25): although this repository is public, Steam
  publication has not happened yet. For the meantime, maintainer/agent work uses
  ordinary commits and direct pushes to `main`; do not create PRs or initiate
  PR review/merge workflows unless the owner explicitly requests one. This
  supersedes the earlier public-repository PR requirement and skill defaults.
  Keep appropriate checks and normal Git protections. Continue this policy until
  the owner changes it; a future Steam release is a reason to revisit it, not
  permission to silently switch workflows. External contributors may still use
  forks and PRs as described in CONTRIBUTING.md.
- Public source availability does not establish gameplay readiness or resolve
  third-party reuse terms. Preserve authorship, notices and provenance, including
  Auto Nav's unverified upstream terms and explicit MIT exclusions. Do not publish
  binary releases as a side effect of documentation or visibility changes.
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
- The owner selected a furnace as Shipbreaker's centrepiece with a tactile
  reactor-like control panel (2026-09-24), initially requesting direct fusion.
  On 2026-09-25 the owner explicitly approved **electrical heating as the first
  route**. Follow `docs/furnace-electrical-direction.md`; this supersedes the
  direct-fusion-first constraint. Reuse native received-electricity accounting
  and remove the reactor-side heat coupler requirement. Follow
  `docs/fusion-smelter-research.md`: meaningful live gauges, bounded process
  controls, automatic recipes plus manual sequencing, local/C1/F3 service access,
  finite heat rejection and preserved hot-state saves. Induction is the researched
  electrical-heater candidate, with hardware/efficiency still provisional.
  Shipbreaker 0.12.0 prepares the F6 electrical candidate at 6 x 6 tiles,
  50 kg rating and 250 kW delivered heat, with a 20 kg first housing batch.
  Shipbreaker 0.13.0 adds optional F6-P underside cooling through a separate
  1 x 1 sealed head at furnace-local (-3.5,+0.5) or (+3.5,+0.5), rotated together.
  Keep the intact sealed native floor and existing exterior radiator route; one
  cooling endpoint per furnace, with equal 100 kg / 12 m² finite assemblies.
  Preserve old radiator IDs/maps. Switching requires cool endpoints and empty
  idle furnace inventories. The underside area is an authored abstraction, not
  a simulated lower deck or an atmosphere vent.
  Shipbreaker 0.14.0 / Framework 0.17.0 add reactor-inspired named attachment
  points, inward-facing coupling artwork, a rotating installation key and native
  guarded-toggle/digit/slider adapters. See `docs/furnace-connections-and-instruments.md`.
  Retain existing placement offsets, thermal rules and saved pairs. Artwork
  selection is presentation only; slider drafts apply through the checked service.
  Follow `docs/furnace-player-guide.md`; gameplay and new art await owner review. Raw fusion heat has no established native outlet and
  remains historical research, not a prerequisite for the electrical furnace.
  Account actual electrical consumption once, including partial supply and losses;
  never grant heat merely because a reactor is running. Yield to flight authority.
  Reuse Framework
  for concrete shared state, accounting, controls and endpoint needs; content owns
  furnace recipes, art and balance. Framework 0.16.0 shares measured receipts,
  thermal/gas primitives and isolated native instruments. Keep hot state, explicit
  resume, exact physical charge, finite gas receiver and guarded output commits.
  The 2026-09-25 follow-up is `docs/furnace-first-cycle.md`: proposed 20 kg
  aluminium housing batch, optional D4/R4 construction use, finite radiator and
  gas receiver; its original direct-fusion source section is superseded. The owner explicitly
  prefers vanilla UI reuse: follow `docs/furnace-ui-and-art.md`, first isolated
  widgets, then native artwork with adapters, original UI art only for gaps.
  Keep game-derived material local and reference native assets at runtime;
  the browser layouts and offline calculations are not Unity/gameplay validation.
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

- Auto Nav 0.10.0 implements the four 2026-09-25 audit recommendations. All RCS
  axes share the selected throttle, including turning; ordinary Fly/Resume uses
  conservative current-motion RCS braking-room admission. Do not apply that
  admission check as an abort of an already running brake. Dock retains its own
  capture policy. Numeric cruise/arrival-speed/distance preferences use a separate
  per-console Framework object store, seeded from configuration without display
  writes; active/suspended profiles remain captured. Details/F3 share mutations.
  Rare native nav-module salvage uses Framework 0.14.0 AdditiveLoot, also reused
  by merchant stock. Content owns balance and leaf-table selection; native pools
  may also feed merchants. Preserve inventories, other providers and stable IDs.
  Existing artwork is unchanged. See `docs/auto-nav-flight-profiles.md`;
  gameplay evaluation remains with the owner.

- Auto Nav 0.6.0 adds the owner's requested torch preference, including approach
  braking where native zones permit it (2026-09-24). Do not impose a blanket
  short-range torch ban. Keep native fuel/heat/wear, running-reactor readiness,
  the safety limiter, no-wake checks and conservative RCS braking room. Clear
  thrust before turning/coasting and on control loss. Save idle reactor actuator
  state, not a live burn; old flights without explicit torch preference remain
  RCS-only. See docs/auto-nav-torch.md. Gameplay validation remains pending.
- Auto Nav's immediate goal is short-range ship/station approach **below 5,000 km**
  (owner direction, 2026-09-24; vanilla threshold owner-reported). Do not confuse
  starting range with arrival distance or add an artificial engagement minimum.
  The owner selected **1 km default arrival**, adjustable closer. Version 0.4.0
  accepts 0.1–100 km arrival requests with native hull clearance; preserve existing
  preferences and capture overrides per flight. Keep diagnostics and panel/F3
  actions on the same service. Obstacle avoidance and continuous working
  position control remain separate future features. See `docs/auto-navigate-adaptation.md`.
- The owner reports successful Auto Nav flight behaviour but wants less RCS waste
  from excessive precision (2026-09-24). Version 0.4.3 captures configurable cruise
  hysteresis per flight, corrects only to the acceptable band and stops chasing
  heading during coasting. Preserve sideways-drift and braking overrides plus the
  separate final-arrival tolerance. Numerical delta-v comparisons are not measured
  game fuel savings; UI 0.4.2 and coasting 0.4.3 still need owner validation.
- The owner requests Polaris in Auto Nav's equipment name to identify its
  navigation-station use. The name is now **Phobos' Asterel N1 Polaris Auto Nav
  Module**, retaining Polaris as a compatibility cue, not our invented brand.
  Apply the chosen name to the item, localized damaged form, construction and
  panel branding. Retain saved IDs and
  the Phobos Auto Nav package identity. Follow `docs/auto-nav-economy.md` for its
  native trade, repair, Restore and mass-balanced salvage. Reuse Framework services;
  do not invent extra scrap mass or duplicate economic machinery. Existing approved
  sprites/faceplate remain suitable because names are rendered as live text.

- Use native JSON definitions for suitable content and existing behaviours.
- Owner sensor direction (2026-09-25): use native sensing where appropriate,
  with full instrumentation realism, built-in basic probes and modular specialist
  instruments. Follow `docs/sensor-integration-research.md`. Measurements need a
  credible source and scope; unavailable/stale/faulty readings are not zero.
  Native ship IR is not a furnace thermometer, and contact silhouettes do not
  establish grabber clearance. Do not silently enable emitting sensors or use
  hidden exact target state to bypass weak contact. Prioritize sensor-aware
  Auto Nav, then console observations and furnace instrumentation. Shared access,
  observation validity, pairing and diagnostics belong in Framework as concrete
  consumers need them; instrument semantics, balance and process responses stay
  content-owned. Preserve ship isolation, saved-state protections, material
  contracts and optional providers. Physical heat/matter continues to exist when
  a probe fails. Auto Nav 0.9.0 implements native live-contact qualification and
  suspends on loss, retaining intent for explicit Resume. Recheck before steering,
  clamping and restoring a saved flight; UI reads stay read-only, and missing
  range/speed stay unknown. Keep selected-operator thresholds fresh with the
  panel closed. See `docs/auto-nav-sensors.md`. Framework 0.13.0 and Shipbreaker
  0.11.0 add shared observations: native room-alarm outputs, built-in R4 cooling
  probes, source/compartment/validity diagnostics and session-only stop evidence.
  See `docs/shared-console-observations.md`. Panel/F3 reads use the same checked
  console boundary; native output witnesses require a fresh evaluation after
  reload, and dock-inclusive ambiguous sampling fails closed. Retain historic
  values only as historic, never erase physical heat when an instrument fails,
  and do not infer that an unrelated alarm caused a machine to stop. Furnace
  and specialist instrumentation remain future work.
- Navigation panels must explicitly use
  `Ostranauts.ShipGUIs.NavStation.Draggable`, not the game's same-named global
  object-hauling component. Bind `NavModBase.DraggableRef`; the package build
  checks the compiled references. See the Auto Nav 0.4.1 Edit-mode fix notes.
- Match navigation placement bounds to visible artwork. Auto Nav 0.4.2 uses the
  native 20% board-height row; owner follow-up confirmed its 2:1 width was too narrow.
  Auto Nav 0.5.0 uses the native 25% column width with that same 20% height and
  sliced rendering of the unchanged approved PNG to preserve corner/screw shapes.
  Normalize saved/default
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

- Owner memoranda (2026-09-25): prefer PixelLab for simpler pixel-art assets,
  and explicitly permit ChatGPT-generated high-resolution equipment/furniture
  bases with separate PixelLab plant, appliance, workpiece and state layers.
  This complementary workflow applies across all Ostranauts mods, including
  Manufacturing. Match final projection, palette and pixel density; use stable
  pivots/attachment positions, retained masters and deterministic native-size
  exports. Keep runtime lighting, visibility, rotation and damage coherent.
  Authoring layers need not be separate gameplay objects. Follow the Agriculture
  layering precedent and `docs/asset-generation-policy.md`; no mandatory use of
  both providers, wholesale art migration or immediate generation is implied.
  This explicit preference supersedes generic imagegen-skill provider defaults.
- Follow `docs/asset-generation-policy.md`, incorporating the inspected Codename
  Gekko PixelLab workflow lessons. Prefer the lowest-cost suitable single-image
  operation; check current allowance and operation cost, preserve prompts/seeds,
  provider IDs and untouched masters, and inspect native-scale exports before
  commissioning more. PixelLab's different operations have different costs;
  do not assume an object/directional batch is as cheap as one image.
- Retain vanilla UI reuse, existing deterministic exporters and the resolution
  memorandum. Generate original world sprites in Ostranauts' overhead projection;
  do not import Gekko's isometric requirement, assets, credentials or separate
  approval process. If the configured service is unavailable or unexpectedly
  requires additional paid credits, explain the gap and request the needed input
  instead of silently purchasing credit or switching to a costlier generator.

- Owner direction (2026-09-25): minimize future graphics rework. Follow the
  production approach in `docs/furnace-ui-and-art.md`: compose panels from
  reusable native controls, resizable framing and live localized text; centralize
  donor mappings and keep full/compact views on the same presentation components.
  Keep new machine art in editable registered source layers with stable canvas,
  crop/pivot and connections, then export conventional flattened native assets.
  Use manifests and small repeatable exporters as real masters become available.
  Do not build a speculative asset framework or migrate unchanged approved art.
  The owner conditions this on avoiding a drastic increase in ChatGPT charges:
  keep added effort small, reuse existing tooling, and avoid extra image-generation
  runs or broad refactors solely for hypothetical future graphics changes.

- Owner authorised a substantial Auto Nav instrument redesign on 2026-09-24,
  superseding the earlier instruction to retain the original faceplate as the
  active UI. Keep that approved master and pickup sprites unchanged. Version
  0.7.0 introduces a separate faceplate, live rotary propulsion/arrival controls,
  phase/target/range/relative-speed readouts and scrollable Details within the
  same native 25% x 20% placement bounds. See `docs/auto-nav-instruments.md`.
  Preserve captured flight settings and command authority in the service;
  display reads must not advance physics or rewrite saves. AUTO is preference,
  not an assertion of torch clearance. The new design awaits owner evaluation.

- Owner's resolution memorandum (2026-09-24): all newly created or visually
  revised artwork uses at least 2x its intended display dimensions, or 4x for
  very small artwork. Working convention: use 4x when the intended short side
  is 32 pixels or less. These are per-axis multipliers, not increased physical
  footprints. Preserve larger original masters. Keep pixel art crisp with
  integer scaling and nearest-neighbour sampling; enlargement alone adds no
  detail. See `docs/artwork-resolution-policy.md` for examples and export rules.
  Native world sprites currently derive size from texture dimensions, so use
  explicit rendering-scale support or code-generated native-size derivatives.
  Do not silently ship larger world PNGs at the old native scale. Preserve
  matching colour/normal/damage alignment, UI bounds and approved designs.
  Existing unchanged assets and reproducible exports need no bulk migration.

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
  the approved artwork and its corner/screw proportions. The 0.5.0 panel uses
  sliced rendering to fill the native column, per the owner's width correction.
  See `assets/phobos-autonav/README.md`.
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

## Auto Nav persistence (2026-09-24)

- Owner authorised docking on 2026-09-24. Auto Nav 0.8.0 adds a separate Dock
  action within 10 km of hull contact distance, using RCS and existing native
  clearance, compatible ports and attachment. Ordinary Fly still stops short.
  Capture the assigned port pair and retain it through saves. Docking always
  suspends after reload for explicit Resume; do not weaken ordinary Fly's own
  resume policy. Reuse Framework storage/controls, keep flight policy in Auto Nav.
  See `docs/auto-nav-docking.md`; gameplay verification remains with the owner.

- Owner requested flight state across saves/reloads and shared Framework support.
  Auto Nav 0.5.0 uses Framework 0.11.0 `Persistence.ObjectStateStore`; native object
  property maps keep each save isolated. Framework owns versioned storage and
  envelope protection; Auto Nav owns flight fields and authority to resume.
- Preserve target/console/module/ship/player IDs, captured flight/coast profile,
  elapsed timeout budget, coasting latch and active/suspended/stopped/arrived mode.
  Never replay saved thrust or substitute crosshair/nearest hardware. Active
  flights resume by default only after load completion and validation; failed
  validation suspends for explicit Resume. `ResumeAfterLoad=false` opts out.
- Keep future/corrupt records intact unless explicitly forgotten. Old saves with
  no flight record remain idle; no retroactive recovery. Native physics serialization
  removes only our active ship's actuator commands in the copied DTO, never live
  velocity/spin/gravity. See `docs/auto-nav-persistence.md`. Do not change the
  independent industrial pause-on-reload policy. Owner gameplay testing is pending.
