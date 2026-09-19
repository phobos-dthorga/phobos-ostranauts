# Medical and portable-power research

Research updated 2026-09-20. Local game session reports **1.0.1.4**. The
[environment record](modding-notes.md#environment-recheck-2026-09-20) lists the
installed plugin versions and configured mods.

The tables below preserve the first pass through installed definitions and supplied
schemas. Follow-up [runtime findings](medical-runtime-findings.md) now resolve
some of their initial questions through selective local code inspection. In
particular, wound-to-patient bleeding, damage-reduced battery capacity, per-use
energy charging and several timer/interaction paths are now traced. Read that
follow-up for the current evidence and the specific remaining save/reload risks.

No gameplay experiment, player-save inspection or device implementation has been
performed. A definition establishes an available data path, not all runtime
semantics. Paths below are relative to
`Ostranauts_Data/StreamingAssets/data/` unless stated otherwise.

## Findings that affect the design

| Area | Evidence in the installed data | Design implication and unresolved question |
| --- | --- | --- |
| Wound ownership | `condowners/condowners_wounds.json` defines 20 wound objects, including robot wounds. Examples have `Wound,...` update commands, wound-specific rules and nested treatment-item slots. `wounds/wounds.json` supplies their effect mappings; `slots/slots_wounds.json` has 40 wound and treatment-item slot definitions. | A treatment may need a specific wound and its patient. Do not treat every health value as a field on the person. The engine's parent/slot traversal and aggregation still need inspection. |
| Wound-to-patient effects | `slot_effects/slot_effects_wounds.json` references parent slot interactions. `SLOTWoundHeadBlunt05` in `interactions/interactions_slots.json` applies `CONDDcWoundBluntHead05Them` to a human; that entry in `loot/loot_wounds.json` names concussion, stun, internal bleeding and severe brain trauma. | There are already links between local injury and broader consequences. Their exact timing and persistence are not yet verified. |
| Dressings and splints | `ItmScrapClothClean`, `ItmScrapClothDirty` and `ItmSplint01` have treatment-slot mappings. `CONDWoundBandaged` applies `IsStaunched`; the dirty variant also changes infection rate. Clean bandaging attaches `BandageCleanDegrade`. | A field kit or dressing station has existing treatment behaviour to investigate. Verify target ownership, cloth degradation, replacement and removal before adding another wound system. |
| Washing wounds | Slot interactions reference clean water, soapy water and other fluids, applying `StatDisinfectAmount` through loot definitions. | A cleaning device may extend a real native action. Static files do not establish the full efficacy calculation, consumable transaction or connection to ship plumbing. |
| Analgesics | `PainkillerMinor1/2` and `PainkillerPresc1/2` in `conditions/conditions.json` define initial and wearing-off stages, with duration values 4 and 1. Their loot effects change `ThreshStatPain`; consumption interactions activate the first stage. | Native pain relief already has stages and acts through pain thresholds in these definitions. Do not describe it as repairing tissue. Timer reset, a maximum count of 1 and cross-stage behaviour need controlled repeat-dose tests. |
| Other drugs | The inspected antibiotic consumption path applies `Immunostimulant`. The anti-atrophy injector applies `OssifexStim` and changes `StatAtrophy`. `Sedated` has its own duration and sleep-related effects. | Product names alone do not establish a real pharmacological model. Follow the complete effect chain before describing a medicine's mechanism or adding interactions. |
| Chronic conditions | `ChronicAsthma`, `ChronicHerniatedDisc`, tendon conditions and multiple scars have recurring symptom definitions. `TUpChronicAsthma2` is a probabilistic trigger. A smoking-related addiction description also exists. | Persistence and recurring symptoms are existing foundations. This pass does not prove a general medication tolerance/withdrawal system or establish how an acute injury becomes chronic. |
| Existing clinical services | `ItmKioskHealth01` offers `ENCOKLGMedServicesKiosk`. `interactions/interactions_encounters.json` contains paid ailment/scar removal paths. | Research existing service outcomes before making new shipboard equipment duplicate or invalidate them. Prices, requirements and actual results have not been tested. |
| Medical rest and time handling | `SleepingMedical` modifies recovery through `CONDSleepingMedicalPer`. Separately, `interactions_events_ffwd.json` contains medical-sleep interactions using `CONDTick1HourSleepMedicalPhysio`, which directly modifies several health statistics. `PainHeal` explicitly has `bTickWhileAway`. | There are multiple relevant data paths. Their existence does not prove when each executes, whether they overlap or whether ordinary, accelerated and away-time results agree. |
| Loaded-mod interaction | Ship's Water's `data/loot/loot_shipswater.json` overrides `CONDTickHydration` and `CONDDcHygiene01`; the latter adds an infection-recovery contribution absent from the inspected core entry. | The current play environment is not a vanilla physiological baseline. Separate core and modded observations and check runtime plugin adjustments too. |

The native data contains comments and compact table-like structures as well as
ordinary arrays of definitions. For example, `conditions_simple.json` stores
seven-field records inside `aValues`. Any later validation tooling must handle
the actual format rather than assuming all files share one schema.

## Portable devices and battery evidence

| Evidence | What it supports | What remains unproven |
| --- | --- | --- |
| `ItmOssifexPen01` is tagged handheld, has hand-slot presentation and an injection interaction. | Portable medical items already have data-driven presentation and use paths. | A medic selecting another patient, rather than self-use, needs an explicit actor/patient/tool investigation. |
| `ItmToolWelder01` has a battery-only internal container, `mapChargeProfiles` and a charged-item presentation alternative. | A separate battery item can be associated with a handheld tool through existing definitions. | How use frequency, charge depletion, empty-pack handling and interrupted work are enforced. |
| `ItmBatteryWelder01` defines current and maximum stored energy of `0.0576`, plus mass and damage state. `StatPower` and the compact `StatPowerMax` definition explicitly describe kWh. | This example's nominal stored energy is 57.6 Wh. Native units can anchor medical-device energy budgeting. | Actual usable energy and behaviour under damage; this value is a reference, not the chosen medical-pack capacity. |
| `ItmToolWelder01Normal` in `chargeprofiles/chargeprofiles.json` uses `StatPower` from a contained item and specifies both tool and charge-item damage amounts. | Native definitions already represent energy consumption and wear during use. | A profile's invocation frequency; whether wear changes capacity or only item condition. Do not equate damage with electrochemical ageing. |
| `ItmChargerBattWelder01` in `condowners/condowners_chargers.json` has an appropriate container filter, `IsRechargingContainer`, an electrical update command, a power ticker and `ChargerWelder01` power info. Other battery families have chargers too. | There is an existing installed charging path to investigate before inventing a separate one. | Transfer rate, grid draw versus delivered energy, losses, behaviour with multiple packs, loss of power and saved partial charge. |
| The interaction schema describes actor, target, third-party tests and tool-dependent effects. | The native system has more roles than a simple self-use verb. | It does not establish how a custom handheld selects its patient or reliably reserves and charges its tool. |

Follow-up code inspection establishes damage-reduced effective capacity; it does
not establish electrochemical ageing, thermal charging or medical-device battery
compatibility. Charging uses a runtime transfer path separate from the charger's
own operating draw. Prefer investigating native storage and charging before adding
new persistent state. See the [runtime findings](medical-runtime-findings.md).

## Research questions and current handoff

The first-pass questions below remain the broad checklist. Local code tracing has
now produced concrete entry points and test risks; the
[next-step brief](medical-next-steps.md) narrows the immediate work to gameplay
value, a native power-cycle measurement and one care scenario. Further reverse
engineering of the entire medical catalogue is not a prerequisite.

| Question | Concrete output of a useful round |
| --- | --- |
| How do we address the correct patient and wound? | Trace the relevant engine paths and record how an operator, a portable tool, a patient and a wound are selected. Compare self-use and treatment of another person. |
| How do native batteries behave over a complete use/charge cycle? | In a separate test save, measure starting/ending energy, usage duration, empty-pack behaviour, swapping and recharging; repeat relevant interruption, speed and save/reload cases. Establish which native pack/charger family can be reused. |
| What does the current medical interface already reveal? | Compare ordinary health information with accessible underlying state in a controlled test. Identify information a handheld or installed diagnostic could usefully add. |
| What happens across a native treatment course? | Observe one injury and one treatment, including medication expiry/repeat use where relevant. Keep wound change, symptom relief and recovery distinct. |
| Which engine hooks and persistence paths are safe to extend? | Identify exact local runtime APIs and state ownership, then test only the extension needed by the first selected capability. No general framework is implied. |
| How do existing mods and station care affect the design? | Compare the chosen workflow with Ship's Water and relevant existing medical mods/services; record actual conflicts or overlap. No additional mod was installed during this pass. |

Research is sufficient for a first experiment when we can name the patient state
being observed or changed, the tool/resource path, the expected result and the
unresolved behaviours the experiment will test. The entire medical catalogue
does not have to be reverse-engineered before that decision.

## External reference status

The [developer's older modding thread](https://steamcommunity.com/app/1022980/discussions/3/3185736852419933798/)
explicitly marks its old document deprecated and links a replacement. The linked
Google document was not accessible through the web tool in this pass; its contents
were not used as evidence. The [official Steam modding guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946)
was read during the earlier environment investigation. Packaging guidance does not
prove medical or power behaviour; the concrete findings above come from local
definitions and supplied schemas.
