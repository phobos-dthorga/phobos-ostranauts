# Agriculture artwork: living visuals and original pilot

## Commodity sprites (0.6.1)

Twelve original PixelLab item sprites replace borrowed native stock images.
`stock-layers.json` maps each unchanged commodity identity to its independent
64 x 64 transparent source master and 16 x 16 native export. Whole-canvas,
nearest-neighbour exports preserve registration; each colour map has a matching
neutral tangent normal map with the same alpha. These normals provide no relief.
World and portrait definitions point to the same dedicated image. Native donor
socket geometry and item/food interactions are preserved.

Run `scripts/export-agriculture-stock-art.py` with Python/Pillow to reproduce
`stock-exports.json`, the mod PNGs and `stock-preview.png`. The review sheet shows
native size and an eight-times enlargement without smoothing. These are offline
inspection views, not game screenshots or owner gameplay approval.

The set includes Continuance seed potato and lettuce packet; Groundwork nutrients
and irrigation charge; raw potatoes, Hearth cooked potatoes and lettuce; crop
residue, legacy and recorded process solution, treatment rejects and a recovery
cartridge. Pictures of multiple potatoes represent one existing weighed portion,
not additional inventory or yield. Packaging is illustrative; no new material or
empty-container return is introduced. Neither lettuce seed production nor new
recovery recipes are added by the artwork.

`stock-generation-records.json` retains exact requests, seeds, job/asset IDs,
returned URLs, usage and source hashes. No game or third-party images were uploaded.
The provider is PixelLab, using `create_image_pixflux`; no ChatGPT image generation
or API fallback was needed for these small sprites. Provider terms are referenced
at [PixelLab's terms of service](https://www.pixellab.ai/termsofservice), separately
from the code licence. Rechecked on 25 September 2026, the page reports a
23 November 2025 update: it permits output use/modification/distribution, including
commercial use, prohibits model training without permission, and references
Open RAIL-M. This records provider terms, not statutory copyright or third-party
clearance. Fourteen included generations produced twelve retained sprites
(1.17 generations per retained sprite); two rejected candidates remain in the
source folder with reasons. No additional credits were purchased.

## Current 0.2.0 candidate

The owner requested high-resolution ChatGPT grow-rack and galley furniture art,
visible plants in the world and a full lettuce family on 25 September 2026.
`source/firstlight-rack-v2.png` and `source/hearth-counter-v2.png` are untouched
1254 × 1254 RGBA originals from the built-in ChatGPT image-generation tool.
The retained PixelLab stove is a separate insert in the galley counter. The
sage-green/cream equipment family belongs to Verdemorrow Agronomics.

`layers.json` records native sizes and registration for the four tray positions
and stove. `scripts/export-agriculture-art.py` exports 28 images plus 14 matching
flat normals, with hashes in `exports.json`. World/panel crop states use twelve
registered rack compositions. Four tray pictures represent one crop cohort;
footprints and crop yields remain unchanged. Native exports are 64-square racks,
32-square cooker/counter and 16-square plant/stove layers. Whole source canvases
are retained; native derivatives use nearest-neighbour sampling. Masters are
kept separately for future higher-resolution uses.

`living-visuals-preview.png` shows all crops at integer enlargement and the
separate equipment layers. It is an offline review sheet, not an in-game image.
All art remains a candidate pending owner review. Dedicated damaged drawings and
native relief normals remain future work; the runtime uses native wear handling.

`living-visuals-generation-records.json` retains the exact prompts, seeds,
provider operations, generation/asset IDs and result URLs for this round. Six
lettuce states used **eight included PixelLab generations** (two wilted attempts
rejected because they looked too healthy), or **1.33 generations per retained
lettuce sprite**. Two separate ChatGPT built-in calls produced the equipment.
Their model identifier and usage price were not disclosed. No additional credit
was purchased. Reference inputs were project-generated plants only; no game,
NASA or ESA images were uploaded. This round's records and the original pilot's
records both ship in the prepared package.

The built-in ChatGPT outputs have separate provider provenance from PixelLab.
[OpenAI's terms](https://openai.com/policies/terms-of-use/) are the provider
reference, not a software license applied to these images. The retrieved page
on 25 September 2026 was region-labelled Europe, so this record does not claim
verification of the owner's applicable account terms. No public release or
third-party rights clearance is asserted by this development candidate.

## Original 0.1.0 pilot (historical)

25 September 2026. Eight retained **candidate** sprites: empty rack, cooker and
potato sprout/young/mature/harvest/wilted/dead portraits. No owner in-game visual
approval is claimed. All references sent to PixelLab were generated for this
project; no game-derived images were uploaded.

Untouched originals are in `source/`, exact requests and returned job/asset IDs in
`generation-records.json`, and selected hashes/canvas/export settings in
`exports.json`. `scripts/export-agriculture-art.py` uses Python/Pillow for mechanical
nearest-neighbour export and registered flat normals. Masters are 128 × 128 for
64 × 64 rack and 32 × 32 cooker; 64 × 64 for 16 × 16 plant portraits. No independent
cropping shifts the stages. Root placement and legibility still need owner review.

The task used **16 included generations**, with every accepted request reporting
one generation. Eight retained candidates gives **2 included generations per
retained sprite**, counting retries. No credit was purchased and no paid-credit
fallback occurred. This is an allowance count, not a measured cash production
price; cleanup time and later gameplay acceptance remain unknown.

`create_image_pixflux` generated the initial plant and equipment. Its first rack
and cooker used the wrong projection; their retries corrected it. Five reference
regenerations failed to distinguish biological states and were rejected. The
one-generation `edit_image_pixen` route then made clear small/young/yellow/wilted/
dead states and removed unwanted plants from the empty rack. Prefer that editor
for related states before considering the much more expensive multi-frame tool.
Rejected/intermediate originals remain for provenance, not package artwork.

Observed [PixelLab terms](https://www.pixellab.ai/termsofservice), last updated
23 November 2025, permit use/modification/distribution of outputs, including
commercial use; they prohibit training other models without permission and refer
to an Open RAIL-M license. Preserve these provider terms separately from Phobos
code licensing. This record reports the provider's terms and is not a guarantee
of statutory copyright or third-party clearance. No NASA/ESA imagery is used.
## Irrigation additions, 25 September 2026

The [water-conduit candidate](../../docs/agriculture-water-conduits.md) adds an
overhead ChatGPT W2 chassis and one PixelLab pipe fitting. Exact prompts, output
IDs, terms, costs, rejected candidates and hashes are in
[irrigation-generation-records.json](irrigation-generation-records.json).
[irrigation-layers.json](irrigation-layers.json) records source crops and native
registration; [irrigation-exports.json](irrigation-exports.json) records outputs.
Run `scripts/export-irrigation-art.py`, then `scripts/export-agriculture-art.py`
with Python/Pillow to reproduce them, including the rack inlet fitting.
The original rack/chassis/plant masters remain unchanged. Two PixelLab supply
candidates were rejected for angled projection; they are retained but never
exported. Three included PixelLab generations and one ChatGPT call produced the
selected sources; no credits were purchased. Native-size previews were inspected;
these assets have not been approved in-game by the owner.

## Seed-production layers (0.7.0)

Two selected PixelLab 64 x 64 masters show yellow flowers and pale ripe seed
heads. Four included generations produced two usable layers; no paid credits.
The first pair is retained but rejected (soil/insufficient reproductive detail).
See [seed generation records](seed-generation-records.json) for prompts, seeds,
job/gallery IDs, hashes, terms and usage. No extracted game art was uploaded.
Unchanged masters export at 16 x 16 with registered four-tray composition;
early/stressed stages reuse existing lettuce assets. The updated preview was
inspected at native pixel scale and integer enlargement. In-game approval is pending.
The pictures simplify morphology; scientific attribution and authored lifecycle
limits are in [seed production](../../docs/agriculture-seed-production.md).
