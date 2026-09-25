# Current player guide

Regional acquisition now covers the current vanilla solar system: see the
[solar-system economy guide](solar-system-economy.md) for availability, native
price factors and limits. The regional builds require Framework 0.23.0+.

[Equipment and item references](item-references.md): functions, use, acquisition, prices and service information for each mod.

Current optional [completion cues](shared-completion-cues.md) use Framework 0.21.0
with Shipbreaker 0.19.0, Agriculture 0.8.0 and Auto Nav 0.14.0. Watches share one
quiet volume/mute setting and never replay old events after loading.

For equipment placement, see the [native INSTALL catalogue and tab locations](install-catalogue.md).

Shipbreaker 0.19.1 introduced a
[pending-construction room-load mitigation](shipbreaker-room-load-mitigation.md).
Keep that grid guard and update to Framework **0.23.1** for a second trigger:
native loading could discard lightly worn construction markers as zero-health
objects before the guard ran. The follow-up preserves recorded damage and
progress; neither guard restores gas lost before saving. The first owner-tested
reload succeeded; gameplay confirmation of the wear-related follow-up is pending.

New here? Read [getting started](getting-started.md) for download availability,
prerequisites and experimental status. Need help? See [support](../SUPPORT.md).

Agriculture is a prepared candidate with visible crop growth, cooking equipment and optional [water conduits](agriculture-water-conduits.md): see the [cultivation and cooking guide](agriculture-player-guide.md). Nutrient-solution piping requires Framework 0.19.0; optional C1 integration uses Shipbreaker 0.14.0 or later. Owner gameplay evaluation is pending.


**Prepared versions:** Phobos Framework **0.23.1**, Shipbreaker **0.22.0**, Auto Nav
**0.16.0**, built against Ostranauts **1.0.1.5** / BepInEx **5.4.23.5**.
These versions support ordinary saves. Builds and offline checks do not establish
in-game compatibility. This guide describes the prepared packages, not a claim
that those packages are already installed or that merchants have restocked.

Use this page for the current operating sequence. The linked equipment guides
provide details; dated research reports describe the evidence available then.

Auto Nav now requires [live native sensor contact](auto-nav-sensors.md).
Contact loss suspends and clears owned thrust; the ship coasts until manual
control or an explicit Resume after contact returns. Emitting sensors remain
under player control. A known station marker alone does not authorize guidance.

The [F6 electric furnace](furnace-player-guide.md) is now a prepared casting candidate,
with separate finite cooling, hot-state saves and local/C1/F3 controls. Version
0.17.0 adds [R4 aluminium feed and cold product collection](furnace-material-routing.md),
while batch Seal, Start, Equalize and Release remain explicit crew actions. Its
first in-game cycle and new artwork still await owner evaluation.

## Install or update

Close Ostranauts and double-click **Install or Update Mods.cmd** in the repository
root. It installs the prepared Auto Nav and Shipbreaker packages and automatically
includes their required Phobos Framework. BepInEx 5 must already be installed.
See [installation and recovery](installing-mods.md) for individual-mod selection,
verification and build instructions. Building is separate from installing.

Crafting Framework, Salvage Workshop and Auto Navigate are not dependencies.
Retain other providers when your save or other mods use their content. Original
Auto Navigate must be disabled for our Auto Nav to engage; installing ours does
not disable it automatically. Shipbreaker 0.22.0 requires Auto Nav 0.16.0+; [selected-G4 capture](shipbreaker-capture.md) uses existing N1/N2 hardware.

After launch, these F3 commands report the actual loaded versions and readiness:

```text
phobosframework status
phobosshipbreaker dependencies
phobosnav status
```

## Obtain the equipment

Buy equipment at its normal merchants, or assemble it at an installed **Bar Table
or Dining Table**. An existing supported workbench is optional. Assembly uses a
Mortorq tool and soldering tool, plus materials; it does not consume those tools.

| Equipment | Unmodified assembly work | Ordinary acquisition |
| --- | ---: | --- |
| Dismantling fixture | Two sections at 60 min each, then 30 min assembly | Broken stock at K-Leg/VORB scrap suppliers; occasional usable stock at K-Leg's fixer; new at San Diego's Halvorson |
| Scrap reclaimer | Two sections at 75 min each, then 45 min assembly | K-Leg/VORB scrap, K-Leg fixer, San Diego Halvorson; [reclaimer guide](scrap-reclaimer.md) |
| Exterior grabber | 60 min | K-Leg/VORB scrap, K-Leg fixer, San Diego Halvorson |
| Hull chute | 30 min | Same industrial suppliers |
| Residue collector | 40 min | Same industrial suppliers |
| N2 Pursuit module | 30 min | Polaris pristine merchant offer or table construction; same electronics bill as N1 |
| N3 Fire Control System | 30 min | Polaris pristine merchant offer or table construction; same electronics bill as N2 |
| Auto Nav module | 30 min | Navigation offers in the economy guide; rare native module salvage |

Stock is probabilistic and appears through normal merchant restocking. Restarting
does not force new inventory. Broken equipment needs **Repair**; functional worn
equipment uses **Restore**. Fetching, skills and interruptions affect elapsed
work. [Prices, bills, stock conditions and maintenance times](equipment-economy.md)
are the authoritative balance reference.

Equipment now uses **Phobos' Asterel** electronics and **Phobos' Rivetline**
industrial model names. See the [equipment name directory](equipment-branding.md)
for the names to look for in shops and construction menus. The role names below
remain shorthand; commands and saved IDs are unchanged.

## Install the connected Shipbreaker

Arrange the equipment flush together, with matching four-tile widths:

```text
SPACE       [ exterior grabber, 4 wide x 3 deep, arms outward ]
HULL        [ chute, 4 wide x 1 deep, OVER four intact walls  ]
INTERIOR    [ processor, 4 x 4, loading mouth toward chute   ]
```

**Keep the four walls.** They provide the pressure seal. The processor needs
interior floor and accessible space alongside it. The grabber needs clear space
outside. There must be no gap or sideways offset between the three pieces.
Build the native electrical conduit separately and power the grabber and
processor. See [mounting and rotation](shipbreaker-hull-intake.md).

The collector is optional: the processor works with its own product tray. Mount
a collector over **two intact exterior walls**, with its service panel inward
and receiving pocket outward, clear exterior space and structural floor inside.
It requires its own electrical connection and a structural-floor route from
the processor. It does not cross gaps, cargo webbing or a docked ship.

## Load, run and unload

1. With crew able to reach the exterior grabber, right-click it and choose
   **Inventory**. Load a detached **ordinary wall**, not a floor, Whipple,
   aerodynamic or DuraWal panel. Use separate, empty, uninstalled 24 kg panels;
   separate stacks before loading. The grabber can hold other solids, but that
   does not make them valid processing inputs.
2. Select awake crew beside the processor, press **F9**, and choose
   **Start / resume pipeline**. At defaults, intake takes 5 powered seconds and
   processing takes 60 powered seconds per panel. Four panels fit in the feed;
   processing handles one at a time.
3. Right-click the processor and choose **Inventory** to collect its products:
   two small mechanical parts, two aluminium scraps, two carbon-fibre scraps,
   six steel scraps and one 13 kg identified R2 residue packet. Total mass is 24 kg.
   Started revision-1 jobs still yield the old unclassified mixed residue.
4. To collect residue automatically, right-click the collector → **Control Panel**,
   select the processor with **Link**, then **Start transfers**. Alternatively
   choose the collector through F9 → **Output routing**, then start
   collection at the collector. Linking alone does not start it.
5. Empty the collector through its **Inventory**. Four packets fill it (52 kg).
   Collected residue remains aboard and counts toward ship mass. Link the fixture
   directly to the [scrap reclaimer](scrap-reclaimer.md), or link the collector
   output to its feed. Start input transfers and processing separately at the
   reclaimer. Each identified R2 packet returns 3 kg steel, 1 kg aluminium and
   9 kg rejects; link its output to a collector with the rejects filter. See
   [automatic routing](automatic-material-routing.md) for the controls. Legacy
   packets remain unclassified storage cargo; collection never ejects material.

**The processor's normal Inventory is output.** For standalone operation, F9 →
**Manual feed (fallback)** opens its separate wall feed; load there and Start.
The chute has no inventory. This distinction explains why a wall cannot be fed
through the processor's ordinary Inventory.

Current processor controls are F9/F3. Its own right-click Control Panel is planned
after the current gameplay evaluation; the collector already has that action.

## Interruptions and settings

Pause retains panel work. Cancel resets work on panels already inside the
processor without deleting them. On reload, panel recipe, progress and duration remain,
and the collector keeps its selected partner, but both systems wait for you to
start them. Short transfer timers reset; their physical cargo stays at the sender.
Full output waits with cargo retained. Nothing processes while its ship is unloaded.

Change settings with the game closed, then restart. Shipbreaker uses
`BepInEx/config/phobosgekko.ostranauts.shipbreaker.cfg`; available controls include
cycle time, electrical demand, queue continuation, transfer time and the F9 key.
An already-started panel keeps its duration and recipe outputs. Unknown saved
recipes stop with the panel retained; status explains the next action.
[Job compatibility](processing-job-compatibility.md) and
[complete settings and commands](shipbreaker-first-build.md).

| Symptom | Next useful check |
| --- | --- |
| Wall rejected or inventory grey | Use grabber Inventory or explicit Manual feed; check crew reach and exact wall type, stack and contents |
| Pipeline not connected | Check flush placement, facing, intact supporting walls and clear exterior cells |
| Processor waiting | Read F9/status for power, feed eligibility or space for a complete output batch |
| Collector waiting | Check pair, Collect state, floor route, clear mouth and its four-packet capacity |
| No equipment in a shop | Allow normal restocking; stock is not guaranteed on every refresh |

For a concrete failure, send the relevant status text and a screenshot:

```text
phobosshipbreaker status
phoboscollector status
phobosshipbreaker dependencies
```

There is no prerequisite isolated test of ordinary power draw. Focus on the
connected workflow, retained materials and any actual failure you encounter.

## Auto Nav and current limits

The [Polaris flight hub](auto-nav-instruments.md) is one tall instrument shared by
N1, N2 and N3. Use native **Edit** to place its new 25%-wide, 80%-high footprint in a
clear column. Existing compact placements do not expand automatically or move
other instruments. Keep the native map, sensors, warnings and Comms available.

- **Navigation:** Approach, Dock, explicit Approach & Dock, propulsion preference,
  cruise speed, arrival speed and separation. Range, signed closing speed and
  relative speed are separate readings. Active docking shows clearance, captured
  ports, alignment and progress directly on this page.
- **Pursuit:** working [N2](auto-nav-pursuit.md) adds Rendezvous and continuous
  Follow with separation and cruise controls.
- **Fire:** [N3 Fire Control System](auto-nav-fire-control.md) adds independent
  observations, 1–9 volleys, group ownership, guarded Engage and optional RCS
  aiming. N2 no longer grants firing permission. Navigation/offensive targets are
  separate; Cease Fire retains Follow and offensive hold until Return to Native.
- **Systems:** essential propulsion readings and native torch controls. Manual
  propulsion actions relinquish automation and retain native restrictions.
- **Details:** diagnostics/help only. Routine controls never require scrolling.

Resume, Disengage and Cease Fire remain on every page. **Disengage clears thrust
and allows coasting; it is not emergency braking.** Native clearance and compatible
assigned ports are required for docking. Approach & Dock stages 1 km beyond
protected hull clearance, matches motion, then checks the RCS terminal handoff.
A failed handoff suspends with its reason and requires Resume. Ordinary Approach
does not dock. See [docking operation](auto-nav-docking.md).

Pursuit and both docking phases suspend after loading. No fire permission or live
thrust is saved; page changes and display refresh cannot authorize actions.
[Qualified sensor contact](auto-nav-sensors.md) is required throughout. Missing
measurements are unavailable, not zero. Saved flights keep their exact hardware,
target and profile; stop before replacing them.

N1/N2 acquisition, repair and salvage remain in the [economy guide](auto-nav-economy.md)
and [N2 guide](auto-nav-pursuit.md). Either working module supplies navigation and
docking. N3 alone supplies Fire and Systems; all combinations share one hub.
Acquire N3 through the Polaris merchant or the same two-electronics/30-minute
construction route as N2. Native spawning: `spawn PhobosNavModFireControl`.

Short-range approaches below **5,000 km** remain the immediate goal. No general
obstacle avoidance, guaranteed pursuit or intact boarding guarantee is supplied.
Use a clear route and keep specialist native instruments accessible. Numerical
checks and offline layout proofs are not gameplay validation; see the
[validation record](auto-nav-hub-validation.md). No installation or
publication is implied by this prepared redesign.

## Industrial controls (0.10.0)

[Console and equipment panel guide](industrial-console-player-guide.md): a 3 x 3 ship-bound workstation, local Control Panels, automatic grouping, search, Attention and routing. The current Shipbreaker 0.22.0 requires Framework 0.23.0 and Auto Nav 0.16.0 and includes [shared observations](shared-console-observations.md) and optional [shared completion cues](shared-completion-cues.md). Prepared for owner testing; no in-game validation claimed.

Agriculture now supports [finite potato and lettuce nutrient-solution piping](agriculture-nutrient-solutions.md) through its W2 supply and irrigation conduits.
