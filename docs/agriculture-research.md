# Phobos Agriculture: research findings

Research date: **25 September 2026**. Owner direction: crops first, managed
machinery with crew tending, a staple plus a fresh vegetable, and openly
documented game-paced growth. Phobos Framework is required; useful links to
other content mods are optional. **Research baseline preceding implementation.** Agriculture 0.2.0 is now prepared
for owner evaluation; see [delivered scope](agriculture-implementation.md). It has
not been installed or gameplay-tested by this task.

Read the [first-slice specification](agriculture-first-slice.md) for the proposed
equipment and accounting, and the [endurance roadmap](agriculture-roadmap.md)
for subsequent work. This report implements the research request, not a runtime
mod, a new atmosphere simulation or an artwork production order.

## Recommendation

Proceed with a contained cultivation rack growing **potatoes and lettuce**.
Potatoes address the main food supply and allow retained tubers to be replanted;
lettuce provides a distinct, smaller food contribution and an eventual seed
production choice. NASA's Linda Herridge describes Ray Wheeler's controlled
crop research, including staple crops and potato propagation, in
[NASA Plant Researchers Explore Question of Deep-Space Food Crops](https://www.nasa.gov/science-research/nasa-plant-researchers-explore-question-of-deep-space-food-crops/).
This supports the crop selection, not our machinery dimensions or yields.

The practical value is extending time away from food vendors while adding a
living part of the ship to maintain. A small rack should supplement stores.
R. M. Wheeler's [NASA bioregenerative life-support overview](https://ntrs.nasa.gov/citations/20205008786)
gives indicative areas of 20–25 m² for one person's oxygen and about 50 m² for
dietary calories, dependent on lighting and cultivation. These are canopy-area
research estimates, not universal requirements or tile conversions.

Food and atmospheric benefit must not be credited twice or detached from actual
growth. A crop cannot manufacture fertilizer, replace every dietary requirement
or recover resources that the native game never physically captures.

## Evidence labels and current environment

Use these labels throughout the connected documents:

- **Observed:** inspected native definitions, repository code or installed files.
- **Scientific reference:** a named external source and its limited finding.
- **Proposed:** authored gameplay behaviour, parameters or future interfaces.
- **Unverified:** a relevant runtime interaction not exercised in the game.

The read-only refresh found **34 packages: 32 configured enabled, two disabled**,
with no metadata errors. The latest inspected Player log reports **Ostranauts
1.0.1.5**; BepInEx reports 5.4.23.5 and 25 plugin startup entries. Installed
metadata includes Framework 0.16.0, Auto Nav 0.10.1, Shipbreaker 0.13.0 and
Ship's Water 0.16.1. The inspected startup log still records Shipbreaker 0.12.0:
installed-file version and last loaded version are distinct evidence.

Local evidence is retained in ignored research files:

- `.local/research/agriculture-inventory-2026-09-25.json`, produced by the existing
  [inventory script](../scripts/inventory-mods.py).
- `.local/research/agriculture-native-evidence-2026-09-25.json`, selected native
  records and source-file hashes.

The inspected Assembly-CSharp SHA-256 is
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
No saves were read or modified. No game-derived definitions, binaries or images
are included in this report or a distributable package.

## What the game and existing mods provide

| Observed source | Finding | Design consequence |
| --- | --- | --- |
| Native `condowners/condowners.json`: `ItmPlanter01`, `ItmPlanter1x101` | Admire/decorative interactions, no crop tickers in these definitions | Use as visual/context precedent; do not describe them as a native farming API or replace existing planters |
| Native `LiquidWater` | A 0.25 L ration with 0.25 kg mass | Finite carried-water input is a fallback without Ship's Water |
| `SeekFoodDirect` and `SeekFoodAllowDirect` | Eating uses native interaction/trigger paths and consumes the item | Preserve ordinary crew food use; verify both AI and manually selected eating |
| `CTFoodAllowDirectThem` | Five units of food-debt reduction and five satiety units | A lettuce flag alone would grant a full ordinary-food effect |
| `CTFoodAllowDirectPreparedThem` | Nine food-debt units, five satiety units and other prepared-food effects | Do not inherit all prepared-food rewards merely to make potatoes edible |
| Native `Food` ticker, `CONDTickFood` and `TDnFood` | One food-debt unit per native hour at the baseline rate; character modifiers apply | Compare candidate production against this game quantity, never label it measured calories |
| Framework 0.16.0 | Construction, saved state, transfers, observations, measured electrical receipts and gas/thermal helpers exist | Reuse these specific services |
| Framework `ProcessJob` | Fixed mass-balanced batches, maximum duration 3,600 seconds | Keep crop lifecycles content-owned and preserve existing processing contracts |
| Shipbreaker `IndustryService` | Discovery and dispatch explicitly recognise its own equipment | A shared provider interface is a concrete new need, not an existing plugin registry |
| Ship's Water 0.16.1 | Installed potable vessels expose `StatLiqH2O` in litres and `TIsWaterVesselInstalled` | A same-ship finite adapter is plausible; no stable public fluid API was verified |
| Installed OCF 0.8.71 adapter | Loaded-plugin detection, same-ship potable-tank selection and checked finite debits | Existing integration precedent; do not make OCF a requirement or copy its whole crafting system |

Native findings above are read-only observations of Blue Bottle Games' Ostranauts.
Joshu's [Official Ostranauts Modding Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946)
documents the JSON content categories and mod structure; the individual food and
planter values here come from the inspected installation, not that guide.
Native record paths above are relative to the installed game's data directory.
Framework sources are in [Processing](../src/PhobosFramework/Processing),
[Persistence](../src/PhobosFramework/Persistence) and
[Controls](../src/PhobosFramework/Controls). See the
[author guide](framework-author-guide.md) for current APIs.

The installed OCF package credits **Crafting Framework contributors**. Its
[Ostranauts Crafting Framework listing](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573443)
identifies the provider; the adapter behaviour in the table is local inspection
evidence. No upstream code or binaries were copied. Existing physical-hauling
compatibility should reuse **LOGUSS's**
[Common Sense Salvage and Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481217)
where suitable; agriculture compatibility remains untested.

Valtora's [Ship's Water documentation](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
describes finite tanks, potable-water refuelling and approximately 60% wastewater
recovery by default, using a consumable filter. That supports reuse for water
supply; it does not prove compatibility with crop drainage, soluble fertilizer
or nutrient recovery. Keep contaminated process water out of potable tanks.

No installed package supplies cultivation. Public searches did not establish a
compatible growing mod to extend. The author description for
[FFU Space Engineering](https://thunderstore.io/c/ostranauts/p/FFU_Group/FFU_Space_Engineering/)
lists hydroponics as under consideration and food fabrication as planned; that
is not evidence of a usable farming provider. The public Workshop search page
could not be retrieved in this pass. This is a bounded survey, not a claim that
no unpublished, newer or differently named mod exists.

## Science informing the design

| Named reference | Supported finding | Application and limit |
| --- | --- | --- |
| [NASA: Station Science 101, Plant Research](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/) | Space experiments investigate root water/air delivery, hydroponics, aeroponics and successive plant generations | Contain irrigation and supply root air; ordinary gravity-fed open troughs are not our assumption |
| [R. M. Wheeler and colleagues, NASA Biomass Production Chamber study (1996)](https://ntrs.nasa.gov/citations/20040089951) | Reported lettuce crops took 28–30 days and potato crops 90–105 days under the study's conditions | Proposed 48/96-hour game crops are explicit time compression, not scientifically demonstrated growth rates |
| [ESA MELiSSA: closed-loop concept](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Closed_Loop_Concept) | Recovery involves multiple biological and physical treatment processes | Keep cultivation, waste treatment and nutrient formulation distinct |
| [ESA MELiSSA: photoautotrophic compartment](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Compartment_IV_The_photoautotophic_compartment) | Studies higher plants and a separate Arthrospira compartment, including biomass and environmental response | Algae is a later specialised process; it is not interchangeable with lettuce trays |

Lighting energy, viable stock, water, carbon and nutrients remain necessary even
with accelerated growth. Nitrogen, phosphorus, potassium, calcium, magnesium,
sulfur and trace nutrients need a formulated supply; cabin nitrogen is not
automatically available fertilizer. Raw salty asteroid material is not a complete
nutrient mix. The [asteroid study](asteroid-life-support-research.md) records
NASA's Bennu evidence and the distinction between occurrence and useful extraction.

The rack depends on cabin conditions, with roots and liquid contained in a
recirculating capillary-supported cassette. It does not create a pressure-tight
greenhouse in a vacuum. Lamps use their own received power, not room brightness.
Microgravity delivery, variable acceleration and thermal rejection are engineering
requirements, not benefits established by a successful offline calculation.

## Balance findings

The [first-slice specification](agriculture-first-slice.md#candidate-resource-budgets)
defines a deliberately simplified, conserved design budget. It accounts for
seed mass, carbohydrate-equivalent growth, retained nutrient solids, water,
gases and heat. It is **not a full elemental assay, nutrient formulation, crop
kinetic model or prediction of human nutrition**.

Under the proposed defaults and ideal conditions, including one hour of rack
turnaround:

| Farm | Baseline crew | Nominal food-debt coverage |
| --- | --- | --- |
| One potato rack + one lettuce rack; supplied lettuce seeds | 1 | 59.71% |
| Same farm | 3 | 19.90% |
| Two potato racks + one lettuce rack; supplied lettuce seeds | 1 | 111.26% |
| One potato rack + one lettuce rack; own lettuce seed crops | 1 | 56.46% |
| Two potato racks + one lettuce rack; own lettuce seed crops | 1 | 108.01% |

These are long-run arithmetic ceilings from our proposed food values, not measured
crew survival, calorie coverage or a complete diet. They exclude startup food,
stress, faults, resource shortages and seed-bank reserve margin. A self-seeding
lettuce rotation consumes growing time: one seed crop supports three subsequent
food sowings after preserving one packet to repeat seed production.

The first potato food arrives after roughly four game days, plus handling/cooking.
At the unmodified ticker rate, one person needs 96 food-debt units supplied from
stores over those four days: about twenty ordinary five-unit rations before
allowing for real appetite, traits and scheduling. Agriculture is an investment;
it does not rescue a crew already out of food.

The useful next step is a small gameplay slice. If owner testing finds this
waiting period or tending loop unrewarding, change the documented time scale or
simplify the crop roster. Do not add more species to hide a weak first loop.

## PixelLab and provenance

The owner selected [PixelLab](https://www.pixellab.ai/) as the preferred plant-art
candidate, following its earlier use in their game project. Its
[style-reference documentation](https://www.pixellab.ai/docs/tools/consistent-style)
and [editing documentation](https://www.pixellab.ai/docs/tools/edit-image-pro)
support a related sprite family, but actual consistency needs visual inspection.

The [published API estimates](https://www.pixellab.ai/pixellab-api) inspected on
25 September 2026 include US$0.00738 for a transparent 64 × 64 Bitforge image
and US$0.095 for a single-direction object state up to 168 × 168. These are
different operations, not interchangeable quality levels or a quote for the
owner's subscription. A Pro Flash tool quote did not establish a verified USD
conversion; do not interpret its numeric cost field as dollars.

The specification includes a potato-stage pilot, layer registration, resolution
and cost recording. **No paid generation was initiated.** Preserve provider
terms and attribution separately from scientific citations; a scientific reference
does not grant rights to its illustrations or imply NASA/ESA endorsement.

## Verification completed and limits

- Refreshed installed metadata and selected current native definitions; inspected
  the present Framework and console boundaries.
- Reproduced the proposed cycle/farm budgets with
  `python scripts/calculate-agriculture-budget.py --compact`.
- Six offline calculator tests cover mass/energy conservation, recovery,
  supply and pace sensitivity, hardware limits, propagation costs, crew scaling
  and invalid assumptions. Run
  `python -m unittest discover -s tests -p test_agriculture_budget.py -v`.
- Reviewed the named primary science and provider sources above.

The calculator's partial-power timing is an ideal upper-bound schedule, not a
living-crop simulation. Crop damage, interrupted harvest commits, native food AI,
save restoration, fluid transfers and owner controls have **specified acceptance
scenarios**, not passing runtime tests. See the
[validation matrix](agriculture-first-slice.md#validation-and-owner-gameplay-scenarios).
Agriculture packages, new Framework interfaces and plant sprites remain unbuilt.
