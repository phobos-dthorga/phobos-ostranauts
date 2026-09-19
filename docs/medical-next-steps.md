# From research to a useful medical experiment

Decision brief, 2026-09-20. No first feature has been selected. The
[vision](medical-system-vision.md), [data evidence](medical-research.md) and
[runtime findings](medical-runtime-findings.md) support a bounded experiment;
they do not justify building the whole catalogue yet.

## Is this worth pursuing?

**Yes, provisionally.** Wounds, treatment effects, condition stages and physical
battery systems provide concrete foundations. The remaining risk is useful play:
a scanner that repeats the health screen would add equipment management without
adding a worthwhile decision. That is the next uncertainty to resolve.

The owner explicitly welcomes candid recommendations to stop or change direction
for **any Ostranauts mod idea**, including medical, industrial and recreational
work. Existing effort is not a reason to keep investing. Apply these checks to
each proposed capability:

| Decision | Evidence that warrants it |
| --- | --- |
| Continue | The feature enables a meaningful action or decision, uses identifiable state and has a bounded implementation/test path. |
| Simplify or redirect | The desired experience is useful, but a narrower device or existing native service can deliver it with much less machinery. |
| Set aside | It duplicates available information/actions; its required physiology does not exist and adding it exceeds the intended scope; or unavoidable engine hooks/persistence problems imply disproportionate maintenance. |

A rejected scanner does not invalidate medical treatment devices. A rejected
medical direction does not invalidate industrial, life-support or crew-comfort
experiments. State the actual obstacle and a concrete alternative when it arises.

## Portable and installed equipment should earn different roles

Portable does not inherently mean inaccurate. Abbott describes its
[i-STAT 1](https://www.globalpointofcare.abbott/us/en/product-details/apoc/i-stat-system-us.html)
as a battery-powered handheld analyser using single-use cartridges with different
test combinations. This is useful design inspiration: portable tests can give
credible answers within a supported scope, with consumables and operating limits.
It is not evidence that Ostranauts simulates those real-world measurements.

Our proposed installed advantages are a broader supported workup, sustained
observations, more simultaneous patients or procedures, better supply access and
more capable treatment. Those capabilities can improve outcomes. A diagnostic
machine should not heal merely because it is larger, nor should a handheld
randomly lie to justify the installed version. Device pairs remain optional.

Keep the original fictional artwork and names independent of reference products.
Specific tests, clinical units and treatment mechanisms need separate research
when selected; no real-world dosing or device operating parameters are adopted.

## First-prototype shortlist

These are recommendations for comparison, not approved features or an implied
implementation order for the whole project.

| Candidate | Useful outcome to prove | Native starting points | Main reason to reject or defer |
| --- | --- | --- | --- |
| Battery-powered field assessment tool | At the patient, obtain a limited, timestamped observation that changes whether to treat, monitor or evacuate. | Handheld presentation, tool charge profiles, patient/wound reads. | Existing interface already provides the same useful answer; patient selection/reporting costs exceed the benefit. |
| Portable follow-up monitor | Show a supported change across observations, then eventually alert to deterioration while powered. | Patient state and native stored energy/continuous power paths. | Sampling, persistence or background behaviour cannot be made trustworthy with a small extension. This is broader than a single scan. |
| Field wound-care device with a later installed station | Make a justified native wound intervention easier or enable a genuinely additional procedure, using resources and crew time. | Wound slots, dressings, splints, cleansing and existing effects. | Merely repackages the same dressing action, or implies unsupported procedures and physiology. Powered operation needs a reason; a kit need not use electricity. |

Recommendation: first compare the existing health interface with the information
needed for **field assessment and follow-up**. If the information gap is real,
a single powered observation is a promising technical checkpoint. Otherwise
assess a treatment/support gap before choosing a scanner for its appearance.
Sample laboratories, advanced imaging, transfusion and detailed pharmacology
remain later candidates because their meaningful patient models are not established.

## Next round: settle usefulness and native reliability

Work on a separate test save, keeping the running play session and real saves
untouched. The current modded environment must be recorded. A core-only comparison
requires a separately controlled configuration; do not silently disable the
owner's active mods to obtain it.

1. **Inventory observable information.** Use a patient with one known wound and
   compare health screen, wound view, tooltips and log information. Record what
   is visible before/after examination and treatment, including another patient.
   Separate missing information from information merely presented inconveniently.
2. **Choose one player decision.** Write one sentence explaining what the proposed
   tool lets the player decide that the current workflow does not. If none is
   convincing, drop that candidate before implementing it.
3. **Measure the native portable-power loop.** Use an existing compatible tool,
   pack and charger. Record actual energy, effective capacity and elapsed game
   time through use, swapping and partial charging. Include a damaged pack.
4. **Observe one native care course.** Compare untreated and treated copies of
   the same test scenario. Record wound and patient effects separately, and
   test medication stages/reload if relevant to the chosen decision.
5. **Return a concrete feature choice.** Present the best candidate with its
   player action, costs, supported result and implementation boundary. Keep the
   owner involved in this meaningful choice; do not build the catalogue first.

The output is a short observation record and a selected or rejected capability,
not an estimated number of devices per round. Research can end once the remaining
unknowns are precisely what a small prototype will test.

## If the powered observation candidate is chosen

Proposed scope: one `Phobos` handheld definition, one observation interaction,
an existing compatible physical battery/charger family and original placeholder
artwork. Start with self-use to establish identity and resource handling, then
prove another-patient use before calling it a field medic's tool. A handheld
is an acceptable first object; a furniture-only milestone would miss the priority.

The result contains patient identity, game timestamp, supported findings and
limitations. Use engine-defined descriptions or clearly named game quantities.
An unavailable observation is labelled unavailable. It never modifies health and
never presents a one-off reading as a trend. A debug log can establish the path;
it is not the finished player interface.

For this first discrete scan, a completion charge is the simplest proposed policy:
check resources when starting and again before completion; pay exactly once for
one completed observation. Cancellation produces no completed report. Account for
how the native tool contract charges before adding a custom deduction. If actual
scan-time consumption is selected instead, define partial consumption and restart
behaviour explicitly before implementation. Continuous monitoring is a separate
mode with elapsed-time consumption, not repeated free reports.

Use a small gameplay service only if code is needed. It resolves the patient,
validates the action and performs the chosen transaction. UI displays its result.
Scope any patch to the new identifiers. Do not add a generic physiology engine,
universal device base class, shared medical battery standard or history database
for an unproven first operation.

## Checks that decide whether the prototype works

These are pending acceptance checks, not results. Run relevant cases for the
selected capability; do not turn unrelated later features into prerequisites.

| Case | Evidence to record / required outcome |
| --- | --- |
| Correct patient | Self and another patient, selected wound and dragged patient where supported. No result or effect is assigned to the tool, wrong person or unrelated parent. Unsupported robot use is rejected clearly. |
| Healthy versus injured | Supported findings agree with the native state and defined scope. Missing data is not interpreted as health. The result supports the chosen player decision. |
| Discrete energy | Starting energy, completion count and ending energy match the defined cost. Missing, empty, insufficient and exactly sufficient packs give explicit outcomes. No double deduction. |
| Cancellation / resource change | Cancel early/late, remove the pack, move the target or consume the available charge before completion. No completed report without its required completed operation. |
| Pack identity | Swap, drop and recover two differently charged packs; save/reload with partial charge. Each pack retains its own state without free refills. |
| Charger behaviour | One and multiple packs, healthy/damaged capacity, partial charge, source depletion and power disconnection. Measure pack gains and source loss; distinguish charger operation from actual transferred energy. |
| Time | Compare equal game-time windows at normal and accelerated speed, then relevant unloaded/catch-up cases. Investigate charging intervals around 1,800 seconds. Set tolerances from measured update behaviour, not an invented precision guarantee. |
| Reload | Save mid-interaction and after completion. Verify target, remaining work, tool selection, energy and result count. Separately compare a partly elapsed medication stage before/after reload. |
| Passive observation | Opening/refreshing the result UI causes no treatment or extra billing. Stale results retain their timestamp and cannot masquerade as new measurements. |
| Installed counterpart, when added | Disconnect supply while observing/supporting the patient. Alerts and loss of support follow an explicit design; the device does not quietly continue its powered benefit. |

For each run record game/plugin versions, configuration, scenario, starting state,
action, elapsed game time, observed result and pass/fail/unresolved. Keep saves and
raw logs ignored; commit concise factual summaries. Implementation tests should
cover actual rules such as patient identity and exactly-once completion, rather
than merely restating static item definitions.

## Existing mods and compatibility

The installed Ship's Water definitions alter hydration and infection-recovery
inputs. Its [author's Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
is a scope reference; local definitions and controlled gameplay remain necessary
to establish the combined result. It is not a mandatory dependency for this project.

[RoomEffects' author documentation](https://github.com/Kriil/ostranauts/blob/main/RoomEffects/README.md)
describes room effects around sleep, exercise and social activity. Its medical-bed
healing bonus is listed under planned updates, so it must not be treated as an
implemented conflict. Recheck the actual version if that mod enters testing.

Historical mentions of a mod called Medical Miracles are a discovery lead only:
this research did not verify a current author-maintained release, source or
compatibility. Search results are not a complete mod inventory. No additional
game mod or dependency was installed during this investigation.

## Subsequent rounds

Expand only after the first device demonstrates value. A useful next round might
connect field observations to one existing treatment and follow-up; another might
prove an installed counterpart's actual advantage. A later round can investigate
one missing physiological mechanism required by a chosen treatment. Scope rounds
around a question or a playable care loop, rather than promising a whole device
category each time. Reconsider the project's direction whenever the evidence changes.
