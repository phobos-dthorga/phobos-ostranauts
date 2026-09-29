# Manufacturing artwork brief and provenance boundary

29 September 2026. Manufacturing's nine sprites (Fennmark V4, X2, H2, K2 and M2
machines; nickel-iron ingot, carbon stock, refinery slag and anhydrous residue)
were produced with PixelLab and live in the shared artwork-completion pipeline:
masters in `assets/artwork-completion/source/`, generation inputs in
`references/`, the request record in `manufacturing-requests.json`, exports in
`mods/PhobosManufacturing/images/phobos/manufacturing/` through
`scripts/export-completion-art.py`. The design record is
[the Fennmark art handoff](../../docs/development/manufacturing-art-handoff.md).
These notes are packaged as `manufacturing-art-brief.md`.

Clay hydrates reuse the game's hydrate mineral artwork at runtime by reference
until a 16 px master exists. The proposed Rivetline M4 machining centre has no
artwork; its brief below stands for when it is implemented.

## Fennmark family

Graphite painted frames with burnt-orange corner clamps and straps, pale sand
deck plates, copper bands and stubs. Machines are full-footprint overhead
sprites (opaque edge to edge) that also serve as their portraits on every form.
Stock sprites are transparent 64 x 64 masters exported at 16 x 16. Connections
(power, water, hydrogen) are separate native objects and live text; do not
paint them into the sprites.

## Rules for new Manufacturing artwork

- Overhead-first PixelLab requests under `docs/development/asset-generation-policy.md`:
  the verbatim prompt prefix, `view="high top-down"`, `isometric=false`, one
  pilot per family, inspection at native size before expanding.
- Resolution memorandum: masters at 2x for native sizes above 32 px, 4x at
  32 px or below; nearest-neighbour integer exports only; keep larger originals.
- Read allowance and cost first; no purchases; record prompt, seed, job ID,
  cost, allowance before and after, hashes and the review with every asset.
- Only original Phobos imagery is uploaded. Rejected outputs go to
  `codex/rejected-artwork`; generation inputs of selected outputs stay on main.
- Preserve pivots, footprints and the registered silhouette when revising a
  family; a repaint pass over a retained first pass is the accepted method.

## M4 machining centre brief (design only)

The 4 x 4 machine would use a 64 x 64 native world sprite from at least a
128 x 128 master: a compact industrial enclosure, restrained work opening,
positive fixture jaws, a short spindle head, cartridge access and one readable
power point, in the Rivetline family. Item proposals: 32 x 32 preform from a
128 x 128 master; 16 x 16 sink and cartridge forms from 64 x 64 masters.
Registration becomes authoritative only when real masters exist.

## Separate rights and research records

Art provenance and licensing (PixelLab's terms as checked on the generation
date) are recorded with the assets, separately from scientific attribution,
which sits beside the design claims in the refinery record. No web photos,
manufacturer CAD, extracted game artwork or foreign mod sprites are
redistributed or submitted as generation inputs.
