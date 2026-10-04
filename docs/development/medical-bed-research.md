# The vanilla Infirmaway medical bed: what it does

Research record, 4 October 2026, for the owner's request to expand on the game's
medical bed. Read from the installed game's data (Ostranauts 1.0.1.5,
`Ostranauts_Data/StreamingAssets/data/`) and the locally decompiled assembly (not
committed). **Nothing here was tested in play.** Items marked *(inferred)* come from
reading code. The design that follows from it is in the
[Phobos Medical design record](medical-bed-design.md).

## Summary

The Van Buren "Infirmaway" Medical Bed (`ItmBedMedical01`) does one thing: sleeping
in it applies the game's **Recuperating** condition (`SleepingMedical`). Its
description's "nano-robotic autodoc", "ease of medical intervention" and care "in
both weightless and high-G environments" have nothing behind them in data or code.

The engine never names the bed, `IsBedMedical` or `SleepingMedical`. The bed is
entirely data: conditions, loot, condition tests, interactions and power info.

## Forms

`condowners/condowners.json` 6874-7114; items in `items/items.json` 3666-3865.

| Form | Id | Notes |
| --- | --- | --- |
| Installed, on | `ItmBedMedical01` | `IsBedMedical`, `IsPowered`, offers `SeekSleepSimple` |
| Installed, off | `ItmBedMedical01Off` | `IsOff`; still offers sleep, as a plain bed |
| Installed, damaged | `ItmBedMedical01Dmg` | no interactions |
| Loose, loose damaged | `ItmBedMedical01Loose`, `ItmBedMedical01DmgLoose` | pick up and drop |

3 x 5 tiles (48 x 80 px), 88 kg, base price 18,570 (the Sleeping Bunk is 2,280),
three power points, no inventory, no wall requirement, `IsCategoryMedical`,
`IsSalvageValueHigh`. It lacks `IsComfortable`, so it never gives the bunk's
Refreshed buffs.

## Medical sleep

- The bed offers only the generic `SeekSleepSimple` (walk to point `use`, teleport to
  point `sleep`). Its replies are tried in order; the first is
  `SeekSleepSimpleLieDownMedical`, which needs the bed to pass `TIsBedMedical`
  (`IsBedMedical`, `IsInstalled`, not `IsDamaged`, not `IsOff`, and `TIsBed`) and the
  sleeper to pass `TCanSleep` (which forbids `Unconscious`).
- It applies `SleepingMedical`, whose effect `CONDSleepingMedicalPer`
  (`loot/loot.json:2305`) adds wound heal rate +0.2, blood heal rate +0.0045,
  infection heal rate +0.05, pain threshold +0.1, security threshold +0.4 and sleep
  comfort threshold -0.3. The condition is clamped at 1.
- Sleep itself sets `Unconscious`, whose own effect (`CONDUnconsciousPer`) adds wound
  +0.05, blood +0.0089 and infection +0.2. Some healing comes from sleeping anywhere.
- The sleep loop (`SeekSleepSimpleCont`) tests only the sleeper, never the bed.
- It ends when the sleeper wakes rested (`TIsWakeUp`), cancels, or is turned away
  for not being sleepy enough (`SeekSleepSimpleDeny`).

Rates on the patient (base `Crew01`: wound 0.1, blood 0.0347, infection 0.6):

| | Awake | Asleep anywhere | Comfortable bunk | Infirmaway |
| --- | --- | --- | --- | --- |
| Wound heal rate | 0.10 | 0.15 | 0.20 | 0.35 |
| Wound shrink per hour, uninfected | 5% | 7.5% | 10% | 17.5% |
| Wound half-life | 13.9 h | 9.2 h | 6.9 h | 4.0 h |
| Blood recovered per hour | 0.0347 | 0.0436 | 0.0436 | 0.0481 |
| Infection cleared per hour | 0.60 | 0.80 | 0.80 | 0.85 |
| Pain eased per hour | 0.52 | 0.65 | 0.65 | 0.72 |

The shrink and half-life rows are our arithmetic from `Wound.Run`: each wound loses
`wound x hours x rate x (1 - infection rate) x 0.5`. Bleeding also eases in
proportion to the heal rate. The bed's real advantage is wound closure; its blood,
infection and pain bonuses over ordinary sleep are about +10%, +6% and +10%.

- **Weightlessness.** `Wound.Run` multiplies the heal rate by 0.05 when the patient
  has `DcGrav01`. The bed is not exempt: its wound half-life becomes about 79 hours.
- **Time skip.** The skip tries payloads from `ACTFFWDContextPayloads` in order.
  `Tick1HourIncapacitated` comes before `Tick1HourSleepMedical` and forbids
  `SleepingMedical`; the medical hour (`CONDTick1HourSleepMedicalPhysio`) removes
  wounds 0.12, blood 0.0045, infection 0.05, poison 0.04 and radiation 0.04.
  Poison and radiation recovery exist only in the skip. The bed is never checked.

## Limits

- Self-service only; every step is sleeper and bed.
- An unconscious or carried patient cannot be put to bed. `PickupDragStart` can drag
  an unconscious, dead or stunned person, but nothing places them in a bed.
- A patient who is not tired is turned away however badly hurt.
- No AI rule sends injured crew to it; the game's AI training data has no values for
  the medical lie-down.
- Nothing treats a patient in the bed. Vanilla treatment is dragging cloth, splints
  or liquids onto wound slots in the inventory paper doll: instant, no second person,
  no skill. The seven medical skills (`SkillMedicalTrauma` and others) scale nothing.
- Nanorobotic first aid (`NanoFirstAid`) exists only as a paid kiosk service (2,500).
- There is no medical screen. The crew status bar shows pain and sleep, not blood,
  infection or wounds.

## Power

- `powerinfos/powerinfos.json:344`: `fAmount 6.25e-6` kWh per game second, 22.5 W
  (bunk 10 W). One draw value.
- It installs switched off. When supply fails, `Powered.ShutDown` switches it to the
  Off form, which gives plain sleep.
- *(inferred)* `TIsBedMedical` is tested only at lie-down and the bed keeps its id
  through form changes, so a patient already asleep keeps Recuperating after the bed
  loses power, is damaged or is uninstalled.

## Upkeep and economy

- Install and uninstall with the Mortorq under FURN (1300 each). Repair (2000) with
  Mortorq and soldering iron: motor 1, motherboard 1, clean cloth 6, small electrical
  parts 3, small mechanical parts 3, aluminium scrap 3, steel scrap 2.
- No dismantle job; the loot `ItmBedMedical01Dismantle` is orphaned.
- Sold loose, in lots of six, at 14 furnishings kiosks; the damaged loose form is a
  rare random find; 50 placements in 10 vanilla ship files.
- The loose form is matched by mark, not id: `BedMedical01Install` takes anything
  passing `TIsBedMedical01Uninstalled` (`IsBedMedical`, not installed, not damaged).
- Its only other use: the Whodunnit plot's blood and DNA analysis.

## What a mod can build on

- New actions on equipment: amend definitions in place (`DefinitionAmendments`).
- Healing rates are patient stats; new conditions on the patient change them.
- Hard-coded and reachable only with Harmony: the wound formula, the microgravity
  factor, pain scaling, knock-out handling, the closed lists of update commands,
  GUI prefabs and PDA apps.
- A three-party interaction (`CTTest3rd`) picks an unset third party at random from
  every loaded ship, so it must be set in code.
- `patient.compSlots.SlotItem(slot, item, bAuto: false)` performs the game's own
  wound slotting from code, with the item's own effects.
- The game's corpse drop shows how to release a dragged person fully:
  `UnSlotItem("drag")`, set the position, `ship.AddCO(person, true)`, set the room.

## Not verified in play

1. A sleeping patient keeping Recuperating when the bed loses power mid-sleep.
2. A sleeper whose bed is uninstalled or destroyed under them.
3. Real time against time skip over the same hours.
4. The exact effect of the `Thresh*` shifts.
