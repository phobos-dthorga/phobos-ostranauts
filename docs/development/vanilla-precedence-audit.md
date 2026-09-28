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

## Round 2: Agriculture and Shipbreaker

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Without Ship's Water, every Agriculture panel became the recycler capture panel. | Blank-trigger fallback as above. | Planned: gate on the adapter and an existing trigger. |
| Agriculture appliances never gain `IsPowered`. | Native sets it only when the power info names `strIntPowerOn`. | Planned: power on/off interactions as Shipbreaker machinery has. |
| Stacked inputs refused. | `StackOrAddToContainer` stacks matching items. | Planned: accept stack heads and members; detach one unit. |
| Tank mode-switch blocked at effects time, orphaning the replacement the game already created under the old ID. | Native creates the replacement before `ModeSwitch`. | Planned: remove the block; refuse at offer time only (owner decision). |
| Food effects applied by prefix over the native eating chain. | Vanilla `SeekFoodAllowDirect` replies carry effects loot. | Planned: own eating replies inserted with `InsertInverse`. |
| Water vapour deleted. | `AddGasMols` ignores an unknown gas; no `StatGasMolH2O`. | Planned: keep vapour in the crop water budget. |
| R4/D4 heat rule stops the machine and needs Resume. | Native rooms hold 10 kPa/40 C industrial bounds. | Planned: pause and auto-resume; show room mols and temperature. |
| Wall eligibility from `StatDamage > 0`. | Vanilla wall-uninstall installable's `CTThem`. | Planned. |
| Protected state after an ambiguous transfer. | Owner-visible re-baseline. | Planned: accept-contents command. |

## Round 3: Auto Nav 0.25.0

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| Torch burns cut whenever Auto Nav flies: the step snapshot compared exact epochs, but the game advances the epoch inside `StarSystem.Update` after our prefix. | `StarSystem.Update` adds `fTimeDelta` to `fEpoch` in its body; `FusionIC.Update` runs on its own 0.27 s cadence. | Planned: validity by step window. |
| Final approach crawls: the target sat in the imminent-obstacle sweep. | Straight-coast sweep about four times the braking distance. | Planned: exclude the target. |
| Zero-duration manoeuvre calls do nothing. | `Ship.Maneuver` returns when `fDeltaTime <= 0`. | Planned: minimal positive duration. |
| Tumbling derelicts can never be docked or captured. | Derelicts spawn with spin up to 0.5 rad/s; native `CanDock` has no spin rule. | Planned: drop the spin gate; native clamp engagement. |
| Departure bypasses fees, the free pass, stolen-ship checks and the grace period. | Native `ScheduleUnDock` path. | Planned. |
| Any Updating sensor reading suspended the flight. | Native contact qualification settles over time. | Planned: hold up to the settle budget. |
| Native close-range guidance refuses under 5,000 km. | `FlyToAutoPilot`. | Kept: Auto Nav's own guidance is the reason the mod exists. |
| Sensor auto-engage, `knobRatio` write, time-scale reset, conveyor transfers, merchant stock floors. | Owner directions. | Kept. |
| Replacing crew hauling with the native "Give" loot effect. | Reviewer suggestion. | Deferred: it would drop the route, reservation and single-item pickup checks for no gameplay gain. |

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
- Agriculture panels without Ship's Water show machine controls.
- Round 2 and 3 checks are listed in their own guides when they land.
