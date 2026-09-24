# Native sensors and Phobos instrumentation

Research and owner direction: **25 September 2026**. Status: **documentation
only**. This round adds no sensor hardware, public API, gameplay behaviour,
artwork, package update or installation. The integration priorities below are
future work; inspected code and definitions are not in-game validation.

**Implementation follow-up, 25 September:** the owner subsequently authorised
the first priority. [Auto Nav 0.9.0](auto-nav-sensors.md) now implements native
contact qualification, suspension on loss, explicit recovery and unknown
readouts. The baseline/gap findings below describe the inspected 0.8.1 release;
console observations, furnace probes and the proposed shared API remain future
work. Offline implementation checks are not gameplay validation.

## Owner direction

Use vanilla sensor packages wherever their actual capabilities fit navigation,
salvage, industry and longer-term habitation. The owner selected **full
instrumentation realism**, with **built-in basic probes and modular specialist
instruments**. Equipment needs a credible way to know the quantities on which
its displays and automatic decisions depend. Missing information is unknown or
stale, not a reassuring zero.

Basic process instrumentation can belong to the machine itself; every reading
does not require another furniture item. Specialist packages should add useful
measurements, coverage or diagnosis. Prefer comprehensible causes and practical
maintenance over arbitrary calibration chores or random accuracy penalties.
Exact new models, capacities, prices, thresholds and artwork remain decisions
for their implementation rounds. Follow [equipment naming](equipment-branding.md),
[localization](localization.md) and the [artwork policy](artwork-resolution-policy.md)
when those rounds arrive.

## Inspected baseline and evidence

| Component | Inspected baseline |
| --- | --- |
| Ostranauts | 1.0.1.5; native definitions and `Assembly-CSharp.dll` |
| Assembly SHA-256 | `91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E` |
| Phobos Framework | 0.12.0 |
| Phobos Auto Nav | 0.8.1 |
| Phobos Shipbreaker | 0.10.1 |
| Installed-mod metadata | 34 packages, 32 configured enabled, 2 configured disabled; no inventory read errors |

Game evidence belongs to Blue Bottle Games. Native paths below are relative to
`Ostranauts_Data/StreamingAssets/data`; runtime names identify inspected methods,
not copied implementations. Local decompilation stays outside Git and packages.
The eight core ship-sensor class inspections were compared with fresh output
from the current assembly during this research. No gameplay tests were run.

| Evidence | What it establishes |
| --- | --- |
| `condowners/condowners_ship_combat.json`, `powerinfos/powerinfos.json` | Ship sensor packages, range/strength conditions, installed/off/damaged forms and native electrical integration |
| `Ostranauts.Ships.Sensors.ShipSensor`, `Optical`, `IR`, `EM`, `Radar`, `Lidar` | Per-type detection inputs, range falloff, on/off state and active emissions |
| `ElectronicSystems`, `ShipSignature` | Combined contributions, operator threshold, package registration and target heat/EM/cross-section proxies |
| `GUIOrbitDraw.VisibleFromNavStation`, `StellarObjectVisible`, `ShipDraw.GetPositionOffset(double)` | Visibility exceptions, contact uncertainty and a distinct stellar-object path |
| `GUIOrbitDraw` silhouette handling; native `MFDSensors` | Contact-shape display and active/passive sensor controls |
| `Ship.UpdateSensors`, sensor registration/removal | Native sensor-list refresh and the pending-update flag |
| `condowners/condowners.json`, `guipropmaps/guipropmaps.json`, `condtrigs/condtrigs.json`; `GasPressureSense`, `Sensor` | Separate local room sensing, threshold conditions and alarm interactions |

Our [Approach Assist service](../src/PhobosApproachAssist/ApproachService.cs)
contains a narrow native-contact precedent. Current
[Auto Nav guidance](../src/PhobosAutoNav/NavigationService.cs) and its
[target adapter](../src/PhobosAutoNav/Adapted/TargetRef.cs) retain the different
behaviour described below. The [inventory helper](../scripts/inventory-mods.py)
was reused read-only; configured enablement does not prove a plugin loaded or
an integration works in the current game session.

## Verified native capabilities and limits

### Ship-contact sensors

The inspected item file contains these ten base operating definitions, with
additional state variants. This is a definition inventory, not an exhaustive
claim about every possible generated item or merchant offering.

| Package | Native ID | Configured base range, km | Mode |
| --- | --- | --- | --- |
| Polaris Optical | `ItmSensorOptical01` | 25 | Passive |
| NASA IR | `ItmSensorIR01` | 300 | Passive |
| Zhuangzi IR | `ItmSensorIR02` | 600 | Passive |
| NASA EM | `ItmSensorEM01` | 300 | Passive |
| Zhuangzi EM | `ItmSensorEM02` | 500 | Passive |
| NASA Radar | `ItmSensorRadar01` | 400; strength 1 | Active |
| Zhuangzi Radar | `ItmSensorRadar02` | 400; strength 1 | Active |
| Weber LiDAR | `ItmSensorLidar01` | 200; strength 2 | Active |
| Zhuangzi LiDAR | `ItmSensorLidar02` | 200; strength 2 | Active |
| Miura EO/IR | `ItmSensorEOIR01` | Optical 50, IR 600, EM 500 | Combined passive |

Base range controls signal falloff; it is **not a hard detection radius**.
The listed Radar/LiDAR brand pairs have matching range/strength conditions in
this inspection; this does not establish equality of price, durability or power.

| Type | Observed detection inputs | Unsupported interpretation |
| --- | --- | --- |
| Optical | Target cross-section, range, environmental visibility, total relative speed and bearing relative to the observing ship | An inspection camera, per-mounted-sensor field of view or close-range clearance map |
| IR | Range, visibility and an abstract heat signature derived from RCS activity and reactor-running state | Individual furnace temperatures, interior hot spots or measured radiator output |
| EM | Range and target emissions, including flying-dark state, beacons and active sensors; an individual contribution is capped | Cargo identification, chemical composition or diagnosis of individual electrical faults |
| Radar / LiDAR | Range, configured strength and an approximate cross-section; their inspected signal methods do not apply the visibility argument | A precision point cloud, surface-distance measurement or obstruction-free grabber path |

The ship's active sensors contribute to its EM emissions. Multiple sensor
contributions combine in the native contact calculation. The native detection
threshold is normally 0.3, or 0.25 with the applicable operator skill; these are
game signal thresholds, not physical accuracy or probability measurements.

Native navigation also handles body occlusion and display exceptions: known
stations, beacons and special/tutorial states can be visible without qualifying
live sensing. Partial contacts receive a displayed positional offset. The UI can
show a silhouette when its signal and zoom conditions permit, but this is not
evidence of measured cutting clearance or visibility through every obstruction.
Reading an exact target object behind a partial contact bypasses that uncertainty.

`GetVisibleShipSignatures` is not a drop-in navigation reader: its default
excludes derelicts and stations, uses the best individual detection range, and
updates shared operator-dependent threshold state. Reuse the native sensing
rules deliberately rather than assuming similarly named methods are equivalent.
The separate stellar-object path also needs its own inspection before asteroid
prospecting can claim parity with ship tracking.

Existing sensor types can be combined in another native package or given
different supported range/strength conditions. A new JSON name alone does not
implement a new measurement type. Furnace probes or chemical analysers can use
their own local observation adapter without pretending to be ship radar.

### Room alarms and thermostats

Native oxygen, nitrogen, carbon-dioxide, smoke and contaminant alarms, plus the
thermostat, evaluate local conditions through mapped sensing points such as
`RoomA`. Their components drive alarm/state interactions. Native pump controls
already expose pressure-sensor signal input, providing a precedent for reusing
alarm signalling rather than inventing another binary control network.

The inspected contaminant clear condition checks ammonia and sulphuric acid;
the smoke clear condition checks smoke and carbon monoxide. These names must
not imply detection of every future chemical. Likewise, an alarm/thermostat
state does not establish a calibrated numeric reading or a general telemetry bus.
An adapter should expose what the hardware supplies; exact concentrations need
an appropriately modelled instrument.

The mapped point determines the sampled room. The gas-alarm point query can
consider docked geometry, whereas the inspected generic `Sensor` query does
not use that same docked-ship option. Neither grants a Phobos console authority
over neighbouring ships. Room membership and authorized access are separate
questions to verify when compartments or docking relationships change.

## Existing Phobos gaps

- **Auto Nav 0.8.1:** follows underlying target objects without enforcing Approach
  Assist's full live-contact policy. The older prototype offers reusable sensing
  ideas, not a requirement to install it or restore its old flight/save limits.
- **Framework 0.12.0:** already provides definition registration, construction,
  maintenance, stock, localization, versioned object state, saved port pairing
  and console access. It has no shared instrument-observation service yet.
- **Shipbreaker 0.10.1:** exposes machinery status and reclaimer environmental
  checks, but does not provide a general native-alarm dashboard or a model of
  serviceable probes. Current raw game-state access must not be described as an
  already instrument-qualified measurement system.
- **Furnace, chemical plant and ore processing:** remain proposed features.
  Their hardware and measurement requirements belong in their designs from the
  start. This report changes no current recipes or equipment requirements.

## Proposed integration order

### 1. Sensor-aware Auto Nav

Create a narrow contact reader used by guidance and diagnostics. Reuse combined
native signal and meaningful occlusion checks; distinguish remembered destination
identity from a usable live track. It must work with the nav panel closed, explain
weak/missing contact, and refresh hardware/operator facts after loading or control
changes. Do not silently activate emitting sensors or substitute a nearby target.

The later implementation must define contact-loss behaviour separately for
ordinary approach and docking. Clear owned unsafe actuator requests, suspend
unsupported target-relative guidance and report the reason. Do not promise that
coasting is collision-safe or calculate a new precise intercept from hidden target
coordinates. Any continued manoeuvre using last observations needs an explicit
bounded prediction policy. This round adds no such policy or runtime behaviour.
Preserve [flight persistence](auto-nav-persistence.md),
[torch authority](auto-nav-torch.md) and [docking safeguards](auto-nav-docking.md).

### 2. Shared console observations

Show appropriate native alarm states with source identity, monitored compartment
and validity. Retain [C1's authorized host-ship scope](industrial-control-console.md)
through docking, towing and undocking. An accessible native alarm is not permission
to control another ship's pumps or machines.

Add **Why did this stop?** details connecting the blocked action to its alarm,
instrument condition and last valid reading. Built-in basic probes may support
existing machinery measurements; define their service and old-save treatment
before adding new runtime prerequisites. This documentation does not make
existing equipment stop working or add an external sensor purchase requirement.

### 3. Furnace instrumentation

Follow the [furnace design](fusion-smelter-research.md): appropriate chamber,
charge and cooling measurements, only for quantities the process actually models.
Built-in temperature probes, pressure gauges and flow/electrical instruments
support meaningful interlocks; specialist instruments can add capabilities later.
Do not use native ship IR as a furnace thermometer.

Royce's vacuum-induction equipment uses thermocouples and optical pyrometers,
alongside pressure and temperature control. This supports distinct measurement
roles, not our proposed furnace's exact specifications or thresholds.
[Henry Royce Institute](https://www.royce.ac.uk/technology-platforms/materials-discovery-and-prototyping/)

Keep physical truth separate from observations: an instrument fault can remove
knowledge or automatic control while the batch remains hot, pressure continues
to evolve and cooling still requires a real path. A missing reading must never
delete heat, matter or progress. The later process service owns its fault response.

### 4. Specialist analysis and external work

- **Material assay:** reveal supported feedstock characteristics before selecting
  a route. Use known authored composition where available; do not manufacture
  precise chemistry for unclassified cargo. Preserve the
  [residue contract](residue-material-contract.md): analysis must not reroll legacy
  residue, improve yields by creating matter or convert terminal rejects into ore.
- **Asteroid survey:** separate contact detection, shape mapping, estimated surface
  composition and sample analysis. NASA's OSIRIS-REx carried distinct cameras,
  laser ranging, spectrometers and sampling equipment. That is a precedent for
  complementary gameplay instruments, not evidence those capabilities already
  exist in Ostranauts. [NASA](https://science.nasa.gov/mission/osiris-rex/in-depth/)
  Keep [native tethered mining](asteroid-life-support-research.md) as the acquisition
  baseline; a survey neither supplies ore nor proves an asteroid's entire contents.
- **Grabber inspection:** investigate local ranging and obstructions when external
  cutting is active work. Native contact signal and a silhouette do not establish
  a safe reachable surface. Keep the current detached-panel intake's scope clear.

### 5. Endurance and later diagnostics

Console/PDA trends, leak warnings and event histories can explain deterioration
and support decisions about maintenance away from stations. Preserve the existing
PDA/visor connection-view proposal as future work. Do not reproduce the installed
cargo manifest simply to add another scanner.

A later industrial-heat adapter could make actual heat rejection contribute to
ship detectability. Native IR does not currently provide that link. Wait for a
credible heat model; do not equate an on/off flag with measured thermal output or
turn the project into a speculative whole-ship thermal simulation.

## Proposed Framework boundary

Document this minimal observation concept now; introduce an API only when
concrete consumers need it. It is not a wire format, saved schema or existing type.

| Observation information | Purpose |
| --- | --- |
| Instrument/provider identity and observed object identity | Attribute the reading and prevent silent reassignment; use stable full IDs where supported |
| Measurement/alarm kind, units and available value | Separate numeric readings from binary or multi-state alarms; name unit conversions |
| Observation time and validity | Distinguish current, stale, unavailable and faulty; retain the actual time of a last valid sample |
| Measurement scope and supporting capability | State which ship, compartment or process is observed and what installed hardware permits it |

Framework owns common access, pairing, validity reporting and diagnostic
mechanisms. Content owns instruments, balance, credible measurement semantics
and action-specific responses. Reuse existing versioned storage and console
services rather than duplicating them. Saved material-port pairs are a useful
identity pattern, not an already implemented telemetry or liquid-transfer API.

Use native alarm signalling where it fits; continuous values need a separate
checked adapter. A native signal percentage is not a justified accuracy estimate.
Do not attach invented decimal precision or uncertainty claims to a threshold alarm.

UI, console commands and automation read the same observations and delegate
actions to checked services. Viewing a screen must not advance physics, take
control, enable a sensor or rewrite saved data. Physical simulation can still
use world state to apply actual hazards without exposing that state as an
unsupported player measurement.

After reload, reassess sensing before describing saved observations as current.
Keep history distinguishable from live state, preserve future/corrupt envelopes,
and retain existing flight versus industrial resumption policies. Avoid global
last-readings files or repeated whole-world scans; scope reads to relevant targets
and instruments. Choose update intervals when each consumer's needs are concrete.

## Optional providers and compatibility

The refreshed metadata identified these configured-enabled companions:

| Provider | Version | Integration boundary |
| --- | --- | --- |
| [Common Sense Cargo Manifest](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481501) | 0.12.10 | Keep existing inventory visibility; specialist assay needs a different useful role |
| [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189) | 0.16.1 | Preserve provider-owned tank, potable-water and refuelling state |
| [Common Sense Firefighting](https://steamcommunity.com/sharedfiles/filedetails/?id=3790484261) | 0.12.6 | Reporting an alarm does not require duplicating its crew response system |
| [Testudo Safe Pump](https://steamcommunity.com/sharedfiles/filedetails/?id=3768554122) | 0.3.3 | Investigate a narrow adapter if needed; preserve existing pump controls |

These are overlap candidates, not verified telemetry APIs. Optional absence or
incompatibility should make the associated observation unavailable, with an
explanation. It must not invent a zero, rewrite provider cargo or remove foreign
definitions. Follow [dependency contingencies](dependency-contingencies.md) and
[chemical storage boundaries](chemical-storage-and-process-fluids.md); release
age alone is not a reason to disable support. No new dependency is declared here.

Native behaviour and analysis are attributed above; external engineering sources
provide design precedent, not game implementation evidence. No third-party code
or artwork is copied by this research. Future adaptations must preserve verified
terms and provenance separately from the owner's permissive working assumption.

## Documentation validation

Checked eight changed Markdown files, 84 local link occurrences and referenced
Markdown anchors; local targets resolved and whitespace/encoding checks passed.
The two engineering sources and Ship's Water listing were accessible during
research. The other three Workshop references retain their installed package
IDs, but their pages could not be retrieved by the research browser; this is not
evidence that those mods were removed or are incompatible. No builds or gameplay
tests were needed for these documentation-only changes.

## Unresolved work and focused verification

| Later integration question | Meaningful check |
| --- | --- |
| Live navigation qualification | Combined weak contributions, partial contacts, known stations without live signal, body occlusion and target changes; no hidden exact-position fallback |
| Native state freshness | Removal, damage, switch/power transitions and pending sensor-list refresh; operator changes with the nav screen open and closed |
| Contact loss and emissions | Approach versus docking interruption, torch/RCS authority release, stale observations and recovery; passive choice never silently enables active scanning |
| Local alarm access | Resolve the actual sampled room after splits/joins and docking; check signal-input behaviour without assuming numeric telemetry |
| Ship/console boundaries | Docked, towed and moored neighbours remain separate; missing or replaced full IDs do not silently bind to another instrument |
| Observation persistence | Save/reload, stale history, unavailable providers and unsupported records; no fabricated fresh sample or restart of saved jobs by viewing status |
| Machinery instrument faults | Missing readings remain unknown while heat, pressure, cargo and cooling continue under the actual process rules; define built-in probe service/migration before gating old machines |
| Specialist analysis | Survey estimates versus samples, credible material identity and mass-balanced outputs; legacy residue and rejects retain their meaning |
| Cost and coexistence | Multiple consumers reuse bounded observations; optional provider failure stays local and does not duplicate controls or trigger a whole-world scan per frame |

These are future checks, not tests passed by this documentation. Do not require
new isolated proof tests of established sensor power/installation patterns.
Concentrate on our reader, authority changes, display semantics and persistence.
The owner performs gameplay tests; successful builds remain offline evidence.

The original implementation order was **sensor-aware Auto Nav**, followed by
**shared console observations and furnace instrumentation**. The subsequent
[0.9.0 implementation](auto-nav-sensors.md) supplies the first slice and its
contact-loss policy. Probe failure/service behaviour, new hardware specifications
and calibration or survey models remain decisions for their concrete consumers.
