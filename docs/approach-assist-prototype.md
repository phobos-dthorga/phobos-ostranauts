# Approach Assist P0: integration prototype

Naming update 0.1.2: the equipment is **Phobos' Asterel N0 Approach Assist
(Prototype)**. Package/command identifiers and prototype behaviour are unchanged.


Version **0.1.1**, prepared 2026-09-20 against Ostranauts **1.0.1.4** and the
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
- F3 console commands for help, diagnostics, normal/damaged item creation, the
  existing test pulse and disengagement. F8 remains an alternative for spawning.

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

## Repeatable installation and verification

Use **PowerShell 7 or later** and the existing build script above, then run:

```powershell
./scripts/install-approach-assist.ps1 -OstranautsPath '<your Ostranauts folder>' -LoadOrderPath '<configured Mods folder>/loading_order.json'
```

This command now delegates to the [shared multi-mod installer](installing-mods.md).
Alternatively use `./scripts/install-mods.ps1 -Mods ApproachAssist`; the default
multi-mod selection is AutoNav and Shipbreaker, not this older pulse prototype.

Supply the load-order file configured in the game's mod manager, rather than
assuming its Mods folder is next to the executable. An alternate unpacked package
can be supplied with `-PackagePath`; by default the installer uses the build
script's `dist/PhobosApproachAssist-P0` output.

- Add `-WhatIf` to preview an installation without writing files.
- Add `-VerifyOnly` to compare installed files with the package and check its
  enabled load-order entry, without changing anything. Verification can run while
  the game is open; installation requires it to be closed.
- Repeating installation skips files that already match and does not add another
  load-order entry. Existing entries keep their order; a disabled Approach Assist
  entry is enabled in place. A new entry is appended after existing mods.
- Changed files and the prior load order are backed up under
  `.local/installations/`, with a receipt. Unexpected extra files in either mod
  folder, duplicate registrations and placement before core require inspection
  instead of being silently deleted or rearranged.

The installer checks matching plugin/native versions, required JSON files and
SHA-256 file hashes. It installs only the packaged plugin and native definitions;
it never launches the game, handles saves or runs gameplay tests. If installation
fails after copying begins, it reports the backup location: keep the game closed,
inspect the error, and restore the affected files from that backup or correct the
problem and rerun. It does not promise automatic rollback.

Installer checks use synthetic installation folders and do not operate on the
real game. After building a package, run them with:

```powershell
./tests/install-approach-assist.tests.ps1
```

## Test installation and use

1. Finish the current play session and exit normally before installing the plugin.
2. Copy the packaged plugin folder into the game's `BepInEx/plugins/` directory.
3. Copy the packaged native mod into the **configured** Mods folder. This location
   is shown by the game's mod manager; it is not necessarily under the executable.
   Enable `PhobosApproachAssist` after core, preserving the existing load order.
   Both the native package and the plugin are required.
4. Restart the game, create a separate test world, and save it under the required
   test name. Confirm the plugin's startup message in the BepInEx log.
5. Open an installed, powered nav console on the player's ship, press **F3** to
   open the game's debug console, and enter **`phobosapproach spawn`**. Close the
   debug console with F3. Alternatively, press **F8** and choose **Add test module
   to this console**. Ordinary salvage, shops and crafting are not changed.
6. Close and reopen the console, choose its native **Edit** control, and drag
   **Phobos' Asterel N0 Approach Assist (Prototype)** onto free pegboard space. Its initial position is only
   a suggestion; occupied space must not displace existing modules.
7. In open space, select a firm nearby contact and reduce relative speed. The
   panel explains unmet requirements. Choose **Run 2-second test**, then observe
   the fuel and velocity change. The pulse ends with the ship still moving.
8. Repeat the interruption checks below before trying the controller more broadly.

Remove the test module before unloading its native data package from a test
save. No removal migration for persisted mod objects is implemented yet.

## Debug command reference

Use the game's **F3** console (the input action is named **Toggle Debug Console**).
These commands do not require `unlockdebug`. Enter one command and press Enter.
Commands and their arguments are case-insensitive; extra arguments are rejected.
The prefix is `phobosapproach`, with no leading slash.

| Command | Result |
| --- | --- |
| `phobosapproach` or `phobosapproach help` | Show the command list; usable without a loaded world. |
| `phobosapproach status` | Report plugin version, test-save gate, loaded native definitions, controller state, console power/damage, module counts, RCS fuel, sensor availability and the first pulse blocker. Read-only; does not arm the controller or enable sensors. Open the ship's nav console for hardware/contact checks. |
| `phobosapproach spawn` | Add one `PhobosNavModApproachAssist` to the open ship nav console. |
| `phobosapproach spawn damaged` | Add one `PhobosNavModApproachAssistDmg` for damaged-panel and repair checks. |
| `phobosapproach pulse` | Run the same capped two-second pulse as the panel, toward the selected firm contact. All existing hardware, contact, distance, speed and fuel checks apply. Refuses to restart an already active pulse. |
| `phobosapproach stop` | Disengage Approach Assist immediately, including with the nav panel closed. Does **not** brake the ship or cancel unrelated flight systems. |

Spawning requires a loaded, named test save and an installed nav console on the
player's current ship. PDA navigation is not a destination. Each command adds
one item, and refuses another copy of the same variant while it is in that
console. Normal and damaged variants can coexist for inspection. To test the
damaged-only refusal, move the normal module out using the game's inventory UI.
Locks, capacity and item compatibility remain enforced. Failed placement removes
only the newly created item; there is no floor-spawn fallback or bulk spawn.
Close/reopen the nav console and use **Edit** after adding either variant.

All command replies also go to `BepInEx/LogOutput.log`. When reporting a problem,
include the `phobosapproach status` reply and the relevant log entries. The status
command reports one snapshot, and its pulse check stops at the first unmet
requirement; fix that requirement and run it again. It reports no hidden-contact
positions or velocities.

If the game says it cannot recognise `phobosapproach`, confirm the **0.1.1** plugin
was copied and the game restarted, then check the startup log for patch errors.
If the command works but says native definitions are missing, enable the native
`PhobosApproachAssist` package too. A full or locked console must be cleared or
unlocked through normal gameplay before retrying.

The integration intercepts only `phobosapproach` in `ConsoleResolver.ResolveString`;
other console commands continue unchanged. Inspection of the installed 1.0.1.4
assembly established that the player's F3 console uses this resolver, rather
than the separate `DevConsole.commands` dictionary. Version 0.1.1 also corrects
the original F8 spawner's native inventory call: insertion must proceed beyond
stacking into the console container. These are code findings, pending runtime
confirmation in the owner's test world.

## Verification record

| Check | Result |
| --- | --- |
| Compile against installed game, Unity and BepInEx assemblies | Passed with zero warnings/errors |
| Controller and command checks | 113 passed: the original 86 controller/test-save checks plus 27 command cases covering routing, case/whitespace, malformed arguments and non-interference with native command names |
| Native content/reference checks | All JSON parsed; 16 installed-game resource references resolved; normal/damaged GUI mappings agree; package contains no game binaries or images |
| Local installation | 0.1.1 plugin and native package installed; four file hashes match; existing mod order preserved. Native mod was enabled at installation and observed disabled during a later owner session; that setting was left unchanged. |
| Installer checks | 23 passed using synthetic installations: preview, verification, repeat runs, updates/backups, load-order preservation, disabled/absolute/duplicate entries, and running-game refusal |
| Plugin startup in the game | Pending |
| F3 help/status/spawn/pulse/stop, rejected arguments and unchanged native commands | Pending |
| Physical item creation, pegboard placement and damaged state | Pending |
| Actual fuel use, motion and manual takeover | Pending |
| Power interruption, module removal, lost contact and competing automation | Pending |
| UI closure, save/reload and fast-forward | Pending |

The game was inspected read-only and had the owner's existing session open at
its save screen. That session was not used for experiments. A successful build
does not establish runtime compatibility or complete the first playable milestone.
The owner chose to perform in-game testing personally. After the owner's later
installation approval, version 0.1.1 was installed and enabled with the game
closed. The reusable install script's initial verification matched; a later
read-only check found the native entry disabled while the game was running. No
changes were made to that session. Startup and gameplay results remain unverified
until the owner reports them.

For each in-game check, record the exact game/plugin versions, test world,
steps and observed result. Measure fuel before/after and native relative velocity;
do not infer that fuel was consumed merely because a log says a pulse was issued.
Test manual input both early and on the pulse's last update. Test paused
engagement, UI closure and a reload during an armed pulse. Compare ordinary speed
and fast-forward, and confirm that a timestep beyond the prototype limit ends
the pulse without continuing thrust.

For commands, check help/status before loading a world, then spawn each variant
in the test world. Confirm duplicate and full/locked-console attempts do not add
items. Check that `phobosapproach spawn damaged extra` refuses without creating
anything and native `help` still works. Test `pulse` with no selected contact,
with damaged-only hardware, with a valid setup and while already armed. Test
`stop` both during a pulse and after closing the nav panel. Record observed item
counts, output messages and motion, rather than treating the parser checks as
proof that spawning or Harmony patches work in the game.

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
