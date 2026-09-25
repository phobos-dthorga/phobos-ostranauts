# Phobos Agriculture changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Fixed

- Fixed unreachable INSTALL entries; Firstlight-4, Hearth-2 and W2 appear under APPS, and irrigation conduit under MISC, including damaged forms. Existing inputs, placement and saved IDs are preserved.

## [0.7.0] - 2026-09-25 - Draft

### Added

- Separate lettuce seed-production planting cycle: 96 default game hours, finite feed and power, four seed packets plus residue at ideal harvest. Food-lettuce and potato budgets remain unchanged.
- Matching seed-production nutrient solution and two original PixelLab flowering/seed-head layers, with retained masters and provenance.

### Changed

- Newly queued W2 treatment spends finite cartridge medium per kilogram of drainage and returns unused capacity with proportional mass/value. Existing bound jobs preserve their original whole-cartridge contract.
- Pipe repair now uses one aluminium scrap; W2 repair includes mechanical/electrical parts and aluminium. Revised new repair timings and generated equipment/service comparisons include W2 and pipes.

### Limits

- Accelerated seed cycles, immediate seed readiness, yields and treatment capacity are authored gameplay. No edible leaves from seed cohorts, nutrient manufacturing, perfect recovery or terminal-reject recycling.
- Offline build/accounting/persistence/native checks pass; owner gameplay, trade and appearance evaluation remains pending. No Steam release is implied.

## [0.6.2] - 2026-09-25 - Draft

### Added

- Agriculture planting stock and food can appear through native fridge contents; seeds, nutrients, irrigation charges, treatment cartridges and loose pipes can appear through locked-crate contents and its bulk-cargo uses.
- One optional extra item per eligible contents roll: 22% total fridge chance and 30% crate chance by default. Existing native and other-mod rewards remain intact.
- Add Loot.Enabled and Loot.ChanceMultiplier settings (0 to 3, default 1; restart required). Settings affect future loot generation, not merchants or already saved inventories.

### Limits

- These are shared native pools, not guaranteed finds or a promise that every fridge uses them. No existing container refill, large machinery, spent waste or artificial measured-drainage records are added.
- Prices, recipes, crop yields and saved item identities are unchanged. Artwork from 0.6.1 remains included; owner in-game loot and appearance checks remain pending.

## [0.6.1] - 2026-09-25 - Draft

### Changed

- Replace borrowed stock graphics with twelve original PixelLab sprites for planting stock, nutrients, irrigation water, produce, cooked potatoes, crop residue, process solutions, treatment rejects and recovery cartridges.
- Use matching world and portrait art with registered neutral normal maps and unchanged native item geometry. Commodity identities, quantities, prices, food actions and crop rules are unchanged.
- Preserve original masters, generation prompts and reproducible native-size exports. Artwork has been inspected offline; in-game appearance remains pending owner evaluation.

## [0.6.0] - 2026-09-25 - Draft

### Added

- One W2 can explicitly supply up to eight compatible racks using one shared pump budget.
- Irrigation routes retain finite parcels and apply authored resistance and transit delays; broken routes keep their contents.
- New recorded drainage can be treated with finite cartridges and measured electricity, retaining all losses as terminal rejects. Legacy waste is not reassayed.
- Existing crop, water and dry nutrient saves remain compatible; gameplay evaluation remains pending.

## [0.5.0] - 2026-09-25 - Draft

### Development baseline

- Include the required native data directory so Ostranauts recognizes Agriculture instead of reporting a missing mod folder.
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
