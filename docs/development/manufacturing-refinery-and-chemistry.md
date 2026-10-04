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
| Carburised steel (5), 2,000 s, Shipbreaker present | 4 x nickel-iron ingot (16 kg) + 1 x carbon stock (1 kg) | 4 x Rivetline steel ingot, 4 kg (`PhobosSteelIngot`); 1 x steel melt remainder, 1 kg (`PhobosSteelMeltRemainder`) | Steel is iron with 0.02 to about 2 wt% carbon (ASM Handbook Vol. 1); 16 kg of melt absorbs 0.03 to 0.3 kg. The remainder holds the unreacted carbon and skimmed oxide. The product is nickel steel, labelled as Shipbreaker's steel ingot so one identity serves both providers. Superseded by revision 8 for new charges since 0.26.0; a bound charge still settles as steel |
| Nickel steel (8), 2,000 s, Manufacturing 0.26.0 | 4 x nickel-iron ingot (16 kg) + 1 x carbon stock (1 kg) | 4 x nickel steel ingot, 4 kg (`PhobosNickelSteelIngot`, new); 1 x refinery slag, 1 kg | The same melt and chemistry as revision 5, ending in Manufacturing's own product (owner decision, 1 October 2026: the mined iron chain ends in its own product, so the scrap-cast steel ingot keeps its price). Our rounding: the carbon the steel picks up and the iron lost to the dross are taken as equal, so the ingots keep 16 kg. Supersedes revision 5; needs no other mod |
| Methane cracking (9), 1,800 s, Manufacturing 0.27.0 | 1 x carbon stock (1 kg) + 4.007 kg methane from a linked store | 4 x carbon black, 1 kg (`PhobosCarbonBlack`, new); 1.007 kg hydrogen to a linked store | CH4 -> C + 2 H2 on 249.77 mol; absorbs 74.87 kJ/mol (CODATA), 5.19 kWh. Elemental carbon catalyses the decomposition and the product deposits on the bed (Muradov, Catalysis Communications 2, 2001: cited from memory, unverified). The bed is consumed into the product; carbon black is priced at 12 cr so methane made from bought water and CO2 never repays the purchase |
| Carbon burning (10), 1,800 s, Manufacturing 0.27.0 | 1 x carbon black (1 kg) + 2.664 kg oxygen from a linked store | 3.664 kg carbon dioxide to a linked store | C + O2 -> CO2 on 83.26 mol, releasing 393.51 kJ/mol (CODATA), 9.10 kWh into the room. Complete combustion is authored. For grow rooms through an A2 |
| Scrubber cartridge reactivation (11), 1,800 s, Manufacturing 0.27.0 | 4 x spent CO2 scrubber cartridge, 2.5 kg (`ItmFilterCO201Dmg`) | 3 x ready cartridge (`ItmFilterCO201`); 1 x exhausted sorbent, 2.5 kg (`PhobosExhaustedSorbent`, new terminal) | Authored, not a chemistry claim: the game's scrubber already sent the CO2 to a canister, and lithium hydroxide is not regenerated by heat alone. A quarter lost each pass |
| EVA filter reactivation (12), 1,800 s, Manufacturing 0.27.0 | 4 x spent EVA CO2 filter, 2.5 kg (`ItmFilterCO202Dmg`) | 3 x ready EVA filter (`ItmFilterCO202`); 1 x exhausted sorbent | As revision 11 |

Energies: 24 kW for the durations above gives 4, 6, 12, 16 and 13.3 kWh. The
casting minimum is sensible plus latent heat for 20 kg of iron (about 0.45 kJ/kg
K over 1,500 K plus 247 kJ/kg, about 0.93 MJ/kg, 5.2 kWh); the authored 16 kWh
is three times that for losses and the hearth. The drying charges are authored
against 2.26 MJ/kg for water evaporation (NIST, below) plus heating the rock.
Fifteen percent of the working power warms the room under the same 10 kPa /
40 C bounds as Shipbreaker's R4 (`RoomHeat`, Framework 0.41.0).

**Refining as a business (owner approval, 1 October 2026; Manufacturing 0.26.0).**
The rules in force are in [the design record](refining-business-and-interdependencies.md#the-rules-now-in-force)
and are checked at live prices by the native and pure suites: a charge fed by mined
ore that yields a finished item earns 1.5 to 2.5 times the ore at base prices,
products valued as the station values them (items at their price, water at 10
cr/kg, stored gases and acid at the game's gas price, which the kiosk now buys
back at 45%); a step inside a chain neither loses nor gains more than half again;
supply charges (hydrates, clay, the salt crust) make no profit claim; fertiliser
formulations carry Agriculture's price; a charge fed only from bought stock earns
at most a quarter more. At 0.26.0 prices: 450 cr of meteoric iron becomes 880 cr
of ingots, 2 cr of gangue and a slag (1.96 times); 99 cr of carbides becomes 190 cr
of carbon, 10 cr of water and 2 cr of gangue (2.04); four nickel-iron ingots and
a carbon (918 cr) become 1,040 cr of nickel steel (1.13, a step); the evaporite
crust 2.00, olivine 2.31 and the sulfide nodule 2.16 times their ore.

The 30 September review below is history, kept for its reasoning: it set
guardrails of half again the inputs and stock priced below its ore, which the
owner's decision of 1 October supersedes.

**Refining value (review of 30 September 2026, superseded).** The rule that every charge must
lose value is retired (owner direction; AGENTS.md "Refining value"). What replaced
it then, as an agent proposal, was checked at live prices by the
native and pure suites: the sellable products of a charge stay within one and a
half times its inputs; a charge fed
only from bought stock gains at most a quarter at base prices; commodity records
(water, stored gases) are valued at the station price for information only, since
nothing sold them back then; and stock stays between scrap steel (3.6 cr/kg) and
the ore it comes from (22.5 cr/kg).

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

Stock was ordinary-priced raw material (owner correction, 29 September 2026:
only the machinery is late-game priced). The nickel-iron ingot returned to the
0.1.0 price of 20 cr (5 cr/kg) in 0.15.0; 0.1.1 had raised it to 24 cr only so
the steel charge would lose value under the retired rule. Carbon stayed at
10 cr. Since 0.26.0 they are 220 and 38 cr. The machines themselves carry the late-game price (see [the equipment economy](../equipment-economy.md#manufacturing-011-late-game-plant)).

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

## The leach unit and the evaporite crust (0.18.0)

Feedstock round three, as approved by the owner on 30 September 2026 (brand, chunk
composition, formulation pricing, both struvite routes and the shared charge
engine; the worked design is in [the round-three record](feedstock-round-three-design.md)).
The Lixivar LC-3 runs on the same charge engine as the V4, with a crew-selected
recipe, 12 kW working power and 15% of it into the room. The first route to struvite
(from the crust's own phosphate) is implemented here; the sulfuric-acid route comes
with the acid plant.

**The chunk (authored).** McCoy et al. 2025 (NASA OSIRIS-REx sample team, Nature,
below) report a complete evaporite sequence in Bennu samples: sodium and calcium
carbonates, sodium sulfate, sodium and potassium chlorides, sodium fluoride, calcite
and a magnesium-sodium phosphate. The paper gives no bulk fractions we could read,
so the 10 kg evaporite crust's proportions are ours: clay matrix 6.30 kg, halite
1.20, sylvite (KCl) 0.60, thenardite (Na2SO4) 0.60, trona 0.50, magnesite 0.50,
NaMgPO4 0.25 and villiaumite (NaF) 0.05 kg. It is deliberately richer than bulk
rock, as the ammonium salt crust is, and takes a 5% share of the C-class roll from
silicates.

| Recipe (machine, revision) | Reaction | Charge | Products | Energy |
| --- | --- | --- | --- | --- |
| Evaporite leach (leach 1) | 2 KCl + Na2SO4 -> K2SO4 + 2 NaCl, KCl limiting (8.048 mol) | 1 crust; 20 kg of water circulating | K2SO4 0.70 kg (0.701); phosphate concentrate 0.25; leached residue 6.80 (matrix and magnesite); brine salt cake 2.25 (2.248: halite, NaCl made, leftover Na2SO4, trona, NaF) | 12 kWh, 1 h |
| Struvite (leach 2) | NaMgPO4 + NH3 + 7 H2O -> MgNH4PO4.6H2O + NaOH (1.757 mol) | 1 concentrate; 0.03 kg NH3 and 0.22 kg water drawn | struvite 0.43 kg (0.431); caustic remainder 0.07 (0.070) | 1 kWh, 5 min |
| Makeup formulation (leach 3) | blending, no reaction | 1 K2SO4 and 2 struvite (1.56 kg) | 39 Groundwork makeup salts packets of 40 g | 0.5 kWh, 2.5 min |
| Calcine (refinery 7) | MgCO3 -> MgO + CO2 (5.930 mol) | 1 leached residue (6.80 kg) | CO2 0.26 kg (0.261) to a linked carbon dioxide store; calcined residue 6.54 (6.539) | 3 kWh, 7.5 min; the reaction absorbs 0.19 kWh |

Molar masses are the IUPAC 2013 conventional atomic weights to three decimals.
Rounding to item units puts the leach's gram into the salt cake and the struvite
step's 2 g into the water drawn; the formulation's unit masses were chosen so it
leaves nothing. Each feed leaves exactly one terminal remainder (brine salt cake,
caustic remainder, calcined residue); intermediates are feeds in their own right.

**Our simplifications.** The glaserite route (US patents 4,215,100 and 6,143,271,
below) runs through K3Na(SO4)2 in cooled crystallisers with yields up to about 98.6%;
we use its overall stoichiometry with complete potassium recovery. Struvite
precipitation is a mature technique for recovering phosphorus from wastewater at an
equimolar Mg:N:P ratio and pH 8 to 9.5 (systematic review, Environmental Evidence
2020, below); making it from the crust's own sodium magnesium phosphate, with the
sodium leaving as hydroxide, is our simplification. Villiaumite and trona stay in
the cake; the recipes do not pretend to separate them.

**Energy (authored).** Dissolving the crust and evaporating 20 kg of water in a
closed crystalliser with condensate recovery is set at 12 kWh, about a quarter of
open-pan evaporation (2.26 MJ/kg, NIST, below), because the condenser returns most
of the latent heat. The calcine's absorbed heat follows the formation enthalpies of
MgO (-601.6 kJ/mol) and CO2 (-393.5 kJ/mol, CODATA key values) and magnesite
(-1113.3 kJ/mol, Robie and Hemingway 1995, below): about 118 kJ/mol, 0.19 kWh per
charge, which the engine takes out of the machine's room heat (never below zero).

**Value.** Leaching earns about 54 cr of salts from a 150 cr crust: a loss taken for
what the ship can use, as with the salt crust. Struvite (17 cr) stays within half
again its concentrate (12 cr) and reagents. The formulation turns about 76 cr of
salts into 39 packets at Agriculture's own 30 cr, the owner's exception to the 1.5 x
guardrail (30 September 2026): formulation is where a finished nutrient's value is
made, the packets are capped at Agriculture's price, and no merchant sells the salts,
so no trade loop pays. The native checks enforce all three.

**Brand.** Lixivar, from lixiviation, the chemists' word for leaching; no chemical,
mining or water-treatment company of that name was found in a web search on 30
September 2026. The sulfuric acid plant and acid tanks join the same brand.

## The acid plant, the sulfide nodule and acid tanks (0.19.0)

The second struvite route needs acid, and the game has no sulfur, sulfide or
sulfate item; it has H2SO4 only as a room gas species with poisoning bands and a
GasPrices entry (3.1 cr/kg). Round three therefore adds a mined **sulfide nodule**,
the **Lixivar SA-3 Sulfuric Acid Plant** and bunded **Lixivar AT-2 to AT-4 acid
tanks** (owner decision, 30 September 2026, to bring in sulfur and acid).

**The nodule (authored).** Iron meteorites carry troilite (FeS) and schreibersite
((Fe,Ni)3P) inclusions (Buchwald 1975, below). The 10 kg nodule is authored as
7.000 kg of troilite, 1.058 kg of phosphide written as Fe2NiP and 1.942 kg of
silicate (the phosphide share is chosen so one flask holds exactly the phosphorus
of three struvite units for the acid route below); it takes a tenth of the meteoric-iron share of M-class (0.04) and S-class
(0.02) finds.

| Step | Reaction | Per nodule |
| --- | --- | --- |
| Roast | 4 FeS + 7 O2 -> 2 Fe2O3 + 4 SO2 | 79.63 mol of troilite |
| Convert | SO2 + 1/2 O2 -> SO3 (vanadium pentoxide bed in real plants) | |
| Absorb | SO3 + H2O -> H2SO4 | 7.809 kg of acid, rounded 7.81 |
| Phosphide | Fe2NiP + 13/4 O2 + 3/2 H2O -> Fe2O3 + NiO + H3PO4 | 5.254 mol; 0.515 kg of phosphoric acid |

Inputs 10 kg of nodule, 6.28 kg of oxygen (drawn from a linked oxygen store) and
1.58 kg of water (drawn from a linked vessel); outputs 7.81 kg of acid (deposited
into a linked acid tank), a 0.515 kg phosphoric acid flask and 9.535 kg of roasted
calcine (Fe2O3, NiO and the silicate, terminal). Both sides are 17.86 kg; the
water and the calcine each take three grams of rounding. Real acid plants dry the air, absorb in 98% acid and
dilute; the charge collapses that to the overall stoichiometry.

**Heat.** Formation enthalpies (NIST and CODATA key values, from memory; verify
before quoting) give about 840 kJ released per mole of troilite through to liquid
acid, and about 1,750 kJ per mole of phosphide (its own formation enthalpy is
estimated, -160 kJ/mol): 21.1 kWh a nodule, recorded as the recipe's `reactionKWh`
and put into the room over the charge on top of 15% of the plant's 4 kW. The
room's existing 10 kPa / 40 C bounds apply, so the plant needs a large or cooled
room; vacuum is not free cooling.

**Acid tanks.** 98 wt% acid is about 1,836 kg/m3 at 20 C (CRC Handbook, from
memory); the gas stores' 0.787 m3 vessel at an 80% fill holds 1,156 kg, authored
1,150 kg in a 240 kg bunded carbon-steel housing (98% acid passivates carbon
steel). The tanks are a separate liquid-store family, never gas stores: the P1
manifold, L2 filler, A2 regulator and RCS feed key off the gas stores, and a gas
store's damage releases its contents into the room. A damaged acid tank isolates
its acid in the bund (Framework's Isolate policy) after an authored 1e-4 of the
service contents enters the room as H2SO4 mist; a destroyed tank mists the same
share and the rest is logged as lost. The fraction is the order of the airborne
release fractions for spilled liquids in the US Department of Energy handbook
DOE-HDBK-3010-94 (value not re-read; an authored gameplay figure). Stations sell
acid into a tank at the game's own GasPrices figure.

**Value.** The roast's sellable products (a 30 cr flask and trash) stay within
half again the 150 cr nodule before its oxygen and water; the acid itself is a
commodity with no sell route. The native checks prove it at live prices.

## Acid consumers on the LC-3 (0.20.0)

Round three's last phase puts the SA-3's acid and flask to work in the LC-3,
which gains an acid tank link (drawn) and a nutrient hopper link (deposited). The
three recipes are machine `leach` revisions 4 to 6. Molar masses are IUPAC 2013;
every balance is exact to the gram in the pack and checked in `LeachChecks`.

**Epsom salt from olivine (`olivine-epsom`, revision 4).** Olivine dissolves in
sulfuric acid to metal sulfates and silica; this is the basis of the olivine
process for neutralising waste acid and making precipitated silica, whose
dissolution kinetics R. C. L. Jonckbloedt measured (Journal of Geochemical
Exploration 62, 1998, below). The game's olivine chunk (`ItmMineral02`, 10 kg,
180 cr) is authored as 7.000 kg of olivine and 3.000 kg of pyroxene, feldspar and
chromite that the acid leaves. The olivine is given the Fa29 composition that
Nakamura et al. (Science 333, 2011, below) report for the Itokawa particles JAXA's
Hayabusa returned, an LL-chondrite olivine; the split between olivine and other
rock is ours.

| Step | Reaction | Per chunk |
| --- | --- | --- |
| Leach | (Mg0.71Fe0.29)2SiO4 + 2 H2SO4 -> 1.42 MgSO4 + 0.58 FeSO4 + SiO2 + 2 H2O | 44.03 mol of olivine; 8.636 kg of acid |
| Crystallise | MgSO4 + 7 H2O -> MgSO4.7H2O (and the iron sulfate as its heptahydrate) | 9.518 kg of water drawn, net of the 1.586 kg the leach makes |

32 units of Epsom salt crystallise (13.824 kg, 0.432 kg each, 89.7% of the
magnesium); the remaining magnesium sulfate stays in the liquor soaked into the
cake. The iron stays with the cake as iron(II) sulfate: a real plant would
oxidise and precipitate the iron before crystallising, and we leave that out (our
simplification). The terminal olivine leach cake holds the rock, 2.645 kg of
silica, 7.100 kg of iron sulfate heptahydrate and 1.586 kg of retained Epsom
liquor, 14.330 kg. Both sides are 28.154 kg. Formation enthalpies (forsterite
-2173.0, fayalite -1478.2, epsomite -3388.7, melanterite -3014.6, amorphous
silica -903.5 kJ/mol; NBS tables and Robie and Hemingway 1995, below, from memory
except epsomite; verify before quoting) give about 434 kJ released per mole of
olivine: 5.3 kWh a charge into the room on top of 15% of the LC-3's 12 kW.

**Acid-route struvite (`struvite-acid`, revision 5).** The owner asked for both
struvite routes. With phosphoric acid, magnesium sulfate and ammonia:

| Reaction | Per flask |
| --- | --- |
| H3PO4 + MgSO4.7H2O + 3 NH3 -> MgNH4PO4.6H2O + (NH4)2SO4 + H2O | 5.255 mol of phosphoric acid (0.515 kg), 3 Epsom salt (5.258 mol), 0.269 kg of ammonia |

Products: three struvite units (1.290 kg, 5.257 mol), three ammonium sulfate units
(0.696 kg; 0.232 kg each), 0.094 kg of water returned to the linked vessel. Both
sides are 2.080 kg; the ammonium sulfate carries a gram and a half of rounding and
the water gives up one gram. The flask size follows from the nodule: its
phosphide share (1.058 kg of Fe2NiP) was set before the SA-3 was published so
that one flask holds the phosphorus of exactly three struvite units, and the
Epsom unit (0.432 kg, 1.753 mol) carries the magnesium of one. The reaction heat,
about half a kilowatt-hour a charge, is not modelled: struvite's formation
enthalpy is too uncertain in the sources at hand. 2 kWh authored.

**Crop nutrients (`crop-nutrients`, revision 6).** The complete formulation
blends one unit of each salt and neutralises drawn ammonia with drawn acid in the
mixer (2 NH3 + H2SO4 -> (NH4)2SO4 on 7.39 mol), which is how fertiliser
granulation plants add ammonium sulfate (the ammoniation step). 0.252 kg of
ammonia and 0.725 kg of acid make 0.977 kg of ammonium sulfate in the blend; with
the four salts (1.794 kg) that is 2.771 kg of crop nutrients, deposited into the
linked Agriculture hopper as its commodity `crop nutrients`. The neutralisation
releases about 275 kJ per mole (NBS tables; from memory, verify), 0.56 kWh a
charge. 1 kWh authored.

The nitrogen top-up is sized so nitrogen to potassium matches Hoagland solution
(N 210, K 235 mg/L; Hoagland and Arnon, Circular 347, 1950, below). The blend is
then, by mass, about 10.1% N, 2.0% P, 11.3% K, 3.1% Mg and 17.3% S: phosphorus
and magnesium land near Hoagland's ratios, sulfur far above them, and there is
no calcium, nitrate or trace element. An all-ammonium, sulfate-heavy feed would
not suit real crops; it is not a hydroponic recipe. Agriculture's crop model uses
only the aggregate nutrient figure, so the composition is recorded, not simulated.

**Why no separate ammonium sulfate charge.** The plan named a fourth recipe making
ammonium sulfate from ammonia and acid alone. The charge engine binds at least
one item unit (a saved charge with a recipe and no units is treated as damaged
evidence), so an item-free charge would weaken the save guard. The formulation's
ammoniation does the same chemistry where it is used, and the acid route makes
ammonium sulfate as an item for the blend's one unit.

**Value.** The olivine charge's 32 Epsom salt (7 cr each, 224 cr) stay within half
again its inputs (180 cr of ore, 26.8 cr of acid and 95 cr of water at their
station prices). The acid route's three struvite and three ammonium sulfate (75 cr)
stay within half again the flask and three Epsom salt (51 cr) before the ammonia.
Crop nutrients take Agriculture's own 1,500 cr/kg under the owner's formulation
decision (30 September 2026). A hopper can bag them into ordinary bulk charges,
which sell, so the formulation earns about 4,150 cr a charge from about 75 cr of
salts. Every salt in the blend is made aboard from mined feed (no merchant sells
them), so bought stock alone never pays; the bought ammonia and acid, about 3 cr
a charge, cannot run without those salts. The native checks prove each at live prices.

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

## Straw from Agriculture (0.37.0)

Owner decision of 4 October 2026: crop waste goes both ways. Agriculture 0.44.0's
B2 presses residue into fixed 1 kg straw bales (0.87 kg CH2O-equivalent organic
matter, 0.03 kg minerals, 0.10 kg water); the V4 takes them under the
`agriculture-straw` requirement, met when Agriculture 0.44.0 or newer has published
the bale at 1 kg. Molar masses IUPAC 2013 (CH2O 30.026, O2 31.998, CO2 44.009,
H2O 18.015, CH4 16.043, C 12.011 g/mol).

- **straw-burn** (revision 13): CH2O + O2 -> CO2 + H2O on 28.975 mol. 0.927 kg of
  oxygen drawn; 1.275 kg of CO2 to a linked carbon dioxide store; 0.522 kg of
  reaction water plus the bale's 0.100 kg, 0.622 kg to the water vessel; 0.03 kg of
  plant ash (terminal). Heat 467.1 kJ/mol (glucose -1273.3 kJ/mol as the stand-in,
  NIST Chemistry WebBook; CO2 and liquid water CODATA key values), 3.76 kWh into the
  room. Complete combustion is authored.
- **straw-char** (revision 14): four bales, 3.48 kg organic matter (115.90 mol C,
  231.8 mol H, 115.9 mol O). 1.000 kg of carbon stock (83.26 mol, 29% of the dry
  organic matter, authored within the 25 to 35% slow-pyrolysis char yields Antal and
  Gronli 2003 report; unverified against the paper). The rest closes by element:
  16.33 mol CO2 (0.719 kg, into the room), 16.29 mol CH4 (0.261 kg, to a linked methane
  store) and 83.27 mol water (1.500 kg) plus the bales' 0.400 kg; four plant ash.
  About 6.85 MJ released, 1.90 kWh, with the same enthalpy basis.

Value: the burn is a supply charge (no finished item). The char is the owner's
exception: four waste bales (1 cr each, sold by no one) become a carbon stock, so the
native value check accepts it rather than the step rule.

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
- McCoy, T. J. et al. (2025), "An evaporite sequence from ancient brine recorded
  in Bennu samples", *Nature* 637, 1072-1077. NASA OSIRIS-REx sample analysis team,
  led from the Smithsonian Institution. https://www.nature.com/articles/s41586-024-08495-6
  — the minerals of the evaporite crust; the proportions are ours (the paper's bulk
  fractions were not readable behind the journal login).
- US patent 4,215,100 and US patent 6,143,271, glaserite processes for potassium
  sulfate from potassium chloride and sodium sulfate.
  https://patents.google.com/patent/US4215100A/en ,
  https://patents.google.com/patent/US6143271A/en — the leach's overall
  stoichiometry; the yield figure quoted is *from memory of the patents, verify
  before quoting*.
- Struvite recovery systematic review, *Environmental Evidence* 9:34 (2020).
  https://environmentalevidencejournal.biomedcentral.com/articles/10.1186/s13750-020-00211-x
  — struvite precipitation at equimolar Mg:N:P and pH 8 to 9.5, and its use as a
  slow-release fertiliser. *Author list not re-read; verify before quoting.*
- Robie, R. A. and Hemingway, B. S. (1995), Thermodynamic Properties of Minerals
  and Related Substances at 298.15 K and 1 Bar, US Geological Survey Bulletin 2131
  — magnesite formation enthalpy for the calcine, forsterite and fayalite for the
  olivine charge. *Values from memory of the tables; verify before quoting.*
- Cox, J. D., Wagman, D. D. and Medvedev, V. A. (1989), CODATA Key Values for
  Thermodynamics — MgO and CO2 formation enthalpies. *From memory; verify.*
- Buchwald, V. F. (1975), Handbook of Iron Meteorites, University of California
  Press — kamacite/taenite nickel contents, troilite, schreibersite and
  cohenite inclusions; the premise of the sulfide nodule, not its authored mix.
- US Department of Energy, DOE-HDBK-3010-94, Airborne Release Fractions/Rates and
  Respirable Fractions for Nonreactor Nuclear Facilities (1994) — the order of
  the acid tank's mist fraction. *Value not re-read; verify before quoting.*
- CRC Handbook of Chemistry and Physics — density of concentrated sulfuric acid.
  *From memory; verify the edition and value.*
- Jonckbloedt, R. C. L. (1998), Olivine dissolution in sulphuric acid at elevated
  temperatures: implications for the olivine process, an alternative waste acid
  neutralizing process, Journal of Geochemical Exploration 62, 337-346
  ([ScienceDirect](https://www.sciencedirect.com/science/article/abs/pii/S0375674298000028))
  — olivine dissolves in sulfuric acid to metal sulfates and silica, controlled
  by surface reaction. The basis of the Epsom salt charge, not its yield.
- Nakamura, T. et al. (2011), Itokawa dust particles: a direct link between S-type
  asteroids and ordinary chondrites, Science 333, 1113-1116
  ([doi:10.1126/science.1207758](https://www.science.org/doi/10.1126/science.1207758))
  — JAXA Hayabusa's Itokawa particles are LL-chondrite material with olivine
  near Fa29. Used for the olivine's composition only; the chunk's olivine share
  is ours.
- Hoagland, D. R. and Arnon, D. I. (1950), The Water-Culture Method for Growing
  Plants without Soil, California Agricultural Experiment Station Circular 347
  — the nutrient solution whose N 210 and K 235 mg/L set the blend's nitrogen.
  *Concentrations as commonly tabulated; the circular itself not re-read.*
- Wagman, D. D. et al. (1982), The NBS Tables of Chemical Thermodynamic
  Properties, Journal of Physical and Chemical Reference Data 11, Supplement 2
  — epsomite, melanterite, ammonium sulfate and amorphous silica formation
  enthalpies. *From memory except epsomite (-3388.7 kJ/mol, confirmed in the
  magnesium sulfate hydrate literature); verify before quoting.*
- ASM Handbook, Volume 1: Properties and Selection: Irons, Steels, and
  High-Performance Alloys — carbon ranges of steels.
- Blue Bottle Games, Ostranauts 1.0.1.5 — item texts, molar masses, canister
  capacity formula, gas conditions, fire and explosion mechanics (locally
  inspected; not redistributed).

Our inference, simplified model and authored balance are stated as such in each
row; the percentages chosen are within the cited ranges but are gameplay
choices, and item-unit rounding is explicit.

## Ethanol tanks and fire (0.38.0)

The Alembrine Cask-2, Cask-3 and Cask-4 hold ethanol as a kilogram record: 495 kg in
the Cask-2 (789.3 kg/m3, CRC Handbook, to re-check before quoting, in the shared
0.787 m3 vessel at 80%). Ethanol has no game species, so a damaged tank cannot mist.
Instead, an authored 5% of its contents burns as C2H5OH + 3 O2 -> 2 CO2 + 3 H2O
(1,366.8 kJ/mol, NIST Chemistry WebBook: 29,670 kJ/kg, 2.084 kg O2 and 1.911 kg CO2
per kg), and only when the room has at least 5 kPa of oxygen and an ignition source.
The water vapour has no species and leaves with the blast, as methane's does. The
refuelling kiosk buys ethanol back at 45% of an authored 20 cr/kg and never sells it
(owner decision, 4 October 2026).
