# Hull chute and exterior grabber

Current prepared candidate: **Shipbreaker 0.9.0 + Framework 0.9.0**, 24 September
2026. Connected intake was introduced in 0.3.0. Use the
[current player guide](player-guide.md) for the complete operating sequence.
The owner approved these designs and requested the connected implementation.
Mounting definitions, physical transfers, construction and runtime sprites are now
implemented and checked offline. Unity placement, crew access and operation still
need owner testing. Attached-hull cutting remains future work.

## Arrangement and dimensions

The owner requests a wall-mounted **1 x 4 chute** between the existing feeder
and an exterior grabber, with matching widths. Interpret this as **four tiles
along the hull and one tile across it**. The proposed grabber is **four tiles
wide and three tiles deep**, extending into space. This implements the proposed
depth accepted for the first connected test. Rotate the whole arrangement for other hull edges.

```text
                        SPACE
             [ grabber + cutting head ]  4 wide x 3 deep
  HULL ======[ sealed transfer chute  ]====== HULL
                                         4 wide x 1 deep
             [ existing feeder/body  ]  4 x 4 fixture retained
                      SHIP INTERIOR
```

Keep the existing fixture's identity, installed footprint and four-panel feed.
Orient its loading mouth toward the chute; retain usable crew access alongside
the machine. The illustration rotates its existing sprite, not its saved objects.
The chute and grabber add distinct hardware, construction mass and power needs.
Do not reinterpret the fixture's 8 x 8 inventory grid as an eight-tile world object.
The earlier 2 x 2 underfloor parts-port concepts remain separate possible endpoints;
they are not silently enlarged or repurposed into this full-panel hull connection.

This arrangement updates the earlier suggestion to move the whole processor
outside. The exterior assembly acquires and retains material; the existing body
processes it inside. Underfloor routing can extend logistics later, but need not
be implemented merely to illustrate this directly adjoining intake.

## Visual brief

- **Chute:** shallow reinforced collar with mounting shoes at its ends, a broad
  dark transfer throat, closed segmented shutter, visible seal strips and small
  end actuators. A closed shutter should communicate a pressure boundary rather
  than a permanently open hole. Intended world colour texture: **64 x 16** pixels.
- **Grabber:** two opposed clamp arms, one on each side of a clearly open central
  capture bed. Each has one chunky hinge/actuator and an inward-facing gripping
  pad. A compact welder-like **laser-cutting head** sits between them near the
  rear crossmember, aimed into the capture area. Intended world colour texture:
  **64 x 48** pixels. Keep all arm tips inside the proposed footprint; no implied
  unlimited reach, hidden sweep area or giant crane.
- **Supporting detail:** one cream service cover, paired actuator housings and
  a small capped power connection are enough. Muted blue structure, charcoal
  recesses, grey jaws/rails and restrained ochre safety marks match the approved
  fixture. Leave substantial empty/dark space between working components.
- **Static presentation:** a parked, slightly open gripping pose; no animation,
  sparks or continuous beam required. Runtime text can communicate activity.
  This is an art decision, not a verified claim that the engine cannot animate
  furniture. The approved static assembly serves both installed and loose states;
  damaged states use the same art with native damage tint and a damaged name.
  Separate damaged/transport drawings can follow testing. Flat normal maps and
  padded portraits are included; no original-game artwork is packaged.
- **Infrastructure:** native electrical conduit remains separately placed. Do
  not bake connected cabling, a ship wall, floor, atmosphere, visibility wedges
  or lighting effects into these sprites.

## Native mounting evidence and implementation limits

Rechecked the installed 1.0.1.4 native `items/items.json` and
`condowners/condowners.json` on 24 September. `ItmTowingBrace01` uses a shaped
seven-column layout with wall/fixture additions, selected obstruction exclusions
and a `TILDockSys` requirement. Its description specifies Bieler docking airlocks.
It is a precedent for supported exterior geometry, not an arbitrary-wall mounting
API and not a required dependency for our equipment. No native sprite or source
definition is included in the concept assets.

The implementation also follows native wall-mounted pump/sensor patterns:
rectangular socket arrays, padded requirements and distinct exterior/decorative
tile conditions. It does not copy the towing brace's docking-airlock restriction.

**The chute overlays four intact installed walls. Do not remove those walls.**
They remain the game's actual pressure barrier. The sealed solids transfer is an
abstraction across that barrier: no portal opens, no atmosphere is moved, and no
gas-lock chamber/pump cycle is simulated. Removing or damaging a backing wall
stops new transfers; native hull leaks remain native behaviour. Uninstalling the
chute leaves the walls in place. This intentionally avoids a new pressure-hull
implementation for the first connected slice.

The grabber occupies twelve exterior fixture tiles and requires four wall cells
immediately behind its rear edge. The chute occupies those four wall cells as
wall decoration. The processor retains its existing floor-only 4 x 4 placement.
With the grabber arms pointing north, its centre is two tiles north of the chute;
the processor centre is 2.5 tiles south of the chute and rotated 180 degrees from
the grabber. No gaps or lateral offsets. All four cardinal rotations use the same
geometry; the symmetric chute can be reversed. Conduit is separately built; the
grabber's two power contacts reach the outer cells of the supporting wall row.

## Operation and construction

- **Grabber Inventory:** normal 4 x 4 native solid storage accepting cumbersome
  items and smaller solids. Only separate, empty ordinary 24 kg walls are moved.
  Unsupported cargo remains untouched, with a reason in the status panel.
- **Chute:** no user inventory. It is the connection between the two machines.
- **Processor Inventory:** 8 x 8 products tray. Its existing four-panel internal
  feed remains saved and accessible through F9 **Manual feed (fallback)** or
  `phobosshipbreaker feed`; ordinary Inventory no longer opens the second grid.
- **Start / resume pipeline:** validates the layout and arms transfer plus
  processing. An empty feed can wait for the grabber. Start while beside the
  processor; loading the exterior grabber uses the game's ordinary nearby/EVA
  inventory access. There is no remote pickup or crew teleportation.
- One transfer takes **5 powered game seconds at 2 kW**, with **0.05 kW idle**.
  `Intake / TransferSeconds` allows 1–60 seconds after restart. Processing keeps
  its separate default 60 seconds / 30 kW and complete 24 kg material accounting.
- Pause/cancel disarms intake. Reload leaves it paused; pending motion loses only
  its short delay and retains the actual wall in the grabber. Processing progress
  remains on that wall. Full feed waits; missing/damaged/locked connections stop.
  Native transfer faults pause and log the issue; do not blindly retry an ambiguous
  ownership failure. No frame-interleaved or crash-atomic transaction is promised.

| New recipe | Steel | Aluminium | Mechanical parts | Electronic parts | Result | Work |
| --- | ---: | ---: | ---: | ---: | --- | ---: |
| Sealed Hull Chute | 24 | 10 | 10 | 2 | One 40 kg chute | 30 min |
| Exterior Panel Grabber | 50 | 20 | 16 | 4 | One 80 kg grabber | 60 min |

Native steel/aluminium units are 1 kg; these parts are 0.5 kg. Both recipes conserve
mass and fit the construction service's 100-input limit. They use the same native
tables and optional benches as the processor. All three parts have native install,
uninstall, damage and repair definitions. Optional debug spawn IDs:
`PhobosHullChuteLoose`, `PhobosExteriorGrabberLoose`, `PhobosShipbreakerLoose`.

Framework 0.3.0 exposes physical-item transfer separately from recipes and machine
rules. A move reuses the actual object, with its ID and saved conditions. It does
not create output definitions, erase the source object, merge stacks or invent
virtual cargo. Shipbreaker owns layout, timing, eligibility and power.

## First owner test

1. During ordinary play, make a clear four-wall strip, a 4 x 3 empty exterior
   area and a 4 x 4 interior floor area. Install the chute **over the walls**, the
   grabber immediately outside with arms outward, and the processor immediately
   inside with its loading mouth facing the chute. Keep side access for crew.
2. Connect native conduit power to the grabber and processor. F9/status should say
   the intake is connected; wrong placement should produce an explanation.
3. Through the grabber's normal Inventory, load one detached ordinary wall. If
   using deliberate debug grants, `spawn ItmWall1x1Loose` supplies a comparison. Stand
   beside the processor and Start. The wall should move once, then become 11 kg
   useful products plus one 13 kg residue item in the processor's Inventory.
4. During ordinary use, check pause/reload leaves material present and waits for
   Start. Try another hull orientation or disconnect a component only if convenient.
   Any rejection, missing sprite or wrong alignment: send a screenshot plus
   `phobosshipbreaker status`. No separate power-consumption proof is required.

Attached-hull cutting still needs finite reach, target eligibility, relative-motion
limits and ownership of released material. The 25 September
[autonomous reclamation specification](shipbreaker-autopilot-research.md) now
requires Auto Nav in the future design and binds one selected G4 by full native
ID. Current packages retain their existing requirements. Active positioning,
holding/cutting and movement along a short wall section form the proposed first
slice, subject to collision-compatible reach. Docking is optional. This guide does not describe
implemented external cutting. Later delivered R4 recovery and paired routes are
documented in the current player guide; general routing and ore machinery remain
separate work. See the
[residue composition and destination decisions](residue-material-contract.md).

Concept files and exact built-in Imagegen prompts:
[hull-intake artwork](../assets/phobos-hull-intake/README.md).

Related: [mounting history](shipbreaker-hull-mounting.md),
[underfloor transport](underfloor-material-transport.md),
[equipment art study](ship-equipment-art-study.md).
