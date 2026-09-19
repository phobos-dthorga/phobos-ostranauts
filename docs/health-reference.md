# Ostranauts health, injury and drug reference

Research date: **2026-09-20**. Installed game: **1.0.1.4**.

This reference maps the game's health systems for Phobos mod design: what causes
trouble, what it changes, how it progresses, and where an intervention can help.
It is an analysis of installed definitions and selected engine code, **not an
in-game treatment trial**. It describes game mechanics, not real-world medicine.

## Contents and evidence

- [Acute conditions and survival systems](#acute-conditions-and-survival-systems)
- [Wounds and traumatic injuries](#wounds-and-traumatic-injuries)
- [Infectious and inflammatory diseases](#infectious-and-inflammatory-diseases)
- [Radiation illness](#radiation-illness)
- [Symptoms and incapacitation](#symptoms-and-incapacitation)
- [Fatal outcomes and intervention windows](#fatal-outcomes-and-intervention-windows)
- [Chronic ailments, all scar families, traits and emotional health](health-chronic-and-traits.md)
- [Medicines, dressings, clinical services and recreational drugs](health-treatments-and-drugs.md)
- [Sources, coverage, anomalies and verification priorities](health-evidence-and-gaps.md)

**D** means a definition or connected data path was inspected. **C** means selected
runtime code was also inspected. **I** means a gameplay/design inference, not a
verified outcome. Every section names its local source entries; full source paths
are indexed in the [evidence record](health-evidence-and-gaps.md#local-source-map).
There is no gameplay-tested evidence tier in this research round.

The catalogue covers the medical families found in the installed core data,
including obscure and apparently dormant paths. It does not imply that every
defined disease is naturally obtainable in the current game. Named stages are
grouped into their diseases rather than presented as hundreds of unrelated
ailments. Story-related effects and artificial-body injuries are explicitly
separated below.

## How to interpret the numbers

- **Higher is often worse.** `StatBlood` tracks red blood cells lost;
  `StatOxygen` tracks asphyxiation. Neither is a normal clinical measurement of
  blood volume or oxygen saturation. `StatFood` and `StatHydration` are deficits;
  `StatSatiety` is a separate fullness system. **D**
- **Thresholds are conditional.** The boundaries below are unmodified definition
  values, not guaranteed UI boundaries for every character. Traits, illness,
  medicines and threshold modifiers can change when a severity band is reached.
  Rules also contain entry/exit adjustments. Do not implement a diagnostic by
  copying these tables into a second severity engine. **D/C**
- **Hours are game hours.** Positive `fDuration` values describe an individual
  condition's timer. A zero duration becomes indefinite in `Condition`'s
  constructor; it does not mean an instant effect. Negative-duration helper
  conditions are a separate mechanism. **C**
- **A timer is not a prognosis.** Reapplication, parent effects, probabilistic
  transitions, changing thresholds and unloaded/fast-forward processing can alter
  the course. Timer expiry carries excess elapsed time into subsequent stages.
  A nominal window can pass in one accelerated update. **C**
- **Removing the cause differs from removing its effects.** Wounds live on body
  parts and contribute to patient-wide blood loss, infection and pain. A symptom
  can expire while its source keeps producing it. **D/C**

## Acute conditions and survival systems

Unless otherwise indicated, evidence is **D** from `conditions.json`,
`condrules.json`, the associated `CONDDc...` entries in `loot.json`, and tickers.
The prevention and response column is **I** where it describes player action;
specific native treatments have their own traced entries in the
[treatment reference](health-treatments-and-drugs.md).

| System and native family | Effects and progression | Prevention, interruption and limits |
| --- | --- | --- |
| **Hypoxia / asphyxiation** — `DcOxygen02/03/04`, `StatOxygen` | Hypoxic and critically hypoxic states impair movement, fatigue recovery and consciousness, and reduce tolerance of blood loss. The last state is fatal. Baseline boundaries: 0.00319, 0.01823, 0.054691. | Restore an actually breathable supply before fatality: appropriate pressure, oxygen supply, working suit equipment and atmosphere. Having oxygen in the room does not establish that a sealed suit is breathable. Painkillers and medical sleep do not provide oxygen. Exact recovery rate and rescue window were not measured. |
| **Low pressure / ebullism** — `DcGasPressure01/02`, `Ebullism` | The human pressure rule gives gasping below 16 and ebullism below 3.039 in `StatGasPressure`. Ebullism adds pain and emotional penalties. | Restore pressure or a working protective suit. Pressure and oxygen composition are separate checks; the inspected rule reads total gas pressure, not oxygen partial pressure. Do not conflate relief of ebullism with reversal of accumulated hypoxia. |
| **Carbon monoxide poisoning** — `DcCOPoisoning01..07`, `StatCOPoison` | Its own exposure statistic and severity chain; visible names incorrectly reuse “Hypercapnia.” Severe effects include headache, dizziness, sweating, gasping and impaired consciousness. Stage 07 has a 0.03 h fatal transition. | Stop CO exposure and supply clean breathable gas. Do not diagnose CO2 solely from the visible name. No specific antidote was established in this pass. |
| **Carbon dioxide poisoning / hypercapnia** — `DcCO2Poisoning01..07`, `StatCO2Poison` | Separate from oxygen deprivation and CO. Increasing exposure progresses through malaise, mild, moderate, advanced, severe, critical and lethal states; stage 02a is an additional mild band. Stage 07 has a 0.03 h fatal transition. | Restore ventilation/scrubbing and safe suit or room gas. Adding oxygen alone does not prove that CO2 has been removed. |
| **Sulfuric acid inhalation** — `DcH2SO4Poisoning01/02/03` | Stinging, then burning eyes/throat; coughing, chest pain and dyspnea; can start persistent chemical pneumonitis. Stage 03 transitions toward fatal pulmonary hemorrhage after 0.03 h. | Leave or isolate the contaminated atmosphere and use appropriate working protection. Clean air prevents further exposure but does not automatically erase the downstream lung condition. |
| **Ammonia inhalation** — `DcNH3Poisoning01/02/03` | Similar staged irritation and respiratory injury, with severe cough and chemical pneumonitis. Stage 03 also leads to pulmonary hemorrhage. | Remove exposure and investigate the persistent lung injury separately. A zero current gas reading does not disprove earlier exposure. |
| **Smoke inhalation** — `DcSmokePoisoning01/02/03` | Irritation, severe cough, chest pain, dyspnea and possible pneumonitis; lethal stage has the same pulmonary-hemorrhage transition. | Fire control, safe atmosphere and protective equipment address the source. Current-version retesting matters: older smoke-related saves received specific developer fixes. |
| **Cold / hypothermia** — `DcBodyTemp04/03/02/01` | Cold, freezing and hypothermia precede the fatal frozen state. Hypothermia reduces wound, blood and infection recovery and defense. | Restore suitable temperature and insulation; consider wetness and impaired thermoregulation. Treat actual body temperature, not just a room thermometer. |
| **Overheating / heat illness** — `DcBodyTemp06/07/08/09`, `DcBodyTempFatal` | Heat exhaustion increases water demand and impairs sleep/defense; heat stroke adds dizziness and further penalties. Organ failure starts a short fatal transition. | Reduce heat exposure and exertion; restore cooling and hydration before the fatal state. Do not assume a suit always cools its wearer or a medical bed compensates for a dangerous room. |
| **Blood loss** — `DcBlood01..04`, `StatBlood` | Baseline bands: below 15, 15–30 minor, 30–40 severe, 40+ hypovolemic shock. Severe loss impairs consciousness, carrying, recovery and thermoregulation. **Shock is already a fatal flag.** | Stop each external wound's bleeding; address internal bleeding sources; support native blood recovery. Bandaging prevents further wound contribution but does not instantly restore lost blood. Chronic micro-g hypovolemia is a separate condition. |
| **Systemic infection** — `DcInfection01..04`, `StatInfection` | Below 35 nominally healthy, 35–65 infection, 65–95 sepsis, 95+ septic shock. Recovery, defense, fatigue, food/water demand and thermal tolerance deteriorate. **Septic shock is a fatal flag.** | Clean and dress wounds, remove continuing infection sources, use traced immunostimulant/nanite and medical-rest paths. Lowering a symptom or treating one wound does not prove net systemic infection is falling. |
| **General poisoning** — `DcPoison01..05`, `StatPoison` | Bands at 4, 16, 28 and 40 add infection burden, abdominal pain, nausea/vomiting and ultimately fatal poisoning. Separate from each gas-specific poison statistic. | Stop the source. Native `PoisonHeal` provides a recovery path; no universal antidote was established. Anti-nausea medicine suppresses a symptom, not `StatPoison`. |
| **Malnutrition / starvation** — `DcFood01..05` | Well-fed, sustained, malnourished, wasting, starved. Deficit boundaries: 7, 72, 650, 1350. Malnutrition impairs several recovery and tolerance systems; starvation is fatal. | Maintain actual nutrition. A temporarily full stomach is not proof of adequate long-term nutrition; disease, traits and drugs change demand. |
| **Thirst / dehydration** — `DcHydration01..05` | Slaked, thirsty, parched, dehydrated, fatal dehydration; boundaries 4, 24, 36, 72. Headaches, impaired recovery, carrying and piloting precede death. | Maintain safe fluid intake, especially with heat, vomiting, diarrhea and blood loss. The installed Ship's Water mod changes this baseline. |
| **Hunger / overfull stomach** — `DcSatiety01..04` | Hungry below 4, sated at 4–8, stuffed at 8–12, sick stomach at 12+. Overfull state adds nausea. | Eat in suitable amounts; use anti-nausea treatment for the immediate symptom if appropriate. Do not confuse fullness with nutrition or induce vomiting as a presumed nutritional cure. |
| **Bowel need / fecal impaction** — `DcDefecate01..05` | Increasing need leads to holding, straining and impaction at 85+. Pain, sweating, fatigue, diarrhea, fullness and defense/social penalties can result. | Provide and use native toilet/defecation interactions. No separate surgical impaction treatment was established. This catalogue does not infer an independently simulated bladder from real physiology. |
| **Exertional fatigue** — `DcFatigue01..05` | Relaxed, winded, fatigued, drained, exhausted at boundaries 1, 2, 3, 4. Severe states greatly impair movement, defense, carrying and piloting and increase resource demand. | Stop exertion and allow recovery. Fatigue and sleep debt have separate statistics; stimulants may modify their thresholds without repaying either deficit. |
| **Sleep debt** — `DcSleep00..05` | Well-rested, rested, tired, drowsy, weary, blacking out at boundaries 8, 16, 24, 30, 36. Temporary cognitive/clumsiness effects and defense/piloting penalties appear. | Sleep in suitable conditions and investigate pain, atmosphere, stimulants and insomnia if sleep fails. Sedation is a timed influence on sleep, not a universal rescue treatment. |
| **Pain / pain shock** — `DcPain00..04` | No/slight/minor/severe pain/shock at 5, 25, 50, 75. Pain affects sleep, work, movement and defense; shock adds recurring blackouts. This shock is distinct from fatal blood-loss or septic shock. | Remove or heal the source and use analgesia for symptom control. A higher pain threshold may improve function without changing the injury. |
| **Atrophy** — `DcAtrophy01/02/03`, `StatAtrophy` | Minor at 10+, severe at 20+: carrying, fatigue, defense and piloting penalties, with altered food/water demand. | Native exercise and Ossifex are relevant investigation paths; Ossifex's immediate and timed effects are traced. Do not promise a particular exercise reversal rate without measuring it. |
| **High-G exposure** — `DcGrav03/04/05`, `G-LOC1/2`, `G-LOCCramps` | Baseline gravity bands begin at 4, 9 and 35. Warning timer is 0.0083 h (about 30 s), with 0.005 h blackout stages. Character modifiers change tolerance. | Reduce acceleration early and use appropriate seating/restraints. Gravusine raises a threshold temporarily; it does not make arbitrary acceleration safe. Very high-G injury generation needs its own runtime/gameplay test. |
| **Encumbrance** — `DcEncumbrance01..04` | Burdened, struggling and overloaded conditions interact with fatigue, carrying capacity and illness. | Reduce carried load and avoid treating capacity loss as a diagnosis by itself. Atrophy, blood loss, fractures and chronic issues can all contribute. |
| **Hygiene** — `DcHygiene01..04` | Well-groomed through filthy, with social and bodily-needs interactions. Individual disease/symptom effects can worsen hygiene. | Use native cleaning and clean dressings. General cleanliness, wound disinfection and systemic infection are different mechanisms. Ship's Water adds a recovery effect that should not be attributed to core. |

### Body temperature boundaries

`DcBodyTemp` reads **body** `StatSolidTemp` in kelvin. These are native boundaries,
not real medical diagnostic thresholds or recommended room settings. **D**

| State | Nominal body-temperature interval |
| --- | --- |
| Frozen to death | Below 293.2 K (20.05 °C) |
| Hypothermia | 293.2–301.0 K |
| Freezing | 301.0–305.4 K |
| Cold | 305.4–308.8 K |
| Comfortable | 308.8–310.9 K |
| Overheating | 310.9–312 K |
| Heat exhaustion | 312–314.3 K |
| Heat stroke | 314.3–315.7 K |
| Organ-failure countdown | 315.7 K (42.55 °C) and above |

## Wounds and traumatic injuries

Sources: `condowners_wounds.json`, `wounds.json`, `slot_effects_wounds.json`,
`loot_wounds.json`, wound slot interactions; runtime `Wound.Run`,
`Wound.ApplyEffectsParent`, `Wound.CalcPain`. **D/C**

Wounds are body-part objects, not simply patient status icons. Their main axes are
blunt damage, cut damage, bleeding, local infection and pain. Parent effects vary
by location and severity. The installed wound table has 20 definitions, including
artificial-body entries; that is not a count of 20 human diseases.

| Injury or wound state | Effects and outcome | Native intervention and limits |
| --- | --- | --- |
| Minor/severe bruising — `DcWoundBlunt02/03` and variants | Local blunt damage and pain; serious locations can add systemic complications. | Native wound healing and supportive recovery. Dressings do not directly erase blunt damage. |
| Broken injury / fracture — `DcWoundBlunt04`, `FracturedBone` | Unsplinted fractures make the blunt-healing factor vary between negative and positive values in `Wound.Run`; recovery can stall or worsen. | A correctly slotted splint supplies `IsSplinted`, bypassing that random penalty. This stabilizes the native process; it is not instant bone repair. |
| Minor/moderate/severe cuts — `DcWoundCut02/03/04` and variants | Local tissue damage; bleeding and infection contribute to systemic deterioration. | Clean the specific wound, dress external bleeding and support healing. Severe damage to a vital wound can kill directly. |
| Local wound bleeding — `DcWoundBlood02/03/04` | Unstaunched bleeding adds directly to the parent's `StatBlood` over elapsed time. | Bandages set `IsStaunched` on the wound. Recheck all wounds; one dressing does not cover every bleeding site. |
| Local infection — `DcWoundInfect02/03/04` | Infection inhibits healing and contributes to patient `StatInfection`, scaled by cut damage in the inspected code. | Cleaning consumes a disinfection amount during wound processing. Dirty dressings add infection rate. Removing systemic infection alone leaves a wound source intact. |
| Internal bleeding — `BleedingInternal` | Adds patient blood-loss rate and water demand. Sources include wound complications and hematopoietic radiation poisoning. | Address the source. Wound-effect removal entries exist as injuries change severity, and radiation burden has its own recovery path. A skin bandage does not suppress the patient-wide internal-bleeding rate. No direct portable surgical fix was established. |
| Fractured rib — `FracturedRib` | Adds pain; chest wound tiers can also contribute coughing blood, pneumonia, internal bleeding and life-threatening complications. | Heal/stabilize the relevant injury; symptom treatment alone does not repair the chest wound. Do not assume every rib fracture inevitably progresses to the worst chest tier. |
| Crippled arm — `CrippledArm` | Work-speed penalty, reduced carrying tolerance and threat. | Wound recovery and source removal. Not proof of a severed limb or a native regrowth/prosthesis installation mechanic. |
| Crippled leg — `CrippledLeg` | Movement penalty, reduced carrying tolerance and threat. | As above; distinguish injury-linked impairment from a chronic knee condition. |
| Concussion — `Concussion` | Nominal 100 h; cognitive impairment, insomnia, dizziness and probabilistic headache/nausea/vomiting. | Prevent further head trauma, support recovery and manage symptoms. Check whether the wound is still supplying it; 100 h is not a guaranteed total recovery time. |
| Strained back — `StrainedBack` | Nominal 48 h; reduced carrying tolerance. Also produced by herniated-disc episodes. | Reduce demands and allow recovery; recurring chronic source may require clinical removal. |
| Blisters — `Blisters` | Nominal 72 h; pain and movement penalty. | Remove the provoking exposure and support recovery; this is not an exclusive diagnostic sign of radiation. |
| Severe burns — `BurnsSevere` | Nominal 72 h; pain, infection burden, reduced healing/fatigue tolerance, worse sleep and hygiene. Expiry triggers the severe-burn scar trait. | Prevent further burns, support healing and infection control. Native scar-removal service exists afterward; claims that a burn scar is universally incurable are contradicted by the installed service data. |
| Aberrant wound healing — `AberrantWoundHealing` | Nominal 72 h; pain, infection burden, worse wound/infection recovery, blood-loss rate and lower tolerance. | Support recovery and prevent added injury. Definition does not establish a specific corrective device. |
| Cardiac arrest — `CardiacArrest1/2` | Stage 1 affects consciousness and temperature; after 0.1 h it has a 92% fatal-stage trigger. | Some chest-wound removal entries remove stage 1, but no usable CPR/defibrillation action was established. Do not turn the remaining 8% roll into a reliable survival claim. |
| Pulmonary hemorrhage — `PulmonaryHemorrhage` | **Already fatal**, despite sounding like an illness one might treat. Can be reached from chest trauma or toxic inhalation. | Intervene in the upstream injury/exposure. Removing this flag from a corpse is not demonstrated resurrection. |
| Severe brain trauma — `TraumaBrainSevere` | **Already fatal**. Head wound effects can produce it. | Prevent progression to the fatal stage. No restorative native treatment established. |
| Artificial-body damage — `ComponentFailure`, `CompromisedArmor`, `HackedRebooting` | Component failure is fatal; armor compromise and reboot are separate machine states. | Human medicines and blood-loss models cannot be assumed to apply. A robot maintenance system would need a separate target/support contract. |

Two especially consequential runtime findings:

1. When the patient has `DcGrav01`, `Wound.Run` multiplies the wound-healing rate
   by **0.05**. This is a 95% reduction in that code path, not a general statement
   that every medical recovery process is 95% slower in microgravity.
2. A vital wound whose blunt or cut damage reaches **1.0** can set the patient's
   kill state directly. A mod cannot safely assume every death travels through a
   removable named disease condition.

## Infectious and inflammatory diseases

Sources: named conditions, their `COND...Per` effects, `TUp...` transitions,
`DcImmunostimulant` and `CONDImmunostimulantCuresPer`. **D**

| Disease | Defined course and consequences | Prevention / abort / recovery |
| --- | --- | --- |
| **Cholera** — `Cholera1/2/3` | 48 h initial stage → 3 h symptomatic stage → 80% trigger for a further 3 h severe stage. Strong infection and dehydration pressure, diarrhea, nausea and vomiting. | Immunostimulant cure payload explicitly removes all three stages. Support water, nutrition and systemic recovery. The acquisition route is not proven: do not assume every dirty drink can transmit it just because the disease exists. |
| **Gastroenteritis** — `Gastroenteritis1/2` | 48 h incubation → 50% symptomatic transition unless immune; symptomatic stage lasts 72 h and adds infection/diarrhea. Weak Stomach also has a direct periodic symptomatic trigger. | Immunostimulant payload removes both stages. Iron Stomach supplies the symptomatic immunity flag. Maintain fluid intake; general food hygiene is sensible prevention, but a complete contaminated-food acquisition model was not established. |
| **Acute hepatitis** — `Hepatitis1..4` | 168 h initial stage, then 72 h, 168 h and 84 h stages with 95%, 70%, 70% transition chances and immunity guards. Later effects include pain, infection rate, fatigue, headache, diarrhea, nausea and vomiting. Final continuation has a 30% guarded trigger. | **Early intervention differs from late care:** immunostimulant instant-cure payload removes `Hepatitis1` only. It may still help systemic infection recovery later, but does not explicitly delete stages 2–4. Do not label it a guaranteed late-stage cure. Acquisition and recurrence need testing. |
| **Pneumonia** — `Pneumonia1/2/3` | Definitions specify 24 h, 24 h and 36 h stages, with cough, fever, pain and impaired infection recovery. Chest wounds reference the initial stage. | Immunostimulant payload explicitly removes the three numbered stages. **Anomaly:** both progression triggers target unnumbered `Pneumonia`, which was not found as a condition in the inspected condition folders. Treat the intended sequence as unverified, not a functioning 84 h disease course. |
| **Chemical pneumonitis** — `Pneumonitis2/3` | Moderate: 24 h with cough, dyspnea and defense loss. Severe: 24 h with severe cough, pain, fever and defense loss, then moderate for another 24 h. Toxic-inhalation helper `Pneumonitis2FAF` selects moderate or severe. | Stop inhalation exposure. The established disease can persist after the air is clean. It is **absent from the antibiotic instant-cure list**; supportive care can address consequences but is not proof of direct lung-condition removal. |
| **Tuberculosis and other chronic respiratory diagnoses** | Recurrent trait-driven conditions, rather than the acute disease chains above. | See [chronic care](health-chronic-and-traits.md#chronic-ailments). Antibiotic instant-cure data does not include chronic tuberculosis, asthma, emphysema, silicosis or COPD. |

These disease labels do not establish real-world transmission, microbiology,
contagion, immunity duration or medication pharmacology. For example, the game's
“antibiotic” path includes an early hepatitis-stage removal; that is a game rule,
not a medical classification to generalize from.

## Radiation illness

Sources: `DcRad`, `CONDDcRad01..06`, `HRP`, `ARS...`, `CRS...`, their effect and
transition entries, `RadHeal` and anti-radiation consumption. **D**

Radiation burden, blood-system injury, acute radiation syndrome and skin injury
are distinct data paths. No source here establishes that they should be merged
into a single “radiation poisoning” percentage or a guaranteed death clock.

| Family | Definition-backed effects | Intervention and limits |
| --- | --- | --- |
| Radiation burden — `StatRad`, `DcRad01..06` | Baseline bands: normal <21; elevated 21–1000; dangerous 1000–4250; life-threatening 4250–10000; lethal 10000–30000; catastrophic 30000+. Elevated applies an 80% HRP entry; higher bands apply HRP and the corresponding ARS onset. | Leave the radiation source, reduce further exposure and use traced anti-radiation treatment. These are native units: do not relabel them Gy/Sv without establishing the engine conversion. |
| Hematopoietic radiation poisoning — `HRP` | Reduced blood and infection recovery, reduced infection tolerance, added infection and internal bleeding. It is indefinite while retained. | Lowering radiation can change the rule that supplies HRP; the resulting removal and net recovery should be measured. Bandaging external wounds cannot address this entire chain. |
| Dangerous ARS — `ARSDangerous1/2/3` | 4 h onset → 668 h nausea phase → 668 h phase with weakness and fatigue-threshold change. | Remove exposure and treat radiation burden/secondary illness. Long timers are real definition values, not a promise that symptoms will persist for that exact total. |
| Life-threatening ARS — `ARSLifeThreat1/2/3` | 2 h onset → 12 h nausea/abdominal pain with reduced infection/fullness thresholds → 8 h headache phase with reduced infection threshold. | Same source control and supportive treatment. Already-started stages have their own timers; instant `StatRad` reduction alone does not prove all stages are canceled. |
| Lethal ARS — `ARSLethal1..7` | Durations: 0.15, 0.35, 0.5, 1, 34, 168, 168 h. Progresses through nausea, abdominal pain, diarrhea, headache, dizziness/cognitive effects and reduced blood/infection tolerance. | The word “lethal” is a severity label here, not a `bFatal` flag on every ARS stage. Survival depends on the resulting condition stack; no cure guarantee established. |
| Catastrophic ARS — `ARSCatastrophic1/2/3` | 0.15 h onset → 0.35 h symptomatic phase → 35 h phase with diarrhea, headache, cognitive effects and severely reduced blood/infection tolerance. | Prevent exposure; attempt prompt burden reduction and support before downstream fatality. No verified rescue protocol. |
| Cutaneous radiation syndrome — `CRS1..4` | 4 h onset → 30% transition to a 12 h painful phase → 168 h latent phase → 72 h illness phase supplying severe burns. | Support the burn/skin consequences and eventual scar. The current natural entry route into `CRS1` was not established; do not claim every irradiated patient runs this chain. |

`RadHeal` uses the current `StatRad` as a coefficient for a nominal hourly recovery
payload of -0.16. This is **not a flat 0.16 native units per hour**. ChymAdd's
consumption payload subtracts 500 native units immediately; its subsequent 24 h
condition has an empty `aPer` payload in the inspected data. Neither observation
proves regeneration of every radiation complication or protection from new doses.

## Symptoms and incapacitation

Sources: corresponding named conditions and their `COND...Per` effects. **D**
Timers below are individual condition defaults; an active source can renew them.

| Symptom/state | What it tells us | Response or interpretation |
| --- | --- | --- |
| `AllergyNasal`, `IrritatedEyes`, `Itching` | Irritation/allergy symptoms; overlap with chronic episodes, exposure and smoking. | Find the source. A symptom name alone does not establish infection. |
| `ChestPain`, `Dyspnea`, `Gasping` | Multiple respiratory, cardiac, exposure and chronic causes. Dyspnea has a 0.15 h timer; gasping is indefinite while retained. | Check atmosphere and injury/condition sources. Do not infer a unique diagnosis from chest pain. |
| `Coughing`, `CoughingSevere`, `CoughingBlood` | Respiratory symptoms shared by lung illness and trauma; coughing blood defaults to 24 h. | Treat the source; this is not automatic proof of pulmonary hemorrhage, which is a separate fatal flag. |
| `Headache`, `HeadacheMild`, `Dizzy` | Possible dehydration, concussion, gas exposure, radiation or drug effects; dizziness supplies temporary clumsiness. | Investigate context and simultaneous measurements before recommending analgesia alone. |
| `Nausea` | 0.5 h; reduces fullness tolerance and has a vomiting transition. | Anti-nausea path removes nausea and can reduce excess satiety. It does not remove every cause or explicitly clear active vomiting. |
| `Vomiting`, `Diarrhea` | Default 0.1 h and 4 h; raise fluid demand and worsen hygiene/defense, with additional bowel effects for diarrhea. | Restore fluids and treat disease/exposure. A quiet interval is not proof the upstream illness ended. |
| `PainAbdominal`, `PainAbdominalSevere`, `Jaundice1/2` | Separate symptom/stage definitions. Jaundice has onset and visible stages; names do not prove an operational liver-function simulator. | Assess their actual triggering condition. Direct cures/acquisition for jaundice were not established. |
| `Feverish`, `Shivering`, `Sweating`, `SweatingHeavy` | Fever raises body temperature and can supply both sweating and shivering. Environmental thermal states also produce symptoms. | Check actual body and environmental state, not a single “hot/cold” icon. |
| `Weakness` | Default 1 h; reduces carrying tolerance and attack ability. | Investigate disease/radiation source rather than treating it as ordinary exercise fatigue. |
| `Stunned`, `Jolted`, `Prone`, `Vulnerable` | Short-term combat/posture/disruption states. Stun is nominally about 5 s. | End the hazard and allow the relevant recovery/action; not interchangeable with sleep or death. |
| `BlackingOutRepeater`, `BlackingOutEffect`, `Unconscious` | Blackouts can be caused by pain, G-force, alcohol, hypoxia and other systems. Unconsciousness changes recovery and resource use but also defense and thermoregulation. | Rescue must target the cause. A generic “wake patient” action is not demonstrated treatment of the underlying emergency. |
| `Sleeping`, `SleepingComfortable`, `SleepingMedical`, `Recovering`, `Roused`, `Sitting`, `SittingSecure` | Rest/posture states with different owners and effects. | Ordinary rest, medical recuperation and safe seating should not be collapsed into one healing flag. |
| `Refreshed`, `Refreshed2/3/4`, `RefreshedBuildUp` variants | Native graded rest benefits and accumulation states. | Preserve these when extending recovery; avoid granting the final benefit simply because sleep was started. |
| `TeethBrushed`, barefoot/improper-footwear states | Hygiene/comfort and equipment can contribute to symptoms and needs. | Check equipment and basic care before assuming unexplained pain requires a new disease system. |

## Fatal outcomes and intervention windows

Sources: condition `bFatal`/`bKO`, `aNext`, `fDuration`, corresponding triggers,
and `Wound.Run`. **D/C**

| Situation | What the data actually establishes |
| --- | --- |
| Blood-loss shock, septic shock, asphyxiation, starvation, dehydration, general fatal poisoning, frozen-to-death state | The terminal condition itself has `bFatal=true`. It is not an available treatment period. |
| Heat organ-failure warning; lethal CO/CO2; lethal acid/ammonia/smoke inhalation | Nominal 0.03 h (108 s) warning-stage timer, then fatal trigger. Whether correcting the source removes the warning before expiry must be tested. This is not a guaranteed 108 s rescue allowance. |
| Cardiac arrest stage 1 | Nominal 0.1 h (6 min), followed by a 92% fatal-stage trigger. Natural rescue by healing the originating chest injury is not established as practical within that window. |
| `DeathSoon`, `DcBlood04Soon`, `DcPoison05Soon`, `PulmonaryHemorrhageSoon`, `TraumaBrainSevereSoon`, `DeathHandprintSoon` | Nominal 0.0001 h (0.36 s) transition helpers. Treat these as effectively terminal handoffs for normal play, not useful treatment windows. |
| Severe vital-wound damage | Runtime can kill directly when local cut or blunt damage reaches 1.0. |
| Alcohol fatal-label anomaly | `DrunkAlcoholPoisoning` is named as a death outcome but is **not** among the explicit `bFatal` definitions. Its trigger requires already-fatal `DcPoison05`; see evidence notes. |
| Removing a terminal condition | Does not demonstrate reversal of kill/death state, restoration of crew control or recovery of a corpse. Native resurrection was not established. |

## What this means for Phobos medical mods

These are **design inferences**, not implemented features.

1. **Build triage around sources and trends.** Distinguish continuing external
   bleeding, internal bleeding, poor blood recovery and radiation-related blood
   effects. A single “blood” bar cannot explain all four.
2. **Keep four treatment promises separate:** exposure prevention, progression
   interruption, symptom relief, and actual recovery. Analgesia and anti-nausea
   care are useful without being represented as tissue repair.
3. **Start with a useful native workflow.** Wound cleaning, dressing inspection,
   splinting support and a treatment-course monitor have concrete native targets.
   Rebuilding the entire disease model would duplicate extensive existing logic.
4. **Reserve new behavior for demonstrated gaps.** Reliable resuscitation,
   transfusion, oxygen therapy interfaces or surgical treatment would require
   actual gameplay design and engine validation. A familiar medical item name
   does not mean its behavior already exists.
5. **Measure capacity before promising rescue.** A treatment may improve recovery
   while the patient still deteriorates because exposure or bleeding exceeds it.
6. **Keep hidden state a deliberate design choice.** A scanner that reveals all
   latent diseases immediately can remove diagnosis and risk from play. Decide
   what it can detect, at what cost and with what uncertainty.

Implementation should retain native ownership and timing, put mutations in shared
gameplay services, and keep UI observational. See the existing
[medical-system vision](medical-system-vision.md),
[runtime findings](medical-runtime-findings.md) and
[bounded next experiments](medical-next-steps.md).
