# Limited autopilot nav module

Research and proposed design, 2026-09-20. The owner requested a deliberately
limited, "pseudo autopilot" nav module. **Feasible in principle; no Phobos
autopilot has been implemented or tested in-game.**

## Recommendation

Build a **Phobos Approach Assist** module: the pilot chooses a nearby contact,
clears the departure area and engages one cautious RCS approach. The module
accelerates, coasts and brakes, then hands control back outside docking range.
The pilot remains responsible for traffic, final alignment and docking.

Approach-and-brake is the recommended first function, pending the owner's
preference between that, velocity matching and a timed burn. The specific limits
below are design proposals, not approved balance values or existing behaviour.

The useful distinction is limited capability with predictable operation. Avoid
random steering errors or deliberately late braking as balance mechanics.
Space, power, fuel, speed and restricted operating conditions provide clearer
tradeoffs. This should be a salvageable piece of ship equipment with one job.

The owner subsequently suggested hardware installed throughout the ship as the
mod grows. Treat this as the intended expansion direction: the nav module can
become the control panel for additional equipment. The basic approach assistant
should still work by itself with the ship's existing propulsion and nav console.
The owner also asked to reuse the game's existing sensor system. **Native ship
sensors are the intended sensing foundation**, rather than a new compulsory
Phobos sensor family. The baseline still requires adequate native sensing.

## Player interaction

1. Install the physical module in a functioning nav console.
2. Undock and clear the local traffic area manually. Select a detected ship or
   station, reduce relative motion, and line up a clear approach.
3. The panel shows the locked target, relative speed, distance, intended stopping
   distance and an estimated fuel margin. It explains why it cannot engage.
4. Engage. The panel reports **Accelerating**, **Coasting** or **Braking**.
5. On arrival, the module reduces relative motion, stops applying thrust and
   reports **Approach complete — take control**. It does not dock or continuously
   hold position after completion.

The panel needs an Engage/Disengage control and a conservative speed selection.
A long list of tuning sliders, upgrade tiers and a second navigation map are
unnecessary for the first version. Closing the panel should not end a valid
approach; the hardware and gameplay service govern it independently of the UI.

## Deliberate limits

| Area | Proposed first-version rule | Player responsibility |
| --- | --- | --- |
| Propulsion | RCS only; respect actual thrust, fuel and a conservative acceleration cap | Maintain thrusters and remass; choose an affordable trip |
| Range | Nearby ship/station contacts only; maximum range to be measured | Perform long transfers and torch burns manually |
| Initial state | Require low relative speed and a roughly aligned approach | Recover from fast, tumbling or badly misaligned flight |
| Target | Lock one currently detected contact when engaged | Select the destination; changing the selected target does not silently redirect the ship |
| Guidance | Modest corrections for ordinary target drift; decline or abort pursuit of a manoeuvring target | Intercept difficult targets and react to changing circumstances |
| Arrival | Stop outside a clearance envelope that accounts for both ships' size | Complete close approach, alignment, communications and docking |
| Traffic | No route planning or collision avoidance | Keep the route clear and supervise the approach |
| Environment | Free-space approach only; exclude atmospheric flight and close planetary manoeuvres | Handle gravity-sensitive and atmospheric navigation |
| Automation | One finite manoeuvre; no automatic undocking, route queue, salvage loop or reacquisition | Start each trip and recover from interruptions |
| Hardware | Installed, undamaged module and powered working console; space occupied on the pegboard | Repair and power the equipment |

Do not choose final speed, range, acceleration or stopping-distance numbers from
guesswork. Tune them against light and heavy ships in a separate test save. A
distance measured only between ship centres is insufficient for large stations.
Nor does a low cruise speed guarantee a safe approach when initial relative
velocity or lateral drift is high.

## Expansion through shipboard hardware

Add equipment when it enables a useful new capability. The following are
candidate roles, not selected devices or verified engine capabilities. Their
costs should come from space, installation, power and maintenance; final numbers
depend on gameplay tests.

| Candidate hardware | Possible added capability | Design constraint |
| --- | --- | --- |
| Guidance computer | More capable intercept calculations and longer approaches | Additional installed, powered equipment; the basic module keeps its short, conservative operating envelope |
| Proximity processing unit | Clearance display and warnings derived from existing sensor contacts | Prove that native contact/geometry information is sufficient first; add a new detector only for an evidenced gap. Warnings do not imply automatic avoidance |
| RCS control interface | Finer thrust regulation and adaptation to uneven or degraded available thrust | Must remain within actual thruster performance and fuel use; more precise control cannot create extra thrust |
| Navigation backup battery | Brief operation through a console/control-power interruption | Powers only the supported control equipment; cannot keep unpowered propulsion working or manufacture remass |

The nav panel should explain what the installed equipment currently permits and
why a function is unavailable. If an optional device fails, remove the capability
that depends on it and reassess the active manoeuvre. An advanced manoeuvre must
not silently continue under assumptions the remaining hardware cannot support.
Loss of a warning-only sensor should be distinguishable from loss of propulsion
control. Preserve the simple module's independent use where its own requirements
still hold.

Prefer the game's installation, power, damage and repair systems. Do not invent
a separate shipwide cable/network framework before a working feature needs one.
Physical placement should matter when it has a defensible gameplay effect, such
as sensor coverage or local vulnerability, rather than arbitrary compulsory
distance between boxes. Confirm native sensor coverage and line-of-sight limits
before promising either mechanic.

No tiers, additional devices or cross-mod dependencies are required for the first
prototype. As each device becomes concrete, add its capability checks to the
gameplay service; the panel continues to display state and delegate actions.

## Reuse the native sensor system

The local runtime contains `Ostranauts.Ships.Sensors.ElectronicSystems`,
`ShipSensor`, `ShipSignature` and concrete `Optical`, `IR`, `EM`, `Radar` and
`Lidar` implementations. These are ship/contact sensors. The unrelated interior
`Sensor` component used by alarms is not the navigation interface.

Native hardware in `condowners/condowners_ship_combat.json` includes
`ItmSensorOptical01`, `ItmSensorIR01/02`, `ItmSensorEM01/02`,
`ItmSensorRadar01/02` and `ItmSensorLidar01/02`, with off, loose and damaged
variants. The operating definitions reference native electrical/power handling
and damage transitions. Reuse the ship's maintained sensor state; still test
power and removal transitions instead of assuming a condition flag updates
instantly.

| Native sensor | Observed inputs to signal strength | Implication for the assistant |
| --- | --- | --- |
| Optical | Target cross-section, range, environmental visibility, relative speed and bearing | The native signal already responds to approach conditions; do not replace it with a fixed-radius check |
| IR | Heat signature, range and environmental visibility; the inspected heat calculation uses RCS acceleration and reactor-running state | A cold coasting target can be less useful to IR; do not assume all detectable targets radiate equally |
| EM | Emissions and range; the signature calculation includes active-sensor emissions and flying-dark state | Passive EM availability depends on what the target emits; one sensor's contribution is capped in the implementation |
| Radar | Cross-section, range and configured strength | Active sensing can support tracking but also contributes to our ship's emissions |
| Lidar | Cross-section, range and configured strength | Reuse its contribution; the inspected signal calculation does not prove that lidar automatically gives more precise docking geometry than radar |

Base range is a parameter in a signal calculation, not a guaranteed hard
detection radius. `ElectronicSystems.GetSignatureStrength` adds contributions
from the sensors. `ShipSensor.EMEmission` and `ShipSignature.CalculateEMEmissions`
provide an existing active-sensing tradeoff. The assistant must not silently turn
on radar/lidar to repair a weak track; leave that decision with the pilot.

The navigation path `GUIOrbitDraw.VisibleFromNavStation` applies combined signal,
the detection threshold, visibility and a body-occlusion check. It also contains
special cases for known stations, signal beacons, the tutorial and debug display.
Consequently, **an icon visible on the map is not by itself evidence of a live
sensor track**. A known station can be displayed with no current sensor lock.
For the first approach assistant, require an actual qualifying sensor signal and
valid visibility checks even for a known station.

`ShipDraw.GetPositionOffset(double)` adds positional uncertainty when the contact
is not fully visible. Do not bypass that by feeding exact world coordinates into
guidance for a partial contact. The first version should reject partial contacts
and release control when the qualifying track is lost, with a clear message.
This supplies a meaningful limitation without manufacturing sensor failures.

There are two integration traps:

- `ElectronicSystems.GetVisibleShipSignatures` defaults to excluding static
  targets, which includes derelicts and stations. It considers loaded ships and
  uses the best individual detection range, whereas the nav path uses summed
  signal. It is a useful reference, not a drop-in equivalent to nav visibility.
- `UpdateDetectionThreshold` uses `SkillOpsSensors` to select 0.25 rather than the
  default 0.3. Reading a track must not accidentally mutate shared detection rules
  or retain a departed operator's benefit. Operator selection and operation with
  the UI closed need explicit testing against native behaviour.

Implement one narrow contact reader for the gameplay service. It should return
whether the selected contact currently qualifies, why it does not, and permitted
navigation data. Keep UI-only state, debug visibility and global ship enumeration
out of the guidance contract. A new sensor fusion simulation or network protocol
is unnecessary. Off-console sensing and native occlusion checks remain integration
work, not a confirmed ready-made API.

## Interruptions and failure behaviour

- **Manual thrust or rotation:** immediately release automatic control. Do not
  keep fighting the pilot or resume automatically after a short delay.
- **Other automation:** refuse engagement while station keeping, hold-thrust,
  another autopilot or a torch programme owns the ship's controls. Do not globally
  unregister unrelated AI as a shortcut.
- **Power loss, removal or damage:** end the manoeuvre and clear this module's
  outstanding thrust demand. The ship continues moving; loss of power cannot
  provide a magical emergency brake.
- **Lost contact or invalid target:** release control with a reason. Do not read
  hidden world state to keep following a target the ship can no longer detect.
- **Insufficient fuel before departure:** refuse engagement. Reserve enough for
  braking plus a margin rather than spending the entire tank accelerating.
- **Shrinking fuel margin during flight:** brake early while control, contact and
  propulsion remain valid. If a controlled stop is no longer possible, report the
  problem without claiming the ship is safe.
- **Docking, ship change or invalid simulation state:** end this ship's operation.
- **Pause and reload:** no progress or fuel use while paused. Save settings, but
  reload disarmed with an interrupted-approach notice; require a fresh engagement.

Stopping thrust is different from stopping motion. In particular, the manual
override path must not erase the pilot's newly issued thrust while clearing old
automatic input. Control ownership and update order need a real test.

## What the installed game supports

These are read-only findings from **Ostranauts 1.0.1.4**, identified by the local
session log. The inspected `Assembly-CSharp.dll` SHA-256 is
`1dc1858a8edc514ec089f2fd7c55932c7f9b62b0a96201c15e2b2720122a03a7`.
The previously checked installation uses BepInEx 5.4.23.5; see
[modding findings](modding-notes.md) for the environment record. These observations
establish possible extension points, not a working integration.

Data paths below are relative to `Ostranauts_Data/StreamingAssets/data/`.
Runtime names identify methods/types in the local assembly. No extracted game
source or assets are included in this repository.

| Evidence | Observed behaviour | Consequence for our design |
| --- | --- | --- |
| `condowners/condowners_navmods.json`, `ItmNavMod` and `ItmNavModMobo` | Physical nav items have mass, price, damage handling and a `NavMod` GUI property map | A real inventory item is supported; merely adding a screen button need not be our final delivery |
| `cooverlays/cooverlays_navmods.json`, `ItmNavModCoursePlot` | An overlay derives a specialised module from a base item and supplies GUI and damaged-state mappings | A uniquely named native content definition can describe our hardware; exact repair and damage transitions still need verification |
| `guipropmaps/guipropmaps.json`, `NavModCoursePlot` | Maps a module to normal/damaged GUI prefab names and a default panel position | JSON describes placement and references, but does not implement a new controller or create a missing prefab |
| `GUIOrbitDraw.LoadModules` | Finds eligible module items in the console; resolves a matching child or loads a resource prefab; expects `Container` and `NavModBase`; checks layout fit | A custom panel needs C# UI integration or suitable asset registration. A named child created before module loading is a candidate, not yet a proven hook |
| `NavModBase.Start`, `EnableMod`, `DisableMod` | Binds to the nav console and subscribes to screen events; panels can be disabled or destroyed | Gameplay cannot depend on panel updates. Its `COSelf` is the console, not proof the separate module is still present |
| `GUIOrbitDraw.ToggleStationKeeping`, `HoldStationAutoPilot.RunCommand` | Native target-relative station keeping already exists, with manual-input handling | A velocity-match-only mod would substantially duplicate existing functionality |
| `FlyToAutoPilot` | Existing course-following command checks fuel and delegates navigation to other commands | Native automation exists; it does not establish that its whole planner fits our intended limits |
| `Ship.Maneuver` | Applies local thrust in world coordinates, consumes RCS gas, accounts for mass including docked ships and includes native damage behaviour | Use the native RCS path rather than directly writing position or velocity; explicitly select RCS mode |
| `Ship.RCSAccelMax`, `DeltaVRemainingRCS`, `CalculateRCSFuelConsumption` | Exposes useful acceleration and fuel-related estimates | Use as inputs after validating units, zero-thruster cases and agreement with actual manoeuvres |
| `Ship.StopManeuver` | Clears RCS acceleration and angular acceleration, not translational velocity | Disengage cannot be advertised as an emergency stop |

Native helpers require care: `FlightCPU.MatchRot` directly changes rotation and
`HoldStationAutoPilot` contains direct adjustments to velocity components. Calling
an existing helper does not by itself guarantee physical, fuel-limited movement.
Our service should request thrust and observe the resulting motion.

## Small implementation boundary

Use native JSON for the module's content and C# for guidance and integration.
Provisional identifiers are `PhobosNavModApproachAssist` and
`PhobosNavModApproachAssistDmg`; finalise them before persistent test objects exist.

Keep one gameplay service that accepts an engagement request, reads permitted
ship/contact state and owns the approach phases. The panel only displays that
state and delegates controls. The service checks the physical module, console
power and competing control modes throughout the manoeuvre.

The first technical proof should expose a simple panel on a test console, require
the module item, apply a short capped native RCS burn and relinquish control
cleanly on manual input. Establish update ordering, time units and fuel use there
before adding accelerate/coast/brake guidance. This is an integration experiment,
not a separate reusable autopilot framework.

Guidance must consider relative velocity in both axes, effective braking
acceleration, the stopping envelope and simulation-step duration. The familiar
`v² / (2a)` stopping-distance calculation can support an estimate in consistent
units; by itself it does not handle lateral drift, accelerating targets or a
large step that crosses the braking boundary. Do not just multiply real-time
updates by the fast-forward setting.

## Existing mods and whether this is worth making

The author's description of [Auto Navigate](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
already offers a physical nav module for automated RCS travel, with configurable
speed and arrival behaviour. [Approach Autopilot](https://www.nexusmods.com/ostranauts/mods/20)
also describes automatic approaches with speed and acceleration controls. These
are author-described capabilities, checked on 2026-09-20, not compatibility tests
by this project. Neither mod was installed or used for this investigation.

If the aim is simply less manual piloting, evaluating an existing mod is the
smaller job. A Phobos version is worth pursuing if its restricted operating
envelope, equipment requirements and explicit handoff are the desired gameplay.
The novelty is not the existence of autopilot. Avoid copying another mod's code
or artwork; any later reuse requires a separate licence check.

## Evidence still needed before calling it playable

Use a separate test save and record the exact game/plugin versions and results.

| Experiment | Acceptance evidence |
| --- | --- |
| Module and power | Installed module enables the function; absence, removal, damage and console power loss disable it even with the UI closed |
| Manual takeover | Input takes effect immediately; old automatic thrust is cleared without cancelling new pilot input |
| Fuel and mass | Measured native fuel use and acceleration agree with conservative estimates on light and heavy ships; empty tanks and no thrusters are handled |
| Approach geometry | Static and drifting targets, lateral motion and large stations respect the declared arrival envelope; unsupported starts are rejected |
| Competing controls | Station keeping, hold-thrust and other automation cannot issue simultaneous commands with our service |
| Time | Pause, normal speed and available fast-forward modes preserve behaviour; large updates cannot skip braking |
| Lifetime | Closing the UI preserves a valid operation; loading a save, changing ships and docking disarm it and clear stale control state |
| Information | Loss of detectable contact ends tracking; hidden contacts and unexplored ships do not become navigable through engine-wide lookups |
| Native sensing | Compare optical, IR, EM and active sensors, combined weak contributions, partial contacts, known stations without a live track, body occlusion and operator changes; never auto-enable emitting sensors |

No claim of collision avoidance, current-mod compatibility or in-game success
should be made until the corresponding behaviour has actually been tested.
