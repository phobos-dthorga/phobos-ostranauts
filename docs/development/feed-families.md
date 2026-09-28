# Feed families: what the D4 takes and what it gives back

Decision record, 29 September 2026. Implemented by **Shipbreaker 0.36.0**. The
player-facing steps are in the [player guide](../player-guide.md#hand-fed-operation-without-the-grabber);
the wall budget and the R4 chain stay as recorded in the
[residue contract](residue-material-contract.md).

## Why families, not a wall list

The owner asked for the widest sensible range of scrap-like inputs, with raw
materials out, in preparation for Phobos Manufacturing and onboard life support.
Three facts from the game data (1.0.1.5) shaped the answer:

- The game has six scrap items (steel, aluminium, carbon fibre, 0.3 kg plastic,
  clean and dirty cloth), trash, small mechanical and electronic parts, and a
  few components. It has no ingots, copper, titanium, glass, rubber, wire or
  plate. Every recoverable output therefore uses a native identity; only the
  remainder needs one of ours.
- Native Dismantle yields are fixed per recipe and not mass-conserving: a
  6.5 kg floor grate gives 9 to 12.5 kg, a 181 kg door gives 12.5 kg, a 0.5 kg
  conduit gives 3 kg. They are game rewards, not compositions, and are not
  copied.
- Almost every structural part on a wreck is a cosmetic overlay variant of one
  base definition with its own mass. Identity is therefore the base definition
  (`WallIdentity.Base` resolves an overlay through `DataHandler.dictCOOverlays`),
  and mass is the part's own, not the base's.

A **feed family** (`src/PhobosShipbreaker/Core/FeedFamilies.cs`) is one or more
loose base definitions, an accepted mass range on a step (whole or half
kilograms), one shipped recipe revision and a declared budget for every accepted
mass. Budgets conserve mass, use native identities for everything recoverable,
end in one terminal reject identity priced at the technical minimum, and lose
value against selling the part whole (the owner's dismantling rule; the plain
ordinary wall remains the documented exception). Each accepted mass has one
immutable catalog; the job saved on a part (revision, progress, duration) keeps
its meaning across updates exactly as before.

## The families (0.36.0)

Prices: steel 3.60, aluminium 1.10, carbon fibre 0.12, plastic 46.00 per 0.3 kg,
mechanical parts 5.00 per 0.5 kg unit, rejects 0.01.

| Family | Loose bases | Mass | Budget | Products' value against the plain part |
| --- | --- | --- | --- | --- |
| Ordinary wall | `ItmWall1x1Loose` and its 15 loose overlays | 14 to 48 kg, whole | revision 2: 13 kg `PhobosPanelResidueR2`, 2 parts, up to 2 aluminium, up to 2 carbon fibre, steel for the rest | adds value (documented exception; the game's own wall does too) |
| Floor grate | `ItmFloorGrate01Loose` and its 117 overlays | 3 to 13 kg, half kilograms | 2 steel, 1 aluminium, 1 parts unit when the mass has a half, `PhobosFloorRejectR1` 1 kg units for the rest | 6.5 kg: 13.30 of 15.00 |
| DuraWal | `ItmWallPlastic1x1Loose` (4 overlays) | 14 kg | 3 plastic, 2 aluminium, 1 parts, 1 steel, `PhobosDuraWalRejectR1` 9.6 kg | 148.80 of 210.00 |
| Window | `ItmWallWindow1x1Loose`, `ItmWallWindow1x1SqLoose` | 10 kg | 2 aluminium, 2 parts, 1 steel, `PhobosWindowRejectR1` 6 kg | 15.80 of 330.00 |
| Whipple | `ItmWallThin1x1Loose` (6 overlays) | 5 kg | 3 aluminium, 1 parts, `PhobosWhippleRejectR1` 1.5 kg | 8.30 of 88.00 |
| Aero | six `ItmWallAero*Loose` bases | 4 to 6 kg, whole | 3 plastic, 1 aluminium plus 1 per kilogram above 4, `PhobosAeroRejectR1` 2.1 kg | 4 kg: 139.10 of 500.00 |

The native suite (`tests/PhobosNative.Tests/FeedFamilyNativeChecks.cs`) checks
every cosmetic variant against live game data: its family, its mass from the
overlay's condition loot, and that its products are worth less than the variant.

Refused, with a status line that names the family and its range: the five Van
Hummel floors at 7.15 kg (not on the half-kilogram step); the 37 floor variants
whose negative mass delta exceeds the base, which the game clamps to 0 kg in
play; the 65 kg turbine lifter (machinery in a floor's clothing); doors, hatches
and docking systems (a loose door is five cells wide and cannot enter the 4 x 4
feed); conduit (0.5 kg with no honest output); furniture, canisters, scrubbers
and machinery shells (fluid or electronic contents the budget cannot vouch for).

## Rules that carried over

- The 13 kg identified residue packet stays wall-only: its composition is the
  ordinary wall's matrix. Light families skip the R4 and end in their own
  terminal reject, which is never `ItmScrapTrash`: a remainder that could re-enter
  a future trash sink would stop being terminal, and a remainder that re-enters
  its own recipe converges to full recovery.
- The feed bin's game-level rule now admits any `IsWall` or `IsFloorGrate` part
  (the same conditions the game's scrap kiosks buy by) plus the cumbersome-fit
  rule; `FeedPatch` then applies the family rule (base, mass, single, empty).
  `IsCategoryHull` was rejected because it admits doors and machinery.
- Floor grates and Whipple panels stack natively (limit 5); the feed bins refuse
  merging so each unit keeps its own saved job, as the F6 charge bin already did.
- The G4 cutter and the capture mission still cut ordinary walls only.
- Light-family rejects leave through the D4's storage output or by hand; the C2
  collector does not take them yet (its four slots would fill with 1 kg units).

## Deferred, with the reason

- Heavy families (doors 181 kg, hatches 88 kg, airlocks 254 kg, furniture,
  machinery shells): products would number in the hundreds, so stacked product
  delivery in Framework comes first, then a feed that admits five-cell parts,
  then budgets with evidence rather than guessed electronics.
- An R4 trash sink (the game's own market maps convert 5 trash to 1 metal) and
  structural residue units need a multi-unit input contract; `ProcessJob` binds
  one input identity today.
- Conduit, and any refined metal without a consumer.
