# Phobos Shipbreaker: first playable build

Current candidate: **Shipbreaker 0.2.1 + Phobos Framework 0.2.1**. Construction and
machinery now work through our provider and native definitions without OCF/SWB.
Framework **0.2.1** subsequently fixes the native menu's false Missing status;
the owner confirmed that correction in-game on 2026-09-24.

Shipbreaker **0.2.1** fixes the feed rejecting ordinary walls: the native wall is
`IsCumbersome`, which the old solid-container filter forbade. A dedicated feed
trigger now requires a wall panel and uses the native cumbersome-compatible
container filter. Existing exact identity, mass, stack and four-panel checks still
apply; installed and oversized items remain excluded. The separate 4 x 4 feed
opens with its native title instead of appearing as an unlabeled child grid.
F9 explains which grid accepts inputs and reports an empty feed explicitly.
Floors may fit the 8 x 8 output storage but are not supported processing inputs.

The native data-only trigger evaluator reproduces the old rejection and accepts
the ordinary wall with the corrected filter; it also rejects native loose floors,
machinery, installed and oversized panels. This is an offline regression check,
not an in-game success claim. After installing 0.2.1 and restarting, retry one
ordinary loose wall in **Wall-panel feed**, then start the queue. Existing saved
IDs, dimensions, contents and recipes are unchanged; no save editing is required.

Offline checks passed. On 2026-09-24 the shared installer installed and verified
Framework 0.2.0, Shipbreaker 0.2.0 and Auto Nav 0.1.1: 39 matching files and enabled
native load-order entries. In-game startup and behaviour remain owner-tested work.
Saved Phobos IDs, progress, dimensions, material bill and artwork are
preserved. See the [migration details](phobos-framework.md).
The previous **0.1.4** baseline was built against Ostranauts **1.0.1.4**, BepInEx
**5.4.23.5**, Crafting Framework **0.8.71** and Salvage Workshop **0.8.71**.
Its build and offline logic checks passed, and installation was verified on
24 September 2026. The owner subsequently confirmed successful dependency/template checks,
both construction recipes registered, and a good initial visual match in-game.
**Processing and interruption behaviour remain unverified.** The attempted
exterior-wall placement conflicts with this build's floor-mount rules; see the
[mounting review and proposed hull attachment](shipbreaker-hull-mounting.md).

Version 0.1.4 replaces the temporary art with the approved pixelated machine design
and matching installed/damaged, transport/damaged, unfinished-section and residue
forms. Machinery uses 64-pixel world textures; residue uses 16 pixels. All six
have their own lighting maps and pixel-preserving inspection portraits. Existing
saved object IDs, masses, footprints, recipes and progress are unchanged; no save
rewrite is needed by design, but loading an older test save remains untested.

## What this build provides

A powered dismantling fixture, built at an installed native Bar Table or Dining Table.
An existing Salvage Workshop workbench is also supported, optionally.
Its **4 x 4 tile footprint and storage capacity are the working design**, not a
smaller diagnostic stand-in. The owner clarified that intended physical size and
capacity should be used from the outset; this build adopts the following baseline.

| Property | First build |
| --- | --- |
| Floor space | 4 x 4 tiles, installed on floor; leave access alongside it |
| Empty machine mass | 160 kg |
| Feed | Up to four separate ordinary loose wall panels, 96 kg total |
| Processing | One panel at a time; loaded queue proceeds automatically |
| Output | Separate 8 x 8 inventory; complete batches must fit before work proceeds |
| Cycle | Default: 60 game seconds of eligible powered work per panel |
| Electrical demand | Default: 30 kW working; 0.12 kW idle |
| Controls | F9 by default, or F3 console commands; selected crew must be beside the fixture to start, pause or cancel |
| Save/load | Panel progress and chosen duration are saved; queue resumes only when started by the player |

The output tray can retain prior results. New batches reserve space for every
product, without merging existing stacks during completion. Available space
therefore depends on actual item footprints and how the tray is arranged.
Feed limits are enforced separately from its inventory grid size. Additional
material recipes can use this machine later without changing its initial scale.

## Material recipe

Only **ordinary loose walls**, definition `ItmWall1x1Loose`, are accepted.
Whipple, aerodynamic and DuraWal panels are different recipes and are rejected.
Inputs must be separate, empty, uninstalled objects with the expected actual mass.

| Result | Count | Mass |
| --- | ---: | ---: |
| Small mechanical parts | 2 | 1 kg |
| Aluminium scrap | 2 | 2 kg |
| Carbon-fibre scrap | 2 | 2 kg |
| Steel scrap | 6 | 6 kg |
| Mixed panel residue | 1 | 13 kg |
| **Total** | | **24 kg** |

These are explicit design yields using the native wall's material families, not
a verified bill of materials for its fictional manufacturer. The operation is
disassembly and separation; it does not melt the panel or purify mixed alloys.
The residue remains a real inventory item with mass, rather than disappearing.
It is a distinct definition without native trash/salvage category flags; the
existing trash sorter does not consume it. Haul, retain or jettison it using normal
item handling. Refining that material is a later feature, not an existing output
use. We do not rebalance the game's or other mods' salvage recipes.

Construction uses **100 steel, 48 aluminium, 20 mechanical parts and
4 electronic parts** in total, totalling 160 kg at the inspected native masses.
At an installed table or supported workbench, make **two Dismantling Fixture Assembly Sections**,
then combine them with **Powered Dismantling Fixture**:

| Stage | Inputs | Result | Work |
| --- | --- | --- | --- |
| Make a section, twice | 50 steel, 24 aluminium, 10 mechanical parts, 2 electronic parts per section | One unpowered 80 kg, 4 x 4 section per craft | 120 seconds each |
| Finish the fixture | Two sections | One 160 kg, 4 x 4 fixture | 60 seconds |

Total work remains 300 game seconds. Each section is a separate, unstackable
physical item; arrange room for both near the workbench. It has no powered
processing, installation or generic scrap role of its own. Native install, uninstall,
damage and repair use our definitions and native game installation templates.

This fixes a registration problem found during the material-use review:
Historically, Crafting Framework 0.8.71 capped a craft at 100 input units; the earlier one-step
recipe requested 172. The new stages use 86, 86 and 2 units. Existing fixture
identities, processing recipes, saved progress, footprint and capacity are
unchanged; the section uses `PhobosShipbreakerSection`. No existing save
needs conversion for this added construction item. No in-game migration was tested.

The [material-use review](shipbreaker-material-uses.md) maps these outputs into
existing repairs and Salvage Workshop recipes, including their remaining inputs
and the limits of their mass accounting.

## Install the prepared package

Required dependencies: **BepInEx 5** and **Phobos Framework 0.2.0 or later**.
OCF, Salvage Workshop, Auto Nav and Common Sense hauling are optional for this
onboard processor. Keep other mods installed if the existing save contains their
objects or other consumers require them; independence is not foreign-object
save recovery. Test the mod-free case using a new separate save.
From this repository, close the game and run
`./scripts/install-mods.ps1 -Mods Shipbreaker`. It includes the prepared Phobos
Framework package automatically and handles both destinations, dependency
preflight, enabling the native entries and backing up updated files.
See [one-command installation](installing-mods.md) for the double-click launcher
and options. The manual steps below are for a standalone ZIP without the installer.

1. Exit Ostranauts normally before copying the package.
2. Install the separate **PhobosFramework-P0.zip** package first: its own plugin
   folder and native metadata folder, each in the matching game directory. Keep
   one shared framework provider rather than copies inside individual mods.
3. From the Shipbreaker ZIP, copy `BepInEx/plugins/PhobosShipbreaker/` into the game's matching
   plugin directory. Do not copy other mods' libraries or overwrite original game
   data.
4. Copy `Mods/PhobosShipbreaker/` into `Ostranauts_Data/Mods/`.
5. Enable the Phobos native data packages after core in
   the game's mod list. Both the plugin and the native data package are needed.
6. Start a **separate test save**. Do not use the player's real save for this first
   build. Retain the content packages and required providers when loading any save that contains the fixture
   or residue; removing a content mod from such a save is not a supported migration.

At startup the BepInEx log should include `Shipbreaker definitions ready`.
Version 0.2.0 checks the loaded Phobos Framework version, our enabled native data,
and the native definitions used by the independent machinery/construction path.
It prepares definitions before publishing them; a failure
during publication restores previous dictionary entries and removes new ones.
Processing stays disabled until both construction recipes have registered too.
The upstream game/native generation phase remains outside that transaction.

Use **`phobosshipbreaker dependencies`** for the startup snapshot: dependency
versions, enabled native packages, missing/changed definitions and construction
registration. It is read-only, works without selecting a fixture and does not
reload mods or retry registration. The F9 panel and normal status also report
the first blocker. No release-age cutoff or invented maximum version is imposed.

If BepInEx rejects the required Framework plugin before Phobos loads, its log is
the diagnostic; the Phobos console command cannot run. These changes do not make
a save safe to load after removing required content. Retain its providers until
a supported transition exists. See the [dependency contingency plan](dependency-contingencies.md).

## Try the useful loop

For the 0.2.0 independence candidate, the new integration checks are:

1. In a separate new save with OCF/SWB disabled, run `phobosframework status`
   and `phobosshipbreaker dependencies`. Both construction stages should be ready.
2. Use an installed Bar Table or Dining Table to make the two sections and final
   fixture, then install and use it through the normal loop below. Native menus
   show input/output quantities; normal material stacks should be accepted.
3. In a separate copy of an older test save, keep providers needed by foreign
   equipment. Check Phobos fixtures, feed/output contents and partial panel work
   survive loading and remain paused. If there was queued Phobos construction,
   check its original bench/material references and that it completes only once.
4. With OCF/SWB present, their other recipes should remain available and each
   Phobos construction stage should appear once. Do not remove those providers
   from the player's real save to prove independence.

These are targeted migration checks, not a request to repeat isolated ordinary
power-consumption tests. Report the version and the failing action if one fails.

1. At an installed **Bar Table** or **Dining Table**, build two **Dismantling Fixture
   Assembly Sections**, then join them with **Powered Dismantling Fixture**.
   To skip gathering construction stock in the test save, the existing F3 console
   accepts `spawn PhobosShipbreakerLoose`; use the normal Install action to place it.
   `spawn ItmWall1x1Loose` supplies a comparison panel if needed. These are the
   game's existing commands, not a Phobos diagnostic mode.
2. Install on a clear 4 x 4 floor area with the normal electrical connection.
3. Open its **Inventory** and place ordinary loose panels in the named **Wall-panel
   feed** window (4 x 4). The larger fixture inventory (8 x 8) is output storage;
   items placed there are not processing inputs. Floor panels are unsupported.
4. Stand beside it, press **F9** (or your configured key), and choose **Start / resume queue**.
5. Collect products from the separate output tray. When blocked, clear sufficient
   space and resume. A blocked fixture stops cutting demand and keeps progress.

Pausing retains work on the actual panel. Cancelling clears queued panel progress
and keeps the panels; spent energy is not refunded. Removing the active panel
pauses the queue. Putting a different panel in its place does not inherit work.
A partly processed original panel can resume its own work if returned.

The interface uses the selected crew member's proximity; it does not remotely
start a machine elsewhere on the ship or fetch panels automatically. Existing
hauling orders can supply material, subject to the fixture's feed restrictions.

## End-user settings

### Planned interface after the current tests

Owner request, 2026-09-24: open a dedicated, illustrated Shipbreaker interface
through **right-click fixture -> Control Panel**, following the game's equipment
interaction convention. This will become the primary entry point; retain F9 as
a fallback and the F3 commands for diagnostics. It is not in the current build.

Bind the panel to the clicked fixture, show its feed, output, progress and blockers,
and route actions through the existing processing service with the same proximity
and eligibility rules. Use original artwork consistent with the approved game
interface references: simple faceplate, restrained colour, clear contrast and
readable runtime labels. Inspect a native Control Panel interaction when this work
starts; the exact integration has not been established by this request.

Finish the current gameplay tests before implementing this change or generating
the panel artwork, so the build under test stays stable.

### Current configuration

BepInEx creates `BepInEx/config/phobosgekko.ostranauts.shipbreaker.cfg` in the game
folder on the first launch with this plugin. Exit the game, edit that text file,
then restart. Settings are read at startup; there is no live settings editor.
Each entry includes a description and, for numbers, an accepted range.

| Section / setting | Default | Accepted values and effect |
| --- | --- | --- |
| `Processing / CycleSeconds` | `60` | 10–3600 powered game seconds for a new panel |
| `Processing / ContinueQueue` | `true` | `false` processes one panel per Start command |
| `Power / WorkingKilowatts` | `30` | 1–1000 kW total active demand; cannot be below idle demand |
| `Power / IdleKilowatts` | `0.12` | 0.01–5 kW; remains positive for native power control |
| `Controls / WindowKey` | `F9` | Unity key name such as `F10`; choose an unused key |

Changing cycle duration affects panels that have not started. A started panel
retains its saved duration and progress across restart, pause and reload. Power
settings apply to all work after restart, so changing duration and power also
changes the energy spent per panel. These are balance options, not measured
physical cutting rates. Queue continuation never automatically starts work on load.

Footprint, chamber/feed capacity, output grid, item masses and recipe yields are
fixed parts of this machine design. They are not editable balance sliders: changing
them independently would affect saved inventory and material conservation.

## F3 console commands

The console uses the same `ConsoleResolver` entry point as Approach Assist.
Commands are case-insensitive; gameplay controls use the same service and access
checks as the panel.

| Command | Result |
| --- | --- |
| `phobosshipbreaker` or `phobosshipbreaker help` | Command help, including native test-save spawn commands |
| `phobosshipbreaker status [fixture-ID]` | List fixtures on the selected crew member's ship, their IDs, progress and control blockers |
| `phobosshipbreaker settings` | Show effective settings and the config filename |
| `phobosshipbreaker dependencies` | Read-only startup dependency versions, template checks and construction registration; no fixture selection needed |
| `phobosshipbreaker start [fixture-ID]` | Start or resume the queue |
| `phobosshipbreaker pause [fixture-ID]` | Pause and retain progress |
| `phobosshipbreaker cancel [fixture-ID]` | Clear work on all queued panels; keep panels; spent energy is not refunded |

Square brackets mean optional arguments; do not type the brackets. Omit the ID
when only one fixture is aboard. With multiple fixtures, copy the exact ID from
`status`; an ambiguous action is rejected. The selected crew member must be beside
that fixture for start, pause and cancel. Status/settings/help do not start work.

For test-save setup, use the game's existing `spawn PhobosShipbreakerLoose` and
`spawn ItmWall1x1Loose` commands, then install the fixture normally. The Phobos
commands do not add a separate item-spawning system.

## Focused owner checks

For this artwork update, first look at the loose fixture, install it, and inspect
its portrait. Check the outline and lighting against nearby equipment at normal
zoom, including with the fixture rotated. The rear sockets are connection cues;
build and place electrical conduit separately. There is no painted conduit loop.
Inspect a section during construction and the residue from the first batch.
If convenient, use `spawn PhobosShipbreakerLooseDmg` in the separate test save to
see the damaged form; no need to damage machinery in a real save. Send a screenshot
if a sprite is missing, moves noticeably between states, clips or lights oddly.
The displayed lamps and transport restraints are static art, not new mechanics.

Try a normal processing batch first. Compare its convenience and crew effort with
ordinary dismantling. No separate demonstration of native power consumption is
required; the native/OCF patterns are the precedent.

Check the behaviour we added while using the fixture:

- A batch removes one 24 kg panel and produces all listed results, including residue.
- Interrupting power stops **our work progress**; restoring power does not earn
  credit for the intervening blackout.
- Replacing a partly processed panel does not transfer its progress.
- A tray that cannot hold the complete batch keeps the input and progress. Clearing
  it and resuming produces one batch, not a duplicate or partial result.
- Save/reload a partly completed job. Progress should remain on that panel, and the
  fixture should wait for Start / resume queue. If changing the cycle setting,
  check that this panel keeps its old duration and fresh panels use the new one.

Report the visible result and any Phobos error in the BepInEx log. There is no need
to rerun unchanged upstream machinery checks. Try other time speeds or conflicting
hauling orders if they expose a specific issue in normal use.

## Implementation and current limits

- Uses native active/idle power coefficients and the powered-tick pattern already
  used by OCF. It adds no independent electrical network and does not debit power
  a second time.
- Phobos Framework owns construction registration and native ingredient integration.
  Queued legacy Phobos action names translate at lookup; see the migration guide.
  Native construction effects are not crash-atomic; faults require inspection.
  Our service owns processing progress, input binding and multi-output completion.
- Progress, chosen cycle duration and recipe revision use native saved conditions on the input object.
  Session clocks and run permission are transient. Long unobserved gaps pause work;
  there is no production while the ship is unloaded.
- The service stages and validates the whole result, plans non-overlapping tray
  placements, and retires the input only after all results are present. Insertion
  failures remove staged products while retaining the input. A native fault during
  irreversible input cleanup pauses processing and is logged rather than retried.
- Output masses are checked at runtime. Changed material definitions that invalidate
  this recipe stop processing instead of silently creating or losing material.
- Artwork is original Phobos imagery with modest geometric normal maps. Runtime
  machinery definitions are authored by Phobos against native game interfaces.
  Neither game's nor dependency's artwork/definitions are bundled. The invisible
  feed container remains native behaviour; the 8 x 8 output is inventory capacity,
  not an 8 x 8 physical extension to the machine. The owner approved the 0.1.4 in-game artwork; new behaviour remains unverified.
- This is an electrical processing implementation, not a complete thermal model.
  Dedicated coolant circulation, heat rejection, tool wear and consumable gas are
  not implemented. The 30 kW / 60 s rating is a gameplay design value, not a claimed
  measured cutting rate. External cutting and autopilot remain later work.

New IDs are prefixed `PhobosShipbreaker`. Recipe revision 1 is saved on a panel
when work starts; unknown revisions are rejected and can be cancelled explicitly.
Do not silently change saved progress meanings or rename item IDs in later builds.

## Build and offline checks

Run `scripts/build-shipbreaker.ps1 -OstranautsPath <local-game-folder>`.
It verifies art-master hashes, exports the world/normal/portrait set without
smoothing, compiles against locally installed assemblies, runs the focused logic checks
and creates `dist/PhobosShipbreaker-P0/` plus a ZIP. It also builds, checks and
packages Phobos Framework independently at `dist/PhobosFramework-P0/` and ZIP.
The consumer tests reference the actual framework assembly. It never writes to the game,
load order or saves. The tests exercise dependency version/data gates, missing and
changed template contracts, construction registration completeness, definition
publication rollback and repeat registration, construction recipe limits and total
material/work accounting, material balance, blackout accounting,
input identity, saved-state validation, console command routing, batch placement and insertion-failure
recovery. They do not claim an in-game test or re-test ordinary upstream power use.

The 0.2.0 offline integration check loads local native game definitions, publishes
our machine family and invokes native installable generation with no OCF/SWB
data or plugin loaded. It also checks queued-name migration, optional bench menus,
changed material masses, partial registration failure and competing providers.
That does not run Unity gameplay or verify every third-party interaction.

See `THIRD-PARTY.md` in the package for dependencies and asset provenance.
