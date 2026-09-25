# Phobos Agriculture 0.2.0

First gameplay candidate, prepared on 25 September 2026. Requires **Phobos
Framework 0.17.0**. The offline checks pass; the owner still needs to evaluate
the complete loop in Ostranauts. Packages are prepared, not installed.

## Equipment and supplies

**Verdemorrow Agronomics** supplies this agricultural family: **Firstlight-4**
cultivation racks, **Hearth-2** galley cookers, **Continuance** seed potatoes and
lettuce seeds, and **Groundwork** formulated nutrients. Harvested produce and
retained materials also carry the Verdemorrow brand. Full names begin with
`Phobos' Verdemorrow`; the [brand register](equipment-branding.md) lists them.

Buy equipment, planting stock and formulated nutrients from the supply kiosk,
fixer or suitable general trader after stock refresh. Availability is additive
and probabilistic. Alternatively, use Framework construction at an ordinary
Bar/Dining Table, with the required screwdriver and soldering tools:

| Equipment | Construction inputs | Work / base price |
| --- | --- | --- |
| Phobos' Verdemorrow Firstlight-4 Cultivation Rack | 48 steel scrap, 24 aluminium scrap, 12 small mechanical parts, 4 small electrical parts | 60 minutes / 9,000 cr |
| Phobos' Verdemorrow Hearth-2 Galley Cooker | 6 steel scrap, 4 aluminium scrap, 2 small mechanical parts, 2 small electrical parts | 20 minutes / 2,400 cr |

Install the **4 × 4 rack** and **2 × 2 cooker** on cabin floors and connect their
power points. Keep room temperature at **18–26 °C** and pressure at **70–110 kPa**
for growth. These are authored gameplay limits, not universal plant tolerances.
The rack needs atmospheric CO₂. Lamps consume electricity and warm the cabin;
ventilation and cooling remain ship responsibilities.

## Living rack visuals

Firstlight-4 now shows potatoes or lettuce in its four trays: sprout, young,
mature, harvest-ready, wilted or dead. These four pictures represent **one crop
cohort**, not four separately plantable slots or four times the yield. The local
panel shows the same composed rack. Growth and health drive the artwork; pausing
the lamps leaves the plants visible. Successful harvesting/clearing empties the
trays. A protected unreadable state shows the base rack; consult its panel warning.

The rack has a new sage-green and cream housing. Hearth-2 now includes matching
galley furniture beneath its separate electric stove insert. The fittings are
part of each machine's housing; the counter is not another buildable object.
The footprints remain 4 × 4 and 2 × 2. Appearance refreshes within about two real
seconds while the ship is loaded; it does not advance growth or resume automation.
Lettuce's harvest-ready image depicts food leaves, not seed production.

## First crop

1. Put one **Continuance 0.2 kg seed potato** or one **Continuance 5 g lettuce seed packet** in the rack's
   normal Inventory. Supplies must be separate, unstacked items.
2. Put native **LiquidWater** rations and **Groundwork** nutrient packets there too.
   Use **Control Panel → Load one water ration / Load nutrients**. Each ten-second
   crew action loads one 0.25 kg water ration or one 40 g packet. The finite
   reservoirs hold 20 kg water and 0.5 kg nutrients. Initial ideal potato cycle:
   at least 19 water rations and one nutrient packet; lettuce: six rations and
   one packet, with unused water/nutrients retained.
3. Choose **Plant**. Fifteen minutes of local crew work consumes the stock and
   starts automatic cultivation. The panel shows crop progress, health, retained
   quantities, native room readings and their compartment source.
4. Keep water, nutrients, CO₂, power and cabin conditions available. You can leave
   the panel closed. Pausing lamps does **not** freeze respiration or stress.
5. At readiness, choose **Harvest and retain stock**. Thirty minutes of crew work
   produces physical cargo only if the complete output fits the rack inventory.
   Remove cargo and replant. Full storage keeps the crop intact; harvest again
   after making room.

| Ideal complete cycle | Potatoes | Lettuce |
| --- | --- | --- |
| Duration | 96 game hours | 48 game hours |
| Active electrical demand | 0.75 kW | 0.40 kW |
| Cycle energy | 72 kWh | 19.2 kWh |
| Water / nutrients | 4.624 kg / 40 g | 1.2658 kg / 5 g |
| Immediate ideal harvest | ten 0.4 kg raw portions, one 0.2 kg seed potato, 0.8 kg residues | four 0.25 kg edible portions, 0.2 kg residues |

Delays and respiration reduce biomass, and damage reduces edible output. Whole
portion rounding can reduce a delayed harvest by one portion; the remainder stays
in residue, never disappears. Lettuce harvests currently give **no replacement
seed**. Buy more seed until its separate reproductive lifecycle is implemented.

The Hearth-2 cooks **one 0.4 kg portion per Start**, at 2 kW for 90 seconds at full
supply. Load raw potatoes, start, then collect the cooked portion. Partial supply
slows cooking. Removing the bound input suspends the cycle; return that exact
portion or Cancel before using another. Cancellation discards cooking progress,
not the food. Cooked potatoes reduce native food debt by five units; lettuce by
one. These values, yields and accelerated growth are gameplay choices.

## Water and central controls

With **Valtora's [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
0.16.1** loaded, Enable optional ship-water supply draws finite potable water from
eligible installed tanks on the same player-owned ship. Powered pumping preserves
the configured crew reserve (10 L by default). Docked ships are separate.
Other provider versions fall back to manual supply pending contract review.
Its water quantities use the provider's litre accounting; we leave its native
vessel-mass policy unchanged. Agriculture's own contents contribute native mass.
The adapter's gameplay compatibility is still unverified.

With Shipbreaker 0.14.0, the C1 console discovers agriculture equipment and exposes
the same checked Start/Pause/receiving commands. Physical loading, planting and
harvesting stay local. Auto Nav operates independently; shortages affect plants
without Agriculture commanding flight.

Local F3 equivalents: `phobosagriculture list`, then
`phobosagriculture status <full object ID>`. Replace `status` with `start`, `pause`,
`receive`, `pause-receive`, `plant-potato`, `plant-lettuce`, `load-water`,
`load-nutrients`, `harvest`, `clear`, `drain` or cooker `cancel`.
F3 uses ordinary access and resource checks.

## Interruptions and maintenance

Reload keeps crop identity, health, material, growth, captured pace and the bound
cooking portion. **Resume cultivation/cooking and receiving separately.** No
unobserved catch-up growth is granted. Loaded, stopped or damaged plants still
respire and deteriorate; unloaded ship time is not simulated in this candidate.
Continuous shortages have a two-hour grace, then progressive stress. Restoring
conditions stops further stress but does not magically restore lost health.

Clear failed crops into retained residue; Drain unloads water plus unused nutrients
as non-potable process solution. Neither has a recovery recipe yet. Empty physical
inventory and numeric contents before uninstalling/dismantling; cancel cooking
progress first. Ordinary repair/Restore use native maintenance. Dismantling returns
dry housing waste with negligible value; it does not create valuable extra metal.

Configuration: `GrowthDurationMultiplier` (0.5–2, captured when planting; total
cycle energy unchanged) and `CrewReserveLitres` (live). No saved identity, footprint
or recipe mass changes when these preferences change.

## Research and next steps

NASA's Ray Wheeler motivates the staple-plus-vegetable crop pair in
[NASA's crop research](https://www.nasa.gov/science-research/nasa-plant-researchers-explore-question-of-deep-space-food-crops/).
His [life-support overview](https://ntrs.nasa.gov/citations/20205008786) explains
why productive area and lighting matter. Our compact racks and short cycles are
not NASA yields. [ESA's MELiSSA concept](https://www.esa.int/Enabling_Support/Space_Engineering_Technology/Melissa/Closed_Loop_Concept)
informs later recovery stages; indefinite habitation is an ambition, not a feature
of this first mod. Neither institution endorses the project.

See [implementation and owner checks](agriculture-implementation.md),
[research](agriculture-research.md) and [roadmap](agriculture-roadmap.md).

Build with `scripts/build-agriculture.ps1 -OstranautsPath <local game folder>`.
After exiting the game, use `scripts/install-mods.ps1 -Mods Agriculture -WhatIf`,
then the same selection without `-WhatIf` when ready. It includes Framework.
Select `Agriculture,Shipbreaker` to include the prepared C1 integration update.
Use `-VerifyOnly` afterwards. See [installation](installing-mods.md).
