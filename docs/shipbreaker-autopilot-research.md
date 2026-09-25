# Autonomous reclamation: G4 research and design

25 September 2026. **Research/specification only.** No attached-hull cutting,
industrial flight API, automatic capture, repeated furnace batches or dependency
change is implemented by this document. Inspected source candidates: Shipbreaker
0.18.0, Auto Nav 0.13.0 and Framework 0.20.0, against Blue Bottle Games'
Ostranauts 1.0.1.5. Prepared source is not installed or gameplay-validated evidence.
The [implementation handover](shipbreaker-autopilot-handover.md) specifies the
delivery sequence and acceptance checks.

## Decision and feasibility

The owner selected one explicitly chosen **Phobos' Rivetline G4 Exterior Grabber**
per mission, an existing N1 or N2 Polaris module, and **Auto Nav as a mandatory
Shipbreaker dependency** for the future implementation. This supersedes earlier
optional-Auto-Nav design direction; current packages still implement that older
dependency arrangement. The player recovers valuables manually first, or elects
to sacrifice supported leftovers. Automation does not perform a valuables pass.

**Recommendation: active target-relative positioning under Auto Nav, with a
bounded align -> hold/cut -> feed -> advance loop.** The owner's clarification
supersedes this document's initial docking-first recommendation. The ship remains
free to follow target drift/spin and advance between cuts. Holding a working pose
may require thrust; it does not mean docking, zero absolute velocity or constant
thrust. Continuous translation during a cut needs its own demonstrated work
envelope and is not required for the first useful wall section.

Native docking is an exact-selection precedent and an optional stabilization
candidate, not a prerequisite for reclamation. Capture/mooring remains worth
investigating where it would improve physical reach, with explicit release and
repositioning. A fixed attachment alone cannot consume an arbitrary wreck.

Free-flight G4 reach is **unresolved**, not disproved: native collision circles
and deck-space coordinates are different representations. A velocity match does
not prove jaw clearance or a valid physical transfer. Resolve their transform,
target loading and collision-compatible reach before enabling acquisition.
Neither docking nor free flight warrants collision suppression, invisible long
reach, teleporting cargo or a second flight simulation. If the present G4 cannot
reach from a permitted pose, identify a finite articulated head or handling
equipment change explicitly; do not silently substitute compulsory docking.

**Whole-wreck reclamation remains a staged objective.** The first useful result
is a short exposed wall section, including movement to the next wall, with real
downstream capacity stops. Unsupported or unreachable remnants remain reported.

## Evidence and attribution

### Blue Bottle Games: native engine evidence

Local `Assembly-CSharp.dll` SHA-256:
`91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
Method names below identify privately inspected Blue Bottle Games code, not Phobos
implementations. Inspection files are retained under ignored
`.local/research/engine/` and `.local/research/shipbreaker-autopilot/`.
No game assembly, decompiled body or extracted asset belongs in public artifacts.

| Evidence | Inspected boundary | Finding and practical limit |
| --- | --- | --- |
| E1 | `Ship.PrimaryDockingPortIDFriendly`, `GetOpenDockingPorts`, `GetAvailableDockingPorts`; `Comms.Clearance` | Docking uses full port IDs and shorter labels. Fit enumerates port pairs using native grids; clearance identifies the assigned target port. |
| E2 | `CollisionManager.GetCollisionDistanceAU`, `ProcessCollision`; `ShipSitu.GetRadiusAU`, `SetSize` | Collision range is the sum of two radii. Radius is `Size` converted to AU; ordinary `SetSize(length)` uses `length * 20`, with a different minimum/body branch. It is not the visible jaw-to-wall gap. |
| E3 | `Ship.InitShip`, `SilhouetteUtility.GetSilhouetteLength`, `Ship.RemoveCO` | Loading derives size from a floor silhouette. The inspected length helper measures its X extent. Removal resets mass and silhouette-point caches and updates tiles; this is not proof that every cut immediately recomputes the navigation radius. |
| E4 | `GridUtils.CreateFullGrid`, `GetIncomingDockRotation`, `CanOverlay`; `CrewSim.DockShip`, `PositionShipsAtAirlock` | Native docking supplies a checked rotated/offset deck relationship. Full grids can include attached neighbours; acquisition must retain individual ship ownership. |
| E5 | `Ship.CreateMooringPorts`; `CrewSim.MoorAddDockingPorts`, `MoorShip`, `UnMoorShip` | Mooring generates `MP\|...` endpoints from projected grid-edge intersections and an overlay test. It loads the incoming ship, changes attachment state and positions decks; unmooring removes temporary ports and can unload ships. Generated endpoints are not permanent G4 identities. |
| E6 | `Installables.Create`; native `Wall1x1Uninstall` and `FloorGrate01Uninstall` definitions | Native uninstall progress leads to loose wall/floor mode transitions. Both inspected definitions use a structure cutter. Calling the finish transition alone would omit the work and admission contract. |
| E7 | `CondOwner.ModeSwitch` | Carries persistent conditions, full ID and property maps into the replacement object. It may relocate loose output to fit, transfer contents and replace object references. Re-resolve the ID and recheck position after completion. |
| E8 | `Ship.RemoveCO`, `Ship.Destroy`; `CrewSim.UnMoorShip` | Removal updates structural/system membership. Whole-ship destruction can destroy remaining objects and actors; it is not a safe empty-wreck shortcut. Releasing an attachment can change grid rotation/loading. |
| E9 | `GUIDockSys.CheckForCrimeIllegalSalvagingOKLG`; `Interaction.strCrime` execution | Native docking can issue a no-license warning, and interactions can log crimes. Neither a sensor contact nor an available mooring method grants salvage permission. |

The AI `Ostranauts.Ships.Commands.Dock` also invokes mooring. Its surrounding AI
clearance, refuelling and control paths are not suitable to copy into player
industrial guidance. The native debug travel action changes position directly;
it is excluded as an operating precedent. Temporary docking collision protection
must not be extended into industrial collision immunity.

### Research and existing mod precedent

**NASA Goddard Space Flight Center**, reported by Madison Olson on 21 March 2017,
describes Raven's visible, infrared and lidar sensors and machine-vision processing
for autonomous relative navigation. This supports treating observation freshness
and relative pose as continuous requirements, rather than accepting arrival once
and assuming the target stays aligned. It does not establish our sensing range,
cutting tolerances or game implementation.
[Primary source: NASA Raven computing and navigation](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/).

**ESA's ATV-5 LIRIS experiment**, reported 9 December 2014, collected infrared
and lidar observations during the vehicle's normally guided rendezvous. ESA
credits **Airbus Defence and Space**, **Jena Optronik** for lidar and **Sodern**
for infrared cameras. This supports researching surface observations for a target
without dedicated rendezvous hardware. LIRIS was an experiment alongside the
normal system, not evidence of autonomous wreck cutting.
[Primary source: ESA's ATV-5 observations](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before).

**Gravy's Auto Dock** describes RCS travel, player-obtained clearance and final
docking. It is a community precedent for phased approach/attachment, not permission
to copy its implementation or evidence of G4 clearance.
[Author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3745448384).
The existing adaptation of **Gravy / mrkmg's Auto Navigate** retains its
[separate attribution and unresolved reuse terms](auto-navigate-reuse-review.md).

All equipment, work rates, recovery yields, thresholds and the proposed capture
window below are authored game design. No institution or author endorses or
validates this mod. Existing artwork has separate
[hull-intake provenance](../assets/phobos-hull-intake/README.md); this round creates
no artwork and places no citations inside sprites.

## G4 selection, positioning and observations

Use the native full object ID as the durable identity. Display the equipment name,
a short ID, hull location/facing, connected chute/D4 and readiness. Start with the
existing eight-character display convention, lengthening colliding labels until
they are distinct within the selector. Full IDs remain available in details/F3.
Never resolve by a shortened label, row number, translated name or nearest item.
Damage/repair mode changes retain identity but revoke readiness. A replacement
object is a different selection even if installed at the same position.

The [current intake geometry](shipbreaker-hull-intake.md) is retained: a 4-wide by
3-deep G4, a 4-by-1 chute over four intact walls, and an aligned internal D4.
The G4's local +Y points outward. No part of the player's backing wall, chute or
attached support is a target. The first proposed capture window is the **open
leading edge of that footprint**: a target wall must touch that edge and project
fully within its width, with a clear path into the capture bed. Do not infer an
extra one-tile reach from the laser depiction. This window is a conservative
design proposal requiring native layout checks, not measured cutting reach.

For the primary free-flight mode, evaluate the selected G4, both complete hulls
and the target part in a verified common physical frame. Validate all four
mounting orientations, a collision-compatible approach and a bounded retreat.
Preview only observed surface work; loaded grid data does not grant a remote
interior survey. Missing surface observations prevent cutting readiness. Native
docking-fit transforms are evidence for attached layouts only, not a substitute
for establishing the unattached transform.

Optional attached work must additionally compose the exact native port-pair
rotation/offset, remeasure after attachment and protect its supports until release.
No G4 handoff or different target-port substitution occurs during the mission.

The required working quantities are tool-to-surface position,
normal alignment and relative velocity at the tool, including each body's angular
motion. Centre distance and centre velocity are insufficient. Derive a transform
from observed native coordinate conventions; never declare one deck tile to be
one navigation metre simply because a local transform uses unit coordinates.
Native motion samples belong at the same system-update boundary used by docking.

Qualified native contacts remain required for approach and repositioning. A
loaded target or a displayed silhouette alone is not a remote surface sensor.
Unattached surface mapping needs an explicit observation source and validity
contract, including how the native target becomes and stays sufficiently loaded
for work. Physically adjacent loaded geometry may support obstruction checks only
where its mapping and access are established. No silent active-sensor enabling or
hidden interior scan. Missing or stale measurements suspend acquisition.

## Acquisition, target integrity and completion

The first slice accepts an exposed, undamaged installed `ItmWall1x1` that can
finish as the existing empty, unstacked 24 kg `ItmWall1x1Loose`. It must belong
to the selected derelict and be inside the measured capture window. Exclude
stations, the player's hull, unrelated attached vessels, occupied targets,
pressurised/unsafe work regions, dock supports, containers and slotted contents.
Humans and robots are occupants; uncertainty blocks work rather than approving it.
Admission must verify native ownership/license eligibility. The first slice
declines unauthorized work and ambiguous jurisdiction; it does not replace the
game's crime system with docking clearance. Preserve native consequences on the
supported action path and do not fabricate a crew member on the wreck.

Shipbreaker owns paid cutting work and exact target progress, separate from D4
processing. The implementation must specify measured electrical demand, work
duration and a finite heat destination before enabling the cutter. Existing 2 kW
intake motors are not evidence for laser power. These balance values remain
unselected pending that bounded implementation design; no historical processing
recipe or progress field is repurposed.

Before completing a cut, confirm space for the actual loose object in the G4,
current working-pose clearance, the original part ID and a viable native loose-item
placement. Complete the supported native transition once, then resolve its ID
again. If native fitting moves it outside the working window, stop with retained
object evidence; never reach remotely to fetch it. Once detached, a dedicated
target-ship-to-G4 transfer adapter must verify the two bound ships, exact object,
range, locks and capacity, with synchronous restore to the target on a proven
detached failure. Reuse Framework's physical commit protocol without weakening
its existing same-ship `NativeItemTransfer` checks.

Journal the source ID, source/destination ownership, recipe/work revision and
commit phase before native mutation. On ambiguous completion, suspend and retain
evidence; do not roll outputs again or reconstruct a guessed missing object.
Reload may resolve a completed move by exact ownership, but never grants new
cutting/flight authority. Persist intent and physical progress, not live commands.

After each committed cut, refresh occupancy, obstruction, room/pressure state,
any attachment supports and the next exposed boundary. Re-resolve changed native
references. Do not assume cut-off islands become independent moving ships.
Initially keep them associated with their native target and stop when unreachable.

Completion states distinguish **window exhausted**, **supported work exhausted
with remnants**, **waiting for capacity**, **suspended/faulted**, and **fully
accounted target**. Whole completion requires accounting for installed parts,
loose cargo, nested/slotted/stacked objects, actors and attachment remnants; a
shrinking wall count is insufficient. Keep the last supporting structure and
target registration until empty-target detachment/cleanup has a tested contract.
The first slice leaves that remnant intact. Never call `Ship.Destroy` merely
because the work queue is empty.

## Feed-family catalogue

The existing read-only salvage inspector was rerun against the configured native
load order. Six requested identities resolved. Eleven duplicate core loot names
were reported; this is a static definition audit, not a runtime output guarantee.
The saved local report is `.local/research/shipbreaker-autopilot/material-audit.json`.
Use the [earlier material audit](powered-shipbreaking-design-findings.md) for its
method and the [residue contract](residue-material-contract.md) for saved jobs.

Reproduce with the existing inspector, supplying a local installation path:

```powershell
python scripts/inspect-salvage.py --game-path "<local game directory>" --item ItmWall1x1Loose --item ItmWallThin1x1Loose --item ItmWallAero1x1Loose --item ItmWallPlastic1x1Loose --item ItmFloorGrate01Loose --item ItmFloorGrate4x401Loose --output .local/research/shipbreaker-autopilot/material-audit.json
```

The inspector returns nonzero when it reports duplicate-definition warnings;
retain and assess those warnings rather than describing the audit as clean.

| Feed family | Evidence | Autonomous plan |
| --- | --- | --- |
| Ordinary wall, 24 kg | Existing D4 revision-2 recipe and native uninstall transition | First supported acquisition family; preserve existing products. |
| Ordinary floor, 6.5 kg | Native uninstall exists; inspected dismantle output remains unresolved | Next structural family needs its own accounted recipe and floor-support/cleanup tests. Unresolved output does not mean zero yield. |
| Thin/Whipple wall, 5 kg | Native dismantle declares 7-8 kg outputs | Do not copy a mass-creating budget; new characterized recipe required. |
| Aerodynamic wall, 4 kg | Native dismantle declares 7.6 kg outputs | Separate geometry and balanced recipe required. |
| Plastic interior wall, 14 kg | Native dismantle declares 7.6-13.8 kg outputs | No automatic aluminium/steel substitution; account polymers and retained remainder. |
| Turbine lifter, 65 kg | `ItmFloorGrate4x401Loose` is machinery; declared output 14-21 kg | Do not classify by the word Floor in its ID. Machinery needs explicit disassembly and contents rules. |
| Other equipment, cargo and provider-defined objects | No generic Phobos acquisition/recipe contract | Leave intact and list as unsupported. Add only explicit family recipes/providers. |

Useful items left behind are not automatically protected by value once a supported
family is selected, but accepting machinery never authorizes loss of its contents.
Unpack into finite physical destinations first or exclude that object. Keep fluid,
gas, hazardous and biological contents outside generic solid reclamation.

## Processing chain and operating windows

Authoritative sources: [wall recipes](../src/PhobosShipbreaker/Core/ProcessRecipes.cs),
[R4 rules](../src/PhobosShipbreaker/Core/ReclaimerRules.cs),
[routing rules](../src/PhobosShipbreaker/Core/RoutingRules.cs), and the
[F6 material route](furnace-material-routing.md).

Each new ordinary wall produces 1 kg mechanical parts, 2 kg aluminium, 2 kg carbon
fibre scrap, 6 kg steel and 13 kg identified residue. R4 converts that residue to
3 kg steel, 1 kg aluminium and 9 kg terminal rejects. Combined: **15 kg recovered
products + 9 kg rejects = 24 kg**, an authored budget, not an assay. Legacy 13 kg
unclassified residue remains ineligible for this R4 recipe.

| Stage/output | Delivered route or limit | Required autonomous extension |
| --- | --- | --- |
| G4 -> chute -> D4 | Detached ordinary walls; finite G4 storage and four-panel feed | Paid attached-wall acquisition into G4, then existing intake. |
| D4 identified residue -> R4 | Explicit paired route, finite R4 feed | Observe capacity and preserve separate receiving/processing permissions. |
| D4 useful products | Remain in the 8-by-8 product tray; existing sender path is residue | Explicit finite product storage route for parts, steel, carbon fibre and aluminium. |
| R4 aluminium -> F6 | Separate MetalsOut; exact individual 1 kg pieces; twenty-piece charge | Keep this contract. Combining D4 aluminium requires a separately designed multi-source route; no implicit fan-in. |
| R4 steel | Remains in product tray | Explicit finite storage route. |
| R4 rejects | Dedicated residue/collector route | Finite storage and a capacity stop. Collector is not space disposal. |
| F6 -> collector | Released 19 kg housing blank + 1 kg melt remainder | Repeated authorized batches and a physical onward storage route; a blank fills the collector's 2-by-2 grid. |

At shipped defaults, D4 needs 60 powered seconds per wall and R4 needs 120 per
residue, excluding transfers/cooling/interruptions. Thus one R4 cannot sustain
one D4 at its maximum wall rate. This is a rule-derived comparison, not a game
benchmark. Existing R4-only aluminium supply requires twenty new wall residues
for a twenty-piece F6 charge. Those walls also produce 180 kg terminal rejects,
plus other retained products. Storage and rejection capacity are core requirements.

Use finite native storage through an explicit exact-ID storage endpoint; do not
turn every container into an industrial receiver or add virtual inventory. First
support the existing explicit pair topology, one destination per output address.
Apply destination geometry, mass, item-filter and physical-grid admission before
starting more acquisition, then recheck at transfer. New ordinary-product output
addresses must not change existing ResidueOut, MetalsOut or Cooling meanings.
Do not guess a new container's capacity: derive it from its native definition.

The present [Phobos furnace service](../src/PhobosShipbreaker/FurnaceService.cs)
disarms work and receiving on **any nonzero RCS manoeuvre or torch request**,
retaining its physical hot state. This is our policy, not a demonstrated native
engine restriction. It currently conflicts with processing during active holding.

The proposed mission should permit concurrent processing during gentle positioning
where checked machine-specific limits allow it. Investigate acceleration/angular
motion, charge/door state, power priority and cooling headroom; select named,
authored limits with evidence before changing the interlock. Translation speed
alone is not acceleration. Do not simply remove the guard or repeatedly rearm it
after every guidance pulse. Keep the current behaviour until a tested replacement
is implemented. Strong manoeuvres/torch use and manual takeover must cause the
appropriate checked process hold, with retained heat/cargo and independent passive
cooling. Auto Nav always retains priority over industrial power demand.

Repeated F6 permission is a separate, opt-in mission setting binding the furnace,
cooling endpoint, room and existing recipe. Its sequence is receive a full valid
charge -> seal -> automatic thermal cycle -> safe equalize -> release -> route
outputs -> receive again. Do not melt partial charges or invent another recipe
to drain the final scraps. Preserve physical leftover aluminium at completion.
Opening a panel, linking a route, or enabling Receive alone never enables heat.
Changed rooms still require local recovery; automation cannot claim the local
valve override. Ambiguous native commits remain protected.

## Remaining limits and conclusion

Active positioning and progressive wall acquisition are the intended operating
model. Its feasibility remains conditional on a collision-compatible physical
reach/transfer path and valid surface observations. Native docking has a known
attached-grid transform, but that does not establish it as the required solution
or validate G4 cutting. No Unity/gameplay success is claimed.

Proceed with the bounded geometry/flight integration gate and useful wall-section
slice in the handover. Close the identified engine questions within that work;
do not make a diagnostic-only prototype the deliverable or describe whole-wreck
cutting as a small extension to Follow. If reach fails, record the exact engine
limit and a concrete equipment/design alternative before extending the mission.
New cutter power/heat balance, concurrent process limits, broader recipes, finite
storage and safe final cleanup remain explicit gates, not implied capabilities.
