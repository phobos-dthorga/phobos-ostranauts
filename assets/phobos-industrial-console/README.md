# Industrial console artwork

Original Phobos artwork generated on 2026-09-24 through the built-in Imagegen
tool. Owner authorised production after reviewing text layouts. No extracted
game sprites are shipped. The faceplate references our previously generated,
owner-approved Auto Nav faceplate; console sprites were generated originally,
with each subsequent state referencing its own intact master.

`manifest.json` retains source filenames, hashes and export rectangles. Original
masters are retained in `source/`; the tool's originals were copied, not moved.
`scripts/export-industrial-console-art.ps1` checks hashes and performs only crop,
nearest-neighbour resizing and alpha thresholding. Four distinct world states
are exported at **48 x 48**, portraits at 256 x 256, and enlarged pixel previews
at 480 x 480. Lighting uses deliberately flat normal maps, consistent with the
existing reclaimer fallback, not authored relief. Damage art is a full state
texture, not a separate moving overlay. The common crop preserves alignment;
minor generated damage-state differences remain visible in the previews.

The shared faceplate stays at 1536 x 1024 and is sliced at runtime to keep its
corners. It contains no labels. Buttons, readouts, grouping and routing are live
Unity UI/TextMeshPro. World export and faceplate inspected visually; final game
lighting, seating and UI scale remain owner checks.

## Prompts and references

Installed intact (no image reference):

> Use case: stylized-concept. Create an ORIGINAL production sprite for an Ostranauts-compatible spaceship industrial control workstation. ONE intact INSTALLED workstation only, transparent background with real alpha, no sheet, no labels, no text, no watermark. Strict orthographic straight overhead view, no perspective, no visible front faces. The entire silhouette is square, designed for a 3 by 3 tile footprint, eventual export 48 by 48 pixels. Make it deliberately VERY COARSE pixel art: chunky clean square pixel clusters, stepped edges, flat 3-tone shading, no antialiasing, no painterly detail, no blur. A wide dark monitor/dashboard occupies the rear/top row; grey-blue muted slate casing and ivory small housings, a few ochre keys; two short side consoles with clear centre operator seat; dark empty upholstered chair at bottom centre facing the monitor, all inside footprint. There must be a clear reading of the seat from above, with no seated person. Simple utilitarian retro industrial engineering, sparse details. Sparse muted green rectangular marks on the monitor, never writing. Thin mounting feet. Keep four clear perimeter attachment points and a small rear power socket but no exterior cable or conduit network. No floor, room, cast shadow, decorative lighting or background. Centre with an even transparent margin about 8 percent. Low contrast neutral material tones, deliberate readable silhouette. Match grounded low-resolution top-down space-salvage machinery, avoid futuristic holograms or glossy 3D.

Installed damaged (reference: installed intact master):

> Use case: precise-object-edit. This is the installed/intact original industrial control workstation sprite. Create ONLY its installed DAMAGED state. Preserve EXACT canvas dimensions, silhouette placement, footprint, top-down angle, neutral palette, chunky coarse pixel scale and every mounting foot/seat position. Add a clear dark diagonal screen crack, one small localized scorch around the right-hand control block, and a broken ivory corner cover exposing a few dark inner pixels. Do not displace or rotate components or expand the silhouette. No detached pieces outside silhouette, no fire, smoke, people or text. Preserve genuinely transparent background alpha. The result is a registered damage texture overlay for the same 48 by 48 world footprint.

Loose intact (reference: installed intact master):

> Use case: precise-object-edit. Create ONLY the UNINSTALLED / LOOSE transport state of this original top-down industrial workstation. Same square canvas and overall square footprint, same slate/ivory/ochre palette and coarse chunky pixel-art scale. Pack the workstation compactly into a utilitarian protective beige square shipping frame; fold the chair inward, protect the monitor with a dark cover, add two simple ochre restraints across it. Make its relation to this same console clear. Straight overhead orthographic view, no perspective. Keep silhouette centred inside the same roughly 8 percent transparent margin. Transparent background with real alpha; no floor, shadow, labels, writing, people, loose components outside frame or conduit. Do not create a sheet or multiple states. This is an original game sprite ultimately exported at 48 by 48 pixels.

Loose damaged (reference: loose intact master):

> Use case: precise-object-edit. Create ONLY the DAMAGED loose packed transport state of this original industrial workstation. Preserve exact square canvas, silhouette, framing, placement, strapped shipping frame, top-down orthographic view and coarse chunky pixel scale. Small dark scorch at right, one broken ivory corner exposing dark internals, a crack in the protective cover. No protruding debris or silhouette expansion. Transparent background real alpha. No text, people, smoke, flames, scene or extra items. Must register with the supplied intact loose-state image for a 48 by 48 pixel game export.

Shared faceplate (reference: `mods/PhobosAutoNav/images/phobos/autonav/PhobosAutoNavPanel.png`):

> Use case: precise-object-edit. Create one NEW wide blank equipment control-panel faceplate in the exact restrained flat monotone slate-blue/grey style of the supplied approved plate. Wide landscape 3:2 canvas. Replace the three fixed inset windows with ONE plain uninterrupted near-black rectangular recessed centre occupying 88 percent width and 83 percent height. Narrow plain slate perimeter, four small slotted dark screws at corners, tiny bevel only. Orthographic face-on rectangular plate, modest rounded outer corners, no perspective, no cast shadow. Centre perfectly empty even near-black for runtime controls. No divider lines, no buttons, lights, lettering, labels, icons or text. Uniform matte flat neutral slate, no strong gradients, rust, ornamental grain or wear. The silhouette nearly fills canvas, transparent alpha only outside rounded corners. Game UI asset suited to nine-slice scaling; all ornament confined to outermost 6 percent border. Original derivative of our supplied generated artwork.
