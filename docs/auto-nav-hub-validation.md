# Flight hub validation — Auto Nav 0.12.0

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
