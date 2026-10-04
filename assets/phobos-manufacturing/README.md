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

## Current chemical machinery artwork (5 October 2026)

The owner approved Oxsmith's new artwork finish and requested the same treatment
for every other chemical reactor, leaving the silos as they were. The selected
replacements are Fennmark V4, X2 and K2; Tolvane AX-2; Lixivar LC-3 and SA-3; and
the Alembrine Copperhead-3 fermenter-still. They retain graphite/orange, teal/yellow,
sage/slate and brass/copper identities respectively. Broader mechanical assemblies,
bolted lids, restrained wear and clear material blocks follow the owner-approved
Oxsmith finish and the local study of Blue Bottle Games' Ostranauts artwork.

[Exact prompts and provenance](chemical-reactor-requests.json) retain the eight
built-in Imagegen calls: seven designs and one K2 correction replacing misleading
mould-like recesses with a closed gas manifold. Every untouched original is
1254 x 1254. Only original Phobos references were supplied; no PixelLab call or
paid-credit purchase was used. The tool does not disclose monetary cost.

[register-chemical-reactors.py](register-chemical-reactors.py), using the common
[registration module](registration.py), prepares 96-colour working masters at
four times native size: 256 for V4, 192 for the three-tile machines, 128 for the
two-tile machines. Native dimensions remain 64, 48 and 32 respectively, with full
opaque coverage, no painted live instruments and no changing workpieces.
The shared completion exporter writes the existing game image paths and matching
neutral normals. Both registration scripts support `--check`; the Manufacturing
build verifies them before packaging.

The [native/four-times review](previews/chemical-reactor-restyle-review.png) shows
each replacement and its retained earlier native reference. Exactly seven game
colour PNGs changed; every other Manufacturing image, including all silos/tanks
and neutral normals, was verified byte-identical. Original footprints, IDs,
ports, chemistry, prices and saved state are untouched; game lighting and rotation
still await owner review.

The seven previous selected working masters are preserved byte for byte in
local archive commit `4b2c15852cdd78e02b61abe328e1b9dd8d173825` on
`codex/rejected-artwork` (not pushed). The request record and archive inventory
retain their hashes and original provider/job records. Earlier native images stay
under `references/` because they were inputs to the selected revisions and to the
initial Oxsmith review. Oxsmith's own sources, masters and review are unchanged;
its initial comparison pictures now explicitly point to those historical snapshots.

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

## Oxsmith EC-4 and CR-4 prepared artwork

5 October 2026. The owner delegated artistic judgement for the
[Oxsmith handoff](../../docs/development/oxsmith-art-handoff.md), keeping a resemblance
to Blue Bottle Games' Ostranauts equipment style. Two calls to OpenAI's built-in
Imagegen made the [EC-4 original](source/oxsmith-ec4-chatgpt.png) and
[CR-4 original](source/oxsmith-cr4-chatgpt.png), each 1254 x 1254. Only original
Phobos images were supplied: the V4 for the overhead pilot, then that reviewed
EC-4 for the reactor's family style. No game art was sent to the provider.

The Oxsmith family uses oxide-red enamel, pale ceramic, blackened steel and small
ice-blue oxygen fittings. The cell has three electrode caps and an empty mould
tray; the reactor has a plain lid with six injector caps, paired gas domes and
an empty grille trough. No glowing sight ports, changing workpieces or live-state
instruments are baked into either machine.

[register-oxsmith.py](register-oxsmith.py) reads the common geometry and pinned
sources in [oxsmith-requests.json](oxsmith-requests.json), making 256 x 256
palette-reduced working masters and 64 x 64 native review colour/neutral normals.
Both cover their four-by-four footprint edge to edge. Source reduction uses BOX,
then 96-colour median cut without dithering; native export uses nearest neighbour.
Run with `--check` for byte-for-byte verification. The
[family preview](previews/oxsmith-family-review.png) shows native and four-times
views beside the V4, LC-3 and SA-3; [export hashes](oxsmith-export-hashes.json)
cover all derivatives. The untouched originals remain larger than the registered
working masters.

The EC-4 pilot was inspected before the CR-4 generation, and both passed the
native-size family review. These are prepared art, with no machine code or runtime
bindings; owner in-game review is pending. No PixelLab call or credit purchase was
used. Built-in Imagegen monetary cost is not disclosed by the tool. Exact prompts
and provenance are separate from code licensing and process research;
[OpenAI Terms of Use](https://openai.com/policies/terms-of-use/) is the provider
terms reference, rather than a new legal determination.

## Separate rights and research records

Art provenance and licensing (PixelLab's terms as checked on the generation
date) are recorded with the assets, separately from scientific attribution,
which sits beside the design claims in the refinery record. No web photos,
manufacturer CAD, extracted game artwork or foreign mod sprites are
redistributed or submitted as generation inputs.
