# F6-C conduit artwork reuse

25 September 2026. No new generation or visual editing.

The original Phobos Agriculture `WaterPipe.png`, `WaterPipeNormal.png`,
`WaterPipeSheet.png` and `WaterPipeSheetNormal.png` are copied byte-for-byte to
Shipbreaker's `images/phobos/shipbreaker/FurnaceCoolantPipe*.png` by
`scripts/export-furnace-art.ps1`. Single tiles stay 16 × 16; the 16-cell cardinal
sheet stays 64 × 64. Normals, alignment and pixel proportions are unchanged.

Original master, provider, exact generation prompts and layered export method:
[Agriculture generation records](../phobos-agriculture/irrigation-generation-records.json),
[layer definitions](../phobos-agriculture/irrigation-layers.json) and
[export manifest](../phobos-agriculture/irrigation-exports.json).
The retained master remains in that source family; duplication of generated
masters is unnecessary. These are project originals, not extracted game assets.

The same visible jacket represents two insulated coolant channels in the F6-C
description. Internal bores are not depicted. Labels and circuit validity remain
live. Distinguish irrigation and coolant equipment by name; visual identification
and electrical overlap await owner evaluation. A later colour variant can change
these exports without changing item IDs, thermal records or route rules.
