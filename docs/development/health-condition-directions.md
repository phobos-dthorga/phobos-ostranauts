# Ostranauts health conditions and directions of change

Research and creative planning record, **6–7 October 2026**. Owner direction: map
negative, neutral and positive health states, what moves them in either direction,
and possible Phobos interventions; leave implementation details to Claude.

The strongest opportunities connect care to ordinary ship life: keeping air safe,
stopping bleeding, supplying food and water, protecting sleep, managing acceleration
and knowing when a station is needed. Useful treatments, compromises and fraudulent
products can coexist. Their promises should differ: preventing injury, easing a
symptom, supporting recovery and removing a cause are four different results.

## Scope and reading the tables

This catalogue covers the identified native human survival, injury, disease,
radiation, symptom, recovery, medicine, substance, chronic, scar and health-relevant
trait families, plus all eleven emotional needs. Stages and anatomical variants
are grouped under their owning condition. A wound object, its bleeding stage and
the patient's resulting blood loss remain separate entries where their responses
differ. The [boundary table](#story-artificial-bodies-and-other-boundaries) includes
robot and plot effects without treating them as ordinary human diseases.

**+ Positive**, **0 Neutral** and **− Negative** describe health or practical
wellbeing in this record, not a moral judgment and not the game's icon colour.
Some states have benefits and costs on different axes. Age, metabolism, personality
and circadian phase are not diseases. Microgravity has a positive native colour
but can disadvantage wound healing; a pleasant drug effect can impair piloting.

**↑** means movement toward health, comfort or safe function; **↓** means movement
toward harm or impaired function; **↔** means no established change to the target
condition. Relief may be **pain ↑ / injury ↔**. These arrows do not guarantee a
transition, rate, cure or survival. They identify the target and direction.

**D** is an inspected native definition/effect path. **I** is an inferred response
or causal interpretation. **C20** is engine evidence from the 20 September study,
not a fresh code trace. **P** is an agent proposal for the owner's consideration.
No arrow here is a new gameplay test. The last column is always **P**, including
where an existing Phobos mod is the suggested home. Detailed concepts, outcomes,
costs and priorities are in [the intervention opportunities](health-intervention-opportunities.md).

Identifiers such as `DcFood01..05` include every numbered stage in that range;
`DcWoundCut02..04` also includes the named a/b variants where present. Helpers,
cooldowns, numeric statistics and episode timers are not additional diagnoses.
The catalogue is broad within this stated scope; it does not assert that every
story flag, memory, social trait or engine condition is a medical condition.

## Evidence baseline

**Blue Bottle Games** owns the native game systems analysed here. Its
[official Ostranauts description](https://store.steampowered.com/app/1022980/Ostranauts/)
establishes the survival setting; specific mechanics come from installed
definitions, documented in our [source map and evidence record](health-evidence-and-gaps.md#local-source-map),
[health reference](../health-reference.md), [chronic and trait reference](../health-chronic-and-traits.md)
and [treatment reference](../health-treatments-and-drugs.md). Those records remain
dated 20 September; the comparison below extends their data evidence without
silently rewriting their historical engine findings.

The current startup log reports **1.0.1.5**. All eight files fingerprinted in the
earlier health evidence record still match: full core conditions, condition rules,
triggers, general effects, wound effects, general interactions, clinic encounters
and tickers. The current full-condition index still contains **2,417** unique
names across its folders. The compact table contains **1,422** simple-condition
names. These totals include substantial nonmedical content.

The assembly now hashes to
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`,
different from the September trace. Thus matching data supports the definition
claims; the old runtime trace requires rechecking before implementation relies
on it. The local read-only index also checked the additional sleep, compression,
greeting and generic recovery entries cited below. No save was inspected or
changed, and no gameplay test ran.

## Survival and ordinary physical states

Sources: **Blue Bottle Games native definitions**, through the
[survival reference](../health-reference.md#acute-conditions-and-survival-systems).
Native stages/effects are **D**. Environmental and activity responses are **I**
unless a named item or service is identified. Removing exposure and clearing
accumulated injury are different steps.

| Condition and included states | Health value | Existing response and direction | Problems that move it down | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Oxygen deficit, `DcOxygen02..04`: hypoxic, critically hypoxic, asphyxiated | −; terminal stage fatal | Restore a genuinely breathable room or suit supply: exposure ↑; accumulated deficit recovery needs observation | Failed suit supply, unsafe gas or insufficient pressure: oxygen status ↓ | Medical air-rescue kit and exposure history; no promise that pain relief supplies oxygen |
| Human pressure, `DcGasPressure01..03`; `Ebullism`: void, thin, normal atmosphere | − → 0; ebullism − | Working suit or repressurisation: safe pressure ↑ | Vacuum or very low pressure worsens exposure and pain: ↓ | Emergency shelter and pressure check, shared with hull repair and Shipbreaker |
| CO poisoning, `DcCOPoisoning01..07`, `02a`, `Fatal` | − → fatal | Leave CO and obtain clean breathable gas: exposure safety ↑; no specific native antidote established | Further CO exposure worsens poisoning: ↓ | Distinguish CO from CO2 on a medical air check; native labels incorrectly say hypercapnia |
| CO2 poisoning, `DcCO2Poisoning01..07`, `02a`, `Fatal` | − → fatal | Scrubbing, ventilation and safe suit gas: exposure ↑ | Exhausted scrubber, poor gas renewal: burden ↓; oxygen addition alone leaves CO2 unresolved | Serviceable scrubber cartridges and a sickbay air warning; industrial/Agriculture tie-in |
| Sulfuric-acid inhalation, `DcH2SO4Poisoning01..03` | − → near-terminal | Isolate contaminated air: exposure ↑; established pneumonitis may remain | Continuing exposure: throat/eye symptoms and lung injury ↓ | Containment and a post-exposure observation period; Manufacturing link |
| Ammonia inhalation, `DcNH3Poisoning01..03` | − → near-terminal | Remove exposure: exposure ↑; assess persistent lung injury separately | Leaks or contaminated suit/room air: condition ↓ | Leak isolation and recovery supplies; Agriculture and Manufacturing link |
| Smoke inhalation, `DcSmokePoisoning01..03` | − → near-terminal | Fire response and safe air: exposure ↑ | Fire smoke and return to contaminated spaces: condition ↓ | Post-fire air assessment and medical watch; reuse existing firefighting providers |
| Body cold, `DcBodyTemp01..04`: frozen, hypothermia, freezing, cold | −; frozen fatal | Suitable warmth/insulation: body temperature ↑ toward comfortable | Cold exposure and poor thermal regulation: temperature ↓ farther from safe band | Recovery warming equipment with finite power; symptoms never replace temperature measurement |
| Comfortable body temperature, `DcBodyTemp05` | + | Maintain safe thermal conditions: preserve + | Heat or cold exposure, illness and impaired regulation: move away from + | Sickbay temperature control and sensible resting clothes |
| Body heat, `DcBodyTemp06..09`, `DcBodyTempFatal`: overheating, exhaustion, stroke, organ failure | − → fatal | Reduce exposure/exertion; cooling and fluids: toward comfortable ↑ | Hot rooms, exertion and dehydration: condition ↓ | Sickbay cooling, heat-safe industry schedules and limited cooling packs |
| Blood loss, `DcBlood01..04`: none, minor, severe, hypovolemic shock | + → −; shock fatal | Staunch external sources; native recovery can reduce loss: blood status ↑ | Open/internal bleeding, poor recovery: blood status ↓ | Existing Ward-3 care plus a source-specific blood-loss report; transfusion is a separate proposed capability |
| Systemic infection, `DcInfection01..04`: healthy, infection, sepsis, septic shock | 0 → −; shock fatal | Clean wound sources; immunostimulant and supported recovery: infection status ↑ | Dirty wounds/dressings, illness and suppressed recovery: infection status ↓ | Existing wound-care order; disinfection and clearer cause tracking are extensions |
| General poison, `DcPoison01..05`: none, minor, moderate, severe, fatal | 0 → − | Stop source and allow native `PoisonHeal`: burden ↑ toward none | Further intake/exposure: burden ↓ | Targeted toxicology information; an anti-nausea item must not be sold as universal detox |
| Nutrition, `DcFood01..05`: well-fed, sustained, malnourished, wasting, starved | + → 0 → −; starved fatal | Actual nutritious intake: deficit ↑ toward well-fed | Insufficient intake or higher demand: nutrition ↓ | Agriculture meals and voyage provisions; no invented native vitamin-deficiency diagnosis |
| Hydration, `DcHydration01..05`: slaked, thirsty, parched, dehydrated, death | + → − | Safe drinking fluids: deficit ↑ toward slaked | Lack of water, vomiting, diarrhea, heat or increased demand: hydration ↓ | A recovery drink with real water/food value; optional Ship's Water integration |
| Fullness, `DcSatiety01..04`: hungry, sated, stuffed, sick stomach | − → + → 0 → − | Appropriate portions and time: toward sated ↑; native anti-nausea path can lower excess satiety | Too little food or overfilling: fullness comfort ↓ | Small recovery portions and clear galley serving sizes; fullness ≠ nutrition |
| Bowel need, `DcDefecate01..05`: normal, moving, holding, straining, impaction | + → 0 → − | Native toilet/defecation actions: need ↑ toward normal | No usable toilet or delayed action: discomfort ↓ | Accessible sanitation and recovery logistics; portable impaction surgery is unestablished |
| Exertional fatigue, `DcFatigue01..05`: relaxed, winded, fatigued, drained, exhausted | + → 0 → − | Stop exertion and recover: fatigue ↑ | Continuing hard work, illness or reduced tolerance: fatigue ↓ | Work/rest recommendations and light duties; stimulant tolerance is not recovered energy |
| Sleep debt, `DcSleep00..05`: well-rested, rested, tired, drowsy, weary, blacking out | + → − | Successful sleep: debt ↑ toward rested | Missed sleep, pain, discomfort, stimulants: sleep status ↓ | Quiet berth and protected rest period; Agriculture drinks can help or hinder the routine |
| Sleep comfort, `DcSleepComfort01/02`: cannot sleep, comfortable enough | − / 0 | Improve actual comfort and address pain/insomnia: sleep opportunity ↑ | Adverse traits, symptoms and uncomfortable surroundings: opportunity ↓ | Bedding/privacy improvements; power-consuming luxury and cheap ineffective versions can coexist |
| Circadian phases, `DcSleepCycleAwake`, `DcSleepCycleRest` | 0 | Native 16 h/8 h phase timers affect sleep comfort; phase itself is not an illness | A proposed schedule mismatch is a gameplay interpretation, not a newly proved native syndrome | Timed lighting and shift planning; benefit would need an explicit game design |
| Patient pain, `DcPain00..04`; `Pain02WorkRatePenalty`, `Pain03WorkRatePenalty`, `Pain04WorkRatePenalty` | + pain-free → − | Source recovery and analgesic threshold relief: pain/function ↑; injury may stay ↔ | Injury, chronic episodes, deficits and withdrawal: pain/function ↓ | Pain history and graded return to duty; monitor improvement must not certify healed tissue |
| Atrophy, `DcAtrophy01..03`: none, minor, severe | 0 → − | Native exercise is relevant; Ossifex lowers amount/rate: atrophy ↑ | Deconditioning/source rate: atrophy ↓; exact exercise course unmeasured | Rehabilitation station and exercise plan with food, water and time costs |
| Gravity, `DcGrav01..05`: microgravity, normal, G-LOC band, supergravic, hypergravic | 0; high-G − | Reduce acceleration; seating, compression trousers and Gravusine support tolerance ↑ | High acceleration: consciousness/function ↓; C20 microgravity wound-healing penalty | Auto Nav patient transport profile and recovery arrangements; no automatic gravity-as-cure claim |
| G symptoms, `G-LOC1/2`, `G-LOCCramps`; `TinglingExtremeties` | − | Remove provoking acceleration: risk ↑ toward safe function | Continued high-G exposure: blackout risk ↓ | Visible pre-burn patient warning and gentle flight choice |
| Encumbrance, `DcEncumbrance01..04`: unburdened, burdened, struggling, overloaded | + → 0 → − | Unload or delegate carrying: function ↑ | Excess carried mass, injury-lowered capacity: function ↓ | Hauling and recovery-duty choices; carrying assistance without pretending it heals |
| Hygiene, `DcHygiene01..04`: well-groomed, dishevelled, dirty, filthy | + → 0 → − | Native cleaning: hygiene ↑ | Neglect, symptoms and poor access to water: hygiene ↓ | Water-conscious wash supplies; general hygiene ≠ wound disinfection |
| Teeth brushed, `TeethBrushed` | + comfort/hygiene | Native brushing supplies temporary benefit: + | Benefit expires; untreated symptoms are not fixed by brushing | Practical hygiene kit and harmless luxury variants; no native dental-disease cure claim |
| Barefoot/improper footwear, `IsBarefoot`, `IsImproperFootwear` | 0 / − context-dependent | Suitable footwear: equipment-related comfort/function ↑ | Unsuitable kit can compound discomfort: ↓ | Comfortable recovery footwear and task clothing; no automatic blister cure |
| Compression trousers, `IsWearingCompressionPants` | + G tolerance, otherwise 0 | Native `CONDWearingCompressionPantsPer` adds 0.03125 to G-threshold field: tolerance ↑ | Removal loses that contribution; it does not repay blood loss or erase micro-g hypovolemia | Explain and provision existing equipment before adding another anti-G product |
| EVA equipment/training, `IsWearingPressureSuit`, `IsWearingEVASuit`, `IsWearingEVASuitWorkRatePenalty` | + protection / − untrained work speed | Correct working kit protects; native training addresses skill context | Failed supply/protection or untrained use: safety/function ↓ | Shipbreaker suit readiness and training support; preserve actual suit air checks |

## Wounds and traumatic complications

Sources: **Blue Bottle Games wound definitions/effects**, in the
[wound reference](../health-reference.md#wounds-and-traumatic-injuries) and
[wound care reference](../health-treatments-and-drugs.md#wound-care-and-supportive-care).
Staunching and splint effects are **D/C20**; practical recovery arrows remain
conditional on the real wound and its ongoing sources.

| Injury or state | Health value | Existing response and direction | Problems that move it down | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Blunt wounds, `DcWoundBlunt02/03` and a/b variants | − | Native wound healing and supportive care: local damage ↑ toward healed | Additional trauma; insufficient net healing: injury ↓ | Ward-3 recovery and wound trends; dressings do not remove bruising |
| Fracture, `DcWoundBlunt04`, `FracturedBone` | − | Correctly slotted splint supplies `IsSplinted`: stabilization ↑ | C20 unsplinted healing penalty; new trauma: recovery ↓ | Existing crew splinting; later supported rehabilitation |
| Cuts, `DcWoundCut02..04`, a/b variants where defined | − | Wound cleaning, bleeding control and native recovery: injury ↑ | New injury, infection and continued bleeding: injury ↓ | Existing care plus proposed cleaning workflow |
| Local bleeding, `DcWoundBlood02..04` | − | Dressing supplies `IsStaunched`: wound's blood contribution ↑ toward stopped | Unstaunched wound or missing dressing: patient's blood status ↓ | Existing automatic dressing; cheaper dirty-cloth emergency option has an infection trade-off |
| Generic bleeding indicator, `Bleeding` | − | Identify actual wound/source; this flag alone does not describe its loss rate | Native flag adds hydration demand; untreated source continues harm | Explain bleeding site and rate rather than use the icon as a complete diagnosis |
| Local wound infection, `DcWoundInfect02..04` | − | Native washing/disinfection and clean dressings: source control ↑ | Dirty dressing adds infection rate; local source worsens systemic burden | Finite wash/disinfection supplies, linked to water recovery |
| Local wound pain, `DcWoundPain02..04` | − | Local injury recovery: pain source ↑; analgesia can improve patient's tolerance | Continuing wound damage: pain/function ↓ | Distinguish wound pain from patient-wide pain; avoid duplicate treatment promises |
| Staunched wound, `IsStaunched` | + bleeding control | Correct native dressing stops the wound's unstaunched contribution; lost blood remains | Removing protection while wound still bleeds restores its harm | Preserve this native marker; inspect dressings rather than grant full blood recovery |
| Splinted wound, `IsSplinted` | + stabilization | Correct native splint bypasses the C20 unsplinted fracture penalty; bone damage remains | Removing support loses this contribution | Preserve native splinting and distinguish supported from healed |
| Internal bleeding, `BleedingInternal` | − | Heal/remove originating wound effect or reduce other source: source ↑ if that path resolves | Chest/abdominal injury or radiation blood-system damage: blood status ↓ | Proposed surgical stabilization or blood support, each a new capability; ordinary cloth is insufficient |
| Fractured rib, `FracturedRib` | − | Source injury healing: injury ↑ | Associated chest injury can add respiratory/internal complications | Recovery support and escalation information; not a guaranteed pneumonia sequence |
| Impaired arm, `CrippledArm` | − function | Source wound recovery/removal: function ↑ | Worsening injury: work/carrying ↓ | Light duties, assistive tooling and rehab; not proof of severing or regrowth |
| Impaired leg, `CrippledLeg` | − function | Source wound recovery/removal: movement ↑ | Worsening injury: movement/carrying ↓ | Mobility support and rehab; distinct from chronic knee disorders |
| Concussion, `Concussion` | − | Avoid further head trauma; support recovery and symptoms: ↑ conditional on source | Persistent wound, disturbed sleep or further trauma: ↓ | Observation and temporary duty advice; nominal timer is not a clearance certificate |
| Strained back, `StrainedBack` | − | Reduce carrying demands and allow episode recovery: function ↑ | Heavy demands or recurrent herniated-disc source: function ↓ | Load assistance and posture support; a brace need not remove a chronic trait |
| Blisters, `Blisters` | − | Remove provoking equipment/exposure and support native recovery: ↑ | Repeated irritation/exposure: comfort ↓ | Footwear and local comfort care; authored benefit, not an assumed native dressing effect |
| Severe burns, `BurnsSevere` | − | Support wound/infection recovery: acute state ↑; native expiry can leave a burn scar | Added burns, infection and impaired healing: ↓ | Burn-care supplies and later scar support; no instant tissue replacement |
| Aberrant wound healing, `AberrantWoundHealing` | − | Recovery support and prevention of extra injury: ↑ only as native course allows | Native penalties compound blood/infection/pain burdens: ↓ | Specialist follow-up and a proposed corrective treatment; no existing direct device cure established |
| Cardiac arrest, `CardiacArrest1/2` | −; stage 2 fatal | Some chest-effect removal paths remove stage 1; useful practical rescue not established | Stage 1's short timer has a fatal transition: ↓ | CPR/defibrillation is a substantial proposed pre-death capability; cause/response must be defined first |
| Pulmonary haemorrhage, `PulmonaryHemorrhage` | Fatal | Prevent upstream injury/exposure; no post-fatal rescue demonstrated | Severe chest injury or toxic inhalation reaches fatality | Earlier warning and stabilization research; no corpse treatment claim |
| Severe brain trauma, `TraumaBrainSevere` | Fatal | Prevent upstream trauma; no post-fatal rescue demonstrated | Severe vital head-wound damage | Head-injury prevention and transport; experimental recovery fiction would be a separate design |

## Named diseases and radiation effects

Sources: **Blue Bottle Games disease and radiation definitions**, in
[infectious disease](../health-reference.md#infectious-and-inflammatory-diseases),
[radiation](../health-reference.md#radiation-illness) and
[medicine](../health-treatments-and-drugs.md#medicines-and-clinical-treatments) records.
Definitions do not establish that every disease has a reachable natural acquisition
route. Do not invent contamination or contagion mechanics to fill that gap.

| Disease or family | Health value | Existing response and direction | Problems or unresolved limits | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Cholera, `Cholera1..3` | −, including quiet onset | Immunostimulant explicitly removes stages 1–3: disease ↑; replace lost fluids and support recovery | Ongoing symptoms create hydration/infection burden ↓; acquisition untraced | Recovery fluids and disease-stage information; no claim that all dirty water transmits it |
| Gastroenteritis, `Gastroenteritis1/2`; `WeakStomachIllness` | − | Immunostimulant removes both stages; specific immunity blocks symptomatic stage: ↑ | Weak-stomach timer can recur; foodborne acquisition not generally established | Gentle portions and rehydration; curing episode leaves weak-stomach trait ↔ |
| Hepatitis, `Hepatitis1..4` | − | Immunostimulant explicitly removes stage 1 only; later systemic recovery support: ↑ on that axis | Later stages persist unless their own course resolves; apparent remission can be a stage change | Early-versus-late care explanation and station referral; new late-stage cure must be an explicit proposal |
| Pneumonia, `Pneumonia1..3` | − | Explicit immunostimulant removal list: disease ↑ | Progression targets an unnumbered missing condition; full natural course unverified | Identify actual present stage; avoid promising a functioning sequence from intended timers |
| Chemical pneumonitis, `Pneumonitis2/3`, `Pneumonitis2FAF` | − | End toxic inhalation: exposure ↑; supportive care addresses consequences | Not in instant antibiotic cure list; clean air can coexist with continuing illness ↓ | Post-exposure care and a later targeted treatment study, distinct from infection care |
| Radiation burden, `DcRad01..06` | 0 → − | Leave source, native recovery and AntiRad consumption: burden ↑ toward normal | New exposure worsens burden and triggers secondary systems ↓ | Exposure diary, shielding/reach planning and anti-radiation stock management |
| Blood-system radiation injury, `HRP` | − | Source burden reduction and native recovery: potentially ↑; actual disappearance needs checking | Internal bleeding and poor blood/infection recovery: ↓ | Monitor blood trend separately from radiation amount; proposed blood support |
| Dangerous ARS, `ARSDangerous1..3` | − | Burden reduction and supportive care: ↑ on measured axes | Long nausea/weakness stages can survive exposure cessation | Long-haul convalescence supplies and duty planning |
| Life-threatening ARS, `ARSLifeThreat1..3` | − | As above; address actual secondary deficits: ↑ | Infection/fullness tolerance and symptoms worsen function ↓ | Intensive supportive care with observable results and escape-to-clinic choice |
| Lethal ARS, `ARSLethal1..7` | −, not every stage already fatal | Prompt source control and burden/secondary care: ↑ only where state responds | Blood/infection vulnerability can reach terminal outcomes ↓ | Serious treatment research; avoid turning the label into a guaranteed death or cure |
| Catastrophic ARS, `ARSCatastrophic1..3` | − | Same distinct targets, with no proved rescue guarantee | Severe downstream vulnerability and short onset: ↓ | Prevention, triage and costly evacuation rather than a universal injector |
| Skin radiation syndrome, `CRS1..4` | −; latent stage not healthy | Burn and skin-consequence support: ↑ conditional on native course | Burn stage and later scar; natural initial acquisition unresolved | Burn supplies and latent-course follow-up; lowering burden ≠ erasing skin injury |

## Symptoms and loss of function

Sources: **Blue Bottle Games symptom definitions**, through the
[symptom reference](../health-reference.md#symptoms-and-incapacitation).
These states frequently have several causes. Proposed comfort care changes a
symptom axis; it must not automatically erase the underlying disease.

| Symptom or state | Health value | Existing response and direction | Problems that move it down | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Nasal allergy, `AllergyNasal` | − | Remove source where identified; episode expiry: possible ↑ | Recurrent allergy or irritant source: symptoms ↓ | Targeted symptom care or genuinely harmless comfort item |
| Irritated eyes, `IrritatedEyes` | − | Remove smoke/irritant source; native recovery: possible ↑ | Continued exposure or recurrent sensitivity: ↓ | Eye-rinse concept; water and treatment effect need their own design |
| Itching, `Itching` | − | Follow scar, allergy, withdrawal or drug-aftereffect source: ↑ conditional | Repeated source or continued course: ↓ | Scar comfort care; no automatic nicotine-dependence cure |
| Chest pain, `ChestPain` | − | Assess chest injury, respiratory illness and other source; analgesic comfort ↑ | Continuing injury/exposure: health ↓ despite relief | Triage guidance and escalation; symptom suppression can create false confidence |
| Breathlessness/gasping, `Dyspnea`, `Gasping` | − | Atmosphere/suit checks and cause-specific care: ↑ | Unsafe gas, pressure or lung disease: ↓ | Air-rescue kit; not every case is fixed by a full oxygen bottle |
| Cough, `Coughing`, `CoughingSevere` | − | Remove exposure and address disease/trait: ↑ conditional | Irritation, chronic episode or lung injury: ↓ | Respiratory observation and proposed symptom relief |
| Coughing blood, `CoughingBlood` | − | Assess actual lung/chest/radiation source: ↑ if addressed | Continuing source: ↓; not identical to fatal haemorrhage flag | Clear escalation warning, avoiding a false single diagnosis |
| Headache, `Headache`, `HeadacheMild` | − | Hydration/exposure/source care and native analgesia: comfort ↑ | Deficits, trauma, disease or drug aftermath: ↓ | Recovery drink and quiet rest; no diagnosis from one icon |
| Dizziness, `Dizzy` | − | End source where possible and allow native course: function ↑ | G-drug aftermath, exposure, injury or illness: ↓ | Temporary safe-duty advice and motion-comfort concept |
| Nausea, `Nausea` | − | Native anti-nausea removal: symptom ↑; cause often ↔ | Radiation, poison, illness, overfull stomach or drug effects: ↓ | Symptom-targeted recovery food/drink; no universal detox promise |
| Vomiting, `Vomiting` | − | Fluids and source care: deficits ↑; direct active-vomiting removal unestablished | Lost fluids and continuing source: ↓ | Measured supportive fluid intake; aspiration/IV systems would be new designs |
| Diarrhea, `Diarrhea` | − | Fluids, sanitation and disease/source care: ↑ on those axes | Water demand, hygiene burden and continued source: ↓ | Recovery fluids and toilet access; no assumed antidiarrheal mechanism |
| Abdominal pain, `PainAbdominal`, `PainAbdominalSevere` | − | Address identified disease/poison source; analgesic comfort ↑ | Continuing illness/poison: ↓ | Patient symptom history and referral; a soothing tea could be neutral or merely comforting |
| Jaundice, `Jaundice1/2` | − | Cause and direct cure not established | Name alone is insufficient for a working liver model | Diagnostic/story opportunity; avoid a fabricated universal liver-cleanse effect |
| Fever, `Feverish` | − | Identify illness and support thermal/hydration balance: ↑ | Native temperature effects and persistent illness: ↓ | Recovery-room management; lowering room heat need not remove the fever condition |
| Shivering, `Shivering` | − symptom | Check body temperature and fever/source: ↑ if addressed | Cold or disease source: ↓ | Warming/comfort choice tied to measurements |
| Sweating, `Sweating`, `SweatingHeavy` | − symptom / 0 physiological response | Check heat, illness and fluids: ↑ toward safe balance | Heat, fever and other continuing causes: ↓ | Cooling and fluid availability; never remove icon as a substitute for cause care |
| Weakness, `Weakness` | − | Source recovery and lighter demands: function ↑ | Disease/radiation and further exertion: ↓ | Light duties and rehabilitation when source permits |
| Stun/jolt, `Stunned`, `Jolted` | − | End impact/hazard; brief native course: function ↑ | Repeated hazard: ↓ | Protective equipment and safe transport; sedation does not reverse stun |
| Prone/vulnerable, `Prone`, `Vulnerable` | 0 posture / − danger | Safe repositioning or native recovery actions: safety ↑ | Hostile/exposed location: ↓ | Rescue handling and mobility assistance |
| Blackouts, `BlackingOutRepeater`, `BlackingOutEffect` | − | Treat actual pain, sleep, gas, G or substance cause: ↑ | Cause continues: repeated loss of function ↓ | Cause-aware casualty support; no generic wake-up cure |
| Unconscious, `Unconscious` | − control, altered recovery | Remove cause and protect patient; native return of consciousness when supported: ↑ | Continuing hypoxia, poison or injury: ↓ | Ward-3 accepts an unconscious casualty; observation does not itself revive |
| Brief recovery immobilization, `Recovering` | − momentary function | Native roughly 3.24 s timer resolves: movement ↑ if no renewed source | New action/hazard can recur | Explain the state correctly; it is not the medical-rest healing flag |

## Chronic ailments

Sources: **Blue Bottle Games chronic traits, recurrence and clinic removal paths**,
in [all fifteen ailment families](../health-chronic-and-traits.md#chronic-ailments).
Every row has a linked native clinic removal definition (**D**); successful
payment, residual timers and restored function remain untested. Clean air,
analgesia and rest support consequences (**I**); they are not trait deletion.

For each suffix below, the trait is `TraitChronic<suffix>` and episodes/cycle use
`Chronic<suffix>` with numbered stages. **− → 0** is definitive removal of the
recurrent source if the native service executes successfully. **Pain ↑ / trait ↔**
is symptom management.

| Ailment and native suffix | Health value | Existing useful direction | Continuing/downward pressure | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Asthma, `Asthma` | − | Clinic − → 0; safe air avoids added harm | Recurrent breathlessness/cough; severe episodes | Respiratory support and exposure control, not an assumed inhaler already in core |
| Emphysema, `Emphysema`; extra `TraitChronicEmphysemaFAF` helper | − | Clinic − → 0; supportive care | Recurrent respiratory impairment; historical extra-variant anomaly | Chronic care history and correct duplicate/helper handling |
| Silicosis, `Silicosis` | − | Clinic − → 0; safe air support | Recurrent respiratory symptoms; cumulative mining dust model unproved | Shipbreaker exposure prevention and station referral |
| COPD, native spelling `CPOD` | − | Clinic − → 0; respiratory support | Recurrent episodes reduce tolerance | Native diagnosis spelling preserved; no universal antibiotic cure |
| Tuberculosis, `Tuberculosis` | − | Clinic − → 0 | Respiratory/fever/pain episodes; absent from instant antibiotic cure list | Specialist station care; quarantine only if a new contagion design is chosen |
| Allergic sensitivity, `AllergicSensitivity` | − | Clinic − → 0; symptom relief/source avoidance | Eye/nasal episodes and worse cough/pain | Targeted allergy comfort; harmless charm could leave trait ↔ |
| Herniated disc, `HerniatedDisc` | − | Clinic − → 0; analgesic comfort ↑ | Recurrent back strain and impaired work/movement | Supports, lighter carrying and rehabilitation |
| Mangled hand, `MangledHand` | − | Clinic − → 0; comfort ↑ | Work impairment and phantom-pain episodes | Assistive grips and occupational rehab; no assumption of a missing limb |
| Torn ACL, `TornACL` | − | Clinic − → 0; pain/function support | Recurrent movement penalties | Knee support and gradual duties |
| Torn MCL, `TornMCL` | − | Clinic − → 0; pain/function support | Recurrent movement penalties | Same support family; preserve diagnosis differences |
| Torn meniscus, `TornMeniscus` | − | Clinic − → 0; pain/function support | Recurrent movement penalties | Same support family and station care |
| Patellar tendonitis, native `PatellerTendonitis` | − | Clinic − → 0; pain/function support | Recurrent movement penalties | Rehabilitation and load management |
| Carpal tunnel syndrome, `CarpalTunnelSyndrome` | − | Clinic − → 0; pain/function support | Recurrent work impairment | Hand support and ergonomic tooling; trait stays without a definitive path |
| Tendonitis, `Tendonitis` | − | Clinic − → 0; pain/function support | Recurrent work impairment | Rest/rehab support and Manufacturing tools |
| Arthritis, `Arthritis` | − | Clinic − → 0; analgesic comfort ↑ | Pain, work and social-threshold episodes | Maintenance care and comfort; no unexplained permanent reversal from exercise |

## Every scar family

Sources: **Blue Bottle Games scar traits and clinic entries**, in the
[27-family scar reference](../health-chronic-and-traits.md#all-scar-families).
Each scar has the trait `TraitChronicScar<suffix>`, cycle `ChronicScar<suffix>`
and episode `ChronicScar<suffix>2`. Each supplies the inspected shared recurring
pain/itch/social-threshold episode: health **−** during symptoms, historical
identity otherwise **0**, sometimes with a threat modifier.

For every row, analgesia is **pain ↑ / scar ↔**. A proposed local comfort patch,
rehabilitation or cosmetic cover can help comfort/presentation without deleting
history. A decorative scar charm is **scar ↔**, while a corrosive home-removal kit
would be a deliberately harmful **new design**, **scar/skin ↓**. The common design
possibilities apply to every row; individual native clinic access differs below.

| Scar source | Native suffix | Native definitive direction | Distinct design opportunity |
| --- | --- | --- | --- |
| Knife injury | `Knife` | Linked clinic removal: − → 0 if successful | Symptom care; threatening appearance need not be treated as moral harm |
| Gunshot injury | `Gunshot` | Linked clinic removal | Trauma aftercare and optional cosmetic choice |
| Crossbow injury | `Crossbow` | Linked clinic removal | Trauma aftercare |
| Snooker injury | `Snooker` | Linked clinic removal | Everyday accident care |
| Brawling/fight club | `FightClub` | Linked clinic removal | Sporting/fighting aftercare without mandatory removal |
| Dart injury | `Dart` | Linked clinic removal | Local scar comfort |
| Appendectomy | `Appendectomy` | Linked clinic removal | Aftercare; scar name does not establish playable appendectomy |
| Heart surgery | `HeartSurgery` | Linked clinic removal | Surgical aftercare fiction; no proof of an existing heart-operation workflow |
| Brain surgery | `BrainSurgery` | Linked clinic removal | Aftercare and memory-story possibilities, kept distinct |
| Cybernetic surgery | `CyberneticsSurgery` | Linked clinic removal | Implant aftercare concept; installation mechanic unestablished |
| Skin graft | `SkinGraftSurgery` | Linked clinic removal | Burn/graft aftercare; graft operation is a proposal |
| Handcuffs | `Cuffs` | Linked clinic removal | Recovery from restraint and optional cosmetic care |
| Severe burn | `Burns` | Linked clinic removal; acute burn-to-scar link traced | A connected acute-care then scar-care journey |
| Hunt cat | `HuntCat` | Linked clinic removal | Occupational/animal injury history |
| Hard transfer | `HardTransfer` | Linked clinic removal | Auto Nav gentle-transfer story tie-in |
| Sports accident | `Sports` | Linked clinic removal | Rehab and exercise safety |
| Breakout injury | `Breakout` | Linked clinic removal | History-aware care without erasing the narrative |
| Racing accident | `Racing` | Linked clinic removal | Flight/vehicle safety and rehab |
| Cargo accident | `Cargo` | Linked clinic removal | Safer hauling and lifting support |
| Airlock accident | `Airlock` | Linked clinic removal | Pressure and handling safety |
| Mining accident | `Mining` | Linked clinic removal | Shipbreaker safety and recovery |
| Industrial accident | `Industrial` | Linked clinic removal | Manufacturing safety and recovery |
| Cooking accident | `Cooking` | Linked clinic removal | Agriculture galley safety |
| LEO brutality | `Activism` | Linked clinic removal | Care and personal history; not a personality defect |
| Botched enhancement | `BotchedCosmetic` | Linked clinic removal | A natural consequence for a proposed risky cosmetic clinic |
| Thief brand | `BrandThief` | Native clinic refuses; no linked removal | Comfort ↑ / brand ↔; illicit removal would deliberately change the legal/story boundary |
| Piracy brand | `BrandPiracy` | Native clinic refuses; no linked removal | Same distinction; cosmetic cover must not silently clear legal identity |

## Persistent physiology and health relevant traits

Sources: **Blue Bottle Games physiology and trait effects**, through the
[trait reference](../health-chronic-and-traits.md#persistent-physiology-and-health-relevant-traits).
Trait effects are **D**. Do not turn supportive equipment into permanent trait
removal unless a new treatment is explicitly designed.

| Trait or state | Health value | Existing response and direction | Downward pressure or limit | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Micro-g hypovolemia, `IsHypovolemic2` | − | AntiHypo-V course has explicit removal path: − → 0 | Impaired recovery/tolerance until resolved; not ordinary blood-loss shock | Course tracking and long-haul support; no cheap instant transfusion claim |
| Immunosuppression, `IsImmunoSuppressed` | − infection recovery | Immunostimulation can support recovery ↑; trait ↔ | Infection sources can exceed reduced recovery | Clean-care logistics and clear vulnerability information |
| Fast/slow healing, `IsHealerFast`, `IsHealerSlow` | + / − recovery | Work with actual recovery rates; support improves course, trait ↔ | Slow healing and extra injury prolong burden | Individual prognosis and supplies, not a standard recovery promise |
| Fast/slow metabolism, `IsMetabFast`, `IsMetabSlow` | 0, different demand | Provision food/water appropriately: deficits ↑; trait ↔ | Mismatched supplies cause avoidable deficits ↓ | Agriculture ration planner |
| Iron stomach, `IsIronStomach`; `IsImmuneGastroenteritis2` | + specific protection | Preserve the specific immunity | No general protection against all poisons or disease | Identify exact protection; avoid universal immunity marketing |
| Weak stomach, `IsWeakStomach`, `WeakStomachIllness` | − recurrence risk | Treat episodes and support fluids: symptoms ↑; trait ↔ | Native recurring illness chance remains | Gentle food choice and episode monitoring; bland food is not a proved timer cure |
| Tough/fragile, `IsTough`, `IsFragile` | + / − injury tolerance | Safer work and care support; trait ↔ | Lower tolerance magnifies consequences | Protective kit with honest limits |
| Strong/feeble, `IsStrong`, `IsFeeble` | + / − task capacity | Reduce load/use assistance: function ↑; trait ↔ | Exceeding capacity: fatigue/function ↓ | Manufacturing assistive tooling; training conversion remains unverified |
| Fit/unfit, `IsFit`, `IsUnfit` | + / − tolerance | Exercise/training is a native path to investigate; exact trait conversion unverified | Poor conditioning and demands exceeding tolerance | Measured rehab without an invented training timetable |
| Insomnia, `IsInsomniac`, `IsInsomniacTemp` | − sleep opportunity | Comfort/source care; temporary version can expire; trait ↔ under ordinary support | Pain, concussion, drugs or permanent trait continue difficulty | Quiet berth and protected rest; sedation carries a separate function cost |
| Heavy/light sleeper, `IsSleeperHeavy`, `IsSleeperLight` | 0, context-dependent | Sleep comfort modifiers differ | Comfort benefit is not proof of deeper healing or safe alarm response | Personal bedding preferences and observable sleep support |
| Impaired vision/blindness, `IsVisualImpaired`, `IsBlinded` | − practical function | Correct cause if available; general native eye cure unestablished | Injury-linked cause or permanent impairment | Assistive displays/correction concept; distinguish assistance from eye repair |
| Poor/sharp hearing, `IsPoorHearing`, `IsSharpHearing`, `IsSharpHearingFAF` | − / + practical function | Preserve/read actual trait; aid/removal path unestablished | Missed auditory information; ordinary noise is not proved to create this trait | Hearing assistance and visible warnings; optional accessibility value |
| Pain aversion/masochism, `IsAgliophobic`, `IsMasochist` | 0 personality; different pain response | Source treatment and comfort; personality ↔ | Low tolerance compounds function loss; high tolerance can conceal seriousness | Patient-centred pain interpretation, not personality erasure |
| Tidiness/slovenliness, `IsTidy`, `IsSlovenly` | 0 disposition; care consequences vary | Native cleaning and appropriate supplies: hygiene ↑ | Poor care routine can leave needs unresolved | Hygiene support and preferences |
| Gluttony/temperance, `IsGlutton`, `IsTemperate` | 0 disposition; appetite consequences vary | Suitable portions/provisioning: needs ↑ | Inappropriate portions or supply assumptions | Agriculture portions without treating disposition as disease |
| Aging, `DcAging01..05`; `RandomAgeChronic` | 0 life stage; health risks vary | Treat actual deficits/ailments; no native aging reversal established | Age framework can supply chronic opportunities; exact frequencies untraced | Long-term maintenance care and fraudulent youth remedies |
| Micro-g upbringing, `IsLifeMicroG` | 0 background | Account for its age-threshold modifier; background ↔ | Not proof of active hypovolemia | Respect history; diagnose current state separately |
| Non-circadian background, `IsLifeLackCircadian` | 0 background | As above; current sleep state needs its own assessment | Not proof of a separate active circadian illness | Personal routines, not compulsory cure |
| Low-sunlight background, `IsLifeLackSunlight` | 0 background | As above; background ↔ | Not proof of native vitamin D deficiency | Light/comfort choices; vitamin-cure claims would be new fiction |
| Developmental radiation exposure, `IsLifeRadExposure` | 0 background, modifier | As above; measure current burden separately | Background is not current `StatRad` poisoning | Honest exposure history; no background deletion by AntiRad |
| Specific immunity flags, `IsImmuneHepatitis2/3/4`, `IsImmunePneumonia1/2`, `IsImmuneCO2Poisoning`, `IsImmuneCaffeine2` | + specific disease protection; caffeine immunity 0 | They guard only named effects/stages | No general immunity; natural acquisition of each flag untraced | Narrow diagnostic explanation; no new vaccine claim inferred from the flag |

## Rest and beneficial recovery states

Sources: **Blue Bottle Games rest and treatment effects**, through the
[rest reference](../health-reference.md#symptoms-and-incapacitation) and
[medical-bed study](medical-bed-research.md). Current Phobos capabilities are
documented in the [Ward-3 and Vigil-2 guide](../medical-player-guide.md); its
offline validation does not establish in-game efficacy.

| State | Health value | Direction and what sustains it | What ends or undermines it | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Ordinary sleeping, `Sleeping` | + debt recovery / 0 unconscious posture | Native sleep can repay sleep debt ↑ | Unsafe surroundings, discomfort or unmet needs | Suitable berth and supplies; no extra healing implied by furniture name |
| Comfortable sleeping, `SleepingComfortable` | + | Native comfort and rest effects ↑ | Loss of appropriate conditions | Quality bedding with a real, bounded benefit |
| Medical rest, `SleepingMedical` | + recovery | Native recovery-rate/threshold additions ↑ | Ongoing injury can exceed benefit; current Phobos care needs power and air | Existing Ward-3 is the base; deeper treatments are separate proposals |
| Sitting, `Sitting` | 0 posture / + rest context | Native posture/rest context | Sitting is not a disease cure | Practical recovery seating |
| Secured sitting, `SittingSecure` | + safety context | Native secured posture/tolerance support | Loss of seating or high acceleration | Patient transport seating tied to Auto Nav |
| Roused, `Roused` | 0 transition | Native awakening state | Cause of earlier sleep/KO may remain | Explain awakening separately from fitness for duty |
| Becoming refreshed, `RefreshedBuildUp`, `2/3/4` variants | + accumulation | Native rest builds graded benefit | Interrupted/insufficient rest changes the course | Let real rest earn benefits; no final bonus simply for entering bed |
| Napped, `Refreshed` | + | Native first graded rest benefit | Benefit expiry or insufficient further rest | Short-rest support |
| Refreshed, `Refreshed2` | + | Native graded benefit | As above | Protect a useful rest break |
| Energized, `Refreshed3` | + | Native graded benefit | As above | Longer-rest opportunity without universal healing |
| Enlivened, `Refreshed4` | + | Native highest listed graded rest benefit | As above | Long-haul routine worth preserving |
| Cardio exercise, `IsExercisingCardio`, `IsExercisingCardioHot` | 0 activity marker; intended + fitness | Native 0.1 h markers distinguish ordinary/hard exercise; marker alone has no healing payload | Activity marker alone does not establish training gain, safe intensity or completed recovery | Measure the actual exercise action before proposing a cardio programme |
| Strength exercise, `IsExercisingStrength`, `IsExercisingStrengthHot` | 0 activity marker; intended + fitness | Same distinction for ordinary/hard strength work | Not proof of instant bone repair or permanent trait improvement | Rehabilitation with an actual action/outcome, not a buff granted for the marker |
| Recent activity markers, `RecentlySlept`, `RecentlyAte`, `RecentlyAtePrepared`, `RecentlyDrank`, `RecentlyExercised` | 0 records | Mark that an activity occurred | A recent activity does not prove adequate nutrition, rest or fitness | Care history; never certify health solely from a marker |

## Medicines substances and treatment aftermath

Sources: **Blue Bottle Games medication/drug paths**, in the
[complete treatment and substance reference](../health-treatments-and-drugs.md).
Benefits are target-specific. Native dependence is established for nicotine;
general analgesic, cannabis or alcohol dependence is not established by these
definitions. New interactions, toxicity or addiction would be authored additions.

| Condition family | Health value | Existing upward/neutral direction | Existing cost or downward direction | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| OTC analgesia, `PainkillerMinor1/2` | + comfort/function | Raises pain threshold, then fades: pain response ↑; wound ↔ | Relief wears off; direct native tissue repair absent | Symptom-aware supply/course record; do not invent a proved native overdose |
| Prescription analgesia, `PainkillerPresc1/2` | + comfort/function | Stronger threshold relief: pain response ↑; wound ↔ | Source may worsen while patient feels better | Recovery-duty advice; a risky extended formulation would be a new design |
| Immunostimulation, `Immunostimulant`, `ImmunostimulantCures` | + specified recovery/disease targets | Infection recovery ↑; explicit named-stage removal ↑ | Targets excluded from cure list stay ↔ | Explain scope and retain native drugs before proposing another antibiotic |
| Anti-nausea treatment | + symptom relief; no lasting native drug condition established | Nausea removal and guarded excess-fullness reduction: comfort ↑ | Poison/radiation/illness may stay ↔ | Measured symptom relief and continued observation |
| Gravusine, `AntiGravStim/2` | + G tolerance / possible later − symptoms | Temporarily raises threshold; native active/fading redose guard | Fades; later headache/dizziness chances | Transport planning; tolerance bonus does not make every burn safe |
| AntiRad, `AntiRadStim/2` | + burden reduction / 0 active marker | Consumption reduces `StatRad` immediately | Active payload empty; guarded fading interval; secondary illness may remain | Exposure tracking and honest course timing |
| Ossifex, `OssifexStim/2` | + atrophy support / − itch aftermath | Immediate atrophy reduction plus active rate change | Later itch and social-threshold changes | Rehab planning and expectable-aftereffect advice |
| AntiHypo-V / Sanguifil injector, `AntiHypoVStim/2`; `DcAntiHypoVDose01..06` | + treatment / 0 dose record | Explicit course completion removes micro-g hypovolemia | Long course and restrictive native clinic gates; six-count endpoint ≠ six ordinary injections | Course continuity and financing story; no shortened course promised |
| Nanite first aid, `NanoFirstAid`, `NanoFirstAidAdv`, `NanoFirstAid2` | + recovery support | Strong then fading native healing support | Time, native service cost/guard; continuing harm can exceed support | Specialist care access or explicitly new onboard supply chain |
| Sedated, `Sedated` | + sleep opportunity / − operational responsiveness | Native sleep-threshold/comfort effect | Not a source cure; ordinary purchasable workflow unestablished | Proposed controlled medical use, with watch and duty trade-off |
| Caffeine, `Caffeine1..3`; `IsImmuneCaffeine2` | 0 onset, + alertness tolerance, − crash | Native tolerance effects; later expiry | Sleep debt stays; temperature/bowel effects and crash | Agriculture coffee and deliberate rest choices; concentrate risks would be authored |
| Alcohol, `Tipsy`, `Drunk`; native `DcDrunk` states | + euphoria/analgesia / − recovery/function | Stop intake and allow native course: toward sober ↑ | Healing/hydration/cognitive penalties; general poison burden | Galley social choice with honest operational cost |
| Alcohol KO, `DrunkPassingOut`, `DrunkPassedOut`, `DrunkAlcoholPoisoning` | −, terminal-poison-linked last label | Prevent further intake and address actual state before fatality | KO and fatal general-poison path; final label itself lacks fatal flag | Casualty watch and clear source diagnosis; no miracle hangover rescue |
| Hangover, `HungOver` | − | Fluids, rest and symptom support ↑ | Native symptoms and recovery penalties | Recovery breakfast; improvement must come from actual food/water/comfort targets |
| Nicotine onset/buzz, `Nicotine1/2`, `NicotineBuzz` | 0 onset / + short tolerance effects with costs | Temporary effects; expiry | Smoking can trigger cough/eye irritation and dependence progression | Social vice and cessation support; no endorsement implied |
| Nicotine comedown/downtime, `Nicotine3`, `Nicotine4/Heavy/Stress` | − comedown / 0 waiting stages | Abstinence engages native cessation opportunities | Re-exposure restarts chain; downtime may schedule withdrawal | Track course without a deterministic quit date |
| Withdrawal, `Nicotine5/6`, `NicotineItch`, `NicotineCraving`; `IsSmoker`, `IsHeavySmoker` | − symptoms / 0 identity with health costs | Native probabilistic cessation; supportive comfort ↑ | Re-exposure relieves withdrawal but dependence may remain | Real cessation aid is a proposal; fake quit-charms leave dependence ↔ |
| Energizing cannabis, `Cannabis1..4`, `CannabisHigh`, `CannabisStoned`, `CannabisComedown` | + comfort/mood / − cognition and piloting | Time/source cessation toward baseline; analgesia is not repair | Demand changes, temporary traits, impaired operation and comedown | Agriculture consumer choice; not a guaranteed disease remedy |
| Indica cannabis, `CannabisIndica1..4`, `CannabisIndicaHigh`, `CannabisIndicaStoned` | + comfort/sleep / − operation | Sleep/comfort effects; eventual expiry | Piloting effects persist into comedown; source disease may remain | Recovery comfort with visible duty implications |

## Emotional needs and temporary cognition

Sources: **Blue Bottle Games emotional rules and traits**, through the
[emotional-health reference](../health-chronic-and-traits.md#emotional-health-cognition-and-social-needs).
Every five-stage family below is ordered from fulfilled **+**, through ordinary
**0**, to distressed **−**. Native social/recreation actions are candidates for
support (**I**); this record does not claim each proposed activity already changes
the listed statistic. These are game needs, not psychiatric diagnoses.

| Need and all five state labels | Native family | Candidate upward direction | Downward pressure | Possible Phobos contribution |
| --- | --- | --- | --- | --- |
| Achievement: triumphant, accomplished, adequate, unsuccessful, useless | `DcAchievement01..05` | Accomplishment and native purposeful activity: + direction | Unmet need and illness/drug threshold changes | Small gardening/repair accomplishments; stories must not merely declare success |
| Altruism: selfless, helpful, aloof, selfish, greedy | `DcAltruism01..05` | Opportunities to help: + direction | Unmet need; pain/itch/illness affects tolerance | Voluntary patient care and crew assistance; no moral diagnosis |
| Autonomy: liberated, independent, unsupervised, micromanaged, oppressed | `DcAutonomy01..05` | Choice and suitable independence: + direction | Unmet need or intrusive care: − direction | Patient choices and off-duty freedom; compulsory wellness can backfire |
| Contact: limelight, noticed, getting by, unnoticed, neglected | `DcContact01..05` | Appropriate social contact: + direction | Unmet need and isolation: − direction | Shared meals, crew check-ins and communication |
| Esteem: celebrated, liked, unremarkable, disrespected, humiliated | `DcEsteem01..05` | Respect/recognition: + direction | Unmet need, hostile encounters and illness thresholds | Recognition for useful care/work; embarrassing public health reports can harm |
| Family: cherished, supported, nostalgic, distanced, outcast | `DcFamily01..05` | Meaningful native family/social contact: candidate + direction | Unmet need and separation | Correspondence and familiar recipes; not a guarantee any letter resolves the need |
| Intimacy: loved, connection, neutral, yearning, desperate | `DcIntimacy01..05` | Appropriate relationships/social actions: + direction | Unmet need or incompatible imposed activity | Private communication and space; preserve personal boundaries |
| Meaning: zealous, purposeful, incidental, aimless, ennui | `DcMeaning01..05` | Valued activity and recreation: candidate + direction | Unmet need and threshold modifiers | Growing food, maintaining a home and ordinary objectives |
| Privacy: solitude, peaceful, uncrowded, pestered, badgered | `DcPrivacy01..05` | Suitable private time/space: + direction | Crowding or intrusive contact: − direction | Private berth and discreet sickbay; more company is not always better |
| Security: calm, composed, unafraid, anxious, terrified | `DcSecurity01..05` | Safe surroundings and supportive native interactions: + direction | Threats, illness and hostile interaction: − direction | Reliable air/equipment and clear care information; fake reassurance can hide danger |
| Self-respect: dignified, confident, competent, self-critical, worthless | `DcSelfRespect01..05` | Appropriate agency/activity/social support: candidate + direction | Unmet need and threshold shifts | Dignified care and return to valued duties |

The new local trace distinguishes concrete temporary social effects (**D**) from
these broader activity proposals: `Greeted` affects contact/mood for 1 h;
`GreetedByCrew` affects security/esteem/mood for 1 h; `Kissed` affects intimacy/mood
for 1 h. `GreetedByEnemy` lowers security/esteem tolerance for 0.5 h. Their exact
effect payloads are `CONDGreetedPer`, `CONDGreetedByCrewPer`, `CONDKissedPer` and
`CONDGreetedByEnemyPer`. This is a native basis for supportive interaction, not a
universal therapy mechanic.

| Other state or family | Health value | Useful direction and limit | Possible Phobos contribution |
| --- | --- | --- | --- |
| Friend/crew greeting and kiss, `Greeted`, `GreetedByCrew`, `Kissed` | + temporary social support | Preserve native, relationship-appropriate interaction; benefit expires | Crew check-ins that use existing social behaviour |
| Enemy greeting, `GreetedByEnemy` | − temporary social tolerance | Native expiry toward baseline; avoid added conflict | Quiet care and conflict-sensitive stories |
| Coordination, `IsClumsy`, `IsClumsyTemp`, `IsClumsyTempSleep` | − operational function | Resolve temporary source (sleep, dizziness, drugs); permanent trait cure unestablished | Assistive work and temporary duty advice |
| Awareness, `IsOblivious`, `IsObliviousTemp`, `IsObliviousTempSleep`; `IsObservant`, `IsObservantTemp` | − / + function, 0 personality | Temporary source recovery can restore baseline; personality is not automatically illness | Accessible reminders and clear measurements |
| Cognitive impairment, `IsObtuse`, `IsObtuseTemp`, `IsObtuseTempSleep` | − task function | Treat temporary source; no general intelligence cure established | Rest and cognitive workload choices |
| Patience, `IsPatient/Temp`, `IsImpatient/Temp` | 0 personality; temporary state can affect care | Source relief or temporary expiry; do not erase personality | Individual care routines |
| Optimism/pessimism, `IsOptimist/Temp`, `IsPessimist/Temp` | 0 personality; wellbeing context varies | Appropriate support; not automatic clinical disorder | Comfort, meaningful activity and honest uncertainty |
| Bravery, `IsBrave`, `IsBraveTemp` | 0 personality; situational safety costs/benefits | Temporary effect expiry; bravery is not restored tissue or safe judgment | Distinguish confidence from physical fitness for duty |
| Trust/suspicion, `IsTrusting/Temp`, `IsSuspicious/Temp` | 0 personality/context | Temporary cause relief; unsupported psychiatric cure | Patient trust and fraud stories; suspicious can be sensible |
| Shyness/lust and substance-triggered social dispositions, `IsShy/Temp`, `IsLustful/Temp`, `IsLiarTemp`, `IsVengefulTemp` | 0 personality/context | Resolve temporary drug/illness source where relevant | Preserve agency; no therapy that standardises personality |
| Weirdness, `DcWeird01..04`: rational, superstitious, paranoid, unhinged | 0 → − game/story context | A general treatment path was not established | Comfort and story response; not proof of clinical psychosis or a psychiatric drug system |
| Memory families, `Mem...`, `MemSuppress...`; `DebugMemoryLoss` | 0 memory/story records; brain-damage-labelled debug state − | No general native PTSD, memory repair or suppression cure established | Memoirs, support and dubious memory clinics as story concepts, not a fabricated current disease model |

## Story artificial bodies and other boundaries

Sources: **Blue Bottle Games plot/artificial-body definitions**, in
[story boundaries](../health-chronic-and-traits.md#story-and-supernatural-effects--spoilers)
and the [fatal-state reference](../health-reference.md#fatal-outcomes-and-intervention-windows).
This table contains spoilers.

| Boundary state | Health value | Direction supported by evidence | Creative boundary |
| --- | --- | --- | --- |
| Meat mutation, `IsMeatMutated` | + recovery/CO2 immunity / − higher resource demand | Native mixed physiological effects; no ordinary antibiotic reversal established | Care for the changed patient; a cure/mutation product is a deliberate story design |
| Anti-meat doses, `StatAntiMeat`, `DcAntiMeat00..03` | 0 course data; intended therapeutic purpose | Clinic entry guarded by `TNever`; ordinary access not established | Research/black-market story, not an existing advertised clinic cure |
| Necrotized handprint, `DeathHandprint`, `DeathHandprintSoon` | Fatal plot outcome | No conventional field-medical reversal established | Charms can be fraudulent/comforting without secretly undoing the story |
| Artificial component failure, `ComponentFailure` | Fatal for its target | Human recovery does not establish robot repair | Separate robotic maintenance concept, Manufacturing fit |
| Artificial armor compromise, `CompromisedArmor` | − protective function | Repair workflow needs separate investigation | Armor service; not blood or infection treatment |
| Hacked reboot, `HackedRebooting` | − temporary function | Artificial-system state, not human sleep | Cybersecurity/recovery concept |
| Artificial toughness, `IsToughArtificial` | + tolerance for its target | Preserve target-specific identity | No assumption that human medicine changes artificial physiology |
| Terminal flags and handoffs, `Death`, `DeathSoon`, `DcBlood04Soon`, `DcPoison05Soon`, `PulmonaryHemorrhageSoon`, `TraumaBrainSevereSoon` | Fatal/effectively terminal | Prevention before native kill; removing a flag is not demonstrated resurrection | Any resurrection fiction needs a separate owner choice and research |
| Ship pressure/gas/temperature bands, `DcGasPp...`, `DcGasTemp...`, container-pressure bands | 0 measurements / hazardous context | Environmental records can explain exposure; not extra human diagnoses | Pair air/environment observations with patient state; no independent room-gas model |
| Heartbeat timer, `IsHeartbeat` | 0 helper | A hidden generic timer tick, not a measured clinical heart rate | Do not sell the presence of this marker as a cardiac diagnostic |
| Population/relationship/plot loneliness, `DcPopulation...`, `REL...`, `DcPlotLonesome01..04` | 0 context; loneliness plot can be distressing | Not interchangeable with physiological illness or the eleven emotional needs | Habitation/story opportunities; plot progression and relationship identity remain native |

## Research limits that matter to the next design

- Match each intervention to its **target axis**. Pain relief, blood-loss recovery,
  radiation reduction and emotional support must not substitute for each other.
- Exact natural acquisition remains unresolved for some cholera, hepatitis and
  CRS paths; pneumonia has a missing-target anomaly. Defined stages are not proof
  of a working contagious-disease simulation.
- Fatal states are already fatal. Nominal warning timers do not guarantee a rescue
  window, especially in accelerated time.
- The current core data matches the earlier study, but the current assembly does
  not. Implementation-specific assumptions from C20 require a fresh trace.
- Existing Phobos Medical is Ward-3 recuperation, Vigil-2 observation and native
  cloth/splint crew care. New disinfection, medicine administration, oxygen therapy,
  transfusion, surgery and resuscitation remain separate proposed capabilities.
- Ship's Water changed hydration/hygiene in the earlier modded installation.
  Refresh its current version/overrides and other medical providers before selecting
  implementation; core-data agreement does not prove modded behaviour agrees.

The companion [opportunity catalogue](health-intervention-opportunities.md)
records agent recommendations and alternative outcomes. No feature selection,
new equipment identity, balance value, implementation milestone or gameplay
readiness is implied by inclusion here.

## Record validation

The offline source check resolved **617 native identifiers** used directly or
through stage/scar/chronic shorthand and confirmed all eight prior fingerprints.
The tables contain **218 condition, family, modifier and boundary rows**, not 218
independent diseases. Every chronic-ailment trait and scar family in the inspected
index has an owning row. Source resolution confirms that the named data exists;
it does not prove natural availability, effect strength or treatment success.
