# Approach Assist P0: integration prototype

Version **0.1.0**, prepared 2026-09-20 against Ostranauts **1.0.1.4** and the
installed BepInEx **5.4.23.5**. This is the first implementation of the
[selected mod](limited-autopilot.md), not the completed approach assistant.

## Implemented scope

- A native physical module and damaged variant, using stable `Phobos` identifiers.
- A placeholder panel that participates in the nav console's module layout.
- A contact reader using native optical, IR, EM, radar and lidar contributions,
  with body-occlusion checks and no automatic activation of emitting sensors.
- One **two-second test pulse** toward a selected firm contact, using native RCS
  and fuel handling. There is **no automatic braking, approach or docking**.
- A maximum commanded acceleration of **0.05 m/s²**, maximum commanded delta-v
  of **0.1 m/s**, and aggregate translational input capped at **10%**. These are
  conservative integration-test limits, not selected balance values.
- Refusal when docked, spinning, in atmosphere, lacking powered hardware/sensors,
  short of fuel, or using native flight automation. Test contact must be within
  100 km, have over 5 km hull clearance, and relative speed at most 5 m/s.
- Immediate release when an external nonzero thrust/rotation command arrives.
  That command continues through the native path without being overwritten by
  the prototype's cleanup.
- Disarmed operation after loading. The pulse is deliberately not saved.

The service applies its demand before the native star-system physics update and
clears its own demand afterward. The controller scales a fractional final pulse
over the actual simulation step, so large ordinary steps cannot exceed its
commanded impulse budget. Steps larger than two game seconds disarm the test.
Native damage or other forces can change observed motion; the numerical limits
describe commanded thrust, not a guarantee about the ship's total acceleration.

The prototype requires a save named exactly **`PhobosApproachAssistTest`** or
beginning **`PhobosApproachAssistTest-`**, including native autosaves of those
names. This is a development restriction. Create a separate test world; do not
rename a normal playthrough just to bypass it. Other mods that directly change
motion, instead of calling native `Maneuver`, are not covered by the takeover hook.

## Build and package

Requirements: .NET SDK 10, a local Ostranauts installation and BepInEx 5. Local
game references are not copied into the output. The current game assemblies
require .NET Standard **2.1**; an initial 2.0 build was rejected by their assembly
references and was corrected rather than suppressing that mismatch.

From the repository root, supply your own installation path:

```powershell
./scripts/build-approach-assist.ps1 -OstranautsPath '<your Ostranauts folder>'
```

The script builds the plugin, runs controller checks and creates
`dist/PhobosApproachAssist-P0/`, containing:

```text
BepInEx/plugins/PhobosApproachAssist/PhobosApproachAssist.dll
Mods/PhobosApproachAssist/mod_info.json
Mods/PhobosApproachAssist/data/...
README.md
```

The build script does not install anything or edit game configuration or saves.
`dist/`, compiled outputs and local game references remain outside Git.

## Test installation and use

1. Finish the current play session and exit normally before installing the plugin.
2. Copy the packaged plugin folder into the game's `BepInEx/plugins/` directory.
3. Copy the packaged native mod into the **configured** Mods folder. This location
   is shown by the game's mod manager; it is not necessarily under the executable.
   Enable `PhobosApproachAssist` after core, preserving the existing load order.
   Both the native package and the plugin are required.
4. Restart the game, create a separate test world, and save it under the required
   test name. Confirm the plugin's startup message in the BepInEx log.
5. Open an installed, powered nav console. Press **F8**, then choose **Add test
   module to this console**. This is the prototype's only distribution route;
   ordinary salvage, shops and crafting are not changed.
6. Close and reopen the console, choose its native **Edit** control, and drag
   **Approach Assist P0** onto free pegboard space. Its initial position is only
   a suggestion; occupied space must not displace existing modules.
7. In open space, select a firm nearby contact and reduce relative speed. The
   panel explains unmet requirements. Choose **Run 2-second test**, then observe
   the fuel and velocity change. The pulse ends with the ship still moving.
8. Repeat the interruption checks below before trying the controller more broadly.

Remove the test module before unloading its native data package from a test
save. No removal migration for persisted mod objects is implemented yet.

## Verification record

| Check | Result |
| --- | --- |
| Compile against installed game, Unity and BepInEx assemblies | Passed with zero warnings/errors |
| Controller checks | 86 passed: timestep variation/final-step impulse, pause, weak thrust, cancellation, invalid inputs, test-save names, coordinate rotation and aggregate throttle limits |
| Native content/reference checks | All JSON parsed; 16 installed-game resource references resolved; normal/damaged GUI mappings agree; package contains no game binaries or images |
| Plugin startup in the game | Pending |
| Physical item creation, pegboard placement and damaged state | Pending |
| Actual fuel use, motion and manual takeover | Pending |
| Power interruption, module removal, lost contact and competing automation | Pending |
| UI closure, save/reload and fast-forward | Pending |

The game was inspected read-only and had the owner's existing session open at
its save screen. That session was not used for experiments. A successful build
does not establish runtime compatibility or complete the first playable milestone.
The owner chose to perform in-game testing personally. The package has not been
installed into the game or added to its load order by this task.

For each in-game check, record the exact game/plugin versions, test world,
steps and observed result. Measure fuel before/after and native relative velocity;
do not infer that fuel was consumed merely because a log says a pulse was issued.
Test manual input both early and on the pulse's last update. Test paused
engagement, UI closure and a reload during an armed pulse. Compare ordinary speed
and fast-forward, and confirm that a timestep beyond the prototype limit ends
the pulse without continuing thrust.

## Graphics checkpoint and provenance

The owner asked us to **pause and discuss graphics before generating them**, using
Ostranauts' original visual style as the reference. This instruction applies when
custom artwork becomes necessary; no graphics have been generated for P0.

The temporary physical icon references the game's existing Course Plot module
images by resource name. Those files are not copied, extracted into the package
or covered by this repository's MIT licence. The panel uses plain generated UI
controls and an existing game font at runtime. These are placeholders, not final
visual design. Revisit artwork after the behaviour works, before packaging a
distinctive finished module for players.

The plugin follows the [BepInEx plugin structure](https://docs.bepinex.dev/articles/dev_guide/plugin_tutorial/2_plugin_start.html)
and [runtime patching approach](https://docs.bepinex.dev/articles/dev_guide/runtime_patching.html).
Original code and definitions in this repository are MIT licensed. No third-party
autopilot code, game source, assemblies or sprites are distributed.
