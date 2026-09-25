# Phobos Auto Nav changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

No additional changes recorded.

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
