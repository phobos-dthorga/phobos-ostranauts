# Hull chute and exterior grabber

24 September 2026. Owner-requested equipment design and original concept art.
These are proposed additional components, not installed machinery or working
transport/cutting features. Shipbreaker's pending 0.2.1 feed fix is independent.
The owner approved the generated chute/grabber visual direction on 24 September.

## Arrangement and dimensions

The owner requests a wall-mounted **1 x 4 chute** between the existing feeder
and an exterior grabber, with matching widths. Interpret this as **four tiles
along the hull and one tile across it**. The proposed grabber is **four tiles
wide and three tiles deep**, extending into space. Depth is a recommendation,
not a confirmed owner selection. Rotate the whole arrangement for other hull edges.

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
  furniture. Damaged/loose forms, normal maps and final texture exports follow
  after the intact design is selected.
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

Before implementing the new hull assembly:

1. Define its structural mounting cells and service side from native placement
   patterns. Existing floor-only fixtures must retain their old placement rules.
2. Establish a real pressure boundary using suitable native wall/hatch behaviour.
   A one-tile-deep graphic does not itself prove a usable transfer lock. Define
   aperture, allowable panel size, seal states, and damage/uninstall consequences.
   If two sequential closures cannot fit or be represented reliably, change the
   transfer arrangement rather than imply an airtight open hole.
3. Start with physical, detached eligible panels retained by the grabber, then
   moved through a valid chute into the existing finite feed. Keep item identity,
   contents and mass; a disconnected/full/unpowered destination retains cargo at
   its source. Avoid new virtual matter stores or duplicated processing yields.
4. Attached-hull cutting is a later acquisition step. It needs explicit target
   eligibility, finite reach, relative-motion limits and ownership of the released
   piece. The static cutting-head drawing does not promise that functionality.
   AutoNav/positioning becomes relevant here, without making the indoor processor
   depend on an autopilot merely to run a batch.

Reusable physical-item transfer logic belongs in Phobos Framework when implemented;
mounting, grabber rules, artwork and machine balance remain content-specific.
Focus future checks on sealing, rotated installation, transfer interruption and
item preservation rather than re-proving established power/container patterns.

Concept files and exact built-in Imagegen prompts:
[hull-intake artwork](../assets/phobos-hull-intake/README.md).

Related: [mounting history](shipbreaker-hull-mounting.md),
[underfloor transport](underfloor-material-transport.md),
[equipment art study](ship-equipment-art-study.md).
