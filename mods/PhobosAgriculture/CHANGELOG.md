# Phobos Agriculture changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Research crop-residue nutrient recovery, optional Ship's Water reject capture, a proposed Groundwork workup bench and non-repairable progressive W2 mixtures. These are implementation plans, not delivered recipes or integrations.
- Expand the native economic audit with electricity sensitivity, repeating seed-production costs and bounded illustrative residue recovery; runtime prices and yields are unchanged.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Fixed unreachable INSTALL entries; Firstlight-4, Hearth-2 and W2 appear under APPS, and irrigation conduit under MISC, including damaged forms. Existing inputs, placement and saved IDs are preserved.

## [0.12.0] - 2026-09-26 - Draft

### Crew automation

- Added standing work for selected crop cohorts, finite replenishment, harvest/replant, potato cooking, nutrient workup, configured water routes, recorded drainage recovery and approved storage.
- Added Agriculture and Cooking training, novice eligibility and qualified-worker preference. Crop growth, chemistry and material yields are unchanged.
- Preserved source seed stock and configured crew-water reserves. Clearing unwanted living crops and draining usable solutions require separate permission.
- Added routine resume-after-load selection and supported onboard time-skip accounting through Framework 0.25.0.

### Compatibility and limits

- Requires Framework 0.25.0. Ship’s Water and Shipbreaker remain optional. No new recipe for uncharacterized wet rejects; no gameplay validation or Steam publication.
- See [crew controls, sources and owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/crew-automation.md).

## [0.11.1] - 2026-09-26 - Draft

### Fixed

- Recycler attachment controls show the live status once after repeated actions and omit the unused crop portrait that appeared as a white square. Other Agriculture panels also suppress echoed successful status while retaining distinct action notices and errors.
- Use Framework 0.24.2's shared feedback rule; hide the portrait slot on actual Agriculture machinery if its artwork fails to load, avoiding another empty white square.
- Optional Recycler capture requires a complete two-tile edge alignment, including quarter-turn rotations, and rechecks it before reserving waste. Shipbreaker 0.24.1 adds floor mounting with a clear service walkway. Existing saved links and cargo remain intact; misaligned pairs wait for repositioning or explicit Unlink.

### Compatibility

- Applies to the optional adapter for [Valtora's Ship's Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189). Payload limits, exclusive pairing, measured waste and pause after reload are unchanged. Prepared and checked offline; owner gameplay confirmation remains pending.

## [0.11.0] - 2026-09-26 - Draft

### Changed

- Increase successful local and regional offers to wholesale lots for machinery, irrigation conduit and consumables, including W2/B2, seeds, nutrients, water charges, treatment cartridges and makeup. Uses Framework 0.24.0; prices, rare fridge/crate loot and existing inventories are unchanged.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.10.0] - 2026-09-26 - Draft

### Added

- Cultivation, cooking, irrigation and nutrient-workup machinery, planting stock, nutrients, root-water charges, cartridges, makeup and pipes gain bounded offers at 15 additional placed vanilla retail markets. Food-producing centres receive higher authored availability. Makeup receives the industrial category; terminal biomass/rejects receive trash. Recorded intermediates retain their separate meaning. Base values, cohorts and fluid records are preserved.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Requires Phobos Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

## [0.9.0] - 2026-09-26 - Draft

### Added

- Groundwork B2 Workup Bench with original registered artwork, native APPS placement, construction, stock, service and finite powered jobs.
- Fresh crop-residue nutrient records, partial concentrate recovery, retained spent biomass and equal-mass purchased makeup formulation. Old cohorts and old residue keep their uncharacterized contract.
- Explicitly selected W2 inventory charges deplete physical mass and value only during paid blending. Remaining grams/percent are visible; Repair and Restore cannot refill consumables. Existing dry inputs remain supported.
- Optional Recycler-to-Residue-Collector attachment for [Valtora's Ship's Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189), requiring Shipbreaker 0.20.0. Finite wet rejects use measured tank changes; full/unavailable destinations stop linked processing before source consumption. Reload pauses permission.

### Balance

- B2 costs 250 cr; makeup salts cost 30 cr per 40 g. Recovery uses 60 percent of the recorded residue nutrient allocation; makeup contributes equal finite mass. Each stage uses 0.02 kWh/kg input with a 0.001 kWh minimum and one-minute crew setup. Existing crop budgets are unchanged.
- These are authored aggregate chemistry and economic choices, not scientific extraction yields. [Jay Garland's NASA TM-107557 (1992)](https://ntrs.nasa.gov/citations/19930008922) supports separating recovered fractions from complete formulations; neither NASA nor the author endorses this equipment or balance.

### Requirements

Phobos Framework 0.22.0 or newer. Crop recovery works without Shipbreaker or Ship's Water. Recycler waste has no nutrient provenance and no fertilizer recipe. Unknown records and interrupted commits remain protected; gameplay, visuals and live provider hooks await owner testing.

### Documentation

- Updated the economic audit, item reference and [operating guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/agriculture-nutrient-production.md), including actual controls, material budgets, optional dependency limits and artwork provenance.

## [0.8.0] - 2026-09-25 - Draft

### Added

- Optional one-shot watches for a committed meal or a whole crop cohort becoming harvest-ready, including seed crops. Local controls, C1 and F3 share the actions; no individual growth-stage or irrigation sounds.
- Use Framework's shared quiet completion cue and volume/mute. Pause, fault or reload clears watches; blocked delivery is not success. Results remain visible when muted.

### Requirements

Phobos Framework 0.21.0 or newer. Existing crops, recipes and save contracts remain unchanged. Gameplay/listening evaluation remains pending.

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
