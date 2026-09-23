# Underfloor material transport — discussion proposal

24 September 2026. Owner idea following the exterior Shipbreaker mounting review:
show conveyors emerging from beneath the floor and returning beneath it, implying
material logistics in a lower service space. The owner compares this abstraction
to the game's RCS gas supply. This note proposes a design; no transport code or
new dependencies have been implemented or approved for release. A later owner
request authorised [port concept artwork](../assets/phobos-material-transport/README.md)
while testing continues; those provisional 2 x 2 designs do not settle the final
hardware or transport behaviour.

## Recommended representation

Use visible loading/receiving ports and short exposed belt or roller sections.
The route beneath the floor is abstract service infrastructure, not a claim that
the game has a second traversable deck. Captive trays, guide rails or enclosed
carriers explain operation in microgravity. Preserve the existing Shipbreaker
body and palette; create only the additional port/belt art once its dimensions
and placement are settled.

The exterior fixture retains a structural mount. A sealed material connection at
the hull can replace the need for crew to reach its output tray directly, provided
the new transfer access is actually implemented. Keep native floors and pressure
boundaries deliberate; decorative openings must not imply an open atmospheric
path that the implementation silently ignores. Sealed transport is an explicit
machine abstraction, not simulated perfect air recovery.

## Small first implementation to consider

One powered Shipbreaker output sends eligible recovered parts, scrap and retained
residue to one explicitly selected receiving terminal on the same ship. The
receiver exposes an ordinary finite physical inventory. Start with an explicit
pair and simple filters; expand to multiple destinations when a real use needs it.

- Move the existing items with their mass and state; do not destroy and recreate
  their definition or pool them into an unlimited virtual resource balance.
- Give ports explicit item-size/capacity limits and transfers a finite duration.
  Full panels need an appropriately sized industrial loading port; the 160 kg
  fixture itself is not an automatic candidate for a small parts conveyor.
- Keep cargo physically in the sender while a transfer is pending. Complete the
  move only when the receiver can accept it; power loss, obstruction or invalid
  links retain cargo. Final engine transfer/rollback behaviour must be inspected
  before claiming this implementation safe.
- Display sending, waiting for power, destination full and disconnected states.
  Short belt movement can reflect actual work once animation support is chosen.
- No automatic routing between docked ships in the first slice. A same-ship check
  alone may not establish structural continuity after hull separation; that is a
  concrete topology question for implementation, not a solved native conveyor API.
- Expose useful enable/pause, destination and filter controls, plus console status
  and commands through the same service. Keep transfer limits/mass semantics clear.

This leaves the existing processing recipe and 4 x 4 machine body intact.
Receiver/access hardware adds its own footprint and construction materials;
an implied lower deck is not free unlimited volume or massless infrastructure.
Decide those small hardware requirements before producing final port artwork.

## Reuse and scope

The owner subsequently selected [Phobos Framework](phobos-framework.md) as the
shared home for material-transport behaviour (24 September 2026). Reusable endpoint
and transfer services belong there; physical ports, artwork and balance belong
in a content mod. The initial framework release has inventory/registration
helpers only. It does not implement conveyors or settle the physical design below.

The existing Shipbreaker feed and output containers provide concrete endpoints.
Its material accounting and processing service stay separate from transport.
Normal crew hauling remains useful at endpoints and for oversized cargo.

[Common Sense Salvage and Storage](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481217)
already supplies crew pickup/haul/sort, optional AUTO containers and category
filters. Its author's description retains physical crew routes; it does not
establish an underfloor conveyor capability or a public reusable routing API.
Treat it as an optional companion and check endpoint recognition when needed.
Do not duplicate its general crew-job system or require it merely for the new
machine transport feature. No suitable Ostranauts conveyor implementation was
verified in this bounded review; broad Workshop search also returned other games.

The next implementation choice is the size and access model of the first port
pair; provisional artwork now explores that at 2 x 2 tiles per parts port. Testing
should target our transfers, item preservation, blocking, save/load and competing
crew access, using established container/power patterns without re-proving them.

Related: [exterior mounting proposal](shipbreaker-hull-mounting.md).
