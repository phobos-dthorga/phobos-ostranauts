# Health research: evidence, coverage and unresolved behavior

Companion to the [health reference](health-reference.md),
[chronic-condition catalogue](health-chronic-and-traits.md) and
[treatment/drug reference](health-treatments-and-drugs.md).

## Baseline and method

Read-only research on **2026-09-20**, against installed core **1.0.1.4**.
The current `Player.log` release line was rechecked. The game assembly hash
matches the earlier [runtime investigation](medical-runtime-findings.md):

`1dc1858a8edc514ec089f2fd7c55932c7f9b62b0a96201c15e2b2720122a03a7`

The analysis indexed the immediate core definition folders, searched references
across the data tree, followed selected condition → effect → trigger → interaction
chains, and reread relevant locally decompiled `Condition`, `CondOwner` and
`Wound` methods. Original data, local parsing helpers and decompiled code remain
outside tracked documentation. No original game files, assemblies, assets or
personal paths are included here.

| Indexed category | Unique named entries |
| --- | ---: |
| Full conditions, including nonmedical/plot families | 2,417 |
| Condition rules | 150 |
| Condition triggers | 5,615 |
| Effect/loot definitions | 9,239 |
| Tickers | 49 |
| Interactions | 4,131 |
| Condition-owning objects | 1,120 |
| Wound definitions, including artificial bodies | 20 |
| Slot-effect definitions | 768 |

These are **search/index scope counts**, not a claim that every entry was manually
traced or represents a health condition. The 1,416 entries in the main
`conditions.json` were screened for relevant named families; the other condition
files and the seven-column compact simple-condition table were also searched.
Generated TSV output, translations and nonmedical plot flags were not treated as
additional independent diseases. Indexing is not proof of runtime load order.

The resulting catalogue explicitly covers survival deficits and exposures,
wounds, named acute disease stages, radiation families, symptoms/incapacitation,
15 chronic ailments, 27 scar families, health-relevant traits, medical items and
services, nicotine/caffeine/alcohol/cannabis, and identified plot/robot boundaries.
It deliberately leaves naturally acquiring some defined diseases, full engine
physiology and actual rescue efficacy as open questions.

The native format is not uniformly strict JSON: comments, compact seven-field
records and a control character in encounter interaction text needed tolerant
local parsing. The indexed folders parsed without remaining errors after handling
those format features. Numeric-looking payload strings were kept literal, not
silently corrected; several appear malformed.

No player save was read or modified. No gameplay experiment, game update,
mod-load-order change, plugin installation or treatment implementation occurred.

## Local source map

All paths in this table are relative to
`Ostranauts_Data/StreamingAssets/data/`. Identifiers are the lookup keys for
reproducing findings after an update; line numbers are intentionally omitted
because game updates reformat and reorder the definitions.

| Source | Specific evidence and lookup examples |
| --- | --- |
| `conditions/conditions.json` | Condition names, duration, caps, effects, transitions, fatal/KO flags: `Cholera1`, `Immunostimulant`, `HRP`, `PainkillerMinor1`, `TraitChronicAsthma`, `Nicotine1`, `CannabisIndica3`, `DcBlood04` |
| `conditions_simple/conditions_simple.json` | Seven-field compact records, including immunity flags such as `IsImmuneGastroenteritis2` |
| `condrules/condrules.json` | Severity and threshold mappings: `DcBlood`, `DcOxygen`, `DcInfection`, `DcBodyTemp`, `DcGasPressureHuman`, `DcImmunostimulant`, `DcAntiHypoVDose` |
| `condtrigs/condtrigs.json` | Stage probabilities, requirements and blockers: `TUpPneumonia2`, `TUpHepatitis3`, `TCanAntiRadInject`, `TCanAntiHypoVInject`, `TDnSmokerChance` |
| `loot/loot.json` | Actual field modifications: `CONDImmunostimulantCuresPer`, `CONDDrugAntiRadPrescAllowDirectThem`, `CONDNanoFirstAidPer`, `CONDWoundWaterCleanSoap`, `CONDChronicAsthmaRemove` |
| `tickers/tickers.json` | Repeating changes and coefficients: `Blood`, `BloodHeal`, `Infection`, `InfectionHeal`, `RadHeal`, `PoisonHeal`, `BandageCleanDegrade`, `PainHeal` |
| `interactions/interactions.json` | Medicine consumption roles/guards and exercise/rest actions: `SeekDrugAntiRadPrescDirectAllowDone`, `SeekDrugAntibioticPrescDirectAllow`, `ACTExcerciseTreadmillDo` |
| `interactions/interactions_encounters.json` | Clinic menus, treatment/purchase gates and payloads: `ENCOKLGMedServices`, `ENCMedServiceAsthma`, `ENCMedServiceScarBurns`, `ENCMedServiceAntiHypoV` |
| `interactions/interactions_events_ffwd.json` | Alternative medical-sleep processing that references `CONDTick1HourSleepMedicalPhysio` |
| `interactions/interactions_slots.json` | Patient/wound slot effects, application and removal paths |
| `condowners/condowners.json` | `Crew01` starting recovery fields, rules, tickers and respiratory update command; medical bed, pills, injectors and other items |
| `condowners/condowners_wounds.json` | Wound components, ownership and body-part configuration |
| `wounds/wounds.json` | Twenty native wound definitions and their effect mappings |
| `slots/slots_wounds.json`, `slot_effects/slot_effects_wounds.json` | Wound/treatment slots and body-part effects |
| `loot/loot_wounds.json` | Head/chest/abdominal/limb consequences and removal entries; internal bleeding, cardiac arrest and fatal trauma links |
| `conditions/conditions_plots.json`, `loot/loot_plots.json` | Mutation, anti-meat and fatal handprint/story-related effects |

For code evidence, the source is locally installed
`Ostranauts_Data/Managed/Assembly-CSharp.dll`, inspected using the existing
ILSpyCmd 11.0.0.9375 tooling. Relevant methods:

- `Condition` constructor: zero-duration semantics and default copying.
- `Condition.Update`: elapsed time in hours, expiration and remaining-time
  propagation to next-stage triggers.
- `Condition.AddAmount`: counts, caps and timer refresh.
- `CondOwner.GetCondAmount`, `EndTurn`, `CatchUp`: state access and time processing.
- `Wound.Run`: microgravity, splinting, cleaning, local healing and vital injury.
- `Wound.ApplyEffectsParent`: direct wound blood loss, infection and pain transfer.
- `Wound.CatchUp`: empty in the inspected implementation, warranting time-mode tests.

## Data fingerprints

SHA-256 fingerprints identify this evidence snapshot without redistributing
source files. A matching game version string alone is a weaker check.

| Relative source | SHA-256 |
| --- | --- |
| `conditions/conditions.json` | `b3b50145e1200a418bc4b6d9eab467a12afd7e07105ea089659e83de48d3de83` |
| `condrules/condrules.json` | `2e380066e70de7c66638fce055f064f6e25457387a99317df7c74467363b772d` |
| `condtrigs/condtrigs.json` | `a9d8c9a6bbc49779419278f5a05371c53f22f7b6e2681d2954cd67483922abcd` |
| `loot/loot.json` | `0bb7be2be6945c573ac195285e81ace4bb4aacef22272eb0bcf98d96c2e10d5e` |
| `loot/loot_wounds.json` | `a3ababf82ccefc8aeaf08ab4343722b1e4c637117937e49f001cc391fa499791` |
| `interactions/interactions.json` | `c451bd6065cf961bc5fe9ea2bde3530153cd3a569dcfc7de733b49094cf288a3` |
| `interactions/interactions_encounters.json` | `9521fb4984b1f03b04344e855070534b0deab5796bd2d8a5056108512ed6267a` |
| `tickers/tickers.json` | `a9de3c5ce1e7649900e04e8484086862fefa5e7fe43db2a928793b3dc50ac15d` |

## External references and source quality

The current installed definitions and selected code are the primary evidence for
mechanics in this catalogue. External research was used to identify disputed
claims and version history, not to replace that evidence.

- **Developer announcement, 0.15.0.33:** confirms a migration intended to remove
  additional emphysema/pneumonitis variants from older saves. This is direct
  evidence that historical player reports may describe a patched state; it does
  not prove the current efficacy of any medicine.
  [Official Steam announcement feed containing the hotfix](https://store.steampowered.com/news/posts/?enddate=1778797463&feed=steam_community_announcements).
- **Developer 0.15.0.32 event:** relevant preceding history. The direct event
  request timed out in this pass, so it is a follow-up pointer rather than a
  separately fetched source used to establish new mechanics.
  [Unwelcome NPCs, Stuck Pilots, and Other Fixes](https://steamcommunity.com/ogg/1022980/announcements/detail/657106446377288203).
- **Community reference pointers:**
  [First Aid](https://ostranauts.wiki.gg/wiki/First_Aid),
  [Health and Safety](https://ostranauts.wiki.gg/wiki/Health_and_Safety),
  [Gravusine](https://ostranauts.wiki.gg/wiki/Gravusine),
  [ChymAdd](https://ostranauts.wiki.gg/wiki/ChymAdd).
  Search-index excerpts were available, but direct fetches of the two broad
  health pages returned 403. They are not represented here as fully read or
  independently verified sources. In particular, the indexed Gravusine stacking
  claim conflicts with the inspected use guard.

Several similarly named unofficial guide domains surfaced with unsupported
mechanics and contradictory treatment claims. Their asserted medical facts were
not adopted. No wiki prose or data tables are copied into this repository; the
catalogue is an original analysis of the local evidence.

## Apparent anomalies and unresolved claims

| Finding | Consequence / required caution |
| --- | --- |
| `TUpPneumonia2` and `TUpPneumonia3` target `Pneumonia`, not numbered stages; unnumbered condition not found in inspected full/simple condition definitions | Possible dead/broken transition path. Do not claim its intended numbered sequence actually executes. Check runtime resolution and natural injury acquisition in a test save. |
| CO poisoning's friendly labels and descriptions say hypercapnia | Diagnose using the underlying CO/CO2 statistics and source, not display text alone. |
| `CONDDcPain04` contains `-StatPiloting=1.0x0.0.25` | Malformed numeric payload; parsing/fallback behavior was not investigated. Do not publish a precise piloting penalty for that entry. |
| Coffee/weak-liquor wound payloads contain `StatInfectionRate=1.0x0.0.05` | Same malformed-value concern. Disinfection and tissue-damage entries can be described separately without silently repairing the infection value. |
| `CONDDcH2SO4Poisoning2Per` contains `Pneumonitis2FAF=1.1x1` | Probability-like value exceeds one; do not report it as a medically meaningful 110% probability. Runtime interpretation needs checking. |
| `DrunkAlcoholPoisoning` has death wording but no explicit `bFatal=true` | Its trigger requires fatal general poisoning. Wording alone is not a reliable fatal-state detector. |
| `AntiRadStim` has an empty active payload | The verified burden reduction is on consumption, not a continuous 24 h removal payload. Additional uninspected runtime behavior remains possible. |
| AntiHypo-V completion increments count after the fifth-dose band | The six-count terminal band does not establish a six-injection course. Timer/rule ordering needs verification. |
| Clinic scar/ailment entries exist | Supports an available native design path, not a tested price, successful payment or guaranteed elimination of every leftover symptom. |
| Thief/piracy brand options have “remove scar” titles but refusal text and no removal payload | Only 25 of the 27 scar menu entries provide a linked removal effect. The two brands must not be listed as routinely curable. |
| `ENCOKLGMedServiceAntiMeat` is guarded by `TNever` | Data existence is not player availability. |
| Recurrent smoker payloads reference their own status flags | Do not predict exact cessation time from trigger chances alone; verify removal and recurrence behavior. |
| Long ARS timers and timed downstream symptoms coexist with radiation threshold rules | Lower radiation burden is not automatically proof all secondary stages disappear. |
| Cholera, hepatitis and CRS initial definitions exist without a fully traced natural acquisition path | Catalogue as defined systems; avoid presenting speculative contamination/exposure routes as observed gameplay. |

## Installed-mod boundary

The [environment record](modding-notes.md#environment-recheck-2026-09-20) identifies
Ship's Water **0.16.1** and other configured mods, plus BepInEx **5.4.23.5**.
Ship's Water overrides `CONDTickHydration` and `CONDDcHygiene01`; the latter adds
infection recovery absent from the inspected core entry. Runtime patches may
affect more than JSON overrides.

Consequently, the catalogue's core numbers and the owner's active-game behavior
must not be silently treated as identical. Record the load order and loaded
plugin versions alongside each future experiment. No mod was disabled for this
research.

## Verification priorities for a separate test save

Use disposable test characters and an isolated test save. No tests below have
been executed, and this list is not authorization to edit the player's real save.
Observe game time, exact patient/wound state, intervention start/end, resources,
power and both visible symptoms and underlying statistics.

| Priority experiment | Question it resolves |
| --- | --- |
| One external bleeding wound: no care, clean dressing, dirty dressing, cleaning; inspect removal | Does staunching stop the correct wound contribution, when do dressings degrade, and does the net blood/infection trend improve? |
| One fracture: supported/unsupported, with and without microgravity | Does native stabilization improve actual recovery as inferred from `Wound.Run`? |
| Named disease stage before/after immunostimulant | Does the explicit cure list behave as expected, including early-versus-late hepatitis and pre-existing symptoms? |
| Pneumonia source and numbered stage transitions | Is the unnumbered target ignored, substituted or repaired elsewhere? |
| Short toxic-gas exposure followed by clean air | Do exposure statistics clear while pneumonitis persists, and what actually cancels a near-fatal countdown? |
| Radiation burden before/after one AntiRad injection | Confirm immediate decrement, redose guard and separate HRP/ARS/CRS behavior; do not start with a lethal exposure. |
| Analgesic redosing during active/fading stages | Refresh, coexistence, expiry and interactions between OTC/prescription effects |
| Nanite care and medical sleep, then power loss | Condition ownership, resource/power checks and whether actual recovery beats the ongoing illness |
| One chronic/scar removal service | Currency transaction, trait removal, pending recurrence, current episode and later save/reload |
| AntiHypo-V sequential course | Fifth-dose completion, count-six meaning, legal/location gates and persistence |
| Nicotine acquisition and abstinence with recorded state | Recurrence and cessation behavior, including self-referential status removal |

For the first gameplay slice chosen, repeat relevant measurements at normal speed,
accelerated time, medical fast-forward, unloaded/away time, and across save/reload.
Do not run an exhaustive combinatorial suite before there is a concrete feature.
Record game version, plugin versions and source fingerprints if changed.

## Maintainer conclusions

Documentation validation completed in this round: 73 targeted source assertions
passed for medication timers, dose guards, disease-cure scope, the 42 ailment/scar
menu entries and their 40 actual removal links/two refusals. Relative links and
section anchors were checked against files included in the repository; a scan
found no personal machine paths or table-row/whitespace issues in the new pages.
These are research/documentation checks, not gameplay tests.

This is a broad, versioned **research baseline**, not a promise that every listed
condition is reachable or every inferred intervention works. The evidence is
already sufficient to choose a narrow medical feature and its acceptance checks.
The most defensible early value is explaining causes, trends and treatment limits
around native care. Creating a universal new disease framework or resurrection
system would be a much larger and less justified first step.
