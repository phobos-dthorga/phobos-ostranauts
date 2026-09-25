# Automatic material routing — 0.9.0

The later [F6 routing study](furnace-material-routing.md) proposes aluminium feed
and released casting collection. Those routes are not part of this implemented
residue system; existing filters and saved ports keep their meaning.

The transport contract below remains current. UI names in the 0.9.0 walkthrough
describe the earlier fallback panels. For 0.10.0 local and central controls, use
the [industrial console player guide](industrial-console-player-guide.md).

Prepared Framework **0.9.0**, Shipbreaker **0.9.0**, Auto Nav **0.3.0**.
Built and checked offline against installed Ostranauts **1.0.1.5**. Gameplay
verification remains with the owner. This extends existing equipment and uses
its current artwork; no extra belt appliance or construction bill is required.

## Connect the equipment

The simplest chain is:

```text
Grabber → chute → dismantling fixture
                         output → reclaimer input
                                  reclaimer output → residue collector
```

Alternatively, retain an existing fixture → collector pair and use that collector
as a buffer: its separate output can feed the reclaimer. A second collector can
receive the reclaimer's rejects. Each port has one partner; receiving and sending
ports on the same machine are independent. There is no broadcast or automatic
nearest-machine selection.

1. Open the reclaimer's **Control Panel → Input routing**. Select the fixture or
   buffer collector with **Link**. The filter is **Identified feed only**.
2. Stand beside the reclaimer and press **Start transfers**. This powers receipt
   of eligible packets; it does not start processing them.
3. In the reclaimer Control Panel, press **Start / resume reclaimer**. An empty
   armed queue waits for arriving feed. With `Processing/ContinueQueue=true`, it
   keeps processing subsequent arrivals until paused or blocked.
4. Open **Output routing** and link a collector. At that collector's **Input
   routing**, choose **Terminal rejects only**, then **Start transfers**.

For the buffered variant, use the first collector's **Output routing** to link
the reclaimer. Start incoming transfers at each receiving endpoint. The fixture's
F9 **Output routing** opens the same controls from its end. The original processor
Control Panel artwork was not included in 0.9.0. Version 0.10.0 implements the
[industrial console and equipment panels](industrial-console-player-guide.md),
including a shared faceplate and checked same-ship commands.

Linking is allowed beside either endpoint. Starting, pausing and changing a
receiver's filter require access to that receiver. All console actions use the
same service and checks. Collector **Inventory** remains its finite cargo space;
reclaimer **Input routing → Inventory** opens its private feed, while ordinary
reclaimer Inventory remains products.

## Eligibility and backpressure

| Receiving port | Available filters |
| --- | --- |
| Reclaimer feed | Identified R2 residue only |
| Collector | All three residue types; identified feed only; terminal rejects only; legacy residue only |

Old links and port names retain their meaning. A collector without a saved filter
keeps its existing three-type acceptance. A reclaimer without a saved filter
accepts identified feed only. Explicit filters survive unlinking and save/reload.
Changing a filter pauses that receiving route and resets its short transfer
timer. It never changes or removes cargo already held. Unknown, future or
incompatible saved filters block transfer until the player selects a valid one.

Every route checks the full IDs, reciprocal pair token, same loaded ship,
installation, damage, locks, receiver switch/signal state, structural floors,
exact item identity/mass, absence of stacks or contents, and destination capacity.
Collector sources also retain their hull-mount checks. Routes do not cross ships,
bare space, EVA floor or cargo webbing. Native electrical conduit is separate.

The item remains in the sender for the complete transfer interval. Only a checked
move of that same object completes delivery; there is no intermediate virtual
inventory or duplicate cargo. Full receivers wait with the item and partial
transfer work retained. Missing/changed endpoints or a broken route pause;
restore the route and explicitly resume. The reclaimer holds at most four
13 kg packets including its active input, and the collector retains four slots
and its 52 kg limit. Processing still pauses when its product tray is blocked.

Useful metal stays in product inventories for hauling, maintenance or sale.
Legacy unclassified residue cannot enter the reclaimer, and terminal rejects
cannot yield another recovery pass. Nothing is ejected or destroyed by routing.

## Power, time and controls

Default reclaimer feeding takes **2 powered seconds per packet** and adds
**2 kW** to that appliance while receiving. Idle plus feeding requests 2.1 kW;
processing plus feeding requests 14 kW at default settings. Total actual supplied
energy follows the existing native room-heating path, including partial brownouts.
Processing and feeding gain no work during a brownout. Adding feed demand does
not accelerate processing. Cooling failure pauses receiving and processing at the reclaimer. A separate
collector can still remove its stored output.

Collector receiving uses the current code default of 5 seconds / 2 kW total
operating demand (`CollectorRules.CycleSeconds`); an existing saved
`Collector/TransferSeconds` setting takes precedence. Earlier prose stated two
seconds, which is the reclaimer feed default rather than the collector default.
Sending stored output does not add a second sender-side power charge. Short
native power ticks can overrun a transfer's final fraction; all delivered
reclaimer energy still becomes heat. Time gaps over the transfer clock's
60-second limit pause it instead of granting catch-up work.

Shipbreaker settings, read at startup:

| Section/key | Default | Bounds |
| --- | ---: | --- |
| `Routing/FeedSeconds` | 2 | 1–60 seconds |
| `Routing/FeedKilowatts` | 2 | 0.1–100 kW additional demand |
| `Routing/ContinueFeeding` | true | Keep receiving after each packet |

These are separate from `Processing/ContinueQueue` and the existing collector
settings. Pausing processing does not implicitly empty or disable its input
route; use **Pause** in Input routing when you also want to stop deliveries.
After reload, saved jobs, pairs, filters and physical cargo survive, but both
transfer and processing permissions reset to paused. Transfer clocks reset;
saved processing work and recipe contracts retain their meaning.

```text
phobosroute status
phobosroute link <full sender ID> <full receiver ID>
phobosroute filter <full receiver ID> <all|feed|rejects|legacy>
phobosroute start <full receiver ID>
phobosroute pause <full receiver ID>
phobosroute controls <full object ID> <send|receive>
phobosroute unlink <full object ID> <send|receive>
```

Use full IDs from status. The direction is mandatory when unlinking a dual-port
machine, so clearing its input cannot accidentally sever its output. Existing
`phoboscollector` commands remain available; use `phobosroute` for both directions.
Routing never starts processing: use the reclaimer panel or `phobosreclaimer start`.

## Shared ownership and verification

Framework supplies persistent exact-ID `SavedPortFilter` records, alongside the
existing reciprocal pairing, finite transfer, clock and floor-search helpers.
Filters live in `PhobosMaterialFilter.<port>`; pairs keep
`PhobosMaterialPort.<port>`. Neither writes native signal settings. Content owns
eligible equipment, filter choices, physical routes, power and gameplay controls.
There is one transfer service for collectors and reclaimer input, not a parallel
conveyor implementation. This is not a general network, fluid API or scheduler.

Offline checks cover separate input/output pairs, native saved filter round trips,
legacy defaults, malformed/foreign records, incompatible filters, safe unlink,
full IDs, additive energy/heat and unchanged native definitions. Existing transfer
checks cover blocked placement, rollback and duplicate delivery. Owner gameplay
checks should focus on the new connected chain, backpressure, independent pause,
filters and save/reload; established basic conduit behaviour needs no separate test.

## Game update observed

The developer's [1.0.1.5 announcement](https://store.steampowered.com/news/app/1022980/view/703280393464317006)
describes fixes to PDA work-order duty categories, residual planetary gravity
after native autopilot, and several ship layouts. The public Steam news API
provided the announcement text when the rendered page was unavailable.

Local Steam build is **25401711**. The installed Unity metadata and assembly
identify **1.0.1.5**; Assembly-CSharp SHA-256 is
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
This matches the assembly previously used for 0.8.0 checks: the earlier 1.0.1.4
label followed owner screenshots, not a fresh binary version check. Current
package metadata now reflects the installed 1.0.1.5 baseline. The power hooks and
native property-map save schema remain available in these files. Patch notes
alone do not establish gameplay compatibility or validate our Auto Nav controller.
