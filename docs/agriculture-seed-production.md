# Lettuce seed production — Agriculture 0.7.0

The Firstlight-4 now offers **Plant lettuce for seed**, a separate cohort chosen
when planting an empty rack. It consumes one existing 5 g Continuance lettuce
seed packet. Ordinary food lettuce remains unchanged and does not return seed.

## Operation and authored budget

Planting takes 15 minutes of native crew work. At the default pace, maintain
water, nutrients, atmosphere and received power for 96 game hours at 0.4 kW
(38.4 kWh). GrowthDurationMultiplier is captured at planting; slower growth does
not reduce total required energy. Use manual supplies or select the W2's
**lettuce seed-production solution** before pairing/receiving. Empty incompatible
solution and line contents before switching. Food-lettuce feed is a different
profile and cannot be used for a seed cohort.

| Ideal complete seed cycle | Amount |
| --- | ---: |
| Starting planting stock | 0.005 kg |
| Root water consumed | 1.356 kg |
| Nutrients consumed | 0.010 kg |
| Net carbon fixation surrogate | 0.0725 kg carbohydrate equivalent |
| Released vapour | 0.200 kg |
| Final retained biomass | 1.200 kg |
| Immediate healthy harvest | Four 0.005 kg seed packets + 1.180 kg residue |

Harvest takes the existing 30 minutes of crew work. The seed cycle produces
**no edible lettuce portions**. Retain one packet for the next seed run; the
other three can start food crops. This reservation is a player decision, not
an extra automatic packet. Respiration, poor health and whole-packet rounding
can lower the harvest; all remaining tissue stays in residue. Clear returns
residue only. One rack still represents one cohort across four tray pictures.

At authored base values, consumed Groundwork irrigation is 13.56 cr, nutrients
15 cr and planting seed 5 cr: 33.56 cr before power, crew, equipment and losses.
The four packets have 20 cr combined base value. Propagation is an endurance
option with a space/time cost; it is not designed as a guaranteed seed-sale
profit. Replanting a harvested packet is not simultaneously a sale.

## Scientific basis and limits

**Dr Greg Welbaum, Virginia Tech**, [Vegetable Seed Production: Lettuce (2005)](https://welbaum.spes.vt.edu/seedproduction/lettuce.html),
describes self-pollination, the later flowering/seed-ripening stage and dry pappus
at harvest. This supports a distinct reproductive cycle and yellow-flower/pale
seed-head artwork. His real-world maturation and dormancy guidance is not our
96-hour gameplay timer.

**Tere Davidson, Melissa DeSa and Héctor E. Pérez, University of Florida IFAS**,
[A Beginner's Guide to Producing and Saving Open-Pollinated Seeds](https://ask.ifas.ufl.edu/publication/EP647),
describe continued care through flowering, mature seed development and drying.
Our harvest action abstracts cleaning, drying and readiness for replanting; it
does not simulate dormancy, genetic diversity, seed viability storage or cultivar
selection. All above masses, gas surrogates, accelerated timing, yield and prices
are authored gameplay, not university measurements or institutional endorsement.

## Saves, artwork and owner checks

Existing potato and food-lettuce profile IDs and budgets are unchanged. The new
`lettuce-seed` cohort and `lettuce-seed-v1` feed use the existing checked saved
state; reload preserves quantities/progress but pauses operation and receiving.
Do not downgrade a save containing the new profiles to an older Agriculture.

Early and stressed stages reuse the registered lettuce layers; two new PixelLab
masters supply flowering and ripe seed heads. See [artwork notes](../assets/phobos-agriculture/README.md)
and [generation records](../assets/phobos-agriculture/seed-generation-records.json).
World and panel derive the same stage from saved crop state without changing it.

Offline checks cover complete/manual and mixed-feed cycles, gas/mass/energy,
health, clearing, repeat harvest prevention, persistence and stage selection.
Owner checks: plant the seed option; save midway and Resume; try incompatible
feed and a full output inventory; harvest and replant an actual returned packet;
compare late stages in the world and panel. These gameplay checks remain pending.
