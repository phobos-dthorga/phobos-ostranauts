# Asteroid resources for long-term life support

Research and owner direction: **2026-09-24**. Extend the ore investigation beyond
metal recovery to replenishing the resources needed to live away from stations.
New asteroid feedstocks are welcome where they fill a real gap. This document
records candidates, not implemented ore, guaranteed deposits or verified adapters.
It complements the [Shipbreaker processing research](shipbreaking-material-processing-research.md).

## Recommended starting point

Begin with **native water ice -> purified water**, then investigate **water ->
oxygen and captured hydrogen**. The game already defines water ice, methane ice,
hydrates and carbon-bearing ore. Reuse these before adding near-duplicates.

The strongest candidates for genuinely new feedstock are **nitrogen-bearing
carbonaceous material** and **phosphate/salt-bearing material**. These would make
asteroid selection matter and eventually help replenish air and crop nutrients.
Do not make every rock a source of every consumable.

The scientific precedent is encouraging: Bennu samples contain ammonia and
nitrogen-rich organic matter, while mineral analysis identified salts including
phosphates. These observations support the presence of relevant ingredients, not
our proposed concentration, extraction efficiency or equipment size.
[Ammonia research paper record](https://ntrs.nasa.gov/citations/20250001355),
[NASA's mineral findings](https://www.nasa.gov/news-release/nasas-asteroid-bennu-sample-reveals-mix-of-lifes-ingredients/).

## Resource map

All new flows below are proposals. Native object IDs and masses were checked in
the installed game; see the companion report's source ledger.

| Ship need | Existing or proposed asteroid feed | Processing and useful output | Important limit |
| --- | --- | --- | --- |
| Drinking, hygiene and process water | Native `ItmIce01` water ice, 24.7 kg; later `ItmMineral11` hydrates, 10 kg | Contained thaw/extraction, impurity separation, purification, clean-water storage | Ice mass is not a certified clean-water yield; hydrates have no defined water fraction. |
| Breathing oxygen | Purified recovered water | Electrolysis, drying/purity control and compression into compatible storage | Hydrogen must be captured or explicitly discharged; full storage must pause work. |
| Nitrogen replenishment | Proposed nitrogen-bearing carbonaceous feed with a declared ammonia/ammonium-bearing fraction | Extraction, chemical treatment as required, ammonia separation/decomposition, gas purification and separation -> nitrogen plus hydrogen | Raw ammonia-bearing gas is not breathable nitrogen. Nitrogen diverted to another use is unavailable for air replenishment. |
| Carbon/hydrogen chemistry | Native carbon/carbides ore and `ItmIce02` methane ice, 24.84 kg | Feed for selected later chemical processes | Carbon ore is not automatically clean carbon, food or activated filter media; methane ice is not drinking-water ice. |
| Phosphorus and selected mineral nutrients | Proposed phosphate/salt-bearing carbonaceous material | Extraction, purification and formulation of specific nutrient salts | Raw salty regolith is not fertiliser. Salt composition and contamination must be declared. |
| Other minerals for crop systems | Suitable characterised native olivine/silicate feeds, supplemented by salts only where justified | Particular soluble nutrient compounds | Magnesium, potassium, calcium, sulfur and trace elements are separate requirements, not a generic “mineral” bonus. |
| Replacement hardware | Meteoric iron and suitable salvage streams | Sorting/refining and verified repair/fabrication consumers | Metals alone cannot replace membranes, seals, catalysts, electronics or all medicines. |

NASA's Bennu briefing identifies magnesium-sodium phosphate, and its release
describes evaporite minerals including halite and sylvite. This is a basis for
researching phosphate and selected salts, **not assuming a uniform ready-made NPK
fertiliser deposit**. [Bennu mineral evidence](https://svs.gsfc.nasa.gov/14772),
[NASA findings](https://www.nasa.gov/news-release/nasas-asteroid-bennu-sample-reveals-mix-of-lifes-ingredients/).

## New feedstocks: keep the first expansion small

### Nitrogen-bearing carbonaceous material

Proposed first new resource family. Place it in selected compatible carbonaceous
asteroid deposits and describe it as a mineral/organic mixture. Avoid depicting
all warm exposed asteroids as reservoirs of pure ammonia ice or free nitrogen gas.

The initial recipe model should identify an extractable nitrogen carrier, its
fraction of the feed, the processing reagents and the retained matrix. Ammonium
salts and organic nitrogen are not equivalent to free ammonia; the extraction
step must account for that difference. A first implementation should choose one
specific route rather than pretending to process all nitrogen compounds.

Ammonia decomposition provides a plausible downstream branch: `2 NH3 -> N2 +
3 H2`, followed by separation and removal of unreacted ammonia. This is industrial
process precedent, not an already available game appliance.
[DOE-hosted ammonia cracking review](https://www.energy.gov/documents/ammonia-cracking-ready-go).

Recovered nitrogen could replenish cabin buffer gas and, through an independently
verified handoff, the game's nitrogen-consuming equipment. Recovery must not credit
the same nitrogen both to gas storage and to nutrient production. Preserve a
future choice between gas replenishment and a suitable nitrogen nutrient stream.

### Phosphate/salt-bearing material

Second new family, introduced only with a useful consumer. A salt-rich fraction
from an aqueously altered carbonaceous parent body is a defensible inspiration.
It should yield particular separated salts and contaminated/insoluble remainder,
not pure phosphorus, potassium and every trace nutrient on demand.

Start with one declared phosphate-bearing mixture. Split further salt subtypes
only if prospecting or processing choices justify the extra objects. Until a food
production or other maintenance consumer exists, keep this family documented
rather than adding unsellable inventory clutter.

Names above are design labels, not allocated saved-game IDs. Before release,
assign stable Phobos-prefixed IDs, full mass/composition definitions and deliberate
spawn integration. Do not overwrite existing ore identities or silently change
native hydrate/carbon batches into richer material.

## Water and oxygen: the first useful chain

Native `ItmIce01` already describes water ice as requiring industrial processing.
`ItmIce02` shares the broad `IsIce` trait but is methane. Native damage can turn
either into the same ice-gangue item. Therefore the first water recipe should
accept **the exact water-ice identity**, not generic ice, ice gangue or `IsMineral`.

An enclosed extractor/purifier can initially combine thawing, contaminant
separation and conditioning into one machine. Dirty condensate, brine and retained
solids remain accounted for. Separate tanks/cassettes and full-output pauses are
required; do not silently discard contamination to make room for clean product.
Hydrates can be added under a separate characterised recipe later, without
claiming that heating all hydrous minerals releases an identical amount of water.

Ship's Water already has tanks, under-deck plumbing and powered wastewater
reclamation. Reuse that economy through an optional **clean product-water** adapter
instead of duplicating crew plumbing. Its documented wastewater machine is not
evidence that it accepts mineral condensate; no public product-injection API has
been verified. If the adapter is unavailable, a safe design is retaining material
in our own finite process buffer and pausing delivery. Such a buffer is not yet a
usable drinking-water item. [Ship's Water author description](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189).

Water electrolysis is an established life-support oxygen source. The ISS also
uses recovered hydrogen with captured crew CO2 to make water and methane through
the Sabatier process. That makes a later CO2 recovery module worth researching,
with hydrogen/methane accounting and real captured CO2 rather than a free resource
counter. [NASA ECLSS description](https://www.nasa.gov/reference/environmental-control-and-life-support-systems-eclss/).

Theoretical checks using rounded atomic masses, **not game recipe yields**:

| Reaction | Approximate mass accounting |
| --- | --- |
| `2 H2O -> 2 H2 + O2` | 36 mass units water -> 4 hydrogen + 32 oxygen |
| `2 NH3 -> N2 + 3 H2` | 34 ammonia -> 28 nitrogen + 6 hydrogen |
| `CO2 + 4 H2 -> CH4 + 2 H2O` | 44 carbon dioxide + 8 hydrogen -> 16 methane + 36 water |

Apply these limits to the purified constituent fraction, never to the entire ore
mass. Retained reactants, contaminants and all output streams still need a balance.
Electrolysis diverts water from drinking and hygiene; settings should allow a
minimum clean-water reserve and destination priority. Compression and purification
consume energy too. Hydrogen is not automatically a substitute for the game's
fusion reactants, and this chain does not replenish helium-3.

## Nutrients support a later biological loop

Minerals would replenish losses from a crop/algae system, alongside reclaimed
water and suitable recycled nutrients. They do not directly become meals.
Cultivation also needs carbon, energy/light, water, other nutrients, time and
appropriate biological stock. NASA's space-crop work identifies water, light,
CO2 and nutrients such as nitrogen, potassium and phosphorus as inputs.
[NASA space-growing discussion](https://www.nasa.gov/podcasts/curious-universe/how-to-grow-plants-in-space/).

Do not add crop machinery as a prerequisite to the first water machine. Refresh
the mod inventory for actual food-growing systems when that branch becomes
relevant; this round did not verify a compatible food-production provider.
Keep process water and crop nutrient solutions separate from drinking water.
Only promise useful nutrient products once their quality requirements and consumer
are known. Wastewater reclamation alone does not establish nutrient recovery.

## Acquisition, persistence and scope

- All new feedstocks should enter through suitable **finite native asteroid
  deposits and mining**, reached through the owner's tethering workflow. No
  station-purchase assumption, passive space scoop or proximity-based generation.
- Native S/C/M loot families provide a starting point for locating hooks, not a
  finished extension API. Add new material through a narrow, compatible extension;
  avoid replacing every ore table. Determine how new ore replaces part of a
  mining yield, rather than granting unaccounted bonus output on every hit.
- Do not rewrite already generated asteroids or saves. Define behaviour for new
  deposits, existing deposits and depleted deposits before implementation. Loading,
  retethering or switching a setting must not be advertised as a replenishment
  mechanism or accidentally reroll Phobos yields. Native world-generation
  persistence still needs targeted inspection at that implementation step.
- Extraction settings may control availability/throughput within bounds. Recipe
  composition and saved batch meaning must be versioned and stable. First use can
  rely on fixed declared feed families rather than a universal assay simulation.
- Offer destination priorities, reserve levels and pause/resume controls through
  both UI and F3, using the same services. Invalid gas identity, full tanks or a
  missing optional adapter must leave matter retained and explain the pause.
- Our framework remains independent of OCF. Put reusable transport, accounting
  and later proven fluid-adapter mechanisms there; ore content and chemistry
  belong in content modules. Preserve provider-owned inventories when a dependency
  becomes unavailable; do not conjure replacement stocks.

## Development order and remaining evidence

1. Keep the current Shipbreaker/Framework and first transport work coherent.
2. Specify a complete native water-ice batch and verified clean-water destination.
   This is the next endurance research-to-implementation candidate.
3. Add oxygen generation only with compatible storage, hydrogen disposition and
   an explicit heat/power budget. Investigate the native gas API and optional pump
   interfaces at this step rather than inventing them now.
4. Add one nitrogen-bearing feed and its matching processing route when nitrogen
   replenishment can be tested. Establish composition and native deposit integration.
5. Add phosphate/salt feeds together with a real nutrient consumer. Consider
   mineral oxygen extraction or more complete carbon recycling later.

The target is **continued operation through recycling, repair and fresh asteroid
inputs**, not a sealed ship creating matter forever. Lost water/gas, waste,
consumables and damaged hardware remain real costs. This direction gives fusion
power useful work while preserving reasons to prospect, mine and maintain the ship.

No new game definitions, runtime adapters, installations or gameplay tests were
made during this research. No additional owner involvement is needed yet.
