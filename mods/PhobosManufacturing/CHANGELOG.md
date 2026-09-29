# Phobos Manufacturing changelog

Maintained from 25 September 2026. Earlier development versions are documented
in the project research and guides; no complete historical release log is claimed.
Dates on Draft entries record preparation, not Steam publication.

## [Unreleased]

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
