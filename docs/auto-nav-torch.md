# Auto Nav torch propulsion (0.6.0)

Current follow-up: [Polaris pursuit and fire control](auto-nav-pursuit.md) adds the
N2 instrument, shared predictive guidance and moving-target docking hold. Its
pursuit modes always suspend after reload; fire authority is never saved. Earlier
version descriptions below remain useful background where not superseded.

Prepared against Ostranauts **1.0.1.5**, BepInEx **5.4.23.5** and Phobos
Framework **0.11.0**. Native code was inspected on 24 September 2026. Compilation
and automated checks pass; the new reactor integration awaits owner gameplay
testing. This document describes a prepared package, not an installed update.

## Behaviour

New flights prefer an already-running, usable torch for worthwhile velocity
changes. This includes acceleration **and braking during approaches** to ships
outside restricted zones. There is no blanket minimum target distance for torch
use. The final small speed correction uses RCS.

Auto Nav aligns the ship's forward thrust axis with the required correction,
cuts the torch before turning, and stops burning when coasting is sufficient.
RCS provides rotation and small corrections. When braking cannot wait for torch
alignment or reactor response, RCS brakes immediately while alignment proceeds.
It does not rotate a burning torch to steer. Closing the navigation panel does
not cancel the flight.

The speed budget always retains an RCS stopping allowance, including the next
simulation step. Thus torch availability can shorten acceleration and reduce
RCS translation without making braking depend on future reactor availability.
This is deliberately conservative: the existing optional departure fuel check
still budgets the whole approach against RCS delta-v. Plenty of fusion reactant
does not compensate for failing that check. There is no new maximum cruise
speed or promise of obstacle avoidance. An already-unsafe incoming trajectory
cannot be made safe by these checks.

Native **full-station no-wake zones extend 300 km**. Auto Nav preserves the
native legality check and also checks a swept relative path through the flight
step plus a one-second reserve, with a 1 km boundary margin and acceleration
allowance. It checks all currently registered native stations, including along
the route, rather than only the selected target. Native outpost/non-full-station
classification is retained. Unknown legality means RCS fallback.

The native reactor's own one-second no-wake check remains intact. Auto Nav
rechecks when considering a burn and before accepting native reactor thrust;
permission expires without fresh guidance. Station motion and the native
order of updating global time, station positions and free ships are covered by
a conservative whole-step position allowance. This can cut the torch earlier
near a moving station or at high time acceleration. A station approach therefore
finishes on RCS. Unrestricted approaches to other ships can use the torch much
closer. Future third-party restricted-zone systems would need explicit adapters;
this integration covers the inspected native rules.

## Operation and settings

Start and maintain the reactor normally. Auto Nav does not ignite a cold
reactor, repair it, override shutdown, replenish fuel or bypass no-wake rules.
Leave manual torch thrust and other flight automation disengaged before **Fly**.
Native navigation throttle must remain above zero; it determines RCS authority
and therefore the conservative braking envelope. Torch acceleration has its own
ceiling.

The existing panel displays TORCH, alignment/startup, RCS, or the reason for
fallback. The phobosnav status command provides the same propulsion status;
phobosnav settings includes the flight's torch preference and current limits.

| Setting in [Torch] | Default | Behaviour |
| --- | --- | --- |
| PreferTorch | true | Captured for new flights; disabling also inhibits active torch use |
| MaximumAccelerationG | 1 | Live acceleration ceiling, adjustable 0.05–2 g, also bounded by the native safety limiter |
| MinimumCorrectionMS | 5 | Velocity change worth torch alignment, adjustable 0.5–100 m/s; sustained braking considers remaining speed to shed |

**phobosnav torch off** saves the preference, cuts any Auto Nav torch burn and
allows RCS guidance to continue. **phobosnav torch on** enables it for new flights
and flights whose saved profile already permits torch. Starting a new flight is
required to give an older RCS-only flight torch permission.

Coast hysteresis and requested cruise/arrival settings still apply. Torch burns
additionally require heading and spin to remain within a one-degree envelope.
Changing native reactor flight controls disengages Auto Nav.
Stop, arrival, native reactor failure, restricted zones and control handoffs
remove Auto Nav's thrust. Ordinary Stop still **coasts; it does not brake**.
The reactor continues its native electrical/thermal operation after an ordinary
cut, returning to its pre-flight idle flow/mode. A native shutdown is not undone.

## Native evidence and implementation boundary

Inspected local methods; their source and assets are not redistributed:

- FusionIC.Run reads knobRatio, slidFlow and slidCycle; Fusion applies
  Ship.GetMaxTorchThrust scaled by core temperature and calls Ship.SetThrust.
  It owns reactant removal, core temperature, power generation, wear and shutdown.
  CatchUp uses the native scheduler rather than forcing an extra fuel tick.
- NavModTorchDrive.GetLimiterSafetyMax derives the native 2 g limiter.
  Auto Nav additionally caps actual force at the configured g ceiling and
  remaining velocity correction. It inverts the nonlinear native limiter.
- NavData.GetFLOWforCYCLE provides the existing flow/cycle relationship.
  Native autopilot's 0.8–1.2 ideal-core-temperature band supplies the readiness
  precedent. Native reactor dependencies remain authoritative.
- FusionIC.CheckNWZ checks every simulation second and changes mode/cycle
  within the station radius. StarSystem.IsWithinNoWakeRangeOfAnyStation
  checks native full stations. Neither method is replaced.
- Ship.SetThrust points thrust along the forward axis. Auto Nav only caps or
  removes thrust supplied by the reactor; it never substitutes a positive
  acceleration when the reactor has supplied none. RCS fuel use remains with
  native Ship.Maneuver.

TorchDriveController owns the native adapter and transient burn permission;
Core/TorchRules contains units, swept-zone geometry, alignment and RCS stopping
budgets. UI and console actions delegate to the navigation service. These are
Auto Nav-specific mechanisms; no speculative propulsion API was added to
Framework. Framework's existing object-state service continues to store flight
intent. Upstream guidance attribution remains in the adapted file and third-party
notice; the torch rules and adapter are original Phobos additions.

## Saves and verification

The schema-1 flight record gains an optional preferTorch field. Absent means
**false**, so upgrading does not grant an existing flight new propulsion
permission. Invalid values are rejected; no saved item identifiers change.
The live acceleration/availability limits are rechecked after reload.

For the controlled ship, saved physics omits torch acceleration as well as RCS
and angular acceleration. For the exact owned reactor, the newly created item
save DTO contains zero cycle and its idle flow/mode. Live controls and physics
are not changed by serialization. Reactor running state, reactants, temperature,
motion and other ships' controls remain native. Reload restores intent and must
obtain fresh legal thrust after the usual ownership/hardware checks. There is no
saved burn lease or replayed force. See [saved flights](auto-nav-persistence.md).

Automated checks execute the production guidance and adapter with explicit
native-boundary doubles. They cover no-wake crossings whose endpoints both lie
outside, moving-zone/time-step allowances, finite-data rejection, expired
permissions, shutdown/manual changes, thrust caps, native-delivery requirements,
isolated save DTOs and legacy flight intent. Numerical approaches complete with
RCS-only, torch-preferred and restricted-zone routes at several step sizes,
including an unrestricted 3 km approach and forward/retrograde burns. They do not
measure actual reactor fuel savings or emulate Unity.

For the owner's next gameplay check, use a running reactor and start a **new**
flight. Observe a useful torch burn and coast, an approach/braking handoff, and
RCS inside a station zone; then save/reload during a burn and check that the
flight resumes only after loading. Stop and manual control should cut our burn.
Pay attention to native fuel, core stability and any unexpectedly rapid switching.
This verifies the new integration rather than re-testing ordinary RCS plumbing.
