# G4 close-work geometry: native evidence and selected arrangement

26 September 2026. The owner authorized implementation of physical reach,
selected-G4 positioning, the ordinary-wall loop and concurrent processing.
The first native geometry check found a material design decision. The owner
subsequently selected temporary capture/repositioning, retaining a deployed head
as a possible later enhancement. See the [capture implementation](shipbreaker-capture.md)
for current runtime scope; this report records the underlying evidence.

## Native evidence

These findings concern **Blue Bottle Games' Ostranauts 1.0.1.5**, inspected locally
against `Assembly-CSharp.dll` SHA-256
`91B50F45CACD64DE39B9BCC30EC7B4542F3E3976AC3BC5589B346976A262425E`.
The method identifiers below are evidence references, not copied implementations.
Decompiled material remains in ignored local research directories.

| Native boundary | Finding | Consequence |
| --- | --- | --- |
| `CrewSim.M_PER_TILE`; `Ship.GetJSON`, `ParseColsRows` | Deck dimensions use 0.32 metres per tile. | One local tile must not be treated as one navigation metre. |
| `SilhouetteUtility.GetSilhouetteLength`; `ShipSitu.SetSize`, `GetRadiusAU` | Ordinary radius uses floor-plan X span multiplied by 20 metres. Span at most one enters the 1,500-metre minimum branch. | Collision radius is not the physical deck half-width and need not shrink monotonically at the final remnant. |
| `CollisionManager.GetCollisionDistanceAU`, `CheckCollisions`, `ProcessCollision` | Free ships collide within the sum of those radii. Overlap correction moves ships apart even when relative speed is small; damage has separate speed rules. | Slow approach or matching velocity does not authorize entering the circles. |
| `GUIOrbitDraw`; `NavIcon.GetSilhouette`, `ConstrainRadius` | The navigation silhouette is scaled so its farthest vertex reaches the desired navigation radius. | The map silhouette is a display mapping, not independent evidence of physical cutter reach. |
| `Ship.PlaceAtAirlocks` | Native attached navigation centres are separated by the radius sum plus ten metres. | Even attached deck contact is not represented by physical deck separation in navigation coordinates. |
| `CrewSim.PositionShipsAtAirlock`; `Ship.MoveShip` | Deck placement separately rotates the incoming grid and translates it using exact port anchors. | Attached deck positioning is a separate operation from navigation movement. |
| `DamageSystem.FindIntersect`; `Ship.CreateMooringPorts` | Incoming direction is projected onto a grid boundary to choose temporary mooring endpoints. | This identifies a candidate side; it is not a metric tool-to-surface transform or arbitrary G4 anchor. |

The [native boundary checks](../tests/PhobosNative.Tests/ShipbreakerGeometryChecks.cs)
read the deck constant from metadata and execute native size/radius calculations
without loading a scene. They verify the mismatch and the small-remnant branch;
they do not establish a Unity transfer or a working capture arrangement.
The native harness passed 9,110 checks on this inspection, including six new
geometry assertions. With the prerequisites in [the build guide](building.md),
run from the repository root using the actual local installation path:

```powershell
$gamePath = '<local Ostranauts installation>'
dotnet run --project tests/PhobosNative.Tests -c Release "-p:OstranautsPath=$gamePath" -- $gamePath .
```

## Bounded numerical example

Consider two **synthetic** square hulls with 21 tile centres per axis, hence a
floor-centre X span of 20. Native size gives each a 400-metre navigation radius:
free-flight centres must remain at least 800 metres apart before any extra margin.
At the physical deck scale each square is 6.72 metres wide.

Under a centred physical-deck interpretation, the two half-diagonals plus a
generous full three-tile G4 extension total only about **10.46 metres**. That
extension is an upper-bound illustration, not a new cutter specification. The
more than 789-metre difference cannot be assigned to invisible grabber reach.
Rotation cannot overcome this radial bound. This example is not an owner-save
measurement or a proof about every possible hull shape.

The native map scaling could be used to author a different close-work model, but
it would need explicit semantics for finite range, physical acquisition, visibility,
obstructions and changing shape. It must not be presented as a discovered physical
conversion. Likewise, changing collision radii or borrowing docking immunity
would change the operating contract and is not implemented here.

## Selected arrangement and deferred alternatives

1. **Temporary capture, then release and reposition:** use native attached deck
   geometry where it genuinely places the selected G4 within reach. Do not turn
   the G4 into a docking port. Verify generated anchors, intact supports, load/
   unload, release and actual jaw clearance before treating capture as workable.
   The ship moves between work windows; a fixed attachment is insufficient for
   the whole wreck. This is the smallest candidate worth evaluating next.
2. **Finite deployed cutting head:** keep the carrier in free flight and design
   a physical remote head and retrieval mechanism with finite deployment distance,
   travel time, power, obstruction, retained payload and recovery after interruption.
   The existing G4 artwork, footprint and transfer motor do not supply this feature.
3. **Explicitly simplified close-work model:** agree a bounded gameplay mapping
   between nav proximity and deck work, including visible explanation of how
   material crosses the gap. This relaxes the original physical-reach requirement;
   it must not be silently introduced as native behaviour.

The owner selected **option 1** on 26 September. Shipbreaker 0.22.0 / Auto Nav
0.16.0 implement the bounded capture/release stage described in the capture guide.
Option 2 may later extend its reach; option 3 was not selected. The full mission
sequence remains unfinished, and no free-flight physical transform is assumed.

## Processing work that survives the choice

The [F6 checked operations](../src/PhobosShipbreaker/FurnaceOperations.cs) already
own Seal, Resume, Equalize and Release; repeated-batch coordination should call
those checked operations through an explicit machine authorization. Current
`ConsoleBinding` requires a nearby operator, so it is not an unattended context.
Passing null would select local access and the local equalization exception;
neither is an acceptable substitute. The exact furnace, input/output participants,
cooling endpoint, ship, owner and room must remain bound and revalidated.

The [current collector service](../src/PhobosShipbreaker/CollectorService.cs)
revalidates physical routes, finite capacity and exact items, but its Start path
also expects local/C1 access. Separate authorized service entry points are needed
for repeat receiving, without changing manual command checks. The mission must
distinguish a capacity wait from manual pause, lost power, changed routes or an
uncertain commit before it can resume automatically.

The [furnace motion guard](../src/PhobosShipbreaker/FurnaceService.cs) is Phobos
policy. Its replacement remains dependent on the selected working model and
checked process limits. Preserve the guard, native receipt accounting and hot
jobs until that replacement is tested. No repeated cycles or changed motion
policy are claimed by this inspection.

Continue from the [implementation handover](shipbreaker-autopilot-handover.md)
for the remaining acquisition and processing work. Owner gameplay evaluation remains
separate from offline native evidence.
