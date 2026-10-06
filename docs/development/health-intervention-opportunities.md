# Health intervention opportunities for Phobos mods

Research and creative planning record, **6–7 October 2026**. Companion to the
[condition and direction tables](health-condition-directions.md). The owner asked
for research and creative possibilities; Claude will handle eventual implementation
details. All additions here are **agent proposals**, not owner-selected features,
implemented treatments or claims of gameplay readiness.

My recommendation is to make healthcare part of living aboard: the crew need clean
air, food, water, time, privacy, supplies and a tolerable journey. Medical machinery
can help, but the interesting choice is often whether to work, rest, spend resources
or return to a station. A dubious product can belong in that world too, provided
its actual effect and its seller's claim are recorded separately.

## Outcome language

| Outcome | Meaning for these proposals | Example |
| --- | --- | --- |
| Positive | The specified health, comfort, function or exposure axis improves | A clean dressing stops the treated wound's bleeding |
| Neutral | The target remains unchanged; useful convenience or personal ritual may still exist | A scar cover changes appearance without treating scar pain |
| Negative | A health/function axis worsens or the purchase creates another practical burden | Dirty emergency cloth staunches blood but adds local infection rate |
| Mixed | Positive on one named axis, neutral or negative on another | Pain is easier to tolerate while the injury remains; a recovery session consumes the day's stamina |

For a snake-oil product, **disease neutral / money negative** is different from
**comfort positive / disease neutral**, and both differ from **body negative**.
An advertised cure is not an effect. A fictional comforting ritual should not be
represented as research-proven placebo efficacy. False reassurance becomes a
practical harm when the player delays a needed action; that is a consequence of
the choice, not an invisible damage roll merely for buying a novelty.

New pharmacology, transmission, tissue repair, dose penalties and clinical
procedures would be authored gameplay. Their numbers need their own later design
and scientific source review. No doses, yields, treatment prices or dimensions are
selected here. Concept labels below are descriptions, not registered product names
or new saved identifiers. Halewright's existing naming rule applies if equipment
is eventually selected; scams need their own original sellers and provenance.

## Existing foundations

| Existing project | Recorded capability to build from | Extension boundary |
| --- | --- | --- |
| [Phobos Medical](../medical-player-guide.md) | Ward-3 recuperation for awake/asleep/unconscious patients; Vigil-2 wound/patient observations; crew apply native cloth/splints and replace dirty dressings | Current care does not administer pills, wash wounds, operate surgery or supply oxygen. The monitor observes only. Owner gameplay verification remains pending. |
| [Phobos Agriculture](../agriculture-player-guide.md) | Real crops and prepared meals; water/nutrient infrastructure | Meals can support native needs. Vitamins, immune cures or perfect nutritional recovery must not be inferred from a crop name. |
| [Phobos Manufacturing](../manufacturing-player-guide.md) | Refining, chemistry, tooling and finished components | A source of equipment/consumables only after a concrete process is designed; chemical purity does not automatically establish sterility or drug manufacture. |
| [Phobos Shipbreaker](../shipbreaker-reclamation.md) | Recovery, cutting and industrial work | Natural home for exposure prevention, casualty extraction and safe work decisions; not flight control. |
| [Phobos Auto Nav](../auto-nav-flight-profiles.md) | Flight and native safety constraints | Can host a proposed patient transport choice while retaining fuel, heat, wear and clearance. The medical mod does not take over flight. |
| [Phobos Banking](pda-apps-and-banking-research.md) | Native debts/ledger and the PDA banking direction | Medical bills or financing are new economic/story proposals. No automatic new loan or repossession consequence is implied. |
| [Phobos Spacer Stories](spacer-stories-vanilla-expansion.md) | Authored news, adverts, objectives, correspondence and small talk | Good home for sellers, complaints, scams and care stories. Text alone must not pretend to perform a treatment. |
| [Phobos Framework](phobos-framework.md) | Shared equipment/services, crew work, observations and infrastructure | Reuse concrete existing support when a feature needs it. This research does not propose another general disease framework. |

Third-party reuse must be refreshed before implementation. The dated
[mod survey](mod-extension-survey.md) is a starting point, not a current enabled-mod
certification or permission to copy other authors' work. This pass rechecked local
metadata for the following providers; their advertised features are not our own
gameplay observations:

- **End's [First Aid for NPCs](https://steamcommunity.com/sharedfiles/filedetails/?id=3786464764)**,
  installed metadata 0.5.0, advertises native medicine, dressings and splints for
  NPCs and NPC self-care. Compare it before designing duplicate medicine administration.
- **Osmod's [G-Immersion Pods](https://steamcommunity.com/sharedfiles/filedetails/?id=3783626623)**,
  installed metadata 0.8.3, advertises powered occupant protection during high-G
  flight. Evaluate it before adding another acceleration-protection device.
- **LOGUSS's [Common Sense Firefighting](https://steamcommunity.com/sharedfiles/filedetails/?id=3790484261)**,
  installed metadata 0.12.6, describes autonomous response to small reachable fires.
  Exposure/medical follow-up should complement that response.
- **Valtora's [Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)**
  describes water, sanitation, recycling and changes to hydration/hygiene. A selected
  wash/fluids feature must account for it rather than silently change its potable pool.

The Ship's Water description was fetched directly; the other three web-page fetches
failed, so their feature summaries rely on the authors' installed metadata. No
public reusable API, loaded-version agreement or Phobos compatibility is certified
by this provider check. Sanguifil is the native AntiHypo-V injector's product label,
not evidence of a separate emergency transfusion item.

## Air exposure and immediate care

Native targets and existing item effects come from **Blue Bottle Games**, through
the [survival](../health-reference.md#acute-conditions-and-survival-systems),
[wound](../health-reference.md#wounds-and-traumatic-injuries) and
[treatment](../health-treatments-and-drugs.md) records. Every new product/action below
is a proposal. The current native drugs remain useful; replacement is not necessary
just to give Phobos a branded version.

| Proposed intervention | Target and positive version | Neutral or negative version | Likely home and decision it creates |
| --- | --- | --- | --- |
| Casualty air kit | Check the patient's actual air source and provide finite safe breathing support: further exposure ↓, oxygen safety ↑ | An empty bottle or leaking mask is ineffective; oxygen without CO2 removal leaves another problem. New supply mechanics need validation. | Medical + Framework; carry rescue reserves or devote mass to profitable salvage |
| Post-exposure watch | Record exposure and later respiratory/blood trends: recognition ↑, disease itself ↔ | Cheap “all clear” sensor reports current room gas only; a patient may still have pneumonitis. False advertising hides that limitation. | Vigil-2 extension + Manufacturing/Shipbreaker; observe longer or leave for work |
| Clean wound-care station | Real washing, clean dressings and follow-up: local infection source ↓ and bleeding ↓ where treated | Dirty emergency dressing already offers blood-loss benefit with infection cost. A convenience-only dispenser does no care itself. | Extend Ward-3 care; spend clean supplies now or accept a known emergency compromise |
| Dressing service and reusable care kit | Keep genuine clean cloth/splints available and collect used supplies: care continuity ↑ | Repacked dirty cloth sold as sterile creates a harmful product; regeneration/sterility would need a real designed process, not a label | Medical + Manufacturing; replenish at port or support a finite onboard cleaning process |
| Thermal recovery pack | Controlled warming/cooling toward a safe body state: thermal status ↑ | Unpowered pack is neutral; a faulty heater, excessive cold application or warm luxury blanket in a hot room can worsen the selected axis | Medical + shared power/cooling; use battery reserves for comfort or essential machinery |
| Patient source report | Identify whether worsening blood/infection comes from continuing wounds, internal effects or reduced recovery: decisions ↑ | A cheap dashboard merely repeats icons; a deceptive premium “health score” hides uncertainty. Neither heals. | Extend Vigil-2; pay for meaningful interpretation rather than decorative precision |
| Occupational exposure locker | Properly provision protection, clean suit supplies and emergency equipment: exposure prevention ↑ | A filter for the wrong hazard has no protective benefit; a counterfeit cartridge may be worse than openly worn equipment | Shipbreaker/Manufacturing; regular preparation versus extra cargo and cost |
| Anti-radiation supply record | Track actual burden, native injection availability and remaining supplies: safe decisions ↑ | “Radiation-proof” sticker does nothing; repeatedly trying a guarded injection does not create more native benefit | Medical + Auto Nav route/operation information; use finite treatment stock or retreat |

These proposals do not require new room-gas species. Rooms and canisters retain
the project's approved native species. A diagnosis of CO should never be reduced
to the game's misleading hypercapnia display label.

## Food rest and rehabilitation

**NASA's Human Research Program** identifies nutrition, exercise and reduced
musculoskeletal fitness as spaceflight concerns in its
[Human Health Countermeasures risk overview](https://www.nasa.gov/humans-in-space/hhc-spaceflight-risks/)
(updated 27 February 2026). That supports the subject matter; it does not establish
Ostranauts crop nutrition, a treatment rate or a fictional meal's efficacy.

**ESA's E4D principal investigator Tobias Weber and co-investigator Jennifer
Struble** describe adaptable exercise and movement feedback in ESA's
[E4D technology-demonstrator account](https://www.esa.int/Science_Exploration/Human_and_Robotic_Exploration/Revolutionising_astronaut_fitness_for_deep_space_missions)
(16 January 2026). **Danish Aerospace Company** developed the equipment and
**Qinematic** supplied motion-capture technology. This supports a rehabilitation
and feedback concept; the article's demonstrator status does not prove every
countermeasure or a cure for chronic injury.

| Proposed intervention | Target and positive version | Neutral or negative version | Likely home and decision it creates |
| --- | --- | --- | --- |
| Recovery galley portions | Actual modest food/water servings support nutrition and hydration without overfilling: deficits ↑ | “Immune stew” is food, not an instant trait/disease cure. Oversized portions can worsen nausea/fullness. | Agriculture + Medical; spend ingredients on reliable meals while preserving appetite |
| Recovery drink | Real potable water and deliberately designed food value: hydration/nutrition ↑ | Tonic with no active benefit is disease ↔; caffeinated/alcoholic versions can trade comfort for sleep or recovery penalties | Agriculture; keep ordinary water useful and label additional effects honestly |
| Familiar meal and shared table | Food meets native needs; a separately designed social action can support contact/meaning: + on those axes | Forced meals may cost privacy/autonomy; flavour alone need not improve a disease. A solitary version can simply be preferred. | Agriculture + Stories; invite the crew or respect the patient's space |
| Quiet convalescent berth | Better sleep opportunity, privacy and a place away from work: sleep/comfort ↑ | Expensive cosmetic upholstery may be physiological ↔; constant wellness announcements can undermine privacy | Medical/habitation; trade floor space and power for a useful rest environment |
| Protected rest and light duties | Keep healing patient supplied and avoid demanding tasks: recovery opportunity/function ↑ | Overprotective orders can remove autonomy or valuable work; “clearance” based only on pain relief is unreliable | Medical + existing crew arrangements; accept less output today for recovery |
| Compact resistance rehabilitation | A sustained, measured programme targets atrophy/function: long-term ↑ if designed and tested | Immediate fatigue/water/food cost; excessive sessions could add an explicitly designed setback. Decorative exercise simulator is atrophy ↔. | Medical + Manufacturing; dedicate space, time and supplies to endurance |
| Joint and hand supports | Functional support reduces task burden or a chosen pain axis: function/comfort ↑; chronic trait may stay ↔ | Cheap brace is neutral; ill-fitting support could worsen a deliberately designed discomfort axis | Medical + Manufacturing; buy assistance or definitive native clinic removal |
| Mobility and carrying aid | Enable less demanding movement/carrying: function ↑ while injury remains ↔ | Bulk, maintenance and power costs; a failed powered aid creates a practical problem | Manufacturing + Medical; preserve independence versus equipment burden |
| Scar comfort and cosmetic cover | Optional local comfort treatment: symptom ↑; cosmetic cover: appearance changes, scar ↔ | A harsh home peel creates a new wound/burn consequence; a charm provides no scar cure | Medical + Stories; comfort, identity, appearance and definitive removal are separate choices |
| Patient transport choice | Deliberately gentler Auto Nav flight targets G exposure: exposure ↑ toward safety | More time/fuel or different manoeuvre feasibility; “luxury patient mode” that changes nothing is neutral fraud | Auto Nav + Medical; urgent arrival versus tolerable acceleration, without defeating native safety limits |
| Long-haul course organiser | Track AntiHypo-V, Ossifex or other native guarded treatment and provisioning: continuity ↑ | An organiser heals nothing; an advert promising instant micro-g reversal is false | Medical + Banking/Stories; invest in expensive long-term care rather than a magic restorative |

**NASA's medical-operations guidance** includes work/rest planning, private medical
communication and behavioural support in
[NASA-STD-3001 medical operations](https://www.nasa.gov/reference/6-0-medical-operations/).
These are operational requirements and rationale, not a clinical trial proving
the proposed berth, crew order or fictional support session. Our gameplay effects
remain authored choices.

## Social comfort and meaningful neutral effects

**NASA's Human Research Program** describes how isolation, sleep loss and workload
can impair performance in
[Isolation and Confinement](https://www.nasa.gov/hrp/hazard-isolation-and-confinement/).
It also identifies space gardening as a research question on that page. A Phobos
garden's psychological bonus would therefore be an authored game choice, not a
NASA finding that plants cure a native psychiatric condition.

The native `Greeted`, `GreetedByCrew` and `Kissed` effects provide a more concrete
game basis for temporary social benefits; see the
[definition trace](health-condition-directions.md#emotional-needs-and-temporary-cognition).

| Proposed intervention | Target and positive version | Neutral or negative version | Likely home and decision it creates |
| --- | --- | --- | --- |
| Crew check-in | Relationship-appropriate native contact plus a designed chance to notice unmet care: contact/security ↑ | Repetition can become pestering; invasive questioning need not help privacy/autonomy | Stories + Medical; care for a crewmate without making every conversation a medical exam |
| Private letters and familiar objects | A separately designed social/story response can support family/meaning; an ordinary keepsake may simply have personal value | No bodily effect; fake letters or predatory subscriptions cost money and trust | Stories; comfort need not be an efficiency buff to deserve a place aboard |
| Recovery garden visit | Selected activity can provide enjoyment/meaning, with explicit native need effects if chosen | Merely owning a plant changes no health state; noisy crowded “therapy tours” can cost privacy | Agriculture + Stories; home-making as a choice rather than compulsory therapy |
| Personal care ritual | Tea, washing, quiet music or a comfort object offers an explicitly selected comfort effect or no stat change | Vendor falsely advertises radiation/infection removal; condition remains | Stories/Agriculture/Medical; useful ritual and false cure promise can coexist |
| Discreet health records | Private patient information helps care choices; direct healing ↔ | Public ranking of crew health can shame a patient or pressure them back to work if that story consequence is designed | Medical + PDA direction; information has social costs as well as benefits |

These proposals preserve the distinction between a distressed emotional need, a
temporary drug/illness effect, a personality and a memory/story state. They do not
add blanket diagnoses of depression, PTSD or psychosis because a game flag sounds
similar to a real condition.

## New capabilities with substantial research remaining

These are worthwhile creative possibilities, not reasons to promise immediate
delivery. Each fills a real gap in the present catalogue; each also changes what
survival aboard can mean. No implementation architecture is selected here.

| Capability | Intended positive direction | Neutral or harmful alternatives | Main research question and likely home |
| --- | --- | --- | --- |
| Transfusion or blood support | Recover a specified pre-fatal blood-loss axis | Ineffective substitute is blood ↔; incompatible/contaminated support would be a separately researched harmful mechanic | Does native blood-loss state support this treatment without duplicating physiology? Medical; real compatibility/clinical claims need their own sources. |
| Internal-injury stabilization | Stop an identified continuing internal loss source before fatality | External dressing has source ↔; failed procedure might add a defined injury rather than remove the wrong source | Which exact native source can be treated, and when? Medical specialist machinery. |
| Resuscitation | Interrupt a selected pre-death arrest pathway, if native patient life state permits | A novelty defibrillator has arrest ↔; careless energy delivery could have a selected harmful outcome | Establish the intervention window and native death boundary first. Medical; not resurrection by flag deletion. |
| Specialist respiratory treatment | Improve a selected pneumonitis/chronic respiratory target | Mere perfume/oxygen branding leaves disease ↔; irritant vapour worsens exposure | Identify an actual effect and medical basis; inhalation/infection/source control are separate. Medical. |
| Diagnostic laboratory | Answer a concrete treatment question unavailable to current monitor | Expensive test provides no useful decision; false certainty can misdirect care | What meaningful native state can it measure? Medical + Manufacturing instruments; no invented test accuracy. |
| Corrective surgery and prosthetic assistance | Treat a selected injury/chronic target or provide function while target stays | Botched procedure leaves scarring/new harm; cosmetic operation may be physiologically neutral | Native scar names do not prove existing operations or missing limbs. Medical + Manufacturing, with separate consent/story and clinical research. |
| Chronic-care clinic at a remote station | Extend access to bounded native removal/services | High bills, legal refusal or limited service; not every provider treats brands | Preserve native service meaning and explicit limitations. Medical + Stories + Banking. |
| Robotic maintenance clinic | Restore selected artificial-system function | Human pills are inappropriate; counterfeit firmware/parts can harm | Artificial-body repair is a separate target contract. Manufacturing + Stories. |

Real medicine is the subject of a later source review if one of these is selected.
This record supplies no real-world procedure, dose or treatment recommendation.
The creative opportunity is not evidence that the game's engine already permits it.

## Snake oil risky commerce and negative interventions

All sellers/products in this table are **fictional concepts**. Their marketing
can be original, convincing and occasionally funny; the reference and player
information should still record actual contents and effects. Do not make the
reputable Halewright line's technical descriptions secretly false. Counterfeit
labels or competing dubious sellers are clearer story choices.

| Fictional concept | Seller's claim | Actual designed direction | Gameplay purpose and possible home |
| --- | --- | --- | --- |
| Gravity memory bracelet | Keeps the body remembering one G | Atrophy/hypovolemia ↔; money ↓; optional comfort ↑ only if deliberately authored | An everyday hope sold to long-haul crews; Stories, with no simulated gravity effect |
| Radiation harmoniser sticker | Aligns your cells against radiation | Radiation ↔; money ↓; delayed retreat can worsen exposure through actual player choice | A cheap fraud with falsifiable claims; Stories |
| Galactic liver cleanse | Clears jaundice, poison and a rough night | Native disease/poison ↔; actual drink may hydrate, or native alcohol may worsen the situation | Contrast ordinary fluid value with unsupported detox marketing; Agriculture/Stories |
| Premium recovery upholstery | Hospital-grade revitalisation through exclusive fabric | Cosmetic comfort only, or physiological ↔; genuine medical rest requires the real bed effect | Luxury pricing without fake nanite surgery; habitation/Stories |
| Scar eraser peel | Removes any scar, even legal brands | Appearance ↔ or a selected skin effect; deliberately corrosive variant adds a new wound/burn ↓ | Illicit beauty business and care consequence; Medical/Stories |
| Sterile surplus dressing | Reclaimed cloth as clean as a clinic | If actually dirty, native staunching ↑ / local infection ↓ toward harm | Honest emergency salvage versus dishonest repackaging; Medical/Manufacturing/Stories |
| All-clear wellness scan | Certifies you fit for duty instantly | No direct healing; misses unmeasured/latent targets by design | Fraud competes with useful Vigil-2 trend information; Stories/Medical |
| Double-shift stamina tonic | No sleep, no fatigue, no downtime | Native caffeine can improve tolerance temporarily while debt remains; stronger crash or toxicity would be a separately authored effect | Temptation when the mortgage is due; Agriculture/Banking/Stories |
| Miracle quit cigarette | Ends dependence without giving anything up | Nicotine re-exposure can relieve withdrawal while restarting its chain; dependence may stay | An appealing contradiction grounded in the existing nicotine path; Stories |
| Anonymous brand removal | No questions and no lasting marks | Comfort/cosmetics may be neutral to legal identity; real removal would deliberately override the native refusal boundary | Choice about law/history rather than an accidental medical bypass; Medical/Stories |
| Forever-young nutrient subscription | Reverses developmental radiation and aging | Food meets ordinary needs; aging/background ↔; recurring money burden if designed | Long-haul insecurity and predatory finance; Agriculture/Banking/Stories |
| Memory polishing clinic | Erases regret and repairs every memory | Story/comfort only unless a new bounded mechanic is selected; ordinary memories are not disease flags to delete | Creative noir healthcare fraud; Stories, with explicit preservation of native plot state |

A harmful product should have an intelligible mechanism and consequence: dirty
cloth, alcohol, corrosive material, a faulty heater or missed real care. If varied
product quality is later selected, record the possible outcomes and how the
player can discover them; do not make careful players reroll an opaque cure lottery.
This is a design requirement, not a selected probability system or balance table.

## Agent recommendation for the first creative groups

| Priority | Group | Why it is convincing | Evidence or decision still needed |
| --- | --- | --- | --- |
| First | Extend care supplies and meaningful Vigil-2 explanations | Directly builds on existing cloth/splint care and observed native distinctions | Decide what new information changes a player's decision; owner checks real treatment outcomes |
| First | Recovery galley and protected rest | Connects Agriculture to Medical without inventing another disease simulator | Choose actual food/water/comfort targets and finite inputs; no vitamin-cure claims |
| First | Gentle patient transport | Health becomes a real operational choice for Auto Nav | Decide target tolerance and journey trade-off; validate with fitted equipment and native safety rules |
| Next | Rehabilitation and assistive supports | Gives long voyages a reason to maintain function beyond buying pills | Native exercise progression, injury limits and chosen costs |
| Next | Scar comfort and dubious wellness commerce | Covers neutral/mixed/negative outcomes and adds setting depth | Decide actual effects separately from seller claims; respect legal-brand boundary |
| Later research group | Air rescue and targeted specialist care | Addresses genuine gaps in staying alive away from stations | Patient air-source mechanics, exact sources, clinical basis and treatment windows |

These priorities are agent judgment, not owner commitments. Transfusion, surgery,
resuscitation and artificial-body repair remain on the creative map even where the
research burden is larger. Their value should be judged against the concrete care
decision they create, not excluded merely because they are ambitious.

## Handoff to Claude after a concept is selected

Choose a condition row and a target axis, then decide what the treatment really
does, what it costs, what it leaves unchanged and what can go wrong. Preserve the
native condition/trait/episode distinctions and existing patient history. Any
selected feature's later plan must identify the saved structures it touches,
migration and old-record fixtures; this research creates no game records.

The material uncertainties are natural acquisition, the changed game assembly,
modded overrides, treatment ownership, accelerated time and actual pre-death
windows. Claude should trace only the mechanics needed for the chosen concept,
using the [condition record](health-condition-directions.md),
[native evidence limits](health-evidence-and-gaps.md) and the owning mod's standing
rules. The owner performs gameplay tests. A successful offline check is not proof
of cure efficacy or compatibility.

No code, equipment data, prices, artwork, installed packages or saves change in
this research round. Mod versions and publication status remain as they were.
