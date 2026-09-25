# F6 material routing: implementation design

25 September 2026. Exploration against Shipbreaker 0.14.0 / Framework 0.17.0.
**Not implemented.** Current F6 feed loading and product collection remain manual.
This designs the authorized next automation direction around the existing
housing cycle; the [heat-sink proposal](furnace-repair-castings.md) is independent.

## Recommended connected slice

```text
R4 product tray -- aluminium only --> F6 existing physical input bin
                                      |
                    crew seals, starts, equalizes and releases
                                      |
F6 released product tray -----------> existing hull collector
                                      crew hauls to bench/storage

R4 residue output -----------------> its existing separate residue collector
F6 cooling connection -------------> F6-R or F6-P (unchanged)
```

Use one explicit aluminium source and one explicit output destination per F6.
Initially support the R4 as source; defer arbitrary containers, splitters and
fan-out. Retain crew finishing/repair jobs. Routing permission never authorizes
Seal, Resume, Equalize, Release or another batch. This removes repeated hauling
while preserving the process decisions and finite inventories.

## Inspected reuse and concrete gaps

| Existing implementation | Required extension |
| --- | --- |
| `RoutingRules.SendPort` is published `PhobosShipbreaker.ResidueOut`; R4 already uses it for rejects | Add a separate R4 `PhobosShipbreaker.MetalsOut` logical address; never steal the existing residue pair |
| Framework `PortPairing` saves reciprocal full IDs and port IDs | New F6 `PhobosFurnace.MaterialIn` and `PhobosFurnace.MaterialOut`; cooling pair remains separate |
| `CollectorService` has one receiver session, physical item binding, `TransferClock`, `PhysicalTransfer.Commit` | Adapt the same service to F6 receiving and explicit released-product acceptance; do not build another conveyor engine |
| `RoutingRules.FilterIds`, `CollectorRules.Accepts` and native collector inventory permit only three residue identities | Add an explicit furnace-products filter with exact identities/masses and native admission; leave `all` and unsaved defaults residue-only |
| Collector has four item slots / 52 kg ceiling and a 2 × 2 inventory grid | Preserve bounds; a 2 × 2 housing blank fills the grid, so its remainder must wait until the blank is hauled away |
| `CollectorRoute.Cells` assumes every non-collector machine is 4 × 4 | Replace the assumption with content-owned endpoint geometry, including the 6 × 6 F6 |
| F6 input already validates exact 1 kg aluminium, no stacks/contents/repair lots, at most 20 objects | Reuse that predicate and native `AllowedCO` / `CanAddSimple`; never widen to all `IsAluminum` objects |
| `FurnaceService.BeginPower` / `FinishPower` owns the furnace's measured electrical receipt | Budget feed movement from that same receipt, including motor heat; never independently debit or count the interval twice |

`CollectorLinks.Sender`, `Receiver`, `Peer` and UI currently infer ports from the
machine family. With a dual-output R4 they must take a concrete logical address;
ambiguous `send` must retain its historic residue meaning. Add an explicit
`metals` selector rather than silently retarget existing commands or saves.
All UI/F3/C1 paths resolve the same address and checked command service.

No new public Framework API is currently justified. Port pairing, exact filters,
bounded floor search (4,096 visited cells), native physical transfer and clocks
already exist. Content-side endpoint descriptors can consolidate family tests,
inventory selection, allowed payload and geometry without changing those APIs.

## Placement and permissions

Proposal: designate furnace-local **(-2.5, -2.5)** as the input approach tile and
**(+2.5, -2.5)** as the output approach tile. These are front-corner tiles inside
its 6 × 6 floor footprint; they rotate with the furnace. Keep the central front
operator access and existing side/rear cooling sockets clear. Add named material
points and show them in the existing live installation schematic. These points
are a design, not already existing native machinery sockets.

Reuse the current underfloor structural-route model. Require the same loaded,
authorized ship, intact installed endpoints and valid supporting floors; exclude
EVA/flexible floors, walls and bare space. Revalidate movement/rotation, floor
loss, damaged endpoints and reciprocal pairs before each commit. A material path
does not turn painted cooling pipes or electrical conduit into transport lines.
The collector retains its hull mounting/exterior checks.

Local input Start/Pause is separate from heating. C1 uses the same ship boundary;
opening inventories stays local. Input Start requires an idle, cool, unsealed F6,
valid feed bin and cooling installation, fresh instrumentation and no flight
preemption. It may wait for scrap but stops at exactly twenty units. Sealing
cancels any transfer clock and pauses feed before binding the exact charge IDs.
After release the next charge requires explicit receiving Start. Reload retains
pairs/filters/cargo and resets receiving permission and clock progress.

Output collection remains receiver-owned: start it at the hull collector. For the
first slice transfer only when the furnace is idle, cool, unlocked and outside
any protected/mutation/delivering state. Do not extract captive charge, infer
solidification from a sprite, or call Release on behalf of a collector. An
unqualified recovered charge remains in the feed bin for local handling.

## Mass, power and backpressure

Only **existing physical objects** move. Select and bind one full item ID, retain
it at the sender throughout the delay, and recheck source membership, mass,
filter, access and destination fit immediately before transfer. A replacement
item cannot inherit elapsed transfer work. A full receiver retains the original
object and progress at the source. Neither unloaded ships nor wall-clock time
advance transport. Time gaps above the existing 60-second limit pause it.

Proposed F6 feed default reuses **2 seconds / 2 kW**: twenty individual pieces
require 40 powered transfer seconds and 80 kJ of motor work, plus instrumentation.
Below-full power can supply instrumentation first; allocate remaining measured
kJ to equivalent motor seconds, capped by elapsed time. All motor work becomes
accounted sink heat; a full sink or missing connection blocks feeding. No heat
goes to melting while idle. Electrical loss/flight immediately removes feeding
permission while preserving cargo and passive physical cooling. UI reads do no work.

The existing collector source default is **5 seconds / 2 kW**
(`CollectorRules.CycleSeconds`, used by `Settings`), despite older prose saying
two seconds. Respect the owner's saved `Collector/TransferSeconds` setting.
Moving a blank and a remainder therefore needs 10 powered seconds / 20 kJ at
the current default, with waiting for cargo space additional. Receiver-side
electricity and room heat retain the existing collector accounting; the sender
does not incur a second charge. Together these idealized transfers add 100 kJ
(0.0278 kWh), excluding idle/instrumentation and native tick overrun. This is a
design calculation, not a measured game result.

The four-slot limit is not four blank capacity: spatial packing still applies.
Attempt the exact native placement; never resize, compress or stack a casting to
make it fit. Clearing the collector remains useful crew work. The F6 product tray
can retain its current outputs while an unavailable destination blocks transfer.
Selecting furnace-products pauses that collector and leaves existing residue
cargo intact; it does not transform `all` into a wildcard. Proposed accepted
first identities: 19 kg `PhobosFurnaceHousingBlank` and 1 kg
`PhobosFurnaceMeltRemainder`. Add a future heat-sink cluster only when its recipe
ships. Finished bench products are not emitted by this furnace route.

## State and failure walkthrough

| Situation | Input route | Output route / material result |
| --- | --- | --- |
| Cool idle, fewer than 20 valid pieces | Receive after explicit Start | Released stock may leave with collector permission |
| Twenty pieces received | Pause complete; no automatic Seal | Other cargo remains physical |
| Seal or running/hot batch | Pause; locked exact charge cannot move | Conservative first slice waits for idle/cool |
| Output tray blocked during Release | No new charge | Existing release checks retain charge; routing cannot bypass commit |
| Full collector | No effect on captured charge | Wait with exact object at sender; no deletion |
| Brownout or power loss | Partial measured work or pause; no free movement | Existing receiver power rules apply |
| Failed probe / full cooling store / flight | Pause input; physical heat continues cooling | Wait for safe furnace state; retain cargo |
| Destroyed floor, damaged equipment, moved/rotated endpoint | Invalidate route and pause | No fallback to nearest machine |
| Pair/filter change | Reset affected clock and pause | Other logical ports retain their pairs |
| Hot reload / protected interrupted output commit | Input remains paused | No transfer from protected state; no duplicate products |

## Concrete later implementation order

1. Refactor content endpoint resolution with legacy-address regression checks.
   Add R4 MetalsOut and F6 MaterialIn/Out, per-port filters and rotated geometry.
2. Extend the existing transfer service and receipt allocation for cold F6 feed.
   Preserve the legacy residue route and the reclaimer's additive power behavior.
3. Add opt-in furnace-product collection with current bounds; wire local/C1/F3
   controls and localized waiting/fault messages through the same services.
4. Verify an existing saved R4-to-collector link alongside its new metals route,
   all four rotations, stack/foreign-feed rejection, twenty-piece cap, sealing
   race, multiple source requests, partial power, blocked output and hot reload.
   Reuse existing rollback tests; add cases only for these changed boundaries.

No new equipment or raster artwork is needed for this slice. Reuse current
furnace/collector portraits, native buttons and the live connection diagram;
label material and cooling connections separately. No PixelLab cost is incurred.
Physical fit, full-cycle interaction and hauling behavior remain owner checks
after implementation. This document itself changes no installed behavior.
