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
| Shipbreaker equipment | 8 |
| Shipbreaker assembly sections | 24 |
| Shipbreaker coolant pipes | 128 |
| Shipbreaker coolant charges | 64 |
| Auto Nav boards | 16 |

Agriculture consumables include planting stock, nutrients, irrigation charges,
treatment cartridges and nutrient makeup. Equipment lots cover both working and
broken offers wherever those forms are sold. W2 and the B2 workup bench are
included. Framework, Manufacturing's scaffold and the historical Approach
Assist prototype have no separate retail stock to multiply.

The same content-owned lot sizes apply at the original K-Leg, San Diego and
Venus endpoints and all supported [regional suppliers](solar-system-economy.md).
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
Rare world salvage, engineering sections found on derelicts and Agriculture
fridge/crate loot retain their previous quantities and probability contracts.

## Maintenance and evidence

Use the constants updater keys `Agriculture.stockMachines`, `Agriculture.stockPipes`,
`Agriculture.stockSupplies`, `Shipbreaker.stockMachines`, `Shipbreaker.stockSections`,
`Shipbreaker.stockPipes`, `Shipbreaker.stockCoolant` and `AutoNav.stockBoards`.
Their catalogue entries update the owning source constants and this table
together. Then run the native stock checks, affected builds and
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
