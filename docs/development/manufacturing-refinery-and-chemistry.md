# Phobos Manufacturing 0.1.0: refinery chemistry, hazards and sources

29 September 2026. Design record for the Fennmark V4 Volatiles Refinery, X2
Chemical Processor and H2 Hydrogen Store, and for the owner's realistic-chemistry
rule that governs them. Player instructions are in
[the player guide](../manufacturing-player-guide.md); the code layout is in
[the implementation record](manufacturing-implementation.md).

## The rule

Owner direction, 29 September 2026: **a recipe yields only what its inputs can
realistically contain**, as close to real stoichiometry as the game's item units
allow, even where that means adding materials or minable chunks of our own.
Every recipe below records its reaction or separation, its mass balance, its
energy and a primary source, and states where item-unit rounding moved it from
the literature. The source informs the design; none of the institutions named
endorses this mod or its balance.

Rejected from the inspiration (Rusty150's Salvage Workshop and Crafting
Framework, MIT, inspected locally as inspiration only): one refined product per
mineral with no consumer; random ingots from gangue (creates mass); CO2 and
water to plastic at a price that prints money and is not the chemistry;
nitrogen from regolith without a nitrogen-bearing feed (deferred to a nitrogen
round with its own chunk).

## The charges

Item units: water in kilograms into a Framework water vessel; gangue is the
game's own 3 kg `ItmMiningTrash`; every other unit is listed. Each charge is an
immutable `ChargeRecipe` bound to a revision when Start binds it, so a running
charge keeps its captured products through later balance changes.

| Charge (rev.) | Inputs | Outputs | Basis and rounding |
| --- | --- | --- | --- |
| Hydrate dehydration (1), 600 s | 1 x hydrates block, 10 kg (`ItmMineral11`) | 1 kg water; 3 x gangue (9 kg) | CM-chondrite structural water 9 to 13 wt% (Alexander et al. 2012, below); authored 10%. Residue is dehydrated silicate and oxide; the game's gangue unit already exists for it |
| Clay hydrate dehydration (2), 900 s | 1 x clay hydrates chunk, 10 kg (`PhobosClayHydrates`, new) | 2 kg water; 1 x anhydrous residue, 8 kg (`PhobosAnhydrousResidue`, new terminal) | CI-chondrite and Bennu-type phyllosilicate water 18 to 22 wt% (Alexander et al. 2012; Lauretta et al. 2024); authored 20%. 8 kg is not a gangue multiple, so it has its own identity |
| Carbon extraction (3), 1,800 s | 1 x carbon/carbides block, 10 kg (`ItmMineral03`) | 5 x carbon stock, 1 kg (`PhobosCarbonStock`, new); 1 kg water; 1 x gangue; 1.0 kg off-gas into the room: 0.6 kg CO2, 0.3 kg CO, 0.1 kg smoke | The game's own text calls the item carbon or carbides, "often hydrated". Authored 50% carbon, 10% water, 30% silicate, 10% pyrolysis gas. The gas split is authored (incomplete combustion of volatiles); the game's molar masses convert kg to moles |
| Nickel-iron casting (4), 2,400 s | 1 x meteoric iron block, 20 kg (`ItmMineral01`) | 4 x nickel-iron ingot, 4 kg (`PhobosNickelIronIngot`, new); 1 x gangue; 1 x refinery slag, 1 kg (`PhobosRefinerySlag`, new terminal) | Iron meteorites are Fe-Ni metal (kamacite and taenite, roughly 5 to 10% Ni, with cobalt) carrying troilite, schreibersite and oxide inclusions and adhering rock (Buchwald 1975); the game calls the item "iron-nickel alloy" with iron oxide. Authored 80% metal, 15% rock, 5% slag. The ingot is nickel-iron, not steel |
| Carburised steel (5), 2,000 s, Shipbreaker present | 4 x nickel-iron ingot (16 kg) + 1 x carbon stock (1 kg) | 4 x Rivetline steel ingot, 4 kg (`PhobosSteelIngot`); 1 x steel melt remainder, 1 kg (`PhobosSteelMeltRemainder`) | Steel is iron with 0.02 to about 2 wt% carbon (ASM Handbook Vol. 1); 16 kg of melt absorbs 0.03 to 0.3 kg. The remainder holds the unreacted carbon and skimmed oxide. The product is nickel steel, labelled as Shipbreaker's steel ingot so one identity serves both providers |

Energies: 24 kW for the durations above gives 4, 6, 12, 16 and 13.3 kWh. The
casting minimum is sensible plus latent heat for 20 kg of iron (about 0.45 kJ/kg
K over 1,500 K plus 247 kJ/kg, about 0.93 MJ/kg, 5.2 kWh); the authored 16 kWh
is three times that for losses and the hearth. The drying charges are authored
against 2.26 MJ/kg for water evaporation (NIST, below) plus heating the rock.
Fifteen percent of the working power warms the room under the same 10 kPa /
40 C bounds as Shipbreaker's R4 (`RoomHeat`, Framework 0.41.0).

Value loss (the dismantling rule), at 0.1.1 prices with water at the station's
10 cr/kg bulk price: 450 cr of meteoric iron becomes 96 cr of ingots and 2 cr
of gangue; 99 cr of carbides becomes 50 cr of carbon, 10 cr of water and 2 cr
of gangue; 150 cr of hydrates becomes 10 cr of water and 6 cr of gangue; 180 cr
of clay hydrates becomes 20 cr of water and a 0.01 cr residue; 106 cr of
nickel-iron and carbon becomes 100 cr of Rivetline steel ingots. Refining trades
money for material aboard; it is never a profit route. The native checks compute
this for every charge from live definitions, Shipbreaker's steel ingot included.

Stock is ordinary-priced raw material (owner correction, 29 September 2026:
only the machinery is late-game priced), and the vanilla ore prices are its
ceiling anyway: a nickel-iron ingot above about 111 cr, or carbon above about
17 cr, would make a charge profitable. Manufacturing 0.1.0 priced the ingot at
20 cr, which made the steel charge gain value (80 + 10 cr in, 100 cr out);
0.1.1 moves the ingot only as far as that fix needs, to 24 cr, and leaves carbon
at 10 cr. The machines themselves carry the late-game price (see [the equipment economy](../equipment-economy.md#manufacturing-011-late-game-plant)).

## Electrolysis

| Cycle | Inputs | Outputs | Basis |
| --- | --- | --- | --- |
| One hour at 6 kW | 1.125 kg water from a linked vessel; 6.0 kWh | 1.000 kg O2 (31.25 mol at the game's 0.0319988 kg/mol) into a linked `ItmRTAO2` up to its rated pressure, or into the room; 0.125 kg H2 into the H2 store | 2 H2O -> 2 H2 + O2. The 9 : 8 : 1 mass ratio is within 0.1% of 9.008 : 7.999 : 1.008. Water's standard enthalpy of formation is -285.83 kJ/mol (NIST WebBook), so splitting 62.4 mol needs 4.96 kWh; the pure checks refuse any cycle energy below it. The remaining 1.04 kWh is room heat |

The oxygen path follows the ISS Oxygen Generation Assembly, which electrolyses
water and vents the hydrogen (NASA ECLSS, below); we keep the hydrogen because
the owner asked for it and a Sabatier stage will consume it. Our 48 kWh per kg
of hydrogen is at the optimistic end of practical electrolysers; that is an
authored choice for a one-hour cycle.

Canister filling uses the game's own capacity formula (volume times rated
pressure over R T, R = 0.008314 kPa m3 / mol K): the native 0.787 m3, 41,400 kPa,
293 K O2 canister holds 13,373 mol, about 428 kg. Framework's `NativeGasCanister`
clamps every addition to that headroom; a damaged canister is refused.

## The hydrogen store

24 kg capacity: the native canister volume at 41.4 MPa and 293 K holds about
27 kg of ideal-gas hydrogen; 24 kg is authored below that for a smaller housing.
The commodity is a kilogram record on a Framework bulk vessel (family
`PhobosHydrogenStore`, commodity `hydrogen`, `DamagePolicy.Leak`, 2 kg/h). No
gas species is created: the game has no hydrogen, `AddGasMols` ignores unknown
names, and the owner's standing rule forbids custom species.

## Hazards

Owner direction, 29 September 2026: gases where vanilla has them, machines that
off-gas while working, a damaged store that leaks or explodes, reactions that go
bad. Every hazard uses the game's own machinery, conserves mass and is journaled.

What Ostranauts 1.0.1.5 provides (read in the decompile, not modified):

| Native mechanism | Evidence | How we use it |
| --- | --- | --- |
| Room gas species CH4, CO, CO2, H2SO4, N2, NH3, O2, Smoke; `GasContainer.AddGasMols` (no cap, negative unclamped) | `GasContainer` | `RoomGas.Emit/Consume` (Framework 0.41.0) clamp at the condition amount and use only these species |
| Poisoning bands CO / CO2 0.3 and 3 kPa, smoke 0.05 / 0.2 kPa; Smoke alarm covers CO; CO2 alarm separate; `ItmAtmoScrubber02` deletes CO and smoke, `01` captures CO2 | Conditions and scrubber definitions | The carbon charge's off-gas triggers them; the player answers with the game's scrubbers |
| Fire is a `SysFire` object (`CrewSim.vfxFire`), consumes O2 into CO2/CO/smoke, continues while ppO2 allows, spreads by `IsFlammable/IsBurnable/IsFireproof`; ignition only from explosions, weapon hits and sparks from damaged powered devices | `SysFire`, `vfxFire` | An existing fire, a working V4 hearth or a >= 50%-damaged powered device in the room is our ignition source |
| Explosions are objects carrying `Explosion,<name>`, defined in `data/explosions`: radius damage without falloff, shrapnel rays, a fire roll (25%, 90% on flammables); they do not touch gas | `Explosion` component, `DataHandler` loads mod `explosions/` folders | `SysPhobosDeflagrationSmall/Medium/Large` with authored radii and ray counts; spawned through `NativeExplosions.Spawn` |
| Canisters take overpressure damage above `StatGasPressureMax` + 150 kPa and leak when damaged (`GasExchange ... leak,0.01`) | Canister definitions | The X2 fills only to rated pressure and refuses damaged canisters |
| No liquid spill mechanic; no methane combustion | Survey | Water and hydrogen stay as records; a future Sabatier fault vents CH4 as a contaminant, not a fire |

Our hazards:

| Hazard | Trigger | Effect | Mass and energy |
| --- | --- | --- | --- |
| V4 off-gassing | Carbon extraction running | CO2, CO and smoke added to the room in proportion to progress each powered step (`OffGasDueKg`); refused while the room is below 10 kPa or the machine has no room | The charge's 1.0 kg gas share |
| X2 cabin oxygen | No canister linked | 1.000 kg O2 per cycle into the room | The recipe's oxygen |
| Reaction gone bad | A nickel-iron or steel melt waiting for a cool room longer than its own duration | The charge finishes as slag: 17 kg slag + gangue for iron, 17 kg slag for steel, logged and noticed | Conserved |
| H2 store damage or destruction | Native damage mode switch or destroy | If ppO2 >= 5 kPa and an ignition source is present: burn min(H2, O2 / 8) kg, consume 8 kg O2 per kg H2 (clamped to the room), heat the room to at most 333.15 K, spawn the deflagration object sized by kg burned (Small < 2, Medium < 8, Large), journal the rest as blast energy; otherwise leak 2 kg/h to space until repaired, and deflagrate later if ignition arrives. Hydrogen not burned in a destruction is lost with the blast, journaled | 2 H2 + O2 -> 2 H2O, 141.9 MJ per kg of hydrogen (higher heating value, from NIST's water enthalpy); the water formed is not modelled as a species |

The 5 kPa oxygen threshold is an authored simplification of hydrogen's wide
flammability range in air (4 to 75 vol%, NASA NSS 1740.16); the game has no
mixing model, so we test the room's oxygen rather than a hydrogen fraction.
The room-heat cap is the same ceiling every Phobos machine respects; the energy
above it is accounted as blast, not deleted quietly.

## Sources

Cited beside the claims above. Where a reference could not be re-read at the
time of writing it is marked *from memory*; verify before quoting numbers.

- NIST Chemistry WebBook, SRD 69: Water, condensed-phase thermochemistry
  (standard enthalpy of formation -285.83 kJ/mol; enthalpy of vaporization).
  https://webbook.nist.gov/cgi/cbook.cgi?ID=C7732185 — supports the
  electrolysis minimum, the drying energies and the hydrogen heating value.
- NASA, International Space Station Environmental Control and Life Support
  System, Oxygen Generation Assembly: water electrolysis with hydrogen vented or
  sent to Sabatier. https://www.nasa.gov/international-space-station/space-station-environmental-control-and-life-support-system/
  — supports the electrolysis design and the later Sabatier stage. NASA is a
  source, not an endorser.
- NASA Safety Standard for Hydrogen and Hydrogen Systems, NSS 1740.16 (1997):
  flammability limits of hydrogen in air 4 to 75 vol%, ignition energy, leak
  and deflagration guidance. *From memory of the standard; verify the edition.*
  Supports the hazard model's framing, not its authored threshold.
- Alexander, C. M. O'D. et al. (2012), "The provenances of asteroids, and their
  contributions to the volatile inventories of the terrestrial planets",
  Science 337, 721–723, doi:10.1126/science.1223474 — bulk water contents of
  CM and CI chondrites. *From memory; verify the percentages.*
- Lauretta, D. S. et al. (2024), "Asteroid (101955) Bennu in the laboratory:
  Properties of the sample collected by OSIRIS-REx", Meteoritics & Planetary
  Science 59, doi:10.1111/maps.14227 — hydrated phyllosilicates and
  water-rich clays in the returned sample. NASA / University of Arizona
  OSIRIS-REx science team authorship. *From memory; verify the citation.*
- Buchwald, V. F. (1975), Handbook of Iron Meteorites, University of California
  Press — kamacite/taenite nickel contents, troilite, schreibersite and
  cohenite inclusions.
- ASM Handbook, Volume 1: Properties and Selection: Irons, Steels, and
  High-Performance Alloys — carbon ranges of steels.
- Blue Bottle Games, Ostranauts 1.0.1.5 — item texts, molar masses, canister
  capacity formula, gas conditions, fire and explosion mechanics (locally
  inspected; not redistributed).

Our inference, simplified model and authored balance are stated as such in each
row; the percentages chosen are within the cited ranges but are gameplay
choices, and item-unit rounding is explicit.
