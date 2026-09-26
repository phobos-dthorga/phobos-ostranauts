# Crew standing orders, training and time-skips

Prepared in Framework 0.25.0, Agriculture 0.12.0, Shipbreaker 0.25.0 and
Auto Nav 0.19.0. These are unpublished development candidates. Automated
checks do not establish in-game behaviour or UI fit.

The owner's subsequent game report exposed repeated `CrewWork.Poll` null
exceptions. Framework 0.25.1 uses Blue Bottle Games' native company roster
(`JsonCompany.GetCrewMembers`), as the native time-skip screen does; the older
`CrewSim.aCrew` field is not populated in the inspected 1.0.1.5 code. Work discovery,
controls and skip preview share the correction. Auto Nav 0.19.1 also fixes its
departure check through this service, retaining a block for unresolved or away crew.
These are code-inspection findings, with owner-observed exceptions; successful
live operation after the fix still needs owner confirmation.

Agriculture 0.12.1 fixes a separate logged `Protected dosing binding` fault and
the related idle/recovery workup save format. Absent selections use explicit
markers accepted by the saved-state wrapper, while exact input IDs and paid
energy remain intact. Restart and reload after installing. Transient session
faults are revalidated; genuinely corrupt, future or foreign saved records remain
protected. No automatic save repair or cargo replacement is performed.

## Giving work to the crew

Open **Crew standing orders and training** from the roster, an equipment
panel, the C1 console or the Auto Nav hub's details page. Orders begin
disabled. Choose the equipment's crop/process, target stock, source store
and destination store, then Enable. Turn on AutoTask and enable the native
Operate or Haul duty for the intended worker. Native repair, construction,
restoration and demolition remain native tasks; this feature does not replace them.

Crew work only during their work shift. Sleep, hunger, thirst, serious pain,
unconsciousness, emergencies, access restrictions and direct queued orders
take precedence. Agriculture, Cooking and Industry permissions initially
allow eligible crew; Exterior permission starts off. Change permissions per
person in the overview. Turning off AutoTask or a duty cancels that worker's
generated work without deleting cargo.

Standing orders publish native tasks and use native claiming, routes and
single-item pickup. The actual worker performs the checked operation. The
selected portrait does not substitute for a worker. Equipment, physical inputs
and destination space have temporary reservations; completion rechecks them.
Other providers retain their native tasks and inventories.

Stock targets count output units in the equipment and its approved destination.
Industrial processors count their recipe products, excluding unrelated cargo.
A complete recipe or crop harvest can exceed the target; recipes are never
split or their yields changed. Hauling uses only the selected stores and real
items. Missing supplies, inaccessible routes, insufficient carrying capacity
and full destinations leave the work pending.

## Supported equipment

| Equipment | Standing work |
| --- | --- |
| Firstlight-4 | Chosen potato, lettuce or lettuce-seed cohort; finite water/nutrient replenishment; harvest and replant; move produce/residue and surplus planting stock |
| Hearth-2 | Bring raw potatoes, start cooking and store portions |
| Groundwork B2 | Bring characterized residue or concentrate/makeup, prepare the selected workup, start it and store physical products/rejects |
| W2 | Finite stock replenishment, configured route operation, recorded-drainage recovery and its cartridge supply; existing route and solution settings remain authoritative |
| D4 / R4 | Supply valid loose feed, start one checked batch and clear physical products to the approved store |
| C2 collector | Enable its existing configured collection route and clear accepted cargo |
| F6 | Supply exact aluminium units, replenish an already enabled managed coolant circuit, and perform an explicitly permitted seal/run/equalize/release sequence |
| G4 | Prepare and launch/resume the existing exact reclamation mission through its recorded capture, equipment and Auto Nav bindings |
| N1 / N2 at Polaris | Launch one explicitly permitted resume of an already recorded flight to the exact bound target |

Agriculture preserves a reserve unit of source planting stock and the configured
crew-water reserve when taking loose water rations. Rack-grown planting stock is
retained for replanting. Clearing dead/unwanted living crops and draining usable
solution each require their own explicit permission. Crop growth, chemistry,
finite products, heat, power, fluids and machine interlocks use the existing
content services. Routine work does not invent a fertilizer recipe for wet rejects.

Drain is a one-shot order: after draining, its permission is consumed and the
standing order suspends, so replenishment cannot create a drain/refill loop.

Choose the water route, formulation, collector connections and coolant mode
through their existing controls first. Orders do not redesign those systems.
Repairs still need native tasks, tools and materials. Captured coolant is not
silently discarded or automatically drained.

Hot furnace batches need the hazardous-operation permission. G4 and flight
orders need both that permission and the exact recorded target. They consume
one launch permission; tracking loss, obstruction, stopping or completion cannot
start a new attempt without Resume. Existing ownership, EVA/access, pressure,
occupancy, sensors, capture and flight-authority checks remain in force. There is
no automatic target acquisition, purchase, sale, disposal or enlargement of a mission.
Changing the recorded equipment chain, flight settings or docking ports also
requires a fresh Enable/Resume; the stored permission cannot authorize a replacement.
Manufacturing is still a held scaffold and registers no operational jobs.

## Learnable specialities

Agriculture, Cooking and Industrial Processing have persistent progress visible
in the crew overview and a native character skill when qualified. Existing crew
start at zero. Novices remain eligible. A qualified available worker is preferred
over a novice at the same native duty priority; higher-priority duties still win.

The initial authored balance is 20 completed practical hours, 10 study hours, or
a proportional mixture. Qualification reduces relevant hands-on duration by 20%.
It does not shorten crop growth or machine cycles, increase output, improve
resource efficiency or award credit for idle machinery. Cancelled and failed
operations grant no training. Travel/hauling does not train a production speciality.
Existing engineering/EVA/piloting skills retain their native roles.

Powered terminals gain separate 15-minute study actions. These preserve the
terminal's existing actions, including those supplied by
[jossla's Study at Terminals](https://steamcommunity.com/sharedfiles/filedetails/?id=3788237703).
An unavailable or damaged terminal cannot grant training. These training
thresholds and the duration factor are gameplay choices, maintained with
the [constants updater](updating-constants.md).

## Time-skip

The native time-skip screen retains its collision warnings, roster display and
Go control. Its Phobos summary and detail view show intended onboard work and
current blockers. This is a read-only indication, not a guaranteed production
forecast: later shortages, heat, damage, route changes or crew needs may stop work.

During a managed skip, the native world clock advances in bounded steps, split
at actual roster boundaries and work completions. Native power consumption
and received-power callbacks advance existing machine services. Finite water,
nutrients, coolant, gas/heat headroom and output space still limit production.
Normal power update epochs are settled so the following ordinary frame cannot
charge those same seconds again.

Each worker has one budget. Travel is conservatively charged from a checked
native path at an authored two seconds per tile, with a return via the worker's
starting position for a pickup. Handling and productive interaction time are
also charged. The character is not teleported; skipped carrying is a checked
transfer of the exact physical unit after the time budget has been spent.
Ordinary play uses actual walking and carrying.

The existing native abstract rest/personal-care/context effects, risk/event rolls,
fuel, payroll and end-of-skip condition handling remain native. A native active
context from the frozen native preview or an existing direct action reserves that worker instead of allowing
simultaneous Phobos labour. Native repair allowance receives only eligible,
unspent work time. Phobos does not simulate additional meals, sleep sessions or
Common Sense errands behind that abstraction. Queued native actions retain their
native catch-up behaviour.

Native preview rest hours are reserved in full. A partial-hour roster boundary
can therefore defer work until both the preview's work allowance and the actual
roster permit it; the coordinator never borrows those native rest minutes.

Exterior missions and automatic manoeuvres suspend before every skip, including
skips with no onboard standing orders. They require explicit Resume afterwards.
Unsupported phases remain pending. Interrupted crew steps receive no completion
or training credit; physical items and completed machine progress remain. Faults
suspend affected work. A six-hour skip is not permission to bypass a batch,
cooling or input limit.

## Saving and optional compatibility

Standing orders, permissions, exact bindings, training and stop reasons are saved
in Framework's versioned object maps. Routine Agriculture and collector orders
can revalidate after load; each has a resume-after-loading option. Industrial
batches, exterior missions and crew-launched flights require Resume. A manual
Stop is sticky. Unknown or corrupt saved records are retained and blocked.
Transient task claims and reservations are rebuilt from the saved intent.

Integration with [LOGUSS's Common Sense modules](https://steamcommunity.com/sharedfiles/filedetails/?id=3789049955)
uses the game's native task system. Common Sense is optional, and no module is
replaced or required. Hauling/storage ordering, sleep and firefighting remain
owned by their existing providers. Local inspection covered Behavior 0.12.6,
SalvageStorage 0.12.14, Manifest 0.12.10, Firefighting 0.12.6 and Finances 0.12.6;
this is interface evidence, not an in-game compatibility certification.

The engine basis is the locally installed 1.0.1.5 game by
[Blue Bottle Games](https://store.steampowered.com/app/1022980/Ostranauts/):
WorkManager claiming/duty order, native pickup and path checks, and GUIFFWD's
single clock boundary and abstract effects. Game code and third-party mod
binaries are not redistributed. The training and travel balances above are
our authored simplifications, not findings endorsed by those authors.

## Verification and owner checks

Automated coverage includes default-disabled and protected orders, manual stops
and reload policies, atomic competing reservations, combined practice/study
thresholds, one/six-hour budgets, real hour boundaries, native method/field
contracts, additive terminal actions and preserved native replies. The existing
material, fluid, power, furnace, reclamation and persistence suites remain part
of the affected builds. See the repository's build output for actual run results.

On 26 September 2026, both affected build scripts completed against local
Ostranauts 1.0.1.5 / BepInEx 5.4.23.5 with no compiler warnings or errors:

| Automated suite | Result |
| --- | --- |
| Framework public assembly/recovery | 8,639 checks passed |
| Native definition/registration contracts | 9,972 checks passed |
| Agriculture | 800 checks passed |
| Shipbreaker processing/material contracts | 8,378 checks passed |
| Reclamation, saved-grid loading, shared observations | 32, 15 and 31 checks passed |
| Auto Nav | Numerical guidance, torch, docking/industrial, sensor, manual-override and fire-control suites passed |
| Repository maintenance | All 59 Python tests passed |

Counts describe assertions in offline suites, including existing regressions;
they are not counts of live crew scenarios. No Unity/game session was controlled
or used to claim gameplay validation.

The 0.25.1/0.12.1/0.19.1 corrective builds subsequently passed 8,639 Framework
checks, 17 roster checks with native boundary doubles, 824 Agriculture checks
and 9,973 native definition/API checks, plus the existing Auto Nav suites.
The roster checks reproduce an absent legacy crew list alongside valid, missing,
destroyed and partially loaded members, including stricter departure admission.
Agriculture tests exercise idle, recovery and formulation through the actual
saved-state wrapper, preserving input IDs, paid energy and protected records.
These tests confirm the corrected code paths offline, not live Unity behaviour.

Gameplay validation remains owner-run. Check these on a copy of an ordinary save:

- AutoTask off/on, disabled duties, role permissions, shift changes, sleep,
  hunger, thirst, injury, emergencies and manual takeover.
- Two workers competing for one machine or store, blocked doors/routes, full
  hands/stores, stacked supplies, interrupted pickup and cancellation.
- Every supported production chain, seed/water reserves, output targets,
  coolant service and furnace interlocks; occupied exterior work areas and
  changed targets must block.
- Partial/shared power, rising room heat, depleted fluids and material totals
  before/after one- and six-hour skips; no repeated output, energy or training
  on the next ordinary frame.
- Save/reload with active, suspended and manually stopped orders; native
  terminal study and repair tasks; Common Sense present, absent and disabled.
- Roster/equipment/time-skip controls at the owner's UI scale. Native component
  checks do not verify Unity layout, pathfinding or live patch interoperability.

Use the guarded installer and installed-file verification while the game is
closed. These candidates have not been published to Steam.
