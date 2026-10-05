# Custom gases: research study

6 October 2026. Owner request: study, as research only, whether new gases can be added to the
game, whether the Phobos mods have already crossed the point of no return for game-modifying
code and save data, and where the mods would benefit, starting with water vapour from
Agriculture's crops.

**Status: research.** Nothing here is implemented. The standing rule "no custom gas species"
(AGENTS.md; owner direction of 29 September 2026) stays in force until the owner decides
otherwise. The [decision](#decision-for-the-owner) section lists the options.

**Evidence.** The game's `Assembly-CSharp.dll` (Ostranauts 1.0.1.5) was inspected read-only by
reflection: the fields and methods of its gas classes, and the calls, fields and string constants
inside the methods named below. No decompiled source was saved. The game's data files under
`StreamingAssets/data` were searched. Statements marked **observed** come from that inspection;
statements marked **inferred** follow from it but were not run in the game; **untested** points
need the [experiment](#the-experiment-owner-run).

## Summary

1. **Three gases need no code at all.** The game's own species list already holds water vapour
   (H2O), hydrogen (H2) and "He2". They do nothing only because the data files never define
   their two conditions. Defining those conditions in a mod's data folder should switch them on
   (inferred; the experiment confirms it).
2. **A truly new gas needs a little code**: three list entries and two small patches.
3. **The threshold was crossed long ago**, in code and in saves. A dormant gas is less invasive
   in code than much of what the mods already do. What is new is where its data lives: in the
   game's own rooms, spreading wherever air flows.
4. **Water vapour is the clear first use.** Hydrogen is the larger gameplay change. A truly
   custom gas is not worth it yet.

## How the game handles gases

- **Species list (observed).** `FluidStrings` holds three parallel static lists built from
  eleven names: CH4, CO2, H2, H2O, H2SO4, He2, N2, NH3, O2, CO, Smoke. The lists are the
  names, the amount conditions (`StatGasMol<name>`) and the partial-pressure conditions
  (`StatGasPp<name>`).
- **Mass and density (observed).** `GasContainer.GetGasMass` and `GetGasDensity` are switches
  over the same eleven names; H2O, H2 and He2 are among the cases. An unknown name gets zero.
- **Planet atmospheres (observed).** `JsonAtmosphere` has a field for each of the eleven,
  including `fH2O`, `fH2` and `fHe2`, and `GasContainer.SyncAtmo` writes all eleven amount
  conditions. No shipped data file sets `fH2O`, `fH2` or `fHe2`, so they are zero everywhere.
- **Data definitions (observed).** `conditions/conditions.json` defines `StatGasMol` and
  `StatGasPp` conditions for eight gases only. None exists for H2O, H2 or He2, anywhere in the
  game's data or in any mod installed on the owner's machine.
- **Why the three do nothing (observed).** `GasContainer.AddGasMols` first calls
  `DataHandler.GetCond("StatGasMol" + name)` and returns without a word when there is no such
  definition. `CondOwner.AddCondAmount` does the same for any condition it does not already
  hold. So every write of an undefined gas is silently dropped. This corrects the cause given
  in the [chemical storage record](chemical-storage-and-process-fluids.md) and the earlier
  AGENTS.md wording ("AddGasMols ignores them"): the effect was right, the reason is the
  missing data definition, not the code.
- **The simulation step (observed).** `GasContainer.Run` applies pending amounts, recomputes
  the total and the pressure, then, for each gas the room holds, finds its position in the
  species list and uses that position to set its partial pressure and add its mass. A name
  that is not in the list gives position -1 and the list access throws.
- **Mixing (observed).** `GasExchange` moves gas between rooms from the rooms' own
  dictionaries, with no species list. Any gas a room holds spreads like the others.
- **Pumps, scrubbers, breathing (observed).** `GasPump.Pump` and `Respire2` act on the gases
  named in `gasrespires` data. Vanilla equipment would neither remove nor react to a new gas
  unless data is added for it.
- **Displays (observed).** The game's readouts hard-code the eight gases (`GUIItemList.Update`,
  `GasModule.Awake`, the helmet display's O2 and CO2). A new gas would not appear on them.
  Total pressure would include it.
- **Crew (observed and inferred).** Suffocation, fire chance and the survival pledges read
  `StatGasPpO2` and `StatGasPpCO2`. Adding another gas raises total pressure and leaves the
  oxygen partial pressure unchanged (inferred from how `Run` computes it: share of moles times
  total pressure). The game's total-pressure checks would see the extra pressure.

## Two tiers

| Tier | Gases | What it takes | Risk in code |
| --- | --- | --- | --- |
| Dormant | H2O, H2, He2 | Two condition definitions per gas in a mod data folder (`StatGasMol<gas>`, `StatGasPp<gas>`, modelled on oxygen's). Mixing, pressure, mass, saving and room merging are the game's existing code. | No patch to the gas code |
| Truly custom | for example ethanol vapour | The conditions, plus: append the name to the three `FluidStrings` lists at start-up, and postfix `GetGasMass` and `GetGasDensity` so the gas has mass. | Two small Harmony patches and a list edit; a missed list entry throws in `Run` every step |

Either tier also needs our own work to be useful: a way to show the gas (our panels, since the
game's readouts will not), a sink so it cannot build up for ever, and rules for what it does.

## Has the threshold been crossed?

Yes, some time ago, and deliberately.

**What the mods already do (counted in the source, 6 October 2026):**

- 152 Harmony patches on the game's methods: 75 in Framework, 23 in Shipbreaker, 16 in Auto
  Nav, 14 in Manufacturing, 11 in Agriculture, 7 in War Has Been Declared and 6 in Medical.
- One hand-written transpiler, which rewrites the instructions of the game's `Wound.Run`
  (Medical's weightless care).
- In-place amendments of the game's own definitions (kiosk tier triggers, Ship's Water tanks,
  study chains, loot tables).
- Saved state on game objects: records in property maps, `StatPhobos*` conditions, our items,
  machines and build sites in ship files, and line contents in pipe segments.
- The Workshop pages already say that removing the mods is not a clean uninstall.

**How a gas compares.** In code a dormant gas is *less* invasive than most of the above: it
adds data and patches nothing in the gas system. The difference is in the reach of its data:

| | Today's saved state | A gas |
| --- | --- | --- |
| Where it lives | On our own machines, items and pipes | On the game's rooms |
| How far it spreads | Stays where the player put it | Through every open door and vent, into docked ships and stations |
| Who reacts to it | Our code | The game's pressure checks, plus anything that reads total pressure |
| On removing the mod | Our objects are orphaned in place | See below |

**Removal (inferred, with one untested point).** With the definitions gone, the game would
drop the gas's amount when it next writes it, because undefined conditions are ignored. What is
not known is the state of a room loaded from a save that still lists the condition: whether the
loader skips it cleanly, and whether the room's saved total and pressure, which included the
gas, correct themselves or stay too high until the room is next recomputed. That is the main
thing the experiment must show. If it leaves a stale pressure, a small load-time cleanup in
Framework could remove our gases before a mod is retired, as it already retires old items.

**Verdict.** Adding a dormant gas does not take the mods into new territory for risk. It does
move saved data into a new place, the game's rooms, and that deserves its own test and its own
cleanup path before it ships.

## Where the mods would benefit

Ranked by value against cost.

1. **Water vapour (H2O), dormant tier. Recommended first.**
   - *Today:* a rack's spare transpiration and all misted water either go to a linked tank or
     vanish, with the record saying "the game's air holds no humidity". Agriculture 0.59.0
     misting can lose 0.6 kg an hour this way.
   - *With humidity:* that water goes into the room as vapour and stays in the ship's books. A
     condenser returns it to a water tank: the rack's own, a Ship's Water tie-in, or a small
     dehumidifier. The water loop closes honestly.
   - *Further uses:* humidity as a crop factor in the crops data pack (beside `co2Response`);
     steam from the T2 ice thaw unit and the F6 furnace's cooling; a dry-air need for stored
     goods, if ever wanted.
   - *Scale (our arithmetic):* a 40 m3 room at 100 kPa and 300 K holds about 1,600 mol of air.
     Air at 25 C saturates near 3.2 kPa of water vapour, about 51 mol or 0.9 kg in that room. A
     wheat crop transpires 0.3 kg over its cycle; misting at full rate adds 0.6 kg an hour. So
     misting would saturate a room in under two hours: a cap at saturation with condensation is
     required, not optional. The saturation figure is the standard value for water and still
     needs a cited source before it is used in a design.
   - *Risk:* without a sink it accumulates and raises cabin pressure. The game's scrubbers will
     not remove it.
2. **Hydrogen (H2), dormant tier. Worth doing, larger change.**
   - *Today:* a damaged H2 store "vents overboard", and the X2 cell's hydrogen can only go to a
     store. That exists only because hydrogen could not enter a room.
   - *With H2 as a gas:* a leaking store fills its room, and the existing `Combustion` service
     can burn it there, as it does methane and carbon monoxide. This is honest chemistry and a
     real hazard. It changes safety for every Manufacturing player, so it needs warnings and a
     migration note, and the game's own fires would not know hydrogen burns unless we add that.
3. **"He2".** Present in the species list, with no use in the mods today. Set aside.
4. **A truly custom gas (ethanol vapour for Alembrine spills).** The spill already burns
   through `Combustion`. A vapour would add realism for two patches and a permanent risk in
   `Run`. Not recommended yet.

## The experiment (owner-run)

No shipped code. One throwaway local data mod, kept in ignored `.local/`, holding two
definitions copied from oxygen's (`StatGasMolH2O`, `StatGasPpH2O`), and one F3 command to add a
known amount of H2O to the room the selected crew member stands in. On a **copy** of a save:

1. Add 10 mol to one room. Expect total pressure to rise by about what 10 mol of any gas adds,
   and oxygen's partial pressure to stay the same.
2. Open a door to the next room. Expect the vapour to spread.
3. Save, load, and read both rooms again. Expect the same amounts.
4. Check the game's readouts: the vapour should be missing from the gas list and present in
   total pressure.
5. Run a scrubber and an air pump. Expect neither to remove it.
6. Remove the data mod, load the save, and note: any error in `Player.log`, each room's
   pressure, and whether it settles after a door cycle or a time-skip.

Report the log and the six readings. They go into this record before any design starts.

## Decision for the owner

| Option | What it means | What it would touch first |
| --- | --- | --- |
| A. Keep the rule | No gas beyond the eight; vapour keeps going to a tank or being lost | Nothing |
| B. Allow dormant gases, H2O first (recommended, after the experiment) | Humidity as a real gas, with a saturation cap, a condenser path and our own readout | Framework `RoomGas` (a ninth species, clamps, a retire-on-removal cleanup), Agriculture's vapour paths, the crops pack, guides |
| C. B plus hydrogen | Hydrogen leaks into rooms and can burn | Manufacturing's stores and X2, `Combustion`, hazard warnings, a migration note |
| D. Also truly custom gases | Any vapour, with patches to the game's gas code | Framework patches on `GetGasMass` and `GetGasDensity`, list edits, a native check that every registered gas is in the list |

Each of B to D would open with its own design record and owner decisions, and would replace
the "no custom gas species" wording in AGENTS.md with the rule chosen.
