# Fluid networks, treatment and coolant servicing

25 September 2026: Agriculture 0.6.0, Shipbreaker 0.17.0 and Framework 0.20.0.
These are implementation candidates. Automated checks are separate from owner
gameplay evaluation. The versions and installation report identify what was
prepared/delivered; no new Steam publication is implied.

## Multiple racks and retained irrigation lines

One W2 can explicitly pair with **eight racks** using the existing Pair controls.
An existing saved pair occupies slot zero unchanged. Each rack still accepts one
supplier, chooses its own receiving permission and must match the W2 formulation.
At a rack, Unpair removes that rack's link; at the W2 it removes all links after
the receiving lines are drained and all endpoints are paused. Invalid/future
port records occupy their slots instead of being silently overwritten.

Each connected, receiving branch gets an equal share of the one measured pump
budget. A full/blocked branch cannot consume or delete another branch's cargo;
unused budget remains available for W2 mixing/provider intake. There is still
only one W2 per connected pipe circuit. Use separate circuits for different feeds.

Routes have a 64-tile limit. The authored model uses a 100 kPa rated head and
quadratic resistance: relative flow is `1 / sqrt(1 + tiles / 16)`; modeled
outlet pressure is `100 / (1 + tiles / 16)` kPa. These are **design calculations**,
not measured sensor readings or real pump specifications. Received electricity
and the shared branch budget additionally limit actual work.

Each selected route has a finite parcel capacity of **0.01 kg per pipe tile**
and **0.5 seconds per tile** of powered transit time. A parcel must finish its
transit before entering the rack reservoir; new feed cannot borrow an old
parcel's clock. Filling, powered transit and delivery share the same work budget; transit work
cannot also fund W2 blending or refill.
Parcels are processed discretely, so this is deliberately a lower-throughput
model than a continuously flowing hydraulic simulation.

Line contents are recorded and physically mass-accounted at the **receiving
rack's service cassette**, including after pipe damage/removal. This is a lumped
custody model: shared trunk volume is reserved per branch, not a per-segment
simulation. Changing the route requires draining retained contents; a broken
pipe cannot erase water/nutrients. Full racks keep line contents. Both component
masses, route identity and remaining transit survive saves; pumping/receiving
pause after reload, and unloaded time grants no movement.

Drain at a rack includes its line parcel, plain water, dry nutrient stock and
mixed feed. Clear a living crop separately when changing crop types. Removing
or dismantling a rack with retained line contents is blocked. Existing water
and dry-stock fields retain their original meaning; missing additive line
records start empty. Longer old routes now need shortening to the 64-tile limit.

## Recorded drainage treatment

New Drain actions produce **Phobos' Verdemorrow Recorded Process Solution** with
the exact remaining water/nutrient masses in a versioned item record. Older
Process Solution items remain uncharacterized and cannot be treated. Neither
item is potable. Existing residue/reject identities are not reassayed.

1. Pause W2 operation and receiving. Place recorded drainage and one
   **Phobos' Verdemorrow Groundwork Treatment Cartridge** in its Inventory.
2. Queue **drainage treatment** (15 minutes of crew work). The job binds those
   exact two physical supplies. Start the W2 to perform treatment.
3. Treatment suspends distribution and draws up to **0.5 kW**, requiring
   **0.01 kWh per kilogram** of recorded drainage. All electricity heats native
   cabin gas. Power loss pauses progress without creating work credit.
4. Completion recovers **90% of recorded water** into the nonpotable water
   buffer and **80% of recorded nutrients** into dry nutrient stock. The entire
   **0.05 kg cartridge**, unrecovered water and unrecovered nutrients become
   **Retained Treatment Rejects**. These rejects cannot be treated again.
5. If water/nutrient capacity or output space is insufficient, all bound inputs
   remain. Clear space and explicitly restart. A paused job can be cancelled;
   supplies remain physical, and spent electrical work stays heat. After reload,
   restore the exact bound supplies or cancel; substitutes do not inherit work.

These yields, medium, costs and lumped separation/decontamination process are
**authored gameplay abstractions**, not laboratory assays or treatment efficacy
claims. Only the two components our own crop model records are characterized;
unmodeled pathogens, salts and real fertilizer chemistry are not inferred.
There is no potable outlet, no perfect recycling and no conversion of old waste.
Original supply art uses existing native material references at runtime, as the
other agricultural supplies do; no extracted artwork is distributed.

## Optional finite furnace coolant

Existing direct and piped sealed assemblies keep working. While the F6 is cool,
idle and paused in a piped mode, select **Enable finite coolant servicing**.
Place **Phobos' Rivetline Thermal Service Fluid Charges** in Products and load
them locally, one kilogram at a time. Six charges fill its 6 kg assembly. Required
working charge is **5 kg + 0.01 kg per route tile**; no coolant appears when the
mode is enabled. Fresh charges are additive ordinary merchant stock (20 cr each).

This is fictional industrial coolant, not ammonia, drinking water or nutrient
feed. The pipe allowance represents the twin-channel jacket together. The old
hot/cold thermal stores and their heat capacities remain the authoritative
effective assembly model; no second coolant heat store is added or lost on leaks.

At sufficient charge, received pump electricity primes the route over
**0.5 seconds per tile** before circulation. Rated static pressure is modeled as
`200 * min(1, working charge / required charge)^2` kPa; route flow uses
`1 / sqrt(1 + tiles / 32)`, with actual partial electricity still limiting
circulation. Values are explicitly authored calculations, not new pressure probes.

A broken route or damaged furnace transfers up to **0.001 kg/s** from working
charge into **sealed secondary containment**, bounded by actual remaining fluid.
It resets priming and stops heating when charge/flow is inadequate. Retained
fluid and all existing thermal energy stay accounted for; no fluid is silently
destroyed or injected into the native atmosphere as an invented gas. Restoration
may prime protective cooling, but heating still requires explicit Resume.

Drain a cool idle assembly into retained coolant waste, including its catch tank,
then refill with fresh charges. Local drain remains available on a damaged cool
furnace. Unknown/interrupted service records protect the equipment. Fluid blocks
uninstall, dismantling, unpairing and mode changes; empty it before returning to
legacy sealed mode. Service actions are also available through `phobosfurnace
coolant-managed`, `coolant-fill`, `coolant-drain` and `coolant-sealed`.

The catch tank is a bounded containment abstraction, not an exterior plume,
toxic-atmosphere, fluid boiling or CFD model. No new ammonia species or room
chemistry is introduced. Individual pipe temperature and per-segment fluid
inventories remain outside this implementation by design.

## Evidence and owner checks

NASA's [2023 water recovery milestone](https://www.nasa.gov/missions/station/iss-research/nasa-achieves-water-recovery-milestone-on-international-space-station/)
describes multistage processing and retained brine. It supports separating
treatment from supply, **not** the W2's selected yields or potable suitability.
Oklahoma State University's [Hydroponics](https://extension.okstate.edu/fact-sheets/hydroponics)
distinguishes open and recirculating culture systems; our recorded remainder
model is much narrower than real solution monitoring and rebalancing.

NASA's [ISS reference guide, November 2010](https://www.nasa.gov/wp-content/uploads/2022/06/508318main_iss_ref_guide_nov2010.pdf)
describes distinct internal/external thermal circuits. NASA's
[Robotic External Leak Locator](https://www.nasa.gov/isam/robotic-external-leak-locator/)
documents the operational significance of coolant leaks. Our fictional fluid,
temperature envelope, pressure law and sealed catch tank are independent
gameplay choices; neither NASA nor Oklahoma State University endorses the mod.
Blue Bottle Games' [Ostranauts](https://bluebottlegames.com/ostranauts) provides the
native placement, power, gas heat, inventory and save mechanisms reused here.

Owner check sequence: two racks sharing one W2; one full rack; a broken route
with a retained parcel; drain and reroute; recorded treatment with/without power,
cartridge and destination space; cancel/reload a bound treatment job; legacy F6
operation; then enable serviced coolant, fill, prime, interrupt the route, inspect
captured leakage, cool, drain and refill. Check mass through damage/repair and
all four rotations. These are gameplay checks to perform, not claimed results.
