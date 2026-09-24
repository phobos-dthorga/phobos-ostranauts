# Phobos Shipbreaker: first playable build

Current candidate: **Shipbreaker 0.9.0 + Phobos Framework 0.9.0**, built against
Ostranauts **1.0.1.5** and BepInEx **5.4.23.5**. Offline checks pass; connected
gameplay validation remains pending. These are prepared-package versions, not
an assertion about the currently installed files. Start with the
[current player guide](player-guide.md) for acquisition and normal operation.

The approved **4 x 3 exterior grabber + 4 x 1 wall chute + 4 x 4 processor** now
form a connected intake. Load detached walls at the grabber; the chute carries
the same objects to the processor. Collect products from the processor's normal
Inventory. The internal feed remains saved, but opens only through the explicit
**Manual feed (fallback)** control. See the [placement and first-test guide](shipbreaker-hull-intake.md).

The 0.2.1 cumbersome-filter correction passed the native data check, but the owner
still reported a grey inventory and rejected walls. No successful in-game feed or
processing test is claimed. This version removes the two-grid ambiguity and adds a
separate native loading inventory which accepts both cumbersome and small solids.
Only eligible ordinary walls are processed; other cargo remains untouched.

Existing fixture IDs, dimensions, construction material bill and panel progress
are preserved. Equipment prices and maintenance changed in 0.6.0 as documented
in the economy guide; historical construction timings are superseded there.
The owner previously confirmed Framework's Missing-label fix and the processor's
visual match. Migration and the connected machine behaviour remain unverified.

The new [Residue Collector](residue-collector.md) adds a two-wide wall port with
a four-packet inventory, saved sender/receiver pairing, structural-floor route and
right-click controls. Choose a partner from either end; pairs survive reload and
collection resumes manually. Material stays aboard; ejection is not implemented.

Buy equipment from its intended merchants, repair second-hand stock, or build it
with tools at a table. See [equipment economy and maintenance](equipment-economy.md)
for prices, stock chances, material bills, work times and upgrade handling.

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

The output tray can retain prior results. New batches check space for every
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
| Identified R2 panel residue (legacy mixed residue for started R1 jobs) | 1 | 13 kg |
| **Total** | | **24 kg** |

These are explicit design yields using the native wall's material families, not
a verified bill of materials for its fictional manufacturer. The operation is
disassembly and separation; it does not melt the panel or purify mixed alloys.
The residue remains a real inventory item with mass, rather than disappearing.
It is a distinct definition without native trash/salvage category flags; the
existing trash sorter does not consume it. Retain, haul or move it to the paired
Residue Collector. It remains aboard; refining and recoverable release are not
implemented. The [residue contract](residue-material-contract.md) preserves these
unclassified packets. Fresh 0.8.0 jobs produce a separate identified R2 stream,
with a useful [reclaimer](scrap-reclaimer.md) consumer now implemented. Started
R1 jobs retain the original outputs. Reclaimer rejects remain physical cargo;
there is no delete-waste action.

Construction uses **100 steel, 48 aluminium, 20 mechanical parts and
4 electronic parts** in total, totalling 160 kg at the inspected native masses.
At an installed table or supported workbench, make **two Dismantling Fixture Assembly Sections**,
then combine them with **Powered Dismantling Fixture**:

| Stage | Inputs | Result | Work |
| --- | --- | --- | --- |
| Make a section, twice | 50 steel, 24 aluminium, 10 mechanical parts, 2 electronic parts per section | One unpowered 80 kg, 4 x 4 section per craft | 60 minutes each |
| Finish the fixture | Two sections | One 160 kg, 4 x 4 fixture | 30 minutes |

Total assembly work is 150 game minutes, plus fetching and interruptions. Each section is a separate, unstackable
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

Required dependencies: **BepInEx 5** and **Phobos Framework 0.9.0 or later**.
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
6. Load your ordinary save. Retain the content packages and required providers when loading any save that contains the fixture
   or residue; removing a content mod from such a save is not a supported migration.

At startup the BepInEx log should include `Shipbreaker definitions ready`.
Version 0.2.0 checks the loaded Phobos Framework version, our enabled native data,
and the native definitions used by the independent machinery/construction path.
It prepares definitions before publishing them; a failure
during publication restores previous dictionary entries and removes new ones.
Processing stays disabled until all five construction recipes have registered too.
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

Follow the [hull intake placement and test steps](shipbreaker-hull-intake.md#first-owner-test).
The chute mounts **over four intact walls**; leave the walls in place. The grabber
sits outside, the processor inside, with all three touching and aligned.

At an installed Bar Table or Dining Table, the two original assembly recipes
build the processor. **Sealed Hull Chute** and **Exterior Panel Grabber** build the
new components. Buying or crafting is the ordinary-save route. If intentionally
using debug grants, native spawn commands can skip gathering construction stock:

```text
spawn PhobosShipbreakerLoose
spawn PhobosHullChuteLoose
spawn PhobosExteriorGrabberLoose
spawn ItmWall1x1Loose
```

Install normally, connect conduit, and load the detached wall through the
**grabber's Inventory** while nearby (ordinary exterior/EVA access applies).
Then stand beside the processor, press F9, check the connection status and choose
**Start / resume pipeline**. Collect the full result from the processor's Inventory.
A small solid may fit in the grabber without being a supported processing input.

For standalone processing, use **Manual feed (fallback)** in F9 or
`phobosshipbreaker feed`, put a wall in that explicitly named window, then Start.
The usual processor Inventory is output storage. Its saved internal feed and the
8 x 8 output tray retain their original identities and capacities.

Pause disarms intake and preserves panel processing progress. Cancel also clears
work on queued panels already inside the processor; it does not erase work on
panels still at the grabber. Reload waits for Start. Pending transfers retain their
physical cargo at the sender and restart only their short motion delay.

Check one complete batch before exploring interruptions. A blocked output retains
the panel and processing progress; clear space and resume. Report any failure with
`phobosshipbreaker status` and a screenshot. The targeted checks concern our new
layout, transfers, saved state and material accounting, not an isolated proof of
native power use. Older provider migration notes are in [Phobos Framework](phobos-framework.md).

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
| `Intake / TransferSeconds` | `5` | 1–60 powered seconds per transfer; pending cargo stays in the grabber across reload |
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
| `phobosshipbreaker` or `phobosshipbreaker help` | Command help, including optional native debug spawn commands |
| `phobosshipbreaker status [fixture-ID]` | List fixtures on the selected crew member's ship, their IDs, progress and control blockers |
| `phobosshipbreaker settings` | Show effective settings and the config filename |
| `phobosshipbreaker dependencies` | Read-only startup dependency versions, template checks and construction registration; no fixture selection needed |
| `phobosshipbreaker start [fixture-ID]` | Start/resume connected intake and processing, or standalone manual-feed work |
| `phobosshipbreaker pause [fixture-ID]` | Pause intake and processing, retaining material and panel progress |
| `phobosshipbreaker cancel [fixture-ID]` | Pause intake; clear work on panels inside the processor; keep material, no energy refund |
| `phobosshipbreaker feed [fixture-ID]` | Open the manual feed fallback explicitly |
| `phobosshipbreaker products [fixture-ID]` | Open the output tray |

Square brackets mean optional arguments; do not type the brackets. Omit the ID
when only one fixture is aboard. With multiple fixtures, copy the exact ID from
`status`; an ambiguous action is rejected. The selected crew member must be beside
that fixture for start, pause and cancel. Status/settings/help do not start work.

For deliberate debug setup, use the game's existing `spawn PhobosShipbreakerLoose` and
`spawn ItmWall1x1Loose` commands, then install the fixture normally. The Phobos
commands do not add a separate item-spawning system.

## Focused owner checks

The owner already approved the processor's visual fit in-game. Check new intake
or collector artwork during ordinary use; there is no need to repeat that approval
as a prerequisite. Send a screenshot if a sprite is missing, clips, changes scale
between states or lights oddly. Conduit is separately built; displayed lamps and
transport restraints are static art, not new mechanics.

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
  fixture should wait for Start / resume pipeline. If changing the cycle setting,
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

New IDs are prefixed `Phobos`; existing `PhobosShipbreaker` IDs remain unchanged.
Version 0.6.1 binds each job's output definitions and space checks to its saved
recipe revision. Only revision 1 is currently enabled. Unknown revisions stop
without changing the panel; cancellation is explicit. Historical revision-1
jobs without a duration retain the original 60 seconds. F9/F3 status reports the
selected revision. See [job compatibility](processing-job-compatibility.md).
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

Version 0.8.0 adds the [scrap reclaimer](scrap-reclaimer.md), R2 output for fresh
wall jobs and explicit collector support for identified residue and terminal
rejects. Historic revision-1 jobs remain unchanged; see the current player guide.
