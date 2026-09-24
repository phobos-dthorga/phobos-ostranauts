# Chemical storage, process fluids and industrial hazards

Owner direction and bounded research: **2026-09-24**. These are **future features**,
not implemented tanks, chemical recipes, refuelling services or new hazards. This
note does not change current Shipbreaker recipes or the priority of existing work.

## Owner requests

- Install shipboard chemical/solvent silos or reservoirs whose contents are
  required for suitable production processes or improve suitable salvage work.
- Make leaks, damage and explosions capable of releasing consequential hazardous
  vapours/gases; consider custom species if the engine permits them.
- Buy bulk supplies by quantity at station refuelling terminals, following the
  installed Ship's Water mod's approach rather than creating another shop system.
- Use **Phobos Framework throughout where appropriate**, keeping reusable
  behaviour available to other content mods instead of duplicating it.
- Document and lightly research this now; implement the gameplay later.

## Verified findings and limits

| Inspected evidence | What it establishes | Limit |
| --- | --- | --- |
| Ship's Water 0.16.1, `ShipsWater.KioskPatch` | Patches native `GUIStationRefuel.SetupFields` and `OnSubmit`; configures `rowLifeBlack` for potable-water purchases and deposits into installed tanks after observing payment | This is a specific water integration, not a verified public generic commodity API or a transaction implementation to copy without review |
| Native `FluidStrings` | A fixed initial species list includes methane, hydrogen, ammonia, carbon monoxide and smoke, alongside oxygen, nitrogen, water and other entries | Presence does not establish complete combustion, exposure, rendering or equipment compatibility for every species |
| Native `GasContainer.GetGasMass` | A switch converts known species from moles to mass; unmatched names return zero in the inspected implementation | A new JSON gas name alone would not have correct mass handling |
| Native `gasrespires/gasrespires.json` | Human exposure entries exist for CO, CO2, NH3, H2SO4 and Smoke, with associated condition effects | Proposed tank accidents and all chemical exposure routes are not verified |
| Native gas save handling | Gas changes and gas-related persistent conditions have native storage mechanisms | End-to-end persistence and simulation of arbitrary custom species remain unverified |
| Native `explosions/explosions.json` | Named definitions configure explosion effects, including damage, radius and optional stat effects | This is not automatically a mass-balanced tank rupture or chemical-reaction model |

The [Ship's Water author listing](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
also documents potable-water purchases by kg at refuelling kiosks. Preserve its
water entry and provider-owned tanks. Separate chemical rows/services need their
own integration; do not overwrite the water row or assume unlimited unused rows.
Water's unit conversion is not a universal liquid-density rule.

Native species are the preferred first route. Custom solvent vapour requires
checking species lists, mass/pressure calculations, transfer, saves, exposure,
sensors, filters and presentation together. C# extension appears plausible, but
no generic gas-registration API was established. Native smoke and the H2SO4
atmospheric entry are game representations, not evidence of complete phase chemistry.

## Suggested gameplay directions

These suggestions are not settled balance or registered items.

| Idea | Useful choice for the player |
| --- | --- |
| Bulk solvent, process-water, reagent and compressed-gas reservoirs | Trade endurance and throughput against space, carried mass and maintenance |
| Separate clean supply, contaminated return and reject storage | Choose when to reclaim supplies or replenish at a station |
| Optional cleaning or conditioning for eligible salvage | Spend fluid and energy for a defined improvement while retaining mechanical processing |
| Required reagents for actual chemical treatment | Select processes according to available inputs and useful outputs |
| Quantity-based station replenishment | Fill compatible installed storage; seek industrial ports for specialist supplies if justified |
| Solvent recovery using electricity and cooling | Reduce station dependence while retaining contaminants and replenishing losses |
| Isolation valves, compatible linings, leak detection and containment | Make maintenance and damage control worthwhile |
| Pressure/temperature warnings and emergency isolation | Recognise and interrupt developing failures |

Keep a silo aesthetic where appropriate, but use **sealed, positively fed
reservoirs** suitable for microgravity rather than gravity-fed hoppers. Set actual
footprints, mass and capacity before producing usable saved machinery; this idea
record does not choose them.

Industrial chemicals must not silently enter potable plumbing or consume crew
drinking reserves. Explicit supply/return links and suitable reserve settings can
let players prioritise crew needs. A future Ship's Water adapter must distinguish
clean product water from contaminated process liquid.

A useful solvent loop is supply → process → contaminated return → recovery →
reusable solvent plus retained waste. Distillation and condensation are established
reclamation processes, but suitability and recovery depend on the mixture. They
provide process precedent, not a space-qualified appliance or universal yield.
[EPA AP-42, Waste Solvent Reclamation](https://www.epa.gov/sites/default/files/2020-10/documents/c4s07.pdf).

## Hazard design and material accounting

Tie consequences to actual contents, damage and operating conditions. Leaks,
pressure rupture, ignition and chemical incompatibility are distinct failure
modes; not every damaged reservoir should explode. Appropriate vapours, toxic
contamination and smoke can reward containment and intervention without unexplained
random punishment. Prefer native atmosphere and damage behaviour over a parallel
simulation where it can represent the intended result.

An accident releases or transforms finite stored material. Do not create arbitrary
bonus chemicals, count contents simultaneously in tank and room, or leave a full
tank after releasing its inventory. Distinguish remaining contents, captured
spill/reject material and explicit discharge. Reaction products must follow the
declared feed and reactants; energy cannot supply missing elements. Existing
unclassified panel residue remains unclassified under the
[residue contract](residue-material-contract.md).

Normal process accounting:

`feed + consumed process inputs = useful products + returned fluid + retained waste + explicit discharge`

Recovery consumes energy and needs a heat destination. Renaming dirty solvent
does not remove contamination. Full storage, unsupported mixtures and missing
adapters should retain material, pause the dependent operation and explain why.
Do not globally change another provider's overflow or failure policy to make our
integration work. Existing mechanical processing gains no blanket chemical dependency.

## Phobos Framework responsibilities

The inspected **0.6.0 Framework API baseline** already supplies definition
registration, construction, maintenance helpers, additive equipment merchant
stock and saved endpoint pairing. Reuse those services for new machinery. Solid-
item transfer and completion helpers provide patterns; they do not already move
bulk liquids or atmospheric gases. Equipment `MarketStock` is not a refuelling API.
See the [Framework author guide](framework-author-guide.md).

Add shared capabilities **when concrete features require them**:

| Shared responsibility | Boundary |
| --- | --- |
| Resource identity, capacity, contents and persistence | Preserve quantities, mass and saved meaning; explicitly convert kg, litres and moles |
| Compatible fluid endpoints and checked transfers | Retain contents on full/invalid destinations and allow explicit source/receiver selection; do not claim current solid-port contracts already handle fluids |
| Station bulk-commodity integration | Register additional services and coordinate actual delivery with payment, preserving native and other-mod terminal entries |
| Native gas and optional provider adapters | Centralise integration checks and diagnostics; preserve provider-owned state instead of maintaining duplicate balances |
| Operation and status services | Right-click controls, settings and F3 commands delegate to the same gameplay checks and mutations |
| Shared failure mechanisms where needed | Reuse native behaviour first and extract common code when concrete consumers establish a shared need |

**Content mods retain ownership** of chemical identities and recipes, artwork,
dimensions, prices, yields, material compatibility and substance-specific hazards.
They declare rules to shared services where needed; Framework must not hard-code
Shipbreaker's chemicals or depend on its content. No public fluid API signature,
generic reaction engine or new simulation is promised here.

Ship's Water and other adapters remain optional unless a particular content module
deliberately requires a provider. If an adapter becomes unavailable, retain contents
and stop dependent operations rather than replacing tanks or inventing equivalent
stock. This is not a promise of save recovery if a required provider owning saved
objects is removed. Follow the [dependency contingency policy](dependency-contingencies.md).

## Deferred decisions and future verification

Choose exact chemicals/mixtures, tank footprints and capacities, prices and port
availability, energy/heat/work budgets, yields and hazard thresholds in the relevant
implementation round. Prefer an existing gas when it provides the intended gameplay.
Start with one useful stored fluid, one process and one compatible station service;
recheck native and Ship's Water hooks and patch coexistence at that time.

Future integration checks should cover:

- Purchase units/prices, absent/full/part-filled tanks, insufficient funds, cancelled
  purchases and partial/interrupted delivery without duplicate charges, duplicate
  material or paid-but-undelivered loss.
- Native and Ship's Water terminal entries remaining usable alongside our service.
- Incompatible liquids/endpoints, contents moving during work, missing providers
  and actionable pause/resume states.
- Fluid/gas mass, finite release during damage, retained waste, relevant exposure
  and filtration, and no repeated release from exhausted contents.
- Save/reload of contents, composition, links and unfinished operations, preserving
  recipe versions and the meaning of older saved material.

These concern our new logic, not isolated re-proof of established native tank or
power patterns. This round needs source, link and consistency checks only; no build,
gameplay test, installation or save change is needed.

## Evidence, versions and attribution

The owner's screenshots identify **Ostranauts 1.0.1.4**. The local Steam manifest
inspected for this note records **build 25401711**. Hashes below identify the exact
files inspected; this is not a new in-game version or compatibility result. Older
research used a different assembly hash, so its snapshot is not interchangeable.

Ship's Water metadata/DLL report **0.16.1 / 0.16.1.0**, Workshop item **3757331189**,
by **Valtora** (local author metadata spells this **Valtorra**). Its kiosk code was
read for precedent, not copied into the repository. No reusable public API or
licence grant was established in this review. Preserve project attribution and
licensing policy if later adaptation is appropriate. No upstream code or binary
is distributed by this note.

Local source locations, relative to the installation or native data root:

- `Ostranauts_Data/Managed/Assembly-CSharp.dll`: `FluidStrings`, `GasContainer`,
  `CondOwner` gas persistence; `GUIStationRefuel` is the patched terminal type.
- `gasrespires/gasrespires.json`, `conditions/conditions.json`: species, persistent
  gas conditions and exposure entries.
- `explosions/explosions.json`: explosion-effect definitions.
- Installed `ShipsWater.dll`: `ShipsWater.KioskPatch`, including the water row,
  installed-tank selection, capacity accounting and purchase handling.

SHA-256 snapshot, 2026-09-24:

| File | SHA-256 |
| --- | --- |
| Assembly-CSharp.dll | `91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e` |
| ShipsWater.dll | `f236324a1c7732a2cefb81daac9de9167b31b3fa6fe646e32f88ac8702f4fc04` |
| gasrespires.json | `9b9d14ea9f67a8ca72a033626cd928bd595d81add3ee1340577e586015d4a46a` |
| explosions.json | `f0f9f8e71675d6a40d9fdf8910b7a27ec0ce0a0ebb1a278ebb59f7be391e1e77` |

Related: [industry roadmap](fusion-industry-roadmap.md),
[fusion furnace and chamber controls](fusion-smelter-research.md),
[processing expansion](shipbreaking-material-processing-research.md),
[asteroid life-support resources](asteroid-life-support-research.md),
[explicit material-port pairing](material-port-pairing.md).
