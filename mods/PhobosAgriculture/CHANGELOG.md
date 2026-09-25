# Phobos Agriculture changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

No additional changes recorded.

## [0.5.0] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Visible crop stages track the saved crop cohort in the four-tray rack.
- Crew load finite supplies, plant, harvest and maintain equipment; automatic controls support the growing environment.
- Use finite manual water and nutrients, or an optional Groundwork W2 supply unit and placed irrigation conduits.
- Mix crop-specific nutrient solution in the W2 and deliver water and nutrients through the same supported circuit.
- Buy or construct equipment through ordinary acquisition routes, with repair, dismantling and retained waste.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.19.0 or newer. Shipbreaker C1 access is optional. The Ship's Water adapter is scoped to inspected version 0.16.1; manual supply remains available.

### Known limits

- Experimental candidate; the complete growing/cooking loop still needs in-game evaluation.
- Four trays represent one crop cohort, not four independent crops or four times the yield.
- One W2 serves one rack per connected circuit. Receiving and pumping require explicit controls and pause after reload.
- Short growth cycles, crop budgets, nutrient mixtures and simplified chemistry are authored gameplay choices, not research results.
- No perfect nutrient recycling or unlimited water. Drained solution remains retained waste.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/agriculture-player-guide.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
