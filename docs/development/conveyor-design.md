# Conveyor belts: design record

Framework 0.61.0 and Shipbreaker 0.56.0 (1 October 2026). Owner decisions of
30 September 2026: working conveyor belts this round; every link between machines
and stores runs through touching equipment or a pipe or belt network, never across
open floor (the refuelling kiosk is the only exception); belt routes resume after a
reload; saves migrate automatically, with a documented manual step only when
unavoidable. This record follows the earlier
[underfloor material transport research](underfloor-material-transport.md).

## What a belt is

- **A Framework line segment.** `Phobos' Rivetline Conveyor Belt`, 1 x 1, 4 kg,
  24 cr, sold with the other lines in lots of 128 and at the faction kiosks at any
  standing, installed from INSTALL > MISC on intact floor. It uses the shared segment
  pattern: its own presence condition, no obstruction, no power conduit, the lowest
  draw layer (`LineLayers.Belt`, 1.03), so pipes and lines share its tiles in their
  lanes above it. Repair takes a steel scrap; dismantling returns the segment as
  retained waste.
- **Reach, not carriage.** `Inventory.BeltNetwork` is a segment family with no port
  participants. Two pieces of equipment are joined when one run of intact installed
  belts lies on or beside (north, south, east or west of) the cells each one uses
  (`FluidTopology.JoinsNear`, read from the cached per-ship snapshot), or when the two
  touch (`BulkVessels.Adjacent`: footprints meeting or one tile apart). Items never sit
  on a belt: each still moves straight from sender to receiver in one checked
  physical transfer, powered and paid for by the machine that sends or receives it,
  exactly as before. No belt power, speed, capacity or item-on-belt state exists.
- **Which cells.** The content mod keeps choosing each endpoint's cells, unchanged
  from the floor routes: a residue collector's inboard tiles, the F6's input or
  output approach, a machine's footprint, a passive store's use point and own tile.

## Shipbreaker's routes

`CollectorRoute.Find` and `Valid` replaced their bounded structural-floor search
with `BeltNetwork.Reaches`. That covers every route the collector service runs
(collector from D4, reclaimer feed, F6 input and output) and D4/R4 storage output.
The status now says whether a route runs *by conveyor belt* or *touching*, where it
used to count floor tiles. A belt cut, damaged or taken up stops the route within the
snapshot's two-second recheck; the route then waits for Resume, as a broken floor
route did.

## Reload

The owner decided that belt routes resume after a reload, like the crew's standing
orders. A running route marks its receiver (`PhobosShipbreakerRouteRunning`), and
running storage unloading marks its machine (`PhobosShipbreakerStorageRunning`); the
marks are ordinary saved conditions, set while armed and cleared on every disarm.
After a load, the first power step arms the route once through every pair, route
and filter check; if one fails, the route stays paused with the reason and the mark
is cleared. Processing permission keeps its own pause-on-reload rule, and hazardous
F6 steps still need their explicit permission. This supersedes the earlier
pause-on-reload rule for these transfer routes only.

## Stacks

The crew hauling orders' unit transfer moved into Framework as
`Inventory.UnitItemTransfer` (with its unit-admission preflight), unchanged for the
crew. Storage output uses it, so products the tray stacked leave one unit at a
time, each checked at its own mass; receivers keep exact single-unit admission.
Since Shipbreaker 0.57.0 (owner request, 1 October 2026) the collector service does
the same for every receiver: the reclaimer feed, the F6 charge bin and collectors take
one unit of a stack, asked about through `UnitItemTransfer.AsUnit` so each receiver's
own unit rule (the crew orders' rule) judges it.

## Stores and the moving item (Framework 0.62.0, Shipbreaker 0.57.0)

- A passive store's cells are its whole footprint (`BeltNetwork.FootprintCells`, from
  the item's live width and height, which the game swaps on rotation) plus its use
  point, so a belt on or beside any side joins it (owner request, 1 October 2026).
- `Inventory.BeltCarriers` shows a half-tile copy of the item's own material riding
  the belt path (`BeltNetwork.PathCells`) at the transfer clock's progress, drawn at
  layer 1.035 between the belt and the first pipe lane, eased each frame. It has no
  collider, is never saved, and any rendering fault switches it off for the session.
  One quad per active belt transfer; routes between touching equipment show nothing.

## Save migration

| Change | Handling |
| --- | --- |
| Belt definitions, art and economy | New content; nothing saved to convert |
| Existing pairs, filters, cargo | Kept unchanged |
| Routes over bare floor from earlier versions | **Manual step**: they stop with *These do not touch and no conveyor belt joins them*; lay belt or move the two within one tile, then start again. Laying belt cannot be automated without creating free material. Stated in the Shipbreaker changelog and guides. |
| Running routes at save time | Only saves made with 0.56.0 carry the running mark; older saves load paused, as before |

## Checks

Framework's offline checks cover `JoinsNear` (beside and under, missing ends, the
end of a row not wrapping to the next, separate runs, an overflowing layout). The
native checks cover the belt definition, layer, INSTALL tab, bills, economy tier and
the shared-segment rules. The route and reload behaviour need owner checks in play:
lay a belt between a D4 and a collector, confirm the status says *by conveyor belt*,
cut a segment and see the route stop, save while running and reload, and confirm
storage unloading takes units from a stacked tray.

## Product stores (Framework 0.98.0, Manufacturing 0.49.0)

Owner question, 5 October 2026, while planning the [regolith programme](regolith-programme.md):
do belts need reassessing? Feed stores already brought items to the Manufacturing machines;
nothing took products away, so a machine left running stopped on a full tray. Framework
`StoreDelivery` is the mirror of `StoreFeed`: one chosen store, touching or on a belt, to which
a started and powered machine sends its own products one unit at a time. Both now share
`StoreChoice` (the saved record, the panel field, the link rule and the status line), so the
two cannot drift apart. Content decides what a product is and when to send. The record is new
(`PhobosState.StoreDelivery`), so no saved structure changes.

Left for later, deliberately: Shipbreaker's D4 and R4 keep their own powered unloading
(`StorageService`), which meters each move; moving them onto the shared service is a tidy-up,
not a need. Material bins admit only the game's mining output, so products need an ordinary
container; widening the bins is a separate decision.
