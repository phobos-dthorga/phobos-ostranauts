# Shipbreaker outputs: existing maintenance and construction uses

Read-only research on **2026-09-23**, while the owner prepares the Auto Nav test.
The useful conclusion is to feed existing repair and workshop systems. A second
conduit, battery or thruster crafting system would duplicate installed content.
This review also found and led to a fix for our construction recipe's input-count
limit; see [the 0.1.1 build guide](shipbreaker-first-build.md).

## Evidence and boundaries

Refreshed the installed inventory: 31 known packages, 29 enabled and two disabled
in the saved native load order. Crafting Framework and Salvage Workshop remain
**0.8.71**; Ship's Water remains **0.16.1**. The latest available startup log reports
21 registered OCF recipes. Salvage Workshop defines 23; its two Rusty's Twin
O2/EVA-battery recipes are excluded because their output items are absent.
This is definition and startup evidence, not an in-game crafting test.

The inspection reused `scripts/inventory-mods.py`, the tolerant JSON reader and
`load_definitions` from `scripts/inspect-salvage.py`. It found 264 native
installable-action definitions referencing at least one of our four useful output
families; that count includes installed/loose and other variants, not 264 distinct
useful projects. The native loader reports 11 same-package duplicate loot names,
as in the earlier audit. None is a repair/recipe ID cited below. Plugin-generated
objects and runtime changes are not fully represented by this static view.

Sources, kept outside Git except for our own files:

- Core `data/installables/installables_repair.json`: named repair records below.
  SHA-256: `7f3ea09d21549acc60e82339e9651bbf47405b4e1b453179e3488dcf3bc1e647`.
- Core `data/condowners/condowners.json` and `data/condtrigs/`: fixed masses and
  material trigger membership. The four output objects satisfy the corresponding
  `TIsPartsMechSmall`, `TIsScrapAluminum`, `TIsScrapCarbonFiber`, `TIsScrapSteel`
  conditions used by repair and workshop inputs.
- Salvage Workshop, Workshop item `3798573453`, `crafting/recipes.json`:
  SHA-256 `2c00a5bbe1a8b6ceddfb2966cef1622cb699672a9982c6bae86a9013cfe8074b`.
  Its `data/condowners/condowners_rmthruster.json` and
  `condowners_equipment082.json` supply the component/equipment masses.
- Crafting Framework, Workshop item `3798573443`: inspected
  `RecipeValidation.Validate` enforces at most 100 input and 100 output units
  per craft. Registration records in `BepInEx/LogOutput.log` corroborate the
  workshop recipes, not our uninstalled add-on.
- Phobos `src/PhobosShipbreaker/Core/ProcessRules.cs` and `Content.cs`: output
  quantities and the intentionally separate residue definition.

Local evidence reports are `.local/research/mod-inventory-material-uses.json`
and `.local/research/material-uses.json`. They are ignored research files, not
redistributed game data. No installed package, configuration, save or game session
was modified. No new outside dependency is proposed by this review.

## What one panel contributes

One ordinary 24 kg wall yields **2 small mechanical parts (1 kg), 2 aluminium
scraps (2 kg), 2 carbon-fibre scraps (2 kg), 6 steel scraps (6 kg), and 13 kg of
retained residue**. The 11 kg of useful products are native item identities, so
matching existing recipes does not require an adapter or replacement resource.

Each row below is an **alternative use** of that batch, not a claim that the same
stock can fund every row. Counts mean item units, not kilograms. Ordinary tools,
crew access, target state and work requirements still apply.

| Existing operation | Contribution from one processed panel | Still required | Source ID |
| --- | --- | --- | --- |
| Repair one ordinary damaged wall | All material inputs: 2 aluminium, 2 carbon fibre, 2 mechanical parts; 6 steel remain | Existing damaged wall and welding tool | `Wall1x1DmgRepair` |
| Repair one ordinary damaged floor tile | Same complete material inputs; 6 steel remain | Existing damaged floor and welding tool | `FloorGrate01DmgRepair` |
| Repair an atmospheric scrubber | All 2 mechanical parts and 2 steel | 1 motor, 1 heat sink, 1 motherboard, 1 electronic part; Mortorq and soldering tools | `AtmoScrubber01DmgRepair` |
| Repair an RCS cluster | 2 of 3 mechanical parts; all 1 aluminium and 1 carbon fibre | 1 more mechanical part, 1 motor; Mortorq and soldering tools | `RCSCluster01DmgRepair` |
| Repair a fusion core pump | 2 of 4 mechanical parts; all 1 steel | 2 more mechanical parts, 1 motor; Mortorq and soldering tools | `FusionCorePump01DmgRepair` |
| Repair Testudo Safe Pump | All 2 mechanical parts and 2 steel | 1 motor, 1 electronic part; Mortorq and soldering tools | `SafePumpTestudoDmgRepair` in Workshop item `3768554122` |

These are repairs to existing damaged objects, not fabrication of new wall/floor
tiles from 5 kg of stock. The normal game manages the damaged object and repair.
The patched-wall variant additionally lists a structure cutter; do not treat all
wall states as having the same tool requirements.

## Reuse Salvage Workshop's fabrication

| Existing recipe | Relevant use of recovered stock | Other needs / downstream route |
| --- | --- | --- |
| `SWB_AlbriteConduit` | 1 aluminium + 1 mechanical part per conduit | 1 electronic part; existing workbench. One panel supports two crafts if two electronic parts are available. |
| `SWB_AzulBB` | 1 aluminium per batch of 5 disposable batteries | 1 electronic part; existing workbench. This recipe already exists; adding another would not address replenishment of its other inputs. |
| `SWB_MakeThrusterComponents` | 1 each aluminium, steel, carbon fibre and mechanical part per kit | 1 motor, 1 heat sink, 1 electronic part; workbench and `IsSWBRMBlueprint`. Three kits become an RM Thruster at the pillar drill. |
| `SWB_MakeBatteryComponents` | 5 steel + 1 mechanical part per kit | 1 plastic scrap, 2 electronic parts, 1 heat sink, 2 motherboards; workbench and `IsSWBBatteryBlueprint`. Small/medium/large batteries consume 1/2/5 kits at the pillar drill. |
| `SWB_MakeRCSComponents` | 3 aluminium + 4 mechanical parts per kit, requiring more than one panel's contribution | 3 motors, 2 heat sinks, 1 motherboard, 2 electronic parts; workbench and `IsSWBRCSBlueprint`. Four kits become Rusty's RCS Distributor at the pillar drill. |

The framework's installed recipes already recover electronic parts, motherboards,
screens and heat sinks from eligible broken electronics (`SWB_SalvageElectronics`),
and mechanical parts, motors, heat sinks and motherboards from eligible broken
machinery (`SWB_SalvageMachinery`). Each guarantees one selection from its pool,
with optional extras; it does **not** guarantee the specific missing component.
Inputs must satisfy the existing broken-item trigger and empty-item requirement.
Keep those routes rather than inventing electronics or motors from wall residue.

Existing extinguisher refilling and cloth washing also use the workshop, with its
optional Ship's Water integration. They do not consume these panel outputs.
Industrial metal recovery therefore does not establish an air/water/food loop.

## Mass accounting remains local to our process

Recipe compatibility does not establish physical conservation throughout the
game. At the inspected fixed base masses, these existing transitions differ:

| Existing transition | Accounted input mass | Output mass | Static difference |
| --- | ---: | ---: | ---: |
| Damaged ordinary wall + its listed repair inputs | 18 + 5 = 23 kg | 24 kg | +1 kg |
| Damaged ordinary floor + its listed repair inputs | 3 + 5 = 8 kg | 6.5 kg | -1.5 kg |
| 3 Workshop thruster component kits -> RM Thruster | 24 kg | 25 kg | +1 kg |
| 1 Workshop battery component kit -> small ship battery | 9.3 kg | 18.4 kg | +9.1 kg |

These are source-definition arithmetic, not measured live transactions or a full
audit of container contents and runtime mass changes. They are sufficient reason
not to advertise the combined chain as mass-balanced. We have not patched those
upstream recipes or silently altered their items. Our panel conversion and staged
fixture construction retain their explicit material ledgers.

The **13 kg residue is not generic trash**. Its Phobos definition carries no scrap
or trash classification, and it has no current refining recipe. Connecting it to
the Workshop trash lottery would turn unknown panel material into unrelated
rewards and bypass its purpose. Any later recovery stage needs a declared
composition, plausible products and a retained remainder before it is worth
implementing. Available fusion energy does not establish that composition.

## Next useful decisions

1. Use the existing first processing batch to repair a damaged wall or floor when
   that need arises in play. This demonstrates a useful end-to-end benefit during
   the normal fixture test, without another isolated upstream power test.
2. Use the existing workshop for missing mechanical/electrical components. Record
   a concrete recurring shortage before adding a fabrication or recovery route.
3. Keep residue refining and thermal equipment as separate future work. The first
   machine's handling, throughput and retained waste still need the owner's
   gameplay judgement; no new processing layer is justified solely by this audit.

The immediate implementation from this review is the **construction-limit fix**,
not a speculative new manufacturing chain. Auto Nav's flight-test package and
behaviour were not changed during this work.
