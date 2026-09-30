# Phobos Shipbreaker changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Documentation

- Prepare four construction sprites: D4 and R4 intermediate stages, plus early and intermediate F6 stages. Retain original frames, source images and repeatable exports. At preparation time these assets were not yet connected to construction progress or included in game packages; Framework 0.32.0 and Shipbreaker 0.32.0 below bind them to D4, R4 and F6 assembly stages.

- Correct furnace, assembly, coolant-service and cargo-recovery instructions. Distinguish historical console/reclaimer designs from current controls and repair generated section placement descriptions. Documentation only; gameplay and saves are unchanged.

- The initial read-only audit identified inaccessible F6-P/F6-R inventories, the split-stack requirement and D4-S cumbersome handling. The 0.31.0 follow-up below implements recovery, clearer maintenance explanations and construction-site assembly; the original audit alone made no gameplay changes.

- Document the third room-load recurrence: the grid guard trusted dimensions recorded before native save trimming. Framework 0.24.1 corrects validated stale headers before padding and synchronizes future outgoing saves after trimming. The earlier marker-health fix was owner-confirmed; the new before/after archive checks are offline, with gameplay confirmation pending.
- Document the wear-related save-load recurrence and Framework 0.23.1 correction. Native marker rejection could remove the pending G4/H4 before Shipbreaker's grid guard ran; retain the existing grid protection and update Framework. The earlier successful owner test remains valid for its original save, not proof of all later reloads.
- Correct the F6 operating guide's direct versus powered-pipe cooling behavior, circulation/feed demand and damaged-pump limits. Point current-version checks to maintained references rather than historical installation notes.
- Clarify that Manufacturing and autonomous G4 reclamation are separate implementation tracks; current material receipt does not authorize repeated furnace batches or replace motion interlocks.
- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Research and planning

- Earlier capture-only research is superseded for supported ordinary walls by the 0.24.0 reclamation implementation below. Broader structural recipes, repeated furnace batches and whole-wreck completion remain outside this round.
- Keep [NASA Goddard's Raven research](https://www.nasa.gov/general/nasas-hybrid-computer-enables-ravens-autonomous-rendezvous-capability/) and [ESA's LIRIS experiment by Airbus, Jena Optronik and Sodern](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/ATV/ATV_views_Space_Station_as_never_before) as sensing context, separate from Blue Bottle Games native evidence and authored gameplay choices. No institutional endorsement or gameplay validation is implied.

## [0.47.0] - 2026-09-30 - Draft

### Changed

- The S3 process water silo's capacity and empty weight and the Y2 material bin's empty weight and cells per tile now live in framework/vessels.json, read through Phobos Framework with player override files in BepInEx/config/PhobosShipbreaker/vessels. The shipped figures are unchanged. Editing the silo leaves the silos you own waiting for Accept rather than changing their contents; the S4, S5, Y3 and Y4 follow the small entry.

### Compatibility and limits

- Requires Phobos Framework 0.51.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.46.0] - 2026-09-30 - Draft

### Changed

- The F6 furnace recipes with their heat profiles, the T2 thaw recipes and the R4 reclaimer budget now live in framework/process-recipes.json, and the housing stock, ingots, remainders and reject packets in framework/materials.json, read through Phobos Framework with player overrides in BepInEx/config/PhobosShipbreaker. Every value is the same as 0.45.0; nothing changes in play. Shipped recipe revisions are frozen: a batch or thaw keeps its revision, and a changed recipe means a new revision. The D4 hand-disassembly budgets are formulas over part mass and stay in the mod.

### Compatibility and limits

- Requires Phobos Framework 0.50.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.45.0] - 2026-09-30 - Draft

### Added

- The T2 Ice Thaw Unit breaks down methane ice. The game's methane ice is read as methane hydrate (about six water molecules per methane, after Circone et al. 2005 of the US Geological Survey), so each 24.84 kg block gives 19.89 kg of water to the linked silo or reservoir, 2.95 kg of methane to a linked Phobos Manufacturing methane store, and the usual 2 kg of ice gangue. A block takes 50 minutes at 6 kW; breaking the hydrate takes 54.2 kJ per mole of methane (Handa 1986, National Research Council of Canada).
- Send methane to on the T2's panel and the C1, shown once a methane store is within one tile. Methane never goes into the air: without a linked store, methane ice waits in the feed while water ice behind it still runs, and crew only bring methane ice once a store is linked.

### Changed

- The game's methane ice is now valued at 250 instead of 20, so breaking a block down still loses money, as every Phobos process does. The correction is made in place; blocks already in a save keep their old price.

### Compatibility and limits

- Methane ice needs Phobos Manufacturing for its methane store; without it the T2 works as before. The hydrate reading, the gangue share and the warming allowance are our choices. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.44.0] - 2026-09-30 - Draft

### Added

- Water ice for the T2 is easier to find. The game has its own ice asteroids (ice walls over ice floors with a stony rim) but never places them; Shipbreaker now lets them appear in C- and S-class asteroid fields, about one asteroid in twenty. Breaking an ice wall gives water ice, sometimes methane ice, and ice gangue.
- C-class ore deposits give water ice about one pull in seven instead of one in ten, in place of some silicates. Nothing is added on top: each find takes its share from an existing one, so the game's other odds are unchanged.
- Two settings, both on by default: Mining/SpawnIceFields and Mining/ExtraDepositIce. Switching one off restores the game's own odds for new rolls.

### Compatibility and limits

- Ice asteroids appear only in asteroids generated for a new game; a save keeps the asteroids it already has. Deposit ice applies to future mining in existing saves. The ice asteroids are the game's own, so a save that later drops the mod still loads them. Moving some C-class finds from silicates to water ice raises their average sale value. Requires Phobos Framework 0.48.0 or newer. Offline checks are not gameplay validation.

## [0.43.0] - 2026-09-30 - Draft

### Added

- Phobos' Rivetline Y2, Y3 and Y4 Material Bins (2 x 2, 3 x 3 and 4 x 4; 60, 115 and 170 kg; 2,400, 3,900 and 5,510 cr): sealed, unpowered stores for mined material only (ore, loose regolith, gangue, water and methane ice, ice gangue and mined chunks such as clay hydrates). Their 4 x 4, 6 x 6 and 8 x 8 grids hold real items, six ore blocks to a cell, so every block keeps its identity. Install from the INSTALL menu FURN tab, beside the game's Storage Bay.
- Crew treat an installed bin as an ordinary unlocked container: loading orders can fetch from it, ship-wide or pinned, and a machine's output store can be a bin for gangue and other mined remainders.
- Overhead PixelLab sprites in the silo family's colours, with ochre hinged lids: one hatch on the Y2, two on the Y3 and four on the Y4.

### Changed

- The D4 and R4 storage outputs no longer list material bins, because bins refuse their scrap products.

### Compatibility and limits

- Uninstalling a bin carries its contents with it, as the game's Storage Bay does; a full Y4 can weigh several tonnes. Dismantling waits until the bin is empty. The Y2 may turn up in engineering salvage; the Y3 and Y4 are purchase-only. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.42.0] - 2026-09-30 - Draft

### Changed

- Performance pass, stage 8. Furnace, radiator and coolant parts, including their passive cooling with the panel closed, are now found through one shared sweep of the world in Phobos Framework 0.46.0 instead of walking every object in the world itself (about 8.5 ms each time in the owner's save). A newly placed machine is picked up within about four real seconds; one that is destroyed or removed drops out at once.

### Compatibility and limits

- Requires Phobos Framework 0.46.0 or newer. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.41.0] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 3 (the game crawls at fast-forward). The two power hooks the game calls for every powered object in the world now classify each object by one dictionary probe and keep no state for appliances that are not ours; before this every appliance in the world paid an allocation and several lookups per power step.
- Furnace records (furnaces, radiators, underside ports, coolant charges) settle every two real seconds, on a fault and before every native save, instead of being rewritten several times per power step; a record that did not change is left untouched. Furnace-family discovery runs every two real seconds (a damaged, repaired or installed replacement is tracked at once); passive cooling and detached hot items still advance every quarter second with the console closed.
- The coolant route between a furnace and its cooling endpoint is found once per power step and shared by admission, settlement and the collector checks (it was searched nine to eleven times per step, each with two scans of the ship), through Framework's shared pipe topology; the second-source rule is a membership lookup.
- Collector, processing, storage and intake routes keep a true admission for the rest of its step instead of validating twice per power step. The central console builds detail text only for the card being read. Reclamation resolves target parts by their ids instead of scanning the target ship. The maintenance hooks recognise our equipment before searching action names. Underside-port artwork paths are built once.
- Recorder scopes shipbreaker.power.hook, shipbreaker.furnace.route and shipbreaker.furnace.saves.

### Compatibility and limits

- Requires Phobos Framework 0.45.1 or newer. A crash (never a save) can lose up to two real seconds of furnace record progress; a coolant conduit laid or cut is noticed within two real seconds. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.40.0] - 2026-09-29 - Draft

### Added

- Phobos' Rivetline S4 (4 x 4, holds 1,960 kg, 6,780 cr) and S5 (5 x 5, holds 3,330 kg, 8,860 cr) Process Water Silos, the medium and large sizes of the S3 (owner direction: every bulk family comes in three sizes). They work exactly like the S3 with the T2, station Bulk supplies, Ship's Water and every Phobos water consumer, and hold more for less per kilogram of capacity.
- Two PixelLab sprites in the S3's colours.

### Changed

- The station offer now reads Process water (Rivetline S-series silos) and fills any size; Keep in reserve steps follow the silo's size (the S3's are unchanged).

### Compatibility and limits

- Requires Phobos Framework 0.44.0 or newer. Existing S3 silos keep their identity, water and records. The S4 and S5 are purchase-only and never turn up in salvage.

## [0.39.0] - 2026-09-29 - Draft

### Changed

- Engineering salvage can now turn up a single aluminium ingot (6%) or steel ingot (4%) per eligible roll, next to the loose metal the game's own engineering loot already carries. Never a merchant lot, and merchant stock is unchanged.
- The R4's 9 kg terminal reject and both furnace melt remainders now count as trash at native markets, like the feed-family rejects and retained coolant. Their $0.01 price, mass and identities are unchanged.

### Compatibility and limits

- Framework 0.39.0 is still the minimum. The S3 silo and T2 thaw unit stay purchase-only: construction of anything beyond semi-advanced equipment waits for the Manufacturing mod. Remainders already in a save may keep their old market category; the price is $0.01 either way. Ingot finds appear only in newly generated loot. See the [economy coverage audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/economy-coverage-audit.md).

## [0.38.1] - 2026-09-29 - Draft

### Artwork

- Give the S3 process water silo, the T2 ice thaw unit, the aluminium and steel ingots and the steel melt remainder their own overhead artwork in Rivetline colours, replacing the placeholder drawings. The silo and thaw unit use one picture for every form and for the inventory; damaged forms keep the game's damage tint. The steel ingot is the aluminium ingot's drawing in darker steel tones. In keeping with the game's art style, the silo carries no painted level gauge; read the water level on its Control Panel or the C1.

### Compatibility and limits

- Artwork only: item IDs, placement, recipes, prices, saves and gameplay are unchanged, and Framework 0.39.0 is still the minimum. Normal maps are flat. Offline image and definition checks do not show how the art looks in play; owner review of scale and lighting is pending. Requests, job IDs and rejected attempts are kept with the [artwork records](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/assets/artwork-completion/bulk-silo-requests.json).

## [0.38.0] - 2026-09-29 - Draft

### Added

- F6 recipes: choose the next charge on the panel, the C1 or with F3 (phobosfurnace recipe). Housing is the unchanged original; aluminium ingots turn twenty aluminium scraps into four 4 kg Phobos' Rivetline aluminium ingots, three scrap gates and the melt remainder; steel ingots turn twenty steel scraps into four 4 kg steel ingots, three steel gates and a steel melt remainder in a hotter, longer cycle (iron melting point and enthalpy of fusion from the NIST Chemistry WebBook; superheat and heat capacities are authored). Every recipe conserves the 20 kg charge and runs through the same lining, sink, radiator and 250 kW rating.
- Ingots are one-cell stock that stacks to ten, trades as metals, is sold at the general markets and regional suppliers in lots of 32, and is the raw stock for coming Phobos Manufacturing machining. Table recipes cut an ingot back into four scraps and lose value.

### Changed

- The charge chamber admits aluminium and steel at the game level; the selected recipe decides which is accepted, and a batch stays bound to the recipe it was sealed with. The heating interlock pressure scales with the recipe's target temperature so a hotter recipe is not refused for the residual gas its own pump left behind. Crew loading orders and R4 delivery bring the selected metal.

### Compatibility and limits

- Requires Framework 0.39.0. Batches saved before 0.38.0 load as the housing recipe and finish as housings; a repeat run suspends if the recipe changes under it. Ingot and steel-remainder artwork are procedural placeholders until the handoff is produced. The real steel cycle length on a ship depends on supplied power and the radiator; offline checks complete and cool a full steel charge. Nothing consumes ingots yet. Offline checks pass; owner play-testing is pending.

## [0.37.0] - 2026-09-29 - Draft

### Added

- Phobos' Rivetline S3 Process Water Silo: a passive 3 x 3 vessel (240 kg empty) holding 1,000 kg of process water as a saved record, never a native fuel stat. Fill it at a station under Bulk supplies (Process water, 10 cr/kg in 10 kg steps), from a linked T2, or, with Ship's Water 0.16.1, draw from the drinking-water tanks above a crew reserve (setting Silo/CrewWaterReserveKg, default 50 kg) and send water to its waste tanks for the Recycler. Keep in reserve, Recover trapped water after repair and Accept contents as they are are on the local panel and the C1.
- Phobos' Rivetline T2 Ice Thaw Unit: a 2 x 2 powered unit that thaws one block of the game's water ice (24.7 kg) at a time into 22.7 kg of process water for a linked silo or Agriculture R3 within one tile, plus the game's own 2 kg gangue packet in its two-cell tray. 6 kW for 40 minutes a block (525 kJ/kg authored, NIST enthalpy of fusion attributed), 15% of it as room heat under the R4's atmosphere rule. Nothing warms until the linked vessel has room for a whole block. Progress lives on the block; reload pauses.
- Load feed by crew and standing orders on the T2 (ice from anywhere aboard, gangue to an output store); C1 groups for silos and thaw units; F3 through phobosindustry.

### Compatibility and limits

- Requires Framework 0.39.0. Water is the only silo commodity: no fuel or gas silos, no custom gases, no methane recipe. Nothing consumes silo water yet apart from a linked R3. Both machines use procedural placeholder sprites until the artwork handoff (docs/development/bulk-silo-art-handoff.md) is produced. Uninstalling carries the water with the loose silo; a silo holding water refuses Dismantle when offered, and a protected silo refuses removal; a destroyed silo loses its water, logged. Offline checks pass; owner play-testing is pending. Guide: docs/shipbreaker-bulk-silos.md.

## [0.36.0] - 2026-09-29 - Draft

### Added

- Feed families: the D4 takes the game's other structural parts by hand, crew order or grabber: floor grates of any make (3 to 13 kg, in half kilograms), DuraWal interior walls, window panels, Whipple shielding panels and aerodynamic panels. Each family has a declared, mass-conserving budget: native steel, aluminium, plastic and mechanical parts plus one terminal reject packet (floor grate 1 kg units that stack to ten, DuraWal 9.6 kg, window 6 kg, Whipple 1.5 kg, aero panel 2.1 kg) priced at the technical minimum and never re-processed. Ordinary walls keep their revision-2 products exactly.
- The feed bin admits any wall or floor grate at the game level, the same conditions the game's scrap kiosks buy by. Doors, hatches, docking systems, the turbine lifter, conduit, furniture and machinery are refused with the reason, as are floor makes the game weighs at nothing or between half kilograms.
- The grabber moves every accepted family; rejects leave through the D4 storage output or by hand.

### Changed

- Feed bins refuse merging stacks (floor grates and Whipple panels stack natively), so each unit keeps its own saved job, as the F6 charge bin already did.

### Compatibility and limits

- Requires Framework 0.38.0. Saved wall jobs and their revisions are unchanged. Dismantling every accepted make loses value against selling it whole, audited against live game data. Heavy parts (doors, hatches, airlocks, furniture, machinery) wait for stacked product delivery; the C2 collector does not take the new rejects yet. Offline checks pass; owner play-testing is pending. Budgets and reasons: docs/development/feed-families.md.

## [0.35.0] - 2026-09-29 - Draft

### Added

- Load feed by crew (on/off): a right-click toggle on the installed D4, R4 and F6. Crew with AutoTask and the Haul duty keep the machine's feed loaded from anywhere aboard until you switch it off, through time-skips and reloads. It is the ordinary standing order, so the crew panel can still pin one input store. F6 sealing, heating and release still need the hazardous permission or a repeat run.
- The D4 waits for feed after one Start, like the R4: walls placed by hand, by crew, by the grabber or by a route are processed as they arrive.
- Loading orders on the D4, R4 and a non-hazardous F6 carry on after a save and reload like a painted job (owner decision, 29 September 2026).

### Fixed

- The game's ordinary wall makes are accepted. Almost every wall on a real wreck is a cosmetic variant with its own name and mass (14 to 48 kg); the feed, the grabber intake, crew supply, the G4 cutter and the capture planner used to accept only the plain 24 kg wall. Each panel now yields the 13 kg identified residue packet plus parts, aluminium, carbon fibre and steel for the rest, so the plain 24 kg wall keeps its products exactly, a light Aero-series wall gives the packet and the parts, and a heavy Glory-series wall gives more steel.

### Compatibility and limits

- Requires Framework 0.38.0. Saved jobs, revision numbers, pairs, filters and cargo are unchanged; started 24 kg jobs finish as before. Floors, doors, windows, Whipple, aerodynamic and DuraWal panels are still not feed. The F6 bin still takes single aluminium pieces; the game's right-click places one piece off a stack. Offline checks pass; owner play-testing is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.34.0] - 2026-09-28 - Draft

### Fixed

- Long unobserved intervals are no longer faults. Collectors, storage unloading, the G4 intake and the D4 queue used to pause with a time-gap notice after any gap over the transfer cycle, including a time-skip or a reload; they now catch up like native machines, bounded by the electricity actually received.
- A room that cannot take the R4 or G4 cutter's heat no longer stops the job for Resume. The machine draws no power that step, keeps its permission and progress, shows the room's temperature, air and pressure against the limits, and continues by itself once the room cools.
- G4 wall eligibility follows the game's own uninstall rule for the wall (installed and not damaged) instead of refusing any wall with wear below the damage threshold.
- A refused furnace maintenance finish still closes the game's task for it, as native effects would, instead of leaving that task listed forever.
- Stacked coolant charges work. The game stacks matching coolant dropped into the F6 bin; coolant fill and the crew's coolant order now take one charge from a stack.

### Compatibility and limits

- Requires Framework 0.37.0. Saved pairs, filters, cargo and hot jobs are unchanged. Offline checks pass; owner play-testing of catch-up, heat waiting and wall eligibility is pending. Findings and evidence: docs/development/vanilla-precedence-audit.md.

## [0.33.0] - 2026-09-28 - Draft

### Added

- F6 Repeat batches: an opt-in run that seals each full twenty-piece aluminium charge, runs the automatic sequence, returns chamber gas to the recorded room once cool and releases the products. It then re-enables paired R4 aluminium receiving for the next charge. End repeat run stops the automation and leaves the current batch as it is.
- The run binds the furnace's ship, room and cooling assembly, and never re-arms interrupted heating. Flight manoeuvres, lost power or probes, manual furnace control, a changed room or cooling assembly and faults pause it until you choose Repeat batches again. A partial final charge stays cold and unprocessed.
- D4 and R4 storage outputs. Choose one unlocked native storage container on the same ship, reachable over structural floor, then start unloading. D4 sends its ordinary products: small mechanical parts, aluminium, carbon fibre and steel scrap. R4 sends steel. Each item uses the existing feeder time and power, paid by the sending machine.
- A full store leaves products in the tray and unloading continues when space appears. Stacked, nested or installed items stay in the tray. Unloading pauses on reload and when the store, route or machine changes. Several machines may choose the same store.
- F3 commands: phobosfurnace repeat and repeat-stop; phobosroute stores, store, unstore, unload and pause-unload. The C1 console and local panels show both controls and their status.

### Changed

- Crew F6 standing orders keep bringing aluminium and clearing products during a repeat run, and leave sealing, heating, gas return and release to the run. Otherwise their hot-step choices are unchanged, now through the same shared step selector.

### Compatibility and limits

- Requires Phobos Framework 0.33.0. Residue, rejects, R4 aluminium, furnace outputs and cooling keep their existing routes. Neither feature starts after loading; batches, pairs, filters and cargo are preserved. No new equipment, recipes or artwork.
- The furnace still pauses on any manoeuvre or torch demand, so autonomous G4 transits pause a repeat run. Seal, heat, gas return and release happen during ordinary play, not inside a managed time-skip. Offline checks pass; owner gameplay testing is pending.

## [0.32.0] - 2026-09-28 - Draft

### Added

- D4, R4 and F6 construction sites show unfinished frames while parts arrive, then open machinery once all parts are present and assembly work begins. The finished form appears only after construction completes.
- Include four retained PixelLab variants with matching normal maps; preserve existing Phobos frames and the full furnace footprint. Stage selection reconstructs after reload and returns to early when required parts are removed.

### Compatibility

- Requires Phobos Framework 0.32.0. Section bills, work budgets, saves, finished equipment and older table jobs keep their existing rules. Missing artwork falls back to the native marker. Offline checks pass; lighting and transitions still need owner playtesting.

## [0.31.0] - 2026-09-28 - Draft

### Added

- D4-S, R4-S and F6-S now offer Assembly information and Install for native construction sites. Crew deliver two D4/R4 sections or three F6 sections separately; the APPS tab in INSTALL selects that route.
- Maintenance information identifies cargo, coolant, temperature, batch state and connected-equipment blockers.

### Fixed

- Replace the new-work table assembly route that required multiple drag-only sections to be carried together. Complete loose machines keep direct Install; legacy table action IDs and bills remain for saved queues.
- Recover stored cargo opens checked local F6-P/F6-R recovery. New deposits are blocked, real construction lots stay protected, and native damage transitions preserve captured existing cargo.

### Compatibility

- Requires Phobos Framework 0.31.0. Section masses, output identities, thermal/coolant saves and native placement stay unchanged. Site work combines previous final assembly and mounting.
- Offline native-definition and production adapter checks are separate from pending owner Unity hauling, recovery and reload tests.

## [0.30.0] - 2026-09-27 - Draft

### Changed

- Raise equipment and section offers to at least 85%, and pipes/coolant to at least 95%, before the availability setting. Fill missing functional stock at general local suppliers. Native engineering rolls can now find one of nine intact/damaged machinery families (40% total), one assembly section (15% total), and one pipe/coolant charge (20% total), as separate bounded choices.
- Applies to future native stock and loot generation; no forced restocks, saved-cargo changes, price changes or live gameplay validation. See [merchant availability and salvage](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/merchant-stock.md).

## [0.29.2] - 2026-09-27 - Draft

### Fixed

- Correct loose machinery and conduit pickup/drop actions. D4-S, R4-S and F6-S assembly sections now use the native cumbersome/drag family instead of hand-held scrap behaviour. Sections retain dismantling and remain construction inputs, not installed machines.
- Existing sections in hands or containers remain where saved. Put a held section down once; future handling uses the drag slot. Requires Phobos Framework 0.30.2.
- Audited all 118 implemented item definitions and checked native action/slot contracts offline. Live menus and loaded inventory handling still require owner testing; see the [item handling audit](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/item-handling-audit.md).

## [0.29.1] - 2026-09-27 - Draft

### Fixed

- Require Framework 0.30.1 so the Polaris industrial console receives the corrected dark button states and readable labels. Machinery operation and saved state are unchanged.

### Known limits

- Native palette contrast and offline regressions are checked; corrected Unity appearance and scrolling still require owner playtesting. No gameplay values or save formats change.


## [0.29.0] - 2026-09-27 - Draft

### Changed

- Give Polaris Industrial Control clearer native button faces and selected tabs, with navigation and action rows wrapping at narrow widths.
- Retain independent scrolling, drafts, stop controls, processing policies and resource accounting.

### Requirements

Requires Phobos Framework 0.30.0 or newer; other provider requirements remain unchanged.

### Known limits

- Offline regression and browser-layout checks are separate from owner-run Unity interaction tests. No additional performance captures or Steam publication are claimed.
- Existing faceplates and suitable vanilla graphics are reused after review; no new artwork generation was warranted.


## [0.28.2] - 2026-09-27 - Draft

### Fixed

- Remove temporary equipment-name strings from recurring furnace discovery without changing scan cadence, passive cooling or measured resource accounting.
- Reuse Framework ship-scoped discovery, presentation pacing and change-only native widgets in industrial panels. Add opt-in furnace, capture and reclamation timing scopes.

### Compatibility and limits

- Requires Phobos Framework 0.29.0; the existing Auto Nav dependency remains. Save formats, construction, recipes, heat, material and flight-authority rules are unchanged.
- See docs/development/performance-audit.md for baseline findings and offline verification. Follow-up measurements are deferred; active machinery performance and Unity interaction remain owner-run checks.

## [0.28.1] - 2026-09-27 - Draft

### Documentation

- Separate the plain furnace fault warning from its detailed log message; stopping, saved contents and recovery checks are unchanged.
- Review English controls, warnings, descriptions and help for practical player language; retain precise diagnostics and established equipment names. Update current guides, item-reference inputs and the Workshop draft.
- Follow the retrospective language rule and glossary in docs/development/player-language.md, informed by Blue Bottle Games' official Ostranauts description and Daniel Fedor's developer AMA. This is an interest-based audience interpretation, not measured demographic data.

### Compatibility and limits

- Wording only: translation keys, placeholders, commands, saved identities, resource values and gameplay rules are unchanged. This is an unpublished development candidate; Unity text layout remains unverified.

## [0.28.0] - 2026-09-27 - Draft

### Artwork

- Give fresh and retained coolant, classified reclaimer feed, terminal rejects, melt remainder, housing blanks, R4 sections and F6 sections dedicated overhead artwork.
- Add damaged, packed and damaged-packed artwork for the hull chute, exterior grabber, residue collector, R4, F6, F6-R and F6-P. Preserve left/right F6-P connection inserts and native installation geometry.

### Compatibility and limits

- Require Framework 0.28.0 for shared native state-art binding. Artwork changes preserve saved IDs, placement, recipes, material accounting and native item actions. Neutral normal maps supply no authored relief. Offline image and definition checks do not establish Unity or owner gameplay approval.
- Retain original masters, exact PixelLab prompts, job IDs and export hashes in the [artwork provenance record](https://github.com/phobos-dthorga/phobos-ostranauts/tree/main/assets/artwork-completion). Design-only Manufacturing and underfloor equipment remain unimplemented; no installation or publication.

## [0.27.1] - 2026-09-27 - Draft

### Changed

- Fresh 1 kg thermal service fluid charges stack to 3. Separate one charge before furnace coolant filling; retained coolant remains unstackable. Per-charge mass and coolant accounting are unchanged.

- Loose intact and damaged F6-C coolant conduits stack to 10; installed segments remain individual fixtures.

### Compatibility and limits

- Intact and damaged segments remain distinct identities. Other equipment, materials and retained coolant retain their existing limits.
- Native stack limits apply on the ground and in compatible containers. Existing IDs, per-item mass, value and recipes are preserved; existing items use current definitions on reload, without automatically consolidating stored cargo. Offline checks do not establish gameplay validation.

## [0.27.0] - 2026-09-27 - Draft

### Shared provider configuration

- Render optional Framework provider fields as compact checked selectors in C1, including Agriculture R3/W2 connections and reserve/target choices. Local/C1 authority and configuration suspension remain checked by the owning content mod.
- Require Framework 0.27.0. Existing industrial recipes, material identities, thermal accounting and Auto Nav authority are unchanged. Offline regression checks are not owner Unity validation.


## [0.26.1] - 2026-09-27 - Draft

### Panel corrections

- Require Framework 0.26.1 for compact controls, stable diagnostics and a visible, input-isolated ship picker.
- Bind picker lines to the selected local/C1 machine and disable unavailable Locate/Clear actions for routing, navigation-console and cooling connections. A broken cooling route retains its saved selection so it can still be located or deliberately cleared.
- Show manual-command confirmations and rejection reasons in the fixed notice area, and use a font-safe collapse marker for equipment groups.
- Retain explicit Apply, remote/local authority, furnace interlocks, mission targets and material accounting. Existing Auto Nav requirements remain unchanged; prepared offline, with Unity evaluation pending.

## [0.26.0] - 2026-09-26 - Draft

### Control panels

- Migrated C1 and local D4/R4/C2/G4/F6 controls to the shared compact console. Legacy collector/reclaimer windows and F9 entry points now open the native panel.
- Replaced endpoint lists with searchable Change/Locate/Clear forms. Local inventory access and C1 ship scope retain the existing checked services.
- Retained native furnace gauges and safety controls. Heat, ramp and cooling drafts apply together after fresh access/range/state validation; invalid or stale edits remain visible. Operation, routing, maintenance and diagnostics have separate views.
- Requires Framework 0.26.0. Saved pairs, cargo, industrial jobs and explicit Resume rules are unchanged. Owner Unity evaluation remains pending.

## [0.25.0] - 2026-09-26 - Draft

### Crew automation

- Added standing D4/R4 feed and product handling, configured collector clearing, managed coolant replenishment and explicitly permitted F6 batch sequences.
- Added Industrial Processing training and shared standing-order controls, using the actual worker and existing material, power, heat and furnace interlocks.
- Added one-attempt crew launch/resume of the existing exact G4 reclamation mission. Existing ownership, sensors, pressure, access, occupancy and equipment bindings remain mandatory.
- Industrial orders require Resume after loading; G4 work and movement suspend before time-skip. Supported onboard work uses Framework’s measured skip coordinator.

### Compatibility and limits

- Requires Framework 0.25.0 and Auto Nav 0.19.0, including suspension of manoeuvres before native time-skips.
- Native repair/construction and existing Auto Nav authority remain in place. Existing identities, recipe revisions and cargo are preserved; gameplay checks remain owner-run.
- See [crew controls, sources and owner checks](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/crew-automation.md).

## [0.24.1] - 2026-09-26 - Draft

### Fixed

- C2 residue collectors can be installed on two structural floor tiles with a clear two-tile service walkway, including damaged installation forms. Native placement checks still reject occupied footprints; the body blocks pedestrian overlap. Existing wall installations retain their support and exterior-mouth rules.
- Agriculture 0.11.1 can align the collector's full two-tile pocket against the optional Recycler on open floor, including in front of it. The collector's opposite service side remains accessible. Floor support and access are rechecked during operation.
- Collector routing windows and local/C1 furnace instruments suppress successful status or notice echoes already present in live status, using Framework 0.24.2. Furnace actions refresh their status immediately; rejection explanations and responses concerning another endpoint remain visible.

### Compatibility

- Retains equipment IDs, inventories, saved pairs, construction inputs, finite payload and explicit resume after reload. No pressure boundary is created or removed. Prepared offline; gameplay confirmation remains with the owner.
- Requires Framework 0.24.2 or newer for shared panel feedback.

## [0.24.0] - 2026-09-26 - Draft

### Added

- Add explicitly enabled G4 reclamation: choose an exposed work wall and separate protected anchor, capture, cut/feed, release, retreat, traverse and recapture another admissible work window. Reuse existing G4/H4/D4, N1/N2 and artwork; require Auto Nav 0.18.0 and Framework 0.24.0.
- Cut only empty, unstacked, undamaged native 24 kg ordinary walls on the exact owned unoccupied target. Retain floors, anchor supports, other equipment and target registration. Unreachable remnants are reported; whole-wreck deletion and new structural recipes are excluded.
- Capture authored 120 powered seconds / 12 kW defaults per started cut. Credit actual received electricity and reject its heat into the connected D4 service room, requiring 10 kPa and a 40 C ceiling. This cooling connection is an authored abstraction; vacuum is not free cooling. Cutting and existing 2 kW transfer run sequentially.
- Add G4/C1/F3 Start, Resume, Pause, Stop and phase/blocker/completion/remnant status. Start authorizes the bound G4/D4 chain; full capacity waits under that same mission. Downstream permissions and furnace motion restrictions remain unchanged.

### Persistence and limits

- Journal uninstall and transfer separately before mutation; re-resolve exact native item identity and resulting position. Preserve paid work and evidence on interruption, without reconstructing cargo or repeating outputs. Framework general transfers remain same-ship.
- Reload, lost tracking and manual takeover suspend authority. Stop retains a current capture; Release stays explicit. Older captures remain releasable but require a newly planned protected support before automatic cutting.
- Update maintained defaults/dependency minima, generated equipment operating tables and the [reclamation guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/shipbreaker-reclamation.md). Native API evidence is from Blue Bottle Games' locally inspected Ostranauts 1.0.1.5; offline checks are separate from owner gameplay evaluation. No repeated furnace batches or Steam publication.

## [0.23.0] - 2026-09-26 - Draft

### Changed

- Increase successful local and regional offers to wholesale lots for all machinery, assembly sections, coolant conduit and service charges. Uses Framework 0.24.0; prices, engineering salvage and existing inventories are unchanged.
- Quantities are authored balance and apply on future native restocks; no forced refill, installation or Steam publication. Offline checks are separate from owner shop validation.

## [0.22.0] - 2026-09-26 - Draft

### Added

- Selected-G4 capture and explicit release through local industrial panels, C1 and F3. Bind full native ship, target, G4/chute/D4 and N1/N2 console/module IDs; shorten and disambiguate display labels only. Auto Nav owns the terminal approach; native mooring establishes deck contact after a hull-fit check.
- Write-ahead native anchor records and a post-capture wall-contact check. Manual takeover, tracking/power loss and reload require explicit restart. Unknown state and uncertain native mutations are retained; no target cargo is deleted or copied.

### Requirements and limits

- Phobos Auto Nav 0.16.0+ is now mandatory alongside Framework 0.23.0+. Builder and installer include the dependency. Existing equipment, cargo, recipe and hot-job identities remain unchanged.
- First stage accepts owned, unoccupied targets only. Cutting, automatic repositioning, repeated furnace cycles and whole-wreck completion remain future work. Stop/release do not brake. Prepared offline, not installed, gameplay-validated or published.
- See the [capture guide and native evidence](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-capture.md). Blue Bottle Games supplies the native mooring/fit precedent; this authored arrangement is not scientific validation.

## [0.21.0] - 2026-09-26 - Draft

### Added

- Industrial equipment, assembly sections, coolant conduits and finite coolant charges gain bounded offers at 15 additional placed vanilla retail markets. Industrial centres receive higher authored availability; Flotilla machinery is refurbished. Clean coolant now uses the industrial category and retained coolant uses trash, never potable water. Base values, material budgets and jobs are preserved.
- [Solar-system economy guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/solar-system-economy.md) documents all 19 native market profiles, authored availability and native pricing limits. Based on Blue Bottle Games' installed Ostranauts 1.0.1.5 data and local engine inspection; these are game-economy choices, not NASA/ESA research results.

### Compatibility

- Requires Phobos Framework 0.23.0 or newer. Existing merchant inventories are not refilled on load. New offers use native generation/restocking. Prepared offline; gameplay validation and Steam publication remain pending.

## [0.20.0] - 2026-09-26 - Draft

### Added

- Allow registered content-owned cargo in the existing Residue Collector under its unchanged four-slot and 52 kg limits. Expose the existing hull/exterior/floor mount check for optional attachments.
- Agriculture 0.9.0 can attach Ship's Water 0.16.1 Recycler wet-reject capture to the same exclusive inlet. Collector hardware, industrial residue identities, default filters and automatic industrial routing retain their meaning. Remove wet packets manually.

### Requirements

Phobos Framework 0.22.0 or newer. Agriculture and [Valtora's Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189) remain optional. No new Shipbreaker equipment artwork or industrial recipe; live attachment behavior awaits owner testing.

## [0.19.1] - 2026-09-26 - Draft

### Fixed

- Mitigate saved-room misassignment triggered by pending Shipbreaker construction at a ship's grid edge. Restore checked saved grid bounds before native room and zone loading; retain native construction restoration, saved atmosphere, identities and progress.
- The owner's G4 marker explains the observed 63-to-62-column mismatch and all four wrong tile indices. Apply only with spawned Shipbreaker installation markers; unrelated ships, matching grids and unsupported geometry are left unchanged.

### Requirements

Phobos Framework 0.21.1 or newer. No saved-state migration or save-file editing. This cannot recover gas already lost before saving. Offline regression and native loader-contract checks passed; the owner reported a successful affected-save reload on 26 September 2026. Broader save/reload coverage remains unverified. See the [investigation and owner check](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/shipbreaker-room-load-mitigation.md).

## [0.19.0] - 2026-09-25 - Draft

### Changed

- Use Framework's common completion channel and volume/mute. Preserve the original D4/R4 one-batch meaning; simultaneous events from other Phobos mods cannot overlap.

### Requirements

Phobos Framework 0.21.0 or newer. No recipe, inventory or save changes. Listening evaluation remains pending.

## [0.18.0] - 2026-09-25 - Draft

### Added

- Optional notification for one explicitly watched D4/R4 batch, after products are committed to the output tray. Further queued batches remain silent; completion also remains visible in text.
- Original 280 ms quiet procedural completion tone through the native sound-effects mixer, with volume/mute in the panels and configuration. Same selected crew and ship only; pause, fault or reload clears watches. Suppressed events never replay; nearby completion bursts are dropped.
- Local Control Panel, C1, F9/reclaimer fallback and C1 F3 watch/unwatch controls. Reclaimer fallback content scrolls to keep controls reachable.

### Requirements

Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.20.0 or newer. No new mod dependency or saved-state migration.

### Known limits

- Prepared first audio trial; owner listening and gameplay evaluation remain pending. No furnace, cooker, navigation or ambient cues. Low playback priority is not a native critical-alarm suppression guarantee.

### Fixed

- Fixed unreachable INSTALL entries; machinery appears under APPS, cooling hardware and conduits under HVAC, and the C1 console under CTRL, including damaged forms. Existing inputs, placement and saved IDs are preserved.

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
