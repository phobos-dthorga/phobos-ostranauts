# G4 automatic wall reclamation

Shipbreaker 0.24.0 adds an explicitly enabled cutting mission to **Phobos'
Rivetline G4 Hull Grabber**, using its existing H4 chute and D4 processor.
It requires Framework 0.24.0 and Auto Nav 0.18.0, with a working N1 or N2.
The same G4 identity, footprint, artwork, manual loading and 2 kW transfer mode
are retained. Prepared implementation and offline checks are not gameplay validation.

## Prepare and start

1. Recover valuables and crew manually. Use an exactly identified, owned,
   unoccupied target, with ordinary walls supported by intact floors. Depressurize
   it first: all measured non-void compartments must be at or below 0.1 kPa.
2. Install and align the G4/H4/D4 chain using the [intake guide](shipbreaker-hull-intake.md).
   Keep its supporting hull walls intact. Provide power, D4 service-room cooling,
   product space and working RCS. Pair and authorize downstream collectors/R4
   separately if wanted.
3. Bind the target and exact console/module through [G4 capture controls](shipbreaker-capture.md).
   Choose **Start reclamation** locally at G4, or through C1. The mission chooses
   an exposed wall and a separate temporary anchor support, stages through Auto
   Nav, and establishes native physical capture before cutting.
4. Watch phase, blocking reason, completed-wall count, retained-wall count and
   paid cutting progress. Clear full destinations when capacity is the blocker.

Old captures remain releasable, but lack the separate protected anchor evidence
required for automatic cutting. Explicitly Release and start a newly planned
working capture. Starting a mission authorizes only the bound G4 and D4 intake;
it does not start other machines or repeated furnace batches.

## Controls

| Control | Effect |
| --- | --- |
| Start reclamation | Create a mission for the bound equipment/target and authorize its G4/D4 chain. |
| Resume reclamation | Recheck the same identities, paid work and physical evidence; observe and replan. |
| Pause / Stop reclamation | Cancel automatic work and flight authority, retaining cargo, paid progress and any current capture. |
| Release | Explicitly release an existing capture through the existing capture service. |

F3 uses the existing `phobosindustry` equipment selector with actions
`reclaim-start`, `reclaim-resume`, `reclaim-pause`, `reclaim-stop` and
`reclaim-status`. See the [console guide](industrial-console-player-guide.md)
for selector syntax. Panels delegate to those same services.

Manual movement, sensor loss, changed bindings and loading suspend the mission.
Reacquisition of a contact does not grant permission to restart. Resume is explicit.
Stop does not unexpectedly disconnect an attached ship.

## Cutting and cooling

| Operation | Powered time | Power | Nominal work energy |
| --- | ---: | ---: | ---: |
| G4 ordinary-wall cutting | 120 seconds per wall | 12 kW | 0.4 kWh |
| G4 transfer to D4 | Existing configured transfer duration | 2 kW | Depends on transfer duration |

These are authored gameplay defaults. Each started cut captures its duration and
power, and advances proportionally to actual delivered electricity. Partial power
slows progress; no power provides none. Cutting and G4 transfer occur sequentially,
so the same power request cannot pay both operations. Other D4 consumption remains
separate. Maintained values and generated tables live in the [item reference](shipbreaker-item-reference.md).

The cooling model assumes a service connection rejecting cutter energy into the
connected D4's interior service area. This is an authored abstraction, not a
simulated coolant pipe or a scientific measurement of G4 machinery. Received
energy becomes native room-gas heat. At least **10 kPa** is required, with a
**40 degrees C ceiling**, including pending heat. Inadequate cooling pauses the
cut; vacuum is not free cooling. The existing furnace motion interlocks are unchanged.

## What the mission will remove

Only exposed, undamaged, empty, unstacked, native **24 kg ordinary walls** qualify.
The wall must retain its floor support and its verified native uninstall contract.
Actors, co-located equipment, attached contents, pressure boundaries, temporary
anchor supports and uncertain state are rejected. Installed floor panels and
other equipment are retained. A navigation distance is never treated as cutter
reach: native capture must establish actual mouth contact on the deck grid.

Before completing a cut the mission reserves G4 space, writes an uninstall journal,
and triggers native uninstallation once. It then re-resolves the exact item ID and
checks its new position before moving that physical object into G4. A separate
transfer journal covers interruption between removal and placement. Unresolved
replacement, relocation or ownership suspends work with evidence retained; missing
cargo is never reconstructed. Framework's general same-ship transfer rules remain
unchanged; only this bound-target adapter crosses the capture.

D4/R4 use their existing recipes and material budgets. Full G4/feed/output capacity
stops further acquisition; a cleared destination can continue while this same
mission remains authorized. Manual pause, faults and reload revoke that permission.

## Traversal and completion

After each removal the mission refreshes exposure, supports, occupancy and capture
geometry. When the current mouth window is spent, it selects the nearest admissible
remaining window by route cost, releases, retreats to a 1.2 km hull gap, traverses
outside the native target collision boundary, then approaches and recaptures.
Candidate search is incremental, at most 64 windows per update; unfinished search
reports planning rather than silently dropping the rest.

**Supported walls exhausted** means no further supported ordinary wall remains
apart from the retained final support. **Unreachable remnants** means walls remain
without an admitted work window. **Capacity wait** retains the current mission;
**Suspended** requires attention and explicit Resume. The target, final supports,
floors and other equipment remain registered. This is not whole-wreck deletion,
new structural recipes, long-distance navigation or automatic furnace batching.

Native uninstall, docking grids and collision-scale evidence comes from locally
inspected [Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts)
1.0.1.5. See [geometry evidence](shipbreaker-close-work-geometry.md),
[departure/avoidance](auto-nav-departure.md) and [offline validation](auto-nav-reclamation-validation.md).
The research links in those documents remain context, not institutional validation
of fictional equipment or authored balance.
