# Medical and power runtime findings

Research date: 2026-09-20. Read with the [native data evidence map](medical-research.md)
and [next experiments](medical-next-steps.md). This is an explanation of selected
installed code paths, not an in-game test or a compatibility guarantee.

## Evidence and reproducibility

- The running session reports Ostranauts **1.0.1.4**. See the
  [environment record](modding-notes.md#environment-recheck-2026-09-20) for plugins
  and configured native mods. Unity's executable version is not the game version.
- Inspected assembly: `Ostranauts_Data/Managed/Assembly-CSharp.dll`.
  SHA-256: `1DC1858A8EDC514EC089F2FD7C55932C7F9B62B0A96201C15E2B2720122A03A7`.
- Selected types were inspected using Mono.Cecil metadata and ILSpyCmd
  **11.0.0.9375**, installed under ignored `.local/tools/`. Decompiled material
  stays under ignored `.local/research/engine/`; it is not project source.
- Findings cite type/member names rather than unstable decompiler line numbers.
  Recheck them against the installed assembly after a game update. Installed
  runtime patches can change behaviour beyond what this assembly shows.
- No player save was inspected or changed. No game files or installed plugins
  were changed, and no medical device was implemented or gameplay-tested.

Evidence levels throughout this research are **data** (definitions/schema),
**code** (selected local implementation), **external** (linked primary source),
and **proposal** (our intended design). There is currently no project gameplay
verification of these medical or battery paths.

## Patients, wounds and observations

| Code evidence | Consequence for a device |
| --- | --- |
| `CondOwner.GetCondAmount(string)` reads a condition value; the overload also resolves threshold modifiers. Missing conditions return zero. | Check patient type and whether a measurement is supported before interpreting zero. Missing support must not be displayed as a healthy result. |
| `CondOwner.GetAllWounds()` collects wound components from its object hierarchy. `RootParent(string)` walks parents, excludes the starting object and contains special handling for dragged crew relationships. | Resolve an explicitly identified patient first, then that patient's wounds. Test self-use, another patient and dragging; blindly taking the root of any selected object is insufficient. |
| `Wound.Run(double)` reads patient recovery state and wound-local injury, bleeding, infection, pain and disinfection state. `ApplyEffectsParent` adds blood loss directly to the patient for an unstaunched bleeding wound. | Patient `StatBloodRate` is not the complete bleeding input. A monitor must not simulate these effects again. Prefer observed changes over time to an unverified formula predicting net blood loss. |
| `Wound.Run` consumes `StatDisinfectAmount`, limits its influence and clears it after use. Dressings influence staunching through the native condition path. | Wound cleaning is an action with downstream effects, not simply a permanent cleanliness score. Follow native ownership for a treatment rather than adding a duplicate patient flag. |

The data names `StatBlood` as blood **lost**, and native recovery also changes it.
Do not label it blood remaining, millilitres or a directly measured clinical value.
An instantaneous rate, current severity and a sampled trend answer different
questions. A sampled trend requires two observations with game timestamps; a
single scan cannot establish it. Observation history does not yet have a selected
storage mechanism.

## Medication and time

`Condition.Update(float, CondOwner)` converts elapsed seconds to hours, expires
the current condition and passes excess elapsed time into triggered next stages.
`Condition.AddAmount` clamps the count and resets age when `bResetTimer` is set,
including additions that reach the maximum count. This supports the native
painkiller stages found in data, but does not establish a dose/concentration model.

Repeat-use tests should distinguish refreshing the first stage, using medication
during its wearing-off stage and interactions between different medicines.
Do not add tolerance, withdrawal or organ injury merely because native conditions
can express a timer. Each would need its own justified gameplay design.

`CondOwner.EndTurn` and `CatchUp` advance timed conditions using game time.
Tickers also distinguish `bTickWhileAway`. `Wound.CatchUp` itself is empty, while
the ordinary wound update applies elapsed-time effects. These are reasons to
compare active, accelerated and unloaded behaviour; they do not prove that wounds
stop progressing off-screen or that all time paths produce equivalent results.

## Native battery model

The native definitions explicitly use kWh for `StatPower` and `StatPowerMax`.
The welder pack's nominal `0.0576` kWh is **57.6 Wh**, a reference example rather
than a selected medical capacity.

| Code evidence | Design implication |
| --- | --- |
| `Powered.PowerStoredMax` multiplies nominal capacity by `CondOwner.GetDamageState()`. With positive `StatDamageMax`, that factor is `1 - StatDamage / StatDamageMax`. | Native physical damage already reduces effective capacity. Avoid applying an additional capacity penalty for the same damage. This is not evidence of chemistry-specific ageing or calendar fade. |
| `Powered.PowerStoredPercent` uses effective capacity. Its update paths can clamp stored energy to the effective maximum. | Charge percentage and retained capacity are different. A damaged pack can be fully charged while storing less energy; examine damage/repair transitions before promising energy conservation across them. |
| `CondOwner.Use(string)` applies a charge profile's fixed energy and wear amounts, drawing from configured self/contained items. | A tool profile represents energy per invocation. A timed interaction's duration alone does not turn that cost into a continuous load. |
| `CondOwner.Usable(string, out string)` checks available energy and consumables. An unknown use-case name returns true; `Use` returns without action for an unknown profile. | Validate profile identifiers explicitly. A misspelling can otherwise bypass both checking and charging. |
| `Use` returns void and its charge helper can drain available energy while seeking the remainder. | Do not treat calling it as proof a complete paid operation succeeded. Preserve native checks and test resource changes between starting and completing an action. |

For a proposed constant load, the unit conversion is
`energy_kWh = power_W * elapsed_game_seconds / 3,600,000`.
For a discrete scan, define energy per successful scan explicitly. These are
budgeting rules, not chosen power ratings. Define standby, scanning and monitoring
modes only when the chosen device needs them.

## Charging and continuous power

`Powered.Update` calls `Run` after at least one game second has elapsed.
`Run` multiplies a device's `jsonPI.fAmount` by elapsed game seconds. Given the
storage units, this path interprets that coefficient as kWh per second.

Charging contained batteries is a separate branch of `Powered.UsePower`, gated
by the recharging-container condition and connected external sources. It walks
contained packs and allocates available connected energy sequentially. Each pack's
ordinary `PowerRechargeAmount` request is **0.001 of its missing effective energy**.
Charging therefore cannot be inferred from the charger's own power-info coefficient.

For elapsed intervals exceeding 1,800 seconds, the inspected charging path scales
the request by 973. This is a coarse catch-up adjustment, not a guarantee of the
same result as many short updates. Test normal speed, acceleration and long gaps,
including both sides of this boundary. Do not publish an exact charging time from
the definitions alone. No general charging-efficiency factor was identified in
this inspected transfer path; losses and thermal restrictions remain unverified.

An existing compatible native pack and charger are the first reuse candidates.
Dedicated medical packs, adapters, thermal state and extra persistent battery
health should wait for a concrete need and evidence the native model cannot meet it.

## Interaction completion and interruption

`CondOwner.EndTurn` reduces the current interaction's duration in game hours and
calls `Interact` once it is complete. `Interact` performs range handling and then
dispatches `Interaction.ApplyEffects` or its chain. `ApplyEffects` uses contracted
tools before applying condition effects. This is the relevant native execution
path; calling `ApplyEffects` or `ApplyChain` directly bypasses the timed queue.

There is native item testing in `Interaction.Triggered`/`CheckItemsAvailable`,
and `EndTurn` can retest when `bRetestItems` is set. This is useful machinery,
but does not prove every resource is continuously reserved or revalidated at the
moment a custom effect commits. Battery removal, another user consuming a shared
resource and target invalidation need explicit tests.

`CondOwner.AICancelCurrent` requests queue cancellation. `ClearInteraction` can
run `strCancelInteraction` effects during cancellation. A custom cancel callback
must not also award the successful result. Prefer normal queue operation and
scoped hooks for `Phobos` interactions over replacing global execution behaviour.

## Saving: specific questions to test

Native object saves contain IDs, condition counts, rules, tickers and queued
interaction data. That is a useful foundation for preserving physical packs;
it is not proof that every transient operation survives correctly.

Two code observations make reload tests especially valuable:

1. The inspected `CondOwner.GetJSONSave` condition entries contain counts, but
   do not explicitly record `Condition` age. The load path can also clear
   conditions marked `bRemoveOnLoad`. Establish medication remaining duration
   before and after reload rather than assuming a timed condition is sufficient.
2. `JsonInteractionSave` declares a tool-use contract field, and post-load code
   can reconstruct it. The inspected `Interaction.GetJSONSave` and its save
   constructor do not populate that field. Test queued tool reselection, remaining
   duration and exactly-once energy charging after reload. Other runtime paths
   may compensate; this is an audit finding, not a confirmed gameplay defect.

Do not call `GetJSONSave` as a supposedly read-only diagnostic snapshot: it also
performs work such as company shift handling. Read only the observations needed.
Any new report/history state must declare its owner, patient ID, timestamp, version
and reload policy before promising persistence.

## Smallest plausible extension

Native JSON remains the candidate for item identity, slots, batteries, resource
requirements and existing treatment effects. A tailored diagnostic report or
longitudinal monitoring may need a small C# extension; no complete native report
path or patch signature has been validated for a proposed device yet.

[BepInEx's runtime-patching documentation](https://docs.bepinex.dev/articles/dev_guide/runtime_patching.html)
establishes an available extension mechanism, not compatibility of a particular
patch. Resolve references locally and verify the installed method signatures and
Unity runtime target before introducing a build. The installed development SDK is
not evidence of the framework a game plugin should target.
