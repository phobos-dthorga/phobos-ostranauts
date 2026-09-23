# Shipbreaker generation prompts

## Pixel-art revision v2 — 23 September 2026

The owner liked the first design and requested much more pixelation to match
Ostranauts. Edited with ChatGPT's built-in Imagegen tool, preserving v1 separately.
Image 1 was `source/PhobosShipbreakerInstalled-concept-v1.png`, the edit target.
Image 2 was the owner's fusion-core close-up, a style reference only, retained
under ignored `.local/research/equipment-art/user-screenshots/2026-09-23/06-fusion-core-closeup.jpg`.
The unchanged output is `source/PhobosShipbreakerInstalled-concept-v2.png`.
No screenshot pixels were manually extracted or composited into the asset.

### Exact edit prompt

```text
Use case: style-transfer.
Image 1 is the EDIT TARGET: our existing intact Phobos Shipbreaker concept.
Image 2 is a STYLE REFERENCE ONLY: the user's Ostranauts machinery screenshot. Do not reproduce its scene, reactor, floor, UI, conduit or source sprites.

The user likes Image 1's design and requests that it be MUCH MORE PIXELATED to match the actual game's own art. Change the rendering style only. Redraw it as deliberate chunky low-resolution pixel art, preserving the same machine layout, silhouette, proportions, orientation and palette.

CRITICAL PIXEL SCALE: design on a coarse 64 x 64 logical pixel grid and present that sprite enlarged using hard nearest-neighbour blocks. Every edge should follow large clearly visible square pixel steps on one consistent grid. Individual bolts, vents and contacts should simplify into one- or two-pixel marks. Use solid opaque colour clusters, approximately 2–3 flat shades for each material. Make the coarse pixels immediately obvious at full viewing size. Remove smooth gradients, soft antialiasing, high-resolution microtexture, airbrushed bevels and glossy 3D shading. Do not add a drawn grid. Do not merely add tiny pixel noise to the existing detailed painting. Large calm areas of flat colour and a few strong contrasting edge pixels should carry the forms, like the blue housing and simple component shapes in Image 2.

PRESERVE: the strict directly-overhead orthographic view; cream service enclosure at upper left; slate-blue upper and outer housings; small dark collection recess at upper right; two unconnected rear/top sockets; the transverse gantry and its single central separation head; four yellow clamps beside the dark empty rectangular bed; two straight side rails; the bottom/front access notch. Keep the intact idle state, existing component positions and full silhouette inside the square four-by-four-tile footprint.

Background: genuinely transparent. Opaque machine surfaces with crisp transparent cutout boundaries. The whole isolated machine centred with a narrow transparent margin. No floor, scene, people, captions, letters, logos, watermark, active cutting effects or cast shadows. No extra devices or added decorations. Electrical conduit is separately built ship infrastructure: do not add a connected cable, perimeter conduit loop or junction boxes.

The result should be visibly coarser and flatter than Image 1 while remaining recognisably that same machine.
```

## Intact concept v1 — 23 September 2026

Generated with ChatGPT's built-in Imagegen tool. This is a new concept, not an
edit of a game sprite. No image-path inputs were supplied to this call; the
written brief below incorporates the source study and owner screenshot review.
The original output is preserved as `source/PhobosShipbreakerInstalled-concept-v1.png`.

### Exact generation prompt

```text
Use case: stylized-concept.
Asset type: first intact world-sprite concept for the Phobos Powered Dismantling Fixture, an original Ostranauts mod machine.
Primary request: create ONE new original overhead industrial panel-dismantling machine, designed for a four-by-four game-tile footprint and eventual 64 x 64 pixel world sprite. This is the machine seen on the ship's floor, not a control-panel UI. Display the whole isolated object enlarged on a square canvas with a genuinely transparent background.

Art direction: match the restrained, low-resolution top-down machinery language of Ostranauts: broad fairly flat colour blocks, crisp stepped pixel-like contours, a few local tonal steps, and small purposeful mechanical details concentrated at joints and edges. In the user's game references, a large blue reactor housing is visually simple, while pale fittings, red bellows and yellow components give distinct local contrast. Follow that economical visual language, but invent a different dismantling-machine design. No copying of the reactor or screenshot layout. The owner values readable shapes and contrast over high-fidelity texture. Make this look like a modest-resolution game sprite enlarged without smoothing, rather than a smooth 3D product rendering.

Geometry and function: strict orthographic view from directly above, zero camera tilt or isometric perspective. Compact roughly square footprint with a slightly notched mechanical outline and small transparent gaps. A large empty dark rectangular working bed is the dominant form. Two clear feed-guide rails, a few substantial clamps and ONE simple transverse gantry with a compact enclosed separation head communicate holding and dismantling a wall panel. A smaller closed service enclosure and a shallow empty collection recess occupy supporting areas within the same footprint. Keep clear access at the bottom/front. Two small unconnected electrical sockets sit near the rear/top corners. All hardware fits inside the four-by-four footprint; no external output bin or sprawling rack.

Palette and finish: charcoal working bed, restrained steel grey mechanisms, a warm off-white service cover, a limited slate-blue housing area, a few ochre/yellow guard accents. Broad calm surfaces between mechanisms. Matte materials with subtle local relief and only sparse handling wear. One or two small dark indicator lenses are enough; no glow. Avoid filling every blank area with bolts, labels or grunge.

State and composition: intact, idle, empty. No wall panel being processed and no permanent heap of scrap. One asset only, large and centred, fully in frame with a narrow even transparent margin. No tiled sheet, inset, diagrams, labels, typography, logos, watermark, floor, environment or people. Do not paint ground shadows, view-obscuration wedges or dramatic directional lighting.

Crucial infrastructure distinction: electrical conduit in Ostranauts is BUILT AND PLACED SEPARATELY. Do not include a yellow-and-black/white striped conduit loop, junction-box perimeter, connected external cabling or imitation conduit border as part of this machine. Show only the machine's own supports and unconnected sockets. No reactor radiation symbol, circular fan or furnace.
```
