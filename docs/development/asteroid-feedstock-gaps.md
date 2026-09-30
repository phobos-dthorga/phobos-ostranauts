# Asteroid feedstocks: vanilla inventory, gaps and possibilities

Research record, **30 September 2026**. The owner asked for a workup of what
feedstocks and reagents the game's own ore already provides, where that leaves
gaps, and where our own ores and asteroid rocks could be inserted. The owner then
asked for every possibility to be recorded for later pursuit. **Nothing here is
implemented or approved for implementation**; each entry below is a candidate
with its evidence, our inference and its open questions kept apart.

This record updates the 24 September
[asteroid life-support research](asteroid-life-support-research.md) with the
native generation chain, the current Phobos consumers and the Manufacturing
0.1.0 to 0.5.0 chemistry in the
[refinery record](manufacturing-refinery-and-chemistry.md). The realistic-chemistry
rule, the terminal-remainder rule and the dismantling (value-loss) rule in those
records apply to every possibility here.

## Evidence inspected

Native data from the installed Steam build (read locally, not redistributed):
`condowners/condowners_mining.json`, `loot/loot_mining.json`,
`interactions/interactions_mining.json`,
`interactions/interactions_modeswitch_mining.json`,
`blueprints/asteroids/asteroids.json`, `blueprints/clusters/asteroidclusters.json`,
`star_systems/star_system.json`, `star_systems/test_environments.json`,
`market/CoCollections/cocollections.json`, and the user-string heap of
`Assembly-CSharp.dll` for hard-coded names. Phobos source: `src/PhobosManufacturing`
(`Materials`, `RefineryRules`, `MiningLoot`), `src/PhobosShipbreaker/Core/ThawRules.cs`
and Framework `Registration/AdditiveLoot.cs`. No game was run for this record.

## How vanilla mining produces material

`star_system.json` spawns asteroid fields from three random ship tables:
`RandomAsteroidC` (48 fields), `RandomAsteroidS` (7) and `RandomAsteroidM` (2);
`test_environments.json` uses the same three. Each table picks cluster
blueprints (`ClusterC01`-`C04`, `ClusterS01`, `ClusterM01`-`M03`), and each
cluster places asteroid blueprints built from wall, floor, vein, edge and 2 x 2
core tiles:

| Asteroid blueprint | Walls / veins | Floor | 2 x 2 core (deposit class) | Used by |
| --- | --- | --- | --- | --- |
| DarkRegolithPure / PureVein / Vein | Dark 05, veins dark 06 or shiny 03 | Dark 1 x 1 | `ItmFloorRock02C` (C) | C01-C04, S01, M01 |
| DarkRegolithMix | Stony 01, veins stony 02 | Stony 1 x 1 | `ItmFloorRock02` (S) | C01, C04, M01 |
| M01Main / M01NoSub | Shiny 03, veins shiny 04 | Stony 1 x 1 | `ItmFloorRock02M` (M) | M01, S01 |
| M02Main / M02PureWall | Shiny 03 with stony edge, veins stony 02 | Shiny 1 x 1 | `ItmFloorRock02M` (M) | M02, S01 |
| M03Rock011W and vein variants | Stony 01 or shiny 03, veins 02 or 04 | Stony or shiny 1 x 1 | `ItmFloorRock02` (S) or `02M` (M) | M03 |
| Ice01 | Ice walls, stony edge | Ice 1 x 1 | `ItmFloorRock02M` (M) | ClusterI01 only |

Ore leaves the rock by three routes. Mining releases no gas.

1. **Wall destruction.** A broken wall switches to `ItmRock0NSalvage`: main walls
   (01, 03, 05) roll their output table 30% of the time and vein walls (02, 04,
   06) 80%; otherwise one 3 kg gangue (`ItmMiningTrash`).
2. **Ore deposits.** Mining a 2 x 2 core damages it into `...DepositDmg`, which
   the crew extract from repeatedly (`ACTMineDeposit` and its skill/tool variants)
   until its damage budget is spent. Each pull adds `CTACTMineDeposit{S,C,M}`.
   1 x 1 floor cores only ever leave gangue.
3. **Ice walls.** A broken ice wall rolls `ItmIce01Salvage`: water ice 0.6 or
   methane ice 0.2 in one choice, a further water ice 0.4, and ice gangue 0.6.

### Expected output per deposit pull

Probabilities multiplied through the native tables; item counts, not mass.

| Class | Items per pull | Composition |
| --- | --- | --- |
| C | 0.95 | silicates 0.38, carbon/carbides 0.35, hydrates 0.095, water ice 0.095, void opal 0.03; plus Phobos clay hydrates about 0.095 through our added branch |
| S | 0.80 | olivine 0.24, meteoric iron 0.16, silicates 0.16, loose regolith 0.16, gold 0.024, cobalt 0.016, wolfram 0.016, platinum/iridium/palladium 0.008 each |
| M | 0.90 | meteoric iron 0.34, olivine 0.17, wolfram 0.12, cobalt 0.07, silicates 0.07, gold 0.044, palladium 0.042, iridium 0.031, platinum 0.018 |

### Wall salvage outputs (when the roll succeeds)

| Wall | Mass | Output choice | Nested class roll |
| --- | --- | --- | --- |
| Stony 01 | 52 kg | olivine 0.3, silicates 0.2, regolith 0.5 | none |
| Stony 02 (vein) | 79 kg | meteoric iron 0.1, olivine 0.1, silicates 0.4, regolith 0.4 | S-class 0.4 |
| Shiny 03 | 88 kg | meteoric iron 0.25, olivine 0.25, regolith 0.5 | M-class 0.04 |
| Shiny 04 (vein) | 110 kg | meteoric iron 0.65, olivine 0.35 | M-class 0.4 |
| Dark 05 | 30 kg | carbon 0.3, silicates 0.15, hydrates 0.15, regolith 0.37, void opal 0.03 | C-class 0.04 |
| Dark 06 (vein) | 45 kg | carbon 0.47, silicates 0.3, hydrates 0.2, void opal 0.03 | C-class 0.6 |

Native mining is a fixed-reward system: a wall's mass is not conserved into its
outputs. That is the game's own behaviour, not something our recipes may copy.

## Native feed inventory and current Phobos use

| ID | Game name | Unit | Base price | Mined from | Phobos consumer today |
| --- | --- | --- | --- | --- | --- |
| `ItmIce01` | Water Ice | 24.7 kg | 1,200 | C deposits; ice walls (not spawned) | Shipbreaker T2 thaw unit |
| `ItmIce02` | Methane Ice | 24.84 kg | 20 | Ice walls only (not spawned) | none |
| `ItmIceTrash01` | Ice Gangue | 2 kg | 0 | Ice walls; T2 remainder | none (T2 output) |
| `ItmMineral11` | Hydrates | 10 kg | 150 | C deposits, dark walls | V4 charge 1 |
| `ItmMineral03` | Carbon/Carbides | 10 kg | 99 | C deposits, dark walls | V4 charge 3 |
| `ItmMineral01` | Meteoric Iron | 20 kg | 450 | S/M deposits, stony and shiny walls | V4 charge 4 |
| `ItmMineral02` | Olivine | 10 kg | 180 | S/M deposits, stony and shiny walls | none |
| `ItmMineral04` | Silicates | 10 kg | 200 | Every class | none |
| `ItmMineralStone01` | Regolith (Loose) | 20 kg | 35 | S deposits, most walls | none |
| `ItmMineral05`-`10` | Platinum, Iridium, Palladium, Cobalt, Gold, Wolfram | 10 kg | 2,000-15,000 | S/M deposits | none |
| `ItmMineral79` | Void Opal | 10 kg | 112,000, not for sale | C deposits, dark walls | none |
| `ItmMiningTrash` | Gangue Material | 3 kg | 2 | Failed wall rolls | V4 output only |
| `PhobosClayHydrates` | clay hydrates chunk (Phobos) | 10 kg | 180 | Our branch on C deposits and dark walls | V4 charge 2 |

Vanilla has **no processing consumer for any ore**: ores only sell through the
`AnyOres` market collection (`IsCategoryOre`), and gold satisfies one plot
trigger. Water and methane ice carry no `IsCategoryOre`, so they are not in that
collection.

## Findings

1. **Ice asteroids are defined but never spawned.** `ClusterI01` (the `Ice01`
   blueprint) is referenced only by `RandomAsteroidI` and `RandomAsteroidAutoGen`,
   and no star-system field, plot or assembly string references either. In
   ordinary play, water ice comes only from the 10% slot of C-class deposit rolls
   (about 0.095 blocks per pull); methane ice and ice-wall gangue are effectively
   unobtainable. The T2 thaw unit therefore has a thin native supply.
2. **Our clay chunk is an additional roll.** `AdditiveLoot.SetItemChoice` adds a
   separate branch, so each C-class roll and each dark-wall output now carries up
   to 10% extra clay on top of the native full share. The 24 September research
   asked for new ore to take part of the existing yield instead.
3. **Dark vein walls roll the clay branch twice.** `ItmRock06SalvageOutput` has
   our branch directly and again inside its 60% nested C-class roll (about 0.16
   clay per successful vein roll); `ItmRock05SalvageOutput` similarly at 0.104.
4. **Several native tables are unused.** `ItmWallRock0NContents`,
   `ItmRandomRockWall{,C,S,M}` and the hand-built `ships/RockCluster*.json`
   templates are not reached by the procedural generator. Hooks there do nothing.
5. **S-class asteroids are mostly M-type rock.** `ClusterS01` is built from
   M02Main, M01NoSub and DarkRegolithPure, so a `RandomAsteroidS` field yields
   mostly M-class and C-class deposits. True S-class cores come only from
   DarkRegolithMix and M03Rock011W.
6. **Adding clusters needs Framework work.** `AdditiveLoot` accepts only
   `strType: "item"` tables; the `RandomAsteroid*` tables are `"ship"`. The
   assembly contains the `blueprints/asteroids/`, `blueprints/clusters/` and
   `blueprints/environments/` paths, but whether mod data folders are scanned for
   blueprints is **unverified**.

## Needs against sources

| Ship need | Native mined source | Phobos route today | Gap |
| --- | --- | --- | --- |
| Water | Water ice (thin, finding 1), hydrates | T2; V4 charges 1-3 | Supply |
| Oxygen | None directly | X2 electrolysis of water | Silicates and olivine carry bound oxygen and are unused |
| Nitrogen (cabin make-up and RCS) | None | None | **No source at all** |
| Carbon dioxide (K2 feed) | None | Purchased canisters; V4 carbon charge vents CO2 to the room | Moderate |
| Methane | Methane ice (not spawned) | K2 only | Supply |
| Hydrogen | Via water | X2 | None |
| Crop nutrients (P, K, S, Mg, Ca) | None | Purchased Groundwork formulations; crop-residue recovery | **Large** |
| Machining tooling | Wolfram, cobalt (sale only) | None | Consumer missing |
| Catalysts | Iridium, platinum, palladium (sale only) | None; no catalyst wear exists | Only if wear is modelled |
| Fusion fuel He3 | None | None | No honest asteroid route |
| Fusion fuel D2O | Deuterium in mined water | None | Honest but very low concentration |
| CO2 scrubber cartridges, oxygen candles | None | None | Long chains |

## Possibilities to pursue

Each entry records what it would do, its basis, what is our inference or authored
balance, prerequisites and open questions. Numbering is for reference, not
priority; the suggested order follows the list.

### A. Framework and world-generation groundwork

#### A1. Decide the loot policy: added roll or carved share

Choose, once, how new chunks enter native tables. **Added roll** (today's
`AdditiveLoot`) raises total yield; **carved share** takes the new chunk's
probability out of a named native entry, preferably loose regolith or gangue, so
valuable native shares and total item count stay the same.

- Basis: finding 2; the 24 September research's "replace part of a mining yield".
- Work: a Framework `AdditiveLoot` variant that amends a native cumulative choice
  in place, reducing one named entry by the carved amount, with the same
  publish-time amendment and removal behaviour as today's branch.
- Open: whether to migrate the clay branch to the carved form (it only affects
  future rolls; saved inventories are untouched either way). Owner decision.
- **Decided (owner, 30 September 2026): carved share, clay migrated.** Framework
  0.48.0 adds `AdditiveLoot.CarveChoice` (see the
  [Framework author guide](framework-author-guide.md#carved-loot-shares-0480)).
  Manufacturing 0.8.0 carves clay from C-class silicates (0.40 to 0.30); the
  donor is silicates rather than regolith because the C-class table has no
  regolith or gangue share, and clays are hydrated silicates.

#### A2. Remove the dark-vein double roll

If the clay branch should apply once per roll, link it only to
`ItmRandomMineralCClass` and let the salvage outputs reach it through their
nested class roll, or only to the salvage outputs and not the class table.
Owner decision on the intended rate; no save effect.

**Done (Manufacturing 0.8.0):** clay sits on the C-class table only. Deposit pulls
keep about 0.095 clay; dark vein output falls from about 0.16 to 0.06 per
successful roll and ordinary dark walls from 0.104 to 0.004, all through the
nested C-class roll.

#### A3. Ship-table additive loot

Extend Framework so content can add one cluster choice to a `"ship"` table
(`RandomAsteroidC/S/M`) with the same guards as the item form. Required by A4
and by any new asteroid type.

**Done (Framework 0.48.0):** `CarveChoice` accepts ship tables. An appended
`aLoots` branch could never win there, because the field generator takes the
first name of `GetLootNames()`; a carve inside the `aCOs` expression can.

#### A4. Verify mod blueprint loading

Confirm, by decompiled `DataHandler` inspection or a small load test, whether mod
`data/blueprints/asteroids` and `data/blueprints/clusters` folders are loaded,
and how duplicate names resolve. If not, Framework needs a registration route
before any new asteroid type (C1-C3) is possible. Research only; no feature.

**Verified by decompiled inspection (30 September 2026):** `DataHandler.LoadMod`
reads a mod's `blueprints/asteroids/`, `blueprints/clusters/` and
`blueprints/environments/` folders like its `explosions/` folder; the last load
wins by `strName`, silently. Asteroid fields are rolled when a new game creates
its star system, so saves keep the asteroids already rolled. A saved asteroid
naming a cluster from a removed mod fails when approached; that is recorded for
later exploration (Framework fallback, existing-save spawner), not as a reason to
hold back new asteroid types (owner direction, 30 September 2026).

#### A5. Spawn the game's own ice clusters

Add `ClusterI01` to `RandomAsteroidC` and/or `RandomAsteroidS` at a small weight,
making ice walls, water ice, methane ice and ice gangue reachable and relieving
the T2's thin supply.

- Basis: finding 1; the content is the game's own.
- Vanilla precedence: the developer ships this content unreferenced, possibly on
  purpose. Treat as an owner decision, ideally behind a setting, and record it in
  the [vanilla-precedence audit](vanilla-precedence-audit.md) if adopted.
- Prerequisite: A3. Only newly generated fields change; existing asteroids and
  saves are not rewritten.
- **Decided (owner, 30 September 2026):** spawn the game's `ClusterI01` behind a
  setting that is on by default, and also carve extra water ice into C-class
  deposits from silicates so existing saves benefit. Owned by Shipbreaker (the T2
  is the ice consumer); planned for Shipbreaker's ice-supply release. Methane
  ice's native price will be corrected in place in the same release as the
  clathrate recipe (D1), so processing it still loses value.
- **Implemented (Shipbreaker 0.44.0):** `ClusterI01` carved into `RandomAsteroidC`
  (from `ClusterC02`) and `RandomAsteroidS` (from `ClusterS01`) at 0.05 each, and
  0.05 water ice into C-class deposits from silicates (silicates 0.40 to 0.25 with
  the clay). Water ice per C-class deposit pull rises from about 0.095 to 0.14.
  Moving 0.05 of each C-class find from silicates ($200) to water ice ($1,200)
  raises that find's expected sale value by about $50. Recorded in the
  [vanilla-precedence audit](vanilla-precedence-audit.md).

### B. New feedstocks (new Phobos identities)

#### B1. Ammonium-bearing clay: nitrogen

A 10 kg chunk (proposed `PhobosAmmoniatedClay`, not yet allocated) found in
C-class deposits and dark walls. Heating releases ammonia; a cracker splits it
into nitrogen for cabin make-up and RCS, and hydrogen for the H2 store.

- Evidence: NASA's Dawn mission (VIR spectrometer) detected ammoniated
  phyllosilicates across Ceres ([De Sanctis et al. 2015, *Nature* 528:241-244](https://www.nature.com/articles/nature16172)).
  Samples of Bennu returned by NASA's OSIRIS-REx are richer in ammonia and
  nitrogen than Ryugu samples and most meteorites
  ([Glavin et al. 2025, *Nature Astronomy* 9:199-210, NASA NTRS copy](https://ntrs.nasa.gov/api/citations/20250001355/downloads/GlavinAbundantSTI.pdf)).
- Chemistry: 2 NH3 -> N2 + 3 H2, 34.06 : 28.01 : 6.05 by mass. Cracking is
  endothermic by roughly the ammonia formation enthalpy (about 46 kJ/mol NH3,
  NIST WebBook; *from memory, confirm*), about 0.75 kWh per kg of ammonia before
  losses. NH3 is a native gas species, so a leak or a bad batch can use the
  game's poisoning bands.
- Our inference: an extractable ammonium fraction, release temperature and
  cracker conversion are **not yet sourced**; the chunk's N content must come
  from measured wt% values before any balance is written. Expect a small yield:
  nitrogen is scarce and the recipe must say so.
- Destinations: N2 into the Manufacturing N2 store or a native `ItmRTAN2` through
  `NativeGasCanister`; H2 into the H2 store. Anhydrous clay residue needs its own
  terminal identity or reuse of `PhobosAnhydrousResidue` if the mass matches.
- Prerequisites: A1 policy; a cracker (new machine or a K2-family mode, owner
  choice); nitrogen output to the P1 path is already supported via the N2 store.

#### B2. Evaporite salt crust: potassium, phosphorus, sulfur and CO2

A chunk (proposed `PhobosEvaporiteCrust`) found in C-class material, leached into
separated nutrient salts for Agriculture and carbonate CO2 for the K2.

- Evidence: Bennu samples contain sodium-bearing phosphates and sodium-rich
  carbonates, sulfates, chlorides and fluorides from an evaporated brine,
  including halite and sylvite ([McCoy et al. 2025, *Nature* 637:1072-1077](https://www.nature.com/articles/s41586-024-08495-6);
  NASA OSIRIS-REx sample team, Smithsonian-led). NASA's mineral summary is linked
  in the [24 September research](asteroid-life-support-research.md).
- Chemistry and limits (our inference):
  - Sylvite (KCl) is directly usable potassium, carrying chloride.
  - Sodium phosphate needs its sodium removed or exchanged before use; sodium is
    harmful to crops, so sodium chloride leaves as its own terminal remainder.
  - Sulfates supply sulfur.
  - Magnesium, iron and calcium carbonates release CO2 on heating; **sodium
    carbonate does not calcine usefully**, and yields CO2 only on acidification.
    The recipe must say which carbonate it assumes.
- Prerequisites: a concrete Agriculture consumer (existing rule: no nutrient
  chunk without one); the [Agriculture endurance roadmap](agriculture-roadmap.md#round-3-characterized-nutrients-and-asteroid-replenishment)
  round 3; a leach/crystallise machine. Mineral fractions must come from the
  McCoy data or labelled authored values.

#### B3. Troilite nodule: sulfur (optional)

Iron meteorites carry troilite (FeS), which the V4 casting charge currently
folds into its terminal slag (Buchwald 1975, cited in the refinery record). A
separate nodule chunk, or a new V4 charge revision that recovers sulfur, could
feed sulfuric acid for B4 and sulfate nutrients.

- Constraint: never reinterpret existing slag or re-process a terminal remainder;
  only a new charge revision or a new chunk.
- Chain (our inference): roasting FeS gives SO2; oxidation and absorption give
  sulfuric acid (the contact process). H2SO4 is in the game's native species list
  and must only enter a room through `RoomGas`.
- Only worth pursuing if B2 sulfates or B4 acid leaching have a real consumer.

### C. New asteroid types (after A3 and A4)

#### C1. Ammonia-bearing dark asteroid

A variant of DarkRegolith with a new wall pair whose salvage carries B1 as a
real share, rather than a scattered bonus. Spawned into `RandomAsteroidC` at a
low weight so prospecting matters. Needs wall/floor artwork (PixelLab,
overhead-first policy) and damaged forms.

#### C2. Brine-altered asteroid

The B2 counterpart: a dark asteroid with evaporite veins. Could share C1's
blueprint with different vein loot.

#### C3. Our own ice cluster (alternative to A5)

If the owner prefers not to spawn the game's `ClusterI01`, a Phobos ice cluster
built from the native ice tiles achieves the same supply with our own weight and
setting. Same prerequisites as A5 plus A4.

**Not needed for supply (30 September 2026):** the owner chose A5, now
implemented. A Phobos ice cluster remains open as creative work (for example an
ice body with its own veins or chunks) alongside C1 and C2.

### D. New uses for native ores (no new ore identity)

#### D1. Methane clathrate processing of `ItmIce02`

Read the game's "Methane Ice" ("Cold, wet, but not great for drinking") as
methane clathrate and dissociate it into methane for the M2 store (RCS through the
P1) and process water.

- Evidence: fully occupied structure I methane hydrate is CH4.5.75 H2O
  ([USGS Fact Sheet 2017-3080, *Gas Hydrate in Nature*](https://pubs.usgs.gov/fs/2017/3080/fs20173080.pdf)).
- Mass balance at full occupancy: M = 16.04 + 5.75 x 18.015 = 119.63 g/mol, so
  13.4 wt% methane. One 24.84 kg block gives at most 3.33 kg CH4 (207.6 mol) and
  21.51 kg water. Natural occupancy is below 100%; the authored yield should sit
  below that ceiling, with an ice-gangue remainder.
- Our inference: the clathrate reading is **authored**; the game only says
  "methane ice" and "wet". Dissociation enthalpy is not yet sourced.
- Machine: a T2 mode or recipe revision (Shipbreaker) with a methane destination,
  or a Manufacturing unit; the T2 already refuses `ItmIce02` by exact identity.
- Prerequisite: A5 or C3, otherwise there is no supply.

**Implemented (Shipbreaker 0.45.0):** the T2 takes `ItmIce02` in its own recipe
catalog: 24.84 kg gives 19.89 kg water (linked water vessel), 2.95 kg methane
(linked methane store on its own port pair) and 2.0 kg ice gangue, in 50 minutes
at 6 kW. Authored hydration number n = 6.0, within the 6.0 to 6.2 measured for
natural and laboratory sI hydrate (Circone et al. 2005, USGS, as reviewed by
[Ruppel and Waite 2020, *JGR Solid Earth*](https://agupubs.onlinelibrary.wiley.com/doi/full/10.1029/2018JB016459)).
Dissociation to gas and liquid water: 54.2 kJ/mol CH4
([Handa 1986, *J. Chem. Thermodynamics* 18, NRC Canada](https://www.sciencedirect.com/science/article/abs/pii/0021961486901497)),
about 4.1 kWh a block with the water-ice warming allowance. `ItmIce02` is
repriced in place from 20 to 250 so the products (about 205 at the station water
price and the game's methane price) are worth less than the block.

#### D2. Carbothermal oxygen from silicates, olivine and loose regolith

Give the two commonest unused ores (and loose regolith) a use: reduce molten
silicate with methane, then recycle the products through the machines we have.

- Evidence: NASA's Carbothermal Reduction Demonstration (CaRD) extracts oxygen
  from simulated lunar regolith using methane and concentrated sunlight, producing
  carbon monoxide ([NASA NTRS 20230003977](https://ntrs.nasa.gov/citations/20230003977);
  [2010 field demonstration, NTRS 20110005526](https://ntrs.nasa.gov/citations/20110005526)).
  NASA notes lunar regolith is about 45% oxygen by mass, mostly bound in silicates.
- Loop (our inference): silicate + CH4 -> CO + H2 (+ reduced metal and slag);
  CO + 3 H2 -> CH4 + H2O returns methane; X2 electrolysis turns the water into O2
  and H2. It closes with the X2, K2 and M2, with methane and hydrogen losses made
  up from stores.
- Needs: a high-temperature machine with a real power and heat budget (a good use
  of fusion electricity), a reduced-metal/slag remainder identity, and source
  values for practical extraction per kilogram. Lunar evidence applied to
  asteroid silicate is our extrapolation.

#### D3. Olivine magnesium for nutrients

Mg2SiO4 + 2 H2SO4 -> 2 MgSO4 + SiO2 + 2 H2O gives Epsom salt, a standard
hydroponic magnesium and sulfur source, with a silica remainder.

- Needs an acid source (B3) and an Agriculture consumer; standard chemistry, but
  the leach efficiency and olivine composition (forsterite-fayalite range) must be
  declared.

#### D4. Olivine as a CO2 sink (recorded, not recommended)

The game's olivine text says it "absorbs CO2". Mineral carbonation,
Mg2SiO4 + 2 CO2 -> 2 MgCO3 + SiO2, is real but slow at cabin temperature and
one-way, so it is not a regenerable scrubber and would lose carbon needed for the
K2. Keep as a possible passive emergency absorbent only if a strong need appears.

#### D5. Cemented-carbide tooling from wolfram and cobalt

When the proposed M4 mill's finite machining cartridge is built, tungsten carbide
tools bound with cobalt give W and Co ores a consumer, using our carbon stock.

- Chemistry: WC is 93.9% W by mass (183.84 / 195.85). Cobalt binder content in
  cemented carbides is typically a few to about 12 wt% (*from memory, cite a
  handbook before use*).
- The ores are "Ore: Wolfram" and "Ore: Cobalt", not pure metal; the grade is
  authored. The high ore prices make the value-loss rule easy to satisfy.
- Prerequisite: the M4 design in the [Manufacturing research](manufacturing-research.md).

#### D6. Platinum-group catalysts (only with catalyst wear)

Proton-exchange electrolysers use iridium-oxide anodes and platinum cathodes, and
Sabatier reactors use ruthenium or nickel catalysts (*from memory, source before
use*). The game's iridium text mentions Os, Ru, Rh and Pt. If the X2 or K2 ever
gains catalyst wear, a refurbishment charge could consume a fraction of an
iridium/platinum block for several catalyst cartridges.

- Only pursue with a real maintenance mechanic; do not invent wear to create a
  consumer. Catalysts use grams, so one 10 kg block must yield many cartridges.

#### D7. Loose regolith and gangue

Loose regolith (20 kg) has no consumer; D2 is its best use. Sintered
construction or shielding would need a consumer we do not have. Gangue stays the
game's terminal waste.

### E. Consumable chains (long, low priority)

#### E1. Oxygen candles from halite

Native oxygen candles are chlorate candles. Sodium chlorate comes from brine
electrolysis of sodium chloride, which B2 would leave as a remainder. Long chain;
record only.

#### E2. Regenerable CO2 scrubbing

Native CO2 scrubber cartridges have no mined route. NASA's ISS Carbon Dioxide
Removal Assembly uses regenerable zeolite molecular sieves (*from memory, source
before use*). A regenerable scrubber would remove a consumable rather than
manufacture one; zeolite synthesis from asteroid silicates is not credible as a
first step.

### F. Fusion fuel (parked or rejected)

#### F1. Heavy water enrichment (parked)

Deuterium is present in all mined water, and electrolytic enrichment is
historical industrial practice, so the route is honest.

- Scale: at a D/H ratio near the terrestrial reference (about 1.56 x 10^-4,
  VSMOW; *from memory, cite the IAEA reference before use*), full recovery gives
  about 1 kg of D2O per 5,800 kg of water. Water in carbonaceous chondrites is
  close to terrestrial D/H (Alexander et al. 2012, cited in the refinery record).
  Comet 67P's water is about three times richer
  ([Altwegg et al. 2015, *Science*, ESA Rosetta ROSINA](https://www.science.org/doi/abs/10.1126/science.1261952)),
  but we mine asteroids, not comets.
- Verdict: park unless fuel, not water, becomes the limit on endurance, and
  measure the reactor's real D2O consumption first.

#### F2. Helium-3 (rejected)

No asteroid feedstock carries helium-3 at a concentration that supports an
honest recipe. Do not add a mined route.

### G. Leave alone

Void opal (the game's not-for-sale lore mineral) and gold (a sale item with a
plot use) should keep their native roles.

## Suggested order

1. A1 and A2 (policy), then A3 and A5 or C3 (ice supply), because the T2 is
   already built and short of feed.
2. B1 nitrogen, the one need with no source at all. Source the nitrogen fraction
   first.
3. D1 methane clathrate once ice spawns.
4. B2 together with Agriculture round 3; D3 and B3 only if B2's consumer needs them.
5. D2 carbothermal oxygen as the next large fusion-powered machine.
6. D5 with the M4 mill; D6 only with catalyst wear.
7. A4, C1 and C2 when a new asteroid type is worth the artwork.
8. E and F remain records unless play shows a need.

## Sources

Cited beside the claims above; listed here for review. None of the institutions
named endorses this mod or its balance. Entries marked *from memory* in the text
must be re-read before any number is used in a recipe.

- De Sanctis, M. C. et al. (2015), "Ammoniated phyllosilicates with a likely outer
  Solar System origin on (1) Ceres", *Nature* 528, 241-244. NASA Dawn, VIR.
  https://www.nature.com/articles/nature16172 - supports B1's premise, not its yield.
- Glavin, D. P. et al. (2025), "Abundant ammonia and nitrogen-rich soluble organic
  matter in samples from asteroid (101955) Bennu", *Nature Astronomy* 9, 199-210.
  NASA OSIRIS-REx. https://ntrs.nasa.gov/citations/20250001355 - supports B1.
- McCoy, T. J. et al. (2025), "An evaporite sequence from ancient brine recorded in
  Bennu samples", *Nature* 637, 1072-1077. NASA OSIRIS-REx.
  https://www.nature.com/articles/s41586-024-08495-6 - supports B2's mineral list.
- U.S. Geological Survey (2017), *Gas Hydrate in Nature*, Fact Sheet 2017-3080.
  https://pubs.usgs.gov/fs/2017/3080/fs20173080.pdf - CH4.5.75 H2O for D1.
- NASA, Carbothermal Reduction Demonstration (CaRD), NTRS 20230003977,
  https://ntrs.nasa.gov/citations/20230003977, and the 2010 solar carbothermal
  field demonstration, NTRS 20110005526,
  https://ntrs.nasa.gov/citations/20110005526 - support D2's route, not its yield.
- Altwegg, K. et al. (2015), "67P/Churyumov-Gerasimenko, a Jupiter family comet
  with a high D/H ratio", *Science* 347. ESA Rosetta, ROSINA.
  https://www.science.org/doi/abs/10.1126/science.1261952 - context for F1.
- Alexander, C. M. O'D. et al. (2012) and Buchwald, V. F. (1975): full entries in
  the [refinery record's sources](manufacturing-refinery-and-chemistry.md#sources).
