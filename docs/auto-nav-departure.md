# Local obstacle avoidance and departure

Prepared for Auto Nav 0.18.0, with Framework 0.24.0 or newer. Existing N1 and
N2 modules and artwork are reused. Owner gameplay evaluation is separate from
the automated checks described in [validation](auto-nav-reclamation-validation.md).

## Departing

Use the **Departure** page of the navigation hub while operating the bound
Polaris console. Ordinary **Fly** never disconnects the ship.

| Action | Result |
| --- | --- |
| Undock & Depart | Release the single connection, retreat using RCS to at least a 1 km hull gap, match relative velocity within the existing 0.2 m/s docking tolerance, then finish. |
| Undock & Continue | Capture the selected destination and displayed continuation mode first, depart as above, then check admission again and enter that operation. Moving the crosshair afterwards does not change the destination. |
| Continuation mode | Cycle Fly, Rendezvous, Follow, Dock and Approach & Dock. Rendezvous/Follow require N2. |
| Resume departure | Reconcile saved intent against actual attachments and replan. Loading never restores departure thrust. |
| Stop departure | Cancel further movement; preserve unresolved detachment evidence. |

F3 equivalents are `phobosnav depart`, `phobosnav depart-continue`,
`phobosnav depart-mode`, `phobosnav depart-resume` and `phobosnav depart-stop`.
They use the same services and preparation checks as the hub.

Before departure, bring every native company-roster member aboard, seal
departure airlocks, repair and power the navigation equipment, and provide working
RCS with at least 42 m/s reserve. This reserve is an authored admission allowance
for the bounded local manoeuvre, not a prediction of every possible detour.
The ship must have exactly one external connection and no secured tow brace.
Orbital stations may have other ships connected: only this ship's exact connection
is released. Ground stations and ambiguous attachment groups are excluded.

Auto Nav 0.19.1 uses Framework 0.25.1's shared company-roster check. A roster
member whose object is missing or unloaded also blocks departure; the mod does
not silently dismiss them or modify the save to clear the restriction.

Obtain native **PUSHBACK & TAXI** clearance from a station or a connection to a
ship you do not own. An owned native mooring needs no invented ATC clearance.
The service reports missing preparation; it does not close doors, recover crew,
pay bills or grant clearance. Native undocking/unmooring retains its legal and
physical consequences. An obstructed exit is refused before detachment.

If interrupted during detachment, **Resume departure** reads the actual connection
and exact ports. A still-attached pending mutation is retained for inspection;
it is never blindly repeated. A confirmed detached ship may resume the outward
leg after fresh checks. Do not remove stored evidence to force a retry.

## Avoidance during local flight

Fly, Rendezvous, Follow, docking approaches, industrial movement and departure
share obstacle observations at the system-update boundary. Candidate ship contacts
pass the existing native visibility, sensor and occlusion checks before geometry
or velocity is used. Sensors are never switched on automatically. Known celestial
boundaries are exclusion regions; asteroid-field markers are not individual rocks.

The local planner uses collision radii, relative motion, the simulation interval
and available RCS braking. It holds a passing side while the route remains valid,
predicts moving contacts and checks the issued path again each control step.
An obstruction or exhausted planning budget produces a named blocked/holding
status and braking rather than an unchecked direct path. The graph is limited to
32 relevant obstacles and 65,536 visibility checks. These are computation bounds,
not permission to discard hazards from the final path checks.

Avoidance takes precedence over pursuit and weapon-facing. Departure, close
detours and industrial traversal use RCS. Torch preferences remain available on
clear cruise segments only after checking the planned burn and subsequent RCS
braking corridor. Manual takeover always releases automatic authority.

Tracking loss suspends the operation, releases owned thrust and requires explicit
Resume. Avoidance cannot promise protection from unseen contacts or after tracking
is lost. Routes and sensor tracks are transient; loading preserves intent, not an
old route. This remains local navigation, not long-distance voyage planning.

## Integration and evidence

`IndustrialNavigation.RequestMove` adds Egress, Transit and CaptureApproach;
the original Request/Observe/Release callers remain supported. Typed status
reports Approaching, Holding, Ready, Blocked or Suspended. Auto Nav owns flight;
Shipbreaker owns capture and material operations.

Native API/geometry evidence comes from the locally inspected 1.0.1.5 build of
[Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts), not a public
API stability guarantee. Earlier sensing research separately cites
[NASA Goddard's Raven work](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/)
and [ESA's LIRIS experiment, with Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before).
Those projects provide context; they do not validate this planner or endorse the
mod. Existing Auto Navigate upstream attribution and unverified reuse terms remain
unchanged; see [provenance](auto-navigate-adaptation.md).
