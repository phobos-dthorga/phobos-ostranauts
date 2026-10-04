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
| Ship's Water tanks had to join the process-water network without taking over their definitions (Framework 0.59.0). | Map points and socket adds are read from the definition when an item is placed or a save loads; the game redraws a placed item's neighbouring sheet pieces only for its own sprite-sheet trigger. | Amended in place at content load for the pinned 0.16.1 only: one map point and one plain `TILFixtureAdds` socket become the water port and joint; the redraw runs through `LineJoints`, so their sprite-sheet trigger, draw order and data stay theirs. A differently shaped definition is skipped. |

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
| Torch burns cut whenever Auto Nav flies: the step snapshot compared exact epochs, but the game advances the epoch inside `StarSystem.Update` after our prefix. | `StarSystem.Update` adds `fTimeDelta` to `fEpoch` in its body; `FusionIC.Update` runs on its own 0.27 s cadence. | Fixed 0.25.0: the picture is valid for the step plus the zone refresh (`NavigationAvoidance.StepFresh`). Necessary but not sufficient: the ignition handshake itself was still broken, see round 5. |
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

## Round 4: Framework 0.38.0, Shipbreaker 0.35.0 (29 September 2026)

Owner request: load every Shipbreaker machine by hand on ships that cannot fit
the grabber chain, with "Load feed" as something the crew keep doing until
cancelled, through the hourly time-skip. Owner decisions: a loading order
carries on after a reload like a painted job (F6 hot steps still need the
hazardous permission); crew draw from anywhere aboard.

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| The D4 feed, the grabber intake, crew supply, the G4 cutter and the capture planner demanded the exact name `ItmWall1x1Loose` / `ItmWall1x1` at exactly 24 kg, so the game's cosmetic wall variants (63,163 of 63,273 ordinary walls in the shipped ship templates) were refused everywhere. | `COOverlay.Init` applies the variant's condition loot (mass delta) and sets `strCODef` to the variant name; `CondOwner.ModeSwitch` keeps the variant on the loose replacement; `DataHandler.GetDataCO` prebuilds overlays under their own keys and `DataCO.Name` returns the overlay name. Loose variants weigh 14, 20, 24, 25, 27, 28 and 48 kg. | Fixed 0.35.0: identity is the definition base (`WallIdentity`), mass any whole kilogram from 14 to 48; revision 2 is derived from the wall's mass with the 13 kg packet fixed and steel as the remainder, so the plain 24 kg wall keeps its shipped products. |
| The D4 refused to wait for feed: Start with an empty feed and no grabber returned false, and a wall placed by hand started only while a job was already running. | The R4 already armed a waiting queue on Start. | Fixed 0.35.0: every processor waits after one Start; `FeedArrived` applies to all. |
| Hand loading was a one-shot window; nothing kept a machine fed. | The PDA's LOAD paints `ACTReloadItem<rule>` (`WorkManager.GetReloadInteraction`, duty Haul); `ClaimNextTask` searches the ship (`Ship.GetCOs(rule, subObjects: true, docked: false, locked: false)`, skipping stack members and carried items); `CompleteTask` never removes a reload task. | Fixed 0.35.0: right-click **Load feed by crew (on/off)** toggles a standing order with the reload job's search scope; our hauling is kept (round-3 deferral: route, reservation and single-unit checks) because the managed skip advances only our orders. |
| Standing orders drew from one chosen store and refused an item lying on the deck. | The reload job searches the whole ship, deck included. | Fixed 0.38.0: `StandingOrder.ShipWide` source, `CrewLogistics.Aboard` (deck, unlocked containers, other machines' trays, nearest first), deck items as cargo in play and in a skip. |
| Every industrial order suspended on reload. | A painted job survives a reload. | Fixed 0.35.0 (owner decision): D4, R4 and non-hazardous F6 orders resume routine work; hazardous F6 orders still wait, as `StandingOrder.Reload` requires. |
| Stock target defaulted to four products, so a loading order stopped after one batch. | The reload job has no target. | Fixed 0.35.0: the toggle sets the order maximum; crew stop when the products have nowhere to go. |
| F6 bin refuses stacks. | The game's own right-click places one unit off a stack (`GUIInventoryItem.OnRightClickDownSelected`, `PopHeadFromStack`); the charge records twenty individual identities. | Deferred: hand loading works the vanilla way; crew bring single pieces. |
| The selected character does not load with AutoTask off. | Painted jobs need AutoTask and the Haul duty too (`WorkManager.CollectTasks` gives manual crew only owned tasks). | Kept: the game's convention; the order status names the blocker. |
| Floors, doors, windows and other wall families. | Each needs its own authored budget. | Deferred (roadmap). |

## Round 5: Auto Nav 0.27.0 and Framework 0.40.0 (29 September 2026)

Owner report (2026-09-29): every mode held range from the target, drifted into neighbouring stations, and the torch never lit; the panel alternated Detour/Blocked with ordinary phases. Read against the local decompile of Ostranauts 1.0.1.5; nothing was run in the game.

| Finding | Native evidence | Verdict |
| --- | --- | --- |
| The torch never lit: `Burn` wrote the controls and reported success only if the reactor delivered thrust inside that call; the flight loop cut the command otherwise, in the same step, so the reactor's own update always read cycle 0. | `FusionIC.Update` runs `Run -> Fusion -> Ship.SetThrust` only when `fEpoch >= fTimeNextRun` (`FUSION_PERIOD` 0.27 s); `CatchUp()` is `Update()`. The offline reactor double ran on the call instead of on its cadence, which hid this. | Fixed 0.27.0: a written command stays pending until the reactor's own update delivers (`BurnState.Pending`), with a startup wait of two periods plus the zone refresh and a 5 s hold-off for a reactor that delivers nothing. The double now updates before the flight step, on the game's cadence. |
| Auto Nav set Cycle and a fixed flow-for-cycle Flow and never regulated the core. | `NavData.TimeAdvance` writes the planned cycle each step, seeds flow with `GetFLOWforCYCLE`, adjusts it each step with `GetAdjustedFLOW` (rescale toward 0.725 outside 5 %, else nudge 0.001 toward the thrust target), leaves flow alone for 2 s after a pilot move (`fFlowEpochResume`) and aborts outside 0.8–1.2. `FusionIC.Run` adds wall damage above 0.75; `NavModTorchDrive.GetLimiterSafetyMax` is the 2 g limiter behind the console torch safety. | Fixed 0.27.0 through Framework `ReactorRules`/`ReactorControls` (owner direction: drive Flow and Cycle like the course plot, within safe limits, and use Framework): same rules; the hot side corrects from the wall-damage temperature, tighter than the course plot; the abort stops the torch and restores the idle controls instead of zeroing flow; the console torch safety caps cycle as the game does. |
| Avoidance seized ordinary approaches: ships docked at the target were ordinary obstacles on the arrival line; a weak contact's 20 % position error (up to 20 km) alone could seize the controls; the planner's torch guard refused any burn whose RCS stop took over 600 s, which at half slider forbade the torch above about 30 m/s. | The native map draws weak contacts up to one fifth of range off; docked ships move with their host; the 600 s figure was the route planner's horizon, not a stopping rule. | Fixed 0.27.0: the target's dock group takes the target margin and never seizes or blocks the route; weak contacts beyond 10 km plan and request sensors but do not seize (`HazardRules.MaySeize`); the braking chord is swept however long the stop. Detour/Blocked notices carry range and weak-contact marks; `phobosnav status` counts takeovers. |
| The heading controller planned with the raw `RotAccelMax` while the budget granted `throttle x 0.25`; the torch worthwhileness check assumed twice the real turn rate and compared the turn against the prediction horizon on braking legs. | `HoldStationAutoPilot` applies `MathUtils.ExpMap` to the slider as manual flying does; `Ship.Maneuver` sums the absolute inputs. | Fixed 0.27.0: `RcsBudget.TurnAuthority` feeds the controller and the worthwhileness check; braking legs judge the turn against the RCS shedding time. The slider curve stays the game's (owner choice, 2026-09-29); the granted authority is shown in `phobosnav status`. |
| The oversized-step hold never ran for flights: `GuardNavigation` disengaged on `dt > MaximumStepSeconds` before `Tick` reached its hold. | Steps grow with time compression. | Fixed 0.27.0: the guard holds the step for flights, docking, capture moves and fire control; the next ordinary step resumes. |
| Cleared: the body-frame RCS conversion, heading convention and relative-motion signs. | `Ship.Maneuver` rotates `(fX, fY)` by the heading; `Ship.SetThrust` points along `(-sin, cos)`. | No frame or sign change. |
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
- Round 4: uninstall a Tsukuda or Minsheng wall by hand, carry it aboard and
  leave it on the deck; right-click the D4 and switch on Load feed by crew; a
  crew member with AutoTask on and the Haul duty carries it in, the D4 starts,
  and the order keeps going as more walls appear; switching it off stops it.
- A one-hour skip with the order on moves walls and completes batches; after a
  save and reload the loading continues without Resume.
- A 14 kg Aero-series wall yields the residue packet plus two parts; a 48 kg
  Glory-series wall waits for tray space while the tray is full.
- With Load feed by crew on at the R4 and no pairing, a residue packet in the
  D4 tray is carried to the R4 feed. On the F6 the bin fills with single pieces
  and Seal still waits for the hazardous permission; picking up an aluminium
  stack and right-clicking the bin places one piece by hand.
- The G4 captures and cuts a wreck built from variant walls.

## RCS propellant (Framework 0.42.0, 29 September 2026)

Owner request: RCS thrusters burn the bulk gases at their real worth, and the game's
own O2 and CO2 canisters are corrected too. For loaded ships Framework serves
`Ship.RemoveGasMass`, `GetRCSRemain` and `GetRCSMax` itself instead of amending them,
because the native loops return one species-blind mass and have no hook for a per-gas
worth or a non-canister feed. The replacement keeps everything the game decides:
regulators in the game's order, their `GasInput*` points, the game's `TIsRCSValidInput`
rule, and each container's own `GasContainer.RemoveGasMass`. It changes only the worth
of each kilogram (nitrogen exactly 1, so nitrogen-only ships are unchanged) and adds
registered feeds found on the same tiles. Shallow-loaded ships, `Maneuver`, refuelling
(`RefuelRCS`, `GUIStationRefuel`) and the thrust formula stay native. A failure in the
replacement falls back to the native method. Verdict: justified replacement, recorded.

Manufacturing 0.43.0 (4 October 2026) registers a second feed, the Slingwright RM-1
reaction mass feeder, through the same `IRcsPropellantFeed` contract. It adds no patch
and changes nothing above: the feed sits on a gas-input tile as the P1 does, and its
kilograms count as nitrogen (worth 1).

## Canister filling (Framework 0.44.0, Manufacturing 0.4.0, 29 September 2026)

Owner request: a safe way to refill the game's own canisters and suit bottles. The
native air pump (`GasPump.Pump`) fills without checking the destination's rated
pressure, which is why bottles burst; it is left untouched. The L2 is an additional
appliance that works on the game's own vessels through the same `GasContainer`
moles the game uses (`AddGasMols`), stopping at 99% of the vessel's own rating and
counting every species. It never changes the pump, the burst rule
(`CheckPressureDifference`), station refuelling or the vessels' definitions, and a
bottle in its rack stays an ordinary item the crew can take out at any time.
Verdict: additive, no native path replaced.

The A2 cabin air regulator (Manufacturing 0.5.0) adds oxygen and nitrogen to its
room through Framework `RoomGas`, the same clamped path as the X2's cabin
fallback. It does not replace the native air pump, scrubbers or room gas rules. It
only adds gas, stops below 10 kPa and never raises oxygen past 30% of the air.
Verdict: additive.

## Trigger refinement hook (Framework 0.45.0, 29 September 2026)

The two postfixes on `CondTrigger.Triggered` (construction selectors, section
assembly selectors) are one postfix, `TriggerRefinements`. It still only refines a
true native result for a registered selector name; unnamed and unknown triggers,
including the game's Blank trigger, keep their native outcome, and nothing is
gated on a name pattern. Checked natively with null, empty and unrelated names.
Verdict: the same amendment, one hook.

## Carved mining shares and the game's ice asteroids (Framework 0.48.0, Manufacturing 0.8.0, Shipbreaker 0.44.0, 30 September 2026)

Owner decisions: new mined material takes a share of an existing entry in the
game's loot tables instead of adding a roll, and the game's own ice asteroids
should appear in play.

- **Carved shares.** Framework `AdditiveLoot.CarveChoice` amends a native table in
  place: the donor unit's chance is reduced and the new choice sits right after
  it in the same cumulative expression, so every other unit keeps its band and
  the table never yields more. Tables are reassigned, never mutated (the game
  caches parsed units by expression). A table another mod rewrote afterwards is
  left alone and logged. Verdict: an amendment of native tables, no replacement.
- **The game's ice asteroids.** The game ships `ClusterI01` (its `Ice01`
  blueprint: ice walls, ice floors, a stony edge) and references it only from its
  own `RandomAsteroidI` and `RandomAsteroidAutoGen` pickers, which no star-system
  field uses. Shipbreaker carves it into `RandomAsteroidC` (from `ClusterC02`) and
  `RandomAsteroidS` (from `ClusterS01`) at 0.05 each, behind the
  `Mining/SpawnIceFields` setting (on by default). This uses the developer's own
  content and tiles and adds no new world object. Only asteroids generated for a
  new game change; saves keep the asteroids already rolled. Because the cluster is
  native, a save that later drops the mod still resolves it. Verdict: additive
  use of shipped but unreferenced native content, reversible by setting.
- **Deposit ice.** Shipbreaker carves 0.05 water ice (`ItmIce01`) into
  `ItmRandomMineralCClass` from silicates, behind `Mining/ExtraDepositIce`, so
  existing saves see more ice on future pulls. Manufacturing's clay hydrates
  take 0.10 of the same donor. Verdict: amendment within the game's own C-class
  composition.

## Methane ice price (Shipbreaker 0.45.0, 30 September 2026)

Owner decision: the game prices `ItmIce02` (methane ice, 24.84 kg) at 20, less
than the water it holds, so thawing it would make money and break the rule that
every Phobos process loses value. Shipbreaker sets its `StatBasePrice` to 250 in
place on the live definition (never republished), in the same release as the T2
recipe that consumes it. The mass, conditions, art and every other field stay the
game's. Blocks already in a save keep the price they were created with. Verdict:
a minimal in-place correction tied to its consumer.

Withdrawn (Shipbreaker 0.49.0, 30 September 2026): the rule it served is retired
(AGENTS.md "Refining value"), so the game's own 20 stands and Shipbreaker no
longer amends the definition. Vanilla precedence favoured this from the start.
Neither thaw product has a sell route, so the gain is water and methane aboard,
not money. Blocks priced 250 in an existing save keep that price.

Restored at 100 (Shipbreaker 0.62.0, 1 October 2026): the owner made refining a
business and approved a price table in which methane ice is worth 100, so a thawed
block earns about twice the block, and the station now buys water and methane
back. The correction is again made in place on the live definition, never
republished, and saved blocks take the live price on load through Framework
`EquipmentSaveUpgrade.FollowPrice`, which edits only the save DTO copy the
game hands to `CondOwner.SetData`. Verdict: the same minimal in-place
correction, now with the load refresh the owner's retroactive direction asks for.

## Mining laser (Shipbreaker 0.59.0, 1 October 2026)

The owner asked for a ship-mounted mining laser. The game has no ship-mounted
miner: crew mine with hand tools, and what falls is rolled by the game's own
tables when a rock's `Destructable` reaches its limit.

- **Mining stays the game's.** The ML-2 asks the game's own Mine rule
  (`TIsMineableDestructableNotDeposit`) which objects are rock, then applies the
  damage a stage has left through Framework `NativeDamage.Apply`, which mirrors
  one hit of the game's `DamageSystem.DamageRay`: add to `StatDamage` no more
  than is left, run the object's `Destructable` check, end its turn. The damaged
  form, the destruction and the ore roll are the game's. Nothing is spawned and
  no loot table is copied. Verdict: uses the native chain.
- **Ore deposits stay crew work.** The game gives deposit ore to the crew member
  drilling, so the laser does not touch an opened deposit.
- **Hull panels** are freed by the same forced native uninstall the G4 uses, on
  the same wall identity, and are left on the target deck. Verdict: unchanged
  native action.
- **No native definition is amended or republished** for the laser. Its own
  definitions use the shared machine family, an exterior fixture socket and two
  required hull walls, as the G4 does. It carries no ship-weapon condition, so
  the game's weapon systems never see it.
- **Animation** uses the game's own frame animation through `Item.SetAlt`. The
  game never detaches an item's frame step when its art changes and adds another
  on each start; Framework `SpriteAnimation` removes the item's own step before
  each change and touches nothing else on the shared event. The game's cosmetic
  overlay route is not used, because it replaces the object and its definition
  name.
- **Never blocks.** Refusals happen before work starts (no moored target, a
  pressurised hull, a person by the beam, a room that cannot take the heat).
  Native destruction, mode switches and uninstalls are never intercepted.

## Floor identity and laid fixtures (Framework 0.73.0, Shipbreaker 0.67.0, 3 October 2026)

Found while investigating the owner's report that no machine would link to a water,
nitrogen or carbon dioxide store by pipe (1 and 3 October 2026; save `pg8`, ship H-5YJG).

- **Floor identity.** In the game, `IsFloor` is a tile property. The tile gets it from
  the socket loot of the object installed on it (`TILFloor`, `TILFloorFixture`). The
  floor object itself never carries it: floor grates carry `IsFloorGrate` (the game finds
  a tile's floor object with a trigger on that mark), and the 4 x 4 aero grate and
  asteroid rock floors carry neither. Every floor object in the owner's save had
  `IsFloorGrate` and no `IsFloor`.
- **What it broke.** `NativeFluidRoute.SoundFloor` (since Framework 0.18.0) asked the
  floor object for `IsFloor`, so no pipe segment ever counted as laid on floor: no water,
  gas or acid line, irrigation conduit or F6-C conduit ever carried anything, and no saved
  segment in any of the owner's saves held contents. Shipbreaker's G4 reclamation floor
  test, its capture-support check and capture planning asked the same of objects and
  data definitions, so no wall could be cut. The F6-P underside check already accepted
  `IsFloorGrate` and was unaffected.
- **Verdict.** Follow the game's own data: Framework `Construction.NativeFloors` treats an
  object as floor when its definition gives its tiles `IsFloor` (read through
  `NativePlaceholders.TileConditions`), or when it carries `IsFloorGrate` or `IsFloor`
  itself. Tile checks keep reading `IsFloor` on the tile. `LineNativeChecks` proves the
  rule on every native object that makes floor, and that floor labels, walls and pipes
  are not floors.
- **Laid fixtures.** The game's installed power conduit (`ItmConduit00`) carries
  `IsHiddenInv` and is not pocketable. Our laid segments were pocketable and visible in
  the ground inventory, where the owner could drag them out of a line, skipping the
  Uninstall job and the drain-first refusal. Framework `ItemHandling.Fixture` now gives
  laid segments the conduit's flags; saved segments are corrected on load through the
  existing detached-save upgrade, and a compressed (`DEFAULT`) save takes them from the
  definition.
- **Unloading.** The game marks a ship destroyed before it destroys the ship's objects on
  a reload, while still reporting itself loaded. Destroy hooks that release, vent or log
  contents (bulk vessels, line contents, Manufacturing stores and reactors) now skip an
  object whose ship is being unloaded (`FrameworkLifecycle.Unloading`); the save keeps
  what it held. Native destruction itself is never blocked.

## Repair and Restore results (Framework 0.74.0, 3 October 2026)

Owner direction: what follows a repair or Restore should be the game's own result.

- **The game.** All 758 native repair jobs (`installables`, `strJobType` repair) were
  read. Every repair (`ACTRepairTEMP`) consumes its inputs and returns exactly one item,
  the repaired form; every Restore (`ACTUndamageTEMP`) consumes nothing and returns only
  the item itself. No native repair or Restore leaves scrap, trash or a byproduct.
- **What differed.** Framework's `RepairRemainderPatch` (since Framework/Shipbreaker
  0.6.0) replaced a Phobos repair's finish loot with the repaired item plus the consumed
  lot's mass as 0.5 kg Spent Service Parts, and refused completion for an unfamiliar lot
  mass. On the owner's long-running ship that left 56 of them (28 kg) inside a loose V4
  and on the deck; the owner had them removed from save `pg8` by hand on 3 October.
- **Verdict.** Removed. `MaintenanceDefinitions.Repair` leaves the native mode switch
  to consume the lot and return the repaired item. Restore already matched the game and
  is unchanged. `EconomyChecks` proves the native rule on the game's data, that every
  Phobos repair returns one item, and that no finish hook rewrites repair output.
- **Saves.** At the owner's request, `LegacyItemConversions.Retire` removes saved spent
  parts from every ship as it loads (the definition stays registered so they load
  first), with one crew-log line on the player's ships. This is an owner-directed
  exception to "never silently destroy an item"; it is announced, not silent.

## Power points in the wall row (Framework 0.77.0, 4 October 2026)

Owner direction: power connectors should sit back a tile, like the game's own, so a
machine set against a wall uses the conduit in it.

- **The game.** Its wall-backed floor equipment puts power points one tile beyond the
  back edge, opposite the use point: `ItmStationNav` (3 x 3) at (0, 32), `ItmChargerBattEVA`
  (2 x 2) at (8, 24), `ItmBattery02` (2 x 2) at (-8, 24).
- **What differed.** Twenty Phobos machines put their points inside their own back row:
  the shared `ApplianceDefinitions.Add` (8n - 8), Manufacturing's X2, K2, AX-2, L2, A2 and
  its V4, LC-3 and SA-3 data-pack entries, Shipbreaker's D4/R4, T2 and C1 (the C1 on the
  edge line itself).
- **Verdict.** Moved to `ApplianceDefinitions.WallRowY(depth)` = 8 x depth + 8.
  `PowerPointNativeChecks` checks every powered installed definition. Exceptions kept on
  purpose: the F6's documented front points, the G4 and ML-2 hull mounts (wall row on
  their hull side), the C2 set into the wall. Saved machines take the new points from
  their definition on load; conduit run under a machine's back row instead of behind it
  is the stated manual step.

## Laser crew jobs (Shipbreaker 0.71.0, Framework 0.78.0, 4 October 2026)

Fetching laser output and drilling opened deposits use the game's own painted jobs
(Framework `Crew.NativeJobs`: `ACTHaulItem` under Haul, `ACTMineDeposit` under Demolish,
queued through `WorkManager.AddTask` with the PDA's task names) instead of a Phobos
hauling order across ships. No native definition is amended; duties, tools, haul zones,
pathing and the ore reward stay the game's. `LaserNativeChecks` holds the game data the
jobs rest on.

## Vessel contents on the card (Framework 0.79.0, 4 October 2026)

Owner request: tanks and silos show their contents on right-click, as Ship's Water's
tanks do. The game's card draws any condition whose definition has `nDisplayType 1`
(its number module, re-read twice a second), so each commodity gets one
`StatPhobosVessel<Name>` condition of that kind, plus a shared `StatPhobosVesselTrapped`
for catch-chamber contents. `BulkVessel.Save` writes them from the record, and a
once-per-ship pass after loading does so for older saves. They are display mirrors:
the kilogram record stays the only truth (bulk-silo direction: no native stat that a
kiosk could fill by tag), nothing reads them back, and the names are Phobos' own.
`VesselContentsNativeChecks` holds every registered commodity to a declared row.

## The Ward-3 medical bed (Medical 0.1.0, Framework 0.82.0, 4 October 2026)

Owner direction: a new own-brand bed, leaving the game's Infirmaway untouched. No vanilla
definition is amended or republished. The Ward-3 joins the game's own systems by carrying
their marks: its installed intact form has `IsBedMedical`, `IsCushion` and `IsSheet`, so the
game's unchanged `SeekSleepSimple` gives medical sleep in it, the game's time skip gives a
patient with `SleepingMedical` its medical hour, and room types count it as a bed. Loose and
damaged forms carry none of these marks, so the Infirmaway's install job (which takes anything
passing `TIsBedMedical01Uninstalled`) and the fixers' lists never take one.

- **Power follows the game's own rule.** The game expresses an unpowered medical bed as
  `IsOff`, which `TIsBedMedical` forbids; the bed service mirrors power into `IsOff`, as the
  Infirmaway's Off form does, and it installs off.
- **Offers are refused, nothing is blocked.** One `TriggeredInternal` postfix refuses Sleep,
  Rest and Lay patient here on a Ward-3 someone else lies in (the game's own `IsOccupied` lapses
  in about ten game seconds, so it cannot hold a bed at high speed), Rest for the uninjured, and
  Sleep for the uninjured on a bed kept for the injured. It touches no other object.
- **The rest chain is the game's own pattern.** Rest is cloned from `SeekSleepSimple`, its loop
  from the chair loop `ACTChairSitAllow`, and a sleepy rester falls asleep through the game's own
  `SeekSleepSimpleLieDownMedical`, below the free-bed test of its opener. Rest is not an idle
  opener.
- **Placement copies the game's Drop Corpse.** Lay patient here releases the dragged body with
  the game's own calls; the patient's knock-out loop stays as the game made it.
- **Removal is refused when offered**, never by blocking native destruction: Uninstall and
  Dismantle are refused while someone lies in the bed.

## Weightless care and sending injured crew (Medical 0.2.0, Framework 0.84.0, 4 October 2026)

- **One constant in the game's wound code.** Weightless healing needs the factor inside the
  game's private `Wound.Run`, which no data reaches, so Framework transpiles exactly that
  constant into a call that keeps the game's own value unless a patient carries the Phobos
  stat. Applied by hand and skipped whole when the game's code changes, so it never fights a
  future version or stops Framework.
- **Orders join the queue.** Send injured crew here appends Rest and recover with the game's
  own `QueueInteraction`; it never uses `AIIssueOrder`, which cancels everything queued, and
  never touches crew the player controls.

