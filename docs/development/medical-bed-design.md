# Phobos Medical: design record

Owner request, 4 October 2026: a new mod that expands on what the vanilla Infirmaway
medical bed does. The [research record](medical-bed-research.md) shows the bed's one
function is a stronger healing sleep. Owner decisions the same day:

- A new content mod, **Phobos Medical** (`PhobosMedical`), requiring Phobos Framework.
- A new brand, **Halewright** (see [equipment branding](equipment-branding.md)).
- **A new own-brand bed with companion equipment.** The Infirmaway is left exactly as
  the game ships it, so other mods and game updates do not collide with ours.
- All four directions: any patient in the bed; a working autodoc; a bedside monitor
  and care by a second crew member; honest power and weightless care.
- **Data packs from the start**: what players may tune or add lives in validated
  packs; the rules that act on them stay in code.
- Base healing is **the game's own Recuperating**, with Halewright extras on top.
- Artwork: a **ChatGPT base with PixelLab layers** ([handoff](medical-art-handoff.md)).

Agent defaults, open to owner revision: any tired crew member may sleep in a free
Ward-3 as in an Infirmaway (a "reserve for the injured" setting is off by default);
care needs a pressurised room, following the rule that Phobos machines do not work in
vacuum; the bed adds no Halewright healing extra of its own at first.

## The Ward-3 bed

Phobos' Halewright Ward-3 Medical Bed, family `PhobosMedicalBed` (Installed, Loose,
InstalledDmg, LooseDmg). 3 x 5 tiles like the Infirmaway, INSTALL FURN, power from
the wall row behind its head, a small bedside drawer (service rack) for dressings.

- The **installed intact form** carries `IsBedMedical`, `IsCushion` and `IsSheet`, so
  the game's own sleep chain, time skip and other mods see a medical bed. It starts
  with `IsOff`; the bed service mirrors power into `IsOff`, so an unpowered bed gives
  plain sleep exactly as the Infirmaway's Off form does.
- **Loose and damaged forms carry none of these marks**, so the Infirmaway's install
  job (`TIsBedMedical01Uninstalled`) and fixer lists never take a Ward-3.
- **Art.** Medical 0.1.0 showed the vanilla Infirmaway images by name as a stand-in; 0.1.1
  binds the owner-selected Halewright 2075 nanomedical concept on every form, drawn as low as
  the game's beds so a patient lies on top ([handoff](medical-art-handoff.md)). The cartridges
  and arms are styling; the care is unchanged.

### Patient record

The game has no bed-to-patient link. The bed keeps a saved record
(`PhobosState.MedicalBed`, version 1): patient id, route, epoch. Three routes:

| Route | How |
| --- | --- |
| Sleep | The game's own `SeekSleepSimple`, unchanged |
| Rest awake | Own chain shaped like the game's chair chain. A rester who becomes sleepy falls asleep in place through the game's own lie-down replies, which do not test for a free bed |
| Laid unconscious | Right-click "Lay patient here" while dragging a living person, using the game's corpse-drop release at the bed's sleep point (Framework `Health.PatientPlacement`) |

The service also adopts a living, unconscious person lying at the sleep point (for
example one knocked out while resting).

### Bed service

A 2-second service, the Cabin Air Regulator pattern:

- validates the record (at the sleep point, same ship, alive; asleep, resting or
  unconscious);
- keeps the bed marked `IsOccupied` for a laid patient (the game's own loops refresh
  it for sleepers and resters);
- refuses other users at offer time through a `TriggeredInternal` postfix;
- discharges when the patient leaves, dies, wakes with nothing holding them, or the
  bed is damaged, removed or destroyed. Native destruction is never blocked.

### Care and honest power

With a patient present and the bed intact, powered and in a pressurised room, the
service ensures the patient's care condition and the in-use condition on the bed,
whose higher draw the game takes through `SetPowerOverride`:

- a sleeping or unconscious patient gets the game's own `SleepingMedical`, which the
  game's wake already clears;
- a patient resting awake gets the bed's own `PhobosMedicalRecovering`, whose effect is
  exactly the game's `CONDSleepingMedicalPer`. As implemented in 0.1.0 this replaces the
  earlier idea of giving resters `SleepingMedical`: the game's social tests
  (`TIsNotSleeping`) would have counted an awake rester as asleep.

When power or air fails, the service removes the care, including the game's
`SleepingMedical` from a sleeper (a hidden `PhobosMedicalCare` mark records that the bed
gave care), and posts one notice per outage; care returns with the power. A sweep every
few passes withdraws care no bed still claims, so a bed destroyed or removed under its
patient leaves nothing behind.

Time skip: a sleeping or unconscious patient carries `SleepingMedical`, so gets the
game's medical hour; a rester's rate stats catch up through the game's own tickers but
without the medical hour's extra amounts. The service reconciles after the skip. This
is bounded, not exact: care is kept for the whole skip.

## The `care` data pack

Owned by Phobos Medical: `mods/PhobosMedical/framework/care.json`, typed pack and
validator in `src/PhobosMedical/Core/CarePack.cs`, JSON Schema `schemas/care.schema.json`,
offline checks in `scripts/validate-data-packs.py`. Player files go in
`BepInEx/config/PhobosMedical/care/`. Precedent: Agriculture's `crops`.

| Section | Keys | Holds |
| --- | --- | --- |
| `stations` | closed: `bed` (later `monitor`, `autodoc`) | idle and working kW |
| `admission` | one object | injured thresholds (blood lost, infection, pain); discharge share |
| `levels` | closed: `bed` (later `autodoc`) | Halewright extras over Recuperating; weightless healing factor |
| `treatments` | open; a file may add | test, effect kind, item or condition, medic and autodoc seconds, skill (set 4) |

Code owns a fixed vocabulary: effect kinds `slot-item` and `apply-condition`; wound
tests `bleeding`, `fracture` and `spent-dressing`; patient tests `wounded`,
`blood-loss` and `infection`. Code never branches on a treatment id.

Every file, shipped or player, must pass: kW within 0 to 2 with idle not above
working; extras and factors within caps; discharge below admission; every treatment
consumes one real unit of a known item, slot items have a native wound slot effect,
conditions are the game's own, timed, neither fatal nor knock-out; seconds within 1 to
3600; nothing writes wound stats directly. Nothing is frozen, because no save names a
treatment or a level.

Elsewhere: prices, bills, lots and kiosk tiers in the standard `economy` pack; names in
`equipment-names.json`; ids, footprint, mass, points, drawer and record names in code.

## Framework services

New services land with Phobos Medical as first consumer:

| Set | Service |
| --- | --- |
| 1 | `ApplianceDefinitions` width-by-depth overload (square families unchanged); `Health.PatientPlacement`; `Health.PatientFacts` (brought forward from set 2: the admission thresholds need it) |
| 2 | `Health.CareGrants`, `Health.WoundGravity` (one hand-applied transpiler on `Wound.Run`; left off with a plain message if the pattern moves) |
| 3 | `Observations.Footprints` (rotation-aware touching, lifted from `LaserRules.Touching`) |
| 4 | `Health.WoundCare` (slot one unit, done in 0.89.0; applying a checked native condition waits for set 6); `CrewRole.Medical`; `CrewWorkOffer.ExcludedActor` |

## Release sets

| Set | Versions | Working slice |
| --- | --- | --- |
| 0 | records only | This record, the research record, the art handoff, the brand, the AGENTS.md direction |
| 1 | Framework 0.82.0, Medical 0.1.0 | Ward-3: sleep, rest awake, lay a casualty, honest power, drawer, status panel, `care` pack |
| 2 | Framework 0.84.0, Medical 0.2.0 | Injured-crew order; weightless care (done; Framework 0.83.0 went to another change) |
| 3 | Framework 0.86.0, Medical 0.3.0 (done) | Vigil-2 patient monitor: planned 2 x 2 cart, pairs by touching, readout and alerts only, never heals; revised from the one-tile Vigil-1 after the owner's size direction ([art handoff](medical-art-handoff.md)) |
| 4 | Framework 0.89.0, Medical 0.4.0 (done) | Treatments in the pack; Keep patient treated, the medic order stocking the drawer |
| 5 | Medical 0.5.0 | Attend-2 autodoc: no rack of its own, progress by measured energy |
| 6 | Medical 0.6.0 | Nanite course cartridge applying the game's `NanoFirstAid` |

Framework numbers are rechecked at each set. Each later set opens with its own design
step and owner go. The autodoc is the medic again as a machine, so it follows the
medic. Left out for now: liquid disinfection, pills, crew carrying casualties to bed
by themselves, a trained medic speciality.

## Saved structures

All new; no migration. Set 1 adds four bed definitions, the bed record, the resting
mark, the bed's in-use and family conditions, and our interaction names in saved crew
queues (these names stay registered for good, or a saved queue would name nothing).

## Risks

- The rectangular footprint through the appliance builder: placement, rotation,
  walking onto the bed.
- A rester's loop uses the game's Sleeping pose (there is no lying-awake animation).
- Re-registering a person released from the drag slot.
- `IsOccupied` lapses in about 10 game seconds; at high game speed the offer gate is
  what keeps a bed reserved.
- Removing the mod leaves saved beds and queued action names without definitions.

## Owner checks for set 1

Place and rotate the bed; sleep powered and unpowered; rest awake then fall asleep in
place; lay a dragged unconscious crew member and drag them off; cut and restore power
mid-care; a second crew member is refused; save and reload in each route; one-hour and
eight-hour skips.

## Set 2 as built (Medical 0.2.0, Framework 0.84.0)

Owner go, 4 October 2026 ("proceed with what's next").

- **Weightless care.** `Health.WoundGravity` (Framework) patches the game's private
  `Wound.Run` by hand: exactly one `ldc.r8 0.05` after its `DcGrav01` test becomes a call to
  `WoundGravity.Factor(patient, 0.05)`, which returns the larger of 0.05 and the patient's
  `StatPhobosWoundGravityFactor`, capped at 1. Any other count leaves the method unchanged
  and `Available` false; the bed's panel then says so. The bed sets the stat to the care
  pack's `levels.bed.weightlessHealing` while caring and clears it with the rest of its care
  (the hidden care mark and sweep cover a bed destroyed under its patient). Shipped value
  1: an agent default, open to owner revision. It restores bleed easing too, because the
  game uses the same rate for both.
- **Send injured crew here.** A sixth field on the bed record (`send`; five-field records
  from 0.1.x read with it off), a right-click toggle and a panel setting. Each bed step,
  if the bed is free, powered and in air, the worst-off injured crew member (Framework
  `CrewWork.Crew()`, `BedRules.Severity`) who is awake, not under the player's control
  (`IsAIManual`, the selected crew member, the player character), not fighting, not asleep
  and not already resting or queued to rest, gets Rest and recover appended to their queue
  (`QueueInteraction`, never `AIIssueOrder`, which cancels everything). A bed waits up to
  90 real seconds for the person it sent. One crew-log line names them.

## Set 3 as built (Medical 0.3.0, Framework 0.86.0)

Owner go, 4 October 2026, after the larger monitor art.

- **Vigil-2**, family `PhobosMedicalMonitor`, 2 x 2, APPS, 34 kg, no inventory (it stores
  nothing), power from the wall row behind it with a working draw while it watches.
- **Pairing by touching.** Framework `Observations.Footprints.Touching` (rotation-aware
  rectangles, the shared one-tile rule; Shipbreaker's `LaserRules.Touching` is the same rule
  and can move there when Shipbreaker next changes). The monitor watches its saved choice if
  that bed still touches it, otherwise the first touching Ward-3; nothing is paired on the
  bed's side, so moving either ends the watch.
- **Readings** come from Framework `PatientFacts` through the bed's own patient record. An
  hour's trend is kept in memory (a sample every ten game minutes) and restarts after a load.
- **Alerts** (`MonitorRules.Fire`): a figure reaching the care pack's `alerts` level fires
  once and re-arms below four fifths of it; a newly bleeding wound fires until nothing
  bleeds. One `PlayerNotices` caution per firing, switchable per monitor.
- **Saved record** `PhobosState.MedicalMonitor` version 1: watched bed (or none) and alerts.
- It never changes the patient.

## Set 4 as built (Medical 0.4.0, Framework 0.89.0)

Continued under the approved plan after set 3; the order and its defaults below are agent
choices open to owner revision.

- **Keep patient treated**, a standing crew order on the installed Ward-3 (Framework
  `CrewWork`, right-click toggle like the L2's bottle order, ship-wide source). Treatments
  use the Operate duty and the new `CrewRole.Medical` (on by default); restocking is
  Framework's own haul (Haul duty) into the bed's drawer.
- **Treatments are data.** The care pack's `treatments` section names a fixed `test`
  (`bleeding`, `fracture`, `spent-dressing`), the only `effect` built so far (`slot-item`),
  the item used up, `medicSeconds` (5 to 1800), an optional skill condition and an
  `order`. Code never branches on a treatment name. `apply-condition` stays reserved for
  set 6. Every file must name an item the game knows that goes on a suitable wound slot by
  the game's own slot effects (`TreatmentRules.NativeCheck`, run while the game loads the
  pack, so a bad player file is refused on its own). Shipped: dress-bleeding (clean scrap
  cloth, 45 s, trauma), splint-fracture (`ItmSplint01`, 90 s, trauma), change-dressing
  (clean cloth for a dirty one, 45 s, nursing). Seconds are authored balance.
- **The game's own effects.** Framework `Health.WoundCare.Apply` calls the game's
  `Slots.SlotItem(slot, item, bAuto: false)`, the same path as a paper-doll drop, so a clean
  cloth staunches (and its own wear ticker later turns it dirty), a splint splints, and an
  item the game uses up (water, spirits) is gone afterwards, which also counts as success.
  `RemoveToDeck` unslots a spent dressing (the game's unslot interaction runs) and sets it on
  the deck beside the bed through the game's deck drop.
- **Order of care** (`TreatmentRules.Due`, pure): treatments by order, wounds within one by
  vital part, bleed rate, depth, then name. A wound may appear under several treatments, so a
  later one is a fallback; the order takes the first step whose item is in the drawer or can
  be fetched. A treatment stays due while its test holds, so an item that does not change the
  wound (water on a bleeding wound) would repeat; the editing guide says so.
- **Integrity.** Every condition is checked before anything moves; one unit leaves the
  drawer only after that; a unit the game refuses after it left is set down on the deck,
  never destroyed. The finished step is matched against the same need (treatment and wound
  part) before it is carried out.
- **The patient never treats themselves.** Framework 0.89.0 adds `CrewWorkOffer.ExcludedActor`,
  checked in `CrewWork.Eligible`; the order sets it to the patient on every offer.
- **Saved structures.** None new beyond the standing order Framework already saves per
  equipment; the order is off on every bed until switched on. The toggle's interaction name
  `PhobosMedicalTreat` joins the names that stay registered for good.
- Not done: pills, disinfection, surgery, dressings applied outside a Ward-3, and the autodoc
  (set 5), which is this order as a machine.
