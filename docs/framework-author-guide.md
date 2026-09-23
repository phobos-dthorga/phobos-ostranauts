# Phobos Framework 0.6.0 — author guide

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

[BepInDependency(FrameworkInfo.PluginId, "0.6.0")]
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
