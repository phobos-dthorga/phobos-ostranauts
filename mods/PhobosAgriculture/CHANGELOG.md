# Phobos Agriculture changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Point the player guide to current dependencies and explain Maintenance information. Distinguish original loot/roadmap milestones from current installation advice. Documentation only; gameplay and saves are unchanged.

- Include every Agriculture item in the complete action audit. Document why retained crops, fluids, jobs and R3 links can hide removal, and why loose conduit stacks require individual pieces for dismantling. No gameplay change is claimed.

- Added source-backed bulk-storage calculations for 54 farm scenarios, the R3 water-reservoir and larger nutrient-charge proposal later delivered in 0.14.0, and a local vanilla-art comparison workflow. Existing machinery, recipes, versions and installed files are unchanged; no production artwork was generated.

- Research crop-residue nutrient recovery, optional Ship's Water reject capture, a proposed Groundwork workup bench and non-repairable progressive W2 mixtures. These were implementation plans at the time; the 0.9.0 entry below delivers recorded residue recovery, the B2 workup bench and optional reject capture.
- Expand the native economic audit with electricity sensitivity, repeating seed-production costs and bounded illustrative residue recovery; runtime prices and yields are unchanged.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Fixed unreachable INSTALL entries; Firstlight-4, Hearth-2 and W2 appear under APPS, and irrigation conduit under MISC, including damaged forms. Existing inputs, placement and saved IDs are preserved.

## [0.21.0] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 2. Irrigation routes come from Framework's shared topology snapshot and are computed once per power step for both the demand check and the pump; the second-source check on a circuit is a membership lookup instead of one search per other W2. The route identity is hashed once per distinct path. Machine records are written only when a value changed. The interaction hooks recognise our machines and supplies by set lookup before searching action names. The two-second world scan keeps its cadence and scope but allocates nothing for objects that are not ours. Ship's Water refills skip zero requests.
- Recorder scopes agriculture.irrigation.route and agriculture.saves.

### Compatibility and limits

- Requires Phobos Framework 0.45.1 or newer. A pipe laid or cut is noticed within two real seconds rather than on the same step. Saved data is unchanged.

## [0.20.0] - 2026-09-29 - Draft

### Added

- Phobos' Verdemorrow Groundwork R4 (4 x 4, 235 kg, 635 cr) and R5 (5 x 5, 400 kg, 830 cr) Agricultural Water Reservoirs, the medium and large sizes of the R3 (owner direction: every bulk family comes in three sizes). They work like the R3, hold more for less per kilogram of capacity, and are purchase-only.
- A W2 now draws from any water vessel within one tile: a reservoir of any size, or a Shipbreaker S3, S4 or S5 process water silo. The original R3 layout still qualifies.
- Two PixelLab sprites in the R3's colours.

### Changed

- Reserve steps and the station water quote follow the chosen reservoir's size; the R3's are unchanged. The station offer reads Agricultural water (Groundwork reservoirs).

### Compatibility and limits

- Requires Phobos Framework 0.44.0 or newer. Existing R3 reservoirs keep their identity, water, records and links.

## [0.19.0] - 2026-09-29 - Draft

### Changed

- The W2 water supply, B2 workup bench and R3 reservoir gain the Firstlight-4 and Hearth-2's second-hand routes: lightly worn at the K-Leg fixer, refurbished and broken at the Venus Orbital scrap kiosk. R3 offers come in lots of four, the others in lots of eight.

### Compatibility and limits

- Framework 0.39.0 is still the minimum. Prices, bills, salvage and saves are unchanged, and the R3 stays purchase-only. New offers appear at normal restocks; existing shop inventories are not refilled. See the [economy coverage audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/economy-coverage-audit.md).

## [0.18.0] - 2026-09-29 - Draft

### Changed

- The R3 reservoir's water custody moved into Framework 0.39.0's shared bulk vessel service. Records, journals and the transfer guard keep their existing names, so a saved R3 reads unchanged; the mode-switch carry-over and destruction log are now Framework's, and Agriculture only pauses the paired W2 on damage.
- A Shipbreaker T2 ice thaw unit within one tile of an R3 can deliver its thaw water into the reservoir (linked from the T2). Agriculture's own water rules, W2 pairing and station offers are unchanged.

### Compatibility and limits

- Requires Framework 0.39.0. No save migration; no gameplay or balance change on Agriculture's side. Offline checks pass; owner play-testing is pending.

## [0.17.0] - 2026-09-28 - Draft

### Fixed

- Machines now have a real power state. The game only maintains IsPowered for equipment whose power info names a power-on action, which ours never did, so every running machine read as blocked on the crew console. Every installed appliance keeps its 20 W idle draw, so the state follows the actual connection; that draw is machine heat, not lamp energy.
- Without Ship's Water, every Agriculture panel used to open as the recycler capture panel, because the game answers an unknown rule name with its always-true fallback. The capture panel now needs Ship's Water and its real recycler rule, and Ship's Water recycler definitions are amended in place rather than republished under their own names.
- Stacked supplies work. The game stacks matching seeds, nutrients, water rations and raw potatoes dropped into a machine; planting, loading and cooking now take one unit from a stack instead of refusing the whole stack.
- Water tanks follow the game's own destructibility (owner decision). Cargo-holding tanks no longer block native destruction, detachment or mode switches at the moment they happen, which used to orphan the replacement the game had already created; refusals happen only when maintenance work is offered. Water lost with a destroyed tank is logged.
- Meals and lettuce keep their authored food values through the game's own eating chain: each has an identity condition and its own eating reply, cloned from the vanilla one and listed ahead of it, instead of a hook that swapped effects at the last moment. Meals and lettuce made before this version eat with the vanilla values.
- Work actions are refused when they are offered, not half an hour later: an occupied rack, a missing seed, a full reservoir, nothing to harvest or drain, a protected or damaged machine. A refused maintenance finish still closes the game's task for it.
- Transpired water no longer vanishes. The game's air has no water vapour, so the racks' humidity emission was silently discarded; transpired water now condenses back into the rack's reservoir while it has room, and the crop's water use drops by that amount.

### Added

- Accept contents as they are: a protected machine or tank whose records are readable offers one action (panel, C1 console or F3) that closes interrupted-transfer journals and sets the item's mass back to what the records say. Unreadable records still need repair.

### Compatibility and limits

- Requires Framework 0.37.0. Saved identities and records are unchanged; new identity conditions apply to food made from now on. Offline checks pass; owner play-testing of power state, stacked supplies, tank destruction and eating is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.16.1] - 2026-09-28 - Draft

### Added

- Maintenance information explains retained crops, food, water/solution, active jobs, protected state and R3 tank links.

### Fixed

- Replace broad removal refusals with specific current blockers; native and completion-time safety checks stay in force.

### Compatibility

- Requires Phobos Framework 0.31.0. No crop, process, yield, inventory or save-format change. Unity interaction remains owner-tested.

## [0.16.0] - 2026-09-27 - Draft

### Changed

- Raise equipment offers to at least 85% and supplies to at least 95%, before the availability setting. Fill missing local stock, add regional produce/meal offers and Hearth meals to native food sellers. Engineering loot gains a 30% single-item choice across five intact/damaged equipment families; existing fridge/crate supply choices remain finite.
- Applies to future native stock and loot generation; no forced restocks, saved-cargo changes, price changes or live gameplay validation. See [merchant availability and salvage](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/merchant-stock.md).

## [0.15.3] - 2026-09-27 - Draft

### Fixed

- Restore native pickup/drop on loose appliances and conduit parts; remove stack actions from single items. Large Firstlight rack and R3 reservoir housing remains use cumbersome handling. Portable supplies and native food actions remain available.
- Remove operating-work entries from damaged R3 reservoirs. Saved cargo remains intact, with legacy hand placement preserved until release. Requires Phobos Framework 0.30.2.
- Audited all 118 implemented item definitions and checked native action/slot contracts offline. Live menus and loaded inventory handling still require owner testing; see the [item handling audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/item-handling-audit.md).

## [0.15.2] - 2026-09-27 - Draft

### Fixed

- Use allocation-free bulk-tank family matching and shared presentation pacing. Keep crop, cooker, irrigation and passive-scan cadence unchanged.
- Add disabled-by-default Scope timings for discovery, machine updates and panel refreshes; retain the existing change-only growth artwork selection.

### Compatibility and limits

- Requires Phobos Framework 0.29.0. Saved state, water, nutrients, power, heat, crop yields and recipes are unchanged.
- See docs/development/performance-audit.md for the source review and baseline evidence. Further captures are deferred by owner direction; measured speedup and Unity interaction are unverified.

## [0.15.1] - 2026-09-27 - Draft

### Documentation

- Review English controls, warnings, descriptions and help for practical player language; retain precise diagnostics and established equipment names. Update current guides, item-reference inputs and the Workshop draft.
- Follow the retrospective language rule and glossary in docs/development/player-language.md, informed by Blue Bottle Games' official Ostranauts description and Daniel Fedor's developer AMA. This is an interest-based audience interpretation, not measured demographic data.

### Compatibility and limits

- Wording only: translation keys, placeholders, commands, saved identities, resource values and gameplay rules are unchanged. This is an unpublished development candidate; Unity text layout remains unverified.

## [0.15.0] - 2026-09-27 - Draft

### Artwork

- Add dedicated overhead sprites for recorded crop residue, recovered concentrate, makeup salts, progressive mixture, spent biomass, Recycler wet rejects and the bulk nutrient charge.
- Add damaged, packed and damaged-packed artwork for Firstlight-4, Hearth-2, W2 and B2. Installed rack damage preserves all eighteen crop-stage overlays; packed racks show their protective covers. Existing cooked-potato and other approved stock artwork is retained.

### Compatibility and limits

- Require Framework 0.28.0 for shared native state-art binding. Existing identities, footprints, sockets, contents, stack limits and recipes are preserved. Neutral normal maps supply no authored relief. Offline image and definition checks do not establish Unity or owner gameplay approval.
- Retain original masters, exact PixelLab prompts, job IDs and export hashes in the [artwork provenance record](https://github.com/phobos-dthorga/phobos-ostranauts/tree/main/assets/artwork-completion). Prepared only; no installation or publication.

## [0.14.1] - 2026-09-27 - Draft

### Changed

- Treatment cartridges, 500 g bulk nutrient charges and 5 kg irrigation charges stack to 3 each. Separate one object before treatment, dosing or water loading. Cartridges retain individual remaining capacity; one treatment batch must still fit one cartridge, without pooling capacities.

- Loose intact and damaged irrigation conduits and seed potatoes stack to 10; lettuce seed packets to 50; formulated nutrient and makeup-salt packets to 25.
- Raw potato portions, Hearth cooked potato portions and lettuce portions also stack to 10 each. Separate an individual raw potato portion before cooker processing; per-portion mass, food effects and recipes are unchanged.

### Compatibility and limits

- Split seeds and nutrient packets into individual items before manual planting, loading, dosing or B2 workup. Existing crew source hauling can deliver individual units from stacks. Partial charges retain their own state; stacking does not refill or combine their contents.
- Native stack limits apply on the ground and in compatible containers. Existing IDs, per-item mass, value and recipes are preserved; existing items use current definitions on reload, without automatically consolidating stored cargo. Offline checks do not establish gameplay validation.

## [0.14.0] - 2026-09-27 - Draft

### Agricultural bulk supplies

- Add the passive 3 x 3 Groundwork R3 reservoir: 120 kg agricultural water, 25 kg empty, one explicitly paired adjacent W2, finite catch and protected reserves. W2 retains one shared power/throughput budget and treatment still pauses distribution.
- Add a physical 500 g Groundwork nutrient charge, gradual selected dosing and optional approved-store crew replacement. R3 crew orders refill from 5 kg irrigation charges; old orders retain their meaning.
- Add station water purchasing at 10 cr/kg in quarter-kilogram steps and single nutrient-charge purchases, with exact destinations, fresh validation and protected settlement evidence. Native refuelling and Valtora’s Ship’s Water 0.16.1 controls remain independent.
- Add original overhead R3 intact/damaged art and native-size exports; loose forms reuse the matching chassis and nutrients reuse existing original packet art. No native game images are distributed or submitted to generation.
- Require Framework 0.27.0. R3 starts empty; saves retain contents/identity, W2 resumes explicitly. Filled or uncertain individual reservoirs block detach/destruction; whole-ship loss remains native. No chemical hazard simulation or automatic crash recovery is claimed.
- Update item/INSTALL/economy/player references and retain attribution to Bruce Dunn/OSU, NASA porous-tube research and Jay Garland/Bionetics NASA TM-107557. Capacities, prices and simplified chemistry are authored gameplay choices. Owner Unity evaluation remains pending.


## [0.13.1] - 2026-09-27 - Draft

### Panel corrections

- Require Framework 0.26.1 for compact control corrections, readable roster access, stable diagnostics and the visible ship picker.
- Bind picker lines to the originating agricultural machine and disable Locate/Clear for absent water, collector and nutrient-charge connections. Missing saved selections remain retained and clearable.
- Keep Apply confirmations visible during live refresh instead of replacing them with an older blank notice.
- Keep crop artwork, recipes, manual operations, draft validation and local access restrictions unchanged. Prepared offline; Unity evaluation remains owner-run.

## [0.13.0] - 2026-09-26 - Draft

### Control panels

- Grouped local machinery controls into Operation, Supplies & connections, Details and standing-order access using Framework 0.26.0.
- Kept crop-stage artwork and manual planting, harvesting, draining and workup actions. Connection and dosing choices use checked Apply forms; current selections are retained when supplies disappear.
- B2 standing orders expose process, stock target, approved input/output stores and routine resume without unrelated crop-clearing, drain or exterior mission controls. Recipes and resource accounting are unchanged.

## [0.12.1] - 2026-09-26 - Draft

### Fixed

- Encode an absent dosing selection and idle or supplement-free workup state with explicit empty-state markers accepted by Framework's save wrapper. This fixes the Protected dosing binding fault and the related Protected workup state fault.
- Preserve exact selected input identities and paid workup energy. Unknown, corrupt and foreign records remain protected; no saved cargo, crop or material record is rewritten as recovery.
- Add save-wrapper roundtrip coverage for idle equipment, recovery and formulation. Restart and reload after updating; gameplay validation remains owner-run.

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
