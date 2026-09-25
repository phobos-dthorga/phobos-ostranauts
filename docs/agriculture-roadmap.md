# Phobos Agriculture: endurance roadmap

**25 September 2026.** Round 1 now has an [implementation candidate](agriculture-implementation.md);
owner gameplay acceptance remains pending. The owner requested further feature
work before gameplay testing on 25 September: the current 0.2.0 candidate adds
[living rack visuals and lettuce art](agriculture-living-visuals.md). Gameplay
acceptance below remains an evidence milestone, not a gate on that authorized work.
Later biological rounds remain research/design. This sequence is based on useful
playable results, not calendar estimates or a promise to implement every branch.
The [research report](agriculture-research.md) records evidence; the
[first-slice specification](agriculture-first-slice.md) defines the candidate rack,
crop budgets, Framework work and acceptance scenarios.

## Destination

Extend time away from stations by growing useful food and recovering resources,
while retaining maintenance, power, heat rejection and replenishment of losses.
ESA's [MELiSSA closed-loop concept](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Closed_Loop_Concept)
uses multiple biological and physical processes for recovery. It supports connected
treatment and cultivation, not a claim that one planter closes a ship's life
support or that indefinite habitation requires no incoming matter.

NASA researcher R. M. Wheeler's [bioregenerative life-support overview](https://ntrs.nasa.gov/citations/20205008786)
gives an indicative approximately 50 m² of crops per person's food energy and
20–25 m² for oxygen, depending on conditions. Continue to make occupied space,
lighting and heat meaningful even when deliberately compressing them for gameplay.
Never equate our food-debt units or game tiles with scientific calories or canopy
area without a separately stated conversion and justification.

## Round 1: useful staple and vegetable harvests

Deliver one Framework-dependent Agriculture package, a cultivation rack and a
minimal potato preparation route. Potatoes and lettuce have distinct contributions
and crop-specific native food effects. Start with purchased viable stock and
formulated nutrients, finite carried water and optional Ship's Water supply.

Introduce the two concrete Framework extensions with their first consumers:
equipment-provider discovery/snapshots/checked commands, and finite water transfer
with a narrow optional provider adapter. Local operation must remain useful without
Shipbreaker, Ship's Water or OCF. Keep crop lifecycles outside fixed industrial jobs.

Complete the potato PixelLab pilot before expanding art production. Use the
[shared workflow](asset-generation-policy.md), independent plant/equipment layers
and cost per usable sprite. This document authorizes no generation charges.

Advance when the owner can acquire, plant, tend, harvest and eat a crop in ordinary
play, with useful warnings and no resource/persistence violations in the specified
checks. Compare actual food coverage, space occupied and crew effort against the
ideal budget: one potato rack plus one lettuce rack targets about 60% of one
unmodified crew member's native food-debt demand. The number is authored balance,
not a nutrition claim or a passing gameplay test.

If tending or waiting offers little value, simplify or retune this loop before
adding species. If native food effects or gas transfers cannot be made reliable,
reduce the documented scope rather than conceal a free-food or free-oxygen path.

## Round 2: propagation and improved water recovery

Make seed potatoes a visible reservation from food yield and introduce a distinct
lettuce seed-production cycle. Demonstrate multiple generations, finite seed stock
and the consequences of eating or losing the reserve. One ideal seed run produces
four packets; retaining one supports the next seed run and three food sowings.
The separate seed lifecycle costs growing space and time, as well as resources.

Investigate actual crop condensate, irrigation purge and compatible recovery.
Keep process liquid separate from potable water. Valtora's
[Ship's Water documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
describes approximately 60% default wastewater recovery with filters; it does
not certify nutrient-bearing crop drainage or imply nutrient recovery. Preserve
upstream station services, crew reserves and provider-owned state.

NASA's [Station Science 101 plant research](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/)
discusses water/air delivery and successive plant generations. Cite those findings
when explaining design choices; identify our reservoir capacities, accelerated
cycles, propagation ratios and recovery efficiencies as authored assumptions.

Advance after repeated food/seed rotations preserve mass and viable stock, and
measured water makeup improves without hiding water in unlimited reservoirs.
If a recovery route adds more machinery than useful decisions, retain purchased
water and the existing provider integration until a better need appears.

## Round 3: characterized nutrients and asteroid replenishment

Define complete nutrient requirements before implementing extraction. Characterize
organic residues, contaminated water and prospective feeds; account for nutrient
availability, impurities, counter-ions, finite storage and terminal waste. Treatment
may require consumables or reject unsafe inputs. Neither waste mass nor a matching
element name establishes that a plant can use it.

Connect to the [asteroid life-support research](asteroid-life-support-research.md):
native tethered mining supplies water ice/hydrates first; proposed nitrogen-bearing
and phosphate/salt-bearing feeds follow only with proven useful processes.
[NASA's Bennu findings](https://www.nasa.gov/news-release/nasas-asteroid-bennu-sample-reveals-mix-of-lifes-ingredients/)
support researching ammonia and relevant salts, not any proposed extraction yield
or universal fertilizer ore. The same recovered nitrogen cannot be credited both
to cabin gas and nutrient production.

Use Framework for concrete storage, transfers, measured inputs and observations.
Agriculture owns crop nutrient requirements; the actual processing content mod
owns extraction chemistry, hardware and recipes. Preserve Shipbreaker's legacy
13 kg residue, revision-bound jobs and terminal rejects. Agriculture is not a
reason to reinterpret existing saved cargo or add a second mining system.

Advance only when a complete, mass-balanced route demonstrably replaces a purchased
input and leaves honest losses/remainders. Keep a short replenishment ledger:
water, nutrient elements, carbon/gases, seeds, filters/media, spare parts and fuel.
An unclosed category remains a station/salvage/mining dependency. Do not claim
indefinite operation from food production alone.

## Round 4: additional biological systems with distinct value

Evaluate new crops only for a different player decision: harvest pattern, processing,
storage, crew preference, propagation, energy/space trade-off or nutrient use.
NASA's [deep-space crop discussion](https://www.nasa.gov/science-research/nasa-plant-researchers-explore-question-of-deep-space-food-crops/)
provides further crop candidates but does not obligate a large catalogue.

ESA's [MELiSSA photoautotrophic compartment research](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Compartment_IV_The_photoautotophic_compartment)
distinguishes higher plants and Arthrospira cultivation. A later Arthrospira
bioreactor should therefore have its own contained liquid, illumination, gas
exchange, harvest processing and contamination questions. It is not a cheap
lettuce reskin or automatic proof of an adequate human diet.

Fungi would consume organic substrate and oxygen rather than serve as a
photosynthetic oxygen source. Research a particular species and useful substrate
route before proposing yields or edible products; no fungal process is verified
or selected here. Put an unconvincing branch aside when its maintenance or content
cost exceeds its gameplay value.

## Integration and evidence milestones

| Boundary | Evidence needed before claiming it works |
| --- | --- |
| Agriculture → Framework | Actual versioned dependency, saved-state migration, physical mass and measured receipts; shared services remain usable without Agriculture |
| Agriculture → industrial console | Shared provider registration, read-only snapshots, fresh command checks and docked-ship isolation; local actions remain available |
| Agriculture → Ship's Water | Checked finite same-ship debits, protected crew reserve, manual fallback and absent/incompatible-provider tests |
| Agriculture → Shipbreaker | Existing physical materials used through unchanged contracts; optional console integration, no mandatory recycler chain |
| Agriculture → future resource processing | Verified purified product identity and quality, complete transfer/remainder accounting, useful consumer |
| Agriculture alongside Auto Nav | No flight command authority; actual supply/conditions drive crop effects; navigation can continue independently |
| Artwork → release | Accepted native-scale family, registered layers, masters/export records, provider terms and documented cost per usable result |

Build and offline checks establish only their tested properties. Record owner-run
gameplay separately, with game/mod versions, scenario and observed outcome.
Repair citations when the related research changes. Keep NASA, ESA, original
researchers, game documentation and mod authors explicitly named beside relevant
claims, and carry applicable references into design, player and release documents.
Artwork and code provenance remain separate records with their own terms.
