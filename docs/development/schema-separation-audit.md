# Schema separation audit: authored data versus business logic

Audit record, **30 September 2026**, at the owner's request before the asteroid
feedstock programme continues ([status](asteroid-feedstock-programme-status.md)).
The question: where should authored data move out of C# into a schema format,
separate from business logic, to gain reusability, ease of maintenance, less work
per new implementation and end-user editability. Every C# file under `src/` was
read or grepped (about 37,600 lines across six mods), together with the data files
each mod ships, the constants catalogue, the installer lists, the artwork manifest
and the native test expectations. Nothing was changed by the audit itself.

Sections 1 to 4 are findings. Section 5 is the recommendation, section 6 the
disagreements and cautions, section 7 the proposed order of work. Owner decisions
are needed on the questions in section 8 before implementation.

## 1. What is already data, and how it is loaded

The repository already has six distinct data mechanisms. Any new schema layer
should be judged against them, and preferably absorb the reusable ones.

| Mechanism | Files | Loaded by | Strictness | Player override |
| --- | --- | --- | --- | --- |
| Native game JSON | `mods/*/data/{conditions,condtrigs,explosions,cooverlays,guipropmaps}` | The game's own `DataHandler` | The game's | Any mod folder (last load wins) |
| Construction recipe packs | `mods/*/framework/recipes.json` | Framework `ConstructionRegistry.RegisterPack` (`src/PhobosFramework/Construction/ConstructionRegistry.cs:71-188`) | Typed DTO, unknown members refused, `schemaVersion` 1, size cap, mass balance and native reference validation, all-or-nothing commit, per-owner status on the F3 console | None |
| Equipment names | `mods/*/framework/equipment-names.json` | Framework `EquipmentNames` (`Localization/EquipmentNames.cs:17-53`), embedded at build | Allow-list of two fields, charset and length rules, cross-checked against the English catalog | None (embedded) |
| Translations | `translations/*/en.json` embedded; overlays in `<mod>/translations` and `BepInEx/config/PhobosTranslations/<owner>` | Framework `TranslationCatalog` (`Localization/TranslationCatalog.cs:151-204`) | Strict file parse; unknown keys warned and skipped; placeholder signatures validated | Yes, by language file |
| War Declared schematics | `mods/PhobosWarDeclared/schematics/*.json` embedded; `BepInEx/config/PhobosWarDeclared/schematics/*.json` | `src/PhobosWarDeclared/Schematics.cs:26-66`, parser `Core/Schematic.cs:90-145` | Allow-lists at every level, unknown field refused, enumerations checked, first-match rules; a bad user file is reported and skipped, never fatal | **Yes, override by file name** |
| Auto Nav hub layout | `assets/phobos-autonav/hub-layout.json` embedded | `src/PhobosAutoNav/HubLayout.cs:10-62` | `Required.Always`, bounds, unique ids; tested against the source file | None |

Build-time data surfaces (not loaded by the game): `config/maintained-constants.json`,
`config/item-reference.json`, `config/mod-dependency-minimums.json`,
`config/workshop-publishing.json`, `assets/artwork-completion/manifest.json`,
`assets/phobos-agriculture/layers.json`, and the hand-written file lists in
`scripts/install-mods.ps1`.

Two conclusions follow. First, Framework already contains most of a generic schema
loader: `ConstructionRegistry` has the typed, versioned, validated, transactional
shape; `Schematics` has the shipped-plus-override discovery and non-fatal
reporting; `TranslationCatalog` has the user directory convention. None of them is
reusable for a second schema without refactoring. Second, the game ships
`Newtonsoft.Json`, and every existing mechanism uses it, so JSON is the format.

## 2. What lives in C#, by kind

The four per-mod inventories (Framework, Shipbreaker, Manufacturing and
Agriculture, Auto Nav and War Declared) classify every authored literal as PURE
DATA, DATA WITH INVARIANT (naming the invariant) or LOGIC. Condensed by kind:

### 2.1 Pure data (safe to externalise)

| Kind | Where | Volume |
| --- | --- | --- |
| Equipment economy specs: price, install/uninstall/repair/dismantle work, repair bills, intact and broken salvage, Restore minutes, loot flag | `Shipbreaker/EquipmentEconomy.cs:31-73` (12 rows plus two ladder formulas), `Manufacturing/EquipmentEconomy.cs:43-71` (7 rows plus the store ladder), `Agriculture/EquipmentEconomy.cs`, `BulkDefinitions.cs:74-77`, `IrrigationDefinitions.cs:28-35`, `WorkupDefinitions.cs:25-31`, `AutoNav/Core/EquipmentRules.cs:7-25` | About 30 rows, 20 values each |
| Merchant offers and stock lots: merchant, tag, item, chance, condition; lot quantities; chance floors | `Shipbreaker/EquipmentEconomy.cs:175-203`, `StockQuantities.cs`, `Manufacturing/EquipmentEconomy.cs:150-181`, `Agriculture/Definitions.cs:108-117`, `AutoNav/EquipmentContent.cs:91-104`, `StockQuantities.cs` | About 60 offer rules and 15 lot constants |
| Regional availability factors (15 regions, repeated per mod) and regional merchant lists | `RegionalEconomy.cs` in Shipbreaker, Manufacturing, Agriculture and Auto Nav | 4 copies of the same 15-row table |
| Loot: engineering-salvage chances, fridge and crate chances, carve shares | `Shipbreaker/EquipmentEconomy.cs:131-151`, `Agriculture/LootContent.cs`, `Manufacturing/Definitions.cs:341-346`, `Shipbreaker/Core/IceSupplyRules.cs`, `AutoNav/EquipmentContent.cs:105-118` | About 30 values |
| Machine plant figures: footprint, dry mass, working and idle kW, cycle seconds, feed capacity, room-heat fraction, access range | Every `Core/*Rules.cs` | About 15 machines, 8 values each |
| Vessel and store families: prefix, commodity, species, model letter, capacity, dry mass, price, leak rate, record names | `Manufacturing/Core/GasStoreRules.cs:143-162` (6 families), `Shipbreaker/Core/SiloRules.cs`, `BinRules.cs`, `Agriculture/BulkDefinitions.cs` | 9 families |
| Materials and stock items: id, mass, price, stack, native category, terminal flag, art key, mined flag | `Manufacturing/Core/Materials.cs:38-53`, `Agriculture/Definitions.cs:78-86`, `WorkupDefinitions.cs:34-36`, `Shipbreaker/FurnaceDefinitions.cs:68-76`, `ReclaimerDefinitions.cs:50-51`, `FeedFamilies.cs:64-69` | About 40 items |
| Definition shaping: install menu tab, map points (use, power, gas, coolant, material), art and portrait paths, container sizes, sockets, `fZScale`, cumbersome flags, held-slot effects | Every `Definitions.cs` and `*Definitions.cs` | About 25 families |
| Settings defaults and ranges | `Plugin.cs` and `Settings.cs` in every mod (Auto Nav alone has 24 bindings) | About 50 settings |
| Crop profiles, harvest ratios, appearance thresholds | `Agriculture/Core/Crop.cs:15-38`, `CropAppearance.cs` | 3 crops, 9 fields each |
| Auto Nav operating rules: arrival and cruise ranges and stops, docking limits, torch limits, sensor margins, timeouts | `AutoNav/Core/{ApproachRules,CoastRules,DockingRules,InstrumentRules,TorchRules,SensorSelection,ContactReading}.cs` | About 60 values |
| UI theme: colours, font sizes, row heights, insets | Framework `Controls/*`, `Crew/CrewPanel.cs`, `AutoNav/AutoNavPanel.cs`, `Shipbreaker/IndustrialPanel.cs` | About 60 values, one overlay colour repeated six times |

### 2.2 Data with an invariant (externalisable only with a validator)

| Kind | Invariant | Where enforced today |
| --- | --- | --- |
| Process recipes: refinery charges (6), furnace recipes (3), thaw (2), feed families (6), reclaimer (1), K2/X2/AX-2 cycles | Mass conservation within `Units.MassToleranceKg`; stoichiometry from the game's molar masses; off-gas only native room species; durations within `ProcessJob` bounds; **revision immutability** (a saved charge stores only its revision and re-reads products from the catalog: `Manufacturing/RefineryService.cs:147`, `Shipbreaker/Core/FurnaceRecipes.cs:69-75`, `ProcessJob.cs`) | `ChargeRecipe` constructor (`RefineryRules.cs:102`), `FurnaceRecipe` constructor, `Balanced()` in the cycle rules, pure tests |
| Furnace thermal profiles (aluminium, steel) | target > melt > reference; the aluminium profile reinterprets saved hot and sink kJ and must not change for pre-0.38.0 batches | `FurnaceRecipes.cs:30-35`, `FurnaceState` load bounds |
| Vessel size ladder (Framework `BulkVesselSizes`: capacity +10%/step, dry −15%/step, price exponent 0.6, rounding steps) | Existing medium and large vessels are mass-checked against dry + contents and capacity-checked on load; a change makes them Protected | `BulkVessel.cs`, `StoredCommodity.Read` |
| Machine dry masses | Saved `StatMass` is compared on load (`ProcessorService.cs:46`, `SabatierService.cs:47`, `CrackerService.cs:47`, `Agriculture/Service.cs:104`); a change protects the machine, with an Accept path | Services |
| Salvage bills | Salvage mass equals machine mass; salvage value below a quarter of the price; damaged price = price/4 | `ManufacturingNativeChecks.cs:255`, `EquipmentValueAudit.cs`, `FeedFamilyNativeChecks.cs` |
| Loot carve shares | Share within the donor's remaining share; the C-class silicates donor is shared by Manufacturing and Shipbreaker | `LootCarveMath.Render` at publish time |
| Feed unit masses and stock masses | Must equal the native item masses and the recipe inputs; Agriculture's 0.04 kg nutrient charge, 19.5 kg irrigation target, 13 kg wet packet and 80/12/20 kg dry masses each appear in three to five places | Tests |
| Native mirrors: molar masses, R, 293 K, heater heat capacity 20.7, reactor bands, sensor thresholds, alarm condition names, install tab codes | Must equal game 1.0.1.5 values | `Available` checks, native tests |
| Settings ranges | Must equal the saved-record validity ranges (`CoastSettings.IsValid`, `FlightPreferences`, crop pace bounds) | Tests |

### 2.3 Logic (keep in code)

Persistence record keys and schema numbers, field allow-lists, port names, guard
and journal names, condition and interaction identifiers, definition prefixes and
the four-form suffixes, state machines, transfer guards, hazard rules, the carve
renderer, the ladder formulas, geometry derivations, physics constants, console
verbs, Harmony patch targets, native prefab and widget paths. These are contracts
with saved games or with the game binary, not tuning.

## 3. Symptoms the audit found

- **The constants catalogue is a regex patch tool over C#.** Of its 168 targets, 93
  point into 39 `.cs` files; every one of its 81 numeric constants targets a C#
  literal. Several patterns are loose (`\bPrice\s*=` matches a local constant in
  `Shipbreaker/EquipmentEconomy.cs:141`; the methane store price pattern anchors on
  a neighbouring literal). The catalogue exists because the data has no home of its
  own. With data files, its numeric entries become plain JSON edits and the
  catalogue shrinks to versions and documentation copies.
- **Duplication.** The regional factor table is written four times. Agriculture's
  formulations are in `Crop.cs` and again as literals in `NutrientSolution.cs:19`.
  Section masses and work seconds are in `framework/recipes.json` and in
  `AssemblyDefinitions.cs`. Shipbreaker's `DependencyContract.cs:35-37` repeats the
  recipe ids from its own `recipes.json`. Prices set in `Content.cs`,
  `CollectorDefinitions.cs` and `IntakeDefinitions.cs` are overwritten by
  `EquipmentEconomy.Apply`. Framework repeats the overlay colour six times, the
  assembly hash twice, 293 K twice, the visit limit 4096 twice and the `Phobos'`
  prefix twice.
- **Hand-maintained mirrors of C# outside C#.** `scripts/install-mods.ps1` carries
  about 163 per-version required-file entries whose image stems duplicate the C#
  art names (and the Auto Nav list omits three faceplates that only the build
  checks by hash). `tests/PhobosNative.Tests/Program.cs:148` pins Shipbreaker at 93
  objects and 210 installables. `EconomyChecks.cs:184-191` pins Auto Nav prices and
  offer counts. Each new machine touches all of these.
- **Every current value that matters is tested by literal.** Around forty test
  files pin values that would move; they will need to read the data instead.
- **Save sensitivity is real but bounded.** Record keys, ids and ports must never
  move. Recipe revisions must stay immutable once published. Dry masses, capacities
  and per-cycle quantities can move, but the existing Protected-plus-Accept path is
  what a player would meet after editing them.

## 4. Where each of the owner's four aims is served

| Aim | Served by |
| --- | --- |
| Reusability | One Framework loader for every schema; shared tables (regional factors, bill materials, size ladder parameters) declared once in Framework data and referenced by content |
| Ease of maintenance | Values live once; the constants catalogue loses 81 regex targets; installer and test counts derive from the data instead of being retyped |
| Less work per new implementation | A new machine of an existing kind becomes rows in `economy`, `equipment`, `materials` and `recipes` files plus its service; the definition shaping, economy, stock, regional and loot registration run from data |
| End-user editing | The War Declared override pattern (`BepInEx/config/<Mod>/<schema>/*.json` overriding shipped files by key) extended to prices, lots, chances, recipes and capacities, with validation that reports rather than crashes |

## 5. Recommendation

### 5.1 Two layers, one loader

1. **Native JSON for what the game already loads** (conditions, triggers,
   explosions, overlays, GUI prop maps, later asteroid and cluster blueprints).
   Already the practice; keep it.
2. **Phobos data packs for our own tables**, loaded by one Framework service
   (working name `Registration.DataPacks`) that generalises `ConstructionRegistry`:
   - a pack is `mods/<Mod>/framework/<schema>.json` with `schemaVersion`, an
     `owner` and a list of keyed entries; shipped packs are embedded resources and
     also present in the mod folder as the readable copy;
   - typed DTO per schema, `MissingMemberHandling.Error`, depth and size caps,
     duplicate keys refused (the `ConstructionRegistry` settings);
   - a validator delegate per schema (mass balance, stoichiometry, native
     references, ladder bounds), run before commit; the C# validator is
     authoritative;
   - player overrides in `BepInEx/config/<Mod>/<schema>/*.json`, merged **by entry
     key**: an override may change a shipped entry's tunable fields or add a new
     entry, never rename or remove a shipped id; a file that fails validation is
     reported on the F3 console and in the log and skipped, never fatal (the
     `Schematics` behaviour);
   - all-or-nothing commit per owner, per-owner status on `phobosframework status`.

   JSON with `//` comments allowed (Newtonsoft reads them) so shipped files can
   carry the same authored-assumption notes the C# comments carry today.

### 5.2 Schemas, in the order they pay off

| Schema | Content | Validator | Player-editable fields |
| --- | --- | --- | --- |
| `economy` | Per family: price, work, repair bill, salvage, broken salvage, Restore minutes, loot flag; merchant offers; lot quantities; chance floors; regional factors (Framework-owned table, content references it) | Salvage mass = dry mass; damaged price rule; bill arrays aligned; merchants and items exist | Prices, chances, lots, factors |
| `recipes` | Refinery charges, furnace recipes, thaw recipes, feed families, reclaimer, X2/K2/AX-2 cycles: inputs, products, off-gas, seconds, revision, thermal profile | Mass conservation; native species; duration bounds; **revision freeze** (below) | Add new revisions; change seconds and kW within bounds |
| `vessels` | Gas store, silo, bin and reservoir families: prefix, commodity, species, capacity, dry mass, price, leak, record names, model letter | Ladder bounds; record names present; commodity known | Capacity, price (with the Protected warning documented) |
| `materials` | Stock, remainders, chunks, coolant, packets: id, mass, price, stack, category, terminal, art, mined | Mass matches recipe inputs; terminal items priced at the minimum | Price, stack |
| `loot` | Carve shares, salvage chances, fridge and crate chances | Share within donor; tables exist | Shares and chances |
| `equipment` | Definition shaping per family: footprint, install tab, map points, art, containers, power points, cumbersome | Points used by code exist (cooling sockets, material approaches); art files exist | None at first |
| `settings` | Defaults and ranges | Ranges equal record validity ranges | Through BepInEx config as today |

The revision freeze: every published recipe revision carries a content hash in a
Framework-owned `config/frozen-revisions.json` regenerated by the build; the loader
refuses a shipped or override entry whose revision is frozen but whose hash differs,
and requires a new revision number instead. That preserves the current guarantee
(a sealed charge keeps its products) without capturing products into every save.

### 5.3 What the tooling becomes

- `scripts/update-constants.py` gains a JSON-pointer target type and loses its C#
  regex targets as each value moves; the catalogue keeps versions, dependency
  minima and documentation copies.
- A Python mirror of each validator (`scripts/validate-data.py`) runs in CI without
  game files; the C# loader remains the truth at runtime. JSON Schema files in
  `schemas/` give editor completion for players and maintainers.
- The installer's required-file lists are generated from a package manifest the
  build writes, not typed by hand.
- Tests read the loaded packs (counts, prices, masses) instead of pinning literals;
  the invariants move into validators that the native test suite runs.
- The item reference generator reads ids, prices and masses from the packs; the
  reviewed prose stays in `config/item-reference.json`.

## 6. Disagreements and cautions

1. **Not everything should move.** Identifiers, record keys, ports, condition names
   and native mirrors are contracts. Putting them in an editable file makes a
   save-breaking rename a one-line accident. They stay in code; data entries refer
   to them by key. Where a data row must introduce an id (a new material), the
   loader forbids renaming or removing shipped ids, and a per-release
   `config/saved-identities.json` lets CI refuse a release that drops one.
2. **Guidance tunables are policy, not data.** Auto Nav's avoidance margins, scoring
   weights and braking reserves (`NavigationAvoidance.cs`, `PredictiveGuidance.cs`)
   are magic numbers that should be *named*, not externalised: a player editing them
   gets a ship that flies into things, and no validator can tell a bad value from a
   good one. The same holds for furnace interlocks and hazard thresholds.
3. **UI theme is low value.** Colours and row heights could be a theme file, but
   nobody has asked for it and it buys no maintenance. Deduplicate the six overlay
   colours into one Framework constant and stop.
4. **End-user editing meets the chemistry rule.** A player can write a recipe that
   makes mass from nothing. My recommendation is that mass conservation and native
   species are enforced for every file, shipped or player, because the loader
   cannot run a machine that violates them; stoichiometric honesty and pricing
   remain authoring rules for shipped data and are not enforced on player files.
   The owner should confirm this split (question 8.3).
5. **Do not migrate in one move.** Each schema should land with one pilot mod,
   verified by a golden comparison: `docs/item-reference-data.json` is already a
   full export of every prepared definition, so a migration is correct when the
   export is byte-identical before and after. Manufacturing is the right pilot: it
   is the newest, has the fewest legacy saves, and has every kind of table.
6. **Save protection on edited values is a feature, not a bug.** A player who
   changes a store's capacity will find existing stores Protected with an Accept
   button. That is the designed behaviour and the guide must say so; the
   alternative, silently rescaling saved contents, is worse.
7. **The cost is mostly in tests and tooling, not in the loader.** Forty test files
   pin values, and the build, installer, item reference, constants catalogue and
   performance audit all touch the moved files. Budget the migration by these
   surfaces, not by the size of the data.

## 7. Proposed order of work

Each step is one commit set with its own checks; numbers are the next free
versions at the time and will move.

1. **Framework: `DataPacks` loader** with the `economy` schema and its validator,
   the JSON-pointer target type in the constants updater, the Python validator, and
   the golden-export check. Migrate Manufacturing's economy, stock, regional and
   loot tables. Prove the export is unchanged.
2. **`recipes` and `materials`** with the revision freeze; migrate Manufacturing's
   charges, cycles and materials, then Shipbreaker's furnace, thaw, feed families and
   reclaimer. Tests read the packs.
3. **`vessels`**; migrate the gas store families, silos, bins and reservoirs, with the
   Framework ladder parameters declared once.
4. **`economy` for Shipbreaker, Agriculture and Auto Nav**; the four regional tables
   collapse to one. Retire the matching catalogue regex targets.
5. **`equipment`** per family as families are touched, not wholesale; the geometry
   validators come with it.
6. **Tooling closure**: generated installer manifest, derived test counts, item
   reference reading packs, player documentation for overrides (a short "editing the
   data files" guide with the protection behaviour and the validator messages).
7. Resume the feedstock programme with round three authored directly in the packs.

Steps 1 and 2 are the ones that change how new work is written; the programme
should not resume before they land.

## 8. Decisions needed from the owner

1. Approve JSON packs under `mods/<Mod>/framework/` with player overrides in
   `BepInEx/config/<Mod>/`, merged by entry key (section 5.1).
2. Approve the revision freeze as the way to keep published recipes immutable
   (section 5.2), instead of capturing products into saves.
3. Confirm which validators apply to player files: mass conservation and native
   species always; stoichiometry and pricing for shipped data only (section 6.4).
4. Confirm the pilot order (Manufacturing first) and that the feedstock programme
   waits for steps 1 and 2.
5. Confirm that identifiers, record keys, ports and native mirrors stay in code
   (section 6.1), and that guidance tunables and UI theme are excluded (6.2, 6.3).

## 9. Progress

The owner approved every recommendation on 30 September 2026 and confirmed the
validator split (decision 3). Steps land as one commit set each, on `main`:

| Step | Delivered | Versions | Commit |
| --- | --- | --- | --- |
| 1 | `DataPacks` loader, `economy` schema, pointer targets, Python validator, golden export; Manufacturing economy | Framework 0.49.0, Manufacturing 0.11.0 | 5c0ab38 |
| 2 | `process-recipes` and `materials` with the revision freeze; Manufacturing charges and materials; Shipbreaker furnace, thaw, reclaimer and packets | Framework 0.50.0, Manufacturing 0.12.0, Shipbreaker 0.46.0 | 1d39d7a |
| 3 | `vessels`: gas store families, S3 silo, Y2 bin, R3 reservoir; ladder kept in code | Framework 0.51.0, Manufacturing 0.13.0, Shipbreaker 0.47.0, Agriculture 0.23.0 | 8a29426 |
| 4 | `economy` for Shipbreaker, Agriculture and Auto Nav; schema generalised (sections, item forms, offer scale, named lots, regional items, explicit loot items, remainder salvage); shared `EconomyStock` replaces four `StockQuantities`/regional copies; 24 catalogue regex targets retired | Framework 0.52.0, Manufacturing 0.14.0, Shipbreaker 0.48.0, Agriculture 0.24.0, Auto Nav 0.30.0 | ff8c68a |
| 5 | `equipment` schema (footprint, mass, idle and working power, room-heat share, feed cells, art, install tab, connection points; read-only to player files) with the V4 as its first entry; recipes gain `circulates` and `reactionKWh`; shared `CommoditySettlement`; the V4 service becomes the shared charge-machine engine the round-three machines use | Framework 0.54.0, Manufacturing 0.17.0 | see Manufacturing 0.17.0 |
| 6 | Tooling closure: JSON Schemas in `schemas/` with a drift test and a built-in checker; build-written `phobos-package.json` checked by the installer; Agriculture `materials` pack (19 items, model-bound masses); six more catalogue targets retired | Agriculture 0.25.0 | 4ba83ea |
| 7 | Feedstock programme round three in the packs | | pending |

Step 3 differs from the table in section 5.2 in two places: the ladder parameters
stay in `BulkVesselSizes` (saved medium and large vessels are checked against
them), and prices stay in the economy pack rather than being repeated in vessels.
The W2 supply is not a vessels entry because its 20 kg is `CropState.ReservoirKg`.

Step 5 keeps the equipment pack read-only to player files for now: a footprint or
connection point is part of every placed machine in a save, so an overlay that differs
from the shipped entry is refused with a message rather than applied. Opening chosen
fields (power, heat share) to player files is a later, separate decision.

Step 6 left two items of section 5.3 as they are, deliberately: tests that pin a
shipped value (the S3's 1,000 kg, the ingot prices) stay as regression guards
against an accidental edit, and the item reference keeps reading the live native
definitions, which now come from the packs, rather than the packs themselves.

Step 4 kept the four regional tables per mod (they differ) rather than collapsing
them, and left Agriculture's food, seed and nutrient item prices in code: those are
a `materials` pack for Agriculture, listed for step 6 rather than mixed into the
economy schema. Ladder increments (silo, bin, reservoir, gas store) stay in code.

## Sources

The four inventories behind this record were produced on 30 September 2026 by
reading the source tree; their per-line tables are summarised in section 2 and
their notable rows cited inline. Related records: [framework author guide](framework-author-guide.md),
[updating constants](updating-constants.md), [item reference maintenance](item-reference-maintenance.md),
[War Declared design](war-declared-design.md) (the override precedent),
[vanilla precedence audit](vanilla-precedence-audit.md).

## Follow-up: outcome tables (4 October 2026)

Owner direction on the gangue wash plan: chance tables are data a player may edit or add to, apart from the code. Framework 0.88.0 adds the `outcomes` schema (`Data/OutcomePack.cs`): a table per base recipe, weights by outcome recipe id, merged by key from `BepInEx/config/<Mod>/outcomes`. The outcomes themselves stay in the frozen process-recipes pack, so mass conservation and the revision freeze apply to them unchanged; the table is not frozen, because odds are balance and a bound charge has already saved its result. Enforced on every file: outcomes share their base's machine, inputs, circulating volumes and duration; whole-number weights from 0 to 10,000; some weight left; a recipe in at most one table. Shipped-only authoring rule: the expected return over a table. The code holds a generic stable pick and no table.

## Follow-up: add-ons players publish (4 October 2026)

Owner direction: players may publish their own changes, additions and fixes on Steam Workshop, beyond editing data packs for personal use. Framework 0.90.0 adds `Data/AddOns.cs`: an add-on is an enabled game mod folder with a `phobos-addon.json` manifest (id, name, author, version, id prefix, required mod versions) and override files under `phobos/<Mod>/<schema>/`, in the local files' own format. `DataPacks.LoadText` reads them through the same merge and validation as local files. Order: shipped pack, add-ons in the game's mod order, then the player's files; any file may set a `priority` header (-100 to 100, lowest first) to change that. An add-on may tune any entry and add only entries under its own id prefix. Recipes added without a `revision` get one derived from their id (`RecipeSchema.DerivedRevision`), so add-ons never collide on a machine's revision space. Enforced on add-on files exactly as on player files: mass conservation, native gas species, unknown fields, size limits. War Declared reads add-on schematics the same way. The offline checker is `scripts/validate-data-packs.py --addon`; the worked example in `examples/addons` is loaded by the unit and Python tests.

Framework 0.91.0 extends add-ons to text: `phobos/translations/<Mod>/<language>.json` is read between the packaged catalog and the player's own file, and may add keys under the add-on's id prefix (`TranslationOverlay.MayAdd`), which is how an added recipe gets its name. The planned `name` field on recipes was not needed.

Framework 0.92.0 with Manufacturing 0.45.0 opens the `materials` schema to additions where the owner allows it (`MaterialContext.AllowAdditions`; Manufacturing is the pilot, Shipbreaker and Agriculture still refuse them). An added entry carries `name`, `description` and `image`; shipped entries may not. Enforced on every file: the id shape and reserved prefixes, the picture path staying under an images folder, and the owner's rule that an added trash-category item is a terminal remainder (declared for the reaction mass feeder when definitions are built). The picture comes from the add-on's own `images` folder: the game's `DataHandler.LoadPNG` walks every enabled mod's `images/` folder for a name (seen in a local research copy of the game code, which stays out of the repository), so no Framework loader is needed. A machine's feed rule admits added items its recipes take, by their identity condition.
