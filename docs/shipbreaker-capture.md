# Selected-G4 native capture

26 September 2026. **Shipbreaker 0.22.0 / Auto Nav 0.16.0 development candidate.**
The owner selected temporary native capture as the first operating arrangement.
A finite deployed cutting head remains a possible later enhancement, using the
same exact bindings and flight boundary. No new equipment or saved item IDs are
introduced. Packages are prepared offline; owner-run gameplay evaluation is separate.

## What this stage does

Bind one installed G4, its aligned chute and D4, an existing N1/N2 navigation
console/module and the navigation-selected target. Short G4 labels lengthen when
their prefixes collide; saved addresses always contain full native IDs.

Auto Nav owns an exclusive, RCS-only terminal approach within its existing
10 km hull-gap admission. It observes native sensor tracking and relative motion,
checks fuel, power, mounting, native controller conflicts and time steps, then
matches motion. A stable five-second observation window precedes readiness.
Shipbreaker creates separate temporary **native MooringPort** anchors and uses
Blue Bottle Games' `CrewSim.MoorShip` to bring the deck grids together. The G4 is
never redefined as a docking port. A fresh post-attachment check must find the
exact selected ordinary wall at the selected jaw mouth.

Release uses `CrewSim.UnMoorShip` for that exact recorded attachment. No target
object is cut, transferred or destroyed in this stage. Repositioning is manual
between captures: release, move, reassess and explicitly start again. Neither
Stop nor Release cancels existing ship velocity.

## Controls and prerequisites

- Install Framework 0.23.0+, Auto Nav 0.16.0+ and Shipbreaker 0.22.0 together.
  Shipbreaker's builder prepares its Auto Nav dependency; the installer selects
  and validates it automatically. Existing N1/N2 identities and artwork remain.
- Both ships must belong to the player. The target must have no people aboard,
  no AI pilot and no existing attachment. Stations/hidden contacts are excluded.
  This deliberately bounded first stage does not grant salvage rights.
- Power and repair the chosen navigation console, G4/chute/D4 assembly. Preserve
  its four backing walls and normal installed alignment. Configure native sensors
  and a positive RCS throttle. Approach within 10 km of the native hull boundary.
- Select the target in navigation. In the G4's local industrial panel or C1 entry,
  choose **Bind selected target using …** for the exact navigation console.
  **Start / Resume capture** authorizes approach and native mooring. Panels may close.
- **Stop capture approach** relinquishes flight control without detaching.
  **Release recorded mooring** detaches only the recorded pair and only when the
  target remains unoccupied and no other ship has joined the attachment group.

F3 uses the existing checked industrial command boundary:

```text
phobosindustry capture-bind <C1-full-id> <G4-full-id> <nav-console-full-id>
phobosindustry capture-start <C1-full-id> <G4-full-id>
phobosindustry capture-status <C1-full-id> <G4-full-id>
phobosindustry capture-stop <C1-full-id> <G4-full-id>
phobosindustry capture-release <C1-full-id> <G4-full-id>
```

The C1's normal local operator checks apply to issuing these commands. Continuing
the authorized approach does not invent an operator or depend on an open panel.
Navigation Stop also relinquishes the industrial flight lease.

## Geometry and native limits

The [geometry investigation](shipbreaker-close-work-geometry.md) establishes that
navigation collision circles and physical decks use different scales. This
implementation uses native attachment for deck contact; it never suppresses
collision or equates a map distance with arm reach.

The proposed contact point is a one-tile ordinary wall centre **two deck tiles
forward of the G4 centre**, across one of its four mouth columns. The wall's near
face meets the three-tile-deep grabber's forward face. This is an authored contact
arrangement, not a cutter range extension or a scientific tolerance. Native
`GridUtils.CreateFullGrid`, `GetIncomingDockRotation` and `CanOverlay` validate
candidate deck placements before mutation. At most 64 native fit attempts are
made per assessment; failure reports no admitted pose, not a proof of global
unreachability. Current native fit rules remain the collision authority.

`MooringPort` has no DockA/DockB offset in the inspected game, so its two anchor
origins coincide. The local boundary tests check this assumption and all sixteen
cardinal port orientation pairs. Native mooring rotates/translates the incoming
deck as part of its normal attachment abstraction; this does not simulate a
continuous physical clamp deployment. Post-capture verification detects a
placement mismatch and leaves cargo and the attachment intact for Release.

Native `MoorShip` also clears crime flags. A synchronous, narrowly scoped patch
suppresses that clearing during this already-owned-ship capture, preserving
unrelated records; it does not create clearance or change ship ownership.
No new construction family is introduced: the existing native INSTALL catalogue
continues to cover G4/chute/D4. The temporary anchors are native mooring state,
not free placeable equipment.

## Saved state and interruptions

Framework `ObjectStateStore` stores an additive `Shipbreaker.Capture` record on
the exact G4. It retains participant IDs, mounting, target wall and temporary
anchor IDs. Write-ahead `CapturePending`/`ReleasePending` states precede native
mutations. An uncertain result is never automatically repeated or erased.
Explicit Resume can reconcile a fully established, correctly placed mooring or
confirm that both attachment and anchors are absent, without replaying either
native mutation. Partial/orphan anchor outcomes still require inspection.
Unknown schemas remain intact. No legacy cargo, recipe or hot-job migration runs.

Reload drops flight permission and requires explicit Resume. Manual thrust,
tracking loss, changed hardware, power/fuel failure or an excessive simulation
step ends the approach. This candidate admits at most one second per terminal
physics step and has a 30-minute approach timeout; explicit restart takes fresh
measurements. Saved physics omits only this controller's actuator commands,
retaining real velocity and spin. Already-attached ships stay attached on reload.
An interrupted anchor creation without a completed mooring remains an explicit
inspection case; there is no destructive automatic cleanup.

## Implementation boundary and next stage

The public Auto Nav service requests an exact-console/module/target working
pose, reports fresh readiness and releases only the matching permission. It
never operates Shipbreaker machinery. Shipbreaker's capture service coordinates
the attachment and checked local/C1/F3 controls. Framework supplies the existing
versioned store; no generic mission framework was added.

Source boundaries: [Auto Nav flight service](../src/PhobosAutoNav/IndustrialNavigation.cs),
[Shipbreaker capture service](../src/PhobosShipbreaker/CaptureService.cs),
[native fit and attachment adapter](../src/PhobosShipbreaker/CaptureGeometry.cs),
[contact/save rules](../src/PhobosShipbreaker/Core/CaptureRules.cs) and
[Framework object store](../src/PhobosFramework/Persistence/ObjectStateStore.cs).

Ordinary-wall cutting/acquisition, automatic release/reposition loops, downstream
machine authorization, repeated F6 cycles and whole-supported-wreck completion
remain in the [staged handover](shipbreaker-autopilot-handover.md). The current
G4 transfer motor is not declared to be a powered cutter. Existing furnace
motion interlocks and hot-job contracts remain. Unsupported cargo is untouched;
this stage never claims a completed reclamation mission.

The next implementation slice must pay for actual cutting work, use the native
uninstall transition, verify and journal the resulting exact object transfer,
refresh reach after every removal, and pause against finite downstream capacity.
Retain this capture enhancement if a deployed head later becomes worthwhile.

## Evidence and owner checks

Blue Bottle Games' Ostranauts 1.0.1.5 engine is the native evidence, identified by
assembly hash and method references in the linked geometry investigation. Offline
checks cover cardinal contact/rejection, label collisions, saved exact IDs and
pending commits, tool-facing guidance, native anchor assumptions and dependency
installation. Builds are not Unity scene tests.

Owner evaluation should cover multiple G4s, every mounting orientation, moving
and rotating targets, closed panels, manual takeover, sensor/fuel/power loss,
fast-forward interruption, failed post-contact checks, reload before/during/after
attachment and repeated release without cargo loss. Shrinking hulls and final
remnants join this matrix when removal is implemented.

For the longer-term observation design, **NASA Goddard's Raven**, described by
[Madison Olson on 21 March 2017](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/),
supports continuous relative navigation as a research precedent. **ESA's LIRIS**,
developed by Airbus with Jena Optronik and Sodern, provides an
[uncooperative-target geometry observation precedent](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before).
Neither establishes this game's behaviour, validates these tolerances or endorses
the mod. No new artwork or third-party asset reuse is part of this stage.
