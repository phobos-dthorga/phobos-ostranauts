# Flight hub validation — Auto Nav 0.14.1

## 0.16.1 control recovery and Rescue follow-up

Blue Bottle Games' Ostranauts 1.0.1.5 `NavModCoursePlot.UpdateUI` reconciles its
engagement switch against `AIShip.ActiveCommandName`. A retained switch is not
proof of a running Phobos flight. Native `GUIOrbitDraw.ToggleInnerPanel` displays
Rescue through the `pnlInside` CanvasGroup; runtime modules are sibling objects.
These findings come from local engine inspection, not a Unity test.

Auto Nav now reports the specific native blocker. **Disengage** explicitly
releases `FlyToAutoPilot`, `HoldStationAutoPilot` or `HoldThrustAutoPilot`, clears
saved native switches on the current ship's navigation consoles, drops the
native target/course and cuts torch cycle/RCS thrust. Reactor ignition, flow,
velocity and spin remain. Independent flight plugins and unrelated AI still
block takeover. No automatic override occurs while drawing the screen.

The hub follows native module draw order and suppresses its own surface and
input while Rescue is visible. Done restores the selected page and saved
placement; Edit continues to disable operational controls.

Owner checks for this candidate:

- With a retained native engagement switch and no Phobos mission, verify that
  Disengage is available, reports the actual blocker and enables fresh admission.
- With a manual torch cycle selected, verify that Disengage cuts thrust without
  extinguishing the reactor or removing ship velocity/spin. Re-engage only after
  checking the new contact, fuel and braking-room assessment.
- Check normal controls, native Edit, Rescue/Done on every hub tab, reopening the
  console and reopening another local station. Rescue must have no visible or
  clickable hub; saved positions must remain unchanged.
- N1 provides navigation. N2 is required for Rendezvous/Follow; N3 for weapons.
  Resume requires a suspended mission. Cease Fire has no action when neither
  flight nor fire control is active. These are deliberate disabled states.

Automated service regressions cover explicit overrides and rejection boundaries.
The appearance/input lifecycle still needs owner evaluation in Unity.


## Display correction, 26 September 2026

The owner's 0.12.1 screenshot shows blank compact labels, absent decrease signs,
truncated telemetry and a missing propulsion knob. Local `Player.log` confirms
that Noto Sans SC replaces U+2212 with a space and that the knob, guard and slider
donors fail with `Path is empty`. Framework used `Assembly.Location`, which is
empty for BepInEx's byte-loaded game assembly. The source already contained all
these controls; the browser preview had not exercised the native text renderer.

The locally inspected Blue Bottle Games font assets and TextMeshPro implementation
explain the typography mismatch: the Noto Sans SC face uses 45.88 units of line
height at 32 points (about 1.434 em), exceeding the preview's 1.12-em assumption.
TMP's vertical ellipsis can discard even the first line in short fields.
These are local implementation observations, not a claim of live validation.

Auto Nav 0.14.1 / Framework 0.21.2 retain the font size, use geometry-centered
glyphs with compact baseline spacing, and leave clipping to existing field masks.
ASCII minus avoids the missing glyph. The donor audit uses the loader's managed
directory when needed and still verifies the original pinned game hash.
Regressions exercise actual byte-loaded assemblies, tampered/missing donor files
and native-font baseline calculations. The current five-tab hub is retained.

Offline validation passed: both package builds with no warnings/errors; 8,131
Framework checks; 852,299 flight, 48,337 torch, 437,246 docking, 290 sensor/hub
and 69 fire-control assertions; 7,322 native definition/registration checks;
36 source and 36 packaged schematic layout cases; and 219 synthetic installer
checks. Item-reference coverage, constants, Workshop records and documentation
links also passed. The installer preview selects Auto Nav 0.14.1 and Framework
0.21.2 without changing load-order entries. These checks do not establish native
glyph visibility or live control interaction.

Owner acceptance after installing both packages and restarting: confirm the title,
Nav/Track/Fire/Sys/Info labels, operation/contact/restriction, footer, all three
telemetry lines, Approach & Dock and decrease buttons. Verify the propulsion knob
and Systems sliders/guards are visible and work, with no `Path is empty` warnings.
Check Fire's guard when N3 is fitted. Native interaction and font rendering remain
owner-run checks; no screenshot of a browser preview proves those results.

## N3 candidate, 25 September 2026

Prepared against Blue Bottle Games' Ostranauts 1.0.1.5. These results are offline;
no live game session, installation, save edit, commit or publication was performed
for N3. The separately prepared 0.12.1 startup correction is retained.

- Auto Nav and Framework compile without warnings/errors. Flight/persistence:
  852,299 numerical assertions; torch/native guidance: 48,337 assertions; docking:
  437,246 assertions. Existing pursuit benchmarks also run, including reversing,
  crossing, burst targets and reordered updates with shared background motion.
- N3 lifecycle/sensor/hub: 284 assertions, including all healthy/damaged N1/N2/N3
  combinations, independent firing, page/read-only behavior, console closure,
  exact module binding, locked-console reload hold, multiple consoles, RCS loss,
  stable mixed-mount reference and native maneuver exceptions. Pilot takeover with
  Auto Aim cancels both; without Auto Aim weapons-only permission survives manual
  takeover of Follow. Starting navigation ends that independent engagement.
- Actual fire controller with native boundary doubles: 69 assertions, including
  every 1–9 budget, mixed readiness, native queue filtering, defensive projectile
  and micrometeoroid paths, setting/replacement changes, contact recovery,
  ammo/mode/jam restrictions, missile lock and unreachable/singular lead.
- Installed native definitions and compiled plugin: 7,313 checks, including N3
  construction/maintenance/mass/value/spawning, ownership hook signatures, managed
  embedded hub parsing and disabled-package registration. Framework independently
  passes 2,563 checks and 35 performance-adapter checks.
- Shared layout: 36 source and 36 packaged-preview cases across Navigation, Track, Fire, Systems, active
  docking and Details, normal/expanded translations and long names at 300 × 480,
  400 × 640 and 600 × 960. Five tab faces, button targets, named faceplate wells
  and readout padding are checked. Details alone scrolls. Fire renders at minimum
  and expanded full size were also visually inspected; long names clip within
  their fields and full diagnostic names remain in Info. Native controls/font
  rendering still require owner evaluation.
- Maintenance scripts: 43 Python tests; constants, Workshop release-note records,
  document links and native package structure pass. All 219 installer transaction
  checks pass using synthetic game directories, never the owner's installation.

Reproduce with `scripts/build-autonav.ps1`, `scripts/verify-autonav-hub.cjs` (also
accepts the prepared package directory) and `tests/install-mods.tests.ps1`.
The [N3 guide](auto-nav-fire-control.md) records controls and native limitations.
No missile override is supplied: eligible-missile tests use synthetic envelope
fields; inspected stock launchers remain blocked by missing automatic requirements.

Owner-run acceptance remains: open/drag/save the real hub; exercise N3 alone and
N2+N3, native manual/defensive fire alongside held offensive groups, actual reload/
jam/ammunition/consequence behavior, console closure, power/contact loss,
coasting aiming, changing mounts, torch transitions, docking starts and reload.
No numerical result establishes real combat effectiveness or intact boarding.

## Historical 0.12.0 preparation report

**Historical preparation report.** The owner subsequently reported a Polaris
opening failure in 0.12.0 and confirmed that restoring the preceding installed
builds removed it. See the [startup diagnosis and 0.12.1 candidate](auto-nav-polaris-startup.md).
The offline results below did not validate Unity deserialization of the plugin's
embedded layout or complete native panel construction.

Prepared 25 September 2026. These are offline checks against Blue Bottle Games'
Ostranauts 1.0.1.5, not gameplay captures or proof of native interaction.
No installation, Steam publication or binary release is implied.

Completed results: both builds passed with zero warnings/errors; 852,299 flight
assertions, 48,335 torch assertions, 437,246 docking assertions, 160 sensor/hub
assertions and 31 fire-control assertions passed. Framework passed 2,470 checks
and its performance adapter passed 35. Native-definition/registration checks
passed 6,996 assertions; the installer passed 219 checks in synthetic installations.
All 24 offline layout cases passed, including the bundled preview opened directly
from its package without a web server. Constants, Workshop records, relative document
links and prepared native-data packages passed their consistency checks.

## Reproducible checks

Owner review caught a gap in the initial layout proof: text could fit inside the
panel yet cross a faceplate recess or lack a defined readout field. The corrected
layout registers six safe faceplate fields, aligns the header/tab/action content
inside them and gives telemetry/settings inset live display wells. Short header
states retain full explanations in Details. Checks now cover field containment
and inner readout boundaries, alongside manual visual review of all four pages.
The raster, its masters and hashes are unchanged by this presentation correction.

- `scripts/build-autonav.ps1`: Framework and Auto Nav builds; flight/profile/layout,
  torch, docking, sensor/persistence/hub and fire-control suites; compiled native
  panel contracts; item/faceplate identity/dimensions and prepared package.
- `scripts/verify-autonav-hub.cjs`: shared registration bounds and overlap checks,
  Navigation/Pursuit/Systems/docking at 300 × 480, 400 × 640 and 600 × 960 with normal
  and expanded labels/long names (24 cases). Critical text has no autoshrink;
  base text is 12 px at minimum size and primary buttons are at least 24 px high.
  Only Details scrolls. Browser/system-font geometry is distinct from Unity.
- `tests/install-mods.tests.ps1`: synthetic installer transactions and dependency
  gates, including Auto Nav's Framework 0.17.0 isolated-control requirement.
- Constants catalogue, Workshop generated notes, documentation links and repository
  layout checks retain their usual separate reports.

The new service tests cover all N1/N2 presence/damage combinations, console power
loss, read-only repeated snapshots, qualified/unavailable telemetry, once-only
placement notice without layout mutation, manual actuator ownership release,
native no-wake/core/limiter guards and malformed native readings. Weapon tests
distinguish measured zero readiness from stale/unavailable observations and check
Cease Fire clears permission and readiness.

Combined docking tests cover initial clearance/fit, exact module/target/port
bindings, staging, deferred handoff after physics, captured torch to RCS transition,
already-inside admission, changed/revoked clearance, occupied ports, unsafe braking,
closed native docking UI, sensor/power loss, save/load suspension and manual
cancellation while handoff is queued. Existing evasive-target hold, propulsion,
time-step and ordinary approach regressions remain in the affected suites.

Compiled inspection verifies the navigation-specific Draggable binding, native
placement normalization, shared prefab identity for both intact/damaged module
forms, silent slider refresh and no direct command/persistence calls from rendering.
It does not execute native Unity input or the native module-loader UI.

## Artwork selection

Local Real-ESRGAN enlarged the selected 992 × 1586 generated source. Visual review
compared a Lanczos-only export with the enhanced output at equal production size;
fastener/trim edges became clearer and surface grain smoother. The source alpha
was resampled independently. Both generated sources, the 1984 × 3172 enhanced
master, 1200 × 1920 production export and processing hashes are preserved.
See [provenance and primary author links](../assets/phobos-autonav/hub-prompt.md).
No tool/model binaries or game-derived pixels are packaged.

## Owner-run checks remaining

1. Place the new hub using native Edit with N1 only, N2 only and both; verify one
   panel, dragging, overlap refusal and saved placement across reload. Confirm
   older compact positions and other modules remain intact. Check native-font
   readability and practical hit targets at the owner's display scale.
2. Exercise ordinary Approach and explicit Approach & Dock with native clearance,
   altered/revoked clearance and occupied ports. Keep Comms/docking accessible;
   check failed handoff reason, explicit Resume, RCS terminal hold and native fees
   and completion. Repeat with an evasive target and reduced available throttle.
3. Check contact loss/reacquisition, damaged bound module, console power loss,
   manual takeover, reload and time acceleration. No loading/page/display action
   should restart combined docking or grant fire permission.
4. On N2, confirm distinct targets, native guard interaction, arcs/ammunition/lock
   indications and immediate Cease Fire from every page. Check manual/defensive
   restrictions, empty ammunition and no duplicate native automatic shots.
5. Check native flow/cycle/limiter/cycle-enable controls, Shutdown and automatic
   ownership release. Retain access to reactor specialist controls and warnings.

These checks target new integration differences. They are not a new-save gate or
a requirement to retest established basic power consumption before useful work.
