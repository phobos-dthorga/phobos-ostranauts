# Chronic ailments, scars, traits and emotional health

Part of the [health reference](health-reference.md). Inspected core version:
**1.0.1.4**, research date **2026-09-20**. Findings are definition-backed (**D**)
unless marked as inference (**I**); none were gameplay-tested in this round.
Source abbreviations and limitations are in the
[evidence record](health-evidence-and-gaps.md).

## Chronic ailments

The data normally separates a permanent `TraitChronic...` condition, a recurrent
`Chronic...` timer and one or two episode conditions. Removing the episode is
not the same as removing the trait. Conversely, properly removing the trait must
also account for already-running timers and symptoms.

All **15 ailment families** below have an `ENCMedService...` removal entry reached
through `ENCOKLGMedServiceRemoveChronic`. These link to `CONDChronic...Remove`
payloads in `loot.json`. The service's existence is stronger evidence than a
condition description saying “permanent”; availability, payment and resulting
in-game state were not tested.

The cycle and episode times below are nominal game hours. **A cycle is an
opportunity for a conditional/probabilistic episode, not a guarantee that an
attack happens exactly that often.** For example, asthma's episode trigger has
60% chance, and its severe transition has 50% chance. Do not transfer those
probabilities to the other ailments.

| Ailment / identifier suffix | Recurrence timer; episode timers | Effects and care implications |
| --- | --- | --- |
| Asthma — `Asthma` | 7.56 h; 0.5 h, severe 0.08 h | Dyspnea, cough and lower fatigue tolerance; severe episode adds gasping and more cough. Safe air avoids an additional respiratory burden but is not a demonstrated trait cure. Clinical removal exists. |
| Emphysema — `Emphysema` | 7.1 h; 0.4 h, severe 0.15 h | Similar respiratory episode effects. A separate `TraitChronicEmphysemaFAF` helper also exists; do not count it as another disease. Clinical removal exists. |
| Silicosis — `Silicosis` | 20 h; 0.6 h, severe 0.15 h | Recurrent respiratory complications. The name alone does not establish a fully simulated cumulative dust-dose model. Clinical removal exists. |
| COPD — **`CPOD`** in identifiers | 11 h; 0.4 h, severe 0.15 h | Dyspnea/cough and fatigue-tolerance loss, with worse severe episodes. Preserve the engine's spelling when referencing it. Clinical removal exists. |
| Tuberculosis — `Tuberculosis` | 7.7 h; 0.25 h, severe 0.15 h | Respiratory symptoms and weakness; severe stage adds pain, chest pain, possible severe/bloody cough and fever. Not in the antibiotic instant-cure list. Clinical removal exists; contagious spread was not established. |
| Allergic sensitivity — `AllergicSensitivity` | 11.56 h; 0.92 h, severe 0.25 h | Irritated eyes and nasal allergy, with cough and pain in severe episodes. No dedicated antihistamine cure was established. Clinical removal exists. |
| Herniated disc — `HerniatedDisc` | 9.17 h; 0.5 h, severe 0.5 h | Strained-back state and pain; severe stage adds work/movement penalties. Analgesia does not remove the recurrent source. Clinical removal exists. |
| Mangled hand — `MangledHand` | 9.03 h; phantom-pain episode 0.1 h | Baseline recurrent condition adds work penalty and pain; episodes add pain, possible itching and an altruism-threshold penalty. Clinical removal exists. |
| Torn ACL — `TornACL` | 9.27 h; 0.15 h | Episodes add movement penalty 0.2 and pain 13. Clinical removal exists. |
| Torn MCL — `TornMCL` | 13.27 h; 0.1 h | Same listed movement and pain additions. Clinical removal exists. |
| Torn meniscus — `TornMeniscus` | 13.27 h; 0.1 h | Same listed movement and pain additions. Clinical removal exists. |
| Patellar tendonitis — **`PatellerTendonitis`** in identifiers | 6.27 h; 0.12 h | Same listed movement and pain additions. Preserve the native spelling. Clinical removal exists. |
| Carpal tunnel syndrome — `CarpalTunnelSyndrome` | 8.77 h; 0.15 h | Episodes add work penalty 0.2 and pain 13. Clinical removal exists. |
| Tendonitis — `Tendonitis` | 8.77 h; 0.15 h | Same listed work and pain additions. Clinical removal exists. |
| Arthritis — `Arthritis` | 6.07 h; 0.3 h, severe 0.1 h | Pain, reduced altruism threshold, work penalty and possible temporary impatience; severe stage strengthens these effects. Clinical removal exists. |

Sources: `conditions/conditions.json`, `condtrigs/condtrigs.json`,
`loot/loot.json`, `interactions/interactions_encounters.json`.

**Prevention limits:** many conditions can originate from character history,
events or injury-related content. This pass does not establish that ordinary
repetitive work naturally causes every named tendon disorder, or that avoiding a
real-world risk factor prevents the game's scripted occurrence. For an existing
patient, symptom relief and removal services are supported more directly than a
universal lifestyle-prevention system.

## All scar families

All **27 scar families** use the same inspected episode payload: pain +10,
50% itching entry and altruism threshold -0.13, with a **0.1 h (6 min)** episode
timer. Their recurrence timers differ. The numeric additions are native data
effects, not a promise of a fixed displayed pain level or a multiplicative work
penalty.

For each suffix below, the trait is `TraitChronicScar<suffix>`, the recurrent
condition is `ChronicScar<suffix>` and the symptom stage is
`ChronicScar<suffix>2`. Matching `ENCMedServiceScar<suffix>` menu entries exist,
but only **25** link to removal payloads. The thief and piracy brand entries
explicitly refuse treatment on legal-liability grounds and have no removal
payload attached. A standalone thief-brand removal payload exists, but that is
not evidence of an accessible cure. **D**

| Scar source / suffix | Recurrence timer (h) |
| --- | ---: |
| Knife — `Knife` | 7.07 |
| Gunshot — `Gunshot` | 6.67 |
| Crossbow — `Crossbow` | 6.37 |
| Snooker injury — `Snooker` | 6.87 |
| Brawling / fight club — `FightClub` | 8.07 |
| Dart — `Dart` | 6.93 |
| Appendectomy — `Appendectomy` | 14.07 |
| Heart surgery — `HeartSurgery` | 12.07 |
| Brain surgery — `BrainSurgery` | 11.07 |
| Cybernetic surgery — `CyberneticsSurgery` | 5.87 |
| Skin graft — `SkinGraftSurgery` | 7.47 |
| Handcuffs — `Cuffs` | 8.07 |
| Severe burn — `Burns` | 9.37 |
| Hunt cat — `HuntCat` | 9.97 |
| Hard transfer — `HardTransfer` | 10.07 |
| Sports accident — `Sports` | 6.67 |
| Breakout injury — `Breakout` | 5.83 |
| Racing accident — `Racing` | 8.97 |
| Cargo accident — `Cargo` | 10.87 |
| Airlock accident — `Airlock` | 5.77 |
| Mining accident — `Mining` | 7.47 |
| Industrial accident — `Industrial` | 7.77 |
| Cooking accident — `Cooking` | 13.07 |
| LEO brutality — `Activism` | 10.27 |
| Botched enhancement — `BotchedCosmetic` | 11.77 |
| Thief brand — `BrandThief` | 7.97 |
| Piracy brand — `BrandPiracy` | 6.15 |

Scar names establish history/content categories, not necessarily playable surgery,
amputation, cybernetic implantation or every corresponding injury-acquisition
mechanic. The concrete acute-to-chronic link traced here is
`BurnsSevere` → `TUpTraitChronicScarBurns`. Do not assume every cut creates the
matching scar automatically.

The permanent traits also differ beyond their shared pain episodes: knife,
gunshot, crossbow, fight-club, LEO-brutality and piracy-brand scars add native
`StatThreat` +6; handcuff and thief-brand scars add +3. The other inspected scar
trait payloads only supply their recurrence condition.

**Care:** analgesia can manage pain while supported clinical removal targets the
scar trait. No ordinary clinic cure was established for the two brand scars.
Treating an itch or pain episode leaves the scar's recurrent source. Preventing
additional trauma is sensible, but does not remove an existing scar. **D/I**

## Persistent physiology and health-relevant traits

Sources: named `Is...` conditions, associated `COND...Per` payloads, simple
condition immunity entries, age rules and relevant medication paths. **D**

| Trait/system | Established effect | Management / design limit |
| --- | --- | --- |
| Micro-g hypovolemia — `IsHypovolemic2` | Lower blood/wound/infection recovery, higher fatigue and food/water demand, reduced carrying tolerance and poorer thermoregulation. | AntiHypo-V has a dedicated course/removal path. This differs from active blood loss and cannot be diagnosed from `StatBlood` alone. |
| Immunosuppressed — `IsImmunoSuppressed` | Infection-heal rate -0.2. | Immunostimulation can improve the recovery balance, but is not explicit permanent trait removal. |
| Fast/slow healer — `IsHealerFast`, `IsHealerSlow` | Wound-heal rate ±0.025, blood-heal rate ±0.0089, infection-heal rate ±0.2. | Include in prognosis; do not claim every character recovers at a baseline rate. |
| Fast/slow metabolism — `IsMetabFast`, `IsMetabSlow` | Food and hydration rates ±0.25. | Provisioning and treatment support must account for demand. |
| Iron stomach — `IsIronStomach` | Supplies `IsImmuneGastroenteritis2`. | Specific symptomatic-stage immunity, not universal immunity to poison, cholera or contaminated environments. |
| Weak stomach — `IsWeakStomach`, `WeakStomachIllness` | Repeating 55 h timer has a 15% symptomatic gastroenteritis trigger unless immune. | This is a concrete non-food-specific disease source; avoid promising that only eating bland food will eliminate the native timer. |
| Tough / fragile — `IsTough`, `IsFragile` | Change wound fraction and pain threshold by ±0.2, plus gravity and social/threat modifiers. | Mod diagnostics should read effective tolerance, not assume identical damage response. |
| Strong / feeble — `IsStrong`, `IsFeeble` | Change carrying, attack, mass and gravity tolerance. | Distinguish permanent build/fitness from acute weakness. Training/removal availability is a separate question. |
| Fit / unfit — `IsFit`, `IsUnfit` | Gravity tolerance ±0.125 and fatigue threshold ±0.25, plus security/threat modifiers. | Native exercise/training exists; measure actual progression before promising a training schedule. |
| Insomnia — `IsInsomniac`, `IsInsomniacTemp` | Sleep-comfort -3; temporary version lasts 0.5 h. | Improve sleep conditions and address triggering illness/drugs. A sedative may facilitate sleep but does not explicitly cure the permanent trait. |
| Heavy/light sleeper — `IsSleeperHeavy`, `IsSleeperLight` | Sleep comfort +2 / -2 respectively. | Do not conflate easier sleep with deeper medical recovery. Specific awakening interactions need tests. |
| Visually impaired / blinded — `IsVisualImpaired`, `IsBlinded` | Defense, security/threat and piloting penalties; blindness is the stronger defense penalty. | Cause and applicable equipment/correction paths must be checked separately. No general eye surgery cure established here. |
| Poor/sharp hearing — `IsPoorHearing`, `IsSharpHearing` | Defense -0.03 / +0.03 respectively, alongside hearing-related identity. | Sensory conditions are relevant to diagnostics/accessibility; this pass does not establish a hearing-aid treatment. |
| Pain-related personality — `IsAgliophobic`, `IsMasochist` | Pain threshold -0.25 / +0.5 respectively; masochist also changes security/threat. | A displayed pain response need not imply a different wound. Do not medicalize every personality trait. |
| Hygiene/appetite dispositions — `IsTidy`, `IsSlovenly`, `IsGlutton`, `IsTemperate` | Existing trait modifiers around needs and behavior. | Treat them as modifiers to care/provisioning, not invented diseases requiring cures. |
| Aging — `DcAging01..05`, `RandomAgeChronic` | Age bands at 31, 45, 65 and 80, plus a named age-related chronic mechanism. | Exact acquisition frequencies and the entire aging implementation were not traced; do not claim guaranteed onset at a birthday. |
| Developmental background — `IsLifeMicroG`, `IsLifeLackCircadian`, `IsLifeLackSunlight`, `IsLifeRadExposure` | Each inspected payload adds -0.09 to `ThreshStatAge`. | Background affects the age framework. It does not prove active circadian disease, vitamin deficiency or current radiation poisoning. |
| Smoking / heavy smoking — `IsSmoker`, `IsHeavySmoker` | Native nicotine dependence flags and recurring itch/craving routes. | See [substances](health-treatments-and-drugs.md#recreational-drugs-and-dependence); there are probabilistic cessation triggers. |

There are **43 `TraitChronic...` definitions** in the inspected condition index:
15 ailment traits, 27 scar traits and one extra emphysema helper variant. Counting
every helper and episode as a separate diagnosis would inflate the catalogue.

## Emotional health, cognition and social needs

Physical illness changes emotional thresholds throughout the inspected data.
There are eleven main emotional-need families: achievement, altruism, autonomy,
contact, esteem, family, intimacy, meaning, privacy, security and self-respect.
Their `Dc...01..05` states range from satisfied to distressed. These are game
need states, not validated psychiatric diagnoses. **D**

Examples matter for medical mod behavior:

- Pain, nausea, blood loss, infection, scars and nicotine withdrawal alter social
  tolerance. A difficult patient may have a bodily cause for the behavior.
- Temporary clumsiness, obliviousness, obtuseness, impatience, suspicion and
  insomnia can be effects of exhaustion, concussion, illness or substances.
  Removing the personality-looking flag alone can leave the cause active.
- Rest, suitable living conditions and native social/recreation interactions are
  plausible supportive measures. They are not a proven cure for every memory or
  plot state. **I**

`DcWeird01..04` labels the rational/superstitious/paranoid/unhinged progression.
`Mem...` and `MemSuppress...` families represent memory-related interaction states,
while `DebugMemoryLoss` is a separate brain-damage-labelled condition. This pass
does **not** establish a general depression/PTSD/psychosis treatment simulator,
psychotherapy efficacy or psychiatric medication system from those names.

For a future social or medical feature, preserve the distinction between a
personality trait, an emotional need, a memory/plot state and a physical symptom.
Social support could provide a future medical tie-in, but it is not an implemented
Phobos treatment.

## Story and supernatural effects — spoilers

Sources: `conditions_plots.json`, `loot_plots.json`, core transition helpers and
medical-service encounter entries. **D**

| Effect | What is established | Boundary for medical mods |
| --- | --- | --- |
| Meat-related mutation — `IsMeatMutated` | `CONDIsMeatMutated` adds food/water demand, wound and blood recovery, hygiene demand and CO2-poisoning immunity. | Not ordinary infection and not universal gas immunity. No standard antibiotic cure established. |
| Anti-meat viral treatment — `StatAntiMeat`, `DcAntiMeat00..03` | Treatment/service definitions exist, but the inspected menu entry uses `TNever`. | Do not advertise it as an accessible medical service merely because its dialogue and dose states are present. Plot-specific alternatives were not exhaustively traced. |
| Necrotized handprint — `DeathHandprint`, `DeathHandprintSoon` | Explicit fatal plot outcome and a 0.36 s nominal handoff helper. | No conventional field-medical reversal established. Preserve story boundaries unless an explicit mod design changes them. |

No model of radiation-induced cancer progression, hair-loss treatment, organ
transplantation, prosthetic installation or universal nanite resurrection was
established. Product flavor text and historical scar names are insufficient
evidence for those systems.
