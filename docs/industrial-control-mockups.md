# Industrial control panels: text mockups

24 September 2026. **Design preview, not running game UI.**
Owner approved this direction. Actual 0.10.0 controls and limits are in the
[player guide](industrial-console-player-guide.md); these layouts remain illustrative.
Read the [research and implementation brief](industrial-control-console.md).
Names, positions, progress and sensor values below are illustrative; capacities
and input/output identities reflect the current equipment. Labels are runtime
translation text, never painted into the artwork.

## Central console: overview

```text
+--------------------------------------------------------------------------+
| o  PHOBOS INDUSTRIAL CONTROL                  Ship: <current ship>     o  |
|    Console: Workshop control                  Operator: <crew>    [Close] |
+--------------------------------------------------------------------------+
| [Overview] [Equipment] [Routing] [Attention 1]                            |
+-----------------------+--------------------------------------------------+
| EQUIPMENT             | SELECTED: SCRAP RECLAIMER                        |
|                       |                                                  |
| > Scrap reclaimer     | PROCESSING             MATERIAL RECEIVING       |
|   RUNNING             | RUNNING                ARMED - WAITING FOR FEED |
|                       | [Pause processing]      [Pause receiving]        |
|   Dismantling fixture |                                                  |
|   WAITING FOR INPUT   | Current batch  [########----] 80 / 120 s         |
|                       | Feed: 2 / 4 packets, including active batch      |
|   Exterior grabber    |                                                  |
|   EMPTY               | COOLING                 OUTPUT                   |
|                       | Room: 34 C / 90 kPa      Next batch fits         |
|   Hull chute          | Cooling check: ready    Reject destination:     |
|   CONNECTED           |                         Reject collector         |
|                       |                         [View destination]       |
|   Reject collector   |                                                  |
|   3 / 4 PACKETS       | POWER                                            |
|                       | Supply state: powered                            |
|                       | Configured demand: 12 kW + 2 kW when feeding    |
+-----------------------+--------------------------------------------------+
| ! Collector nearly full. Empty it locally to keep the line running.      |
| [Inspect issue]                                    [Pause industry]      |
+--------------------------------------------------------------------------+
```

The right side is the selected machine's reusable detail view. A long equipment
list scrolls; there is no need for a fixed screen for each machine. The Attention
count is derived from structured states; this example's near-full warning is a
proposed presentation rule, not a new gameplay block. A full collector stops
transfers by the existing capacity checks.

**Pause industry** pauses our supported processes and receiving routes on this
ship. Its result lists successes and failures. It does not turn off the reactor,
disable life support or pretend to stop unsupported equipment.

## Routing: the material path is explicit

```text
+--------------------------------------------------------------------------+
| PHOBOS INDUSTRIAL CONTROL                      [Overview] [Routing] [X] |
+--------------------------------------------------------------------------+
| SOURCE / OUTPUT        DESTINATION / INPUT           FILTER / STATE      |
| Dismantling fixture -> Scrap reclaimer                Identified feed     |
|                         [Input settings]             RECEIVING ARMED     |
| Scrap reclaimer    -> Reject collector                Terminal rejects    |
|                         [Input settings]             PAUSED              |
+--------------------------------------------------------------------------+
| SELECTED: SCRAP RECLAIMER / INPUT                                        |
| Receives from: Dismantling fixture, aft workshop                          |
| Accepts: identified reclaimer feed                                      |
| Floor route: connected                  Capacity: 2 / 4 packets          |
| [Start receiving] [Pause receiving] [Change source...] [Unlink input]    |
|                                                                          |
| Sending products has separate settings: [View output routing]           |
| Metal products stay in their trays for collection.                       |
+--------------------------------------------------------------------------+
| A full destination waits. Cargo remains at its source.                   |
+--------------------------------------------------------------------------+
```

Provide source and destination pickers with friendly names and positions, plus
full identity in details. Do not require typing IDs for ordinary UI use. Keep
full IDs in F3 commands. Link replacement follows existing explicit-unlink rules.
No dragged line automatically creates an unsupported junction or shared bus.

A buffer is represented with its two independent roles:

```text
Fixture output -> Buffer input | Buffer output -> Reclaimer input
Reclaimer output -> Reject collector input
```

These are material routes. A console's ability to issue a command is separate.

## Local fixture panel

```text
+------------------------------------------------------------+
| o  DISMANTLING FIXTURE - AFT WORKSHOP                   o  |
| [Process] [Intake] [Output routing] [Details]       [Close] |
+------------------------------------------------------------+
| READY / PAUSED                                             |
| Current wall: [######----------] 24 / 60 s                  |
| Feed: 3 / 4 walls             Output: next batch fits      |
| Power: powered               Working demand: 30 kW        |
|                                                            |
| [Start / resume pipeline] [Pause pipeline]                  |
| Starting also arms the connected exterior intake.           |
|                                                            |
| Grabber -> Chute -> This fixture                            |
| Connected; detached ordinary walls only.                   |
| [Inspect intake]                                           |
|                                                            |
| [Open product tray]   [Manual feed...]                       |
| [Job details...]                                           |
+------------------------------------------------------------+
```

Open product tray and manual feed are local actions. When this same detail view
is shown at the central console, replace them with a read-only contents summary
and an explanation that handling requires local crew access. Do not open a
draggable inventory from across the ship. Cancel/reset progress belongs under
job details with its energy/progress consequence stated, away from ordinary pause.

## Reclaimer: two independent permissions

```text
+------------------------------------------------------------+
| o  SCRAP RECLAIMER                                      o  |
| [Process] [Input routing] [Output routing] [Details] [Close] |
+------------------------------------------------------------+
| PROCESS                        RECEIVING                   |
| PAUSED                         ARMED, WAITING              |
| [Start processing] [Pause]      [Pause receiving]           |
|                                                            |
| Batch: identified feed, 13 kg                               |
| [--------------------] 0 / 120 s                           |
| Feed: 1 / 4 packets, including active batch                 |
| Products: 3 kg steel + 1 kg aluminium + 9 kg rejects       |
|                                                            |
| ROOM CONDITIONS               OUTPUT                       |
| 34 C / 90 kPa                 Next batch fits              |
| Cooling check: ready          Rejects -> Reject collector  |
|                                                            |
| [Open feed] [Open product tray] [Job details...]             |
+------------------------------------------------------------+
```

Open feed/products are visible as interactive controls only under local access.
An armed empty processing queue says **Waiting for feed**; it must not appear
to be consuming processing power. Blocked cooling replaces the healthy state
with the precise reason, using both wording and an amber/red lamp.

## Grabber and chute: one intake page, target-specific status

```text
+------------------------------------------------------------+
| EXTERIOR INTAKE                                     [Close] |
+------------------------------------------------------------+
| Grabber -> Hull chute -> Dismantling fixture                 |
| CONNECTED        CONNECTED        PAUSED                    |
|                                                            |
| Selected: Hull chute                                       |
| Supporting walls: present     Alignment: correct            |
| Condition: intact             Type: passive passage        |
|                                                            |
| [Open fixture controls]                                    |
|                                                            |
| Grabber input: detached ordinary walls only.                |
| This assembly does not cut attached hull or vent a doorway. |
+------------------------------------------------------------+
```

For a selected grabber, replace the passive detail with intake progress,
power/input eligibility and a local Inventory action. Show any mounting or
alignment failure where the faulty connection appears. The screen does not
invent independent grabber automation or a door-opening control.

## Collector/buffer: contents and selected port

```text
+------------------------------------------------------------+
| RESIDUE COLLECTOR                                   [Close] |
| [Contents] [Input routing] [Output routing] [Details]        |
+------------------------------------------------------------+
| Receiving: PAUSED          Filter: terminal rejects        |
| Stored: 3 / 4 packets      Mass: 27 / 52 kg                 |
| From: Scrap reclaimer      Output: not linked              |
|                                                            |
| [Start receiving] [Pause receiving]                         |
| [Open inventory - local] [Read-only contents]               |
|                                                            |
| ! One packet space remains.                                |
| Collection retains cargo aboard. No material is ejected.   |
+------------------------------------------------------------+
```

The four-packet limit and mass ceiling are distinct: three 9 kg rejects weigh
27 kg, not 39 kg. Capacity should come from actual contents, not count times a
single assumed packet mass. Input settings and output settings never share an
ambiguous Unlink button.

## Workstation silhouette: proposed 3 x 3 footprint

```text
                 REAR / POWER CONNECTION
             +---------+---------+---------+
             |  screen |  screen | service |
             |    dark display   | housing |
             +---------+---------+---------+
             | left    | operator| right   |
             | keys    | position| keys    |
             +---------+---------+---------+
             | desk    |  chair  | desk    |
             | edge    |         | edge    |
             +---------+---------+---------+
                     FRONT / APPROACH
```

This is a composition guide, not an obstruction/socket map. Keep the central
seat legible in a 48 x 48 image and verify native use/sit placement before final
export. The panel art uses the approved subdued slate style; this world object
uses coarse pixel shapes and a restrained industrial palette. Neither includes
baked text, lighting wedges, conduits or a painted crew member.

## States the finished graphics must accommodate

- Ready, running, deliberately paused, armed/waiting, blocked and unavailable
  are distinct text/icon states. Colour is supplementary.
- Console power lost: commands disabled, explicit unavailable status; previously
  shown telemetry must not masquerade as a live healthy reading.
- No equipment: explain what this console supports and where to obtain it.
- Equipment removed or provider missing: keep the selection identifiable while
  explaining it is unavailable; never select another target silently.
- Local and remote views show the same process state; only access-sensitive
  controls differ. No screenshot-like duplicate state hidden behind the panel.
- Long translations wrap/scroll. Progress, quantities, names and button labels
  are all dynamic UI elements over the reusable blank frame.
