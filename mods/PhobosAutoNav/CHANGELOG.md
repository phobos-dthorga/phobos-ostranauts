# Phobos Auto Nav changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Research and planning

- Earlier capture-only research is superseded for supported ordinary walls by the implementation below. Broader structural recipes, repeated furnace batches and whole-wreck completion remain outside this round.
- Keep [NASA Goddard's Raven research](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/) and [ESA's LIRIS experiment by Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before) as sensing context, separate from Blue Bottle Games native evidence and authored gameplay choices. No institutional endorsement or gameplay validation is implied.

## [0.18.0] - 2026-09-26 - Draft

### Added

- Add shared local obstacle avoidance to Fly, Rendezvous, Follow, docking and industrial approaches. Admit native sensed contacts before reading geometry; predict motion, retain passing side, bound route search and check the final swept path. Blocked routes brake/hold; lost tracking suspends for explicit Resume. Known celestial regions are excluded; field markers are not individual obstacles.
- Add explicit Undock & Depart and Undock & Continue for one exact orbital station/ship/mooring connection. Require crew, sealed boundaries, native clearance, working RCS and an admitted exit; retain station neighbours. Depart to a 1 km hull gap and relative stop, with the destination/mode captured before detachment. Ordinary Fly never disconnects.
- Extend IndustrialNavigation additively with egress, transit, capture approach, route cost and typed status for Shipbreaker. Manual takeover has priority. Departure and close industrial legs use RCS; torch burns require an admissible burn and braking corridor.

### Persistence and limits

- Store departure intent and detachment journals separately on the console. Reload never replays live thrust or blindly repeats a pending native mutation. Ground stations, ambiguous groups and secured towing are excluded.
- Retain N1/N2 identities, artwork, existing flight preferences and Framework 0.24.0 dependency. Local avoidance is not a promise about hidden contacts or long-distance voyage planning. Existing upstream attribution and binary-distribution hold remain.
- Add the [departure guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-departure.md) and [offline validation record](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-reclamation-validation.md). Builds and synthetic/native-boundary checks are not owner gameplay validation; Steam publication remains pending.

## [0.17.0] - 2026-09-26 - Draft

### Changed

- Increase successful local and regional N1/N2/N3 offers to wholesale board lots, including the existing worn, refurbished and broken offers. Uses Framework 0.24.0; unit prices, rare N1 salvage and saved flights are unchanged.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.16.1] - 2026-09-26 - Draft

### Fixed

- Replace the generic native-automation warning with the actual blocker: native pilot, saved engagement switch, station-keeping, held thrust, torch request or waypoints. Disengage is available for local native control intent even without a Phobos flight record. Explicitly release the three standard native autopilots, clear local console switches and cut torch/RCS thrust while preserving reactor operation, velocity and spin. Other flight plugins and unrelated AI remain protected.
- Keep the flight hub behind native overlays and hide its surface/input on the Rescue screen; Done restores the existing page and placement. Native layout Edit still blocks operational controls. Track help identifies its N2 requirement; Resume and Cease Fire retain their operation-specific availability.

### Validation and limits

- Regression checks cover stale native switches, two local consoles, manual torch request, standard native pilots, foreign-console and independent-controller rejection. Build and offline checks do not validate Unity clicking or live flight. [Hub owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-hub-validation.md) remain required. Prepared candidate; no Steam publication.

## [0.16.0] - 2026-09-26 - Draft

### Added

- Narrow industrial working-pose service for the selected N1/N2 console/module and target: exclusive RCS guidance, fresh readiness observations and permission-specific release. Existing native tracking, fuel, power, motion and step limits apply; other flight/aiming controllers cannot compete.
- Tool-facing terminal guidance for four mounting orientations, closed-panel operation, immediate manual takeover and industrial status in the navigation panel. Reload discards industrial flight authority and never replays saved thrust.

### Limits

- Shipbreaker 0.22.0 uses this service for native capture/release. It does not provide cutting, automatic hull traversal or repeated downstream jobs. See the [capture guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-capture.md).
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

- [Flight hub controls, migration and validation](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-nav-instruments.md)
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

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/auto-navigate-adaptation.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
- [Auto Navigate by Gravy / mrkmg](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
