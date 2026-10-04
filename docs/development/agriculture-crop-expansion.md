# Agriculture crop expansion: phases, sources and open decisions

Owner direction, 4 October 2026: more food crops, industrial crops and ties between
Agriculture and the other Phobos mods, with the data pack (schema) files used more
widely. This record supersedes Round 4 of the
[endurance roadmap](agriculture-roadmap.md) for crops; the roadmap's cautions about
fungi and about claiming a complete diet still stand.

Nothing here is gameplay-validated. Source findings, our derivations and authored
balance are labelled separately throughout. Citing a research group does not imply
that it endorses or has validated this mod.

## Owner choices

| Area | Chosen |
| --- | --- |
| Food crops, first wave | Dwarf wheat, dwarf tomato, soybean |
| Industrial chains | Biomass to carbon; sugar beet with a fermenter; flax; rubber dandelion |
| Cross-mod ties | CO2 raises yield; transpiration water; spirulina bioreactor |
| Schema | Crops and cooker recipes become data packs before any new crop |

Chili, radish and activated char were offered and not chosen.

## Phases

| Phase | Release | What it delivers | State |
| --- | --- | --- | --- |
| 0 | this record | Sources, per-crop decisions, open questions | Written |
| 1 | Agriculture 0.40.0 | Crops and Hearth-2 recipes as data packs; no change in play | Built, offline checks only |
| 2 | Agriculture 0.41.0 | Wheat, tomato (repeat picking), soybean; their foods and artwork | Planned |
| 3 | Agriculture 0.42.0, Manufacturing 0.37.0 | CO2 response; residue charred to carbon stock in the V4; rack vapour overflow to a linked water tank | Planned |
| 4 | later | Flax: fibre to cloth, linseed; oil press decision | Needs its own design record |
| 5 | later | Sugar beet and a fermenter: ethanol and CO2 | Needs its own design record |
| 5b | later | Rubber dandelion: latex to seals | Needs an owner decision on repair supplies |
| 6 | later | Spirulina bioreactor, a separate machine | Needs its own design record |

Phases 4 to 6 add machines, brands, commodities and prices, so each opens with a
design record and owner decisions before any code.

## Phase 1: what was built

- `mods/PhobosAgriculture/framework/crops.json` (schema `crops`, owned by
  Agriculture: `PhobosAgriculture.Core.CropPack`). One entry per crop, keyed by the
  name a saved planting stores, plus the crop items and their food values.
- `mods/PhobosAgriculture/framework/process-recipes.json`: the Hearth-2's recipes in
  Framework's existing recipe schema (machine `hearth`).
- Both are frozen by hash (`frozen-crops.json`, `frozen-process-recipes.json`). A
  saved planting stores only its crop name, and a portion in the cooker is cooked by
  the recipe that names its item, so a published entry never changes; a change adds
  an entry beside it.
- Rules on every file, shipped or player: mass closes
  (water + nutrient + 0.4 x carbon - vapour = final - seed), the rack's limits hold,
  items are the mod's own with matching unit masses, feed and conduit names are
  unique, artwork is a shipped family.
- Proof: the item reference export changed only in source hashes and versions;
  saved-record fixtures for the three plantings and a half-cooked potato load
  unchanged; the pack's figures equal the old constants bit for bit.

Known limits of phase 1:

- A player file can add a crop but not an item or artwork, so an added crop reuses
  the mod's items and a shipped crop's growth stages.
- A cooker recipe is one item in, one item out. Phase 2's bread and soybean meal
  need water as a second input; that is a phase 2 schema extension.
- Supersession (a new crop replacing an old one for new plantings, as recipes have)
  is not built. Phase 3's CO2 response needs it, or new crop names.

## Sources

Three NASA Kennedy Space Center documents carry most of the figures:

| Tag | Document | Link |
| --- | --- | --- |
| TM-2003 | Wheeler, Sager, Prince, Knott, Mackowiak, Stutte, Yorio, Ruffe, Peterson, Goins, Hinkle, Berry (NASA Kennedy Space Center and Dynamac), 2003. *Crop Production for Advanced Life Support Systems: Observations From the Kennedy Space Center Breadboard Project.* NASA/TM-2003-211184 | [NASA NTRS record 20030032422](https://ntrs.nasa.gov/citations/20030032422) |
| W2008 | Wheeler, Mackowiak, Stutte, Yorio, Ruffe, Sager, Prince, Knott (NASA Kennedy Space Center, University of Florida, Dynamac), 2008. "Crop productivities and radiation use efficiencies for bioregenerative life support." *Advances in Space Research* 41:706-713 | [DOI 10.1016/j.asr.2007.06.059](https://doi.org/10.1016/j.asr.2007.06.059) |
| W1996 | Wheeler, Mackowiak, Stutte, Sager, Yorio, Ruffe, Fortson, Dreschel, Knott, Corey (NASA Kennedy Space Center), 1996. "NASA's Biomass Production Chamber: a testbed for bioregenerative life support studies." *Advances in Space Research* 18(4/5):215-224 | [NASA NTRS record 20040089951](https://ntrs.nasa.gov/citations/20040089951) (abstract only read) |

All yields below are dry mass per square metre of growing area. "Derived" marks our
arithmetic on a source's figures.

### Wheat

NASA Kennedy Space Center Biomass Production Chamber runs (TM-2003 Tables 5 to 7,
W2008):

| Figure | Source finding |
| --- | --- |
| Cycle | 77 to 86 days to harvest; about 35 days to anthesis |
| Edible yield | 6.7 to 12.6 g per m2 per day; best 11.3 at 67 mol PAR per m2 per day |
| Total biomass | 23 to 40 g per m2 per day |
| Harvest index | 28 to 40% (derived from the tables); W2008 calls 29% low and suggests ethylene |
| Canopy | about 50 cm |
| Water use | 4.7 L per m2 per day (TM-2003 Table 9) |

Dwarf cultivars, Utah State University Crop Physiology Laboratory (NASA-funded):

- USU-Apogee is a full-dwarf hard red spring wheat, 45 to 50 cm tall, released in
  1996, yielding 10 to 30% more than Yecora Rojo and Veery-10: Bugbee and Koerner,
  1997, *Advances in Space Research* 20:1891-1894
  ([abstract](https://europepmc.org/article/MED/11542565)).
- Its heads emerge 23 days after germination, and it was grown on the ISS in 2003:
  [Utah State University Crop Physiology Laboratory, dwarf crops](https://qanr.usu.edu/labs/cpl/research/dwarf-crops).
- Super Dwarf is under 30 cm and ethylene-sensitive (a 60% yield loss at 50 ppb),
  where Apogee was not significantly affected: Klassen and Bugbee, 2002, *Crop
  Science* 42(3) ([NASA NTRS record 20040087496](https://ntrs.nasa.gov/citations/20040087496)).
- A wheat record of 60 g of grain per m2 per day at 150 mol PAR per m2 per day and
  1,200 ppm CO2, harvest index 41 to 44%: Bugbee and Salisbury, 1988, *Plant
  Physiology* 88:869-878 ([NASA NTRS record 20040112090](https://ntrs.nasa.gov/citations/20040112090)).
  The cultivar is not Apogee.

Unverified: Apogee's own days to harvest, grain yield per m2 and harvest index. Its
1997 paper and registration note could not be read in full.

Grain as stored holds 12.8% water (hard red spring wheat,
[USDA FoodData Central 168889](https://fdc.nal.usda.gov/food-details/168889/nutrients)).

### Soybean

Biomass Production Chamber runs, cultivars McCall and Hoyt (TM-2003, W2008):

| Figure | Source finding |
| --- | --- |
| Cycle | 90 or 97 days; about 28 days to flowering |
| Seed yield | 3.4 to 6.0 g per m2 per day |
| Total biomass | 10 to 16 g per m2 per day |
| Harvest index | 32 to 38% (derived); TM-2003 Table 5 says about 40% |
| Canopy | about 45 to 70 cm |
| Water use | 4.7 L per m2 per day |
| Seed composition, dry basis | protein 37.1%, fat 20.0%, ash 7.4% (TM-2003 Table 8) |

Seed as stored: water 8.5%, protein 36.5%, fat 19.9%
([USDA FoodData Central 174270](https://fdc.nal.usda.gov/food-details/174270/nutrients)).
The fat figure bounds what a later oil press may give: about a fifth of the seed.

### Tomato

Biomass Production Chamber runs, cultivar Reimann Philipp 75/59, a cherry type and
not a dwarf (TM-2003, W2008):

| Figure | Source finding |
| --- | --- |
| Cycle | 84 to 91 days; flowering about day 35 |
| Picking | fruit picked as it ripened from day 65 to the final harvest (W2008); about 19 to 26 days of picking (derived) |
| Fruit yield | 6.1 to 9.8 g per m2 per day |
| Total biomass | 13 to 20 g per m2 per day |
| Harvest index | 47 to 50% (derived) |
| Canopy | about 35 to 45 cm |

Ripe tomato is 94.5% water
([USDA FoodData Central 170457](https://fdc.nal.usda.gov/food-details/170457/nutrients)).

The dwarf cultivar Red Robin flew in NASA's VEG-05 experiment on the ISS from
14 December 2022 to 24 March 2023, with harvests planned at days 83, 90 and 100;
water stress cost most of the fruit: Spern, Hummerick, Khodadad, Morales, Dixit,
Spencer, Mitchell, Morrow, Douglas, Wheeler, Massa (NASA Kennedy Space Center and
partners), 2025 ([NASA NTRS record 20240016407](https://ntrs.nasa.gov/citations/20240016407)).
It supports the picking pattern and the cultivar choice, not a yield.

Unverified: Red Robin's height, any per-m2 yield from VEG-05, the chamber's fresh
fruit yield, and tomato water use (TM-2003 Table 9 omits tomato).

### Carbon dioxide response

| Finding | Source |
| --- | --- |
| The chamber ran at 1,000 or 1,200 ppm (0.10 or 0.12 kPa) in the light | TM-2003, W2008 |
| C3 crop photosynthesis saturated between about 1,000 and 1,500 ppm; the compensation point was 50 to 100 ppm | TM-2003 section 2.4.4 |
| Wheat, 350 to 1,200 ppm: vegetative growth +25%, seed yield +15%; at 2,500 ppm seed yield fell 15 to 22% | Grotenhuis and Bugbee (Utah State University), 1997, *Crop Science* 37:1215-1222 ([NASA NTRS record 20040088863](https://ntrs.nasa.gov/citations/20040088863)) |
| Wheat, 350 to 1,000 ppm: seed yield +33%; 1,000 to 10,000 ppm: seed yield -37% | Reuveni and Bugbee, 1997, *Annals of Botany* 80:539-546 ([NASA NTRS record 20040089164](https://ntrs.nasa.gov/citations/20040089164)) |
| Soybean at 500, 1,000, 2,000 and 5,000 ppm: McCall yielded most at 1,000 ppm | Wheeler, Mackowiak, Siegriest, Sager, Knott (NASA Kennedy Space Center), 1993, *Journal of Plant Physiology* 142:173-178 ([NASA NTRS record 20040090155](https://ntrs.nasa.gov/citations/20040090155)) |
| Little advantage above 1,000 to 1,500 ppm; above 5,000 ppm can harm some species | Wheeler and colleagues (NASA Kennedy Space Center), 2024, *Journal of Plant Interactions* 19:2292219 ([DOI](https://doi.org/10.1080/17429145.2023.2292219)) |

Across the wheat studies, enrichment from ambient to about 1,000 to 1,200 ppm gave
+13 to +40% seed yield. That range, and a fall above about 1,200 ppm, is what
phase 3 may model.

### Shared figures

- Carbon is about 45 to 48% of plant dry mass by organ: Ma, He, Tian, Zou, Yan,
  Yang, Zhou, Huang, Shen, Fang (Peking University), 2018, *Biogeosciences*
  15:693-702 ([article](https://bg.copernicus.org/articles/15/693/2018/)). The
  chamber's own crops imply about 40 to 46% (derived from TM-2003 Table 6).
- The chamber's lamps drew about 0.75 kW per m2 at an average 750 micromoles per m2
  per second (W2008).
- Potato and lettuce in the same chamber: 90 to 105 days and 28 to 30 days (W1996).

## How the sources apply, and where they stop

- **A rack is not a square metre.** The Firstlight-4 is an authored 4 x 4 tile
  machine with four trays, and its crops are single cohorts with compressed cycles
  (potato 96 hours against the chamber's 90 to 105 days). Phase 2 keeps that
  compression: the sources set *ratios between crops*, not hours.
- **Ratios phase 2 should keep** (our reading of the tables above):
  - cycle length: wheat and tomato a little shorter than potato, soybean about the
    same as potato
  - light: wheat the hungriest, soybean and tomato moderate
  - harvest index: wheat about 30 to 40%, soybean about 35 to 40%, tomato about 50%,
    against potato's 84% and lettuce's 83% in the current pack; so wheat and soybean
    leave far more residue per portion
  - carbon: 40 to 46% of dry mass
- **Fresh against dry.** The pack's masses are fresh. Grain and beans are dry
  produce (9 to 13% water) and tomatoes are 94.5% water, so one kilogram of tomatoes
  holds far less food than one of grain. Food values should follow dry matter.
- **Picking.** Tomato is picked over roughly the last quarter of its cycle. Phase 2
  models that as several picks from one planting; the number of picks is authored.

## Decisions phase 2 still needs

These are ours to propose in the phase 2 change and the owner's to revise:

1. Authored hours, power and yields for the three crops, in the ratios above.
2. Whether wheat needs milling. The proposal: the Hearth-2 bakes grain and water
   straight into flatbread, with the simplification stated.
3. A second cooker input (water), which extends the recipe rule of one item in.
4. Number of tomato picks and the regrow interval.
5. Names for the new planting stock and foods under Continuance and Hearth.
6. Artwork: six growth stages per crop plus item icons from PixelLab. This spends
   credits, so it starts with one pilot crop for review.

## Later phases: starting notes

- **Biomass to carbon.** Pyrolysis yield and the residue's carbon record decide the
  charge. Crop residue is fresh mass; a char recipe has to account for its water.
- **Fermenter.** Glucose to ethanol and CO2 is 51% ethanol and 49% CO2 by mass
  (stoichiometry). Ethanol becomes a bulk commodity only with named uses.
- **Flax and rubber dandelion.** No source has been read yet; nothing is claimed.
- **Spirulina.** ESA's MELiSSA programme treats Arthrospira as its own compartment;
  see the roadmap.
- **Transpiration.** Racks already condense transpired water back into their own
  reservoir. Only overflow from a full reservoir is lost, so the tie is a link to a
  water tank, not a new machine.
