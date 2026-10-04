# Phobos Auto Nav changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Changed

- Reviewed every English entry. Simplified flight, docking and weapon messages. Return to Native is now Return to ship controls, with the same effect: the ship may resume automatic firing. Commands and saved flights stay the same.

### Documentation

- Correct stale dependency and merchant advice and distinguish the old unscrolled hub from current compact controls, Combat and towing. Documentation only; gameplay and saves are unchanged.

- Include all board forms, aliases and residues in the complete item-action audit. Native maintenance actions resolve and attach in offline checks; floor INSTALL is intentionally absent for slot-mounted boards. Live menu approval remains separate.

- Retire the obsolete Approach Assist prototype from source, installer selections, Workshop preparation and item references. Auto Nav remains the supported navigation mod; historical source stays in Git.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Research and planning

- Earlier capture-only research is superseded for supported ordinary walls by Shipbreaker 0.24.0 reclamation, which relies on the 0.16.0 capture flight and 0.18.0 local avoidance and departure entries below. Broader structural recipes, repeated furnace batches and whole-wreck completion remain Shipbreaker work, not Auto Nav flight changes.
- Keep [NASA Goddard's Raven research](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/) and [ESA's LIRIS experiment by Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before) as sensing context, separate from Blue Bottle Games native evidence and authored gameplay choices. No institutional endorsement or gameplay validation is implied.

## [0.33.0] - 2026-10-05 - Draft

### Changed

- A docking approach that was under way when the game was saved carries on after loading, for both Dock and Approach and Dock, like an ordinary flight. It runs the same hardware, target, port and control checks as Resume, and stays suspended with the reason if one fails.

### Save compatibility

- Automatic. Nothing saved changes; a saved docking approach now resumes.

### Compatibility and limits

- Follows Auto Nav's existing ResumeAfterLoad setting. Rendezvous and Follow still suspend after loading, and fire authority is never saved.
- Checked offline; not yet seen in the game.

## [0.32.0] - 2026-10-03 - Draft

### Changed

- Repairing a broken Polaris module uses up its two small electronic parts and gives back only the working module, as the game's own repairs do. No more Spent Service Parts.

### Save compatibility

- Automatic. Spent Service Parts left by older repairs are removed from each ship as it loads, wherever they lie (deck, containers, machines, pockets), with one crew-log line on your ships saying how many went. Nothing else in the save changes, and repairs already under way finish normally.

### Compatibility and limits

- Requires Phobos Framework 0.74.0 or newer.

## [0.31.3] - 2026-10-01 - Draft

### Changed

- Performance pass, first round (owner request, 1 October 2026). Fire control no longer reads every weapon on every physics step once the nav station has been opened: with the station closed and nothing aimed, permitted, targeted, held or in combat, it does nothing until one of those is true again.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Static review only. Flight, docking and firing rules are unchanged; the mission-flight findings are listed in the performance record for a measurement first.

## [0.31.2] - 2026-10-01 - Draft

### Fixed

- Switching crew with the nav screen open no longer leaves the Polaris Flight Hub reading and commanding the previous nav console. The game keeps its module panels when you switch to a crew member seated at another console; the hub now follows the console on screen, drops any unapplied setting changes and closes open pop-ups. This caused a stuck "Another navigation console controls the active flight" warning that only a reload cleared.
- When another nav console aboard the same ship is flying it, the warning now says so and Disengage works from this console too; the ship coasts, as with any Disengage. Starting or changing a flight still belongs to the console that owns it, and a console on another ship still cannot stop it.

### Compatibility and limits

- No save changes. Offline checks only; the crew-switch case has not been tried in game.

## [0.31.1] - 2026-09-30 - Draft

### Fixed

- The speed preference buttons read "- Cruise speed" and "- Arrival speed" again: the minus sign they used is missing from the game's fonts and showed as a blank.

### Compatibility and limits

- Saves are unchanged. Offline checks are not gameplay validation.

## [0.31.0] - 2026-09-30 - Draft

### Added

- The Asterel N1, N2 and N3 boards are now also sold for scrip at the CCRE faction kiosks at Zhonghuamen Terminal and Port Yangshan (Mars) and the GalCon faction kiosk at Port Mojave (Ceres), sixteen at a time. The N1 needs Warm standing, the same as the kiosks' own nav modules; the N2 pursuit and N3 fire-control boards need Friendly, since they chase and fight other ships. Nothing needs Honored.

### Compatibility and limits

- Requires Phobos Framework 0.53.0 or newer. Kiosks keep their current stock until their next normal restock; nothing is refilled or edited in a save. Offline checks are not gameplay validation.

## [0.30.0] - 2026-09-30 - Draft

### Changed

- The board economy now lives in framework/economy.json, read through Phobos Framework with player override files in BepInEx/config/PhobosAutoNav/economy: the N1, N2 and N3 prices and broken prices, repair and dismantle work, the repair bill, the four merchant routes, the regional factors, the lot of sixteen and the derelict salvage chance the Salvage settings start from. Shipped figures are unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.52.0 or newer. Saves, flights and stocked shops are unchanged. Offline checks are not gameplay validation.

## [0.29.0] - 2026-09-30 - Draft

### Fixed

- Flights stopped with Physics interrupted whenever the game's own ship update failed. In the owner's save the game throws a null-reference error in that update from time to time while it spawns an NPC ship in the background, which stopped Auto Nav seven times in one session. A failure that did not come from Auto Nav's own code is now ridden out: Auto Nav clears its thrust for that step and guidance carries on the next frame. If the game's update fails more than three times within ten real seconds, the flight, docking or capture move is suspended rather than stopped, keeping its destination, ports and elapsed time for Resume. A fault in Auto Nav's own code still stops the flight, as before.

### Compatibility and limits

- The error itself is in the game and is not changed; Auto Nav only decides how to respond to it. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.28.0] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 5 (the game crawls at fast-forward). While a flight, docking, capture move or fire control runs, every controller asked the game about the same things again each physics step: the target and every other ship's contact reading, the console's hardware state, the flight's binding to the console, the RCS reserve and the own ship's docked partners. Each is now read once per step and shared by the guard, the guidance tick, docking, fire control and the hazard sweep; Auto Nav's own sensor switches refresh the shared readings at once, and panels, the torch controller and other mods still read fresh.
- The flight record no longer rewrites the console's saved map on every step. Progress settles every two real seconds, on every state change (engaged, suspended, arrived, stopped, ports assigned) and before every native save, so a saved game always carries the latest elapsed budget. A crash, never a save, can lose up to two seconds of elapsed flight budget. A corrupt record still stops the flight on the very next step.
- The two checks for other flight controllers resolved their types by reflection on every call, once per frame while flying and on every panel read. The AutoDock assembly and the retired Approach Assist prototype are looked up once per session; their live state is still read on every call.
- Hazard sweeps reuse their working buffers instead of allocating lists, sets and copies of the ship table every step; the native weapon hooks pass the game's own lists through untouched while no weapon group is leased; manual-takeover handlers are kept as one array rebuilt on subscription; sensor housekeeping decodes the flight record twice a second instead of every frame; a target's generic display name is looked up only when needed.
- Recorder scopes autonav.guard.update, autonav.hazards.scan, autonav.persist.write and autonav.foreign_controller.check, and the counter autonav.contact.reads.

### Compatibility and limits

- Requires Phobos Framework 0.45.1 or newer. Saved data, flight rules, admission checks and sensor policy are unchanged. Offline checks are not gameplay validation.
- The repository installer archives the retired Approach Assist 0.1.2 prototype plugin when it installs this version; that prototype was still loading beside Auto Nav and its patches ran every frame. Workshop installs are unaffected.

## [0.27.0] - 2026-09-29 - Draft

### Added

- Torch burns now run the reactor the way the game's own long-range course plot does. Auto Nav sets Cycle for the acceleration the approach needs and steers Flow to hold the core near its ideal, correcting on the hot side from the temperature at which the game begins to damage the reactor wall. Cycle stays under the console's torch safety limiter and your g setting. A core that drifts outside the game's safe band (80 to 120 percent of ideal) stops the torch, hands the pre-flight Flow and Cycle back, tells the crew, and RCS carries on; the torch returns once the core has settled within 5 percent of ideal. The panel shows Torch starting while the reactor takes the command.
- phobosnav status reports the RCS authority the throttle slider actually grants, and how often avoidance took the controls this flight and why. Detour and Blocked notices name the obstacle's range and mark weak contacts.

### Fixed

- The torch never lit. The burn command was written and cancelled within one physics step, so the reactor's own update, which runs on its own 0.27 second cadence, always read a zero cycle. The command now stays written until that update delivers thrust, with a short startup wait and a five second hold-off for a reactor that accepts controls but delivers nothing. No-wake rules gate the waiting command exactly as before.
- Ordinary approaches in station clusters no longer circle or hold range under the avoidance planner. Ships docked at your target count as part of the target; a weak contact farther than 10 km no longer seizes the controls on its inflated position error, though it still shapes the route and asks for sensors; and the torch guard no longer refuses every burn whose RCS stop would take over ten minutes.
- Turning and torch alignment plan on the turn authority the throttle slider actually grants, so the heading settles instead of overshooting. A braking leg judges the turn against the time the RCS would need to shed the speed, not only the target-prediction horizon.
- Heavy time compression holds a flight step instead of ending the flight, as 0.25.0 intended; the shared guard used to disengage before that hold was reached.

### Compatibility and limits

- Requires Phobos Framework 0.40.0. Saved flights, ports and preferences are unchanged. Auto Nav never ignites, repairs or refuels the reactor, drives Flow and Cycle only while a burn is commanded, and a pilot slider move still hands control back. The 0.25.0 torch note was incomplete: that fix removed one always-false guard, this one the ignition handshake. Offline checks pass, including a reactor double with the game's core model; owner play-testing of torch approaches and cluster approaches is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.26.0] - 2026-09-29 - Draft

### Changed

- The N2 Polaris Pursuit and N3 Polaris Fire Control boards gain the N1's second-hand routes: lightly worn at the K-Leg fixer, broken at K-Leg supplies and refurbished at the Venus Orbital scrap kiosk, in the usual board lots.

### Compatibility and limits

- Prices, repair, salvage and saves are unchanged. New offers appear at normal restocks; existing shop inventories are not refilled. See the [economy coverage audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/economy-coverage-audit.md).

## [0.25.0] - 2026-09-28 - Draft

### Fixed

- Torch burns are no longer cut whenever Auto Nav flies. The obstacle picture taken before each physics step was compared with the exact epoch, but the game advances the epoch inside its update and the reactor's own thrust update runs on its own cadence, so the guard refused every burn. The picture now stays valid for the whole step and the zone refresh.
- Final approaches no longer crawl. The flight's own target was swept as an imminent obstacle across the whole braking phase, handing it to slow avoidance; the target is the arrival controller's business and is left out of that sweep.
- A distant tracked ship that later fades no longer suspends the flight; only obstacles in the flight corridor are carried forward as threats, and a ship that leaves the world (destroyed, despawned, hidden) is gone rather than a lost track. A clear direct leg needs no route plan however many contacts are around.
- Any native sensor or power refresh, not only one Auto Nav caused, is a short hold rather than a lost contact, for flights, docking, capture moves, combat and fire control, up to the same settle budget.
- Ships the game's own navigation station always shows, this ship's docked partners, signal beacons and the tutorial derelict, count as tracked without a signal test.
- Tumbling targets can be docked and captured. The clamp and capture stability checks required the target's spin to be nearly zero, which derelicts never satisfy; the game's own docking has no spin rule.
- Clearing thrust works. Three places asked the game for a zero-duration manoeuvre, which it ignores, leaving RCS acceleration running; the smallest positive duration now carries the zero command.
- Docking attaches through the game's own clamp button: its alignment check admits the clamp and its clamp sequence does the port choice, crime checks, docking events, autosave and MFD change. Auto Nav holds in position until that check admits. Docking a further ship while one is attached is allowed when a port is open, as the game allows.
- Undock and Depart releases the clamps through the game's own clamp button when the docking console is open on that station and cleared for the connected ship, so the stolen-ship check, grace period, free-pass reset and undock event apply; otherwise the plain native undock is used as before.
- A native orbit lock on the engaged ship (orbital mode, the game's own anchoring) is a pilot takeover: Auto Nav steps aside and the lock proceeds, instead of being silently swallowed.
- An oversized simulation step (heavy time compression) is held for that step instead of ending the flight, docking, capture move or fire control. Resume admission uses the step the game actually ran.
- The throttle slider is mapped the way the game maps it for the pilot's own RCS commands, so Auto Nav's RCS authority matches manual flying at the same setting.

### Compatibility and limits

- Saved flights, ports and preferences are unchanged. Known stations still go through the signal test (their record belongs to the navigation station). Body velocities are still measured from positions, since the game keeps only per-update deltas. Offline checks pass; owner play-testing of torch approaches, tumbling-target docking, the clamp hand-off and time-compressed flights is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.24.0] - 2026-09-28 - Draft

### Added

- When the target, or a weak contact near the route, is too faint to track, Auto Nav switches on the fewest fitted sensors that fix it: non-emitting optical, infrared and EM first, radar or LiDAR only when those are not enough, and nothing when no sensor would help. Sensors you already have on are left alone. Each sensor's contribution is predicted with the game's own signal formula, and switching uses the native Sensors page switch.
- Every time, a crew log line names the sensors, the reason and whether they emit. The native nav-map warning banner, with its tone, appears while that ship's navigation station is open, at most once every 20 seconds. The Polaris hub shows Sensors on for Auto Nav while they stay on; Details and phobosnav sensors mark them.
- New AutoEngage setting in the Sensors section: All (default), Passive (never radar or LiDAR) or Off.

### Changed

- When its work ends (arrival, Stop, completed docking or departure, released industrial work), Auto Nav switches off only the sensors it switched on, after about five game seconds, and logs it. Suspended flights keep them for Resume. Your own switch always wins: a sensor you switch off stays off until that work ends, and one you switch on is never switched off by Auto Nav.
- A sensor-list refresh right after Auto Nav's own switching briefly holds thrust instead of suspending. A refresh that does not finish still suspends.
- Requires Phobos Framework 0.34.0.

### Compatibility and limits

- Saved flights, preferences and module identities are unchanged. Notes about Auto Nav's sensors are saved on the sensor units, so switch-off also works after loading; loading never switches sensors immediately. Hazards are surveyed at most every two game seconds. The 1.2 x headroom, the grace and the hazard interval are gameplay choices. Power draw, heat and multi-type sensor units behave as with the native Sensors page. Offline checks pass; owner play-testing is pending.

## [0.23.0] - 2026-09-28 - Draft

### Added

- Fly to an asteroid under the crosshair. It needs a live track within 1,000 km, following the native map's asteroid rule: optical, radar and LiDAR can see asteroids, infrared and EM cannot, and there is no Sensor Operations bonus. The default arrival ends inside native tether reach. The asteroid keeps its identity once tethered. Docking, Rendezvous, Follow and weapons remain ship-only.
- Details and the phobosnav sensors command list each sensor type's signal on the current target, the combined total against the threshold, and fitted sensors that are switched off.
- phobosnav sensors passive switches on fitted optical, infrared and EM sensors; phobosnav sensors all also switches on radar and LiDAR, which emit. Both use the native Sensors page switch. Auto Nav still never switches sensors on or off by itself.

### Changed

- Local avoidance, departure checks and industrial route costs also avoid weak contacts within 100 km, with extra clearance of one fifth of their range, the position error the native map shows. Rocks in asteroid fields near the route are avoided too; the game checks collisions against them individually.
- A hazard that fades from a clear to a weak signal stays avoided. Losing it entirely still suspends guidance for an explicit Resume.

### Compatibility and limits

- Saved flights, preferences and module identities are unchanged; asteroid flights save the asteroid's native ID. Known stations and beacons still need a live sensor track before a flight. The 100 km reach and the clearance use of the map's error are gameplay choices. Offline checks pass; owner play-testing is pending.

## [0.22.4] - 2026-09-27 - Draft

### Changed

- Raise equipment offer chances to at least 85% before the availability setting, fill missing N1/N2/N3 offers at general suppliers, and include all three boards in native module salvage. New configurations default to 30% total per eligible roll; existing saved settings remain authoritative. One board per successful salvage choice, with damaged-only pools preserved.
- Applies to future native stock and loot generation; no forced restocks, saved-cargo changes, price changes or live gameplay validation. See [merchant availability and salvage](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/merchant-stock.md).

## [0.22.3] - 2026-09-27 - Draft

### Fixed

- Keep board and remnant pickup/drop actions consistent with their actual stack limits through the shared handling helper. Preserve module slots, repair, restore, dismantling and navigation behaviour. Requires Phobos Framework 0.30.2.
- Audited all 118 implemented item definitions and checked native action/slot contracts offline. Live menus and loaded inventory handling still require owner testing; see the [item handling audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/item-handling-audit.md).

## [0.22.2] - 2026-09-27 - Draft

### Fixed

- FCS and Combat now accept the same reciprocal, securely braced two-ship tow as ordinary navigation. FCS uses only weapons aboard the piloted ship; Engage still grants firing permission separately. Docking and industrial close work still require releasing the tow.
- Reject the attached ship as a fire target, including a connection made after Engage. Brace faults cancel aiming and firing before dispatch; restoring the brace does not re-arm the group. Show the specific towing blocker instead of a generic unavailable fault.
- Stop standalone weapon aiming through a positive native update interval: a zero interval was ignored by the game. Native weapon eligibility, missile locks, fuel accounting, saved assignments and reload Hold remain unchanged.
- Offline service tests cover secured towing, invalidation, attached targets, Combat and reload. Unity handling and firing remain owner-tested; see the [towing guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-towing.md).

## [0.22.1] - 2026-09-27 - Draft

### Fixed

- Ordinary Approach, Rendezvous and Follow now accept a reciprocal two-ship connection with secured native towing braces. Previously every docked connection was rejected, including a valid tow. Native RCS mass, fuel and torch controls remain authoritative.
- Include the attached hull in clearance, traffic avoidance and projected no-wake checks; exclude that hull from independent traffic. Stop on lost brace security, pending brace updates or conflicting controls aboard the tow.
- Navigation warnings take priority over an idle FCS fault. Dock and Approach & Dock, FCS/Combat and industrial close work still require releasing the tow; stations, mooring, chains and unsecured attachments remain blocked.

### Validation

- Read the owner's save and logs without changing them. Added mirrored coupled towing sequences, attachment/admission and warning regressions, plus torch admission checks. Native group physics and live handling still require owner playtesting. See [the towing repair record](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-towing.md).

## [0.22.0] - 2026-09-27 - Draft

### Added

- Track now offers explicit N2 + N3 Combat: match the selected fire target's motion and separation with RCS or permitted torch manoeuvres, and request the selected weapon's facing. Entering suspends the previous mission and grants no firing permission. Engage remains separate.
- Show movement priority and effective separation. Track instructions scroll; fixed stop controls and the ten-per-second refresh limit remain.

### Fixed

- Keep avoidance waypoints in the target-relative frame used by guidance. Coupled regressions reproduced a docking diversion/timeout when both ships shared orbital motion; mirrored moving cases now complete without weakening clearance or attachment checks.
- Base the coast tolerance on attainable guidance speed so a high saved cruise setting cannot leave an almost stationary Approach & Dock coasting indefinitely.
- Clear commanded RCS when terminal fuel admission fails after acquiring docking intent.
- Permit N3 coordination during ordinary Approach, Rendezvous and Follow. Navigation retains its destination; braking, traffic, torch manoeuvres, docking, departure and industrial movement take priority.

### Compatibility and limits

- Framework minimum remains 0.30.1. Saved schemas, assignments, gameplay values and other mod versions are unchanged. Combat is transient: reload leaves the previous mission suspended and FCS on Hold, without movement, aiming or firing permission.
- Cease Fire keeps Combat range matching. Leave Combat, Disengage or Return to Native exits Combat; the previous mission needs explicit Resume. Change movement settings after leaving Combat. Contact, hardware or operator loss ends combat authority without automatic restart.
- Automatic Artemis firing remains unsupported by the inspected native envelope. Native shot, lock and ammunition checks remain authoritative.
- Tests use production guidance/update ordering with native boundary doubles. The original owner incident is not proven reproduced; live handling and Unity approval remain owner checks. No performance captures or artwork generation were requested.

## [0.21.1] - 2026-09-27 - Draft

### Fixed

- Restore readable Polaris buttons through Framework 0.30.1. Replace conflicting painted interior boundaries with separate live frames, compact command/help text and smaller buttons. Departure, Info and dialogs have inset scroll areas with contrasting handles; emergency actions stay fixed.
- Show current Resume guidance when tracking has recovered; retain the earlier suspension reason in event history. Resume still rechecks flight safety.
- Describe an off weapon as "Off: check power or control signal" rather than implying a manual switch. Report damage first when both conditions apply.

### Known limits

- Native palette contrast and offline regressions are checked; corrected Unity appearance and scrolling still require owner playtesting. No gameplay values or save formats change.


## [0.21.0] - 2026-09-27 - Draft

### Changed

- Refresh all six Flight Hub tabs, including Departure, with clearer native controls, two-row navigation and expandable warnings. Retain placement, Edit/rescue support and ten-per-second routine refreshes.
- Show installed weapon groups, including switched-off and damaged weapons. A switched-off Artemis is identified instead of reporting an empty group.
- Add a confirmed group handoff: return the old group to native control and hold the new group without firing permission. Native automatic fire may resume for the released group.
- Left-click Volleys to increase and right-click to decrease, wrapping from 1 to 9. Automatic missile eligibility and lock restrictions remain unchanged.

### Requirements

Requires Phobos Framework 0.30.0 or newer; other provider requirements remain unchanged.

### Known limits

- Offline regression and browser-layout checks are separate from owner-run Unity interaction tests. No additional performance captures or Steam publication are claimed.
- Existing faceplates and suitable vanilla graphics are reused after review; no new artwork generation was warranted.


## [0.20.3] - 2026-09-27 - Draft

### Fixed

- Stop rebuilding native hull grids for the idle navigation display. Show clearance separately; docking commands, active checks and attachment still validate native hull fit.
- Refresh the shared header and selected page at most ten times per second, with immediate action/context updates. Reuse one presentation snapshot and retained widgets; skip hidden-page detail reads and unchanged display writes.

### Compatibility and limits

- Requires Phobos Framework 0.29.0. Flight, firing, docking, saved IDs and resource rules are unchanged; commands always recheck current state.
- Baseline measurements and audit findings are in docs/performance-audit.md. Further captures are deferred by owner direction; offline checks do not establish a measured speedup or Unity interaction approval.

## [0.20.2] - 2026-09-27 - Draft

### Documentation

- Review English controls, warnings, descriptions and help for practical player language; retain precise diagnostics and established equipment names. Update current guides, item-reference inputs and the Workshop draft.
- Follow the retrospective language rule and glossary in docs/development/player-language.md, informed by Blue Bottle Games' official Ostranauts description and Daniel Fedor's developer AMA. This is an interest-based audience interpretation, not measured demographic data.

### Compatibility and limits

- Wording only: translation keys, placeholders, commands, saved identities, resource values and gameplay rules are unchanged. This is an unpublished development candidate; Unity text layout remains unverified.

## [0.20.1] - 2026-09-27 - Draft

### Changed

- Auto Nav assembly offcuts and board residue stack to 10 each.

### Compatibility and limits

- Navigation modules and shared spent service parts remain individual items.
- Native stack limits apply on the ground and in compatible containers. Existing IDs, per-item mass, value and recipes are preserved; existing items use current definitions on reload, without automatically consolidating stored cargo. Offline checks do not establish gameplay validation.

## [0.20.0] - 2026-09-26 - Draft

### Control panels

- Retained the approved Polaris hub artwork, native placement, flight/propulsion/fire separation and persistent Stop/Cease controls.
- Navigation speed, arrival distance, propulsion preference and departure-intent edits now remain drafts with Apply/Discard. Fresh validation rejects stale settings; new flight commands wait until the draft is resolved.
- Compacted departure/details controls and routed crew access through the shared draft-aware interface. Requires Framework 0.26.0; existing flight identities and captured flight intent are preserved.

## [0.19.1] - 2026-09-26 - Draft

### Fixed

- Use Framework's native company-roster check for departure, fixing an always-blocked crew check when the old crew-list field is unpopulated. Missing or away crew still block departure.
- Requires Framework 0.25.1 for the shared roster service. Existing flight permissions, saved targets and explicit Resume remain unchanged; automated checks are not gameplay validation.

## [0.19.0] - 2026-09-26 - Draft

### Crew automation

- Added a standing crew order to resume one explicitly permitted recorded flight, using the exact saved target and a working navigation module. It never acquires a replacement target.
- Added shared crew controls to hub details and preference for native piloting skill during eligible hands-on preparation.
- Time-skip suspends departure, industrial movement and ordinary automatic flight before advancing time. Crew-launched flights require explicit Resume after reload or interruption.

### Compatibility and limits

- Requires Framework 0.25.0. Native collision warnings and flight authority checks remain unchanged. Auto Nav’s existing upstream provenance hold remains in force; no Steam publication or gameplay validation.
- See [crew controls, sources and owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/crew-automation.md).

## [0.18.0] - 2026-09-26 - Draft

### Added

- Add shared local obstacle avoidance to Fly, Rendezvous, Follow, docking and industrial approaches. Admit native sensed contacts before reading geometry; predict motion, retain passing side, bound route search and check the final swept path. Blocked routes brake/hold; lost tracking suspends for explicit Resume. Known celestial regions are excluded; field markers are not individual obstacles.
- Add explicit Undock & Depart and Undock & Continue for one exact orbital station/ship/mooring connection. Require crew, sealed boundaries, native clearance, working RCS and an admitted exit; retain station neighbours. Depart to a 1 km hull gap and relative stop, with the destination/mode captured before detachment. Ordinary Fly never disconnects.
- Extend IndustrialNavigation additively with egress, transit, capture approach, route cost and typed status for Shipbreaker. Manual takeover has priority. Departure and close industrial legs use RCS; torch burns require an admissible burn and braking corridor.

### Persistence and limits

- Store departure intent and detachment journals separately on the console. Reload never replays live thrust or blindly repeats a pending native mutation. Ground stations, ambiguous groups and secured towing are excluded.
- Retain N1/N2 identities, artwork, existing flight preferences and Framework 0.24.0 dependency. Local avoidance is not a promise about hidden contacts or long-distance voyage planning. Existing upstream attribution and binary-distribution hold remain.
- Add the [departure guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-departure.md) and [offline validation record](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/auto-nav-reclamation-validation.md). Builds and synthetic/native-boundary checks are not owner gameplay validation; Steam publication remains pending.

## [0.17.0] - 2026-09-26 - Draft

### Changed

- Increase successful local and regional N1/N2/N3 offers to wholesale board lots, including the existing worn, refurbished and broken offers. Uses Framework 0.24.0; unit prices, rare N1 salvage and saved flights are unchanged.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.16.1] - 2026-09-26 - Draft

### Fixed

- Replace the generic native-automation warning with the actual blocker: native pilot, saved engagement switch, station-keeping, held thrust, torch request or waypoints. Disengage is available for local native control intent even without a Phobos flight record. Explicitly release the three standard native autopilots, clear local console switches and cut torch/RCS thrust while preserving reactor operation, velocity and spin. Other flight plugins and unrelated AI remain protected.
- Keep the flight hub behind native overlays and hide its surface/input on the Rescue screen; Done restores the existing page and placement. Native layout Edit still blocks operational controls. Track help identifies its N2 requirement; Resume and Cease Fire retain their operation-specific availability.

### Validation and limits

- Regression checks cover stale native switches, two local consoles, manual torch request, standard native pilots, foreign-console and independent-controller rejection. Build and offline checks do not validate Unity clicking or live flight. [Hub owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/auto-nav-hub-validation.md) remain required. Prepared candidate; no Steam publication.

## [0.16.0] - 2026-09-26 - Draft

### Added

- Narrow industrial working-pose service for the selected N1/N2 console/module and target: exclusive RCS guidance, fresh readiness observations and permission-specific release. Existing native tracking, fuel, power, motion and step limits apply; other flight/aiming controllers cannot compete.
- Tool-facing terminal guidance for four mounting orientations, closed-panel operation, immediate manual takeover and industrial status in the navigation panel. Reload discards industrial flight authority and never replays saved thrust.

### Limits

- Shipbreaker 0.22.0 uses this service for native capture/release. It does not provide cutting, automatic hull traversal or repeated downstream jobs. See the [capture guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-capture.md).
- Prepared offline; gameplay evaluation remains owner-run. Existing upstream provenance and distribution holds remain unchanged. No Steam publication.

## [0.15.0] - 2026-09-26 - Draft

### Added

- N1, N2 and N3 gain bounded regional offers at 15 additional placed vanilla retail markets. Native control-system demand and surplus affect eligible module prices; the Flotilla supplies refurbished modules. Existing offers, base values, recipes and flight saves are preserved.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Requires Phobos Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

## [0.14.1] - 2026-09-26 - Draft

### Fixed

- Correct blank flight-hub title, tabs, status/footer fields and truncated telemetry with the native font: retain full-size glyphs inside each field's mask, use compact baseline spacing and clip long lines without allowing vertical ellipsis to erase short fields.
- Use a native-font-supported minus sign for decreasing cruise speed, arrival speed and separation. Keep explicit two-line Approach & Dock labels and all three range/speed readings.
- Require Framework 0.21.2 for the shared text correction and native knob/switch/slider donor audit when BepInEx loads the game assembly from memory. The pinned game hash and isolated widget checks remain enforced.

### Compatibility and limits

- Preserve flight behavior, save identities, module placement and approved artwork. Prepared from the current five-tab N1/N2/N3 hub; the reported screenshot and installed game were using 0.12.1's four-tab hub.
- Offline regression/build checks are separate from owner-run visual and gameplay confirmation. No installation or publication is implied.

## [0.14.0] - 2026-09-25 - Draft

### Added

- Optional one-shot arrival watches for active Approach or Rendezvous through Details or F3. Cue only after ARRIVED is saved; Stop, suspension, persistence failure and reload clear the watch.
- Use Framework's shared quiet completion channel and volume/mute. Preserve native docking audio; no cue for Approach & Dock staging, indefinite Follow or weapons.

### Requirements

Phobos Framework 0.21.0 or newer. Flight rules and saved IDs remain unchanged. Gameplay/listening evaluation and upstream provenance review remain pending.

## [0.13.0] - 2026-09-25 - Draft

### N3 Fire Control System

- Add Phobos' Asterel N3 Polaris Fire Control System within Auto Nav, independent of N1/N2 during manual flight or coasting. N1 retains navigation/docking; N2 retains pursuit but now requires N3 for automated firing. Explain this once per console and preserve existing saved identities/recipes.
- Add N3 intact/damaged identities, native spawning, Polaris merchant stock, repair and Restore. Authored balance matches N2: 0.4 kg, $5,400/$1,350, two electronics parts, 30-minute construction and retained mass-balanced offcuts/residue.
- Separate qualified per-weapon observation and group ownership from navigation. Native, FCS Hold, Armed, Held and Fault are distinct. Filter controlled offensive queues while preserving unrelated groups, defensive PDCs and deliberate native manual shots.
- Offer 1–9 explicitly authorized native volleys, default one. Recheck power, damage, modes, ammunition, arc/range, targeting and missile lock before each batch. Native salvo costs, jams, reloads, projectiles and witnesses remain authoritative; consequences apply once per successful batch. Decoys and unsupported weapons are excluded.
- Cease Fire, budget completion and reload retain offensive Hold until explicit Return to Native, which may resume native autofire. Save preferences/ownership only; never restore targets, remaining permission or live aiming. Contact, power, exact hardware/player binding or invalid intervals revoke permission without automatic rearm.
- Add separately permitted RCS-only coasting aim and an N2 Follow attitude request with a stable selected weapon reference. Braking, clearance and propulsion limits take priority. Pilot input during Auto Aim cancels aiming/firing; weapons-only authorization supports manual piloting. Navigation/docking starts end engagement.
- Add Fire to the shared hub with five compact localized tabs, a browsable weapon card, ready/selected counts, volley/ownership/aim controls and guarded Engage. Keep Cease Fire on every page, preserve approved faceplate/native artwork, and retain the 0.12.1 managed layout/startup correction.
- Keep Framework 0.17.0 minimum and existing native weapon restrictions. Inspected stock missile definitions lack the required automatic envelope; show that limitation without a manual-mode override or invented range. No guaranteed hits or intact boarding.
- Attribute historical weapon-role context to Daniel Fedor of Blue Bottle Games' [July 2025 combat preview](https://store.steampowered.com/news/app/1022980/view/503954886346412815); current behavior follows locally inspected 1.0.1.5 code. See the [N3 guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-fire-control.md) for operating limits and evidence. No institutional endorsement or scientific validation is implied.
- Validate offline capability/lifecycle, finite volleys, ownership, persistence, native contracts and panel sizes separately from pending owner-run combat/gameplay evaluation. This prepared version is not an installation or Workshop publication.

## [0.12.1] - 2026-09-25 - Draft

### Polaris startup correction candidate

- Replace the flight hub's Unity JSON reader with the managed parser already used by Framework. Validate embedded region names and dimensions, and identify missing regions explicitly instead of raising an opaque sequence exception.
- Contain failures while constructing our hub so native Polaris loading can continue; detach incomplete hub objects before the native module loader runs.
- When the native Auto Nav package is disabled, skip hub creation and reject equipment registration before publishing definitions or stock additions.
- Add checks against the compiled plugin's embedded layout, malformed layouts and disabled-package registration. The owner reports that reverting 0.12.0 to the previous installed builds removes the Polaris exception; this candidate still requires owner gameplay evaluation.
- Preserve module identities, saved flights, placement keys, recipes and artwork. No save migration, installation or Workshop publication is performed by preparing this candidate.

## [0.12.0] - 2026-09-25 - Draft

### Shared flight hub

- Align header text, tabs and persistent actions with the actual faceplate recesses. Put telemetry and settings in inset display fields with bounded text; use compact contact/operation labels and retain full explanations in Details. Validate designated field containment as well as overall panel fit.
- Reduce the four page-button faces to leave clear margins within their recessed row, retaining readable labels and the larger click targets.
- Replace compact N1/N2 displays with one tall Polaris flight hub per console: Navigation, Pursuit, Systems and diagnostic-only Details. Either healthy module supplies navigation/docking; pursuit and guarded fire require N2.
- Expose routine flight, docking and propulsion controls without scrolling. Keep Resume, Disengage and Cease Fire on every page; Disengage clears thrust and allows coasting, not emergency braking.
- Show qualified range, signed closing speed, relative speed, docking progress, separate offensive target and native weapon readiness. Unavailable/stale measurements are never represented as zero.
- Reuse Blue Bottle Games' native controls by runtime reference through Framework 0.17.0 or newer. Manual propulsion relinquishes automation and retains native core, limiter and no-wake restrictions. Missing guarded fire controls inhibit panel Engage.
- Add the PhobosNavFlightHub layout identity at 25% width by 80% height. Place it through native Edit; existing compact positions do not expand or move other instruments. Preserve physical IDs, recipes and exact flight bindings.

### Combined docking

- Add explicit Approach & Dock and F3 approachdock. Require native clearance and compatible assigned ports, capture their identities, approach 1 km beyond protected hull clearance, match motion and revalidate before existing RCS-only terminal guidance.
- Check handoff after both ships advance. Changed clearance, occupied ports, unsafe motion or an unavailable native docking interface suspend with a reason; no silent retargeting or repeated approach restart.
- Both combined phases suspend after loading for Resume. Manual takeover cancels; contact loss suspends. Preserve native fees, attachment restrictions and completion events. Rendering and page changes grant no docking or firing permission.

### Artwork and validation

- Retain approved compact masters and the prepared 0.11.1 work. Add one reusable slate/graphite faceplate with live labels and controls, a retained 1984 x 3172 AI-upscaled master and a 1200 x 1920 runtime export.
- Record explicit owner-approved local Real-ESRGAN processing, original 992 x 1586 generated sources and hashes. Enhanced texture is inferred, not lossless recovery or native high-resolution generation.
- Check all four routine views at 300 x 480, 400 x 640 and 600 x 960 with long names/expanded labels, plus affected flight, docking, fire, persistence, native-boundary and installer checks. Offline layouts and boundary doubles do not establish in-game readability or native interaction; owner gameplay evaluation remains pending.
- Prepared package only: no installation, publication or binary release. Workshop remains held for upstream provenance review.

### References

- [Flight hub controls, migration and validation](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/auto-nav-instruments.md)
- [Native docking evidence and preserved attachment contract](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-docking.md)
- [Real-ESRGAN by Xintao Wang, Liangbin Xie, Chao Dong and Ying Shan](https://github.com/xinntao/Real-ESRGAN)
- [Pursuit research with NASA, ESA and original researcher attribution](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-pursuit.md)

## [0.11.1] - 2026-09-25 - Draft

### N2 instrument artwork

- Give N2 a separate graphite-blue faceplate with amber trim and twin identification bars. Retain N1 artwork, panel footprint, live labels and existing control positions.
- Reuse Blue Bottle Games' native navigation-module intact/damaged sprites, portraits and normal maps by runtime reference; no game artwork is distributed.
- Reuse the native guarded switch for explicit Engage/Cease Fire and native button artwork for pursuit actions. Cease Fire remains outside the cover; an unavailable native switch uses the existing explicit button fallback. Display refresh does not grant fire permission.
- Require Phobos Framework 0.17.0 or newer for its isolated native switch adapter. Keep saved identities and flight policy unchanged.
- Retain the original generated faceplate, exact prompt and provenance. Artwork and UI interaction await owner in-game evaluation. Workshop publication remains held for upstream provenance review.

## [0.11.0] - 2026-09-25 - Draft

### Pursuit and fire control

- Add Phobos' Asterel N2 Polaris Pursuit Module in the Auto Nav package, with Rendezvous, continuous Follow, separate offensive target and native weapon-group selection, and explicit Engage / Cease Fire. One shared service owns navigation across N1 and N2.
- Replace long acceleration extrapolation with bounded position-and-velocity replanning from qualified relative observations at a shared simulation boundary. Account for turning, native torch delivery and RCS braking handoff.
- Preserve selected aggregate RCS throttle and native torch protections. All turning now uses physical RCS, including configurations that previously requested instantaneous rotation.
- Give navigation/braking priority over firing. Retain native arcs, aiming time, manual/defensive settings, ammunition, jams, reload, projectile lead and consequences; prevent duplicate automatic salvos against pursuit targets.
- Add a checked hold/retreat phase for manoeuvring docking targets. Dock remains a separate native-clearance-checked RCS command.

### Economy and saves

- Add the 0.4 kg N2 at an authored $5,400 base value, a 60% pristine Polaris merchant offer and a 30-minute two-electronics construction recipe retaining 0.6 kg offcuts. Repair, Restore and dismantling reuse the existing board-material contracts.
- Preserve N1 identities, salvage chances and ordinary flight preferences. Save N2 mission intent and console weapon-group preference; always suspend pursuit and docking on reload. Never save prediction history, aim or automatic-fire permission.
- Add new native N2 item identities; avoid removing or downgrading the provider from saves containing them. No new mandatory mod dependency.

### Validation and limits

- Offline comparisons cover accelerating, reversing, crossing and burst-dodging targets; aggregate RCS thrust reversals fell from eight to four in the specified benchmark. The individual reversing case rose from two to three. Follow mean separation error was about 14 m at 1 km in the tested burst scenario.
- Check shared velocity/gravity, reversed update order, propulsion restrictions, sensor loss, saved suspension, native clearance and explicit firing interlocks. Numerical boundary doubles are not gameplay validation or measured fuel savings.
- No general obstacle avoidance, guaranteed catch, intact boarding guarantee or automatic docking after combat. Native missile fire requires the open native display and its completed lock. Automatic fire is held above a one-second simulation step.
- Package remains an unpublished draft held for upstream provenance review. Reuse approved casing artwork; gameplay and N2 layout await owner evaluation.

### References

- [Pursuit operation, benchmark conditions and implementation](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-pursuit.md)
- [Zhou, NASA Langley: manoeuvring-target relative feedback (2012)](https://ntrs.nasa.gov/citations/20120014309)
- [Hartley, Trodden, Richards and Maciejowski: ESA ORCSAT rendezvous control (2012)](https://eprints.whiterose.ac.uk/id/eprint/90483/1/orcsatpaper_final.pdf)
- [ESA: ATV Jules Verne checked approach, holding and retreat](https://www.esa.int/Enabling_Support/Operations/ATV_i_Jules_Verne_i)
- [Auto Navigate by Gravy / mrkmg](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
- [Authorship, game evidence and licence limits](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)

## [0.10.1] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Choose cruise speed, arrival speed and stopping distance at each console.
- Use live native sensor contact, torch preference with RCS fallback, and bounded RCS throttle.
- Preserve flight intent and captured preferences across saves, subject to validation before resuming.
- Obtain the module through merchants, construction or rare native module salvage.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.15.0 or newer. Original Auto Navigate is not a dependency and must be disabled for Phobos Auto Nav to engage.

### Known limits

- Stop / Coast clears thrust; it is not emergency braking. Fly stops short; Dock is a separate action.
- No obstacle avoidance or continuous relative-position holding. Sensor loss suspends guidance.
- Docking suspends after reload. Ordinary flight restoration follows its own validation and preference policy.
- Earlier guidance has owner-reported gameplay success; current features and integration need further evaluation.
- Upstream-derived guidance reuse terms remain unresolved. This draft is held for provenance review before Workshop publication.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/auto-navigate-adaptation.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
- [Auto Navigate by Gravy / mrkmg](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
