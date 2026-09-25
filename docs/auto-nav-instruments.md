# Polaris flight hub — Auto Nav 0.12.0

Prepared 25 September 2026 for Blue Bottle Games' Ostranauts 1.0.1.5 and
Phobos Framework 0.17.0 or newer. Offline checks and packaging are separate from
owner-run gameplay evaluation. This redesign is not installed or published.

## Place the new hub

Install either a working **Phobos' Asterel N1 Polaris Auto Nav Module** or
**Phobos' Asterel N2 Polaris Pursuit Module** in the console. Either supplies
navigation and docking; pursuit and fire permission require N2. Both installed
still produce one hub. Damage never lets a different module silently take over
a saved flight's exact hardware binding.

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
current highest-priority restriction. When firing is permitted it also shows the
separate offensive target. Long names can wrap/truncate; Details retains the full
diagnostic explanation. Missing or stale readings show an unavailable mark.
Header content and tabs are inset within the raster's actual display recesses.
Telemetry and settings have separate live display wells, padded inside their
bezels. Short contact/operation labels stay on one line; names can occupy two.
The Navigation tab uses the short label **Nav**.

| Page | Controls and readings |
| --- | --- |
| Navigation | **Approach**, **Dock**, **Approach & Dock**; RCS or RCS + Torch preference; cruise speed, arrival speed and separation. Range in km, signed closing speed and total relative speed in m/s. |
| Active docking | Navigation automatically exposes clearance, captured ports, heading alignment and approach/hold/capture progress, replacing locked flight preferences. |
| Pursuit | **Rendezvous**, **Follow**, separation/cruise, separate offensive-target selection, weapon group, guarded **Engage**, and arc/ammunition/ready counts. Requires working N2. |
| Systems | RCS authority and remaining mass, delivered acceleration, torch endurance/core temperature, connected stored energy, no-wake state; native flow/cycle sliders, safety/cycle switches and Shutdown. |
| Details | Scrollable diagnostics, full explanations and help. No routine flight or docking controls are hidden here. |

**Resume**, **Disengage** and **Cease Fire** remain available in the bottom strip
on every page. **Disengage clears commanded thrust and allows coasting; it is not
an emergency brake.** Cease Fire revokes our offensive permission while retaining
Follow. Changing pages, refreshing displays and loading a save authorize neither
fire nor docking. If the native guarded switch cannot be obtained, panel Engage
is unavailable; there is no unguarded replacement.

Settings apply to the next flight and lock while intent is active or suspended.
Stop before changing destination/profile. The existing RCS inhibition can remove
torch use from a flight, but cannot add permission to an old RCS-only mission.
F3 remains available through the same service; see [flight profiles](auto-nav-flight-profiles.md)
and [pursuit controls](auto-nav-pursuit.md).

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

Manual propulsion actions first release automatic flight/fire ownership, then use
native reactor properties. They retain core readiness, limiter and no-wake checks;
they cannot cold-start a reactor or bypass a missing reading. Shutdown uses the
native override-off condition. Keep specialist reactor functions accessible.

RCS authority uses the selected aggregate throttle. Connected kWh is native stored
electrical energy, not generation rate. Torch hours are native estimated endurance,
not a fuel-mass assay. Heading alignment is the native centre-facing geometry, not
a measurement of physical airlock offset. Fire counts describe the most recent
qualified weapon evaluation and become unavailable when stale or held. They do not
constitute permission to fire. Native missile lock/manual/defensive restrictions
still apply. See [sensor qualification](auto-nav-sensors.md).

## Layout, artwork and verification

[Shared registration](../assets/phobos-autonav/hub-layout.json) drives both Unity
controls and the [offline preview](../assets/phobos-autonav/previews/flight-hub.html).
The preview uses schematic controls and a system font. It checks Navigation,
Pursuit, Systems and docking at 300 × 480, 400 × 640 and 600 × 960, including expanded
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
