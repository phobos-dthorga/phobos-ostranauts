# Fennmark artwork handoff: V4, X2, H2 and Manufacturing stock

Prepared and **produced the same day with PixelLab**, 29 September 2026, for
Manufacturing 0.1.0. Owner review of the art in play (scale, lighting, damage
tint) is pending. Requests, seeds, job IDs, costs and reviews are in
[manufacturing-requests.json](../../assets/artwork-completion/manufacturing-requests.json);
masters and hashes in [the manifest](../../assets/artwork-completion/manifest.json).

## Result

| Asset | Native / master | Operation and selected job | Notes |
| --- | --- | --- | --- |
| V4 volatiles refinery (`PhobosVolatilesRefinery`) | 64 / 128 px, full footprint | Pixflux second pass over the first, strength 120, job `1cabd3c2` | Graphite frame, orange clamps, sand plate, hearth lid with glowing port, banded retort, hatch, tray |
| X2 chemical processor (`PhobosChemicalProcessor`) | 32 / 128 px, full footprint | Pixflux second pass, strength 120, job `eead4e3d` | Green sight glass, grey and orange gas domes, control plate |
| H2 hydrogen store (`PhobosHydrogenStore`) | 32 / 128 px, full footprint | Pixflux second pass, strength 120, job `1ede8a48` | Two strapped graphite cylinders, orange vent wheel |
| Nickel-iron ingot (`StockNickelIronIngot`) | 16 / 64 px | Derived: luminance recolour of the aluminium ingot master onto a warm iron ramp | Same drawing as both Rivetline ingots, browner tone; no generation |
| Carbon stock (`StockCarbon`) | 16 / 64 px | Pixflux second pass, strength 70, job `d36d57e3` | Graphite briquette with a lighter flecked face |
| Refinery slag (`StockRefinerySlag`) | 16 / 64 px | Pixflux, strength 60, job `921c4375` | Purple-grey lump with a rust streak |
| Anhydrous residue (`StockAnhydrousResidue`) | 16 / 64 px | Pixflux, strength 60, job `eb34bd07` | Pale tan cracked chunk |

The pass used 10 included generations (allowance 1,875 to 1,865), $0 credit,
no purchases. Only original Phobos drawings and this pass's own first passes
were uploaded. Nothing was archived: every unselected output is a retained
generation input of a selected one (`references/*-first-pass.png`), as the T2
precedent. Clay hydrates keep the game's hydrate art by reference.

## Deviations from the requests, and why

- **Second passes for the machines and the carbon stock.** First passes were
  overhead and on-family but flat (V4 nine colours; the carbon briquette
  unreadable on a dark background). One repaint pass each, over the first
  output, added shading and wear without moving the layout, the T2 method.
- **Connection stubs did not survive.** The copper water outlet and power
  sockets on the V4 and the copper manifold on the H2 came out grey or vanished.
  Connections are separate native objects and live text, so the sprites do not
  carry them; the frame clamps remain the readable fixing points.
- **Fennmark colours.** Graphite frame, burnt-orange clamps and straps, sand
  deck plates and copper bands, chosen to sit beside Rivetline's blue, yellow
  and cream without repeating them. Recorded in [equipment branding](equipment-branding.md).
- **No separate portraits; full-footprint masters.** As the S3/T2: the world
  sprite is the portrait on every form, and the machines are opaque edge to edge.

## Original requests

### Rules that apply

- [Asset generation policy](asset-generation-policy.md): overhead-first prompt
  prefix, `view="high top-down"` and `isometric=false`, one pilot per family
  before expanding, rejected candidates archived.
- [Resolution memorandum](artwork-resolution-policy.md): 2x for the 64 px V4
  (128 px master), 4x for the 32 px X2 and H2 (128 px) and the 16 px stock
  (64 px); nearest-neighbour exports only.
- [Equipment branding](equipment-branding.md): Fennmark V4, X2 and H2; no
  painted text, numbers or logos.

### Prompt prefix (verbatim, first in every request)

ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking
straight down at the object's TOP SURFACE ONLY. Rectangular edges run
horizontally and vertically on the canvas. Show no vertical front or side faces.

### Request 1: V4 volatiles refinery (pilot)

Square opaque canvas, the whole 4 x 4 footprint: a graphite frame with orange
corner clamps around a sand-coloured deck plate; a round hearth lid with a
glowing sight port upper left; a banded horizontal retort drum upper right; a
square charge hatch with an orange handle bar lower left; a recessed grille
tray lower right; a copper water outlet on the right rail and two power sockets
on the left rail. 64 x 64 colour and normal exports.

### Request 2: X2 chemical processor

Same family, 2 x 2: a sand lid with a tall pale green sight glass on the left,
two round gas domes (grey and orange) on the right, a dark control plate along
the bottom, a copper water inlet on the left rail and a power socket on the
bottom rail. 32 x 32 exports.

### Request 3: H2 hydrogen store

Same family, 2 x 2: a dark cradle holding two horizontal graphite cylinders,
each with two orange straps; an orange vent valve wheel on the right rail and a
copper manifold stub on the left. 32 x 32 exports.

### Request 4: stock sprites

Nickel-iron ingot (warm iron-grey bar, same layout as the Rivetline ingots),
carbon stock (dark graphite briquette), refinery slag (dark glassy lump with a
rust streak, distinct from both melt remainders) and anhydrous residue (pale
baked clay chunk), 16 x 16 native from 64 x 64 masters, transparent with
8-pixel margins.

### Review checklist (completed 29 September 2026)

- Overhead only, no side faces, no diamond: checked on every selected master
  and at native size.
- Native-size inspection (64, 32, 16 px) against the approved Rivetline
  equipment and stock sprites. Done.
- Masters retained, hashes and prompts recorded, generation inputs kept. Done.

## Manufacturing 0.2.0 pass (29 September 2026)

| Asset | Native / master | Operation and selected job | Notes |
| --- | --- | --- | --- |
| K2 Sabatier reactor (`PhobosSabatierReactor`) | 32 / 128 px, full footprint | Pixflux second pass over the first, strength 120, job `a9be2935` | Steel catalyst dome with an orange heater ring, finned condenser, teal water trap, control plate |
| M2 methane store (`PhobosMethaneStore`) | 32 / 128 px, full footprint | Pixflux second pass, strength 120, job `221e8728` | One olive dome with an orange band and a steel valve cap, distinct from the H2 store's twin cylinders |

Same method as the 0.1.0 machines: an original Fennmark start drawing, a Pixflux
first pass, one repaint pass kept on its layout. Four included generations
(allowance 1,865 to 1,861), $0 credit, no purchases; both first passes are retained
as the inputs of the selected passes, so nothing was archived. The copper pipe on
the reactor did not survive; connections are separate objects and live text.
Requests, seeds and reviews are appended to
[manufacturing-requests.json](../../assets/artwork-completion/manufacturing-requests.json).

## Manufacturing 0.3.0 pass (29 September 2026)

| Asset | Native / master | Operation and selected job | Notes |
| --- | --- | --- | --- |
| P1 RCS propellant manifold (`PhobosPropellantManifold`) | 16 / 64 px, full footprint | Pixflux second pass over the first, job `e0dcf9fb` | Steel valve block with a header pipe, three orange handwheels and an amber line stub at the bottom edge |
| Propellant line (`PropellantPipe`, `PropellantPipeSheet`) | 16 px tile, 64 px sheet | No generation: deterministic amber recolour of Agriculture's water line | Same joint layout as the other conduit families; normal maps copied unchanged |

Two included generations (allowance 1,861 to 1,859), $0 credit, no purchases. The
first pass is retained as the input of the selected pass. The line recolour is
reproduced and checked by `scripts/export-propellant-line-art.py --check`, which
the Manufacturing build ran. Superseded by Framework 0.56.0's lane art: the gas line
is now drawn in its own lane by `scripts/export-line-art.py` (same amber ramp); see
[the line art record](../../assets/line-art/README.md).

## Manufacturing 0.4.0 pass (29 September 2026)

Fourteen sprites, one Pixflux pass each over an original procedural start drawing
(strength 130), all selected: the H3/H4 hydrogen stores (three and four strapped
cylinders), the M3/M4 methane stores and the O, N and C stores in three sizes (one,
four or nine domes), and the L2 filling station (bottle rack, compressor grille,
control plate). Masters are four times native: 128 px for 2 x 2, 192 px for 3 x 3
and 256 px for 4 x 4. Gas colours echo the game's canister convention by name only
(green oxygen, blue nitrogen, pale grey carbon dioxide); no game imagery was
uploaded. Fourteen included generations (allowance 1,794 to 1,780), $0 credit, no
purchases; one M4 upload was refused as truncated before generation and not
charged. Requests, seeds and start-drawing hashes are in
[manufacturing-requests.json](../../assets/artwork-completion/manufacturing-requests.json).

## 0.5.0: the A2 cabin air regulator (29 September 2026)

One Pixflux pass (strength 130, seed 9290541) over an original procedural start
drawing in the Fennmark family: graphite frame and burnt-orange clamps, a steel
manifold bar with a green (oxygen) and a blue (nitrogen) valve wheel, a slotted
sensor head and a diffuser grille, and the amber gas-line stub. Selected; 128 px
master, 32 px native. One included generation (allowance 1,776 to 1,775), $0
credit. No painted gauges, per the owner's ruling; readings stay live on the panel.
