# Phobos Agriculture: first-slice specification

**25 September 2026 — design baseline.** Agriculture 0.2.0 now implements a
first candidate; see [implementation differences](agriculture-implementation.md)
and the [current player guide](agriculture-player-guide.md). This document specifies
the crop-first direction selected by the owner. Read the
[research evidence](agriculture-research.md) and [endurance roadmap](agriculture-roadmap.md)
alongside it. All equipment ratings, yields, durations, tolerances and food values
below are **authored candidates for gameplay evaluation**, unless labelled observed.
The broader design below is not a statement that every proposed feature ships.

## Playable outcome and boundaries

Acquire planting stock, water and complete nutrient supplies; construct and plant
a rack; maintain its environment; harvest physical produce; cook potatoes; keep
propagation stock and remove residues. An installed rack controls its own lamps,
root irrigation and air circulation. Crew perform planting, tending, harvesting,
cleaning and repairs. It should reduce repeated shopping without eliminating
space, power, water, consumable or maintenance constraints.

Start with potatoes and lettuce. NASA's Ray Wheeler discusses candidate staple
crops and potato propagation in [NASA's deep-space crop research](https://www.nasa.gov/science-research/nasa-plant-researchers-explore-question-of-deep-space-food-crops/).
This informs selection; it does not establish the game recipes below. A complete
human nutrition simulation, animals, pressure-independent greenhouses, radiation
biology and a new mining system are outside the first slice.

Phobos Framework is the only required Phobos dependency. Agriculture owns all
crop identities, biology, balance, recipes, artwork and equipment definitions.
Use native JSON where sufficient and a content-owned C# lifecycle service where
required. Preserve existing Framework fixed-batch processing and its one-hour
duration limit; growing crops are not very long industrial jobs.

## Equipment, space and acquisition

These names and model designations are proposed, not already assigned to released
items. Follow the [branding policy](equipment-branding.md), localization catalogues
and existing damaged/repair/salvage naming conventions together.

| Candidate equipment | Footprint and capacity | Electrical and thermal design |
| --- | --- | --- |
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | 4 × 4 tiles; 80 kg dry; one crop cohort; 20 L process-water reservoir; 0.5 kg nutrient cartridge; harvest drawer rated 6 kg with an 8 × 8 inventory grid; separate 2 kg residue buffer | 1.5 kW maximum; ideal cycle averages 0.75 kW potatoes and 0.4 kW lettuce; powered idle target 0.02 kW, additional to cycle budgets |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | 2 × 2 tiles; 12 kg dry; one 4 kg potato charge; products remain in its physical inventory until collected | 2 kW for 15 minutes per 4 kg batch, 0.5 kWh; no heat credit merely for being connected |

The rack is a contained root cassette inside a habitable cabin, with a lamp,
circulation fan, finite irrigation loop and condensate return. It is not its own
pressure vessel. Root delivery uses retained media and capillary-supported flow;
NASA's [Station Science 101 plant research](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/)
explains why supplying roots with both water and air is difficult in microgravity.
Changing thrust does not turn an ordinary open trough into a reliable system.
Native acceleration exposure and any useful malfunction response remain an
implementation investigation; do not invent an exact acceleration sensor.

Four-by-four tiles describe installation geometry, not square metres of canopy.
NASA researcher R. M. Wheeler's [life-support overview](https://ntrs.nasa.gov/citations/20205008786)
gives an indicative approximately 50 m² of crops per person's dietary calories.
The compact candidate is intentionally compressed gameplay. Inspect the footprint,
crew access, plant legibility and production together before accepting the balance.

Use Framework construction on obtainable native Bar/Dining Tables and optional
Workshop benches, additive merchant stock and native maintenance helpers.
Stock seed potatoes, lettuce seed packets and formulated nutrients through ordinary
shops. Assign actual bills and prices after comparing current equipment economy;
an 80 kg housing must have an accounted material bill and salvage remainder.
Shipbreaker-derived suitable material can satisfy an existing ingredient contract,
but Shipbreaker is not required to buy, construct or repair the rack.

All stored liquid, nutrients, plants, produce and residues contribute to ship mass
exactly once. Numeric reservoir state must back an actual mass-bearing native
container representation, with an implementation check before release; it must
not become a weightless virtual inventory. Block dismantling with living crops or
contents, and provide deliberate local unloading/salvage instead of deleting them.

## Crop profiles and crew work

| Profile | Ideal game duration | Planting stock | Products at ideal harvest | Propagation decision |
| --- | --- | --- | --- | --- |
| Potato food crop | 96 hours | 0.20 kg viable seed potato | 4.20 kg tubers + 0.80 kg wet non-edible biomass | Reserve 0.20 kg tubers, leaving 4.00 kg food; eating the reserve sacrifices the next planting |
| Lettuce food crop | 48 hours | One 0.005 kg seed packet | 1.00 kg edible leaves + 0.20 kg wet residue | This harvest produces no new seeds |
| Lettuce seed crop, later propagation increment | 96 hours | One 0.005 kg seed packet | Four 0.005 kg seed packets + 1.18 kg non-edible biomass | Keep one packet to repeat seed production; three support later food crops |

The seed-crop profile is budgeted now to prevent a fictitious closed loop, but
first runtime delivery can use purchased lettuce seeds. Explicit seed mode is
selected at planting; do not allow switching a nearly finished leaf crop into a
finished seed crop. Seed quality and loss need a usable treatment before claiming
sustainable propagation. The ideal table assumes viable stock and no losses.

R. M. Wheeler and colleagues' [NASA Biomass Production Chamber study (1996)](https://ntrs.nasa.gov/citations/20040089951)
reports 28–30-day lettuce and 90–105-day potato crops under its conditions. Our
two/four-day defaults are deliberately accelerated. A duration multiplier of
0.5–2.0 applies to newly planted cohorts, with their resource/energy budgets
captured at planting. Shorter duration requests proportionally more power within
the 1.5 kW limit; it does not discount total resources. A power limit can make the
actual cycle longer than the chosen duration.

Candidate progress stages: sprout 0–10%, young 10–40%, mature 40–85%, finishing
85–100%, then harvest-ready. Potato tubers need not be visibly exposed to signal
readiness: the panel and distinct canopy state can communicate it. Wilt/death
override the healthy appearance, not saved crop identity. Lettuce seed mode needs
its own later bolting/seed-head artwork rather than pretending leaves are seeds.

Budget one crew-hour of rack turnaround per cycle: 15 minutes planting, 30 minutes
harvesting and 15 minutes cleanup. These are balance candidates, not timing
measurements. Tending is a brief inspection/cleaning intervention when flagged;
avoid repetitive watering clicks when supplies and automation are healthy.
Interrupted actions retain elapsed work and exact inputs. No partial action
produces its full output, and two crew cannot collect the same harvest.

Potatoes require a simple preparation route. Proposed cooker output is ten
0.40 kg portions from 4.00 kg tubers, with five native food-debt units and three
satiety units per portion. Lettuce gives four 0.25 kg servings, each one food-debt
and one satiety unit. These are game effects, not calories or dietary advice.
Do not inherit every native prepared-food effect: the inspected native prepared
trigger grants nine food-debt units plus other rewards. Implement crop-specific
effects through the existing eating paths and check AI selection and manual use.
Retained seed packs, residues and nutrient cartridges are not food by default.

The closed cooker retains food water or collects any separated condensate; it
cannot discard unreported mass. Its electrical input is accounted once as food
sensible heat and room heat. Cooking energy is additional to the rack calculator,
and harvesting/cooking delays are additional to its ideal production schedule.

## Candidate resource budgets

The repeatable [budget calculator](../scripts/calculate-agriculture-budget.py)
is the authoritative candidate arithmetic. It is a deliberately simplified
mass-and-energy ledger, not a measured crop assay, elemental nutrient formula,
photosynthesis rate law or full thermal simulation.

Dry fractions used solely for this surrogate: potato stock/tubers 20%, potato
residue 10%, lettuce leaves 5%, lettuce food residue 10%, lettuce seeds 90%, and
lettuce seed-crop residue 5%. Retained formulated nutrient solids are included in
final dry matter. Remaining new dry matter is treated as CH2O-equivalent material.
Use the formal net mass relation **44 CO2 + 18 H2O → 30 CH2O + 32 O2**. This
abstracts real plant chemistry; it does not imply all dry matter is glucose or
that every ion in a real fertilizer cartridge is incorporated into a plant.

| Cycle ledger, kg unless stated | Potato | Lettuce food | Lettuce seed |
| --- | ---: | ---: | ---: |
| Incoming planting stock | 0.200 | 0.005 | 0.005 |
| Retained nutrient solids | 0.040 | 0.005 | 0.010 |
| Net CO2 uptake | 1.232000 | 0.088733 | 0.091667 |
| Gross water drawn, including subsequently recovered water | 8.4240 | 3.1658 | 5.1600 |
| Harvest product, including reserved stock | 4.200 | 1.000 | 0.020 |
| Wet residue | 0.800 | 0.200 | 1.180 |
| Recovered process condensate | 3.800 | 1.900 | 3.800 |
| Water vapour passed to cabin | 0.200 | 0.100 | 0.200 |
| Net oxygen released | 0.896000 | 0.064533 | 0.066667 |
| Net water makeup | 4.6240 | 1.2658 | 1.3600 |
| Electrical input, kWh | 72.000 | 19.200 | 38.400 |
| Direct room heat, kWh | 67.8972 | 18.8463 | 37.9688 |
| Food-debt units after propagation reserve | 50 | 4 | 0 |

Water passes into tissue, reaction water and transpiration. The authored 95%
condensate return applies **only to the modelled transpiration**, not all irrigation
or crew wastewater. Condensate stays in the agricultural loop and is not certified
potable. The unreturned fraction enters native room vapour only if an appropriate
finite native transfer is verified; otherwise retain it in the contained circuit
and constrain operation. Never silently destroy it.

For potatoes, inputs total 9.896 kg and outputs total 9.896 kg. The 0.84 kg of
new carbohydrate-equivalent matter stores 3.9667 kWh using an authored effective
17 MJ/kg. Escaping vapour carries 0.1361 kWh using a rounded 2.45 MJ/kg latent
heat convention. The remaining 67.8972 kWh becomes room heat. These coefficients
are bookkeeping assumptions, not a temperature-dependent physical model.
Condensation heat is included in the room term; do not add it twice. Later native
condensation of escaped vapour must also not double-credit latent energy.

Resource budgets are a **net healthy-cycle reference**. Actual biology needs a
content-owned respiration step: living tissue consumes oxygen and releases CO2,
water and heat even without useful illumination. Gross assimilation and respiration
must settle through the same finite matter ledger so a nominal cycle reproduces
the net budget; delays and stress may reduce edible yield and change gas totals.
Do not implement this table as an oxygen timer. Validate native gas species units,
room isolation, gas energy and trace-quantity precision before enabling exchange.
If those transfers cannot be made reliably, simplify the first public scope openly;
do not advertise biological life support or award free gases.

The nutrient figures count only retained solids. Actual complete nutrient supply
must account for carrier water, counter-ions, unabsorbed salts and purge liquid in
the finite reservoir. First supply is a purchased, labelled complete formulation;
specific N/P/K chemistry and recovered nutrient recipes remain research. A rack
does not directly consume scrap rejects, raw ammonia, untreated waste or regolith.
NASA's [space-crop discussion](https://www.nasa.gov/podcasts/curious-universe/how-to-grow-plants-in-space/)
identifies water, light, CO2 and mineral nutrients as requirements. New quantities
or chemical simplifications must continue to be labelled authored balance.

## Environment, power and failure behaviour

Candidate cabin operating envelope: 18–26 °C and 70–110 kPa for full-rate growth;
reduced growth outside those bounds while the plant remains viable. These broad
shared game thresholds are not species-specific agronomic findings. Start warnings
immediately outside the operating band; prototype severe exposure below 0 °C,
above 45 °C or below 20 kPa. Test and revise before shipping. Root water, available
CO2, oxygen, actual received electricity and waste-heat capacity also limit growth.
No threshold may erase existing mass or promise instant death without a rate model.

Use bounded simulation-time steps, not frame counts or wall-clock time. A content
service settles physical inputs, respiration, growth and outputs once per step.
Power receipts come from Framework's existing native electrical path; do not call
the source twice or treat requested power as received power. Allocation to plant
growth is bounded by all available inputs. Electricity already spent on lamps and
pumps still becomes heat when growth is resource-limited; unused inputs stay held.
No reusable bank of growth credit accumulates through a prolonged dry or dark spell.

At 50% continuous supply the ideal potato schedule becomes 192 hours for the same
72 kWh, before stress and standby losses. Zero supply yields zero photosynthetic
growth, while respiration and deterioration continue. The offline calculator's
proportional schedule is an upper bound; it does not prove that a real crop stays
healthy at that supply or survives an arbitrarily long delay.

Separate **machine automation state** from **biological condition**. Pausing lamps,
pumps or processing does not freeze living tissue. Progressive states are healthy,
stressed, wilted and dead. First prototype stress grace candidates are two game
hours without irrigation or adequate circulation, with warnings before yield
loss; severe thermal/decompression exposure can act sooner. Exact damage and
recovery rates are an explicit implementation tuning task, not simulated results
from the ideal calculator. Healthy restoration can recover a wilted crop, but
must not reset previously lost biomass or regrow already collected produce.

Dead plants become retained waste with their remaining mass and water. Failed
harvest portions, spilled fluids and rotten products need explicit destinations.
Full drawers stop output placement; produce remains attached/held and can continue
to deteriorate. A blocked harvest does not spill free food onto an adjacent ship.

Use built-in temperature, pressure, reservoir-level and lamp/pump status probes
where a credible native source exists. CO2/O2 readings require local species
sampling and validity; pH, conductivity and microbial assays need future actual
models/instruments. Missing or stale readings show unavailable, not zero or a
fictional exact value. Probe failure does not remove physical heat or gas.
Native ship IR is not a leaf thermometer; room brightness is not lamp electricity.

## Framework and optional integrations

Reuse current Framework registration, construction, maintenance, market stock,
localization, ObjectStateStore, native electrical receipts, gas/thermal primitives,
observations and physical-item transfer helpers. [Framework's author guide](framework-author-guide.md)
documents the implemented API. The following are **two proposed extensions**,
introduced with this actual consumer rather than a separate speculative subsystem.

### Equipment-provider contract

Add a small Framework-owned registration boundary, tentatively
`Controls.IEquipmentProvider`, with the following responsibilities:

- A stable provider ID and supported native definition IDs, with duplicate
  ownership rejected and clean unregistration when a provider is unavailable.
- Discovery constrained to the requesting console's authorized host ship and
  full object IDs. Docked neighbours are not automatically authorized.
- Immutable read-only equipment snapshots: identity, activity, available
  observations with source/time/validity, attention reason and supported commands.
- Checked command dispatch with stable action IDs, supplied arguments and freshly
  resolved actor, target and ship context. Return a structured result/reason;
  never interpret translated status text as permission.

Shipbreaker's C1 console consumes this registry while its provider retains current
equipment semantics. Agriculture registers its rack/cooker and delegates to the
same service used by local panels and F3. Framework owns shared access checking
and discovery, not crop thresholds, recipe balance or equipment artwork.
Physical loading, planting, harvesting and repairs remain local crew actions;
remote access may select policy, pause/resume automation or view observations.

This requires changing the current Shipbreaker-specific console discovery and
dispatch; merely adding a rack class will not expose it. Shipbreaker remains an
optional consumer, with no required Agriculture-to-Shipbreaker assembly reference.
Without it, local controls and F3 remain complete. Preserve existing console
entries, industrial interlocks, native instruments and fixed-batch contracts.

### Finite liquids and Ship's Water adapter

Framework should own a narrow finite transfer contract: resolve compatible source
and destination, inspect quantity/capacity read-only, check reserve/permissions,
commit a bounded transfer on the main thread, then return measured source and
destination deltas. Identify commodity/quality and units explicitly. Start with
water; do not imply a general multi-species chemical network already exists.

Use kilograms internally, with the scoped native water assumption 1 L = 1 kg for
the inspected water definitions; do not apply that density to arbitrary fluids.
Plans are not reservations or receipts. Recheck source quantity, target headroom,
same-ship ownership and crew reserve at commit. Never credit the requested amount
when less was debited. A failed/partial transfer retains documented ownership of
every remainder and blocks retry until reconciled. No claim of crash-atomic
cross-provider transactions is established by this design.

Manual fallback consumes selected native 0.25 L LiquidWater units into the finite
process reservoir, or leaves them untouched when headroom is insufficient. Do
not delete other container contents. Existing solid helpers may move the packet
but are not liquid accounting. A later fractional packet path needs its own
verified native remainder support rather than rounding water into existence.

The optional Ship's Water adapter detects the loaded provider plus compatible
installed potable-tank definitions. The inspected conditions are `StatLiqH2O`
(litres) and `TIsWaterVesselInstalled`; isolate those native assumptions in one
adapter, test against the installed version, and fail closed if incompatible.
No stable public provider API was verified. Prefer cooperation with the author
over broad private-method patches; do not ship or silently fork its binaries.
Valtora's [Ship's Water documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
describes finite vessels and approximately 60% default wastewater recovery, not
agricultural drainage acceptance or nutrient recovery.

Candidate reserve is 10 L per configured crew member, minimum one; this is a
visible game preference, not a real drinking-water recommendation. Read configured
crew count explicitly if reliable native occupancy is unavailable. Reserve is
checked across eligible same-ship potable stores at each debit, including when
crew or another machine drew water since the preview. Manual carried-water supply
remains possible. Keep receiving permission separate from grow automation.
Do not return agricultural water, fertilizer or contaminated drainage to potable
tanks; capture it locally until a separate compatible treatment route exists.

| Other link | First-slice contract |
| --- | --- |
| Shipbreaker | Optional console provider and suitable physical construction materials; no changed 13 kg residue or terminal-reject meaning |
| Asteroid processing | Future purified water and characterized nutrient ingredients; no new mining system or raw-ore-to-food recipe |
| Hauling/trading mods | Ordinary physical stock/food with stable IDs and native inventories; verify provider compatibility rather than invent special menus |
| Auto Nav | Independent flight authority; agriculture reacts to received power and evidenced flight conditions, never commands navigation |

## Persistence and controls

Agriculture owns a versioned ObjectStateStore schema on each rack. Capture cohort
ID, crop/profile revision, chosen propagation mode, duration and resource settings,
last settled simulation time, stage/progress, health/stress, seed identity, biomass
and water/nutrient ledgers, reservoir contents, outstanding harvest quantities,
placed output IDs and automation/receiving preferences. Save physical contents
through native inventories; avoid a second saved copy that can be collected too.

Keep bounded fields within the current store's 512-character value rule and safe
character restrictions; use invariant numeric serialization. Unknown schemas,
different owners and malformed data remain intact with an actionable stopped
state. Never reset a crop to healthy/default to repair a failed load. Captured
cohort balance survives configuration edits; new settings apply next planting.

Restore machinery paused for explicit resume, without resetting biological state.
While the ship is actively simulated, living tissue still consumes/loses resources
according to its passive step. Time with the game closed grants neither growth
nor new damage. Unloaded-ship handling must follow verified native update/save
behaviour; until supported, preserve the last settled state without retrospective
growth, harvesting or invented exposure. Document that abstraction in player help.
No UI read advances time or writes state.

Harvest is a staged physical conversion with one cohort/harvest identity: reserve
eligible biomass, place only outputs that fit, record exact placed quantities/IDs,
then reduce remaining claimable material. Reuse Framework BatchPlacement and
existing guarded native transfer patterns where applicable. Interruption/reload
reconciles partial placement before continuing; a second crew or command cannot
mint the reserved outputs again. Native persistence ordering still needs runtime
verification; do not claim perfect transactional recovery without evidence.

Local Control Panel and F3 share one service. Show crop/mode, stage and progress,
condition, water/nutrients, measured supply, cabin observation source/validity,
harvest space and the next useful crew action. Explain unknown completion time
under shortages rather than presenting a guaranteed countdown. Keep labels live
and localized, use Framework equipment-name patterns, and reuse native controls.
Proposed F3 actions are status, inspect, set crop, set reserve, start, pause and
resume; no debugging command bypasses supplies, physical work or access checks.

## Plant artwork and PixelLab pilot

Follow the [shared asset-generation policy](asset-generation-policy.md) and
[resolution policy](artwork-resolution-policy.md). PixelLab is the owner's preferred
plant provider, subject to a small quality/cost pilot. Its
[consistent-style documentation](https://www.pixellab.ai/docs/tools/consistent-style)
and [image editing documentation](https://www.pixellab.ai/docs/tools/edit-image-pro)
support reference-driven families; they do not guarantee usable stages.

1. Pilot one potato family: sprout, young, mature, harvest-ready, wilted and dead.
   Produce and inspect the first useful plant before commissioning the full set.
2. Keep tray/rack layers independent of plants. Use a fixed orthographic overhead
   view, canvas, root anchor, silhouette margins, palette and logical pixel grid.
   Reserve space for the largest canopy and preserve colour/normal/damage alignment.
3. Candidate rack export is 64 × 64 with at least a 128 × 128 master; a 16 × 16
   plant layer needs at least 64 × 64, and a 32 × 32 cooker needs at least 128 × 128.
   Confirm actual plant bounds before production. Integer nearest-neighbour
   export preserves the native footprint; do not ship a larger world PNG at the
   old rendering scale or call simple enlargement new detail.
4. Reuse project-owned references and edits for subsequent stages. Start with the
   least expensive suitable single-image route; static plants need no eight-view
   directional batch. Escalate only for an identified visual failure.
5. Inspect native-size and enlarged previews for identifiable stages, transparent
   edges, registration, restrained shading and coherent perspective. Check in-game
   lighting before expanding to lettuce and its later seed-head states.

The [published PixelLab API examples](https://www.pixellab.ai/pixellab-api) are
illustrative prices, not an approved production budget. Track quoted and actual
charges, retries and cleanup, and calculate **total cost divided by accepted usable
sprites**. Record crop/stage, prompt, seed when available, reference provenance and
hashes, operation/model, generation ID, original output dimensions and export
settings. Preserve untouched masters and provider/licensing terms. Game-derived
research stays ignored and local; it is not an authorized generation reference.
**This research round initiates no paid generation.**

## Validation and owner gameplay scenarios

Only the offline design calculator is executable in this round. Its six tests
check mass/energy balances, recovery, power/pace limits, propagation, crew scaling
and invalid assumptions. Run the commands in the [research report](agriculture-research.md#verification-completed-and-limits).
The following runtime acceptance work remains future implementation work.

| Scenario | Required result and evidence |
| --- | --- |
| One person and three-person crew | Measure native food-debt changes, edible portions, tending burden and startup stores; compare against the ideal budget, not calories |
| Full, half, intermittent and zero power | Only measured receipt funds work; account heat once; growth slows/stops while passive deterioration continues |
| Fast-forward, pause, closed panel | Same settled resources for equal simulated exposure; no frame-rate advantage or UI-dependent progress |
| Dry reservoir, missing nutrients, depleted CO2 | Useful early warning; finite growth ceiling; no negative stocks; recoverable stress before appropriate loss |
| Warm cabin, lost atmosphere, failed sensor | Physical effects persist; missing measurement is unavailable; no fictitious vacuum cooling or fabricated probe result |
| Harvest drawer/residue buffer full | Output remains accounted at source; placement resumes once space exists; no duplicate or vanished food |
| Interrupted harvest, two crew, reload mid-placement | Same cohort and exact output claims survive; already placed items cannot be collected again |
| Reload healthy/stressed/dead crops | Identity, contents, health, captured settings and staged outputs persist; automation needs explicit resume; no free catch-up |
| Setting change, old/future/corrupt schema | Existing cohort contract stays fixed; unsupported state remains preserved and stopped with diagnostics |
| Provider absent, disabled or incompatible | Manual water/local controls work; no missing-provider crash or conversion of orphaned stocks; existing saves are protected |
| Crew draws water during irrigation | Rechecked reserve wins; only measured finite debit reaches the rack |
| Docked ship and moved equipment | No neighbour-tank theft, remote harvest or command permission inherited through docking; source/target ship checked afresh |
| AI eating, cooking interrupted, seed reserved | Crop-specific native food effects once; no free cooked output; propagation stock remains available until deliberately consumed |
| Lettuce seed mode in later increment | Seed production costs a distinct cycle; sustainable sowings match actual retained packs and losses |

Owner-run gameplay should begin with ordinary acquisition, one planted rack, a
successful harvest/preparation, then brief recoverable power/water interruptions.
The owner chooses conditions for destructive stress tests. Record the exact game,
Framework and provider versions and distinguish these observations from build,
definition, calculator and simulation checks. Do not edit saves to manufacture
successful results or claim this specification has passed those scenarios.
