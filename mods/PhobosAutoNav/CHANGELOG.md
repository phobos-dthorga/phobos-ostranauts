# Phobos Auto Nav changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

No additional changes recorded.

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
