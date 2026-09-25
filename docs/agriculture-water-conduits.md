# Agriculture water conduits — first working slice

25 September 2026. **Agriculture 0.4.0 requires Framework 0.18.0.** Prepared
implementation candidate, not installed or gameplay-validated. This implements
the first stage of the [shared-fluid research](fluid-conduits-and-irrigation-research.md).
This page records the water-only baseline. [Agriculture 0.5.0 nutrient-solution
piping](agriculture-nutrient-solutions.md) extends the same W2 and pipes with
finite mixed feed and shared liquid capacity. Multiple racks per pump and returns
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

These are authored material/economic budgets. Repair uses native work and retains
actual repair waste. Dismantling an intact W2 returns 4 kg steel and 16 kg retained
housing waste; damaged W2 returns 1 kg steel and 19 kg waste. Each dismantled pipe
returns 1 kg retained waste. Contents/protected records block supply uninstall
and dismantle. The native-data checks compare salvage value against whole sale
and construction output value against purchased inputs; merchant quotes vary.

1. Install the W2 on interior structural floor and connect its **electrical**
   power points. Load native 0.25 kg water rations or Groundwork 5 kg irrigation
   charges into Inventory; use the corresponding crew loading action.
2. Install fluid conduits between the W2's **right upper outlet** and the
   Firstlight-4 rack's **left upper inlet**. The first/last pipe tile sits directly
   outside that fitting. At zero rotation their named points are respectively
   (+1.5, +0.5) and (-2.5, +0.5) tiles from equipment centre. Points rotate with
   the equipment. The rack retains its 4 × 4 footprint and gains a small inlet
   fitting on every crop image.
3. Pause operation **and receiving at both ends**. Open either local Control
   Panel and choose the named/full-ID peer from the pairing list. Pairing selects
   **pipe-fed water** at the rack and disables its direct provider bypass.
   Reopen the panel to refresh the candidate list after installing equipment.
4. Enable the rack's selected water supply and start the W2. Start cultivation
   normally. A stopped pump, broken/removed pipe, damaged support, full rack,
   ownership change or invalid pair stops replenishment; the rack retains its
   existing water and normal crop/environment rules.

One W2 binds one rack. Use separate pipe circuits for additional pairs. Joining
two intact source outlets into one circuit blocks pumping: branching supply and
shared allocation are deliberately outside this first slice. Cardinal corners,
T-junctions and crosses connect; crosses never represent isolated crossing pipes.
Normal installation is supported, not a new drag-build menu. Pipes can occupy
electrical-conduit tiles through independent sockets; visual layering and native
placement still need owner evaluation. Walls, flex floors and EVA tiles do not
form valid water paths; there is no hull penetration or atmosphere opening.
Off-grid endpoints fail closed. The bounded implementation permits at most 4,096
eligible pipe cells on a ship; it does not guess connectivity beyond that limit.

The optional W2 inlet uses **Valtora's Ship's Water 0.16.1**, following the
[author's documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
and the version-scoped local contract. Enable selected supply on the W2 to draw
from eligible same-ship tanks above the configured crew reserve. This inlet is
provider plumbing; foreign tanks have no newly claimed physical pipe connection.
Unavailable versions leave manual supply working. Agriculture drainage never
returns to drinking-water tanks.

## Accounting and saved state

Pump limits are **0.05 kg/s**, **0.001 kWh/kg**, and **0.18 kW** at full flow.
Actual received electricity bounds each settled interval. Optional tank filling
and outgoing delivery consume one shared pump budget; neither gets a second
allowance. All electricity becomes cabin heat, including unsuccessful pumping.
There is no accumulated energy credit or transfer through an unloaded ship.
No suitable cabin heat recipient means no pump power admission.

Water exists only in finite equipment reservoirs. Pipe hold-up, pressure and
travel time are neglected gameplay abstractions. Removing a pipe does not spill
imaginary cargo. The existing 20 kg empty-appliance reservoir record also serves
the W2; it cannot accept crops, nutrients or cooking jobs. Native contents mass
includes the numerical water plus any physical inventory exactly once.

Full object IDs and a distinct `PhobosAgriculture.Water` logical port use
Framework's existing reciprocal one-to-one pairing. Furnace/material port keys
and saves are unchanged. Separate versioned mode records preserve old racks'
direct/manual default. Selecting pipe-fed mode is explicit and paused; unpairing
does **not** silently restore the direct provider bypass. All pumping and receiving
permissions pause on reload. The previous contents and bindings remain.

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
```

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
fill the rack; pause each end; save/reload; and verify continued local nutrient
loading. Check partial power and room warming, standalone manual operation and,
if present, Valtora 0.16.1 reserve protection. Two joined source circuits must
block delivery. These are the remaining gameplay checks, not completed results.
