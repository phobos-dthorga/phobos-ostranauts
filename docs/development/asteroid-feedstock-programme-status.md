# Asteroid feedstock programme: status and remaining stages

Status record, **30 September 2026**, written at the owner's request before the
programme pauses for the [schema separation audit](schema-separation-audit.md).
It records what the programme delivered, what the owner decided along the way,
what remains in each later stage, and what changed underneath the plan. The
research behind every item is in [asteroid feedstocks: gaps and possibilities](asteroid-feedstock-gaps.md);
the chemistry is in [the refinery record](manufacturing-refinery-and-chemistry.md).
Nothing here is gameplay-validated; owner in-game checks are listed per guide.

## Delivered

| Commit | Version | What it delivered |
| --- | --- | --- |
| 3a8de52 | Framework 0.48.0, Manufacturing 0.8.0 | `AdditiveLoot.CarveChoice`: new loot enters the game's tables by carving a share from an existing entry, rendered from the originals so two mods can share one donor. Clay hydrates migrated to a carve (0.10 of the C-class roll from silicates), ending its double roll. |
| f0ca6ce | Shipbreaker 0.43.0 | Phobos' Rivetline Y2, Y3 and Y4 material bins: native item-grid containers (4 x 4, 6 x 6, 8 x 8 cells; 2 x 2 to 4 x 4 tiles) admitting every mined ore, mineral and ice through a trigger that ANDs the game's own container and mining-output rules. Crew treat them as stores without new code. PixelLab art. |
| 2a0240d | Shipbreaker 0.44.0 | Ice supply: the game's own `ClusterI01` ice cluster carved into the C- and S-class field pickers (new games), and extra water ice carved into C-class deposits (existing saves too), both behind settings that restore the native tables when off. |
| f2f72da | Shipbreaker 0.45.0 | Methane ice in the T2: `ItmIce02` (24.84 kg) thaws into 19.89 kg of water and 2.95 kg of methane for a linked methane store (Circone et al. 2005, USGS n = 6.0; Handa 1986 dissociation enthalpy), with its own port pair and a product-driven finish. The native methane ice price was corrected from $20 to $250 so the old value-loss rule held. |
| 0fa8b4f | Manufacturing 0.9.0 | Nitrogen, part 1: the ammonium salt crust (a C-class carve, 0.05), the V4 charge that heats it into 0.955 kg of ammonia, water, CO2 into the room and a terminal salt cake, the Fennmark Q2/Q3/Q4 ammonia stores, V4 stored-gas outputs, and ammonia as RCS propellant through the P1 (owner decision). |
| 335dfaa | Manufacturing 0.10.0 | Nitrogen, part 2: the Phobos' Tolvane AX-2 Ammonia Cracker, 1 kg of ammonia an hour into 0.822 kg of nitrogen and 0.178 kg of hydrogen for linked stores; Tolvane is the first new brand under the crowded-brand direction. |
| 1b4e686 | AGENTS.md | The refining value-loss rule retired (see below). |

Round one (loot policy, bins, ice) and round two (methane ice, nitrogen) are
complete as planned, with two deviations recorded in the research record: the
nitrogen chunk became a salt crust because measured Bennu nitrogen ruled out clay,
and the cracker brand became Tolvane because the proposed Azomere sits close to
a real fertiliser company.

## Owner decisions taken during the programme

- Bins are native item-grid containers owned by Shipbreaker (Rivetline).
- Spawn the game's own ice clusters, behind a setting; carve extra deposit ice so
  existing saves benefit.
- New chunks carve a share from an existing loot entry, never add a roll.
- Correct methane ice's native price in place (now a review candidate, below).
- Maximum creative freedom: save-removal safety is later engineering, never a reason
  to shrink a design.
- Invent a new brand where an existing one is crowded.
- The RCS burns ammonia (player flexibility, with the poisoning warnings kept).
- The refining value-loss rule is retired (30 September); refining may gain value
  where the work is real. Recorded in AGENTS.md under "Refining value".

## Remaining stages

### Stage A: refining-value review (done, Manufacturing 0.15.0 / Shipbreaker 0.49.0)

The retired rule's checks are replaced by the guardrails recorded in AGENTS.md
"Refining value" (agent-applied, open to owner revision): sellable products of a
charge within 1.5 x its inputs; a quarter at most when every input is bought
stock; commodity records valued for information only; stock priced between
scrap steel and its ore per kilogram. Outcomes: the nickel-iron ingot returns to
20 cr (carburising gains eleven percent at base prices; neither input is sold by
any merchant, so there is no loop); the
methane ice price correction is withdrawn and the game's 20 stands (the thaw
gains water and methane aboard, which nothing sells back); carbon, the clay and
salt crust chunks, F6 casting and the K2/AX-2 conversions keep their prices, each
now justified by real work rather than a loss. Round three prices its salts
under the same guardrails.

### Round three: salts, sulfur and fertiliser (B2, B3, D3)

Research done 30 September 2026: [the round-three design record](feedstock-round-three-design.md)
gives the sourced chemistry, an authored evaporite chunk, four worked recipes, the
Lixivar LC-3 design and five decisions for the owner.

Owner decisions (30 September 2026): the Lixivar LC-3 name, the authored chunk and
the formulation priced at Agriculture's own packet price are approved; the kiosk
should also sell bulk fertiliser, which the owner chose to deliver as a Groundwork
nutrient hopper the refuelling kiosk fills and a W2 doses from; both struvite
routes, with sulfur and sulfuric acid, the second from a meteorite sulfide-phosphide
nodule; and the V4 service refactored into a shared charge engine rather than
copied. The plan runs in five phases:

| Phase | Delivered | Versions |
| --- | --- | --- |
| A | Shared charge engine, equipment pack, recipe working volumes and reaction heat, commodity settlement | Framework 0.54.0, Manufacturing 0.17.0 (2e66342) |
| B | Lixivar LC-3; evaporite crust (C-class carve, 0.05); evaporite leach, struvite from the crust's phosphate, makeup formulation with Agriculture; V4 calcine of the leached residue into a CO2 store | Manufacturing 0.18.0 |
| C | Groundwork E2, E3 and E4 nutrient hoppers; kiosk crop nutrients at 1,500 cr/kg; W2 dosing from a hopper; bagging back into charges | Agriculture 0.27.0 |
| D | Lixivar acid tanks, kiosk acid, sulfide nodule, SA-3 acid plant | pending |
| E | Epsom salt from olivine, ammonium sulfate, acid-route struvite, complete formulation into the hopper | pending |

- Research first: the evaporite mineral fractions from McCoy et al. 2025 (NASA
  OSIRIS-REx) for an evaporite crust chunk.
- One new hydrometallurgy machine under a new brand (working name *Lixivar*, from
  lixiviation): water from a linked vessel, explicit recipe choice on the F6
  `SelectRecipe` pattern. Recipes: evaporite leach to potassium, phosphate and
  sulfate salts with sodium chloride as the terminal remainder; later olivine plus
  acid to magnesium sulfate; fertiliser formulation.
- Carbonate CO2 goes to a CO2 store through the V4's stored-gas outputs (delivered
  in 0.9.0), feeding the K2.
- Fertiliser: a formulation that consumes the nitrogen carrier (ammonia from the
  Q stores) and the salt carriers in a declared ratio and outputs Agriculture's
  existing Groundwork Makeup packet, gated on Agriculture being present. No
  Agriculture code change; no single chunk ever becomes "complete fertiliser".
  Agriculture's aggregate nutrient model (no N/P/K split) is the constraint.
- Sulfur (B3) only if acid leaching needs it: troilite as a new chunk or a new V4
  revision; SO2 stays inside the machine record (not a game gas).

### Round four: carbothermal oxygen (D2)

A large high-temperature reactor under a new brand (working name *Oxsmith*):
silicate or olivine plus methane to CO, hydrogen and reduced metal or slag;
methanation back to methane needs a CO store family and a methanation step; the
X2 turns the water into oxygen. Needs a real power and heat budget and the
one-hour charge limit revisited. Source practical extraction values from NASA's
Carbothermal Reduction Demonstration before balancing.

### Round five and later

- Tungsten-carbide tooling (D5) with the M4 mill from the Manufacturing roadmap.
- New asteroid types (C1 ammonia-bearing dark rock, C2 brine-altered rock) with
  their own walls, veins and PixelLab tile art, shipped as native
  `data/blueprints/{asteroids,clusters}` files and carved into the asteroid tables.
- Removal safety and existing-save reach: a Framework fallback where a saved
  asteroid names a missing cluster blueprint; a spawner that seeds Phobos fields
  into existing saves; an uninstall helper only with explicit authorisation.
- E1 oxygen candles from halite, E2 regenerable scrubbing, F1 heavy water: only
  when play shows the need.

### Small leftovers

- Admit Phobos ingots, carbon stock and remainders to the Rivetline bins (a Phobos
  stock condition alongside the mining-output trigger).
- Owner in-game checks for everything above: the per-mod guides list them
  ([Shipbreaker bins](../shipbreaker-material-bins.md), [bulk silos and the T2](../shipbreaker-bulk-silos.md),
  [Manufacturing](../manufacturing-player-guide.md)).

## Rejected or set aside (unchanged)

Helium-3 (no honest feedstock), olivine as a CO2 sink (slow, one-way, destroys
carbon the K2 needs), platinum-group catalysts without a real wear mechanic.

## What the pause is for

The owner asked (30 September 2026) for a full audit of where authored data
should move out of C# into a schema format before more machines are added. Every
round above adds recipes, size ladders, economy specs, loot shares and art
registrations of the same shapes; the audit decides the format they will be
written in. See [the schema separation audit](schema-separation-audit.md).
