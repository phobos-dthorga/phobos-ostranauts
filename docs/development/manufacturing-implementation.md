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
| `src/PhobosManufacturing/Core/` | Pure rules compiled into the pure tests: `ManufacturingRules` (conditions, commodities, adjacency, the shared cycle rule), `Materials`, `Equipment` (the equipment pack), `LeachRules` (+ `LeachRecipes`, the LC-3), `ChargeRecipe`, `ChargeCatalog` (every charge machine's recipes, one pack load, per-machine views), `ChargeState` (`RefineryState` keeps the V4 record keys), `RefineryRules` (+ `RefineryRecipes`, the V4 facade), `ProcessorRules`, `ProcessorState`, `SabatierRules`, `SabatierState`, `FuelStoreRules` (`FuelStore`, `FuelStores`, `MethaneRules`), `HydrogenRules` |
| `Definitions.cs` | Three `ApplianceDefinitions` families, the V4 feed bin and triggers, the X2 and H2 store, cloned materials, deflagration objects, `MiningLoot` |
| `ChargeMachine.cs`, `ChargeMachineSpec.cs`, `ChargeMachines.cs` | Shared charge-machine engine (0.17.0, lifted from the V4's former `RefineryService.cs`): Start binds the feed's exact multiset to a recipe, automatically (V4) or to the selected one; `BeforePower/BeginPower/FinishPower/AfterPower` credit seconds of powered work, admit room heat including reaction heat, emit off-gas, spoil a waiting melt; `Finish` delivers solids through `BatchPlacement` and settles water and stored gases through Framework `CommoditySettlement`. A spec per family carries identity, texts, selection, gates, spoil policy and commodity links; the registry resolves definitions and feed bins |
| `mods/PhobosManufacturing/framework/equipment.json` | Read-only equipment pack: the V4's footprint, mass, power, room-heat share, feed cells, art and connection points |
| `ProcessorService.cs` | Batch pattern: water hold drawn through `LiquidTransferGuard`, cycle energy credited per step, oxygen through `NativeGasCanister.TryAdd` or `RoomGas.Emit`, hydrogen into the store |
| `SabatierService.cs` | K2 (0.2.0): reactant hold charged from the H2 store (guarded) and the native CO2 canister (`NativeGasCanister.TryTake`), measured electricity plus reaction heat into the room, one saved conversion step from reactant hold to product hold, guarded delivery of water and methane, damage dump |
| `StoreService.cs` | Both fuel stores over `BulkVessel`: status, vent, damage/destroy decision (leak or burn), leak to space (hydrogen) or into the room (methane), 2-second leak scan; `Ignite` for a reactor's hydrogen |
| `ShipbreakerStock.cs` | `OptionalStock`: soft detection of an optional Phobos mod and its published identities at the expected masses; `ShipbreakerStock` (0.38.0+, the steel charge) and `AgricultureStock` (0.9.0+, the makeup packet for the LC-3's formulation) |
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

## 0.4.0: gas store ladder, oxygen/nitrogen/CO2 stores and the L2 (Framework 0.44.0)

Owner request, 29 September 2026: medium and large variants of every bulk family,
applied retroactively, bulk O2/N2/CO2 stores (reversing the earlier "no gas
silos" rule, because the game has canisters only for those three), and a safe
canister filling station. New save-stable identities:

- **Store sizes:** `PhobosHydrogenStoreMedium/Large`, `PhobosMethaneStoreMedium/Large`,
  and `PhobosOxygenStore*`, `PhobosNitrogenStore*`, `PhobosCarbonDioxideStore*` in
  all three sizes, with records named after the small ones plus the size.
- **Filler:** `PhobosCanisterFiller*`, the record `ManufacturingFiller`, the
  condition `PhobosManufacturingFilling`, the rack trigger
  `TIsFitContainerPhobosCanisterFillerRack` and the map point `PhobosGasLineIn`.
- **Commodities:** `oxygen`, `nitrogen` and `carbon dioxide`.

`GasStores` replaces the fuel-store table: five `GasFamily` declarations, each
built into three `GasStore` sizes through Framework `BulkVesselSizes`. A family's
optional `Combustion` keeps the hydrogen and methane burns. The line port moves
with the footprint (`GasStore.Outlet`). The X2's oxygen destination and the K2's
CO2 source accept bulk stores within one tile as well as canisters, through
guarded transfers; the manifold accepts all five gases. `FillerService` follows
the X2's power-receipt pattern and moves gas through Framework `GasTransfers`,
settling the RCS manifold's buffered draws first. Medium and large stores are not
salvage loot; the engineering-loot share is split across machines and small
stores only.

## 0.5.0: the A2 cabin air regulator

New save-stable identities: `PhobosCabinAirRegulator*` (four forms) and the record
`ManufacturingRegulator` (on, both set points, both store links and the running
totals). It reuses the map point `PhobosGasLineIn` and the gas line fixture port.

`RegulatorService` runs from the plugin's two-second scan for every installed A2,
not from the power step: it needs no work demand, only the native `IsPowered`
condition that its 0.1 kW appliance power sets. It reads the room through Framework
`RoomHeat.Read`, computes the shortfall in `RegulatorRules` (pure, checked in
`RegulatorChecks`), drains the linked store and emits into the room through
`RoomGas`. The elapsed time comes from the game clock, so fast-forward scales the
flow and a pause adds nothing; a jump of more than an hour is skipped rather than
paid as one burst. Unlike the batch machines it keeps working after a reload,
like the game's own air pumps; its record is protected and accepted like the
others. Links resolve through the same gas-line connection as the L2.

The L2 gains the crew order Keep suit bottles charged: `FillerCrewProvider`
implements Framework's `ICrewWorkProvider` and `ICrewOrderPresentation` with one
recipe, `charge-bottles`, and a right-click toggle `PhobosCanisterFillerBottleOrder`
on the intact installed form (the Shipbreaker `ToggleFeeding` pattern: ship-wide
source, routine resume, off is a manual stop). Hauling is Framework
`CrewLogistics.Supply`/`Output`, so its rules about hands, locked containers and
system bins apply unchanged; our filter adds "uncharged" (below
`FillerRules.ChargedFraction`, 0.9) and excludes bottles already in an L2's rack.
The only machine action is Start, offered when the rack holds an uncharged bottle
and the station is paused. Stopping the order never stops a running fill.
