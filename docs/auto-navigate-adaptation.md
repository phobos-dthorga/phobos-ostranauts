# Phobos Auto Nav: standalone adaptation

**Current 0.12.0:** use the [shared tall Polaris flight hub](auto-nav-instruments.md).
Navigation, Pursuit and Systems expose routine controls without scrolling;
Details contains diagnostics/help. Native Edit places the new layout identity
without enlarging old compact placements. Explicit Approach & Dock captures
assigned ports, stages 1 km beyond protected hull clearance and revalidates the
RCS-only handoff. Both docking phases suspend after loading. The preserved
0.11.1 compact artwork remains provenance. This redesign is prepared, not installed
or published; see [offline validation and owner checks](auto-nav-hub-validation.md).
Earlier version-specific layout descriptions below are historical.

**Current source candidate; built against Ostranauts 1.0.1.5 / BepInEx 5.4.23.5.**
Ordinary saves are the baseline from 2026-09-24. Phobos Framework 0.17.0+ now
provides shared construction, merchant and maintenance services. No original Auto
Navigate dependency. See [prices, acquisition and service bills](equipment-economy.md).
In-game validation of this update remains pending. See the
[current player guide](player-guide.md) for the suite's installation and operating sequence.

The equipment is now named **Phobos' Asterel N1 Polaris Auto Nav Module** to identify its
navigation-station use. The package remains Phobos Auto Nav; saved item IDs are
unchanged. Buying, selling, salvage and service details are in the
[Auto Nav economy guide](auto-nav-economy.md). Pickup artwork is retained; the
owner-authorised [instrument-panel redesign](auto-nav-instruments.md) adds live
rotary controls and separates flight state from detailed explanations.

The owner requested a standalone adaptation instead of a Workshop dependency,
with original-author credit, and clarified that public releases are the intended
destination. The repository is public; binary/Workshop publication remains held for upstream provenance review. The
[third-party notice](../THIRD_PARTY_NOTICES.md) records attribution, exact binary
provenance, the adapted files, a note for the original author and unverified terms.
That note has not been sent. No blanket community reuse grant is claimed.

## Implemented slice

- Current predictive guidance and the new N2 instrument implement the approved
  [Polaris pursuit and fire-control plan](auto-nav-pursuit.md): Rendezvous, Follow,
  explicit offensive selection/Engage/Cease Fire and a separate moving-target
  docking hold. The linked guide supersedes older guidance/pursuit limitations
  below; prior version descriptions remain historical context.

- Version 0.10.0 applies the console throttle to translation plus turning, checks
  braking room before ordinary Fly/Resume, saves console-specific numeric defaults
  with speed controls in Details, and adds rare native module salvage. See
  [flight profiles, safety and salvage](auto-nav-flight-profiles.md).
- Version 0.9.0 requires [live native sensor contact](auto-nav-sensors.md) for
  Fly, Resume and Dock. Losing contact suspends and clears owned thrust while
  preserving intent; reacquisition requires explicit Resume. Unavailable range
  and speed remain unknown. Tracking works with the panel closed and never
  enables emitting sensors. Existing Framework storage handles saved suspension.
- Version 0.8.0 adds deliberate [RCS docking](auto-nav-docking.md) within 10 km
  of the hull after native Comms clearance. Navigation has a Dock button; F3 has
  `phobosnav dock`. Reload suspends docking for explicit Resume.

- Version 0.7.0 provides propulsion and stopping-distance dials, phase, destination,
  range and total relative-speed readouts, Fly/Resume, Stop/Coast and scrollable
  Details. It retains the corrected native panel footprint and saved-flight rules.
- Version 0.6.0 prefers a running torch for acceleration and approach braking
  where native zones permit it, with RCS for turning, small corrections and
  restricted zones. A conservative RCS braking envelope remains available.
  See [torch operation, settings and engine evidence](auto-nav-torch.md).
- Standalone `PhobosAutoNav.dll`, separate `PhobosNavModAutoNav` native module and
  damaged variant. **No AutoNavigate.dll reference or runtime dependency.** Uses
  BepInEx 5, Phobos Framework and private Phobos board definitions, with the owner's approved original
  Phobos faceplate and intact/damaged item artwork.
- Reuses Gravy/mrkmg's guidance and target prediction rather than building another
  approach controller. No inherited vendor/loot injection or artwork is included.
- The panel's **Fly** and **Disengage** buttons and F3 commands share one service.
  Current scope is selected ships/stations in free space. Planetary travel,
  collision avoidance and continuous working-position control are not
  offered. Current-world target resolution is now gated by native combined
  sensing and celestial occlusion. This is contact qualification, not a precision
  survey or collision-clearance measurement.
- Arrival no longer succeeds solely because the distance boundary was crossed:
  within the arrival band, excessive relative speed commands braking first. The
  new calculation respects an aggregate RCS throttle cap and cannot deliberately
  reverse relative velocity in one step. This cannot undo an approach that was
  already too fast to avoid a collision; it is not obstacle avoidance.
- The ship keeps its original flight target and configured cruise/arrival values
  until stopped. Closing the console does not intentionally cancel the flight.
  Missing/zero throttle stops the controller, rather than applying upstream's
  minimum 1% or fallback 25% throttle. Console removal, damage or power loss,
  invalid data and conflicting native navigation stop it too. World changes clear
  runtime references; 0.5.0 restores validated saved flights after loading.
- Nonzero external maneuver commands release control before the incoming command
  executes. Original Auto Navigate being loaded blocks engagement; an active
  Approach Assist pulse or Auto Dock also blocks it. This is not a universal
  third-party autopilot arbitration system.
- Version 0.5.0 preserves flight intent through native saves, using Framework
  0.11.0 versioned object storage. Active flights resume after validation by default;
  blocked flights remain suspended for explicit Resume. See
  [saved flights](auto-nav-persistence.md). **Stop clears thrust and coasts, not brakes.**
- Ordinary saves can fly and use the explicit debug spawn command. There is no
  save-name restriction. Merchants and table assembly are the normal acquisition
  paths; hardware, power, fuel and conflicting-control checks still apply.

## Navigation Edit mode fix (0.4.1)

The owner reported that the module appeared but could not be dragged from the
Edit menu, with a native "can't find mod" message. The compiled 0.4.0 panel had
attached the game's global `Draggable` (world-object hauling), rather than
`Ostranauts.ShipGUIs.NavStation.Draggable` (navigation-panel placement). Version
0.4.1 explicitly uses the latter and binds `NavModBase.DraggableRef` for both
intact and damaged panels. Existing module IDs, artwork and saved layout data
are unchanged; native placement and overlap rules still apply.

The owner confirmed that leaving Edit mode restored normal interaction. Native
Edit mode locks the simulation paused until closed; this was not evidence of a
hung game process. No pause-lock override is added.

A compiled-component regression check rejects the installed 0.4.0 assembly and
passes the corrected 0.4.1 build. It runs in the normal package build. In-game
verification remains pending: drag Polaris to available space in Edit mode,
leave Edit, then reopen the console and confirm its placement persists and
normal interaction works.

## Panel sizing correction (0.4.2)

The follow-up screenshot exposed oversized placement bounds around the fitted
faceplate. The [vanilla layout audit](auto-nav-panel-layout-audit.md) records the
prefab measurements and native placement rules. Version 0.4.2 uses a normal 20%
board-height row, calculates the container width for the approved 2:1 artwork
and removes the child aspect fitter. Saved/default sizes are normalized before
native fit checks, retaining the panel's top-left position. Artwork and item IDs
are unchanged. Native overlap rules still require a clear space on the board.
Build and geometry checks cover this correction; owner placement/reopen testing
remains pending.

Owner screenshots of 0.4.3 confirmed the height but showed that the 2:1 constraint
left the panel too narrow. Version 0.5.0 keeps the 20% height and uses the full
native 25% column width. Sliced rendering of the unchanged faceplate preserves
the corner/screw shapes as the centre widens. The updated geometry checks pass;
this width correction still needs an in-game look after installation.

## Short-range approach goal (2026-09-24)

The owner identifies **below 5,000 km** as the gap left by vanilla autopilot and
sets that as Auto Nav's immediate goal. This threshold is owner-reported; this
round did not independently verify the vanilla gate. Our controller already had
no minimum engagement range. Its old **5 km arrival setting** was a stopping
distance, not a 5,000 km starting requirement. No new maximum engagement range is
imposed, and native long-range navigation is unchanged.

Version 0.4.0 makes closer approaches configurable: the owner chose **1 km** for
new configurations, adjustable from **0.1 to 100 km**. These are centre-to-centre
distances. Effective arrival distance is the greater of the requested setting and
**1.5 times the native hull-contact distance**. Unknown/invalid hull geometry
blocks engagement rather than silently using the smallest clearance. Arrival
retains the existing 5% approach band and speed tolerance, so the setting is not
an exact positioning guarantee.

Panel status shows current range and effective arrival distance. F3 status also
shows the requested distance and relative speed. Reading these values does not
advance target physics. Starting within the arrival band brakes excess relative
speed or completes if already slow enough; it does not move the ship outward to
the requested distance. This is approach assistance, not a docking or position-hold
controller. Version 0.10.0 now checks braking room before ordinary Fly/Resume;
this is not obstacle avoidance or a collision guarantee.

Existing configuration values, including the old 5 km default, remain intact.
With no active or suspended flight, use `phobosnav arrival 1` once to save that
console's preferred default. Use `phobosnav fly 0.5` for a single flight requesting 500 m without
changing the saved default. Both commands validate distance before changing
anything; an active flight retains its captured settings until stopped. Module
identities and saved items are unchanged.

## Fuel-conscious coasting (0.4.3)

The owner reports that autopilot guidance works well in ordinary play, but burns
too much RCS fuel chasing unnecessary precision. That feedback concerns the
installed 0.4.1 flight behaviour; the 0.4.2 UI changes had not yet been tested.
It does not establish compatibility of every flight mode or validate this update.

The old cruise hysteresis resumed corrections above 3 m/s error and continued
until below 0.6 m/s. Its burns targeted zero error, and even coasting continued
to chase heading. Version 0.4.3 introduces a separate Phobos cruise policy:

- Resume correction above the greater of `CoastToleranceMS` and
  `CoastSpeedTolerancePercent` of requested cruise speed. Begin coasting at
  `CoastEnterFraction` of that threshold. Defaults at 100 m/s cruise are
  **10 m/s resume / 7.5 m/s stop correcting**. Small course errors between those
  limits retain the previous state; corrective burns aim for the inner band,
  not perfect agreement with a continually changing target velocity.
- Bound sideways drift separately: no more than one quarter of effective
  arrival radius projected over 30 seconds. At a 1 km arrival radius this limits
  cross-track velocity to about 8.33 m/s; at 100 m it is about 0.83 m/s. The inner
  hysteresis fraction also applies to this limit. This is a short-horizon guard,
  not a complete trajectory or obstacle simulation.
- While coasting, stop chasing target heading. Damp significant existing spin
  through native RCS commands; do not magically reset angular velocity. During
  powered course corrections, use a configurable 2-degree heading tolerance
  instead of the inherited approximately 0.1-degree tolerance.
- Braking overrides both cruise bands. Cap approach speed using actual remaining
  range to the arrival band, the next simulation step and conservative available
  braking acceleration (85% reserve and the arrival brake's two-axis aggregate
  throttle allowance). This can start slowing earlier than the inherited lead-point
  estimate. The final arrival-speed tolerance, hull clearance, manual handover and
  existing arrival-brake calculation are unchanged. An already unsafe approach
  is not made collision-proof by this estimate.

Useful `[Flight]` settings, captured when a flight begins:

| Key | Default | Allowed range | Meaning |
| --- | ---: | ---: | --- |
| `CoastToleranceMS` | 3 | 0.1–20 | Existing absolute floor for the cruise error band |
| `CoastSpeedTolerancePercent` | 10 | 0–25 | Cruise-relative error allowance; zero uses the absolute floor |
| `CoastEnterFraction` | 0.75 | 0.2–0.9 | Fraction of the resume band at which corrections stop |
| `BurnHeadingToleranceDegrees` | 2 | 0.1–10 | Heading tolerance during powered correction |

Existing settings are retained. New keys receive defaults on launch, so the
existing 3 m/s setting does not prevent the new relative allowance from helping.
The installer does not edit the configuration. `phobosnav settings` labels its
coasting settings as active-flight or next-flight values. Verbose steering logs
include velocity error, resume/cross-track limits and braking state. Drift horizon,
drift fraction and braking reserve are named policy constants rather than extra
player settings. Looser cruising can take longer and follows a less exact path.

Offline checks cover hysteresis, partial correction, cross-track limits, braking
priority, spin damping and invalid inputs. A bounded varying-cruise scenario
reduces corrective delta-v relative to the old policy; this is not a measurement
of game fuel. Straight-line numerical approaches across distances, acceleration
levels and time steps still reach the existing arrival-speed tolerance. In-game
checks remain: compare RCS use and coast/correction frequency on similar routes,
then confirm braking and arrival in a supervised short-range approach. No fixed
fuel-saving percentage is claimed.

## Settings and commands

The first launch creates `BepInEx/config/phobosgekko.ostranauts.autonav.cfg`.
Edit it with the game closed. Defaults: cruise **100 m/s**, arrival **0 m/s**,
arrival distance **1 km** for new configurations, tolerance **0.5 m/s**, timeout **48 simulation hours**,
maximum simulation step **10 seconds**. Range/speed are relative to the target;
arrival distance is centre-to-centre and also floored by the inherited hull check.
Larger simulation steps abort and return to normal speed; they are not subdivided.
After an abort the ship coasts, so this limit is not a collision guarantee.

From 0.10.0, configuration supplies initial numeric preferences only. Each console
then saves its own cruise/arrival-speed/distance through Details/F3 or its first
successful Fly; display reads do not create records. See
[flight profiles and salvage settings](auto-nav-flight-profiles.md).

Other settings control coast corrections, bounded rotation, the inherited
approximate departure fuel check, detailed logging and the master switch.
Rotation's instantaneous comparison mode is inherited and nonphysical; leave
`UseThrusterRotation=true` for ordinary testing. The fuel estimate is not proof
that turning, damaged thrusters and changing trajectories fit its reserve.
RCS fuel use goes through native `Ship.Maneuver`; torch fuel, heat and wear
remain native `FusionIC` behaviour. The `[Torch]` settings default to a 1 g
ceiling and 5 m/s minimum useful correction. Native no-wake rules remain active.

F3 commands:

| Command | Purpose |
| --- | --- |
| `phobosnav help` | Command list and ordinary-save usage |
| `phobosnav status` | Version, engagement, economy registration and last result |
| `phobosnav settings` | Effective flight defaults and config filename |
| `phobosnav spawn` | Add one module to an open compatible console (debug grant) |
| `phobosnav fly` | Engage using the saved default through the same checks as the panel |
| `phobosnav fly 0.5` | Request a 500 m arrival for this flight; larger hull clearance still applies |
| `phobosnav arrival 1` | Save this console's 1 km default with no active/suspended flight |
| `phobosnav cruise 100` | Save this console's cruise speed in m/s |
| `phobosnav arrivalspeed 0` | Save arrival relative speed in m/s; zero matches target motion |
| `phobosnav defaults` | Explicitly clear only this console's numeric preferences |
| `phobosnav resume` | Recheck and resume the saved destination/profile |
| `phobosnav forget` | Explicitly discard a retained saved-flight record |
| `phobosnav torch on` / `phobosnav torch off` | Save torch preference; off cuts the current torch burn while RCS guidance continues |
| `phobosnav stop` | Clear commanded thrust and coast |

## Approved artwork

Version 0.1.1 integrates the selected neutral slate-grey faceplate, intact cassette
and cracked/scorched damaged cassette. The source faceplate remains the original
2:1 PNG; 0.5.0 slices it into a native column with undistorted corners and screws.
Title, status, Fly/Resume and Disengage are live controls. Native panel
placement still uses the game's Edit mode. Item images use the native 16-pixel
size, with 256-pixel inspection portraits. The exact approved masters, prompts
and export decisions are recorded in [artwork provenance](../assets/phobos-autonav/README.md).
No earlier weathered faceplate draft or game image file is packaged.

## Build and package

```powershell
./scripts/build-autonav.ps1 -OstranautsPath '<local game installation>'
```

Output: `dist/PhobosAutoNav-P0.zip`, containing only the standalone plugin,
native definitions, approved artwork, guide and provenance notices. The build
regenerates the small item exports from the approved masters. No Workshop subscription is needed for
Auto Navigate. No installation script runs as part of this build.

For a later owner-run test, close the game and use
`./scripts/install-mods.ps1 -Mods AutoNav` from this repository. The shared
[installer guide](installing-mods.md) also covers the double-click launcher,
updates and previews. Keep original Auto Navigate disabled and confirm it does
not load. Installation is separate from the owner-run gameplay test.

For a standalone ZIP without this repository's installer, install the separate
PhobosFramework-P0 package first (plugin and native metadata), then copy the package's
`BepInEx/plugins/PhobosAutoNav` directory into the game's corresponding plugins
directory and `Mods/PhobosAutoNav` into `Ostranauts_Data/Mods`. Enable that native
data package through the normal game mod controls. Keep original Auto Navigate
disabled and confirm it does not load.

In an ordinary save, buy or assemble a module and fit it to a powered nav console.
The explicit `phobosnav spawn` command remains available for debugging. Reopen the
console and place the module using Edit. Select another ship/station
with ample clearance and set a nonzero throttle before using Fly.

## Verification and remaining checks

Build passed without warnings. Offline numerical checks exercised braking from
100 m/s to 0 or 20 m/s across five headings and five time steps: bounded aggregate
throttle, decreasing speed, no reversal and invalid-data rejection. This verifies our new calculation, not the inherited guidance in-game.
Package checks parse native JSON, verify artwork paths, dimensions, transparency
and the approved faceplate hash, and ensure Auto Navigate is neither referenced
nor bundled. Established module/UI and native RCS patterns are reused without
requiring separate proof tests for basic behaviour.

The useful next owner checks concern changed behaviour: arrival inside the ring
while still moving fast; closing/reopening the panel; manual handover; removing
or depowering the controlling equipment during flight; save/load restoration and blocked-resume behaviour; and
normal versus accelerated time. During that same test, check panel labels/button
alignment and intact/damaged item appearance at the owner's usual UI scale;
no separate basic module proof test is needed. Record versions and outcomes. Do not call the
prototype suitable for unattended travel or external cutting before these checks.

The 0.4.0 numerical checks additionally cover approach planning at sub-kilometre,
1–100 km, 1,000 km and either side of 5,000 km, large-hull overrides, invalid
geometry, close arrival braking and locale-independent distance parsing. They
exercise the policy used by engagement and runtime clearance, not the native
flight simulation. Owner checks: select a clear target within 5,000 km, save
`phobosnav arrival 1`, approach from outside that stopping distance, then try
`phobosnav fly 0.5` with adequate clearance. Compare requested/effective arrival
in `status`; verify that the one-flight override leaves the default unchanged.
Near-target or high-relative-speed behaviour still needs supervised game testing.

Longer-term work should address sensor-qualified target tracking, stopping-distance
admission, obstacles and a deliberate handover into a separately designed work
position controller. None is implied by a successful arrival.
