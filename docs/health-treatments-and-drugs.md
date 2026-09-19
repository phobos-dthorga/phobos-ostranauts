# Treatments, medicines and recreational drugs

Part of the [health reference](health-reference.md). Installed core:
**1.0.1.4**, inspected **2026-09-20**. **D** = definition-backed,
**C** = selected runtime code, **I** = practical/design inference.
No medical treatment was tried in a game during this research.

## Treatment classes

Four different promises should appear separately in a medical UI:

| Promise | Native example | What it does not establish |
| --- | --- | --- |
| Prevent additional harm | Safe atmosphere; bandage staunching | Reversal of damage already sustained |
| Interrupt a condition | Immunostimulant's explicit named-stage removals | Cure of every infection or its residual systemic effects |
| Relieve symptoms | Analgesic threshold changes; anti-nausea removal | Wound repair, detoxification or disease eradication |
| Improve recovery | Medical rest; nanorobotic first aid | Survival when ongoing injury exceeds recovery |

## Wound care and supportive care

Sources: wound-slot interactions, `CONDWound...` payloads in `loot.json`,
`BandageCleanDegrade`, `SleepingMedical`, and runtime `Wound.Run` and
`Wound.ApplyEffectsParent`. **D/C**

| Treatment | Native target/effect | Limit, cost or complication |
| --- | --- | --- |
| Clean cloth dressing — `ItmScrapClothClean` | Correct wound slot supplies `IsStaunched`; stops that wound's direct unstaunched blood contribution. | Does not replenish blood or stop separate internal bleeding. Clean dressing degradation uses a nominal 25 h ticker adding item damage; do not interpret this as a tested universal 25 h service life. Inspect/replace degraded dressings. |
| Dirty cloth dressing — `ItmScrapClothDirty` | Also supplies staunching. | Adds wound infection rate +0.01. It can address bleeding while worsening another problem; no claim of equal cleanliness or long-term safety. |
| Splint — `ItmSplint01` | Supplies `IsSplinted`; native fracture healing avoids the unsplinted random factor. | Does not instantly heal bone. Correct body-part targeting and treatment-slot fit matter. |
| Clean-water washing — `CONDWoundWaterClean` | Adds `StatDisinfectAmount` 0.5 to the wound. | The runtime clamps and consumes disinfection in a wound update; it is not a permanent infection-immunity buff. |
| Soapy clean-water washing — `CONDWoundWaterCleanSoap` | Disinfection amount 0.95. | Stronger listed disinfection than plain water; actual repeated-use behavior and consumable transactions need gameplay verification. |
| Hard liquor on wound — `CONDWoundLiquorHard` | Disinfection amount 0.8. | Also adds cut damage 0.06. A “stronger disinfectant” comparison that omits tissue damage would be misleading. |
| Weak liquor on wound — `CONDWoundLiquorWeak` | Disinfection 0.4, cut damage +0.03. | Also contains a malformed infection-rate literal; do not rely on its intended numeric value. |
| Coffee on wound — `CONDWoundCoffeeDrink` | Disinfection 0.2. | Contains the same malformed infection-rate literal. These native options are not real-world wound-care recommendations. |
| Ordinary rest/sleep | Native rest states influence recovery and resource demand. | Environment, sleep comfort, pain and other conditions may prevent useful rest. |
| Medical recuperation — `SleepingMedical` | Wound-heal rate +0.2; blood-heal +0.0045; infection-heal +0.05; pain threshold +0.1, plus comfort/security effects. | These are additions to native rate/threshold fields, **not** “20% of every wound healed” or a universal 20% speedup. Bed availability/power behavior needs a controlled test. |
| Nutrition, safe fluids, temperature and atmosphere | Avoids or reverses the deficits/exposures feeding illness. | Supportive care cannot be assumed to erase named diseases or terminal conditions. The current installed water mod changes some calculations. |

`TIsBedMedical` checks medical-bed identity, installation, damage, off state and
bed eligibility. Power has a separate `TIsPowered` trigger. The existence of
these checks is not proof that every native care path checks power continuously.
This matters for our intended powered equipment: measure power loss while care
is already underway, not only at interaction start.

The accelerated medical-sleep path also references
`CONDTick1HourSleepMedicalPhysio`, which directly modifies sleep, blood loss,
infection, poison, radiation and wound fields. Do not add this payload to ordinary
rest mathematically and claim a combined hourly cure rate: invocation and target
ownership are separate questions. **D**

## Medicines and clinical treatments

Sources: `SeekDrug...` interactions, `CONDDrug...AllowDirectThem` consumption
payloads, named conditions and their `aPer`/`aNext` chains, and `TCan...Inject`
guards. The values below are **native field changes**, not independently tested
clinical measurements. **D**

| Medicine / service | Immediate or active effect | Duration, aftermath and limits |
| --- | --- | --- |
| **OTC pain reliever** — `ItmPillPainkillerMinor01`; `PainkillerMinor1/2` | Pain-threshold modifier +0.2, then +0.1. | 4 h active + 1 h wearing off. Symptom/function support; no direct cut/blunt/blood repair. Both stages have timer-reset behavior and count cap 1. |
| **Prescription pain reliever** — `ItmPillPainkiller01`; `PainkillerPresc1/2` | Pain-threshold modifier +0.7, then +0.35. | 4 h active + 1 h wearing off. Does not prove opioid tolerance, addiction, overdose or real-world drug interactions. |
| **Antibiotic / immunostimulant** — `ItmPillAntibiotic01`; `Immunostimulant` | Consumption adds immunostimulant. `DcImmunostimulant` adds infection-heal rate +0.2 and triggers explicit instant cures. | 12 h timed condition, timer reset enabled, cap 1. Cure list: cholera 1–3, gastroenteritis 1–2, pneumonia 1–3, **hepatitis 1 only**. Does not explicitly clear chemical pneumonitis or chronic respiratory traits. |
| **Anti-nausea pill** — `ItmPillAntiNausea01` | `CTDrugAntiNauseaAllowDirectThem` invokes nausea removal and a guarded reduction of excess satiety. | No lasting prophylactic antiemetic condition was established. It does not directly clear `StatPoison`, radiation, infection or every vomiting condition. |
| **Gravusine** — `ItmAntiGravPen01`; `AntiGravStim/2` | Adds +0.5 to `ThreshStatGrav`, fading to +0.25. | 0.5 h active + 1 h fading, then independent 25% dizziness and headache triggers. Native use guard forbids redosing while either stage is present. Do not advertise “stacks twice” or translate the raw threshold modifier into +0.5 physical G without further validation. |
| **ChymAdd / AntiRad** — `ItmAntiRadPen01`; `AntiRadStim/2` | Consumption subtracts **500 from `StatRad`**, then adds the active condition. | 24 h active marker + 1 h due/fading stage. The active marker's own payload is empty; fading affects altruism/autonomy/security thresholds. Use guard forbids both active and due stages, so the nominal guarded redose interval is 25 h. Not a verified shield, cancer cure or blanket cancellation of existing ARS stages. |
| **Ossifex** — `ItmOssifexPen01`; `OssifexStim/2` | Consumption subtracts **0.5 `StatAtrophy`**; active condition subtracts 0.95 from `StatAtrophyRate`. | 24 h active + 1 h itch. Itch modifies altruism/autonomy thresholds; guarded use forbids both stages. The “itch” is a native aftermath, not proof of a persistent Ossifex addiction/tolerance system. |
| **AntiHypo-V** — `ItmAntiHypoVPen01`; `AntiHypoVStim/2` | Adds a dose count and a 72 h support condition affecting blood/wound/infection recovery, fatigue, carrying, metabolism and thermoregulation. | Use guard forbids active treatment and completed-course marker. At expiry with dose-band 05, it adds a completion condition and increments the count. Completion removes `IsHypovolemic2`; see course analysis below. Not an emergency blood transfusion. |
| **Nanorobotic first aid** — `NanoFirstAid` | Active payload supplies immunostimulant, wound-heal rate +0.4 and blood-heal rate +0.0133. | 3 h strong effect + 1 h `NanoFirstAid2` with +0.15 wound-heal and +0.0045 blood-heal. Native menu advertises $2,500 / 4 h. Not instantaneous full healing. |
| **Extended nanorobotic first aid** — `NanoFirstAidAdv` | Same strong payload. | 23 h strong + 1 h fading. Menu advertises $25,000 / 24 h. The service guard rejects existing normal, advanced or fading nanite treatment. |
| **Sedation** — `Sedated` | Sleep threshold -0.8; sleep comfort +10. | 8 h condition with timer reset and cap 1. A dedicated ordinary purchasable sedative item was not established in the inspected medicine objects. Presence of the condition does not prove a player-accessible anesthetic workflow. |
| **Scar/chronic-ailment removal** — `ENCMedService...` | Clinic entries link removal payloads for 15 ailments and 25 scar families. | Thief/piracy brand entries refuse treatment and have no attached removal payload. Other services use encounter and ledger paths; payment, residual timers and restored function need verification. Neither surgery machinery nor a portable equivalent is implied. |

### What the AntiHypo-V course actually says

The traced path supports this **data-derived intended course**, not a tested
prescription:

1. A successful injection adds one `StatAntiHypoVDose` and starts 72 h of
   `AntiHypoVStim`.
2. `TCanAntiHypoVInject` blocks another injection while that active condition
   exists.
3. The fifth-dose band is `DcAntiHypoVDose05` (nominal count 5 to 6).
4. Expiry triggers for course completion and the final counter increment require
   that fifth-dose band. `AntiHypoVStim2` removes `IsHypovolemic2` and has no normal
   expiry; its presence blocks further injections.

This suggests **five sequential 72 h courses**, a minimum nominal 360 h / 15 game
days from the first injection with immediate allowed redosing, followed by a
completion count of six. It is not evidence that six ordinary injections are
needed. Verify condition-rule update order and save/reload before building a
course UI around that interpretation.

The menu advertises $1 million per dose, tests `AnyStation_MVOL`, and the purchase
acceptance tests `TIsMVOLStrataLegal`. Those gates matter: the shared menu list is
not proof every clinic sells it. Prices here are menu text; ledger execution was
not tested.

### Repeat dosing, stacks and interactions

`Condition.AddAmount` clamps counts and can reset condition age. Many medication
stages cap at one. Therefore repeat use may refresh a stage instead of increasing
its effect. Some use interactions explicitly block redosing while either the
active or fading stage exists; analgesics have different entry paths. **D/C**

This supports testing **each drug**, not inventing one global stacking rule.
Especially important cases are an analgesic taken during its own fading stage,
mixing OTC and prescription relief, and nanite-supplied immunostimulant lifetime.
The nanite payload supplies a 12 h condition from a shorter-lived parent effect;
the resulting ownership/removal behavior must be measured before promising a
full additional 12 h of antibiotic benefit after nanites wear off.

No general pharmacokinetic concentration model, analgesic dependence, cannabis
dependence, drug-specific organ damage or comprehensive drug–drug interaction
system was established. That is a limit of the traced evidence, not proof no
such behavior can exist in a plugin or uninspected engine path.

## Recreational drugs and dependence

Sources: `Caffeine...`, `Tipsy`, `Drunk...`, `Nicotine...`, `Cannabis...`,
`CannabisIndica...`, their effects and transition triggers, and `DcDrunk`. **D**

| Substance | Benefits/effects | Aftermath and prevention/response |
| --- | --- | --- |
| **Caffeine / coffee** | About 1 min onset (`Caffeine1`), then 2 h high: sleep threshold +0.5, fatigue threshold +0.25, temperature +0.4 and bowel need +6, plus an achievement-threshold change. | 2 h crash stage includes temperature -0.4. `IsImmuneCaffeine2` can block the high. Stop repeated intake and permit normal rest; no direct “detox caffeine” treatment traced. Its high changes tolerance, not automatically the underlying sleep debt. |
| **Alcohol** — rice wine/vodka paths | `StatDrunk` bands: tipsy at 2, drunk at 4, blackout path at 8. Tipsiness uses minor analgesic/euphoria payloads; drunkenness uses prescription-strength analgesic/euphoria payloads. | Drunkenness also worsens wound/infection recovery, hydration and thermal handling, with cognitive/dizziness effects. Stop intake and allow native recovery in a safe environment. Do not present alcohol as a clean substitute for medical pain relief. |
| **Alcohol blackout / poisoning** | `DrunkPassingOut` repeats on a short timer; `DrunkPassedOut` has `bKO=true` and can repeat. The drunkenness rule also adds general poison burden. | The alcohol-poisoning label is triggered when already-fatal `DcPoison05` is present. Ordinary waiting is not a guaranteed remedy after the terminal poison state. No alcohol-dependence/withdrawal syndrome was established from these definitions. |
| **Hangover** — `HungOver` | 80% transition from the expiring drunk condition; nominal 3 h. Possible nausea/headache, worse hydration and healing, bowel/piloting penalties. | Hydration/rest and symptom management address consequences; neither reverses a fatal poison event. |
| **Nicotine** — Viceroy cigarette paths | ~15 s onset, 0.25 h buzz, 0.3 h comedown. Changes sleep/fatigue/security thresholds and fatigue coefficient; buzz adds defense and changes thirst/appetite-related fields. | Smoking transitions can cause cough and irritated eyes. Dependence, recurrent withdrawal and probabilistic cessation are explicitly defined below. |
| **Cannabis, energizing variant** — Damask Rose path; `Cannabis1..4` | ~1 min onset → ~5 min rising high → 0.8 h stoned → 0.25 h comedown. Pain-threshold relief, appetite/thirst-demand changes, meaning/social effects and probabilistic temporary personality/cognitive effects. | Defense/piloting penalties and possible dizziness/clumsiness/suspicion. Stop exposure, allow the stages to expire, and manage ordinary needs. Analgesia is not tissue repair. |
| **Cannabis, indica variant** — Unicorn Dream path; `CannabisIndica1..4` | Similar timers. Stronger pain-threshold relief (+0.4 rising, +0.6 stoned) and substantial sleep-facilitating effects, with appetite and temporary cognitive/personality changes. | Stoned stage sleep threshold -0.8 and comfort +10; piloting penalties continue through the comedown. Not a demonstrated disease cure or risk-free substitute for a medical workflow. |

### Nicotine addiction and cessation

This is the strongest explicit native dependence model found in this pass.

- Onset attempts `TUpIsSmokerChance`: **2%** to acquire smoker status when neither
  smoker nor heavy smoker is present. `TUpIsHeavySmokerChance` has **0.5%** chance
  when already a smoker and not heavy. These are trigger probabilities, not
  percentages per hour or per real-world cigarette.
- After buzz/comedown, ordinary dependence has a **4.07 h** downtime condition;
  heavy dependence has a **1.07 h** one. A stress-related **0.06 h** timer also
  exists. Subsequent itch/craving checks include awake/dependence requirements
  and 50% chances.
- `Nicotine5` itch and `Nicotine6` craving each last **0.3 h**. They penalize pain
  tolerance, sleep comfort and several emotional thresholds; craving is stronger.
- Expiring itch/craving invokes smoker-removal checks: **2%** for ordinary smoker
  status, **0.5%** for heavy status, with appropriate guards. Heavy withdrawal
  also schedules another downtime cycle.
- Re-exposure removes several withdrawal/downtime stages and starts the buzz
  chain again. That is symptom relief through continued use, not cessation.

**Inference:** abstinence can engage a native probabilistic cessation path, but
there is no fixed “quit after X days” guarantee here. Self-referential smoker
payloads and condition removal should be checked in a controlled test before a
Phobos cessation feature promises a deterministic outcome. No nicotine patch or
cessation medicine was established.

## Practical scenario map for mod research

The following are **testable design hypotheses**, not proven rescue protocols.

| Scenario | Native workflow worth testing | What a new device could add |
| --- | --- | --- |
| Bleeding cut, patient still conscious | Targeted clean dressing, wound cleaning, monitoring and recovery support | Locate the contributing wound and show whether blood loss has actually slowed |
| “Bleeding internally” despite dressings | Inspect chest/head/abdominal wounds and radiation-related state | Explain possible sources without claiming a bandage addresses all of them |
| Recurrent pain with healthy-looking wounds | Check scar/chronic trait episodes and analgesic phase | Episode history and distinction between symptom relief and definitive removal |
| Ill after contaminated-air exposure, now in clean air | Inspect persistent pneumonitis and the previous exposure context | Distinguish present atmosphere from a delayed lung consequence |
| Named infectious disease | Check exact stage against immunostimulant's cure list | Indicate why early hepatitis differs from later stages |
| Long-term micro-g patient | Track hypovolemia, atrophy, dose guards and actual course completion | A treatment-course record rather than an instant “restore health” button |
| Patient declining while recuperating | Compare ongoing loss with observed recovery, supplies and device power | Detect an ineffective treatment course and escalate before terminal flags |

Any new handheld treatment must establish actor, tool, patient and wound roles,
resource consumption, interruption, depleted battery behavior and save/reload.
An item interaction whose `Them` receives medicine after an inverse interaction
is not sufficient proof that a medic can administer it to an unconscious third
party. See [runtime ownership findings](medical-runtime-findings.md).
