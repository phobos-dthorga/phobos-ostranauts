# Field and shipboard medical system

Design direction, 2026-09-20. The owner wants a multitude of health-based devices
and a broader medical project. The equipment and mechanics below are proposals
for discussion, not an approved feature list, implementation schedule or claims
about the current game. No first object has been selected.

Subsequent owner direction places slightly greater emphasis on handheld equipment,
with portable and installed options offering different strengths. Batteries and
their realistic management are an explicit part of the portable equipment scope.
Research now includes a [data evidence map](medical-research.md),
[runtime findings](medical-runtime-findings.md) and a
[shortlist with next experiments](medical-next-steps.md).

## The intended experience

A ship can grow from an improvised sickbay into a capable medical vessel.
Equipment changes what its crew can discover, which patients they can keep alive,
what treatment they can attempt and how well people recover afterwards. A more
capable infirmary has greater demands on space, electricity, supplies and crew.

A medic can also take useful equipment to an injured person on a derelict or
another ship. Packing tools, charged batteries and consumables is part of preparing
for an expedition. Access to a hospital-grade facility should expand what can be
done without making the field kit obsolete.

The central play loop is **assess, stabilise, treat, monitor, recover**. Devices
have different jobs within it. A monitor supplies evidence for a decision; a
pump delivers a chosen treatment; a steriliser prepares supplies; rehabilitation
addresses recovery after the immediate crisis. Each addition should change a
decision or enable a useful action.

Existing health information remains useful. New diagnostics earn their place
through additional supported information, trends, tests or treatment monitoring.
Do not create artificial value by silently concealing vanilla health information.

## Candidate equipment families

This is a menu of connected possibilities. Individual devices can be combined,
deferred or dropped when implementation and playtesting reveal overlap.

| Family | Candidate devices | Decisions they could support |
| --- | --- | --- |
| Assessment and monitoring | Bedside patient monitor; diagnostic examination couch; blood analyser; portable imaging scanner | Whether to keep working, observe, treat aboard or seek a better-equipped facility; distinguish symptoms from supported underlying causes. |
| Emergency care | Trauma trolley; oxygen therapy station; infusion/transfusion pump; assisted-ventilation unit | What buys time, which supplies to use and whether the ship can sustain support long enough to reach definitive care. |
| Treatment and medication | Wound-care station; controlled medication cabinet; pharmacy compounding bench; surgical/autodoc table | Choose an intervention, administer a dose, manage contraindications and decide whether available equipment and crew are adequate. |
| Clinical support | Autoclave; medical refrigerator; specimen incubator; clinical waste processor | Prepare reusable equipment, preserve treatments and samples, wait for useful results and manage contaminated material. |
| Isolation and recovery | Isolation berth with filtration; monitored recovery bed; rehabilitation frame; sleep-assessment berth | Separate hazards, observe a vulnerable patient, manage return to duty and investigate persistent impairment. |

These candidate devices imply very different amounts of new behaviour.
For example, an incubator needs a meaningful specimen/test system; imaging needs
something useful to reveal; a refrigerator needs an explicit storage or spoilage
rule. None should be added as empty scenery with an unrelated numerical bonus.

A smaller vessel should still have meaningful options. Portable equipment,
manual interventions and purchased consumables can support limited care without
requiring every ship to carry a hospital. Equipment tiers should change capability
and operating constraints, not merely increase a universal healing multiplier.

## Portable and installed capabilities

Treat portability as a design choice throughout the equipment families. Pair
devices where the differences are useful; this does not require two versions of
every item in the catalogue. Early work should give handheld tools real weight.

| Capability | Portable proposal | Installed counterpart proposal |
| --- | --- | --- |
| Initial assessment | Handheld triage scanner for a quick, limited assessment at the patient's location | Diagnostic station for a broader workup with more preparation and infrastructure |
| Observation | Portable or wearable monitor for selected observations, limited by battery life | Bedside monitor for sustained observation, trends and alerts while the patient remains connected |
| Sample testing | Cartridge analyser offering a small, rapid test panel | Laboratory analyser offering a broader panel and higher throughput, with reagents and sample handling |
| Injury care | Field treatment kit for immediate, limited interventions | Wound-care station with a wider range of supported procedures and supplies |
| Drug delivery | Injector or compact delivery device for a selected field intervention | Infusion station supporting controlled delivery over time and associated monitoring |
| Power supply | Replaceable packs and carried spares | Ship-powered charging dock; a later backup-power option for critical equipment |

The portable advantage is especially time to reach and assess a patient. It does
not imply every handheld test processes faster than its laboratory counterpart.
Installed equipment should earn better outcomes through additional supported
information, sustained support or treatment capability. Applying the same drug
should not magically make it stronger merely because a larger device administers
it. Dose, delivery, monitoring and the actual selected mechanics explain any
difference in outcome.

Diagnostic depth and precision need explicit definitions tied to available game
state. Limited test coverage is often more understandable than adding random
wrong readings. Proposed clinical signals require evidence or a deliberately
designed simulation before either form of device can report them.

## Batteries as physical equipment

Design baseline to investigate and implement with the first powered handheld:

- A physical rechargeable pack with capacity, remaining energy, mass and a
  defined compatibility rule. Prefer existing battery families where suitable;
  evaluate a shared medical pack only when there is a concrete need.
- Spare packs occupy inventory and retain their own charge when swapped,
  dropped, traded or saved. Removing and reinserting a pack must not reset it.
- Device load determines consumption. Scanning and continuous monitoring have
  different duty cycles; account for elapsed game time rather than frame count.
- Charging takes time and draws from a powered source. Partial charge is useful.
  Establish native energy units, transfer rate and losses before assigning values.
- Clearly expose low charge and interruption. An unfinished scan must not silently
  yield a completed result after the pack runs flat. Specify continuation/restart
  rules, and the consequences of lost support, for each device type.
- Display charge and, where meaningful, estimated remaining use based on the
  selected mode. Distinguish charge from physical condition or retained capacity.

Further realism candidates include capacity loss with age/use, temperature effects,
self-discharge, charging losses, different pack capacities and backup operation
for installed equipment. These are not a commitment to implement a full
battery-chemistry simulation. Select them for understandable effects on expedition
preparation and care. The subsequent code inspection found that native physical
damage already reduces effective battery capacity; do not duplicate that penalty.
Calendar ageing, chemistry-specific wear and thermal charging remain unverified.

An appealing equipment loop is to charge packs aboard, take a field kit on a
salvage run, decide when to spend energy on tests or sustained monitoring, swap
packs when needed, and return for replenishment and higher-capability care.

## Interacting health systems

The local investigation found native definitions for blood loss, oxygen deficit,
pain, infection, dehydration and recovery rates. It also found medication effects,
wound-related definitions, medical sleep and exercise. These provide starting
points; their full behaviour and extension boundaries still need investigation.

The following describe design goals and candidate extensions. The first research
pass has now found native painkiller stages, chronic-condition cycles and several
existing lasting conditions; distinguish extensions from those existing systems:

- **Symptoms and underlying problems:** relief of pain or distress does not itself
  repair the cause. The patient, observation and treatment views must keep those
  outcomes distinguishable.
- **Medication over time:** onset, duration and residual effects make timing
  matter. Native painkiller definitions already include an effect and a wearing-off
  stage. A chosen drug may provide a benefit while impairing work or interacting
  with another treatment. Tolerance, dependence and withdrawal are optional deeper
  systems to select deliberately, not assumptions about every medicine.
- **Constrained support:** treatment may hold a dangerous state in check while
  supplies and power last. Stopping support need not be equivalent to completing
  a cure. Equipment-specific interruption behaviour must be explicit.
- **Recovery with consequences:** candidates include residual weakness, scarring,
  recurring pain, reduced function and rehabilitation needs. Lasting effects
  should follow understandable causes and offer management or recovery choices
  where the design allows them. Chronic pain and scars already have definitions;
  investigate how acute injuries acquire them before designing new transitions.
- **Clinical uncertainty:** tests may take time and have defined limits. Avoid
  unexplained random failure and spurious precision. The player needs to know what
  was measured, when it was measured and what remains unknown.

Drug combinations, organ damage and specific treatment mechanisms require their
own design and research before implementation. No medical units, dose schedules,
clinical claims or new hidden patient variables are established by this document.

## An example care loop

Consider a salvager returning with an injury, blood loss and pain. Proposed play:

1. An examination identifies what the ship can currently assess. A patient
   monitor establishes observations and later shows whether the situation changes.
2. The operator chooses wound care or available supportive treatment. Treatment
   consumes the appropriate supplies and takes crew time.
3. Medication can make the patient more comfortable while introducing its own
   work-related tradeoff. Being comfortable is not proof that the injury is fixed.
4. Follow-up observations inform whether to continue treatment, change it or head
   for station care. A report of recovery must reflect actual patient behaviour.
5. A recovery bed and, later, rehabilitation equipment support return to duty.
   The design can make premature strenuous work affect recovery, if that mechanic
   is selected and the player can understand the risk.

This connects several pieces of equipment through one patient's course. A failed
power circuit, depleted supplies or a decision to resume salvage can then matter
because of the care being delivered, rather than because of arbitrary penalties.

## A first connected demonstration to consider

A **field assessment and recovery** scenario is a candidate target: portable
assessment at the patient's location, a charged pack and its recharging path, one
meaningful treatment, and follow-up aboard. An installed diagnostic or monitoring
counterpart could later demonstrate the benefit of bringing the patient back.
First assess which existing medical supplies, chargers and medical bed can fill
support roles without duplicating them.

This is a proposed first care loop, not a selected milestone or fixed equipment
count. One functioning device remains the first technical checkpoint. Choose it
after investigating patient identity, wound access, battery consumption, available
treatments and how a treatment result can be observed. The checkpoint should
contribute directly to the connected demonstration.

Detailed pharmacology, surgery, infection testing and long-term rehabilitation
can each become another useful group once the relevant patient behaviour exists.
There is no fixed order or workload estimate for those groups yet.

## Implementation boundaries

- Keep the whole design understandable, but implement one coherent group at a
  time. A device catalogue does not justify building a general medical framework.
- Reuse the game's authoritative health state where it represents the intended
  mechanic. Establish a clear owner before introducing any new persistent state;
  avoid two independent systems both simulating the same blood loss or infection.
- Native definitions are candidates for furniture, interactions and existing
  effects. New treatment behaviour, longitudinal observations and complex patient
  relationships may require C#. Confirm each boundary against the installed game.
- Gameplay services own treatment decisions and mutations; panels show state and
  delegate actions. Introduce shared services when actual devices establish a
  shared need. Monitoring must not silently heal or alter the patient it observes.
- Ship's Water is loaded in the inspected installation and describes changes to
  hydration, hygiene and infection recovery. Account for it before extending
  those areas; integration must not make it a hidden mandatory dependency.
- Every implemented device needs proportionate checks for relevant consumption,
  power loss, interruption, time acceleration and save/reload. Test autonomous
  crew use and unloaded-ship effects separately when those become features.
- Use a consistent family of original placeholder sprites. Develop the final
  visual language and AI-assisted asset variants once object roles and footprints
  are stable, recording provenance as required by the contributor guide.

The repository stays private. This document changes the design frame, not the
installed game, save files, runtime dependencies or publication status.
