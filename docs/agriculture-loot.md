# Agriculture supplies in native loot

Agriculture **0.6.2** adds planting stock, food and maintenance supplies to two
existing native contents pools. Framework 0.20.0 remains the required dependency.
This is prepared implementation with offline validation; owner gameplay checks
still need to confirm finds in newly generated eligible loot.

## Where items can appear

| Native contents pool | Added item | Default chance per pool roll |
| --- | --- | ---: |
| Fridge contents | Continuance seed potato | 3% |
| Fridge contents | Continuance lettuce seeds | 4% |
| Fridge contents | Raw potatoes | 6% |
| Fridge contents | Lettuce | 5% |
| Fridge contents | Hearth cooked potatoes | 4% |
| Locked-crate contents / its bulk-cargo uses | Continuance seed potato | 3% |
| Locked-crate contents / its bulk-cargo uses | Continuance lettuce seeds | 4% |
| Locked-crate contents / its bulk-cargo uses | Groundwork nutrients | 8% |
| Locked-crate contents / its bulk-cargo uses | Groundwork 5 kg irrigation charge | 6% |
| Locked-crate contents / its bulk-cargo uses | Groundwork treatment cartridge | 5% |
| Locked-crate contents / its bulk-cargo uses | Loose irrigation conduit | 4% |

Each pool roll gets **at most one** extra Agriculture item: 22% total for the
fridge pool, 30% for the crate pool. Native contents still roll normally. These
are authored availability choices, not percentages of ships or a guarantee of
finding anything in a particular container. A native parent may invoke a pool
more than once. These shared pools can be used outside derelicts as well.

The previous fridge seed branch is reused and expanded, so reloading definitions
does not append a second seed reward. A single 5 kg water charge remains finite;
no new stacks, measured drainage, terminal rejects or machinery are generated.
Racks, cookers and W2 units retain their existing purchase/construction routes.

## Settings and existing saves

In the Agriculture configuration's `[Loot]` section, `Enabled` defaults to
`true` and `ChanceMultiplier` to `1`. The multiplier accepts 0–3; zero disables
the additions. At 3, total chances are 66% and 90%, still at most one added item
per pool roll. Restart the game after changes. Merchant availability is separate.

Only **future native loot generation** uses the revised tables. Existing saved
inventories are not refilled or rerolled, and items already found remain when
loot additions are disabled. Loading a save is not a request to populate all
fridges. Ownership, lock access, container placement and native spawning remain
the game's responsibility. A full container may not place every rolled item.

## Native evidence and economic scope

Inspected [Blue Bottle Games' Ostranauts](https://bluebottlegames.com/ostranauts)
**1.0.1.5** local data: `data/loot/loot.json` defines
`ItmFridge01Contents` and `ItmRandomCrateLockedContents`.
`data/condowners/condowners.json` assigns the latter to the 3 x 3 locked crate
`ItmCrate01Lock`, with `TIsFitCrate`. `ItmLootSpawnBulkCargo` also references that
contents pool. The inspected `Li Bai Gen II Refit` ship data references the
fridge pool; this does not mean every native fridge uses it. No game data or
textures are redistributed by this change.

Framework's existing `Registration.AdditiveLoot` preserves native and foreign
entries, inserts an idempotent namespaced branch, and uses native cumulative
choice parsing. The content mod owns the selected items and probabilities.
Checks cover real native crate acceptance, bounded single-item choices,
repeat/disable registration, foreign-entry preservation and invalid settings.

At current **base definition prices**, the added expected value is 3.92 cr per
fridge-pool roll and 10.53 cr per crate-pool roll, before native placement, wear,
merchant/market effects and access costs. This is an arithmetic balance check,
not a live selling quote. No prices, yields or treatment efficiencies change.
Loot offers another finite acquisition route; it does not implement lettuce
propagation, nutrient manufacturing or autonomous indefinite survival.

Owner checks: explore newly generated eligible cargo/fridges; confirm finite
single-item quantities, ordinary ownership/locks and readable sprites; reload
without duplicated contents. Later compare actual supply availability with crop
consumption before increasing chances or adding new pools.
