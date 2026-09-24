# Original scrap-reclaimer artwork

Created with the built-in Imagegen tool on 2026-09-24 for Shipbreaker 0.8.0.
This is a new, unapproved testing candidate in the established Phobos/Ostranauts
visual direction. No game sprite or community artwork was used as an edit target.
The existing approved Phobos fixture preview was inspected for style context.

Master: `source/PhobosScrapReclaimer-v1.png` (1254 x 1254, transparent PNG).
SHA-256: `4e861578e9fd24dc11599cf2e1b09fe699d860e3bddbb599ed42096c93402328`.
The unchanged generated master is retained. The export script crops
`[120,120,1040,1040]`, samples to 64 x 64 with nearest neighbour, thresholds alpha,
and creates a 256 x 256 portrait, 512 x 512 preview and neutral technical normal.
These are mechanical exports, not alternate generated designs. Run
`scripts/export-reclaimer-art.ps1`; the normal Shipbreaker build also runs it.

Installed, loose and damaged forms share the closed housing sprite with native
damage tint. This is disclosed in the player guide; no separately approved damaged
art or sculpted normal is claimed. The MIT repository scope and generated-art
qualifications in `mods/PhobosShipbreaker/THIRD-PARTY.md` apply.

Exact prompt:

> Create an original production game sprite: a top-down orthographic industrial scrap reclaimer for the game Ostranauts. One compact square 4 x 4 floor-tile appliance, intended to read at only 64 x 64 pixels. Extremely coarse pixel art, visible blocky pixels and stepped edges, restrained flat shading, NO photorealism, NO perspective/isometric angle. Closed rectangular cream/grey crushing chamber in center, small desaturated teal motor unit along one side, dark captive input slot at the bottom, two clearly separated product drawers at top, a few mustard yellow latch details, small green status dot. Simple functional silhouette and uncluttered details, weathered space-industrial equipment. Four small mounting feet, dark thin outline. No attached flooring, no room, no text, no grid, no loose scrap, NO electrical conduit loop around it. Genuine transparent background outside machine. Square canvas with small equal transparent margin. Render as a coarse 64-pixel sprite enlarged with nearest-neighbor pixel blocks. This is new machine art fitting alongside Phobos Shipbreaker's cream cabinets, blue chassis, dark work surfaces and sparse yellow safety details, but a distinct enclosed shredder/separator rather than an open dismantling table.
