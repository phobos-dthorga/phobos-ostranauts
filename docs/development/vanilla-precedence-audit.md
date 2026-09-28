# Vanilla-precedence audit (28 September 2026)

Owner request: audit the earlier ChatGPT-era code for gates that never or almost
never pass, and for places where our code overrides or works around a game
action that should take precedence. Remove or rewrite whole sections where
needed so the mods keep a vanilla feel.

Method: three independent reviews of Framework, Agriculture/Shipbreaker and
Auto Nav against the game 1.0.1.5 decompile (local, not committed). Every
finding that drives a change was re-verified by reading the native method
named beside it. Mechanical checks: every condition, trigger, interaction and
loot name our code references exists in native data (Ship's Water names are
optional); none of our JSON overrides a vanilla definition by name; no loot
expression of ours carries a zero chance.

Owner decisions (28 September 2026): vanilla destructibility for cargo-holding
equipment, with refusals only at offer time; keep authored food values but
deliver them through the food's own eating replies; keep the stepped time-skip
and fix its faults; accept flight targets the game's own navigation-station
visibility accepts.

Verdict key: **Fixed** (version), **Kept** (with the reason), **Planned**
(round and version), **Deferred** (with the reason). Native evidence names the
game method; none of this is Unity or gameplay validation, which stays with the
owner.

## Round 1: Framework 0.36.0

### Crew standing orders

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Only the player's own character could take a standing-order task: the task named the player as owner, and the game forbids everyone not in an owner list. | `Task2.GetOwnership` returns Forbidden for any ID not in `aOwnerIDs`; `WorkManager.CollectTasks` skips Forbidden tasks. | Fixed 0.36.0: tasks name no owner, like a painted job. |
| One task per store: several orders targeting one store were silently dropped after the first. | `WorkManager.AddTask(task, nMax = 1)` counts existing tasks with the same target and action. | Fixed 0.36.0: up to 16 per target. |
| Need gating far stricter than the game: work refused for crew merely not fully rested, sated or slaked, or in moderate pain. The game never blocks work on needs; pledges put needs first. | `TCanDrinkThirsty` forbids only the lowest hydration band; `TIsSleepy` includes the second sleep band; painted jobs have no need gate. | Fixed 0.36.0: the five need triggers and the pain check are gone; alive, conscious, on shift, duty, role, ship and company remain. |
| Work refused when the equipment's contents changed after the claim (a tidied stack), although the provider re-validates at completion. | Provider `Next`/`Complete` check the actual contents. | Fixed 0.36.0: the claim-time snapshot is gone. |
| Training credit silently lost: the per-interaction credit guard was keyed on pooled Interaction objects that the game resets and reuses. | `DataHandler.GetObject` pops a pooled Interaction and calls `ResetObject`. | Fixed 0.36.0: reset clears the guard and the active-job map. |
| Task action cloned from the Inventory panel action. | `ACTTogglePower` is a plain vanilla Operate-duty job. | Fixed 0.36.0: cloned from that job; no panel, replies, switch effects or AI-opener flag. |
| A skilled idle colleague defers the unskilled worker. | Same admission as a claim. | Kept: the check already requires the colleague to be idle, eligible, routed and able to carry. |
| One task announcement per Enable, quiet re-adds afterwards. | `WorkManager.Update` interrupts every on-shift crew member's study when the task total rises. | Kept (0.35.0). |

### Time-skip

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Repair allowance replaced by our own count, up to six times smaller than the game's. | `GUIFFWD.FFWD` computes `ship-duty hours x skip seconds / 3600 x 2.25` and passes it to `UndamageParts`. | Fixed 0.36.0: the native amount is scaled by the share of on-shift crew time our jobs did not use. |
| Thirsty or queued crew excluded from skip work. | Same need rule as above; cancelled and Wait actions are idle. | Fixed 0.36.0 through the shared eligibility rule. |
| Orders on other ships suspended by any skip. | Only the skipping crew's ships are simulated by our stepping. | Fixed 0.36.0: only orders on those ships that the managed skip cannot advance are suspended. |
| Stepped simulation instead of one native update. | Owner decision. | Kept: fix its faults, keep the stepping. |

### Registration and finishes

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Finish-time refusals stranded the game's task: prefixes returning false skipped the native task closure. | `Interaction.ApplyEffects` calls `WorkManager.CompleteTask(strChainStart ?? strName, us, them)` before any effect. | Fixed 0.36.0: `NativeEffects.Refuse` closes the task and logs the reason; used by dismantle, repair-remainder and construction gates. Agriculture and Shipbreaker finishes follow in round 2. |
| Internal bin "empty" required an exact zero mass. | Native mass stats are rounded. | Fixed 0.36.0: shared mass tolerance. |
| Native loot tables and construction stations were cloned and republished under their own names. | `Installables.Create` appends to definitions in place; the game keeps private state on originals. | Fixed 0.36.0: `NativeDefinitions.Amend` runs in-place, idempotent links after commit; stations gain the craft action in place. |
| A trigger built in code passes never (chance zero). | `CondTrigger.Init` sets chance 1, so `new CondTrigger()` is passable; only an explicit zero fails. | Kept (0.35.0 validation refuses an explicit zero). |

### Clocks and budgets

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Transfer clocks, pump budgets and processing jobs treated any interval over 60 s or 3,600 s as a fault and disarmed the machine. | Native machines catch up after a long interval; received electricity already bounds the work. | Fixed 0.36.0: any finite non-negative interval is credited up to the remaining duration; cycles stay bounded. Shipbreaker's "time gap" pauses are removed in round 2. |

### Ship's Water and UI

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| A missing Ship's Water trigger silently became "always true". | `DataHandler.GetCondTrigger` returns the Blank trigger for an unknown name. | Fixed 0.36.0: the rule must exist in the trigger table. |
| The LowerUI guard intercepted every panel close, not only the one hosting our shell. | `CrewSim.LowerUI` lowers `goUI`, whatever it is. | Fixed 0.36.0: the guard applies only while the raised panel hosts the shell. |
| Object picking blocked the game's pause, time-scale, console and cancel keys; Escape did not close the picker the way it closes a native sub-window. | `CrewSim.CloseGUIData` calls `GUIData.CloseOutermostWindow` for registered `IDataWindow`s before lowering. | Fixed 0.36.0: the picker registers as a native window; only world selection is taken over. |

### Diagnostics

`phobosframework crew` now lists who could claim each order step right now,
by the same admission the claim uses.

## Round 2: Framework 0.37.0, Agriculture 0.17.0, Shipbreaker 0.34.0

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Without Ship's Water, every Agriculture panel became the recycler capture panel. | Blank-trigger fallback as above. | Fixed 0.17.0: gated on the adapter and on `NativeDefinitions.Trigger`; Ship's Water recycler definitions are amended in place. |
| Agriculture appliances never gained `IsPowered`, so every running machine read as blocked on the crew console. | Native sets and clears it only when the power info names `strIntPowerOn`. | Fixed 0.37.0/0.17.0: appliance power info names a self-targeted power-change action; the game's Run always proceeds; every installed appliance keeps its 20 W idle draw (machine heat, not lamp energy). |
| Stacked inputs refused (seeds, nutrients, rations, raw potatoes, coolant). | `StackOrAddToContainer` stacks matching items; `RemoveCO` detaches a member by itself. | Fixed 0.37.0/0.17.0/0.34.0: `StackUnits` counts members as units; one member is taken, the head stays. D4/R4 product unloading still moves unstacked products only (untested stacking of products). |
| Tank mode-switch, destroy, detach and scheduled destruction blocked at effects time, orphaning the replacement the game had created under the old ID. | Native creates the replacement before `ModeSwitch`. | Fixed 0.17.0 (owner decision): blocks removed; contents follow a tank successor; lost water is logged; refusals only at offer time. |
| Food effects applied by a prefix over the native eating chain. | Vanilla `SeekFoodAllowDirect` replies carry effects loot; openers list replies in order. | Fixed 0.17.0: identity conditions, triggers and cloned replies inserted ahead of the vanilla replies. Food made earlier eats with vanilla values. |
| Water vapour deleted. | `AddGasMols` ignores an unknown gas; no `StatGasMolH2O`. | Fixed 0.17.0: transpired water condenses back into the reservoir while it has room; crop water use drops by that amount; latent heat nets to zero. Respired water beyond the reservoir stays an accepted small loss. |
| Work actions failed after the walk. | Offer-time `TriggeredInternal` gate is the native way to withhold an action. | Fixed 0.17.0: `Service.WorkProblem` at offer and at run. |
| Refused maintenance and furnace finishes stranded the native task. | As round 1. | Fixed 0.17.0/0.34.0 through `NativeEffects.Refuse`. |
| R4/G4 heat rule stopped the job and needed Resume. | Native rooms hold 10 kPa/40 C industrial bounds. | Fixed 0.34.0: no power that step, job keeps permission, status shows temperature, air and pressure; continues by itself. The per-step interval bound (3,600 s) stays as an authored safety rule. |
| Shipbreaker "time gap" pauses after any interval over the transfer cycle. | Native machines catch up. | Fixed 0.34.0: removed in collectors, storage, intake and the D4 queue. |
| Wall eligibility from `StatDamage > 0`. | Vanilla `Wall1x1Uninstall` gates on `TIsWall1x1InstalledUndamaged` (installed, not damaged). | Fixed 0.34.0: the same rule, plus started-job continuity. |
| Protected state after an interrupted transfer or a mass drift with no way out but repair. | Owner-visible recovery. | Fixed 0.17.0: accept-contents on panel, C1 and F3 for readable records (`LiquidTransferGuard.Resolve`); unreadable records still need repair. |

## Round 3: Auto Nav 0.25.0

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Torch burns cut whenever Auto Nav flies: the step snapshot compared exact epochs, but the game advances the epoch inside `StarSystem.Update` after our prefix. | `StarSystem.Update` adds `fTimeDelta` to `fEpoch` in its body; `FusionIC.Update` runs on its own 0.27 s cadence. | Fixed 0.25.0: the picture is valid for the step plus the zone refresh (`NavigationAvoidance.StepFresh`). |
| Final approach crawls: the target sat in the imminent-obstacle sweep. | Straight-coast sweep about four times the braking distance. | Fixed 0.25.0: the target is excluded from that sweep; the arrival controller owns the approach. |
| Any tracked ship that later faded suspended the flight; a destroyed or despawned ship counted as a lost track; a clear direct leg was refused above the planner's obstacle count. | Corridor threats are the only ones the planner uses. | Fixed 0.25.0: only corridor threats carry forward; a ship that left the world is gone; a clear direct leg needs no plan. |
| Zero-duration manoeuvre calls do nothing. | `Ship.Maneuver` returns when `fDeltaTime <= 0`. | Fixed 0.25.0: smallest positive duration (`HoldThrust`). |
| Tumbling derelicts can never be docked or captured. | Derelicts spawn with spin up to 0.5 rad/s; native `CanDock` has no spin rule. | Fixed 0.25.0: target-spin gates removed from docking and capture stability. |
| Attachment bypassed the game's clamp admission and sequence. | `GUIDockSys.CanDock` and `ClampEngage` (alignment, port choice, crime checks, events, autosave). | Fixed 0.25.0: `DockingAdapter.Clamp` admits through native `CanDock` and presses native `ClampEngage`; the flight waits for the docked state. Our own attach and notification code is gone. |
| Departure bypassed the stolen-ship check, grace period, free pass and undock event. | `GUIDockSys.ClampEngage` (undock branch) and its `Undock` coroutine. | Fixed 0.25.0 when the docking console is open on that station and cleared for the peer (`DockingAdapter.ReleaseClamps`); otherwise the plain native `UndockShip` as before, with the clearance rule already enforced. |
| Any Updating sensor reading suspended the flight. | Native contact qualification settles over time. | Fixed 0.25.0: any refresh is held up to the settle budget, for flight, docking, capture, combat and fire. |
| Targets the game's own nav station always shows needed a signal test. | `GUIOrbitDraw.VisibleFromNavStation`: docked partners, signal beacons, tutorial derelict, known stations. | Fixed 0.25.0 for docked partners, beacons and the tutorial derelict. Known stations still take the signal test: their "known" record belongs to the station's own map. |
| Native orbit locks on the engaged ship were silently swallowed. | `ShipSitu.LockToBO/LockToOrbit`, `Ship.LockToOrbit`. | Fixed 0.25.0: a pilot takeover; Auto Nav disengages and the lock proceeds. |
| An oversized simulation step ended the flight, docking, capture move or fire control; Resume admission assumed the configured cap. | Steps grow with time compression. | Fixed 0.25.0: the step is held; admission uses the last real step. |
| Throttle slider used raw. | The game maps the slider with `MathUtils.ExpMap` for the pilot's RCS. | Fixed 0.25.0. |
| Resume refused a flight whose time budget was used up. | The saved elapsed budget is preserved across suspension and reload by owner direction (Auto Nav persistence, 2026-09-24). | Kept: a used-up budget is refused; the player starts a new flight. |
| Body velocity from finite differences (first sample unknown). | `BodyOrbit.dVelX` is a per-update position delta, not a velocity. | Kept: positions are differenced as before. |
| Physics finalizer disengages on any exception in `StarSystem.Update`. | Our own ticks catch their exceptions; what reaches the finalizer is native or third-party. | Kept: a physics exception leaves ship state uncertain, so stepping aside is the safe reading. |
| Native close-range guidance refuses under 5,000 km. | `FlyToAutoPilot`. | Kept: Auto Nav's own guidance is the reason the mod exists. |
| Sensor auto-engage, `knobRatio` write, time-scale reset, conveyor transfers, merchant stock floors. | Owner directions. | Kept. |
| Replacing crew hauling with the native "Give" loot effect. | Reviewer suggestion. | Deferred: it would drop the route, reservation and single-item pickup checks for no gameplay gain. |
| Hub flow control gating, no-wake margin from relative velocity, nearest-body occlusion, coasting threshold relative to the plan, recording issued RCS commands, native toggle listeners. | Refinements, not dead gates. | Deferred to a later round with in-game observations. |

## Owner checks (copy of an ordinary save)

- An NPC crew member with AutoTask on takes a rack or tray order.
- A thirsty or tired crew member still works until the game's own pledge sends
  them to drink or rest.
- A six-hour skip repairs about as much as the unmodded game; with heavy Phobos
  work it repairs proportionally less.
- Training progress keeps rising after many sessions.
- Cancelling a dismantle or repair finish with cargo present leaves no stuck
  task in the crew task list.
- Escape while picking a store closes the picker first, then the panel; pause
  and time-scale keys work while picking.
- Agriculture panels without Ship's Water show machine controls; a running rack
  reads as running, not blocked, on the crew console, and the game's power
  display shows it powered.
- Two seed potatoes stacked in a rack plant one at a time; stacked coolant fills
  one charge at a time.
- A damaged full tank can be destroyed; the crew log names the water lost.
- A meal cooked after the update satisfies as before; lettuce gives its authored
  small value; the eating log line reads as the vanilla one.
- Planting into an occupied rack is refused in the action menu with the reason.
- The R4 in a warm room shows the room's temperature and pauses drawing power,
  then continues when the room cools; a six-hour skip completes queued D4 work.
- A worn but undamaged wall is cut by G4; a damaged one is not.
- A torch-preferred approach burns continuously on the cruise leg; the final
  approach keeps cruise speed until braking begins.
- Docking a tumbling derelict with the docking console open: Auto Nav holds at
  the clamp position, the console's own clamp engages, and the flight ends
  docked; flipping a power switch mid-flight holds briefly instead of suspending.
- Undock and Depart with the docking console open releases through the console
  and the grace period applies; selecting orbital mode during a flight hands
  control back; heavy time compression holds a step instead of stopping.
