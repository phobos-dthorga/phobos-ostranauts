# Local obstacle avoidance and departure

Prepared for Auto Nav 0.18.0, with Framework 0.24.0 or newer. Existing N1 and
N2 modules and artwork are reused. Owner gameplay evaluation is separate from
the automated checks described in [validation](development/auto-nav-reclamation-validation.md).

## Departing

Use the **Departure** page of the navigation hub while operating the bound
Polaris console. Ordinary **Fly** never disconnects the ship.

| Action | Result |
| --- | --- |
| Undock & Depart | Release the single connection, retreat using RCS to at least a 1 km hull gap, match relative velocity within the existing 0.2 m/s docking tolerance, then finish. |
| Undock & Continue | Capture the selected destination and displayed continuation mode first, depart as above, then check admission again and enter that operation. Moving the crosshair afterwards does not change the destination. |
| Continuation mode | Cycle Fly, Rendezvous, Follow, Dock and Approach & Dock. Rendezvous/Follow require N2. |
| Resume departure | Check the saved departure against the current connection before continuing. Loading never restores departure thrust. |
| Stop departure | Stop further movement; keep the record of any unfinished disconnect. |

F3 equivalents are `phobosnav depart`, `phobosnav depart-continue`,
`phobosnav depart-mode`, `phobosnav depart-resume` and `phobosnav depart-stop`.
They use the same services and preparation checks as the hub.

Before departure, bring every crew member on the company roster aboard, seal
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
physical consequences. An obstructed exit is refused before detachment. When the
docking console is open on that station and cleared for the connected ship, the
release is the game's own clamp button (0.25.0): the stolen-ship check, grace
period, free-pass reset and undock event apply, and the departure waits a few
seconds for the clamps to let go. Otherwise the plain native undock is used.

If interrupted during detachment, **Resume departure** reads the actual connection
and exact ports. If a disconnect is still unconfirmed, it stops for inspection instead of trying again. A confirmed detached ship may resume the outward
leg after fresh checks. Do not remove stored evidence to force a retry.

## Avoidance during local flight

Fly, Rendezvous, Follow, docking approaches, industrial movement and departure
share obstacle observations at the system-update boundary. Avoidance uses the ship
contacts and asteroid-field rocks that your sensors register, checking their size and
movement. A weak contact near the route is still avoided, with extra clearance of one
fifth of its range: the position error the native map shows for such contacts. Weak
contacts more than 100 km away are left out rather than guessed into the route.
When the target or a weak contact near the route is too faint, Auto Nav may switch on
the fewest fitted sensors that help, non-emitting ones first, and tells you each
time; see [automatic sensor engagement](auto-nav-sensors.md#automatic-sensor-engagement-0240).
Known celestial boundaries are exclusion
regions; the game also checks collisions against individual rocks in asteroid fields,
so sensed rocks near the route are avoided like ships.

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

Losing a tracked hazard entirely, for example behind a celestial body or with the
sensors off, suspends the operation, releases owned thrust and requires explicit
Resume. A hazard whose signal only fades to weak stays avoided with wider clearance.
Avoidance cannot promise protection from unseen contacts or after tracking is lost. Routes and sensor tracks are transient; loading preserves intent, not an
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
unchanged; see [provenance](development/auto-navigate-adaptation.md).
