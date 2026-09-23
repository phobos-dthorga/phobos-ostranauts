# Vanilla economy audit

Generated from the installed vanilla definitions; excludes Workshop overrides.
Base values are not merchant quotes. Dismantling products below are newly generated, before wear/market adjustments.

| Native item | kg | Base value | Dismantling value lower / upper bound | Upper recovery / base | Output kg bounds |
|---|---:|---:|---:|---:|---:|
| `ItmWall1x1Loose` | 24 | $21.00 | $19.74 / $44.34 | 211.1% | 9–18 |
| `ItmRCSCluster01Loose` | 28 | $5,520.00 | $53.82 / $53.82 | 1.0% | 8–8 |
| `ItmBattery02Loose` | 45 | $2,623.00 | $64.70 / $64.70 | 2.5% | 5–5 |
| `ItmAirPump02OffLoose` | 11 | $2,050.00 | $56.20 / $56.20 | 2.7% | 7–7 |
| `ItmTowingBrace01Loose` | 90 | $11,972.00 | $314.98 / $314.98 | 2.6% | 29–29 |
| `ItmNavModMobo` | 0.4 | $767.00 | $61.05 / $61.05 | 8.0% | 3–3 |

Towing-brace loose form is **one 90 kg half**, not the complete 180 kg brace.
Bounds use the counts written in the definitions. Native item loot floors random quantities; upper endpoints are conservative bounds, not a measured distribution or expected payout.
The wall is an economic exception: its recovery range extends above its fully restored value; even minimum recovery exceeds the worn item's value.
The nav board also creates more output mass than its input. Neither anomaly is a target for Phobos.

- `ItmWallDismantle`: ItmPartsMechSmall01 × 2–4; ItmScrapAluminum × 2; ItmScrapCarbonFiber × 2; ItmScrapSteel × 2–6; ItmScrapTrash × 2–6.
- `ItmThrusterDismantle`: ItmComponentMotor01 × 1; ItmPartsMechSmall01 × 3; ItmScrapTrash × 2; ItmScrapAluminum × 1; ItmScrapCarbonFiber × 1.
- `ItmBatteryLargeDismantle`: ItmComponentMobo01 × 1; ItmPartsElecSmall01 × 3; ItmScrapTrash × 2; ItmScrapSteel × 1.
- `ItmAirPumpDismantle`: ItmComponentMotor01 × 1; ItmPartsMechSmall01 × 3; ItmScrapTrash × 2; ItmScrapSteel × 1.
- `ItmTowingBraceDismantle`: ItmComponentMotor01 × 4; ItmComponentMobo01 × 1; ItmPartsMechSmall01 × 12; ItmPartsElecSmall01 × 5; ItmScrapTrash × 2; ItmScrapSteel × 4; ItmScrapCarbonFiber × 4.
- `ItmNavModMoboDismantle`: ItmComponentMobo01 × 1; ItmPartsElecSmall01 × 3; ItmScrapTrash × 1.

## Native maintenance thresholds

| Item | Install / uninstall | Repair | Dismantle | Damage maximum |
|---|---:|---:|---:|---:|
| `ItmWall1x1Loose` | 600 / 600 | 0 | 110 | 15 |
| `ItmRCSCluster01Loose` | 1000 / 1000 | 0 | 150 | 6 |
| `ItmBattery02Loose` | 1000 / 1000 | 0 | 140 | 10 |
| `ItmAirPump02OffLoose` | 500 / 500 | 0 | 140 | 4 |
| `ItmTowingBrace01Loose` | 1500 / 1000 | 0 | 1300 | 40 |
| `ItmNavModMobo` | 0 / 0 | 480 | 100 | 4 |
| `ItmRCSCluster01DmgLoose` | 1000 / 1000 | 3000 | 50 | 19 |
| `ItmBattery02DmgLoose` | 1000 / 1000 | 3000 | 140 | 30 |
| `ItmAirPump02DmgLoose` | 500 / 500 | 1500 | 40 | 11 |
| `ItmTowingBrace01DmgLoose` | 1500 / 1000 | 100 | 300 | 15 |
| `ItmNavModMoboDmg` | 0 / 0 | 480 | 100 | 4 |

Zero in this table means the stat is absent, not an instant action. Native installation, removal and repair advance 5 units per unmodified 3.6-second tick; dismantling advances 1. Tool/skill modifiers and hauling are additional.

## Merchant multipliers

| Native discount table | Range |
|---|---|
| `CONDTraderDiscountBuyKiosk` | 0.4–0.5 × |
| `CONDTraderDiscountSellKioskScrap` | 1.2–1.3 × |
| `CONDTraderDiscountBuyFixer` | 0.5–0.9 × |
| `CONDTraderDiscountSellFixer` | 0.9–1.25 × |
| `CONDTraderDiscountBuyKioskScrapVORB` | 0.4–0.5 × |
| `CONDTraderDiscountSellKioskScrapVORB` | 1.2–1.5 × |
| `CONDTraderDiscountBuySanDiego` | 1–1.2 × |
| `CONDTraderDiscountSellSanDiego` | 2–3 × |

Buy is the merchant buying from the player; Sell is the merchant selling to the player. Market category supply/demand and negotiation can further change a live quote. Compare input and outputs at the same buyer/market; do not compare an item's shop purchase quote with scrap's inventory value.

## Repair inputs

- `RCSCluster01DmgLooseRepair` repair inputs: TIsMotor=1.0x1, TIsPartsMechSmall=1.0x3, TIsScrapAluminum=1.0x1, TIsScrapCarbonFiber=1.0x1
- `Battery02LooseRepair` repair inputs: TIsMobo=1.0x1, TIsPartsElecSmall=1.0x3, TIsScrapSteel=1.0x1
- `AirPump02DmgLooseRepair` repair inputs: TIsMotor=1.0x1, TIsScrapSteel=1.0x1, TIsPartsMechSmall=1.0x3
- `TowingBrace01DmgLooseRepair` repair inputs: TIsMotor=1.0x2, TIsMobo=1.0x1, TIsPartsMechSmall=1.0x4, TIsPartsElecSmall=1.0x2, TIsScrapSteel=1.0x1, TIsScrapCarbonFiber=1.0x1
- `NavModMoboRepair` repair inputs: TIsMobo=1.0x1, TIsPartsElecSmall=1.0x2
