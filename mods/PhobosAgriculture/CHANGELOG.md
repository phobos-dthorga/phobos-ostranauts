# Phobos Agriculture changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Changed

- Reviewed every English entry. Simplified plant-feed choices, water and nutrient warnings, ready alerts and maintenance instructions; names, amounts and saved crops stay the same.

## [0.58.0] - 2026-10-05 - Draft

### Fixed

- A rack, W2, cooker or bench in a room that loses its air, through a hull breach or a collision, now stops and says the room has no air. It used to fault instead: it stopped, refused every command until the game was loaded again and wrote an error to the log, because the small standby draw every installed machine keeps counted as work done without air.

### Save compatibility

- Nothing saved changes. A machine that faulted this way recovers when the game is loaded.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.57.0] - 2026-10-05 - Draft

### Added

- Performance captures report how many Agriculture machines hold a live session, once a second while recording, so growth that never comes back down shows up.

### Dependencies

- Needs Phobos Framework 0.104.0 or newer.

### Save compatibility

- Nothing saved changes. Recording stays off until you start it.

### Compatibility and limits

- Checked offline; not yet recorded in the game.

## [0.56.0] - 2026-10-05 - Draft

### Fixed

- A rack with a crop crew order switched on no longer tells you to enable a crew order. When the rack is empty or its crop is ripe, its Next line now shows where the order stands: the crew member working on it and the step, the reason it is waiting, or why it stopped. It reads the same status as the Crew operations screen.

### Save compatibility

- Nothing saved changes.

### Compatibility and limits

- Problems the crew cannot fix, such as the room, power, water or nutrients, are still named first.
- Checked offline; not yet seen in the game.

## [0.55.0] - 2026-10-05 - Draft

### Changed

- One nutrient now suits every crop. The 40 g packet, the 500 g bulk charge and bulk in a hopper are the same nutrient in three sizes. The separate feed a W2 mixed for each crop is gone, with its Select feed buttons and its charge picker.
- A W2 takes nutrients by itself from any Groundwork nutrients in its Inventory, or from a nutrient hopper within one tile, while it runs and with its racks linked. Nothing has to be paused, unlinked or drained to add or change nutrients.
- A W2 feeds nutrients with the water: up to 10 g with every kilogram the pump moves, until each linked rack holds 0.1 kg. A W2 with no nutrients sends plain water, and the rack's Next line asks for them.
- When a rack is already full of water but short of nutrients, the W2 recirculates that rack's water to carry them in. The rack's water stays the same, the pump uses its ordinary power, and the W2's panel says which rack it is feeding. A fed rack is not recirculated.
- Crops now grow in rooms up to 31 C (30 C before), on the view that crops of the game's day are engineered ones and a ship's rooms sit at about 25 C or a little above.
- The growing room, the stress rules and the W2's feeding figures are now in the crops data file, in a new growth section. Player files and Workshop add-ons can change them or give any crop, their own included, its own room. See Growing room and stress in the data files guide.
- Several W2s may share one irrigation pipe without matching anything, because the pipe holds only water.
- A new crop in a player file or add-on no longer needs feed names. The W2's Supplies page shows its nutrients and where they come from.
- Crew orders leave a pipe-fed rack's nutrients to its W2 when that W2 has nutrients or a source for them. The W2's supply order keeps it stocked.

### Save compatibility

- Automatic, with no mass changed. Feed held in a rack or W2 becomes water and nutrients when the save loads.
- Feed left in irrigation pipe is flushed out by its W2 the next time it runs a rack: into the W2's water and nutrient stores while there is room, the rest as Recorded Process Solution in its Inventory. It can also be drained into a canister as before.
- A drain canister of old feed put in a W2 pours in as water and nutrients.
- Links, crops, charges, hoppers, crew orders and settings are kept. A saved choice of nutrient charge is still used while that charge lasts.

### Compatibility and limits

- Nutrients travel with the water; the small amount that would sit in the pipe is left out of the model.
- A W2 tops every linked rack up to the same 0.1 kg, whatever it grows. Nothing is lost, and the figure is in the data file.
- The room limits and stress rates are authored for play, not measured plant tolerances.
- Checked offline; not yet seen in the game.

## [0.54.0] - 2026-10-05 - Draft

### Fixed

- Rack and W2 buttons now say what they did. Start, Pause, the water supply buttons and the pipe-fed and direct refill choices used to work without a word, so they looked dead.
- Every page of a rack's or W2's Control Panel now ends with a Next line naming the one thing it is waiting for: air, power, water, nutrients, seed or Start. A pipe-fed rack names what its W2 is missing, such as a paused pump.
- Linking a rack to a W2, choosing pipe-fed water, or enabling water supply on a pipe-fed rack now switches the rack's intake on and starts the W2's pump. Before, a linked rack stayed dry until the pump was started separately at the W2, and nothing said so.
- Crops now grow in rooms up to 30 C. The limit was 26 C, and a ship with machinery running sits just above it, which stopped growth and killed crops within half a day.
- Crew orders on a pipe-fed rack no longer carry water to it by hand. They switch its intake on instead.
- Crew orders no longer plant into a rack that has no water or nutrients. The order waits and its status says what for.
- Stopping or changing a crew order no longer switches off water intake or pauses a growing crop.
- An empty rack or W2 no longer shows lost health from standing in a room without air.

### Save compatibility

- Automatic. Saved links, crops and settings are kept. Empty racks and W2s that showed lost health read as sound again.

### Compatibility and limits

- A rack still needs nutrients: 40 g packets by hand, or a W2 mixing that crop's feed from a nutrient charge. To change a linked W2's feed, unlink it first.
- Crew fetching seed from a stockpile was reported not to work. It could not be reproduced offline; the order's status line now gives the reason when a haul fails.
- Checked offline; not yet seen in the game.

## [0.53.0] - 2026-10-05 - Draft

### Changed

- Irrigation pipe now joins a W2 or a rack it runs under or right beside, on any side, the same rule as the process-water line. It used to join only at one outlet tile beside the W2 and one inlet tile beside the rack, so racks or W2s set side by side could not link.
- A W2 within one tile of a rack feeds it directly, with no pipe.
- Several W2s can share one irrigation pipe while they mix the same feed. A W2 that would pump a different feed into a pipe another running W2 uses waits, and its panel says why. Before, any two W2s on one pipe stopped pumping.
- The link lists for racks, W2s, water tanks and nutrient hoppers now say why something aboard is not offered.

### Save compatibility

- Automatic. Every layout that linked before still links; saved links and the water or feed in the pipes are kept.

### Compatibility and limits

- Requires Phobos Framework 0.103.0 or newer.
- Touching machines do not join separate irrigation runs into one, so two W2s side by side keep their own runs and feeds.
- Checked offline; not yet seen in the game.

## [0.52.0] - 2026-10-05 - Draft

### Changed

- Economy audit, 5 October 2026: crop nutrients are 150 cr/kg (they were 1,500). A 40 g packet is 6 cr, a 500 g bulk charge 75 cr, and the refuelling kiosk fills hoppers at the same 150 cr/kg. Makeup salts are 3 cr a packet. Growing food now pays a little over its water and nutrients, and formulating nutrients for sale no longer prints money.
- Hearth flatbread is 200 cr and soybean stew 190 cr (they were 90 and 70), so cooking with a water ration no longer costs more than the meal fetches.

### Save compatibility

- Automatic. Saved meals, produce and nutrient charges take the new prices when the game loads; a part-used charge is priced by what is left in it.

### Compatibility and limits

- Requires Phobos Framework 0.102.0 or newer.
- Checked offline; not yet seen in the game.

## [0.51.0] - 2026-10-05 - Draft

### Fixed

- Control Panel on Agriculture equipment no longer does nothing when the crew member is within the game's reach of the machine. When a panel cannot open, the crew member's log now says why.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.96.0 or newer.
- Checked offline; not yet seen in the game.

## [0.50.0] - 2026-10-05 - Draft

### Changed

- Grow racks, the Hearth-2 cooker, the W2 water supply and the B2 bench carry on after loading a save if they were working when it was made: crops keep growing, a portion keeps cooking, and receiving and pumping water continue. A damaged or uninstalled machine stays stopped.

### Save compatibility

- Automatic. A save made before this version holds no record of what was running, so machines wait for Start once more after the first load; from the next save on they carry on.

### Compatibility and limits

- Requires Phobos Framework 0.95.0 or newer. Turn the behaviour off with ResumeAfterLoad in the Framework configuration file.
- Collecting a Ship's Water recycler's wet rejects still has to be enabled again after loading.
- Checked offline; not yet seen in the game.

## [0.49.0] - 2026-10-05 - Draft

### Changed

- The grow racks, Hearth-2 cooker, W2 supply and B2 bench follow Framework's new machine heat setting: a quarter of their heat by default, so a grow room stays cool for longer.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.94.0 or newer. Set the share with MachineHeatScale in the Framework configuration file.
- Checked offline; not yet seen in the game.

## [0.48.0] - 2026-10-05 - Draft

### Added

- Add-ons and your own data files can add items of their own, with their own pictures: seed and produce for a crop the file adds, and meals for a Hearth-2 recipe it adds. An added meal carries its own food values and the crew eat it like the shipped ones. See the add-on publishing guide and its Dockside Extras example.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.92.0 or newer.
- A crop a file adds still borrows a shipped crop's growth pictures. Added items are not sold by merchants. A save holding an add-on's items needs that add-on enabled.
- Checked offline; not yet seen in the game.

## [0.47.0] - 2026-10-04 - Draft

### Changed

- Treatment rejects, recycler wet rejects and retained waste are declared as remainders, so Phobos Manufacturing's reaction mass feeder can grind them into RCS reaction mass.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.87.0 or newer. The feeder itself comes with Phobos Manufacturing 0.43.0.
- Checked offline; not yet seen in the game.

## [0.46.0] - 2026-10-04 - Draft

### Added

- Sugar beet (owner-approved crop list, 4 October 2026). Sow a 20 g Continuance sugar beet seed packet; a harvest after 140 hours gives nine 0.5 kg beets and the packet back.
- Beet sugar at the Groundwork B2: one beet gives a 70 g packet of white sugar, a small food, and its wet pulp as residue for the straw press. A crew order, Extract sugar from beets, keeps the bench working and loads the pulp into the press.
- Sugar beet seed at the usual seed sellers and faction kiosks, and now and then in locked crates.

### Changed

- The B2's flax scutching and beet sugar share one job type; flax scutching works as before.

### Save compatibility

- Automatic. One new crop and a new bench job; nothing saved earlier changes.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- Beets and sugar are not sold, so they cannot be bought and turned into sugar or spirit for profit. The fermenter and spirit are planned for Phobos Manufacturing.
- Root composition and sugar recovery sit within published sugar beet figures; growth time, light and yields are gameplay choices.
- Checked offline; not yet seen in the game.

## [0.45.0] - 2026-10-04 - Draft

### Added

- Fibre flax (owner-approved crop list, 4 October 2026). Sow a 10 g Continuance flax seed packet; a harvest after 90 hours gives four 0.25 kg bundles of flax straw and the packet back.
- Flax scutching at the Groundwork B2: one bundle gives two of the game's own clean scrap cloth, the cloth bed and medical-bed repairs take and weapon buffing uses, and its woody shives as residue for the straw press.
- A crew order, Scutch flax into cloth, keeps the bench working and loads the shives into the press.
- Flax seed at the usual seed sellers and faction kiosks, and now and then in locked crates.

### Save compatibility

- Automatic. One new crop and a new bench job; nothing saved earlier changes.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- Flax straw is not sold, so it cannot be bought and turned into cloth for profit. Linseed oil and edible linseed are not included.
- The cloth yield (20% of the straw) sits within published scutching and hackling yields; growth time, light and the bundle's make-up are gameplay choices.
- Checked offline; not yet seen in the game.

## [0.44.0] - 2026-10-04 - Draft

### Added

- Straw press on the Groundwork B2 (owner decision, 4 October 2026: crop waste both ways). Load crop residue and spent crop biomass into the press, start the bench, and it dries them into 1 kg straw bales, sending the steam to a Rivetline water tank the bench touches or shares a process-water line with. With Phobos Manufacturing 0.37.0, a V4 burns a bale into carbon dioxide for a grow room or chars four into a carbon stock.
- Empty the straw press returns what it holds as one recorded crop residue, so nothing is lost.
- The B2 has a process-water port for the dryer's tank.

### Save compatibility

- Automatic. Residue and spent biomass made from now on record their plant matter; older residue still goes through nutrient recovery but cannot be pressed. The press is a new saved record on the bench.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer. The bale's uses need Phobos Manufacturing 0.37.0 or newer.
- The dryer needs a water tank with room for all the water it removes; without one it stops and says so. The bench cannot be removed while the press holds anything.
- Bale composition and dryer power are gameplay choices; drying energy follows water's latent heat (NIST).
- Checked offline; not yet seen in the game.

## [0.43.0] - 2026-10-04 - Draft

### Added

- Enriched air grows crops faster (owner direction, 4 October 2026). Every crop grows more per hour and per kWh with more carbon dioxide in its room: about 20% faster at 0.10 kPa and 25% at 0.15 kPa, slowing again above that and back to the ordinary rate by 0.5 kPa. It takes the same water, nutrients and light per kilogram, so the cycle is just shorter. The rack's panel shows the room's CO2 and the factor. An A2 regulator from Phobos Manufacturing set to 0.10 kPa holds a grow room near the best point. Crops already growing benefit too.
- Spare condensate goes to a tank: water a full rack cannot keep, which used to be lost, now goes to a Rivetline water tank the rack touches or shares a process-water line with.
- Data files: crops.json has a co2Response curve you can tune. See the data file guide.

### Save compatibility

- Automatic. Nothing new is saved; plantings saved by earlier versions grow on the same budgets.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- The curve follows NASA and Utah State University crop-chamber findings on wheat and soybean; its exact values and the absence of a penalty below ordinary air are gameplay choices. Racks fed only from Ship's Water drinking tanks still lose their spare condensate; it is not routed into drinking water.
- Turning crop residue into carbon stock is not included: it needs a design decision first.
- Checked offline; not yet seen in the game.

## [0.42.0] - 2026-10-04 - Draft

### Added

- Dwarf tomatoes, picked as they ripen (owner direction, 4 October 2026). Plant a 5 g Continuance tomato seed packet: first ripe after 64 hours at 0.7 kW. Pick ripe fruit takes three 0.25 kg portions and leaves the plant growing; the fruit turns green, then red again in about ten hours. A plant gives three picks, and Harvest takes the rest of the fruit, a seed packet and the vine. A crew order picks while it can, then harvests. Tomatoes are eaten fresh.
- Soybeans: plant a 30 g Continuance soybean seed packet; 90 hours at 0.65 kW give one 0.25 kg portion of dry beans, the packet back and 0.62 kg of straw. The Hearth-2 cooks beans and a water ration into a 0.5 kg bowl of soybean stew in fifteen minutes, filling and protein-rich.
- Seed packets at the supply kiosk, K-Leg fixer, Halvorson and regional merchants; soybean stew at the food carts; all five items in finds and at the faction kiosks at any standing.
- New artwork: six growth stages each for tomato and soybean in the rack, and five item icons.
- Data files: a crop may allow picks (picks and pickKg in crops.json). See the data file guide.

### Changed

- Fridge and crate finds keep their old totals, now shared by every crop's seed and food.

### Save compatibility

- Automatic. A planting records its picks only once picked; plantings, feeds and cooking saved by earlier versions load unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- Picks, hours, yields and food values are gameplay choices in the ratios NASA's crop chamber found. Cooking moisture loss and soaking are not modelled.
- Checked offline; not yet seen in the game.

## [0.41.0] - 2026-10-04 - Draft

### Added

- Dwarf wheat, the first new crop (owner direction, 4 October 2026). Plant a Continuance 50 g seed wheat packet: 84 hours at 1.2 kW. A healthy harvest gives one 0.4 kg portion of wheat grain, gives the seed packet back, and leaves about 0.95 kg of straw as recorded residue for the B2 bench. The W2 mixes wheat feed. Grain keeps indefinitely.
- Hearth flatbread: the Hearth-2 bakes one grain portion and one water ration into a 0.65 kg loaf over ten minutes, as filling as the game's own prepared meals. If the water ration is gone when baking finishes, the cooker stops with the bread nearly done; add water and choose Start. The crew cooking order fetches grain and water and keeps the drinking reserve.
- Seed wheat at the supply kiosk, K-Leg fixer, Halvorson and regional merchants; flatbread at the food carts; seed wheat, grain and flatbread in fridge and crate finds; all three at the faction kiosks at any standing.
- New artwork: six wheat growth stages in the rack and three item icons.

### Changed

- Fridge and crate finds keep their old totals; the wheat items share them, so potato and lettuce items turn up slightly less often.
- Cooking recipes may now take one supply beside the portion, used when the cooking finishes.

### Save compatibility

- Automatic. Wheat is a new crop; plantings, feeds and cooking saved by earlier versions load unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- Wheat's figures follow the ratios NASA's crop chamber found against potato; the hours, yields and food values are gameplay choices. Baking moisture loss is not modelled.
- Tomato and soybean follow after review of the wheat pilot.
- Checked offline; not yet seen in the game.

## [0.40.0] - 2026-10-04 - Draft

### Changed

- Crops are now a readable data file. What a Firstlight rack grows (growth time, power, water, nutrient, harvest, items, feed and artwork of each crop) moved from the code into framework/crops.json, and the Hearth-2's cooking recipe into framework/process-recipes.json. Potatoes, lettuce and lettuce grown for seed behave exactly as before. This is groundwork for the new crops to come (owner direction, 4 October 2026).
- You can add a crop of your own with a file in BepInEx/config/PhobosAgriculture/crops: it gets its own planting job, W2 feed and crew order. It must conserve mass, use the mod's own items and borrow a shipped crop's artwork. See the data file guide.

### Save compatibility

- Automatic. Plantings, feeds, pipes and half-cooked portions saved by earlier versions load unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.
- The shipped crops and the shipped cooking recipe are frozen: a file that edits one is skipped with the reason; add a new one beside it instead. A file cannot add an item or artwork.
- If you remove a file that added a crop while a rack is growing it, that rack waits for attention until the file is back.
- Checked offline against saved-record fixtures and the item reference export; not yet seen in the game.

## [0.39.0] - 2026-10-04 - Draft

### Added

- Nutrient hoppers show the crop nutrients they hold when you right-click them, in kilograms.

### Fixed

- Buying crop nutrients into a Groundwork hopper at a station works. Every purchase failed with The destination or available capacity changed, because the purchase reserved the hopper and then refused it for being reserved. A quote for a hopper or W2 is also no longer voided by the hopper's level or the W2's inventory changing; room is checked again on delivery, and what does not fit is refunded.

### Save compatibility

- Automatic. Hoppers in older saves show their contents once their ship has loaded.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.

## [0.38.0] - 2026-10-01 - Draft

### Changed

- Performance pass, first round (owner request, 1 October 2026): the recovery record of every rack, cooker, bench and W2 is written only when it changes, instead of on every save of the machine. Nothing else changes.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.71.0 or newer. Static review only; the larger findings (several saves per power step, conduit reads) are listed in the performance record for a measurement first.

## [0.37.0] - 2026-10-01 - Draft

### Changed

- The Firstlight-4 rack's inventory is 4 x 3 cells and the Hearth-2's is 2 x 2, where each was 8 x 8 (owner direction, 1 October 2026: size every inventory to its job). Harvests now arrive as stacks, ten portions to a cell, so two harvests, their seed and residue and about six kinds of supply fit the rack.
- Bagging from a nutrient hopper stacks full bags, three to a cell, so its four-cell rack holds twelve bags (6 kg) before it needs emptying.

### Save compatibility

- Automatic. An inventory saved with more than fits re-packs into stacks when the save loads, and anything that still has no place is put on the deck beside the machine, with one line in the crew log. The cooker's portion in hand keeps its place first.

### Compatibility and limits

- Requires Phobos Framework 0.71.0 or newer. Recorded products (residue, process solution, mixtures) never stack. Offline checks are not gameplay validation.

## [0.36.0] - 2026-10-01 - Draft

### Changed

- Inventories are sized to what the equipment does (owner direction, 1 October 2026). The W2 has eight cells (4 x 2), where it had sixty-four: one for each kind of supply it handles and two for drained or treated solution. The B2 workup bench has six (3 x 2): a job's residue and supplement, its two products and two to spare. A nutrient hopper has a four-cell rack for the charges it bags.
- Bagging from a hopper, or draining a tank into its rack, now says in the crew log when the rack is full instead of doing nothing.

### Save compatibility

- Automatic. Items stored beyond a smaller inventory are moved to free cells inside it when the save loads, and what finds no cell is put on the deck beside the machine, as ordinary cargo; the crew log says so once per ship. An item a saved job names (a selected charge, a recovery input and its cartridge, a workup input) keeps its place inside first.

### Compatibility and limits

- Requires Phobos Framework 0.70.0 or newer. Bagged hopper charges do not yet merge into one stack, so a hopper holds four bagged charges at a time; take them out to bag more. The Firstlight-4 rack and the Hearth-2 keep their present size for now. Offline checks are not gameplay validation.

## [0.35.0] - 2026-10-01 - Draft

### Fixed

- The W2 links to a water silo along a process-water line laid under or right beside both, on any side (Phobos Framework 0.69.0). Before, the line had to end on one particular tile beside each. Ship's Water tanks join a water line the same way.

### Added

- The W2's water tank choice says why a silo aboard is not offered: loose, damaged, locked, or no working line touching it.

### Save compatibility

- Automatic. No saved record changes.

### Compatibility and limits

- Requires Phobos Framework 0.69.0 or newer. The irrigation conduit between a W2 and its racks keeps its own connection points. Offline checks are not gameplay validation.

## [0.34.0] - 2026-10-01 - Draft

### Changed

- After you change a W2's formulation, its pipes flush themselves: when the W2 next runs a rack, the pump first pushes the old feed back to the W2 before the new feed fills the pipes. Old plain water returns to the W2's reservoir while there is room; old feed, and any water that does not fit, comes back as Recorded Process Solution in the W2's inventory, ready for drainage treatment. If the W2's inventory has no room for it, the flush waits and the rack's status says so.
- A drain canister of feed the W2 does not mix (another formulation, or feed in a water-only W2) now pours into the W2's inventory as Recorded Process Solution, so drainage treatment can recover it. Water and the W2's own feed still pour straight into its reservoir.

### Compatibility and limits

- Requires Phobos Framework 0.65.0 or newer. Treatment still recovers only 90% of the water and 80% of the nutrients, as before. Offline checks are not gameplay validation.

### Documentation

- The hopper guide names Phobos Manufacturing 0.20.0's LC-3 as a second way to fill a nutrient hopper. Documentation only; gameplay and saves are unchanged.
- Point the player guide to current dependencies and explain Maintenance information. Distinguish original loot/roadmap milestones from current installation advice. Documentation only; gameplay and saves are unchanged.

- Include every Agriculture item in the complete action audit. Document why retained crops, fluids, jobs and R3 links can hide removal, and why loose conduit stacks require individual pieces for dismantling. No gameplay change is claimed.

- Added source-backed bulk-storage calculations for 54 farm scenarios, the R3 water-reservoir and larger nutrient-charge proposal later delivered in 0.14.0, and a local vanilla-art comparison workflow. Existing machinery, recipes, versions and installed files are unchanged; no production artwork was generated.

- Research crop-residue nutrient recovery, optional Ship's Water reject capture, a proposed Groundwork workup bench and non-repairable progressive W2 mixtures. These were implementation plans at the time; the 0.9.0 entry below delivers recorded residue recovery, the B2 workup bench and optional reject capture.
- Expand the native economic audit with electricity sensitivity, repeating seed-production costs and bounded illustrative residue recovery; runtime prices and yields are unchanged.

- Added a maintained per-mod item reference covering function, use, acquisition and applicable economic/service data; generated tables and coverage checks share a one-click updater.

### Fixed

- Fixed unreachable INSTALL entries; Firstlight-4, Hearth-2 and W2 appear under APPS, and irrigation conduit under MISC, including damaged forms. Existing inputs, placement and saved IDs are preserved.

## [0.33.0] - 2026-10-01 - Draft

### Changed

- The irrigation conduit now holds what it carries, about 0.2 kg of water or feed on every tile, like the other Phobos lines. A running W2 first fills its racks' pipes from its own reservoir; once the pipes are full, water or feed flows straight through to the racks. The old travel delay is gone: a long line now takes a few minutes to fill the first time, and stays full.
- Right-click an installed conduit and choose Drain line into canister to drain the run into a Framework drain canister; the run then stops until Return line to service and refills. A conduit holding water or feed cannot be taken up until drained.
- A drain canister of water, or of the W2's own feed, put in the W2's inventory pours into its reservoir.

### Save compatibility

- Automatic. Feed that was in the pipes of a rack saved by an earlier version is delivered into that rack on the W2's next powered run, then the old record empties. Conduits already laid start empty and fill from the W2 when it runs, taking about 0.2 kg a tile from its reservoir.

### Compatibility and limits

- Requires Phobos Framework 0.64.0 or newer. The 16 mm bore and dilute feed at water's density are authored figures. Changing a W2's formulation leaves the old feed in its pipes until drained; drained feed pours back only into a W2 with the same formulation, not into drainage treatment. Offline checks are not gameplay validation.

## [0.32.0] - 2026-10-01 - Draft

### Changed

- A Firstlight-4 rack refilling straight from Ship's Water, and a W2 falling back to Ship's Water, draw only from tanks that touch them or share their process-water line. The rack gains a process-water port on the same tile as its irrigation inlet, beside the middle of its left-hand side, each line in its own lane. When Ship's Water is installed but no tank is in reach, the rack or W2 says so and what to do.

### Compatibility and limits

- Requires Phobos Framework 0.59.0 or newer. **Manual step:** a rack or W2 that drew from Ship's Water tanks elsewhere aboard stops drawing until a tank sits within a tile of it or process-water line joins their water ports. A W2 linked to a water silo, and racks fed through a W2, are unaffected. Offline checks are not gameplay validation.

## [0.31.0] - 2026-10-01 - Draft

### Changed

- The R3, R4 and R5 reservoirs retire into Phobos Framework's Rivetline S3, S4 and S5 process water silos, one ladder shared by every Phobos mod. Every saved reservoir, installed or loose, becomes the silo of its footprint when the game loads, in place, with its water, reserve, inventory and W2 link. Its housing weighs and is worth what a silo does.
- Loading a 5 kg irrigation charge, recovering trapped water, draining, and the standing fill order work on every silo, the new S2 included. Water for them is bought as Framework's process water under Bulk supplies; the separate agricultural water offer is gone.
- Reservoirs are no longer sold or found, and they leave the INSTALL menu. The machinery salvage roll drops from 0.3 to 0.25 so the other five families keep their one-in-twenty.
- The W2's Supplies page says **Water silo connection**, and its intake status and waiting message name the silo instead of the R3. The fill order is now **Keep the silo stocked with irrigation charges**.

### Documentation

- The bulk-storage guide leads with feeding a W2 from a silo and says what happens to old reservoirs; the other guides name silos instead of reservoirs.

### Compatibility and limits

- Requires Phobos Framework 0.58.0 or newer. The conversion is automatic and happens once; loading the save with an older Agriculture afterwards is not supported. Offline checks are not gameplay validation.

## [0.30.0] - 2026-10-01 - Draft

### Added

- Process-water ports: every R3, R4 and R5 reservoir has one on its left-hand side, and the W2 has an intake there. A W2 can now draw from a reservoir or silo along Framework's process-water line as well as when they touch.
- One reservoir or silo can feed several W2s; its panel lists every machine linked to it, and Clear on the reservoir releases all of them.

### Changed

- Rack pairing lists only racks joined to the W2 by irrigation conduit, from the W2's outlet to the rack's inlet, and refuses a pairing across open floor with the reason.

### Compatibility and limits

- Requires Phobos Framework 0.57.0 or newer. Saved pairings and reservoir records are kept; a saved W2 link reads as the reservoir's first slot. A rack pairing saved without a conduit between them stays saved but delivers nothing until conduit is laid, as before. Offline checks are not gameplay validation.

## [0.29.0] - 2026-09-30 - Draft

### Changed

- The irrigation conduit draws in its own lane and depth, a little thinner than before, so it can share a tile with other kinds of line. The PDA's Conduits filter takes it, and painting jobs on Equipment leaves it alone.

### Compatibility and limits

- Requires Phobos Framework 0.56.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.28.0] - 2026-09-30 - Draft

### Fixed

- The control panel redraws as soon as a setting is applied or an action runs, so supplies, links and offered actions no longer stay stale until the panel is reopened.
- A protected nutrient hopper's accept button and a water reservoir's Details page show their text instead of bracketed keys.

### Compatibility and limits

- Requires Phobos Framework 0.55.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.27.0] - 2026-09-30 - Draft

### Added

- Phobos' Verdemorrow Groundwork E2, E3 and E4 Nutrient Hoppers: passive stores of formulated crop nutrients in three sizes (2 x 2, 3 x 3 and 4 x 4; 10, 25 and 48 kg). Install them from APPS.
- Station refuelling kiosks sell crop nutrients by the kilogram into a hopper under Bulk supplies, at 1,500 cr/kg: the same per kilogram as a bulk nutrient charge or a 40 g packet. Nothing sells back.
- A W2 within one tile can dose from a hopper while mixing: pick the hopper as the W2's nutrient source on its Supplies page. It takes only what each mixing step needs.
- Hopper crew work: recover nutrients a repair left in the catch chamber, and bag up to 500 g at a time into an ordinary bulk nutrient charge to empty a hopper before moving it.
- Overhead sprites for all three hopper sizes, in the reservoir family's colours.

### Balance

- Hoppers cost 300, 490 and 690 cr (a fifth when broken) and are sold on the reservoirs' routes, four to a lot, and at the faction kiosks for Warm standing. The E2 joins Agriculture's share of engineering salvage, which is now split one more way: each machine turns up in 2.5% of rolls instead of 3%, the same total.

### Compatibility and limits

- A damaged hopper traps its nutrients in a catch chamber until it is repaired and recovered; nothing leaks. A hopper holding nutrients refuses to be moved or dismantled. The hopper's contents are the crop model's one aggregate nutrient figure. Saves are unchanged. Offline checks are not gameplay validation.

## [0.26.0] - 2026-09-30 - Draft

### Added

- Every Verdemorrow machine, supply, crop and meal is now also sold for scrip at the CCRE faction kiosks at Zhonghuamen Terminal and Port Yangshan (Mars) and the GalCon faction kiosk at Port Mojave (Ceres), in the usual lots. Standing needed: Neutral for seed stock, nutrients, irrigation and recovery cartridges, makeup, pipe, produce and Hearth meals; Warm for the Firstlight-4 rack, Hearth-2 cooker, W2 supply, B2 workup bench and every reservoir size. Nothing needs Honored.

### Compatibility and limits

- Requires Phobos Framework 0.53.0 or newer. Kiosks keep their current stock until their next normal restock; nothing is refilled or edited in a save. Offline checks are not gameplay validation.

## [0.25.0] - 2026-09-30 - Draft

### Changed

- The loose items now live in framework/materials.json, read through Phobos Framework with player override files in BepInEx/config/PhobosAgriculture/materials: seed potatoes and lettuce seed, nutrient, makeup and bulk nutrient charges, irrigation charges, raw potatoes, cooked portions and lettuce, the recovery cartridge, and the recorded wastes, each with its mass, price, stack size, market category and art. Shipped figures are unchanged. Masses the crop, recovery and workup models are written for (irrigation and nutrient charges, the cartridge, makeup salts) and the two prices those models quote are bound and refused if a file changes them.

### Compatibility and limits

- Requires Phobos Framework 0.52.0 or newer. Saved items are unchanged. Offline checks are not gameplay validation.

## [0.24.0] - 2026-09-30 - Draft

### Changed

- The Verdemorrow equipment economy now lives in framework/economy.json, read through Phobos Framework with player override files in BepInEx/config/PhobosAgriculture/economy: the rack, cooker, W2, B2 and R3 prices, repair and dismantle work, repair bills and salvage (what the salvage leaves of the housing still returns as a housing remainder), the irrigation pipe, every merchant offer, the regional factors, the lots and the fridge, crate and engineering finds. Shipped figures are unchanged; the R4 and R5 follow the R3 entry. Food, seed and nutrient item prices stay in code for now.

### Compatibility and limits

- Requires Phobos Framework 0.52.0 or newer. Saved data and stocked shops are unchanged. Offline checks are not gameplay validation.

## [0.23.0] - 2026-09-30 - Draft

### Changed

- The R3 reservoir's capacity and empty weight now live in framework/vessels.json, read through Phobos Framework with player override files in BepInEx/config/PhobosAgriculture/vessels. The shipped figures are unchanged. Editing them leaves the reservoirs you own waiting for Accept rather than changing their contents; the R4 and R5 follow the R3 entry. The W2 supply's 20 kg stays fixed: it is the rack reservoir the crop cycle is written for.

### Compatibility and limits

- Requires Phobos Framework 0.51.0 or newer. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.22.0] - 2026-09-30 - Draft

### Changed

- Performance pass, stage 8. The two-second check of your racks, cookers, benches and supplies now reads its machines from one shared sweep of the world in Phobos Framework 0.46.0 instead of walking every object in the world itself (about 8.5 ms each time in the owner's save). A newly placed machine is picked up within about four real seconds; one that is destroyed or removed drops out at once.

### Compatibility and limits

- Requires Phobos Framework 0.46.0 or newer. Saved data is unchanged. Offline checks are not gameplay validation.

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
