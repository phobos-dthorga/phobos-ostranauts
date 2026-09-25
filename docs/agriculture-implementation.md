# Agriculture implementation and validation

Agriculture 0.7.0 adds [lettuce seed production](agriculture-seed-production.md) and
[maintenance/treatment economics](agriculture-treatment-economy.md). Existing food
crops and already bound treatment jobs retain their previous contracts.

Current extension: [fluid-network operations](fluid-network-operations.md) documents Agriculture 0.6.0 / Framework 0.20.0 fan-out, line contents, treatment and optional Shipbreaker 0.17.0 coolant servicing. Earlier version-specific sections below retain their baseline scope.

25 September 2026. **Implementation candidate; no game session has been run.**
Read the [player guide](agriculture-player-guide.md) for the actual first version.
The earlier [specification](agriculture-first-slice.md) retains broader design
targets; this record identifies delivered behavior and remaining work.

Version 0.5.0 adds [finite nutrient-solution piping](agriculture-nutrient-solutions.md).
The W2 mixes content-owned potato/lettuce profiles; Framework 0.19.0 transfers
both components with measured receipts and interruption journals. Additive saved
solution records preserve old water and dry-stock meanings. Existing equipment,
artwork, crop yields and manual supply remain usable. No new art was generated.

Version 0.4.0 implements the [first routed-water slice](agriculture-water-conduits.md):
W2 finite supply, independently installed conduits, one reciprocal rack binding,
actual electrical budgets, local nutrient loading and explicit legacy/routed
selection. Framework 0.18.0 supplies native floor routing, bounded delivery budgets
and durable interruption journals. Existing crop, cooker and residue contracts
remain unchanged. Artwork and routes still need owner gameplay evaluation.

The 0.1.1 branding revision establishes **Verdemorrow Agronomics**, independent
of Shipbreaker's Rivetline. Equipment uses Firstlight-4 and Hearth-2; planting
stock uses Continuance; nutrients use Groundwork. Produce, meals and byproducts
also use the shared localized naming mechanism. Native definitions, construction
fallbacks, merchant names and panel titles derive from the same content catalog.
The owner confirmed the mod has never been used, so its equipment/supply/recipe
IDs now use `PhobosVerdemorrow...` without migration aliases. No biology, balance,
footprint or artwork changes accompany the rename.

Version 0.2.0 adds state-driven world rack artwork, six lettuce states and new
ChatGPT-generated rack/galley furniture masters. See [visual implementation](agriculture-living-visuals.md).
Development continues before owner gameplay testing, as requested on 25 September.

Version 0.3.0 applies the [economic follow-up](agriculture-economy-review.md):
revised equipment/seed prices, separate repair bills, useful intact/broken salvage,
condition-based stock, stored planting-stock finds and finite manual irrigation
charges. Native Restore and construction contracts are unchanged.

## Delivered

- Separate Framework-dependent native/content package, construction, additive
  merchant stock, repairs/Restore, condition-specific mass-balanced dismantling, translated
  equipment branding and native Control Panel lifecycle with shared widgets.
- Agriculture-owned potato/lettuce cohorts: finite water/nutrients, actual native
  electrical receipts, net CO₂ uptake/O₂ release, explicit blocked/dark respiration,
  water vapour and room heat. Gas changes go through the native room gas container.
- Local crew planting, loading, harvesting/clearing; physical output fit checks;
  potato propagation reservation; explicit native food effects and a cooker.
- Framework 0.17.0 `EquipmentProviders` immutable snapshots/action lists and
  content-owned checked dispatch. Shipbreaker 0.14.0 discovers providers through
  its existing console access boundary. Fixed industrial recipes and their
  one-hour limit are unchanged.
- Framework `FiniteLiquidTransfer` measures the source debit and destination
  receipt, bounds capacity/reserve, rejects different ships and restores unreceived
  debit. The optional `ShipsWaterSupply` adapter is scoped to inspected 0.16.1.
  No wastewater, refuelling or generalized chemical-fluid API was introduced.
- ObjectStateStore schema 1 saves quantities, crop/cohort, captured duration,
  growth/health and cooker input ID. Automation permissions are session-only;
  corrupt/future/inconsistent content is protected. A >1 hour unobserved step
  stops automation instead of manufacturing catch-up growth.

Source observations: installed game **1.0.1.5**, BepInEx 5 references, and
**Valtora's [Ship's Water 0.16.1](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)**.
Local inspection of its Plumbing/WaterMath found `TIsWaterVesselInstalled` and
`StatLiqH2O` litre quantities. The adapter preserves its tank selection semantics
with additional same-ship/owner/lock guards. It neither changes foreign capacities
nor claims that the provider already accounts liquid weight in `StatMass`.
Quantity conservation is tested; provider integration and ship flight mass need
owner verification. Extracted source remains ignored and is not distributed.

## Deliberate first-version simplifications

- One native 8 × 8 inventory holds physical inputs and outputs. The proposed
  separate six-kilogram harvest drawer and two-kilogram residue buffer are not
  separate runtime containers. Reservoirs and each cohort still have finite mass.
- The cooker processes one 0.4 kg portion per command, preserving the proposal's
  0.5 kWh per 4 kg energy ratio. Its received energy enters room heat; food
  temperature is not a second stored heat balance.
- Healthy cultivation is a continuous-light **net** growth surrogate. Simplified
  carbohydrate chemistry and retained/transpired water are authored balance.
  It does not reproduce photoperiods, real plant physiology, microbial decay or
  human nutrient requirements. Dead retained matter may continue the same slow
  oxidation surrogate. No oxygen is awarded just for elapsed time.
- Native room temperature/pressure are explicitly identified as native room data.
  They are not advertised as independently simulated root-zone probes. Dedicated
  root/CO₂ instruments, acceleration faults and maintenance schedules remain later
  work. Existing cabin failures and native equipment damage provide interruptions.
- Potatoes and lettuce each have six states in the world and local panel. Separate
  source layers are flattened into registered native textures at export time;
  native Item.SetAlt selects the saved cohort's image. Four tray pictures are one
  cohort. Source masters and native-scale exports remain separate. Dedicated stock
  and damage artwork remains future work; native wear still applies, and supplies
  reference native artwork at runtime. In-game appearance remains unverified.
- Seed potatoes are reserved now. Lettuce reproduction, nutrient recovery,
  drainage treatment and asteroid feedstocks remain roadmap work.

## Evidence and scientific boundaries

NASA's [plant-water research](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/)
supports contained root delivery with both water and air; it does not validate our
20 L reservoir. Ray Wheeler's [NASA crop discussion](https://www.nasa.gov/science-research/nasa-plant-researchers-explore-question-of-deep-space-food-crops/)
supports the crop pair. R. M. Wheeler's [life-support overview](https://ntrs.nasa.gov/citations/20205008786)
informs area/lighting constraints. [ESA MELiSSA](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Closed_Loop_Concept)
supports staged recovery and replenishment, not perfect recycling. All four are
references rather than institutional validation of our implementation.

## Offline checks versus owner gameplay

Automated checks exercise per-step water/nutrient/carbon/gas mass and heat balance,
full cycles, partial power, dry roots, respiration and stress at different step
sizes, saved permission resets, malformed state, finite water reserve, failed/full
destinations and docked-ship rejection. Native-definition checks inspect game
references, construction bills, branding, power and food effects. Framework,
Shipbreaker and installer regression suites accompany package preparation.
These checks cannot prove Unity panel behavior, native action completion, visual
fit, power wiring or cross-mod runtime compatibility.

Owner-run scenarios in ordinary play:

1. With Framework and Agriculture only, buy/build both machines, install/wire,
   load stock and manual water, plant each crop, harvest, cook and eat. Verify
   material quantities, native hunger response and the physical footprint.
2. Compare normal speed and fast-forward. Interrupt planting/harvesting; ensure
   no free seed, duplicate produce or lost partially completed work. Reload at
   growth, readiness, harvest queue and partial cooking. Resume explicitly.
3. Reduce power, remove water/nutrients and change cabin conditions. Observe
   slower growth or progressive stress; stop the machine and confirm biology
   continues. Restore conditions before loss, then repair damaged equipment.
4. Fill inventory before harvest/cooking. Check source retention, clear space and
   retry once. Remove a bound cooking portion and confirm replacement cannot
   inherit its progress. Clear/drain before dismantling.
5. With Ship's Water 0.16.1, compare the exact tank debit and rack receipt at the
   reserve; repeat while docked. Remove/disable only the optional integration in
   a controlled comparison and verify manual operation, preserving provider saves.
6. With C1, verify same-ship discovery and Start/Pause, loss of console power,
   crew access and docked isolation. Do local physical work at the rack.
7. Observe one-person and small-crew food coverage over several harvests. The
   ideal research budget estimates roughly 60% of one vanilla crew member's food
   debt from one rack of each crop; stress, leftovers and travel time reduce this.
   Do not claim a complete diet or indefinite endurance from that estimate.

Keep the next round focused on actual owner findings before more crop species.


### Validation result, 25 September 2026

Agriculture and Framework Release builds completed without warnings or errors.
Passed 336 Agriculture accounting/harvest/persistence/liquid checks, six research
budget tests, 6,045 native-definition/registration checks, 1,954 Framework checks,
35 performance-adapter checks, 7,336 Shipbreaker checks and 31 observation checks.
The installer passed 163 checks against synthetic installations, including an
Agriculture-only selection that automatically installs Framework and preserves
unrelated entries. No live installation or owner save was changed.

Review corrected liquid-ration admission (the native solid inventory filter
rejects water), native cargo mass inclusion and crop mass restoration after
native damage/repair mode changes. These native integration paths still require
the gameplay scenarios above; passing offline checks is not a substitute.
