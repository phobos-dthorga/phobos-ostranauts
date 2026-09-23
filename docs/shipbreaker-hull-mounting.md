# Shipbreaker exterior mounting: owner feedback and design proposal

**Latest owner direction:** a separate 1 x 4 hull chute and matching-width exterior
grabber, retaining the existing feeder/body inside. See the
[hull-intake design](shipbreaker-hull-intake.md). The recommendation below to move
the whole processing body outside is historical and superseded by that arrangement.

24 September 2026. Installed build: Phobos Shipbreaker 0.1.4, game 1.0.1.4.
This records findings and a proposed direction; no mounting or gameplay changes
have been made for this review.

Subsequent owner proposal: [underfloor material transport](underfloor-material-transport.md)
could connect an exterior processor to internal receiving ports, keeping most
logistics beneath an implied service deck. It is a discussion proposal and does
not yet resolve mounting, pressure boundaries or physical item transfer.

## Evidence from the owner's first session

- The owner supplied `phobosshipbreaker dependencies` output confirming the
  0.8.71 Framework plugin/data and Workshop data, successful template precheck,
  and both registered construction recipes. Registration is now owner-observed
  in-game. Processing, persistence and interruption behaviour remain unverified.
- The owner said the fixture looks like part of the game. This supports the
  initial in-game appearance, not every rotated/damaged state or lighting case.
- The installation screenshot shows a preview crossing the ship's outer wall
  and extending outside the floor area. The owner wants exterior attachment,
  possibly with a door/chute behind it, and suggested the native towing brace.

## Current placement rules explain the mismatch

`src/PhobosShipbreaker/Content.cs` assigns installed forms a 4 x 4 rectangle of
`TILFixtureAdds`, with `TILFloor` required beneath all sixteen cells and
`TILObstruction` forbidden over the same cells. This is the indoor floor fixture
previously implemented, not an exterior hull attachment. Rotating it does not
remove those requirements. The screenshot's attempted placement conflicts with
that model; it is not evidence of a failed dependency or missing artwork.

For a temporary processing test, an unobstructed 4 x 4 floor area, suitable power
connection and crew access remain the intended installation. Do not suggest
cutting away the existing pressure hull as a workaround for this build.

## Towing-brace precedent

The installed game's `data/items/items.json`, `ItmTowingBrace01`, uses a shaped
seven-column placement layout, including a `TILDockSys` requirement and selected
wall/fixture additions and obstruction exclusions. It does not use our full
floor rectangle. Its native object description ties it to Bieler airlock docking
systems; it is not a universal attachment to any plain wall. The installation
recipe combines two loose halves, which is specific to the brace, not a recipe
we should copy into Shipbreaker.

The owner's [T7N reference](https://ostranauts.wiki.gg/wiki/Ryokka_%22T7N%22_Towing_Brace)
likewise describes the airlock association and installation clearance. Direct
page fetch returned 403, but indexed page text was available; local game
definitions are the primary technical evidence. No game definitions or pixels
are copied into this document or distribution.

## Recommended direction, pending implementation design

- Keep the processing body 4 x 4, outside the pressure hull, with a deliberate
  attachment edge and service point facing the crew compartment. Define the
  complete collar/hatch/access envelope as well as the machine body.
- Give it a dedicated structural mounting collar, drawing on the brace's shaped
  attachment pattern without requiring a towing brace or occupying the main dock.
- Accept loose panels at an outward-facing loading mouth. Provide recovered
  material access through an inward-facing transfer hatch if practical.
- Treat a through-hull chute as a real pressure boundary. Prefer existing native
  airtight wall/door/airlock behaviour; a small transfer chamber with interlocked
  closures is the intended design, not automatic gas deletion or an unsealed hole.
  An existing cargo airlock plus exterior loading is the lower-complexity fallback
  if a dedicated transfer hatch needs disproportionate custom simulation.
- Conduit is still separate infrastructure. Exterior mounting does not remove
  the need for power, support, accessible controls or physical material handling.
- Retain the processing service and its material accounting. An exterior loose-
  material processor does not itself cut another ship or require AutoNav.

Before changing the implementation, establish native placement/attachment rules,
pressure sealing, damage/uninstallation consequences, and crew/hauling access at
the new service point. The current controls check crew distance and ship identity;
that alone does not establish a valid transfer route through a hull. Reuse native
patterns and focus checks on these changed integration points. Do not silently
change existing saved fixture placement or supply extra mount/hatch mass for free;
decide the construction bill and saved-fixture transition explicitly.
