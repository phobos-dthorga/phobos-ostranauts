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
| `src/PhobosManufacturing/Core/` | Pure rules compiled into the pure tests: `ManufacturingRules` (conditions, commodities, adjacency, the shared cycle rule), `Materials`, `RefineryRules` (+ `ChargeRecipe`, `RefineryRecipes`), `RefineryState`, `ProcessorRules`, `ProcessorState`, `SabatierRules`, `SabatierState`, `FuelStoreRules` (`FuelStore`, `FuelStores`, `MethaneRules`), `HydrogenRules` |
| `Definitions.cs` | Three `ApplianceDefinitions` families, the V4 feed bin and triggers, the X2 and H2 store, cloned materials, deflagration objects, `MiningLoot` |
| `RefineryService.cs` | F6-style charge model: Start binds the feed's exact multiset to a recipe; `BeforePower/BeginPower/FinishPower/AfterPower` credit seconds of powered work, admit room heat, emit off-gas, spoil a waiting melt; `Finish` delivers solids through `BatchPlacement` and water through `BulkVessel` conversion |
| `ProcessorService.cs` | Batch pattern: water hold drawn through `LiquidTransferGuard`, cycle energy credited per step, oxygen through `NativeGasCanister.TryAdd` or `RoomGas.Emit`, hydrogen into the store |
| `SabatierService.cs` | K2 (0.2.0): reactant hold charged from the H2 store (guarded) and the native CO2 canister (`NativeGasCanister.TryTake`), measured electricity plus reaction heat into the room, one saved conversion step from reactant hold to product hold, guarded delivery of water and methane, damage dump |
| `StoreService.cs` | Both fuel stores over `BulkVessel`: status, vent, damage/destroy decision (leak or burn), leak to space (hydrogen) or into the room (methane), 2-second leak scan; `Ignite` for a reactor's hydrogen |
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

## 0.2.0: Sabatier and methane

Owner decisions, 29 September 2026: a separate reactor rather than a second X2
mode, and a methane store rather than venting (an explicit exception to the rule
that a commodity needs a consumer). New save-stable identities:
`PhobosSabatierReactor*`, `PhobosMethaneStore*`, condition
`PhobosManufacturingReacting`, commodity `methane`, records `ManufacturingSabatier`
and `ManufacturingMethane` (with their journals and guards), and vessel-side ports
`PhobosManufacturing.SabatierVesselIn` and `PhobosManufacturing.StoreOut` (distinct
from the refinery's and the X2's, so one vessel serves all three). The CO2 canister
is a one-sided saved id, as the X2's oxygen canister is.

The reactor's record keeps reactants and products apart: a completed cycle turns
the reactant hold into the product hold in one saved step, and each product then
leaves under both transfer guards, so a reload during delivery cannot deliver twice
(the X2 keeps its 0.1.0 pattern). The hydrogen store's service became `StoreService`
over a `FuelStore` description shared with the methane store; hydrogen identities,
records, text and behaviour are unchanged, except that blast size is now chosen by
energy (identical results for hydrogen) and the explosion object's name reads "Gas
deflagration".

Verification, 29 September 2026: Manufacturing pure checks 135 PASS (Sabatier
stoichiometry and heat, the record's conversion and corrupt-record refusal, port
distinctness, methane store and burn); native checks 16,755 PASS (both families'
forms, names, power, the game's installed-CO2-canister trigger, the methane vessel
registration, economy parity and value); art export check. Not gameplay validation.

## 0.3.0: RCS propellant manifold (Framework 0.42.0)

Owner request, 29 September 2026: RCS thrusters burn the bulk gases, with a dedicated
switchable machine and piping. New save-stable identities: `PhobosPropellantManifold*`,
`PhobosPropellantLine*` (with `PhobosPropellantLineWaste`), conditions
`PhobosPropellantLinePresent`/`Intact`, the record `ManufacturingManifold`, and the map
points `PhobosPropellantIn` (manifold) and `PhobosPropellantOut` (both fuel stores). The
manifold's store links are one-sided saved ids, up to four, each with its own switch,
plus a master switch and the draw order; all start off.

`ManifoldService` implements Framework's `IRcsPropellantFeed`: Framework finds the
manifold on a regulator's gas-input tile while serving the engine's RCS draw and asks it
for nitrogen-equivalent mass; the manifold converts to each store's own kilograms and
takes them through `BufferedDrains`. Connection checks (within one tile, or a
`NativeFluidRoute` over intact propellant line, 64 tiles at most) are cached for five
seconds. The manifold is not airtight and not a gas container, so the game's own
refuelling never treats it as a canister (checked natively). The line art is a recorded
recolour of the shared conduit sheet (`scripts/export-propellant-line-art.py`).
