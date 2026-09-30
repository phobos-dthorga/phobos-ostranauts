# Phobos Manufacturing changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

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
