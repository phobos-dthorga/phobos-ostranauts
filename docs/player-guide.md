# Current player guide

New here? Read [getting started](getting-started.md) for download availability,
prerequisites and experimental status. Need help? See [support](../SUPPORT.md).

Agriculture 0.2.0 is a new prepared candidate with visible crop growth and furnished cooking equipment: see the [cultivation and cooking guide](agriculture-player-guide.md). It requires Framework 0.17.0; optional C1 integration uses Shipbreaker 0.14.0. Owner gameplay evaluation is pending.


**Prepared versions:** Phobos Framework **0.17.0**, Shipbreaker **0.15.0**, Auto Nav
**0.10.1**, built against Ostranauts **1.0.1.5** / BepInEx **5.4.23.5**.
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
with a separate exterior radiator, hot-state saves and local/C1/F3 controls. Its
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
| Scrap reclaimer | Two sections at 75 min each, then 45 min assembly | K-Leg/VORB scrap, K-Leg fixer, San Diego Halvorson; [reclaimer guide](scrap-reclaimer.md) |
| Exterior grabber | 60 min | K-Leg/VORB scrap, K-Leg fixer, San Diego Halvorson |
| Hull chute | 30 min | Same industrial suppliers |
| Residue collector | 40 min | Same industrial suppliers |
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

Auto Nav 0.10.0 adds [console-specific flight settings and safer starts](auto-nav-flight-profiles.md).
Set cruise and arrival speed in **Details**, and stopping distance with the
existing dial. F3 accepts `phobosnav cruise <m/s>`, `phobosnav arrivalspeed <m/s>`
and `phobosnav arrival <km>`. These numeric defaults stay with that console;
active/suspended flights retain their captured values. All RCS commands now
respect the selected throttle including turning. Fly/Resume refuses an approach
without enough conservative RCS braking room; Details explains the refusal.
Rare module salvage joins the existing acquisition paths, using unchanged art.

Auto Nav 0.8.0 adds [automatic RCS docking](auto-nav-docking.md) as a separate
maneuver within 10 km of the hull. Request Comms clearance, select that target,
then use Details → Dock or `phobosnav dock`. Keep Comms/docking controls open for
final attachment. Reload suspends docking for explicit Resume.

Auto Nav 0.7.0 adds the [Polaris instrument panel](auto-nav-instruments.md):
rotary AUTO/RCS preference and stopping distance, clear live flight readings,
Fly/Resume, Stop/Coast and scrollable Details. AUTO prefers a usable running torch;
it does not start a cold reactor or override no-wake restrictions. Arrival remains
locked for active/suspended flights; stop before choosing a new distance.

Auto Nav 0.6.0 adds [torch-preferred travel and approach braking](auto-nav-torch.md).
Start the reactor normally, then begin a new flight. Native no-wake zones,
alignment and braking room determine when the torch is useful; RCS handles
turns, fine corrections and restricted approaches. Use **phobosnav torch off**
for RCS-only operation. Existing saved RCS-only flights retain that choice.

Look for **Phobos' Asterel N1 Polaris Auto Nav Module** in shops or table construction.
The [Auto Nav economy guide](auto-nav-economy.md) lists sellers, conditions,
prices, repair materials, Restore and dismantling. Its package is still Phobos Auto Nav.

Install the module in a compatible navigation console and use its **Fly** and
**Stop / Coast** controls. F3 equivalents are `phobosnav fly` and `phobosnav stop`.
Use the console's **Edit** mode to drag the panel into available space, then
leave Edit before flying; the game deliberately locks pause while editing.
Auto Nav 0.4.1 corrects the failed dragging / "can't find mod" issue in 0.4.0.
Version 0.4.2 corrects its oversized footprint and keeps the artwork and placement
bounds aligned. Reopen the console after updating; existing modules are supported.
Version 0.5.0 also widens it to the full standard column (25% of the board), keeping
the confirmed 20% row height and the original artwork's corner/screw shapes.
If the board is full, rearrange or remove another panel to make space.
Version 0.4.3 favours fuel-saving coasting: at the default 100 m/s cruise, stop
correcting at 7.5 m/s velocity error and resume above 10 m/s, subject to tighter
sideways-drift and braking checks. It no longer chases heading while coasting.
Arrival-speed tolerance is unchanged. `phobosnav settings` shows the adjustable
coasting values; see the [coasting policy](auto-navigate-adaptation.md#fuel-conscious-coasting-043).
Version 0.5.0 adds [saved flights](auto-nav-persistence.md): active flights resume
after load-time checks, preserving their target, profile and elapsed time. Blocked
flights stay suspended for **Resume** / `phobosnav resume`. Set
`Persistence.ResumeAfterLoad = false` for manual resumption. Stop/arrival remain
stopped after reload. Older saves without flight records remain idle.
Short-range approaches below **5,000 km** are the current goal; Auto Nav has no
minimum engagement range. New configurations stop at **1 km**, adjustable down
to **100 m** subject to larger hull clearance. Existing settings stay unchanged:
while disengaged, `phobosnav arrival 1` saves that console's 1 km default. `phobosnav fly 0.5`
requests 500 m for one flight. These distances are centre-to-centre, and arrival
is a band rather than an exact docking position. Status reports effective range.
Choose a ship/station target and use the [Auto Nav guide](auto-navigate-adaptation.md)
for settings and integration limits. Stopping clears commanded thrust: the ship
coasts. Stop is not emergency braking. Fly does not dock; obstacle avoidance and
continuous relative-position holding remain unimplemented.

The grabber currently receives manually loaded detached walls. It does not cut
attached hull. The combined scrap reclaimer is now available; asteroid-water
processing and persistent cargo release remain future work. See the
[residue contract and next processing stage](residue-material-contract.md).

Translation catalogs, language settings and contributor guidance: [Localization](localization.md).

## Industrial controls (0.10.0)

[Console and equipment panel guide](industrial-console-player-guide.md): a 3 x 3 ship-bound workstation, local Control Panels, automatic grouping, search, Attention and routing. The current Shipbreaker 0.15.0 requires Framework 0.17.0 and includes [shared observations](shared-console-observations.md). Prepared for owner testing; no in-game validation claimed.
