# Regional economy: generated native evidence

Source: **Blue Bottle Games, installed Ostranauts 1.0.1.5**, inspected 26 September 2026.
The [economy guide](solar-system-economy.md#sources-and-verification) identifies the native files and original game documentation.
Reproduce with `scripts/audit-regional-economy.py --game <game-folder>` or the normal economic audit.
Selected facts only; no saves, live prices or proprietary definitions are exported.

Game assembly SHA-256: `91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.

Two defined supply kiosks (MLAB and JPTN) have no literal inventory-table reference in any current native ship blueprint. Their supply overlays also have no blueprint references; no regional stock is added to these inactive templates.

## Native capacity relevant to Phobos goods

Capacity is in native category inventory units, not kilograms or physical Phobos shop stock.
A dash means that this cargo profile does not list the category; it does not establish a universal ban on retail trade.
Hub-sharing and the live world's additional actors can change a market's aggregate state.

| Profile | Retail inventory table | Placed retail | Industrial | Control systems | Food |
|---|---|---|---:|---:|---:|
| BCER | `ItmSupplyKioskBCERInv` | Yes | — | — | — |
| BCRS | `ItmSupplyKioskBCRSInv` | Yes | 660 | — | 660 |
| EJDR | `ItmSupplyKioskEJDRInv` | Yes | 880 | — | 880 |
| HQCH | `ItmSupplyKioskHQCHInv` | Yes | 360 | — | 180 |
| JATL | `ItmSupplyKioskJATLInv` | Yes | 220 | — | — |
| JFTS | `ItmSupplyKioskJFTSInv` | Yes | 1320 | — | — |
| JPTN | `ItmSupplyKioskJPTNInv` | No; inactive template | 500 | — | — |
| MHNG | `ItmSupplyKioskMHNGInv` | Yes | — | — | 3300 |
| MLAB | `ItmSupplyKioskMLABInv` | No; inactive template | 2200 | — | 2200 |
| MSUZ | `ItmSupplyKioskMSUZInv` | Yes | 500 | — | 500 |
| MTRS | `ItmSupplyKioskMTRSInv` | Yes | 13200 | — | 13200 |
| MVOL | `ItmSupplyKioskMVOLInv` | Yes | 330 | 330 | — |
| OFLT | `ItmFlotillaScrapKioskInv` | Yes | — | — | 100 |
| OKLG | `ItmOKLGSupplyKioskInv` | Yes | 500 | 500 | 500 |
| SVIR | `ItmSupplyKioskSVIRInv` | Yes | 8800 | 8800 | — |
| VCBR | `ItmSupplyKioskVCBRInv` | Yes | 130 | 130 | — |
| VENC | `ItmSupplyKioskVENCInv` | Yes | — | 1100 | 1100 |
| VNCA | `ItmSupplyKioskVNCAInv` | Yes | 3300 | 3300 | — |
| VORB | `ItmVORBScrapKioskInv` | Yes | 3300 | 3300 | 1100 |

## Native production roles used for interpretation

These are native production-map identifiers, not new Phobos processes or measured real-world production.
Their economic roles inform the explicitly authored availability factors in the guide.

- **BCER**: `BCERTextilesAndElectronicsToSpaceSuitsConverter`, `BCERElectronicsToMediaConverter`.
- **BCRS**: `BCRSIndustrialProductsToOre`, `BCRSFoodToTrash`, `FoodConsumer`, `IndustrialProductsConsumer`.
- **EJDR**: `EJDRFoodAndIndustrialGoodsToHe3Converter`.
- **HQCH**: `HQCHIndustrialProductsToOresConverter`, `HQCHFoodConsumer`.
- **JATL**: `JATLElectronicsToSensorsConverter`, `JATLElectronicsHVACMetalAndIndustrialProductsToFusionPartsConverter`.
- **JFTS**: `JFTSTextilesAndElectronicsToSpaceSuitsConverter`, `JFTSIndustrialProductsToToolsConverter`.
- **JPTN**: `JPTNTextilesAndElectronicsToSpaceSuits`, `JPTNHVACAndIndustrialProductsToScience`.
- **MHNG**: `FoodProducer`, `ElectronicsProducer`, `MHNGWaterToFoodConverter`.
- **MLAB**: `FoodConsumerx3`, `IndustrialProductsConsumer`, `ElectronicsProducerx3`, `MLABFoodToTextilesConverter`, `MLABFoodToConsumerGoodsConverter`, `MLABIndustrialProductsToElectronicsConverter`, `MLABIndustrialProductsToHVACConverter`, `MLABFoodToTrashConverter`.
- **MSUZ**: `FoodProducer`, `ElectronicsProducer`, `MSUZWaterToFoodConverter`, `MSUZPlasticsAndMetalsToElectronicsConverter`, `MSUZIndustrialProductsToFurnitureConverter`, `MSUZIndustrialProductsToToolsConverter`.
- **MTRS**: `ElectronicsProducer`, `FoodProducerx3`, `IndustrialProductsProducer`, `MTRSElectronicsToConsumerGoodsConverter`, `MTRSMetalsAndIndustrialProductsAndToolsToFusionPartsConverter`, `MTRSMetalsToIndustrialProductsConverter`, `MTRSPlasticsAndMetalsToElectronicsConverter`, `MTRSWaterToFoodConverter`.
- **MVOL**: `IndustrialProductsProducer`, `MVOLScienceToIndustrialProductsConverter`, `MVOLScienceToControlSystemsConverter`, `MVOLPlasticsToControlSystemsConverter`, `MVOLElectronicsToToolsConverter`, `MVOLElectronicsToSensorsConverter`, `MVOLScienceAndIndustrialProductsToFusionPartsConverter`.
- **OFLT**: `FoodConsumer`.
- **OKLG**: `OKLGIndustrialProductsToControlSystemsConverter`, `OKLGIndustrialProductsToFurnitureConverter`, `OKLGIndustrialProductsToHVACConverter`, `FoodConsumer`.
- **SVIR**: `SVIRMetalToIndustrialProductsConverter`, `ControlSystemsConsumerx3`.
- **VCBR**: `VCBRIndustrialProductsToHVACConverter`, `VCBRElectronicsToScienceConverter`, `VCBRControlSystemsConsumer`.
- **VENC**: `VENCFoodToLuxuryGoodsConverter`, `FoodConsumer`, `ControlSystemsConsumer`.
- **VNCA**: `VNCAIndustrialProductsToPlasticsConverter`, `VNCAMetalToIndustrialProductsConverter`, `ControlSystemsConsumer`.
- **VORB**: `VORBMetalToIndustrialProductsConverter`, `ControlSystemsConsumer`.
