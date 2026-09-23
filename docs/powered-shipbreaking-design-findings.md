# Powered shipbreaking: power, materials and the first experiment

Second research round, **2026-09-23**. Continues the
[initial feasibility study](powered-shipbreaking-research.md).
The sequence remains onboard processing first, external cutting later.

Implementation follow-up: the [first fixture build](shipbreaker-first-build.md)
now implements this recipe at its intended 4 x 4 footprint, with user settings
and F3 console controls. Build and offline logic checks pass; gameplay checks
remain with the owner. The research decisions below retain their original context.

## Decision from this round

The next implementation is a **single-workpiece powered fixture with one useful,
deterministic, mass-balanced recipe**. An ordinary loose wall panel is the first
comparison workpiece. The owner's **2026-09-23 verification preference** supersedes
the earlier proposal for a separate non-destructive power-metering prototype:
reuse established mod patterns and test our additions within the working feature.
Reuse Crafting Framework for suitable recipes and access to workshop content,
Salvage Workshop for its existing equipment, and Common Sense for normal hauling.
A small Phobos service should own the additional processing rules if the
experiment establishes their value.

The installed automatic framework cannot express the intended multi-material
process directly. Forcing the result into one magic salvage token would add a
second unpacking operation, material accounting and hauling complications.
Building another general crafting framework would be disproportionate. A focused
upstream extension is also possible, but no upstream change or author contact is
assumed. No gameplay code or installation was produced in this research round.

## Evidence and limits

Local definitions and selected implementation paths were inspected for game
**1.0.1.4**, Crafting Framework **0.8.71** and Salvage Workshop **0.8.71**. Game,
framework and load-order fingerprints still match the first round. The refreshed
inventory again finds 29 enabled native Workshop packages and 22 startup plugin
entries. See the [inventory](mod-extension-survey.md) for the separate native and
plugin loading states; counts are not compatibility tests.

The findings distinguish inspected implementation, inferred edge cases and
untested Phobos behaviour. Established mod patterns are a basis for implementation;
they do not require independent proof tests. Our complete integration is not yet
tested.
Definition-level masses and base prices are not measured inventory totals or
actual market quotes. No game session or save was modified. Decompiled material,
machine paths and raw reports remain under ignored `.local/`.

## Power: reuse native draw and connect it to our processing rules

| Inspected source | Finding | Consequence |
| --- | --- | --- |
| Native `Powered.Run`, `Powered.SetData`; `powerinfos/powerinfos.json` | `strOverrideCond` selects `fOverrideAmount` instead of `fAmount`. Native examples use `IsTurboOn` and `IsReloading`. The selected coefficient is multiplied by elapsed game seconds. | A Phobos work condition can select active versus idle draw without editing shared power definitions or writing a new grid. |
| Native `Powered.UserPowerExt` / `UsePower` | The public entry returns no supplied-energy amount. A request may consume partial available energy before shutdown. With valid on/off interactions, `UsePower` sets or clears `IsPowered` according to its result. | Connect our progress to eligible powered intervals; checking only at completion would miss a blackout. Investigate source depletion only if our integration introduces a concrete accounting concern. |
| Native `Powered.UsePower` | In the fallback to the machine's own `StatPower`, one branch compares and subtracts the original request after external collection, rather than the remaining demand. | Avoid mixing machine-owned storage with partial external supply in the first experiment. This is a code-path concern, not a demonstrated general game bug. |
| Native `CondOwner.EndTurn`; OCF `CraftCompletionPatch` / `MaterialFetching.CanComplete` | Ordinary work decreases interaction duration. Trigger retesting is conditional; OCF checks again before applying the result. Neither path is a history of energy supplied throughout the job. | A power trigger alone does not establish paid work. Test a blackout followed by restoration before nominal completion. |
| OCF `AutomaticPowerTickPatch`, `Automation.Tick` | Automatic machinery observes `UsePower`, requires powered state on consecutive observations and skips an interval longer than its cycle. | Useful precedent for a small observer, but it neither reports delivered kWh nor establishes arbitrary fast-forward or unloaded production. |

**Proposed first implementation boundary:** use the existing native power path
and OCF's powered-tick pattern for our fixture. Do not also call `UserPowerExt`
for the same work and charge twice. Configure valid on/off interactions, keep
the input fixed, and advance our processing only during eligible intervals.
Validate that new progress logic within the useful recipe; a standalone meter
or battery-depletion demonstration is not a prerequisite. Add diagnostic logging
only when it helps resolve a concrete integration issue.

Native rate switching applies the current rate to the elapsed interval. Account
for that in our start/stop logic; changing the work condition midway through an
interval does not imply exact sub-interval billing. Test a transition separately
only if our implementation changes the established pattern or exposes a fault.
Unknown or large time gaps should pause/revalidate rather than manufacture
unobserved work. Offline production is outside the first slice.

Sizing example, **not a selected cutting specification or required test**:

- 30 kW for 60 game seconds is **0.5 kWh = 1.8 MJ**.
- Its native active coefficient would be `30 / 3600` kWh per game second.
- An idle interval should draw the separately declared idle rate; blocked output
  must not keep charging at the cutting rate.
- Energy spent in a failed attempt is still spent. Never refund it merely because
  no product was completed, or turn partial payment into a whole successful batch.

Cutting rate still needs a defined operation: cutting attachment points and
separating a panel is different from melting all of it. A large available reactor
does not establish electrical distribution capacity, cutting speed or cooling.

## Materials: do not copy native yields as physical composition

The audit follows each candidate's dismantle definition to its named output
loot and sums fixed base masses times the declared minimum/maximum counts.
These are bounds, not expected values or claims about random-roll distributions.

| Loose workpiece | Input kg | Defined output kg | Input base price | Output base-price bounds | Assessment |
| --- | ---: | ---: | ---: | ---: | --- |
| Ordinary wall, `ItmWall1x1Loose` | 24 | 9–18 | 21 | 19.74–44.34 | Best controlled comparison; native output leaves 6–15 kg unaccounted for. |
| Whipple framework, `ItmWallThin1x1Loose` | 5 | 7–8 | 88 | 19.50–19.55 | Output exceeds input; unsuitable as a physical recipe template. |
| Aerodynamic angled wall, `ItmWallAero1x1Loose` | 4 | 7.6 | 500 | 111.50 | Output exceeds input by 3.6 kg; also an expensive intact component to sacrifice. |
| DuraWal interior wall, `ItmWallPlastic1x1Loose` | 14 | 7.6–13.8 | 210 | 104.54–298.74 | Fits under input mass, but material composition and residue still need justification. |
| Ordinary floor, `ItmFloorGrate01Loose` | 6.5 | Unresolved | 15 | Unresolved | Inspected dismantle entry has neither named nor direct output. This does not prove zero in-game yield. |
| Turbine lifter, `ItmFloorGrate4x401Loose` | 65 | 14–21 | 3,100 | 24.26–68.66 | Identifier is misleading: this is machinery, not a generic large floor panel. Defer. |

Sources: core `condowners/condowners.json`, `items/items.json`,
`installables/installables_dismantle.json` and `loot/loot_components.json`.
The active native data scan found no replacement of these selected definitions.
Runtime plugins can still alter behaviour. Base-price comparisons flag questions
for testing; they do not establish profit or final balancing.

The ordinary wall's reference outputs are 2–4 small mechanical parts (0.5 kg
each), 2 aluminium scrap (1 kg each), 2 carbon-fibre scrap (1 kg each), 2–6 steel
scrap (1 kg each) and 2–6 trash (1 kg each). Plastic scrap in the other recipes is
**0.3 kg**, not the 1 kg used by several other scrap definitions. Summing item
counts alone would give the wrong result.

**Design requirement, reinforced by the owner's concern about mass creation:**

`workpiece + consumed material inputs = recovered products + retained residue + explicitly accounted discharge`

For the first enclosed process, keep residue aboard: no implicit disappearance
into space. Include consumable losses when consumables are introduced. Track
actual object masses, including children and stacks, rather than assuming that
an object's base definition describes everything it contains. Reject unsupported
or non-empty inputs. Keep mass tolerance small and tied to numeric precision,
not a balancing allowance.

Do not equate a bookkeeping balance with correct metallurgy: matching kilograms
does not make steel out of plastic or preserve intact carbon fibres through
melting. The first recipe should cut/separate identifiable parts; remelting and
refining remain [idea 1](fusion-industry-roadmap.md#1-salvage-remelting-and-refining).
Recovery fractions and the residue representation are not selected yet. Avoid
turning all unknown remainder into generic sortable trash, since that would
inherit unrelated reward tables. Test the residual material's storage and
disposal before treating the production loop as complete.

## Input identity, multiple outputs and saving

Three additional boundaries matter before destructive processing:

1. **Progress must belong to the selected workpiece.** OCF's `Automation.Process`
   looks up a matching input each time, while progress and pending result rolls
   live on the machine. Removing an input does not reset those values in that
   path. Swapping an identical input can therefore inherit earlier work, by code
   inspection. For our process bind to the actual instance, reject stacks in the
   first slice, and stop on removal. Reload must restore that association or pause
   for explicit revalidation; it must not silently choose another matching panel.
2. **An output batch must fit as a whole.** Native `Container.AddCO` may merge
   stacks, place a remainder, or search nested contents. A null return usually
   means no remainder, but some invalid-input paths also return null. Testing
   each product independently against an unchanged tray does not reserve space
   for the combined batch. Completion needs validated inputs, a joint placement
   plan and recoverable failure handling before destroying the workpiece. Keep
   the first tray free of nested containers to reduce the cases to prove.
3. **Saving is different from mode switching.** `CondOwner.GetJSONSave` serialises
   condition values and object identity; `ModeSwitch` explicitly carries
   conditions marked `bPersists` to the replacement object. That flag alone is
   not proof of a complete saved job. Test progress, input identity, recipe revision,
   residue, pending completion and clocks across both save/reload and uninstall.
   Resume paused after reload until bindings are checked; do not credit the
   unloaded interval.

Could we simply advance native dismantling? `Installables.Create` connects the
progress threshold to a finishing mode switch. `Interaction.ApplyEffects` turns
the first result into the replacement object and drops additional results nearby,
including overflow. It does not promise delivery into a reserved output tray.
The native `TCanBeDismantled` also forbids `IsInContainer`; it does **not** itself
forbid damaged items. Directly increasing native progress inside our input bin
would bypass that handling rule. Keep native dismantling as a comparison rather
than using it as a hidden container transaction.

## Heat and dependency choices

A cooled machine still needs somewhere to send the heat. Moving heat into a
water loop does not remove it from the ship; vacuum does not provide convective
cooling. NASA describes conduction within spacecraft, radiation to the external
environment and finite thermal storage in its
[thermal-control overview](https://www.nasa.gov/smallsat-institute/sst-soa/thermal-control/).
Our inference is that sustained throughput should eventually be constrained by
both electrical supply and heat rejection. A temporary thermal buffer can allow
a short batch, but cannot silently reset on pause, uninstall or reload.

Do not make Ship's Water mandatory until an actual coolant-consuming or
coolant-circulating process is selected. Its detected OCF adapter establishes a
water-consumption integration, not a verified closed-loop cooling system. Keep
Common Sense hauling an optional compatibility target. Use Workshop dependencies
for OCF/Salvage Workshop where the fixture uses their content; scope any Phobos
patches to Phobos definitions and do not redistribute their assemblies.

Autopilot remains a later external-cutting concern. This onboard experiment needs
a secured input, not free-flight station keeping. The
[earlier external-work branch](powered-shipbreaking-research.md#autopilot-and-external-cutting)
still applies.

## Next useful implementation and focused checks

Build a fixture with plain controls, existing runtime resource references and
**one deterministic mass-balanced wall-panel recipe**. Reuse established power,
crafting and hauling patterns directly. Resolve the recipe's material yields and
residue representation as part of that implementation. Do not add a separate
power-observation or non-destructive metering milestone.

The owner tries the useful processing loop in a separate test save. Focus checks
on the new material ledger, workpiece binding, batch completion and saved job
state. These checks have not been executed.

| Case involving our added behaviour | Required observation |
| --- | --- |
| Complete the wall-panel recipe | Input mass equals products plus retained residue; exactly one workpiece is consumed and one result batch is produced. |
| Interrupt power during our processing, then restore it | Our progress logic cannot finish using unpaid elapsed time. This checks the new work coupling, not whether native power consumption works. |
| Remove the panel and insert an identical panel | No transfer of previous progress or pending output to the new instance. |
| Block the output tray or compete with a hauling order | No partial batch, lost input or duplicate output from our completion logic. |
| Cancel or save/reload during our job | Input binding, progress and material accounting survive without repeated completion. |

Add targeted damage, uninstall, near-empty supply or fast-forward cases where
our implementation changes the relevant behaviour or a specific concern remains.
Do not repeat the full upstream machinery test matrix by default. Compare handling
and total game-time against native dismantling of the 24 kg wall and relevant
Salvage Workshop recovery. Fixed yields make early accounting faults easier to
diagnose; randomness can follow if it adds value.

Proceed only if the result offers useful handling or sustained throughput. If it
merely replaces a short native action with loading, unloading and waste handling,
simplify to an assisted work fixture or set this machine aside. Static research
has now identified the next questions; another broad survey is less useful than
this useful implementation and its focused integration checks.

## Reproduce the definition audit

Run `scripts/inspect-salvage.py --game-path <local-game-folder>` with repeated
`--item <exact-definition-ID>` arguments and
`--output .local/research/salvage-candidates.json`. Local paths are supplied at
runtime. The script reads core plus enabled native mod data, records provenance,
and calculates bounds only for supported guaranteed-output expressions. It
does not inspect saves, expand nested loot or emulate generated/plugin changes.
Item socket dimensions are recorded as such, not claimed as inventory sizes.

The round-two run found all **12 selected definitions** (six candidate workpieces
and six output materials). It reported **11 same-package duplicate loot names**
elsewhere in the core dataset; none is a selected output table. These warnings
remain in the ignored report and cause a nonzero audit exit status, rather than
silently claiming a fully resolved runtime database.

Four offline fixture tests cover comment/trailing-comma parsing without input
mutation, native load precedence/disabled packages, conservative output bounds,
and named-loot versus direct-output resolution. Run
`python -m unittest discover -s tests -p test_salvage_inspection.py -v`.
They validate the research tool, **not in-game machinery or mod compatibility**.
