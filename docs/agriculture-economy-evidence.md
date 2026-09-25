# Agriculture economy: generated evidence

25 September 2026. Current Agriculture definitions plus installed Ostranauts 1.0.1.5, evaluated with Blue Bottle Games' native DataCO.GetBasePrice and trade triggers. Values are per object, in credits, before merchant/market adjustments. No game session or live quote was sampled; this report reflects the current authored economic balance.

## Equipment and construction

| Machine | Base | Pristine | Broken base | Worn broken | Raw construction inputs | Work minutes | Dismantle outputs |
|---|---:|---:|---:|---:|---:|---:|---:|
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | 700.00 | 875.00 | 140.00 | 35.00 | 317.20 | 60 | 62.01 |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | 150.00 | 187.50 | 30.00 | 7.50 | 65.00 | 20 | 8.31 |
| Phobos' Verdemorrow Groundwork W2 Water Supply Unit | 250.00 | 312.50 | 50.00 | 12.50 | 115.60 | 30 | 14.41 |
| Phobos' Verdemorrow Groundwork Irrigation Conduit | 2.00 | 2.50 | 0.40 | 0.10 | 1.10 | 2 | 0.01 |

Phobos' Verdemorrow Firstlight-4 Cultivation Rack: VORB endpoint illustration: pristine ingredients at the highest sell multiplier cost 594.75; a full-condition constructed machine at the lowest buy multiplier returns 280.00. Difference -314.75 before tools, labour, hauling, availability and market category effects.

Phobos' Verdemorrow Hearth-2 Galley Cooker: VORB endpoint illustration: pristine ingredients at the highest sell multiplier cost 121.87; a full-condition constructed machine at the lowest buy multiplier returns 60.00. Difference -61.87 before tools, labour, hauling, availability and market category effects.

Phobos' Verdemorrow Groundwork W2 Water Supply Unit: VORB endpoint illustration: pristine ingredients at the highest sell multiplier cost 216.75; a full-condition constructed machine at the lowest buy multiplier returns 100.00. Difference -116.75 before tools, labour, hauling, availability and market category effects.

Phobos' Verdemorrow Groundwork Irrigation Conduit: VORB endpoint illustration: pristine ingredients at the highest sell multiplier cost 2.06; a full-condition constructed machine at the lowest buy multiplier returns 0.80. Difference -1.26 before tools, labour, hauling, availability and market category effects.


## Native service definitions

| Target | Repair inputs | Repair outputs | Restore inputs | Dismantle outputs |
|---|---|---|---|---|
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | TIsPartsMechSmall=1x2, TIsPartsElecSmall=1x1, TIsScrapAluminum=1x2 | PhobosVerdemorrowFirstlight4Loose + Framework actual spent materials | None; native in-place wear work | ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmScrapAluminum, ItmPartsMechSmall01, ItmPartsMechSmall01, PhobosVerdemorrowFirstlight4HousingWaste |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | TIsPartsMechSmall=1x1, TIsScrapAluminum=1x1 | PhobosVerdemorrowHearth2Loose + Framework actual spent materials | None; native in-place wear work | ItmScrapSteel, ItmScrapSteel, ItmScrapAluminum, PhobosVerdemorrowHearth2HousingWaste |
| Phobos' Verdemorrow Groundwork W2 Water Supply Unit | TIsPartsMechSmall=1x1, TIsPartsElecSmall=1x1, TIsScrapAluminum=1x1 | PhobosVerdemorrowGroundworkW2Loose + Framework actual spent materials | None; native in-place wear work | ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, ItmScrapSteel, PhobosVerdemorrowGroundworkW2HousingWaste |
| Phobos' Verdemorrow Groundwork Irrigation Conduit | TIsScrapAluminum=1x1 | PhobosVerdemorrowWaterConduitLoose + Framework actual spent materials | None; native in-place wear work | PhobosVerdemorrowWaterConduitWaste |

Repair bills and work differ by machine; actual consumed repair mass returns as spent material. Restore removes wear in place and does not award pristine condition. See the current player guide for bills and timings.

## Supply and food values / buyer acceptance

These are native data-trigger results for empty loose definitions. 'Buy' means the merchant buys from the player. Stock insertion does not establish buy-back eligibility.

| Item | Base | Category flags | Supply buyer | VORB buyer | Fixer generic buyer | Halvorson buyer |
|---|---:|---|---|---|---|---|
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | 700.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Spent Service Parts (0.5 kg) | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack (Damaged) | 140.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | 150.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Hearth-2 Galley Cooker (Damaged) | 30.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork W2 Water Supply Unit | 250.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork W2 Water Supply Unit (Damaged) | 50.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork Irrigation Conduit | 2.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork Irrigation Conduit (Damaged) | 0.40 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Continuance Seed Potato (0.2 kg) | 40.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Continuance Lettuce Seeds (5 g) | 5.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork Formulated Crop Nutrients (40 g) | 60.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Raw Potatoes (0.4 kg) | 12.00 | IsCategoryFood | Yes | Yes | No | No |
| Phobos' Verdemorrow Hearth Cooked Potatoes (0.4 kg) | 35.00 | IsCategoryFood | Yes | Yes | No | No |
| Phobos' Verdemorrow Lettuce (0.25 kg) | 8.00 | IsCategoryFood | Yes | Yes | No | No |
| Phobos' Verdemorrow Crop Residue | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Process Solution | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Recorded Process Solution | 0.01 |  | Yes | Yes | No | No |
| Phobos' Verdemorrow Retained Treatment Rejects | 0.01 |  | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork Treatment Cartridge | 25.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Groundwork Irrigation Charge (5 kg) | 50.00 | IsCategoryIndustrialProducts | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |
| Phobos' Verdemorrow Agricultural Housing Waste | 0.01 | IsCategoryTrash | Yes | Yes | No | No |

## Ideal crop economics with purchased native water

Native LiquidWater: 150.00 per 0.25 kg = 600.00 per kg. Native acceptable algae meal: 60.00 per portion. Water prices here describe native inventory rations, not Ship's Water tank-refill tariffs.

| Cycle | Consumed water value | Consumed nutrient value | Whole harvested output value | Propagation stock |
|---|---:|---:|---:|---:|
| potato | 2,774.40 | 60.00 | 350.00 | 40.00 retained seed potato |
| lettuce | 759.48 | 7.50 | 32.00 | Consumes one 5.00 packet; no seed return |
| lettuce-seed | 813.60 | 15.00 | 20.00 | Consumes one packet; harvests four seed packets, no edible leaves |

Groundwork irrigation charges: 50.00 per 5 kg. Consumed root-water cost: potato 46.24; lettuce 12.66. Purchase whole charges; remaining water stays available. No potable conversion.


The ideal potato row assumes cooking all nominal whole portions. It excludes delayed-harvest respiration/rounding, electricity, cooker work, losses and equipment amortization. Retained potato seed is not both sold and replanted. Lettuce consumes only 5 g of a 40 g nutrient packet; unused nutrient remains inventory, not an eightfold recurring expense.

Conclusions and proposed changes are in [Agriculture economy review](agriculture-economy-review.md). These prices are authored game balance; NASA/ESA research does not establish fictional prices, profit margins or repair bills.

New treatment jobs: 25 cr / 25 kg drainage capacity = 1 cr per kg at base value. A 0.25 kg batch uses 0.0005 kg medium and 0.25 cr of capacity; a 20 kg batch uses 0.04 kg and 20 cr. Unused medium returns with proportional base value. Historic bound jobs retain whole-cartridge consumption. Electricity remains 0.01 kWh/kg, excluding crew/equipment.


## Cultivation cost sensitivity and propagation

Computed from current crop rules and native base values. Groundwork irrigation, consumed portions only; retained potato seed is reused. Each lettuce sowing below buys its seed. These are ideal healthy cycles, not live profits.
The 0, 0.25 and 1 cr/kWh columns are hypothetical marginal electricity costs, not game tariffs; zero does not mean the plant uses no energy. Add cooking, pumping, standby, cooling, crew, equipment, treatments, losses and trade adjustments separately. No climate-control or labour rate is invented.

| Cycle | Water + nutrients + purchased seed | Cultivation kWh | At 0 cr/kWh | At 0.25 cr/kWh | At 1 cr/kWh |
|---|---:|---:|---:|---:|---:|
| potato | 106.24 | 72 | 106.24 | 124.24 | 178.24 |
| lettuce | 25.158 | 19.2 | 25.158 | 29.958 | 44.358 |
| lettuce-seed | 33.56 | 38.4 | 33.56 | 43.16 | 71.96 |

A repeating lettuce rotation reserves one returned seed packet, grows 3 food cohorts and yields 12 edible servings over 240 rack-growth hours. It consumes 89.034 cr of water/nutrients and 96 kWh. Per food cohort: 29.678 cr and 32 kWh, before other costs. Initial stock is a one-off investment; no retained packet is simultaneously counted as sold or bought each rotation.

## Proposed crop-residue recovery ceiling — not a recipe

Illustrative conservative allocation: distribute only nutrients consumed by growth in proportion to final biomass; allocate the residue share, then recover 60% of that share. The 60% is an authored sensitivity assumption, not NASA's leaching yield. Seed nutrients receive no extra credit. Recovered material is an incomplete concentrate until formulation is defined.

| Ideal crop | Wet residue kg | Allocated nutrient ceiling g | Candidate recovered equivalent g | Maximum avoided stock cost cr |
|---|---:|---:|---:|---:|
| potato | 0.8 | 6.4 | 3.84 | 5.76 |
| lettuce | 0.2 | 0.833 | 0.5 | 0.75 |
| lettuce-seed | 1.18 | 9.833 | 5.9 | 8.85 |

Avoided stock cost is an upper bound, not sale value: subtract processing, make-up nutrients, consumables, labour and capital. Terminal rejects cannot be rerun. Larger batches can amortize setup work; no waste-processing recipe is registered by this audit. See [nutrient recovery direction](agriculture-nutrient-recovery.md).
