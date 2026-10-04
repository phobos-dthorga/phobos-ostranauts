# Performance audit and practical playtesting

Framework 0.31.1 follow-up: unnamed native trigger/interaction hooks return before
dictionary lookup. This corrects an owner-reported exception without introducing
scans, logging or recurring work. Native true/false outcomes remain unchanged;
compiled-hook regression tests pass, with live confirmation pending.

Prepared 27 September 2026 for Framework 0.29.0, Auto Nav 0.20.3,
Shipbreaker 0.28.2 and Agriculture 0.15.2. Manufacturing 0.0.1 remains held.
This is an offline-verified development candidate, not a measured speedup or
Unity interaction approval. The owner deferred further captures on 27 September;
ordinary play and qualitative feedback are the next evaluation step.

## Evidence and priorities

Six owner-run 30-second summary captures preceded these optimisations. The owner
confirmed the same scene, target, zoom and machinery activity. All report speed 1
and zero dropped/rejected records. The first closed-console capture includes both
paused and unpaused frames; retain it as supporting evidence, not a matched run.
Two captures end with an incomplete timed operation. Their frame samples remain
usable; the affected operation totals omit the unfinished call.

| Capture ID | Nav console | Median frame ms | 95th percentile ms | Frames over 100 ms | Panel ms per second | Furnace ms per second |
|---|---|---:|---:|---:|---:|---:|
| 2c75de0d6aa24c288cee94ff9a6a5217 | Closed; pause changed | 13.35 | 47.64 | 13 | 0 | 154.85 |
| fa60cfa7a2994561af0d19a25ac9f043 | Closed | 13.67 | 48.42 | 20 | 0 | 189.36 |
| d812f1587c984b1e8e196db40d092d32 | Closed | 13.48 | 48.00 | 19 | 0 | 189.80 |
| c00af084833047639282b35abdd63536 | Open, Nav | 174.24 | 237.90 | 173 | 676.46 | 126.59 |
| cf619a851cbc4812a559fd64743db44c | Open, Nav | 168.91 | 241.10 | 173 | 680.76 | 132.58 |
| 1c8c5b450d2641caa0572e129ef838f2 | Open, Nav | 166.15 | 259.00 | 166 | 637.92 | 169.07 |

Timings are inclusive elapsed wall time: nested timings must not be added into
CPU percentages. Almost all panel time is in `autonav.panel.read`; display writes
account for roughly 1 ms per second of the parent scope in these runs. The native
game, collection pauses and third-party work are not independently attributed by
these summary recordings. No required save provider was removed for comparison.

The baseline exposed an allocation API that reported only zero, without proving
that it worked. Those totals are unavailable evidence, not zero allocation.
The new recorder checks that a known allocation advances the counter before
reporting support. Collection counts are process-wide and are not attributed to
an individual mod. Main-thread allocation measurements, when supported, are also
not individual-mod measurements.

## Findings and decisions

| ID | Finding and evidence | Implemented decision / remaining limit |
|---|---|---|
| P1 | Auto Nav's per-frame idle display called `SelectPorts`. Local inspection of Blue Bottle Games' installed `Ship.GetAvailableDockingPorts` shows two `CreateFullGrid` calls followed by port gathering and rotated overlay checks. | Display reads clearance/open ports only. Full native fit checks remain at command admission, ongoing docking checks and attachment. The display says fit is checked when starting. This is a strong source-supported cause, not an isolated timing measurement of that native method. |
| P2 | The native panel update read and formatted hidden tabs, decoded state repeatedly and rediscovered modules/contacts. | One synchronous presentation snapshot, shared header plus selected page, 0.1-second real-time pacing. Open/actions/page changes invalidate immediately; console/operator/target/language identity changes also invalidate. Common safety warnings can still read facts needed by the header. |
| P3 | Furnace discovery examines about 42,034 world objects at its existing quarter-second cadence. Three equipment families used up to four concatenated strings each per candidate. | Allocation-free exact family comparison shared with Agriculture bulk tanks. Keep world coverage and cadence: passive cooling and detached hot items must not stop when the UI is closed or the player moves. A global retained equipment index is deferred until lifecycle equivalence and additional benefit are established. |
| P4 | Crew equipment/stores discovery filtered the global object map for each crew ship; industrial discovery already used native ship scope. | Shared fresh `ShipEquipment.Read`, explicit loaded ship, roots, destruction and content predicates. Native `Ship.VisitCOs` was inspected: root `mapICOs` plus room owners, with neighbouring docked ships excluded. No cross-frame membership cache. |
| P5 | Shared persistence allocated empty dictionaries for missing state and copied an old map merely to validate a write. | Shared immutable empty result and direct current-map validation. Successful reads still return detached immutable snapshots; unknown schema, owner and malformed fields remain protected. |
| P6 | Existing panel widgets repeatedly looked up children and reassigned unchanged artwork/text. | Weakly held text/native-widget bindings and change-only writes. Reused presentation cadence in Agriculture and Shipbreaker panels. Unity rendering and native Edit/rescue interaction still need owner feedback. |
| P7 | Baseline coverage lacked calibrated allocation support and several first-party scopes. | Keep bounded opt-in panel, background, industrial, Agriculture and crew timings plus frame/collection samples. Allocation availability is calibrated; new captures and overhead measurement are deferred. |
| R1 | Agriculture's two-second global scan averaged about 3 ms per second; it includes damaged/uninstalled equipment, visual state and session cleanup. Native power hooks separately settle measured growth/cooking/fluid updates. | Keep cadence and world scope. Existing artwork already compares the selected path before changing native art. No cached biology, free power or revised material budget. |
| R2 | Auto Nav's closed-console system prefix averaged about 1.06–1.07 ms per second in the fully unpaused runs. It also maintains movement history needed for navigation safety. | Keep physics-boundary guidance, docking, fire, obstacle and departure work authoritative. No UI throttle is applied to simulation or collision checks. Postfix work is outside this prefix timing. |
| R3 | Capture/reclamation traverse live sessions, and processing/routing run through native power boundaries. Baseline idle capture/reclamation costs are small; active industrial workloads were not benchmarked. | Preserve mutation-safe snapshots, reservations, exact-ID validation and measured receipts. Larger job/index rewrites are deferred; these scans are not interchangeable with ship equipment lists. |
| R4 | Registration, economy, loot, construction, language files, optional-provider reflection and native assembly audit are primarily load-, command- or first-use work. Save compatibility/room repair runs on native load hooks. | Preserve registration transactions, historical compatibility, one-time reflection and load repairs. No recurring disk writes were added; capture export remains explicit. |
| R5 | Crew panels refresh at one-second cadence. Object-picker projections and hit-testing follow the camera/pointer each visible frame, with candidate refresh every half-second. | Leave interaction cadence intact. Projection-buffer reuse is a future measured opportunity; membership and hit validation must remain current. Completion audio polling is constant-size, without discovery. |
| R6 | Manufacturing has only an Awake registration/logging scaffold and no machinery/update loop. An old local 0.0.1 DLL was still loading despite a disabled native entry. | Build for dependency compatibility, retain 0.0.1 and publication hold. The installer can archive the verified old scaffold DLL out of the loader directory while preserving its disabled native entry and other files. No operational provider is removed. |

## Coverage and shared interfaces

The [source ledger](../../config/performance-audit.json) accounts for every first-party
C# file under `src`, excluding build output. It records normalized source hashes,
runtime hooks, scan/allocation/IO indicators and review dispositions. Coverage is
static runtime-path and call-site review, with deeper inspection of the measured
hot paths, changed implementations and safety boundaries. It is not a claim of
line-by-line formal verification or an exhaustive native/third-party profile.
`python scripts/audit-performance.py` rejects missing, extra or stale source entries,
and the Documentation workflow runs it on every push. The 27 September snapshot
reviewed 259 files: Framework 96, Auto Nav 51, Shipbreaker 74, Agriculture 37 and
Manufacturing 1. Since 29 September the ledger is regenerated, never hand-edited:
`python scripts/audit-performance.py --refresh --finding <code> [--since <commit>]
[--carry] --report` recomputes every hash, carries unchanged rows verbatim, stamps
changed and new files with the finding code (retaining earlier codes), records the
review date, drops removed files and recomputes the static indicators of the rows
that changed. A changed file with neither a finding nor `--carry` is refused, and
`--report --strict` fails when a stamped code is not described in this document.

Framework additions are additive: `PresentationRefresh` (real-time display pacing),
`Presentation` (change-only text/visibility/control/colour writes), `ShipEquipment`
(fresh native root-object discovery) and `EquipmentIdentity.IsFamily` (ordinal
four-form matching). Native instrument helpers retain their public signatures.
Text bindings are first obtained after constructing the label and its optional
CompactText component; do not add/remove that component after binding.
Auto Nav's presentation snapshot is private, short-lived and never passed to a
flight, firing or transfer command. Content keeps its own policies and balance.

## Verification and remaining owner checks

Focused offline checks cover cadence, immediate invalidation, hidden-tab reads,
module/contact reuse, changed hardware, docking fit rejection after display,
immutable persistence, unchanged widget writes, and discovery after creation,
destruction, same-ID replacement, movement and reload. Tests use boundary doubles
where Unity is unavailable; native assembly/definition checks complement them.
Normal build checks cover navigation/docking/sensors/fire, crew, processing,
fluids, conservation, native definitions, localization and packaging.

Completed offline results for this candidate: 19,046 Framework assertions;
853,806 navigation, 48,337 torch, 437,312 docking/industrial, 326 sensor/presentation
and 69 fire-control assertions; 8,378 Shipbreaker and 824 Agriculture checks;
11,249 native-definition/binding checks; 15 presentation/discovery boundary checks;
47 recorder checks accepted by the pinned Rust analyser; 75 Python maintenance
tests and 303 synthetic installer checks. Crew, observation, reclamation and load
boundary suites also pass. Counts include numerical parameter sweeps, not gameplay
coverage. All five mods compile with zero warnings/errors. Item references were
regenerated from native definitions; only versions/source fingerprints changed.

Optional ordinary-play checklist:

1. Open/close Nav several times and switch every tab. Notice whether opening,
   target selection and tab changes feel smoother and buttons respond promptly.
2. Change a target or operator, try native Edit and rescue, and apply/discard a
   settings draft. Check that text and controls reflect the current situation.
3. Fly and dock normally. Missing clearance, damaged equipment and invalid hull
   fit must still block unsafe commands. Check pause and simulation-speed changes.
4. Operate the machinery and reload a save. Check retained contents, heat, crew
   assignments and the existing explicit-resume rules.

Record observations as qualitative feedback, including the scenario and versions.
After-capture comparisons, profiler-overhead measurements and the original 50%
presentation-cost target are deferred by owner direction; none is an acceptance
claim for this release. Use the existing opt-in recorder later if needed.
The six original captures, baseline packages and verified pre-profiling rollback
packages remain local. Installation uses the guarded installer, never a running
game; Manufacturing remains held. Steam publication is not part of delivery.

## P8 — Polaris interface follow-up

The owner reported that the installed optimisation was already working much
better. This is qualitative play feedback; no percentage/FPS improvement is
claimed. The six original captures remain unchanged and additional captures stay
deferred.

The subsequent [interface refresh](polaris-interface-refresh.md) retains the
0.1-second presentation gate and hidden-tab boundaries. Installed weapon discovery
runs only for Fire/picker presentation or an explicit checked weapon action.
Native shot eligibility remains separate. Widget styles retain weak bindings;
selected state changes update only their marker/label. Industrial navigation wraps
on layout/size changes. No recording is enabled and no capture file is written.
The source ledger has been extended for these reviewed additions; the earlier
259-file count and test totals above describe the optimisation delivery.

## Polaris readability correction

The owner-reported white-button regression is corrected by owning all native colour states when binding widgets. No per-frame work is added. Flight Hub footer geometry is set at creation; recovered tracking changes only the choice of displayed warning from the existing snapshot. Weapon damage/off wording uses existing inventory facts. The ten-per-second scheduler, hidden-tab inactivity, command validation and discovery cadence remain unchanged. No new measurements were taken; see the [Polaris correction](polaris-interface-refresh.md).

## C1 — Auto Nav 0.22.0 follow-up

Combat reuses the current guidance controller, holds only transient session bindings,
and preserves selected-tab refresh and widget suppression. Avoidance cache coordinates
now follow the same target-relative frame as its velocity model. Combat validation
runs at the common physics boundary; no additional recorder or per-frame logs were
introduced. See [the repair and Combat record](../auto-nav-combat.md). No additional
performance captures or quantitative improvement claims were made.

## T1 — Secured towing correction

Auto Nav 0.22.1 reads the fresh native attachment map for one reciprocal tow; no retained discovery cache is added. The enclosing radius protects both hulls and existing bounded presentation remains. No new recording or performance measurements. See [towing evidence](../auto-nav-towing.md).

### T2 — secured towing fire-control follow-up (0.22.2)

FCS reuses the existing fresh, ship-scoped towing policy at admission and update.
Attached-target exclusion also runs immediately before dispatch. No global
inventory scan, retained discovery results or extra presentation refresh was added.
Combat continues through the existing single movement authority. The native aim
stop now uses a positive interval, matching the core's existing stop convention.
No new frame-time measurements or Unity validation are claimed.

### H1 — native item handling correction

Content-owned handling choices are normalized once during definition preparation. Explicit heavy-item flags are added to detached load DTOs; existing hand slots have a weakly tracked, event-driven release exception. No world scans or recurring update work were added. Native slot, maintenance, food and construction behaviour remain authoritative. Live inventory interaction is unverified.

### E1 — merchant coverage and finite world loot

Stock probability floors, missing-offer checks and new loot branches run only during definition preparation. Coverage uses the prepared merchant branches and existing offer registry; no merchant inventory scans, per-frame hooks, forced restocks or recurring allocations were added. Existing generation hooks and content-owned balance are retained.

## 29 September: fast-forward pass

The owner reports the game slowing to a crawl at accelerated time, and often at
normal speed, with every mod active and little Phobos equipment installed. Three
static audits of the current tree (Framework 0.44.0, Shipbreaker 0.40.0, Agriculture
0.20.0, Auto Nav 0.27.0, Manufacturing 0.5.0, War Declared 0.1.0) found the
mechanism: the game runs every powered appliance's `Run`/`UsePower` once per game
second, so at fast-forward every cost added per power step runs up to once per frame
per appliance; real-time polls over every world object add fixed hitches at every
speed. R6 above is outdated: Manufacturing has live per-step services, power patches
and game-time rechecks.

The ledger is now regenerated by `python scripts/audit-performance.py --refresh`
(see the coverage section) and verified by CI. Finding codes for this pass, each
described in its stage's entry below as it lands:

| Code | Scope |
|---|---|
| FF0 | Source reviewed in this pass (static call-site review); instrumentation scopes added |
| FF1 | Shared Framework primitives: definition index, per-step memo, real-time cadence, changed-only saves, save boundary, deferred text, one trigger refinement hook |
| FF2 | Fluid topology cache (pipe, gas line, coolant, floor validity) with real-time recheck and native invalidation; supersedes the P3/P4 deferral for fluid topology only |
| FF3 | Power-hook gating: null state for appliances that are not ours, one classification per call |
| FF4 | Settled saves: records written on change, at a bounded real-time cadence and before every native save |
| FF5 | Per-step reuse of contact readings, hardware checks, routes and candidate lists |
| FF6 | Auto Nav foreign-controller reflection cached; flight record cadence |
| FF7 | War Declared poll and retry cadence |

Baseline captures for this pass (owner-run, 29 September, summary mode, 30 s,
fast-forward, little Phobos equipment installed) are kept locally under
`.local/performance-captures/before-fastforward/`; the earlier six speed-1
captures remain separate evidence. Two distinct captures at speed 8 with the nav
console closed: median frame 24.8 and 25.9 ms, 95th percentile 125 and 140 ms,
99th 174 and 238 ms, 205 and 222 of about 800 frames over 33 ms, 64 and 63 over
100 ms; `shipbreaker.furnace.scan_objects` 5.2 and 5.0 million in 30 s (about
174,000 objects a second), `agriculture.scan_objects` 0.68 and 0.73 million,
`framework.crew.discovery` 3.5 and 5.3 ms per second with 12 and 21 ms maxima.
The existing scopes attribute little of the frame time: the costs of this pass sat
in unscoped hooks.

### Stage 2: Framework 0.45.0 and Agriculture 0.21.0 (FF1, FF2, FF4, FF5)

| Finding | Evidence | Decision |
|---|---|---|
| F-B two `CondTrigger.Triggered` postfixes | Up to three string lookups per call on the game's most frequent method; the section postfix ran even on false results | One hook, `Construction.TriggerRefinements`, returns on a false result, a null name or an empty table before any lookup; selectors register into one merged table rebuilt with each registration |
| F-A crew task filter | `Admissible` ran two A* searches per task per crew member per AI turn, plus two more per other crew member; keys were rebuilt as strings per check | Cheap facts first, then `Path` and `Prepare`, each memoised per step (`StepMemo`); keys built once per step per job; `Admit` still checks fresh, so withholding semantics are unchanged |
| F-C `NativeFluidRoute.Find` | Full ship scan with the six-condition endpoint check before the cheap segment test; a list and a query per visited cell; irrigation ran one search per other supply per power step | Segment test first and reused buffers in the uncached search; `FluidRouteCache` keeps one topology per ship and family for two real seconds with native invalidation, and answers the second-source rule by component membership. Endpoints stay fresh. This supersedes the P3/P4 deferral for fluid topology only |
| F-D RCS prefixes | Three collections and a pass array per fuel query; a lock and a closure per object; a substring per species | One input list per ship per step, a static pass order, a copy-on-write feed array and a precomputed species table |
| F-E, F-K bulk vessels and buffered draws | `Snapshot` read the record twice and threw on a protected vessel every poll; each settle wrote a log line | `TryRead` and one journal read; protected vessels remembered until accepted; settle lines at Debug level |
| F-F saved state | Character-by-character queries per validation; every save replaced the map | Loops; `TryWriteIfChanged` and `Status`; `SaveBoundary` for cadence-settled records |
| F-G Ship's Water | Plugin enumeration with reflection, a full ship scan and a quadratic sum per rack per power step, even for a zero request | Zero requests return at once; the plugin answer is remembered after world load; installed tanks per ship reread every two real seconds; the pool summed once; capacity fields cached |
| F-H, F-I, F-J, F-M alarms, receipts, skip, study | Six concatenations per sensor run; a weak-table probe per gather; reflection per skip step; per-skill concatenation per completed interaction | Static name tables, a pending count, compiled accessors |
| A-A, A-B Agriculture hooks | About 24 allocations per true native offer before our object was checked | Set lookups first; the ladder prefixes built once |
| A-C, A-D irrigation and saves | Routes twice per step, a SHA-256 per pump step, six record replacements per tick | One route per pair per step through the cache; the route key hashed per distinct path; changed-only writes; the world scan without query allocations |

Trade-offs taken (owner-accepted): a pipe or tank change is seen within two real
seconds; log verbosity for settled draws. No saved format changed.

### Stage 3: Shipbreaker 0.41.0 (FF3, FF4, FF5)

| Finding | Evidence | Decision |
|---|---|---|
| S-A `Powered.UsePower`/`Run` hooks | A state object, about 35 string compares and five weak-table removals per call for every powered object in the world | `PowerKinds.Classify` remembers each definition's kind; a foreign appliance costs one probe and no state; the finalizer keeps accounting witnessed partial delivery for our machines |
| S-B furnace scan and saves | Every quarter second a query over every world object; every furnace-family object saved about four times per power step, coolant charge included; artwork paths rebuilt per pass | Discovery every two real seconds plus the mode-switch hook; passive physics every quarter second as before (P3); `Flush` on a two-second cadence, on faults and at the save boundary with changed-only writes; artwork paths built once per appearance |
| S-C coolant route | Nine to eleven route searches per furnace power step, each with two ship scans and one more search per other endpoint | One route per furnace and endpoint per step through `FluidRouteCache`; second-source rule by component membership; one ship scan per step to list other endpoints |
| S-D routing double validation | `AfterPower` re-ran the whole admission it had just passed | A true admission holds for its step, guarded by the item and clock still being present; a false verdict is always recomputed |
| S-E, S-F, S-G console, reclamation, hooks | Every card's detail built each refresh; target parts resolved by scanning the target ship; name searches on every native offer | Lazy card detail (P2 pattern); id lookups; our-object test first |

Deferred: status strings for active routes still format per step (one call each), and
`CollectorRoute.Valid` still walks its route tiles per admission. Trade-offs: up to
two real seconds of furnace record progress on a crash; conduit changes seen within
two seconds.

### Stage 4: Manufacturing 0.6.0 (FF3, FF4, FF5)

| Finding | Evidence | Decision |
|---|---|---|
| M-A two-second world scan | Two queries over every world object, each classifying against fifteen store sizes with a closure; about 80,000 allocations per pass with no Manufacturing item built | `GasStores.For` through the definition index; one plain pass, one probe per object |
| M-B power hooks | A state object and four family checks per hook per appliance, eight more in the finalizer | `MachineKinds.Classify` remembered per definition; null state for foreign appliances |
| M-C per-step paths | Filler `NextJob` up to eight times per step; processor and reactor listing every candidate aboard to test one linked canister; reasons formatted on success paths; the refinery's feed looked up three times and its working line formatted twice per step | One job kept per step; direct candidate tests; reasons formatted on refusal only; one vessel read per check; feed looked up once; working line cached per recipe and language |
| M-D, M-E game-time rechecks | Manifold, filler, cell, reactor, refinery and regulator rechecks keyed on the game clock, every frame at high speed; gas-line searches uncached for the regulator | `Cadence`/real-time rechecks; `GasLine` through `FluidRouteCache`; the manifold's draw reason cached per language; regulator records changed-only |
| M-F, M-G hooks and panels | Name searches on every native offer; a ship scan per store per panel refresh | Our-object test first (`MachineKinds.IsOurs`); machines aboard listed once per step |

Deferred: idle machines still take their idle receipt through `RoomHeat` each step
(the idle heat share is part of the accounting). Trade-offs: gas-line changes seen
within two seconds; missing links rechecked every five real seconds.

### Stage 5: Auto Nav 0.28.0 (FF5, FF6)

| Finding | Evidence | Decision |
|---|---|---|
| N-A foreign-controller reflection | `AutoDockBusy` resolved an assembly-qualified type on every call (guard and tick, once per frame while flying); the Approach Assist check resolved a type and two properties on every admission check and panel read | Both resolved once per session; live values still read per call (`autonav.foreign_controller.check`) |
| N-B contact reads per step | The guard read every ship in the system, the previous threats again, then the target again in the tick, fire control per weapon and the hazard sweep per rock; each read walks every celestial body for occlusion and builds a native signature | `NativeContactReader` keeps one reading per (observer, target) for the open step through `StepMemo`; the service opens and closes the step at the physics boundary; Auto Nav's own sensor switches invalidate; reads outside a step are fresh (counter `autonav.contact.reads`) |
| N-C repeated verdicts | `HardwareProblem` (five conditions, tow check, RCS reserve), `FlightBindingValid` (a module search) and `GetRCSRemain` ran two to four times per step | `NavigationStep`: one verdict per step for the same console, flight record and ship |
| N-D docked partners | `TowFlight.Contains`/`RadiusAU` asked the game for the docked-partner dictionary per other ship and per obstacle | One partner list and envelope radius per ship per step |
| N-E record writes | `PersistProgress` encoded and rewrote the saved map every step | Settles every 2 s real, on every mode or engagement change, at commit points and on `SaveBoundary`; `Valid` and the envelope `Status` still checked every step so a corrupt record aborts at once (`autonav.persist.write`) |
| N-F sweep allocations | Lists, four hash sets, `ToArray` of the ship table and of previous threats, LINQ `Where/ToList/OrderBy` per step; the hazard scan allocated an iterator and a candidate list | Persistent buffers cleared per step; `NativeHazards.Asteroids` fills a caller's list (`autonav.guard.update`, `autonav.hazards.scan`) |
| N-G native hooks | The weapon hooks copied and filtered the game's list on every `ShootAuto`/aim call with no lease; `Yield` allocated the invocation list on every `Ship.Maneuver` of every ship | Idle early return; handler array rebuilt on subscription |
| N-H housekeeping | `ReconcileSensors` decoded the flight record every frame while a holder existed (indefinitely with a suspended flight); `TargetRef` looked up its generic name on every construction | 0.5 s real cadence; lazy name |

Deferred: fire control's `Observe` still builds its weapon readings with LINQ each
step while a fire console is bound (combat only); status strings for active flights
still format per step. Trade-offs: up to two real seconds of elapsed flight budget on a
crash (never on a save); contact readings and hardware verdicts shared within one
physics step; sensor release decided twice a second.

### Stage 6: War Declared 0.1.1 (FF7)

| Finding | Evidence | Decision |
|---|---|---|
| W-A poll allocations | Every two real seconds: a LINQ pass and array over the damage marks, a LINQ pass and array over the world's ships | Reused lists; the damage sweep runs only when marks exist (`war.poll`) |
| W-B schematic facts | `Facts(part)` rebuilt a conditions set from three native lookups on every sort comparison (`Rank`) and again per evaluation, every poll while sites were pending | One `PartFacts` per part until content reloads; `Reset` clears it |
| W-C blocked sites | A site refused because the player held an item or the ship was not editable was retried on every poll, re-sorting and re-evaluating the whole pending list | `WarRules.RetrySeconds` (10 s real) per ship after a retry disposition; stand-down, a new loss in combat and Lay held clear the wait (`war.lay_pending`) |

Not changed: the damage-check and mode-switch hooks already return on a dictionary
probe; the poll cadence (2 s real) was already right. Trade-off: a blocked build
site waits up to ten real seconds after the item is put away.

### Stage 7: installer, records and evidence

- The retired Approach Assist 0.1.2 prototype (`BepInEx/plugins/PhobosApproachAssist/
  PhobosApproachAssist.dll`, SHA-256 `EC4841B5…77A8`) was still loading beside Auto
  Nav on the owner's machine, and its patches ran every frame. The installer's
  retired-file catalogue now accepts an entry with a `folder`: when Auto Nav is
  installed, that folder is archived and removed if every file in it is a listed
  exact-hash retirement; anything else there stops the update. Same backup, receipt
  and game-closed guards as other retirements (`tests/install-mods.tests.ps1`).
- The captures guide lists every scope this pass added and advises summary mode at
  fast-forward. The ledger is fully stamped (FF0 to FF7) and CI verifies it.

### Defect found by the after-captures: Framework 0.45.1

The first after-captures (30 September, builds 0.45.0 / 0.28.0 / 0.41.0 / 0.21.0 / 0.6.0) had no frame-time
samples and no crew-discovery calls. The Unity player log showed why: Framework 0.45.0 threw an
`AmbiguousMatchException` while installing its patches, because `Ship.AddCO` has two overloads and the pipe
cache's patch named it by name alone. Framework's start-up stopped there. Those captures therefore show the
other mods' changes but not a working Framework, and their frame percentiles do not exist. 0.45.1 names both
overloads. The native suite now resolves every attribute-declared Harmony patch in all six plugins to exactly one
game method (`PatchResolutionChecks`); the check was shown to fail on the 0.45.0 patch and pass on the fix. The
lesson for this audit: the offline suites never start a plugin, so patch resolution is checked by reflection, and
an after-capture is only valid when the BepInEx or player log shows every plugin starting without an exception.

### Stage 8: after-captures with 0.45.1 and the shared world sweep (Framework 0.46.0)

Two valid after-captures (30 September, 07:44; speed 8, navigation console closed; the player log showed every
Phobos plugin starting without an exception) against the two baseline captures:

| Measure | Before | After (0.45.1) |
|---|---|---|
| Median frame | 24.8 / 25.9 ms | 24.2 / 20.3 ms |
| 95th percentile | 125 / 140 ms | 124 / 45 ms |
| Frames over 100 ms in 30 s | 64 / 63 | 62 / 32 |
| Frames rendered in 30 s | 822 / 732 | 866 / 1,130 |
| Measured Phobos time per real second | about 37 ms | about 15 ms |

The furnace update fell from about 30 to 3.8 ms per second and crew discovery from 3.5 to 5.3 down to 1.5 to 1.8.
The frames over 100 ms recur every 0.1 to 0.7 s, far more often than any Phobos pass runs, so most of that time is
outside the measured code: the game's own simulation at speed 8 on a very large save (the Little Patch recovery
log reports its save JSON above 512 MiB), other mods, or Phobos hooks inside native methods that no scope covered.
Only two matched runs were taken on each side, and conditions differed between the two after-runs.

What remained measurable was three full passes over every world object (about 49,000): Agriculture's machine
scan, Manufacturing's store and regulator scan and the furnace discovery, each about 8.5 ms every two real
seconds. Decision, revising P3 and P4 for these three scans: Framework's `Discovery.WorldFamilies` (pure logic in
`WorldIndex<T>`) runs one sweep every two real seconds spread across frames, with a full pass right after a load.
Lifecycle equivalence: every read rechecks each member (alive and registered in `DataHandler.mapCOs` under its
current id), so removals are exact; the game adds objects to that map in many places (a local IL survey found
more than a dozen writers), so additions are found by the sweep rather than hooks, within one to two cycles, or at
once through `Offer`. Trade-off: a newly placed machine is picked up within about four real seconds instead of two.

To find the rest, Framework 0.46.0 adds capture probes: timings of `CrewSim.Update`, `CrewSim.AdvanceSim`,
`StarSystem.Update`, `Powered.Update` and `Interaction.TriggeredInternal`, the total of every mod's postfixes on
the last, and a count of `CondTrigger.Triggered`. Seven Phobos postfixes sit on that offer check (one per mod and
three in Shipbreaker); each exits after a dictionary probe or two, so their total is bounded by the probe. The
probes are patched in when a capture starts recording and removed when it stops (their own Harmony id), so they
cost nothing in ordinary play; the native suite resolves their targets by exact signature. The next captures
decide whether the long frames sit in the game's own simulation or in hooks worth consolidating.

### Stage 9: what the capture probes showed (Framework 0.47.0)

Three captures with 0.46.0 (30 September, 08:11 to 08:13; speed 8, navigation console closed; every Phobos plugin
started cleanly). The owner had disabled George Dorn's Orbital Trajectory Fixes (Workshop 3801852103) for unrelated
trouble, so these runs are not like-for-like with the baseline. The probe counters wrote one record per call and
filled the 20,000-record limit, so the frame samples were lost (fixed in 0.47.0); the timed operations are
aggregates and are complete.

| Per real second | Run 1 | Run 2 | Run 3 |
|---|---|---|---|
| `game.crewsim.update` (the game's whole main loop) | 452 ms | 327 ms | 356 ms |
| `game.sim.advance` (crew and object simulation) | 377 ms | 244 ms | 280 ms |
| Worst single simulation step | 180 ms | 178 ms | 196 ms |
| `game.interaction.offer_check` (about 215 calls a second) | 133 ms | 90 ms | 98 ms |
| `game.starsystem.update` | 69 ms | 76 ms | 70 ms |
| `game.powered.update` (about 14,000 calls a second) | 22 ms | 24 ms | 23 ms |
| All measured Phobos scopes | about 10 ms | about 10 ms | about 10 ms |

Findings. The long frames are the game's own crew and object simulation: single `AdvanceSim` steps of 180 to 196
ms at speed 8 on this save. All mods' postfixes on the offer check summed to a fraction of a millisecond over each
capture, so the seven Phobos hooks there are negligible. Appliance updates cost about 1.6 microseconds each
including every hook. The scans replaced in stage 8 now cost microseconds; the shared sweep costs about 7 ms per
second with a peak of about 6 ms when it snapshots the world.

The trigger check (`CondTrigger.Triggered`) runs about 400,000 times a second (about 20,000 counter records filled
in the first half-second). Framework's single postfix there did a dictionary lookup for every passing trigger.
Decision: `NameFilter`, an exact negative test on name length and first character built from the registered
selectors (`PhobosCraftSelect_`, `PhobosCraftStation_`, section job ids), returns before any hashing for the game's
own names (`TIs...`, `Blank`); the table still decides every name that passes. The sweep snapshot now uses the
collection's bulk `CopyTo` instead of an interface enumeration. Neither change alters results.

What is left is not ours to change: the game's simulation cost at speed 8 on a very large save. The next captures,
with frame samples restored, show the frame times with these fixes.

### Follow-up: the game's ship-update exception and Auto Nav 0.29.0

The owner's session after stage 9 logged seven `NullReferenceException`s thrown by `StarSystem.Update`'s own body
(the stack shows only the patched method and `CrewSim.Update`), each in the frame the game spawned an NPC ship in
the background, and each stopped Auto Nav through its finalizer. The previous session with the same Phobos builds had
none, and no capture was running, so neither the performance pass nor the capture probes caused them; the game's
update dereferences ships, stellar objects, the selected crew member and the player's ship without null checks.
Auto Nav 0.29.0 rides out a failure whose stack holds no Auto Nav frame (one step of held thrust, three within ten
real seconds, then a resumable suspension) and still stops on a fault in its own code. The same session's load
warnings (`Could not deserialize System.String[]`, `Blackboard Deserializer, found unknown type: System.Char`) come
from the game's `Ostranauts.Core.Blackboard`, which saves a text value as a list of characters and cannot read it
back; no Phobos mod uses that store.

### Data packs (Framework 0.49.0, Manufacturing 0.11.0)

The data-pack loader (`Data/DataPacks.cs`, `Data/EconomyPack.cs`) reads each
mod's shipped pack and the player's override files once per content load, inside
`Content.Prepare`, and never afterwards: no polling, no file watching, no
per-frame work. Manufacturing's economy tables moved into its pack with the same
values (the golden item export is unchanged). Reviewed as R4 (load-time
registration work); the touched Framework console, lifecycle and plugin files
carry the same finding.

### Cadence policy

Conserved accounting (power receipts, transfers, thermal and crop steps, elapsed
flight budgets) stays on game time inside the native hooks. Topology, discovery,
candidate lists, presentation and record settlement run on unscaled real time:
2 s for topology and settlement, 5 s for machine link rechecks, 10 s for blocked
build sites, with explicit invalidation where a cheap native hook exists (mode
switch, destroy, ship add/remove, our own sensor switches, content reload).

### Trade-offs taken (owner-accepted)

- Crew task admissibility is shared within one frame; supply scans for starved
  machines are shared per poll.
- Pipe, gas-line, coolant, floor and water-tank topology changes are noticed within
  2 real seconds (at once where a native hook fires); at high speed that is up to
  about 100 game-seconds of flow along a route that just broke, bounded by pump
  rate and still journaled; no mass is created.
- Furnace, Auto Nav, Agriculture and Manufacturing records settle every 2 real
  seconds, on every state transition and before every native save; a crash (never
  a save) can lose up to 2 s of record progress or elapsed flight budget.
- Contact readings, hardware verdicts and docked partners are shared within one
  physics step; Auto Nav sensor release is decided twice a second.
- Manufacturing links and vessels are rechecked every 5 real seconds; War Declared
  retries blocked build sites every 10 real seconds.
- Settle log lines go to Debug level; journals are unchanged.

### After-capture protocol (owner)

Install the new builds with the game closed (`scripts/install-mods.ps1`; the
installer also archives the prototype DLL), load the same save and scene as the
baseline, speed 8, navigation console closed, and record two or three captures:

```text
phobosframework perf start summary 30 20000
phobosframework perf stop
```

(Since Framework 0.75.0 each 30-second window is exported as it ends, until stop;
for the 29 September protocol, let it run three windows before stopping.)

Then compare: `python scripts/compare-performance.py --before <baseline files>
--after <new files> --output <report.json>`. The report gives frame percentiles,
long-frame counts and per-scope cost per second; the new scopes attribute what the
after-captures still spend. Expect `shipbreaker.furnace.scan_objects` and
`agriculture.scan_objects` to fall to zero or near it, and
`framework.crew.discovery` to shrink; the frame percentiles are the outcome that
matters. Ordinary-play checks after the install: crew loading orders still fetch
within seconds; furnace passive cooling continues with the panel closed; irrigation
and coolant routes still refuse a joined second source; a freshly laid or cut pipe
is noticed within two seconds; an Auto Nav flight resumes after reload with elapsed
time within two seconds; the BepInEx log no longer lists Approach Assist. Offline
checks are not gameplay validation; the after-captures and these checks are.

## 28 September: construction and maintenance follow-up

R10 — Section assembly uses native work scheduling, hauling and saved lots. There
are no new frame loops, equipment scans or automatic restarts. Instructions build
only on explicit opening; maintenance reasons reuse the existing service checks.
The section selector hook uses a dictionary fast path. Cooling's native admission
hook performs an allocation-free empty-list check during ordinary operation;
its captured-cargo set exists only during a synchronous damage/repair transition.
No repeat performance capture or measured FPS claim accompanies this change.

## 30 September: data packs, faction kiosks and the shared charge machine (K1)

K1 — Static review of the 30 September changes: the equipment, economy, recipe and
faction-kiosk data packs (Framework 0.52.0 to 0.54.0), the faction-kiosk tier marks
(Framework 0.53.0) and Manufacturing 0.17.0's shared charge machine. Pack loading,
validation, recipe freezing and the kiosk trigger amendments run once at start-up;
kiosk tier stamping runs only while a kiosk generates stock. The V4's power, feed,
panel and maintenance paths moved unchanged into one engine; power and feed hooks
resolve the machine through a memoized definition-id dictionary (one probe per call,
the same cost as the old family test), and the shared commodity settlement runs once
per finished charge. Manufacturing 0.18.0 adds the LC-3 as a second engine
instance on the same paths (the registry probe stays one dictionary lookup per
definition id) and one more mining carve applied at preparation. Agriculture 0.27.0
adds the nutrient hopper: a W2 resolves a selected hopper by id on its power step
(one dictionary lookup and the hopper's record), and the ship-wide list of hoppers
within one tile is built only when a player or crew member chooses a source, never
per step. Manufacturing 0.19.0 adds the SA-3 as a third engine instance on the
same paths and the AT acid tanks: a tank's panel lists its linked machines through
the gas stores' once-per-step machine list, its pour targets are listed only when
the panel or console builds the field, its damage and destruction hooks test the
family with one dictionary lookup, and the mist is a single event on damage, not a
per-step leak. Two more mining carves apply at preparation. Manufacturing 0.20.0
gives the LC-3 two more links on the same engine paths (candidates listed only
when a panel or console builds its fields, peers resolved by saved id), and its
requirement gate is a plain predicate with no allocation per check. No new scans,
cadences or per-frame work. No performance capture or measured FPS claim accompanies this
change.

## 30 September: panel refresh and interface fixes (U1)

U1 — Static review of Framework 0.55.0, Manufacturing 0.21.0, Shipbreaker 0.51.0,
Agriculture 0.28.0 and Auto Nav 0.31.1. The control panels now rebuild their page
when a configuration sheet closes after an Apply and after a player command: one
rebuild per click, the same work as opening the page, never per frame (the 0.5 s
text refresh is unchanged). Selection sheets receive the current choice id the
fields already computed. Console group headings look up a provider-registered
label in one dictionary probe per heading while the list is built. The
`phobosframework loot` report reads one loot table on demand. No new scans,
cadences or per-step work. No performance capture accompanies this change.

## 30 September: line networks and shared foundations (L1)

L1 — Static review of Framework 0.56.0 with Manufacturing 0.22.0, Shipbreaker 0.52.0
and Agriculture 0.29.0. The fluid topology now builds every registered line family
for a ship in one object scan instead of one scan per family (three families today,
up to six planned), so the scan count drops as families are added. A new snapshot's
recheck starts consumed, removing the second rebuild every invalidation used to
cause, and a mode switch invalidates only its own ship and only when the object is a
segment, a participant, a floor or a wall before or after the switch: door cycles,
crew face changes and unrelated equipment no longer flush every ship's snapshots.
Network families add participants (one extra predicate per scanned object and a
pairwise touching test over the few participants per family); union-find replaces
the breadth-first component pass. Participant network steps are one search per
source participant per snapshot, remembered. `LineReach` answers from the snapshot
with fresh endpoint checks; candidate lists are built only when a panel or console
builds its fields. The joint redraw postfix on `Ship.UpdateTiles` is one dictionary
probe for every placement and does work only for equipment with ports on more than
one line family; the PDA filter postfix runs when the player changes filters. Load-
time definition conversion is one dictionary probe per saved item and saved object.
No performance capture or measured FPS claim accompanies this change.

## 1 October: shared lines and vessel links (L2)

L2 — Static review of Framework 0.57.0 with Manufacturing 0.23.0, Shipbreaker 0.53.0
and Agriculture 0.30.0. Every machine-to-vessel check on a power step now goes through
`VesselLink.Connected`: the same two saved-record reads the reciprocal pair check
made before, then `LineReach` (the touching test first, then two dictionary probes
and a union-find comparison on the ship's cached snapshot). No new scan runs on a
power step. The two shared families add their participants to the one object scan
per ship; port lookups are a dictionary probe per scanned object. Candidate lists
test reach per registered vessel of the commodity and read each candidate's record
for its full or empty mark, only when a panel or console builds its fields. The
P1, L2, A2 and store transfers answer from the snapshot instead of a bounded route
search per store, so their five-second recheck gets cheaper. Manufacturing's existing
two-second scan adds one pass over installed oxygen and fuel stores, grouped by ship,
each asking its gas network's members (a pass over that ship's gas participants).
Framework's own items publish once per content load. No performance capture or
measured FPS claim accompanies this change.

## 1 October: one water silo ladder (L3)

L3 — Static review of Framework 0.58.0 with Shipbreaker 0.54.0 and Agriculture 0.31.0.
The water tanks are passive: no ticker, no power interface and no per-frame work;
their records are read when a panel, console or linked machine asks, as the
Shipbreaker silos were. Moving them to Framework changes the owner, not the work.
Loading a save adds one dictionary probe per spawned object's definition id; only
a retired reservoir is converted, once, with its conditions compared against two
definition lists. The crew water reserve setting reads Shipbreaker's configuration
file once, the first time it is needed. Agriculture's crew provider now also
accepts the tanks, one more prefix test per candidate it already examined. No
performance capture or measured FPS claim accompanies this change.

## 1 October: Ship's Water tanks on the water line (L4)

L4 — Static review of Framework 0.59.0 with Agriculture 0.32.0. Content load amends
twelve Ship's Water definitions once. The Ship's Water tanks become process-water
participants, so the ship's one object scan tests one more port list per tank. Every
refill or deposit filters the per-ship tank list (still reread every two real seconds)
by `LineReach`: a touching test, then two dictionary probes and a union-find
comparison on the cached snapshot, per tank. The rack and W2 run that filter on the
power steps they already used to refill; with Ship's Water absent it is skipped. A
silo's status counts reachable drinking and waste tanks only while its panel or the
console asks. No performance capture or measured FPS claim accompanies this change.

## 1 October: the acid line (L5)

L5 — Static review of Framework 0.60.0 with Manufacturing 0.24.0 and Shipbreaker
0.55.0. The acid line is one more family in the ship's single object scan, with
twenty ported definitions. Its hooks add a definition-id comparison to every
`ModeSwitch` and `Destroy`; only an installed acid segment goes further, reading
the cached snapshot once and each member tank's saved links. The removal refusal
runs that same read only when uninstall or dismantle work is offered on a segment.
Pouring filters tanks aboard by `LineReach` instead of the touching test alone.
No performance capture or measured FPS claim accompanies this change.

## 1 October: conveyor belts (L6)

L6 — Static review of Framework 0.61.0 with Shipbreaker 0.56.0. The belt is one
more family in the ship's single object scan (a definition-id comparison per object).
A route check that walked a bounded floor search on every power step now asks the
cached snapshot about the few cells beside each endpoint (a handful of dictionary
probes) after the touching test, so it is cheaper than before. Resume after a
reload is one condition read per receiver per step until the first attempt. Storage
output lists the tray's units with stacks opened, as the crew orders already do. No
performance capture or measured FPS claim accompanies this change.

## 1 October: stacks, store footprints and the belt display (L7)

L7 — Static review of Framework 0.62.0 with Shipbreaker 0.57.0. Routed receivers list
the sender's units with stacks opened (as storage output and crew orders already
did), one short list per admitted step. A store's cells grow from two to its
footprint, a handful more dictionary probes when a route is found or rechecked. The
belt display adds one quad per active belt transfer, a path found once per route
and one eased position per frame for each shown item; with no belt transfer running
it costs one empty check per frame. No performance capture or measured FPS claim
accompanies this change.

## 1 October: lines that hold their contents (L8)

L8 — Static review of Framework 0.63.0 with Manufacturing 0.25.0.

- **Top-up pass.** Every two real seconds, for each loaded player-owned ship, a new
  pass (`framework.line_contents.maintain`) reads the cached topology snapshot for each
  holding family. The snapshot now also keeps the segment object per carrying cell.
  - The pass reads segment records only in runs that have a store on them, and writes
    only segments whose record changed, so a full line costs record reads and no writes.
  - Canister pouring scans the ship's objects once per pass and looks inside the
    containers of registered stores.
- **Topology scan.** The scan adds one dictionary probe per segment for the closed flag.
- **Hooks.** Mode switches, destruction, and offers and completions of interactions each
  add a definition-prefix test. For the removal refusal, only an uninstall or dismantle
  action name reaches it. Draining, venting and reopening walk the physical run once,
  when the crew action is offered and when it finishes.

No performance capture or measured FPS claim accompanies this change.

## 1 October: pumped circuits and the coolant conduit (L9)

L9 — Static review of Framework 0.64.0 with Shipbreaker 0.58.0. A serviced F6 asks
once per power step for its circuit's segments, from the cached snapshot and the
route it already finds, memoised for the step. It then reads their records for
fullness, and writes only when priming moves coolant. The top-up pass now skips
content-filled families at once. Canister pouring asks each registered receiver
about an installed container that is not a bulk vessel: a definition test per such
object on the existing two-second scan. No performance capture or measured FPS
claim accompanies this change.

## 1 October: the irrigation conduit holds its feed (L10)

L10 — Static review of Agriculture 0.33.0 with Framework 0.64.0.

- **Per receiving branch and power step, the W2 now does:**
  - one pass over its connected run from the cached snapshot (the route it already
    finds);
  - a record read per segment for room;
  - writes only while priming.
- **Removed:** the route-key hash and the parcel's per-step save.
- **Once the run is full**, delivery is the same guarded mixture transfer as before,
  one per branch.

No performance capture or measured FPS claim accompanies this change.

## 1 October: Ship's Water line filling and the feed flush (L11)

L11 — Static review of Framework 0.65.0 with Agriculture 0.34.0. The two-second
top-up pass now also tests each water-line participant for Ship's Water drinking
tanks (one trigger check) and, on a run that wants water, sums the ship's
drinking tanks from the existing two-second tank list. A W2 branch step reads
each run segment once to find old feed; a flush writes only while old feed
remains. No performance capture or measured FPS claim accompanies this change.

## 1 October: the mining laser (L12)

L12 — Static review of Framework 0.66.0 with Shipbreaker 0.59.0 (the Ablatine
ML-2 mining laser). The power hooks classify the laser by definition id through
the existing per-definition cache; a laser with no session costs one dictionary
probe and one condition test a power step. A started laser steps once a game
second. While a cut is in hand it resolves one object by id, re-checks one line
of sight (at most 48 tile samples) and tests the people aboard both ships
against the beam path. It scans the moored ship's objects, and both ships for
mooring ports, only when it looks for the next cut: once per finished cut. The
gangue rule is cached per damage-loot name. The sweep record is written when it
changes, which while cutting is once a powered second, as the G4's is. The beam
is one quad per firing head, repositioned each frame while visible; the firing
sheet uses the game's own per-frame step. A new operation metric,
shipbreaker.laser.update, covers the step. No performance capture or measured
FPS claim accompanies this change.

## 1 October: retired section assembly and the legacy sweep (L13)

L13 - Static review of Framework 0.67.0 with Shipbreaker 0.60.0 (the D4, R4 and
F6 come whole; saved sections convert). The legacy-item sweep is polled from
Framework's update and does nothing until a content mod registers a rule. Then,
once every 15 real seconds, it lists each loaded player-owned ship's objects
(including contained ones) once, tests each for a placeholder site or a retired
definition id, and collects live site lots into one set. Conversion work happens
only when a retired part is found, so after the first sweep of a converted save
the pass is one object listing per ship per 15 seconds. Section sites keep their
existing per-site appearance view; whole-machine install sites gain the same
bounded view (at most ten checks a real second per site). INSTALL retirement and
table-offer removal run once at content confirmation. No performance capture or
measured FPS claim accompanies this change.

## 1 October: the laser's radiator link (L13)

L13 — Static review of Shipbreaker 0.61.0. A started laser now checks its paired
cooling assembly once a game second in its existing step (one saved-link read,
one object lookup, the assembly's own mounting test) and keeps the result for
the power steps, which only read the assembly's room. The assembly's store is
written through the furnace service's existing settlement cadence. The panel
reads the same check when it is shown. No performance capture or measured FPS
claim accompanies this change.

## 1 October: refining as a business (L14)

L14 — Static review of Framework 0.68.0, Manufacturing 0.26.0 and Shipbreaker
0.62.0. The kiosk buy-back runs only while the player has the Bulk supplies view
open, enumerating sale lines and eligible stores on demand like the purchase side;
a sale is one journalled record write. The load-time price refresh adds one set
lookup to each object's `CondOwner.SetData` and clones a save DTO only for a
registered material or methane ice whose saved price differs, once. The V4's
recipe availability now builds a small set of superseded revisions per call (eight
recipes); it runs where feed admission and charge matching already enumerated the
catalog. No per-frame work is added. No performance capture or measured FPS claim
accompanies this change.

## 1 October: reactors that feed each other (L15)

L15 — Static review of Manufacturing 0.27.0. The V4's automatic match now asks,
per candidate recipe, whether each gas it draws has a linked store: one saved-link
read per drawn gas, only when the machine binds a charge (on Start or after a
finished charge), not per power step. The V4 lists five gas links instead of two
on its panel, built when the panel is shown. The A2's carbon dioxide feed adds one
room-gas read and, when dosing, one store draw to its existing two-second tick.
No performance capture or measured FPS claim accompanies this change.

## 1 October: any touching pipe joins (L16)

L16 — Static review of Framework 0.69.0, Shipbreaker 0.63.0, Manufacturing 0.28.0
and Agriculture 0.35.0. The topology scan now gives each line participant its
footprint cells and their four neighbours instead of one port cell: at most 25
tile lookups and 45 join cells for a 5 x 5 tank, computed once per object per
rebuild and shared by every family it has a port for. The rebuild cadence is
unchanged (every two real seconds, or when a segment, participant, floor or wall
changes), and the union-find is linear in those cells. More stores can sit on one
run, so the line top-up pass, on its existing two-second cadence, may fill runs
that were dead before; it stops when they are full. The "not offered" note is a
function read only when a setting's sheet opens: one ship object scan and a few
snapshot lookups per vessel aboard, never per frame and never while a page of
fields is drawn. The Details line is built when that page is shown. Fields that
now appear whenever a vessel of their cargo is aboard add one ship scan per field
when a panel page is drawn. The T2 and ML-2 panels change host only. No
performance capture or measured FPS claim accompanies this change.

## 1 October: inventories sized to the job (L17)

L17 — Static review of Framework 0.70.0, Shipbreaker 0.64.0, Manufacturing 0.29.0
and Agriculture 0.36.0. Inventory roles are recorded once per content load, one
dictionary entry per definition. The load-time fit (`ContainerFit`) is polled on a
fifteen-second real-time cadence, and each of the player's ships is swept once after
it loads: one object scan, one dictionary probe per object, and a planner pass only
for declared containers that hold something. A ship is swept again only while a
container on it was deferred (locked, or its window open). After that the poll costs
one weak-table probe per owned ship every fifteen seconds. The planner is quadratic
in a container's cells and contents, bounded by the old 8 x 8 grids (at most 64
items against 64 cells, once). The full-rack message in Agriculture reads one grid
after a failed crew action. No per-frame work is added. No performance capture or
measured FPS claim accompanies this change.

## 1 October: products delivered into stacks (L18)

L18 — Static review of Framework 0.71.0, Shipbreaker 0.65.0, Manufacturing 0.30.0
and Agriculture 0.37.0. A delivery now reads the tray's stacks once (one kind test
per stack and member: a few condition lookups and, for recorded units only, a string
built from their records) and plans in one pass over the batch. It runs when a batch
finishes, never per frame, and replaces a plan that reserved one cell per product.
Fewer objects sit in trays as heads, so the tray window, crew searches and the
container scans that enumerate heads handle fewer entries. The fit pre-check a
machine makes while it waits for tray room reads definitions only and creates no
objects. A T2 waiting on a full tray no longer creates a gangue object every five
seconds and leaves it unreferenced: it destroys the unplaced one (a real leak, found
in this review). The load-time fit merges units without a cell one at a time, once
per ship load. No performance capture or measured FPS claim accompanies this change.

## 1 October: static optimisation pass, first round (L19)

L19 — Owner request (1 October 2026): a performance optimisation of every Phobos
Ostranauts mod. This round is a static review of all six mods, with only the
changes that are safe from reading the code. No capture was taken (the owner was
away); the last valid captures are still stages 8 and 9 (Framework 0.46.0 and
0.47.0), taken before L1 to L18 added lines, links, belts and the load-time sweeps.
Framework 0.72.0, Shipbreaker 0.66.0, Manufacturing 0.31.0, Agriculture 0.38.0 and
Auto Nav 0.31.3.

### Changed in this round

| Where | What it cost | Now |
| --- | --- | --- |
| Framework `FluidRouteCache` destroy, add and remove hooks | A ship's line layout was dropped for any object (a door cycle, an item picked up, dropped or eaten); the next power step paid a full ship scan and topology build. L1's relevance filter covered only the mode-switch hook, and that hook tested the old object twice | Every hook tests relevance (segment, participant, floor, wall) and that the ship has a layout. The mode-switch postfix tests the new object. New counter `framework.fluid_route.invalidations`; the `framework.fluid_route.find` scope now covers the whole rebuild |
| Framework `LineContents.Maintain` | Rebuilt every owned ship's layout every two seconds before checking it had segments | Asks `FluidRouteCache.KnownEmpty` first; a ship without working segments is not read again until something changes it |
| Framework `DrainCanisters.Pour` | Walked every object of every owned ship every two seconds | Canisters come from the shared world sweep, once per pass |
| Framework `PortPairing`, `MaterialPort` | LINQ over id characters, a string concatenation and a closure on every port read (three or more per linked machine per power step) | Loops, the map key built once, shared immutable answers for unlinked and invalid |
| Framework `FluidTopology.Path` | A fresh search (delegate, dictionary, queue, list) for the same route on every power step | Remembered per snapshot |
| Framework hooks | `NativeRoomAlarms.MonitoredRoom` allocated a list and a closure per alarm sensor run; `WaterTanks.For` and `LineHoldUpFamily.IsForm` used LINQ closures; the legacy-finish hook hashed a key with an empty table | Reused buffer and loops; an early exit |
| Shipbreaker `Plugin.OnGUI` | Three empty immediate-mode GUI calls on every GUI event of every frame | Removed |
| Shipbreaker `FurnaceService.FlightCommand`, `ReclamationService.ManualTakeover` | A closure, and an array for the takeover, on every ship's every nonzero manoeuvre or burn | No allocation; an immediate return with no sessions |
| Shipbreaker capture, reclamation and laser `Update` | `ToArray` every frame | Nothing copied on a frame with nothing to do |
| Shipbreaker `MachineProblem` (D4, R4, T2), `Feed` | The feed bin fetched four or five times per check, with a closure each | Once, by loop |
| Shipbreaker `CoolantRouteNow` | A full ship object list four times a second and on every power-step frame per routed F6 | Furnace-family parts from the shared world sweep |
| Shipbreaker `FeedPatch`, `FurnaceFeedStackPatch`, `RoutingRules.Family` | Repeated container dereferences; an array and LINQ per family test | One read; plain comparisons |
| Manufacturing `ChargeMachines.ForBin`, `ChargeMachineSpec` | A closure, an enumerator and up to three string concatenations for every container admission and stack test in the game | Cached ids and a loop |
| Manufacturing `ChargeRecipe`, `ChargeMachine.FeedKg` | Recipe lists rebuilt with LINQ on every read; the available-recipe set rebuilt per bound unit per step | Derived once in the constructor; a feed table rebuilt only when a requirement gate changes |
| Manufacturing `SabatierService`, `CrackerService` `Linked` | Two vessel snapshots per check (about three record reads each) | One, handed back to the caller |
| Manufacturing `FillerService` | Its save reset the five-second back-off, so a station that could move nothing searched every step at working power; status text formatted per move | The back-off holds and the station stands down; one status per step; changed-only record writes |
| Manufacturing `StoreService.Ignites` | Three full ship passes every two seconds per damaged fuel store, even without oxygen | One pass that stops at the first source, only with enough oxygen |
| Manufacturing `ManifoldService` reserve and capacity | A LINQ closure every frame per ship | Loops |
| Agriculture `RecoveryService.SaveRecovery` | Rewritten on every save of every machine | Changed-only |
| Auto Nav `TickFire` | After the station was opened once, every physics step read hardware, preferences, the contact and every weapon, with the station closed and no target | An idle exit while nothing is aimed, permitted, targeted, held or in combat and the station is closed |

Checked and left alone: `SetCondAmount` with no change is one lookup and an early
return in the game's code, so guarding an idle machine's working flag gains
nothing (the Manufacturing guards added here cost the same and are harmless).
Caching `DockingAdapter`'s reflection lookups was tried and withdrawn: the docking
checks require a member that goes missing to refuse the clamp at once, and the
lookups run only on a ready approach.

### Found, not changed: needs a capture or an owner decision

| Finding | Why it waits |
| --- | --- |
| Manufacturing machine records are written with a full rebuild on every powered step (`ChargeMachine`, `ProcessorService`, `SabatierService`, `CrackerService`); Agriculture `Service.Save` runs several times per step; the laser and reclamation records likewise. The trade-off list above says these settle every two seconds, but only the furnace and Auto Nav do | Settling changes what a crash can lose (up to two seconds of progress), and a ship serialised without a save does not raise `SaveBoundary`. Owner decision, then a capture |
| Shipbreaker reclamation window selection: per window, a target-ship scan, a contact read of every ship in the system and two full grids, up to 64 windows in one step | Mission flight code; hoisting is safe in principle but the path has never been captured (R3). Capture `shipbreaker.reclamation.update` during a mission first |
| Shipbreaker and Auto Nav binding checks about four times per frame during capture, egress and transit (`CaptureService.BindingProblem`, `IndustrialProblem`) | A per-frame memo is low risk, but it is flight authority code; measure with a mission capture |
| Framework crew task filter: saved-record reads per task per crew member per search (`CrewSpecialities.Allowed`, `Skilled`, `CrewRoster.Members`) | Measured 1.5 to 1.8 ms/s in stage 9; a per-frame memo needs capture C3 |
| Framework world sweep, about 7 ms/s in stage 9 | A longer cycle delays discovery of new machines; capture C4 and an owner decision |
| Framework room-alarm recording on every sensor run, read only by the C1 console | Recording only for sensors lately read changes the first console open; needs a counter first |
| Framework legacy-item sweep every 15 s on every owned ship | It is how retired parts convert when they come aboard; gating it must keep the unfinished-site step |
| Running D4 and R4 jobs re-plan the tray fit on every power step; waiting collector and storage routes evaluate twice per step | A change-only recheck is a trade-off; measure |
| Agriculture conduit reads two or three times per branch per step; the W2 destination list built twice per step; hopper record read six to nine times per dosing step | Per-step memos are likely safe; measure the W2 with several racks first |
| War Declared rewrites its whole ledger for each destroyed part | Dirty-flag saves trade crash safety for the frame of a large explosion; capture one |
| Auto Nav panel reads at 10 Hz, the body-motion loop while idle, change-only canvas writes | Recorded as P2 and R2; measure before changing |

### Captures to take

Three matched 30 s summary windows per side (`phobosframework perf start summary 30
20000`, wait for three windows, then `phobosframework perf stop`; since Framework 0.75.0
each window is exported as it ends), same save and scene, speed 8, nav
station closed; check the BepInEx log and `Player.log` show every plugin starting.
Compare with `python scripts/compare-performance.py --before <3> --after <3>`.

| Capture | Scene | Reads |
| --- | --- | --- |
| C1 | A crewed ship with Phobos machines on lines, crew walking through doors | `framework.fluid_route.find` calls per second (expect about 0.5 per ship with a layout), `framework.fluid_route.invalidations`, frame percentiles |
| C2 | The usual save with several owned ships and little Phobos equipment | `framework.line_contents.maintain` |
| C3 | Several crew standing orders enabled, crew idle | `framework.crew.task_filter`, `framework.crew.path_checks` |
| C4 | Any of the above | `framework.world.sweep`, `framework.world.sweep_objects` |
| C5 | A capture or reclamation mission under way | `shipbreaker.reclamation.update`, `shipbreaker.capture.update`, `autonav.guard.update` |
| C6 | Several Manufacturing reactors and a V4 running, C1 console closed then open | `framework.state.write`, `framework.state.writes_skipped`, frame percentiles |

A before side needs the builds of commit 4fd18ee (Framework 0.71.0); the after
side is this round. No performance capture or measured FPS claim accompanies
this change.

## 3 October: empty-layout shortcut bounded (L20)

L20 — Correction to L19. `FluidRouteCache.KnownEmpty` (Framework 0.72.0) trusted a ship's
cached "no working segment" answer until a hook dropped the layout, without the
two-second recheck that every other read honours. A layout first read before the
ship's segments were ready (during loading) could leave that ship unfilled for good.
Framework 0.73.0 trusts an empty layout for at most 30 s (`EmptyTrustSeconds`), so a
ship without lines is still read about once every 30 s instead of every 2 s, and a
missed change is caught within that bound. The same release makes pipe segments count
over the game's floors for the first time (see the vanilla-precedence audit), so
`framework.line_contents.maintain` and `framework.fluid_route.find` will now do real
work on ships with lines; captures C1 and C2 of L19 should be taken on Framework 0.73.0
or later. No capture accompanies this change.

## 3 October: repair hook removed, retired-item sweep (L21)

L21 — Framework 0.74.0 (owner direction: repairs follow the game). `RepairRemainderPatch`,
a prefix on `Interaction.ApplyEffects` that ran for every completed interaction in the
game, is removed. `LegacyItemConversions.Retire` adds a check every 2 s (real time) that
reads the loaded-ship list and skips every ship already cleared; each ship is scanned
once, with one `GetCOs` pass, the first time it is loaded, then never again until the
next load. No capture accompanies this change.

## 3 October: rolling captures (L22)

L22 — Owner request: performance snapshots are exported automatically as they are
generated, only while recording is on, and `perf stop` ends it. Framework 0.75.0 turns
`perf start` into rolling windows of the chosen length: `PerformanceSession` exports a
window the moment it ends (its time limit, which can be reached inside any timing
call, or a world change) and starts the next with the same options once a world is
ready. Stop exports the partial window; exit still writes nothing. Exports happen on
the main thread between windows, so their cost lands in no window; a summary window
is a small file. A failed automatic export stops recording once and keeps the capture.
`PhobosPerformance.Tests` covers the world-change wait, roll-over at the time limit and
inside a timing call, stop, exit and a failed export (60 checks). No in-game capture
accompanies this change.

## 4 October: laser crew jobs (L23)

L23 — Shipbreaker 0.71.0 with Framework 0.78.0: the ML-2 queues the game's own Haul and
Mine jobs. Nothing runs per frame or per power step. `LaserService.QueueJobs` runs once
when a cut finishes (at most one a second per laser, in practice one every 22 to 60
seconds), and only with a setting on; it reads two saved switches, and with hauling on
makes one `GetCOs` pass over the moored ship to find what dropped within two tiles.
`NativeJobs.HasStockpile` is read when a setting is switched on and once per Start when
jobs are queued with no zone. No capture accompanies this change.

## 4 October: vessel contents on the card (L24)

L24 — Framework 0.79.0 with Manufacturing 0.36.0 and Agriculture 0.39.0: bulk vessels
show their contents on the right-click card. `BulkVessel.Save` now ends with
`VesselContentsDisplay.Refresh`, two `GetCondAmount` reads and a `SetCondAmount` only
when a figure changed, so a line topping up a full store every two seconds writes
nothing. `VesselContentsDisplay.Poll` runs on a five-second cadence and sweeps each
loaded ship once (`GetCOs` plus one registry probe per object), then never again until
the next load. The game's own card re-reads the row twice a second while open. No
capture accompanies this change.

## 4 October: crops as a data pack (L25)

L25 — Agriculture 0.40.0: crop figures come from the crops pack. `Crop.Get`, `Crops.ByFeed`
and `HearthRecipes.ForInput` replace string comparisons with one dictionary probe (or a
scan of a one-entry recipe list) on the paths that already called them each power step.
`Crops.All`, `Definitions.MixActions` and `LineService.FeedProfiles` allocate small arrays,
and are read when a panel is drawn, an order is evaluated or definitions are prepared, not
per step. The packs load once per content load. No capture accompanies this change.

## 4 October: wheat and a second cooker input (L26)

L26 — Agriculture 0.41.0: wheat is one more crops-pack entry; nothing per step scales with
crop count except the dictionary probes L25 introduced. The cooker's recipe lookup scans a
two-entry list; `Cookable` and the crew cooking order scan it once per Start or order
evaluation, with one inventory probe per recipe supply. No capture accompanies this change.

## 4 October: tomato picking and soybean (L27)

L27 — Agriculture 0.42.0: two more crops-pack entries and one cooking recipe. Picking
adds `CropState.PickPortions`, a few arithmetic operations called when a pick is offered,
when the crew order is evaluated and in the panel's status line; the status line also
reads the crop's pick count. Nothing new runs per power step. No capture accompanies
this change.

## 4 October: CO2 response and spare condensate (L28)

L28 — Agriculture 0.43.0: each rack power step reads the room's CO2 and total moles once
more and interpolates a six-point curve. Spare condensate exists only when a rack's
reservoir is full; then `VapourReturn` looks up the nearest reachable water tank at most
every 30 seconds (one `BulkVessels.Aboard` pass and a line-reach check per tank, from the
cached topology) and saves the tank's record once per step it deposits. The panel's status
line reads the curve once. No capture accompanies this change.

## 4 October: straw press and straw charges (L29)

L29 — Agriculture 0.44.0 and Manufacturing 0.37.0: the B2 saves one more small record
(the straw press) with its other records, written only when it changes. While the dryer
runs, each power step adds energy to that record; the tank look-up (`VapourReturn.Room`,
one `BulkVessels.Aboard` pass and a line-reach check per tank) runs once per drying batch,
not per step. Loading the press walks the bench's own six-cell tray once per crew action.
The V4's two new charges add two catalogue entries and one more gas link (methane now
both drawn and stored); charge settlement is unchanged. No capture accompanies this change.

## 4 October: fibre flax (L30)

L30 — Agriculture 0.45.0: one more crop row and one more B2 job mode. A scutching job
reads its bound bundle once per power step, as the recovery job does. The flax crew
order checks the bench's six-cell tray for pressable residue once per offer. No capture
accompanies this change.

## 4 October: sugar beet (L31)

L31 — Agriculture 0.46.0: one more crop row and one more B2 conversion. Flax and sugar
share `BenchConversions`; a running job looks its row up once per power step from a
two-entry table. No capture accompanies this change.

## 4 October: ethanol tanks and line (L32)

L32 — Framework 0.80.0 with Manufacturing 0.38.0: three more tank sizes and one more line
family on the existing bulk-vessel and line-contents paths. A tank's damage hook runs
once per damage switch or destruction; the ethanol line's runs once per segment damage
switch and searches for an ignition source only when the room has the oxygen to burn,
as the gas stores do. No per-step cost is added. No capture accompanies this change.

## 4 October: fermenter-still (L33)

L33 — Manufacturing 0.39.0: a fourth charge machine on the shared engine, with two
recipes; its power step and settlement are the engine's own. Its ignition role is
checked only when an ethanol or fuel spill already needs an ignition source. No
capture accompanies this change.

## 4 October: bottling unit (L34)

L34 — Manufacturing 0.40.0: one more batch machine on the existing power hook. While
armed it checks its two linked vessels each power step, as the X2 does; the tray probe
builds seven servings and discards them, so it runs once per batch, before the batch's
first energy, rather than every step. No capture accompanies this change.

## 4 October: feed from a machine's own inventory (L35)

L35 — Framework 0.83.0 with Shipbreaker 0.72.0 and Manufacturing 0.41.0: a started machine with an empty feed looks
in its own inventory for feed. The look is skipped when that inventory is empty, and otherwise runs at most once
every two real seconds for each armed, idle machine (a few admission checks over a tray of at most a dozen cells);
a working machine and a paused one never look. No capture accompanies this change.

## 4 October: optional feed stores (L36)

L36 — Framework 0.85.0 with Shipbreaker 0.73.0 and Manufacturing 0.42.0: an armed, idle machine with a chosen feed
store also looks there, on the same two-second real-time cadence as its own inventory and only when that gave
nothing. The look reads the saved choice, asks the cached belt layout whether the two still join, and runs the feed's
admission over the store's units. A machine with no store chosen pays one saved-record read per look. No capture
accompanies this change.

## 4 October: reaction mass feeder (L37)

L37 — Framework 0.87.0 with Manufacturing 0.43.0: one more RCS feed and one more batch machine on the existing
power hook. The reserve query the game makes every frame is two dictionary probes and no allocation; an idle feeder
looks through its four-cell inventory each power step and in its feed store at most every five real seconds. The
inventory admission hook adds one prefix comparison for containers that are not charge feeds. No capture
accompanies this change.

## 4 October: outcome tables and the gangue wash (L38)

L38 — Framework 0.88.0 with Manufacturing 0.44.0: a charge with an outcome table is resolved once, at bind, by one
hash over its bound unit ids; nothing is added to a power step. The recipe list a machine offers now skips outcome
recipes through one dictionary probe per recipe. No capture accompanies this change.

## 4 October: ledger refresh for L29 to L38

The ledger file was last regenerated at L28. The reviews for L29 to L38 above were written here as each change was
made, but the refresh that stamps the files was not run (an agent omission, found on 4 October when another session
ran the audit). It was run once for the whole span, so every file changed between L28 and L38 carries all ten codes
rather than only the codes of the changes that touched it. The dispositions above stand; the stamping is coarser
than usual for these rows.

## 4 October: add-ons (L39)

L39 — Framework 0.90.0 with War Declared 0.2.0: add-on discovery reads the game's mod list and each enabled folder's manifest once per content load, and each pack load lists one folder per add-on. Nothing runs per frame except a counter comparison for the skipped-file notice. No capture accompanies this change.

## 4 October: add-on text and added items (L40)

L40 — Framework 0.91.0 and 0.92.0 with Manufacturing 0.45.0: translation catalogs are read once more per content load, when the mod list is known; added materials are built with the others at content load and add one identity to a machine's feed rule. Nothing is added to a frame or a power step. No capture accompanies this change.

## 5 October: added items in Shipbreaker and Agriculture (L41)

L41 — Shipbreaker 0.75.0 and Agriculture 0.48.0: materials a data file adds are built with the others at content load. The furnace panel's recipe label makes one catalogue lookup more per recipe when the panel is drawn. Nothing is added to a frame or a power step. No capture accompanies this change.

## 5 October: receipt hook keeps the request (L42)

L42 — Framework 0.93.0: the GatherPower hook gains a prefix that copies one number, for every powered object in the world, so the postfix can compare the request with what remained. The postfix still returns on one integer when no receipt is open. No capture accompanies this change.

## 5 October: machine heat scale (L43)

L43 — Framework 0.94.0 with Shipbreaker 0.76.0, Manufacturing 0.46.0 and Agriculture 0.49.0: every machine's heat check and deposit multiplies by one stored number. No new hook, lookup or allocation; nothing is added to a frame beyond that multiplication. No capture accompanies this change.

## 5 October: work carries on after a reload (L44)

L44 — Framework 0.95.0 with Shipbreaker 0.77.0, Manufacturing 0.47.0, Agriculture 0.50.0 and Auto Nav 0.33.0: each machine's existing power step asks once whether it carries the saved resume mark (one condition probe for an unmarked machine) and keeps the mark equal to its running state (a write only when it changes). One offer per machine per load; no new hook on the game's power path and no world scan. No capture accompanies this change.

## 5 October: panel reach measured tile to tile (L45)

L45 — Framework 0.96.0: the crew access check looks up two tiles (the crew member's and the machine's use point) instead of comparing two positions. It runs when a panel opens, on each panel refresh (twice a second while one is open) and on a command, never on a frame or power step. No capture accompanies this change.

## 5 October: cycle meters as a share and minutes (L46)

L46 — Manufacturing 0.48.0: the X2, K2, AX-2 and bottler status lines compute a percentage and the minutes left where they formatted two kWh figures: a few arithmetic operations on the same step and panel refresh as before. No capture accompanies this change.

## 5 October: product stores (L47)

L47 — Framework 0.98.0 with Manufacturing 0.49.0: a started charge machine or bottler with a product store looks in its tray once a real second and moves at most one unit; with no store chosen the cost is one saved-record read at that cadence. No new hook and no world scan. No capture accompanies this change.

## 5 October: regolith leach (L48)

L48 — Manufacturing 0.50.0: four recipes and one outcome table as data, and one more feed identity on the LC-3. The outcome pick is the existing hash at bind. Nothing is added to a frame or a power step. No capture accompanies this change.

## 5 October: regolith in the V4 and the regolith floor (L49)

L49 — Manufacturing 0.51.0: the charge engine's feed table is kept per machine preference (one small dictionary per distinct choice, built on first use), and reading a machine's preference is a session lookup on the paths that already ran. Two recipes, two materials, one floor object and two install jobs are built at content load. Nothing is added to a frame. No capture accompanies this change.

## 5 October: the Oxsmith EC-4 and ferrosilicon (L50)

L50 — Manufacturing 0.52.0: a fifth charge machine on the shared engine. The per-admission bin test (`ChargeMachines.ForBin`) compares one more string, five in all, with no allocation; the definition memo and the power hooks are unchanged. Three recipes, two materials, one machine family and one more LC-3 link are built at content load. Nothing is added to a frame. No capture accompanies this change.
