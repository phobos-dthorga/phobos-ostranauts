# Residue Collector — 0.5.0 testing candidate

24 September 2026. Requires **Phobos Framework 0.5.0** and Shipbreaker **0.5.0**.
Implemented and checked offline against the local 1.0.1.4 installation; in-game
mounting, controls and transfers await owner testing. This adds to the connected
grabber/chute/processor candidate without changing its saved item IDs or recipes.

The collector moves existing mixed-panel residue from a selected processor to a
finite wall-mounted inventory. **Material remains aboard and still weighs on the
ship.** There is no jettison button, automatic destruction or persistent-space
release in this build. [Research and later release options](material-disposal-port-research.md).

## Build and placement

Build a **Residue Collector** at an installed Bar Table or Dining Table, or at an
already available supported workbench. It takes 60 seconds and consumes:

| Material | Units | Mass |
| --- | ---: | ---: |
| Steel scrap | 12 | 12 kg |
| Aluminium scrap | 4 | 4 kg |
| Small mechanical parts | 6 | 3 kg |
| Small electronic parts | 2 | 1 kg |
| **Collector** | **1** | **20 kg** |

The body is **two tiles along the hull and one tile deep** (32 x 16 world pixels).
Install it **over two intact exterior walls**, retaining both walls. Face its dark
collection pocket outward, cream service panel inward. Both adjacent inboard
tiles require structural floor; the two outward tiles must be clear of ship
structure and solid items, including docked neighbours. Repair damaged supporting
walls. Rotation is supported in quarter turns.

Connect native electrical conduit separately. Backing walls remain the native
pressure barrier. The collector is a sealed-transfer abstraction, not an open
door, replacement hull or simulated airlock.

Its normal **Inventory** is the only collector inventory: a 2 x 2 grid accepting
four separate 13 kg `PhobosShipbreakerResidue` packets, **52 kg payload maximum**.
Both installed and loose forms retain that finite storage/filter. Other items,
stacks, modified-mass residue and items with contents are refused. This collector
cannot take whole wall panels, ordinary trash or valuable recovered parts.

## Use

1. Stand an awake crew member beside its inboard service panel. Right-click the
   collector and select **Control Panel**. F3 `phoboscollector controls` opens the
   same window when there is one collector on the ship.
2. Use **Link** beside the chosen source processor. Alternatively, at the processor
   open F9 → **Residue destination / unlink**, and choose a receiving collector.
   Both ends must be installed, undamaged and unlocked on the same loaded ship.
   The pair is saved: one processor residue output to one collector input. Names
   and short IDs distinguish equipment; hovering over a Link button shows its full ID.
   An occupied endpoint refuses another partner until explicitly unlinked.
3. Press **Collect residue**. Default operation moves one available packet in five
   powered game seconds at 2 kW, then waits for more. Useful parts remain in the
   processor's normal product tray. The processor may keep working independently.
4. Use **Inventory** to empty the collector, or **Pause** to suspend it. A full
   collector waits without deleting input. Its full buffer does not discard the
   processor's remaining residue; ordinary processor output-space limits still apply.

Use **Unlink** at either endpoint before changing its partner. This pauses that
route and discards only its short transfer timer, retaining every item. If one
endpoint is gone, unlink the surviving endpoint. An orphan cannot automatically
claim a new machine or sever a newer pair. Linking from the processor does not
start the remote collector; press Collect at the receiver when ready.

The selected endpoints require a continuous cardinal route through **structural
flooring**, including floor beneath installed furniture. The path stops at walls,
missing floors and flexible exterior webbing. A closed door can be crossed only
where a structural floor tile remains underneath; no route is inferred merely
from a shared room, power connection or ship ID. The search visits at most 4096
cells and fails with an explanation if no route is found within that bound.

Flooring represents the underfloor service route in this first slice. No belt
segments, second deck or separate sender object are required. The collector's
construction bill accounts for its mechanism; there is no separately metered duct
material or distance-scaled transfer time yet. If that abstraction becomes too
generous, add explicit route infrastructure without silently changing old jobs.

The chosen route is validated during operation. A removed floor, moved endpoint,
changed grid, damaged wall or blocked exterior mouth pauses collection. Restore
the layout and press Collect to find a route again. Transfer never crosses to a
different ship, even when docked.

## Interruption and save behaviour

Cargo remains physically in the processor during the short transfer timer.
Completion uses Framework's checked move of the **same object**, preserving its
ID, mass and conditions. Power loss earns no transfer progress. Full storage
waits; restored capacity resumes. Pausing retains the short timer in the current
session. Moving/removing the selected packet invalidates that timer; another
packet cannot inherit its work. Ambiguous transfer failures pause and log the
problem rather than blindly retrying.

**Reload retains the pair and real cargo, but resets the timer and pauses collection.**
Press Collect to resolve the same full object IDs and recheck the floor route.
Already spent electricity is not refunded. No work accumulates while unloaded,
and an unexpected time gap pauses the collector. Missing, mismatched or invalid
links stop work; there is no nearest-machine fallback. The same checks run before
each transfer, so stale jobs cannot continue after unlinking or replacing a pair.

Native property maps store the link, following the electrical signal connection
pattern. Native equipment mode changes carry the ID and maps across to the new
form; damage/uninstallation still blocks use until repaired/reinstalled. Runtime
sessions never grant automatic restart. See [pairing research and API](material-port-pairing.md).
Existing pre-0.5.0 collectors have no saved links: link them once after upgrading.

The first filter is fixed to our exact residue definition. Framework's reusable
allowlist is available to other mods, but category editing, arbitrary exports and
native/common-mod filter synchronization are future features. Leave third-party
AUTO hauling off for the collector unless intentionally arranging crew delivery;
we do not alter another mod's settings or crew jobs.

## Console and settings

```text
phoboscollector help
phoboscollector status
phobosshipbreaker status
phoboscollector controls [collector-ID]
phoboscollector link <collector-ID> <processor-ID>
phoboscollector unlink [endpoint-ID]
phoboscollector start [collector-ID]
phoboscollector pause [collector-ID]
phobosshipbreaker settings
```

`link` always takes both full object IDs. `unlink` also accepts a processor ID,
including a loaded loose/damaged endpoint, so a lost receiver cannot trap its
sender. Omit the ID only when there is exactly one installed collector on the
selected crew's ship. Status includes full object IDs, logical port IDs and pair
ID; commands use the object ID alone, not the short display ID or logical port suffix.
Commands enforce the same crew access and gameplay checks as the panel.

In `BepInEx/config/phobosgekko.ostranauts.shipbreaker.cfg`, restart after changing:

| Setting under `[Collector]` | Default | Meaning |
| --- | ---: | --- |
| `TransferSeconds` | 5 | 1–60 powered seconds per packet. |
| `WorkingKilowatts` | 2 | Total operating draw, 0.05–100 kW; idle stays 0.05 kW. |
| `ContinueQueue` | true | False collects one packet per Start; true waits for subsequent packets. |

The collector always starts paused after loading. Capacity, footprint, accepted
definition and mass are fixed. The nominal operating cost per default cycle is
10 kJ / 0.00278 kWh; native ticker quantization can affect exact metering.

## Focused owner test

Use the separate test save. Obtain the collector through its recipe or the native
test command `spawn PhobosResidueCollectorLoose`, then install normally.

- Use a processor with recovered parts and at least one residue packet. Link it
  through the collector's Control Panel and start collection. Only residue should
  move; the same 13 kg remains aboard in the collector.
- During ordinary use, let the collector fill or pause it. Residue must remain
  available, with a useful waiting message. Resume after clearing space.
- Save/reload when convenient. Collected and pending cargo should remain in their
  respective inventories. The same pair should remain selected and paused; press
  Collect to restart without relinking.
- With another endpoint available, try linking an occupied endpoint: it should
  refuse. Unlink the original pair, link the new one, and confirm only the selected
  receiver collects residue. No basic power-draw proof test is needed.

Do not repeat isolated proof tests for basic power draw. If placement, controls or
transfer fail, send the screenshot and `phoboscollector status` output, plus
`phobosshipbreaker dependencies`. Rotation, route interruption and crew competition
are meaningful follow-up checks if a concrete issue appears.

## Implementation boundary

Framework 0.4.0 adds an immutable exact-ID filter, a short item-bound transfer
clock (also reused by intake) and bounded cardinal route search. It retains the
existing same-ship native transfer adapter. Framework 0.5.0 adds reusable saved
one-to-one port pairing. Shipbreaker owns endpoint discovery, floor/mount rules,
the port's powered service, native definitions, recipe and controls. There is no
global network scheduler, public recipe registry for disposal, or new external
dependency.

New saved IDs are the `PhobosResidueCollector` installed/loose/damaged family.
Keep Shipbreaker and Framework installed once saves contain this equipment.
Original graphics, source hash, exact built-in Imagegen prompt and export script
are recorded in [the art notes](../assets/phobos-residue-collector/README.md).
