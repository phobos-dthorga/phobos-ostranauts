# Phobos Ostranauts contributor instructions

These are the standing rules for work in this repository: owner directions still in force,
stated once and in their current form. What each release delivered lives in the owning mod's
`mods/<ModId>/CHANGELOG.md` and the design records in `docs/development/` (indexed in
`docs/development/README.md`). The full dated history of owner decisions, as this file held it
until 5 October 2026, is on the branch `archive/agents-md-2026-10-05`
(`git show archive/agents-md-2026-10-05:AGENTS.md`); read it when a rule's origin or an old
release's scope matters. Keep this file to rules: when a direction is delivered, record the
delivery in the changelog and design record, and add or amend only the rule here.

## Working with the owner

- Practical, small-team project: prefer a working slice over speculative frameworks or process.
  Aim for maximum creative freedom (owner, 2026-09-30); risks such as a mod becoming unsafe to
  remove from a save are engineering problems to report honestly and explore later, never a
  reason to drop, defer or shrink a design.
- Candidly recommend simplifying, setting aside or changing direction when gameplay value,
  engine limits or maintenance cost make further work unconvincing. Do not continue merely
  because effort was spent. Use observed progress to plan rounds; never invent hour estimates.
- Explain what works, what was checked and what remains uncertain. Distinguish observed engine
  behaviour from proposed designs and untested assumptions. A build or offline check is never
  an in-game test; never claim untested compatibility.
- The owner runs gameplay tests. Do not control the owner's mouse or keyboard; give
  instructions for interactive steps. Never edit, replace or delete save files without explicit
  authorisation (read-only copies for diagnosis are fine; delete them afterwards). Read the
  owner's save and `Player.log` before assuming a reported layout or fault is the owner's error.
- The owner's PC is high-end (Ryzen 9 7950X3D, 64 GB): treat captured performance as a best
  case for players, and do not dismiss a few ms per second.
- The overarching direction is long-term habitation and survival in hostile space. Prefer
  features that extend endurance, support crew health and keep the ship maintainable away from
  stations. Indefinite operation is an ambition supported by recovery, maintenance and
  replenishment, never a claim of perfect recycling or unlimited matter. Research each need as
  it arises in play; reuse existing mods first (`docs/development/mod-extension-survey.md`).
- Owner choices, agent defaults and agent choices made while the owner is away are labelled as
  such in design records, so the owner can revise them.

## Git, sessions and publication boundaries

- Commit and push directly to `main`; no PRs unless the owner asks (external contributors may
  use forks, see `CONTRIBUTING.md`). Never force-push, rewrite history or change any
  repository's visibility. Edit in the base checkout, not `.claude/worktrees`, so the owner can
  follow the work.
- Several agent sessions may share this tree. Claim a Framework (or other shared) version number
  with the other sessions before using it, commit explicit paths, and leave others' uncommitted
  work alone.
- Public source does not establish gameplay readiness or third-party reuse terms. Preserve
  authorship, notices and provenance, including Auto Nav's Gravy / mrkmg credit and the MIT
  exclusion on its `Adapted/` files. Do not publish binary releases as a side effect of other
  work.
- Steam: `scripts/prepare-workshop.ps1`/`.py` prepare candidates offline
  (`docs/development/workshop-upload-preparation.md`); `scripts/upload-workshop.ps1` is the
  owner-run uploader, `scripts/update-workshop.ps1` updates only items whose mod version is
  newer than the one uploaded, and `scripts/remove-local-mods.ps1` retires local copies. Agents
  maintain and test them (fake SteamCMD in `tests/workshop-upload.tests.ps1`) but never run a
  real upload, handle credentials, subscribe, announce or change visibility. Keep real item IDs,
  dependencies, holds and the uploaded version and visibility in
  `config/workshop-publishing.json`, unknown IDs as null.
- Never commit saves, decompiled game source, game assemblies, extracted game assets,
  credentials or personal machine paths. Resolve game paths from local settings
  (`.local/install-settings.json`) or arguments. Game-derived research material stays in ignored
  `.local/`. Private-key generation and credential entry are owner-run.

## Versions, constants, changelogs and Workshop pages

- Version numbers (owner rule): a fix or patch raises the last number (0.57.0 to 0.57.1), new
  functionality the middle number, a total conversion or API overhaul the first. Bug fixes,
  wording corrections and performance work that adds no player-facing capability are patches; a
  release mixing a fix and a feature takes the middle bump. Classify before setting a version.
- `scripts/update-constants.py` is the only route for versions and registered constants
  (`config/maintained-constants.json`): preview with `--set Key=value`, apply with `--apply`,
  verify with `--check --format json`, discover with `--list`
  (`docs/development/updating-constants.md`). Never hand-edit registered copies. Extend the
  catalogue, script, validation and tests together when a new recurring field is needed (run
  `tests/test_update_constants.py`), and keep this tooling proportional: migrations, material
  budgets and safety rules are not tuning knobs. Renaming a documentation row that holds a
  registered constant needs its pattern updated too. Current dependency minima live in the
  catalogue and `config/mod-dependency-minimums.json`; preserve historical compatibility gates.
- Every relevant change updates, in the same commit, the owning `mods/<ModId>/CHANGELOG.md`
  (restricted Markdown: Unreleased plus dated Draft/Released entries, no backticks or `>`) and
  `workshop/<ModId>/page.bbcode` (Steam BBCode, at most 7,500 bytes, no ASCII double quotes or
  backslashes). Record player-visible features, fixes, balance, dependencies, save effects and
  known limits; prototypes say they are held. Phobos Scope is not a Workshop mod.
- Generate `workshop/<ModId>/releases/<version>.bbcode` with `scripts/workshop-release-notes.py
  --write`; never edit generated notes. Check with `--check --format json`. Mark a release
  Released only after confirmed publication, with its real date; correct older entries
  explicitly. Follow `docs/development/workshop-publication.md`. A page draft is not evidence a
  Workshop item exists.

## Player language and localisation

- All player-facing English follows `docs/development/player-language.md`, retroactively:
  controls, notices, descriptions, settings help, guides and Workshop drafts. Write for players
  interested in ship operation and survival, assuming no engineering or coding knowledge.
- Original working-spacer voice: practical, worn-in, occasionally dryly humorous in routine
  text; direct in controls, warnings and recovery steps. No forced dialect, gratuitous profanity
  or humour that hides a fault. Say what happened, what it means and what the player can do;
  never promise an action or recovery the code does not support.
- Equipment and marketed-product descriptions speak as the fictional maker's salesperson or
  spokesperson addressing a customer in Ostranauts' world. Lead with why a working spacer would
  buy it, give each maker a consistent voice, and weave accurate specifications and limits into
  the pitch. Ground the copy in vanilla life, work and scarcity; keep it original and compatible
  with established lore. Do not put developer commentary, gameplay simplifications or a recipe
  manual in the sales copy. Controls, faults, safety warnings and operating guides stay direct;
  a sales voice never hides a hazard or promises an unsupported capability. Apply this to
  existing descriptions as well as new ones; see `docs/development/player-language.md`.
- Every command says what it did, and every refusal says why; never ship a silent success or a
  silent refusal. A machine's panel, status and crew order name the one thing it waits for.
- Keep useful game terms and explain unfamiliar ones (shared glossary); keep implementation
  language and precise diagnostics in logs and developer tools. Preserve commands, IDs,
  placeholders, units, numerical contracts, the literal `Phobos'` prefix, makers and models.
- Player text lives in per-mod UTF-8 catalogs with embedded English fallbacks and Framework's
  shared lookup (`docs/development/localization.md`). Stable keys, complete messages with
  placeholders; never translate native IDs, saved keys, commands or configuration keys, and
  never drive gameplay from translated text.
- Review every English entry you add or change and maintain the language audit ledger
  (`config/english-language-audit.json`): refresh it after the item references, add new
  documents under `docs/development` to its `documents` and to `docs/development/README.md`, then
  run `python scripts/audit-player-language.py --check`.

## Documentation

- Player operating guides and item references live in `docs/`; contributor instructions,
  implementation detail, research, designs and audits in `docs/development/`, each with its
  index. Guides lead with player actions and match the implemented controls; keep
  version timelines in changelogs or clearly marked history; link maintained versions rather
  than copying them. When a workflow changes, check every linked guide, reference input and
  generator wording. Preserve dated evidence and citations when moving documents.
- `docs/player-guide.md` is the player starting point, `docs/equipment-economy.md` the current
  prices and bills. Label older snapshots as historical.
- Charts and diagrams are GitHub's native Mermaid, in the Markdown itself. Never use Figma (owner
  rule): not for charts, diagrams or designs, and do not suggest or mention its connector.
- One end-user reference per mod covering every equipment family, item, commodity, byproduct
  and damaged form: function, use, acquisition, placement, value, salvage, Repair and Restore;
  label internal compartments, legacy identities and unimplemented designs. Regenerate with
  `scripts/update-item-reference.ps1` after relevant changes
  (`docs/development/item-reference-maintenance.md`); reviewed wording lives in
  `config/item-reference.json` (2-space JSON); never hand-edit generated references or the data
  snapshot; check with `python scripts/update-item-reference.py --check`.
- Every placeable family, including damaged forms, appears in a suitable native INSTALL tab via
  Framework `InstallMenu` categories (`docs/development/install-catalogue.md`); record genuine
  exceptions rather than inventing fixtures. Menu visibility is not free construction.
- Data packs and their player guide: any schema change keeps `docs/editing-data-files.md`,
  `docs/publishing-an-add-on.md`, `examples/addons`, `schemas/` (regenerate with
  `scripts/write-json-schemas.py`) and `scripts/validate-data-packs.py` working.

## Research attribution

- Name NASA, ESA or the relevant organisation directly whenever its work supports a claim,
  calculation or design decision, with a labelled primary-source link beside the claim (the
  document, experiment or mission, with date or version where material). Attribute the actual
  researchers, distinguishing authorship from a hosting repository. Apply the same to game
  documentation and other mod authors.
- Separate the source's findings from our inference, simplified models, fictional equipment and
  authored balance; label accelerated growth, authored yields and simplified chemistry. Never
  imply endorsement. Mark unverified references as unverified; never invent citations; never
  cite figures a design record lists as unverified. Keep citations in text and documentation,
  never in artwork. Repair missing attribution when you meet it.

## Branding

- Every full equipment name is `Phobos' <original brand> <model> <equipment type>`, with the
  literal `Phobos'` prefix in every language; the functional type stays recognisable; plain
  materials need no model number. Brands are invented for this project and plausible for the
  game's utilitarian setting; share a brand across a related family, and invent a new brand
  where an existing one would crowd (siblings keep their family's brand). Record brands,
  etymology and models in `docs/development/equipment-branding.md`.
- Renaming is presentation only: preserve definition IDs, saved keys, translation keys, recipe
  IDs, commands and package identities; update item and damaged forms, construction, shop
  labels, panel titles and current guides together. Framework owns
  `Localization.EquipmentNames`; content owns its naming maps.
- Prefix new game identifiers with `Phobos` and keep them stable once saves can hold them.
  (Agriculture's PhobosVerdemorrow namespace was a one-off pre-release exception.)

## Architecture

- **Framework vs content.** Phobos Framework is our own shareable framework (instead of OCF);
  every Phobos mod requires it. Concrete shared services (state, notices, observations,
  persistence, transfers, lines, stores, panels, crew work, instruments) go in Framework in the
  same change as their first consumer when another current or plausible mod would need them;
  policy, balance, art and chemistry stay in the content mod. Other authors must be able to use
  Framework without our content mods (`docs/development/phobos-framework.md`). Share equipment
  every mod needs (lines, belts, water tanks, panel hosts) in Framework and consolidate
  near-duplicates; content mods may depend on each other where sharing needs it. One provider per
  item, no circular dependencies.
- **UI presents, services act.** Panels, consoles and F3 commands read state and delegate to
  checked services; they never mutate state themselves. Provide useful F3 commands beside the
  controls through the same services. Every panel choice is listed by its provider's
  `IsConfiguration`, or Apply refuses it. Provider-driven equipment uses Framework's shared
  `ProviderPanel`; the D4, R4, F6, G4, C2 and cooling assemblies keep the industrial panel.
- **Press twice to go ahead** (owner, 2026-10-06, every panel screen and F3 command in every
  mod, retroactively): a change or button that needs other steps first (pausing machines or
  intakes, removing an old link, applying a draft, stopping work, cancelling a batch) never
  refuses for that reason. The first press warns what will be done, including progress lost;
  a second press on the same choice does it through the service and lets paused machines
  carry on without ending their crew orders (Framework `Confirmations`, `PressGuard`,
  `PausedChange`). Only what the service cannot clear (heat, contents, faults, damage, another
  ship or console, a choice only the player makes) refuses, naming the machine and the step.
  Classify every do-first refusal in `config/panel-override-audit.json` and keep
  `scripts/audit-panel-overrides.py --check` passing (`docs/development/panel-override-audit.md`).
- **Test commands** (owner, 2026-10-07, every mod): an F3 test command that changes saved data,
  or how play goes on if the player carries on, works only after the game's own `unlockdebug`
  (Framework `DebugCommands`), warns on every use that the save then lies outside what the mod
  was built for and that later versions will not put it back, needs `confirm`, and marks the
  save as test-changed. Read-only readouts and ordinary player commands stay open.
- **Native definitions first**, C# where the data system cannot express it. Never republish a
  native definition by name; amend in place (`DefinitionAmendments`, `NativeDefinitions.Amend`).
- **Vanilla precedence** (`docs/development/vanilla-precedence-audit.md`): the game's own
  actions win; hook or amend them instead of working around them. Never block native
  destruction, mode switches or task closure: refuse at offer time, and return
  `NativeEffects.Refuse` where a finish must be refused. No owner-only tasks, no need gating
  beyond the game's. A missing trigger name resolves to the always-true Blank trigger: gate
  optional providers on the trigger table (`NativeDefinitions.Trigger`). What follows a repair
  or Restore is the game's own result; never reintroduce repair byproducts.
- **Data, not code.** Authored tables are Framework data packs (`mods/<Mod>/framework/<schema>.json`,
  one loader, a validator per schema), overridable by player files in
  `BepInEx/config/<Mod>/<schema>/` and by Workshop add-ons (`phobos-addon.json`, own id prefix,
  `Data.AddOns`): tune or add by key, never rename or remove a shipped id. Published recipe
  revisions and crops are frozen by content hash: a change adds a revision, and a changed
  construction bill is a new recipe id with the old one marked `retired`. Mass conservation and
  native gas species are enforced on every file; stoichiometry and pricing are authoring rules
  for shipped data. Identifiers, record keys, ports and native mirrors stay in code. Rules built
  from the game's condition names (where something may sit, what it accepts) are data packs with
  a validator, offline checker, JSON Schema and guide section. Chance outcomes are data: one
  frozen recipe per outcome in an `outcomes` pack, picked by a stable hash at bind
  (`Outcomes.Pick`), never rolled at runtime or rerolled.
- **Story content.** News, adverts, goal chains, small talk, loading tips, encyclopedia
  articles and data files go in `story` packs (Framework 0.107.0 to 0.110.0;
  `StoryContent.Register`, one pack per mod, guide `docs/writing-story-content.md`). Never use the game's plots, pledges, new
  social interactions or player conditions for added content: a removed plot breaks the GOALS
  panel. Goals are ordinary objectives whose `PhobosStory.` test reads as the game's Blank when
  its pack is gone; small talk changes only the text of the game's own social lines, through
  lead-ins that use the game's own grammar tokens. New test kinds, moments and channels are code
  in the fixed vocabulary; the later work list is in `docs/development/story-system-design.md`.
  Content belongs somewhere (Framework 0.114.0): shipped arcs, news, adverts, small talk and
  files declare a `thread` or a `place` (the game's stations, `places` in Framework's pack) and
  letters name a `person`; threads connect through `setFlags`, `arcsAtStep` and `newsSeen`,
  never through text that assumes the player saw something. Standing changes stay small and
  go through the game's own faction scores.
- **Links.** Every machine-to-store or machine-to-machine link runs through touching equipment
  (within one tile) or a pipe or conveyor-belt network, never open floor; the refuelling kiosk is
  the only exception. Any pipe or belt on or beside equipment joins it; joins chain across the
  ship. New links use `VesselLink`, new ports `LinePorts`/`LineDefinitions.AddPort`. Every link
  picker offers only what is in reach and says why anything aboard is not offered
  (`LinkChoices.Note`, `LinkNotes`). A store is a container the player can open: weapons,
  chargers, filter holders, toilets and other single-purpose sockets are never stores and
  crew never take from them (Framework `stores` pack, `CrewWork.IsStore`). A line segment counts wherever the game lets it be laid
  (Framework `lines` pack); never add a placement rule stricter than the item's own sockets.
- **Lines hold their contents** (`docs/development/line-contents-design.md`): every pipe holds
  what it carries until drained into a drain canister; declare new holding lines through
  `LineContents.Declare`, never a separate spill.
- **Inventories.** The game shows one inventory per object: a started machine takes feed from
  its own inventory (`OwnInventoryFeed`) and Cancel returns it; never design a second inventory
  window. A machine may optionally take feed from one named store (`StoreFeed`) and deliver to
  one (`StoreDelivery`); hand loading always stays. Size every inventory to its job
  (`InventorySpec`/`EquipmentInventory`): a tray for one to two batches of the largest recipe, a
  small rack for record-based vessels, none for equipment that stores nothing. Deliver products
  as stacks (`TrayDelivery`); `ContainerFit` re-packs shrunk grids; equipment that loses an
  inventory keeps a hidden `LegacyReceptacle`.
- **Reload.** Work that was running when the game was saved carries on after a reload through
  Framework `Persistence.ResumeAfterLoad` (mark while the player's Start stands, start through
  the machine's own checks, a failed check stops it with the reason). Still explicit: the F6's
  hot batch and repeat runs, Auto Nav Rendezvous and Follow, a bare G4 capture approach, the
  recycler wet-reject capture and fire authority. New machines use this from their first version.
- **Power.** Floor machines take power from the tile row directly behind their back edge
  (`ApplianceDefinitions.WallRowY`), checked by `PowerPointNativeChecks` (explicit exceptions:
  F6 front points, G4/ML-2 hull mounts, the C2). Account received electricity once, including
  partial supply; never grant work or heat because a reactor merely runs.
- **Heat and vacuum.** Phobos machines do not work in vacuum and say so plainly (Framework
  `RoomHeat.Check`/`Describe`); a machine whose room loses its air stops without faulting.
  Machines give off a player-set share of their heat (`RoomHeat.MachineHeatScale`, default 0.25)
  through `RoomHeat.Check`/`Deposit`/`Machine`; fires and deflagrations use `DepositHazard`.
- **Time and steps.** A time-skip steps running machines (`CrewSkip.Advance`), never hands them
  hours at once; machines keep the resume mark while running so they are stepped. Use
  `Cadence.RealTime` (counts skipped seconds) for real-time rechecks. Never relax the heat
  check to accept long steps. A crew-work provider without `ICrewSkipProvider` leaves its
  machines unstepped in a skip: implement it whenever a machine gains orders. The game pauses
  breathing, scrubbers, coolers, heaters and air through open doors during a skip, so by
  default Agriculture machines give off no room heat then, room temperature holds nothing
  back and a CO2-short crop waits unharmed, unless the player's `TimeSkip/RoomConditions`
  setting is on (owner, 2026-10-06; Agriculture only, `Service.SkipLenient`).
- **Engine traps.** A Harmony postfix sees by-value arguments as the method left them: capture
  in a prefix. Guard every `CondOwner.Destroy` hook that releases or announces contents with
  `FrameworkLifecycle.Unloading(co)` (a ship unload is not a loss). Measure crew reach tile to
  tile (`CrewWork.Reach`), no tighter than the action's own range, and say why a panel refused
  (`ProviderPanel.Refused`). Navigation panels use `Ostranauts.ShipGUIs.NavStation.Draggable`.
  Harmony patches name overloads by parameter types (patch resolution is checked natively).
- **Crew work.** Vanilla behaviour is the reference (`docs/crew-automation.md`): crew study
  through the game's study chain; standing orders announce one task per Enable; failed steps
  back off and keep the order; never overwrite learned AI history. Load-feed orders keep going
  until cancelled, use our hauling with the ship-wide source, never take from hidden bins,
  locked containers, the equipment itself or anyone's hands, and need AutoTask and the Haul
  duty. Phobos crew orders stay same-ship; work on another ship goes through the game's own jobs
  (`Crew.NativeJobs`). A machine's right-click Maintenance sheet shows its order and upkeep
  with buttons into the Crew panel; the panel's own Standing orders button stays.
- **Crew upkeep** (`docs/development/crew-upkeep-design.md`): idle crew tune, inspect, tidy and
  practise through Framework `Upkeep`, switched ship-wide and always after standing orders.
  Housekeeping moves only loose deck supplies, into stores already holding their kind or stores
  registered with `Upkeep.RegisterTidyStore`; never into or out of machines. A tune only
  makes a machine work faster: call `Upkeep.Draw` where the machine asks for power, so it draws
  that much more and each job costs the same electricity; never change a yield, a recipe or a
  mass with it. New machines register a family (`Upkeep.Register`) from their first version;
  tunable figures are the `upkeep` pack and the `Upkeep` settings.
- **Saves.** Migrate existing saves automatically with idempotent load-time conversions
  (converting old assets to new ones is preferred); a manual step only when unavoidable, stated
  in the changelog and guides. Every plan lists the saved structures it touches and tests them
  with old-record fixtures. Preserve saved IDs, queued-job meaning and historic recipe
  contracts; keep future or corrupt records intact; never silently relocate or destroy items to
  enforce a new rule.
- **Equipment form.** Build machinery at its intended footprint from the first usable version.
  No machine assembled from several identical sections; machines are bought or found whole and
  installed by their own job. Keep bills describing what a machine is made of; dedicated
  assembly equipment is coming to replace table assembly, so do not invest further in table
  assembly behaviour. Use `ItemHandling` for carrying rules (`docs/development/item-handling-audit.md`).
- Expose reasonable player preferences as documented settings; keep identities, dimensions and
  mass-balanced recipes stable rather than configurable. Name limits, conversions and
  tolerances; derive displayed figures from authoritative rules. Extract shared code when real
  features need it, not before.

## Materials, chemistry and economy

- **Honest chemistry** (the owner is an applied chemist): outputs are only what the inputs can
  realistically yield, as close to real stoichiometry as item units allow; state rounding;
  record each recipe's reaction, mass balance, energy and primary source; label authored
  percentages. Every recipe conserves mass and uses native identities for everything
  recoverable. Never copy the game's Dismantle tables. No fixed-reward yields, mass-creating
  recipes or loops that create value while consuming nothing.
- **No custom gas species.** Only native species (CH4, CO, CO2, H2SO4, N2, NH3, O2, Smoke) ever
  enter a room or canister, always through Framework `RoomGas`/`NativeGasCanister`; hydrogen and
  every other missing chemical are kilogram records. The owner reviewed this on 2026-10-06 and
  kept it: the game's dormant H2O, H2 and He2 stay off too, because gas data would live in the
  game's own rooms and spread beyond our equipment, a risk to saves and clean removal not worth
  the gain (`docs/development/custom-gas-research.md`). Do not define their conditions or reopen
  this without the owner asking.
- **Bulk storage** is a Framework bulk vessel (`docs/development/framework-bulk-storage.md`,
  `docs/shipbreaker-bulk-silos.md`): a saved kilogram record, native mass kept equal to dry plus contents, journals around
  transfers, vanilla destructibility. Bulk families offer small, medium and large sizes
  (`BulkVesselSizes`) unless niche; larger sizes are purchase-only. Every vessel commodity
  declares its right-click row (`VesselContentsDisplay.Declare`). Process water is stored only in
  Framework water tanks. Ship's Water: reclaim, never join (draw above the crew reserve through
  reachable tanks, deposit to its waste tanks; never its potable pool or kiosk row).
- **New commodities** only with a concrete consumer (the M2 methane store was a named owner
  exception). New identities only for terminal remainders and raw stock with a real consumer.
- **No trash piling up.** Every remainder needs a consumer before the thing that makes it;
  declare terminal remainders through `Registration.Remainders` (the Manufacturing RM-1 feeder
  consumes them) and deliver them as one stack.
- **Hazards** use only the game's machinery (fire, explosions, damage switches, leaks, poisoning
  bands), conserve mass, are journaled, warn through `PlayerNotices` and never block native
  destruction. Thresholds and recipes are content-owned.
- **Value.** Breaking whole equipment or parts into materials loses value against selling it
  whole; reverse steps that undo a refinement lose value. Refining is a business: chains earn
  about 1.5 to 2.5 times the ore at base prices; supply chains (water ice, hydrates, clay, gases)
  keep native gas prices; bought stock at most 1.25 times; no gaining loop (the one owner
  exception is Phobos Exchange's share drift, under its own guard below); the kiosk buys bulk
  back from installed stores at 45% (never hopper nutrients). Rules and price table:
  `docs/development/refining-business-and-interdependencies.md`; prove new products in the native
  value checks. Building to sell must lose money: rerun `scripts/audit-economy.py` after any
  price, bill or loot change. Repricing an item that saves its own price needs
  `EquipmentSaveUpgrade.FollowPrice`. Machinery from Manufacturing is late-game priced and
  carries `IsSalvageValueHigh`; materials keep ordinary prices.
- **Selling.** Stock offered goods in large finite lots through content-owned `StockQuantities`
  and the registered `*.stock*` constants (`docs/development/merchant-stock.md`); keep
  probability separate from count; preserve native restocking and saved inventories; never put
  wholesale lots, installed machinery or process records into world loot. Every sold item is also
  offered at the CCRE and GalCon faction kiosks for scrip with a reputation tier in the same
  change; nothing ever needs Honored (`docs/development/faction-kiosk-stock.md`). Keep every
  machine family's merchant, loot, repair, Restore and dismantle coverage at parity with its
  siblings.

## Mod-specific rules

- **Agriculture.** A crop is a data entry plus artwork; never branch on a crop id in code. One
  nutrient for every crop: never bring back a feed to choose or a reason to unlink a W2; crop
  climate, stress and feeding figures are the crops pack's `growth` section, rack capacities stay
  in code. Brands: Verdemorrow, with Firstlight (cultivation), Hearth (cooking), Continuance
  (planting stock), Groundwork (nutrients). Cultivation never grants perfect recycling;
  wastewater nutrients stay uncharacterised. Guides: `docs/agriculture-player-guide.md`,
  `docs/fluid-network-operations.md`; expansion record `docs/development/agriculture-crop-expansion.md`.
- **Manufacturing.** Shipbreaker owns recovery and rough casting; Manufacturing owns refining,
  machining, tooling and finished components. Machines are purchase-only; ores and chunks are
  mined, never sold. The K2 record keeps its holds and one-step conversion; new fields are
  optional and written only when non-zero, never required. Optional recipes (`chosen-<id>`) run
  only on a machine whose saved preference names them. Records:
  `docs/development/manufacturing-refinery-and-chemistry.md`, `docs/development/regolith-programme.md`.
- **Shipbreaker.** The F6 furnace heats electrically; keep its hot state, explicit resume and
  exact physical charge (`docs/development/furnace-electrical-direction.md`). The ML-2 laser works
  only on attached targets and drives the game's own mining damage and uninstall, so the game's
  loot tables stay the only source of ore: never spawn ore or copy a native table; opened
  deposits stay crew work. The G4 cuts only supported empty native walls and never suppresses
  collisions, expands reach or becomes a port. Unsupported cargo is retained, never deleted.
- **Auto Nav.** Auto Nav owns flight; Shipbreaker owns capture and mission coordination. Keep
  native fuel, heat, wear, clearance and the safety limiter; clear thrust before turning and on
  control loss; never replay saved thrust. Sensor use (`docs/auto-nav-sensors.md`): switch on
  the fewest fitted sensors needed, passive first, never claim sensors the player already has
  on, warn every time, switch off only our own when the work ends; the player's switch always
  wins. Never discard hazards on planner exhaustion or resume after tracking loss. Keep the
  approved faceplate and module art.
- **War Has Been Declared.** Puts the game's own build sites back where combat destroyed parts
  on the player's ships; crew build them with real parts. What is replaced comes from strict
  player-editable schematic files; extend the format with tests and the guide together.
- **Banking.** Phobos Banking (`PhobosBank`) builds on the game's own ledger: it shows the
  debts the game already keeps, payment stays in the game's Finances window, and default has
  only the game's own consequences until the owner reopens repossession. Loans may fund broker
  purchases, integrated with the broker's own window; lenders local to stations build on the
  story system's places, people and threads. Apps on the PDA go through Framework `PdaApps`.
  Design record: `docs/development/pda-apps-and-banking-research.md`.
- **Exchange** (owner, 2026-10-07). Phobos Exchange (`PhobosExchange`) is its own mod needing
  only Framework; its link to Banking is optional at runtime (Framework `PlayerHoldings`).
  Listed companies mix the game's ubercorps, invented ones and our makers; news about the
  game's companies is a wire report, never their own words. Prices follow the game's cargo
  market plus trend phases lasting weeks, with a real-world upward drift: the owner's one
  exception to "no gaining loop", held to an expected annual return at most half the cheapest
  Banking loan's yearly cost, so borrowing to hold never pays (validator and test). Market
  noise is a stable hash of save, company and step, never rolled; a reload never rerolls it
  (peeking ahead by reloading is accepted). Pace for weeks of play; any time jump, up to years
  at once, is caught up exactly at a bounded cost with no per-step side effects. Design record:
  `docs/development/share-market-and-charts-design.md`.
- **Medical.** The Halewright brand (models a word plus footprint width); the Ward-3 bed is the
  visual baseline (`docs/development/medical-art-handoff.md`). Treatments perform the game's own
  wound slotting on real items; the monitor never heals; a patient never treats themselves
  (`CrewWorkOffer.ExcludedActor`). A treatment is a `care` pack entry over the code's fixed tests
  and effects; never branch on a treatment id in code. The vanilla Infirmaway stays untouched.

## Artwork

- Follow `docs/development/asset-generation-policy.md` and
  `docs/development/artwork-resolution-policy.md`. PixelLab for simpler and small sprites
  (overhead-first prompts: vertical orthographic camera, top surfaces only, `view="high top-down"`,
  `isometric=false`; inspect one pilot before a family); ChatGPT/Imagegen for high-resolution
  equipment bases with PixelLab layers where useful. Check allowance and cost first; never
  silently buy credit or switch to a costlier generator.
- New or revised art is made at least 2x its display size (4x when the short side is 32 px or
  less) and exported to native size deterministically; keep untouched masters, prompts, seeds,
  provider IDs and hashes; keep colour, normal and damage forms registered.
- Match the game: overhead projection, coarse pixel clusters, simple shapes, restrained palette.
  No painted live-state instruments (gauges, fill strips, screens) on world sprites; readings stay
  live text on panels. Keep separately placed infrastructure (conduit) out of machine art; give
  readable connection points instead. Dark screenshot regions are view obscuration, not missing
  light.
- Rejected and unselected attempts go to `codex/rejected-artwork` with requests, reasons and
  hashes recorded on main (`assets/rejected-artwork-archive.json`, `scripts/archive-artwork.py`).
  Never remove selected masters or needed inputs; never ship game sprites.
- Prefer vanilla UI widgets and artwork at runtime; compose panels from reusable controls and
  live localized text; avoid art reruns or refactors for hypothetical future changes (the owner
  watches ChatGPT costs). Approved selections stay unless the owner reopens them.
- Sounds only where usefulness is high: quiet, never alarming, with volume and mute
  (`docs/shared-completion-cues.md`). Cues stay brief. Local mechanical operating sounds
  must be loopable for continuous playback while equipment actually works (owner,
  2026-10-06, superseding the earlier occasional-snippet direction and operating-loop
  exclusion). Codex creates the audio and handoff; Claude does the wiring
  (`docs/development/machine-work-sounds-handoff.md`). No alarm suites, unrelated ambient
  loops or routine per-action sounds.

## Builds, installation and verification

- Build scripts need the game path (`-OstranautsPath`, from `.local/install-settings.json`).
  Install only with the game closed through `scripts/install-mods.ps1` (`-WhatIf`, `-VerifyOnly`;
  `docs/installing-mods.md`); never copy files or edit load orders by hand. Approach Assist is
  retired. Create or extend scripts when repeated work warrants it.
- Scale checks to the change; test gameplay rules and persistence, not static documentation.
  Well-established patterns in reputable mods are sufficient evidence for reuse; focus
  verification on new logic and concrete suspected failures. For new machinery check material
  accounting, progress, interruption and persistence where you change them.
- The usual release run: build with native checks, `scripts/update-item-reference.ps1`,
  `scripts/calculate-agriculture-storage.py --write` when Agriculture changes, the language
  ledger and `audit-player-language.py --check`, `audit-performance.py --refresh --finding <Lnn>`
  for changed hot paths, release notes, `python -m unittest discover -s tests`, then commit.
- Performance (`docs/performance-captures.md`, `docs/development/performance-audit.md`):
  recording is opt-in and costs nothing while off. Record each hot-path change as a finding.
  Memory sources that cannot be read are named, never recorded as zero; percentiles from
  buckets are upper bounds, never interpolated; memory per operation is unavailable on the
  game's Mono, so say so. A mod that keeps a strong collection by key registers it with
  `Performance.RegisterFootprint`. Keep the `recording_id` and `window` capture metadata.
  Recorder and analyser contract changes follow the Phobos Scope repository's own AGENTS.md.
