# Faction kiosk stock for scrip

Owner direction, 30 September 2026: every Phobos machine, piece of equipment and
item should also be sold at the game's faction merchants, which trade only in
faction scrip. How much standing an item needs depends on its value and
usefulness. The one fixed rule: **nothing needs Honored.** The owner delegated
prices and gates, asking that both be based on comparable vanilla items, with a
best guess where there is no close match.

Framework 0.53.0 implements the mechanism. Shipbreaker 0.50.0, Agriculture
0.26.0, Auto Nav 0.31.0 and Manufacturing 0.16.0 list their goods. The standing
gates below are authored gameplay choices anchored on vanilla evidence, not
measurements. The player summary is in [equipment economy](../equipment-economy.md#faction-kiosks-scrip).

## What the game does

**Observed native implementation** (Blue Bottle Games' Ostranauts 1.0.1.5 data
and `GUITrade`/`Trader`/`JsonFaction`/`CondTrigger`, inspected locally with the
existing ILSpy tool; the proprietary source stays out of the repository.
Primary product attribution: [Blue Bottle Games — Ostranauts](https://bluebottlegames.com/games/ostranauts);
that page is not a public source for these internals):

- Three faction kiosk traders sell from three loot tables:
  `ItmFactionKioskBCRSInv` (CCRE, Zhonghuamen Terminal), `ItmFactionKioskMTRSInv`
  (CCRE, Port Yangshan on Mars) and `ItmFactionKioskBCERInv` (GalCon Peacekeepers,
  Port Mojave on Ceres). Alternate station layouts reuse these tables; in one, a
  CCRE kiosk charges four times the price.
- Buying uses `StatCCREScrip` or `StatGalileanConfederacyScrip`. The price is the
  item's ordinary credit price times the faction's `fScripConversionRate`, 0.05 for
  both factions: **one scrip per 20 credits**. No vanilla item has a separate
  scrip price. The kiosks buy only mining output (`TIsMiningOutput`), which is how
  players earn scrip.
- Standing is the faction's score for the buyer's factions: Warm from 25, Friendly
  from 50, Trusted from 75, Honored from 100. Buying at a kiosk raises standing
  by 0.000084 per credit of value.
- The kiosk offers its stock through five triggers in order: `TIsFactionTierNeutral`,
  then Warm, Friendly, Trusted and Honored. Each item is listed under the **first**
  trigger it passes; tiers above the buyer's standing show as locked rows. The
  higher triggers accept any one of their listed tags (`bAND` false); Neutral
  accepts anything not forbidden. Vanilla gates by what an item is, not its price:

| Standing | Vanilla kiosk stock | Credits |
| --- | --- | ---: |
| Neutral | Tools, mining drills, batteries, beacons, 5.56/5.8 mm ammunition, rifles, PPC cladding, Polaris decoy launcher | 5 to 86,000 |
| Warm | Control-systems gear (all nav modules), Weber and Halvorson brand gear, Weber coilgun | 24 to 25,000 |
| Friendly | Railgun and 150 mm ammunition | 100 to 45,000 |
| Trusted | Smartlink and BNW PDCs, 20 mm ammunition | 5 to 40,000 |
| Honored | Missile launchers and missiles | 9,000 to 93,000 |

Elsewhere in vanilla, control systems such as the Miura RCS regulator (32,266)
and the heavy lift rotor (56,774) sit at Warm, while an IC fusion reactor
(141,000), radars and EO/IR sensors are Neutral wherever they are sold.

## Tier rule

Value and usefulness together, anchored on those rows:

| Standing | Phobos goods | Vanilla anchor |
| --- | --- | --- |
| Neutral | Supplies, materials, crops and meals: anything consumed | Tools, batteries, ammunition, cladding |
| Warm | Equipment under 10,000 credits, and control boards | Nav modules, branded gear |
| Friendly | Equipment from 10,000 credits; hull capture and cutting; boards that chase or fight ships | Railgun |
| Trusted | Equipment from 30,000 credits, and the F6 furnace as Shipbreaker's centrepiece | PDCs |
| Honored | Nothing (owner rule) | Missiles |

A family's band is judged on its base size. Other store, silo, bin and hopper
sizes share that tier, because a bigger tank is not a new capability. Until
Shipbreaker 0.60.0 retired them, assembly sections shared their machine's tier so
that three F6 sections could not bypass the F6's gate; they are no longer sold. Agriculture machines are cheap but keep a crew fed indefinitely, so
they ask Warm rather than Neutral.

## Prices

Scrip prices stay native: an item's credit price at the faction's rate. Kiosks
then apply their usual regional and discount factors. That matches every vanilla
kiosk item, keeps our credit price anchors (see [equipment economy](../equipment-economy.md)),
and avoids buying cheap in scrip to sell dear for credits. The figures below
are base values; the kiosk's own factors apply on top.

### Shipbreaker

| Standing | Item | Credits | Scrip |
| --- | --- | ---: | ---: |
| Neutral | Steel and aluminium ingots; coolant charge; F6-C conduit | 3 to 25 | 0.15 to 1.25 |
| Warm | H4 chute, C2 collector, Y2/Y3/Y4 bins, T2 thaw unit, C1 console, F6-R radiator, F6-P port | 1,800 to 7,200 | 90 to 360 |
| Friendly | G4 grabber | 6,400 | 320 |
| Friendly | ML-2 mining laser (it cuts other hulls, like the G4) | 9,600 | 480 |
| Friendly | D4 fixture | 12,000 | 600 |
| Friendly | R4 reclaimer | 14,800 | 740 |
| Trusted | F6 furnace | 24,000 | 1,200 |

### Agriculture

| Standing | Item | Credits | Scrip |
| --- | --- | ---: | ---: |
| Neutral | Seeds, nutrients, bulk nutrient charge, irrigation and treatment cartridges, makeup salts, irrigation conduit, raw and cooked produce | 2 to 750 | 0.10 to 37.50 |
| Warm | Firstlight-4 rack, Hearth-2 cooker, W2 supply, B2 bench, E2/E3/E4 nutrient hoppers | 150 to 700 | 7.50 to 35 |

### Auto Nav

| Standing | Item | Credits | Scrip |
| --- | --- | ---: | ---: |
| Warm | N1 Polaris Auto Nav Module | 3,600 | 180 |
| Friendly | N2 Polaris Pursuit Module | 5,400 | 270 |
| Friendly | N3 Polaris Fire Control System | 5,400 | 270 |

The boards carry the game's control-systems tag, so they would sit at Warm with
the kiosks' own nav modules even without a mark. The N2 and N3 steer toward and
fight other ships, so they ask Friendly.

### Manufacturing

| Standing | Item | Credits | Scrip |
| --- | --- | ---: | ---: |
| Neutral | Lixivar acid line | 6 | 0.30 |
| Friendly | H, M, O, N, C and Q gas stores, all three sizes | 20,000 to 50,540 | 1,000 to 2,527 |
| Friendly | A2 cabin air regulator, P1 manifold, L2 canister filler | 23,000 to 26,000 | 1,150 to 1,300 |
| Trusted | X2 processor, AX-2 cracker, K2 Sabatier reactor, LC-3 leach unit, SA-3 acid plant, V4 refinery | 38,000 to 64,000 | 1,900 to 3,200 |

### Framework

Framework 0.57.0 owns the shared lines; the gas line keeps the tier it had under Manufacturing. Framework 0.58.0 adds the water silos, which keep the Warm tier they had under Shipbreaker; Agriculture's R3 to R5 reservoirs retired into them and are no longer offered.

| Standing | Item | Credits | Scrip |
| --- | --- | ---: | ---: |
| Neutral | Gas line, process water line | 3 | 0.15 |
| Neutral | Rivetline conveyor belt | 24 | 1.20 |
| Neutral | Rivetline D20 drain canister (Framework 0.63.0), a supply like the lines | 40 | 2 |
| Warm | Rivetline S2/S3/S4/S5 process water silos | 2,950 to 8,860 | 147.50 to 443 |

## Mechanism

- Framework registers four hidden conditions, `IsPhobosFactionTierWarm`,
  `...Friendly`, `...Trusted` and `...Honored`. Neutral has no mark.
- After every mod's data loads, Framework amends the game's five tier triggers
  in place (never republishing them). Each mark joins its own tier's
  requirements, and every lower tier forbids it. A marked item therefore fails
  each lower tier and lands exactly on its own, even when a native tag would place
  it lower. Vanilla items carry no mark, so their tiers are unchanged. The
  amendment is skipped, with a log line, if a tier trigger is missing or no longer
  any-of. The loaded trigger is read directly from the game's table; the game's
  own lookup hands out clones.
- `MarketStock.Add` takes an optional `FactionTier`. The kiosk offer stamps the
  mark on each freshly generated unit, like its condition flag. Ordinary shop
  offers carry no mark.
- Each economy pack's `factionKiosks` section names the kiosks, an offer chance
  (1, like the game's own kiosk stock) and a tier per equipment key (every
  saleable size, intact form), supply key or item id. `EconomyStock.ApplyFactionKiosks`
  offers each listed item at every listed kiosk in its usual finite lot, pristine,
  with offer ids `PhobosFaction_<mod>_<kiosk>_<item>`.
- Player override files in `BepInEx/config/<Mod>/economy/*.json` can retune a
  tier or add an item, as with any economy entry.

## Maintenance

- A new sold item needs a `factionKiosks` tier. `tests/test_data_packs.py` fails
  when anything sold elsewhere is missing from the kiosks, or when a shipped
  entry asks Honored. The native `FactionKioskChecks` confirm every kiosk carries
  every sold item, the vanilla anchors keep their tiers, and each mark files both
  a control-systems board and ordinary stock exactly at its tier.
- Revisit a tier when an item's price crosses a band or its role changes. Keep
  larger sizes with their family.

## Limits

- Kiosks already stocked keep their stock until their normal restock. Nothing is
  refilled, and no save is edited.
- The lots are the usual Phobos lots (eight machines, 16 boards,
  up to 128 pipes). Kiosk stock drops into the kiosk's trade zones and overflows
  into the kiosk itself, as with any restock, so native space can limit what
  appears.
- Retuning a tier changes future stock only. Units already on a kiosk keep their
  old mark until the next restock. A bought item keeps its hidden mark, which does
  nothing outside kiosks.
- Offline and native-data checks are not gameplay validation. The owner still needs
  to check a restocked kiosk in play.
