# Agriculture PixelLab pilot

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
