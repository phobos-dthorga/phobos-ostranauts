# F6 electrical heating: selected direction

**Implementation update (25 September 2026):** Shipbreaker 0.12.0 / Framework 0.16.0
now prepare the electrical casting candidate described in the [F6 operating guide](furnace-player-guide.md).
Read that guide for current dimensions, acquisition, controls and owner checks.
The research and direct-fusion installation diagrams below retain their historical scope;
they are not a record of an in-game test.

**25 September 2026 — owner-approved change of energy source.** Electrical
heating is now the first implementation route. This supersedes the earlier
direct-fusion-first requirement in the furnace research and installation study.
It changes the design direction, not installed gameplay or existing item IDs.

Electricity is the practical supported route, not a claim that direct fusion
integration is impossible. The native electrical network already supplies our
machinery. Direct process-heat extraction has no verified native outlet and no
longer blocks the first furnace.

## What carries forward

Retain the proposed 6 x 6 footprint, 50 kg hardware rating, exact 20 kg first
housing recipe, captive mould, gas receiver, exterior radiator, hot-state
persistence and vanilla-style instrument layout. Preserve the same local/C1/F3
checked service boundary. Cooling remains finite and essential.

Use **Phobos' Rivetline F6 Electric Furnace** as the revised provisional display
name. No saved equipment identity exists to migrate. An induction heater is the
existing researched candidate; its converter/coil design and final efficiency
remain equipment design work. Do not add coil current, frequency or stirring
readouts without an actual model.

The reactor-side heat coupler and adjacency requirement are removed from the
first build. Connect to ordinary native electrical supply. Any compatible native
source, including batteries, may supply it; a running fusion reactor is not a
separate eligibility requirement. Let the game account for upstream generation,
fuel and battery depletion. Do not debit reactor fuel again for the same electricity.

The initial installation mockup and coupler artwork list remain labelled historical
concepts until the next layout revision. Preserve the approved panel grouping;
replace source/coupler details with electrical demand, received power and brownout
status. No separate electrical cabinet or replacement coupler artwork is required
by this decision; incorporate the heater electronics in the F6 chassis by default.

## Concrete existing integration

`src/PhobosShipbreaker/ReclaimerHeat.cs` already witnesses native
`Powered.GatherPower` demand minus remaining demand, then accounts for the
appliance's stored-power change around consumption. This includes partial supply
and provides a practical receipt precedent. `UserPowerExt` alone is not a receipt,
and `IsPowered` alone is not a measured energy quantity.

Reuse/extract that concrete accounting into Framework when the furnace becomes
its second consumer. Bind each receipt to the appliance and consumption interval;
validate finite nonnegative amounts, avoid replay/nested-call double counting,
and distinguish storage charging from consumed energy. Convert received kWh to
kJ once. Shipbreaker owns the split into useful process heat and conversion losses.
Do not reuse the reclaimer's policy of sending the entire load straight into room
gas: the furnace stores process heat and routes losses to its finite cooling system.

Reduced supply slows enthalpy gain; no supply stops heating while stored heat and
passive cooling persist. Demand must respect thermal headroom before consumption.
Retain industrial load shedding for flight priority without taking over reactor
controls or requiring Auto Nav. The precise shared flight-priority signal remains
implementation work, including manual flight and installations without Auto Nav.

## Provisional power budget

Keep **250 kW as delivered process heat**, not the electrical input rating.
At the existing authored 90% conversion assumption, the heater needs up to
277.8 kW electrical input, plus 2 kW heating auxiliaries: approximately **280 kW
peak demand**. The 27.8 kW maximum conversion loss reaches the cooling assembly;
it does not disappear. Revalidate efficiency for the chosen heater hardware.

The existing offline model can be read with its `source_MJ` quantity as heater
electricity under this same 90% assumption. The cold case is 47.370 MJ heater
electricity + 3.648 MJ auxiliaries, approximately **14.17 kWh**, through thermal
release. Its roughly 54-minute estimate remains conditional on that delivery
and the proposed radiator. Handling, evacuation/equalization and bench finishing
remain additional; this is not measured native consumption or elapsed gameplay.

The next implementation checks are native electrical receipts under full/partial
supply, local storage use and recharge, multiple appliances, time acceleration,
flight load shedding and power loss, alongside the already specified material,
gas, thermal and save rules. No new furnace runtime is added by this decision.
