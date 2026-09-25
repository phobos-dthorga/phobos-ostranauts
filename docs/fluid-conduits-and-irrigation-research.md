# Shared fluid conduits and plant sustenance

Current implementation follow-up: [Agriculture 0.5.0 nutrient-solution piping](agriculture-nutrient-solutions.md) now implements finite mixed feed; staged proposals below retain their original research context.

**25 September 2026 — research and proposed implementation sequence.**
The owner requested pipes for Agriculture's water/nutrient supply, with shared
services in Framework, and suggested following vanilla conduits in part. This
report implements no pipes, changes no packages and establishes no new gameplay
validation. The furnace's current fixed cooling attachments remain unchanged.

**Subsequent implementation:** the owner authorized this sequence and necessary
artwork later on 25 September. Agriculture 0.4.0 / Framework 0.18.0 now prepare
the [first water-conduit slice](agriculture-water-conduits.md). The research below
retains its original baseline/proposals; it is not the current implementation
inventory. Nutrient-mixture pipes and furnace coolant networks remain future work.

## Recommendation

Use **separately installed fluid conduits that feel like vanilla electrical
conduits**, supported by Framework's shared routing and measured transfers.
Agriculture should be the first consumer. Reuse native installation, tile socket
properties and automatic connection artwork; keep fluid contents and transfers
separate from the electrical grid.

Begin with **clean-water distribution to the rack's existing finite water store,
with nutrients loaded locally and metered by the rack**. This is the smallest
useful implementation and supplies the plants without changing existing crop
budgets. It is water piping, not yet central nutrient-solution piping. The
recommended next extension is a finite mixing/feed unit and a dedicated fresh
nutrient-solution route. Design the transport boundary to admit that extension,
but introduce mixture state only with its working consumer.

The initial water stage is an implementation recommendation, not an owner
decision to exclude nutrient pipes. A combined water-and-nutrient first release
is feasible, but must include the mixture work described below rather than
relabel water as nutrient solution.

This direction is justified by plant supply and ship layout. It does not depend
on turning the furnace's present heat-transfer abstraction into liquid plumbing.

## What exists now

| Inspected source | Delivered behaviour | Missing for routed plant supply |
| --- | --- | --- |
| [Framework `FiniteLiquidTransfer`](../src/PhobosFramework/Liquids/FiniteLiquidTransfer.cs) | Same-ship, matching-commodity scalar transfers; measures source debit and destination receipt, bounds reserve/capacity and attempts to restore unreceived quantities | No route, pump, branching, mixture composition, persistent transaction recovery or general hydraulic model |
| [Framework `ShipsWaterSupply`](../src/PhobosFramework/Liquids/ShipsWaterSupply.cs) | Optional adapter explicitly limited to Ship's Water 0.16.1; draws installed eligible tanks while protecting an aggregate crew reserve | No selected physical supply port or pipe path; no nutrient/wastewater intake; foreign contents mass remains provider-owned |
| [Framework `GridRoute`](../src/PhobosFramework/Inventory/GridRoute.cs) | Bounded cardinal search with a caller-supplied permitted-tile predicate | Caller must supply fluid topology, same-ship boundaries and fresh validation; it is not a network flow allocator |
| [Framework `PortPairing`](../src/PhobosFramework/Inventory/PortPairing.cs) | Reciprocal saved one-to-one full-object/port identities | One logical port cannot supply many racks; branching needs distinct outlet ports or a new explicit consumer-binding service |
| [Agriculture service](../src/PhobosAgriculture/Service.cs) | Optional automatic water receiving into the rack when its checks pass; current cap is 0.25 kg/s with a positive electricity receipt | This is a refill cap, not a researched pump curve or energy-per-kilogram model |
| [Agriculture crop state](../src/PhobosAgriculture/Core/Crop.cs) and [material operations](../src/PhobosAgriculture/MaterialOperations.cs) | Separate water and nutrient quantities; finite crop uptake; manual 0.25 kg water and 0.04 kg nutrient inputs, plus the 0.3.0 economy follow-up's 5 kg irrigation charge; drain produces retained process-solution cargo | No concentration, individual nutrient ions, pH, EC, dissolved oxygen or characterized recoverable drainage |
| [Furnace attachments](furnace-connections-and-instruments.md) | One paired radiator/thermal-port assembly, stored heat and bounded heat transfer | No coolant inventory, pipe segments, circulating fluid or shared sink network |

This describes inspected source, not a fresh installed-game test. The
[Agriculture economy follow-up](agriculture-economy-review.md) advanced to 0.3.0
in the shared working tree during this research; its new irrigation charge is
included here and was not implemented by this task.

The rack currently holds up to 20 kg water and 0.5 kg nutrient stock. These are
separate accounting quantities: **the code does not establish that all nutrient
stock is dissolved in the water**. A future mixing model must not silently make
that assumption about existing saves.

Valtora's [Ship's Water author documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
describes automatic under-deck supply from installed tanks. Local inspection of
Valtora's **0.16.1** `Plumbing.Run`, `CoTank` and `WaterMath` confirms tank-list
draw/deposit operations, not route traversal in those inspected paths. Its
potable supply is therefore a useful optional source, not an already implemented
placeable pipe network. Its published wastewater recovery does not establish
compatibility with crop drainage. Preserve its fixtures, kiosks and saved state.

## What vanilla conduits can give us

Primary game evidence is **Blue Bottle Games' Ostranauts**, inspected locally on
25 September against the assembly fingerprint below. The official
[Blue Bottle Games Ostranauts site](https://bluebottlegames.com/ostranauts) identifies
the game/developer; the specific findings here come from installed definitions
and code, not a claim that its website documents a public fluid API.

| Native evidence | Finding | Proposed reuse |
| --- | --- | --- |
| `ItmConduit00` / damaged / loose definitions; `Conduit00Install` | Separate installed items with native work, damage and repair paths; ordinary installation is registered through `JsonInstallable` | Own pipe definitions and material bills using the same installation machinery |
| `items.json`: `aSocketAdds`, `aSocketForbids`, `aSocketReqs` | Native placement adds tile conditions and prevents duplicate conduit occupancy; the intact conduit adds `TILPowerConduit` | Dedicated Phobos fluid occupancy/connection conditions; retain native floor/wall pressure boundaries |
| `TILPowerConduit` versus `TILPowerConduitOff` | Both mark conduit presence; only the intact path adds `IsPowerPath` | Separate visible pipe presence from usable fluid connectivity; a damaged pipe can remain visible while blocking delivery |
| `Item.SetSpriteSheetIndex`, `Ship.UpdateTiles` | Four cardinal neighbours choose among sixteen connection states using a configurable `ctSpriteSheet`; neighbouring matching sprite families refresh after tile changes | Own fluid trigger and registered original sprite sheet for straight, corner, junction and end states |
| `TileUtils.GetPoweredTiles` | Traverses cardinal power-path tiles from named `PowerOutput` points and records connected power providers | Follow that connectivity pattern, using fluid-specific topology and endpoints |
| `Powered.UsePower`, `JsonPowerInfo.aInputPts` | Consumers consult connections at configured power-input points and draw electrical resources | Keep this intact for actual pump electricity; it does not transport water |
| `Ship.AddCO` / `RemoveCO`, `IsPowerRecalc`, `UpdatePower` | Topology refresh is tied to equipment changes | Invalidate our route cache on installation/removal/mode changes, with a bounded fallback audit and fresh checks before transfers |

**Reuse assessment:** native sprite selection is parameterized enough to be a
strong reuse candidate. Native power distribution is tied to electrical fields,
conditions and providers; calling it with fluid items is not a generic network
API. Use Framework's existing `GridRoute` or a small shared component walk with
its own visited set. Native `GetFloodTiles` also uses tile-global scratch flags,
so it offers less isolation than the existing Framework helper.

Do not give pipes `IsPowerPath` or `IsPowerConduit`, write
`Tile.aConnectedPowerCOs`, modify native conduit definitions, or treat an
electrical connection as a fluid connection. The pipe family's sprite trigger
must also be distinct, so pipes do not visually join electrical cables.

The first layout should have one pipe service per tile, cardinal joins and no
nonjoining pipe crossovers. Fluid pipe and electrical conduit coexistence is a
specific placement/rendering check for implementation. Opposed endpoint faces,
rotated named ports and machine-edge artwork must agree with actual connectivity;
generic neighbour art alone cannot validate a connection. Start with supported
interior routes; hull crossings require a separately designed sealed fitting.

`Conduit00Install` uses the native `POWR` build category. `Installables` groups
entries by `strBuildType`, but this inspection does **not** prove an arbitrary new
category gets a visible drag-build tab. Deliver normal native item installation
first; inspect the build-menu adapter before promising conduit-style drag laying.
No new graphical construction mode is needed just to supply the first rack.

## Plant-delivery evidence and its limits

| Primary source | Supported finding | Our proposed application |
| --- | --- | --- |
| **NASA; NASA/Techshot/Tupperware collaboration**, Danielle Sempsrott, [The Shape of Watering Plants in Space](https://www.nasa.gov/missions/station/the-shape-of-watering-plants-in-space/), 4 March 2020; identifies NASA's Howard Levine and Techshot's Dave Reed | PONDS uses a local reservoir and passive delivery; development addressed both overwatering and insufficient root aeration | Contained root modules with a local buffer. A supply pump need not imply continuous pumping directly through every root. This small lettuce experiment does not validate our potato rack or its capacity. |
| **NASA**, [Station Science 101: Plant Research — Water Delivery](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/), accessed 25 September 2026 | Plant Water Management investigated water/air delivery; XROOTS tested hydroponic and aeroponic techniques without traditional soil | Positive delivery and containment suit the design better than assuming a gravity-fed trough. We are not selecting a particular flight-qualified XROOTS design or claiming all acceleration cases are solved. |
| **Bruce Dunn and Hardeep Singh, Oklahoma State University Extension**, [Electrical Conductivity and pH Guide for Hydroponics, HLA-6722](https://extension.okstate.edu/fact-sheets/electrical-conductivity-and-ph-guide-for-hydroponics), April 2017 | Dissolved nutrients, water quality, concentration ratios, EC and pH require management; solution composition can drift during reuse | Keep fresh nutrients and potable water distinguishable; do not present our aggregate nutrient mass as measured EC/pH or a complete chemical assay. This is terrestrial horticultural guidance, not space qualification. |
| **ESA MELiSSA**, [Closed Loop Compartments](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Closed_Loop_Compartments) and [Higher Plant Compartment IVb](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Compartment_IV_The_photoautotophic_compartment), accessed 25 September 2026 | Waste conversion and higher-plant cultivation are separate stages; crop modelling needs biomass/mineral composition and environmental context | Supply pipes do not purify drainage or convert generic waste into fertilizer. Recovery needs a characterized feed and treatment process. |

**Design inference:** a sealed supply, locally buffered root modules and controlled
nutrient dosing are a credible direction. Exact pipe capacities, flow limits,
pump power, mixing profiles, failures and accelerated growth remain **authored
gameplay choices**. NASA, ESA, the researchers and Blue Bottle Games have not
endorsed or validated this mod. Root aeration remains an explicit engineering
requirement; the current model does not simulate dissolved oxygen or root-zone
wetting, so it must not display invented measurements for them.

## Choose the smallest useful supply model

| Approach | Benefit | Cost / limitation | Recommendation |
| --- | --- | --- | --- |
| Existing optional ship-wide water refill | Already minimizes water handling | No physical routes; nutrients still loaded locally | Preserve as an explicit legacy mode |
| Water pipes + local nutrient stock/dosing | Mixed crops can share a water line; preserves present budgets and manual fallback | Nutrient packets still travel by crew; no central fertilizer piping | First useful route |
| Fresh nutrient-solution feeder + dedicated supply pipes | Both water and nutrient mass reach racks through the network | Requires mixture receipts, recipe compatibility and explicit handling of existing rack contents | Next extension, or include this work if both are required for the first release |
| Shared recirculating nutrient loop | Could later recover unused solution | Adds changing composition, contamination, return storage and treatment | Defer until a characterized return has a useful consumer |
| Full hydraulic/chemical simulator | Pressure and detailed fluid behaviour | Disproportionate before a useful supply route exists | Do not start here |

The gameplay gain needs to be honest. From the **authored current crop budgets**
in [Crop.cs](../src/PhobosAgriculture/Core/Crop.cs), ideal healthy full growth uses:

| Cohort | Water | Aggregate nutrients | Base cycle |
| --- | --- | --- | --- |
| Potato | 4.624 kg | 0.040 kg | 96 game hours |
| Lettuce | 1.2658 kg | 0.005 kg | 48 game hours |

These are total growth inputs, not circulating flow rates, nutritional advice or
experimental uptake measurements. Stress/respiration changes actual balances.
A 20 kg water fill therefore covers about four ideal potato cohorts or fifteen
lettuce cohorts; nutrient handling is already infrequent. The 0.3.0 follow-up's
5 kg irrigation charge also replaces many small manual loads. The strongest
reason for pipes is therefore a visible maintainable supply route serving several
racks and future equipment, not an assertion that one rack needs elaborate
plumbing to be usable. A large fertilizer plant is not justified solely by one rack.

The two crops also use different nutrient/water ratios. Dividing the totals above
gives about **8.65 g/kg** and **3.95 g/kg**, respectively. These are accounting
ratios, **not recommended hydroponic concentrations**. Feeding both from one fixed
premix cannot be assumed to match both budgets. Local dosing avoids that problem;
a future mixed-solution service must expose it rather than discard excess nutrient
mass or invent a separator at the destination.

## Proposed first working slice

```text
Manual water -------------------> Agriculture supply unit
Optional Ship's Water adapter --> finite clean-water buffer + powered delivery
                                  |
                           placed fluid conduits
                                  |
                       Firstlight-4 local water store
                       + crew-loaded Groundwork stock
                                  |
                       rack-owned metered root supply
```

The supply unit and conduits are proposed content, not registered equipment.
Use original `Phobos'` equipment names, Agriculture's Verdemorrow families and
normal merchant/construction acquisition. Keep the 4 x 4 rack footprint.
Select the supply unit's actual footprint, contents capacity, mass, bill and
power/flow settings before its first saved implementation; no real pump rating
is inferred from the existing 0.25 kg/s refill cap.

1. One supply unit, one explicit rack binding and one intact pipe route. The
   supply unit accepts finite manually delivered water, including the existing
   Agriculture irrigation charge where suitable, without Ship's Water.
   Its optional provider inlet uses the existing version-scoped adapter and
   protects crew reserves; expose that inlet as provider plumbing, not as a
   claim that its foreign tanks have physical pipe connections.
2. Route existence permits transfer; it is not permission to run. Require enabled
   sending/receiving, same owned ship, intact installed equipment, compatible
   commodity, destination room and a fresh route. Pumps use actual received
   electricity with a declared transfer-energy budget and heat destination.
3. No detailed liquid inventory in pipe tiles initially. Contents live in finite
   equipment reservoirs and move synchronously when the route is valid. Explicitly
   label neglected pipe hold-up/transit time as a gameplay abstraction. This
   avoids hidden line cargo, draining puzzles and imaginary spills on pipe removal.
4. A broken line or stopped pump halts replenishment; the rack can use its existing
   buffer subject to its normal power/environment rules. Opening a panel cannot
   move liquid or resume work. Keep manual water/nutrient loading available.
5. Save full source/port/rack identities and quantities; rebuild routes from the
   loaded ship. Receiving/pumping resumes explicitly. Preserve unknown/corrupt
   records. No elapsed-time refill on an unloaded ship and no docked-ship borrowing.
6. Add several racks after the single route works. Use explicit output ports or
   consumer bindings, fair bounded allocation and a **shared** per-pump/per-segment
   budget. A second consumer must not multiply the capacity of their common pipe.
   Cycles may provide alternate routes; they never create flow or duplicate cargo.

Existing racks need an explicit choice between legacy provider refill and routed
supply; enabling the route disables that rack's direct provider refill. Otherwise
a broken pipe could appear harmless because the old path silently bypasses it.
Switch modes paused, preserve stored quantities and do not force old saves to
construct new pipes. Another provider disappearing must not discard local water.

## Where the shared code belongs

**Framework:** commodity/capacity checks, topology access, bounded routes,
endpoint binding, measured receipts, scheduling/allocation when branching arrives,
versioned state, observation validity and optional provider adapters. Reuse existing
energy receipts, `GridRoute`, object storage and panel/action infrastructure.
Keep the old one-to-one `PortPairing` save format intact; do not widen its meaning
for a network or rewrite furnace/material links.

**Agriculture:** supply machinery, pipe item definitions/art and initial stock
routes, nutrient compatibility/mixing, root delivery, crop effects, balance and
player text. Framework remains usable without any content mod. Initially register
each physical item once in Agriculture; later consumers can provide their own
compatible segment family through Framework. If common hardware becomes a real
cross-mod dependency, choose one shared content provider then, without duplicate
IDs, implicit required Agriculture, or a circular dependency.

**Shipbreaker:** furnace thermal requirements and any future coolant hardware.
**Manufacturing:** any eventual cutting-fluid products and contaminated returns.
Neither gains a fluid requirement as a side effect of irrigation work. The current
Manufacturing cartridge proposal and furnace hot-state/pairing contracts remain valid.

The existing scalar transfer helper has no persistent protection against every
adapter failure. Before autonomous multi-rack use, ensure an exception after a
debit/receipt protects both endpoints from retry, preserves evidence and reconciles
observed quantities. A best-effort refund is not a crash-atomic transaction.
Use one authority per fluid quantity and per scheduling interval, not competing
Framework and content-mod ticks.

## Extending the same pipes to nutrient solution

Use a finite Agriculture-owned mixing/feed unit. It accepts real water and
Groundwork packets, consumes measured inputs and produces a declared fresh
solution profile. Begin with one formulation per network and explicitly compatible
crops; separate crop networks are simpler than universal automatic chemistry.
Fresh clean-water and nutrient-solution routes have distinct compatibility keys.

Framework's added receipt must conserve **water mass and nutrient mass separately**,
as well as total mass. Every portion of a well-mixed batch carries its proportional
components. Bound the transfer by every destination component's remaining capacity;
if either will not fit, leave the remainder at the source. Two independent scalar
commits are insufficient unless their combined failure/recovery behaviour is defined.
The current water-only litre/kg equivalence must not become a density rule for all
mixtures. Until density is specified, use mass units for solution storage/display.

Agriculture must distinguish dry stock from dissolved nutrient inventory, define
how mixed solution occupies the rack's capacity and preserve previous field meaning
through an explicit schema migration. Do not reinterpret saved dry stock as a
high-concentration liquid, change captured cohorts, or make old process-solution
cargo a certified feed. A crop switch with incompatible remaining solution requires
retention/drain handling, not silent conversion. Local manual operation remains valid.

A first fresh-supply model need not simulate pH or EC. Explain its automatic
conditioning as an authored abstraction and show only quantities/conditions that
are actually represented. Adding real concentration-sensitive growth later requires
its own crop/solution balance revision. A nutrient return needs a separate, finite,
nonpotable destination; it cannot feed crew tanks or Ship's Water's recycler merely
because it contains H2O. Recovery remains a later research-backed process.

Future coolant reuse can share segment/endpoint/routing machinery, but requires
fluid temperature/energy, closed-loop return, actual pump accounting and a finite
heat rejector. Irrigation is principally consumptive supply. Those different
processes must not be conflated into one universal liquid behaviour.

## Implementation checks and remaining questions

Use established native installation/power patterns directly; no separate diagnostic
prototype is necessary before building the useful water route. Focus checks on
our new differences:

- Exact receipt conservation, full destination, depleted source, crew reserve,
  same-ship ownership and unavailable/changed provider; contents mass follows its
  owner without double-counting inventories or rewriting foreign mass policy.
- Cardinal connections, rotations, corners/junctions, pipe/cable coexistence,
  wrong commodity, damage, removal, repair, support loss and topology changes
  during construction; pipes never provide electrical power or breach atmosphere.
- Fair multi-rack demand, common-segment throughput, cycles, disconnected branches,
  two sources and paused valves; stable bindings never select the nearest replacement.
- Partial electricity, zero supply, loaded elapsed time, reload and fast-forward;
  consumption/heat recorded once and no automatic retry of uncertain transfers.
- For nutrient extensions: composition conservation, partial receipt, full water
  versus full nutrient capacity, crop changes, migration and incompatible returns.
- Local panel/C1/F3 use the same checked service, with read-only displays. Explain
  distinct states such as unbound source, broken route, disabled receiving, reserve
  protected, full reservoir, incompatible solution and provider unavailable.

The remaining implementation decisions are the supply unit's concrete economics
and footprint, physical pipe layering/support rules, pump rate/energy budget,
event hooks for route invalidation, and whether nutrient transport is included in
the first release. Native drag-build menu integration is a convenience follow-up.
Owner gameplay checks should evaluate installation, useful crew-effort savings,
break/repair feedback and actual cross-mod behaviour. Offline inspection does not
establish Unity rendering, flight mass or gameplay acceptance.

## Evidence record

Current game evidence corresponds to the locally inspected **1.0.1.5** baseline.
`TileUtils`, `Item` and `Ship` were freshly inspected for this report. Native and
Valtora-derived source stays in ignored local research; this document carries only
findings and references. No game code, images, assemblies or foreign mod code is
added to distributable content. Original future artwork needs its separate
provenance/licensing record under the [asset policy](asset-generation-policy.md).

| Installed source | SHA-256 |
| --- | --- |
| `Ostranauts_Data/Managed/Assembly-CSharp.dll` | `91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e` |
| `StreamingAssets/data/condowners/condowners.json` | `e72d70768957bc834f04596d5fa8984be0bff4419385055152e94339a7941a03` |
| `StreamingAssets/data/items/items.json` | `d18109c927e473d7f42fcdf30419ec45dcbf7623e20bedb9f30c0f71ddba70b4` |
| `StreamingAssets/data/installables/installables.json` | `ee921a2f02892a39a6180cbcecd5fe83ee61c469a8a7a1d676651f759781b0fa` |
| `StreamingAssets/data/condtrigs/condtrigs.json` | `a9d8c9a6bbc49779419278f5a05371c53f22f7b6e2681d2954cd67483922abcd` |
| `StreamingAssets/data/loot/loot.json` | `0bb7be2be6945c573ac195285e81ace4bb4aacef22272eb0bcf98d96c2e10d5e` |

Related: [Agriculture implementation](agriculture-implementation.md),
[endurance roadmap](agriculture-roadmap.md), [Framework](phobos-framework.md),
[chemical/process-fluid direction](chemical-storage-and-process-fluids.md) and
[furnace player guide](furnace-player-guide.md).
