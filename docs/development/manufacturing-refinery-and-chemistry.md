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
round with its own chunk; delivered in 0.9.0 as the ammonium salt crust below).

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
| Ammonium salt crust (6), 900 s, Manufacturing 0.9.0 | 1 x ammonium salt crust, 10 kg (`PhobosAmmoniumSaltCrust`, new) | 0.955 kg ammonia to a linked ammonia store; 0.505 kg water; 1.235 kg CO2 into the room; 1 x spent salt cake, 7.305 kg (`PhobosSpentSaltCake`, new terminal) | 2 NH4Cl + Na2CO3 -> 2 NH3 + CO2 + H2O + 2 NaCl. See [the salt crust](#the-ammonium-salt-crust-090) below |
| Carburised steel (5), 2,000 s, Shipbreaker present | 4 x nickel-iron ingot (16 kg) + 1 x carbon stock (1 kg) | 4 x Rivetline steel ingot, 4 kg (`PhobosSteelIngot`); 1 x steel melt remainder, 1 kg (`PhobosSteelMeltRemainder`) | Steel is iron with 0.02 to about 2 wt% carbon (ASM Handbook Vol. 1); 16 kg of melt absorbs 0.03 to 0.3 kg. The remainder holds the unreacted carbon and skimmed oxide. The product is nickel steel, labelled as Shipbreaker's steel ingot so one identity serves both providers |

Energies: 24 kW for the durations above gives 4, 6, 12, 16 and 13.3 kWh. The
casting minimum is sensible plus latent heat for 20 kg of iron (about 0.45 kJ/kg
K over 1,500 K plus 247 kJ/kg, about 0.93 MJ/kg, 5.2 kWh); the authored 16 kWh
is three times that for losses and the hearth. The drying charges are authored
against 2.26 MJ/kg for water evaporation (NIST, below) plus heating the rock.
Fifteen percent of the working power warms the room under the same 10 kPa /
40 C bounds as Shipbreaker's R4 (`RoomHeat`, Framework 0.41.0).

**Refining value (review of 30 September 2026).** The rule that every charge must
lose value is retired (owner direction; AGENTS.md "Refining value"). What replaces
it, as an agent proposal the owner may revise, is checked at live prices by the
native and pure suites: the sellable products of a charge stay within one and a
half times its inputs (a gain reflects real work, never a windfall); a charge fed
only from bought stock gains at most a quarter at base prices, well inside the
game's buy/sell spread, so no repeatable trade loop pays; commodity records
(water, stored gases) are valued at the station price for information only, since
nothing sells them back; and stock stays plausible beside the game's own metals,
between scrap steel (3.6 cr/kg) and the ore it comes from (22.5 cr/kg).

At 0.15.0 prices with water at the station's 10 cr/kg bulk price: 450 cr of
meteoric iron becomes 80 cr of ingots and 2 cr of gangue (the game prices ore far
above metal, so refining ore is for the stock, not the money); 99 cr of carbides
becomes 50 cr of carbon, 10 cr of water and 2 cr of gangue; 150 cr of hydrates
becomes 10 cr of water and 6 cr of gangue; 180 cr of clay hydrates becomes 20 cr
of water and a 0.01 cr residue; 90 cr of nickel-iron and carbon (refined from
mined ore; no merchant sells either) becomes 100 cr of Rivetline steel ingots, an
eleven percent gain for a 250 kW melt; 150 cr of salt crust becomes 3.25 cr of ammonia (at the
game's own 3.40 cr/kg), 5.05 cr of water and a 0.01 cr cake, for the nitrogen,
not the money.

Stock is ordinary-priced raw material (owner correction, 29 September 2026:
only the machinery is late-game priced). The nickel-iron ingot returns to the
0.1.0 price of 20 cr (5 cr/kg) in 0.15.0; 0.1.1 had raised it to 24 cr only so
the steel charge would lose value under the retired rule. Carbon stays at
10 cr. The machines themselves carry the late-game price (see [the equipment economy](../equipment-economy.md#manufacturing-011-late-game-plant)).

## The ammonium salt crust (0.9.0)

The first nitrogen the ship can mine. The research record's first proposal,
an ammoniated clay chunk ([asteroid feedstock gaps, B1](asteroid-feedstock-gaps.md#b1-ammonium-bearing-clay-nitrogen)),
was dropped when the measured numbers were read: samples of Bennu returned by
NASA's OSIRIS-REx carry about 0.23 to 0.25 wt% nitrogen in total and about
13.6 micromoles of ammonia per gram (Glavin et al. 2025, below). A 10 kg clay
chunk would hold about 24 g of nitrogen however it was processed, and only about
2 g of free ammonia; an honest recipe would round to nothing. The salt deposits
on the dwarf planet Ceres are a concentrated source instead. NASA's Dawn mission
found ammonium chloride in the bright faculae of Occator crater (Raponi et al.
2019, VIR spectrometer), with sodium carbonate as the main bright salt, and
ammonium-rich bright material elsewhere on Ceres (De Sanctis et al. 2024);
Ceres' dark surface is ammoniated phyllosilicate (De Sanctis et al. 2015).

**Authored crust** (our composition, not a measured sample): 3.000 kg ammonium
chloride (56.08 mol), 2.972 kg sodium carbonate (exactly the stoichiometric
partner), and 4.028 kg of clay and salt hydrate that stay in the cake. The sources
show these salts together; they do not give this mix.

**Reaction on heating:** 2 NH4Cl + Na2CO3 -> 2 NH3 + CO2 + H2O + 2 NaCl (the
carbonate displaces ammonia from its salt, as lime does in the classic ammonia
preparation). On 28.04 mol of reaction, with the game's molar masses:

| Product | Amount | Destination |
| --- | --- | --- |
| Ammonia | 56.08 mol, 0.955 kg | The V4's linked ammonia store (a Q2, Q3 or Q4); never vented |
| Carbon dioxide | 28.04 mol, 1.234 kg, rounded to 1.235 kg | Breathed into the room over the charge, as the carbon charge's off-gas is |
| Water | 28.04 mol, 0.505 kg | The linked water vessel |
| Spent salt cake | 3.278 kg NaCl plus the 4.028 kg remainder, 7.305 kg | The tray; terminal |

Mass balance: 0.955 + 1.235 + 0.505 + 7.305 = 10.000 kg. The carbon dioxide
takes the gram of rounding. **Energy:** from standard enthalpies of formation
(ammonium chloride -314.4, sodium carbonate -1,130.7, ammonia gas -45.9, CO2
-393.5, water vapour -241.8, sodium chloride -411.2 kJ/mol; NIST Chemistry
WebBook and standard tables) the reaction absorbs about +210 kJ per mole, 1.6 kWh
for the charge, plus about 0.8 kWh to heat 10 kg of crust. The authored 900 s at
24 kW is 6 kWh, the rest losses and the hearth, like the other drying charges.

**Loot:** the crust carves 0.05 of the game's C-class mineral roll from silicates
(Framework `AdditiveLoot.CarveChoice`), beside clay hydrates (0.10) and
Shipbreaker's extra deposit ice (0.05): silicates fall from the game's 0.40 to 0.20.
Dark regolith walls reach it through their nested C-class roll. It clones the
game's hydrates block (mining, stacking six, ore sale at 150 cr), with its own
PixelLab sprite.

**Ammonia storage.** Ammonia is a game gas species (it poisons in bands) with no
game canister, so it lives in a new Fennmark gas store family, Q2, Q3 and Q4.
Industry keeps ammonia liquefied: it condenses at about 0.86 MPa at 20 C, far
below the vessel's 41.4 MPa rating, and the saturated liquid is 609 kg/m3 at
20 C (Engineering ToolBox tables after NIST, below). The same 0.787 m3 vessel at
an 80% fill holds 383 kg; authored 380 kg, then 940 and 1,820 kg on the shared
size ladder. The game treats every gas as ideal elsewhere; the capacity is the
only place the liquid matters. A damaged store leaks the game's NH3 into the room
(2, 3 or 4 kg an hour by size) and a destroyed one releases what it held. No
station sells ammonia (the game prices it at 3.40 cr/kg in `GasPrices`, used only
for the value-loss check).

**Ammonia in the RCS (owner decision, 30 September 2026: ultimate player
flexibility).** The P1 manifold accepts ammonia stores like every other gas
store. At a heat-capacity ratio of 1.310 and 17.031 g/mol, ammonia is worth 1.41
times nitrogen per kilogram as cold gas (the formula in the RCS section below).
Ammonia cold-gas thrusters are a real, if uncommon, choice. It leaves the ship as
exhaust, not into any room.

**Since 0.10.0** the Tolvane AX-2 cracker turns stored ammonia into nitrogen and
hydrogen (next section); fertiliser formulation is later. One ammonia charge
cannot be credited twice: the player routes each kilogram to the RCS, the cracker
or, later, fertiliser.

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

## Sabatier (0.2.0)

| Cycle | Inputs | Outputs | Basis |
| --- | --- | --- | --- |
| One hour at 1.2 kW | 0.125 kg H2 (62.0 mol) from the H2 store; 0.682 kg CO2 (15.5 mol) from an installed native CO2 canister | 0.2487 kg CH4 into the M2 store; 0.5585 kg water into the linked vessel | CO2 + 4 H2 -> CH4 + 2 H2O. Masses use the game's molar masses; water is the balancing remainder and agrees with 2 x 15.5 mol x 18.015 g within 0.01%. Complete conversion of the limiting hydrogen is an authored simplification |

The reactor mirrors NASA's ISS Carbon Dioxide Reduction Assembly (the Sabatier
system delivered in 2010), which reduces the CO2 the crew's air revitalisation
removes with hydrogen from the Oxygen Generation Assembly, returns the water and
vents the methane; here the owner chose to keep the methane (29 September 2026).
With the X2, 0.559 of every 1.125 kg of water split comes back, about half, which
matches the ISS system's hydrogen-limited recovery in character. The electricity
figure (compressor, bed heaters, condenser fan) is authored.

Heat: from NIST standard enthalpies of formation, CO2(g) -393.51, CH4(g) -74.87
and H2O(l) -285.83 kJ/mol, the reaction releases 253.02 kJ per mole of CO2 with
the water condensed, 1.09 kWh per cycle. That and the 1.2 kW of electricity go
into the room (about 2.3 kW while working) under the same 10 kPa / 40 C bounds.

The methane store holds 160 kg (the native canister volume at 41.4 MPa holds
roughly 200 kg of compressed methane; authored below that). Methane is one of the
game's gas species, so a damaged store leaks into its room, not to space, and a
burn follows CH4 + 2 O2 -> CO2 + 2 H2O: 3.99 kg of oxygen per kilogram, 2.74 kg
of carbon dioxide into the room, 55.5 MJ/kg (NIST higher heating value, 890.6
kJ/mol). The water vapour has no game species and leaves with the blast, as for
hydrogen. Blast size now follows energy: small below the energy of 2 kg of
hydrogen, medium below 8 kg, so hydrogen sizes are unchanged. A damaged reactor
dumps its held CO2 and methane into the room and its hydrogen burns or escapes.

## The ammonia cracker (0.10.0)

The Tolvane AX-2 is the reverse of the Haber-Bosch synthesis: ammonia over a hot
catalyst bed splits into its elements, 2 NH3 -> N2 + 3 H2. It is the established
route for recovering hydrogen from ammonia carried as a hydrogen store (reviewed by
Lucentini, Garcia, Vendrell and Llorca 2021, Universitat Politècnica de Catalunya,
below): nickel catalysts work at roughly 500 to 700 C, ruthenium lower, and
equilibrium conversion above about 400 C is nearly complete at low pressure.

| Per one-hour cycle at 2 kW | Amount | Destination |
| --- | --- | --- |
| Ammonia in | 1.000 kg, 58.72 mol | From a linked Q2, Q3 or Q4 through its ordinary outlet |
| Hydrogen out | 88.07 mol, 0.1775 kg | A linked H2, H3 or H4 through its cracker inlet |
| Nitrogen out | 29.36 mol, 0.8225 kg (the remainder) | A linked N2, N3 or N4 through its cracker inlet |

Mass balance: nitrogen is the balancing remainder, so every cycle conserves mass
exactly; with the game's own molar masses it stays within 0.004% of the
stoichiometric nitrogen (hydrogen as the remainder would miss by 0.017%, beyond the
0.01% bound the reactors keep). **Energy:** the reaction absorbs the reverse of
ammonia's formation enthalpy, 45.94 kJ per mole (NIST Chemistry WebBook), 0.749 kWh
per cycle. The authored 2 kWh per cycle covers that, heating the gas to the bed and
the losses of a small recuperated unit; the absorbed share leaves as chemical energy
in the products, so about 1.25 kW goes into the room while it works (Framework
`RoomHeat`, the same 10 kPa / 40 C bounds as the other machines). Complete
conversion is an authored simplification; a real cracker leaves a trace of ammonia
that is scrubbed or recycled.

**Value.** The products are worth slightly more than the ammonia at the game's own
gas prices (0.822 kg N2 at 4.10 and 0.178 kg H2 at 2.43 against 1 kg NH3 at 3.40
cr/kg), which the dismantling rule would normally forbid. It does not apply here:
stored gases are kilogram records with no sell-back route, as the K2's water and
methane are, so no conversion of stored gas can be sold at a profit. The rule stays
in force for every item the V4 or F6 makes.

**Ports.** The cracker draws ammonia from a store's ordinary outlet (the port the
K2 uses on a hydrogen store) and delivers into a separate cracker inlet on each
product store, so one hydrogen store can take from an X2 and a cracker, and one
nitrogen store can feed an A2, at once.

**Hazard.** A damaged or destroyed AX-2 dumps its hold: ammonia and nitrogen into
the room as the game's own gases (ammonia poisons in its bands), hydrogen by the
fuel-store rule (burns with oxygen and an ignition source, otherwise escapes). The
hold is at most one cycle, about a kilogram.

**Brand.** Tolvane is a new, original brand for the nitrogen line (owner direction
on crowded brands, 30 September 2026). The planning proposal, Azomere, was dropped
because it sits close to Azomureș, a real Romanian fertiliser maker.

## RCS propellant (Framework 0.42.0, Manufacturing 0.3.0)

The game's RCS is species-blind: `Ship.Maneuver` asks `Ship.RemoveGasMass` for a
mass of gas from whatever airtight containers sit on the regulators' gas-input
tiles, and thrust is that mass times a fixed exhaust speed of 5.26077e-9 AU/s,
787 m/s. That is exactly nitrogen expanding to vacuum from about 298 K. Ideal
cold-gas expansion gives v_e = sqrt(2γ/(γ−1)·RT/M) (standard rocket-propulsion
result, for example Sutton and Biblarz, *Rocket Propulsion Elements*; *from
memory of the text, verify the edition*), so at one temperature each gas is worth
sqrt((γ/(γ−1))/M) relative to nitrogen: hydrogen 3.69, methane 1.45, ammonia
1.41, carbon monoxide 1.00, oxygen 0.94, carbon dioxide 0.90, with γ from standard gas tables
near 298 K and the game's molar masses. A mixture is taken at its mass-weighted
worth, a stated approximation. Real cold-gas thrusters reach close to, not
exactly, the ideal figure.

Framework keeps every RCS quantity in the engine's own unit, nitrogen-equivalent
kilograms: it serves `RemoveGasMass` in the game's own order (regulators, their
gas-input points, the game's input rule, each container's own removal) at each
gas's worth, and adds registered feeds on the same tiles; `GetRCSRemain` and
`GetRCSMax` report the same unit, so delta-v and Auto Nav's planning follow. A
nitrogen-only ship is unchanged. Distant, shallow-loaded ships keep the vanilla
model. Station refuelling, which fills only nitrogen, is untouched.

The P1 manifold is Manufacturing's feed: its switched-on stores supply the
nitrogen-equivalent asked for, converted back to their own kilograms, through
Framework's buffered draws (settled every couple of seconds and before a save).
Hydrogen, methane and ammonia leave the ship as exhaust, not into any room.

## Gas stores in three sizes (Framework 0.44.0, Manufacturing 0.4.0)

Owner direction, 29 September 2026: every bulk family offers small, medium and
large sizes unless its commodity is niche or high-value. Each size is one tile
wider. Framework's shared ladder scales the small size's values, and every rule
below is our authored balance, not measured hardware:

- **Capacity:** floor area plus a 10% housing-efficiency gain per step (walls and
  fittings take a smaller share of a bigger vessel): x2.475 for 3 x 3, x4.8 for
  4 x 4 against 2 x 2.
- **Housing mass:** floor area less 15% per step.
- **Price:** floor area to the power 0.6, an economy of scale.

The new oxygen, nitrogen and carbon dioxide stores use the same vessel as the H2
and M2: the game's own RTA canister volume (0.787 m3) at its rated 41.4 MPa and
293 K holds about 13,400 mol of ideal gas (n = PV / RT with the game's own R).
Authored at 80% of that, about 10,700 mol: 340 kg of oxygen, 300 kg of nitrogen
and 470 kg of carbon dioxide. Real carbon dioxide at that pressure is a dense
supercritical fluid and would hold more; the game treats every gas as ideal, so
the stores do too. A small store therefore holds about one game canister's worth
in four tiles, because the game's canisters are very dense; bulk gas pays off in
the larger sizes.

Station bulk purchase uses the game's own gas price table (`GasPrices`, the one
the refuelling kiosk charges from): oxygen 13.2, nitrogen 4.10 and carbon dioxide
1.3 credits per kilogram in 1.0.1.5, read live. Nothing sells back.

## Canister filling (the L2)

Observed game behaviour (1.0.1.5, local assembly): the air pump moves gas each
tick without checking the destination's rated pressure, and
`GasContainer.CheckPressureDifference` damages a vessel once its pressure
difference to the surrounding room passes its rating plus 150 kPa. A suit bottle
(0.003 m3, 20,684 kPa) filled to 99% sits 207 kPa under its rating, below the
burst margin even in vacuum; so does a canister at 99% of 41,400 kPa. The L2
counts every species in the vessel, not only the rated one.

Electricity per kilogram is isothermal compression from an authored 10 MPa
suction to the vessel's rating, W = (R T / M) ln(P2 / P1), at an authored 50%
efficiency (the standard ideal-gas result; see any engineering thermodynamics
text, for example Cengel and Boles, *Thermodynamics: An Engineering Approach*;
*cited from memory, verify the edition*). Oxygen into a canister costs about
0.06 kWh/kg: at 3 kW a full 428 kg canister takes about 8.6 hours and a suit
bottle about 30 seconds of compressor time. A minimum pressure ratio of 1.5
keeps low-rated vessels from filling for free. All the electricity ends as heat
in the room: the gas warms on compression and cools back to the room.

## Cabin air regulation (the A2)

At one temperature and volume a species' partial pressure is the total pressure
times its mole fraction (Dalton's law, standard ideal-gas physics). The A2 reads
the room's committed total moles N, pressure P and oxygen moles n, so it needs no
room volume: adding x moles of oxygen gives p_O2' = P (n + x) / N, so x = target
N / P - n. Adding y moles of nitrogen gives P' = P (N + y) / N, so y = N (target -
P) / P. Oxygen added in the same step is subtracted from the nitrogen shortfall,
since it is still pending in the room's gas. Oxygen is capped so that (n + x) /
(N + x) stays at or below 0.30 (authored fire-safety cap).

Set points are authored: oxygen 19, 21 or 23 kPa (the game counts 20 kPa and
above as adequate, `DcGasPpO2`; Earth's sea-level oxygen is about 21.2 kPa) and
pressure 80, 90 or 101 kPa, or left alone. Flow limits are authored at 6 kg of
oxygen and 12 kg of nitrogen an hour. For scale, a crew member consumes about
0.8 kg of oxygen a day: NASA Johnson Space Center, Anderson, Ewert, Keener and
Wagner, [NASA Life Support Baseline Values and Assumptions Document, NASA/TP-2015-218570](https://ntrs.nasa.gov/api/citations/20150002905/downloads/20150002905.pdf), March 2015. We have not rechecked the exact table value
against the document, so treat 0.8 kg as approximate. The A2 therefore
refills a room far faster than a crew uses it. That is gameplay balance, not a
claim about any real regulator. No energy is charged for the gas itself: it
leaves a pressurised store through a valve. The 0.1 kW is the unit's sensors and
valves, through the game's own power accounting.

Gas leaves the store's kilogram record through `BulkVessel.Drain` (after the RCS
manifold's buffered draws settle) and enters the room as the game's own species
through Framework `RoomGas.Emit`, so the room's alarms, poisoning bands and fire
rules apply unchanged. A room below 10 kPa (`RoomHeat.MinPressureKPa`) is treated
as breached and is never fed.

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
| V4 salt crust off-gas | Salt crust charge running | 1.235 kg CO2 into the room in proportion to progress; ammonia is never vented: the charge waits until its linked store is intact and has room | The reaction's CO2 |
| AX-2 damage or destruction | Native damage mode switch or destroy | Held ammonia and nitrogen into the room; held hydrogen burns by the H2 store rule or escapes | Conserved; at most one cycle |
| Ammonia store damage | Native damage mode switch or destroy | Leak 2, 3 or 4 kg/h of NH3 into the room until repaired; destruction releases the contents. The game's NH3 poisoning bands apply | Conserved |
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
- NASA, International Space Station Carbon Dioxide Reduction Assembly (the
  Sabatier reactor, launched 2010): CO2 + 4 H2 -> CH4 + 2 H2O on crew CO2 and
  OGA hydrogen, water returned, methane vented. Described in NASA's ECLSS
  overview linked below; *from memory of the programme history, verify the date
  and figures before quoting*. Supports the K2's role, not its authored numbers.
- NIST Chemistry WebBook, methane: standard enthalpy of formation and of
  combustion (890.6 kJ/mol). https://webbook.nist.gov/cgi/cbook.cgi?ID=C74828
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
- Raponi, A. et al. (2019), "Mineralogy of Occator crater on Ceres and insight
  into its evolution from the properties of carbonates, phyllosilicates, and
  chlorides", *Icarus* 320, 83-96. NASA Dawn, VIR spectrometer.
  https://www.sciencedirect.com/science/article/pii/S0019103517305535 — ammonium
  chloride with sodium carbonate in the Occator faculae; supports the salt
  crust's premise, not its authored mix.
- De Sanctis, M. C. et al. (2024), "Ammonium-rich bright areas on Ceres
  demonstrate complex chemical activity", *Communications Earth & Environment*
  5, 131. NASA Dawn. https://www.nature.com/articles/s43247-024-01281-2 —
  ammonium carbonate or chloride in Ceres' bright material.
- De Sanctis, M. C. et al. (2015), "Ammoniated phyllosilicates with a likely
  outer Solar System origin on (1) Ceres", *Nature* 528, 241-244. NASA Dawn.
  https://www.nature.com/articles/nature16172
- Glavin, D. P. et al. (2025), "Abundant ammonia and nitrogen-rich soluble
  organic matter in samples from asteroid (101955) Bennu", *Nature Astronomy* 9,
  199-210. NASA OSIRIS-REx. https://ntrs.nasa.gov/citations/20250001355 — the
  measured nitrogen and ammonia that ruled out an ammoniated clay chunk.
- Engineering ToolBox, ammonia properties at gas-liquid equilibrium (tables
  compiled from NIST data) — saturated liquid density and vapour pressure near
  20 C. https://www.engineeringtoolbox.com/ammonia-liquid-gas-equilibrium-properties-d_2013.html
  *Secondary compilation; the page address is from memory, verify before quoting.*
- NIST Chemistry WebBook, ammonia: standard enthalpy of formation of the gas
  (-45.94 kJ/mol), used for the salt crust energy and the cracker's reaction heat.
  https://webbook.nist.gov/cgi/cbook.cgi?ID=C7664417
- Lucentini, I., Garcia, X., Vendrell, X. and Llorca, J. (2021), "Review of the
  Decomposition of Ammonia to Generate Hydrogen", *Industrial & Engineering
  Chemistry Research* 60(51), 18560-18611, doi:10.1021/acs.iecr.1c00843.
  Universitat Politècnica de Catalunya. https://pubs.acs.org/doi/10.1021/acs.iecr.1c00843
  — catalysts, operating temperatures and equilibrium conversion of ammonia
  cracking; supports the AX-2's process, not its authored power or rate. *The
  temperature and conversion figures quoted above are from memory of the review;
  verify them before quoting.*
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
