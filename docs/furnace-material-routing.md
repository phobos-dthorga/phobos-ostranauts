# F6 material routing

25 September 2026. **Implemented in the Shipbreaker 0.17.0 candidate.**
Framework 0.20.0 is required by this combined release. Material routing reuses
existing Framework pairing, exact filters, physical transfers and transfer clocks;
it adds no public Framework API, new equipment or generated artwork.
Offline checks do not establish in-game compatibility. Owner evaluation remains.

## Operating sequence

```
R4 product tray -- aluminium only --> F6 physical charge bin
                                      |
                        crew seals, starts, equalizes and releases
                                      |
F6 released product tray -----------> hull collector --> crew hauling

R4 residue outlet -----------------> its existing residue collector
F6 cooling connection -------------> existing selected cooling assembly
```

1. Install and power the R4 and F6 on the same player-owned ship. Prepare the
   furnace cooling connection and let its instruments establish valid readings.
   Keep structural floor between the material endpoints.
2. In the F6 local Control Panel or C1 routing page, select the R4 as its input.
   Alternatively choose the separate **Aluminium output** on the R4. Its residue
   output remains independently paired. Receiving and processing are distinct.
3. Press **Receive** on the cool, idle, unsealed F6. Only existing, individual
   one-kilogram aluminium scrap pieces qualify. Separate native stacks locally;
   this first route does not split them automatically. Receiving stops at twenty
   pieces. With ContinueFeeding disabled, press Receive for each piece.
4. Seal and run the batch explicitly, equalize when ready, then Release.
   Receiving never issues these process commands. Seal cancels pending transfer
   work. Another charge requires another explicit Receive.
5. Pair the F6 output to an existing hull collector. Select **Released furnace
   products only**, then start that collector. It accepts the 19 kg housing blank
   and 1 kg melt remainder only after the F6 is idle, cool and unlocked.
6. Haul cargo away from the collector. The 2 x 2 blank fills its 2 x 2 grid;
   the other product waits upstream until space exists. Item ordering uses full
   IDs, so the remainder may arrive first. The four-item / 52 kg limits remain.

Manual loading and collection still work. The collector's default **All** filter
continues to mean all supported residue, not all possible cargo. Changing its
filter pauses receiving and leaves all existing cargo intact. No rejects,
historic residue, repaired lots or finished bench housings become furnace feed.

## Connections and placement

| Logical port | Connection |
| --- | --- |
| R4 `PhobosShipbreaker.ResidueOut` | Existing residue route, unchanged |
| R4 `PhobosShipbreaker.MetalsOut` | One F6 input |
| F6 `PhobosFurnace.MaterialIn` | One R4 aluminium source |
| F6 `PhobosFurnace.MaterialOut` | One hull collector |
| F6 `PhobosFurnace.Cooling` | Existing cooling pair, unchanged |

Full native object IDs and reciprocal pair tokens identify each endpoint. There
is no nearest-machine fallback, split, fan-out or automatic replacement binding.
Damage and uninstallation preserve logical addresses for explicit unlinking.

The furnace input is at local **(-2.5, -2.5)** and output at **(+2.5, -2.5)**,
rotated with its heading. The live installation key marks both front corners.
These are structural-floor transport approaches inside the 6 x 6 footprint;
they do not route through painted pipes or electrical conduit. The middle front
operator aisle and cooling connections keep their existing roles.

Routing requires intact structural floor, grid-aligned installed endpoints and
the same ship. Walls, flexible floor, EVA tiles and bare space cannot carry the
route. Search remains bounded to 4,096 visited cells. Moving or rotating an
endpoint, damage or broken floor invalidates the route. The collector retains
its two hull-wall supports and clear exterior mounting pocket.

## Controls and F3

Local panels, C1 and F3 delegate to the same checked service. C1 keeps its
same-authorized-ship boundary; opening physical inventories remains local.

```
phobosroute status
phobosroute link <R4-full-ID> <F6-full-ID>
phobosroute start <F6-full-ID>
phobosroute pause <F6-full-ID>
phobosroute link <F6-full-ID> <collector-full-ID>
phobosroute filter <collector-full-ID> furnace-products
phobosroute start <collector-full-ID>
phobosroute unlink <R4-full-ID> metals
phobosroute unlink <F6-full-ID> receive
phobosroute unlink <F6-full-ID> send
```

`send` on an R4 retains its historic residue meaning. `metals` explicitly selects
its aluminium output for unlinking or `phobosroute controls`. A link to an F6
selects the R4 metals address automatically. F6 controls also expose Receive and
Pause receiving separately from heat enable, and its stop control stops feeding.

## Power, heat and backpressure

The F6 feed uses the existing Routing settings: default **2 seconds / 2 kW** per
piece, with captured per-item duration. Instruments and the optional cooling
pump take priority; the motor receives the remaining measured electricity.
Below-full supply advances only the equivalent paid motor seconds, never more
than elapsed simulation time. All paid motor electricity enters the finite
cooling store, even if a late interlock prevents movement. Feeding an idle
furnace supplies no melting energy and cannot arm heat.

The F6 uses one native energy receipt for its instruments, pump, motor and
process. Duplicate settlement is ignored. A lost cooling connection after
admission retains the receipt as hot-node energy instead of sending heat to a
missing sink. Route, pair, exact physical item, filter and destination admission
are checked again before movement. Replacement cargo cannot inherit paid work.

The collector retains its own configured power and room-heat handling. Its
current default is **5 seconds / 2 kW** per item. The F6 incurs no second charge
for collector-owned movement. Ideal default budgets are 80 kJ for twenty feed
pieces and 20 kJ for two output items, excluding instrumentation, waiting and
native tick overrun. These are authored gameplay budgets, not measured savings.

A full destination retains the original object and its clock at the sender;
there is no virtual inventory, item cloning or silent disposal. New clock
progress is never saved or accumulated while unloaded. Time gaps over the
existing sixty-second bound pause receiving.

## Interruptions and saves

| Event | Result |
| --- | --- |
| Twenty pieces received | Receiving pauses; no automatic Seal or Start |
| Seal, active/hot batch, protected native commit | No material movement from captive or protected contents |
| Full collector | Product stays in the F6 tray until exact native placement fits |
| Power loss, flight, invalid probe or unusable cooling | F6 receiving pauses; cargo and heat remain; explicit Receive required |
| Partial measured power | Only paid motor time advances |
| Floor/endpoint movement, damage, pair or filter change | Affected route pauses; other logical ports retain their connections |
| Reload | Links, filters and physical cargo survive; receiving permission and clock credit do not |
| Unqualified recovered charge | Remains in the charge bin for local handling; output routing does not extract it |

The optional serviced-coolant extension in this same release can also block
feeding when its loop is not ready. Direct and legacy sealed installations
retain their existing cooling contracts. See the [coolant guide](furnace-coolant-conduits.md).

## Verification and remaining owner checks

Offline checks cover port independence, duplicate pairing, save conversion,
retained heat, exact IDs/masses, stack/contents/repair-lot rejection, all rotated
material coordinates, native named points, twenty-piece capacity, finite sink
headroom, zero/partial motor power, captive-state exclusion, and actual product
admission through the native collector filter. Shared transfer rollback and
batch-placement checks remain in use.

Owner checks: run R4 residue collection and F6 feeding together; test native
stack handling, all four placements, broken floor, brownouts, flight interruption,
sealing while receiving is enabled, collector backpressure, save/reload and
local/C1/F3 controls. Artwork and gameplay remain unapproved until evaluated.
The separate [replacement-casting study](furnace-repair-castings.md) remains
future Manufacturing work; this release preserves the original housing recipe.
