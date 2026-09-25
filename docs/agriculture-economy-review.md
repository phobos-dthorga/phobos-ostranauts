# Agriculture economy review

## Current status (0.7.0)

Delivered: [maintenance/treatment balance](agriculture-treatment-economy.md)
adds W2/pipe service comparisons, affordable pipe repairs and metered finite
cartridges. [Lettuce seed production](agriculture-seed-production.md) is now a
separate playable cycle. The [loot extension](agriculture-loot.md) remains enabled.

The [nutrient-recovery research and specification](agriculture-nutrient-recovery.md)
and expanded native audit now quantify electricity sensitivity, seed-sustaining
rotations and a conservative residue-recovery ceiling. No runtime price changes
were made before establishing an actual production recipe.

Remaining: compare full operating costs in actual play (power, cooling, labour,
losses, equipment and market quotes); investigate nutrient/media manufacturing
and characterized crop-residue recovery when useful. Terminal rejects remain
terminal. Propagation and loot do not replenish all lost nutrients or water.

All prices, rates and yields are authored gameplay choices. The generated native
evidence establishes definition values, not trading profitability in play.

## Historical audit and implemented first economic pass

**Implemented follow-up: Agriculture 0.3.0.** Current values and owner checks are
in the [player guide](agriculture-player-guide.md#economy-and-maintenance-030);
the [generated evidence](agriculture-economy-evidence.md) now reflects current definitions.
The audit below records the historical 0.2.0 baseline, not current prices.

Machine base prices are now 700/150 cr, with distinct repair bills, mass-balanced
intact/broken salvage, condition-based merchant stock, occasional stored seeds,
explicit categories, 5 cr lettuce seeds and a finite 5 kg/50 cr irrigation charge.
Construction inputs and native Restore remain unchanged. Repair/Restore can add
value through crew work; this is an intended service reward. Discount checks are
bounded examples, not a claim that every market or cross-station route is unprofitable.


25 September 2026. Agriculture **0.2.0**, Framework **0.17.0**, installed native
Ostranauts **1.0.1.5**. This is an audit and recommendation, not an applied balance
change. Current source and native-trigger calculations (superseding baseline values) are in the
[generated evidence](agriculture-economy-evidence.md), reproduced through
`scripts/audit-economy.ps1 -OstranautsPath '<local game folder>' -AgricultureOnly`.
Omit the final switch for the broader suite audit.

## Assessment

An economic pass is warranted before considering Agriculture balanced. Buying,
construction, maintenance and trade are implemented, but several first-slice
values are placeholders. The important distinction is between **construction
and resale**, which currently creates a large value premium, and **dismantling**,
which currently destroys almost all value. Neither is established as an exploit
in actual play merely by comparing definition values.

| Area | Current evidence | Recommended direction |
|---|---|---|
| Firstlight-4 construction | 317.20 cr raw material value, 60 minutes assembly, 9,000 cr full-condition machine value | Reconcile inputs, finished value and assembly effort; use independently obtainable cultivation components if retaining a substantial machine premium |
| Hearth-2 construction | 65 cr materials, 20 minutes assembly, 2,400 cr machine value | Apply the same review to the cooker; do not reward equipment assembly far more than cultivation without an explicit balance decision |
| Manual irrigation | Native 0.25 kg LiquidWater costs 150 cr base: 600 cr/kg | Provide a finite bulk/manual supply route suited to cultivation, preserving the optional Ship's Water adapter and potable crew reserve |
| Repair | Both machines consume one small mechanical part plus one aluminium scrap: 6.10 cr and 1.5 kg, with the same 2400-unit work requirement | Agriculture should own distinct meaningful bills and timings; Framework should execute them and retain actual replaced mass |
| Dismantling | Intact or broken 80 kg rack / 12 kg cooker each becomes one 0.01 cr housing-waste object | Return a modest mass-balanced mix of recoverable parts/materials and terminal waste, with lower broken recovery; keep recovery below worn whole-object and construction values |
| Merchant acquisition | Three additive routes, pristine stock only; no Agriculture derelict loot registration | Add purposeful worn/broken/refurbished offers and a narrow suitable salvage route if useful; preserve native stock, optional providers and other mods |
| Categories | Seeds, nutrients, raw potatoes, crop residue and process solution have no IsCategory flag | Assign honest native economic categories where appropriate; do not mark raw food edible just to make its category useful or relabel drainage as potable water |

All credits here are whole-object native base values, not shop quotes. Pristine
equipment receives the native 25% premium: 11,250 cr rack and 3,000 cr cooker,
before merchant adjustments. Broken definitions use 20% of functional base:
1,800 / 480 cr, with further wear possible. These are authored choices, not a
requirement to change them to another mod's percentages.

The construction concern survives a deliberately adverse *illustrative* VORB
comparison: buying pristine materials at 1.5 times their pristine value and
selling a full-condition assembled rack at 0.4 times base still leaves about
3,005 cr before tools, hauling, labour, stock availability and market effects.
This is not a live quote, a promise of repeatable trading, or a reason to remove
the value of skilled assembly. The gap merits an explicit decision.

## Water, seeds and harvested food

One ideal potato cycle consumes 4.624 kg water. Native ration water therefore
represents **2,774.40 cr**, plus 60 cr nutrients, against **350 cr** for ten cooked
portions. First planting also ties up a 40 cr seed potato; a healthy ideal harvest
reserves its replacement. A retained seed cannot also count as sale proceeds.
Buying complete rations initially requires 19 rations, leaving unused water;
consumption cost is different from the first purchase bill.

Valtora's [Ship's Water documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
describes station bulk refilling at approximately **8.4 cr/kg by default**,
configurable and based on the game's H2O value. Its locally inspected 0.16.1
configuration corroborates the 1.5 price multiplier. At that default, the same
potato cycle uses about **38.84 cr** of water. This is a materially different
supply economy, not a discrepancy to fix by globally discounting native rations.
Tank acquisition, available station service and adapter gameplay remain separate.

Lettuce exposes another imbalance even at that bulk-water rate: 25 cr seed,
7.50 cr consumed nutrient and about 10.63 cr water total **43.13 cr**, versus
**32 cr** of ideal leaf produce. Electricity, crew work, equipment and failures
are additional. The 40 g nutrient packet supports eight nominal lettuce cycles;
charging its entire 60 cr against every cycle would be incorrect.

Self-sufficiency can be worth more than selling crops, especially away from a
station. Nevertheless, manual supply should be a practical supported route and
seed economics should suit that goal. Revisit seed packet value/sowings and the
already-planned lettuce seed-production lifecycle before broadly inflating food
prices. The current 35 cr potato meal and 8 cr lettuce values need not both change
merely because the irrigation purchase route is expensive. Cooked food has a
deliberate value premium over 12 cr raw potatoes; preparation consumes power/work.
Full crop profitability also depends on respiration, portion rounding and losses.

## Selling, Restore and retained materials

The native data-only trade evaluator accepts Agriculture's empty loose machines,
supplies and produce at **K-Leg general supplies and the VORB scrap buyer**.
The generic fixer buyer and Halvorson specialist buyer reject these definitions,
even though additive stock lets those merchants sell selected Agriculture items.
This is not a broken sell-back promise: distinguish stock from buying policies.
No recommendation here replaces a merchant's existing filters or other stock.

**Restore does not need a new system.** It already performs native in-place
wear work with tools and no material bill. Repair restores a broken machine's
functional definition; it does not confer the pristine retail flag. Maintain
those distinctions when revising prices and condition-based offers. Repair and
Restore must continue preserving the crop, inventories and finite contents.

Current 0.01 cr residue/drainage/housing prices avoid the native zero-price
mass fallback. They are conservative placeholders, not characterized recycling
feedstocks. Keep crop waste distinct from Shipbreaker's existing residues and
terminal rejects. Any useful recovery route needs actual mass accounting and
real consumers; no repeated conversion or reroll of the same material.

## Proposed next pass and limits

1. Reconcile machine assembly value and add content-owned repair/salvage bills.
2. Make buying/selling categories and condition-based acquisition deliberate.
3. Add a finite bulk manual irrigation route, then reassess seed/food economics
   across standalone and Ship's Water configurations.
4. Keep the generated economy evidence in regular checks and evaluate actual
   prices, work effort and supply availability in later owner gameplay.

No machine, food, input, yield, price or recipe was changed by this audit.
No game files or saves were modified. These are fictional economy choices:
NASA and ESA research cited in the [agriculture research report](agriculture-research.md)
supports cultivation/life-support design, not credit prices or repair margins.
Native evidence is attributed to Blue Bottle Games; provider behavior above is
attributed to Valtora. The existing shared equipment audit previously omitted
Agriculture, so its scope wording and the repeatable audit entry point now make
that coverage explicit.

## B2 production balance — 26 September 2026

The [current nutrient-production chain](agriculture-nutrient-production.md) uses
a 250 cr B2 bench and 30 cr / 40 g makeup salts. Recovery replaces part of
resupply rather than granting fertilizer from wet waste mass. Regenerated
evidence now includes the bench, all new supplies and avoided nutrient purchases
after makeup costs. Crop water, feed, growth and historic repair contracts remain
unchanged. In-game costs and usefulness still need owner evaluation.
