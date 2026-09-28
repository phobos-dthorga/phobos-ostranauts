# Auto Nav sensors

Auto Nav steers only by what your ship's own sensors can track. This guide covers
the whole sensor suite, asteroid targets and hazards (0.23.0), then the original
live-contact rules (0.9.0), which still apply.

## Using the whole sensor suite (0.23.0)

Prepared on **28 September 2026** against Ostranauts **1.0.1.5**. Builds and
automated checks are not in-game validation; owner play-testing is pending.

**Every switched-on sensor counts.** Optical, infrared, EM, radar and LiDAR signals
add together exactly as the native navigation map combines them, including the
three sensors inside a combined EO/IR package. The **Details** page and
`phobosnav sensors` list each sensor type with its signal on the current target,
the combined total against the threshold, and any fitted sensors that are off.

| Sensor | What it picks up | Notes |
| --- | --- | --- |
| Optical | Size and motion of the target | Short base range; best on slow or head-on targets |
| Infrared | Heat from thrusters and a running reactor | Cold derelicts and asteroids give no infrared signal |
| EM | Emissions: active sensors, beacons, ships not flying dark | Each EM sensor's contribution is capped |
| Radar, LiDAR | Reflections from the target's size | Active: they emit and make your ship easier to detect |

**Switching sensors on is your call.** Auto Nav never switches sensors on or off by
itself. From the navigation console aboard your own ship:

```text
phobosnav sensors            show the breakdown for the current target
phobosnav sensors passive    switch on fitted optical, infrared and EM sensors
phobosnav sensors all        also switch on radar and LiDAR, which emit
```

These use the same switch as the native Sensors page, including its power override,
and nothing is switched off afterwards.

**Asteroids can be flown to.** Put an asteroid under the crosshair and choose Fly.
It needs a live track within **1,000 km**. Beyond that the native map shows an
asteroid only as an approximate contact. Optical, radar and LiDAR can see asteroids;
infrared and EM cannot, because asteroids are cold and silent. The native asteroid
threshold has no Sensor Operations bonus. The default arrival, 1 km from the surface
at matched speed, ends inside native tether reach: within 2.5 km of the surface and
below 50 m/s relative. Tether with the native mooring control; the asteroid keeps
the same identity once tethered. Docking, Rendezvous, Follow and weapons remain
ship-only.

**Hazards use more of what the sensors see.** Local avoidance now also avoids:

- weak contacts within 100 km, with extra clearance of one fifth of their range,
  the position error the native map shows for such contacts;
- rocks in asteroid fields near the route, which the game checks for collisions
  individually.

A hazard whose signal fades from clear to weak stays avoided. Losing it entirely,
behind a body or with the sensors off, suspends guidance for an explicit Resume,
as before. Weak contacts are never used as flight targets.

**Deliberately unchanged.** Known stations and signal beacons appear on the native map
from charts and broadcasts, but a flight still needs a live sensor track of its
target. A map marker is a destination, not a track.

Native behaviour inspected in the local 1.0.1.5 game by
[Blue Bottle Games](https://bluebottlegames.com/ostranauts): the navigation map's
ship and stellar-object visibility rules, its partial-contact position error, the
per-sensor signal formulas, the Sensors page toggle, the asteroid-field collision
check and the tether admission rule. No engine source or assets are included.
The 100 km weak-contact reach and the clearance use of the map's error are our
gameplay choices, not native navigation rules.

Owner checks for this release:

1. With some sensors off, open Details and `phobosnav sensors`; compare the listed
   types with the native Sensors page. Try `passive`, then `all`.
2. Fly to a visible asteroid from a few hundred kilometres. Confirm arrival stops
   within tether reach, then tether with the native control.
3. Approach an asteroid field and a slow ship that shows only as an approximate
   contact; watch for detours instead of passing close.

## Live contact rules (0.9.0)

Prepared on **25 September 2026** against Ostranauts **1.0.1.5**, BepInEx
**5.4.23.5** and Phobos Framework **0.12.0**. This implements the first sensor
integration priority: navigation that depends on a usable native contact.
Builds and automated checks are not in-game validation. Installation is separate.

### Operating behaviour

Fly, Resume and Dock now require a current contact supplied by the controlled
ship's native sensors. A remembered station or other map marker is a destination,
not proof that the ship can currently track it. Radar and LiDAR remain entirely
under the player's control; Auto Nav never enables them to improve a weak track.
Passive sensors can qualify a contact when their combined native signal suffices.

If contact becomes weak, obscured, unavailable or unreadable during guidance:

- Suspend the flight before another guidance command. Clear Auto Nav's RCS and
  torch commands and retain the destination, flight settings and elapsed budget.
  Docking also retains its assigned port pair; lost contact prevents clamping.
- The ship **coasts**. This does not cancel velocity or spin, predict a safe
  path, or guarantee collision clearance. Use manual control if needed.
- Show range and relative speed as **unknown** until a usable track returns.
  Details and `phobosnav status` explain the contact condition. A saved destination
  name remains identifiable, but no last-known position is presented as live.
- Recover contact, then explicitly choose **Resume** or enter `phobosnav resume`.
  Reacquisition alone never restarts a suspended flight. Stop cancels its intent.

Tracking continues with the navigation screen closed and does not follow later
crosshair changes. Native sensor or power refresh pending on the controlled ship
temporarily makes the reading unavailable; if encountered by guidance this also
suspends. Failures of the controlling console, RCS, binding or other flight
interlocks retain their existing stop policies.

After loading, an ordinary active flight follows `ResumeAfterLoad` only after
fresh sensing and all other validation succeed. A failed contact check records
suspension. Flights already suspended remain so even on a later reload with
good contact. Docking continues to require explicit Resume after every reload.
No contact reading or burn permission is restored from a save. Existing schema-1
flight records and item identifiers remain compatible; unsupported records stay
protected by Framework storage.

### Native boundary and deliberate limits

`NativeContactReader` resolves the observer and selected target in the current
world by ship registration, rejects hidden/destroyed/self/missing targets and
uses native `ShipSignature` and `ElectronicSystems.GetSignatureStrength`.
It passes native range in kilometres and the lower of the two visibility
modifiers. The native combined signal is compared with the inspected detection
threshold: **0.3**, or **0.25** for the selected crew member aboard this ship
with `SkillOpsSensors`. Selecting another operator re-evaluates that benefit,
including with the navigation panel closed. This does not borrow a skill bonus
left behind in the native panel's shared threshold field.

The reader uses `StarSystem.IsLOSBlockedByBO` for physical celestial bodies;
asteroid-field markers and native draw-flag-1 placeholders are excluded. This
is celestial occlusion, not a local hull/obstacle clearance map. Native station
knowledge, debug map visibility and silhouette availability do not bypass contact
qualification. Signal strength is not displayed as an accuracy percentage.

The native sensor registry/state is used as maintained by the game. Inspection
of `Ship.UpdateSensors`, registration/removal and `StarSystem.UpdateShip` shows
sensor updates outside the navigation UI. The adapter neither rebuilds the
registry nor calls toggles, threshold setters, visibility UI methods or target
physics updates. It reads only the selected target and celestial-body collection,
not every ship in the world. Native damage, power and pooled-update timing still
need gameplay integration checks; offline doubles do not prove those transitions.

One shared reader serves engagement, each guidance step, docking before/after
physics, saved-flight restoration, instruments and F3 status. The independently
scheduled native fusion-thrust filter also rechecks contact so a previous burn
permission cannot bypass a lost track. Guidance suspends at its next applicable
unpaused update even if a contact lost at the fusion boundary briefly returns.
Simply drawing the UI never writes saved state or issues thrust.

This is contact qualification around the existing approach/docking controller,
not a new precision measurement, confidence model, survey instrument or obstacle
avoidance system. It adds no sensor furniture, recipes, art or dependencies.
Framework's existing object-state store handles persistence; ship-specific
detection policy stays in Auto Nav. Shared console observations and furnace
probes remain subsequent work.

### Verification

The build runs production reader/service code against narrow native doubles,
covering weak/combined signals, operator changes, closed-panel guidance,
occlusion, missing sensors and targets, pending updates, native read errors,
unknown readouts, contact-loss suspension, explicit resume, reload and protected
saved records. Existing docking tests exercise lost contact immediately before
attachment. Torch tests exercise the production thrust filter and stop path.
The native signal formula is reused, not recreated by these tests.

Owner gameplay checks for this new integration:

1. Select a contact and confirm Fly/Details agree about whether it can be
   tracked. A known map destination without a current track must stay blocked.
2. During an otherwise safe approach, close the panel. Change available sensing
   or operator so the track is lost; confirm guidance suspends, then recover
   contact and explicitly Resume. Check active-sensor choices remain unchanged.
3. Save/reload an active flight and a contact-suspended flight; confirm fresh
   contact gates restoration and the suspended one remains under manual control.
4. During a safe docking trial, lose contact before attachment; confirm no clamp
   attempt and the original pair remains available for a checked Resume.

Original native behaviour belongs to Blue Bottle Games. Existing adapted
guidance retains Gravy/mrkmg attribution in the
[adaptation guide](development/auto-navigate-adaptation.md). No engine source or assets are
included. Existing arrival, fuel, legality and collision limitations remain.
