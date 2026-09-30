# Merchant stock in useful quantities

Owner direction, 26 September 2026: stocked Phobos equipment and supplies must
be available in much larger quantities, especially parts laid out in bulk.
Framework 0.24.0, Shipbreaker 0.23.0, Agriculture 0.11.0 and Auto Nav 0.17.0
prepare this supply policy. These are authored gameplay quantities, not measured
shop inventories or a scientific/economic claim.

## Default lot per successful offer

| Stock family | Units |
| --- | ---: |
| Agriculture equipment | 8 |
| Agriculture irrigation pipes | 128 |
| Agriculture consumables | 64 |
| Agriculture nutrient hoppers | 4 |
| Agriculture 500 g nutrient charges | 8 |
| Shipbreaker equipment | 8 |
| Shipbreaker assembly sections | 24 |
| Shipbreaker coolant pipes | 128 |
| Shipbreaker coolant charges | 64 |
| Shipbreaker ingots | 32 |
| Auto Nav boards | 16 |
| Manufacturing equipment | 8 |
| Framework gas and process-water lines | 128 |
| Framework water silos (S2 to S5) | 8 |

Agriculture consumables include planting stock, nutrients, irrigation charges,
treatment cartridges and nutrient makeup. Equipment lots cover both working and
broken offers wherever those forms are sold. W2 and the B2 workup bench are
included. Since Framework 0.57.0 Framework sells its own two lines (the gas line
moved from Manufacturing with its lot unchanged), and since 0.58.0 the water silos
(moved from Shipbreaker; Agriculture's R3 to R5 reservoirs retired into them and are
no longer sold, so the reservoirs lot now serves the nutrient hoppers); Manufacturing's scaffold and the
historical Approach Assist prototype have no separate retail stock to multiply.

The same content-owned lot sizes apply at the original K-Leg, San Diego and
Venus endpoints and all supported [regional suppliers](../solar-system-economy.md).
Prices are still per item, not per lot. The shop receives individual physical
objects; players can purchase the quantity they need. This does not increase
player inventory capacity, alter stack rules, reduce mass or turn pipes into a
new bundle item. Native merchant storage/capacity can constrain what appears.

## Restocking and probability

Quantity and availability are separate. Each existing offer makes one probability
roll; a successful roll requests its whole finite lot. The stock-availability
setting and regional factors still affect probability, not units per lot. A
small chance is not multiplied once per object. Independent worn/refurbished
or broken offers can both succeed.

These changes apply to future normal merchant generation/restocks after updating
and restarting the game. Already-generated shop inventories are retained; there
is no forced refill, save edit or replacement of another mod's stock. An update
or reload alone does not guarantee that an existing shop restocks immediately.
World salvage uses separate single-item choices; see the current expansion below. Agriculture fridge/crate supply odds are unchanged.

## Maintenance and evidence

Use the constants updater keys `Agriculture.stockMachines`, `Agriculture.stockPipes`,
`Agriculture.stockSupplies`, `Shipbreaker.stockMachines`, `Shipbreaker.stockSections`,
`Shipbreaker.stockPipes`, `Shipbreaker.stockCoolant` and `AutoNav.stockBoards`.
Their catalogue entries update the owning source constants and this table
together. Then run the shop stock checks, affected builds and
`scripts/update-item-reference.ps1` to refresh every item-level offer table.
See [constant maintenance](updating-constants.md) and
[item-reference maintenance](item-reference-maintenance.md).

Framework's quantity-aware `MarketStock.Add` and `RegionalMarkets.Add` overloads
keep the original single-unit overload available for existing compiled callers.
The shared API validates finite integer lots before preparing a branch and
preserves other branches. Its defensive per-offer limit is not a balance knob.
Content owns the actual quantities. Named constants avoid separate numbers for
local and regional shops.

**Observed native implementation:** Blue Bottle Games' Ostranauts 1.0.1.5
`Loot` parser accepts a fixed quantity in each item expression, and native item
loot creates that many objects after a successful choice. The offline suite
checks parsed quantities for every registered Phobos merchant branch, plus
idempotence, invalid limits, old-call compatibility, availability scaling and
unchanged rare-loot checks. Primary product attribution:
[Blue Bottle Games — Ostranauts](https://bluebottlegames.com/games/ostranauts).
This is locally inspected implementation evidence, not a claim on that webpage.
Proprietary source remains local. Builds and offline checks are not owner-run
shop/restock gameplay validation.

## Expanded availability — 27 September 2026

Auto Nav 0.22.4, Shipbreaker 0.30.0 and Agriculture 0.16.0 require Framework
0.30.3. Equipment, boards and assembly sections now have an **85% minimum**
chance per registered offer; consumables, pipes and food have a **95% minimum**.
Existing higher offers remain higher, capped at 100%. Framework's existing
`StockAvailabilityMultiplier` applies afterward. These are authored gameplay
choices responding to the owner's repeated sparse merchant selections, not a
measurement of the game's economy. Prices and finite lot sizes above are unchanged.

Coverage includes all 15 previously supported regional supply/scrap endpoints,
K-Leg supplies and fixer, Venus scrap and San Diego Halvorson. All functional
retail families now have offers at those general suppliers. Polaris retains its
N1/N2/N3 offers; Hearth meals also reach both K-Leg food-cart tables and San Diego
Future Foods. Food-only shops do not receive industrial machines. Existing broken,
worn and refurbished offers remain; filling a coverage gap does not duplicate
an already prepared offer for the same item. Produce and prepared meals now have
explicit general/regional retail offers. Internal compartments, recorded process
materials, waste and Manufacturing's unimplemented designs remain excluded.

### World finds

| Native pool | Added choice | Total default chance per roll |
| --- | --- | ---: |
| Engineering equipment | One of nine Shipbreaker machines, intact or damaged | 40% |
| Engineering equipment | One D4/R4/F6 assembly section | 15% |
| Engineering equipment | One coolant pipe or clean coolant charge | 20% |
| Engineering equipment | One aluminium (6%) or steel (4%) ingot, since Shipbreaker 0.39.0 | 10% |
| Engineering equipment | One of five Agriculture machines, intact or damaged | 30% |
| Navigation-module leaf pools | One N1/N2/N3 board | 30% for new configurations |

Each row is an independent, mutually exclusive **single-item** choice. No shop
lot sizes enter salvage. Machinery choices give intact and damaged forms equal
weight. Mixed board pools retain one-third functional and two-thirds damaged;
damaged-only pools remain damaged. Existing Agriculture fridge/crate branches
remain 22%/30% total; its loot enable/multiplier also controls the new machinery
choice (30% at 1, 90% at its maximum 3). Native ownership, physical placement,
locks and available space still apply. A table roll is not a guarantee per ship;
shared native pools can also serve merchants or other world generation.

Auto Nav's existing `NavModuleChance` is not silently migrated: older installations
may still have 0.03. Set it to 0.30 with the game closed for the new default, or
retain another preferred rate. New merchandise likewise appears only during
normal future stock generation; revisiting or reloading an existing shop does
not guarantee a refill. No save edits or forced refreshes are included.

### Audit and acceptance

Reviewed all implemented machinery, supplies, boards, food, sections and damaged
forms against their prepared native stock/loot definitions. Framework's spent
parts and Manufacturing's scaffold add no retail equipment. The exact identities,
probabilities and quantities are regenerated in the [item references](../item-references.md).
The 27 September native export covers all 118 registered definitions. Across the
three content mods it records 658 merchant offers (previously 551), 45 distinct
retail item identities (previously 42), and 48 identities in added world-loot
choices (previously 14). Intact/damaged forms count separately; multiple shops
are separate offers. Manufacturing contributes no items. Finished furnace
castings retain their production route; internal compartments, waste and recorded
process intermediates are deliberately not manufactured by the new stock/loot rules.

Existing classification preserves native industrial/control-system/food pricing;
there is no added inflation model, station production rewrite or new salvage yield.

Native-parser checks cover minimum chances, full functional general-market
coverage, finite lots, intact/damaged choices, repeated registration, configured
multipliers, disabled Agriculture/Auto Nav loot and preservation of other providers.
Player confirmation remains outstanding: check a normally restocked supplier and
newly generated engineering/nav-module loot, then reload without duplicated cargo.
Large equipment still needs native placement space; definitions alone cannot prove
that every rolled find fits or that every merchant has room for every offered lot.

## Coverage audit — 29 September 2026

The [economy coverage audit](economy-coverage-audit.md) rechecked every item across
the five mods. Shipbreaker 0.39.0, Agriculture 0.19.0 and Auto Nav 0.26.0 close its
gaps: every machine family now has used, refurbished and broken routes; single
ingots join engineering salvage; the remaining terminal remainders trade as trash.
Native checks now also confirm that a native buyer accepts every retail identity.
The S2 to S5 silos and the T2 stay purchase-only by owner decision (the R3 to R5 reservoirs retired into the silos in Agriculture 0.31.0).

## Faction kiosks — 30 September 2026

Framework 0.53.0 adds the CCRE and GalCon faction kiosks, which sell for scrip,
as a further route for every sold Phobos item. Each item keeps its usual lot and
asks a reputation tier below Honored. See [faction kiosk stock](faction-kiosk-stock.md).
