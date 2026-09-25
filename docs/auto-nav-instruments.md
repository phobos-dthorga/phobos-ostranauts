# Polaris instrument panel — Auto Nav 0.10.0

**0.10.0 controls:** Details adds cruise and arrival-speed minus/plus controls.
Numeric speed/distance defaults now belong to each console; Fly readiness also
checks braking room. See [flight profiles and safety](auto-nav-flight-profiles.md).
The existing plate and pickup sprites are unchanged. The artwork preview below
remains a historical illustration of the original 0.7.0 controls.

**0.9.0 sensing:** range and relative speed are unknown without a
[usable native contact](auto-nav-sensors.md). Details and F3 status explain
blocked or suspended tracking. Existing artwork and control positions suffice;
the new messages use translation keys and the existing scrollable Details view.

**0.8.1 naming:** the live title is now **Phobos' Asterel N1 Polaris Auto Nav**,
using Framework 0.12.0. The plate artwork and control positions are unchanged.

**0.8.0 addition:** Details now includes **Dock with selected target**, using the
same service as `phobosnav dock`. During active/suspended docking the stopping
distance reads CLAMPS; docking uses its own capture limits and RCS. See the
[docking guide](auto-nav-docking.md) for clearance, ports and explicit Resume
after reload. The linked artwork preview illustrates the original 0.7.0 layout.

Prepared against Ostranauts 1.0.1.5 and Framework 0.14.0. The owner authorised a
substantial redesign on 24 September 2026. This is a prepared candidate, not an
installed update or an in-game validation claim.

## Operating the panel

The same native navigation-module slot now contains a dedicated flight display,
two rotary controls and three buttons. Its placement remains 25% of board width
by 20% of board height, retaining saved positions and native fit/overlap rules.

- **Flight display:** current phase, destination, range and total relative speed.
  Relative speed is not signed closing speed. Range is centre-to-centre. Coasting
  explicitly reports idle translation; RCS may still correct rotation.
- **Propulsion:** AUTO prefers a running, usable torch with native zone checks and
  RCS fallback. RCS inhibits Auto Nav torch burns immediately without stopping
  guidance. This setting neither ignites the reactor nor certifies torch legality.
- **Stop distance:** requests a centre-to-centre arrival distance; native hull
  clearance can increase the effective distance shown in Details. Presets are
  0.1, 0.25, 0.5, 1, 2, 5, 10, 25, 50 and 100 km. The dial stops at its limits;
  it never wraps from 100 km to 100 m. Existing custom values stay exact until
  the next deliberate detent. F3 `phobosnav arrival <km>` remains available.
- **Fly / Resume:** starts toward the selected ship/station or resumes the saved
  destination. Changing the crosshair does not retarget a saved flight. A rejected
  engagement opens Details with the full reason.
- **Stop / Coast:** ends guidance and removes its commanded thrust; it does not
  brake. After stopping, select settings and start another flight normally.
- **Details / Overview:** switches the display area. Details scrolls with a visible
  scrollbar and retains full destination names, last event, current restriction,
  requested/effective stopping distances, cruise/torch settings and control help.
  Long translations fit here rather than being packed into the main display.

Click the left/right half of a dial, scroll, or drag vertically to step it.
Focused arrow keys also step. Amber values indicate locked settings. Arrival is
locked during active or suspended flights. An old RCS-only flight cannot acquire
torch permission through the panel: stop and begin a new flight. A torch-permitted
flight can switch between RCS inhibition and AUTO without changing saved intent.
Numeric defaults are saved per console; active flight values are captured per
flight. Propulsion preference retains its shared configuration behaviour.

Damaged hardware disables flight/settings controls; Details remains readable.
In native Edit mode the controls yield pointer events to placement and cannot
change flight settings. Exit Edit before interacting, as with native modules.
An active flight on another console blocks takeover and setting changes here.

## Artwork and implementation

The new [faceplate and exact prompt](../assets/phobos-autonav/instruments-prompt.md)
are original built-in Imagegen output, with no game pixels copied. Production
resolution is **1942 x 809** for a **600 x 250** reference display, exceeding our
[2x requirement](artwork-resolution-policy.md). The retained full-resolution raster
scales with code into the native slot. No world-item dimensions change. Existing
pickup art and the former approved faceplate are preserved.

Text, status colour and rotating pointer bars are rendered live. The previous
three-line status block is replaced; artwork contains no baked labels. All player
messages are in the translation catalog. Framework supplies the existing clipped
scroll panel. NavigationService owns snapshot reads and settings/actions, reusing
the same mutations as F3; flight guidance and saved schema are unchanged.

The [interactive preview](../assets/phobos-autonav/previews/instruments.html)
uses sample values and a system font. It demonstrates layout and interactions,
not native Unity input/font behaviour. `scripts/verify-autonav-preview.cjs`
optionally exercises it with Playwright and headless Edge, writing local review
screenshots. Native integration checks remain in `scripts/build-autonav.ps1`.

## Focused verification

Automated coverage exercises bounded/custom arrival detents, captured profile
protection, display reads without save/config writes, same-console authority,
RCS inhibition without cancelling guidance and retention of future save records.
Existing flight, persistence, torch and compiled native-placement checks remain.
The package also verifies artwork identity and minimum production dimensions.

Owner checks after installation: confirm the panel still fits and drags in Edit;
exit Edit and try the rotary controls; check a normal flight and a resumed one;
open and scroll Details. Verify readable labels and input behaviour with the
game's font and your display scale. No basic power-consumption retest is needed.
