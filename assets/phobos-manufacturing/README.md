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
archive commit `4b2c15852cdd78e02b61abe328e1b9dd8d173825` on
`codex/rejected-artwork` (pushed 5 October 2026). The request record and archive inventory
retain their hashes and original provider/job records. Earlier native images stay
under `references/` because they were inputs to the selected revisions and to the
initial Oxsmith review. Oxsmith's own sources, masters and review are unchanged;
its initial comparison pictures now explicitly point to those historical snapshots.

## Support machinery follow-up (5 October 2026)

The owner expanded the Oxsmith finish to the remaining equipment on 5 October
2026, then clarified that the really small sprites should stay with PixelLab.
The selected follow-up is the **L2 filling station, A2 air regulator and Corker-2
bottling unit**. Their 1254 x 1254 untouched originals, 128 x 128 working masters
and 32 x 32 native exports retain the existing two-by-two footprints, game image
names and maker palettes. Small materials, pipe tiles, one-tile P1/RM-1, the
original floor and all bulk storage remain unchanged.

[Exact requests and provenance](manufacturing-restyle-requests.json) include the
scope clarification and every earlier trial. [register-support-machinery.py](register-support-machinery.py)
uses the common registration module and supports `--check`; the Manufacturing
build verifies it. The [native/four-times comparison](previews/manufacturing-support-restyle-review.png)
shows the three selected replacements beside their earlier art. Only three
colour PNGs changed; all 145 other Manufacturing PNGs were verified byte-identical.
Function, prices, ports, saved state and image dimensions are unchanged. In-game
lighting and rotation still await owner review.

Five built-in Imagegen calls were made before the clarification; three outputs
are selected and two one-tile designs are archived. Twenty-four included PixelLab
generations also completed before the scope narrowed (allowance 1669 to 1645,
credit balance unchanged at $0); none is selected. Ten further submissions hit
the provider's eight-job queue limit, created no jobs and were not retried.
No additional small-sprite generation was submitted after the clarification.
Unselected originals, redundant snapshots and the three superseded selected
masters were verified in archive commit
`bccf43af5b5e215cfeca10e1d24d869bfc5e1c46` on `codex/rejected-artwork` before
removal; the archive append was pushed on 5 October 2026. Required inputs for the selected
three machines remain on main. [retain-pixellab-image.py](../../scripts/retain-pixellab-image.py)
can recover a completed job's original PNG, refuses to overwrite a different
retained file and limits destinations to the repository's asset tree.

## Plain acid and ethanol tank follow-up (5 October 2026)

The owner made an exception to the earlier silo exclusion for **Lixivar AT-2,
AT-3 and AT-4 sulphuric acid tanks** and **Alembrine Cask-2, Cask-3 and Cask-4
ethanol tanks**, explicitly ruling out readings. The selected designs have plain
sealed lids: sage enamel and slate for acid, steel with copper and brass for
ethanol. Their containment rims fill the entire square footprint. No text, logos,
gauges, fill strips, screens or live-state instruments are painted into either.

[Exact prompts and provenance](liquid-tank-requests.json) record two built-in
Imagegen originals, each 1254 x 1254. The original Phobos AT-2 supplied only the
overhead camera and full square footprint reference; it remains on main as a
required input. No native game art was uploaded. Each 32-pixel family pilot was
inspected at native size and four-times enlargement before deriving its larger
sizes from the same source. No further family generation was needed.

[register-liquid-tanks.py](register-liquid-tanks.py) reuses the existing common
registration module: 96-colour working masters at 128, 192 and 256 pixels, then
nearest-neighbour exports at the unchanged 32, 48 and 64 pixels. The completion
manifest binds all six existing image paths and all forms. The
[six-size comparison](previews/liquid-tank-restyle-review.png) and
[derivative hashes](liquid-tank-export-hashes.json) retain the review. Exactly six
game colour PNGs changed; all 142 other Manufacturing PNGs, including every normal
map and other bulk store, were verified byte-identical. Footprints, pivots, IDs,
ports, capacities, prices and saved state are unchanged. In-game lighting and
rotation still await owner review.

Four included PixelLab trials preceded the selected bases: two introduced floor
and shadows, and two targeted corrections held onto the disliked earlier design.
All are rejected. The allowance went from 1645 to 1641, with the credit balance
at $0 and no purchase. The two built-in calls do not disclose monetary cost.
The four rejected originals and three superseded masters were verified in
[archive commit 5ab8f35](https://github.com/phobos-dthorga/phobos-ostranauts/tree/5ab8f35e868398ca877118932833004f7fd85e9f)
on `codex/rejected-artwork` before removal; the append was pushed on 5 October
2026. Existing
archive contents, the selected originals and required generation inputs remain
preserved; the request record pins the old selections and archive hashes.

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
working masters. Since Manufacturing 0.52.0 the EC-4 master is bound to the machine through
the shared completion exporter (`assets/artwork-completion/manifest.json`, key `oxsmith-ec4`),
and since 0.53.0 the CR-4 master is too (key `oxsmith-cr4`).

The EC-4 pilot was inspected before the CR-4 generation, and both passed the
native-size family review. Both now have runtime bindings in the releases named
above; owner in-game review is pending. No PixelLab call or credit purchase was
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
