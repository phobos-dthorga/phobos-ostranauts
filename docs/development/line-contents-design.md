# Lines that hold their contents: design record

Framework 0.63.0 and Manufacturing 0.25.0 (1 October 2026). The player guide is
[draining and venting](../lines-and-draining.md).

## Owner decisions (1 October 2026)

- Lines hold their contents until drained, across all Phobos piping; draining uses
  hauling, with the Common Sense mods as an optional tie-in.
- A gas line may hold any mix of gas species; players are not made to keep one line
  per gas.
- Drained contents go into a portable drain canister that crew haul.
- Realistic hold-up per tile.
- Irrigation parcels and the furnace coolant charge move to the same model, with their
  saved contents converted on load. Shipbreaker 0.58.0 delivers the coolant conduit
  and Agriculture 0.33.0 the irrigation conduit (see *Pumped circuits* below).

## Model

- **Segment record.** Each installed segment of a holding family keeps an
  `ObjectStateStore` record (`PhobosState.LineContents`, owner the Framework plugin id,
  schema 1): `state` open or closed, and commodity/kilogram pairs `c0`, `kg0`, and so on.
  A segment with no record is empty and open. The segment's native `StatMass` is its
  definition's dry mass plus its contents (`LineContents.Write`).
- **Hold-up.** `LineGeometry` authors a 25 mm bore, one metre of pipe per tile
  (0.491 L) and a 1,000 kPa gas line at the game's reference 293 K:
  - A liquid holds the bore volume at its density. For water, 998.2 kg/m³ at 20 °C
    ([NIST Chemistry WebBook](https://webbook.nist.gov/chemistry/fluid/), Lemmon et al.)
    gives 0.490 kg. For 98% sulfuric acid, 1,836 kg/m³ (Manufacturing's existing
    figure, CRC Handbook, marked there for re-checking) gives 0.901 kg.
  - A gas holds n = PV/RT moles, 0.2015 mol a tile. The code uses the game's own gas
    constant and molar masses (`NativeGasCanister`), giving grams a tile: H2 0.41,
    CH4 3.2, NH3 3.4, N2 5.6, O2 6.4, CO2 8.9.
  - These are authored geometry and pressure. There is no flow resistance, pressure
    drop or temperature.
- **Mixtures.** A segment's fullness is the sum over commodities of mass over that
  commodity's hold-up. A gas line fills each segment's free volume with an even share
  from each gas on offer, then from any gas that still has some
  (`LinePlanner.Fill`). A liquid family holds one liquid.
- **Filling.** `LineContents.Poll` runs every two real seconds, on the route cache's
  cadence, for each loaded player-owned ship:
  - For every holding family, a component of the cached topology gets topped up when
    it has a registered bulk vessel of a held commodity: installed, undamaged, not
    reserved, not protected.
  - It draws only the vessel's available kilograms (above its reserve), after settling
    buffered draws. Stores are debited first, and a segment is only credited with what
    a store gave.
  - Machines' transfers are unchanged: they still move store to machine directly, and
    the line's contents are the pipe being full.
- **Closed runs.** Draining or venting marks every segment of the physical run closed:
  its installed segments of the family joined through neighbouring tiles, intact or
  damaged. The topology scan leaves closed segments out of the allowed cells, so a
  closed run carries nothing, is not refilled, and no `LineReach` passes through it.
  *Return line to service* opens the run. An empty open segment drops its record, so
  a later uninstall leaves a clean loose part.

## Crew actions

Three Framework interactions, cloned from the game's Inventory action as Agriculture's
work actions are. They are offered on the installed and installed-damaged forms of a
holding family (`LineContents.OfferActions`):

- **Drain line into canister** (liquids, two minutes). Needs a drain canister the worker
  carries, or one loose on the deck within two tiles. It may be empty, or hold the same
  liquid with room left. It drains the run nearest first (`LinePlanner.Drain`).
- **Vent gas line** (gases, 30 seconds). Native species go into the room air through
  `RoomGas`; hydrogen goes overboard, since the game has no room condition for it.
- **Return line to service** (30 seconds).

Refusals are given when the work is offered (`TriggeredInternal`) and checked again
when it finishes. A finished action reports its amounts to the worker's log.

## Drain canister

- `PhobosLineDrainCanister` is a 1 x 1 pocketable item: 3 kg empty, 40 cr, never
  stacking. It holds 20 L of one liquid as a saved record (`PhobosState.DrainCanister`),
  so 19.96 kg of water or 36.72 kg of acid.
- It is sold as a regional item on the expanded merchants in lots of 16 (registered
  constant `Framework.stockCanisters`), and at the faction kiosks at Neutral.
- The art was procedural (`scripts/export-line-art.py`) in 0.63.0 and 0.64.0. Since
  Framework 0.65.0 it is a PixelLab overhead master in `assets/artwork-completion`
  (owner request, 1 October 2026), exported at a recorded nearest-neighbour phase.
  The attempts, prompts and rejections are in `drain-canister-requests.json`.
- **Pour on arrival.** On the same two-second pass, every canister in the container of
  an installed, undamaged registered bulk vessel of the same commodity is poured in.
  Pouring stops at the vessel's room and is wrapped in the vessel's conversion journal.
  The canister's mass change propagates to the vessel as cargo while its contents rise
  by the same kilograms, so the vessel's mass is unchanged and the mass invariant holds.
- **Racks.** A store with no general inventory takes canisters through
  `ApplianceDefinitions.SetRack` with `DrainCanisterDefinitions.RackTrigger`, which
  admits only the canister's marker condition. The AT acid tanks use a 2 x 2 rack.
  Framework's water silos already have general inventories.
- **Common Sense.** It is an optional tie-in with no code link. Its Salvage and Storage
  module improves the game's own hauling and container storage, which moves canisters
  like any other cargo. Phobos neither detects nor calls it.

## Damage, destruction and removal

- A mode switch keeps the record, because the game copies property maps to the new
  object; the new form's mass is set to its dry mass plus contents.
- **Damage.** A damaged form releases every gas (room or overboard) and a liquid's
  declared mist, and keeps the rest. Acid declares the tanks' mist fraction, 1e-4, as
  H2SO4.
- **Destruction** (not the old half of a mode switch) releases the same way. What
  cannot reach the room is lost; it is logged, with a caution on player ships.
- **Removal.** Uninstalling or dismantling a segment that holds anything is refused
  when the work is offered, and again when it finishes (`NativeEffects.Refuse` closes
  the task). Native destruction is never blocked.
- **Superseded behaviour.** Manufacturing 0.24.0's "wetted segment" spill drew from the
  linked tank into its bund. It is replaced, and `AcidLineService` is retired.

## Save migration

| Structure | Handling |
| --- | --- |
| Water, gas and acid segments already laid | Automatic: no record means empty and open; they fill from their stores within seconds, taking their hold-up from them |
| AT acid tanks | Automatic: the rack comes from the definition when the save loads; cargo starts empty, so the mass invariant is unchanged |
| Manufacturing 0.24.0 bund contents | Unchanged; recovered from the tank's panel as before |
| F6 coolant charge (`FurnaceCoolantCharge`, four keys) | Automatic: record unchanged; its meaning becomes the reservoir, and what it held above 5 kg (the old 0.01 kg a tile pipe allowance) is pumped into the conduit on the next powered step |
| A running serviced loop | **Manual step:** it needs about 0.33 kg a conduit tile more before it circulates; the furnace status says how much, and the crew coolant order loads it |
| Irrigation parcels (`AgricultureLine`, seven keys, at the rack) | Automatic: a non-empty parcel is delivered into its rack on the W2's next powered run, whatever the route now is (it was on its way there and matches the rack's profile, as the load check requires); the empty record then stays harmlessly. Until then it keeps counting in the rack's mass and relink gates |
| Irrigation conduit already laid | Automatic: starts empty; the W2 fills it when it runs, about 0.2 kg a tile from its reservoir |

## Pumped circuits (Framework 0.64.0, Shipbreaker 0.58.0)

- **Content-filled families.** A holding family need not be a network: one without
  ports (`StoreFilled` false) is skipped by the top-up pass and filled by its content
  mod through `LineContents.Circuit` (the open segments on the connected runs of a
  path), `Room`, `Full`, `Holding` and `Top`.
- **The F6 coolant conduit.**
  - It holds the authored service fluid, 1,050 kg/m³ in a 20 mm bore, about 0.33 kg
    a tile.
  - The furnace's four-key charge is its reservoir. The pump primes the whole connected
    circuit from the surplus above the 5 kg base, at an authored 0.1 kg a second of
    pumping. Mass leaves the furnace's charge and joins the segments.
  - `Filled` now means a full circuit plus the base charge. The route length, the
    priming time and the flow fraction keep their earlier rules.
  - The legacy sealed assembly (servicing disabled) keeps an empty conduit.
- **Canister receivers.** `DrainCanisters.RegisterReceiver` lets a machine that is not
  a bulk vessel take a canister's liquid from its inventory; the receiver records the
  kilograms and its own mass.
  - The F6 accepts coolant under the same guards and service journal as loading a
    charge.
- **The irrigation conduit (Agriculture 0.33.0).**
  - It holds one of four stable commodities: `water`, `potato feed`, `lettuce feed`
    or `lettuce seed feed`. Each is dilute feed in its profile's declared proportions,
    at an authored 998.2 kg/m³ in a 16 mm bore, about 0.20 kg a tile.
  - A running W2 primes each receiving branch's connected run from its own reservoir,
    taking the reservoir's own proportions. With a full run it delivers straight to
    the rack through the existing guarded mixture transfer.
  - This replaces the per-rack parcel's authored transit time (0.5 s a tile, 0.01 kg
    a tile). Branches sharing a trunk share one run; the one-W2-per-circuit rule keeps
    one profile a run.
  - The W2 is a canister receiver for water (into its intake reservoir) and its own
    feed (within each component's headroom).
  - Since Agriculture 0.34.0 (owner decisions, 1 October 2026), a formulation change is
    flushed automatically. When the W2 next runs a branch, any other commodity in the
    run is taken out, up to the step's budget and 20 kg. Plain water returns to the
    W2's reservoir within its room. Feed, and water that does not fit, becomes one
    recorded process solution item (`AgricultureDrainage`, water and nutrients by the
    profile's ratio) in its inventory, and the flush waits while there is no room.
  - A canister of another feed put in a W2 becomes the same item, so drainage treatment
    recovers it.
- **Ship's Water tanks (Framework 0.65.0).** Drinking tanks joined to a water line are
  sources of the top-up pass after the Phobos stores. `ShipsWaterSupply.LineAvailableKg`
  and `DrawForLine` never take the ship below `WaterTanks.CrewWaterReserveKg` across
  every drinking tank aboard, and skip a tank with an interrupted transfer.
- **A drained conduit is closed**, so the furnace sees no route and its reservoir
  follows the existing broken-route leak into the catch tank until the run returns
  to service.

## Performance

- The pass reuses the route cache's snapshot, which now also keeps the segment object
  per allowed cell.
- It reads each segment's record only in components that have a source, and writes
  only segments whose record changed (`TryWriteIfChanged`).
- Pouring scans the ship's objects once per pass.
- The pass is measured as `framework.line_contents.maintain`.

## Checks

- Framework's offline `LineContentsChecks` cover the geometry, the hold-up figures,
  record round trips and refusals, filling order, gas sharing and drain order with
  conservation.
- The native `LineContentsNativeChecks` and `AcidLineNativeChecks` cover the families,
  the actions on installed forms, the canister definition, rack trigger, economy and
  the acid tanks' rack.
- The in-game behaviour needs owner checks, listed in the player guides.
