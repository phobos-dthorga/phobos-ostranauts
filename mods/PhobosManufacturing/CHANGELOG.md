# Phobos Manufacturing changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

### Added

- Prepared matching Oxsmith EC-4 and CR-4 artwork: oxide-red frames, pale ceramic decks and blackened steel, with an electrode lid and mould tray on the cell, injector caps and paired gas domes on the reactor. Original 1254 x 1254 sources, 256 x 256 working masters and 64 x 64 native review exports are retained with exact prompts and provenance. These are planned machines; this artwork adds no equipment or production.

### Changed

- Redrew the Lixivar AT-2, AT-3 and AT-4 acid tanks and Alembrine Cask-2, Cask-3 and Cask-4 ethanol tanks with plain sealed tops and containment rims. Sage enamel and slate distinguish acid; steel, copper and brass distinguish ethanol. There are no painted readings, gauges or fill strips. All forms keep their existing image names, footprints and storage rules; other silos and small sprites are unchanged.

- Redrew the L2 Canister Filling Station, A2 Cabin Air Regulator and Alembrine Corker-2 Bottling Unit with the same mechanical finish as the new reactor artwork. Each keeps its maker's colours, two-by-two footprint and existing image names on every form. The owner excluded the really small sprites, so materials, pipe tiles, one-tile equipment and floor remain unchanged. Production, prices, ports and saved state are unchanged.

- Redrew the V4 refinery, X2 processor, K2 Sabatier reactor, AX-2 ammonia cracker, LC-3 leach unit, SA-3 acid plant and Copperhead-3 fermenter-still with the more detailed Oxsmith artwork finish. Each keeps its maker's colours, footprint and existing image names. All forms show the replacement artwork, with the game's damage tint where applicable. Production, prices, ports and saved state are unchanged.
- Reviewed every English entry. Rewrote machinery descriptions and batch messages around supplies, work and products. Decant is now Empty into stores; Recover acid from the bund is Recover trapped acid. Recipes and saved batches stay the same.

## [0.54.0] - 2026-10-05 - Draft

### Changed

- The X2 oxygen and K2 carbon-source link lists now say why a canister or store aboard is not offered, including the game's own canisters and carbon monoxide stores. The L2's canister list does the same. The game's canisters have no line fitting, so they have to stand within one tile.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Checked offline; not yet seen in the game.

## [0.53.0] - 2026-10-05 - Draft

### Added

- Phobos' Oxsmith CR-4 Carbothermal Reactor: oxygen out of plain rock by way of methane. A 4 x 4 machine under INSTALL, APPS, 72,000 cr, drawing 30 kW while it works, half the EC-4. Load loose regolith or Silicates ore. A 20 kg regolith lump takes 3.91 kg of methane from a linked store and gives 6.83 kg of carbon monoxide, 0.98 kg of hydrogen and 0.4 kg of water to linked stores, with three ferrosilicon and nine slag, in an hour. A Silicates chunk takes 2.61 kg and gives 4.55 kg of carbon monoxide, 0.65 kg of hydrogen, two ferrosilicon and three slag in 40 minutes.
- Phobos' Fennmark Z2, Z3 and Z4 Carbon Monoxide Stores: 300, 745 and 1,440 kg, from 20,000 cr, red domes. They join the gas line, show their contents on the right-click card and sell back at a refuelling kiosk like the other gas stores.
- The K2 takes carbon monoxide. Its carbon source field, now CO2 or carbon monoxide from, also lists carbon monoxide stores. With one chosen, each cycle takes 0.125 kg of hydrogen and 0.579 kg of carbon monoxide and makes 0.332 kg of methane and 0.372 kg of water. With an X2 splitting that water, a lump's carbon monoxide becomes about 3.9 kg of oxygen and the methane comes back.

### Changed

- The K2's carbon source field is renamed from CO2 canister to CO2 or carbon monoxide from. Reactors already set up keep their source and work as before.

### Save compatibility

- Automatic. A K2 saved before this version reads unchanged; its record gains two fields only once it has held carbon monoxide. Everything else is new.

### Compatibility and limits

- Carbon monoxide is poison with no smell, and it burns. A damaged store leaks it into the room, where the game's own carbon monoxide poisoning applies; with oxygen and something to light it, the contents burn into carbon dioxide. A damaged K2 lets out the little it holds.
- The CR-4 is not cheaper to run than the EC-4: about 68 kWh a lump across the reactor, the K2 and the X2, against 60. It is cheaper to buy, draws half the power at once and uses machines a water-recycling ship already has. One K2 needs about twelve hours for a lump's gas.
- A P1 manifold can burn stored carbon monoxide as cold gas, worth what nitrogen is. The L2 does not bottle it. Stations do not sell it.
- NASA's Carbothermal Reduction Demonstration at Johnson Space Center takes oxygen out of lunar regolith simulant as carbon monoxide. Our reactor is about eight times as efficient as that test rig by its own measure; the yield, the hour and the 30 kW are our figures.
- Source: A. Paz and others, Carbothermal Reduction Demonstration (CaRD), NASA NTRS 20230003977, 2023.
- Checked offline; not yet seen in the game.

## [0.52.0] - 2026-10-05 - Draft

### Added

- Phobos' Oxsmith EC-4 Electrolysis Cell: oxygen out of plain rock. A 4 x 4 machine under INSTALL, APPS, 96,000 cr, drawing 60 kW while it works. Load loose regolith or Silicates ore; it melts the rock and splits it with current. A 20 kg regolith lump gives 3.9 kg of oxygen for a linked oxygen store, 0.4 kg of water for a linked water silo, three ferrosilicon and nine slag in an hour. A 10 kg Silicates chunk gives 2.6 kg of oxygen, two ferrosilicon and three slag in 40 minutes. It uses no reagents.
- Ferrosilicon, the iron and silicon the cell leaves, 2.2 kg a unit. An LC-3 has a new recipe, hydrogen from ferrosilicon: three units and 3.02 kg of water give 0.339 kg of hydrogen for a linked hydrogen store and three spent ferrosilicon for the RM-1 feeder, in 20 minutes.
- The EC-4 can take its feed from a material bin and send its products to a crate, like the other charge machines, and carries on after a reload.

### Save compatibility

- Automatic. Everything here is new; nothing saved before changes. An LC-3 already installed gains an unset hydrogen store link.

### Compatibility and limits

- The cell is not a way to make money. Oxygen sells back at 45 percent and ferrosilicon is worth 2 cr, so a lump's products never repay the lump; Silicates ore sells for far more raw than its oxygen is worth.
- About half of what the cell draws warms its room, and a regolith lump lets 0.1 kg of carbon dioxide into the air. While working it can light a leaking fuel store in its room, like the refinery.
- NASA Kennedy Space Center has demonstrated molten regolith electrolysis on lunar material. The lump's make-up, the 3.9 kg yield, the hour and the 60 kW are our figures. The real ferrosilicon process uses up caustic soda; ours keeps its caustic in the machine.
- Sources: L. Sibille, S. S. Schreiner and J. A. Dominguez, Advanced Concepts for Molten Regolith Electrolysis, 2019; E. R. Weaver and others, The Ferrosilicon Process for the Generation of Hydrogen, NACA Report No. 40, 1920.
- Checked offline; not yet seen in the game.

## [0.51.0] - 2026-10-05 - Draft

### Added

- Regolith in the V4. A new Loose regolith choice on the refinery's Connections page: leave it alone (the default), Regolith bake or Regolith pavers. The bake gives 0.4 kg of water and a 19.5 kg baked lump for the RM-1 feeder in 15 minutes; pavers gives 0.4 kg of water and three 6.5 kg pavers in 40 minutes. A refinery with no choice made never takes a lump, so a shared feed bin keeps its regolith for other machines.
- Sintered regolith floor. Lay a paver from INSTALL, under hull, with a welder: one paver covers a 2 x 2 patch and behaves like the stations' polished regolith floor, floor to pipes, belts and machines. A structure cutter lifts it back into a paver.

### Save compatibility

- Automatic. A refinery's saved record gains the choice only when you set one; nothing else saved changes.

### Compatibility and limits

- A save with the regolith floor laid needs Phobos Manufacturing kept installed; removing the mod leaves those squares without floor.
- A paver is 13 cr, below the floor's 21, because a prospector sells regolith. The 2 percent water is our figure. Until the paver has its own picture it shows the game's loose floor plate; baked regolith keeps the game's regolith picture.
- Checked offline; not yet seen in the game.

## [0.50.0] - 2026-10-05 - Draft

### Added

- Regolith leach on the LC-3: the game's loose regolith finally has a use. One 20 kg lump, 1.08 kg of sulfuric acid and 1.65 kg of water give four Epsom salt and washed tailings in 30 minutes. By chance a leach also gives scrap steel, a Silicates ore chunk or, rarely, a nickel-iron ingot. The result is fixed by the lump, and the odds are a data file you can edit.

### Changed

- Washed tailings stack 25 to a cell, up from 20, so a leach's tailings arrive as one stack.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- The Epsom salt follows the olivine recipe; how much olivine a lump holds is our figure. The scrap steel and the ore chunk are game-like finds.
- A prospector sells regolith, so the odds are lean: a leach is expected to return a little more than it costs.
- Checked offline; not yet seen in the game.

## [0.49.0] - 2026-10-05 - Draft

### Added

- Send products to: the V4, LC-3, SA-3, fermenter-still and Corker-2 bottler can empty their trays into a crate or locker you choose on the Connections page, touching the machine or joined by conveyor belt. While started and powered a machine sends one finished item a second, so a full tray no longer stops it. Name the same store as another machine's feed store, or an RM-1's, and the chain runs by itself. Optional: collecting by hand works as before.

### Save compatibility

- Automatic. Nothing saved changes; the choice is a new record on the machine.

### Compatibility and limits

- Requires Phobos Framework 0.98.0 or newer.
- Material bins take only ore, rock and ice, so use an ordinary container for products. A machine sends anything in its tray that it can make, including nickel-iron ingots or carbon stock you loaded by hand; press Start straight after loading those.
- Checked offline; not yet seen in the game.

## [0.48.0] - 2026-10-05 - Draft

### Changed

- The X2, K2, AX-2 and bottler panels show cycle progress as a share done and the minutes left at full power, instead of a kilowatt-hour meter that read like a countdown. The top line says how long is left; the status below says how far through the cycle it is and how many cycles are finished. Nothing about how the machines work has changed.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- The minutes assume full power; on a weak supply a cycle takes longer than shown.
- Checked offline; not yet seen in the game.

## [0.47.0] - 2026-10-05 - Draft

### Changed

- The V4, LC-3, SA-3, fermenter-still, X2, K2, AX-2, L2 filling station and Corker-2 bottler carry on after loading a save if they were working when it was made. A bound charge resumes from its saved progress, and a started machine waiting for feed goes back to waiting. A failed check leaves the machine stopped with the reason.

### Save compatibility

- Automatic. A save made before this version holds no record of what was running, so machines wait for Start once more after the first load; from the next save on they carry on.

### Compatibility and limits

- Requires Phobos Framework 0.95.0 or newer. Turn the behaviour off with ResumeAfterLoad in the Framework configuration file.
- Checked offline; not yet seen in the game.

## [0.46.0] - 2026-10-05 - Draft

### Changed

- Every Manufacturing machine follows Framework's new machine heat setting: a quarter of its heat by default, reaction heat included. Panels show the heat that actually arrives. Burning methane, ethanol and hydrogen fires are not affected.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.94.0 or newer. Set the share with MachineHeatScale in the Framework configuration file.
- Checked offline; not yet seen in the game.

## [0.45.0] - 2026-10-04 - Draft

### Added

- Add-ons and your own data files can add items of their own, with their own pictures, for Manufacturing's recipes to make and use. A machine admits an added item when one of its recipes takes it. See the add-on publishing guide.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.92.0 or newer.
- Added items are not sold by merchants. A save holding an add-on's items needs that add-on enabled.
- Checked offline; not yet seen in the game.

## [0.44.0] - 2026-10-04 - Draft

### Added

- The gangue wash on the LC-3 (owner request, 4 October 2026): four lumps of gangue and a kilogram of sulfuric acid, with 10 kg of water on hand, wash for 20 minutes into 13 kg of results by chance. About half the washes give only washed tailings; the rest give 2 scrap steel or 2 scrap aluminium, and rarely a nickel-iron ingot, with tailings for the remainder.
- A wash's result is set by its four lumps and never rolled again: reloading, or cancelling and starting again, changes nothing.
- Washed tailings come as one stack and are a remainder the RM-1 reaction mass feeder grinds.
- The odds are a data file players can edit: the new outcomes pack, with overrides in BepInEx/config/PhobosManufacturing/outcomes. See the editing guide.

### Save compatibility

- Automatic. New recipes and one new item only.

### Compatibility and limits

- Requires Phobos Framework 0.88.0 or newer.
- The wash is game-like: rock does carry nickel-iron grains, but free aluminium and the odds are gameplay choices.
- Checked offline; not yet seen in the game.

## [0.43.0] - 2026-10-04 - Draft

### Added

- The Slingwright RM-1 Reaction Mass Feeder (owner decisions, 4 October 2026): a 1 x 1 powered grinder for an RCS intake, INSTALL, HVAC. It turns remainders (slag, cakes, calcine, rejects, retained waste) into up to 60 kg of reaction mass that the thrusters burn like nitrogen. It grinds by itself at 3 kW, 0.15 kWh a kilogram, takes remainders only, and can draw them from a bin or locker under Take feed from.
- Every terminal remainder this mod makes is now declared, so the feeder is its consumer.
- Sold at the usual Manufacturing merchants and, for scrip, at faction kiosks (Friendly standing).

### Save compatibility

- Automatic. New equipment only; remainders already aboard are accepted as they are.

### Compatibility and limits

- Requires Phobos Framework 0.87.0 or newer.
- The exhaust speed, energy and worth of reaction mass are gameplay choices; throwing ground solids through gas thrusters is game-like.
- Checked offline; not yet seen in the game.

## [0.42.0] - 2026-10-04 - Draft

### Added

- Optional feed store for the V4, LC-3, SA-3 and fermenter-still (owner request, 4 October 2026): under Connections, Take feed from, choose a material bin or other store that touches the machine or is joined to it by conveyor belt. While started and powered, the machine takes exact charges from there as well as from its own inventory. Loading by hand works as before.

### Save compatibility

- Automatic. Nothing saved changes; the choice is a new record on the machine.

### Compatibility and limits

- Requires Phobos Framework 0.85.0 or newer. Conveyor belts and material bins come with Phobos Framework and Phobos Shipbreaker; any ordinary container that touches the machine also works.
- A repeating machine still never takes what it makes, from a store either.
- Checked offline; not yet seen in the game.

## [0.41.0] - 2026-10-04 - Draft

### Fixed

- The V4 refinery, LC-3, SA-3 and fermenter-still now use what you put in their inventory. The game shows one inventory for a machine, so a charge loaded by hand lay beside the products and the machine reported an empty feed. Start now takes one exact charge out of the inventory, and Cancel puts it back.
- A running machine never takes what it has just made: a V4 does not carburise its own ingots or burn its own carbon by itself. Press Start again to work one such charge.

### Save compatibility

- Automatic. Nothing saved changes; a charge already inside a machine's inventory is picked up at the next Start.

### Compatibility and limits

- Requires Phobos Framework 0.83.0 or newer.
- Checked offline; not yet seen in the game.

## [0.40.0] - 2026-10-04 - Draft

### Added

- The Alembrine Corker-2 Bottling Unit (owner decisions, 4 October 2026): a 2 x 2, 0.4 kW unit that draws ethanol from a linked Cask tank and water from a linked water silo and fills seven servings of Alembrine spirit at a time into its tray. Each half-hour batch takes 84.7 g of ethanol and 160.3 g of water. Press Start; it keeps bottling while supplied and waits for Start after a reload.
- Alembrine spirit: a 35 g serving at 40% alcohol that crew drink like the game's own liquor, worth about 8 cr. A beet rack's ethanol bottles into about 31 servings.
- The bottling unit is sold at the usual Manufacturing merchants and, for scrip, at faction kiosks (Friendly standing). No merchant sells the spirit.

### Save compatibility

- Automatic. New equipment and a new item only.

### Compatibility and limits

- Requires Phobos Framework 0.80.0 or newer. Ethanol comes from the fermenter-still, which needs Phobos Agriculture 0.46.0 or newer.
- The spirit's strength and mass follow published densities of ethanol and water, leaving out the small volume loss on mixing; its price and the batch power are gameplay choices.
- Checked offline; not yet seen in the game.

## [0.39.0] - 2026-10-04 - Draft

### Added

- The Alembrine Copperhead-3 Fermenter-Still (owner decisions, 4 October 2026): a 3 x 3, 3 kW fermenter and pot still for Phobos Agriculture's sugar beets or beet sugar. Six beets give 0.253 kg of ethanol, 0.241 kg of carbon dioxide and 2 kg of water; six sugar packets give 0.208 kg of ethanol and 0.199 kg of carbon dioxide. Ethanol goes to a Cask tank, CO2 to a store, water to a silo, and a spent mash to the tray.
- A working still can light an ethanol spill in its room.
- Sold at the usual Manufacturing merchants and, for scrip, at faction kiosks (Friendly standing).

### Save compatibility

- Automatic. New equipment only.

### Compatibility and limits

- Requires Phobos Framework 0.80.0 or newer. The fermenter-still needs Phobos Agriculture 0.46.0 or newer for its beets and sugar; without it the still is idle and says why.
- The fermented share of the sugar (92%) and the still's power are gameplay choices within published fermentation figures.
- Checked offline; not yet seen in the game.

## [0.38.0] - 2026-10-04 - Draft

### Added

- Alembrine ethanol tanks (owner decisions, 4 October 2026): the Cask-2, Cask-3 and Cask-4 hold 495, about 1,230 and about 2,380 kg of ethanol. They are bunded like the acid tanks, with a canister rack and Pour ethanol into on their panel. A station's refuelling kiosk buys stored ethanol back at 45% of its 20 cr per kg value and never sells it.
- The Alembrine ethanol line (INSTALL, HVAC) joins casks and machines like the acid line, holds its ethanol and drains into a drain canister.
- Ethanol burns: a damaged cask or line segment can catch fire when the room has oxygen and something to light it, through the game's own fire and blast. Otherwise the spill waits in the bund.
- The fermenter-still and bottler that make and use ethanol come in the next release.

### Changed

- The acid tanks and acid line share their code with the ethanol ones; they behave as before.

### Save compatibility

- Automatic. New equipment only; acid tanks, the acid line and their records are unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.80.0 or newer.
- On screen the ethanol line shares a lane with Shipbreaker's furnace coolant conduit; lay them on different tiles.
- The share that burns and the ethanol price are gameplay choices; density and heat of combustion follow published figures.
- Checked offline; not yet seen in the game.

## [0.37.0] - 2026-10-04 - Draft

### Added

- Straw from Phobos Agriculture 0.44.0 (owner decision, 4 October 2026: crop waste both ways). The V4 burns one straw bale with stored oxygen into 1.275 kg of carbon dioxide for a linked store, plus water and plant ash, so an A2 can dose a grow room from crop waste. It chars four bales into one carbon stock, with water, 0.261 kg of methane for a linked methane store and 0.719 kg of carbon dioxide into the room.
- Plant ash: the small terminal remainder of a burned or charred bale.

### Save compatibility

- Automatic. Two new recipe revisions; earlier charges settle as before.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer. The straw charges need Phobos Agriculture 0.44.0 or newer and stay hidden without it.
- Char yield (29% of the plant matter as carbon) is authored within slow-pyrolysis findings; complete combustion is authored. Charring waste bales gains value, an owner-approved exception to the refining step rule.
- Checked offline; not yet seen in the game.

## [0.36.0] - 2026-10-04 - Draft

### Added

- Gas stores and acid tanks show what they hold when you right-click them: hydrogen, methane, oxygen, nitrogen, carbon dioxide, ammonia and sulfuric acid, in kilograms, each in the game's own colour for that gas.

### Fixed

- Buying oxygen, nitrogen, carbon dioxide or acid at a station, or selling stored gas back, no longer fails with The destination or its available room changed when the store is on a line or feeding a machine (Framework 0.79.0).

### Save compatibility

- Automatic. Stores in older saves show their contents once their ship has loaded.

### Compatibility and limits

- Requires Phobos Framework 0.79.0 or newer.

## [0.35.0] - 2026-10-04 - Draft

### Changed

- The X2, K2, AX-2, L2, A2, V4, LC-3 and SA-3 take power from the tile row directly behind them, so set against a wall they reach the electrical conduit in it, like the game's own equipment.

### Save compatibility

- Automatic: installed machines take their new power points as the save loads. Manual step, stated plainly: if you ran conduit under a machine's back row rather than in the wall or row behind it, run it one tile further back; a machine with its back against a powered wall needs nothing.

### Compatibility and limits

- Requires Phobos Framework 0.77.0 or newer.

## [0.34.0] - 2026-10-03 - Draft

### Fixed

- The X2, K2, AX-2, L2, V4, LC-3 and SA-3 no longer claim to be waiting for the room to cool when they stand in vacuum. They say they do not work in the vacuum of space and need a room with at least 10 kPa of air; a room that is too warm gives its temperature. The V4 still warns that a waiting melt freezes, and the SA-3 that its roaster gives off far more heat than it draws.

### Save compatibility

- None. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.76.0 or newer.

## [0.33.0] - 2026-10-03 - Draft

### Changed

- Repairs on Manufacturing equipment and the acid line use up their parts and give back only the repaired machine, as the game's own repairs do. No more Spent Service Parts.

### Save compatibility

- Automatic. Spent Service Parts left by older repairs are removed from each ship as it loads, wherever they lie (deck, containers, machines, pockets), with one crew-log line on your ships saying how many went. Nothing else in the save changes, and repairs already under way finish normally.

### Compatibility and limits

- Requires Phobos Framework 0.74.0 or newer.

## [0.32.0] - 2026-10-03 - Draft

### Fixed

- Reloading or quitting no longer treats every gas store, K2 and cracker as destroyed in play, which could vent its gas into a ship that was only being unloaded and log a loss. The save keeps what they hold.

### What to expect

- With Phobos Framework 0.73.0, gas, water and acid lines now link stores and machines as laid, including the C4 carbon dioxide and nitrogen stores that never linked by pipe before. A store that is still loose must be installed first.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.73.0 or newer. Owner gameplay checks remain pending.

## [0.31.0] - 2026-10-01 - Draft

### Changed

- Performance pass, first round (owner request, 1 October 2026). None of this changes what the equipment does, except the L2 fix below.
- The feed hooks that run for every container in the game allocate nothing now, and a running V4, LC-3 or SA-3 no longer rebuilds its recipe lists on every power step.
- The K2 and AX-2 read each linked store once per check instead of twice.
- A damaged fuel store looks for an ignition source in one pass over its ship, and only when the room has the oxygen to burn; it made three passes every two seconds before.
- The RCS reserve the game asks for every frame is summed without allocating.

### Fixed

- An L2 filling station that could move nothing (a full target, an empty source) searched for work on every power step at working power. It now stands down and looks again every five seconds, as intended.

### Save compatibility

- Automatic. Nothing saved changes.

### Compatibility and limits

- Requires Phobos Framework 0.71.0 or newer. Static review only; settling machine records every two seconds instead of every step is left for the owner's decision and a measurement.

## [0.30.0] - 2026-10-01 - Draft

### Changed

- The V4, LC-3 and SA-3 product trays are 4 x 3 cells, where each was 8 x 8 (owner direction, 1 October 2026: size every inventory to its job). Products now arrive as stacks: ingots ten to a cell, salts twenty, the 39 makeup packets of a formulation in two cells. Each tray holds two charges of every recipe, except that the LC-3 holds one evaporite leach (its two residues are 2 x 2 each) and waits for room before the next.

### Save compatibility

- Automatic. A tray saved with more than fits re-packs into stacks when the save loads, and anything that still has no place is put on the deck beside the machine, with one line in the crew log. Charges in progress finish with the same products.

### Compatibility and limits

- Requires Phobos Framework 0.71.0 or newer. Residues, cakes and calcines never stack: empty them between charges, by hand or with a crew output store. Offline checks are not gameplay validation.

## [0.29.0] - 2026-10-01 - Draft

### Changed

- The A2 cabin air regulator no longer has an inventory (owner direction, 1 October 2026: size every inventory to its job). It never stored anything; the grid was a leftover.
- Every Manufacturing inventory now declares what it is for. The V4, LC-3 and SA-3 product trays keep their present size until products can be delivered into stacks.

### Save compatibility

- Automatic. Anything a save left inside an A2 is put on the deck beside it when the save loads, as ordinary cargo, and the crew log says so. Nothing is destroyed.

### Compatibility and limits

- Requires Phobos Framework 0.70.0 or newer. Offline checks are not gameplay validation.

## [0.28.0] - 2026-10-01 - Draft

### Fixed

- Machines link to water silos and to nitrogen, carbon dioxide and other gas stores along a line laid under or right beside both, on any side (Phobos Framework 0.69.0; owner report, 1 October 2026). Before, the line had to end on one particular tile beside each, so the X2 found no water silo and the K2 found no carbon dioxide store unless they stood within one tile.

### Added

- Every link choice on the V4, LC-3, SA-3, X2, K2, AX-2, A2, L2, P1, the gas stores and the acid tanks says why a silo, tank or store aboard is not offered: loose, damaged, locked, no working line touching it, a drained line, or two lines that do not meet.
- A store's transfer choice and an acid tank's pour choice now show whenever another store or tank of the same contents is aboard, in reach or not, so the sheet can say what keeps it out. The V4, LC-3 and SA-3 do the same for their optional links.

### Changed

- The A2, L2 and P1 no longer offer a damaged or locked store that merely touches them, matching every other machine. A link already saved to such a store is kept.

### Save compatibility

- Automatic. No saved record changes.

### Compatibility and limits

- Requires Phobos Framework 0.69.0 or newer. The A2 still adds nothing to a room below 10 kPa. Offline checks are not gameplay validation.

## [0.27.0] - 2026-10-01 - Draft

### Added

- The V4 cracks methane: link a methane store and a hydrogen store, load one carbon stock, and it turns 4 kg of methane into four carbon black (1 kg each, 12 cr) and 1 kg of hydrogen. With a K2 this returns the Sabatier's methane as hydrogen and leaves the carbon as a solid, as NASA's Bosch work for the ISS set out to do.
- The V4 burns carbon black: link an oxygen store and a carbon dioxide store, load one carbon black, and it makes 3.7 kg of carbon dioxide for a store, warming the room by about 9 kWh.
- The V4 reactivates the game's spent CO2 scrubber cartridges and EVA filters, four at a time: three come back ready and the fourth is an exhausted sorbent remainder. This is our simplification, not real lithium chemistry; the game's scrubber has already sent the carbon dioxide to a canister.
- The A2 cabin air regulator can dose a grow room with carbon dioxide from a linked carbon dioxide store: 0.05, 0.10 or 0.20 kPa, up to 1 kg an hour, never past 0.25 kPa, below the 0.3 kPa where the game starts to warn the crew. Crops stop growing in a room with none.
- New materials: Phobos' Fennmark carbon black and exhausted sorbent, with recoloured sprites of existing masters.

### Changed

- The V4 links stores for gases it draws (methane, oxygen) as well as those it fills (ammonia, carbon dioxide and now hydrogen). A charge that draws a gas is chosen only when that store is linked, so a carbon stock waiting for a nickel steel charge is never cracked by surprise.
- The V4's feed now takes spent CO2 filters and carbon black.

### Save compatibility

- Automatic. Refineries keep their links and any bound charge. A2 regulators saved before 0.27.0 load with carbon dioxide left alone; their records gain the three new fields on the next save.

### Compatibility and limits

- Requires Phobos Framework 0.68.0 or newer. Downgrading to 0.26.0 or older after a save marks A2 records as needing Accept on their panel. Carbon black is priced low on purpose, so methane made from bought water and carbon dioxide never pays its way back. Reactivating cartridges saves purchases; it makes no money, because the game prices spent and ready cartridges alike. The carbon-on-carbon cracking route is cited from memory and marked to check. Offline checks are not gameplay validation.

## [0.26.0] - 2026-10-01 - Draft

### Added

- Phobos' Fennmark nickel steel ingots: 4 kg, 260 cr, stack ten. The V4 makes four from four nickel-iron ingots and one carbon stock, with or without Shipbreaker. This is the end of the mined iron chain, separate from Shipbreaker's plain steel ingot.
- The station buys back from every Fennmark gas store (hydrogen, methane, oxygen, nitrogen, carbon dioxide and ammonia) and from Lixivar acid tanks, at 45% of the game's own price for that gas. Look for the Sell lines under Bulk supplies at a refuelling kiosk.

### Changed

- Refining is now a paying business, by owner decision of 1 October 2026: a charge from mined ore earns roughly 1.5 to 2.5 times the ore at base prices. New prices: nickel-iron ingot 220 cr (was 20), carbon stock 38 (10), potassium sulfate 190 (42), phosphate concentrate 110 (12), struvite 125 (17), Epsom salt 13 (7), ammonium sulfate 20 (8), phosphoric acid flask 300 (30).
- The V4 makes nickel steel from nickel-iron and carbon; the plain steel charge is no longer offered for new charges. Nickel-iron ingots and carbon stock now enter the V4 feed without Shipbreaker.
- Fertiliser stays above the band by owner decision, because fertiliser is rare in the game's world. Crop nutrients in a hopper are not bought back by the station.

### Save compatibility

- Automatic. Ingots, carbon, salts and flasks already aboard take their new prices when the save loads. A V4 already working a plain steel charge finishes it as steel, and still needs Shipbreaker for that.

### Compatibility and limits

- Requires Phobos Framework 0.68.0 or newer. The prices are authored balance, accepted by the owner pending gameplay testing; payback on the smaller machines is long, from 300 to 740 hours of running. The nickel steel ingot uses a recoloured ingot sprite. Offline checks are not gameplay validation.

## [0.25.0] - 2026-10-01 - Draft

### Changed

- The acid line now holds its acid, about 0.9 kg on every tile, filled from the acid tanks on it and kept there until drained. Right-click an installed acid line and choose Drain line into canister: a crew member drains the whole run into a Framework drain canister, 36.7 kg of acid to a canister. Draining closes the run, so no machine reaches a tank through it until you choose Return line to service.
- Every AT acid tank has a four-place canister rack. Haul a canister of acid into it, with the game's own Haul orders or a hauling mod such as Common Sense, and the acid pours into the tank, leaving the empty canister in the rack. Nothing but drain canisters fits.
- Taking up an acid line is refused while it holds acid, whatever it is linked to; drain it first.

### Hazards

- A damaged or destroyed acid line section releases a ten-thousandth of the acid it holds into the room as the game's own H2SO4, and the crew are warned. A damaged section keeps the rest until drained. A destroyed one loses the rest with it, and the log records how much. The tank's bund no longer catches spills from the line, since the line now holds its own acid.

### Save compatibility

- Automatic. Acid lines already laid start empty and fill from their tanks within seconds, which takes about 0.9 kg per tile from those tanks. Tanks gain their canister rack when the save loads.

### Compatibility and limits

- Requires Phobos Framework 0.63.0 or newer. The hold-up is authored, a metre of 25 mm bore per tile at the tanks' 98% acid density, not a flow simulation. Offline checks are not gameplay validation.

## [0.24.0] - 2026-10-01 - Draft

### Added

- The Lixivar acid line: lined pipe for concentrated sulfuric acid, laid tile by tile from INSTALL, HVAC, sold in lots of 128 at 6 cr a segment and at the faction kiosks at any standing. The LC-3, the SA-3 and every AT acid tank have an acid port on the tile beside their right-hand side, one row below the gas port. Acid line between two ports, or the equipment touching, links them. It shares tiles with the other lines in its own violet lane.
- Acid tanks pour into another acid tank they touch or share an acid line with.

### Changed

- An LC-3 or SA-3 reaches an acid tank by touching it or through the acid line, never across open floor.
- Machine descriptions and link refusals now say a store or silo must touch the machine or share its water, gas or acid line, where they used to say within one tile; O2 and CO2 canisters and nutrient hoppers still link by touching only. Water vessels are called water silos, after Framework 0.58.0.
- An acid tank's bund message says to recover the acid once the tank is intact.

### Hazards

- A wet acid line spills when it is damaged or destroyed. A segment is wet while it joins an acid tank to a machine linked to that tank. The spill draws about a kilogram from that tank: a ten-thousandth mists into the room as the game's own H2SO4, and the rest is held in the tank's bund until you recover it from the tank's panel. The crew log says which tank and how much. Taking up a wet segment is refused until the tank is unlinked from its machines. The one-kilogram hold-up is ours (a metre of 25 mm bore line holds about 0.9 kg of 98% acid); the mist fraction is the tanks' own.

### Documentation

- With Phobos Framework 0.58.0 or newer, the process water silos come with Framework itself, so water work no longer needs Shipbreaker or Agriculture installed. The guide and page say so.

### Compatibility and limits

- Requires Phobos Framework 0.60.0 or newer. Machines and tanks already aboard gain their acid port when the save loads; acid links made before were all touching and keep working. Offline checks are not gameplay validation.

## [0.23.0] - 2026-10-01 - Draft

### Added

- Water and gas ports on every machine: the V4, X2, K2, LC-3 and SA-3 take process water through the port on their left-hand side, and every machine takes gas through the port on its right-hand side (the AX-2 has no water port). Lay Framework's process-water or gas line from a port to a store's, or place the two touching.
- Stores are shared: one store can serve up to eight machines of each kind, so a hydrogen store fed by an X2 can supply a K2, and one water tank can feed several machines. Link lists name how each store is reached and mark a full destination or an empty source; a store's panel lists every machine linked to it.
- A caution on the stores' panels, and one crew-log note, when an oxygen store and a fuel store share one gas line. Nothing is blocked.

### Changed

- The link rule applies everywhere: a machine links to a store, and a link keeps working, only while the two touch (footprints within one tile, as before) or share a line network, never across open floor. Chains of touching machines and stores count, so a tank touching a V4 touching an X2 serves the X2. Oxygen and CO2 stores for the X2 and K2 follow the same rule; the game's own canisters still need to touch.
- The P1 manifold, L2 filling station, A2 regulator and store-to-store transfers use the same gas-line network: every store whose port a line reaches is offered, with no 64-tile limit.
- The gas line moved to Framework with its saved identity, name, price and stock unchanged; it no longer needs Manufacturing installed. Only power points are treated as the V4, LC-3 and SA-3's electrical inputs.

### Compatibility and limits

- Requires Phobos Framework 0.57.0 or newer. Saved links and records are kept, and laid gas line stays in place. A link between a machine and a store that neither touch nor share a line now reports the store as not ready: move them together or lay the line between their ports. Offline checks are not gameplay validation.

## [0.22.0] - 2026-09-30 - Draft

### Changed

- The Fennmark gas line draws in its own lane and depth, so it can share a tile with other kinds of line. The PDA's Conduits filter now takes it, and painting jobs on Equipment leaves it alone.
- The Control Panel runs on Framework's shared panel host, with the same pages and controls.

### Compatibility and limits

- Requires Phobos Framework 0.56.0 or newer. Saves are unchanged, and links behave as before in this version. Offline checks are not gameplay validation.

## [0.21.0] - 2026-09-30 - Draft

### Fixed

- The Control Panel redraws the moment a link or setting is applied, and after Start, Pause or Accept, so the Connections page shows the new link straight away. Each selection sheet opens with the current link or setting marked.
- The console groups for Manufacturing equipment in Shipbreaker's industrial console now have names instead of bracketed keys, and accepting a protected acid tank shows the right notice.

### Changed

- The player guide and item reference give the chunk odds per pull from a deposit (for example a clay hydrates chunk about one pull in eleven from a C-class deposit, a sulfide nodule about one in thirty-six from an M-class deposit), and show how to read a live table with the console command phobosframework loot.

### Compatibility and limits

- Requires Phobos Framework 0.55.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.20.0] - 2026-09-30 - Draft

### Added

- The LC-3 takes acid. Link an acid tank within one tile, and for crop nutrients a Groundwork nutrient hopper, from its panel or C1.
- Epsom salt from olivine: one of the game's 10 kg olivine chunks with 8.64 kg of sulfuric acid and 9.52 kg of water gives 32 Epsom salt (magnesium sulfate, 0.432 kg each) and a 14.33 kg olivine leach cake in an hour. The reaction also puts about 5.3 kWh of heat into the room.
- Acid-route struvite: one phosphoric acid flask from the SA-3 and three Epsom salt, with 0.27 kg of ammonia, give three struvite and three ammonium sulfate in ten minutes, and return 94 g of water to the linked vessel. This is the second struvite route; the crust route is unchanged.
- Crop nutrients, with Phobos Agriculture 0.27.0 or newer: one potassium sulfate, one struvite, one Epsom salt and one ammonium sulfate, with 0.25 kg of ammonia and 0.73 kg of sulfuric acid drawn from their links, make 2.77 kg of crop nutrients straight into a linked nutrient hopper in five minutes. A W2 then doses from that hopper as it does from a bought fill.
- Overhead sprites for Epsom salt, ammonium sulfate and the leach cake.

### Changed

- The SA-3's sulfide nodule carries a little more phosphide (1.058 kg), so one phosphoric acid flask (now 0.515 kg) holds exactly the phosphorus of three struvite. The plant draws 6.28 kg of oxygen and 1.58 kg of water per nodule and leaves 9.535 kg of calcine. Version 0.19.0 was never published, so no save holds the old figures.
- The plan's separate ammonium sulfate charge became the formulation's own ammoniation step, as in a fertiliser granulation plant: a charge must bind at least one item, so ammonia and acid alone cannot make a charge.

### Balance

- Epsom salt is 7 cr and ammonium sulfate 8 cr; the leach cake is trash. None of them is sold by merchants. Crop nutrients made aboard carry Agriculture's own 1,500 cr/kg (the owner's formulation decision). Bagged from the hopper into bulk charges they sell like any other charge; every salt in the blend is made aboard from mined feed, so bought stock alone never pays. This makes the formulation a strong earner: about 4,150 cr of nutrients a charge from about 75 cr of salts.

### Compatibility and limits

- Requires Phobos Framework 0.54.0 or newer. Phobos Agriculture stays optional; without 0.27.0 or newer the crop nutrient recipe is not offered. Existing charges and saves are unchanged. The olivine's make-up (7.000 kg of Fa29 olivine and 3.000 kg of other rock), the Epsom yield, the blend's nitrogen level and the energies are authored; the reactions and sources are in the refinery design record. The blend is sulfate- and ammonium-rich with no calcium, nitrate or trace elements: Agriculture counts only its total. Offline checks are not gameplay validation.

## [0.19.0] - 2026-09-30 - Draft

### Added

- Phobos' Lixivar SA-3 Sulfuric Acid Plant (3 x 3, 260 kg, 4 kW): one sulfide nodule an hour, roasted in 6.28 kg of oxygen from a linked oxygen store with 1.58 kg of water from a linked vessel, into 7.81 kg of sulfuric acid for a linked acid tank, a 0.515 kg phosphoric acid flask and 9.535 kg of roasted calcine. The reactions release about 21 kWh a nodule into the room on top of the plant's electricity: give it a big, cooled room, or it waits for the air to cool.
- Phobos' Lixivar AT-2, AT-3 and AT-4 Sulfuric Acid Tanks (2 x 2, 3 x 3 and 4 x 4; 1,150, 2,850 and 5,520 kg). They are bunded liquid tanks, not gas stores: no gas line, and never a source for thrusters, filling stations or cabin air. Pour acid from one tank into another within one tile from the panel.
- Station refuelling kiosks sell sulfuric acid into an acid tank under Bulk supplies, at the game's own price (3.1 cr/kg), in 10 kg steps. Nothing sells back.
- The sulfide nodule, a mined 10 kg chunk of troilite and schreibersite of the kind found in iron meteorites, takes a share of the meteoric iron in M-class (4%) and S-class (2%) finds. Government kiosks buy it like other ore.
- Overhead sprites for the SA-3, the three tank sizes, the nodule, the flask and the calcine.

### Hazards

- A damaged acid tank's bund holds the acid, but a ten-thousandth of it gets into the room as sulfuric acid mist, the game's own H2SO4, whose poisoning bands apply; a destroyed tank mists the same share and the rest is lost. The crew are warned. Repair the tank, then recover the acid from the bund.

### Balance

- The SA-3 costs 56,000 cr (Trusted at the faction kiosks) and the AT-2 16,000 cr (Friendly), sold where the other machines and stores are. The phosphoric acid flask is 30 cr; the calcine is trash. The SA-3 and the AT-2 join Manufacturing's one-in-twenty share of engineering salvage, so each machine's and store's part of it is a little smaller; the total is unchanged.

### Compatibility and limits

- Requires Phobos Framework 0.54.0 or newer. Saves are unchanged. The nodule's proportions, the plant's energy and the mist fraction are authored; the reactions and sources are in the refinery design record. Offline checks are not gameplay validation.

## [0.18.0] - 2026-09-30 - Draft

### Added

- Phobos' Lixivar LC-3 Leach and Crystallise Unit, a 3 x 3, 220 kg machine under a new brand, Lixivar. Choose a recipe on its panel, load that charge and Start. It draws 12 kW while working and warms its room. Link a water vessel and, for struvite, an ammonia store, each within one tile.
- Evaporite leach: an evaporite crust in 20 kg of circulating water gives 0.70 kg of potassium sulfate, 0.25 kg of phosphate concentrate, a 6.80 kg leached residue and a 2.25 kg brine salt cake in an hour. The water comes back.
- Struvite: one phosphate concentrate with 30 g of ammonia and 0.22 kg of water gives 0.43 kg of struvite, a slow-release fertiliser, and 70 g of caustic remainder in five minutes.
- Makeup formulation, with Phobos Agriculture: one potassium sulfate and two struvite make 39 Verdemorrow Groundwork makeup salts, nothing left over. Without Agriculture the recipe is not offered and the salts keep as stock.
- A new V4 charge: calcining a leached residue drives its carbon dioxide (0.26 kg) into a linked carbon dioxide store and leaves 6.54 kg of calcined residue. An L2 filling station can bottle it into a CO2 canister for the K2 Sabatier reactor.
- The evaporite crust, a mined 10 kg chunk of salt-streaked clay after the minerals NASA's OSIRIS-REx team found in Bennu samples, takes a five percent share of the C-class mining roll from silicates. Government kiosks buy it like other ore.
- Overhead sprites for the LC-3, the crust and the seven new materials.

### Balance

- The LC-3 costs 48,000 cr and is sold wherever the other machines are, including the CCRE and GalCon faction kiosks at Trusted standing. Its products are never sold by merchants. Potassium sulfate is 42 cr, struvite 17 cr and phosphate concentrate 12 cr; remainders are trash. The makeup salts carry Agriculture's own 30 cr price per 40 g packet (owner decision).

### Compatibility and limits

- Requires Phobos Framework 0.54.0 or newer. Phobos Agriculture is optional. Existing V4 charges and saves are unchanged. The crust's mineral proportions and the recipes' energies are authored gameplay figures; the reactions and sources are in the refinery design record. Offline checks are not gameplay validation.

## [0.17.0] - 2026-09-30 - Draft

### Changed

- The V4 refinery now runs on a shared charge-machine engine that later Manufacturing machines will use too. Its recipes, prices, links, controls, messages and saved records are unchanged; a V4 part-way through a charge carries on as before.
- The V4's physical figures (4 x 4 footprint, 180 kg, 24 kW working and 0.1 kW idle, 15 percent of its heat into the room, six feed cells, artwork and connection points) now live in an equipment data file shipped with the mod. Player files cannot change them yet, because a footprint or connection change would move a V4 already placed in a save.

### Compatibility and limits

- Requires Phobos Framework 0.54.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.16.0] - 2026-09-30 - Draft

### Added

- Every Fennmark machine, every gas store size and the propellant line are now also sold for scrip at the CCRE faction kiosks at Zhonghuamen Terminal and Port Yangshan (Mars) and the GalCon faction kiosk at Port Mojave (Ceres), in the usual lots. Standing needed: Neutral for propellant line; Friendly for the gas stores, P1 manifold, L2 canister filler and A2 cabin air regulator; Trusted for the X2 chemical processor, AX-2 ammonia cracker, K2 Sabatier reactor and V4 refinery. Nothing needs Honored.

### Compatibility and limits

- Requires Phobos Framework 0.53.0 or newer. Kiosks keep their current stock until their next normal restock; nothing is refilled or edited in a save. Offline checks are not gameplay validation.

## [0.15.0] - 2026-09-30 - Draft

### Balance

- The nickel-iron ingot returns to 20 cr (5 cr per kilogram, above scrap steel and far below the ore). Refining is no longer required to lose value: four ingots and one carbon stock (both only ever refined from mined ore; no shop sells them) now carburise into four 25 cr steel ingots, an eleven percent gain at base prices for a 250 kW melt. The other charges keep their prices; ore is still worth far more sold than refined, and the salt crust charge is for the nitrogen.

### Compatibility and limits

- Requires Phobos Framework 0.52.0 or newer. Ingots already in a save keep the price they were made with. Offline checks are not gameplay validation.

## [0.14.0] - 2026-09-30 - Draft

### Changed

- Merchant offers, regional stock and world finds are applied through Phobos Framework's shared stock resolver instead of this mod's own copy. The offers, chances, lots and finds are unchanged; only the internal ids of the propellant line offers differ, which no save or shop records.

### Compatibility and limits

- Requires Phobos Framework 0.52.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.13.0] - 2026-09-30 - Draft

### Changed

- The six gas store families (H2, M2, O2, N2, C2, Q2) read their capacity, empty weight and damaged leak rate from framework/vessels.json, read through Phobos Framework with player override files in BepInEx/config/PhobosManufacturing/vessels. The shipped figures are unchanged, so nothing in a save moves. Editing a capacity or weight leaves the stores you own waiting for Accept rather than changing their contents; the medium and large sizes follow the small entry.

### Compatibility and limits

- Requires Phobos Framework 0.51.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.12.0] - 2026-09-30 - Draft

### Changed

- The six V4 charges now live in framework/process-recipes.json and the seven materials (ingots, carbon, remainders, mined chunks) in framework/materials.json, read through Phobos Framework with player overrides in BepInEx/config/PhobosManufacturing. Every value is the same as 0.11.0; nothing changes in play. The shipped charge revisions are frozen: a running charge keeps its revision, and a changed charge means a new revision.

### Compatibility and limits

- Requires Phobos Framework 0.50.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.11.0] - 2026-09-30 - Draft

### Changed

- The economy now lives in a data file, framework/economy.json: every machine's and store's price, install, uninstall, repair and dismantle work, repair bill, salvage, Restore time, the merchant offers, lots, regional availability and the engineering-salvage odds. Nothing changes in play: every value is the same as 0.10.0. Players can override entries with files in BepInEx/config/PhobosManufacturing/economy; see Editing the Phobos data files in the guides.

### Compatibility and limits

- Requires Phobos Framework 0.49.0 or newer. Saves are unchanged. Offline checks are not gameplay validation.

## [0.10.0] - 2026-09-30 - Draft

### Added

- Phobos' Tolvane AX-2 Ammonia Cracker (2 x 2, 150 kg, 2 kW, 42,000 cr, the INSTALL menu APPS tab): turns stored ammonia into the two gases the ship uses. Each one-hour cycle splits 1 kg of ammonia from a linked ammonia store into 0.822 kg of nitrogen for a linked nitrogen store and 0.178 kg of hydrogen for a linked hydrogen store (2 NH3 to N2 + 3 H2, the reverse of how industry makes ammonia). Mined salt crust now feeds the cabin air through an A2, and hydrogen through a K2 or the RCS.
- Tolvane is a new brand for the nitrogen line, with its own colours (teal frame, pale enamel lid, yellow corner brackets), because Fennmark already carries a dozen machine and store families. It has a new PixelLab sprite.
- The cracker links on its panel and the C1 (Ammonia from, Nitrogen to, Hydrogen to). A hydrogen store can take from an X2 and a cracker at once, and a nitrogen store can feed an A2 while a cracker fills it.

### Hazards

- The split soaks up part of the heat, so about 1.25 kW warms the room while the cracker works; it waits for the room to cool at 40 C. A damaged or destroyed cracker releases the ammonia and nitrogen it holds into the room, and its hydrogen burns or escapes like a damaged K2's. It holds at most about a kilogram.

### Compatibility and limits

- Requires Phobos Framework 0.48.0 or newer. Saved machines and stores are unchanged. Complete conversion each cycle is an authored simplification. Offline checks are not gameplay validation.

## [0.9.0] - 2026-09-30 - Draft

### Added

- Ammonium salt crust, a new minable chunk (10 kg): ammonium chloride with sodium carbonate and clay, like the bright salt deposits NASA's Dawn mission found on the dwarf planet Ceres. It is the ship's first mined source of nitrogen. C-class mineral finds give one about one pull in twenty, in place of some silicates (the shared silicates share falls from 30% to 25% with Manufacturing alone). Dark regolith walls reach it through their own C-class find. Government kiosks buy it like other ore.
- A new V4 charge: one salt crust gives 0.955 kg of ammonia into a linked ammonia store, 0.505 kg of water into the linked vessel, 1.235 kg of carbon dioxide breathed into the room and a 7.305 kg spent salt cake in the tray, in 15 minutes at 24 kW. The reaction, mass balance, energy and sources are in the chemistry record. The spent salt cake is a new terminal remainder.
- Phobos' Fennmark Q2, Q3 and Q4 Ammonia Stores (2 x 2, 3 x 3 and 4 x 4, holding 380, 940 and 1,820 kg of liquefied ammonia; 20,000, 32,530 and 45,950 cr; the INSTALL menu APPS tab). They pour, vent and appear on the C1 like the other gas stores. No station sells ammonia.
- The V4 can now send a gas to a store: a Send ammonia to field on its panel and the C1, for any ammonia store within one tile. The salt crust never vents its ammonia: the charge waits, with the reason, until its store is linked, intact and has room.
- The P1 manifold burns ammonia in the RCS (owner decision: player flexibility), at its cold-gas worth of about 1.41 times nitrogen per kilogram.
- PixelLab artwork: amber recolours of the nitrogen store masters for the Q2 to Q4, a new salt crust sprite and a spent salt cake derived from the anhydrous residue.

### Hazards

- A damaged ammonia store leaks 2, 3 or 4 kg an hour of the game's own ammonia into its room until repaired, and a destroyed one releases what it held; the game's ammonia poisoning applies. Baking salt crust puts carbon dioxide into the V4's room, so run a CO2 scrubber there.

### Compatibility and limits

- Requires Phobos Framework 0.48.0 or newer. Saved charges, stores and loot rolls already made are unchanged; only future C-class finds can give the crust. The crust's mix is authored from the Dawn findings, not a measured sample. Nothing turns ammonia into nitrogen or fertiliser yet; an ammonia cracker is planned next. Offline checks are not gameplay validation.

## [0.8.0] - 2026-09-30 - Draft

### Changed

- Clay hydrates now come out of the game's C-class mineral finds in place of some silicates, instead of as an extra find on top (owner loot rule, 30 September 2026). Clays are water-bearing silicate minerals, so the chunk takes a tenth of the C-class roll from the silicates share (40% becomes 30%). C-class deposits give clay as often as before, about one pull in ten; dark regolith walls now reach it only through their own C-class find, so dark vein walls give it less often than in 0.7.0 and never twice from one wall.

### Compatibility and limits

- Requires Phobos Framework 0.48.0 or newer. Clay chunks already mined are unchanged. Only future finds use the new odds. Offline checks are not gameplay validation.

## [0.7.0] - 2026-09-30 - Draft

### Changed

- Performance pass, stage 8. The two-second check of damaged gas stores and cabin air regulators now reads its machines from one shared sweep of the world in Phobos Framework 0.46.0 instead of walking every object in the world itself (about 8.5 ms each time in the owner's save). A newly placed machine is picked up within about four real seconds; one that is destroyed or removed drops out at once.

### Compatibility and limits

- Requires Phobos Framework 0.46.0 or newer. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.6.0] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 4 (the game crawls at fast-forward). The power hooks the game calls for every powered object in the world classify each object by one dictionary probe and keep no state for appliances that are not ours. The two-second world scan (damaged stores, regulators) is one plain pass with one probe per object; before this it ran two queries over every world object with a scan of fifteen store sizes each.
- Gas-line connections come from Framework's shared pipe topology, and the manifold, filling station, electrolysis cell, reactor and refinery recheck missing links and vessels on real time (five seconds) rather than game time, which at fast-forward had shrunk to every frame.
- Per-step work while machines run: the filling station keeps one job per step instead of searching for the next up to eight times; the electrolysis cell and reactor check their linked canister directly instead of listing every candidate aboard, read each vessel once and format a reason only when refusing; the refinery looks its feed up once per check and formats its working line once per recipe. The manifold's draw reason is formatted once per language. Store panels list the machines aboard once per refresh. The regulator writes its record only when a value changed. The maintenance hooks recognise our machines before searching action names.
- Recorder scopes manufacturing.scan, manufacturing.power.hook, manufacturing.machine.step, manufacturing.manifold.refresh and manufacturing.regulator.tick.

### Compatibility and limits

- Requires Phobos Framework 0.45.1 or newer. A gas line laid or cut is noticed within two real seconds; missing vessels and links are rechecked every five real seconds. Saved data is unchanged. Offline checks are not gameplay validation.

## [0.5.0] - 2026-09-29 - Draft

### Added

- Phobos' Fennmark A2 Cabin Air Regulator (2 x 2, 0.1 kW, 23,000 cr, the INSTALL menu HVAC tab): keeps the room it stands in breathable from linked bulk stores. It adds oxygen up to a set point of 19, 21 or 23 kPa, then nitrogen up to 80, 90 or 101 kPa (or leaves pressure alone), up to 6 kg of oxygen and 12 kg of nitrogen an hour. Stores link within one tile or along a gas line, as for the L2.
- A PixelLab sprite in the Fennmark family, with green oxygen and blue nitrogen valve wheels.
- Keep suit bottles charged: a right-click crew order on the installed L2, like the Shipbreaker loading orders. Crew with the Haul duty bring loose suit O2 bottles below 90% from around the ship into the rack and start the station; with a destination store chosen in the Crew panel they carry charged bottles there. They never take a bottle from anyone's suit or hands, a locked container or another L2's rack.

### Compatibility and limits

- It only adds gas: no venting, scrubbing or cooling. It stops feeding a room below 10 kPa (open to space) and never lets oxygen pass 30% of the air. Unlike the batch machines it keeps working after a reload, like the game's air pumps.
- Requires Phobos Framework 0.44.0 or newer, as 0.4.0. Owner gameplay checks remain pending.

## [0.4.0] - 2026-09-29 - Draft

### Added

- Every gas store now comes in three sizes (owner direction): small 2 x 2, medium 3 x 3 and large 4 x 4. The H3/H4 hydrogen stores hold 59 and 115 kg and the M3/M4 methane stores 395 and 770 kg. Bigger stores hold more for less per kilogram of capacity; the medium and large sizes are bought, never found in salvage.
- Phobos' Fennmark oxygen (O2/O3/O4), nitrogen (N2/N3/N4) and carbon dioxide (C2/C3/C4) stores, holding 340 to 1,630 kg of oxygen, 300 to 1,440 kg of nitrogen and 470 to 2,260 kg of carbon dioxide. Stations sell these three gases through the Bulk supplies view at the refuelling kiosk's own price per kilogram; nothing sells back. The X2 can send its oxygen to an oxygen store, the K2 can draw its CO2 from a carbon dioxide store, and the P1 manifold accepts all five gases.
- Phobos' Fennmark L2 Canister Filling Station (2 x 2, 3 kW, 26,000 cr, the INSTALL menu HVAC tab): tops up suit O2 bottles in its four-cell rack and O2, N2 or CO2 canisters installed beside it to a safe 99% of their rating and stops, from linked stores or a canister set as a source. Decant mode empties them back into stores. It waits for Start after a reload and plays the quiet completion cue when a watched run finishes.
- Pour into: any gas store can move everything that fits into another store of the same gas within one tile or along a gas line.
- Fourteen new PixelLab sprites in the Fennmark family, with the game's canister colours for each gas.

### Changed

- The propellant line is now the Fennmark gas line; its identities and saved routes are unchanged.
- Store panels list every machine linked to the store, vent in readable steps sized to the store, and the vent control reads Vent overboard.
- The within-one-tile rule for linking machines and vessels now comes from Phobos Framework, shared with Agriculture; placements that worked before still work, and any size of Shipbreaker silo or Agriculture reservoir can supply water.

### Compatibility and limits

- Requires Phobos Framework 0.44.0 or newer. Existing H2 and M2 stores keep their identities, contents and records.
- A damaged oxygen, nitrogen or carbon dioxide store leaks into its room, and a destroyed one releases what it held. The game has no hydrogen or methane canisters, so the L2 cannot bottle them. Owner gameplay checks remain pending.

## [0.3.0] - 2026-09-29 - Draft

### Added

- Phobos' Fennmark P1 RCS Propellant Manifold (1 x 1, 10 kg, passive, 24,000 cr, the INSTALL menu HVAC tab): installed where a gas canister would go, on an RCS Intake Regulator's gas-input tile, it feeds the thrusters from up to four linked hydrogen or methane stores. Each store has its own on/off switch, the manifold has a master switch, and Draw order chooses manifold first or canisters first. Everything starts switched off.
- Phobos' Fennmark Propellant Line (3 cr, lots of 128, the INSTALL menu HVAC tab): sealed gas line from a store's new line port to the manifold, on the same pattern as the other conduits but its own family. Stores within one tile need no line.
- Each gas pushes by its real cold-gas worth (Phobos Framework 0.42.0): methane about 1.45 times nitrogen per kilogram, hydrogen about 3.7 times. A full M2 store is worth about 232 kg of nitrogen, a full H2 store about 88 kg.
- Original amber line art (a recorded recolour of the shared conduit sheet) and a PixelLab manifold sprite.

### Compatibility and limits

- Requires Phobos Framework 0.42.0 or newer. RCS readings count the stores in nitrogen-equivalent kilograms. Draws settle into the stores every couple of seconds and before a save. Owner gameplay checks remain pending.

## [0.2.0] - 2026-09-29 - Draft

### Added

- Phobos' Fennmark K2 Sabatier Reactor (2 x 2, 150 kg, 1.2 kW working, 44,000 cr): one carbon dioxide and four hydrogen molecules become one methane and two water molecules. Each one-hour cycle takes 0.125 kg of hydrogen from the H2 store (one X2 cycle's output) and 0.682 kg of CO2 from an installed native CO2 canister (fill it with the game's CO2 scrubber), and makes 0.559 kg of water for a linked S3 or R3 and 0.249 kg of methane for a linked methane store. With the X2, about half the water the cell splits comes back, as on the ISS. The reaction heat and the electricity, about 2.3 kW, warm the room.
- Phobos' Fennmark M2 Methane Store (2 x 2, 160 kg empty, holds 160 kg, 21,000 cr): keeps the methane until something aboard can use it (owner decision), with an explicit vent overboard.
- Hazards: a damaged methane store leaks the game's own methane gas into its room until repaired, and with oxygen and an ignition source its contents burn into carbon dioxide through the game's own explosion. A damaged reactor dumps the CO2 and methane it holds into the room; its hydrogen burns or escapes.
- Original Fennmark artwork for both machines, produced with PixelLab from original start drawings. Same merchants, loot, repair, Restore and dismantle routes as the other Fennmark machines; the engineering-loot chance stays 5% in total, now shared by five machines.
- A mod-menu and Workshop cover, matching the other Phobos covers: a PixelLab scene of the V4, X2 and H2 in the Shipbreaker cover's frame.

### Changed

- The explosion object's name now reads "Gas deflagration"; blast size is chosen by the energy released, which gives the same sizes for hydrogen as before.

### Compatibility and limits

- Saved V4, X2 and H2 machines, records and contents are unchanged. The reactor pauses after reload until Start and starts a new cycle only once the last cycle's products are delivered. Nothing burns the stored methane as fuel yet; the reactor converts all of its hydrogen, an authored simplification. Owner gameplay checks remain pending.

## [0.1.1] - 2026-09-29 - Draft

### Balance

- The Fennmark machines are now priced as late-game plant, alongside the game's own radars, heavy lift rotors and missile launchers and below a fusion reactor: V4 refinery 64,000 cr (broken 16,000), X2 processor 38,000 cr (broken 9,500), H2 store 22,000 cr (broken 5,500). Installing, repairing, dismantling and Restore take longer to match.
- Repairs now need the game's own components, as its late-game equipment does: motors, mainboards, heat sinks and, for the V4, a screen. Dismantling returns some of those components; it still conserves mass and returns only 2 to 3 percent of the machine's value.
- The machines carry the game's high-salvage mark: the K-Leg fixer buys them intact, the Venus scrap kiosk intact or broken, and the K-Leg supplies kiosk no longer buys them.
- World finds are rare: about one engineering-loot roll in twenty yields a machine, three times in four broken (was two in five).
- Only the machinery is late-game priced; ingots, carbon, ore and remainders keep ordinary raw-material prices. The nickel-iron ingot moves from 20 to 24 cr, the smallest change that stops the steel charge gaining value (four nickel-iron ingots and carbon at 90 cr made four steel ingots worth 100 cr; now 106 cr in). Every charge now loses value, checked against live prices.

### Compatibility and limits

- Saved machines, charges, holds and stores keep their identities and state; new prices and bills apply to the definitions, and merchants pick them up at their normal restock. Repairs already gathering materials keep the lot they gathered. Owner gameplay checks remain pending.

## [0.1.0] - 2026-09-29 - Draft

### Added

- Phobos' Fennmark V4 Volatiles Refinery (4 x 4, 180 kg, 24 kW working): load one charge into its feed and Start. Hydrates (10 kg) give 1 kg of water and three gangue; the new clay hydrates chunk gives 2 kg of water and an 8 kg anhydrous residue; carbon ore gives five carbon stock, 1 kg of water, one gangue and 1 kg of pyrolysis gas breathed into the room (CO2, CO, smoke: run a scrubber); meteoric iron gives four 4 kg nickel-iron ingots, one gangue and 1 kg of slag; with Shipbreaker 0.38.0 or newer, four nickel-iron ingots and one carbon stock give four of its steel ingots and its melt remainder. Water goes to a linked S3 or R3 vessel within one tile; solids to the tray. Every charge conserves mass exactly, as recorded in the refinery design record.
- Phobos' Fennmark X2 Chemical Processor (2 x 2, 130 kg, 6 kW): water electrolysis. Each one-hour cycle splits 1.125 kg of water from a linked vessel into 1.000 kg of oxygen, delivered into a linked native O2 canister up to its rated pressure or into the cabin air when none is linked, and 0.125 kg of hydrogen into a linked H2 store. It runs only while every output has room.
- Phobos' Fennmark H2 Hydrogen Store (2 x 2, 160 kg): a passive pressurised store for 24 kg of hydrogen with an explicit vent overboard. Damaged, it leaks about 2 kg an hour to space until repaired; with oxygen in the room and a fire, a working refinery hearth or a sparking damaged device, its contents burn through the game's own explosion machinery, using the room's oxygen and heating its air.
- Materials: nickel-iron ingots and carbon stock (Manufacturing raw stock with real consumers), refinery slag and anhydrous residue (terminal remainders, trash), and the clay hydrates chunk, which joins the game's dark-regolith rock and C-class deposit mining tables as one bounded choice and sells to the government kiosks like other ore. Nothing new is sold in shops.
- Original Fennmark artwork, produced with PixelLab on 29 September 2026 from original Phobos start drawings: overhead full-footprint sprites for the V4 (64 px), X2 and H2 (32 px), and 16 px stock sprites for carbon stock, refinery slag and anhydrous residue; the nickel-iron ingot is a recorded recolour of the aluminium ingot. Clay hydrates use the game's hydrate artwork by reference until they have their own.
- A local Control Panel, C1 listing when Shipbreaker is present, the phobosmanufacturing F3 console, and the same merchant, loot, repair, Restore and dismantle coverage as the sibling machines: used at the K-Leg fixer, broken at K-Leg supplies and Venus, refurbished at Venus, pristine at Halvorson, regional supply kiosks, and engineering salvage.

### Compatibility and limits

- Requires Phobos Framework 0.41.0 or newer. Water comes from a Shipbreaker S3 or Agriculture R3 vessel within one tile; without one the water charges and the electrolyser wait. Shipbreaker 0.38.0 or newer is optional and only enables the steel charge; a steel charge saved before Shipbreaker is removed is kept and reported, never overwritten.
- The V4 and X2 follow the Phobos batch pattern: explicit Start, work that repeats while supplied, a pause after reload until Start. A melt (nickel-iron or steel) left waiting for a cool room longer than its own run freezes into slag with its mass kept. No crew loading orders, no construction recipes (the machines are purchase-only), no Sabatier stage yet, no hydrogen or water vapour species anywhere. Owner gameplay checks are pending; offline checks are not gameplay validation.

### Documentation

- The installer reads the package version and applies the maintained Framework minimum (0.41.0 from 0.1.0); the explicit hold option still retires only the known 0.0.1 scaffold DLL. The player guide, refinery design record, art handoff and item reference replace the held-scaffold notes, the empty item reference and the scaffold bootstrap diagnostic, following the shared player-language rule.

## [0.0.1] - 2026-09-25 - Draft

### Development baseline

- Initial changelog baseline for the current source; this version has not been published to Steam.
- Buildable mod scaffold with Framework integration.
- Research and an artwork plan for an enclosed machining centre and heat-sink finishing.

### Requirements

Scaffold baseline: Ostranauts 1.0.1.5, BepInEx 5 and Phobos Framework 0.17.0 or newer. Shipbreaker integrations are optional design work.

### Known limits

- Held draft: no operational machinery, recipes or merchant stock. Not a playable machining mod yet.
- Proposed equipment, tooling, yields and resource budgets remain designs; no gameplay validation is claimed.

### References

- [Current mod guide](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/docs/development/manufacturing-implementation.md)
- [Authorship and third-party terms](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/THIRD_PARTY_NOTICES.md)
