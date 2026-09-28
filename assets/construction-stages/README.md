# D4, R4 and F6 construction artwork

Prepared **28 September 2026**, following the owner's four-sprite authorization.
These are selected **authoring layers and prepared exports**, not registered game
content. No construction renderer, gameplay behaviour, save, package or installed
file changes accompany this batch. See the [code audit](../../docs/development/construction-artwork-audit.md)
for the missing binding and lifecycle checks.

| New sprite | Intended appearance | Retained master | Native export |
|---|---|---|---|
| D4 intermediate | Workbed fitted, service cover absent, wiring exposed | 128 × 128 | 64 × 64 |
| R4 intermediate | Rollers visible before the central lid is fitted | 128 × 128 | 64 × 64 |
| F6 early | Open core with temporary cross braces | 192 × 192 | 96 × 96 |
| F6 intermediate | Open chamber bowl and unfinished service cabinet | 192 × 192 | 96 × 96 |

Existing D4/R4 section sprites provide possible early appearances; existing
completed sprites provide the finished appearances. Exact delivery/work thresholds
remain an implementation decision, not something encoded into these images.

## Review and registration

[New sprites at 2× and native size](previews/new-assets.png) and the
[three-stage comparisons](previews/stages.png) were inspected offline. Strict
overhead projection, transparent margins and native canvas dimensions are retained.
No labels, scenery, lighting wedges, operating glow or separate ship conduits were
added. The dark furnace core is a backed recess, not a hole in the ship's floor.

PixelLab did not preserve all outside structures in the R4/F6 edits. Those outputs
are selected as **interior layers**, not replacement chassis: reviewed patches in
[manifest.json](manifest.json) place their useful details onto unchanged original
Phobos frames. Feet, exterior plumbing and sockets outside the patches retain
their original pixels. The D4 edit did not produce the requested half-fitted bed;
its open service bay is retained as a later assembly stage, without another paid
attempt. No candidate was rejected in full; all four untouched masters are needed
to reproduce selected layers.

The export command is:

```text
python scripts/export-construction-art.py
python scripts/export-construction-art.py --check
```

It requires Pillow, validates retained-input hashes and 2× master resolution,
reduces with nearest-neighbour sampling and checks unchanged pixels outside the
approved patch masks. Colour/normal pairs and previews are written only within
this asset directory. `--check` verifies exact reproducibility without writing.
Coordinates are native pixels with half-open bounds and a centred pivot.

Original chassis normal maps remain outside edited areas; new interiors use
neutral tangent normals with matching transparency. This avoids retaining raised
lid lighting over an open compartment, but **new interior relief is not authored**.
Unity lighting, rotation, placeholder highlights and owner visual approval remain
unverified. Neutral interiors are a known limitation for integration review.

## Provenance and usage

All four new masters came from **PixelLab `edit_image_pixen`**, one generation per
image. Allowance decreased from 1,894 to 1,890; dollar credit balance remained $0.
There were four requests, no retries, no purchased credits and no other provider
calls. Prompts, seeds, job IDs, asset IDs and completion records are in `requests/`.
Every prompt begins with the project's vertical-overhead instruction. Pixen has
no separate camera parameter in this operation.

`source/` contains untouched provider PNGs. `references/` contains the original
Phobos machinery and normal maps used for deterministic composition; paths and
hashes are recorded in [references.json](references.json). Generation inputs use
the project's original Phobos artwork at commit
`e8534f53d8b5568a4f7e96d0c21753f85b7e3255`, not extracted Blue Bottle Games textures
or another mod author's images. Existing selected masters remain unchanged.

The provider's [PixelLab terms](https://www.pixellab.ai/termsofservice) apply.
The repository's [existing terms record](../artwork-completion/manifest.json)
was checked on 27 September 2026; this batch does not independently re-audit those
terms or assert exclusive copyright. Keep generated-art provenance separate from
the code licence. No new third-party game artwork is redistributed here.

No mod rebuild or installation is needed for these unbound assets. The next code
change must register stage selection without altering material bills, identities,
footprints, work progress or completion gates, and then test it in the game.
