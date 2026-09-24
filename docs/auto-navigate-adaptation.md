# Phobos Auto Nav: standalone adaptation

**0.3.0 candidate; built against Ostranauts 1.0.1.4 / BepInEx 5.4.23.5.**
Ordinary saves are the baseline from 2026-09-24. Phobos Framework 0.7.0+ now
provides shared construction, merchant and maintenance services. No original Auto
Navigate dependency. See [prices, acquisition and service bills](equipment-economy.md).
In-game validation of this update remains pending. See the
[current player guide](player-guide.md) for the suite's installation and operating sequence.

The owner requested a standalone adaptation instead of a Workshop dependency,
with original-author credit, and clarified that public releases are the intended
destination. The repository remains private until explicitly changed. The
[third-party notice](../THIRD_PARTY_NOTICES.md) records attribution, exact binary
provenance, the adapted files, a note for the original author and unverified terms.
That note has not been sent. No blanket community reuse grant is claimed.

## Implemented slice

- Standalone `PhobosAutoNav.dll`, separate `PhobosNavModAutoNav` native module and
  damaged variant. **No AutoNavigate.dll reference or runtime dependency.** Uses
  BepInEx 5, Phobos Framework and private Phobos board definitions, with the owner's approved original
  Phobos faceplate and intact/damaged item artwork.
- Reuses Gravy/mrkmg's guidance and target prediction rather than building another
  approach controller. No inherited vendor/loot injection or artwork is included.
- The panel's **Fly** and **Disengage** buttons and F3 commands share one service.
  Current scope is selected ships/stations in free space. Planetary travel,
  docking, collision avoidance and continuous working-position control are not
  offered. Inherited target resolution still tracks engine objects rather than
  enforcing Approach Assist's full live sensor-contact policy; that is a known
  integration difference, not a claim of sensor realism.
- Arrival no longer succeeds solely because the distance boundary was crossed:
  within the arrival band, excessive relative speed commands braking first. The
  new calculation respects an aggregate RCS throttle cap and cannot deliberately
  reverse relative velocity in one step. This cannot undo an approach that was
  already too fast to avoid a collision; it is not obstacle avoidance.
- The ship keeps its original flight target and configured cruise/arrival values
  until stopped. Closing the console does not intentionally cancel the flight.
  Missing/zero throttle stops the controller, rather than applying upstream's
  minimum 1% or fallback 25% throttle. Console removal, damage or power loss,
  invalid data, world changes and conflicting native navigation stop it too.
- Nonzero external maneuver commands release control before the incoming command
  executes. Original Auto Navigate being loaded blocks engagement; an active
  Approach Assist pulse or Auto Dock also blocks it. This is not a universal
  third-party autopilot arbitration system.
- Loading/new games disarm. No automatic resumption or persistent active-flight
  state is implemented. **Stop means clear commanded thrust and coast, not brake.**
- Ordinary saves can fly and use the explicit debug spawn command. There is no
  save-name restriction. Merchants and table assembly are the normal acquisition
  paths; hardware, power, fuel and conflicting-control checks still apply.

## Settings and commands

The first launch creates `BepInEx/config/phobosgekko.ostranauts.autonav.cfg`.
Edit it with the game closed. Defaults: cruise **100 m/s**, arrival **0 m/s**,
arrival distance **5 km**, tolerance **0.5 m/s**, timeout **48 simulation hours**,
maximum simulation step **10 seconds**. Range/speed are relative to the target;
arrival distance is centre-to-centre and also floored by the inherited hull check.
Larger simulation steps abort and return to normal speed; they are not subdivided.
After an abort the ship coasts, so this limit is not a collision guarantee.

Other settings control coast corrections, bounded rotation, the inherited
approximate departure fuel check, detailed logging and the master switch.
Rotation's instantaneous comparison mode is inherited and nonphysical; leave
`UseThrusterRotation=true` for ordinary testing. The fuel estimate is not proof
that turning, damaged thrusters and changing trajectories fit its reserve.
Actual fuel use goes through native `Ship.Maneuver`.

F3 commands:

| Command | Purpose |
| --- | --- |
| `phobosnav help` | Command list and ordinary-save usage |
| `phobosnav status` | Version, engagement, economy registration and last result |
| `phobosnav settings` | Effective flight defaults and config filename |
| `phobosnav spawn` | Add one module to an open compatible console (debug grant) |
| `phobosnav fly` | Engage through the same checks as the panel |
| `phobosnav stop` | Clear commanded thrust and coast |

## Approved artwork

Version 0.1.1 integrates the selected neutral slate-grey faceplate, intact cassette
and cracked/scorched damaged cassette. The faceplate retains its original 2:1
proportions; title, status, Fly and Disengage are live controls. Native panel
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
or depowering the controlling equipment during flight; save/load disarming; and
normal versus accelerated time. During that same test, check panel labels/button
alignment and intact/damaged item appearance at the owner's usual UI scale;
no separate basic module proof test is needed. Record versions and outcomes. Do not call the
prototype suitable for unattended travel or external cutting before these checks.

Longer-term work should address sensor-qualified target tracking, stopping-distance
admission, obstacles and a deliberate handover into a separately designed work
position controller. None is implied by a successful arrival.
