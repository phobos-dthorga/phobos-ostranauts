# Performance audit and practical playtesting

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

The [source ledger](../config/performance-audit.json) accounts for every first-party
C# file under `src`, excluding build output. It records normalized source hashes,
runtime hooks, scan/allocation/IO indicators and review dispositions. Coverage is
static runtime-path and call-site review, with deeper inspection of the measured
hot paths, changed implementations and safety boundaries. It is not a claim of
line-by-line formal verification or an exhaustive native/third-party profile.
`python scripts/audit-performance.py` rejects missing, extra or stale source entries.
This release reviews 259 files: Framework 96, Auto Nav 51, Shipbreaker 74,
Agriculture 37 and Manufacturing 1. The ledger is a dated source-review snapshot;
future performance work should refresh the affected dispositions and hashes.

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
introduced. See [the repair and Combat record](auto-nav-combat.md). No additional
performance captures or quantitative improvement claims were made.

## T1 — Secured towing correction

Auto Nav 0.22.1 reads the fresh native attachment map for one reciprocal tow; no retained discovery cache is added. The enclosing radius protects both hulls and existing bounded presentation remains. No new recording or performance measurements. See [towing evidence](auto-nav-towing.md).

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

## 28 September: construction and maintenance follow-up

R10 — Section assembly uses native work scheduling, hauling and saved lots. There
are no new frame loops, equipment scans or automatic restarts. Instructions build
only on explicit opening; maintenance reasons reuse the existing service checks.
The section selector hook uses a dictionary fast path. Cooling's native admission
hook performs an allocation-free empty-list check during ordinary operation;
its captured-cargo set exists only during a synchronous damage/repair transition.
No repeat performance capture or measured FPS claim accompanies this change.
