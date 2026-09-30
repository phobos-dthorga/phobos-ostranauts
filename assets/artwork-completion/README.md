# Dedicated artwork completion — 27 September 2026

Prepared for Agriculture 0.15.0 and Shipbreaker 0.28.0 using Framework 0.28.0.
All 48 selected masters were inspected overhead and at native export size;
owner gameplay/visual approval remains pending. No installation or publication.

Fifteen supply/intermediate sprites and three state variants for eleven equipment
families supply 136 native color/normal PNGs, including crop and coupling
derivatives. Existing approved originals, food sprites and physical geometry
remain unchanged. Protective covers and straps are visual cues, not extra items.

`manifest.json` records selected sources, registration, dimensions and hashes.
Request JSON files retain exact prompts, seeds, operation costs and provider
job/gallery IDs. `exports.json` records direct exports; `runtime-hashes.json`
also covers composed derivatives. Numbered previews show native and integer
enlargements. The source checkout retains original downloads in `source/` and
registration inputs in `references/`. Packages include records and previews;
complete masters and mechanical export tooling remain in the
[repository](https://github.com/phobos-dthorga/phobos-ostranauts/tree/main/assets/artwork-completion).

Run `python scripts/export-completion-art.py --check` from a source checkout
with Pillow for byte-for-byte verification without generation. Nearest-neighbor
exports use retained masters at least twice each output axis (four times when
the output short side is 32 pixels or less). Mechanical registration restores
the original alpha silhouette; selected damage patches retain the original
chassis outside authored damage regions. Enlarged references are registration
guides, not new production masters. Normals are neutral, with matching alpha;
they do not provide authored surface relief.

PixelLab supplied all new masters through Pixflux creation or one-generation
Pixen edits. The account allowance decreased from 1,952 to 1,894 during the pass
(58 included generations), with $0 credit balance throughout and no purchases.
One B2 damage request explicitly timed out before a successful retry. Capacity
rejections returned no job ID and were retried after slots became available.
Rejected/superseded originals are preserved on the dedicated archive branch;
see the [archive inventory](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/assets/rejected-artwork-archive.md) for exact paths and hashes.
Initial projection failures prompted the owner's mandatory overhead-first rule.

PixelLab's [terms of service](https://www.pixellab.ai/termsofservice), checked
27 September 2026, report a 23 November 2025 revision. Output use, modification
and distribution are permitted subject to those terms; model-training restrictions
and their Open RAIL-M reference remain applicable. Provider terms are separate
from code licensing, statutory copyright and third-party clearance. Only original
Phobos imagery or text was submitted; no Blue Bottle Games assets were uploaded.

Underfloor terminal concepts remain design-only until equipment contracts are
settled; Manufacturing's first equipment has its own pass below. Native circuit boards and generic maintenance waste
intentionally reuse game resources at runtime. R3 already has dedicated intact
and damaged masters shared with its loose forms. No gameplay systems, scientific
claims or material outputs are added by artwork.

## Bulk silo pass — 29 September 2026

Shipbreaker 0.38.1 adds five selected masters: `source/s3-silo.png`,
`source/t2-thaw-unit.png`, `source/aluminium-ingot.png`, `source/steel-ingot.png`
and `source/steel-melt-remainder.png`. Every request, seed, setting, job ID,
cost and review is in [bulk-silo-requests.json](bulk-silo-requests.json); the
original start drawings the selected jobs used are kept in `references/`.
Pixflux and Pixen created the masters; the steel ingot is a recorded luminance
recolour of the aluminium ingot, not a generation. The S3 and T2 are marked
`fullFootprint` in the manifest: they are opaque edge to edge because they fill
their deck squares. The allowance decreased from 1,890 to 1,875 (15 included
generations) with $0 credit and no purchases. Only original Phobos drawings were
uploaded. Rejected and superseded outputs are archived at commit `1df5784` on the
archive branch; the design record is
[the bulk silo handoff](../../docs/development/bulk-silo-art-handoff.md).

## Manufacturing 0.1.0 pass, 29 September 2026

Seven selected masters for the Fennmark family: `source/v4-refinery.png`,
`source/x2-processor.png`, `source/h2-store.png` (full footprint, world sprite
as portrait), `source/carbon-stock.png`, `source/refinery-slag.png`,
`source/anhydrous-residue.png`, and `source/nickel-iron-ingot.png`, a recorded
luminance recolour of the aluminium ingot. Every request, seed, setting, job
ID, cost and review is in [manufacturing-requests.json](manufacturing-requests.json);
the original start drawings and the first passes that the selected second
passes used are kept in `references/`. The allowance decreased from 1,875 to
1,865 (10 included generations) with $0 credit and no purchases. Only original
Phobos drawings were uploaded. Nothing was archived: every unselected output
is a retained input of a selected one. The design record is
[the Fennmark art handoff](../../docs/development/manufacturing-art-handoff.md).

Manufacturing 0.2.0 adds `source/k2-reactor.png` and `source/m2-methane-store.png`
(full footprint, 32 px native from 128 px masters) with their start drawings and
first passes in `references/`; four included generations (1,865 to 1,861), $0
credit, no purchases, nothing archived. Requests are appended to
[manufacturing-requests.json](manufacturing-requests.json).

Manufacturing 0.18.0 adds the Lixivar family: `source/lc3-leach-unit.png` (full
footprint, 48 px native from a 96 px master; the first pass with a recorded repair
turning a blue drain ring steel grey, so nothing reads as a lamp), and the
evaporite crust, potassium sulfate, phosphate concentrate and caustic remainder
(64 px masters, 16 px native). Struvite is a recorded palette swap of the potassium
sulfate sack; the leached and calcined residues are recorded luminance ramps of the
anhydrous residue and the brine salt cake one of the spent salt cake. Start drawings
are original Pillow drawings in `references/`. Nine included generations (1,762 to
1,753), $0 credit, no purchases; two phosphate attempts returned empty canvases and
the rejected LC-3 second pass and struvite pass stay in the provider gallery, so
nothing was archived. Every request, seed, job ID, repair and derivation is in
[round-three-requests.json](round-three-requests.json).

Agriculture 0.27.0 adds the Groundwork nutrient hopper: `source/e2-nutrient-hopper.png`
(full footprint, the E2 at 32 px and the E4 at 64 px, both integer reductions of one
128 px master) and `source/e3-nutrient-hopper.png` (96 px, from the E2 layout), from an
original start drawing in the reservoir family's palette. Three included generations
(1,753 to 1,750), $0 credit; the first E2 pass is kept in `references/` as the input of
the selected second pass.

Manufacturing 0.19.0 adds `source/sa3-acid-plant.png` (full footprint, 48 px native
from 96 px), `source/at2-acid-tank.png` (the AT-2 at 32 px and the AT-4 at 64 px,
both integer reductions) and `source/at3-acid-tank.png` (96 px, from the AT-2 layout),
the sulfide nodule and phosphoric acid flask (64 px masters), and the roasted
calcine as a recorded rust ramp of the anhydrous residue. Five included generations
(1,750 to 1,745), $0 credit, nothing rejected. Records are appended to
[round-three-requests.json](round-three-requests.json).
