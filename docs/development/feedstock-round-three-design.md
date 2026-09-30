# Feedstock round three: salts, phosphate and fertiliser (design record)

Prepared 30 September 2026 after schema separation steps 1 to 6 and the refining
value review ([programme status](asteroid-feedstock-programme-status.md)). This
is the research-first record the round asks for: sourced chemistry, authored
fractions labelled as ours, worked mass balances, the machine design and the
decisions the owner should see before any of it is written into a pack.

**Status (30 September 2026).** The owner approved all five decisions, choosing the
kiosk nutrient hopper, both struvite routes with sulfur and acid, and the shared
charge engine. Manufacturing 0.18.0 implements this record's chunk, the four
recipes and the LC-3 (at 12 kW, so the struvite and formulation steps run five and
two and a half minutes), with two changes: the formulation blends one potassium
sulfate with two struvite into 39 packets so nothing is left over, and the phosphate
concentrate is priced at 12 cr so struvite stays within the 1.5 x guardrail (the
10 cr below would not). Progress is tracked in
[the programme status](asteroid-feedstock-programme-status.md).

## What the round delivers

- A mined **evaporite crust** chunk from C-class material (the second Phobos
  chunk beside the ammonium salt crust), carved from the silicates share the way
  the salt crust was.
- One new hydrometallurgy machine under a new brand: a leach and crystallise
  unit that takes the chunk and water from a linked vessel, and stored ammonia
  from a Q store for the phosphate step.
- Four recipes in the packs: leach and potassium sulfate (glaserite route),
  struvite from the phosphate concentrate and ammonia, fertiliser formulation
  into Agriculture's Groundwork makeup packets (gated on Agriculture), and a V4
  charge that calcines the leached residue's magnesite into stored CO2 for the K2.
- The first `equipment` pack entry (audit step 5), declaring the new machine's
  shape (footprint, mass, power, art, INSTALL tab) beside its economy row.

## Sources

- McCoy, T. J. et al. (2025), "An evaporite sequence from ancient brine recorded
  in Bennu samples", *Nature* 637, 1072-1077, NASA OSIRIS-REx sample analysis
  team led from the Smithsonian Institution ([Nature](https://www.nature.com/articles/s41586-024-08495-6);
  abstract at the [University of Manchester research explorer](https://research.manchester.ac.uk/en/publications/an-evaporite-sequence-from-ancient-brine-recorded-in-bennu-sample/)).
  Finding used here: Bennu samples hold sodium-bearing phosphates and sodium-rich
  carbonates, sulfates, chlorides and fluorides from an evaporated late-stage
  brine: Na,Ca carbonate (gaylussite, pirssonite), Na carbonate (trona,
  wegscheiderite), Na sulfate (thenardite), Na and K chlorides (halite, sylvite),
  Na fluoride (villiaumite), with calcite, and a Mg,Na phosphate beside them.
  Eleven minerals form a complete evaporite set. The paper reports the minerals
  as vein and crust phases; **it does not give bulk weight fractions we could
  read**, and the full text is behind the journal's login. Every fraction below
  is therefore authored and labelled so.
- University of Arizona / NASA summary, 29 January 2025 ([Kuiper-Arizona
  Laboratory](https://kalfaa.lpl.arizona.edu/news/asteroid-bennu-comes-long-lost-salty-world-ingredients-life)):
  the salts decompose in Earth's air; the sequence runs from calcite to halite and
  sylvite; water sat in pockets or veins.
- Glaserite route to potassium sulfate: 3 KCl + 2 Na2SO4 -> K3Na(SO4)2 + 3 NaCl,
  then glaserite plus KCl solution -> K2SO4 plus a sulfate liquor, cooled
  crystallisers, reported yields up to 98.6% ([US patent 4,215,100](https://patents.google.com/patent/US4215100A/en);
  [US patent 6,143,271](https://patents.google.com/patent/US6143271A/en)). The
  overall stoichiometry we use is 2 KCl + Na2SO4 -> K2SO4 + 2 NaCl.
- Struvite precipitation for phosphorus recovery: MgNH4PO4.6H2O at an equimolar
  Mg:N:P ratio, pH 8 to 9.5, a mature wastewater technology and a slow-release
  fertiliser ([Environmental Evidence 9:34, 2020, systematic review](https://environmentalevidencejournal.biomedcentral.com/articles/10.1186/s13750-020-00211-x)).
- Molar masses: IUPAC 2013 conventional atomic weights, rounded to three decimals
  in the balances below.
- Agriculture's nutrient model is one aggregate kilogram figure; the makeup
  packet is 40 g at 30 cr ([nutrient production guide](../agriculture-nutrient-production.md)).

## The chunk (authored)

`PhobosEvaporiteCrust`, 10 kg, mined and never sold by merchants, bought by the
kiosks at the hydrates price like the other chunks. It is an evaporite-rich
piece of a brine vein, deliberately far richer than Bennu's bulk sample, as the
ammonium salt crust is; the minerals are the ones McCoy et al. report, the
proportions are ours:

| Phase | Formula | kg | Why this much |
| --- | --- | ---: | --- |
| Clay matrix | phyllosilicate | 6.30 | Most of any real chunk is host rock |
| Halite | NaCl | 1.20 | The dominant late evaporite |
| Sylvite | KCl | 0.60 | The potassium; kept small, potassium is scarce |
| Thenardite | Na2SO4 | 0.60 | The sulfate, a little over the sylvite's need |
| Trona | Na3(CO3)(HCO3).2H2O | 0.50 | Sodium carbonate: no useful CO2 without acid |
| Magnesite | MgCO3 | 0.50 | The carbonate that calcines (stands in for the Mg,Ca carbonates) |
| Na,Mg phosphate | NaMgPO4 (simplified) | 0.25 | The phosphorus |
| Villiaumite | NaF | 0.05 | Reported; a fluoride hazard note, no recipe use |

Villiaumite and trona stay with the brine salt cake; the recipes do not pretend
to separate them.

## Recipes and balances

Water is drawn from a linked vessel and returned to it: the leach dissolves the
salts and the crystalliser evaporates and condenses the water back, so the net
water change is the hydration water gained by the products. Every figure is
per chunk; energy is authored beside NIST heats where a heat is named.

### R1. Leach and potassium sulfate (leach unit)

Dissolve the crust in 20 kg of water, crystallise by the glaserite route,
2 KCl + Na2SO4 -> K2SO4 + 2 NaCl; KCl limits (8.05 mol KCl, 4.22 mol Na2SO4).

| | kg |
| --- | ---: |
| In: evaporite crust | 10.000 |
| In: water (returned) | 20.000 |
| Out: potassium sulfate (K2SO4), 4.024 mol | 0.701 |
| Out: phosphate concentrate (NaMgPO4, wet-separated) | 0.250 |
| Out: leached residue (matrix + magnesite) | 6.800 |
| Out: brine salt cake, terminal (halite 1.200 + NaCl made 0.470 + Na2SO4 left 0.028 + trona 0.500 + villiaumite 0.050) | 2.249 |
| Out: water returned | 20.000 |

Sum of solids out: 10.000. Rounded to item units: potassium sulfate 0.70 kg,
phosphate concentrate 0.25 kg, leached residue 6.80 kg, brine salt cake 2.25 kg
(the 0.001 kg rounding goes to the cake). Sodium chloride and the sodium
carbonates leave as the terminal remainder because sodium harms crops (B2 note).

Energy (authored): dissolving and evaporating 20 kg of water in a closed
crystalliser with condensate recovery is set at 6 kW for two hours (12 kWh),
about a quarter of the open-pan evaporation figure (2.26 MJ/kg water, NIST),
because the condenser returns most of the latent heat; 15% of the working power
warms the room under the R4 rule.

### R2. Struvite (leach unit, needs a linked Q store)

NaMgPO4 + NH3 + 7 H2O -> MgNH4PO4.6H2O + NaOH (our simplification of struvite
precipitation from the chunk's own Mg,Na phosphate; the sodium leaves as
hydroxide, which the brine cake takes up as carbonate in air).

| | kg |
| --- | ---: |
| In: phosphate concentrate, 1.757 mol | 0.250 |
| In: ammonia from the Q store | 0.030 |
| In: water (consumed, hydration) | 0.222 |
| Out: struvite (MgNH4PO4.6H2O) | 0.431 |
| Out: caustic remainder (NaOH), terminal | 0.070 |

Sum 0.502 both sides. Rounded: struvite 0.43 kg, caustic remainder 0.07 kg,
ammonia 0.03 kg, water 0.22 kg (the 0.002 kg rounding goes to the water drawn).
This is the round's nitrogen consumer: 30 g of ammonia per chunk, so one
ammonium salt crust (0.955 kg of ammonia) feeds about thirty evaporite crusts.
Energy: mixing and a low-temperature dry, 1 kW for one hour (authored).

### R3. Fertiliser formulation (leach unit, requires Agriculture)

Potassium sulfate 0.70 kg + struvite 0.43 kg -> 28 Groundwork makeup packets
(28 x 0.040 = 1.120 kg) + 0.010 kg of blending remainder (terminal, the packet
rounding). Requires Agriculture's makeup identity at load, the
`shipbreaker-steel-stock` pattern: without Agriculture the recipe is unavailable
and the salts stay saleable stock. Agriculture's model is one aggregate nutrient
figure, so a packet from asteroid salts is the same 40 g packet the station
sells; the honesty is in what went into it (K, S, N, P, Mg), stated in the
recipe notes, not in a split Agriculture cannot use. Energy: 0.5 kW for one hour.

### R4. Calcine the leached residue (V4, revision 7)

MgCO3 -> MgO + CO2 on the 6.80 kg residue (0.50 kg magnesite, 5.93 mol):

| | kg |
| --- | ---: |
| In: leached residue | 6.800 |
| Out: CO2 to a linked C2 store (stored gas, the salt crust's outlet pattern) | 0.261 |
| Out: calcined residue (matrix + MgO 0.239), terminal | 6.539 |

Rounded: CO2 0.26 kg, calcined residue 6.54 kg. Energy: magnesite decomposes
from about 350 C; authored 3 kW for one hour on the V4 hearth with the usual 15%
room share. This is the carbonate CO2 the K2 was promised; sodium carbonate
(trona) does not calcine usefully, which is why it stayed in the cake.

## Value under the refining guardrails

Chunk 150 cr (the hydrates price, as the other chunks). Proposed stock prices:
potassium sulfate 60 cr/kg (42 cr per 0.70 kg), struvite 40 cr/kg (17 cr),
phosphate concentrate and leached residue 0.01 cr (intermediates, never sold
for more than trash); terminal cakes 0.01 cr. R1 sellable products 42 + 0.01 x 3
cr against 150 cr in: a loss, for the salts, as the salt crust is for the
nitrogen. R2 turns 0.25 kg of trash-priced concentrate and 0.10 cr of ammonia
into 17 cr of struvite: a gain far above 1.5 x, so the concentrate must carry
a price (proposed 40 cr/kg, 10 cr) making R2 0.10 + 10 -> 17 cr, within the
bound; the concentrate is never sold by merchants, so the bought-stock bound does
not apply. R3: 59 cr of salts -> 28 x 30 = 840 cr of makeup packets. **That
breaks the 1.5 x guardrail badly** and is the round's real pricing question:
the station's makeup packet is priced at 30 cr for 40 g (750 cr/kg) because it
is a finished formulated product, while the salts are priced as raw stock. Two
honest options for the owner: price the packet made aboard at the guardrail
(the recipe yields fewer, richer packets, or the salts carry fertiliser-grade
prices of about 500 cr/kg so 59 becomes 566 cr in), or accept that formulation
is where the value is made and exempt R3 as the one step that turns raw salts
into a product Agriculture buys at 750 cr/kg, capped by that price. The record
recommends the second with the cap: no loop exists (no merchant sells the salts
or buys them above stock price) and the 30 cr packet is Agriculture's own value.

## The machine

**Brand and model (proposal):** *Lixivar*, from lixiviation, the chemists' word
for leaching; no chemical, mining or water-treatment company of that name was
found in a web search on 30 September 2026. Model **LC-3 Leach and Crystallise
Unit**: L for leach, C for crystallise, 3 for the footprint. Full name
*Phobos' Lixivar LC-3 Leach and Crystallise Unit*. Siblings later (an acid
leach for olivine, D3) keep the Lixivar brand.

- 3 x 3 tiles, 220 kg, INSTALL > APPS, one power point, feed bin of four units
  (the V4's `AddFeedBin` pattern, admitting the evaporite crust, the phosphate
  concentrate, potassium sulfate and struvite), products to its own tray.
- Links: water vessel within one tile (`BulkVessels.Adjacent`, drawn and
  returned through the Framework transfer guard, the X2's inlet pattern plus the
  V4's outlet pattern), an ammonia Q store within one tile for R2 (the cracker's
  ammonia inlet pattern).
- Recipe choice: explicit, the F6 `SelectRecipe` pattern, because R1's chunk,
  R2's concentrate and R3's salts never overlap but a player may hold both salts
  and a chunk in the bin; the panel shows the selected recipe and Start binds it.
- Batch pattern: explicit Start, repeat while supplied, pause after reload; heat
  waits, not stops; a cancelled charge returns its units.
- Art: overhead-first PixelLab request from an original procedural start image
  in new Lixivar colours (proposal: pale sage enamel frame, dark slate leach
  tank, copper-brown crystalliser drum), 48 px native for 3 x 3 with a 192 px
  master under the resolution memorandum; one pilot inspected before the loose
  and damaged forms; no painted gauges. 1,762 generations remain this cycle.

### Implementation shape (audit step 5 and complexity reduction)

The V4's `RefineryService` is 400 lines of charge binding, links, heat waits,
power receipts and delivery that the LC-3 would repeat. The intended change is
to lift that into a Framework or Manufacturing `ChargeMachine` service
parameterised by a rules object (prefix, ports, feed rule, power, room-heat
share, link kinds) with the V4 and LC-3 as two instances; the recipe catalog
already carries a `machine` key per recipe (`refinery`, `leach`). The LC-3's
shape goes into the first `equipment` pack entry: footprint, mass, idle and
working power, room-heat share, feed capacity, art name and INSTALL tab, with
the V4 migrated alongside so both read the same schema.

## Decisions for the owner before implementation

1. The brand and model name (Lixivar LC-3), or another.
2. The authored chunk composition above, especially the 0.60 kg of sylvite (the
   potassium yield) and the 0.25 kg of phosphate.
3. R3 pricing: cap the made packet at Agriculture's 750 cr/kg (recommended) or
   hold the 1.5 x guardrail by re-pricing the salts.
4. Whether the struvite step should instead use magnesium from the residue with
   an acid step (D3), which would wait for sulfur (B3); the record keeps the
   simpler own-Mg route.
5. Whether to refactor the V4 service into the shared charge-machine service in
   the same change (recommended) or copy it for the LC-3.

## What is not in this round

Sulfur (B3) waits for a real acid consumer; olivine magnesium (D3) waits for
sulfur; villiaumite is noted as a fluoride hazard and not processed; the
carbonate CO2 comes only from magnesite, not from trona.
