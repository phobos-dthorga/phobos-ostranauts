# F6 repair casting: replacement heat sinks

25 September 2026. Research completed against Shipbreaker 0.14.0, Framework
0.17.0 and installed Ostranauts 1.0.1.5. **Proposed next product; not implemented.**
The existing housing recipe and hot saves retain their current meaning.

**Subsequent owner decision:** dedicated machining will belong to a separate
**Phobos Manufacturing** mod, requiring our Phobos Framework, with optional
Shipbreaker integration and no Crafting Framework/Salvage Workshop dependency.
See the [Manufacturing handover](manufacturing-handover.md). This supersedes the
ordinary-table heat-sink finishing proposal below. Its work/tool assumptions,
proposed finished-item ownership/IDs and machining yield need revision during
that research; native repair evidence remains useful. The already implemented
housing-finishing recipe is unchanged.

## Recommendation

Make a batch of **Phobos' Rivetline Replacement Heat Sinks**, beginning with the
native CO₂ scrubber repair as the principal endurance use. This is substantially
more useful than another large housing: it supplies a distinct replacement part
already required by life-support equipment. It does not replace motors,
electronics, sorbent cartridges or the rest of the repair bill.

Blue Bottle Games' installed `data/condowners/condowners.json`, definition
`ItmHeatSink01`, describes a passive aluminium-or-copper heat exchanger with an
integrally cast standardized mounting pattern. Its native mass is **1.5 kg** and
base value **$27.50**. This is unusually direct game evidence for a casting use.
Developer attribution: [Blue Bottle Games](https://store.steampowered.com/developer/bluebottlegames/).
Local definition IDs below are reproducible source references, not distributed
copies of the game's data or a claim of developer approval.

## Native demand and alternatives

The repository's read-only `scripts/audit-furnace-repair.py` finds **252
broken-item definitions using the native Repair templates**, of which **68 require a heat sink**. Counts
include installed/loose variants and are not counts of distinct machines.
Restore uses a different native template and is excluded. The scan covers
`data/installables/*.json`, with selected bills in `installables_repair.json`.

| Native repair | Required parts | Total input mass / base value | Repair progress |
| --- | --- | --- | ---: |
| `AtmoScrubber01DmgRepair` | 1 motor, 1 sink, 1 motherboard, 2 mechanical parts, 1 electronic part, 2 steel | 8 kg / $114.20 | 2,000 |
| `AtmoScrubber02DmgRepair` | Same bill | 8 kg / $114.20 | 1,000 |
| `Cooler01DmgRepair` | 1 motor, 1 sink, 3 mechanical parts, 2 steel | 7.5 kg / $87.20 | 2,100 |
| `Heater01DmgRepair` | 1 motor, 1 sink, 2 mechanical parts, 2 steel | 7 kg / $82.20 | 900 |
| `AirPump02DmgLooseRepair` | 1 motor, 3 mechanical parts, 1 steel | 5 kg / $56.10 | 1,500 |
| `Battery02LooseRepair` | 1 motherboard, 3 electronic parts, 1 steel | 3 kg / $64.60 | 3,000 |
| `Antenna01DmgLooseRepair` | 2 aluminium, 4 electronic parts | 4 kg / $60.20 | 200 |

All listed repairs require Mortorq plus soldering, except the air pump, which
requires Mortorq plus welding. Scrap counts are 1 kg units; mechanical/electronic
parts and the motherboard are 0.5 kg each; the motor is 2.5 kg. These are native
definition values, not live merchant quotes or measured completion times.
Repair progress, skill modifiers, hauling and finishing work are distinct.

The heater and cooler benefit directly; the air pump and battery do not consume
a sink in these repairs. A generic cast "mechanical part" would imply gears,
fasteners and other unproven capabilities. An antenna bracket would only replace
scrap already available. A pressure-vessel patch is a poor first use of generic
unassayed aluminium. Heat sinks therefore win on both native fit and usefulness.

**Reuse before manufacture:** Crafting Framework contributors' installed
[Salvage Workshop 0.8.71](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573453)
already offers `SWB_SalvageElectronics` and `SWB_SalvageMachinery`, each 45 work
seconds, with a heat sink among possible recovered parts. Its
`crafting/recipes.json` also consumes sinks in thruster, RCS-distributor and
battery-component assembly. No deterministic heat-sink manufacturing recipe
was found in that inspected catalog. Keep these salvage/assembly paths intact;
the furnace offers planned stock from metal rather than replacing recovery.
Optional recipe acceptance of a Phobos sink still needs checking against that
provider's item/trigger matching; it is not yet a verified integration.

## Proposed material and work contract

Keep the first implementation at the existing **20 kg batch**, with a separate
captive mould recipe. Do not repurpose a saved housing job or let a housing blank
be reinterpreted as a heat-sink cluster.

| Step | Input | Output |
| --- | --- | --- |
| F6 qualified cast and cool | 20 exact, empty, unstacked 1 kg `ItmScrapAluminum` objects | 1 rough 19 kg heat-sink cluster + existing 1 kg terminal melt remainder |
| Finish mounting faces, separate parts and inspect | 1 exact 19 kg cluster | 12 finished sinks × 1.5 kg + 1 kg native aluminium offcut |
| Recover unused finished sinks, optional later recipe | 2 exact 1.5 kg sinks | 3 × 1 kg aluminium scrap |

Conservation: **20 = 19 + 1 = 12 × 1.5 + 1 + 1 kg**. The terminal remainder
stays terminal; the finishing offcut is recoverable. The paired recovery recipe
avoids inventing a fractional native scrap unit. Do not attach generic native
dismantling without inspecting its complete output contract.

Proposed identities: `PhobosFurnaceHeatSinkBlank`, `PhobosFurnaceHeatSink`,
`PhobosFinishFurnaceHeatSinks`. They are names reserved in this document only.
The finished object should carry native `IsHeatSink`, preserve native 1.5 kg mass,
and use our brand and art. `TIsHeatSink` in `data/condtrigs/condtrigs.json` checks
`IsHeatSink`, with no forbidden conditions or nested triggers. That supports
reuse of the existing native repair inputs without modifying all 68 recipes.
Verify generated repair gathering with the real item before claiming it works.
The rough cluster must never satisfy `TIsHeatSink`.

**Superseded proposal, retained for comparison:** **1,800 bench work-seconds per cluster** at existing Bar/Dining Tables,
with optional existing Workshop bench support. Reuse Mortorq and welding tool
requirements from housing finishing; describe manual dressing, mounting checks
and deburring. These tools do not prove CNC accuracy. Keep casting fins coarse
and native standardized-fit capability an explicit gameplay abstraction. Do not
introduce a speculative machine-tool dependency for this first researched product.

Proposed base prices: rough cluster **$220**, finished sink **$27.50**, existing
terminal remainder **$0.01**. Twenty aluminium cost $22 at base values; finished
stock totals $330 plus $1.10 of finishing offcut. Recovery of two sinks returns
$3.30 against their $55 whole value. The apparent manufacturing margin pays for
machinery, energy, labour and losses; it is not measured profit. Compare actual
merchant demand and total attended work before accepting this balance.

## Thermal, persistence and repair boundaries

Retain the 20 kg aluminium model, 700°C target, hold, finite radiator/gas receiver
and explicit release. A second mould with the same effective thermal capacity
is a **proposed balance assumption**; geometry may change real solidification.
Do not advertise a new measured cycle time. No new process gas, chemical refining
or coolant is introduced by this recipe.

`FurnaceState` currently saves revision 1 without a recipe ID, and release
explicitly produces the housing/remainder pair. The later implementation must
bind a stable recipe ID and output contract at sealing, default every historical
record to housing revision 1, preserve all captured heat/gas/input IDs, and block
unknown contracts without replay. Recipe selection is available only while idle,
cool and empty. Keep the same guarded physical output commit; fitting one 19 kg
cluster in the furnace tray does not prove room for twelve finished sinks on a
bench. Use Framework staged placement and retain the cluster if any output fails.

Native repairs consume their gathered lot through native mode switching.
Framework's spent-material return currently applies only to registered Phobos
maintenance jobs (`MaintenanceDefinitions` / `MaintenanceSafety`). Supplying our
sink does **not** authorize a global repair-waste rewrite or claim that vanilla
repair conserves mass. The new casting/finishing/recovery chain is mass-balanced;
the unchanged native repair boundary retains its existing abstraction. Phobos
spent-service packs and historic residue contracts remain untouched.

NASA's [On-Demand Additive Manufacturing for Deep Space brief](https://www.nasa.gov/wp-content/uploads/2024/09/27-on-demand-manufacturing-spec-sheet-508.pdf)
(hosted September 2024, prepared by Secor Strategies for the NASA opportunity)
identifies manufacturing, machining/joining and inspection as separate needs for
usable spares. It supports including finishing and checking fit, not our yield,
temperature, tool choice or proof of space-qualified heat sinks. Neither NASA nor
Blue Bottle Games has validated this fictional process.

## Graphics and next implementation boundary

Two small original designs suffice: rough multi-part cluster and finished finned
sink. Reuse the current remainder, furnace, cooling equipment and native panel.
Use a separate registered master for each; for a 16 × 16 finished world sprite,
retain at least 64 × 64 source. A proposed 32 × 32 cluster needs at least 128 × 128
source. Matching normals/portraits and native damage tint follow the existing
exporter. Native sink art remains a local reference only. Use PixelLab when this
recipe enters production; **no generation was needed for this research**.

Implementation acceptance: native `TIsHeatSink` repair gathering; exact cold/hot
mass/energy; old housing reload; blocked finishing output; no casting selection
mid-cycle; terminal-waste rejection; finite recovery; owner economy/appearance.
The [routing study](furnace-material-routing.md) can progress independently with
the current housing recipe. No runtime changes, version bump or installation
result from this report.
