# F6 sealed coolant conduits

Shipbreaker **0.16.0**, Framework **0.19.0**. Prepared candidate; not installed
or gameplay-validated. This extends the shared routing begun in
[Agriculture](agriculture-water-conduits.md). Agriculture is not a dependency.

The optional **Phobos' Rivetline F6-C Sealed Coolant Conduit** connects a furnace
side fitting to an F6-R radiator elsewhere along the same ship's exterior.
Existing direct F6-R and adjacent F6-P installations retain their geometry,
thermal records and saved pairings. A missing mode record means direct cooling.
Piped F6-P connections are not supported: that fitting keeps its beside-furnace role.

## Installation

1. Mount the F6-R outside over its six intact supporting hull walls, as before.
2. Lay F6-C conduits on intact interior structural floor from a furnace side
   fitting to the radiator's interior service point. There must be a pipe on
   each endpoint tile and continuous cardinal adjacency between them.
3. While the furnace is cool and empty and its existing cooling assembly is cool,
   select **Piped cooling — left fitting** or **right fitting** in the local/C1
   panel. Choose the radiator with the existing Pair control. F3 uses the same service:

```text
phobosfurnace cooling-left <full furnace ID>
phobosfurnace pair <full furnace ID> <full radiator ID>
phobosfurnace status <full furnace ID>
```

Use `cooling-right` for the other side or `cooling-direct` to return to the
original mounting arrangement. Changing mode never moves equipment or discards
stored heat. Unpair an old cool assembly before pairing a different one; existing
reciprocal one-to-one pairing prevents duplicate radiator ownership.

| Connection | Local tile coordinate, rotated with its own equipment |
| --- | --- |
| Furnace left fitting | (-3.5, +0.5) |
| Furnace right fitting | (+3.5, +0.5) |
| F6-R interior pipe termination | (+0.5, -3.5) |

The radiator mounting wall is at local y=-2.5; its service point is one tile
further inward. The existing sealed mounting assembly represents the penetration.
Do not remove the wall or cut an atmospheric opening. The two machines may face
different cardinal directions if their respective fitting tiles connect.

Only intact installed F6-C conduits carry this route. Damaged, missing, unsupported
or off-grid pipes block it. Walls, flex floors and EVA tiles are not pipe routes.
Electrical cables retain their separate sockets and native power behaviour.
Irrigation conduits do not carry coolant, even though both use the same original
fitting artwork. Supply and return are two channels inside one jacket; corners,
T-junctions and crosses connect both channels. Crosses are not isolated crossings.

One circuit may touch only one furnace and one F6-R, including unused fitting
connections to other installed equipment. Sharing a circuit blocks service.
The selected shortest route is limited to **64 pipe tiles**; Framework's existing
4,096-cell scan bound also applies. Extra capacity is never gained by branching.

## Thermal model and interruptions

This is a **lumped sealed-loop model**, not water irrigation or an inventory of
transferable liquid. Existing furnace enthalpy and the radiator's finite 80 kJ/K
store represent the hot and cold sides. The existing 250°C sink limit, 12 m²
radiating area, 100 kW transfer bound and insulation model remain authoritative.
Pipe hold-up, transport delay, pressure drop and separate pipe temperature are
neglected. The jacket's one kilogram is structural mass. No water or coolant
commodity is created, consumed, certified or returned to drinking-water tanks.
A future fill/drain/leak simulation needs an explicit fluid mass/energy contract;
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
an unloaded interval. Removing a conduit does not delete energy from either node;
there is no separately modelled fluid cargo in the segment to spill.

## Construction, artwork and scope

Each conduit uses one kilogram of aluminium scrap and 120 seconds of configured
work with Mortorq and welding tools. Its base value is 3 credits (0.6 broken).
Install/uninstall use 50 native work units; repair uses 50 units and one aluminium
scrap, returning actual replaced material through Framework. Full-bar Restore is one minute at unit multipliers. Dismantling takes
20 units and retains one kilogram of low-value housing waste. These are authored
gameplay budgets. Normal supply, fixer and Halvorson stock offer one conduit per
eligible roll, subject to native placement and restocking.

The colour and normal exports are unchanged originals from Agriculture's
[asset record](../assets/phobos-agriculture/irrigation-generation-records.json).
Shipbreaker packages its own copies; installing Agriculture is unnecessary.
The [reuse record](../assets/phobos-furnace/coolant-conduit-reuse.md) identifies
the source/export paths. No new image-generation service was used.

D4 dismantling and R4 recovery have no implemented process-water need. They retain
their current power, cabin cooling, material routes and recipe budgets. Neither
receives a speculative liquid requirement. Future chemical or machining fluids
remain separate work with declared products and contaminated returns.

The physical fitting, native cardinal sheets and installation conventions derive
from Blue Bottle Games' [Ostranauts](https://bluebottlegames.com/ostranauts), observed
in installed 1.0.1.5 definitions. The routing code and equipment are Phobos work.
The existing [first-cycle research](furnace-first-cycle.md) retains the primary
scientific attribution for the thermal baseline. This new route's pump rating,
distance limit and lumped approximation are gameplay choices, not NASA/ESA
qualification or an assertion about actual coolant performance.

Offline checks cover energy conservation, zero/partial pump power, sink capacity,
probe loss, unchanged direct cooling, hot-state reload, mode validation, rotated
fitting coordinates, mass/value budgets, native definitions and Agriculture
regressions. Owner checks remain: all four rotations; independent pipe/electrical
placement; radiator termination visibility; foreign/water pipes; broken floor;
joined circuits; power interruption; hot reload; and local/C1/F3 operation.
