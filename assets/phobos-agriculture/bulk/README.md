# Groundwork R3 original artwork

Two retained original masters provide intact and damaged overhead chassis.
Loose forms reuse the corresponding chassis unchanged; labels and installation
state are live. Existing original nutrient-packet art is reused for the 500 g
charge. No new UI imagery, decorative pump, liquid animation or baked readings.

Run `scripts/export-bulk-storage-art.py` with Python/Pillow. It registers opaque
chassis bounds onto a 48 x 48 transparent canvas (16 pixels/tile, 3 x 3 tiles),
with a centre pivot and paired flat normal map using the exact colour alpha.
The 1,254+ pixel masters exceed the required two-times native dimensions.
`exports.json` records actual dimensions, registration and hashes.
`preview.png` shows all four forms at native size, four-times enlargement and
quarter turns. The pilot was inspected before the damaged variant was generated.

Two PixelLab trials produced unsuitable isometric projection and were rejected.
The built-in image generator then produced the strictly overhead original master
and a damaged edit. `generation-records.json` retains prompts, jobs and observed
usage. No generation costs are invented. Original outputs follow the providers'
recorded terms and the repository asset policy.

Blue Bottle Games' Ostranauts tanks, pumps and instruments were inspected only
as local references in the [art audit](../../../docs/development/agriculture-bulk-storage-art.md).
No native texture was submitted to a generator or added to distributed files.
Scientific attribution belongs in the [research report](../../../docs/development/agriculture-bulk-storage-research.md),
separate from this artwork provenance. Static registration checks do not establish
in-game lighting, actual rotation/pivot appearance or owner approval.
