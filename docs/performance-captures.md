# Opt-in performance captures

Framework **0.15.0**, Auto Nav **0.10.1** and Shipbreaker **0.11.1** integrate
[Phobos Scope](https://github.com/phobos-dthorga/phobos-scope). Profiling is disabled
by default. Enable a bounded capture through F3, reproduce a workload, stop and
export, then analyse outside the game. A Rust process is not needed during play.

## Install the prepared builds

Close Ostranauts normally before running `Install or Update Mods.cmd` or
`pwsh -File scripts/install-mods.ps1` from the repository. The existing installer
includes Framework automatically for Auto Nav/Shipbreaker, preserves rollback
receipts and refuses to modify a running game. Its `-WhatIf` option previews and
`-VerifyOnly` checks files after installation. Game execution remains owner-run.

Framework's plugin directory contains both `PhobosFramework.dll` and one
`Phobos.Scope.Recording.dll` (0.1.1). Consumer packages do not duplicate the recorder.
The installer checks recorder identity/version and refuses missing, duplicate or
newer conflicting shared copies. Check the BepInEx startup log for Framework 0.15.0,
Auto Nav 0.10.1 and Shipbreaker 0.11.1 after the next launch.

## Capture a workload

Load a world fully, open F3 and enter:

```text
phobosframework perf start detailed 30 20000
phobosframework perf status
phobosframework perf stop
phobosframework perf export
```

Start options are positional: mode (`detailed` or `summary`), integer real seconds
(1–3600), retained records (1–20000). Defaults are detailed / 60 seconds / 20000
records. `perf help` gives the syntax. Extra arguments and user-supplied export
paths are rejected. Export prints a new file under
`BepInEx/captures/PhobosScope/`; no personal path is stored in the capture itself.

Status shows elapsed real time, limits, completed/open/incomplete scopes, drops and
rejections. Each export gets a new filename. A failed export retains the stopped
snapshot for retry. Export a stopped capture before starting another, so an error
cannot accidentally erase your only copy. Keep the process open until exported;
there is no implicit disk write at shutdown.

The real-time duration limit continues during pause. A Framework Update poll stops
idle captures at that limit. Starting a load or new game stops immediately; losing
world readiness and content reload also stop. A new world never silently resumes
recording. Open scopes become incomplete instead of inventing end times.

Starting profiling does not enable Auto Nav verbose logging, change sensor
emissions, control flight, run machines, transfer material or write saves.
Diagnostics faults disable further recording for the process, attempt one warning,
and preserve a stopped snapshot with a rejected-measurement quality marker. Status
and export remain available; restart after investigating a recorder fault.

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
conditions and compare both calls per real second and per-call cost. Verify loading
another world stops the capture and that export remains available. Retain original
JSON so it can be reanalysed.

## Build and verification boundaries

Initialize the pinned private source dependency with
`git submodule update --init --recursive`. Framework builds it via a project
reference; do not copy recorder source into individual mods. The pin is the
reproducible dependency identity. The Scope project licence is still undecided:
its bundled licensing notice is separate from Framework's MIT grant.

`scripts/build-framework.ps1` runs the adapter checks. `scripts/verify-performance.ps1`
creates synthetic adapter captures and sends them through the pinned Rust analyser.
The normal Auto Nav/Shipbreaker builds retain their gameplay-rule/native-data checks,
and `tests/install-mods.tests.ps1` exercises packaging, shared dependency checks and
rollback receipts in fixtures.

Compiled against the installed Ostranauts assemblies and BepInEx 5.4.23.5; package
metadata targets Ostranauts 1.0.1.5. Builds, synthetic captures and offline native
checks are not in-game tests. Owner gameplay evaluation remains pending.
