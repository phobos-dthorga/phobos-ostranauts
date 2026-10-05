# Opt-in performance captures

Performance recording was introduced in Framework **0.15.0**, Auto Nav **0.10.1**
and Shipbreaker **0.11.1**. See the [player guide](player-guide.md) for current
prepared versions. These integrate [Phobos Scope](https://github.com/phobos-dthorga/phobos-scope).
Profiling is disabled
by default. Start recording through F3 and play: each window is written to a file
as it ends, until you stop. Analyse the files outside the game. A Rust process is
not needed during play.

## Install the prepared builds

Close Ostranauts normally before running `Install or Update Mods.cmd` or
`pwsh -File scripts/install-mods.ps1` from the repository. The existing installer
includes Framework automatically for Auto Nav/Shipbreaker, preserves rollback
receipts and refuses to modify a running game. Its `-WhatIf` option previews and
`-VerifyOnly` checks files after installation. Game execution remains owner-run.

Framework's plugin directory contains both `PhobosFramework.dll` and one
`Phobos.Scope.Recording.dll` (0.2.0 since Framework 0.104.0). Consumer packages do not duplicate the recorder.
The installer checks recorder identity/version and refuses missing, duplicate or
newer conflicting shared copies. Check the BepInEx startup log for the versions you installed; the current
prepared versions are listed in the player guide.

## Capture a workload

Load a world fully, open F3 and enter:

```text
phobosframework perf start summary 30 20000
phobosframework perf status
phobosframework perf stop
```

Since Framework 0.75.0 recording runs in **windows** of the chosen length (30 seconds
above) until `perf stop`. Each window is exported to its own file the moment it ends,
and the next starts straight away with the same settings; nothing waits for a manual
export. `perf stop` ends recording and exports the partial last window. `perf status`
shows the current window, how many have been exported and the latest file. `perf export`
writes the latest window again under a new name.

At fast-forward use summary mode: detailed mode fills its record cap within seconds
and the capture stops early. The 29 September 2026 pass used
`phobosframework perf start summary 30 20000` at speed 8 with the navigation console
closed, and compares captures taken the same way before and after installing.
One start now gives as many matched windows as you let it run: three 30-second
windows take a minute and a half, then `perf stop`.

Start options are positional: mode (`detailed` or `summary`), integer real seconds
(1–3600), retained records (1–20000). Defaults are detailed / 60 seconds / 20000
records. `perf help` gives the syntax. Extra arguments and user-supplied export
paths are rejected. Export prints a new file under
`BepInEx/captures/PhobosScope/`; no personal path is stored in the capture itself.

Status shows elapsed real time, limits, completed/open/incomplete scopes, drops and
rejections. Each export gets a new filename; with 30-second windows that is about 120
small files an hour in summary mode, so stop when you have what you need. If a
window cannot be written (a full disk, a locked folder), recording stops with one
log line and keeps that window for `perf export` once the folder is fixed; a new
start is refused until it is exported, so an error cannot erase your only copy.
Quitting the game writes nothing: use `perf stop` before you quit to keep the
window in progress.

The real-time window length continues during pause. Starting a load or a new game
ends the current window, which is exported; recording stays on and the next window
begins once the new world has finished loading (status says it is waiting). This
reverses the earlier rule that a new world never resumed recording: recording is on
until you stop it. Open scopes become incomplete instead of inventing end times.

Starting profiling does not enable Auto Nav verbose logging, change sensor
emissions, control flight, run machines, transfer material or write saves.
Diagnostics faults disable further recording for the process, attempt one warning,
and preserve a stopped snapshot with a rejected-measurement quality marker. Status
and export remain available; restart after investigating a recorder fault.

## Memory, footprints and frame times (Framework 0.104.0)

Framework 0.104.0 ships recorder 0.2.0, which writes **capture format 2**. Every
counter now keeps one complete total (samples, sum, minimum, maximum, last value)
whatever the mode or record limit. Summary captures keep those totals instead of
every sample, so a busy counter can no longer fill the window and push out the
rest, which is what happened to every Framework 0.46.0 capture on 29 September.
Detailed captures still keep samples while room remains. Use summary mode for the
review, as before.

**Memory**, read once a real second while recording and at the start of each window:

| Reading | What it is |
| --- | --- |
| `memory.managed_heap` | The game's managed heap in use, without forcing a collection |
| `memory.unity.mono_used`, `memory.unity.mono_heap` | Unity's own figures for that heap: used, and reserved from the system |
| `memory.unity.allocated`, `memory.unity.reserved` | Unity's native memory: allocated, and reserved |
| `memory.process.working_set`, `memory.process.private` | The game process as Task Manager shows it |
| `memory.managed_heap_after_collection` | The managed heap at the first frame after each collection |

Each source is tried once when the game starts. One that fails or reads nothing is
left out and named in the capture metadata (`memory_sources_unavailable`), never
recorded as zero; `memory_sources` lists those that work. These are whole-game
figures. The heap after each collection is the plainest sign of a leak: if its
lowest value keeps rising over an hour of play, something is holding on to memory.

**Footprints** are counts of what each mod keeps alive by key, read on the same
once-a-second cadence. They are counts, not bytes, and show growth that never comes
back down:

| Footprint | Counts |
| --- | --- |
| `framework.crew.orders`, `framework.crew.jobs`, `framework.crew.retry_records` | Standing orders, crew jobs in progress, and their notices and retry records |
| `framework.crew_skip.records` | What the managed time-skip keeps per crew member and machine |
| `framework.fluid_route.ships`, `framework.buffered_drains.entries` | Ships with a cached line layout; stores with a buffered draw |
| `agriculture.sessions` | Agriculture machines with a live session |
| `shipbreaker.furnace.sessions`, `shipbreaker.capture.sessions`, `shipbreaker.laser.sessions`, `shipbreaker.reclamation.sessions` | Shipbreaker machines and missions with a live session |
| `war.records`, `war.tallies`, `war.damaged`, `war.part_facts` | War Has Been Declared's battle ledger, damage queue and schematic facts |

Manufacturing, Auto Nav and Medical hold their sessions in tables that let go of a
machine when the game does, so they cannot grow this way and report no footprint.
A mod adds its own with `Performance.RegisterFootprint` at start-up.

**Frame times** are also counted into fixed buckets (`game.frame.upto_8_3ms` up to
`game.frame.over_500ms`, with bounds at 33.3, 50 and 100 ms among them). A summary
capture keeps these counts, so `scripts/compare-performance.py` reports long frames
exactly, the mean and worst frame exactly, and each percentile as the upper bound of
its bucket, never interpolated. Format 1 captures still report exact percentiles from
their retained samples; the script warns when a comparison mixes the two.

What none of this gives: memory per operation. That needs the per-thread allocation
counter, which the game's runtime does not support (`allocation_measurement` reads
`unavailable`). To see how much the mods use in all, compare captures of the same save
with and without them, or with only Framework installed.

## What is measured

| Stable operation key | Coverage |
|---|---|
| `framework.room_alarm.read` | Existing read-only room-alarm observation path |
| `autonav.guidance.update` | Guidance service after active-flight admission guards |
| `autonav.docking.update` | Active docking update at each existing system boundary |
| `autonav.contact.read` | Selected-target native contact read, including display callers |
| `shipbreaker.processing.check` | Processor validation/feed checks |
| `shipbreaker.processing.advance` | Active powered processing progress |
| `shipbreaker.routing.check` | Existing receiver/route checks and candidate selection |
| `shipbreaker.routing.advance` | Armed receiver progress and delivery work |
| `shipbreaker.panel.refresh` | Industrial/local panel refresh, including delegated reads |

Scopes added by the 29 September 2026 fast-forward pass, which attribute the costs
that sat in unscoped hooks before it:

| Stable operation key | Coverage |
|---|---|
| `framework.fluid_route.find` / `framework.fluid_route.scan_objects` | Shared pipe, gas-line and coolant topology walks and the objects they examine |
| `framework.crew.task_filter` / `framework.crew.path_checks` | Crew task admission and the path searches it still runs |
| `framework.rcs.collect` | RCS propellant input collection per ship per step |
| `framework.state.write` / `framework.state.writes_skipped` | Saved-state writes and identical writes skipped |
| `framework.water_supply.refill`, `framework.skip.machine_step` | Ship's Water refills; machine steps inside the managed time-skip |
| `agriculture.irrigation.route`, `agriculture.saves` | Irrigation route lookups; crop and rack record writes |
| `shipbreaker.power.hook`, `shipbreaker.furnace.route`, `shipbreaker.furnace.saves` | The power hooks for our machines; coolant route lookups; furnace record writes |
| `manufacturing.scan`, `manufacturing.power.hook`, `manufacturing.machine.step`, `manufacturing.manifold.refresh`, `manufacturing.regulator.tick` | The two-second world scan, the power hooks, machine steps, manifold rechecks and regulator ticks |
| `autonav.guard.update`, `autonav.hazards.scan`, `autonav.persist.write`, `autonav.foreign_controller.check`, `autonav.contact.reads` | The pre-physics guard sweep, the asteroid scan, flight record writes, the other-controller check and the count of native contact reads |
| `war.poll`, `war.lay_pending` | The two-second ship poll and the laying pass |
| `framework.world.sweep`, `framework.world.sweep_objects` | Framework 0.46.0's shared world sweep that replaced each mod's own pass, and the objects it examined |
| `game.crewsim.update`, `game.sim.advance`, `game.starsystem.update`, `game.powered.update`, `game.interaction.offer_check` | The game's own main loop, simulation step, ship update, appliance updates and crew offer checks, including every mod's hooks inside them. Installed only while a capture records (Framework 0.46.0) |
| `game.interaction.offer_postfixes`, `game.condtrigger.calls` | Milliseconds spent in all mods' postfixes on the offer check, and how many trigger checks ran; summed in memory and recorded once per frame (Framework 0.47.0; 0.46.0 recorded one record per call and overflowed captures) |

`shipbreaker.routing.candidate_items` is an **increment** in items: the candidate
collection size when routing starts selecting another item. It is neither active
connection count nor an exact predicate-evaluation count. No extra ship scans are
added just to produce counters. Names remain bounded; no ship/item IDs or player
names are exported.

Context is emitted initially and when a main-thread poll observes a change:
native speed multiplier, pause, navigation-console visibility, and (when loaded)
Shipbreaker's industrial/local control-panel visibility. Very short changes between
polls can be missed. Speed uses `Time.timeScale`; pause is reported separately.
The installed `CrewSim.TimeScaleMult`/`ResetTimeScale` implementations were inspected
to verify this mapping. Metadata includes game, Framework, loader and loaded Phobos
content versions, with no save data or arbitrary plugin configuration.

Timings are inclusive **real elapsed time**, including nested calls and waits.
Do not add parent and child timings into a CPU-use percentage. Detailed-event
drops do not erase complete operation aggregates, but timelines/windows use only
retained records. Summary mode has no duration-event timeline. The recorder is
synchronous and single-threaded; no async scope or worker-thread support is claimed.

## Analyse and inspect

Build Phobos Scope separately, then run:

```text
phobos-scope analyse CAPTURE.json NEW_REPORT_DIRECTORY 1000
```

Open `trace.json` from a detailed report in [Perfetto](https://ui.perfetto.dev).
Use `summary.csv` for operation comparisons and `time-series.csv` for trends.
Read `report.json` for loss/incomplete/rejected warnings first. Missing observations
are not zero, and a high elapsed duration is not automatically exclusive mod CPU.

For owner-run evaluation, capture a similar workload three ways: ordinary speed,
fast-forward, and an open industrial console. Where useful, separately capture
coasting/braking/docking or routes with different candidate counts. Record relevant
conditions and compare both calls per real second and per-call cost. Windows that
span a load are short and end with the world change; leave them out of comparisons.
Retain original JSON so it can be reanalysed.

## Build and verification boundaries

Initialize the pinned public source dependency with
`git submodule update --init --recursive`. Framework builds it via a project
reference; do not copy recorder source into individual mods. The pin is the
reproducible dependency identity. Original Scope work is MIT licensed; preserve its separate bundled licensing notice.

`scripts/build-framework.ps1` runs the adapter checks. `scripts/verify-performance.ps1`
creates synthetic adapter captures and sends them through the pinned Rust analyser.
The normal Auto Nav/Shipbreaker builds retain their gameplay-rule/native-data checks,
and `tests/install-mods.tests.ps1` exercises packaging, shared dependency checks and
rollback receipts in fixtures.

Compiled against the installed Ostranauts assemblies and BepInEx 5.4.23.5; package
metadata targets Ostranauts 1.0.1.5. Builds, synthetic captures and offline native
checks are not in-game tests. Owner gameplay evaluation remains pending.

## Current audit and deferred comparisons

The owner recorded fast-forward baseline captures on 29 September 2026 before the
performance pass; matched after-captures with the new builds are the next step, and
`scripts/compare-performance.py` reports the difference. Recording remains disabled
by default. See the [performance audit](development/performance-audit.md) for the
baseline numbers, the findings and their limitations.

Additional scopes cover Auto Nav panel refresh/read, system-prefix work and fire;
Shipbreaker furnace/capture/reclamation; Framework crew discovery; and Agriculture
scan/update/panel work. `framework.equipment.scan_objects`,
`shipbreaker.furnace.scan_objects` and `agriculture.scan_objects` count candidate
objects actually examined by those entry points. They are not machine counts.
`framework.crew.scan_objects` belongs to the baseline global-scan instrumentation;
it is not emitted by the new ship-scoped discovery path.

`game.frame.interval` records main-thread frame intervals in milliseconds.
`game.gc.gen0/1/2` record process collection-count increments, not per-mod
collections. `game.allocations.main_thread` is available only when the runtime's
counter passes an allocation probe. Early baseline captures reported unsupported
zero totals; do not interpret those as allocation measurements. Metadata now
records calibration support and assembly build IDs without object/player IDs.
Memory stays bounded by the existing record cap: only one window is held at a time.
Since Framework 0.104.0 summary windows keep counter totals rather than samples; see
[memory, footprints and frame times](#memory-footprints-and-frame-times-framework-01040).

For a future comparison, `scripts/compare-performance.py --before <captures...>
--after <captures...>` emits JSON with per-run percentiles, long frames, operation
cost per second, collections, memory and footprint ranges, and quality flags. It reads
format 1 and format 2 captures. Supply three matched 30-second
captures per side and compare open/closed scenarios separately. Incomplete scopes
limit their own operation totals, not otherwise valid frame samples. Unsupported
allocation totals are omitted. This tool does not verify identical saves, zoom,
workloads or recorder overhead, and does not establish gameplay correctness.
