# Agriculture water conduits

For the current multi-rack setup, use [fluid-network operations](fluid-network-operations.md):
one W2 can serve up to eight linked racks. Since Agriculture 0.53.0 the irrigation
conduit joins a W2 or rack it runs **under or right beside, on any side**, the way
the process-water line does, and a W2 within one tile of a rack feeds it with no pipe
at all. The fixed outlet and inlet tiles and the one-rack restriction below belong to
the historical water-only baseline.

## Historical water-only baseline

25 September 2026. **Agriculture 0.4.0 requires Framework 0.18.0.** Prepared
implementation candidate, not installed or gameplay-validated. This implements
the first stage of the [shared-fluid research](development/fluid-conduits-and-irrigation-research.md).
This page records the water-only baseline. [Agriculture 0.5.0 nutrient-solution
piping](agriculture-nutrient-solutions.md) extended the same W2 and pipes with
per-crop mixed feed, which Agriculture 0.55.0 replaced with one nutrient fed with the
water. Multiple racks per pump and returns
remain later stages; [F6 coolant conduits](furnace-coolant-conduits.md) use a
separate content-owned thermal circuit.

## Equipment and operation

**Phobos' Verdemorrow Groundwork W2 Water Supply Unit** occupies 2 × 2 floor
tiles, weighs 20 kg empty and holds 20 kg root water. **Phobos' Verdemorrow
Groundwork Irrigation Conduit** occupies one tile and weighs 1 kg. Both are
available through additive ordinary supply/fixer/general-trader stock and
Framework construction at Bar/Dining Tables. No Shipbreaker or Ship's Water
dependency is added.

| Item | Construction | Work | Base price |
| --- | --- | --- | --- |
| W2 supply | 8 kg steel, 8 kg aluminium, 4 small mechanical parts and 4 small electrical parts (0.5 kg each) | 30 minutes | 250 cr |
| Conduit | 1 kg aluminium scrap | 2 minutes | 2 cr |

These are authored material/economic budgets. Repair uses native work and, as in
the rest of the game, uses up its parts. Dismantling an intact W2 returns 4 kg steel and 16 kg retained
housing waste; damaged W2 returns 1 kg steel and 19 kg waste. Each dismantled pipe
returns 1 kg retained waste. Contents/protected records block supply uninstall
and dismantle. The native-data checks compare salvage value against whole sale
and construction output value against purchased inputs; merchant quotes vary.

1. Install the W2 on interior structural floor and connect its **electrical**
   power points. Load native 0.25 kg water rations or Groundwork 5 kg irrigation
   charges into Inventory; use the corresponding crew loading action.
2. Lay irrigation conduit from any tile under or right beside the W2 to any tile
   under or right beside the rack (Agriculture 0.53.0), or set the W2 within one
   tile of the rack and lay none. Until 0.53.0 the pipe had to cover one outlet tile
   beside the W2's upper right and one inlet tile beside the rack's upper left, at
   (+1.5, +0.5) and (-2.5, +0.5) tiles from each centre; those fittings are still
   drawn and an old layout still joins. The rack keeps its 4 × 4 footprint.
3. Pause operation **and receiving at both ends**. Open either local Control
   Panel and choose the named/full-ID peer from the pairing list. The list offers
   only racks (or W2s) joined by conduit or within one tile; a pairing across open
   floor is refused with the reason, and the picker says why each W2 or rack aboard
   is not offered. Pairing selects **pipe-fed water** at the rack and disables
   its direct provider bypass.
   Reopen the panel to refresh the candidate list after installing equipment.
4. Enable the rack's selected water supply and start the W2. Start cultivation
   normally. A stopped pump, broken/removed pipe, damaged support, full rack,
   ownership change or invalid pair stops replenishment; the rack retains its
   existing water and normal crop/environment rules.

In this baseline one W2 bound one rack, and two W2 outlets on one circuit blocked
pumping. Since Agriculture 0.53.0 several W2s may share one pipe; see
[fluid-network operations](fluid-network-operations.md). Cardinal corners,
T-junctions and crosses connect; crosses never represent isolated crossing pipes.
Normal installation is supported, including the native INSTALL > MISC entry
(see [catalogue](development/install-catalogue.md)); continuous drag laying is not verified. Pipes can occupy
electrical-conduit tiles through independent sockets, and since Agriculture 0.29.0 they can
also share tiles with other kinds of Phobos line: each kind draws in its own lane and depth,
so none hides another. The PDA's Conduits filter selects these pipes; painting jobs on
Equipment leaves them alone. Visual layering and native placement still need owner
evaluation. Flex floors and EVA tiles do not form valid water paths; since
Framework 0.100.0 a pipe laid inside a wall counts, by the lines data pack's rule.
There is no hull penetration or atmosphere opening.
Off-grid endpoints fail closed. The bounded implementation permits at most 4,096
eligible pipe cells on a ship; it does not guess connectivity beyond that limit.

The optional W2 inlet uses **Valtora's Ship's Water 0.16.1**, following the
[author's documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
and the version-scoped local contract. Enable selected supply on the W2 to draw
from tanks that touch the W2 or share its process-water line, above the configured
crew reserve (counted over every drinking tank aboard). Since Framework 0.59.0 each
Ship's Water tank joins a process-water line that runs under or right beside it; its
water stays in Ship's Water's own accounting.
Unavailable versions leave manual supply working. Agriculture drainage never
returns to drinking-water tanks.

## Accounting and saved state

Pump limits are **0.05 kg/s**, **0.001 kWh/kg**, and **0.18 kW** at full flow.
Actual received electricity bounds each settled interval. Optional tank filling
and outgoing delivery consume one shared pump budget; neither gets a second
allowance. All electricity becomes cabin heat, including unsuccessful pumping.
There is no accumulated energy credit or transfer through an unloaded ship.
No suitable cabin heat recipient means no pump power admission.

Water exists only in finite equipment reservoirs and, since Agriculture 0.33.0, in
the pipes themselves, about 0.2 kg a tile filled by the W2 (see
[fluid networks](fluid-network-operations.md) and [draining](lines-and-draining.md)).
Pressure and travel time remain gameplay abstractions. A pipe holding water must be
drained before it is taken up. The existing 20 kg empty-appliance reservoir record also serves
the W2; it cannot accept crops, nutrients or cooking jobs. Native contents mass
includes the numerical water plus any physical inventory exactly once.

Full object IDs and a distinct `PhobosAgriculture.Water` logical port use
Framework's existing reciprocal one-to-one pairing. Furnace/material port keys
and saves are unchanged. Separate versioned mode records preserve old racks'
direct/manual default. Selecting pipe-fed mode is explicit: since Agriculture
0.66.0 the rack and its W2 pause for the change on the second press and carry on
after it, instead of having to be paused first. Unpairing does **not** silently
restore the direct provider bypass. Pumping and receiving
that were running when you saved carry on after a reload; paused ends stay
paused. The previous contents and bindings remain.

Framework's `LiquidTransferGuard` writes pending evidence at both endpoints before
any debit. It clears that evidence only after a measured receipt reconciles.
An exception or interrupted save leaves protected records and prevents retries;
unknown records remain intact. Ship's Water vessels receive only our namespaced
journal; its quantities remain provider-owned. This is conservative interruption
protection, **not** a crash-atomic save transaction or automatic recovery tool.
Investigate saved quantities/evidence before any deliberate repair; never delete
the journal merely to restart a pump.

Local panels, optional C1 commands and F3 share checked services. F3 examples:

```text
phobosagriculture list
phobosagriculture link-water <full supply ID> <full rack ID>
phobosagriculture receive <full rack ID>
phobosagriculture start <full supply ID>
phobosagriculture pause <full supply ID>
phobosagriculture pause-receive <full rack ID>
phobosagriculture water-legacy <full rack ID>
phobosagriculture link-water <full supply ID> <full rack ID> confirm
```

A command that offers to pause machines or remove an old link first says so; repeat
it with `confirm` at the end to go ahead.

Pairing from the local panel remains available without C1. C1 can control existing
bindings and modes; physical loading stays local. Reads never pump or resume work.

## Evidence, artwork and validation

**Blue Bottle Games' [Ostranauts](https://bluebottlegames.com/ostranauts)** supplied
the inspected native installation, named-point and cardinal sprite-sheet patterns
(game 1.0.1.5; assembly fingerprint in the research report). Fluid routing uses
Framework's bounded grid search and separate sockets. It never sets native
`IsPowerPath`/`IsPowerConduit`, edits electrical connectivity, or redistributes
game art. Existing furnace cooling attachments are unaffected.

**NASA**, Danielle Sempsrott's [PONDS report, 4 March 2020](https://www.nasa.gov/missions/station/the-shape-of-watering-plants-in-space/),
supports the design inspiration of buffered local root delivery. Our tank size,
power, flow and crop timing are authored balance; this is not a NASA pump design,
hydroponic qualification or endorsement. See the research report for NASA, ESA,
Dunn/Singh and Valtora attribution and nutrient-solution limitations.

The [asset record](../assets/phobos-agriculture/irrigation-generation-records.json)
preserves exact prompts, providers and rejected projection candidates. A ChatGPT
overhead chassis master and one PixelLab fitting produce the registered 32 × 32
supply, 16 pipe masks and rack inlet. Native normals are flat, not inferred relief.
All sources remain separate, original and reproducible through the two exporters.

Offline checks cover measured conservation, partial power, full buffers, foreign
ships, interrupted debit/receipt, future-schema protection, existing crop budgets,
native registration, construction mass/value, salvage and unchanged electrical
definitions. Builds do not validate Unity rendering or a running save.

Validation on this candidate: 1,979 Framework checks, 35 performance-adapter
checks, 377 Agriculture checks, 6,403 native-definition checks, 7,342 Shipbreaker
regression checks, 31 observation/access checks and 204 installer checks using
synthetic installations passed. Constants consistency/recovery and local document
links also passed. No installed mods or owner saves were changed.

Owner checks: install a small route; try all four equipment rotations and a
pipe/electrical overlap; break/repair/remove a pipe or its supporting floor;
fill the rack; pause each end; save/reload with the pump running and confirm it
carries on by itself; and verify continued local nutrient
loading. Check partial power and room warming, standalone manual operation and,
if present, Valtora 0.16.1 reserve protection. Two joined source circuits must
block delivery. These are the remaining gameplay checks, not completed results.
