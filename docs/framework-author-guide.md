# Phobos Framework 0.15.0 — author guide

Current extension: Framework 0.21.0 owns [shared completion cues](shared-completion-cues.md).
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
work. See [capture commands, metrics and owner-run checks](performance-captures.md).

## Additive item loot (0.14.0)

`Registration.AdditiveLoot.SetItemChoice(definitions, tableId, branchId, chances)`
adds an optional, mutually exclusive item choice to an existing native item loot
table. Supply a stable `Phobos`-prefixed branch ID and item-ID-to-probability map;
finite nonnegative probabilities must sum to at most one. Zero choices disable
the owned addition. Identifiers accept letters, digits, underscores and dots.
The branch ID must differ from its parent table ID.

The helper clones unstaged native definitions, keeps native/foreign entries,
replaces only its own standalone branch, and creates one cumulative native
choice expression. Each added branch yields at most one item. Publish using
the existing `NativeDefinitions` transaction after all content has prepared.
It never restocks merchants or rewrites inventories. Missing/non-item parent
tables fail preparation rather than silently inventing a replacement pool.

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

Auto Nav 0.5.0 is the first consumer. See [its saved-flight contract](auto-nav-persistence.md)
for an example of hardware/ship binding, captured parameters, cumulative progress
and revalidation before controls resume. Existing material-port records keep
their original schema and pause behaviour. Consumers using this new API must
declare Framework **0.11.0** or later.

## Industrial controls (0.10.0)

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
they must not conflict with any live provider. Do not put this pack in OCF's
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
- `TransferClock(itemId, duration)` binds 1–60 seconds of work to an item.
  `Advance(itemId, elapsed, powered)` rejects changed identities, invalid deltas
  and gaps over 60 seconds; unpowered calls earn nothing. Progress caps at Duration.
  The owner manages pause/reload, checks current ownership and binds credit to an
  actual paid power interval. This class neither charges electricity nor saves itself.
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
See [native precedent, format and tests](material-port-pairing.md).

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
and caps it at one. Each offer yields at most one item. Conditions are Pristine,
Refurbished, Worn (15% wear), or Broken (supply a damaged definition). Native
merchant stock updates normally; registration never forces restocking.

`Registration.MaintenanceDefinitions` supplies native Restore/Dismantle builders,
stat replacement, and a runtime-referenced remainder item helper. Supply your
own balance and output arrays. Restore reduces wear in place. Dismantling
registers shared cargo/repair-lot guards; `emptyInternalBin` permits only a named,
empty, zero-mass system slot, removed at completion. `ReturnRepairMaterials`
registers a repair finish action to return actual lot mass in 0.5 kg spent packs.
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

Recipe schema 1 optionally accepts `toolTriggers`, up to eight distinct native
tool selectors. They are reusable tools fetched through native `Use` handling,
not mass-bearing ingredients. Existing packs omitting the field still work.
Output overlays are supported only when their base defines explicit mass and they
have no arbitrary condition loot. Consumer prices, repair bills, yields and stock
locations belong in the consumer. See [current consumer balance](equipment-economy.md).

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
the two concrete consumers; see [automatic routing](automatic-material-routing.md).

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

[Agriculture water conduits](agriculture-water-conduits.md) are the first consumer.
They keep one pump/rack pair per circuit. Return flow, fluid temperature
and multi-consumer fairness need concrete additional contracts; scalar `water`
must not be used to erase nutrient composition or manufacture cooling capacity.

Framework 0.19.0 adds two-component `LiquidMixture` and `IMixtureReservoir` for
[Agriculture's finite nutrient solutions](agriculture-nutrient-solutions.md).
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
[F6 sealed coolant routing](furnace-coolant-conduits.md). It accepts explicit
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
observations or a CFD solver. See [operating contracts](fluid-network-operations.md).

### Fluid network extension (Framework 0.20.0)

`Inventory.PortBank` provides a bounded bank of existing reciprocal ports; slot
zero preserves the caller's original single-port ID. Admission, ship scope,
permissions and route discovery remain content responsibilities. `FluidLine`
retains a two-component parcel, immutable occupied route binding, capacity and
remaining transit. Its endpoint custodian must include contents in physical mass,
use transfer guards, block removal and offer a finite drain. Missing state starts
empty; never infer fluid from geometric pipe volume. `HydraulicRoute` supplies a
bounded authored resistance fraction and equal budget shares, not pressure
observations or a CFD solver. See [operating contracts](fluid-network-operations.md).

## Recycler reject reservation — Framework 0.22.0

`ShipsWaterRejects` provides a version-scoped opt-in reservation around Valtora’s
[Ship’s Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
private Recycler settlement. A content sink owns pairing, access, storage and
journals. Framework bounds source processing and measures same-ship tank deltas;
it supplies no nutrient assay. `CollectorCargo` admits registered physical cargo
under Shipbreaker’s existing capacity and native container checks; its endpoint
validator retains Shipbreaker’s mount rules. Neither API authorizes automatic
industrial routing. See [the concrete consumer](agriculture-nutrient-production.md).
