# F6 first processing cycle: research specification

**Direction updated:** the owner has now [approved electrical heating](furnace-electrical-direction.md)
for the first furnace. That decision supersedes this report's original direct-fusion
source and reactor-side coupler. The material, thermal, gas and operating design
remains the baseline; the direct-source audit below is retained as research history.

**25 September 2026 — research complete; furnace runtime not implemented.**
This is the concrete follow-up to [fusion furnace feasibility](fusion-smelter-research.md).
The [UI reuse and graphics brief](furnace-ui-and-art.md) and
[interactive layout study](../assets/phobos-furnace/research/layouts.html) accompany it.
All new dimensions, recipes, prices and process parameters below are proposals.
Nothing here changes existing jobs, item definitions, packages or installations.

## Verdict and first product

Recommend a **non-pressure aluminium service housing**, usable as an optional
construction input for the D4 dismantling fixture and R4 reclaimer. Make a captive
rough casting, cool it, then finish its mounting faces with ordinary workshop
tools. It is a casing/support cover, not a pressure vessel, precision bearing,
motor, circuit board or certified structural alloy.

The design closes a material loop and can reduce attended fabrication work, but
its gameplay advantage is modest: existing recipes already use scrap. It does
not solve the supply of mechanical parts or electronics. Retain this as the
first **process demonstrator with a real consumer**, not a justification for
replacing every existing recipe. A 6 x 6 furnace is a substantial investment for
this one product; broader useful castings would need evidence before expansion.

**Direct fusion delivery remains the implementation blocker.** The native
electrical abstraction does not expose a conservatively accounted process-heat
outlet. The research identifies candidate hooks and a required debit test, not a
verified adapter. Do not start a release by supplying heat from reactor presence.
The useful next runtime slice must solve that accounting together with the cycle.

### Construction versus repair: actual current contracts

Rechecked the current construction catalog and `EquipmentEconomy` against the
installed 1.0.1.5 definitions. Native unit masses/prices are steel 1 kg/$3.60,
aluminium 1 kg/$1.10, mechanical parts 0.5 kg/$5, electronics 0.5 kg/$14.50.
These are base definition prices, not live merchant quotes.

| Consumer | Current input bill | Output | Work / base material value | First-casting assessment |
| --- | --- | --- | --- | --- |
| D4 section | 50 steel, 24 aluminium, 10 mechanical, 2 electronic units | 80 kg section | 3,600 recipe work-seconds / $285.40 | Enough aluminium for one useful casing; strong initial candidate |
| R4 section | 56 steel, 26 aluminium, 12 mechanical, 4 electronic units | 90 kg section | 4,500 recipe work-seconds / $348.20 | Same casing family is useful; retain machinery-specific finishing/assembly |
| D4 damaged-machine repair | 4 steel, 2 aluminium, 4 mechanical, 4 electronic units | Persistent repaired machine plus replaced-material waste | 3,600 native progress units / $94.60 | Only 2 kg aluminium; 18 kg casting is unsuitable |
| R4 damaged-machine repair | 4 steel, 2 aluminium, 6 mechanical, 4 electronic units | Persistent repaired machine plus replaced-material waste | 4,200 native progress units / $104.60 | Mixed failure categories; a universal cast repair part would invent missing capability |

Construction work-seconds and native repair progress are different contracts;
they are not measured player completion times. Both construction paths use
`TIsToolMortorq` and `TIsToolSoldering` at native Bar/Dining Tables, with the existing
optional bench support. A cast housing must not replace the electronics/tools.
Current D4/R4 repair also requires Mortorq and soldering tools; its work quantity
is native repair progress, not a furnace finishing recipe.

Propose two **additional**, versioned construction recipes, leaving existing ones:

| Candidate alternative | Bill | Mass | Proposed work |
| --- | --- | --- | --- |
| D4 section with finished housing | 50 steel + 6 aluminium + 10 mechanical + 2 electronic units + one 18 kg housing | 80 kg | 2,400 recipe work-seconds |
| R4 section with finished housing | 56 steel + 8 aluminium + 12 mechanical + 4 electronic units + one 18 kg housing | 90 kg | 3,000 recipe work-seconds |

Add 600 work-seconds for finishing each rough casting, using `TIsToolMortorq`
and the native `TIsToolWelding` contract. The design targets **600/900 fewer bench
work-seconds** per D4/R4 section after finishing. Furnace handling and process
time are additional; this is not a total completion-time saving or measured fuel saving.
Mechanical/electronic scarcity remains. Reuse native hauling for physical outputs.

### Proposed material contract: one recipe, one 20 kg batch

| Stage | Inputs | Outputs |
| --- | --- | --- |
| Captive remelt/cast | 20 x `ItmScrapAluminum`, exactly 1 kg each | 19 kg rough housing + 1 kg retained melt remainder |
| Bench finishing | One 19 kg rough housing | One 18 kg finished housing + 1 x `ItmScrapAluminum` offcut |
| Optional recovery of an unused housing | Its exact 18 or 19 kg object | Same mass as native aluminium scrap; no added parts |

The 1 kg melt remainder is an authored reject budget for mixed contamination,
unusable heel and handling losses, **not a chemical assay or 1 kg of newly created
oxide**. It is a physical, terminal, non-trash feed-excluded packet. Do not accept
legacy residue, R2 residue, terminal rejects, mixed metal objects or ore. No recipe
rerolls rejects. The recoverable finishing offcut may be remelted; each subsequent
batch still loses its specified fraction to terminal remainder.

This deliberately accepts the game's generic aluminium abstraction for a low-demand
casing. It does not prove alloy composition or purification. No flux, oxygen,
protective gas or volatile-metal chemistry is claimed. Native chamber gas is
tracked as unchanged species in this first model. Reactive/contaminated specialty
feeds need separate chemistry and may not be admitted by widening the filter.

Suggested draft identities are `PhobosFurnaceHousingBlank`,
`PhobosFurnaceHousing`, `PhobosFurnaceMeltRemainder`; none is registered yet.
Use full names beginning with `Phobos' Rivetline`, with no artificial model for
ordinary material. Proposed base values: blank $55, finished housing $60, terminal
remainder $0.01. Processing can add labour value. Direct scrap cost is $22 per
batch; finishing returns $1.10 of scrap. Whole-housing dismantling returns only
$19.80 or $20.90. A purchased finished housing raises either section's material
bill by $40.20; producing it onboard consumes 2 extra kg at the furnace and
returns 1 kg at finishing. These are balance candidates, not a profitable-trade claim.

## Energy source: what the engine does and does not supply

Inspected assembly SHA-256:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.

| Native evidence | Consequence |
| --- | --- |
| `FusionIC.Run` assigns `StatICPwrTotal`, halves a local quantity for `StatICPwrFus`, and populates MHD/thrust display values. | The named readouts are not independent inventories of available energy. |
| `FusionIC.Fusion` derives fuel requests using a 10^12 multiplier and pellet rate; decrements D/He3 from same-ship canisters; checks shortfalls. | Observe one successful ordinary interval; never call extra fusion updates or use a pre-debit reading as a receipt. |
| Thrust calls `GetMaxTorchThrust` and core-temperature scaling; the fetched `StatICPwrThrust` is not used in that calculation. | Subtracting from the thrust gauge does not reduce actual thrust. |
| Successful fusion assigns `StatPower = jpi.fAmount`; both native IC ignition definitions specify **34,000,000**. | Replenishment is a fixed electrical availability abstraction, not power integrated over the interval. A one-time subtraction is overwritten. |
| `Run` also supplies individual MHD modules with `StatPower = 2`. | Debiting only the core can miss an electrical output path. |
| `Powered.GatherPower` / `TransmitPower` and recharge distribute stored quantities; `UserPowerExt` has no returned delivery receipt. | A real debit must reach actual delivered electricity and every relevant producer, not just a UI property. |
| Auto Nav temporarily owns FLOW/CYCLE/RATIO for flight. | Industrial demand is subordinate; never ignite, force a ratio, suppress pilot input or shut down the reactor. |

Candidate observation points are entry/exit of the existing `Fusion` interval,
its successful replenishment assignment and the electrical delivery/recharge paths.
A postfix can witness completion but **does not by itself implement allocation**.
The unresolved conversion is how a finite diverted pre-MHD quantity maps to the
native fixed replenishment and individual MHD outputs. Capping all electrical
generation to a newly authored physical model would be a broader reactor rebalance,
not a narrow furnace integration. Do not introduce that change silently.

Required acceptance experiment for the future adapter: under identical native
source state and controlled electrical demand, enabling a known furnace allocation
must reduce the corresponding available/delivered output by its declared equivalent;
disable it and baseline delivery returns. Include demands near the source limit,
repeated replenishment, multiple MHD modules, recharge and two consumers. Ordinary
light loads alone will not reveal a debit against a huge replenished reserve.
Until this holds, the heat provider returns **Unavailable**.

The proposed provider contract uses source ID, session/interval identity, completed
simulation interval, requested/granted kJ and coupling loss. One Framework-owned
ledger allocates a source budget across consumers. Commit a native reservation
before issuing receipts, grant no more than available/requested, and invalidate
unconsumed receipts on authority loss or reload. Captured thermal energy remains
in the batch. No wall-clock accrual, stored permission replay or grants after fuel
shortfall. Flight demand cancels future industrial grants before steering; an
already committed finite receipt may finish accounting, without commandeering flight.

## Thermal and physical design

Retain F6's 6 x 6 footprint and 50 kg chamber rating as **design headroom**. The
first mould accepts exactly 20 kg; it is not a variable-yield 50 kg recipe.

Propose a **2 x 2 reactor-side coupler**, directly beside an eligible native reactor,
and a short straight shielded connection to the furnace (at most two intervening
tiles). Use a closed high-temperature radiative transfer cavity with a shutter
and refractory receiver as the visual/engineering concept. It is an authored
extension of the game's fictional reactor, not raw plasma in a water hose or a
verified native outlet. Its pickup/emitter physics and source debit still need proof.
Allow only same-ship paired full IDs and inspected geometry, never range-only or
nearest-object pairing. Electrical conduit independently supplies controls/pumps.

Propose a **6 x 4 fixed exterior radiator** with a dedicated short cooling connection
through a sealed fitting at intact hull. Maximum two intervening interior tiles;
no ship-wide thermal network. Backing walls remain the pressure boundary. The
layout is a fit study, not a claim that mounting is implemented. Obstruction or
foreign/docked geometry blocks the affected path. A sealed factory cooling assembly
has finite thermal capacity; it neither consumes potable water nor needs a new
generic fluid network for this first cycle. The candidate uses passive heat pipes
and a controllable thermal path that fails open to HIGH on electrical loss.
The gas evacuation pump is a separate powered function. Cooling auxiliaries below
are controls/actuation, not a free-running electrically driven circulation pump;
if hardware development instead requires pumped circulation, recalculate power-loss
cooling with that flow stopped.

### Calculation assumptions and results

Run `python scripts/calculate-furnace-cycle.py --output docs/research/furnace-cycle-calculations.json`.
The [generated numbers](research/furnace-cycle-calculations.json) are offline
design estimates. All model units are kJ, kW, kg, kelvin and simulation seconds.

- Charge: 20 kg; solid effective heat capacity 1.05 kJ/kg/K; liquid 1.177;
  melting plateau 933.45 K; latent heat 397 kJ/kg; target 973.15 K (700 C).
  These rounded aluminium surrogates are informed by [NIST thermochemistry](https://webbook.nist.gov/cgi/cbook.cgi?ID=C7429905&Mask=2).
  They are not measurements of native scrap. NIST's standard liquid formation
  enthalpy alone is **not** the latent heat at the melting temperature.
- Lining: 30 kg at effective 1 kJ/kg/K. Charge and lining share an equilibrated
  hot node in this calculation. A later runtime model must validate finite internal
  transfer; this model cannot support separate claimed charge/core probe readings.
- Cooling assembly: 100 kg effective mass at 0.8 kJ/kg/K; 250 C maximum;
  hot-to-sink transfer capped at 100 kW and 0.30 kW/K conductance.
- Radiator: **12 m2 effective emitting area**, emissivity 0.85, 200 K equivalent
  background, clear exposure. This is a required physical design surface after
  view-factor/self-obstruction losses, not a conversion of 24 game tiles into m2.
  Artist must show a credible fin pack; native tile metres/realization of that
  area remain a layout check. Do not count mutually facing fins at full area.
- Heat: 250 kW delivered ceiling, 90% coupling, 2 K/s solid preheat ramp, 60 s qualifying
  hold. Auxiliary delivery is 2 kW when heating/holding and 1 kW cooling, with
  protective throttling at sink saturation. All delivered auxiliaries become heat.
- Enclosure-to-room transfer: 1 W/K against a 25 C accepting room. The estimate
  assumes finite room headroom is available throughout; runtime must check it.
  Vacuum/missing gas removes this pathway and leaves the untransferred heat stored.
- Release: hot node no warmer than 50 C; cooled hatch and pressure equalization
  also required. Proposed radiator output falls from **42.40 kW at 250 C** to
  **5.38 kW at 50 C**, explaining the long cool tail. The radiative equation and
  environmental boundaries follow [NASA thermal-control guidance](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/).

| Scenario | Heating + hold | Through 50 C thermal release | Reactor-side energy | Peak sink |
| --- | ---: | ---: | ---: | ---: |
| Cold, 250 kW ceiling | 418.75 s | 3,229.50 s (53.8 min) | 47.370 MJ | 250 C |
| Half delivered ceiling, 125 kW | 459.25 s | 3,267.50 s | 47.399 MJ | 250 C |
| Lining initially 100 C, fresh cold feed | 396.75 s | 3,205.50 s | 44.870 MJ | 250 C |
| Next batch after 50 C release, retained lining/sink heat | 411.50 s | 3,236.50 s | 46.537 MJ | 250 C |
| Source unavailable at 100–280 s, then resumed | 599.25 s | 3,405.75 s | 47.410 MJ | 250 C |
| Half effective radiator area | 418.75 s | 6,209.50 s (103.5 min) | 47.370 MJ | 250 C |

These exclude handling, evacuation, pressure equalization and bench finishing.
The warm case conserves 2.25 MJ already in the lining before equilibrating a cold
charge; it is a sensitivity case, not permission to open a hot cassette. A separate
cool loading path would be needed to exploit it. Cold heating stores 42.466 MJ
in the hot node. At release, 1.275 MJ remains there and 1.566 MJ in the sink.
The cold energy balance is 47.370 MJ source + 3.648 MJ auxiliaries = 47.562 MJ
radiated + 0.616 MJ room + 2.841 MJ retained (rounding applies).
The repeat-batch case removes the cooled casting, admits cold feed and carries
forward 0.750 MJ in the 50 C lining plus 1.566 MJ in the approximately 44.6 C sink.
Its 2.316 MJ initial store is counted explicitly. It assumes gas recovery and
safe loading have completed; their extra time/heat is not simulated. A partly
full receiver cannot simply accept a second evacuation: two such charges would
exceed the proposed 200 kPa operating limit even at the original temperature.

More peak input barely improves total throughput. Prefer adequate radiator area
and a useful unattended workflow over increasing the heat ceiling. **Recommend
keeping 250 kW only as the hardware ceiling**, not a continuous duty promise.
The isolated 20 kg casing does not yet justify a faster or larger industrial plant.

## Chamber gas and a repeatable batch

Use an 80 litre process volume and integrated 50 litre receiver. At 25 C and
100 kPa, the chamber holds 3.22716 mol. Pumping to 0.1 kPa transfers 3.22394 mol;
0.003227 mol remains. A previously empty receiver reaches 159.84 kPa, or
178.60 kPa if it heats to 60 C. Set a proposed 200 kPa operating stop and 300 kPa
design rating. Use actual species vectors, temperatures and volumes, not the
sample room's presumed oxygen percentage. This is low-pressure processing,
not certified oxygen-free metallurgical vacuum. Residual pressure at 700 C
would be approximately 0.3264 kPa before leaks/outgassing.

Propose a maximum pump transfer of 0.05 mol/s, with backpressure and received
electricity reducing it. Ideal evacuation needs at least 65 s; reserve at most
180 s before a seal/pump diagnostic stop. Confirm the cold pressure target,
then isolate the valve; hot pressure above 0.5 kPa blocks holding and heating.
No arbitrary pressure decrement or gas erasure is permitted.

Native `GasContainer` derives pressure from moles, volume and temperature using
an ideal-gas constant. `GasPump` subtracts/adds species to source/destination
pending maps and applies changes. It also has a void destination path. Reuse
the finite-container precedent only: a missing receiver must not turn into void.
Its temperature mixing is not a complete compressor-work model. Account received
pump electricity once as assembly heat and carry gas thermal energy with transfers;
the simple cycle estimate includes auxiliary heat but omits the small chamber-gas
thermal store. This limitation must be removed in runtime conservation checks.

After cooling, return receiver gas to the sealed chamber until its pressure
matches the freshly sampled local room within 1 kPa. At equal original temperatures
and pressure this reverses the original transfer exactly. A changed room or finite
receiver may require an explicit measured equalization with that same authorized
room; preserve every mole and its energy. Unknown compartment/species, harmful
contents or insufficient capacity blocks release and requests service. Do not
silently vent to space, backfill from nothing, or force room pressure to a setpoint.
Any receiver remainder is saved and reduces the next batch's available capacity.

## Cycle, instruments and interruption rules

| Phase | Entry / actual work | Advance or block |
| --- | --- | --- |
| Idle / load | Cold accessible cassette, one exact recipe, verified physical feed; capture recipe revision | Reject incompatible/nonempty unexpected contents; no remote inventory manipulation |
| Verify | Full source/sink IDs, geometry, ownership, mould, receiver, output capacity and basic probes | Explicit reasons; missing telemetry is Unknown |
| Seal | Command latch and confirm actual closed state | Failed/damaged seal prevents evacuation and heating |
| Evacuate | Move native species to finite receiver using received auxiliary power | Reach 0.1 kPa cold target; timeout or full receiver stops |
| Preheat | Request energy under 250 kW cap and 2 K/s ramp | Progress is actual enthalpy; partial supply slows it |
| Melt | Pay latent heat at phase plateau | No time-only completion; confirmed liquid fraction required |
| Hold | 60 continuous simulation seconds at 699.5–700.5 C and hot pressure <=0.5 kPa | Excursion resets hold; loss of valid process probe isolates heating |
| Solidify / cool | Close heat shutter; captive mould retains whole batch; transfer heat to finite sink | Keep contents captive; no gravity pour or hot-item ejection |
| Equalize | Below 50 C, measure room, return captured gas then bounded room exchange if needed | Unknown receiver/room or pressure mismatch retains batch |
| Release | Stage 19 kg blank and 1 kg remainder into physical inventory exactly once | Full output retains casting; idempotent saved delivery prevents duplicates |
| Service / fault | Keep contents, heat, receiver inventory and wear; local maintenance | Acknowledge clears notification, not cause; Resume rechecks interlocks |

AUTO and STEP use the same phases and interlocks. STEP requests the next eligible
transition and cannot force a cold reading, skip latent heat or bypass cooling.
Instrument setpoints are bounded requests: actual heat, temperature, pressure and
phase come from the process. Lamp test is labelled TEST and never sets readiness.
OFF, Stop & cool and guarded Isolate cut heat/feed and retain protective cooling.
Manual reactor intervention, propulsion demand and ownership change revoke heat
permission. The native reactor is never shut down to protect this machine.

### Bounded operating requests

| Control | Proposed range / behaviour |
| --- | --- |
| Mode / sequence | OFF, STANDBY, RUN; AUTO or STEP. RUN requests an eligible transition, never bypasses verification. |
| Guarded heat enable | Explicit permission for this batch; clears on fault, flight preemption, reload or ownership loss. Separate guarded emergency isolation remains available. |
| Delivered heat cap | 25–250 kW in 25 kW detents; zero demand through OFF/Stop. Actual received energy may be smaller. |
| Solid preheat ramp | 0.5–2 K/s, default 2. Lower supply may slow it; no forced-temperature mutation. |
| This recipe's target / hold | Locked 700 C and 60 s in the first automatic recipe. Show live values; later recipes may own bounded adjustable ranges. STEP cannot lower the completion requirement. |
| Cooling LOW / AUTO / HIGH | Requested transfer caps 25/50/100 kW; AUTO raises to 100 kW above 200 C sink temperature. Physical conductance, source temperature and sink headroom always limit transfer. The calculations use HIGH. |
| Seal / evacuate / equalize / release | Phase-appropriate checked actions; receiver, temperature, room and latch evidence govern availability. Loading and maintenance remain local. |
| Stop & cool / isolate | Stop heating and feed immediately; retain passive cooling. Isolation additionally revokes the coupler's permission until an explicit checked reset. |

### Failure walkthroughs

| Trigger | Immediate response | Retained state / recovery |
| --- | --- | --- |
| Auxiliary electricity lost during evacuation or melting | Stop powered gas transfer and heat admission; close heat shutter; cooling path fails open | Retain gas, melt fraction and energy. Passive heat-pipe transfer and radiation remain bounded by sink temperature/headroom. Recheck and explicitly resume. |
| Receiver already partly full or hot | Refuse evacuation before exceeding its 200 kPa operating limit | No gas disappears. Cool or perform measured return/equalization; recalculate capacity before the next attempt. |
| Process probe fails during hold | Mark Unknown, isolate heat, reset qualifying hold | Physical enthalpy and gas still evolve. Restore a valid probe, then explicitly resume and qualify the full hold again. |
| Cooling path lost or sink reaches its limit | Reduce grants to available rejection/headroom, then isolate if necessary | Store heat without free vacuum cooling. Retain captive casting; repair the actual path before resuming. |
| Output becomes full before release | Leave casting and remainder in the cassette | Saved staged delivery commits each item once when physical space is available; do not start another batch around retained output. |
| Reload while hot | Restore physical state; invalidate grants and pause heating/feed | Passive cooling follows the policy below. Require fresh probes, identities and explicit Resume; do not repeat product creation or pump transfers. |
| Two consumers request heat while flight starts | One shared budget bounds combined receipts; flight revokes new industrial grants first | Already captured heat stays with each batch. Interval identity prevents duplicate use; the actual native allocation proof remains outstanding. |

Persist batch/recipe revision, admitted masses, output commitments, phase,
enthalpies, gas vectors/thermal state, wear and full source/sink IDs in Framework's
protected object store. Heating/feed permission returns paused; fresh native
energy receipts never survive reload. Unknown/corrupt records stay protected.

While simulation is paused, physics and hold time do not advance. Loaded hot
machines continue passive cooling even with processing paused. For unloaded
same-session ships, use bounded analytical/passive integration over **simulation
time only**, with the last known valid installed passive path; apply no source,
pump or cross-object transfer while endpoints are unobservable. Preserve unmet
cooling as stored heat and require fresh checks on return. No closed-game
wall-clock catch-up. If sink existence/geometry cannot be established across
unloading, suspend that transfer conservatively and retain a catch-up diagnostic;
do not assume the radiator survived. This is a deliberate scope limit requiring
owner checks, not a promise of continuous whole-universe thermal simulation.

## Interfaces and verification boundary

Framework reuses ObjectStateStore, immutable recipe/job binding, full-ID pairing,
control authority, observations and construction. Add only concrete shared energy
receipt/ledger and bounded thermal helpers when implementing this consumer.
Solid routing is not already an energy/gas pipe. Shipbreaker owns the native
reactor adapter, hardware, thermal parameters, recipes and consequences. Local,
C1 and F3 dispatch the same checked furnace commands and read the same observations.
There is no furnace runtime API or saved schema published by this research.

**Executed offline:** phase/enthalpy inversion and latent plateau; energy closure
each integration step; partial supply; source interruption/resume; warm initial
energy; finite sink saturation; timestep comparison; no-radiator failure to
complete within six simulation hours; gas conservation arithmetic; actual prefab
hierarchy/sprite inspection. Failure walkthroughs above are design reasoning, not
executed gameplay tests.
No build or gameplay pass is implied.

**Required at implementation:** real native debit under multiple consumers,
duplicate/reordered receipts, failed fuel interval, torch preemption; physical
mass through blocked/reloaded staged output; gas receiver full/hot/reused;
per-phase save/reload; unknown provider/state; source/sink removal and cross-ship
changes; power loss and protective cooling; faulted instrument without erased
heat; pause/fast-forward/unloaded catch-up. UI-specific checks are in the companion brief.

The installed load-order refresh contains core, 29 configured-enabled Workshop
entries, three enabled local Phobos packages and two disabled entries. Configuration
is not proof of current runtime loading. Existing OCF/SWB, Ship's Water and Testudo
remain optional reuse precedents; no new provider is required or copied here.

Sources: current recipe catalog, EquipmentEconomy, installed native material and
power definitions; locally inspected FusionIC, Powered, GasContainer, GasPump,
Ship and existing Phobos services. The game/code/assets belong to Blue Bottle
Games. Native evidence remains in ignored local research; distributable work is
original prose, metadata inspection tooling, authored calculations and geometry
mockups. External physical references establish principles, not game integration.
