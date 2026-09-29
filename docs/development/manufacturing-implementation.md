# Phobos Manufacturing: implementation record

29 September 2026. **Version 0.1.0 is the first operational release**: the
Fennmark V4 Volatiles Refinery, X2 Chemical Processor and H2 Hydrogen Store,
five materials, hazards built on the game's own mechanics, the same economy
routes as the sibling mods, and original artwork. The M4 machining centre of the
[first research round](manufacturing-research.md) remains a design; the
[handover](manufacturing-handover.md) is history. Chemistry, energies, hazards
and sources are in [the refinery record](manufacturing-refinery-and-chemistry.md);
player instructions in [the player guide](../manufacturing-player-guide.md).

## Layout

| Location | Purpose |
| --- | --- |
| `src/PhobosManufacturing/Core/` | Pure rules compiled into the pure tests: `ManufacturingRules` (conditions, commodities, adjacency), `Materials`, `RefineryRules` (+ `ChargeRecipe`, `RefineryRecipes`), `RefineryState`, `ProcessorRules`, `ProcessorState`, `HydrogenRules` |
| `Definitions.cs` | Three `ApplianceDefinitions` families, the V4 feed bin and triggers, the X2 and H2 store, cloned materials, deflagration objects, `MiningLoot` |
| `RefineryService.cs` | F6-style charge model: Start binds the feed's exact multiset to a recipe; `BeforePower/BeginPower/FinishPower/AfterPower` credit seconds of powered work, admit room heat, emit off-gas, spoil a waiting melt; `Finish` delivers solids through `BatchPlacement` and water through `BulkVessel` conversion |
| `ProcessorService.cs` | Batch pattern: water hold drawn through `LiquidTransferGuard`, cycle energy credited per step, oxygen through `NativeGasCanister.TryAdd` or `RoomGas.Emit`, hydrogen into the store |
| `HydrogenService.cs` | Thin `BulkVessel` adapter: status, vent, damage/destroy decision (leak or deflagration), 2-second leak scan |
| `ShipbreakerStock.cs` | Soft detection of Shipbreaker 0.38.0+ and resolution of its ingot/remainder definitions; `Content.Prepare(bool steelStock)` for tests |
| `EquipmentEconomy.cs` | Prices, work, repair bills, mass-balanced salvage, Restore rates, merchants, regional markets, engineering loot |
| `Provider.cs`, `Panel.cs`, `Plugin.cs` | C1 provider and panel fields, the local Control Panel over `ConsoleShell`, Harmony patches and the `phobosmanufacturing` console |
| `mods/PhobosManufacturing/` | `mod_info.json`, `data/conditions` marker, `data/explosions` deflagrations, `framework/equipment-names.json`, `images/phobos/manufacturing/` exports |
| `translations/PhobosManufacturing/en.json` | Player text, also embedded |
| `tests/PhobosManufacturing.Tests/` | Pure checks; `tests/PhobosNative.Tests/ManufacturingNativeChecks.cs` with the game |
| `assets/phobos-manufacturing/` | Art brief; masters live in `assets/artwork-completion` |

## Contracts kept

- Identities are save-stable from 0.1.0: `PhobosVolatilesRefinery*`,
  `PhobosChemicalProcessor*`, `PhobosHydrogenStore*`, the five materials, the
  three `SysPhobosDeflagration*` objects, ports `PhobosManufacturing.RefineryOut`,
  `VesselIn`, `WaterIn`, `VesselOut`, `HydrogenOut`, `StoreIn`, and the records
  `ManufacturingRefinery`, `ManufacturingProcessor`, `ManufacturingHydrogen`.
- A bound charge keeps its captured recipe revision. A steel charge saved before
  Shipbreaker is removed is retained and reported, never overwritten.
- Reload drops the session: the V4 and X2 pause with their records intact and
  wait for Start (the Phobos batch pattern). Receiving/processing permissions
  do not exist here; there is one Start.
- Native precedence: refusals at offer time through `NativeEffects.Refuse`
  (uninstall or dismantle with a bound charge, held water or stored hydrogen);
  no native definition republished by name; destruction never blocked, the
  store's contents decided before the native destroy.
- Room heat and gas only through Framework 0.41.0 `RoomHeat`, `RoomGas` and
  `NativeGasCanister`; native species only, clamped; hydrogen never a species.
- The water vessel is any registered Framework water vessel within one tile
  (Chebyshev rule generalised to two footprints in `ManufacturingRules.Adjacent`);
  Manufacturing requires Framework only.

## Build, package and install

```powershell
./scripts/build-shipbreaker.ps1 -OstranautsPath '<local game folder>'   # once, for the steel charge in native checks
./scripts/build-manufacturing.ps1 -OstranautsPath '<local game folder>'
./scripts/install-mods.ps1 -Mods Shipbreaker,Manufacturing -WhatIf
```

`build-manufacturing.ps1` builds Framework and Manufacturing, runs the pure and
native suites, checks the artwork exports and packages
`dist/PhobosManufacturing-P0.zip` with the player guide as README plus the
design records. The installer reads the package's `mod_info.json` and applies
the maintained `Manufacturing.Framework` minimum (0.41.0 from 0.1.0); the 0.0.1
scaffold keeps its 0.17.0 floor and `-HoldManufacturing` still retires it.

## Verification record

29 September 2026: Framework pure checks 19,784 PASS; Manufacturing pure checks
97 PASS; native checks 16,396 PASS with Ostranauts 1.0.1.5 (Shipbreaker present
and absent through `Prepare(true/false)`); artwork export check; constants,
release-notes, item-reference, handling and language audits. None of this is
gameplay validation; the owner checks are listed in the player guide.

## Later rounds (recorded, not built)

- 0.2.0 Sabatier on the X2: CO2 from a paired `ItmRTACO2` via
  `NativeGasCanister.TryTake`, hydrogen from the H2 store, methane as explicit
  discharge or a commodity once a consumer exists; a cooling fault vents CH4.
- Crew loading orders for the V4; a nitrogen-bearing chunk -> NH3 -> N2 into
  `ItmRTAN2`; silicates -> O2 by molten-regolith electrolysis; plastics only
  with real polymer chemistry; the M4 mill consuming nickel-iron and steel;
  construction of advanced equipment; a 16 px master for the clay chunk.
