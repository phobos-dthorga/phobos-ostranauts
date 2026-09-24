# Artwork resolution policy

Owner memorandum, 24 September 2026. Applies to newly created artwork and visual
revisions across the project. The owner also agreed to integer scaling and
nearest-neighbour sampling for pixel art.

## Production resolution

Keep a production asset at **at least twice the intended width and height**.
Use **four times each dimension for very small graphics**. The working convention
for “very small” is an intended short side of 32 pixels or less; this threshold
is our implementation choice, rather than a number specified by the owner.
Record intended dimensions before selecting the multiplier. For responsive UI,
use the planned reference display size, not the dimensions of a generated master.

| Intended dimensions | Multiplier | Minimum production dimensions |
| --- | --- | --- |
| 16 x 16 item icon | 4x | 64 x 64 |
| 64 x 16 narrow machine sprite | 4x | 256 x 64 |
| 48 x 48 console sprite | 2x | 96 x 96 |
| 64 x 64 machine sprite | 2x | 128 x 128 |
| 256 x 256 portrait | 2x | 512 x 512 |

Retain larger original masters, exact generation prompts and provenance. Do not
shrink an existing approved master merely to meet the minimum. Production
resolution and the source's actual level of detail are separate: enlarging a
small bitmap adds pixels, not new visual information.

Preserve the game's readable silhouettes, coarse pixel clusters and restrained
detail. Use exact integer enlargement and nearest-neighbour sampling for pixel
art; do not introduce blur or smooth edges. Judge the result at its intended
display size as well as enlarged. Smooth faceplates can retain their approved
surface treatment; this rule does not turn every UI texture into pixel art.

## Rendering and exports

Resolution does not change tile footprint, inventory capacity, collision bounds,
UI hit areas or navigation-panel placement bounds. The
[equipment art study](ship-equipment-art-study.md#scale-and-pixels) records that
the inspected native world-item path derives displayed size from texture
dimensions at 16 pixels per tile and selects point filtering. A larger PNG
alone would therefore enlarge the object.

For a new or revised asset, use either an explicit render-scale adjustment that
preserves its intended bounds, or a reproducible code-generated native-size
derivative while retaining the larger production asset. The latter fits the
current world-sprite exporters; this memorandum does not claim that a general
high-resolution world-rendering adapter already exists. A native-size derivative
cannot display all the additional source detail. Higher-resolution UI/portraits
and any later scale-aware world renderer can use the retained production asset.

Keep colour, normal, damage and other registered maps aligned at each resolution,
including crop, pivot, margins and silhouette. Preserve normal-vector validity
when generating technical maps. UI scaling must preserve the faceplate's borders,
screws and live text/control bounds; increase bitmap resolution without enlarging
those controls.

When an asset changes, record its intended dimensions, production dimensions,
multiplier and actual runtime export/scale in the asset family's notes or manifest.
Extend the existing exporter and its dimension/alignment checks as needed. Reuse
common export helpers when multiple real consumers need them.

## Existing artwork

The approved Auto Nav and industrial artwork remains the visual baseline.
Unchanged assets do not require bulk upscaling, regeneration or reinstallation.
Rebuilding the same deterministic export is not a visual revision. Apply this
policy when creating an asset or deliberately changing its appearance; retain
historic masters and review records rather than rewriting their recorded sizes.

This documentation establishes the policy. It does not alter shipped graphics,
export scripts, runtime rendering or game definitions.
