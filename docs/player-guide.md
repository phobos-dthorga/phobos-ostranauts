# Current player guide

**Prepared versions:** Phobos Framework **0.7.0**, Shipbreaker **0.7.0**, Auto Nav
**0.3.0**, built against Ostranauts **1.0.1.4** / BepInEx **5.4.23.5**.
These versions support ordinary saves. Builds and offline checks do not establish
in-game compatibility. This guide describes the prepared packages, not a claim
that those packages are already installed or that merchants have restocked.

Use this page for the current operating sequence. The linked equipment guides
provide details; dated research reports describe the evidence available then.

## Install or update

Close Ostranauts and double-click **Install or Update Mods.cmd** in the repository
root. It installs the prepared Auto Nav and Shipbreaker packages and automatically
includes their required Phobos Framework. BepInEx 5 must already be installed.
See [installation and recovery](installing-mods.md) for individual-mod selection,
verification and build instructions. Building is separate from installing.

Crafting Framework, Salvage Workshop and Auto Navigate are not dependencies.
Retain other providers when your save or other mods use their content. Original
Auto Navigate must be disabled for our Auto Nav to engage; installing ours does
not disable it automatically. Auto Nav is optional for onboard Shipbreaker use.

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
| Exterior grabber | 60 min | K-Leg/VORB scrap, K-Leg fixer, San Diego Halvorson |
| Hull chute | 30 min | Same industrial suppliers |
| Residue collector | 40 min | Same industrial suppliers |
| Auto Nav module | 30 min | See the navigation offers in the economy guide |

Stock is probabilistic and appears through normal merchant restocking. Restarting
does not force new inventory. Broken equipment needs **Repair**; functional worn
equipment uses **Restore**. Fetching, skills and interruptions affect elapsed
work. [Prices, bills, stock conditions and maintenance times](equipment-economy.md)
are the authoritative balance reference.

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
   six steel scraps and one 13 kg mixed-residue packet. Total mass is 24 kg.
4. To collect residue automatically, right-click the collector → **Control Panel**,
   select the processor with **Link**, then **Collect residue**. Alternatively
   choose the collector through F9 → **Residue destination / unlink**, then start
   collection at the collector. Linking alone does not start it.
5. Empty the collector through its **Inventory**. Four packets fill it (52 kg).
   Collected residue remains aboard and counts toward ship mass. It has no current
   refining or recoverable-ejection action. Retain or haul it as ordinary cargo.

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

Install the module in a compatible navigation console and use its **Fly** and
**Disengage** controls. F3 equivalents are `phobosnav fly` and `phobosnav stop`.
Choose a ship/station target and use the [Auto Nav guide](auto-navigate-adaptation.md)
for settings and integration limits. Stopping clears commanded thrust: the ship
coasts. This is not emergency braking, automatic docking, obstacle avoidance or
continuous relative-position holding.

The grabber currently receives manually loaded detached walls. It does not cut
attached hull. Shredders, recycling, asteroid-water processing and persistent
cargo release are not present in these versions. See the
[residue contract and next processing stage](residue-material-contract.md).

Translation catalogs, language settings and contributor guidance: [Localization](localization.md).
