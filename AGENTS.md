# Phobos Ostranauts contributor instructions

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
  `docs/asteroid-life-support-research.md`; these machines/resources are research,
  not implemented features or verified integrations.
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

- Use native JSON definitions for suitable content and existing behaviours.
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
  Conveyor transport is a planned shared service, not an existing implementation.
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

## Artwork

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
- Use a separate test save for gameplay experiments. Do not directly edit, replace
  or delete the player's real saves without explicit authorisation.
- Do not commit saves, decompiled game source, game assemblies, extracted game
  assets, credentials or personal machine paths.
- Resolve game references from a local path or configuration, not a committed
  machine-specific location. Document third-party licences and asset provenance.
- For private-key generation, conversion or credential entry, provide manual
  instructions to the owner; do not automate it or inspect populated secret fields.

## Verification

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
- For new machinery, check our material accounting, progress, interruption and
  persistence where we introduce or change that behaviour. Cover power or
  fast-forward interactions when they pose a specific integration risk. Record
  the game and plugin versions tested.
- Never call a successful build an in-game test. Do not claim untested compatibility.
