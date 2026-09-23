# Phobos Shipbreaker: first playable build

Version **0.1.2**, built against Ostranauts **1.0.1.4**, BepInEx **5.4.23.5**,
Crafting Framework **0.8.71** and Salvage Workshop **0.8.71**.
Build and offline logic checks passed. **In-game behaviour has not yet been tested.**
The owner's game, installed packages, load order and saves were not changed.

## What this build provides

A powered dismantling fixture, built at the existing Salvage Workshop workbench.
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
At the existing workbench, make **two Dismantling Fixture Assembly Sections**,
then combine them with **Powered Dismantling Fixture**:

| Stage | Inputs | Result | Work |
| --- | --- | --- | --- |
| Make a section, twice | 50 steel, 24 aluminium, 10 mechanical parts, 2 electronic parts per section | One unpowered 80 kg, 4 x 4 section per craft | 120 seconds each |
| Finish the fixture | Two sections | One 160 kg, 4 x 4 fixture | 60 seconds |

Total work remains 300 game seconds. Each section is a separate, unstackable
physical item; arrange room for both near the workbench. It has no powered
processing, installation or generic scrap role of its own. Native install, uninstall,
damage and repair behaviours are adapted from Salvage Workshop's machinery.

This fixes a registration problem found during the material-use review:
Crafting Framework 0.8.71 caps a craft at 100 input units; the earlier one-step
recipe requested 172. The new stages use 86, 86 and 2 units. Existing fixture
identities, processing recipes, saved progress, footprint and capacity are
unchanged; the new section uses `PhobosShipbreakerSection`. No existing save
needs conversion for this added construction item. No in-game migration was tested.

The [material-use review](shipbreaker-material-uses.md) maps these outputs into
existing repairs and Salvage Workshop recipes, including their remaining inputs
and the limits of their mass accounting.

## Install the prepared package

Required existing dependencies: **BepInEx 5**, **Crafting Framework 0.8.71 or later**,
and **Salvage Workshop**. Later upstream versions are not automatically certified;
the current reference versions are listed above. Common Sense hauling is optional.

From this repository, close the game and run
`./scripts/install-mods.ps1 -Mods Shipbreaker`. It handles both destinations,
dependency preflight, enabling the native entry and backing up updated files.
See [one-command installation](installing-mods.md) for the double-click launcher
and options. The manual steps below are for a standalone ZIP without the installer.

1. Exit Ostranauts normally before copying the package.
2. From the ZIP, copy `BepInEx/plugins/PhobosShipbreaker/` into the game's matching
   plugin directory. Do not copy other mods' libraries or overwrite original game
   data.
3. Copy `Mods/PhobosShipbreaker/` into `Ostranauts_Data/Mods/`.
4. Enable the native data mod after Crafting Framework and Salvage Workshop in
   the game's mod list. Both the plugin and the native data package are needed.
5. Start a **separate test save**. Do not use the player's real save for this first
   build. Retain both packages when loading any save that contains the fixture
   or residue; removing a content mod from such a save is not a supported migration.

At startup the BepInEx log should include `Shipbreaker definitions ready`.
Version 0.1.2 checks the loaded Framework version, enabled native dependencies,
required definitions and the template relationships used for storage and power.
It prepares private copies before publishing any Phobos definitions; a failure
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

1. At an installed Salvage Workshop workbench, build two **Dismantling Fixture
   Assembly Sections**, then join them with **Powered Dismantling Fixture**.
   To skip gathering construction stock in the test save, the existing F3 console
   accepts `spawn PhobosShipbreakerLoose`; use the normal Install action to place it.
   `spawn ItmWall1x1Loose` supplies a comparison panel if needed. These are the
   game's existing commands, not a Phobos diagnostic mode.
2. Install on a clear 4 x 4 floor area with the normal electrical connection.
3. Open its **Inventory** and place ordinary loose panels in **Wall-panel feed**.
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
- Crafting Framework owns construction recipe registration and ingredient handling.
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
- The work deck artwork is a temporary runtime reference to an existing native
  4 x 4 asset. Machine templates are derived at runtime from the installed Salvage
  Workshop package. Neither game's nor dependency's assets/definitions are bundled.
- This is an electrical processing implementation, not a complete thermal model.
  Dedicated coolant circulation, heat rejection, tool wear and consumable gas are
  not implemented. The 30 kW / 60 s rating is a gameplay design value, not a claimed
  measured cutting rate. External cutting and autopilot remain later work.

New IDs are prefixed `PhobosShipbreaker`. Recipe revision 1 is saved on a panel
when work starts; unknown revisions are rejected and can be cancelled explicitly.
Do not silently change saved progress meanings or rename item IDs in later builds.

## Build and offline checks

Run `scripts/build-shipbreaker.ps1 -OstranautsPath <local-game-folder>`.
It compiles against locally installed assemblies, runs the focused logic checks
and creates `dist/PhobosShipbreaker-P0/` plus a ZIP. It never writes to the game,
load order or saves. The tests exercise dependency version/data gates, missing and
changed template contracts, construction registration completeness, definition
publication rollback and repeat registration, construction recipe limits and total
material/work accounting, material balance, blackout accounting,
input identity, saved-state validation, console command routing, batch placement and insertion-failure
recovery. They do not claim an in-game test or re-test ordinary upstream power use.

The 0.1.2 offline source audit found all 51 required definitions and the four
expected machine relationships in the installed enabled dependency files plus
our proposed native data. That is not a live-game registration result. During
the owner's next fixture test, the new console report should confirm both
construction stages registered. There is no need to uninstall dependencies from
an existing save to demonstrate the error handling.

See `THIRD-PARTY.md` in the package for dependencies and asset provenance.
