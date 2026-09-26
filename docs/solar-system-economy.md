# Phobos economy across the vanilla solar system

Current stock quantities: [bulk merchant lots](merchant-stock.md) supersede the older single-item offers below. These content versions require Framework 0.24.0+.

Prepared 26 September 2026 against **Ostranauts 1.0.1.5**. Regional builds:
Framework **0.23.0**, Shipbreaker **0.21.0**, Agriculture **0.10.0**, Auto Nav
**0.15.0**. The three content builds require Framework 0.23.0 or newer.
These are unpublished candidates. Offline checks are not gameplay validation.

## What changes for players

Phobos equipment and operating supplies now have bounded offers at the supply
kiosks serving 14 additional vanilla settlements and the Flotilla scrap kiosk.
Existing K-Leg, Venus Orbital and San Diego specialist offers remain available
on their previous terms. No new world locations or shops are created.

Shipbreaker offers its nine machinery families, three assembly sections, coolant
pipe and finite coolant charge. Agriculture offers the rack, cooker, W2 supply,
B2 workup bench, both planting stocks, nutrients, root-water charge, treatment
cartridge, nutrient makeup and irrigation pipe. Auto Nav offers N1, N2 and N3.
The Flotilla sells refurbished machinery/modules; consumable inputs remain fresh.
Availability is a chance per native inventory-generation roll, not a guaranteed
item, delivery time, stock quantity or estimate of a shop's current inventory.
Existing inventories are not replaced or topped up on load. Native restocking,
access rules and each merchant's willingness to buy your goods remain native.
In particular, ordinary supply kiosks may sell equipment without buying it back.

## Regional availability

These **authored gameplay factors** multiply the content mod's base offer chance.
They are a modest specialization model, not measured prices or a transport
simulation. Industrial-product production supports better industrial availability;
food production supports better cultivation supply. Remote and import-dependent
markets have smaller selections. Water abundance alone does not manufacture
fertilizer or farming equipment. Electronics production is only an availability
proxy for assembled navigation boards, not proof of native board production.

| Native market | Location | Shipbreaker | Agriculture | Auto Nav |
|---|---|---:|---:|---:|
| HQCH | Qincheng Station, Mercury | 0.75 | 0.50 | 0.50 |
| VNCA | Long Beach Terminal, Venus | 1.25 | 1.00 | 0.75 |
| VCBR | Cloudbreak, Venus | 1.00 | 0.75 | 0.75 |
| VENC | Porto do Encantado, Venus | 0.75 | 1.00 | 0.75 |
| VORB | Venus Orbital | Existing offers | Existing offers | Existing offers |
| EJDR | Port Shajiang, Luna | 0.75 | 0.75 | 0.75 |
| MTRS | Port Yangshan, Mars | 1.50 | 1.50 | 1.25 |
| MLAB | Royal Carriers Launch Facility, Mars | No placed supply kiosk | No placed supply kiosk | No placed supply kiosk |
| MHNG | Qiantangmen, heliocentric | 0.50 | 1.25 | 1.00 |
| MSUZ | Panmen, heliocentric | 1.00 | 1.25 | 1.25 |
| MVOL | Upsilon Docking, Deimos | 1.50 | 0.75 | 1.50 |
| BCER | Port Mojave, Ceres | 1.00 | 0.75 | 1.00 |
| BCRS | Zhonghuamen Terminal | 1.25 | 0.75 | 0.75 |
| OKLG | K-Leg, 1036 Ganymed | Existing offers | Existing offers | Existing offers |
| OFLT | Flotilla | 0.50 | 0.50 | 0.50 |
| JFTS | Port Independence, Ganymede | 1.00 | 0.75 | 0.75 |
| JATL | Atlantis Landing Zone, Europa | 0.75 | 0.75 | 0.75 |
| JPTN | Porto Nuevo, Europa | No placed supply kiosk | No placed supply kiosk | No placed supply kiosk |
| SVIR | Cassini Spaceport, Titan | 1.25 | 0.75 | 0.50 |

Royal Carriers (MLAB) and Porto Nuevo (JPTN) have defined supply-kiosk
overlays/tables, but neither is referenced by a current vanilla ship blueprint.
Those inactive templates receive no new retail offers. Their existing generic
scrap/food/cargo shops remain unchanged. Regional category pricing still applies
where their native market tracks the category; imports and native bulk trade
remain subject to the game's existing access and stock. There is no promise that
every Phobos item is individually purchasable at every settlement.

The 19 rows are the installed game's named cargo-market profiles. The retail
routes use physical supply/scrap kiosks; they do not author new virtual cargo inventory
or change native production recipes. Native category selection can include
eligible Phobos goods in ordinary bulk stock; recorded intermediates remain
excluded from the new classifications. San Diego specialist retailers retain their
separate existing leaf tables. No fabricated Earth, Uranus or Neptune economy is
added merely because a celestial body exists on the map. Access to a given
settlement still depends on the game's world/save and travel mechanics.

| Offer family | Base chance before regional factor |
|---|---:|
| Shipbreaker major machinery | 20% per family |
| Shipbreaker chute/collector | 30% per family |
| Shipbreaker assembly sections | 30% per section type |
| Coolant pipe/charge | 65% per type |
| Agriculture machinery | 25% per family |
| Agriculture supplies/irrigation pipe | 65% per type |
| Auto Nav N1 / N2 / N3 | 30% / 15% / 10% |

Each offer generates at most one physical item per roll. The existing Framework
`Economy.StockAvailabilityMultiplier` then applies once, capped at a final chance
of 100%. Separate offers can both succeed. For example, a major Shipbreaker
machine at Port Yangshan has a default 20% × 1.5 = **30%** chance per roll;
an N3 at Cassini has 10% × 0.5 = **5%**. Exact offers also appear in the
[per-mod item references](item-references.md).

## Prices, scarcity and useful economic limits

**Observed native implementation:** Blue Bottle Games' `DataHandler` builds
`DataCO` records and `DataCoCollection` membership during `PostModLoadMainThread`,
after Framework's content-registration prefix. `ShipMarket.GetPriceModifierForItem`
uses those collections to find the local category multiplier, returning 1 when
none applies. `CalculatePriceModifier` combines net hourly category demand with
inventory fill. Its normal clamped demand term is -0.8 to +0.8; blockade handling
can raise the upper term to +1.7. The resulting normal category factor is within
0.2–1.8 for a fill fraction within 0–1, before condition and merchant terms.
These are bounds, not prices promised at every station.

Phobos keeps this native pricing path. There is no additional distance markup,
planet multiplier, inflation hook or invented dynamic scarcity state. The same
item can therefore respond to native surplus, demand and blockade conditions
where the local market actually tracks its category. Merchant margins,
negotiation, pristine status and wear remain additional native factors.

| Phobos goods | Native category / treatment |
|---|---|
| Intact loose machinery, assembly sections, pipes | Industrial Products |
| N1/N2/N3 loose modules | Control Systems, inherited from the native motherboard |
| Potatoes, lettuce and prepared food | Food |
| Seeds, nutrients, root-water charges, treatment cartridges, nutrient makeup, clean coolant charges | Industrial Products: packaged operating inputs |
| Retained coolant, spent biomass, terminal recovery/wet rejects and ordinary maintenance remnants | Trash; tiny authored base values remain unchanged |
| Recorded crop residue, recovered concentrate, in-progress mixture and characterized drainage | No new bulk classification; retain their existing process-specific identity |

Native collections exclude installed equipment and, for Industrial Products and
Control Systems, damaged forms. A broken offer does not acquire the intact
category's scarcity premium through a Phobos override. No buyer acceptance is
guaranteed. Root-water charges and coolant never masquerade as potable bulk water.
Recorded intermediates are not sold as assayed fertilizer or generated as new
nutrient-bearing stock by this change.

Base values, physical masses, construction and service bills, dismantle yields,
crop budgets, stored fluids and in-progress jobs are unchanged. Native scrap and
repair inputs keep their own regional factors. Existing
[equipment valuation](equipment-value-audit.md) and
[Agriculture economic evidence](agriculture-economy-evidence.md) remain definition
comparisons; they are not guarantees of profit or identical recovery margins in
every market. Labour, power, tools, travel and native merchant restrictions still
matter. Finite equipment outputs do not become perfect recycling.

## Sources and verification

Primary game-documentation attribution: **Joshu**,
[Official Ostranauts Modding Guide, 22 June 2026](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946),
documents Blue Bottle Games' native data layout and loot/condition definitions.
It supports the integration approach, not our chosen availability factors.

The economic evidence comes directly from **Blue Bottle Games' installed
Ostranauts 1.0.1.5**: `data/market/Markets/market_actor_configs.json`,
`data/market/Production/production_maps.json`, `data/market/CoCollections`,
`data/loot`, `data/guipropmaps`, station blueprints and `data/star_systems`.
Local inspection of `DataCoCollection`, `ShipMarket`, `DataHandler` and `Trader`
establishes the code behaviour above. Proprietary definitions and decompiled
source remain local and are not redistributed. The
[generated regional evidence](solar-system-economy-evidence.md) records a compact
reviewable summary of relevant native production roles and endpoint coverage.
No NASA/ESA research is invoked to justify fictional prices, and no institution
or game developer is represented as endorsing this balance.

Native-definition tests check all 19 endpoint IDs, coverage of the 15 additional placed retail endpoints by all three content
mods, native sale filters, bounded offers, preservation of foreign stock,
repeat preparation, native collection membership and surplus/demand lookup.
A missing optional endpoint is reported and skipped, without inventing another
merchant or disabling the rest of the mod. Normal K-Leg dependencies are retained.

Framework has no retail machinery of its own. Manufacturing remains an
unimplemented scaffold.
Neither gains fictional products or a new survival-economy contract.
In-game shop presentation, restocking and live quotes remain owner-run checks.
