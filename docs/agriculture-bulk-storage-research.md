# Agriculture bulk storage: findings and first recommendation

**Implementation follow-up:** Agriculture 0.14.0 / Framework 0.27.0 now provide the [first R3 slice](agriculture-bulk-storage.md). The dated proposal/audit below remains the pre-implementation record. Actual API boundaries and conservative destruction fallback are documented in the current guide; [original production masters and exports](../assets/phobos-agriculture/bulk/README.md) are now retained. No owner Unity validation is claimed.

27 September 2026. **Research and proposed implementation only.** No reservoir,
station service, recipe or production artwork is registered by this work.
Baseline inspected: Framework 0.26.1, Agriculture 0.13.1, Ship's Water 0.16.1.

## Recommendation

Start with **Phobos' Verdemorrow Groundwork R3 Agricultural Water Reservoir**:
a proposed 3 x 3, 120 kg water store directly coupled to one existing W2. Pair
it with a **500 g formulated nutrient charge**, using the existing gradual W2
dosing model. Use a separate **Bulk supplies** view opened from the native
station refuelling screen. Keep existing small packets and optional Ship's Water
working. R3 is an optional endurance upgrade, not a new farming requirement.

Do not build a dedicated nutrient silo, prepared-feed tank or drainage tank in
the first slice. Their benefit does not currently justify three more machines
and fluid circuits. The [implementation blueprint](agriculture-bulk-storage-design.md)
specifies the shared support, concrete proposed equipment and later extension
boundaries. The [art audit](agriculture-bulk-storage-art.md) identifies what can
be reused and what actually needs original artwork.

## What current code establishes

- `CropState` owns a 20 kg liquid envelope and 0.5 kg nutrient limit. W2 currently
  uses those same limits. A rack is 4 x 4 / 80 kg dry; W2 is 2 x 2 / 20 kg dry.
- W2 supports eight selected racks. A circuit permits one pump authority; another
  W2 on that circuit blocks delivery. Additional W2s need independent circuits.
- Nutrient profiles are water plus aggregate nutrient mass. They do not identify
  N/P/K, pH, electrical conductivity, pathogens or salt compatibility. The word
  "concentrate" on a B2 product does not establish that it is a liquid.
- Manual loads use 5 kg irrigation charges or 0.25 kg native water units, and
  40 g nutrient packets. Each load interaction takes ten seconds before travel
  or speciality effects. Selected W2 charges deplete physically; no substitution
  occurs when a selected charge disappears.
- Existing Framework services already provide scalar/two-component reservoirs,
  guarded measured transfers, paired ports, route checks and retained parcels.
  They do not provide a station commodity registry or a purchase transaction.
- W2 treatment excludes simultaneous distribution. B2 recovers recorded crop
  nutrients; it does not accept uncharacterized wastewater or Recycler wet rejects.

These are source observations, not new Unity playtest results. Exact inputs and
fingerprints are in the generated [calculation tables](agriculture-bulk-storage-calculations.md).

## Size the benefit before sizing the silo

The calculator reads current crop demand from source rather than importing the
older design calculator's gross transpiration values. It covers all 54 combinations
of three crops, one/four/eight racks, seven/thirty days and pace 0.5/1/2.
Continuous ideal growth deliberately excludes turnaround: consumption is a supply
planning envelope, not a prediction of actual harvests. Lack of power, CO2,
temperature control, seeds, labour or output space reduces actual production.

At default pace, eight potato racks require **277.440 kg water and 2.400 kg
nutrients** over thirty growth-days. Eight food-lettuce racks require 151.896 kg
and 0.600 kg; eight seed-lettuce racks require 81.360 kg and 0.600 kg.
Faster pace doubles these horizon demands; slower pace halves them. Per-cohort
material and energy costs do not change.

Using 19.5 kg water per rack/W2 as a conservative mixed-feed headroom allowance,
eight potato racks plus W2 carry 18.98 days of water. R3 raises the water envelope
to 31.95 days. This is **not thirty days unattended**: the model's central 0.5 kg
nutrient stock lasts only 6.25 days, and harvesting/planting still needs crew.
Rack nutrient stocks are deliberately excluded from that central-stock comparison.
One potato rack plus W2 already has about 33.74 days of water: R3 is poor value
for that small farm unless the owner wants a much longer reserve.

| Alternative | Assessment |
|---|---|
| Existing rack buffers | Best first choice for small farms; already paid for. Initial filling and later access still take work. |
| Six additional W2 stores | Nominal 120 kg storage, but 24 extra tiles, 120 kg dry machinery and 1,500 cr versus proposed R3's 9 tiles, 25 kg and 450 cr; not a legal same-circuit bank. Independent circuits offer redundancy rather than an equivalent central reserve. |
| More 5 kg water packages | Same useful water mass without fixed machinery; cargo footprint and carrying depend on chosen storage. Fifty-six loads cover the eight-rack potato case from empty. R3 reduces onboard handling only when station-filled; filling it from the same small packets does not erase those loads. |
| 500 g nutrient charges | Five rather than sixty 40 g packages cover that case, with 0.1 kg unused stock versus zero for sixty packets. No additional installed floor footprint. Requires a new supported item and deliberate crew charge-selection policy. |
| Crew replenishment | Removes player clicking, not crew work, walking or required stocks. Remains useful with or without R3. |
| Prepared feed reserve | Duplicates substantial rack buffers, binds storage to a crop profile and complicates pump authority; defer. |
| Recorded drainage receiver | Useful only during explicit drain/changeover; the current model creates no continuous return flow during healthy growth. Defer until actual changeover traffic warrants bulk custody. |

The generated tables separate water and nutrient limits, installed tile counts,
minimum useful supply mass and dry machinery mass. Actual carried packages round
up and include unused contents. Access aisles, pipes, seeds and product stores are
additional. Whole-load counts are fleet-wide lower bounds: distribution between
individual inventories can require extra partial packets/trips. Thirty-second
and two-minute carrying examples are sensitivities, not measured crew paths.

## Recovery remains a process, not free stock

One healthy potato crop yields 0.8 kg recorded residue. Current B2 accounting
recovers 3.84 g concentrate plus 0.79616 kg spent biomass. Another 3.84 g of
external makeup salts produces 7.68 g finished nutrient mixture. Two crew setups
take two minutes; both powered stages require 0.017 kWh in total, or 2.04 minutes
at full 0.5 kW, and deliver that electrical energy as room heat. No extra process
water is charged by this existing recipe. Quantities for both lettuce crops are
also calculated. These finished products remain physical charges for W2.

An illustrative drain batch containing 19.5 kg water and 0.1 kg nutrients consumes
0.0392 kg treatment medium from a 25 kg-rated cartridge. It returns 17.55 kg
nonpotable water and 0.08 kg nutrient stock, with 2.0092 kg terminal rejects and
5.4 kg cartridge treatment capacity left. It costs 0.196 kWh and 23.52 powered
minutes plus fifteen crew setup minutes. Power loss extends time; blocked output
retains material. This is not a continuous farm effluent rate.

Even one kilogram of water in each potato rack covers roughly 20.76 hours at
default pace. W2 treatment's nominal 23.52-minute interruption alone does not
justify another prepared-feed pump. Check actual rack levels before treatment;
a new tank cannot rescue a drained rack while its sole W2 distributor is busy.

## Research support and limits

- **Bruce Dunn, Oklahoma State University Extension, Hydroponics (July 2025,
  HLA-6442):** [primary guide](https://extension.okstate.edu/fact-sheets/hydroponics).
  Distinguishes supply/return cultivation systems and nutrient management. It
  supports separating storage from formulation/treatment; our fixed two-component
  profiles are much simpler than real solution management.
- **NASA Kennedy Space Center, Passive Porous Tube Nutrient Delivery System:**
  [technology description](https://technology.nasa.gov/patent/ksc-tops-73).
  Describes capillary delivery suited to microgravity. Our inference is to favour
  enclosed liquid management over gravity hoppers; it does not validate R3's
  proposed diaphragm, pump, capacity or reliability.
- **Jay Garland, Bionetics Corporation, NASA TM-107557 (December 1992):**
  [Characterization of the Water Soluble Component of Inedible Residue from
  Candidate CELSS Crops](https://ntrs.nasa.gov/citations/19930008922). The author
  investigated nutrient recovery from crop residues, with incomplete recovery
  and crop-dependent composition. This supports retaining makeup inputs and
  separating residue from finished feed, not our 60% recovery or equal-mass recipe.
- **Blue Bottle Games, [Ostranauts](https://bluebottlegames.com/ostranauts):**
  installed native definitions/code establish the inspected game mechanisms.
  **Valtora, [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189):**
  inspected 0.16.1 provides the optional water precedent. Neither code nor artwork
  from these providers is redistributed by this research.

Sources checked 27 September 2026. None establishes our growth speed, storage
ratings, prices, safety or treatment efficacy; no institutional endorsement is
implied. No new pH, pathogen, gas or reactive-mixture simulation is proposed.

## Reproduce and evidence boundaries

Run `python scripts/calculate-agriculture-storage.py --check` and
`python -m unittest discover -s tests -p test_agriculture_storage.py`.
Use `--format json` for all scenarios; `--write` regenerates the reviewed tables.
The calculation tool opens source text only, never game saves.

Local native inspection lives in `.local/research/agriculture-bulk/`; image
reproduction is described in the art audit. Assembly SHA-256:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
Ship's Water SHA-256:
`f236324a1c7732a2cefb81daac9de9167b31b3fa6fe646e32f88ac8702f4fc04`.
These match the earlier chemical-storage inspection. The new decompilation
examined `GUIStationRefuel`, `GUIStationRow` and `ShipsWater.KioskPatch` only.
No full game dump, build, installation, save mutation or artwork generation was
needed. Remaining Unity uncertainties and tests are explicit in the blueprint.

Final offline review: all 66 Python checks passed, including seven new storage
calculation checks. Maintained constants, generated item references, Workshop
records and local document links also passed. Six source-art comparison sheets
were inspected. This establishes a reviewed research proposal; it does not verify
future station settlement, Unity layout/lighting or in-game endurance.
