# Phobos Shipbreaker changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

No additional changes recorded.

## [0.17.0] - 2026-09-25 - Draft

### Added

- Route exact aluminium feed from a separate R4 metals output to the F6. Stop at twenty pieces; receiving never starts or releases a batch.
- Collect released, cold housing blanks and melt remainders with an explicit furnace-products collector filter. Existing residue routes, default filters and tray limits retain their meaning.
- Account furnace feed motors through the existing electrical receipt, including partial supply and retained motor heat. Preserve cargo on full destinations and pause receiving on reload or failed interlocks.
- Expose material pairing and receiving through local panels, C1 and F3, with rotated front-corner installation markers.
- Add optional finite coolant servicing: fill/drain and captured leakage, with a conservative lumped heat model. Existing sealed cooling installations remain supported.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.20.0 or newer. Agriculture and Auto Nav remain optional.

### Known limits

- Prepared candidate; owner gameplay and appearance checks remain outstanding.
- Individual aluminium feed only; crew separate native stacks. No automatic Seal, Start, Equalize or Release.
- A housing blank fills the existing collector grid; haul it away before the next item can fit.
- No new finished-part recipe, arbitrary container routing or generated artwork. Manufacturing remains separate.

### References

- [Furnace routing guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/furnace-material-routing.md)
- [Cooling scope](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/furnace-coolant-conduits.md)

## [0.16.0] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Process manually supplied detached wall panels; connect a grabber, wall chute and indoor dismantling fixture.
- Recover steel and aluminium from identified processing residue, retaining finite rejects.
- Pair collectors and material routes explicitly; manage machines through local Control Panels, C1 or F3.
- Cast aluminium housings with the electrically heated F6 furnace and finite cooling.
- Choose existing direct exterior or underside cooling, or optional F6-C sealed conduits to a remote F6-R radiator.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.19.0 or newer. Auto Nav and Agriculture are optional. No Crafting Framework or Salvage Workshop dependency.

### Known limits

- Experimental candidate; current machinery and artwork still need in-game evaluation.
- No attached-hull cutting, persistent cargo ejection or perfect recycling. Recipe yields are authored gameplay budgets.
- Processing and receiving are separate permissions and pause after reload. Saved links are retained.
- Coolant conduits use a simplified sealed thermal circuit, not transferable water or a fill/drain/leak simulation. Existing direct installations retain their saved meaning.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/player-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
