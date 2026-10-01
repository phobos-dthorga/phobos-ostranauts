# Mining laser design record

Shipbreaker 0.59.0 and Framework 0.66.0, prepared 1 October 2026. Player
instructions are in the [mining laser guide](../shipbreaker-mining-laser.md).
Offline checks only; nothing here is gameplay validation.

## Owner request and decisions (1 October 2026)

The owner asked for a mining laser for Shipbreaker: an installed sprite that is
animated to some degree, as the game's environmental heater is, with a laser
effect drawn from the sprite that travels a 60 degree arc; used to mine
asteroids and perhaps to take derelicts apart, possibly as a better replacement
for the G4's cutting; a heavy electrical consumer; with a chemical laser as a
later alternative that ties into the chemical systems.

Decisions the owner made on the plan:

1. **Reach: attached targets only.** A tethered asteroid or a hull the G4 has
   moored. No free-flight range model.
2. **Brand: Ablatine ML-2.** A new directed-energy brand (see
   [equipment branding](equipment-branding.md)).
3. **Power: 24 kW now, with a radiator link planned.** The second phase is
   recorded below.
4. **Hull panels: freed panels drop where they are, and the crew pick them up
   and haul them.** No G4 collection step.

This supersedes, for this machine, the earlier note that no second mining
system should be added. The design keeps the game's own loot tables as the only
source of ore, as that note intended.

## What the game does (inspected Ostranauts 1.0.1.5)

These are observations of Blue Bottle Games' code and data, not our design.

- **Sprite animation.** An item definition or a cosmetic overlay may carry
  `objAnimation` (frame count, frame rate, loop, sheet columns and rows). The
  heater's on state (`ItmHeater02`, a cosmetic overlay of `ItmHeater01`) and the
  two firing point-defence guns are the only uses in the shipped data.
  `Item.SetAlt` takes an image, its normal map, a damage image, a tint and an
  animation; `Item.SetupAnimation` sizes the item from one sheet cell and adds
  the item's frame step to a static per-frame event that the game raises only
  while unpaused. Each cell must therefore be the item's footprint at sixteen
  pixels a tile. `SetAlt` without an animation does not remove that step, and
  every `SetupAnimation` adds another.
- **Why not the overlay route.** Applying a cosmetic overlay replaces the
  object's definition name with the overlay's name and happens through the
  game's mode switch, which replaces the object. Our machines never swap
  definition for power state, and their saved records, power routing and family
  tests are keyed to the definition.
- **Mining.** There is no ship-mounted miner. Crew use a drill or the handheld
  Halvorson mining laser through the `ACTMine` action, whose target test is
  `TIsMineableDestructableNotDeposit`. Rock walls take 15 damage points and
  become a damaged form that takes 29 or 30 more; destroying that form switches
  it into `ItmRock01Salvage`, a table that rolls ore or 3 kg of gangue. Ice
  walls take 10 and 30. Cores (2 x 2 floor) take 29, 58 or 115 and become
  either an ore deposit or rubble. Ore deposits are extracted by a separate
  crew action that gives the ore to the crew member. Bare rock floor, ice floor
  and rubble destroy straight into gangue.
- **Damage.** `DamageSystem.DamageRay` is what tools, weapons and mining
  charges go through. For each destructible object hit it adds no more than the
  object has left to `StatDamage`, runs the object's `Destructable` check and
  ends its turn. The check queues the object's own damage action, which is the
  mode switch above.
- **Mooring.** The nav station's mooring clamp turns a nearby asteroid into a
  ship and moors it; a G4 capture moors a hull the same way. Moored ships share
  one deck space: `Ship.GetTileAtWorldCoords1` resolves a world position on
  either. Deck tiles are 0.32 m. Free-flying ships are kept apart by collision
  circles hundreds of metres across
  ([close-work geometry](shipbreaker-close-work-geometry.md)), which is why a
  beam can only meet deck tiles under attachment.
- **Exterior fixtures** mark their tiles as obstructions (`TILExtFixtureAdds`).

## Design

**The head.** 2 x 2 exterior fixture, two hull walls required behind it (the
G4's socket pattern), 120 kg, purchase-only, Friendly at the faction kiosks
because it cuts other hulls. It holds nothing. The crew point and the heat go
to the tile behind the left backing wall. A square footprint keeps the item's
transform scale uniform, which a rotated child (the beam) needs.

**Reach.** A 60 degree sector, 24 tiles, from the emitter on the head's outer
edge. The facing is read from where the emitter actually sits, not from an
angle convention. Line of sight is a sampled walk every half tile: our own hull
or fixtures block, the other ship's walls and obstructions block, and a
candidate is admitted only when the first solid thing on its line is itself.
Candidates are taken in one-degree steps across the arc from the last finished
bearing, nearest first.

**Rock.** The laser asks the game's own Mine rule, then refuses anything whose
breaking can only leave gangue (followed through the game's tables at run time:
damage loot, the action it queues, what that action turns the object into). One
job is one native damage stage: the laser is paid `points left / points per
second` seconds of work at its working draw, writes its pending phase, and then
applies exactly the damage left through Framework's `NativeDamage.Apply`, which
mirrors the per-hit chain of `DamageRay`. The game's own action and loot roll
follow. Nothing is spawned by us, so the mass and the ore are the game's. This
is the native route, not a Phobos recipe; our recipes still conserve mass.

**Hull panels.** The same forced native uninstall the G4 uses, on the same
ordinary wall identity and mass range, with line of sight replacing the G4's
jaw reach, exposure and floor requirements. The panel stays on the target deck.

**Protected.** The wall, floor and port a G4 on our ship holds the target by;
anything within a tile and a half of a mooring port on either ship.

**Power and heat.** `NativeEnergyReceipts` around the game's own power step;
work is credited from the electricity that actually arrived. Sixty percent of
it is paid into the room behind the mount through `RoomHeat` under the shared
10 kPa and 40 C rule; a room that cannot take the next step's heat is a wait,
not a stop. The interior room standing in for the head's cooling is an authored
abstraction, like the F6-P's underside assembly.

**People.** A person within a tile of the segment from emitter to target holds
power and posts a caution. The laser never calls the game's damage ray, so it
cannot wound anyone; the hold is a rule the player can see.

**Saved state.** `Shipbreaker.Laser` on the head: ship, mount, moored ship and
port, sweep cursor, counts, and the job in hand (object, kind, the definition
it had when the job began, work paid). The pending phase is written before the
one native change. After a load nothing fires until Start; a pending change is
settled by looking at the object, and a change that did not take is paid for
again. `Shipbreaker.LaserFilter` holds the player's choice separately so the
panel's stale-configuration stamp does not change with every powered second.
Both records are new; no existing record changes.

**Presentation.** Framework `SpriteAnimation` starts and stops the firing sheet
through `Item.SetAlt` and detaches the game's frame step before each change.
Framework `WorldBeam` is a light-sprite quad (the game's own
`prefabQuadLightSprite`) parented to the head and stretched from the emitter to
the target every frame. Nothing about either is saved or read by the service,
and any art or rendering fault switches the display off and leaves the work
alone. One native tracer streak is drawn at the moment a cut is made.

## Authored figures

| Figure | Value | Why |
| --- | --- | --- |
| Working draw | 24 kW | Twice the G4's cutter, and the most the air-cooled rule lets run for more than a few seconds: 14.4 kW into about 800 mol of room air is about 0.87 K a second |
| Idle draw | 0.1 kW | The T2's |
| Heat share | 60 % | The rest is taken to leave with the vapour and debris |
| Damage rate | 1 point a second | A rock wall (45 points) in 45 s and 0.3 kWh; a crew member with the handheld laser manages roughly 0.15 a second in the game's data |
| Panel time | 60 s | Half the G4's time at twice its power: 0.4 kWh |
| Largest stage | 150 points | Covers every wall stage and intact core |
| Arc and range | 60 degrees, 24 tiles | The owner's arc; 7.7 m of reach |
| Price | 9,600 cr | Between the G4 (6,400) and the D4 (12,000) |

None of these is a measurement. The game's damage points are not a unit of
energy.

## Artwork

One PixelLab generation (`create_image_pixflux`, 128 x 128, overhead settings,
from an original procedural start drawing) gave the selected head. The eight
firing frames and the beam texture are derived mechanically by
`scripts/derive-laser-art.py`: the lens port pulses, the collar behind it warms
at the peak and two pixels on each fin alternate; the exporter verifies that no
other pixel differs from the still image. Requests, seeds, hashes and the
derivation are in `assets/artwork-completion/mining-laser-requests.json`. No
level gauge or other reading is painted; the pulse is the head's physical
state.

Not yet seen in the game: whether the light-sprite pass draws the beam as
bright as intended, the quad's pivot, how the beam looks over tiles hidden from
view, and whether starting and stopping the sheet repeatedly stays smooth.

## Second phase: radiator link (Shipbreaker 0.61.0)

Built on Shipbreaker 0.60.0, prepared 1 October 2026. Framework is unchanged.

**What it does.** A head may be paired with one of the F6's cooling assemblies
(the F6-R radiator or the F6-P port) that touches it. The pairing is the shared
one-to-one port link on the assembly's own single cooling port, so an assembly
serves one furnace or one laser and the furnace's pairing code needs no change.
While the paired assembly is installed, undamaged, properly mounted, readable
and has room for a whole power step's heat, that heat is paid into its saved
store; otherwise the step falls back to the room rule. The assembly radiates as
it always did, every quarter second whether paired or not, with its own limit.

**The high setting.** A job is a fixed amount of energy (a rock wall 0.3 kWh, a
panel 0.4 kWh). A job started while the high setting is chosen and a paired
assembly is ready captures 48 kW, so it takes half the time. The power info
still asks for the standard draw and the power step scales the request, as the
G4 scales its cutter. One assembly at its 250 C limit sheds about 42 kW, more
than the high setting's 28.8 kW of heat, and settles near 203 C; at the
standard setting it settles near 130 C. These follow from the furnace's
existing authored radiator figures (12 square metres, emissivity 0.85, a 200 K
background), not from any new measurement.

**Rules kept.** Vacuum is still not cooling: only a real cooling assembly, with
its finite store, takes heat. Pairing and unpairing need a paused laser and an
assembly at 50 C or below, the furnace's own rule. A cut already started keeps
the draw it captured.

**Where this differs from the plan the owner approved.**

- *The radiator node did not move to Framework.* Both users of the assembly are
  Shipbreaker machines, and the assembly itself is Shipbreaker equipment. A
  second mod could only use it by depending on Shipbreaker, or after the F6-R
  itself moves to Framework as the water silos did, which is a save-touching
  migration that deserves its own change. So this release adds a narrow
  Shipbreaker surface (`RadiatorSink.cs`: problem, temperature, room left,
  deposit) and leaves the furnace's thermal code and records exactly as they
  were. The Framework lift stays open for the owner to call.
- *No conduit link.* The plan allowed a link through F6-C conduit. The
  conduit's coolant is kept in the furnace as its reservoir, the route checks
  admit one furnace and one radiator, and the owner's rule is one circuit, one
  furnace, one radiator. A circuit for a laser needs decisions about where its
  coolant lives and what pumps it, so it is left for the owner. Touching
  satisfies the rule that links run through touching equipment or a line.

## Later ideas, not planned

- The G4 reclamation mission using the laser to free panels before collecting.
- A chemical laser that draws on stored chemicals in place of electricity. The
  real chemical lasers the agent recalls (hydrogen fluoride or deuterium
  fluoride, and chemical oxygen iodine) need fluorine, or chlorine with
  hydrogen peroxide and iodine, none of which the mods store. This recollection
  is **unverified** and has no source attached; a research round with primary
  sources comes before any design.
- A machine-side route for ore deposits, which the game reserves for a crew
  member.
