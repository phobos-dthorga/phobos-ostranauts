# Phobos Ostranauts contributor instructions

## Localization and constants

- Owner direction (2026-09-27): all current and future player-facing English
  across every Ostranauts mod must follow `docs/development/player-language.md`. Apply this
  retrospectively, including controls, notifications, item/recipe descriptions,
  settings help, player guides and Workshop drafts. Write for players interested
  in ship operation and survival without assuming engineering or coding knowledge.
- Use an original working-spacer voice: practical, worn-in and occasionally dryly
  humorous in descriptions and routine messages. Keep controls, warnings and
  recovery instructions direct. No forced dialect, gratuitous profanity or humour
  that hides a fault. Say what happened, what it means and what the player can do;
  never promise an action or recovery the implementation does not support.
- Keep useful game terms and explain unfamiliar ones. Use the shared glossary;
  avoid implementation language in ordinary messages. Keep precise diagnostics
  in logs/developer tools. Preserve commands, IDs, placeholders, units, numerical
  contracts, the literal `Phobos'` prefix, established makers and model names.
- Put operating essentials first. Keep scientific attribution, limitations and
  authored gameplay assumptions in relevant help and guides rather than routine
  labels. Preserve research and historical release records. Review every English
  entry and current player document, including retained wording; maintain the
  language audit record and run localisation/reference checks with related edits.

- Owner direction (2026-09-25): `scripts/update-constants.py` is the standard
  maintenance route for versioning and frequently updated, appropriate constants
  across all Phobos Ostranauts mods. Use `config/maintained-constants.json` to
  register their authoritative copies and current summaries; do not maintain
  registered copies independently or repeat ad hoc search-and-replace edits.
- Preview with `--set Key=value`, apply with `--apply`, and verify with
  `--check --format json`. Use `--list` to discover supported fields and
  `--format json` for machine-readable reports. Follow `docs/development/updating-constants.md`,
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
  English fallbacks and Framework's shared lookup. See `docs/development/localization.md`.
- Use stable translation keys and complete messages with placeholders. Never
  translate native IDs, saved-state keys, command names or configuration keys,
  and never use translated status text to drive gameplay decisions.
- Name meaningful limits, unit conversions and tolerances; derive displayed
  capacities and yields from authoritative rules. Keep balance in the owning
  content mod and preserve historic recipe contracts. Do not generalize every
  layout coordinate or expose every constant as a setting.

## Changelogs and Steam Workshop publication records

- Workshop preparation uses `scripts/prepare-workshop.ps1 -Mod <name> -Build
  -Prepare`, `scripts/prepare-workshop.py --status` and `--verify <candidate>`.
  Follow `docs/development/workshop-upload-preparation.md`. These are offline-only tools;
  never add uploads to builds or CI. Maintain real item IDs, dependencies and
  publication holds in `config/workshop-publishing.json`; preserve unknown IDs
  as null. Preparation does not authorize Steam login, uploads or visibility changes.
- Owner request (2026-09-29): `scripts/upload-workshop.ps1` is the owner-run
  SteamCMD uploader (receipts, Private-only creation, blocker and duplicate-create
  guards, item-ID recording); `scripts/remove-local-mods.ps1` retires local copies
  before a Workshop subscription test. Agents maintain and test them with the fake
  SteamCMD in `tests/workshop-upload.tests.ps1` but never run a real upload, handle
  credentials or change visibility. Keep pages at or below 7,500 bytes, without
  ASCII double quotes or backslashes (SteamCMD VDF strings).

- Owner memorandum (2026-09-25), effective immediately for every Ostranauts mod:
  maintain `mods/<ModId>/CHANGELOG.md` and `workshop/<ModId>/page.bbcode` as part
  of each relevant change. This includes prototypes and scaffolds, whose drafts
  must clearly say they are held rather than ready for publication. Phobos Scope
  is a separate toolkit, not an Ostranauts Workshop mod.
- Record player-visible features, fixes, balance, dependencies, save/migration
  effects and known limits in the owning mod's changelog. Update its Workshop
  page draft whenever capabilities, requirements, controls, installation, support,
  links or limitations change. Keep this work in the same change/checkpoint;
  do not defer documentation to the eventual upload or manufacture release history.
- Main changelogs use the restricted Markdown format in
  `docs/development/workshop-publication.md`, with Unreleased plus dated Draft/Released version
  entries. Workshop page text uses Valve's Steam BBCode, not GitHub Markdown.
  Keep direct research/author attribution, licence limits and gameplay caveats.
- Generate `workshop/<ModId>/releases/<version>.bbcode` from each dated changelog
  entry with `scripts/workshop-release-notes.py`. Maintain one document per
  release/version in that separate folder; do not edit generated notes by hand.
  Draft exports are allowed for preparation but must remain explicitly unpublished.
  Mark Released only after actual publication is confirmed, with its real date.
- Use the constants updater for versions (including Workshop page version fields),
  then author the new changelog entry, review the page, regenerate release notes,
  and run `python scripts/workshop-release-notes.py --check --format json`.
  CI must reject missing records, stale current versions or stale generated notes.
  Extend the scripts, catalogue, checks and documentation when formats evolve.
- Publication records are mandatory maintenance, not authorization to upload,
  subscribe, send announcements or change Steam visibility. Follow
  `docs/development/workshop-publication.md` before publishing; preserve older releases and
  correct their source entries explicitly when necessary. Build success is not
  gameplay validation, and a page draft is not evidence that a Workshop item exists.

## Merchant stock quantities

- Owner memorandum (2026-09-26): stock all offered Phobos goods in substantially
  larger finite lots, especially piping and other high-consumption supplies.
  Follow `docs/development/merchant-stock.md`; use content-owned `StockQuantities` for both
  original and regional merchants. Keep probability separate from physical count.
- Maintain quantities through the registered `*.stock*` constants and regenerate
  the per-item economic references after changes. New retail offers must explicitly
  select an appropriate lot; the legacy single-unit API is compatibility support.
  Preserve native restocking, existing inventories, other providers and rare-world
  salvage contracts. Do not force stock refreshes or rewrite saves.

## Per-mod equipment and item references

- Documentation audit follow-up (2026-09-28): current operating guides must lead
  with player actions and match implemented controls. Keep introduction-version
  timelines in changelogs or clearly marked history; link maintained current
  versions/dependency minima rather than copying unregistered numbers. When a
  workflow changes, check every linked guide, maintained reference input and the
  reference generator's wording, not only the new feature page. Preserve original
  research and historical evidence, adding a current-guide pointer where needed.

- Owner memorandum (2026-09-25): maintain one end-user reference per Ostranauts
  mod covering every equipment family, gear item, loose commodity, byproduct and
  supported damaged form. Explain function, use, acquisition, where it appears,
  placement, base value, salvage, Repair and Restore. Explicitly label internal
  compartments, legacy identities, prototype inheritance and unimplemented designs.
- Run `scripts/update-item-reference.ps1` (or double-click its `.cmd` launcher)
  after each relevant update. It reuses the existing economy audits, exports live
  source definitions and regenerates all current references and their index. Read
  `docs/development/item-reference-maintenance.md`; `-Check` verifies against fresh native
  data without rewriting tracked files. The game path comes from the local
  installer settings or an explicit argument, never a committed machine path.
- Keep reviewed explanations in `config/item-reference.json`; keep numerical
  balance in its owning definitions and use the constants updater where registered.
  Do not hand-edit generated references or the data snapshot. Add new identities,
  acquisition labels and service inputs when coverage reports them. Review use
  instructions when behaviour changes; the generator must not invent them.
- Maintain the exporter, coverage tests, packaging links, owning changelogs and
  Workshop drafts with this workflow. Run `python scripts/update-item-reference.py
  --check` and `python -m unittest discover -s tests -p test_item_reference.py`.
  CI verifies saved evidence without proprietary game files; local native checks
  establish the snapshot. Updating documents does not install or publish mods.

## Native INSTALL catalogue maintenance

- Owner memorandum (2026-09-25): every implemented placeable Phobos furniture,
  equipment and machinery family must appear in an appropriate native INSTALL
  tab, including supported damaged forms. Maintain this alongside additions,
  renames, placement changes and removals; it is part of delivering the item.
- Use Framework `InstallMenu` category constants and native installables; preserve
  existing item IDs, physical inputs, footprints, work rates and saved state.
  Keep other providers' entries intact. Menu visibility is not free construction.
- Run the native catalogue coverage checks and maintain
  `docs/development/install-catalogue.md`, owning changelogs and Workshop page drafts.
  Record genuine exceptions (slot-only modules, non-placeable supplies and
  unimplemented scaffolds); do not invent floor fixtures for them.

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
  See `docs/development/equipment-branding.md` for assigned models and agricultural supply lines.
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
- Owner direction (2026-09-30): invent a new original brand and model family
  wherever an existing brand would become crowded and a separate line gives
  players a clearer identity (Fennmark already carries many Manufacturing lines).
  Siblings of an existing family keep that family's brand. Record each new brand,
  its etymology and models in `docs/development/equipment-branding.md`. Brand
  crowding is separate from machine count, which is a maintenance question.
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

- Owner-approved nutrient-production follow-up (2026-09-26): Agriculture 0.9.0,
  Framework 0.22.0 and Shipbreaker 0.20.0 implement recorded fresh crop residue,
  the Groundwork B2 workup bench, finite makeup formulation, selected nonrepairable
  W2 inventory charges and optional Ship’s Water 0.16.1 Recycler wet-reject capture.
  Follow `docs/agriculture-nutrient-production.md`. Preserve old crops/waste,
  measured receipts, exclusive collector inlet, finite physical outputs and
  explicit resume after reload. Wastewater nutrients remain uncharacterized and
  have no fertilizer recipe. Offline checks are not gameplay validation.

- Owner authorized all outstanding fluid extensions. Agriculture 0.6.0 / Framework
  0.20.0 provide up to eight W2 rack pairs, retained endpoint-owned line parcels,
  authored resistance/transit, recorded drainage treatment and finite rejects.
  Shipbreaker 0.17.0 adds optional finite coolant servicing with captured leaks.
  Follow `docs/fluid-network-operations.md`; retain legacy sealed F6 mode,
  original pair slot zero, old waste identities and component/heat conservation.
  In-game testing remains owner-run and is not a prerequisite for useful work.

- Nutrient-solution follow-up: Agriculture 0.5.0 / Framework 0.19.0 add finite
  potato/lettuce feed through the existing W2 and irrigation conduits. Follow
  `docs/agriculture-nutrient-solutions.md`: preserve schema-1 plain water/dry
  nutrients, additive solution records, measured two-component transfers,
  shared power budget and authored formulation labels. Reuse existing artwork.
  No potable return, chemical assay or gameplay validation is implied.

- Owner follow-up: research shared fluid pipes for plant sustenance (water and
  nutrients), taking after vanilla conduits in part. This is distinct from
  Agriculture cooling. Follow `docs/development/fluid-conduits-and-irrigation-research.md`:
  reusable transport belongs in Framework, equipment/biology in content mods;
  native conduit placement and sprite patterns are candidates for reuse, while
  fluid accounting stays separate from electricity. Water-first/local nutrient
  dosing was the first recommendation, not an exclusion of later nutrient pipes.
  The owner subsequently authorized implementation and necessary artwork.
  Agriculture 0.4.0 / Framework 0.18.0 prepare one W2 supply-to-rack water route,
  finite manual/provider inlet, local nutrients, independent pipe sockets and
  guarded receipts. Follow `docs/agriculture-water-conduits.md`. Receiving/pumping
  pauses on reload; legacy refill remains explicit. Multi-rack allocation remains
  future work. Later nutrient/coolant stages have separate contracts. No installation
  or gameplay validation is implied by prepared packages.

- Shipbreaker 0.16.0 / Framework 0.19.0 add optional F6-C sealed coolant conduits
  to a remote F6-R, using shared `NativeFluidRoute` and measured electricity.
  Follow `docs/furnace-coolant-conduits.md`: keep existing direct F6-R/F6-P pairs
  and thermal saves; mode changes require cool/empty machinery. One circuit has
  one furnace and one radiator, with distinct coolant/irrigation identities.
  The lumped loop retains heat in existing nodes, neglects pipe hold-up and
  requires incremental pump power. It is not a transferable water inventory or
  a fill/drain/leak model. Do not impose water on D4/R4 without a real process need.

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
  are authored gameplay balance. Follow `docs/development/agriculture-research.md`,
  `docs/development/agriculture-first-slice.md` and `docs/development/agriculture-roadmap.md`.
- The owner subsequently authorized implementation. Agriculture 0.2.0 supplies
  the Firstlight-4 rack, Hearth-2 portion cooker, potato/lettuce cohorts and finite manual inputs.
  Framework 0.17.0 adds equipment providers and measured liquid transfers;
  Shipbreaker 0.14.0 exposes agriculture through C1. The optional Ship's Water
  adapter is scoped to inspected 0.16.1, with manual fallback for other versions.
  Follow `docs/agriculture-player-guide.md` and `docs/development/agriculture-implementation.md`
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
  Follow `docs/development/agriculture-living-visuals.md` and `assets/phobos-agriculture/layers.json`.

- Owner direction (2026-10-04), crop expansion: more food crops, industrial crops and
  ties between Agriculture and the other mods, with the schema files used more widely.
  Owner choices: dwarf wheat, dwarf tomato and soybean first; biomass to carbon, sugar
  beet with a fermenter, flax and rubber dandelion as industrial chains; CO2 raising
  yield, transpiration water and a spirulina bioreactor as cross-mod ties; crops and
  cooker recipes as data packs before any new crop. Follow
  `docs/development/agriculture-crop-expansion.md` for the phases and sources.
  Agriculture 0.40.0 is phase 1: `framework/crops.json` (schema `crops`, owned by
  Agriculture, frozen by crop name in `frozen-crops.json`) and the Hearth-2 recipes in
  Agriculture's `process-recipes.json`. A crop is a data entry plus artwork: never
  branch on a crop id in code again. Player files may add a crop from the mod's own
  items and a shipped artwork family, never edit a shipped one. Later phases that add
  machines, brands, commodities or prices each open with a design record and owner
  decisions. Agriculture 0.41.0 adds dwarf wheat as the pilot crop (owner go, 4 October
  2026): seed wheat, grain and Hearth flatbread baked from grain and a water ration
  (the cooker recipe may take one supply beside its portion). The owner approved the
  wheat pilot the same day; Agriculture 0.42.0 adds dwarf tomato, with repeat picking
  (crops-pack `picks`/`pickKg`, saved `picks`; a pick sets growth back by exactly the
  mass it took), and soybean with Hearth soybean stew. Phase 2 is complete.
  Agriculture 0.43.0 (phase 3): crops grow faster per hour and per kWh in enriched air
  (crops-pack `co2Response`, outside the frozen entries; budgets per kilogram unchanged),
  and a full rack's spare condensate goes to a Framework water tank it reaches instead
  of vanishing. Owner decision the same day, residue to carbon **both ways**:
  Agriculture 0.44.0 adds the B2 straw press (residue records gain `organic`; a saved
  press accumulator dries straw into a tank and packs fixed 1 kg bales) and
  Manufacturing 0.37.0 burns a bale to CO2 (V4 revision 13) or chars four into carbon
  stock (revision 14), the char a recorded value exception. The owner then delegated
  phase 3b and phase 4 decisions while away; agent choices are marked as such in the
  crop expansion record. Agriculture 0.45.0 (phase 4): fibre flax, whose straw the B2
  scutches (a third workup mode) into the game's own clean scrap cloth, the shives
  going to the straw press; flax straw is unsold; no oil press until oil has a use.
  Owner decisions on phase 5 (same day): a Manufacturing fermenter-still under a new
  brand; bulk ethanol in its own tanks and line, bottled by a separate bottler into an
  own-brand spirit priced in the refining band; beets mashed directly or B2 sugar first;
  kiosk buy-back of ethanol only; a damaged ethanol tank or line spills to its bund and
  can catch the game's fire, the working still being an ignition source; rubber
  dandelion set aside. Agriculture 0.46.0 (set A): sugar beet and B2 sugar, through
  `BenchConversions` (one-item B2 jobs as data rows); beets and sugar unsold.
  Framework 0.80.0 with Manufacturing 0.38.0 (set B): the **Alembrine** brand (owner
  choice of name), Cask-2 to Cask-4 ethanol tanks and the ethanol line. `LiquidFamily`
  takes a mist or a fuel, and the acid line became `LiquidLine` with one rules row per
  liquid. Ethanol has its own lane and port in Framework; on screen it shares the
  coolant lane. A damaged cask or segment can burn through the shared
  `Combustion.Burn` and the existing deflagrations. Set C (fermenter-still, bottler,
  spirit) is next. Manufacturing 0.39.0 adds the Copperhead-3 fermenter-still, a
  charge machine (`machine: fermenter`, requirement `agriculture-sugar-crops`). It
  takes six beets as mash or six sugar packets as a wash, 92% of the sucrose
  ferments, and it is an ignition source while working. Manufacturing 0.40.0 adds the
  Corker-2 bottling unit as its own service (the charge engine binds items): a cask and
  a silo give seven 35 g servings of Alembrine spirit a batch in one settlement. The
  spirit clones the game's `LiquidVodka` (keeping `IsLiquor`, so the game's own drinking
  applies; dropping `IsBismertnaya`) at 8 cr a serving. Phase 5 is complete.

## Manufacturing direction (2026-09-25)

- The owner approved a separate **Phobos Manufacturing** content mod for dedicated
  machining and finished components, beginning with research for one enclosed
  milling machine/machining centre and heat-sink finishing. Follow
  `docs/development/manufacturing-handover.md` in its separate task. Mod creation and research
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
  `docs/development/manufacturing-research.md` and `docs/development/manufacturing-implementation.md`.
  The proposed M4 enclosed mill, two-sink preform and finite machining cartridge
  remain unregistered designs. The scaffold has no operational machinery and
  is not installed. Keep the optional F6 recipe separate from historic housings;
  follow `assets/phobos-manufacturing/README.md` for layered artwork planning.
- Owner direction (2026-09-29): Manufacturing begins with a refinery and a
  chemical processor, taking inspiration only (no code, data or art) from
  Rusty150's Salvage Workshop and Crafting Framework. Manufacturing 0.1.0 with
  Framework 0.41.0 ships the **Fennmark** V4 Volatiles Refinery (4 x 4, one
  bound charge at a time: hydrates, the new clay hydrates chunk, carbon ore,
  meteoric iron, and with Shipbreaker 0.38.0+ four nickel-iron ingots plus one
  carbon stock into its steel ingots), the X2 Chemical Processor (2 x 2 water
  electrolysis, 1.125 kg water + 6 kWh into 1.000 kg O2 and 0.125 kg H2 per
  hour) and the H2 Hydrogen Store (24 kg Framework bulk vessel, Leak policy).
  Follow `docs/development/manufacturing-refinery-and-chemistry.md` and
  `docs/manufacturing-player-guide.md`. Machines are purchase-only; ores and
  chunks are mined, never sold; the refuelling kiosk's Bulk supplies view is
  the only commodity purchase route and sells nothing new this round. Water
  comes from any registered Framework water vessel (S3 or R3) within one tile;
  Manufacturing requires Framework only. The V4 and X2 use the Phobos batch
  pattern (explicit Start, repeat while supplied, pause after reload). Oxygen
  fills a linked native O2 canister to its rated pressure through Framework
  `NativeGasCanister`, or the cabin when none is linked. The M4 mill remains a
  design; nickel-iron joins ingots as its stock. Owner gameplay checks pending.
- Owner direction (2026-09-29): Manufacturing equipment is mid-to-late-game and
  rather expensive. Anchor its prices, work, Restore and component repair bills
  on vanilla's late-game kit (IC fusion reactor, radars, heavy lift rotor),
  carry the game's `IsSalvageValueHigh` mark on every form and keep world finds
  rare (Manufacturing 0.1.1: V4 $64,000, X2 $38,000, H2 $22,000, 5% engineering
  finds). Keep stock lots and availability floors as the stock memoranda set
  them; price is the gate. Only machinery is late-game priced (owner correction,
  same day): ingots, carbon, ore, reagents and remainders keep ordinary
  raw-material prices; see `docs/equipment-economy.md` and the refinery record.
  Their old ceiling (below the vanilla ore they come from, so every charge lost
  value) came from the refining value-loss rule the owner retired on 2026-09-30;
  see "Refining value" below.
- Owner decisions (2026-09-29), Manufacturing 0.2.0: the Sabatier stage is a
  separate Fennmark K2 reactor (not an X2 mode), and its methane goes to a new
  Fennmark M2 methane store rather than overboard. This is an explicit owner
  exception to "add a bulk commodity only with a concrete consumer"; do not treat
  it as permission for other consumer-less commodities. Methane is a native gas:
  the store leaks it into the room and a burn leaves CO2. Keep the reactor's saved
  reactant/product holds and its one-step conversion (amended 5 October 2026 for the
  carbon monoxide mode: see the regolith programme entry); follow the refinery record.
- Owner request (2026-09-29), Framework 0.42.0 / Manufacturing 0.3.0: RCS thrusters
  burn bulk gases at their real cold-gas worth, and native O2/CO2 canisters are
  corrected too. Framework `Propulsion.RcsPropellant` serves the engine's RCS gas
  loops in nitrogen-equivalent kilograms (nitrogen-only ships unchanged; shallow ships
  and refuelling stay native) and takes registered `IRcsPropellantFeed`s; bulk-vessel
  draws go through `BufferedDrains`. Manufacturing's P1 manifold (switchable, draw
  order, up to four stores, all off by default) and its own propellant-line family
  are the first feed. Record further RCS changes in the vanilla-precedence audit.
- Owner direction (2026-09-29), Framework 0.44.0 / Manufacturing 0.4.0: every bulk
  chemical or reagent family offers small, medium and large sizes, retroactively and
  for future families, unless the commodity is niche or high-value. Use Framework
  `Liquids.BulkVesselSizes` (one tile wider per step; small keeps its IDs and records).
  Medium and large sizes are purchase-only, never salvage loot. The owner reversed
  "no fuel or gas silos": bulk O2, N2 and CO2 stores exist because the game's only
  canisters are O2/N2/CO2, sold through Bulk supplies at the game's own gas price.
  The Fennmark L2 fills native canisters and suit bottles to 99% of their rating
  (`NativeGasVessel`, `GasTransfers`), never patching the native air pump. Shipbreaker
  0.40.0 adds the S4/S5 silos; Agriculture 0.20.0 the R4/R5 reservoirs, and a W2 now
  draws from any water vessel within one tile (`BulkVessels.Adjacent`). Manufacturing
  0.5.0 adds the Fennmark A2 Cabin Air Regulator: from linked O2/N2 stores it holds its
  room's oxygen set point, then pressure (Dalton's law on the room's own moles), adds
  gas only through `RoomGas`, stops below 10 kPa, caps oxygen at 30% and keeps working
  after reload like the native air pump. The L2's right-click crew order Keep suit bottles
  charged (`FillerCrewProvider`, Framework `CrewLogistics`) hauls loose bottles below 90%
  into the rack and starts it; never from suits, hands, locked containers or another L2.

## War Has Been Declared direction (2026-09-29)

- Owner request: a separate mod, **Phobos' War Has Been Declared** (`PhobosWarDeclared`,
  Framework only), puts the game's own construction placeholders back where
  installed parts were destroyed on the player's ships during combat, vanilla and
  modded alike, so post-battle repair is not tile-by-tile re-placement. Owner
  decisions: automatic combat detection plus a manual Battle stations / Stand down
  order (the game has no combat mode); native build sites built by crew with real
  parts, never free materials; a limited default because placeholders block
  pathfinding, and an all-inclusive option for players who accept that risk.
- Owner follow-up the same day: what is replaced is decided by player-editable
  **schematic files** (shipped `safe`, `safe-walls`, `everything`, `hull-only`; player files in
  `BepInEx/config/PhobosWarDeclared/schematics` override by name). Keep the format
  strict (unknown fields refused), first-match rules and the documented fields;
  extend it with tests and the player guide together.
- Framework 0.43.0 owns `Construction.NativePlaceholders` (rebuild target through
  damage chains, overlays and uninstall-to-install state variants; footprint; the
  save-load-path lay) and `Observations.NativeCombat` (read-only facts). Content
  owns the battle window, ledger, schematics and orders. Never block native
  destruction or mode switches; capture only damage-driven switches of installed
  parts on player-owned ships. Walk-through placeholders are deferred research
  (matched tile-condition counts across save reload). Follow
  `docs/war-declared-player-guide.md` and `docs/development/war-declared-design.md`.
  Owner gameplay checks remain pending.

## Working style

- Keep this a practical, small-team project. Prefer a working slice over a
  speculative framework or extensive process.
- Owner direction (2026-09-30): aim for maximum creative freedom. Risks such as
  a mod becoming unsafe to remove from a save (for example saved asteroids naming
  a removed Phobos cluster) are engineering problems to explore later, such as
  Framework fallbacks or existing-save spawners. Report them honestly, but never
  use them to drop, defer or shrink a creative design.
- Use observed progress to plan rounds; do not invent hour estimates.
- Owner direction (2026-09-30): the asteroid feedstock programme is paused after
  Manufacturing 0.10.0 for a schema separation audit. Read
  `docs/development/schema-separation-audit.md` (findings, recommendation and the
  decisions it asks for) and `docs/development/asteroid-feedstock-programme-status.md`
  (what was delivered and what each later stage still holds) before adding
  machines, recipes, economy rows or loot shares; the format they are written in
  is being decided.
- Owner decisions (2026-09-30) on that audit, all its recommendations approved:
  authored tables move into Framework **data packs** (`mods/<Mod>/framework/<schema>.json`,
  embedded and shipped, loaded by one Framework loader with a validator per
  schema) with player overrides in `BepInEx/config/<Mod>/<schema>/*.json` merged by
  entry key (tune or add, never rename or remove a shipped id); published recipe
  revisions are frozen by content hash so a change must add a revision; **mass
  conservation and native gas species are enforced on every file, shipped or
  player; stoichiometric honesty and pricing remain authoring rules for shipped
  data only**; identifiers, record keys, ports and native mirrors stay in code;
  guidance tunables and UI theme are not externalised; Manufacturing is the pilot
  and the feedstock programme waits for the loader and the recipe schema. Verify
  each migration by the golden export (`docs/item-reference-data.json` unchanged).
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
  Owner decision (2026-09-29): Auto Nav's Workshop publication hold is lifted; keep
  the Gravy / mrkmg credit, notices and MIT exclusion on `Adapted/` files unless
  they are independently rewritten or the author's terms are recorded.
- Research industrial ideas as the owner encounters relevant gameplay and can
  test them. Current priority: powered shipbreaking, onboard processing first,
  external cutting and its positioning/autopilot needs later. See
  `docs/development/fusion-industry-roadmap.md` and `docs/development/powered-shipbreaking-research.md`.
- The owner expanded industrial research on 2026-09-24 to shredders, material
  recyclers and asteroid feedstocks, including new ore types that replenish life
  support. Use native tethered asteroid mining as the acquisition baseline; do
  not assume purchased ore or add a second mining system. Reuse native water ice,
  methane ice, hydrates and carbon-bearing ore first. Investigate new nitrogen-
  and phosphate/salt-bearing feeds where they fill a concrete endurance gap.
  Keep proposed assays/yields distinct from native evidence, preserve existing
  residue and saved-job meaning, and account for every product and remainder.
  See `docs/development/shipbreaking-material-processing-research.md` and
  `docs/development/asteroid-life-support-research.md`. The combined scrap reclaimer is now
  implemented as described below; ore and life-support processing remain research,
  not implemented features or verified integrations.
- The owner selected a furnace as Shipbreaker's centrepiece with a tactile
  reactor-like control panel (2026-09-24), initially requesting direct fusion.
  On 2026-09-25 the owner explicitly approved **electrical heating as the first
  route**. Follow `docs/development/furnace-electrical-direction.md`; this supersedes the
  direct-fusion-first constraint. Reuse native received-electricity accounting
  and remove the reactor-side heat coupler requirement. Follow
  `docs/development/fusion-smelter-research.md`: meaningful live gauges, bounded process
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
  guarded-toggle/digit/slider adapters. See `docs/development/furnace-connections-and-instruments.md`.
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
  The 2026-09-25 follow-up is `docs/development/furnace-first-cycle.md`: proposed 20 kg
  aluminium housing batch, optional D4/R4 construction use, finite radiator and
  gas receiver; its original direct-fusion source section is superseded. The owner explicitly
  prefers vanilla UI reuse: follow `docs/development/furnace-ui-and-art.md`, first isolated
  widgets, then native artwork with adapters, original UI art only for gaps.
  Keep game-derived material local and reference native assets at runtime;
  the browser layouts and offline calculations are not Unity/gameplay validation.
- Future industrial chemical storage is documented in
  `docs/development/chemical-storage-and-process-fluids.md` (owner direction, 2026-09-24).
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
- Follow `docs/development/residue-material-contract.md` and `docs/scrap-reclaimer.md` for
  the implemented 0.8.0 chain. Legacy 13 kg residue remains unclassified, and
  started revision-1 wall jobs keep their exact outputs. Fresh revision-2 wall
  jobs produce identified 13 kg feed; the combined 4 x 4 reclaimer returns
  3 kg steel + 1 kg aluminium + 9 kg terminal rejects. These are authored
  budgets, not native chemical assays. Never reroll rejects or convert old cargo.
  Framework now owns shared immutable recipes, saved-job binding and mass checks;
  content owns native keys, identities and balance. Default reclaimer operation
  is 120 seconds / 12 kW, delivering heat into native room gas. Require enough
  atmosphere/thermal headroom; vacuum is not free cooling. Owner decision (2026-10-03):
  **Phobos machines do not work in the vacuum of space**, and say so plainly (Framework
  `RoomHeat.Check`/`Describe`, one shared message for every machine); casing radiation
  was considered and declined. The F6-R/F6-P radiators keep their own model.
  Owner decision (2026-10-05), for game balance: **machines give off a quarter of their
  heat**, as a player setting (Framework 0.94.0 `RoomHeat.MachineHeatScale`, BepInEx
  Heat/MachineHeatScale, 0.05 to 1, default 0.25). It covers heat into the room
  (electrical and reaction heat) and the mining laser's heat into a cooling assembly;
  the furnace's own melt heat is unchanged and fires and deflagrations stay unscaled
  (`RoomHeat.DepositHazard`). New machines put heat in only through `RoomHeat.Check` and
  `Deposit`, or `RoomHeat.Machine` where they warm their air directly, and show the
  scaled figure on their panels. Use explicit
  paired output collectors. Version 0.9.0 adds automatic reclaimer feed from
  fixtures or collector buffers, independent input/output pairs and saved exact-ID
  filters through Framework. Follow `docs/automatic-material-routing.md`. Keep
  receiving and processing permissions separate (running belt routes resume after
  reload since Shipbreaker 0.56.0; processing stays paused). Full
  destinations retain cargo at the sender; no virtual inventories or silent disposal.
  Preserve shared staged delivery and the native powered-job pause on reload.
- Prefer extending existing mods over duplicating their systems. Steam Workshop
  dependencies are welcome. Refresh the inventory when it matters; see
  `docs/development/mod-extension-survey.md`. Use permissive mod licensing as the owner's
  working assumption unless restrictions are explicitly stated; record verified
  terms separately, follow them and preserve attribution. Game assets remain
  subject to the game/repository boundaries below.
- Plan for dependencies that remain incompatible or unavailable without treating
  release age alone as failure. Follow `docs/development/dependency-contingencies.md`: prefer
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
  Default selection is AutoNav and Shipbreaker. Approach Assist is retired;
  do not restore its folders or installer entries without owner direction. Keep
  the game-closed guard and leave gameplay tests to the owner.
  See `docs/installing-mods.md`; do not repeat manual file-copy/load-order edits.

## Architecture

- Autonomous reclamation follow-up (2026-09-26): the owner selected temporary
  native capture/mooring, release and repositioning as the first arrangement.
  A finite deployed head may later enhance this foundation. Read
  `docs/development/shipbreaker-capture.md` and `docs/development/shipbreaker-close-work-geometry.md`:
  native deck tiles and navigation collision radii are separate scales. Never
  suppress collisions, expand G4 reach invisibly or turn the grabber into a port.
- Shipbreaker 0.22.0 / Auto Nav 0.16.0 prepare exact-G4 native capture/release for
  owned, unoccupied targets, an aligned G4/chute/D4 and existing N1/N2 hardware.
  Auto Nav is now mandatory in loader checks, build packages and the installer.
  All participant addresses use full native IDs; labels disambiguate prefixes.
  Unknown records and ambiguous native commits remain intact. Reload requires
  explicit Resume, and Stop does not brake or detach. Offline checks do not
  establish gameplay readiness; owner evaluation remains separate.
- Auto Nav owns flight exclusively, Shipbreaker owns capture and later mission
  acquisition/coordination, and Framework supplies existing concrete shared
  services. This capture candidate does not yet cut, transfer target objects,
  reposition automatically or authorize repeated furnace operation. Continue the
  stages in `docs/development/shipbreaker-autopilot-handover.md`; the owner has authorized
  acquisition and processing implementation. Preserve finite buffers, hot jobs,
  material/heat budgets and the current furnace motion guard until a tested
  policy replaces it. Unsupported cargo is retained, never deleted to declare
  completion. Manual valuables recovery remains the player's preparatory step.

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
  position control remain separate future features. See `docs/development/auto-navigate-adaptation.md`.
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
  instruments. Follow `docs/development/sensor-integration-research.md`. Measurements need a
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
  See `docs/development/shared-console-observations.md`. Panel/F3 reads use the same checked
  console boundary; native output witnesses require a fresh evaluation after
  reload, and dock-inclusive ambiguous sampling fails closed. Retain historic
  values only as historic, never erase physical heat when an instrument fails,
  and do not infer that an unrelated alarm caused a machine to stop. Furnace
  and specialist instrumentation remain future work.
- Owner sensor-engagement direction (2026-09-28), superseding earlier "Auto Nav
  never switches sensors" rules: Auto Nav may switch on the fewest fitted sensors
  needed to keep its target and nearby hazards tracked. Use non-emitting sensors
  first, radar/LiDAR only when those cannot help, and nothing when no sensor would.
  Never claim sensors the player already has on. Warn every time through Framework
  `PlayerNotices` (native nav-map banner while that station is open, crew log) and
  keep a steady Polaris hub line. When the work ends, not while suspended, switch
  off only Auto Nav's own sensors through Framework `SensorLeases` notes on the
  sensor units. The player's switch always wins, and a player switch-off holds for
  that operation. Displays and planning queries never switch sensors. Players choose
  `Sensors.AutoEngage` All/Passive/Off. Auto Nav 0.24.0 / Framework 0.34.0 implement
  this; see `docs/auto-nav-sensors.md`. Owner gameplay checks remain pending.
- Owner crew-study direction (2026-09-28): vanilla behaviour is the reference.
  Crew study at terminals through the game's own study chain, and Phobos
  specialities join that chain (openers the idle AI can pick, chooser, continuation
  gated on our studying mark, refusal once skilled, time-skip tick, credit per
  completed step) instead of bespoke actions. Framework 0.35.0 implements this;
  see `docs/crew-automation.md`. Never republish a native definition by name after
  loading (the game keeps private state on it); amend lists in place through
  `Registration.DefinitionAmendments`. Standing orders announce one native task
  per Enable and keep later re-adds quiet, because the game interrupts every
  on-shift crew member's study when its task total rises. Failed steps back off
  (30 s to 10 min) and keep the order. AI-history seeding copies the vanilla
  construction-study entries only where absent; never overwrite or remove
  learned history. Keep `phobosframework crew` read-only.
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
  rules. See `docs/development/auto-nav-panel-layout-audit.md` for the vanilla measurements.
- Use C# extensions for behaviour the native data system cannot express cleanly.
- UI code presents state and delegates actions; gameplay services own mutations.
- Extract shared code when concrete features establish a shared need. Avoid
  duplicated business logic and premature generalisation.
- Owner standing order (2026-09-29): make use of Phobos Framework wherever it is
  appropriate, now and as the Ostranauts mods grow. When a change touches a
  native system or a rule that another current or plausible Phobos mod would
  also need (reactor state and guarded controls, notices, observations,
  persistence, transfers, instruments), put the concrete service in Framework
  in the same change as its first consumer; keep policy, balance and art in the
  content mod; record the Framework version and dependency minimum through the
  constants catalogue. One real consumer plus a plausible second is enough;
  this refines the rule above rather than replacing it. First applied:
  Framework 0.40.0 `Processing.NativeReactor` for Auto Nav 0.27.0.
- The owner selected our own shareable Ostranauts framework **instead of OCF**
  on 2026-09-24, explicitly correcting an earlier misuse of "in lieu of".
  Follow `docs/development/phobos-framework.md`: reusable services belong in Phobos Framework;
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
  Shipbreaker's collector is one paired route, not a general conveyor network (a floor
  route until Shipbreaker 0.56.0; now a conveyor belt or touching equipment).
  Framework/Shipbreaker 0.6.0 and Auto Nav 0.2.0 add ordinary merchant acquisition,
  maintenance, tool requirements and save-compatible economy upgrades. Reuse the
  shared additive stock and native maintenance helpers; keep balance in content
  mods. Follow `docs/equipment-economy.md` and protect cargo from dismantling.
- Owner direction (2026-10-03): **what follows a repair or Restore is the game's own
  result.** A repair uses up its parts and returns only the repaired item; Restore
  removes wear and leaves nothing. This supersedes the earlier rule that repairs
  retain their consumed mass as Spent Service Parts. Framework 0.74.0 removed that
  hook (`MaintenanceDefinitions.Repair`), and saved spent parts are removed as each
  ship loads (`LegacyItemConversions.Retire`, owner request the same day, with a
  crew-log line). Never reintroduce repair byproducts.
- Owner direction (2026-10-04): **floor machines take power from the tile row directly
  behind their back edge** (`ApplianceDefinitions.WallRowY`), the wall row where ships
  carry conduit, as the game's own nav station, EVA charger and battery do; never inside
  the machine's own tiles. `PowerPointNativeChecks` holds every powered Phobos machine to
  it; the exceptions are explicit (F6 front points, G4/ML-2 hull mounts on the hull side,
  the C2 set into the wall).
- Owner decision (2026-10-05): **work that was running when the game was saved carries on
  after a reload.** This supersedes the pause-on-reload rule for processing machines in
  every mod, the ML-2 laser, G4 reclamation missions and Auto Nav docking approaches, as
  the 30 September decision did for belt routes. Still explicit: the F6 furnace's hot
  batch and repeat runs, Auto Nav Rendezvous and Follow, a bare G4 capture approach, the
  recycler wet-reject capture, and fire authority (never saved). Framework 0.95.0
  `Persistence.ResumeAfterLoad` keeps one hidden saved condition on the machine while the
  player's Start stands (`Mark`, or `Sync` each step; `Flags` for a machine with more
  than one thing to carry on) and offers each marked machine once per load (`Due`), when
  loading has finished and its ship is loaded. The owner then starts it through its own
  checks, never by replaying work, and a failed check leaves it stopped with the reason.
  A crew-access check is not part of a resume. Players turn it off with
  Persistence/ResumeAfterLoad; Auto Nav keeps its own setting. New machines use this
  from their first version; older "pauses on reload" wording elsewhere in this file is
  history where it names one of the machines above.
- Owner report (2026-10-05), Framework 0.96.0: **measure reach as the game does, and never
  refuse silently.** The game judges an interaction's range tile to tile; `CrewWork.LocalAccess`
  measured from the crew member's exact position, could come out a tile further, and the panel
  then returned false without a word. Use `CrewWork.Reach` (tile to tile) for any crew-to-machine
  distance, keep our access range no tighter than the action's `fTargetPointRange`, and tell the
  crew member why a panel did not open (`ProviderPanel.Refused`).
- Prefix new game identifiers with `Phobos` and keep them stable once saved games
  can contain them. Document migrations for incompatible changes.
- Distinguish observed engine behaviour from proposed designs and untested assumptions.
- Build machinery at its intended physical footprint from the first usable
  implementation, and **size every inventory to its job** (owner direction,
  2026-10-01, after finding inventories far too large across the board): a product
  tray holds about one to two batches of the machine's largest recipe; a vessel whose
  contents are a record in kilograms gets a small service rack; equipment that stores
  nothing has no inventory; only declared storage (the Rivetline Y bins) is sized as
  storage. Declare the role through Framework `InventorySpec` / `EquipmentInventory`
  (0.70.0); the native checks refuse a container without a role and list the families
  still at the old general 8 x 8 grid. Shrinking or removing a grid needs no migration
  entry: Framework `ContainerFit` re-packs saved contents on load and puts what no
  longer fits on the deck with a crew-log notice. Equipment that had an inventory in a
  save and loses it keeps a hidden `LegacyReceptacle`, never a null container (the
  game leaves the saved contents of a removed container unattached). Shipbreaker's D4
  is 4 x 4 tiles with one active panel, a four-panel feed and a 4 x 3 product tray.
  Products are delivered into stacks through Framework `TrayDelivery` (0.71.0), never
  one cell each; `TrayFitNativeChecks` proves every tray against every real recipe at
  the game's own stack limits, so a new recipe that outgrows its tray fails there.
- Owner report (2026-10-04): **the game shows one inventory for an object.** A hidden
  feed compartment cannot be loaded by hand, so a started machine takes feed from its
  own inventory (Framework 0.83.0 `OwnInventoryFeed`; Shipbreaker 0.72.0 T2, D4, R4;
  Manufacturing 0.41.0 charge machines, which never take their own products except by
  Start) and Cancel puts it back. Never tell a player to use a second inventory
  window, and never design one. Every panel choice must be listed by its provider's
  `IsConfiguration`, or Apply refuses it for ever (`EquipmentField.Unlisted`).
  Owner request the same day, **optional for the player**: a machine may name one feed
  store (a material bin or any ordinary store, touching or on a conveyor belt) and take
  its feed from there while started and powered (Framework 0.85.0 `StoreFeed`;
  Shipbreaker 0.73.0 T2; Manufacturing 0.42.0 charge machines). Hand loading always
  stays; never make a belt or bin a requirement.
- Owner rule (2026-10-04): **no trash-like objects left to pile up in numbers.** Every
  remainder needs a consumer before the thing that makes it, even a game-like one. The
  consumer is Manufacturing 0.43.0's Slingwright RM-1 Reaction Mass Feeder, which grinds
  declared remainders into RCS reaction mass (a second `IRcsPropellantFeed`, worth 1,
  0.15 kWh/kg, authored). Declare every new terminal remainder through Framework 0.87.0
  `Registration.Remainders` (retained maintenance waste is declared automatically) and
  deliver it as one stack. `RemainderNativeChecks` refuses a Phobos trash-category item
  that is neither declared nor listed there with the recipe that takes it. The feeder
  lives in Manufacturing (owner choice), so other mods' remainders need it installed.
- Owner direction (2026-10-04): **chance outcomes are data.** A charge that can turn out
  more than one way has one exact, frozen recipe per outcome and a table in the owner's
  `outcomes` data pack (Framework 0.88.0 `Data.OutcomePack`; player overrides in
  `BepInEx/config/<Mod>/outcomes`). Every outcome has its base's machine, inputs,
  circulating volumes and duration. The pick is `Outcomes.Pick`, a stable hash of the bound
  unit ids, applied at bind and saved as the charge's revision: never roll at runtime,
  never hard-code a table, never reroll. Value is judged over the table's expected
  return (at most 1.5 x cost, feed no merchant sells). First use: Manufacturing 0.44.0's
  gangue wash on the LC-3 (leach revisions 7 to 10; washed tailings are a declared
  remainder). Game-like recipes are allowed where the owner says so and are labelled.
- Owner direction (2026-10-04): **players may publish add-ons on Steam Workshop.** An
  add-on is a data-only game mod folder with `phobos-addon.json` and override files
  under `phobos/<Mod>/<schema>/` (Framework 0.90.0 `Data.AddOns`). Order: shipped pack,
  add-ons in the game's mod order, the player's local files; any file may set a
  `priority` header. Add-ons tune anything and add only under their own id prefix;
  every check on player files applies to them. Keep `docs/publishing-an-add-on.md`,
  `examples/addons` (loaded by tests) and `scripts/validate-data-packs.py --addon` working
  whenever a schema changes; a new schema loaded through `DataPacks.Load` supports
  add-ons by itself. Framework 0.91.0 adds text from add-ons
  (`phobos/translations/<Mod>/<language>.json`; new keys only under the add-on's prefix).
  Framework 0.92.0 with Manufacturing 0.45.0 lets files add materials with their own
  name and picture (`MaterialContext.AllowAdditions`); added trash must be `terminal`, which declares
  it a remainder. Shipbreaker 0.75.0 (kind `stock`, cast by an added F6 recipe) and
  Agriculture 0.48.0 (`stock`, `food`, `waste`; a crop item entry for an added
  material has no `text`) take them too. Keep both worked examples under
  `examples/addons` loading in the unit, native and Python checks.
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
  design work. Follow `docs/development/industrial-control-console.md` and its text mockups;
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

- Owner direction (2026-09-27), Polaris interface refresh: existing interface
  artwork is a starting point, not an unconditional preservation constraint.
  Replacements are authorized where suitability is MEDIUM-HIGH or HIGH for
  readability, control distinction or reusable layout. Prefer suitable vanilla
  widgets/artwork at runtime; match vanilla's utilitarian instrument style.
  Assess and record the unmet need before generating one pilot. Keep text and
  states live, preserve prior masters and follow provenance/rejected-art policies.
  This supersedes earlier absolute faceplate-preservation wording for Polaris
  interfaces only; it does not authorize unrelated art replacements.

- Owner clarification (2026-09-27): smaller Polaris button/help text and compact
  buttons are welcome where readable, following vanilla's information density.
  Keep important state prominent. Bound scrolling content inside visible frames
  with clear scrollbars; keep tabs and emergency actions fixed. Do not retain
  obsolete painted dividers that contradict the live layout. Browser geometry
  checks must include actual frame/viewport boundaries, not merely the panel edge.


- Owner direction (2026-09-27): keep rejected/unselected production attempts on
  `codex/rejected-artwork`, not in the current `main` tree. Preserve and verify
  the archive branch before removing originals from main; retain exact requests,
  rejection reasons, hashes and archive links on main. Maintain
  `assets/rejected-artwork-archive.json` and its README. Never remove selected
  masters, generation inputs needed by accepted art, or runtime exports merely
  because their filenames contain "rejects". Do not rewrite Git history.

- Owner direction (2026-09-27): **overhead-first PixelLab requests** for every
  world/inventory sprite. Follow the explicit prompt prefix and inspection rule
  in `docs/development/asset-generation-policy.md`: vertical orthographic camera directly
  above, top surfaces only, axis-aligned rectangular edges; set `view="high
  top-down"` and `isometric=false` wherever supported. Describe overhead surfaces,
  not standing product views. Prefer a verified overhead original Phobos reference
  for variants. Inspect one pilot before expanding a family; reject visible
  vertical side/front faces and diamond projection. These weak provider settings
  reduce ambiguity but do not guarantee correct output. UI faceplates are exempt
  from the world-camera rule. Preserve prompts, rejected attempts and review evidence.
  Owner ruling (2026-09-29): world sprites carry no painted live-state instruments
  such as level gauges, which are contrary to the vanilla art style; readings stay
  live text on panels and consoles.

- Owner follow-up: apply the quiet cue where appropriate across the suite.
  Framework 0.21.0 owns one native-effects player, shared volume/mute and a
  three-real-second burst limit. Shipbreaker 0.19.0 watches D4/R4 batches;
  Agriculture 0.8.0 watches meal delivery or whole-cohort readiness; Auto Nav
  0.14.0 watches finite Approach/Rendezvous arrival. Explicit watches clear on
  stop, fault/suspension and reload. No docking/weapon/ambient/transfer pings or
  ambiguous furnace-success signal. Follow `docs/shared-completion-cues.md`.
  Prepared builds are not listening/gameplay validation.

- Owner direction (2026-09-25): retain PixelLab talking-portrait/mouth-shape
  animation as a possible future tool. It produces visual frames/timing, not
  spoken audio; do not assume it supplies sound or that Ostranauts can consume
  a GIF directly. Follow `docs/development/animation-and-sound-direction.md` and the existing
  layered-art/provenance policies. No animation generation is requested now.
- The same direction supports considering retroactive ChatGPT-assisted sound
  synthesis only where its gameplay usefulness is **HIGH or MEDIUM-HIGH**.
  Samples must be brief, quiet, non-startling, non-alarming and unobtrusive;
  divert attention from other game elements only for an exceptionally strong
  reason. Do not interpret this as permission for an alarm suite, ambient loops
  or sounds on every action. Follow `docs/development/animation-and-sound-direction.md`:
  distinguish owner requirements from proposed defaults, assess native feedback
  first, and retain visual information. Candidate ratings are design judgments,
  not measured player benefits. The authorized first trial is Shipbreaker 0.18.0's
  optional one-shot watched D4/R4 batch cue: original procedural audio, native
  effects mixing, volume/mute, transient watches and burst suppression. Follow
  `docs/development/shipbreaker-completion-cue.md`; listening/gameplay review remains pending.

- Owner memoranda (2026-09-25): prefer PixelLab for simpler pixel-art assets,
  and explicitly permit ChatGPT-generated high-resolution equipment/furniture
  bases with separate PixelLab plant, appliance, workpiece and state layers.
  This complementary workflow applies across all Ostranauts mods, including
  Manufacturing. Match final projection, palette and pixel density; use stable
  pivots/attachment positions, retained masters and deterministic native-size
  exports. Keep runtime lighting, visibility, rotation and damage coherent.
  Authoring layers need not be separate gameplay objects. Follow the Agriculture
  layering precedent and `docs/development/asset-generation-policy.md`; no mandatory use of
  both providers, wholesale art migration or immediate generation is implied.
  This explicit preference supersedes generic imagegen-skill provider defaults.
- Follow `docs/development/asset-generation-policy.md`, incorporating the inspected Codename
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
  production approach in `docs/development/furnace-ui-and-art.md`: compose panels from
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
  same native 25% x 20% placement bounds. See `docs/development/auto-nav-instruments.md`.
  Preserve captured flight settings and command authority in the service;
  display reads must not advance physics or rewrite saves. AUTO is preference,
  not an assertion of torch clearance. The new design awaits owner evaluation.

- Owner's resolution memorandum (2026-09-24): all newly created or visually
  revised artwork uses at least 2x its intended display dimensions, or 4x for
  very small artwork. Working convention: use 4x when the intended short side
  is 32 pixels or less. These are per-axis multipliers, not increased physical
  footprints. Preserve larger original masters. Keep pixel art crisp with
  integer scaling and nearest-neighbour sampling; enlargement alone adds no
  detail. See `docs/development/artwork-resolution-policy.md` for examples and export rules.
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
  `docs/development/ship-equipment-art-study.md` (owner-requested equipment study, 2026-09-23).
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
  filters on 2026-09-24. See `docs/development/material-disposal-port-research.md`. Native
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
  item (owner clarification, 2026-09-24). This covers breaking whole equipment or
  parts into materials; refining raw materials is no longer held to a loss (see
  "Refining value", 2026-09-30). Audit actual native material prices,
  damaged definitions and wear tiers; show combined output value alongside whole
  value rather than presenting material counts alone. Distinguish definition
  value from merchant quotes and do not claim every regional market is identical.
  Use `docs/development/equipment-value-audit.md` and `docs/development/vanilla-economy-audit.md`; preserve
  mass without assuming it implies conservation of monetary value.
- For new machinery, check our material accounting, progress, interruption and
  persistence where we introduce or change that behaviour. Cover power or
  fast-forward interactions when they pose a specific integration risk. Record
  the game and plugin versions tested.
- Never call a successful build an in-game test. Do not claim untested compatibility.
- Owner report (2026-10-05), Framework 0.93.0: a Harmony postfix sees a by-value argument
  as the game's method left it, not as it was passed. `Powered.GatherPower` counts its
  argument down, so the receipt hook recorded nothing delivered and every machine on
  conduit power stood still. Capture such an argument in a prefix (`__state`); check the
  game's method for writes to its own parameters before reading one in a postfix.

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

## Furnace material routing (2026-09-25)

- Shipbreaker 0.17.0 implements R4 MetalsOut -> F6 MaterialIn and F6 MaterialOut
  -> hull collector, through the existing checked CollectorService and Framework
  pairing/physical transfer. See `docs/furnace-material-routing.md`. Preserve R4
  ResidueOut and F6 Cooling meanings; `send` on R4 stays residue, `metals` selects
  aluminium. Default/all collector filters remain residue-only; furnace-products
  is explicit. Never automate Seal, Start, Equalize or Release through receiving.
- F6 feed motors share the measured F6 receipt, take remaining energy after probes
  and cooling pump, and retain paid losses as sink heat. Receive only exact single
  aluminium identities/masses into a cool idle unsealed bin; stop at twenty.
  Protect hot/native-commit state, recheck routes/IDs at settlement and preserve
  blocked cargo. Sealing cancels pending transfer clocks. Reload retains pairs,
  filters and cargo, with zero transport credit; since Shipbreaker 0.56.0 receiving
  that was running resumes by itself.
- Front input/output approaches are (-2.5,-2.5)/(+2.5,-2.5) in furnace-local tile
  coordinates, rotated with the 6 x 6 machine. Structural floor routes remain
  distinct from coolant and electrical conduit. Reuse existing artwork; no new
  generated assets are needed. Gameplay validation remains with the owner.


## Local departure and reclamation direction (2026-09-26)

- Auto Nav 0.18.0 / Shipbreaker 0.24.0 implement shared local avoidance, explicit
  Undock & Depart/Continue and bounded G4 capture/cut/feed/release/traverse/recapture.
  This supersedes earlier future-work exclusions of those operations. Follow
  `docs/auto-nav-departure.md`, `docs/shipbreaker-reclamation.md` and their validation
  record. Owner gameplay evaluation remains separate; do not claim whole-wreck
  deletion, new structural recipes or repeated furnace operation.
- Sensor/visibility admission precedes contact geometry. Never discard hazards on
  planner exhaustion or resume after tracking loss. Sensor switching follows the
  2026-09-28 owner sensor-engagement direction; never switch without telling the player.
  Auto Nav owns flight; manual takeover cancels mission authority. Ordinary Fly
  never undocks. Native departures bind exact attachments and persist intent before
  mutation, without granting clearance, paying fees or repeating uncertain detachments.
- G4 cuts only supported empty native 24 kg walls, preserving anchor supports and
  floors. Reserve capacity, pay actual electricity, journal uninstall and transfer
  independently, resolve the same physical ID and suspend uncertainty. Retain
  Framework's generic same-ship transfer restriction. Cutting/transfer are sequential.
- Cutter defaults are maintained constants, captured per job. Native D4 service-room
  heat uses the existing 10 kPa/40 C industrial bounds; the cooling connection is an
  authored abstraction. Full destinations may retry only within the same authorized
  mission; Pause, manual takeover and reload revoke automatic authority.
- Keep current runtime and installer minima together through the constants catalogue
  and `config/mod-dependency-minimums.json`; preserve historical compatibility gates.
  Update item-reference operating modes, per-mod guides, changelogs and Workshop drafts
  whenever these features change. Install only through the guarded existing installer.

## Native item actions and handling (2026-09-27)

- Audit direct actions, native maintenance jobs, carrying slots and conditions together. Use Framework `ItemHandling.Apply` after preparing owned definitions; content explicitly declares bulky sections/housings through `ItemHandling.Cumbersome`. Keep ordinary small supplies portable and installed equipment out of carrying slots. Internal compartments are not loose cargo.
- Reuse native pickup/drop and stack actions where valid; native Pick Up can use the drag slot for cumbersome cargo. Do not add operating-machine controls to unfinished assembly sections. Owner direction (2026-10-01), superseding the 2026-09-28 section-site follow-up: no machine is assembled from several identical sections, which the owner found arbitrary and confusing. The D4, R4 and F6 are bought or found whole and installed through their own Install job (Framework `SectionAssembly.RetireFromMenu`, construction stages through `SetWholeAppearance`); saved sections convert automatically through Framework `LegacyItemConversions`, complete sets into the whole machine and leftovers into their recipe bill, and saved section sites with a full bill may still finish (Framework 0.67.0, Shipbreaker 0.60.0). Do not design new equipment around multiple identical parts. Keep direct Install on complete loose machinery, saved table actions, delivered lots, cancellation and saved progress. See docs/section-assembly-and-maintenance.md. Retain native food, module, repair, restore and dismantle behaviour.
- Preserve saved cargo and placement. Correct handling on detached load data, allowing a previously saved hand slot only until successful native release; never silently relocate or destroy an item to enforce a new classification.
- Maintain the complete [handling ledger](docs/development/item-handling-audit.md) through the item-reference exporter and `scripts/audit-item-handling.py`. Add regression coverage for changes, including existing saves; definition checks do not establish live context-menu or Unity approval.

## Merchant availability and loot (2026-09-27)

- Owner requests much broader merchant availability and world finds. Current stock floors are content-owned (85% equipment/sections/boards, 95% supplies/food) before Framework availability configuration, with existing finite lots. Maintain through the constants updater and regenerate item references.
- Fill general-market coverage without duplicating prepared offers; retain native restocking, prices, merchant roles, other providers and saved inventories. Use suitable native leaf/engineering pools and single-item loot choices; never put wholesale lots, installed machinery or fabricated process records into world loot. See [merchant stock](docs/development/merchant-stock.md).

## Faction kiosks (2026-09-30)

- Owner direction: every sold Phobos machine, equipment and item is also offered at the game's faction kiosks (CCRE and GalCon), which sell only for scrip, gated by reputation according to its value and usefulness. The one owner rule: **nothing needs Honored.** Prices and gates are delegated, based on comparable vanilla items. Framework 0.53.0 implements this: each economy pack's `factionKiosks` section lists kiosks and a tier per family, supply or item; hidden tier marks are stamped on kiosk stock, and the game's tier triggers are amended in place (mark required by its tier, forbidden by every lower one). Scrip prices stay native (credit price x 0.05, as for all vanilla kiosk goods).
- Follow [the faction kiosk record](docs/development/faction-kiosk-stock.md) for the tier rule and vanilla anchors. A new sold item needs a tier in the same change; `tests/test_data_packs.py` and the native `FactionKioskChecks` enforce coverage and the Honored ban. Sections share their machine's tier and larger sizes their family's.

## Vanilla precedence (2026-09-28)

- Owner direction: the game's own actions take precedence; hook or amend them rather than working around them, and remove sections that fight them. The findings, verdicts and native evidence are in [the vanilla-precedence audit](docs/development/vanilla-precedence-audit.md); extend it when a related gate or override is found or changed.
- Never republish a native definition by name; amend in place (`DefinitionAmendments`, `NativeDefinitions.Amend`). Never block native destruction, mode switches or the native task closure: refuse at offer time, and where a finish must be refused return `NativeEffects.Refuse` so the game's task closes. No owner-only tasks, no need gating beyond the game's, no zero-duration manoeuvres, epoch snapshots by step not by exact value, and no interval-length faults where received electricity already bounds the work.
- A trigger name that does not exist resolves to the game's always-true Blank trigger; gate optional providers on the trigger table (`NativeDefinitions.Trigger`), never on the lookup. Framework 0.36.0 implements round 1 (crew, registration, clocks, UI); Framework 0.37.0, Agriculture 0.17.0 and Shipbreaker 0.34.0 implement round 2 (native power state through `ApplianceDefinitions`, stacked units through `StackUnits`, vanilla destructibility, own eating replies, offer-time work refusals, heat waits instead of stops, the vanilla wall rule and owner-confirmed accept-contents recovery). Auto Nav 0.25.0 implements round 3 (step-valid torch guard, the target left out of the imminent sweep, corridor-only threats, sensor refreshes as bounded holds, no target-spin gate, the docking console's own clamp and release through `DockingAdapter.Clamp`/`ReleaseClamps`, orbit locks as pilot takeover, oversized steps held, the game's throttle mapping). The saved elapsed flight budget stays preserved. Framework 0.38.0 and Shipbreaker 0.35.0 implement round 4 (hand feeding): wall identity by definition base with mass-derived revision-2 products for the game's cosmetic wall variants, every processor waiting for feed after one Start, and the crew loading order below. Owner gameplay checks remain pending.

## Crew loading orders (2026-09-29)

- Owner direction: "Load feed" is something the crew keep doing until cancelled, through the hourly time-skip. It is the right-click toggle `IndustrialRules.FeedOrder` on the intact installed D4, R4 and F6, which enables the family's standing order with the ship-wide source (`StandingOrder.ShipWide`: deck, unlocked containers and other machines' trays on the same ship, nearest first, as the game's own PDA Reload job searches), the order maximum stock and routine resume; off is a manual stop. A store already chosen in the panel is kept. Loading orders carry on after a reload like a painted job (owner decision); hazardous F6 orders still wait for Resume, and F6 hot steps still need the hazardous permission.
- Keep our hauling (route, reservation and single-unit checks) rather than the native Give effect; the managed skip advances only our orders. Deck items are cargo in play and in a skip; never take from a hidden system bin, a locked container, the equipment itself or someone's hands. The game's convention stands: crew need AutoTask and the Haul duty. F6 stack acceptance by hand stays deferred because the game's right-click places one unit; routed feeds take one unit of a stack since Shipbreaker 0.57.0.

## Materials and chemistry direction (2026-09-29)

- Owner direction, recorded from the scrap-and-silos review: **no custom gas species.** H2, H2O and He2 have no native conditions, `AddGasMols` ignores them and `GasContainer.Run` indexes a fixed list, so chemicals the game lacks live as bulk silo commodities in kilograms; only native species (CH4, CO, CO2, H2SO4, N2, NH3, O2, Smoke) are ever vented into or drawn from a room or canister.
- **Budgets, not native yields.** The game's Dismantle tables are fixed rewards that create or destroy mass; never copy them. Every Phobos recipe conserves mass and uses native identities for everything recoverable; a recipe that breaks a whole part into materials loses value against selling the part whole (the dismantling rule). Refining is not bound by that rule since 2026-09-30 (see "Refining value"). New identities are added only for terminal remainders (one per feed family, technical minimum price, never re-processed, never `ItmScrapTrash`) and for Manufacturing raw stock with a real consumer; the owner has approved custom ingots as that stock. No refined metals, glass, wire or plate without a consumer. Shipbreaker 0.38.0 implements the approved ingots: the F6 has an immutable recipe catalog (`Core/FurnaceRecipes.cs`; revision 1 is the unchanged housing, 2 aluminium ingots, 3 steel ingots) with per-recipe thermal profiles through the same lining, sink, radiator and rating; a batch stays bound to the revision it was sealed with, saved batches before 0.38.0 load as housings, the chamber admits aluminium and steel at the game level and the selected recipe decides, and the hot-pressure interlock scales with the profile target. Ingots (`PhobosAluminiumIngot`, `PhobosSteelIngot`, 4 kg, stack ten, priced above their four scraps) are Manufacturing raw stock sold at the general markets; table recovery to four scraps loses value; `PhobosSteelMeltRemainder` is terminal. Shipbreaker 0.36.0 implements the feed families (`docs/development/feed-families.md`): identity by definition base, mass on a declared step, one immutable catalog per accepted mass. Products are delivered into stacks since Shipbreaker 0.65.0.
- **Ship's Water: reclaim, do not join.** Process water may be drawn from its potable tanks and deposited into its waste tanks so its own recycler returns potable water with its own loss; a Phobos vessel never joins its potable pool (`IsVesselWater`) and never takes over its kiosk row. The adapter stays optional and version-pinned; no source reuse, only data tags and Harmony patches on public members.

## Bulk silos direction (2026-09-29)

- Owner direction from the scrap-and-silos review, implemented as Framework 0.39.0, Shipbreaker 0.37.0 and Agriculture 0.18.0: bulk shipboard storage is a **Framework bulk vessel** (`Liquids.BulkVesselSpec`/`BulkVessels`/`BulkVessel`), a saved record in kilograms with the native mass kept equal to dry housing plus contents plus cargo, journals around every transfer and conversion, owner-confirmed acceptance, and vanilla destructibility. It is deliberately **not "just like He3/D2O"**: a native stat would be filled by the kiosk by tag yet never burned by the reactor by name, and any mod could edit it outside the invariants. Content declares family, commodity, capacity, dry mass and record names; Framework never assumes a density.
- **No fuel or gas silos.** The native canisters already hold He3, D2O, O2, N2 and CO2 with native fill paths; a later bunker transfers into the exact native canisters. Water is the first and only silo commodity; add another only with a concrete consumer.
- Shipbreaker owns the S3 process water silo (3 x 3, 1,000 kg, 240 kg dry, `PhobosProcessSilo*`) and the T2 ice thaw unit (2 x 2, `PhobosIceThaw*`, exact `ItmIce01` 24.7 kg into 22.7 kg water plus the native 2 kg gangue, 6 kW for 40 minutes, 525 kJ/kg authored with NIST fusion attribution, 15% room heat under the R4 rule, no hold vessel: a block finishes only when the linked vessel has room). Pairing is `PortPairing` between `PhobosShipbreaker.ThawOut` and `PhobosShipbreaker.VesselIn` on any registered water vessel within one tile (Chebyshev rule in `ThawRules.Adjacent`). Agriculture's R3 re-points onto the same service with its existing record names; a T2 may deliver into it.
- Ship's Water stays reclaim-not-join: `ShipsWaterSupply.Refill` draws above a crew reserve, `DepositWaste` fills their waste tanks up to the capacity their own public configuration declares (reflection on `ShipsWater.Plugin`, 0.16.1 pin), never their potable tanks or kiosk row. Station purchase goes through `Trading.VesselSupplyProvider` and the existing Bulk supplies view.
- Artwork for the S3, T2, both ingots and the steel melt remainder was produced with PixelLab on 29 September 2026 (Shipbreaker 0.38.1) through `docs/development/bulk-silo-art-handoff.md`; the placeholders and their exporter are retired. The selected masters live in `assets/artwork-completion` with requests in `bulk-silo-requests.json`; the S3 and T2 are full-footprint masters (opaque edge to edge) whose world sprite is also the portrait, and the steel ingot is a recorded luminance recolour of the aluminium ingot. Revise them through the existing artwork and provenance rules; never reintroduce placeholder masters as reviewed art.
- Owner request (2026-10-04), Framework 0.79.0: every bulk vessel shows its contents on
  the game's right-click card, as Ship's Water's tanks do. `VesselContentsDisplay` keeps
  one display-only `StatPhobosVessel<Name>` condition per commodity (plus a shared
  Trapped row), written from the record by `BulkVessel.Save` and once per ship after
  loading; nothing reads it back. A new vessel commodity declares its row with
  `VesselContentsDisplay.Declare` beside `BulkVessels.Register`; the native checks
  refuse an undeclared one.
- Owner decision (2026-09-29, economy coverage audit): the S3, T2 and Agriculture R3 stay **purchase-only**. Construction of anything beyond semi-advanced equipment waits for Phobos Manufacturing to decide where and when it belongs; do not add ingot- or section-based recipes for them before then. Keep every machine family's merchant, loot, repair, Restore and dismantle coverage at parity with its siblings, checked natively (`docs/development/economy-coverage-audit.md`).

## Realistic chemistry and hazards (2026-09-29)

- Owner direction, applied from Manufacturing 0.1.0 to every Phobos Ostranauts
  recipe: **outputs are only what the inputs can realistically yield**, as close
  to real stoichiometry as item units allow. Add new materials or minable asteroid
  chunks through our mods where honest chemistry needs them (the clay hydrates
  chunk, nickel-iron ingots, carbon stock and the two terminal remainders are the
  first). Round to item units and state the rounding; record each recipe's
  reaction, mass balance, energy and a primary source beside the claim, and label
  authored percentages as ours. The owner is an applied chemist; do not present
  fixed-reward yields, mass-creating recipes or loops that create value while
  consuming nothing (see "Refining value" for what may legitimately gain value).
- **Hazards use only the game's own machinery** and are welcome where a reaction
  goes bad, a store is damaged or a machine off-gasses: native fire (`SysFire`),
  explosions (`Explosion,<name>` objects from a mod `data/explosions` folder),
  damage mode switches, canister leaks and poisoning bands. Only native gas species
  (CH4, CO, CO2, H2SO4, N2, NH3, O2, Smoke) ever enter a room or canister, always
  clamped through Framework `RoomGas` / `NativeGasCanister`; hydrogen and every
  other missing chemical stay kilogram records and never enter a room as gas.
  Every hazard conserves mass, is journaled, warns the player through
  `PlayerNotices` and never blocks native destruction. Framework 0.41.0 owns
  `RoomHeat`, `RoomGas`, `NativeGasCanister`, `NativeExplosions`,
  `ConsoleAuthority`, `ApplianceDefinitions.AddFeedBin/SetPowerOverride` and the
  bulk-vessel `DamagePolicy.Leak`; content owns thresholds, recipes and balance.

## Feedstock round three (2026-09-30)

- Owner decisions on the round-three design record: the Lixivar LC-3 name and the
  authored evaporite crust are approved; the formulation into Agriculture's makeup
  salts is priced at Agriculture's own packet price (an exception to the 1.5 x
  guardrail, capped by that price; no merchant sells the salts); the refuelling kiosk
  also sells bulk fertiliser, delivered as a Groundwork nutrient hopper a W2 doses
  from; both struvite routes, with sulfur and sulfuric acid (a meteorite
  sulfide-phosphide nodule, Lixivar acid tanks that are not gas stores, an SA-3 acid
  plant); and the V4 service refactored into a shared charge engine, not copied.
  Follow `docs/development/feedstock-round-three-design.md` and the phase table in
  `docs/development/asteroid-feedstock-programme-status.md`. Manufacturing 0.17.0
  (engine), 0.18.0 (LC-3, crust, calcine) and 0.19.0 (SA-3 acid plant, AT acid
  tanks, sulfide nodule), 0.20.0 (Epsom salt from olivine, acid-route struvite,
  crop nutrients into a hopper) and Agriculture 0.27.0 (nutrient hoppers, kiosk
  crop nutrients, W2 dosing) implement all five phases; the planned standalone
  ammonium sulfate charge became the formulation's ammoniation, because a charge
  must bind an item. Owner gameplay checks remain pending.

## Shared equipment, links and save migration (2026-09-30)

- Owner direction: **share resources and equipment across the Phobos mods where
  possible; inter-mod dependencies are acceptable.** Equipment every mod needs (pipe
  and belt lines, water tanks, panel hosts) belongs in Framework, which every mod
  already requires; consolidate near-duplicate families instead of keeping them in
  parallel. This refines "Framework only" dependency wording elsewhere: a content mod
  may depend on another where sharing needs it, recorded through the constants
  catalogue and `config/mod-dependency-minimums.json`.
- Owner rule (must): **every link between our machines and stores, and machine to
  store to machine, runs through touching equipment (footprints touching or one tile
  apart, `BulkVessels.WithinOneTile`) or a pipe or conveyor-belt network, never across
  open floor.** The station refuelling kiosk is the only exception. Touching suitable
  equipment joins as if piped, and joins chain across the player's ship.
- Owner rule: **migrate existing saves automatically** (the owner plays with these
  mods). Prefer idempotent load-time conversions (converting old assets to their new
  equivalents is preferred to keeping legacy forms); a documented manual step is
  acceptable only when unavoidable and must be stated explicitly in the changelog and
  player guides. Every plan lists the saved structures it touches, marks each
  automatic or manual, and tests it with old-record or old-save fixtures.
- The approved plan (fixes; shared line networks and stacking; process-water, gas and
  acid lines; shared stores; the Framework water-tank ladder with R3-R5 converted on
  load; Ship's Water tanks joining water networks; working conveyor belts) is carried
  out in release sets; check the changelogs for what has landed.
- Landed so far: Framework 0.56.0 (topology, `LineReach`, `SharedPorts`, lanes,
  `ProviderPanel`, `DefinitionMigrations`); Framework 0.57.0 with Manufacturing 0.23.0,
  Shipbreaker 0.53.0 and Agriculture 0.30.0 (Framework-owned gas and process-water
  lines, the `LinePorts` rule, `VesselLink` for every machine-to-vessel link, shared
  banks, link labels and the oxygen/fuel caution); Framework 0.58.0 with Shipbreaker
  0.54.0 and Agriculture 0.31.0 (the Rivetline S2-S5 water silos in Framework under
  their saved ids, `MachineFamilies`, R3-R5 converted on load through
  `DefinitionMigrations.Retarget`); Framework 0.59.0 with Agriculture 0.32.0 (Ship's
  Water 0.16.1 tanks amended in place with a water port, `ShipsWaterSupply` drawing and
  depositing only through tanks the machine or silo reaches, the rack's water port;
  open-floor Ship's Water users need a line or a touching tank, stated as a manual
  step); Framework 0.60.0 with Manufacturing 0.24.0 and Shipbreaker 0.55.0 (the
  Lixivar acid line with acid ports on the LC-3, SA-3 and AT tanks, the wet-line
  spill into the source tank's bund through `BulkVessel.Contain` and
  `LineReach.MembersThrough`, and link wording that names touching or the line);
  Framework 0.61.0 with Shipbreaker 0.56.0 (the Rivetline conveyor belt,
  `BeltNetwork` and `UnitItemTransfer`; Shipbreaker's item routes need a belt or
  touching equipment instead of structural floor, with routes over bare floor
  stopping as a documented manual step). Owner decision, applied in Shipbreaker
  0.56.0: running belt routes and storage unloading **resume after a reload**; this
  supersedes the pause-on-reload rule for those transfer routes only, and processing
  permission keeps its own rule. See `docs/development/conveyor-design.md`.
  New machine or store links go through
  `VesselLink`; new ports through `LinePorts` and `LineDefinitions.AddPort`; process
  water is stored only in `WaterTanks`, and new mods add work to them by amendment.
- Owner decision (2026-10-01), after no water, nitrogen or CO2 store would link in
  play: **any touching pipe joins.** A water, gas or acid line joins equipment when
  it runs under it or directly beside it on any side, as a belt does; the 0.57.0
  single port tile is retired as a requirement (it only marks who takes part and
  where the joint is drawn). Framework 0.69.0 with Shipbreaker 0.63.0, Manufacturing
  0.28.0 and Agriculture 0.35.0 implement it, with no saved change. Every link
  picker must say why something aboard is not offered (`LinkChoices.Note`,
  `LineReach.Problem`): show the field whenever a vessel of its cargo is aboard, and
  never leave a player with an empty list and no reason. The irrigation conduit and
  the F6-C coolant conduit keep their exact points until the owner decides otherwise.
- Owner decision (2026-10-01): the T2 thaw unit and the ML-2 mining laser open
  Framework's shared `ProviderPanel` (Shipbreaker 0.63.0), like the water silos and
  Manufacturing's machines. The D4, R4, F6, G4, C2 and cooling assemblies keep the
  industrial panel. New provider-driven equipment uses the shared panel.

## Lines hold their contents (2026-10-01)

- Owner decisions: every Phobos pipe holds what it carries until drained, with realistic
  hold-up; a gas line may hold any mix of gases; drained liquid goes into a portable drain
  canister that crew haul (the game's Haul, with LOGUSS's Common Sense as an optional,
  no-code tie-in); irrigation parcels and the furnace coolant charge move to the same
  model with their saved contents converted on load.
- Framework 0.63.0 with Manufacturing 0.25.0 implement the water, gas and acid lines:
  - `LineContents` keeps a per-segment record, and segment mass is dry mass plus contents.
    Open runs are topped up every two seconds from the stores on them.
  - Crew actions: Drain line into canister, Vent gas line, and Return line to service.
    A drained run is closed and joins nothing.
  - Removal of a holding segment is refused when offered. Damage vents gas and releases
    a declared mist.
  - The Rivetline D20 drain canister pours into a matching tank's inventory or canister
    rack.
  - Follow `docs/development/line-contents-design.md`. Declare new holding lines through
    `LineContents.Declare`; never add a separate spill for them.
- Framework 0.64.0 with Shipbreaker 0.58.0 put the F6-C coolant conduit on the model
  as a pumped circuit:
  - Non-network holding families are filled by their content mod through
    `LineContents.Circuit`/`Top`.
  - The furnace's unchanged four-key charge is the reservoir; the pump primes the
    circuit from its surplus above 5 kg, and circulation needs a full circuit.
  - `DrainCanisters.RegisterReceiver` lets the F6 take a canister of coolant.
  - The legacy sealed assembly keeps an empty conduit. A running serviced loop needing
    more charges is the documented manual step.
- Agriculture 0.33.0 completes the set: the irrigation conduit holds water or one of
  three feeds.
  - A running W2 primes each branch's run before delivering straight through,
    replacing the per-rack parcel and its transit clock.
  - Old parcels are delivered into their racks on the next powered run.
  - The W2 takes canisters of water or its own feed.
  - Owner gameplay checks remain pending.
- Owner decisions (1 October 2026), Framework 0.65.0 with Agriculture 0.34.0:
  - Ship's Water drinking tanks fill the water lines they join, above the crew reserve.
  - A W2 formulation change flushes the pipes automatically: old water back to its
    reservoir, old feed as recorded process solution.
  - Drained feed of another formulation goes to drainage treatment as the same item.
  - Gas lines keep venting, with no gas canisters for now.
  - The drain canister has PixelLab art.
- Owner direction (1 October 2026): make edits in the base project checkout rather
  than a `.claude/worktrees` worktree wherever possible, so the owner can follow the
  work via Git.

## Refining value (2026-09-30)

- Owner direction: the rule that **every refining charge must lose value** belonged
  to an earlier stage and is retired. Refining and chemical processing may give
  better value than their inputs where the work is real. Re-examine which current
  and planned operations should gain value, rather than defending the old ceilings.
- Unchanged: mass conservation, realistic yields and sourced chemistry, terminal
  remainders, and the separate dismantling rule for breaking whole equipment or
  parts into materials. Reverse steps that undo a refinement (table recovery of an
  ingot into scrap) still lose value, so no refine-and-recover loop pays.
- Working guardrails until the review settles them (agent proposal, not yet owner
  decisions): a gain should reflect a real input (scarce mined feed, energy, time,
  machine wear, crew work); no closed loop that returns its own inputs may gain
  value; inputs bought at a station deserve the most scrutiny, because they make a
  repeatable trade loop; prices stay plausible beside the game's own economy.
- Review outcome (2026-09-30, Manufacturing 0.15.0 / Shipbreaker 0.49.0, agent-applied
  under the guardrails above and open to owner revision): the nickel-iron ingot
  returns to $20 (carburising gains eleven percent at base prices; no merchant
  sells nickel-iron ingots or carbon, so there is no loop); the methane ice price correction is withdrawn and the game's $20
  stands (neither thaw product sells back); carbon, the mined chunks, F6 casting
  and the K2/AX-2 conversions keep their prices. `ManufacturingNativeChecks`,
  `RefineryChecks` and `SiloNativeChecks` now prove the guardrails at live prices:
  sellable products within 1.5 x inputs, a quarter when every input is bought
  stock, stock priced between scrap steel (3.6 cr/kg) and its ore (22.5 cr/kg).
  Price new refined products against these; do not reintroduce a loss requirement.
- Owner direction (2026-10-01): **refining is to be a reasonably profitable business
  venture from now on, retroactively.** Owner decisions the same day:
  - Target: about double. Finished products sell for roughly 1.5 to 2.5 times the raw
    ore at base prices, before power, time and machine cost.
  - Sales route: the refuelling kiosk buys bulk back from the ship's stores at a set
    share of its selling price, so buy-and-resell never pays; hydrogen, methane and
    ammonia gain prices. No trade-good packing.
  - Measure: each product chain is profitable overall; low-yield survival charges
    (hydrates to water) stay supply.
  - That record also ranks the reactor interdependency ideas (owner request, same
    day: anything goes, vanilla consumables included).
- Owner approval (2026-10-01) of the record's Part 1: **the 30 September guardrails
  above (1.5 x inputs, stock below its ore) are superseded.** The rules now in force
  are in "The rules now in force" of
  `docs/development/refining-business-and-interdependencies.md`: business chains
  1.5 to 2.5 x the ore at base prices; supply chains (water ice, hydrates, clay, the
  salt crust's gases) keep native gas prices; fertiliser may exceed the band because
  it is rare in the game's world; the kiosk buys bulk back from installed stores at
  45%, never hopper nutrients; bought stock at most 1.25 x; no gaining loop; shared
  products priced so no chain leaves the band, or split. The price table is
  accepted provisionally, pending the owner's gameplay feel; methane ice is
  re-priced to 100; the mined iron chain ends in a new nickel-alloy ingot while the
  steel ingot keeps its price and F6 source. Decisions 6 to 10 follow the record's
  recommendations (scrap loops left, reactivation at 25% loss with EVA filters,
  magnesia terminal, a minimal supersession mechanism, first slice ideas 1 to 3).
  Price every new refined product against these rules and prove it in the native
  value checks. Framework 0.68.0 (kiosk buy-back `IBulkBuybackProvider`/`VesselBuybackProvider`,
  recipe `supersedes`, `EquipmentSaveUpgrade.FollowPrice`), Manufacturing 0.26.0 (the
  price table, the nickel steel ingot as V4 revision 8 superseding the plain steel
  charge) and Shipbreaker 0.62.0 (methane ice at 100) implement Part 1. Manufacturing
  0.27.0 implements Part 2's first slice: V4 gas draws (a drawing charge binds only
  when its store is linked), methane cracking into a new low-priced carbon black
  (kept below the bought-water loop), a carbon burner, CO2 filter reactivation as a
  non-gaining service, and A2 carbon dioxide dosing for grow rooms.

## Regolith programme (2026-10-05)

- Owner request: uses for the game's unused Regolith (Loose). All five chosen: an LC-3 acid
  leach with an outcome table, a V4 volatile bake, sintered pavers laid as a Phobos twin of
  the game's Polished Regolith Floor, and oxygen from rock by both routes under one new
  brand, **Oxsmith** (EC-4 molten regolith electrolysis, CR-4 carbothermal reduction with
  methane). Ferrosilicon is new stock with a use in the same release (silicol hydrogen on
  the LC-3). Methanation is a second K2 mode fed from new Fennmark carbon monoxide stores;
  old K2 records must read unchanged. Manufacturing machines gain an optional product
  store by touching or belt (Framework 0.98.0 `StoreDelivery`, Manufacturing 0.49.0), and
  the V4 leaves regolith alone unless told to bake or sinter it. Manufacturing 0.50.0 ships
  the leach (leach revisions 11 to 14); a prospector sells regolith, so its table is held
  to the bought-stock rule (1.25 x), not the 1.5 x table rule. Manufacturing 0.51.0 ships
  the V4 bake and pavers (refinery revisions 15 and 16) as **optional recipes**: a recipe
  requiring `chosen-<its id>` is available only on a machine whose saved `prefer` names it
  (`ChargeCatalog.MetWith`), so the default leaves regolith alone. The regolith floor is
  `PhobosRegolithFloor`, a clone of the game's `ItmFloorGrate02` on the game's own item
  definition with its own Install and Uninstall jobs; never give the game's tile an
  uninstall, and never let a loose Phobos item carry `IsFloorGrate`. Any charge fed only
  by regolith is held to 1.25 x by the native value check (the paver is 13 cr for that
  reason). Manufacturing 0.52.0 is set 4: the **Oxsmith EC-4 Electrolysis Cell**
  (`PhobosElectrolysisCell`, machine `electrolysis-cell`, a fifth charge machine, 4 x 4,
  60 kW, 96,000 cr as an agent default) melts a regolith lump or a Silicates ore chunk into
  oxygen for a linked store, ferrosilicon and slag; the LC-3's leach revision 15 turns
  three ferrosilicon and water into hydrogen for a linked hydrogen store. Follow
  `docs/development/regolith-oxygen-design.md`. The 3.9 kg yield and the lump's
  composition are authored; the NASA Kennedy source gives the process, not a yield. The
  hydrogen recipe keeps its caustic soda as a standing charge in the machine (agent
  choice, stated wherever the recipe is described): do not present it as the real
  silicol balance. Both EC-4 charges are **supply**, judged by the native check as loops
  (bulk at the kiosk's buy-back share, ferrosilicon at 1.2 x, never repaying the rock),
  which is why ferrosilicon is 2 cr. Manufacturing 0.53.0 is set 5, which completes the
  programme: the Fennmark **Z2 to Z4 carbon monoxide stores** (a seventh `GasFamily`; a
  game gas, so leaks go into the room, and a fuel through the shared `Combustion`), the
  **K2's second mode** (CO + 3 H2 -> CH4 + H2O, chosen by linking a carbon monoxide
  store as the carbon source), and the **Oxsmith CR-4 Carbothermal Reactor**
  (`PhobosCarbothermalReactor`, machine `carbothermal-reactor`, 30 kW, 72,000 cr as an
  agent default), which reduces the same rock as the EC-4 with methane drawn from a
  linked store. Owner decision (5 October 2026) amending the 29 September K2 rule: the
  saved holds and the one-step conversion stay, and the record gains two optional
  fields (`co`, `used_co`) written only when not zero, so an old record reads and
  writes unchanged; the gas in the hold decides the cycle being finished. Never add a
  required K2 field. The CaRD source gives oxygen per kWh, not per kilogram: the yield
  is authored and the reactor is stated to be about eight times as efficient as NASA's
  rig. A P1 may burn carbon monoxide (agent choice under the player-flexibility rule, a
  change from the plan, recorded in the design record). Still open: the paver's own
  picture and the owner's checks in play. Follow `docs/development/regolith-programme.md` for decisions, order, agent
  defaults and the list of figures still unverified; never cite those until checked.
  The Oxsmith art handoff (`docs/development/oxsmith-art-handoff.md`) was written first
  because the owner's ChatGPT plan is time-limited.
- Owner artwork direction, 5 October 2026: use the agent's artistic judgement for
  Oxsmith while retaining a resemblance to vanilla gameplay art. EC-4 and CR-4
  artwork is now prepared with built-in Imagegen (two calls, no PixelLab): untouched
  1254 x 1254 sources, 256 x 256 working masters, 64 x 64 native pilots, opaque 4 x 4
  footprints. Keep oxide-red enamel, ceramic decks, charcoal steel and small ice-blue
  oxygen fittings. The EC-4 overhead pilot was inspected before serving as the CR-4's
  family reference. No glowing sight ports, live instruments or changing workpieces;
  the CR-4's paired domes use a compact vertical layout. Exact requests, registration,
  review and hashes are in `assets/phobos-manufacturing/oxsmith-requests.json`.
  Neither machine is implemented or bound to runtime images yet.
- Owner approved the Oxsmith artwork and requested the same finish for all other
  chemical reactors on 5 October 2026, excluding silos. V4, X2, K2, AX-2, LC-3,
  SA-3 and Copperhead-3 replacements retain native dimensions, image IDs, maker
  palettes and all gameplay. Eight built-in Imagegen calls include one K2 gas
  manifold correction; exact requests, references, review and archive receipt are
  in `assets/phobos-manufacturing/chemical-reactor-requests.json`. The completion
  exporter binds the reviewed masters. All other Manufacturing images, including
  stores/tanks/silos and neutral normals, were verified unchanged. Use the common
  machinery registration scripts for reproducible masters and
  `scripts/archive-artwork.py` for future verified archive appends.

## Mining laser direction (2026-10-01)

- Owner request: a ship-mounted mining laser for Shipbreaker, animated like the
  game's heater, with a beam drawn from the sprite across a 60 degree arc, to
  mine asteroids and take derelicts apart; a heavy electrical consumer, with a
  chemical laser as a later alternative. Owner decisions on the plan: it reaches
  **attached targets only** (a tethered asteroid or a G4-captured hull, never a
  free-flight range model); it carries the new **Ablatine** brand as the ML-2;
  it draws 24 kW with room heat now and gains a **radiator link** as a second
  phase; and **freed panels drop where they are for the crew to haul**.
- This supersedes "do not add a second mining system" for this machine only, and
  keeps its intent: the laser drives the game's own mining damage and uninstall,
  so the game's loot tables stay the only source of ore. Never spawn ore or copy
  a native table. Opened ore deposits stay crew work.
- Shipbreaker 0.59.0 with Framework 0.66.0 implement it. Framework owns
  `Hazards.NativeDamage`, `Observations.BeamGeometry` and `AttachedTiles`, and
  `Effects.SpriteAnimation`, `WorldBeam` and `BeamTransform`; Shipbreaker owns
  the head, its figures, filter and panel. Follow
  `docs/shipbreaker-mining-laser.md` and `docs/development/mining-laser-design.md`.
  Animated sprites use the game's own frame animation through `Item.SetAlt` with
  the frame step detached before each change; never the cosmetic overlay route,
  which replaces the object. Sheets are derived mechanically from a selected
  master and checked against it by the exporter. Presentation is never saved and
  a rendering fault never reaches the work. Owner gameplay checks are pending.
- Shipbreaker 0.61.0 adds the radiator link by touching: a head pairs with one
  F6-R or F6-P through the assembly's own cooling port (one assembly, one
  machine), its heat goes to that assembly's store while it is ready and has
  room, and a High setting (48 kW) applies to jobs started then. Two parts of
  the approved plan were left for the owner and are not to be assumed done: the
  radiator node stays in Shipbreaker (`RadiatorSink.cs`) and was not lifted into
  Framework, and there is no F6-C conduit link for the laser. See the design
  record for the reasons.
- Owner decision (2026-10-04), Shipbreaker 0.71.0 with Framework 0.78.0: two laser
  settings, off by default, queue **the game's own jobs** on what a finished cut
  leaves: Haul on freed panels, ore and gangue, and Mine on a deposit it opened
  (Framework `Crew.NativeJobs`, the PDA's own task fields). Phobos crew orders stay
  same-ship; work on a moored ship goes to the crew this way, never through a
  cross-ship hauling order. The laser still never cuts a deposit or collects anything.
- The chemical laser is an idea only. It needs sourced chemistry and feeds the
  mods do not store yet; do not add consumer-less commodities for it.

## Medical direction (2026-10-04)

- Owner styling clarification (4 October 2026): use the owner-selected Ward-3
  infirmary bed as the visual baseline for Medical equipment, including the
  Vigil-2. Preserve its original Halewright family: off-white enamel, pale
  mint-grey upholstery where appropriate, mid-grey structural trim, dark recesses,
  quiet muted blue-teal accents, practical folded mechanisms and restrained wear.
  Match its coarse Ostranauts pixel treatment and overhead projection while
  keeping each device's function recognizable. Use the original Phobos Ward-3
  source as the image reference; retain the current handoff's technical requirements and
  no painted live-state instruments. See `docs/development/medical-art-handoff.md`.
- Owner size direction (4 October 2026): the original monitor image was too small;
  choose a size suited to Ostranauts and the other mods. Agent choice: 2 x 2 cart,
  32 x 32 native, 128 x 128 working master, keeping the untouched 1254 x 1254 source.
  The planned monitor is now Vigil-2 under the width-based naming convention.
  The exact original Vigil-1 prompt remains historical; no monitor runtime IDs
  or saved equipment exist yet.

- Owner request: a new mod expanding on the vanilla Van Buren Infirmaway medical bed,
  whose only real function is a stronger healing sleep (the game's `SleepingMedical`;
  see `docs/development/medical-bed-research.md`). Owner decisions the same day: a new
  content mod **Phobos Medical** (`PhobosMedical`, Framework required) under the new
  brand **Halewright** (models a word plus the footprint width: Ward-3, initially Vigil-1,
  Attend-2); **a new own-brand bed with companion equipment, leaving the Infirmaway
  untouched**; all four directions (any patient in the bed, a working autodoc, a
  bedside monitor and medic care, honest power and weightless care); authored tables
  in data packs from the start (the content-owned `care` schema, players tune or add,
  code never branches on a treatment id); base healing is the game's own Recuperating
  with Halewright extras on top; artwork a ChatGPT base with PixelLab layers
  (`docs/development/medical-art-handoff.md`; the owner runs the ChatGPT step).
- Follow `docs/development/medical-bed-design.md` for the release sets. Treatments
  perform the game's own wound slotting on real items, never authored wound changes;
  the monitor never heals; the autodoc follows the medic. Shared patient, wound,
  placement and gravity services belong in Framework with Medical as first consumer.
  Delivered: Medical 0.1.x (Ward-3 bed, care pack, owner-selected 2075 art) and 0.2.0 with
  Framework 0.84.0 (Send injured crew here; weightless care through one hand-applied
  `Wound.Run` transpiler, `levels.bed.weightlessHealing` default 1 as an agent default),
  and 0.3.0 with Framework 0.86.0 (Vigil-2 2 x 2 patient monitor, pairing by touching through
  `Observations.Footprints`, readings, hour trend and alerts; it never heals), and 0.4.0 with
  Framework 0.89.0 (Keep patient treated: the care pack's `treatments` section, `CrewRole.Medical`,
  `Health.WoundCare` slotting real items the game's own way, `CrewWorkOffer.ExcludedActor` so a
  patient never treats themselves). Owner gameplay checks remain pending.

## Documentation audiences

- Keep current player operating guides and item references in `docs/`. Put contributor instructions, implementation details, research, design proposals and audit reports in `docs/development/`, with separate indexes. Mixed operating guides may retain necessary limits and direct research credits; link detailed evidence rather than burying the next player action.
- Preserve dated evidence and citations when moving documents. Update relative links, generator inputs, build/packaging references and review inventories together. Packages must preserve the same player/development separation.
