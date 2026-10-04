# Oxygen from rock: design record for the Oxsmith EC-4 and CR-4

Opened 5 October 2026 for sets 4 and 5 of the [regolith programme](regolith-programme.md).
The owner chose both routes to oxygen from the game's loose regolith, "maximum player
choice": molten regolith electrolysis (this record's first part, Manufacturing 0.52.0)
and carbothermal reduction with methane (the second part, set 5). The recipes' full
arithmetic is in the [refinery and chemistry record](manufacturing-refinery-and-chemistry.md);
this record holds what was checked, what is ours and why each choice was made.

Nothing here has been seen in the game. Offline checks are not gameplay validation.

## Part 1: the EC-4 Electrolysis Cell and ferrosilicon (set 4)

### What the sources say, and what they do not

**NASA Kennedy Space Center, molten regolith electrolysis.** L. Sibille (Southeastern
Universities Research Association, Swamp Works, NASA Kennedy Space Center), S. S.
Schreiner (Jet Propulsion Laboratory) and J. A. Dominguez (Florida Institute of
Technology), *Advanced Concepts for Molten Regolith Electrolysis: One-Step Oxygen and
Metals Production Anywhere on the Moon*, Developing a New Space Economy, 2019, abstract
5100 ([Universities Space Research Association host](https://www.hou.usra.edu/meetings/lunarisru2019/pdf/5100.pdf)).
Read for this record on 5 October 2026. It states that:

- the process was demonstrated "to produce raw feedstock materials and oxygen at high
  yield with lunar materials" under Kennedy's leadership in the 2000s;
- it is a one-step process: "direct electrochemical separation of molten metal oxides
  into oxygen and a glassy metallic product collected at opposite electrodes";
- melting the regolith, rather than dissolving it, "frees the technology from dependence
  on consumable components";
- it must run with the oxide mixture molten, and "sustained operation at temperatures
  in excess of 1600 C" is a containment problem, answered by a cold-walled cell whose
  pool is heated by its own current while the regolith at the walls stays granular.

The abstract gives **no yield in kilograms**, and none is quoted from it here.

**A yield to compare against.** NASA's lunar resource work reports, for the other route
(carbothermal reduction), an oxygen yield of more than 20 percent of the regolith's mass
(G. Sanders, *Progress Review: NASA Lunar ISRU*, NASA NTRS 20250003730; found by search
on 5 October 2026 and **not read in full**, so it is cited as a comparison only). The
same search returned a planning figure of 70 percent of the regolith's oxygen for
electrolysis, which would be about 28 percent of the rock's mass; its source was not
identified, so it is not cited.

**Reaction heats.** NIST-JANAF formation enthalpies at 298 K: FeO -272.0 kJ/mol, quartz
-910.9 kJ/mol, liquid water -285.83 kJ/mol. These are standard values, stated from the
tables rather than re-read this round.

**The silicol process.** E. R. Weaver, W. M. Berry, V. L. Bohnson and B. D. Gordon, *The
Ferrosilicon Process for the Generation of Hydrogen*, National Advisory Committee for
Aeronautics Report No. 40, 1920 ([NASA NTRS 19930091069](https://ntrs.nasa.gov/citations/19930091069)).
Its catalogue summary, read on 5 October 2026, describes hydrogen made by reacting
ferrosilicon with sodium hydroxide and water, used for military and naval balloons
because the generator is small and needs no power beyond stirring and pumping. The body
of the report was not read; the equation used below is the textbook one.

### What is ours

- **The lump's composition.** The game says only that regolith is broken rock. We author
  a chondrite-like lump: 2 percent bound water and half a percent of carbon dioxide (the
  V4 bake's figures), 5.457 kg of iron oxide and 5.045 kg of silica that the cell
  reduces, and 9 kg of oxides it does not. That is 21 percent iron by mass, which sits
  between carbon-rich and ordinary chondrites; no source sets it for the game's rock.
- **The yield.** 3.9 kg of oxygen from a 20 kg lump, 19.5 percent, follows from that
  composition and nothing else. It is below the comparison figure above on purpose.
- **Ferrosilicon as the metal.** Iron and silicon are the two oxides a molten silicate
  gives up first; magnesium, calcium and aluminium hold their oxygen harder and stay in
  the slag. A unit is 2.2 kg: 1.414 kg of iron and 0.786 kg of silicon.
- **Time and power.** One hour at 60 kW for a lump, forty minutes for a Silicates chunk.
  The reductions absorb 27.0 of a lump's 60 kWh; the other 55 percent is loss and warms
  the room (before the player's machine heat setting). Sixty kilowatts is two and a half
  V4s: this is a machine for a ship with a reactor to spare.
- **Lunar work applied to asteroid rock** is our extrapolation, as is fitting a
  cold-walled cell into a 4 x 4 shipboard machine. NASA has validated neither.

### The caustic soda the LC-3 does not have

The owner chose ferrosilicon with a use shipped in the same release: the LC-3 reacts it
with water into hydrogen, the silicol process. The real process consumes sodium
hydroxide: Si + 2 NaOH + H2O -> Na2SiO3 + 2 H2. No Phobos mod stores caustic soda, and
adding a bought reagent for one recipe would break the rule against commodities without
a wider use.

**Agent choice, open to owner revision:** the recipe is written as Si + 2 H2O -> SiO2 +
2 H2 and treats the caustic liquor as a standing charge inside the LC-3 that is
regenerated as the silicate drops out as silica. Mass and energy balance exactly on that
equation; what is simplified is that a real plant must keep buying soda. The recipe's
notes and the chemistry record say so in those words. If the owner would rather have
the soda as a real input, that is a new reagent and needs its own design note.

Worth knowing: the charge frees all of the water's hydrogen, the same 11.2 percent the
X2 does, for about a quarter of the electricity. What it costs is the oxygen, which
stays locked in the silica, and three ferrosilicon.

### Choices

| Question | Choice | Why |
| --- | --- | --- |
| Recipe selection | Automatic, from the feed | The two feeds are different items, so there is nothing to choose; an EC-4 exists to eat regolith, so it has no "leave it alone" setting |
| Links | Oxygen store and water vessel, both shown always | A lump always gives water; a cell with no oxygen store has nowhere to put its product, and oxygen is never vented into the room in bulk |
| Feed bin | Two cells | One charge at a time; lumps are 20 kg |
| Spoiling | None | A cold-walled cell that waits simply freezes and melts again; nothing is lost, unlike a V4 melt |
| Ignition source | Yes, while working | A molten pool beside a leaking fuel store is the V4's hazard exactly |
| Price | 96,000 cr, Trusted at the faction kiosks | Agent default from the programme record: half again the V4 |
| Slag | The existing refinery slag, 1 kg units | Same material, already a declared remainder; no new trash identity |
| Spent ferrosilicon | A new terminal remainder, 3.095 kg | Iron fines and silica are not slag; declared, so the RM-1 grinds it |

### Value

Both electrolysis charges are **supply**, as the approved plan records. The Silicates ore
(200 cr) is worth far more raw than its 2.6 kg of oxygen; the guides say so. Regolith is
different: a K-Leg prospector sells it, so the charge must not be a way to make money
from bought rock. Oxygen leaves a ship only through the kiosk's buy-back at 45 percent,
so the native value check judges each charge as a loop: oxygen and water at the
buy-back share, ferrosilicon at a generous 1.2 times its price, against the rock.

| | Regolith lump | Silicates chunk |
| --- | ---: | ---: |
| Rock | 35.00 cr | 200.00 cr |
| Oxygen sold back (13.2 cr/kg x 45%) | 23.17 cr | 15.44 cr |
| Water sold back (10 cr/kg x 45%) | 1.80 cr | none |
| Ferrosilicon at 1.2 x 2 cr | 7.20 cr | 4.80 cr |
| **Back** | **32.17 cr** | **20.24 cr** |

That fixes ferrosilicon at **2 cr a unit**, well below scrap steel by weight. It is a
by-product whose worth is the hydrogen it makes, and the record says so rather than
pretending it is metal stock. Its hydrogen charge has no finished product and makes no
profit claim.

### Saved structures

All new: the cell's record `ManufacturingElectrolysisCell`, its two link ports, the
LC-3's hydrogen port, leach revision 15 and the cell's revisions 1 and 2. Nothing saved
before 0.52.0 changes, so there is nothing to migrate. An LC-3 saved before gains its
hydrogen link unset.

### Artwork

The EC-4 takes the owner-approved Oxsmith master prepared on 5 October 2026
([handoff](oxsmith-art-handoff.md)), bound through the shared completion exporter with
no new generation. Ferrosilicon and spent ferrosilicon are recorded luminance recolours
of the aluminium ingot and the olivine leach cake. Damaged and loose forms share the
intact picture, as the other Manufacturing machines do.

### Owner checks

1. Install an EC-4 with its back to a wall row carrying power, touching an oxygen store
   and a water silo. Link both, load a lump of loose regolith, Start.
2. After an hour of game time: 3.9 kg more oxygen in the store, 0.4 kg more water, three
   ferrosilicon and nine slag in the tray, the room a little warmer and a trace of
   carbon dioxide in it.
3. Load Silicates ore: 2.6 kg of oxygen, two ferrosilicon, three slag in forty minutes.
4. On an LC-3, choose **hydrogen from ferrosilicon**, link a hydrogen store and a water
   silo, load three ferrosilicon, Start: 0.339 kg of hydrogen, three spent ferrosilicon.
5. Save while the cell works and reload: it carries on.

## Part 2: the carbothermal route (set 5)

Not yet written. It opens when set 5 starts, with the NASA Carbothermal Reduction
Demonstration values read first.
