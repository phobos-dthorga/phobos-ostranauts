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
| 2 | Agriculture 0.41.0, 0.42.0 | Wheat (pilot), then tomato with repeat picking and soybean | Built, offline checks only; the owner approved the wheat pilot on 4 October 2026 |
| 3 | Agriculture 0.43.0 and 0.44.0, Manufacturing 0.37.0 | CO2 response, rack vapour overflow to a linked tank, the B2 straw press and both V4 straw charges (offline checks only) | Built |
| 4 | Agriculture 0.45.0 | Fibre flax: straw bundles scutched at the B2 into the game's clean scrap cloth, shives to the straw press; linseed only as planting stock; no oil press yet (offline checks only) | Built |
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

## Phase 2: the wheat pilot (Agriculture 0.41.0)

Owner, 4 October 2026: go, with wheat as the pilot crop and the proposed flatbread.

| Figure | Wheat | Potato, for scale | Basis |
| --- | --- | --- | --- |
| Hours | 84 | 96 | Chamber cycles 77 to 86 days against 90 to 105 |
| Rack power | 1.2 kW | 0.75 kW | Wheat's best yields came at 67 mol PAR per m2 per day against potato's 42 |
| Seed | 50 g packet | 0.2 kg tuber | Our choice; one packet kept back each harvest |
| Final mass | 1.4 kg | 5 kg | Ripe wheat is mostly dry matter; potato is mostly water |
| Carbon (as CH2O) | 1.1 kg | 0.84 kg | Chamber total biomass per cycle about 1.3 times potato's (derived) |
| Nutrient | 45 g | 40 g | Chamber nutrient use per cycle about 1.1 times potato's (derived from TM-2003 Table 9) |
| Edible | 0.46 kg (33%) | 4.2 kg (84%) | Chamber edible dry mass per cycle about half potato's (derived) |
| Harvest | one 0.4 kg grain portion, the packet back, 0.95 kg straw | ten portions, a seed potato, 0.8 kg | Whole portions; the remainder is residue |

The Hearth-2 bakes one 0.4 kg grain portion and one 0.25 kg water ration into a
0.65 kg flatbread over ten minutes at 2 kW: about 62% water to grain, an ordinary
dough. Milling is folded into the step (owner decision: no mill). Baking moisture
loss is not modelled because the game's air has no water vapour to receive it, so
the loaf keeps the water's mass. Its food values are the game's own prepared meal
(nine and five). A loaf from bought water costs more than it fetches: bread is
supply, not trade.

The cooker recipe schema now allows one supply beside the bound portion; the supply
is used when the cooking finishes, and the cooker stops and waits if it is gone.

Artwork: 17 PixelLab generations from the subscription, 9 selected and 8 rejected,
recorded in `assets/phobos-agriculture/wheat-generation-records.json`. Rejected
outputs are on the archive branch. The selected mature plant has broader leaves
than real wheat, a stylisation for readability at 16 pixels that the owner may
revise.

Loot: fridge and crate finds keep their old totals (0.22 and 0.30 per roll); the
wheat items share them, so potato and lettuce items are slightly rarer than before.

## Phase 2: tomato and soybean (Agriculture 0.42.0)

Owner, 4 October 2026: the wheat looks good; continue with the rest.

| Figure | Tomato | Soybean | Basis |
| --- | --- | --- | --- |
| Hours | 64 to first ripe | 90 | Chamber tomato fruit from day 65 of 84 to 91; soybean 90 to 97 days, potato's |
| Rack power | 0.7 kW | 0.65 kW | 38.6 and 36.5 mol PAR per m2 per day against potato's 42 |
| Seed | 5 g packet | 30 g packet | Our choice; both keep one back |
| Final mass | 5 kg | 0.9 kg | Tomato fruit is 94.5% water (USDA); a ripe soybean stand is dry |
| Carbon (as CH2O) | 0.52 kg | 0.68 kg | Chamber total dry matter per cycle about 0.6 and 0.5 times potato's (derived) |
| Edible | 3.6 kg fruit | 0.29 kg beans | About half (tomato) and a third (soybean) of the dry matter |
| Harvest | fourteen 0.25 kg portions, packet, 1.5 kg vine | one 0.25 kg portion, packet, 0.62 kg straw | Whole portions |

**Repeat picking.** The crops schema gains optional `picks` and `pickKg`, and the
saved planting an optional `picks` count (absent means none, so older records are
unchanged). A pick takes whole portions, up to `pickKg`, from a ripe plant with picks
left. It removes their mass and the matching share of the plant's carbon, and sets
growth back by exactly the share of a cycle they were. Regrowing therefore uses the
same water, nutrient and carbon budget per unit of progress that grew them, and a
healthy plant returns to the state it was picked from; the unit checks prove this
by stepping the regrowth. Tomato allows three picks of up to 0.75 kg, about ten
hours of regrowth each. The chamber picked its tomatoes as they ripened over the
last quarter of the cycle; three picks is our authored number.

**Soybean stew.** Hearth revision 3: 0.25 kg of dry beans and one 0.25 kg water ration
into a 0.5 kg bowl over fifteen minutes; dry beans take up about their own mass of
water. Real soybeans soak for hours first; the short cook is authored. Food values
eight and six (authored: about three potato portions of energy, and protein-rich).
Oil from soybeans waits for phase 4's press decision.

Artwork: 24 PixelLab generations from the subscription, 17 selected (twelve growth
stages, five icons); rejected and superseded outputs are on the archive branch
(`assets/phobos-agriculture/tomato-soybean-generation-records.json`). The tomato's
mature stage carries green fruit and its harvest stage red, so a picked plant is
seen to ripen again.

Fridge and crate finds keep their 0.22 and 0.30 totals, now shared by every crop.

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

## Phase 3: CO2 response and spare condensate (Agriculture 0.43.0)

Owner, 4 October 2026: proceed with phase 3.

**CO2 response.** `crops.json` gains a top-level `co2Response` curve, outside the frozen
crop entries: a growth factor against the room's carbon dioxide partial pressure. The
factor multiplies growth per hour and per kWh, and every budget per unit of growth is
unchanged, so mass is conserved and an enriched room only shortens the cycle and its
energy; the unit checks grow wheat at 1 and 1.25 and compare. One curve serves every
shipped crop (all are C3 plants):

| CO2, kPa | 0.04 | 0.10 | 0.15 | 0.25 | 0.5 | 1.0 |
| --- | --- | --- | --- | --- | --- | --- |
| Factor | 1.00 | 1.20 | 1.25 | 1.05 | 1.00 | 0.85 |

Source findings (see Carbon dioxide response above): saturation near 1,000 to 1,500
ppm; wheat +13 to +40% at 1,000 to 1,200 ppm; 15 to 22% less at 2,500 ppm and 37%
less by 10,000 ppm. Our choices: +20% and +25% rather than the top of the range, no
penalty below ordinary air (a room with no CO2 still stops growth, as before), and
one curve for crops the studies measured separately. It applies to plantings already
growing, because it changes their rate and never their budgets. The A2's 0.10 kPa
setting gives x1.20.

**Spare condensate.** A rack keeps condensing its plants' water into its own
reservoir. What a full reservoir cannot hold used to be discarded (the game has no
water vapour species); it now goes to the nearest Framework water tank the rack
reaches by the owner's link rule (touching or the process-water line), through the
tank's own record, so the water stays in the ship's books. With no reachable,
ready tank it is lost as before. Ship's Water drinking tanks are not used: condensate
is not routed into the potable pool (reclaim, do not join).

## Residue to carbon: why it waits

Findings from the Manufacturing charge engine (4 October 2026):

- Every V4 input has a fixed unit mass, checked at admission, binding and delivery;
  crop residue and spent biomass each carry their own mass (0.2 to 1.5 kg), so the
  engine cannot bind them as they are.
- Honest yields are small. Biomass pyrolysis leaves roughly a quarter to a third of
  the dry matter as char (our estimate, not yet sourced); a wheat harvest's straw
  would give about a quarter kilogram of carbon, against 5 kg from one carbon ore
  charge.
- Residue is priced at 0.01 cr, so any carbon product fails the refining value rules'
  step test by orders of magnitude; like fertiliser, it would need its own owner
  rule.

Options put to the owner: a fixed-mass dried straw bale made at the B2 bench and
charred four at a time in the V4; burning bales into CO2 for the grow room instead
of carbon stock, which closes a loop with the CO2 response; or setting the tie aside.

## Phase 3b: straw bales both ways (Agriculture 0.44.0, Manufacturing 0.37.0)

Owner decision, 4 October 2026: **both**: bales burned to CO2 and charred to carbon
stock, knowing the char needs a value exception. The owner then left the remaining
choices to the agent; those below are agent decisions, open to revision.

**Residue records.** A residue record may now carry an `organic` field: the
residue's share of the plant's CH2O-equivalent fixed carbon (the crop model's
`Carbon`, so the field is exact in the model's own terms). A harvest writes it; B2
recovery writes it onto spent biomass too, with the nutrients left behind. Records
without it (all earlier residue) still recover nutrients and cannot be pressed.

**The press** is a saved accumulator on the B2 (`AgricultureStrawPress`: organic,
minerals, water, drying energy), counted in the bench's mass and blocking its
removal while it holds anything. Loading is a one-minute crew action that takes
pressable items whole, up to 12 kg. Starting the bench with no workup job runs the
dryer at 1 kW (agent choice), which boils off water above the bale's share at
2,257 kJ/kg (NIST Chemistry WebBook, latent heat of vaporisation at 100 C; sensible
heating left out) and puts it into a reachable Framework water tank through
`VapourReturn`, all or nothing: without room it stops and says so. Whole bales then
go to the tray. A bale is fixed at 0.87 kg organic, 0.03 kg minerals and 0.10 kg
water (about 10% moisture, the usual figure for stored straw; agent choice) so the
V4 can bind it by mass. Minerals sit between fresh residue (3.5 to 8% of the organic
matter) and spent biomass (1.4 to 3.3%); what no whole bale can take waits for more
straw or leaves through **Empty the straw press** as one recorded residue, so nothing
is created or destroyed.

**The bale** (`PhobosVerdemorrowStrawBale`, 1 kg, 1 cr, stack 10, not sold or found)
carries the `PhobosVerdemorrowStrawBaleIdentity` condition the V4's feed admits at
the game level. Its icon is a mechanical crop of the top surface of a PixelLab
output (records in `assets/phobos-agriculture/straw-bale-generation-records.json`).

**The V4 charges** (Manufacturing 0.37.0, revisions 13 and 14, gated on Agriculture
0.44.0's bale at 1 kg; chemistry in the refinery record):

| Charge | In | Out |
| --- | --- | --- |
| straw-burn | 1 bale, 0.927 kg O2 drawn | 1.275 kg CO2 to a store, 0.622 kg water, 1 plant ash (30 g); 3.76 kWh into the room |
| straw-char | 4 bales | 1 carbon stock, 1.9 kg water, 0.261 kg methane to a store, 0.719 kg CO2 into the room, 4 plant ash; 1.90 kWh into the room |

Char yield: 1.000 kg of carbon from 3.48 kg of organic matter, 29%, authored within
the quarter-to-a-third char yield of slow pyrolysis reported in M. J. Antal and
M. Gronli, The Art, Science, and Technology of Charcoal Production, Industrial and
Engineering Chemistry Research 42 (2003), 1619-1640 (unverified against the paper:
read before quoting). Char is treated as pure carbon and tar as cracked; the gas
split closes by element. Heats use formation enthalpies with glucose
(-1273.3 kJ/mol, NIST) standing in for plant carbohydrate.

**Value.** Burning makes no finished item, so it is a supply charge. Charring turns
4 cr of waste bales into a carbon stock worth far more, which the refining rules'
step test refuses; the owner's choice of both routes accepts it, and the native
value check records it as an exception: bales are priced as waste and sold by no
one, so no bought loop exists.

## Phase 4: fibre flax (Agriculture 0.45.0)

The owner delegated phase 4 while away (4 October 2026); every choice below is an
agent decision, open to revision.

**Why cloth.** The game's clean scrap cloth (`ItmScrapClothClean`, 25 g, 2.40 cr) is
a real consumable: ten native repairs take it (beds, medical beds and others, four
to six each) and weapon Restore uses absorbent scrap cloth as its buffing tool. A
ship far from a station runs short of it; flax closes that gap with the game's own
item, so no new textile identity is needed.

**The crop.** One 10 g packet sown dense, 90 hours at 0.9 kW, 1.6 kg stand; four
0.25 kg bundles of retted flax straw, the packet back, 0.59 kg residue. Fibre flax
takes about 90 to 100 days from sowing to pulling; the hours, light and yields are
authored, kept in the same proportion to the other crops as their real cycles.

**Scutching.** A third B2 job (`scutch`): one 0.25 kg bundle, authored as 88%
organic matter, 2% minerals and 10% water, gives two clean cloth (0.05 kg, 20% of
the straw) and 0.2 kg of shives as recorded residue (0.17 kg organic, 5 g minerals,
25 g water) that the straw press takes. Source: *Comparing flax and hemp fibres
yield and mechanical properties after scutching/hackling processing*, Industrial
Crops and Products (2021), [ScienceDirect](https://www.sciencedirect.com/science/article/pii/S0926669021008104):
about 25% fibre after scutching and 15% after hackling at industrial scale; authors
not yet recorded here (to complete before quoting). Spinning and weaving are folded
into the bench's work; the game has items for neither. 0.05 kWh per bundle is
authored.

**Trade.** Flax straw is priced at 2 cr and sold by no one, so a bought bundle can
never be resold as 4.80 cr of cloth. The seed packet is sold like the other seed (12
cr, Neutral faction tier, one crate share taken from the nutrient charge's so the
crate total stays 0.30).

**Crew.** A bench order **Scutch flax into cloth** fetches bundles, runs the job and
hauls the cloth out; between jobs it loads pressable residue into the straw press,
so shives never fill the six-cell tray. It does not run the dryer, which needs a tank.

**Not done.** Linseed exists only as the planting packet. An oil press, shared with
soybean oil, still waits for a real use for oil (the plan's rule); edible linseed
was left out to keep the crop's decision about cloth.
