# Phobos Manufacturing changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

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
