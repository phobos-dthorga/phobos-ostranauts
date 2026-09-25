# Polaris flight hub — Auto Nav 0.14.0

Prepared 25 September 2026 for Blue Bottle Games' Ostranauts 1.0.1.5 and
Phobos Framework 0.21.0 or newer. Offline checks and packaging are separate from
owner-run gameplay evaluation. Preparing this version does not install or publish it.

Details offers an optional [arrival watch](shared-completion-cues.md) after engaging
Approach or Rendezvous, plus shared cue volume/mute. F3: `phobosnav watch`,
`phobosnav unwatch`, `phobosnav cue-volume`.

## Place the new hub

Install the relevant working module in the console: N1 supplies navigation and
docking, N2 also supplies pursuit, and **N3 Fire Control System** supplies Fire and
Systems independently. N2 no longer authorizes firing. All combinations produce
one hub. Damage never lets another module inherit a saved flight or engagement.

The hub occupies **25% of console width × 80% of height**, using a 600 × 960
reference layout. In native **Edit**, enable/place **PhobosNavFlightHub** in a
clear column, then exit Edit. Native dragging and overlap checks still apply.
Existing compact placements are retained under their old layout identities;
they are not expanded over other instruments. A once-only console message explains
the new placement. Other modules keep their positions. Physical item IDs, recipes
and saved flight bindings are unchanged.

Keep the native map, sensors, warnings and access to Comms/docking. This is a
flight hub, not a replacement for every specialist instrument or reactor panel.

## Routine controls

The header always shows navigation target, operation, contact condition and the
current highest-priority restriction. With N3 it also shows the separate offensive target and
qualified contact condition, even before Engage. Long names can wrap/truncate; Details retains the full
diagnostic explanation. Missing or stale readings show an unavailable mark.
Header content and tabs are inset within the raster's actual display recesses.
Telemetry and settings have separate live display wells, padded inside their
bezels. Short contact/operation labels stay on one line; names can occupy two.
Five compact tabs use **Nav**, **Track**, **Fire**, **Sys** and **Info**.

| Page | Controls and readings |
| --- | --- |
| Navigation | **Approach**, **Dock**, **Approach & Dock**; RCS or RCS + Torch preference; cruise speed, arrival speed and separation. Range in km, signed closing speed and total relative speed in m/s. |
| Active docking | Navigation automatically exposes clearance, captured ports, heading alignment and approach/hold/capture progress, replacing locked flight preferences. |
| Pursuit / Track | **Rendezvous**, **Follow**, separation/cruise. Requires working N2. |
| Fire | Separate offensive target, group, 1–9 volleys, ownership, Auto Aim/reference, guarded Engage and browsable per-weapon readiness. Requires working N3. |
| Systems | RCS authority and remaining mass, delivered acceleration, torch endurance/core temperature, connected stored energy, no-wake state; native flow/cycle sliders, safety/cycle switches and Shutdown. |
| Details | Scrollable diagnostics, full explanations and help. No routine flight or docking controls are hidden here. |

**Resume**, **Disengage** and **Cease Fire** remain available in the bottom strip
on every page. **Disengage clears commanded thrust and allows coasting; it is not
an emergency brake.** Cease Fire ends shooting and aiming, retaining offensive hold and
Follow. Explicit Return to Native releases that hold and may resume native autofire. Changing pages, refreshing displays and loading a save authorize neither
fire nor docking. If the native guarded switch cannot be obtained, panel Engage
is unavailable; there is no unguarded replacement.

Settings apply to the next flight and lock while intent is active or suspended.
Stop before changing destination/profile. The existing RCS inhibition can remove
torch use from a flight, but cannot add permission to an old RCS-only mission.
F3 remains available through the same service; see [flight profiles](auto-nav-flight-profiles.md)
[pursuit controls](auto-nav-pursuit.md) and [N3 controls](auto-nav-fire-control.md).

## Approach & Dock

Obtain native DOCK clearance and select its destination. **Approach & Dock**
(`phobosnav approachdock`) checks assigned compatible ports, captures the exact
target/port pair/module and approaches a staging distance **1 km beyond protected
hull clearance**. Protected clearance is the existing 1.5 × native collision
distance; the margin is an authored guidance policy, not a research result.
The cruise preference and allowed torch apply to this travel phase; arrival speed
is zero. After matching motion, the service revalidates and hands off to the
existing RCS-only terminal controller at a shared physics boundary.

Already inside staging uses terminal admission immediately. Changed clearance,
occupied ports, unsafe motion, missing control authority or an unavailable native
docking interface prevent handoff and expose the reason. Failure suspends intent
for explicit Resume; it never substitutes ports or silently restarts an approach.
Keep native Comms/docking controls available for the checked handoff and attachment.
Native fees, salvage restrictions and completion events are retained. Ordinary
Approach, Rendezvous and Follow do not imply docking.

Both phases always suspend after loading, including when ordinary Approach has
auto-resume enabled. Contact loss suspends; manual takeover cancels automation.
See [terminal policy and native evidence](auto-nav-docking.md).

## Systems scope and measurement limits

Manual propulsion actions first end automatic flight/fire permission (offensive hold remains), then use
native reactor properties. They retain core readiness, limiter and no-wake checks;
they cannot cold-start a reactor or bypass a missing reading. Shutdown uses the
native override-off condition. Keep specialist reactor functions accessible.

RCS authority uses the selected aggregate throttle. Connected kWh is native stored
electrical energy, not generation rate. Torch hours are native estimated endurance,
not a fuel-mass assay. Heading alignment is the native centre-facing geometry, not
a measurement of physical airlock offset. Fire counts describe the most recent
qualified weapon evaluation and become unavailable when stale; loaded counts are not ammunition quantities. They do not
constitute permission to fire. Native missile lock/manual/defensive restrictions
still apply. See [sensor qualification](auto-nav-sensors.md).

## Layout, artwork and verification

[Shared registration](../assets/phobos-autonav/hub-layout.json) drives both Unity
controls and the [offline preview](../assets/phobos-autonav/previews/flight-hub.html).
The preview uses schematic controls and a system font. It checks Navigation,
Pursuit, Fire, Systems and docking at 300 × 480, 400 × 640 and 600 × 960, including expanded
labels and long names. Routine pages do not scroll; Details does. At the smallest
size base labels are 12 px and primary buttons at least 24 px high. Final controls
are reflowed within the generated plate's usable fields rather than shrinking text.
Named safe fields register the header, tabs, main body and bottom strips to the
faceplate. Verification checks containment in those fields and readout padding;
overall panel bounds alone are insufficient. Unity text also has local clipping
at its designated field, without reducing critical font size.

The [faceplate provenance](../assets/phobos-autonav/hub-prompt.md) retains both
992 × 1586 Imagegen outputs and the approved compact N1/N2 masters. With explicit
owner approval, local Real-ESRGAN produced a 1984 × 3172 master and an exact
1200 × 1920 runtime export. It infers texture; it is not lossless recovery or a
native high-resolution generation. All labels, switches and readings remain live.
Blue Bottle Games' button/switch/slider artwork is referenced at runtime through
Framework's isolated adapters; no unrelated reactor controller is instantiated
and no game artwork is redistributed.

See [validation record and remaining owner checks](auto-nav-hub-validation.md).
Earlier 0.7–0.11.1 compact layouts and prompts are preserved as artwork provenance;
their control instructions are superseded by this guide.
