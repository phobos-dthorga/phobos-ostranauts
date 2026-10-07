# Refining profit repricing: proposal for owner decision

Proposal record, 8 October 2026. Nothing here is built; the build follows the owner's review.

## The owner's direction

On 8 October 2026 the owner wrote that profits from all refining, and perhaps manufacturing,
"need to be raised *substantially*. The player is investing substantial amounts of their time
here and for pitiful rewards." The owner's choices the same day:

- **Owner:** the business band rises from 1.5 to 2.5 times the ore to **3 to 5 times the ore**.
- **Owner:** the lever is **product prices**. Machines keep their late-game prices.
- **Owner:** **every guard stays**. A conversion fed only by bought stock earns at most 1.25 x,
  no loop gains, and the kiosk buys bulk back at 45%.
- **Owner:** a **proposal first**, then the build. The Alembrine spirit is repriced near the
  game's vodka in the same proposal.

The complaint is payback. Today the V4 repays itself in about 99 hours of running its best
charge, the LC-3 in about 320 and the SA-3 in about 740.

## How the figures were made

Every figure is a base price in credits, valued as the [economy audit](economy-audit-2026-10-05.md)
and the native value checks value them:

- **Items** are valued at their own price.
- **Bulk** is valued at the game's `GasPrices` table, read from the installed Ostranauts 1.0.1.5,
  with process water at 10 cr/kg, ethanol at 20 and crop nutrients at 150.
- **"Ore x"** is what a charge's products are worth over the mined ore it took.

A script valued every recipe in `mods/PhobosManufacturing/framework/process-recipes.json` at
today's prices and at the proposed ones. The figures are not seen in play. A kiosk pays about
half of base for items, and the same half for ore sold raw. Every ratio holds at the kiosk, but
every credit figure halves there.

## Proposed prices

The target is the middle of the owner's band: about **4 times the ore** for each business
chain. A step inside a chain stays between 1 and 1.5 times its inputs, as the rules require.

| Item | Today | Proposed | Why |
| --- | ---: | ---: | --- |
| Nickel-iron ingot (4 kg) | 220 | 450 | Puts meteoric iron at 4.0 x |
| Nickel steel ingot (4 kg) | 260 | 520 | Carburising stays a step (1.11 x); the iron and carbon chain ends at 4.4 x |
| Carbon stock (1 kg) | 38 | 78 | Puts carbon ore at 4.1 x |
| Carbon black (1 kg) | 12 | 25 | Methane cracking stays a step (1.18 x) |
| Potassium sulfate (0.7 kg) | 190 | 380 | With the concentrate, puts the evaporite crust at 4.0 x |
| Phosphate concentrate (0.25 kg) | 110 | 220 | As above |
| Struvite (0.43 kg) | 125 | 250 | The concentrate step stays at 1.12 x; the flask route stays at 1.32 x |
| Phosphoric acid flask (0.515 kg) | 300 | 575 | Puts the sulfide nodule at 4.0 x with its acid |
| Alembrine spirit (35 g) | 8 | 30 | Owner: near the game's 42.5 cr vodka. Own brand, about 70% of the import |

The following are unchanged:

- **Epsom salt (13), ammonium sulfate (20), ferrosilicon (2), pavers (13).** The Epsom salt reason is below.
- **Every ore, crust and nodule.**
- **Every terminal remainder.**
- **Shipbreaker's steel and aluminium ingots and housings.**
- **Every bulk gas price.** These are the game's own.

## Chains at the proposed prices

| Charge | Machine | Ore x today | Ore x proposed | Out over in, proposed |
| --- | --- | ---: | ---: | ---: |
| Carbon ore to carbon stock | V4 | 2.04 | **4.06** | 4.06 |
| Meteoric iron to nickel-iron | V4 | 1.96 | **4.00** | 4.00 |
| Nickel-iron and carbon to nickel steel (step) | V4 | | | 1.11 |
| Methane cracking (step) | V4 | | | 1.18 |
| Evaporite crust to salts | LC-3 | 2.00 | **4.00** | 4.00 |
| Concentrate to struvite (step) | LC-3 | | | 1.12 |
| Sulfide nodule to acid and flask | SA-3 | 2.16 | **3.99** | 2.41 |
| Flask, Epsom salt and ammonia to struvite (step) | LC-3 | | | 1.32 |
| Olivine to Epsom salt | LC-3 | 2.31 | 2.31 | 1.38 |

**Machine payback on the best charge, at base prices:**

| Machine | Price | Best charge | Today | Proposed |
| --- | ---: | --- | ---: | ---: |
| V4 refinery | 64,000 | Nickel-iron | about 99 h | **about 32 h** |
| LC-3 leach unit | 48,000 | Evaporite crust | about 320 h | **about 107 h** |
| SA-3 acid plant | 56,000 | Sulfide nodule | about 740 h | **about 160 h** |

- **At kiosk prices** every figure in the payback table doubles.
- **At the top of the band** (5 x), the three would be about 24, 80 and 112 hours.
- **Mining sets the real pace.** The LC-3 and SA-3 run on crusts and nodules, which turn up on
  roughly one pull in twenty or fewer, so a mined chain is limited by its ore, not its machine.

## What holds the guards

- **Bought stock.** No charge whose item inputs can all be bought gains more than 1.25 x.
  Hub-sold salts, from the [reagent-sale record](reagent-sale-at-industrial-hubs.md), count as bought.
  - **Crop nutrients.** Bought salts become crop nutrients worth 0.62 x the salts.
  - **Makeup packets.** Bought salts become makeup packets worth 0.13 x the salts.
  - **The flask and concentrate.** Both stay unsold, so no bought route reaches struvite.
- **The methane loop.** Methane made from bought water and CO2, then cracked with carbon stock,
  must not repay what was bought. With carbon stock at 78 and carbon black at 25, about 257 cr
  comes back for about 273 cr bought (the native check's own convention). It still loses, but by
  less than today, so the check stays.
- **The K2 water seam.** Hydrogen stays unsold, and the new native check guards it.
- **Outcome tables.** The odds are balance, not frozen, so they can move.
  - **Gangue wash.** Its expected return must stay within 1.5 times its cost. A dearer
    nickel-iron ingot takes it to 2.3 x at today's odds of 50/30/15/5. **Proposed odds:
    52/30/15/3**, about 16.1 cr expected against 11.1 cr in (1.45 x).
  - **Regolith leach.** It is fed by regolith that a prospector sells, so it must stay within
    1.25 x. At today's odds of 72/22/4/2 it reaches 1.31 x. **Proposed odds: 73/22/4/1**, about
    67.1 cr expected against 54.8 cr in (1.22 x).
- **Shipbreaker's scrap furnace.** Scrap can be bought, so the F6 ingots and housings keep their
  prices. Raising them would widen the scrap loop the owner left alone on 1 October.

## Where the band cannot reach, and the owner's choices

1. **Olivine stays at 2.3 x (agent recommendation: accept for now).** Epsom salt is also what a
   regolith leach makes from bought regolith. Even at 13 cr, four Epsom salt sit at the
   leach's 1.25 x limit. The rules allow a separate product for one of the chains, but a second
   magnesium salt is new clutter. The better route is the magnesia consumer researched under
   idea 8 of the [refining record](refining-business-and-interdependencies.md). Epsom salt goes
   back to acid and magnesia, and the magnesia becomes a furnace refractory lining. Olivine then
   ends in a priced product that can carry it into the band.
2. **The fertiliser formulation becomes supply (choice needed).** At the proposed salt prices,
   the formulation turns salts worth about 666 cr into crop nutrients worth 416 cr.
   - **Keep crop nutrients at 150 cr/kg (agent recommendation).** Formulation is then what a
     crew does to feed its own racks, as the makeup packet already is. Growing food stays as
     cheap as the 5 October audit made it.
   - **Raise crop nutrients to about 250 cr/kg.** Formulation would break even, and every crop
     would cost more to grow than it sells for again.
3. **"Perhaps manufacturing" (choice needed).** The Manufacturing mod's charges are covered
   above. Building equipment at a table to sell is a separate case. AGENTS.md says it must lose
   money, the 5 October audit fixed it that way, and dedicated assembly equipment is coming to
   replace table assembly. Agent recommendation: leave construction until that equipment exists.
4. **Supply machines.** The EC-4, CR-4, X2, K2, AX-2 and T2 make bulk gases and water at the
   game's own prices, so product prices cannot shorten their payback. They are survival plant.
   If the owner wants them cheaper to own, the lever is their machine price, which the owner set
   aside for this round. Agent recommendation: no change now.
5. **Spirit at 30 cr.**
   - **Beet route.** A sugar beet rack's 31 servings become worth about 930 cr, against about
     130 cr of nutrients and water.
   - **Sugar route.** A sugar wash's ethanol bottles into about 18 servings, worth about 540 cr.
   - **No bought-stock loop.** Ethanol stays unsold and beets and sugar are never sold. The value
     comes from growing, fermenting and bottling, so neither route breaks a guard.
   - **Shelf price.** The serving undercuts the game's vodka on the shelf, as an own brand
     should.

## What the build changes

- **Data.** Manufacturing `materials.json` prices and notes, the two outcome-table odds, and
  `EquipmentSaveUpgrade.FollowPrice` for the repriced items. Salts and ingots already follow
  their price on load, and the build confirms that spirit and carbon black do too.
- **Native checks.**
  - **Business band.** The band becomes 3 to 5 x the ore, and the iron chain's bound moves to
    match.
  - **Formulation exceptions.** The crop-nutrients and makeup exceptions are re-expressed under
    the bought-stock rule, as the reagent-sale outline already plans.
- **Rules.** The value rule in AGENTS.md, "The rules now in force" in the refining record, and
  the price tables in [equipment economy](../equipment-economy.md) and the Manufacturing player
  guide's "Selling what you make".
- **The reagent-sale record.** Its classification table quotes today's salt prices, and the
  build updates them.
- **Release.** `scripts/audit-economy.py` is rerun and its tables regenerated. The release is a
  Manufacturing minor version, since it is a balance change across the mod, with its changelog,
  Workshop page and item references.
- **Saves.** Items aboard take the new prices when the save loads. Nothing new is saved.
