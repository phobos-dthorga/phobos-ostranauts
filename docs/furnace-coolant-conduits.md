# F6 sealed coolant conduits

Current operating follow-up: [the furnace guide](furnace-player-guide.md) covers optional serviceable coolant, local cargo recovery and maintenance. Drain serviceable fluid before changing modes or removing equipment. The sealed-loop assumptions below describe the original mode, not a restriction on the later finite-coolant option.

Implementation history: [fluid-network operations](fluid-network-operations.md) documents Agriculture 0.6.0 / Framework 0.20.0 fan-out, line contents, treatment and optional Shipbreaker 0.17.0 coolant servicing. Earlier version-specific sections below retain their baseline scope.

Shipbreaker **0.16.0**, Framework **0.19.0**. Prepared candidate; not installed
or gameplay-validated. This extends the shared routing begun in
[Agriculture](agriculture-water-conduits.md). Agriculture is not a dependency.

The optional **Phobos' Rivetline F6-C Sealed Coolant Conduit** connects a furnace
to an F6-R radiator elsewhere along the same ship's exterior. Since Shipbreaker
0.80.0 the conduit joins a furnace it runs **under or right beside, on any side**,
and a radiator anywhere along its mounting wall or the row of tiles just inside
it; furnaces and radiators may share a run.
Existing direct F6-R and adjacent F6-P installations retain their geometry,
thermal records and saved pairings. A missing mode record means direct cooling.
Piped F6-P connections are not supported: that fitting keeps its beside-furnace role.

## Installation

1. Mount the F6-R outside over its six intact supporting hull walls, as before.
2. Lay F6-C conduit from any tile under or right beside the furnace to any tile
   of the radiator's mounting wall, or of the row just inside it, with continuous
   cardinal adjacency between them. Conduit may run on floor or inside walls.
3. While the furnace is cool and empty and its existing cooling assembly is cool,
   select **Piped cooling** in the local/C1 panel. Choose the radiator with the
   existing Pair control; the list offers the radiators the conduit reaches and
   says why any other aboard is not offered. F3 uses the same service:

```text
phobosfurnace cooling-piped <full furnace ID>
phobosfurnace pair <full furnace ID> <full radiator ID>
phobosfurnace status <full furnace ID>
```

Use `cooling-direct` to return to the original mounting arrangement. The older
`cooling-left` and `cooling-right` commands still work and mean piped; a furnace
saved with a left or right fitting loads as piped. Changing mode never moves equipment or discards
stored heat. Unpair an old cool assembly before pairing a different one; existing
reciprocal one-to-one pairing prevents duplicate radiator ownership.

Until Shipbreaker 0.80.0 the conduit joined only at these fittings, which are
still drawn and still join:

| Connection | Local tile coordinate, rotated with its own equipment |
| --- | --- |
| Furnace left fitting | (-3.5, +0.5) |
| Furnace right fitting | (+3.5, +0.5) |
| F6-R interior pipe termination | (+0.5, -3.5) |

The radiator mounting wall is at local y=-2.5; the inside row is one tile
further inward. The existing sealed mounting assembly represents the penetration.
Do not remove the wall or cut an atmospheric opening. The two machines may face
different cardinal directions.

Only intact installed F6-C conduits carry this route. Damaged, missing, unsupported
or off-grid pipes block it. Flex floors and EVA tiles are not pipe routes; conduit
inside a wall counts since Framework 0.100.0.
Electrical cables retain their separate sockets and native power behaviour.
Irrigation conduits do not carry coolant. Since Shipbreaker 0.52.0 the coolant line
draws in its own green lane and depth, so it can share a tile with other kinds of
Phobos line and is told apart at a glance; the PDA's Conduits filter selects it, and
painting jobs on Equipment leaves it alone. Supply and return are two channels inside one jacket; corners,
T-junctions and crosses connect both channels. Crosses are not isolated crossings.

Several furnaces and radiators may share one run of conduit (Shipbreaker 0.80.0;
before, a second furnace or radiator on a run stopped every loop on it). Pairing
stays one furnace to one radiator: each furnace pays for its own pump and rejects
heat only into the radiator it is paired with, so sharing a run adds no capacity.
A shared run is one body of coolant, which every serviced furnace on it helps fill.
The shortest route is limited to **64 pipe tiles**; Framework's existing
4,096-cell scan bound also applies.

## Thermal model and interruptions

This is a **lumped sealed-loop model**, not water irrigation or an inventory of
transferable liquid. Existing furnace enthalpy and the radiator's finite 80 kJ/K
store represent the hot and cold sides. The existing 250°C sink limit, 12 m²
radiating area, 100 kW transfer bound and insulation model remain authoritative.
Transport delay, pressure drop and separate pipe temperature are neglected. The
jacket's one kilogram is structural mass. With the legacy sealed assembly the pipe
holds nothing; with [serviced coolant](fluid-network-operations.md#optional-finite-furnace-coolant)
it holds about 0.33 kg of service fluid a tile since Shipbreaker 0.58.0, as mass
only. No water commodity is created, consumed, certified or returned to
drinking-water tanks.
Historical design boundary at 0.16.0: a fill/drain/leak simulation needed an explicit fluid mass/energy contract (the later optional implementation is linked above);
the scalar Agriculture water transfer helper cannot supply that by itself.

Piped circulation adds up to **1 kW** of electrical demand, accounted through
Framework's measured native energy receipts in addition to existing auxiliaries.
After the instrument allowance, the incremental pump receives priority over
process heating. Partial pump receipts proportionally bound transfer; electricity
is retained as cold-side heat. Each interval settles once, with no banked pumping
credit. Sink headroom limits admission. Rated transfer remains limited by both
temperature difference and finite heat capacity, not simply by pipe presence.

Piped circuits have no unpowered thermosiphon shortcut. On power loss or a broken
route, heating stops and hot-node energy remains stored. The furnace still leaks
its bounded heat to an accepting room, and an exposed radiator still radiates its
own stored energy independently. Restoring power may run protective cooling;
it never automatically re-enables heating. A failed probe stops heating while
measured pump power can still cool. Flight priority remains unchanged.
An actually damaged furnace has no operating pump; its retained heat must lose
energy through the remaining available paths. Hot maintenance interlocks still apply.

Mode is saved separately from the unchanged furnace/sink records. Unknown mode
schemas are protected, never silently treated as direct. Cooling paths are
rechecked at admission and settlement. No heating or pumping is simulated through
an unloaded interval. Removing a conduit does not delete energy from either node.
A sealed-assembly conduit holds no fluid; a serviced one holds its coolant until
drained into a canister, and cannot be taken up before then.

## Construction, artwork and scope

Each conduit uses one kilogram of aluminium scrap and 120 seconds of configured
work with Mortorq and welding tools. Its base value is 3 credits (0.6 broken).
Install/uninstall use 50 native work units; repair uses 50 units and one aluminium
scrap, returning actual replaced material through Framework. Full-bar Restore is one minute at unit multipliers. Dismantling takes
20 units and retains one kilogram of low-value housing waste. These are authored
gameplay budgets. Normal supply, fixer and Halvorson stock offer one conduit per
eligible roll, subject to native placement and restocking.

Since Shipbreaker 0.52.0 the colour and normal sheets are drawn by the shared
[line-art exporter](../assets/line-art/README.md) from Agriculture's original
[irrigation masters](../assets/phobos-agriculture/irrigation-generation-records.json),
recoloured green and moved into the coolant lane. Shipbreaker packages its own
copies; installing Agriculture is unnecessary. The earlier byte-for-byte reuse is
kept in the [reuse record](../assets/phobos-furnace/coolant-conduit-reuse.md).
No new image-generation service was used.

D4 dismantling and R4 recovery have no implemented process-water need. They retain
their current power, cabin cooling, material routes and recipe budgets. Neither
receives a speculative liquid requirement. Future chemical or machining fluids
remain separate work with declared products and contaminated returns.

The physical fitting, native cardinal sheets and installation conventions derive
from Blue Bottle Games' [Ostranauts](https://bluebottlegames.com/ostranauts), observed
in installed 1.0.1.5 definitions. The routing code and equipment are Phobos work.
The existing [first-cycle research](development/furnace-first-cycle.md) retains the primary
scientific attribution for the thermal baseline. This new route's pump rating,
distance limit and lumped approximation are gameplay choices, not NASA/ESA
qualification or an assertion about actual coolant performance.

Offline checks cover energy conservation, zero/partial pump power, sink capacity,
probe loss, unchanged direct cooling, hot-state reload, mode validation, rotated
fitting coordinates, mass/value budgets, native definitions and Agriculture
regressions. Owner checks remain: all four rotations; independent pipe/electrical
placement; radiator termination visibility; foreign/water pipes; broken floor;
joined circuits; power interruption; hot reload; and local/C1/F3 operation.
