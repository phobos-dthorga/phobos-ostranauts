# Powered shipbreaking: feasibility and first observations

**25 September 2026 follow-up:** [autonomous reclamation research](shipbreaker-autopilot-research.md)
and its [implementation handover](shipbreaker-autopilot-handover.md) now cover the
selected-G4 external branch. Auto Nav is required in that future design, with N1
or N2 hardware; existing packages are unchanged. The Approach Assist assessment
and deferred ordering below are historical. Active G4 positioning with finite
reach, holding/cutting and advancement between walls is the recommended first
slice, subject to the documented geometry gates. Docking/capture is optional;
whole-wreck automation is not implemented.

Research date: **2026-09-23**. Owner-selected sequence: **onboard processing first,
external cutting later**. Part of the [fusion-industry ideas](fusion-industry-roadmap.md).

**Current status, 2026-09-24:** the historical OCF/SWB dependency recommendation
below is superseded by the owner's [independent Phobos Framework decision](phobos-framework.md).
For new downstream machines and ore, see [shredding and material processing](shipbreaking-material-processing-research.md)
and [asteroid life-support replenishment](asteroid-life-support-research.md).

Follow-up: [power, material accounting and the first bounded experiment](powered-shipbreaking-design-findings.md)
records the second round's native power hooks, input-identity concern and calculated
definition-level salvage balances. It refines the prototype proposed below.
The owner's subsequent verification preference removes a separate power-metering
prototype: reuse established mod patterns and check new behaviour in a useful
processing feature. The follow-up's implementation plan reflects that decision.
The subsequent [first build guide](shipbreaker-first-build.md) records the actual
4 x 4 fixture, chosen yields, editable settings and console commands. Its 30 kW /
60 s defaults are design choices; in-game verification is still outstanding.

## Recommendation

Develop an add-on to **Ostranauts Crafting Framework and Salvage Workshop** if
owner observations establish a useful processing gap. Those installed mods
already supply a workbench, broken-item recovery, a pillar drill, automatic trash
sorting, charging and ingredient collection. Common Sense Salvage and Storage
already addresses physical hauling. A second generic salvage bench would add
little value.

The strongest candidate is an **enclosed powered cutting fixture for selected
loose structural salvage**, with an explicit choice between retaining a usable
part and cutting it into useful stock/components. Its advantage should come from
handling, controlled processing or sustained throughput, measured against the
existing alternatives. Research alone has not established that this advantage
exists. A stationary cutter also does not make material too large to haul become
transportable before it reaches the machine.

Do not require a fusion reactor merely to enable the button. A credible workshop
load can be supplied briefly by batteries and sustained by generation. Larger
industrial throughput can follow when the owner reaches that stage.

## Evidence scope

Evidence labels: **data** = installed definitions; **code** = inspected local
implementation; **log** = startup evidence; **author** = supplied documentation;
**proposal** = design awaiting tests. There were **no in-game tests** in this round.
The running game, configuration, installed packages and saves were left unchanged.

- Game log: **1.0.1.4**. `Ostranauts_Data/Managed/Assembly-CSharp.dll` SHA-256:
  `1DC1858A8EDC514EC089F2FD7C55932C7F9B62B0A96201C15E2B2720122A03A7`.
  This matches the earlier locally inspected assembly.
- Crafting Framework installed/plugin version: **0.8.71**; assembly SHA-256:
  `2CBD6E2003892A4C28E620720965729AC9E4897B5A8C355A000E260C343F552E`.
- Salvage Workshop: **0.8.71**. Its 23 recipe definitions yield **21 registered
  recipes** in the observed startup; two Rusty's twin-item recipes are rejected
  because their output mods are absent. One powered machine registers.
- [Inventory and integration priorities](mod-extension-survey.md) distinguish
  configured native packages, loaded plugins and untested behaviour.
- Selective decompilation stays in ignored `.local/research/`. Findings below
  cite type/member names and relative data paths; no extracted code/assets are
  distributed with these notes.

## What already exists

Core paths below are relative to `Ostranauts_Data/StreamingAssets/data/`.
Mod paths are relative to the named Workshop package.

| Evidence | Finding | Consequence |
| --- | --- | --- |
| Core data: `condowners/condowners.json`, `ItmToolLaserTorch01` | Weber 'Lance' supplies cutting, welding, soldering and wire-cutting tool conditions with value 2. Its battery is `ItmBattery04`, nominal 0.82 kWh. | An installed cutter must outperform a capable native tool in a specific task, not merely have a larger description. |
| Core data: `chargeprofiles/`, `ItmToolLaserTorch01Normal`; code: `CondOwner.Use` | The normal profile costs 0.000016 kWh per use and applies wear. These are per-invocation values. | Do not call this a continuous watt rating without measuring actual invocation frequency. |
| Core data: `installables/installables.json`, `Wall1x1Uninstall`, `FloorGrate01Uninstall` | Generated uninstall actions use structural-cutting tools, target progress and crew skill; removal produces a loose component. | Retain the native removal/job system for recovering installed parts. Recipe crafting on loose items is a separate stage. |
| Core data: `Wall1x1LooseScrap`, interaction templates `ACTScrapTEMP` and `ACTDismantleTEMP` | Scrapping damages an item; dismantling is described as careful recovery. Native actions carry crime information. | Measure scrap/dismantle/uninstall separately; a custom process must not silently bypass ownership or damage consequences. |
| Salvage Workshop data: `crafting/recipes.json`, `SWB_Salvage*` | Six recipes already recover broken electronics, machinery, hull, furnishings, suits and other objects. The hull recipe consumes one broken item for a random aluminium/carbon-fibre recovery with possible bonus. | Avoid copying these categories or promising universal material accuracy from their gameplay yields. |
| Salvage Workshop data: `SWB_SalvageBrokenHull` and machinery triggers | Inputs require damaged, solid, uninstalled, non-human items; recipes require empty inputs. | A powered alternative needs a different, explicit eligibility rule and deliberate item selection. |
| Author: Common Sense Salvage and Storage README, v0.12.14 | Physical pickup/haul/sort, AUTO containers, cargo categories and one level of nested storage are already provided. | Reuse physical containers and existing orders; test machine-bin recognition before claiming automatic feeding. |
| Log and code: optional `ShipsWaterAdapter` | Crafting already detects Ship's Water; the adapter is enabled in this startup. | Do not create a second water registry or plumbing network for workshop recipes. |

## Crafting Framework extension surface and limits

`Registry.Load` scans enabled native mod directories for `crafting/recipes.json`.
`Automation.Load` reads `crafting/automation.json`. Both accept schema version 1
and reject unknown JSON members. Recipe packs are scanned by directory ordering,
so a later native load position is not a safe recipe-override mechanism.
Use unique `Phobos` recipe/machine IDs; duplicate recipe IDs are rejected.

| Capability | Verified code/data boundary | Design implication |
| --- | --- | --- |
| Crew-operated recipes | Public `Recipe` data has station IDs/trigger, ingredients, outputs, work seconds, range, state transfers, chance groups, bonus outputs and optional PDA blueprint. | Add recipes and native machine definitions in our own package. Reuse UI, collection and blueprint behaviour. |
| Ingredient protection | `Ingredient` supports exact `item`, condition `trigger`, count and `requireEmpty`. `CraftCompletionPatch` rechecks availability/station access before effects and blocks cancellation effects. | Useful foundations; still test competing jobs, stacks, theft and save/reload. These are not proof of an atomic transaction in every case. |
| Workstation power | Recipe schema has no `powerKW`, energy-per-job, heat or tool-wear field. A station trigger can require `IsPowered`; `SWB_TInstalled` and the inspected pillar-drill trigger do not require it. | A powered-looking station is not proof that work consumes industrial energy. Do not add invented JSON fields. Continuous progress/power coupling needs an explicit test or narrow code extension. |
| Automatic recipes | `Automation.ValidateRecipe`: exactly one specific input item/count 1; at most one output unit (or a chance of one). No state transfers, bonus outputs or operator blueprint. | It cannot directly automate the existing multi-output salvage recipes, consume cutting gas alongside the workpiece, or express a realistic many-part cutting batch. |
| Automatic eligibility | `Automation.Process` selects its input by exact definition. It does not re-run all the manual ingredient trigger/empty checks there. | Do not feed arbitrary containers, batteries or gas vessels to a custom automatic recipe. Constrain inputs and verify handling before reuse. |
| Machine definition | Installed object needs the declared input slot, output container and power info. One machine mapping per installed item definition. | A dedicated fixture is possible, but recipe switching is not an established capability of this schema. |
| Power/time | `AutomaticPowerTickPatch` hooks `Powered.UsePower`; `Tick` checks installed, powered, undamaged and not overridden off. It skips elapsed gaps longer than a cycle. | This is useful powered automation, but not general offline production or guaranteed high-speed catch-up. Test actual game-time step sizes. |
| Output blocking | `Process` checks output fit/stacking before removing input, preserves pending roll/progress and attempts input restoration on insertion failure. | A basis for full-tray behaviour; test it, including partial stacks and reload. |
| Persistence | `StatOCFAutoProgress`, `StatOCFAutoRoll`, `StatOCFAutoChoice` are persistent conditions; clock/fault tracking is transient. | Save/reload intent is visible. Do not claim verified persistence until tested. |
| Public API stability | Recipe/machine data classes are public; registry, automation and helpers are mostly internal. | Prefer the JSON interface. More advanced processing may merit an upstream extension or a narrowly scoped Phobos adapter, rather than depending broadly on internals. |

**Power reference:** `SWB_SorterPower.fAmount = 0.0001`; the inspected native
`Powered.Run` path interprets the coefficient as kWh per game second, equivalent
to **0.36 kW**. This is a code-derived reference, not a measured draw. The sorter
power definition is not a model for high-power cutting, and it does not itself
distinguish working from powered-idle demand. See the earlier
[native power findings](medical-runtime-findings.md#charging-and-continuous-power).

## Physical process and proposed first machine

An enclosed electrical cutter is a better initial fit than routing fusion plasma
through the crew's workshop. The enclosure can restrain a workpiece, contain hot
debris and support extraction. Cutting does not perform alloy purification.
Finished metal parts, composites and pressurised/electrical equipment need
different eligibility rules.

TRUMPF's [laser fusion-cutting explanation](https://www.trumpf.com/en_US/solutions/applications/laser-cutting/fusion-cutting/)
describes melting along a cut and using nitrogen or argon to eject the melt.
"Fusion cutting" here means melting, not a nuclear reaction. Hypertherm's
[Powermax125 operator manual](https://xnet.hypertherm.com/Xnet/library/library.jsp?file=HYP116922)
provides a roughly tens-of-kW industrial comparison and consumable/gas requirements.
These terrestrial processes are references; their gas flow and operating
conditions cannot simply be copied into vacuum. A dry mechanical shear is an
alternative if lasers add little gameplay value.

The proposed fixture should have a small, explicit input whitelist; one operator
action; useful existing outputs where they make physical sense; actual grid
draw; and a clear stop/pause state. Prefer direct player selection of valuable
workpieces. Automatic ingredient fetching must not turn an unrelated stockpile
into feedstock without an intelligible selection rule.

Illustrative energy calculation only: a **30 kW** electrical load running for
**60 game seconds** consumes **0.5 kWh**. This is not a chosen rating or proof of
cutting performance. Determine time from the operation, then account for energy;
do not choose a huge consumption value just to make a reactor compulsory. Waste
heat, ejected material and cooling require separate accounting.

Build the first useful recipe using established OCF patterns. Our added progress
logic must stop paid work when power is lost; check that integration in the recipe
rather than first running isolated demonstrations of ordinary power consumption.
If stock OCF timing permits work to finish after spending much of the duration
unpowered, extend the process service before adding more recipes.
Keep that service separate from the UI and preserve OCF's existing ownership of
recipe collection and completion wherever possible.

## Autopilot and external cutting

The owner wants this branch researched for later use. A machine processing cargo
aboard our ship does not need a proximity flight controller. Its workpiece must
be secured, and any acceleration interlock should describe that practical limit.

External cutting introduces three separate problems: approaching the derelict,
maintaining a working pose, and authorising a particular cut. The current
[Approach Assist](limited-autopilot.md) prototype has a test pulse and sensing
integration; it has neither completed approach guidance nor a work-position hold.
Reuse its sensing and control-ownership work when justified, without treating the
unbuilt capability as available.

**Native code:** `Ostranauts.Ships.Commands.HoldStationAutoPilot.RunCommand`
uses relative velocity for translational corrections and target bearing for
rotation. This inspected path does not regulate a chosen stand-off distance or
match a selected surface's attitude. It also includes direct velocity-component
assignments and reads the nav engine-mode setting. It is not a ready-made,
strictly RCS, fuel-accounted close-work controller for our use.

**Existing mod:** [Auto Navigate](https://steamcommunity.com/sharedfiles/filedetails/?id=3745533691)
advertises RCS approaches, fuel checks and off-console operation, then disengages
at an arrival boundary. Its documented default zero-arrival tolerance is 5 m/s,
which is not a cutting-clearance guarantee. It is absent from the refreshed
inventory. No stable inter-mod flight API or current-build compatibility was
verified. The later [reuse and permissions review](auto-navigate-reuse-review.md)
resolves the misleading page banner: Steam's metadata reports the item public
and not banned. That does not establish installed-game compatibility or licensing.

Recommended later order:

1. Test externally mounted equipment against a **docked or otherwise mechanically
   secured** test target, if the native mounting/docking model supports it.
2. Determine whether a free-flying cutter offers enough value to justify active
   control; docking may be the better operating model.
3. If needed, prove stand-off, relative velocity, attitude, target rotation,
   clearance, sensor confidence and braking reserve with the cutter disabled.
4. Enable cutting only inside the demonstrated envelope. Stop cutting immediately
   on manual takeover, lost tracking, uncertain target, fuel/power failure or
   competing flight control. Stopping a cut does not stop a moving ship.
5. Keep controls under one owner; no simultaneous native station keeping, external
   autopilot and Phobos thrust commands. Reload disarmed and require a fresh check.

Directly modifying a remote ship also needs exact part targeting, obstruction and
crew checks, existing ownership/permit rules, hull/pressure consequences and
debris handling. A distant sensor contact is not authorisation to delete hull
tiles. Remote cutting and tractor-like cargo collection remain unproven engine
work, not consequences of adding an autopilot.

## Owner-run observations when the equipment is encountered

Use a separate test save for destructive comparisons. Begin with the installed
mods; there is no Phobos cutter build to install yet.

| Observation | Record | Decision it informs |
| --- | --- | --- |
| Native removal and dismantling | Exact component, condition, tools, crew skill, game-time duration, tool charge and outputs | Whether cutting speed or hauling is the actual bottleneck |
| Existing workbench | One empty, unstacked eligible broken item; time and actual outputs from its recipe | Whether Salvage Workshop already solves the need |
| Trash sorter, when relevant to the proposed recipe | Input/output handling and useful throughput | Gameplay comparison; ordinary power use can be accepted from established patterns |
| Hauling | Move ordinary material to the bench area and sorter input through Common Sense orders | Which bins/tasks cooperate without a new logistics system |
| Interruptions | Mid-cycle loss of power, cancel, full output, damage, input removal and two competing jobs | Preservation of materials and progress |
| Time and persistence | Normal speed, available fast-forward speeds, pause, save/reload mid-cycle and leaving/returning to ship | Duplicate/lost output, large-step skips and unsupported away-time work |

For the implementation, define its energy budget and state/ownership rules, then
select checks for our new behaviour and concrete integration concerns. The table
is a set of possible observations, not a mandatory prerequisite test matrix.
Established upstream behaviour does not need isolated proof tests. Record current
game and dependency versions for checks actually performed.

**Proceed** if a specific loose-salvage operation is worth improving and the
extension can preserve physical handling and resource accounting. **Simplify or
set aside** if it becomes a duplicate bench, a trivial global speed multiplier,
or requires rebuilding the upstream framework for little benefit.
