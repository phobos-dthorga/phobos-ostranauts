# Economy audit — 5 October 2026

Owner request, 5 October 2026: after many recent changes and additions, audit every
economic factor across all seven Phobos Ostranauts mods, covering selling prices, loot
tables, missing information and whether a player can make a profit.

This record holds the findings, the evidence behind each and the decisions they need. The
numbers are in [the generated tables](economy-audit-tables.md), which
`python scripts/audit-economy.py` rebuilds from the current item evidence, the mods' data
packs and the game's own definitions. Nothing in this audit changes gameplay; the only
edit it makes is filling five missing rows in [equipment economy](../equipment-economy.md).

## Method

- **Values** are base values: our own from `docs/item-reference-data.json`, the game's
  from its definitions, with our one amendment of a game price (methane ice at 100).
  Bulk is valued per kilogram as the code prices it: water 10, ethanol 20, crop
  nutrients 1,500, and gases and acid at the game's own `GasPrices`.
- **Trade** uses the game's merchant factors from the
  [vanilla economy audit](vanilla-economy-audit.md): kiosks buy at 0.4 to 0.5 x, the
  K-Leg fixer buys intact high-salvage equipment at 0.5 to 0.9 x, scrap kiosks sell at
  1.2 to 1.5 x, and the refuelling kiosk buys bulk back at 0.45 x.
- **Limits.** A live quote also moves with local supply and demand (0.2 to 1.8 x by
  category), negotiation and wear, so every margin here is an estimate at base
  value. Labour, power, travel and the fixer's chance to buy at all are not priced.
  Base-value comparison is the measure the refining rules already use.

## What passed

- **Faction kiosks.** Every family sold at retail has a tier; none needs Honored.
- **Dismantling.** No item dismantles into more than it is worth whole.
- **Terminal remainders.** None is sold by a merchant, and all sit at the technical
  minimum price.
- **Bought-stock loops in Manufacturing.** No charge fed only by bought stock gains
  more than a quarter. The native value check already enforces this; the audit agrees.
- **Availability.** Every loose identity that is neither sold nor found is made aboard
  (ingots, salts, spirit, ferrosilicon, pavers), a damaged pipe, or a retired legacy
  form (the three assembly sections and the R3 reservoirs). None is a gap.

## Findings

Ranked by how much each lets a player make money from nothing, or loses it for them.

### 1. Construction recipes are worth 10 to 186 times their parts (high)

Ten table builds turn a few hundred credits of scrap and small parts into equipment
priced at late-game levels. Building and selling to a kiosk at 0.5 x, after buying the
parts at 1.3 x:

| Build | Parts | Product | Gain per build | Build time |
| --- | ---: | ---: | ---: | ---: |
| F6-R radiator or F6-P thermal port | $210 | $7,200 | $3,327 | 60 min |
| G4 exterior grabber | $340 | $6,400 | $2,758 | 60 min |
| N2 Pursuit or N3 Fire Control board | $29 | $5,400 | $2,662 | 30 min |
| C1 industrial console | $241 | $5,200 | $2,286 | 45 min |
| N1 Auto Nav board | $29 | $3,600 | $1,762 | 30 min |
| C2 residue collector | $107 | $2,400 | $1,061 | 40 min |
| H4 hull chute | $176 | $1,800 | $671 | 30 min |

The Venus scrap kiosk buys any tradeable item, so every one of these sells. The boards
are the starkest: two small electronic parts become a $3,600 to $5,400 board in half an
hour. The bills were set when the prices were low. The machines were later repriced to
late-game levels (owner direction, 29 September 2026), but these bills were left as
they were. Agriculture's builds (rack, Hearth, W2, B2) sit at about 2 x their parts
and lose money when sold, which is the pattern the others should follow.

**Options.** Raise each bill to the game's own repair-style components (motors,
mainboards, heat sinks, a screen; a navigation board's own `ItmNavModMobo` at $767)
so the parts come to roughly half the product; or lower the products' prices to their
bills; or make the high-value ones purchase-only, as the S3, T2 and R3 already are.

### 2. Engineering salvage is more than doubled (high)

Each mod adds its own extra roll to the game's `ItmLootSpawnEngineering`, so the
chances stack instead of sharing one roll. Per engineering find:

| Mod | Chance of a Phobos item | Expected value added |
| --- | ---: | ---: |
| Shipbreaker (machine 1 in 2.7, service item 1 in 5, ingot 1 in 10) | 67% | $1,841 |
| Manufacturing (machine 1 in 20, three in four broken) | 5% | $732 |
| Medical | 2% | $147 |
| Framework (S3 silo) | 3% | $100 |
| Agriculture (machine 1 in 4) | 25% | $50 |
| **All mods** | **78%** | **$2,870** |

The game's own roll is worth about $1,870 at base value (estimated from its loot
tables). Manufacturing's share was cut from 40% to 5% on 29 September for exactly this
reason ([economy coverage audit](economy-coverage-audit.md)); Shipbreaker's was not
revisited when its machines were repriced.

**Recommendation.** Bring Shipbreaker's machinery roll to Manufacturing's 1 in 20 with
three in four broken, and halve its service and ingot rolls. That cuts the total
added to roughly $800 a find.

### 3. Crop nutrients: the LC-3 prints money, and crops lose it (high)

Crop nutrients are priced at $1,500 a kilogram, Agriculture's own price, which the
owner allowed above the refining band because fertiliser is rare in the game's world.
Two consequences follow from that one figure.

- **The LC-3 formulation gains 11.8 x.** $351 of salts become 2.77 kg of nutrients
  worth $4,156 in five minutes. The hopper bags them into 0.5 kg bulk charges, which
  any kiosk buys. At 0.45 x that is about $1,870 a charge, and about $20,000 an hour
  over selling the salts raw. The only limit is mined salt supply.
- **Crops that buy their nutrients lose money.** A harvest's water and nutrients at
  station prices cost more than the produce for wheat (−$45), soybean (−$15), flax
  (−$58), sugar beet (−$97) and seed lettuce (−$9). Potato (+$20), lettuce (+$12) and
  tomato (+$28) gain a little, over 48 to 96 hours on a $700 rack.

Growing food is meant to be supply rather than business, so the second point is a
question of feel. The first is a loop.

**Options.** Price crop nutrients far lower (the bulk charge, makeup packet and kiosk
price follow it); or cap the bulk charge's price so bagging cannot sell formulation
output at the full figure; or keep $1,500 and accept the LC-3 route as a reward for
the mining it takes.

### 4. Cooking destroys value (medium)

The Hearth-2's flatbread and soybean stew each take one of the game's water rations
(`LiquidWater`, $150), so $180 of inputs make a $90 flatbread and $175 make a $70 stew.
The game's own hot meals are priced $99 to $205. Potatoes cook from $12 to $35, which is
fine.

**Options.** Price the two meals in the game's hot-meal range (about $200 each); or add
new recipe revisions that take process water from a linked silo, so the meal prices can
stay.

### 5. Repairing broken machines pays, as it does in the game (information)

A broken machine bought from a scrap kiosk, repaired with its bill and sold whole to
the fixer makes between $200 and $11,000 at worst and up to $57,000 at best, depending
on the machine. The game's own equipment does the same: its cargo AI makes $13,400 to
$32,800 this way. Our damaged forms are priced at a quarter of whole, against about a
fifth for the game's. This follows the game; the size of the margin comes from our
late-game prices. No change is recommended.

### 6. Smaller pricing seams (low)

- **Sugar is worth more as sugar than as spirit.** The B2 turns a $4 beet into a $10
  sugar packet (2.5 x), but the fermenter-still turns $65 of sugar wash into $4 of
  ethanol. Beet mash is roughly even (1.06 x). Fermenting sugar is never worth doing.
- **The Hearth pays for itself in minutes.** Cooking potatoes gains $920 an hour at base
  value on a $150 cooker. That's harmless, since the potatoes are the real constraint.
- **Water ice is worth more raw.** The game prices a block at $1,200; thawing gives $227
  of water. This is a survival trade, and the guides already say ores and ice are worth
  more raw.

### 7. Documentation (fixed)

[Equipment economy](../equipment-economy.md) had no price rows for the oxygen and
nitrogen stores, the Cask ethanol tanks, the Copperhead-3 fermenter-still, the Corker-2
bottler or the RM-1 feeder. This change adds them.

## Decisions needed

1. Construction recipes: raise the bills, lower the prices, or make the dear ones
   purchase-only.
2. Engineering salvage: cut Shipbreaker's rolls as recommended, or set other figures.
3. Crop nutrients: a lower price, a cap on the bulk charge, or keep $1,500.
4. Meals: reprice the flatbread and stew, or add water-from-silo recipe revisions.

A new price reaches items already in a save only where the item follows its price on
load (`EquipmentSaveUpgrade.FollowPrice`). Today that covers Manufacturing's materials
and methane ice. Repricing anything else (Agriculture's nutrients and meals, the boards,
Shipbreaker's equipment) should add that hook in the same change, so no save needs a
manual step.

## Rerunning

Run `scripts/update-item-reference.ps1`, then `python scripts/audit-economy.py`. It
reads the game folder from `.local/install-settings.json` (or `--game`) and rewrites
[the tables](economy-audit-tables.md). It is read-only and changes no source.
