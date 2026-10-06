# Phobos Framework 0.15.0 — author guide

Framework 0.40.0 adds `Processing.ReactorRules`, `IReactorPanel`, `IReactorState`,
`ReactorControls` and `NativeReactor`: facts about the game's fusion reactor with
native citations (0.27 s update cadence, ideal core 0.725, the course plot's 5 %
correction and 0.8–1.2 abort bands, wall damage above 0.75, the 2 s pilot flow
grace), read-only `Intact`/`Ready`/`IsNoWake`/`PilotTouchedFlow` reads, the vanilla
flow regulation (`InitialFlow`, `AdjustFlow`, hot side corrected from the
wall-damage temperature) and guarded flight-control writes that remember what
the owner commanded and the idle settings to hand back. The rules are pure over
`IReactorPanel`; `NativeReactor` wraps a `CondOwner`. Consumers keep policy: when to
burn, how hard, and what to do when the core leaves the band. First consumer:
Auto Nav 0.27.0 ([torch guide](../auto-nav-torch.md)). Nothing here ignites,
repairs or refuels a reactor.

Framework 0.28.0 adds `ApplianceDefinitions.ApplyStateArtwork`. Call it after
registering a complete Installed/InstalledDmg/Loose/LooseDmg appliance family.
Supply a content-owned image base plus Damaged, Loose and LooseDamaged exports
with matching Normal maps. It binds native images and portraits without changing
physical fields. Dynamic views must select the same registered states. See
[dedicated artwork coverage](artwork-completion.md).

Framework 0.27.0 adds [bulk custody, station settlement and compact provider selectors](framework-bulk-storage.md). Existing reservoir, equipment and crew interfaces remain compatible.

The current console redesign uses compact views and checked drafts. See the
[control-panel guide](../control-panel-guide.md) for Apply/Discard, storage selection,
ship picking, training and the separate Unity validation checklist.

Framework 0.21.0 introduced [shared completion cues](../shared-completion-cues.md).
Framework 0.21.1 adds `Persistence.SavedGridBounds.TryPlan`: a pure, bounded
expansion calculation for [saved-grid restoration](shipbreaker-room-load-mitigation.md).
It rejects shrinking, fractional offsets and geometry outside the saved bounds.
Content owns activation and native application before room/zone loading;
Framework does not apply general room-grid restoration. Framework **0.23.1**
additionally scopes a saved-marker health correction to native `Ship.SpawnItems`:
`Ship.IsItemDestroyed` must not discard an identified, living saved construction
marker using the generic `Placeholder` zero maximum when its deterministic saved
conditions prove remaining health. Resolve saved COs from `DataHandler.dictCOSaves`
(native loading has already cleared `ship.json.aCOs`), bind full IDs to a unique
saved placeholder and require both target definitions. Never edit damage or
progress. Templates, missing providers, ambiguous records and genuinely exhausted
health retain native behavior. Nested and failed load scopes unwind through a
Harmony finalizer. See the linked investigation for read-only save audits and limits.

Framework **0.24.1** also corrects the inspected native save-trimming order.
`Ship.GetJSON` captures dimensions before trimming but serializes origin/rooms/zones
afterwards. A postfix synchronizes outgoing full-game-save dimensions with the
post-trim live count and origin. A separate `Ship.InitShip` prefix validates already
inconsistent headers through `Persistence.SavedRoomGrid`: require a unique smaller
grid, complete exterior boundary and every saved Compartment item's position in
its own room. Correct dimensions before `SpawnItems` and any padding consumers;
never remap tiles or replace gas records. Templates, shallow-only calls and
ambiguous records stay native. Read-only `--audit-room-grid` native-test mode and
the incident guide describe the supplied before/after saves and owner checks.
Content owns a transient `Audio.CompletionWatch`, arms it after access checks with
actor/ship IDs, and calls `Audio.CompletionCues.Complete` only after a real committed
result. Cancel on stop, suspension, faults and reload. Use `CompletionCues.VolumeLabel`
and `CycleVolume` for presentation; never infer completion from panel reads or save a watch.

## Optional performance recording (0.15.0)

Framework owns the shared Phobos Scope recorder and capture lifecycle. Register
stable handles once during plugin initialization through
`Phobos.Ostranauts.Framework.Diagnostics.Performance.RegisterOperation(name, category)`.
Wrap a coarse synchronous service operation with
`using var timing = Performance.Measure(handle);`. The disabled path returns
an empty scope; do not create per-call names or start recording from content code.

`RegisterIncrement(name, category, unit)` and `Increment(handle, value)`
record explicitly defined counts. `RegisterContext(name, provider)` samples
small string values only while recording and stores initial/changed values.
Keep providers cheap and side-effect free. Use the main thread, properly nested
scopes and no `await` inside a scope. Timing is inclusive real elapsed time.

Consumers require Framework 0.15.0 and must not bundle another recorder DLL.
Instrumentation stays in the owning service; UI files only measure presentation
work. See [capture commands, metrics and owner-run checks](../performance-captures.md).

## Data packs (0.49.0)

`Data.DataPacks` is the one loader for authored tables that live outside C#
(owner decision, 30 September 2026, from the [schema separation audit](schema-separation-audit.md)).
A pack is `mods/<Mod>/framework/<schema>.json`, embedded in the plugin as
`<Mod>.<schema>.json` and kept in the mod folder as the readable copy. Players
override it with partial files in `BepInEx/config/<Mod>/<schema>/*.json`.

- Declare a typed pack: a class deriving from `DataPack` (which carries
  `schemaVersion`, `schema` and `notes`) with public lowercase fields, dictionaries
  keyed by the identifiers the code already uses. Unknown fields are refused
  (`MissingMemberHandling.Error`), as the construction registry refuses them.
- Load with `DataPacks.Load<T>(new DataPackSource(owner, modFolder, schema,
  assembly, resource), validate)` during content preparation. `validate` runs on
  the shipped pack (a failure throws: a packaging fault) and on every candidate
  player overlay (a failure rejects that file and keeps the earlier ones).
- Player files are merged by property name at every level: objects merge, arrays
  replace, a `null` is refused, so a file can tune fields of a shipped entry or add
  an entry and can never rename or remove one. Files apply in name order.
  `DataPacks.LoadText` does the same from text and a folder, for tools and tests.
- Problems are logged, kept in `DataPacks.Problems` and shown by
  `phobosframework status`. The shipped copy is always valid on its own.
- Write the schema's checks in Framework beside its DTO so every mod shares them.
  The rule (owner ruling): mass conservation and native gas species are enforced
  on every file, shipped or player; stoichiometric honesty and pricing rules are
  authoring rules for shipped data and are not enforced on player files.
- `scripts/validate-data-packs.py` mirrors the structural checks for CI without
  game files; the C# validator is authoritative.
- A migration of existing values into a pack is verified by the golden export:
  `scripts/compare-item-export.py` shows `docs/item-reference-data.json` unchanged
  before and after, apart from version strings and source hashes. Constants registered in the
  maintained catalogue point at pack values with `pointer` targets
  ([updating constants](updating-constants.md)).

The first schema is `economy` (`Data.EconomyPack`, `EconomySchema.Validate`):
per-family price, work, repair bill, salvage, Restore minutes and flags; supplies;
offer templates and explicit offers; region factors and regional stock; lots and
chance floors; world loot. Its checks: every family the code names has an entry
and no other exists, positive prices and work, bills of known materials, salvage
that weighs what the machine weighs, known conditions, merchants and regions,
chances within (0, 1] and lots within 1..256. Manufacturing 0.11.0 is the first
consumer; identities and dry masses stay in its rules and the pack is checked
against them. Consumers require **0.49.0**.

Framework 0.52.0 generalised the schema for the other mods and moved the shared
stock logic into `Data.EconomyStock`: equipment entries carry `kind` (`equipment`
or `section`, an assembly part sold whole with only a price, dismantle work and
salvage), `forms` (`machine`, `item` for an id and its `Dmg` twin, `single`),
`offerScale`, `regionalChance`, `salvageRemainder` (the owner adds a remainder
carrying the rest of the mass, so salvage may weigh less, never more) and named
`lot`/`floor`; supplies carry `expanded`, `regionalChance` and `regionalCondition`;
`regional.items` lists loose commodities offered in every region; `lots` and
`chanceFloors` are free-named tables every entry must point into; a world-loot entry
may name `items` explicitly or several `tables`. `EconomyStock.Classify` resolves an
item to its lot and floor (exact regional items and single/item forms first, then
the longest supply or machine prefix); `AddOffers`, `ApplyRegional` and
`AddWorldLoot` apply the pack against the owner's `EquipmentSale` list (every size
of a ladder against its small entry, larger sizes never loot), and `RepairInputs`
turns a bill into the game's requirement triggers. Owners keep only their ladder
increments, remainder naming and category flags. Consumers: Manufacturing 0.14.0,
Shipbreaker 0.48.0, Agriculture 0.24.0 and Auto Nav 0.30.0 require **0.52.0**.

Tooling around the packs: `scripts/write-json-schemas.py` writes the JSON Schema
files in `schemas/` from the same field sets the Python validator uses (run it
after changing a schema; `--check` and `tests/test_data_packs.py` refuse drift),
and `scripts/check-json-schemas.py` validates every shipped pack against them with
a small built-in validator. `New-PhobosPackage` writes `phobos-package.json` into
each package's native folder (id, version, every shipped file); the installer
checks a package against its manifest when present, so new files no longer need a
version-gated line in `install-mods.ps1` (the hand lists stay for older packages).
Agriculture 0.25.0 moved its loose items into a `materials` pack whose masses the
crop, recovery and workup models depend on are bound to their constants in the
loader, the pattern for any pack value a formula in code is written for.

### Process recipes and materials (0.50.0)

`Data.RecipePack` (`process-recipes`) holds fixed recipes keyed by id: machine,
revision, inputs, products (items or commodities such as `water`), `offGas` by
native species, optional `seconds`, `legacySeconds`, `melt`, `requires` keys and a
furnace `thermal` profile. `RecipeSchema.Validate` enforces, on every file: mass
conservation within `Units.MassToleranceKg`, only the game's room gases as off-gas,
positive units whose kg match the known item mass, unique ids, one revision per
machine, duration bounds, known machines and requirement keys, and a sane thermal
profile. Formula catalogs (the D4 feed families, the wall budget) stay in code.

Published revisions are frozen. `scripts/freeze-recipes.py` writes each mod's
`frozen-process-recipes.json` (embedded beside the pack) with a SHA-256 of every
`machine@revision` over the entry's JSON with `notes` removed, keys sorted and no
whitespace; `RecipeFreeze.Enforce(rawPack, frozen)` runs inside the loader's
raw-aware validator (`DataPacks.Load<T>(source, Action<T, JObject>)`) and refuses a
frozen revision that changed or disappeared, shipped or player. To change a recipe,
add a revision and run the freezer; CI runs `freeze-recipes.py --check`. The
Python and C# digests are asserted equal on a fixed sample in both test suites.

A content mod may define its own schema: any `DataPack` subclass loads through
`DataPacks.Load<T>` with the owner's validator. Agriculture's `crops` schema
(`PhobosAgriculture.Core.CropPack`, Agriculture 0.40.0) is the first. Its entries are
keyed by the name a saved planting stores, so the same freezer writes
`frozen-crops.json` keyed by crop name and `CropFreeze.Enforce` reuses
`RecipeFreeze.Hash`. A mod-owned schema still needs its validator in
`scripts/validate-data-packs.py`, its JSON Schema in `scripts/write-json-schemas.py`
and a row in the player editing guide.

`Data.MaterialPack` (`materials`) holds a mod's loose items by definition id: kind
(how the owner builds it), kg, price, stack, side, category, terminal flag and art.
`MaterialSchema.Validate` requires an entry for every id the owner names and no
others, with positive mass and price and bounded stack and side. Owners add their
own checks in the validator lambda (Shipbreaker binds reject and packet masses to
the budgets written for them; Manufacturing feeds the material masses into the
recipe check so a mass edited on one side is refused on the other).

First consumers: Manufacturing 0.12.0 (six V4 charges, seven materials) and
Shipbreaker 0.46.0 (three F6 recipes with thermal profiles, two T2 recipes, the R4
budget; thirteen packets, ingots and rejects). Consumers require **0.50.0**.

### Vessels (0.51.0)

`Data.VesselPack` (`vessels`) holds the small size of every bulk vessel or bin
family a mod ships, keyed by definition prefix: `kind` (how the owner builds it),
`commodity`, `capacityKg`, `dryKg`, `leakKgPerHour` and, for item-grid bins,
`cellsPerTileSide` instead of a commodity and capacity. `VesselSchema.Validate`
requires an entry for every prefix the owner names and no others, positive
capacity and dry mass within bounds, a non-negative leak rate and the right field
set for the kind (`VesselContext.ItemKinds`, `bin` by default). Owners add their own
binding in the validator lambda: Manufacturing refuses a commodity that differs
from the family's code, Framework pins its S3 tank to `water` (Shipbreaker did, before 0.58.0) and Shipbreaker pins the bin to its kind.

What stays in code: identities, record, journal and guard names, species, model
letters, prices (economy pack) and the size ladder (`Liquids.BulkVesselSizes`),
because saved medium and large vessels are checked against it. Read the pack
through properties, not at static initialisation: a `BulkVesselSpec` built from it
should be rebuilt when `Pack` changes identity (see `GasStore.Spec`), so a reload
after `DataPacks.Reset()` is not served stale ratings. A capacity or dry mass that
no longer matches a saved vessel leaves it **Protected** until the owner accepts it
through the existing `BulkVessel` path; the pack does not hide that, and the
player guide says so.

First consumers: Manufacturing 0.13.0 (six gas store families), Shipbreaker 0.47.0
(the S3 silo and the Y2 bin) and Agriculture 0.23.0 (the R3 reservoir; the W2 supply
stays in code because its 20 kg is the rack reservoir the crop model is written
for). Consumers require **0.51.0**. Since 0.58.0 the S3's entry is Framework's own
(`mods/PhobosFramework/framework/vessels.json`), and Agriculture's R3 entry only feeds
the conversion of saved reservoirs.

### Equipment, recipe working volumes and commodity settlement (0.54.0)

`Data.EquipmentPack` (`equipment`) holds a machine's physical figures by definition
prefix: `kind` (how the owner builds it), `footprint` (tiles per side), `massKg`,
`idleKW`, `workingKW`, `roomHeatFraction`, `feedCells`, `art`, `installTab` and named
connection `points` (x, y in native pixels). `EquipmentSchema.Validate` requires an
entry for every prefix the owner names and no others, ranges for each figure and a
known kind and install tab. The pack is **read-only to player files**: pass the
shipped entries as `EquipmentContext.Baseline` (read the shipped text with
`DataPacks.ShippedText(source)`) and any overlay that differs is refused with
`EquipmentSchema.read_only`, because a footprint or point change would move
equipment already placed in a save. `EquipmentSchema.MapPoints(entry)` formats the
points as the native `name,x,y` list.

`RecipeEntry.circulates` names commodities (by the owner's `IsCommodity`) a recipe
needs on hand but returns, such as a wash water volume; they are outside the mass
balance and may not be item ids. `reactionKWh` is heat the reaction itself releases
into the room over the charge (negative when absorbed), bounded by
`RecipeSchema.MaximumReactionKWh`. Entries without either field hash exactly as
before, so frozen revisions are unaffected.

`RecipeEntry.supersedes` (0.68.0) lists earlier revisions of the same machine the
recipe replaces for new charges. A frozen revision can never change or disappear,
so this is how a recipe is retired: the owner's catalog leaves a superseded
revision out of what it offers, and a saved job bound to it still resolves by
revision. The schema refuses a revision that is not earlier, does not exist on the
same machine, or is superseded twice. Manufacturing's `ChargeRecipeView.Available`
is the reference consumer; a superseded recipe returns when its replacement's
requirements are not met.

`Liquids.CommoditySettlement` settles a finished charge's commodities against bulk
vessels. `SettlementPlan.Build(legs)` nets the draw, deposit and circulate legs per
vessel (a vessel drawn from and deposited into changes by the difference);
`SettlementPlan.Check(need, snapshot, held)` refuses Protected, Busy (a reservation held),
Catch (catch chamber in use), Short (contents below the draw plus circulating
volume) and Full (headroom below the net deposit). `CommoditySettlement.Commit`
opens one conversion journal per changing vessel, commits the item delivery
(`IBatchDelivery`), then applies draws before deposits and closes the journals; a
refused delivery closes the journals and changes nothing. It is not crash-atomic: an
exception after the delivery leaves the remaining journals open as evidence and those
vessels Protected until their owner accepts them. Pure planning is covered by
the Framework tests; the live commit is used by Manufacturing's charge machines.

First consumer: Manufacturing 0.17.0, whose V4 now runs on a shared charge-machine
engine (`ChargeMachine` per family, a `ChargeMachineSpec` for identity, texts, recipe
selection, requirement gates, spoil policy and commodity links). Consumers require
**0.54.0**.

### Story packs (0.107.0)

A mod adds TV news, adverts and story arcs with a `story` pack: ship
`mods/<Mod>/framework/story.json`, embed it, and register it once in `Awake`:

```csharp
Phobos.Ostranauts.Framework.Story.StoryContent.Register(new DataPackSource(
    Text.Owner, "<Mod>", StorySchema.Name, typeof(Plugin).Assembly, "<Mod>.story.json"));
```

Framework loads every registered pack on each game load, after all mods have published
their items, and refuses a reused id or an unknown item, condition, arc or bulletin
entry by entry. Translations of pack text use `Story.<id>.<field>` in the owning mod's
catalogue. Never use the game's plots, pledges, new social interactions or player
conditions for added story content. The file format is in
[Writing story content](../writing-story-content.md); the design and save footprint
in [the story system design record](story-system-design.md). Consumers require
**0.107.0**. First consumer: Agriculture 0.60.0.

**Story services for other mods (0.127.0).** Content that is not a story pack can still be
local, gated and remembered the way story content is:
`StoryLocation.Region`, `DockedPlace` and `Near(place)` say where the player is;
`StoryGates.Blocked(requires, thread)` checks a story `requires` block against the live game
and returns the first reason it fails, or null; a pack of your own may carry such a block,
checked with `StorySchema.ValidateRequires(requires, where)` when it loads and with
`StoryContent.Library.UnknownReference(requires)` (and `UnknownPlace`, `UnknownPerson`) once
the story library is built, in `FrameworkLifecycle.ContentLoaded`; `StoryFlags.Has`, `Set`
and `Clear` mark what happened for story packs to react to; `StoryArcs.TryBegin(arc, out
message)` starts an arc only when its requirements hold. `GameClock` holds the game's
calendar: its 87,658.125-second day, shifts as the game numbers them and `ShiftCount` for
the shift changes between two times. First consumer: Phobos Banking 0.2.0.

## Carved loot shares (0.48.0)

`Registration.AdditiveLoot.CarveChoice(definitions, tableId, donorId, choiceId, share)`
gives a new choice part of one existing unit's probability in a native loot table,
instead of adding a roll (owner loot policy, 30 September 2026). The choice is
inserted immediately after the donor in the same cumulative expression and the
donor is reduced by the same amount, so every other unit keeps its exact band and
the table's total never changes. Use it for new mined chunks and for asteroid
types: it accepts `item` tables and `ship` tables (the `RandomAsteroid*`
field pickers), where an appended `aLoots` branch can never be chosen because the
field generator takes the first result.

- The donor must appear exactly once, in a positive expression. Preparation fails
  when the table, donor or choice is unknown (a ship table's choice must be a ship
  or asteroid cluster blueprint) or the share exceeds the donor alone.
- Several sets may carve one donor. `LootCarveRegistry` keeps each table's
  original expressions and renders every carve on it together, in ordinal choice
  order, so publication order does not matter. A carve the donor can no longer
  cover is refused and logged; the rest still apply.
- A zero share restores the donor. Republishing is idempotent.
- If another mod rewrites the table after our carve, the carve is logged and
  skipped rather than overwriting that edit.
- `NativeDefinitions.LootCarves` records each set's carves by table and choice;
  the item-reference exporter lists them as acquisition sources.
- Carves change future rolls only: saves and already generated asteroids keep
  what was rolled. Donor budgets shared across mods (such as C-class silicates)
  are named constants in their owning content, not player tuning knobs.

Manufacturing 0.8.0 is the first consumer (clay hydrates from C-class silicates).
Consumers of this API require **0.48.0**.

## Additive item loot (0.14.0)

`Registration.AdditiveLoot.SetItemChoice(definitions, tableId, branchId, chances)`
adds an optional, mutually exclusive item choice to an existing native item loot
table. Supply a stable `Phobos`-prefixed branch ID and item-ID-to-probability map;
finite nonnegative probabilities must sum to at most one. Zero choices disable
the owned addition. Identifiers accept letters, digits, underscores and dots.
The branch ID must differ from its parent table ID.

The helper publishes only your branch as one cumulative native choice
expression; each added branch yields at most one item. The native table gains
(or, for zero choices, loses) one standalone link to that branch in place when
the set publishes, through `NativeDefinitions.Amend`, so the game's own table
object and every native or foreign entry stay (0.36.0; earlier versions cloned
and republished the table under its own name). `NativeDefinitions.LootBranches`
records the branches each set links per table, which `MarketStock.AddMissing`
uses to avoid duplicate offers. Publish using the existing `NativeDefinitions`
transaction after all content has prepared. It never restocks merchants or
rewrites inventories. Missing/non-item parent tables fail preparation rather
than silently inventing a replacement pool.

`MarketStock` now uses this same helper while retaining its condition hooks and
availability scaling. Auto Nav 0.10.0 is the salvage consumer: content owns table
selection, identities, balance and enablement. Prefer explicit native leaf pools
to avoid adding a second chance to both a composite parent and its child. Shared
pools can serve multiple generation contexts; do not promise derelict-only drops
without checking those callers. Consumers of this API require **0.14.0**.

## Shared observations (0.13.0)

`Observations.Observation` carries immutable source, ship and subject IDs,
kind/capability, units, a numeric value **or** `AlarmState`, native simulation
timestamp, validity and reason. `Assess(shipId, subjectId, now, maximumAgeSeconds)`
marks aged evidence stale and removes its value if scope or time moves backwards.
Unknown is nullable, not zero. Consumers must check `Validity == Current` before
using readings for automation; a retained stale/faulty value is historical only.

`NativeRoomAlarms.Kind(source)` recognises the supported native room alarm flags.
`Read(source)` observes native outputs without toggling signals or supplying
new concentrations. Its witnesses come from real native sensor updates and are
reset on world/content loading. Power, damage, ambiguous foreign sampling geometry,
unknown output and missing updates fail closed. The declared five-second age
limit uses simulation time. `HardwareProblem` and `MonitoredRoom` are available
for concrete built-in probes. Native queued lamp changes can lag an environment;
this is an output adapter, not an independent gas analyser.

These APIs confer no ownership or control authority. Check a captured
`Controls.ConsoleBinding` (with fresh native facts) or equivalent local access
before exposing values, and validate source/subject identities every read.
Read only the relevant host ship; avoid polling the whole world. Keep process
semantics, hardware capabilities, recipes and automatic responses in content
mods. Shipbreaker provides the first real consumer and source-linked stop evidence.
See [console observations](shared-console-observations.md) for behaviour,
limitations and tests. No generic provider bus, saved history or telemetry wiring
protocol is part of this initial API.

## Equipment name patterns (0.12.0)

Use `Localization.EquipmentNames` with content-owned brand/model metadata and
translated type/variant catalog entries. The four-argument `Translations.Register`
overload accepts an embedded naming-resource name. Normal UI and construction
lookups then receive the same formatted name, beginning with `Phobos'`. The
original registration overload and unregistered messages are unchanged.
See [equipment branding](equipment-branding.md) for the format and migration of
older full-name overrides. Consumers using this API require **0.12.0**.


## Versioned object state (0.11.0)

`Persistence.ObjectStateStore` wraps one namespaced entry in an object's native
`mapGUIPropMaps`. Construct it with the maps, a stable consumer name, the full
object ID and a positive schema version. `Read(out fields)` returns Missing,
Ready, Invalid, DifferentOwner or UnsupportedVersion. Validate the consumer's
payload even when the envelope is Ready. `TryWrite(fields)` publishes a detached
replacement only for Missing/Ready envelopes; it never repairs unreadable or
different-version data by overwriting it. `Clear()` is an explicit reset, not an
automatic recovery policy. Reads return detached, read-only snapshots.

Use on the native main thread. Keys allow letters/digits/dot/underscore/hyphen;
nonempty values exclude control characters, commas and equals signs because the
native property-map serializer uses string fields. Store stable IDs and invariant
numbers, not translated labels. No file I/O, timers, live-object cache, movement
authority, schema migration or automatic resumption is supplied by Framework.
The native save owns the record; consumers own validation and lifecycle decisions.

Auto Nav 0.5.0 is the first consumer. See [its saved-flight contract](../auto-nav-persistence.md)
for an example of hardware/ship binding, captured parameters, cumulative progress
and revalidation before controls resume. Existing material-port records keep
their original schema and pause behaviour. Consumers using this new API must
declare Framework **0.11.0** or later.

## Industrial controls (0.10.0)

`Controls.PanelFeedback.Additional(success, message, liveStatus)` (Framework
0.24.2) removes successful command feedback only when its complete lines already
appear in the fresh live status. Use it immediately after a command, then retain
the returned extra message rather than a stale status snapshot. It preserves
errors, distinct notices and responses about another endpoint; do not use its
text comparisons to decide gameplay state. CLI callers retain their full replies.

`Controls.ConsoleBinding` stores session-only console, ship and operator IDs.
Call `Check` with freshly resolved native identities, current player/owner and
hardware/operator readiness on **every action**. Identity changes permanently
end that binding; temporary power loss does not. The helper does not discover
ships, grant ownership or override a machine interlock. Shipbreaker illustrates
the native adapter in `ControlAuthority` and shared UI/F3 dispatch in
`IndustryService`. Keep cargo manipulation behind ordinary local access.

`EquipmentActivity` carries a typed status and display detail; never parse
translations to decide whether a device needs attention. `PanelWidgets` supplies
small runtime Unity UI/TMP label, button, search and vertical-scroll primitives.
The caller owns hosting, layout and disposal; button callbacks delegate to its
gameplay service. Framework does not depend on Shipbreaker artwork. There is no
universal third-party equipment registry, remote inventory or PDA overlay yet.

This experimental Ostranauts library supplies definition registration, native
mass-balanced construction, grid placement, production completion and physical transfers,
with exact-ID filters, item-bound transfer clocks and bounded grid route search.
It is independent of OCF/SWB, but is not a drop-in reader for OCF packs. A general
conveyor network, machine scheduling and saved transport jobs are not implemented.
## Build and install

From the repository root, with PowerShell 7 and the .NET SDK used by this project:

```powershell
./scripts/build-framework.ps1 -OstranautsPath '<local game folder>'
./scripts/install-mods.ps1 -Mods Framework -WhatIf
# Close the game before the actual update:
./scripts/install-mods.ps1 -Mods Framework
```

The prepared package is `dist/PhobosFramework-P0.zip`. It contains our assembly,
metadata and documentation; no game or loader binaries. Install one provider at
`BepInEx/plugins/PhobosFramework/PhobosFramework.dll`. The native metadata package
currently contains no gameplay definitions. The plugin's startup message reports
the version and implemented services. BepInEx 5 is required; OCF and Salvage
Workshop are not requirements of the framework itself.

Shipbreaker 0.2.0 is the first independent consumer. Building it also prepares the framework
package, and the installer selects the framework dependency automatically.
Neither package requires OCF/SWB.

F3 commands: `phobosframework status`, `phobosframework recipes` and
`phobosframework help`. These report the registry and allowed station IDs; they
do not mutate gameplay, hot-reload content or bypass construction.

## Reference one shared provider

Consumers compile against `PhobosFramework.dll`, targeting `netstandard2.1` for
the current game environment. Resolve reference paths locally and set
`Private="false"` in the assembly/project reference so that each content mod does
not distribute another provider copy. Declare a BepInEx dependency:

```csharp
using BepInEx;
using Phobos.Ostranauts.Framework;

[BepInDependency(FrameworkInfo.PluginId, "0.8.0")]
// Other normal BepInPlugin/BepInProcess attributes belong on your plugin here.
public sealed class MyPlugin : BaseUnityPlugin { }
```

This is a minimum version requirement, not certification of all future versions.
Public API changes in this experimental series must be documented and coordinated
with consumers. Namespace imports are:

```csharp
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;
```

## Native construction

Subscribe to `FrameworkLifecycle.ContentLoading` in `Awake`, and unsubscribe in
`OnDestroy`. During that callback, prepare/publish your owned item definitions,
then call `ConstructionRegistry.RegisterPack(yourPluginId, absoluteRecipePath)` or
`Register(yourPluginId, recipes)`. Register one complete pack per owner per load.
Use `ContentLoaded` to check `Ready(owner)` and `Status(owner)` before enabling
processing dependent on those recipes. Callbacks run on the native loading thread;
there is no hot-reload API. Clear consumer session state during `ContentLoading`.
Stations keep their own native definition object: the craft action is appended
to it in place after the pack commits (0.36.0), the way `Installables.Create`
appends the game's own actions.

Recipes use schema 1. This example assumes the named item definitions already
exist, with those explicit unit masses:

```json
{
  "schemaVersion": 1,
  "recipes": [{
    "id": "ExampleAuthorPlate",
    "name": "Plate",
    "description": "Join two kilograms of stock.",
    "stationIds": ["ItmTable01", "ItmTable02"],
    "optionalStationIds": [],
    "legacyActionIds": [],
    "ingredients": [{"trigger":"TExampleAuthorMetal","item":"ExampleAuthorMetal","count":2,"unitMassKg":1}],
    "outputs": [{"item":"ExampleAuthorPlate","count":1,"unitMassKg":2}],
    "workSeconds": 60,
    "range": 2
  }]
}
```

Use your own author prefix for IDs. Required stations must exist and be installed;
optional stations are used only when present. Both still require an undamaged,
installed physical station at completion. The selector uses exact allowed station
IDs, never all furniture. Native menus/hauling/work supply the player controls.

Every ingredient has an existing native trigger, exact item ID, count and unit
mass. All consumed units must contain no cargo. Ordinary material stacks are
accepted as individual units; `requireEmpty: true` additionally requires separate,
unstacked objects (used for assembly sections). Counts total at most 100 per
input/output side. Mass must balance, with explicit outputs for any waste.
Definition and runtime mass mismatches block work. A pack is at most 1 MiB/128
recipes, uses strict known fields, and rejects identity collisions before publishing.
No random outputs, substitutions, stock injections or blueprint unlocks exist.

Active action IDs are `PhobosCraft_` plus recipe ID. The provider creates its own
selectors and input/output loot. Do not register arbitrary actions under that
prefix. Legacy aliases are explicit saved-action migrations for your own recipes;
they must not conflict with any live provider. Never change a published bill in
place: a job a save already queued is checked against the recipe when it finishes.
Add the new bill under a new recipe ID and set `"retired": true` on the old one
(Framework 0.102.0); it stays registered so saved jobs finish, but no station offers
it. An ingredient may be a game overlay (such as a Polaris navigation module) when
its condition loot never names `StatMass`. Do not put this pack in OCF's
`crafting/recipes.json` or register it twice. Shipbreaker's active pack and empty
legacy migration stub demonstrate the transition.

Registration snapshots DTOs and publishes the whole pack and station menus
atomically. That guarantee **does not cover native crafting effects**. The latter
use the game's material removal/output path. Arrived inputs are rechecked, and
an in-memory completion gate prevents repeating effects on the same interaction.
A partial native failure or process crash is not rolled back or journalled.
Preserve existing saved recipe meaning when changing work time/materials; publish
a new recipe ID and a deliberate migration for incompatible changes.

`NativeDefinitions` groups the nine native definition dictionaries and publishes
through `DefinitionTransaction`. `NativeDefinitions.Clone` copies loadable data,
including Unity vectors, without evaluating computed diagnostics. This helper
allows replacement: consumers must validate ownership before publication and
must not use it to claim another author's definitions. It is a native adapter,
not a generic machine generator. See Shipbreaker's `MachineDefinitions` for a
content-specific consumer.

Never republish a native definition by its own name after `ContentLoading`. The
game's `PostModLoadMainThread` fills private state on the loaded objects (for
example `JsonCondOwner` job actions from installable generation) that a clone
cannot carry, and a same-name republish at `ContentLoaded` silently drops it.
To add to a native definition, amend it in place with
`Registration.DefinitionAmendments`: `AppendInteractions` (definition or live
object, refreshing the game's action flags), `InsertInverse` (a reply before a
chosen reply) and `AppendLoot`. All are additive and idempotent, like the game's
own `Installables.Create`. `Publish` also validates staged triggers: an explicit
zero chance is refused and missing lists are restored (0.35.0).

## Grid placement

```csharp
bool[,] occupied = new bool[8, 8]; // Snapshot from your physical inventory.
occupied[0, 0] = true;
var plan = BatchPlacement.Plan(occupied,
    new[] { new ItemSize(2, 2), new ItemSize(1, 1) });
if (plan == null) { /* Wait for space; no physical items were changed. */ }
```

The array is `[x, y]`; `true` means occupied. A successful plan returns one
`Position` per input size, in the same order. This deterministic first-fit planner
does not rotate items or merge stacks. It returns null for an invalid size or a
batch that does not fit in that order; it is not an exhaustive packing solver.
It copies the occupancy grid and never reserves the real inventory. The consumer
must revalidate or reserve actual space before completing a physical operation.

## Production delivery

Implement `IBatchDelivery` around your engine adapter, then call
`BatchDelivery.Commit(adapter)` on the game thread:

| Member | Consumer obligation |
| --- | --- |
| `Prepare()` | Stage the complete product batch and recheck space/inputs. Return false if blocked. |
| `PlaceProducts()` | Insert every staged output, tracking partial insertions for rollback. |
| `ConsumeInput()` | Consume exactly the reserved input once output placement succeeded. |
| `InputConsumed` | Report the actual irreversible outcome, including when consumption throws after taking effect. |
| `RollbackProducts()` | Remove staged/placed outputs before input consumption. Make cleanup safe to repeat. |

The result is `Blocked` or `Completed`; faults are exceptions. Once input
consumption has taken effect, the helper preserves outputs even if a late error
occurs. The caller must pause/report faults rather than retry blindly. This is a
synchronous orchestration helper, not database atomicity or a save transaction.
It cannot make a faulty engine adapter safe, enforce recipe mass, lock concurrent
consumers, restore items after a crash, or move cargo. See Shipbreaker's
`ProcessingService.NativeDelivery` for the current engine integration.

## Physical item transfers (0.3.0)

`PhysicalTransfer.Commit(IPhysicalTransfer)` moves one existing object on the game
thread. It returns false for a blocked preflight, true when delivered (including
an already-delivered call), and throws on a fault. Adapters report actual source,
destination and detached ownership; implement `Prepare`, `Detach`, `Place` and
`Restore`. A failed move restores a detached item where possible. If placement
already succeeded, it never creates another copy at the source. Ambiguous ownership
or failed recovery raises an error requiring inspection; callers must stop.

`NativeItemTransfer(sourceContainer, destinationContainer, item)` supplies the
native adapter for one unstacked item between finite containers on the same loaded
ship. It checks same-ship ownership, containment filters and grid space, uses native removal/insertion
and retains the object identity and its conditions. Call `Redraw()` after a
successful commit. It does not merge, clone, consume or split items.

The consumer must check its own permissions, item eligibility, machinery layout,
power, timing and capacity rules immediately before committing. The adapter is a
low-level operation, not a conveyor network or authority to move arbitrary cargo.
Native methods can throw after partial side effects; this helper cannot make them
database-atomic or crash-safe. A failure is a pause-and-inspect event, not an automatic
retry loop. See Shipbreaker's intake service for a bounded use with actual cargo
retained at the sender during the powered transfer delay.

## Definition registration

```csharp
var transaction = new DefinitionTransaction();
transaction.Stage(myTargetDictionary, myPreparedDefinitions);
transaction.Commit();
```

Stage fully prepared private definitions; commit synchronously during the game
data-loading phase. Each transaction is single-use. Existing entries are replaced
and new ones added; therefore validate namespace ownership and collisions before
staging. This is not an authorization or registry-ownership layer. Values are
captured by reference, not deep-copied. Do not modify staged objects or touch the
dictionaries concurrently.

On a failed write, rollback attempts all recorded restorations in reverse order.
A recovery failure raises `AggregateException` containing the original failure
and recovery errors. Stop startup/production and require correction/restart;
do not treat that result as a clean rollback. A committed transaction cannot be
undone through this API. It does not recover missing providers in existing saves.

## Small transport helpers (0.4.0)

All three are in `Phobos.Ostranauts.Framework.Inventory` and contain no game-world
mutations:

- `ItemDefinitionFilter(IEnumerable<string>)` snapshots an exact, case-sensitive
  allowlist. `Allows(string)` rejects null/unknown IDs. An empty list accepts
  nothing. It does not infer categories, names, value, dimensions or recipe yields.
- `TransferClock(itemId, duration)` binds 1–60 seconds of work to an item
  (`MaximumCycleSeconds`). `Advance(itemId, elapsed, powered)` rejects changed
  identities and invalid deltas; any finite non-negative interval is credited up
  to Duration, so a time-skip or reload gap catches up like a native machine
  (0.36.0; earlier versions treated a gap over 60 seconds as a fault). Unpowered
  calls earn nothing. The owner manages pause/reload, checks current ownership and
  binds credit to the electricity actually received, which is what bounds a long
  interval. This class neither charges electricity nor saves itself.
  `Liquids.LiquidDeliveryBudget.Kilograms` and `Processing.ProcessJob.Advance`
  follow the same rule.
- `GridRoute.Find(columns, rows, starts, goals, allowed, visitLimit = 4096)` returns
  an array of row-major cell indices, including both endpoints, or null. It uses
  cardinal neighbours, never wraps row boundaries, and bounds discovered cells.
  A null result can mean disconnected topology or a reached search limit. The
  caller supplies traversability, validates the current path again before a move,
  and owns physical infrastructure, costs and cross-ship policy.

Shipbreaker uses these helpers for a finite residue collector. Floor sockets,
wall support, filters' chosen IDs, timing/balance and UI remain consumer concerns.
Other mods need neither Shipbreaker nor its recipes. There is no global route
registry, endpoint discovery, lock manager or automatic native-power patch here.
(Framework 0.56.0 adds network families with participants and `LineReach`; see
[line networks](#line-networks-shared-stores-and-the-provider-panel-0560) below.)

## Saved material ports (0.5.0)

`MaterialPort(objectId, portId, propertyMaps)` wraps one stable logical endpoint
in the object's native `mapGUIPropMaps` dictionary. Use the full persistent
`CondOwner.strID` and a namespaced port ID that remains stable across releases.
Constructing a wrapper does not create a link. No separate data registration,
save patch or outside save file is required.

```csharp
var sender = new MaterialPort(source.strID, "Example.MaterialOut", source.mapGUIPropMaps);
var receiver = new MaterialPort(destination.strID, "Example.MaterialIn", destination.mapGUIPropMaps);
// Perform your access, equipment and route checks first.
bool linked = PortPairing.TryLink(sender, receiver, out string problem);
bool stillPaired = PortPairing.Matches(sender, receiver);
PortLink snapshot = PortPairing.Read(receiver);
// Explicit user action; resolve the saved peer first if it is available.
PortPairing.Unlink(receiver, sender);
```

`Read` returns `Unlinked`, `Linked` or `Invalid`. `Linked` means a well-formed
local record, not a resolved or usable route: always require `Matches` on both
resolved endpoints before moving cargo. It checks full addresses, direction and
pair token. `TryLink` refuses occupied/invalid endpoints and is idempotent for an
existing exact pair. `Unlink` clears only its endpoint and an exact reciprocal
peer, permitting safe orphan cleanup. Call these helpers on the game's main
thread, after consumer access checks. They mutate only their namespaced maps.

`ShortId` is presentation only. Persist/resolve full IDs. Native property-map
saves and mode switches preserve the records; missing or malformed links block
use. Preserve unknown-version records until explicit user cleanup. A consumer
must still decide whether a peer is loaded, local, installed, unlocked and valid,
and must invalidate in-flight work when its pair changes. Never treat a saved
link as automatic permission to run. Shipbreaker's first use resumes manually.
See [native precedent, format and tests](../material-port-pairing.md).

## Validation and provenance

The framework's consumer tests reference its built DLL. Shipbreaker's existing
checks exercise that same DLL for mixed inventory placement, interrupted delivery
and registration rollback. They do not load the game or prove engine compatibility.
Original code/documentation and adapted construction portions carry MIT notices; retain all included notices
when reusing them. See `THIRD-PARTY.md` for scope and exclusions.


## Equipment economy and maintenance (0.6.0)

`Trading.MarketStock.Add(definitions, merchantLoot, offerId, itemId, probability,
condition)` appends one namespaced offer, retaining native/other-mod entries. Call
during `ContentLoading`, then publish the prepared definitions. Use a unique Phobos
offer ID and a probability in (0,1]. The framework availability setting scales it
and caps it at one. The original overload yields one item; Framework 0.24.0 adds a final integer quantity argument for a bounded physical lot. Use content-owned [stock quantities](merchant-stock.md) for new retail offers. Conditions are Pristine,
Refurbished, Worn (15% wear), or Broken (supply a damaged definition). Native
merchant stock updates normally; registration never forces restocking.

`Registration.MaintenanceDefinitions` supplies native Restore/Dismantle builders,
stat replacement, and a runtime-referenced remainder item helper. Supply your
own balance and output arrays. Restore reduces wear in place. Dismantling
registers shared cargo/repair-lot guards; `emptyInternalBin` permits only a named,
empty, zero-mass system slot, removed at completion. `Repair` (Framework 0.74.0)
makes a repair finish the game's way: the parts are used up and only the repaired item
returns. `ReturnRepairMaterials` is an obsolete alias kept for older builds.
`Persistence.LegacyItemConversions.Retire` removes a retired item from every ship as it
loads (the old spent service parts use it); `Register` converts one into materials.
No new world artwork or game data files are copied. Native finish actions remain
responsible for replacement, placement and lot consumption.
`LegacyFinish(savedDefinition, oldAction, newAction)` redirects an explicitly
known queued finish for that object only; Auto Nav uses it to keep old board jobs
mass-balanced without changing generic vanilla jobs.

`EquipmentSaveUpgrade.Register(definitions, savedId, definitionId, legacyMount,
legacyRepair)` opts an item into the first-economy migration. It clones incoming
save DTOs, refreshes price/missing work limits and preserves busy progress limits;
it never opens save files. Use it only for coordinated migrations with known old
thresholds. It is not a generic save-recovery or arbitrary-version migration API.

`EquipmentSaveUpgrade.FollowPrice(definitionId)` (0.68.0) registers a definition
whose saved items take the live definition's `StatBasePrice` on every load. It is
markerless and idempotent, so a later price change reaches saved items too; a
DEFAULT-compressed save without its own price term is left alone because it
already reads the definition. It reads the live definition at load, so it also
serves a native definition amended in place (Shipbreaker's methane ice). Use it
for materials whose price is a balance decision; never for equipment with wear
tiers or saved work, which keep `Register`.

Recipe schema 1 optionally accepts `toolTriggers`, up to eight distinct native
tool selectors. They are reusable tools fetched through native `Use` handling,
not mass-bearing ingredients. Existing packs omitting the field still work.
Output overlays are supported only when their base defines explicit mass and they
have no arbitrary condition loot. Consumer prices, repair bills, yields and stock
locations belong in the consumer. See [current consumer balance](../equipment-economy.md).

Translation catalogs, language settings and contributor guidance: [Localization](localization.md).

## Shared processing jobs (0.8.0)

`Phobos.Ostranauts.Framework.Processing` provides `ProductSpec`, immutable
`ProcessRecipe` (explicit input mass), `ProcessRecipeCatalog`, `ProcessJob` and
`ProcessMaterial`. Both wall processing and the reclaimer use these. Each content
consumer owns its catalog and stable native save keys. Only fresh inputs select
Current; saved jobs resolve their exact revision and duration. No job is advanced
merely by reading it. A restored job is a session object; content owns permission
to resume after loading. Validate actual item identity, mass and readiness before
advancing and committing through existing staged batch delivery.

Keep historic recipes immutable. Opt into `legacySeconds` only for a revision
actually shipped without saved duration. Do not assume a different machine's
revision 1 shares that exception. Content owns cooling, power, eligibility, art
and balance; this is not a universal process scheduler or fluid system.

## Saved port filters (0.9.0)

`Inventory.SavedPortFilter.Read(MaterialPort)` returns Default, Configured or
Invalid. Default delegates to the consumer's explicit `ItemDefinitionFilter`;
Configured contains exact immutable definition IDs; Invalid allows nothing.
`Set(port, ids)` validates a bounded unique list before replacing the namespaced
filter record. An explicit empty list blocks everything, not a fallback. Native
property-map conversion and owner remapping follow the existing pair model.

Pairing and filters are separate. Multiple logical ports on one object keep
independent records; unlinking a pair does not erase its filter. Consumer code
must reject IDs incompatible with the equipment, validate access, pause/reset
any active transfer when settings change, and require explicit resume after load.
No filter grants transfer permission or replaces real container capacity, mass,
route or ownership checks. Shipbreaker's reclaimer and collector receivers are
the two concrete consumers; see [automatic routing](../automatic-material-routing.md).

## Furnace-driven shared services (0.16.0)

`Processing.NativeEnergyReceipts` witnesses an existing native `UsePower` call.
Begin/Complete are one-use, per-powered-component sessions; do not save or replay
a receipt or tick native power again to obtain one. `EnergyReceipt` accounts for
external delivery plus appliance storage changes; unexpected over-debit remains
real energy rather than being clamped away. Always Forget in final cleanup.

`ThermalMath` supplies finite sensible/radiative calculations, and `GasParcel`
retains finite named species and sensible energy. They do not simulate a ship
atmosphere or choose recipes/limits. Shipbreaker owns F6 phase rules, gas transfers,
endpoints, thermal settings and hot-state records using `ObjectStateStore`.

`Controls.NativeInstruments` clones only audited knob/LED/lamp subtrees from
installed prefabs under an inactive owned root. It checks the assembly fingerprint
and component whitelist, validates sprite/row contracts, and suppresses native knob
callbacks explicitly during refresh. A missing or changed donor produces one
diagnostic and a null result for the caller's existing control fallback. Never
clone the reactor controller, mutate shared sprite/font assets or distribute them.
Consumers still own authority, localization, numeric formatting and physical
measurements; absent data must stay Unknown.

Framework 0.21.2 resolves a byte-loaded game assembly's empty `Location` through
BepInEx's managed directory before applying the same pinned hash. Missing or
changed files still fail the audit. `PanelWidgets.FitFixedText` is for text inside
a caller-owned fixed-field mask: keep its font size, center visible glyphs and
normalize baseline spacing to 1.12 em using the selected font's face metrics.
It generates overflowing glyphs for the mask to clip, avoiding TMP's vertical
ellipsis erasing an entire short field. Explicit line breaks remain supported;
long names clip and complete explanations should remain in a scrollable view.
Callers can restore normal wrapping for prose. Do not apply this to unmasked
fixed fields or mutate shared font assets to achieve compact spacing.

### Native control additions (0.17.0)

`NativeInstruments.Clone<T>` also accepts the audited reactor `GUISafetyToggle`,
`GUI7Seg` and `Slider` donors used by the F6. The clone's serialized events are
replaced before activation; guard buttons and toggle sprite feedback are then
initialized by their own audited native behaviours. Component references must
remain inside the cloned subtree. Toggle groups and explicit navigation links
are removed. Native guard refresh uses `SetIsOnWithoutNotify` and explicitly
refreshes the toggle sprite; it must never dispatch a service command.

`InstrumentNumber.TryFormat` returns invariant digits, dot position and sign,
rejecting missing, nonfinite and overflowing values. The digit adapter bypasses
the native formatter, blanks invalid readings and retains the native artwork.
Consumers must display the full signed localized measurement (or Unknown) beside
the digits; the sprite sheet has no sign glyph. Slider consumers set their own
bounds and submit drafts through checked services. Framework does not own F6
balance or permit commands merely because a widget is enabled.

See [the F6 attachment and instrument record](furnace-connections-and-instruments.md)
for exact donor paths, native attribution and pending Unity-scene checks.


## Agriculture consumer extensions (0.17.0)

`Controls.EquipmentProviders` registers one content-owned `IEquipmentProvider`
per stable owner and native definition set. Duplicate ownership is rejected.
Snapshots contain immutable identity, group, activity and copied action lists;
reading them must not advance simulation. `Command` must revalidate the live
actor, full equipment ID, ship, optional `ConsoleBinding` and machine interlocks.
Registration does not grant remote authority. Agriculture is the first external
consumer; Shipbreaker's C1 performs its existing access check before dispatch.

`Liquids.FiniteLiquidTransfer` accepts synchronous `ILiquidReservoir` endpoints
with kg quantities/capacities, stable identity, same-ship scope and commodity.
It bounds requests by reserve and destination headroom, measures debit/receipt,
and returns unreceived material when the destination partially accepts or throws.
Unexpected mutation must stop the consumer for reconciliation; receipts are not
persisted or replayed. Call on the game thread. This is not a station-refuelling
API or a replacement for solid-item transfers.

The [27 September bulk-storage blueprint](agriculture-bulk-storage-design.md)
audits these services and proposes additive endpoint snapshots, reservations and
separate station purchase contracts for Agriculture. Those proposed APIs and R3
equipment are **not implemented**; use the interfaces documented here for current
consumers. The [research report](agriculture-bulk-storage-research.md) and
[art/native-widget audit](agriculture-bulk-storage-art.md) explain the boundaries.

`Liquids.ShipsWaterSupply` is a narrow optional adapter for Valtora's
[Ship's Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189).
Its inspected potable-vessel field stores litres; this adapter treats water as
1 kg/L, leaves foreign mass/capacity policy intact, protects a same-ship reserve,
and declines other provider versions until reviewed. Agriculture retains manual
water loading. Agricultural drainage is not accepted by this adapter.

`Registration.ApplianceDefinitions` builds the shared native installable/loose,
intact/damaged appliance skeleton used by Firstlight-4 and Hearth-2. Content supplies branding,
footprint, mass, price, image prefix and power rating. Supply a matching `Normal`
image derivative; never include extracted game art. Biology and balance remain
Agriculture-owned; fixed industrial batches and the one-hour cap are unchanged.
See [Agriculture implementation and owner checks](agriculture-implementation.md).

### Routed water delivery (Framework 0.18.0)

`Liquids.NativeFluidRoute.Find` takes two native named endpoint points and a
content-owned compatible-segment predicate. It resolves fresh cardinal paths
over intact same-ship structural flooring, with owned/installed/unlocked endpoint
checks and a bounded search. It never borrows the native electrical network.
Pair selection and run/receive permission remain explicit consumer responsibilities.

`LiquidDeliveryBudget.Kilograms` bounds one interval by actual received electricity
and the content's flow/energy ratings; invalid intervals and gaps over one hour
yield zero. The consumer must share that budget across every action of its pump,
settle all consumed energy to a real recipient and avoid persistent energy credit.

`LiquidTransferGuard.Commit` wraps the measured scalar transfer with namespaced
versioned pending journals at both endpoints. Check both guards before operation
or manual contents changes. A failure leaves evidence and disables retry, including
after reload. Do not automatically clear pending/future records. This protects
against uncertain completion; it does not provide crash-atomic native saves.
The new optional guarded `ShipsWaterSupply.Refill` overload preserves the earlier
four-argument API and adds a private Framework journal to eligible provider tanks.

[Agriculture water conduits](../agriculture-water-conduits.md) are the first consumer.
They keep one pump/rack pair per circuit. Return flow, fluid temperature
and multi-consumer fairness need concrete additional contracts; scalar `water`
must not be used to erase nutrient composition or manufacture cooling capacity.

Framework 0.19.0 adds two-component `LiquidMixture` and `IMixtureReservoir` for
[Agriculture's finite nutrient solutions](../agriculture-nutrient-solutions.md).
`MixtureTransfer.Allowance` preserves source proportions and checks both component
capacities and total capacity. Use the `LiquidTransferGuard.Commit` mixture
overload for journaled writes and measured component receipts. Adapters must set
both quantities together and expose a stable nonempty ship/identity/profile.
Partial accepted receipts must retain the source composition; equal total mass
alone does not reconcile a transfer. A failed/ambiguous write protects both ends.
Content owns the profile definitions, blending, reactions and consumption; this
is not a general chemistry or pressure simulation. Share the existing delivery
budget across mixing, transfer and intake rather than granting each a full budget.

Framework 0.19.0 adds a world-point overload of `NativeFluidRoute.Find` for
[F6 sealed coolant routing](../furnace-coolant-conduits.md). It accepts explicit
`allowLockedEndpoints` / `allowDamagedEndpoints` flags for physical thermal paths;
segments always require intact, unlocked, owned installation. The one-argument
`EndpointReady` and original named-point `Find` APIs retain their signatures and
strict behaviour for already-built Agriculture consumers. Content still owns
connection points, circuit exclusivity, pump demand and thermal accounting.
Geometry supplies no heat, fluid, pump credit or authority to resume machinery.

### Fluid network extension (Framework 0.20.0)

`Inventory.PortBank` provides a bounded bank of existing reciprocal ports; slot
zero preserves the caller's original single-port ID. Admission, ship scope,
permissions and route discovery remain content responsibilities. `FluidLine`
retains a two-component parcel, immutable occupied route binding, capacity and
remaining transit. Its endpoint custodian must include contents in physical mass,
use transfer guards, block removal and offer a finite drain. Missing state starts
empty; never infer fluid from geometric pipe volume. `HydraulicRoute` supplies a
bounded authored resistance fraction and equal budget shares, not pressure
observations or a CFD solver. See [operating contracts](../fluid-network-operations.md).

### Fluid network extension (Framework 0.20.0)

`Inventory.PortBank` provides a bounded bank of existing reciprocal ports; slot
zero preserves the caller's original single-port ID. Admission, ship scope,
permissions and route discovery remain content responsibilities. `FluidLine`
retains a two-component parcel, immutable occupied route binding, capacity and
remaining transit. Its endpoint custodian must include contents in physical mass,
use transfer guards, block removal and offer a finite drain. Missing state starts
empty; never infer fluid from geometric pipe volume. `HydraulicRoute` supplies a
bounded authored resistance fraction and equal budget shares, not pressure
observations or a CFD solver. See [operating contracts](../fluid-network-operations.md).

## Recycler reject reservation — Framework 0.22.0

`ShipsWaterRejects` provides a version-scoped opt-in reservation around Valtora’s
[Ship’s Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
private Recycler settlement. A content sink owns pairing, access, storage and
journals. Framework bounds source processing and measures same-ship tank deltas;
it supplies no nutrient assay. `CollectorCargo` admits registered physical cargo
under Shipbreaker’s existing capacity and native container checks; its endpoint
validator retains Shipbreaker’s mount rules. Neither API authorizes automatic
industrial routing. See [the concrete consumer](../agriculture-nutrient-production.md).

## Crew standing orders

See [crew automation, specialities and time-skips](../crew-automation.md) for default-disabled orders, native duty/AutoTask rules, approved stores, training, saved stops and supported onboard work. Industrial batches, exterior missions and crew-launched flight require explicit Resume. Gameplay and UI checks remain owner-run.

Framework 0.25.1 resolves available crew through Blue Bottle Games' native
`JsonCompany.GetCrewMembers` roster API, including for panels and skip preview.
The legacy `CrewSim.aCrew` field is not initialized in the inspected 1.0.1.5 code.
`CrewWork.AllCrewAboard(ship)` is a stricter departure guard: unavailable company
data, an empty roster, unresolved members and members away from that ship block
permission. It does not remove roster entries or alter native saves.

Register an additive `Crew.ICrewWorkProvider` through `CrewWork.Register` during
plugin initialization. `Next` reports an offer and a localized blocker without
advancing production. `Complete` receives the actual `CrewWorkContext.Actor`
and must recheck through the owning service before mutating resources. The
existing equipment-provider contract is unchanged. Keep recipes, interlocks,
balance and machine mutations in the content mod.

`CrewLogistics` reserves and moves exact physical units. Native tasks retain
ordinary claiming, walking, carrying and cancellation. `ICrewBoundProvider`
adds an immutable equipment/mission fingerprint captured on explicit Enable;
providers compare it before admission and completion. `ICrewSkipProvider` is
an accounting opt-in, not a timer callback: only register machines whose existing
power callbacks settle finite inputs, outputs, fluids and heat. Unsupported
operations suspend before the native skip.

Register content-owned specialities with `CrewSpecialities.Register`, and map
timed native work with `RegisterPractical`. Call `CreditPractical` only after
successful completion; its per-interaction guard prevents duplicate credit.
Never credit idle machine time or award progress while preparing/previewing an
offer. The shared saved-order, training and permission maps use Framework's
protected `ObjectStateStore`; generated tasks and reservations are transient.

`Crew.CrewStudy` (0.35.0) teaches every registered speciality through the game's
own study chain, cloned from the software-engineering family: an opener
(`PhobosStudySkill_<Id>`, `bOpener`, no duty) the idle AI can pick, a chooser
that sets the `PhobosStudying_<Id>` mark, a continuation chooser gated on that
mark and inserted into `ACTStudySkillAllowCont` before the vanilla skills, a
refusal once `PhobosSkill_<Id>` is held, and a time-skip tick listed in
`ACTFFWDContextPayloads`. Study material is any terminal the vanilla
`TIsStudyMaterialUnused` rule admits, so saved terminals qualify without a new
condition. Completed `ACTStudySkillCont*` steps credit the marked speciality;
each skipped hour credits an hour. Content mods register nothing extra.

The game only chooses study actions present in a crew member's AI history
(`mapIAHist`, seeded from `dictAIPersonalities["Abner"]` when a person is
created and saved with them). `CrewStudy` therefore copies the vanilla
`ACTStudySkillEngConstruction` entries for each opener into the template at
`ContentLoaded` and into the player's crew on the first poll after a world
loads, only where absent. Never overwrite or remove learned entries.

`CrewWork` keeps the native task total in step (`WorkManager.nTotalTasks`) when
it re-adds a task for a continuing order, because the game interrupts every
on-shift crew member's study whenever that total rises; the first task after an
Enable is announced normally. Failed steps back off through
`CrewBalance.RetryDelay` and keep the order; `CollectTasks` withholds tasks the
crew member could not claim. `phobosframework crew [name]` reports all of this
read-only, including who could claim each step now.

Tasks name no owner (0.36.0): `Task2.GetOwnership` forbids everyone not on an
owner list, so the earlier player-only owner meant no other crew member ever
took a step. Up to `CrewWork.TasksPerTarget` tasks may target one store, since
the native `AddTask` default keeps one per target and action. Eligibility has no
need gate: the game's pledges handle hunger, thirst, rest and pain before work,
as they do for painted jobs. The task action is cloned from the vanilla
`ACTTogglePower` Operate job (`CrewSpecialities.WorkTemplate`). Time-skips keep
the game's own repair allowance (`GUIFFWD.UndamageParts`) and scale it by
`CrewBalance.RepairShare`, the share of on-shift crew time not spent on Phobos
jobs; only orders on the skipping crew's ships that the managed skip cannot
advance are suspended.

## Presentation and equipment discovery

Framework 0.29.0 adds `Controls.PresentationRefresh` for real-time UI cadence with
explicit invalidation, and `Controls.Presentation` for change-only widget writes.
Build labels and optional CompactText components before their first binding.
`Controls.NativeInstruments` retains its API and binds isolated digit/guard
children once. Keep these helpers out of simulation scheduling and command admission.

`Controls.ShipEquipment.Read(ship, predicate)` returns a fresh loaded-ship root
snapshot, excluding destroyed objects, nested cargo and docked neighbours. Content
must supply its equipment/store predicate and revalidate when acting. It is not a
retained object registry. `Registration.EquipmentIdentity.IsFamily(id, prefix)`
compares the four exact native Installed/InstalledDmg/Loose/LooseDmg forms without
constructing temporary candidate IDs. See [the audit](performance-audit.md).

### Framework 0.30.0: optional Polaris presentation

`PolarisWidgets.Button`, `Style` and `Selected` use native button artwork with
retained weak bindings, change-only selected markers and new control states.
No native controller or donor listener is copied. `SecondaryClick.Bind` adds a
right-click action to a Button; it consumes right clicks, respects active and
inherited interactability, and leaves left-click dispatch to Unity Button.
Consumers must still validate access/current state in their action services.
`ConsoleShell.UsePolarisStyle()` opts into wrapped navigation and action rows;
other shells retain existing dimensions and appearance. These additions are
presentation-only and preserve existing public signatures.

The shell's unsaved-changes guard on `CrewSim.LowerUI` applies only while the
raised panel hosts that shell; lowering any other panel proceeds natively
(0.36.0). `ObjectPicker` registers with the game's own window stack
(`GUIData.RegisterOpenWindow`, `IDataWindow`), so Escape closes it before the
hosting panel, as with native sub-windows. While picking in the world only
world selection is taken over; the game's pause, time-scale, console and
screenshot commands stay live.

### Refusing at effects time (0.36.0)

The game's `Interaction.ApplyEffects` closes the task that queued the action
(`WorkManager.CompleteTask`) before applying any effect. A Harmony prefix that
returns false skips that and leaves a painted or standing task listed forever.
Return `Registration.NativeEffects.Refuse(interaction, reason)` instead: it
closes the task, logs the reason and writes it to the actor's crew log, then
returns false. Prefer refusing at offer time (`TriggeredInternal`) so crew are
not sent to fail; keep effects-time checks for what can only be known then.

### Native item handling

Framework 0.30.2 provides opt-in `Registration.ItemHandling.Apply(NativeDefinitions)` after content definitions are complete. It preserves non-transport actions, supplies native pickup/drop, gates stack actions by stack limits, and keeps installed/internal objects out of ordinary carrying. Content calls `ItemHandling.Cumbersome(definitions, id)` for explicitly bulky loose components; mass is not used as a universal threshold. Existing native cumbersome machine forms remain drag-only. Only Phobos item definitions are normalized, leaving optional foreign-provider definitions intact.

The explicit bulky registry also corrects saved condition copies. A saved hand slot is temporarily retained until native unslot succeeds, then removed; contents and placement are never forcibly changed. See the [complete handling audit](item-handling-audit.md) for evidence and owner checks.

### Framework 0.31.0: section sites and maintenance information

`Construction.SectionAssembly.Add` registers a finite section bill through native
installables before publication. Content supplies the section trigger, installed
output, count/mass, tools and native work target. Only separate, empty, matching
sections qualify; the finish revalidates the actual native site lot. Since 0.67.0
section jobs are retired from new work: see below. Whole-machine direct actions
remain valid.
`RetireTableOffers` removes only named new offers, preserving old action definitions
and saved queues. Native placeholders own hauling, cancellation and serialization.

`Controls.ItemInformation.Register` adds read-only, scrollable instructions to
explicitly selected definitions. `Registration.MaintenanceInformation.Register`
adds shared cargo/lot/stack guidance and an optional content-owned blocker reader.
Neither authorizes work. Content services retain current-state validation. See
[section assembly and maintenance](../section-assembly-and-maintenance.md).

Framework 0.116.0 makes that entry the right-click **Maintenance** sheet. The
interaction ids and the `Register(definitions, action, extra)` signature are
unchanged, so content mods need no code. An installed machine's sheet adds its
standing order (`CrewWork.ReadStatus`) and upkeep (`Upkeep.MachineReport`) with
buttons into the Crew panel; `MaintenanceSheet.Sections` builds the sections
from plain facts for offline checks. `ItemInformation.Register` and `Show` also
take `InformationSection` lists with `InformationLink` buttons; a link's `Open`
only delegates and returns null or the reason it could not open, which the
sheet shows. The sheet rereads its sections once a second. `CrewPanel.Show(co,
view)` opens on `CrewPanel.OrdersView` or `UpkeepView`, and
`CrewPanel.Unavailable(co)` says why it would refuse.

Framework 0.116.0 also adds `ICrewStoreEquipment`: equipment with a crew-work
provider is not a store unless the provider implements it and
`CountsAsStore` answers true, so a machine whose tray others deliver to keeps
that standing when it gains orders (Manufacturing's charge machines and RM-1).
`CrewWork.IsStore` and crew hauling also apply the `stores` data pack
(`Inventory.StoreRules`): `NotAStore` (excluded conditions, single-purpose
container rules, no `Inventory` interaction) for store lists, `SealedContainer`
for what crew never take from. Both are judged once per definition id.

### Framework 0.32.0: opt-in construction appearance

After `SectionAssembly.Add`, call `SectionAssembly.SetAppearance(jobId,
new SectionAssemblyAppearance(earlyImage, intermediateImage, nativeSize))`.
Paths omit the extension; each colour has a companion `Normal` image. Content owns
the images and native size. Existing callers remain unchanged.

Only registered section markers receive a view. Early artwork remains until the
complete, distinct, valid lot is present and finite native work is positive.
Intermediate remains even at the work limit: only native completion creates the
finished machine. No visual state is serialized or used to authorize actions.
The view checks at most ten times per real second per site, clones that marker's
material once, preserves native property blocks/geometry and changes textures
only when needed. Disable/destruction releases the clone; changed or foreign
markers restore their original material. Texture failures log once per image per
registration and fall back. No global site scan, movement hook or per-frame log
is added. Unity appearance remains owner-tested.

## Room heat, native gas and vessel damage (0.41.0)

`Processing.RoomHeat` is the air-cooled operating rule every powered Phobos
machine follows: `Budget(mols, kelvin, pendingKelvin, pressureKPa, kw, seconds,
out rise)` refuses a room below 10 kPa, any vacuum, and any step that would take
the room past 40 C, using the game's own 20.7 J per mol K. `Read(machine, point)`
returns the room's `Air` (or null: never free cooling), `Admit` applies the budget
to it, and `Deposit(air, kWh, fraction)` adds the declared share of supplied energy
to the room's pending temperature for the game's mixing to settle. Shipbreaker's
`ReclaimerRules.CoolingBudget` and Agriculture's own rule are unchanged copies.

`Processing.NativeGasCanister` holds the game's species and molar masses (the
constants in its `GetGasMass`), `CapacityMoles(m3, maxKPa, K)` by the game's
refuelling arithmetic, and guarded reads and writes on an installed rated O2, CO2
or N2 canister: `TryAdd` is capped at the rated pressure because the game caps
nothing, `TryTake` is clamped to contents because a negative total corrupts the
game's internal count. `Processing.RoomGas.Emit/Consume(air, species, kg)` do the
same for a room's air, in kilograms, for the eight room species only; hydrogen,
water vapour and helium are never created anywhere. The game's alarms, poisoning,
scrubbers and fires respond to the result on their own.

`Hazards.NativeExplosions.Spawn(ship, position, definitionId)` places a
definition that carries the native `Explosion,<name>` update command, exactly as
a mode-switch loot does for an armed charge; the game's `Explosion` component
applies the damage, shrapnel and fire rolls and removes the object. Content
declares its own entries under `data/explosions` and `data/condowners`.

`Liquids.BulkVesselSpec` gains `VesselDamagePolicy` and `LeakKgPerHour`. The
eight-argument declaration keeps the default `Isolate` (contents trapped in the
catch chamber on damage). A pressurised store declares `Leak` with a rate; on
damage its contents stay in service and the owning service drains them with
`BulkVessel.Drain(co, kg, reason)`, which logs each loss, until the vessel is
repaired. Nothing models pressure inside a Phobos vessel.

`Registration.ApplianceDefinitions.AddFeedBin(d, prefix, containerTrigger, cells,
name)` adds the hidden locked feed compartment and its slot to an appliance
family; `SetPowerOverride(d, prefix, idleKW, workingKW, workingCondition, points)`
sets the two-level electrical demand. `Controls.ConsoleAuthority.Check(target,
binding, accessTiles, consoleDefinition)` is the remote-command rule; wording
stays with the caller.

## RCS propellant and buffered draws (0.42.0)

`Propulsion.RcsPropellant` keeps every RCS quantity in the engine's own unit,
nitrogen-equivalent kilograms. `ExhaustRatio(species)` is the ideal cold-gas worth
of a kilogram relative to nitrogen at one temperature (N2 exactly 1, unknown
species 1), and `MixtureRatio` weights a mixture by mass. For loaded ships Framework
serves `Ship.RemoveGasMass`, `GetRCSRemain` and `GetRCSMax` itself in the game's
order: regulators, their `GasInput*` points, the game's `TIsRCSValidInput` rule and
each container's own removal. Shallow ships, refuelling and thrust stay native, and
a failure falls back to the native method.

A content mod adds remass the game cannot see by registering an
`IRcsPropellantFeed` (`RcsPropellant.Register`, `Unregister(id)` on unload). A feed
is an object on a regulator's gas-input tile: `IsFeed(co)` claims it, `DrawFirst`
chooses before or after the tile's canisters, `Offer(feed, kg)` returns the
nitrogen-equivalent it actually supplied, and `ReserveEquivalentKg` and
`CapacityEquivalentKg` feed the fuel and delta-v readings. Keep feed objects
non-airtight so the game's refuelling never treats them as canisters.

`Liquids.BufferedDrains` is for consumers that take a little every frame.
`AvailableKg(vessel)` is the service amount less what is owed, `Take(vessel, kg,
reason)` never takes more, and the owed total settles through the quiet
`BulkVessel.Drain(co, kg, reason, log: false)` every two seconds and before
`Ship.GetJSON` saves, with one log line per settlement. A load or new game forgets
unsettled draws, so a crash loses at most about two seconds of them.

## Bulk vessel sizes, native gas vessels and gas transfers (0.44.0)

`Liquids.BulkVesselSizes` is the shared size ladder. `Prefix`/`Name(small, size)`
keep the small size's original identity and records and append `Medium` or
`Large` for the others. `Footprint` adds one tile per step, and `SizeOf` or
`InLadder` resolve a definition within one family. `Spec(...)` builds one size's
`BulkVesselSpec`, scaling capacity (floor area plus 10% per step), dry mass (floor
area less 15% per step) and, through `PriceFactor`, price (area to the power 0.6),
rounded by `Round`. Register each size as its own family: records must stay
distinct per owner.

`Processing.NativeGasVessel` reads any of the game's rated O2, N2 or CO2 vessels,
installed or loose canister or suit bottle. `TryRead` counts every species in
`TotalMoles`; `HeadroomMoles(fraction)` stops below a fraction of the rating
(`SafeFillFraction`, 99%, below the game's burst margin even in vacuum). `TryFill`
refuses damaged vessels; `TryDrain` is clamped to contents; `PricePerKg` reads the
game's own gas price table.

`Liquids.GasTransfers` moves gas between a bulk vessel's record and a native
vessel (`StoreToVessel`, `VesselToStore`) under the store's conversion journal,
changing the record first when gas leaves a store and last when it enters one, so
an interruption can lose gas but never create it. `VesselToVessel` decants
between two native vessels and puts back whatever the target cannot take. Both
settle `BufferedDrains` first; `BufferedDrains.Settle(vessel)` is public for any
consumer that changes a record directly.

`Trading.VesselSupplyProvider` also takes one offer per commodity naming several
families, so a single station line fills every size.

Selling back (0.68.0): `Trading.IBulkBuybackProvider` is the other side of the
kiosk's Bulk supplies view, registered with `BulkSupplies.RegisterBuyback` and
removed by the same `Unregister(id)`. Its offers carry the price the station pays.
`Trading.VesselBuybackProvider` takes the same lines as a `VesselSupplyProvider`
(the offer carrying the station's own selling price) and pays
`BulkSupplies.BuybackShare` (0.45, the owner's decision of 1 October 2026) of it;
a source is an installed, ready, unprotected vessel of those families with an
empty catch chamber, and what may be sold is the service contents above its
reserve. `BulkSettlement.Sell(quote, IBulkSaleTarget, journal)` withdraws first,
pays exactly for the measured withdrawal and keeps the purchase journal's states
and protection, in its own `BulkSale` journal. Never register a buy-back for a
commodity whose bought price would let a loop gain, such as nutrients priced by
another mod.
`Registration.ApplianceDefinitions.SetRack(d, prefix, trigger, width, height)`
turns an appliance's tray into a restricted rack behind the game's own Inventory
window.

## Performance primitives (0.45.0)

Since 0.46.0, `Discovery.WorldFamilies` replaces a content mod's own pass over every
world object: register once (`WorldFamilies.Register(key, definitionId => ...)`, a pure
definition-id predicate) and read `family.Members(list)` when you need the objects.
Members are checked on every read (alive and registered in the world under their id);
new objects are found by one shared sweep every two real seconds, spread across frames,
or at once through `family.Offer(co)` (a mode-switch replacement). The pure logic is
`Discovery.WorldIndex<T>`. Keep filters that depend on live state (installed, damaged,
ship loaded) in your own loop over the members.

The 29 September 2026 performance pass added small shared helpers; use them where a
content mod would otherwise scan, format, write or search on every power step.

- `Registration.DefinitionIndex<T>`: family prefix to value, one dictionary probe per
  definition id after the first look. It indexes immutable definitions, never live
  membership; `BulkVessels.SpecFor` uses it. Clear it with your registrations.
- `Processing.StepMemo<TKey,TValue>` and `NativeSteps.Frame`: a value that holds for one
  step (Unity's frame count) and vanishes with it; `Invalidate()` when what it
  measures changed. Never key on an exact epoch.
- `Cadence`: a real-time gate (`Time.unscaledTime`) for topology rechecks, discovery,
  candidate lists and record settlement. Conserved accounting stays on game time
  inside the native hooks.
- `ObjectStateStore.TryWriteIfChanged` and `Status()`: the same validation as
  `TryWrite`, with an identical record left untouched.
- `Persistence.SaveBoundary.BeforeShipSave`: settle cadence-written records before
  the game serialises a ship. `BufferedDrains` subscribes; a failing handler is
  logged and never stops the save.
- `Localization.DeferredMessage`: a status kept as key plus arguments, formatted
  only when read (`Resolve`/`ToString`); `Set` returns false and allocates nothing
  when nothing changed. Presentation only.
- `Construction.TriggerRefinements`: the single `CondTrigger.Triggered` postfix.
  Construction ingredient and station selectors and section assembly selectors
  register into its table through the existing registries; do not add another
  postfix on that method.
- `Liquids.FluidSegmentFamily` and `FluidRouteCache`: declare your segment family
  once (`new FluidSegmentFamily("Owner.Water", c => c.strCODef == Pipe + "Installed")`)
  and ask `Find` for routes or `SharesCircuit` for the second-source rule. Endpoints
  are checked fresh on every call; the pipe and floor layout is reread every two real
  seconds or on a mode switch, destruction or ship membership change. `NativeFluidRoute.Find`
  remains the uncached search.

## Line networks, shared stores and the provider panel (0.56.0)

The owner's link rule (30 September 2026): a machine reaches a store, or another
machine, only through touching equipment (footprints touching or one tile apart,
`BulkVessels.WithinOneTile`) or a line network, never across open floor. The station
refuelling kiosk is the only exception. Touching suitable equipment joins as if
piped, and joins chain across the ship.

- **Network families.** `new FluidSegmentFamily(id, compatible, ports, adjacencyJoins)`:
  `ports` returns an object's map points for the family (null or empty: not a
  participant); `adjacencyJoins` lets participants within one tile join. One
  instance per id (`FluidRouteCache.Register` refuses a second). One object scan per
  ship builds every registered family; a mode switch invalidates only its own ship,
  and only for segments, participants, floors and walls; a new snapshot's recheck
  starts consumed. `FluidTopology` gains participants, `ParticipantsConnected` and
  `Hops` (one search per source participant per snapshot, remembered). Families
  without participants (irrigation, coolant, whose rules allow one pump or furnace
  per circuit) behave exactly as before.
- **`Liquids.LineReach`.** `Of(a, b, family)` returns `Adjacent`, `Line` or `None`
  (touching first, then the same network; null family: touching only). `Hops`,
  `Reachable` (for candidate lists) and `Members` (a participant's network). The
  named-point overload (the gas line's first form) is obsolete since 0.69.0: nothing
  calls it. A bridging participant that became locked keeps
  bridging until the snapshot is reread (two seconds at most).
- **`Inventory.SharedPorts` and `PortBank` roles.** A vessel-side port becomes a bank
  of `SharedPorts.Slots` (eight); slot zero keeps the old port id, so every saved link
  reads unchanged. `PortBank` takes a role: `Sender` (the W2's racks) or `Receiver`
  (a store fed by several machines). `SharedPorts.TryLink(ours, vessel, primary,
  machineSends, resolve, out problem)` reuses a machine's slot, takes a free one, or
  reclaims one whose machine resolves on the same ship and no longer points back.
  `SharedPorts.Peer` builds the vessel-side port a machine is saved against;
  `SharedPorts.Peers(vessel)` lists every linked machine for a panel.
- **`Registration.LineDefinitions`, `LineLayers`, `LineJoints`, `LineJobFilter`.**
  `LineDefinitions.Add(d, LineSegmentSpec)` publishes a 1 x 1 line family on the
  shared pattern: its presence (and optional intact) condition, sprite trigger, joint
  fixture loot, floor-only placement that forbids only its own family, pocketable
  loose stacks and a draw layer from `LineLayers` (one per family, so families share
  tiles without flicker). `AddPort(d, equipmentPrefix, line, point, x, y, socket)`
  adds a port point and draws the joint; a tile already drawing another family gets a
  combined loot, and further families on one item are redrawn by `LineJoints`' guarded
  `Ship.UpdateTiles` postfix (the game refreshes only the item's own sheet trigger).
  `LineJobFilter` makes the PDA paint filter count every registered line as Conduits
  and never as Equipment, by each family's own machine condition; the game's conduit
  trigger itself is never changed (it also drives the power conduit's jobs).
  Lane art: `scripts/export-line-art.py` and `assets/line-art/README.md`.
- **`Pda.PdaApps` (0.126.0).** A mod's own app on the PDA home screen. Call
  `PdaApps.Register(new PdaApp { Name, Icon, Label, Title, Tooltip, Open })` during
  `ContentLoading`, once the mod knows its package is enabled. `Name` is lowercase
  letters, digits and underscores, starting with a letter and never one of the game's own
  app names (`PdaApps.NativeNames`); `Icon` is an image path under the mod's `images/`
  folder without `.png`, drawn as the game draws its own (a white disc with a black glyph,
  256 px; the game tints it); `Label`, `Title` and `Tooltip` are catalogue lookups.
  Framework writes the icon entry and the `GUI_PDA_BUTTON_<NAME>` tooltip strings into the
  game's tables after every content load, so the mod ships no `pda_apps` or `strings`
  data. A tap closes the PDA and calls `Open`, which returns null when its panel opened
  or the reason it did not (written to the player's log). Open a panel the way
  `LettersPanel` does, with a `RaiseUI` restore prefix keyed by its name. The quick bar
  under the home screen is the player's own setting and cannot be added to. Phobos
  Banking is the first user; design record:
  [PDA apps and banking research](pda-apps-and-banking-research.md).
- **`Controls.ProviderPanel`.** The shared equipment Control Panel (Operation,
  Connections, Details) over any `IEquipmentProvider` with `IEquipmentPanelFields`:
  register a `ProviderPanelSpec` (native GUI key, provider, access, resolution, the
  mod's own page texts). It redraws its page after a command and when a configuration
  sheet applies a change (`ConsoleShell.Changed`, 0.55.0); fields may name their
  current choice (`EquipmentField.Current`). Providers name their console groups with
  `EquipmentProviders.RegisterGroup`.
- **Press twice to go ahead (0.125.0, owner rule).** A command or configuration that
  needs other steps first (pausing a machine or its intake, removing an old link,
  stopping work, cancelling a batch) must not refuse for that reason. In the service,
  strip a confirmed action with `Confirmations.Split(ref action)`, work out the steps,
  and call `Confirmations.Ask(warning, confirmed, out message)`: it returns true when
  the player confirmed, otherwise it records the offer and gives the message to
  refuse with. Word the warning as what will happen ("The rack will be unlinked from
  W2 first. The W2 will pause for the change and carry on afterwards."), and name any
  progress lost. Then pause through `PausedChange.Hold(name, working, pause, resume)`,
  whose pause must never call `CrewWork.ManualStop`, make the change, and call
  `Release()`, which resumes through the machine's own checks and returns what could
  not carry on. `ConfigurationSheet` and `ProviderPanel` already run each press
  through a `PressGuard`, which arms on the offer and relabels the button "... again";
  your own panel hosts do the same. A knob or guarded switch uses
  `Confirmations.PressWithCard`. F3 parsers call `Confirmations.TakeWord(args, out
  confirmed)` and pass `Confirmations.Confirmed(action)`. A second press whose warning
  has changed is a new offer, never a blind go-ahead. Classify each do-first refusal
  in `config/panel-override-audit.json`; `scripts/audit-panel-overrides.py --check`
  fails on one that is not ([panel override audit](panel-override-audit.md)).
- **`Persistence.DefinitionMigrations`.** `Register(oldId, newId, recordRenames,
  rewrite)` converts saved objects of a retired definition to their new equivalent
  as the game spawns them (the ship's item list and each object's saved conditions),
  renaming the listed property-map records and optionally rewriting their values; one
  step only, idempotent, never touching a save file. The game rebuilds map points and
  socket adds from definitions on load, so a conversion only renames.

## Shared lines, ports and vessel links (0.57.0)

- **Framework-owned items (`Items`).** `FrameworkItems.Prepare()` returns Framework's
  own definitions; `FrameworkLifecycle.Begin` publishes them before raising
  `ContentLoading`, so content mods may add ports to them and name them in stock.
  `ItemEconomy` is Framework's economy data pack (`mods/PhobosFramework/framework/economy.json`,
  overrides in `BepInEx/config/PhobosFramework/economy`). `SharedLines.GasSpec()` and
  `ProcessWaterSpec()` are the two line families: the gas line keeps every saved id of
  Manufacturing's former propellant line.
- **`Liquids.LineFamilies` and `LineCommodities`.** `LineFamilies.ProcessWater` and
  `Gas` are network families with adjacency joins. `LineFamilies.Assign(commodity,
  family)` says which line carries a bulk commodity (water is preassigned); a
  commodity with no line links by touching only. `LineCommodities` is the same map
  by family id, free of game types for offline checks.
- **`Liquids.LinePorts`.** The port rule: `Water(footprint)` is the neighbouring tile
  on the local -X side and `Gas(footprint)` on the +X side, both in the middle row
  (the upper of two on an even footprint); `Acid(footprint)` is one row below the
  gas port. Each returns the point offset and the footprint socket that draws the
  joint. `LineDefinitions.AddPort` records the port against the spec's `Family`, and
  `LinePorts.Points(family, definition)` feeds the family's participant test. Only
  `Power` points should ever become electrical inputs.
- **Inventory roles and the load-time fit (0.70.0; owner direction, 1 October 2026:
  size every inventory to its job).** `MachineFamilies.Add` and
  `ApplianceDefinitions.Add` still start a family with the general 8 x 8 grid every
  saved family had; right after them, say what the inventory is for with
  `EquipmentInventory.Apply(d, prefix, spec)`: `InventorySpec.ProductTray(w, h)` (about
  one to two batches of the largest recipe), `ServiceRack(w, h)` (a few cells for hand
  work on a vessel whose contents are a record), `Storage(w, h, trigger)` (declared
  storage only), or `LegacyReceptacle(w, h)` for equipment that used to have an
  inventory and no longer does (a hidden grid that admits nothing; never a null
  container, because the game leaves the saved contents of a removed container
  unattached). `EquipmentInventory.Declare(co, spec)` records a container whose access
  the content builds by hand (a hidden `Feed` bin, a family with its own recovery
  action). `ApplianceDefinitions.SetRack` and `AddFeedBin` record their roles
  themselves. Changing a size needs no migration entry: `Persistence.ContainerFit`
  checks every declared container on the player's loaded ships against its live grid,
  moves saved items to free cells in place (`Inventory.GridFit`, pure), and puts what
  finds none on the deck through the game's own drop, with one crew-log notice per
  ship. Register `ContainerFit.KeepFirst(prefix, named)` for items a saved job names,
  so they keep a place before the rest. A locked container, or one with its window
  open, waits for a later pass.
- **Stacked delivery (0.71.0).** Deliver a batch's products with
  `Inventory.TrayDelivery`: `Plan(tray, products)` (null when there is no room),
  `Place()`, and `Rollback()` before destroying the products when the commit fails;
  `Fits(tray, (definition, count)...)` is the same question before the products exist.
  Units of one `StackUnits.Kind` share a stack: plain units (the definition's own mass,
  no Phobos record) by definition id, recorded units only when mass and records are
  identical. A batch tops up the stacks of its kind already in the tray, then forms new
  stacks up to the game's stack limit, larger pieces taking their cells first
  (`BatchPlacement.PlanStacked`, pure). A new stack is built with the game's
  `CondOwner.StackFromList` and placed with `Container.AddCOSimple`, as a saved stack
  is restored; a stack that takes more units is taken out, rebuilt with its head still
  on top, and put back at its cell, as the game pops a stack's head. Check placement
  with `StackUnits.Inside(product, machine)`, never by parent alone, and count stored
  items with `StackUnits.All`. `ContainerFit` uses the same delivery to give units
  without a cell to stacks of their kind before anything goes to the deck.
- **Any touching pipe joins (0.69.0; owner decision, 1 October 2026).** A port no
  longer says where the pipe must lie. It marks the equipment as a participant of the
  family and places the drawn joint. `FluidRouteCache` gives each participant its own
  tiles and the tiles north, south, east and west of them
  (`NativeFluidRoute.FootprintCells`, `FluidTopology.OnOrBeside`), so a segment under
  the equipment or beside any edge joins it, as a conveyor belt does. The old port
  tile is one of those cells, so earlier layouts still join. The one footprint tile
  that carries a family's joint still refuses that family's segment, because a
  segment forbids its own presence on its centre tile. Irrigation and coolant
  families are not networks and keep their named points.
- **Why a link is not offered (0.69.0).** `LineReach.Problem(machine, store, family)`
  returns one `ReachProblem` (loose, damaged, locked, no pipe at the store or the
  machine, a drained run, separate runs), classified by the pure
  `LinkDiagnosis.Classify`. `LinkChoices.Note(machine, link, offered)` turns the
  vessels of a link's commodity aboard in any state (`BulkVessels.AboardAnyState`)
  into the text a sheet shows under its choices; pass it as `EquipmentField.Note`
  (read only when the sheet opens) or to the `ConfigurationSheet.Choices`/`Objects`
  overloads that take a note. `LinkChoices.NetworkSummary(co)` lists what an object
  is joined to through each line, for a Details page. Show a link field whenever a
  vessel of its cargo is aboard, not only when one is in reach, so the note can be
  read.
- **`Liquids.VesselLink`.** One machine-to-vessel link: machine port, vessel-side
  bank primary, commodity and pairing direction (`machineSends`; a vessel that feeds
  a machine, like Agriculture's tank feeding a W2, is the sender). `Candidates`,
  `Connected` (same ship, ready, reciprocal, in reach; cheap enough for power steps),
  `Link` and `Unlink` apply the owner's link rule through `LineReach` and shared
  banks. Content keeps its own texts, guards and settlement.
- **`Controls.LinkChoices`.** `Label(machine, vessel, link, deposit)` names a
  candidate with how it is reached and whether it is full (destination) or empty
  (source); `LinkedMachines`/`LinkedNames(vessel)` list every linked machine for a
  vessel's panel, from any mod.
- **`Liquids.GasNetworkSafety`.** Content classes its commodities as `Oxidiser` or
  `Fuel`; `Mixed`/`Warning(participant)` report an oxidiser and a fuel store on one
  gas network, and `Review(ship, stores)` posts one Caution per ship through
  `PlayerNotices`. Advice only: nothing is blocked. Primary source for the practice:
  the U.S. Occupational Safety and Health Administration,
  [29 CFR 1910.253(b)(4)(iii)](https://www.osha.gov/laws-regs/regulations/standardnumber/1910/1910.253)
  (oxygen cylinders in storage kept 20 feet from fuel-gas cylinders or behind a
  five-foot, half-hour fire-rated barrier). Applying it to a line that holds only a
  few grams of gas a tile (since 0.63.0) is our design choice.


## Water tanks, machine families and definition retargeting (0.58.0)

- **`Items.WaterTanks`.** The Rivetline process water tanks every mod shares:
  `WaterTanks.All` is the S2, S3, S4 and S5 (`WaterTank`: model, footprint, prefix,
  capacity, dry mass, price, record names), `For(id)`/`IsTank` identify any form,
  and each tank is a registered `BulkVessel` of `water` with a general inventory, a
  process-water port and Framework's `PhobosFrameworkTankControls` panel. The S3 to
  S5 keep Shipbreaker's definition ids, record names and record owner
  (`WaterTanks.RecordOwner`), so saved silos read unchanged. The S3's capacity and
  dry mass come from Framework's `vessels` pack (`ItemVessels`); the rest follow the
  size ladder. `TankEconomy` derives price, work, bills and salvage per size from
  the S3's economy entry. Content adds its own work to the published tank
  definitions with `DefinitionAmendments` (Agriculture adds charge loading, trapped
  water recovery and draining) and never republishes them.
- **`Registration.MachineFamilies`.** `Add(d, prefix, buildCategory, conditionName,
  name, shortName)` generates the four forms (Installed, Loose, InstalledDmg,
  LooseDmg), family condition, triggers, mode-switch damage and native install,
  uninstall and repair jobs under the exact ids Shipbreaker's machines have always
  used, so equipment that changes owner keeps every saved id and queued job.
  `Border(footprint, interior)` builds the socket rim.
- **`Persistence.DefinitionMigrations.Retarget`.** `Retarget(oldId, newId, oldDryKg,
  newDryKg, definitionOnly...)` is a conditions rewrite for `Register`: the saved
  object takes the new definition's starting conditions plus every saved condition
  that differs from the old definition's (wear, progress, locks, marks), leaving out
  the definitions' own stats and the listed family marks, and its saved mass moves by
  the difference in housing. Agriculture 0.31.0 uses it to turn saved R3 to R5
  reservoirs into S3 to S5 tanks, with `recordRenames` moving the water record,
  journal and guard to the tank's names.

## Ship's Water tanks on the water line (0.59.0)

- **`Liquids.ShipsWaterPorts`.** With the pinned Ship's Water 0.16.1 loaded, Framework
  amends its installed tank definitions (drinking water and waste, small, medium and
  large, intact and damaged) in place at content load: one `LinePorts.Water` point, the
  water line's joint on the footprint tile beside it, and the redraw through
  `LineJoints`. The tanks' own sprite-sheet trigger, draw order and data are left alone;
  a definition whose footprint or port tile differs from the inspected one is skipped.
  `Amend(objects, items)` is the table-level step, for offline checks.
- **`Registration.LineDefinitions.AmendPort`.** The in-place counterpart of `AddPort`
  for a definition another mod published: point, joint and redraw, or nothing when the
  footprint or socket is not as expected. Never republish a foreign definition to add
  a port.
- **`Liquids.ShipsWaterSupply` reach.** `Refill(via, ...)` and `DepositWaste(via, ...)`
  take the Phobos object the water enters or leaves, and use only tanks it reaches by
  `LineReach` (touching, or on its process-water network). The crew reserve is still
  counted over every drinking tank aboard. `ReachableTanks(via, waste)` lists them for
  status text. The 0.58.0 ship-wide overloads remain, marked obsolete, for content
  built against them; new callers use the `via` forms.

## Spills held by a bund, and what a segment joins (0.60.0)

- **`Liquids.BulkVessel.Contain`.** `Contain(co, kg, reason)` moves up to `kg` of a
  vessel's service contents into its catch chamber (`StoredCommodity.Contain`): mass
  and capacity are unchanged, the move is logged, and the contents wait for
  `Recover`. Use it where a spill stays with the vessel (a bund), and `Drain` where it
  leaves. A protected vessel moves nothing.
- **`Liquids.LineReach.MembersThrough`.** The participants on the network that runs
  through a line segment (`FluidTopology.ParticipantsOn(cell)` on the cached
  snapshot), so a segment switching to its damaged form still answers for the
  network it belonged to. Manufacturing 0.24.0's acid line uses it to find the tank
  a wet segment carries acid from.

## Conveyor belts and unit transfers (0.61.0)

- **`Inventory.BeltNetwork`.** The Rivetline conveyor belt as a plain segment family
  (no port participants). `Joins(ship, starts, goals)` is true when one run of intact
  belt lies on or beside a start cell and a goal cell (`FluidTopology.JoinsNear` on
  the cached snapshot); `Reaches(a, b, aCells, bCells)` adds the touching test, which
  is the owner's link rule for items. Content chooses each endpoint's cells. Items
  never ride a belt; transfers stay the content mod's checked moves.
- **`Inventory.UnitItemTransfer`.** One unit into a container, detached from a native
  stack if it is in one, with `IsUnitPreflight` so content admission knows a single
  unit is being asked about; moved from the crew hauling orders, which use it
  unchanged (`CrewLogistics.IsUnitPreflight` delegates to it).
- **`Items.SharedLines.BeltSpec`.** The belt's segment spec, published with the other
  Framework lines. See the [conveyor design record](conveyor-design.md).

## Unit admission, store footprints and the belt display (0.62.0)

- **`UnitItemTransfer.AsUnit(item, check)`** runs any admission question with
  `IsUnitPreflight` true for the item, so a content rule judges one unit of a stack at
  its own mass; `Stacked` and `UnitKg` are the matching helpers. Shipbreaker's routed
  receivers use it.
- **`BeltNetwork.FootprintCells(co)`** lists every tile an object covers from the item's
  live width and height (the game swaps them on rotation); pass them as a store's cells
  so a belt on any side joins it. **`BeltNetwork.PathCells`** gives the belt cells
  between two sets of cells, for display only.
- **`Inventory.BeltCarriers`** shows a copy of a travelling item along a belt path:
  `Show(key, ship, item, path, progress)` and `Hide(key)`. Presentation only, no
  collider, never saved; the `Belts/ShowMovingItems` setting turns it off.

## Lines that hold their contents (0.63.0)

Owner decisions of 1 October 2026: lines hold what they carry until drained, a gas
line may hold any mix, and drained contents go into a hauled canister. The design
record is [line contents](line-contents-design.md).

- **`LineContents.Declare(family, prefix, commodities)`** makes a network line family
  hold contents. Pass its definition prefix (the shared four forms) and one or more
  `LineCommodity`:
  - `LineCommodity.Liquid(name, density, bore, mistSpecies, mistFraction)` for a
    liquid;
  - `LineCommodity.GasOf(name, roomSpecies)` for a gas, with a molar mass for a gas the
    game has no room condition for, which vents overboard;
  - `LineContents.AddCommodity` adds a gas another mod carries.
  Declare when you prepare definitions, and call **`LineContents.OfferActions(d,
  prefix, gas)`** after `LineDefinitions.Add` to offer the drain or vent and
  return-to-service actions.
- **Filling is Framework's.** Every two seconds each open run is topped up from the
  registered bulk vessels on its network, only from their available kilograms. Your
  transfers need no change.
- **Closed runs.** A closed segment is left out of the network, so `LineReach` and
  `VesselLink.Connected` stop reaching through a drained run. Read one with
  `LineContents.IsClosed`, `Read` and `Held(Run(...))`.
- **Removal and damage.** Framework refuses uninstalling or dismantling a segment that
  holds anything, and releases gases and declared mists on damage and destruction. Do
  not add your own spill for a holding family.
- **Canisters.** `DrainCanisters` reads, fills and pours `PhobosLineDrainCanister`. A
  store without a general inventory can offer a canister rack with
  `ApplianceDefinitions.SetRack(d, prefix, DrainCanisterDefinitions.RackTrigger, 2, 2)`.
  Canisters in a registered vessel's container pour in on the next pass.

## Pumped circuits and canister receivers (0.64.0)

- **A holding family may be a pumped circuit.** Declare a family without ports
  (`LineHoldUpFamily.StoreFilled` is false) and fill it yourself:
  - `LineContents.Circuit(ship, family, pathCells)` lists the open segments on the
    runs through a path;
  - `Room` and `Holding` measure them, and `Full` tests them;
  - `Top(segments, family, commodity, kg)` fills them from kilograms you have already
    taken from your own store, returning what it used. Keep the rest.
  Shipbreaker's F6 coolant conduit is the example.
- **`DrainCanisters.RegisterReceiver(ICanisterReceiver)`** lets a machine take a
  canister's liquid from its own inventory. `Accept(machine, commodity, kg)` records
  the kilograms in your custody, adds them to the machine's mass, and returns what it
  took; Framework removes the same from the canister. Refuse with zero when the
  machine is not safe to fill.

## Ship's Water tanks fill water lines (0.65.0)

- The top-up pass treats Ship's Water drinking tanks on a process-water line as sources,
  after the Phobos stores on the same run (owner decision, 1 October 2026). For your own
  draws, use `ShipsWaterSupply.IsDrinkingTank`, `LineAvailableKg(ship, tanks, reserveKg)`
  and `DrawForLine(ship, tanks, kg, reserveKg)`. They keep the reserve across every
  drinking tank aboard and skip a tank whose transfer journal is open; Framework's own
  pass uses the `WaterTanks` `CrewWaterReserveKg` setting.

## Whole machines and retired parts (0.67.0)

Owner direction, 1 October 2026: a machine is never assembled from several identical
sections. Shipbreaker's D4, R4 and F6 come whole; saved sections convert on load.

- **`Construction.SectionAssembly.RetireFromMenu()`** (replaces `PreferAssemblyMenu`).
  Call after native generation. For each registered section job, INSTALL lists the
  other job that installs the same form in the same tab (the whole machine's own
  `<Prefix>LooseInstall`), whatever the generation order; with none, the section job
  simply leaves the menu. The section loses its own Install offer. The job and its
  interactions stay registered so saved sites load, finish and cancel unchanged.
- **`SectionAssembly.SetWholeAppearance(jobId, wholeId, installedId, appearance)`**
  shows the construction stages on a whole machine's own Install site: early until
  the loose machine is in the site's lot and work has started, then intermediate.
  Appearance only; the native job, its input, work target and completion are the
  game's. A job id cannot be both a section job and a whole-machine site.
- **`Persistence.LegacyItemConversions.Register(retiredId, unitKg, setSize, wholeId,
  wholeKg, materials...)`** converts retired loose parts that cannot be renamed one to
  one. Registration refuses any rule whose set does not weigh the whole item or whose
  materials do not weigh the part. Every 15 real seconds, on each loaded ship the
  player owns:
  1. saved section sites whose bill is incomplete are cancelled through the game's
     `Placeholder.Cancel` (complete sites are left to finish);
  2. free parts (not installed, stacked, reserved by or inside a construction site)
     are taken in ID order. Each complete set becomes one whole item and each
     leftover its materials, dropped with the game's `Ship.DropCO` where the part, or
     whatever held it, lay.

  Outputs are created and mass-checked before any part moves. A failed placement puts
  the parts back and removes the outputs. One crew-log notice summarises each sweep.
  Merchant stock and other ships are untouched until the parts come aboard. Register
  in your content's confirmation step, after recipes are published, so the materials
  can come from the retired recipe itself (`ConstructionRegistry.Find(recipeId)`
  returns a copy of a registered recipe).
- **When to use which.** One old definition to one new definition: use
  `DefinitionMigrations`, which edits the data the game is about to spawn. Several
  parts to one whole, or one part to many materials: use `LegacyItemConversions`.

## Rectangular appliances and patient services (0.82.0)

First consumer: Phobos Medical's 3 x 5 Ward-3 bed.

- **Rectangular families.** `ApplianceDefinitions.Add(d, prefix, name, description,
  width, depth, kg, price, image, controls, kW, tab)` builds the four forms `width`
  tiles across and `depth` tiles front to back: socket grids `width` columns wide with
  the one-tile border ring, the use point in front, the power points in the wall row
  behind the back edge (`WallRowY(depth)`). The square overload calls it with
  `width == depth` and builds exactly what it built before.
- **`Health.PatientFacts.Read(person)`** returns a read-only snapshot: blood lost
  (`StatBlood` counts loss), infection, pain, and each wound's cut, blunt, bleed rate,
  infection rate, staunched, fractured, splinted and vital state. The game's wound
  list also walks a dragged body, so only wounds that lead back to the person are
  kept. Reading never changes anyone; missing figures read as zero, as the game's own
  lookup does.
- **`Health.PatientPlacement`** puts down a person someone is dragging at a named point
  of a piece of equipment, with the game's own Drop Corpse calls (`UnSlotItem("drag")`,
  `Ship.AddCO(body, true)`, the room at the body's position). `CanPlace(dragger)` says
  why not (nobody dragged, dead, awake, no ship); `Place` returns the person or null.
  The person's own queued actions are left as the game made them.

## Weightless care and the crew list (0.84.0)

- **`Health.WoundGravity`.** The game multiplies a weightless patient's wound-healing rate
  (which also eases bleeding) by 0.05 in its private `Wound.Run`. Framework replaces that
  one constant with `WoundGravity.Factor(patient, 0.05)`: the larger of 0.05 and the patient's
  `StatPhobosWoundGravityFactor`, capped at 1. Set the stat with `SetCondAmount` while your
  equipment cares for someone and clear it when it stops; nobody has it otherwise. The patch
  is applied by hand at start-up; if the game's IL does not have exactly one such constant,
  nothing is changed and `WoundGravity.Available` is false, so say that to the player.
  `Rewrite` is pure and checked against a synthetic method and the game's own IL.
- **`CrewWork.Crew()`** returns the player's loaded crew from the company roster the game's
  time skip uses.

## Touching footprints (0.86.0)

`Observations.Footprints.Touching(a, b)` says whether two installed objects on one ship touch:
side by side or one tile apart without overlapping, from each item's width and depth turned by
its rotation (`OnDeck`). The pure `Touching(ax, ay, aw, ah, bx, by, bw, bh)` takes centres and
sizes in tiles. First used by Phobos Medical's Vigil-2 to find its bed.

## Wound care and medical crew work (0.89.0)

- **`Health.WoundCare`** treats a wound the game's own way. A wound part `Wound<Place>` has
  the item slot `WoundItem<Place>` (`ItemSlot(part)`). `Fits(item, slot)` reads the item
  definition's own slot effects; `Occupant(patient, slot)` says what lies there. `Apply`
  slots one single, loose unit (detach it from its stack first with `StackUnits.Detach`)
  through the game's `Slots.SlotItem(slot, item, bAuto: false)`, so the item's own slot
  interaction does the treating. An item the game uses up on the wound counts as success.
  `RemoveToDeck` unslots a spent item and sets it down beside a given object; `SetDown`
  does the same for any loose item. Nothing is ever destroyed.
- **`CrewRole.Medical`** for medical crew work, on by default per crew member like the other
  roles except Exterior.
- **`CrewWorkOffer.ExcludedActor`**: the full id of one person who must not take the work,
  such as the patient a medic treats. Empty means anyone eligible.

## Add-ons (0.90.0)

A content mod that loads its packs through `DataPacks.Load(source, validate)` supports add-ons with no further work: the loader asks `AddOns.For(source.ModFolder, version of source.Assembly)` and applies each add-on's `phobos/<ModFolder>/<schema>/*.json` between the shipped pack and the player's files. Three things to know when you add a schema:

- **New entries must be keyed at the second level** (`recipes.<id>`, `tables.<id>`, `crops.<id>`), because the id-prefix rule is checked there. Tuning a shipped key is always allowed; adding one needs the add-on's prefix.
- **Anything saved by number needs a derived number.** Register an overlay preparer as `RecipeSchema.PrepareOverlay` does, so an author never picks a number that another add-on also picked.
- **Validate on every file.** The validator you pass runs on the shipped pack and on each override on top of the ones before it; put authoring-only rules (pricing) in tests, not in the validator.

Files a mod reads outside `DataPacks` (War Declared's schematics) call `AddOns.For` themselves. The player guide is `docs/publishing-an-add-on.md`; keep it and `examples/addons` current when a schema changes.
