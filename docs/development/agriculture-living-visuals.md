# Agriculture 0.2.0: living racks and galley furniture

25 September 2026. Implemented candidate; offline checks and asset inspection are
separate from owner gameplay acceptance. The owner selected further feature work
before gameplay testing, specifically visible growth, lettuce artwork and
high-resolution ChatGPT equipment/furniture masters.

## Delivered appearance

Verdemorrow Firstlight-4 has four contained trays in a sage-green frame with cream
service panels, irrigation fittings, ventilation and inset light rails. Plants
are separate source images. Six states each for potatoes and lettuce compose onto
the same registered tray positions. Four pictured plants represent one rack's
existing crop cohort; there are no new planting slots or yield multipliers.

Hearth-2 now has a matching galley counter with a separate stove insert using the
existing PixelLab appliance source. The counter and its small preparation surface
are part of the cooker housing, not new independently buildable furniture or a
new food preparation recipe. There is no pictured sink or claimed plumbing route.

Both ChatGPT equipment originals are **1254 × 1254 RGBA**, as returned, despite
1024-square requests. Preserve them untouched. Native derivatives remain **64 ×
64** for the 4 × 4 rack and **32 × 32** for the 2 × 2 cooker. Plant sources remain
64 × 64 with 16 × 16 native derivatives. The originals exceed our resolution
policy; texture size does not enlarge a machine's physical footprint.

## Rendering and persistence

`CropAppearance` is the single read-only stage selector shared by the local panel
and world image. Healthy plants show sprout below 15% progress, young below 45%,
then mature and harvest-ready. Health below 75% selects wilted, and zero selects
dead. These thresholds and appearances are authored visual conventions, not
scientific measurements or diagnoses. Lettuce readiness depicts edible leaves,
not bolting, flowers or seed production. Pausing equipment does not hide biomass.

The existing two-second loaded-machine scan applies a new texture only when its
key changes. It calls native `Item.SetAlt` on the original item/renderer with
matching normal and damage settings. Native rotation, visibility, lighting,
collision and wear remain in control. It adds no emissive layer or always-visible
plant renderer. Art errors are logged once per key and cannot fault the crop.

Appearance is derived from saved crop state, not independently saved. Clearing
or successful harvesting selects the empty rack. Protected/unreadable state uses
the base rack with the existing panel warning rather than inventing a healthy
crop. Missing textures leave the existing world image; this is a diagnostic
fallback, not a new gameplay state. Rendering neither advances time nor restores
automation permissions. Reloaded/damaged items are refreshed through the same
scan. Unloaded ships are not rendered or simulated by this feature.

## Registered sources and provenance

The repository's `assets/phobos-agriculture/layers.json` owns the tray centers,
plant size and stove placement. `scripts/export-agriculture-art.py` produces all
compositions, matching flat normals, export hashes and a review sheet. Whole
canvases are retained across stages; no independent per-stage bounding-box crop
can shift a root. Normals are neutral placeholders, not generated relief maps.

ChatGPT's built-in image tool generated two original furniture/equipment masters;
PixelLab generated six retained lettuce candidates using eight included
generations, including two rejected wilted attempts. No additional credit was
purchased. Tool-reported usage does not establish a cash cost for ChatGPT imagery.
Prompts, seeds, IDs, result URLs, rejection notes and usage are retained separately
from the original potato pilot in `living-visuals-generation-records.json`.
The package includes this as `agriculture-living-art-provenance.json`, alongside
`agriculture-art-layers.json`, `agriculture-art-exports.json` and the art notes.
No game-derived or NASA/ESA image was submitted as a generation reference.

NASA's Space Station Research Integration Office describes the root water/air
delivery challenge in [Station Science 101: Plant Research](https://www.nasa.gov/missions/station/ways-the-international-space-station-helps-us-study-plant-growth-in-space/).
That research motivates contained irrigation and circulation in our design;
these fictional fittings do not depict certified NASA hardware. The wider
[research report](agriculture-research.md) retains NASA crop research and ESA
MELiSSA attribution. Artwork is not evidence that our simplified crop simulation
or short growth cycles reproduce those experiments.

## Validation and remaining uncertainty

Offline checks cover the shared stage boundaries, both crops, stopped machinery,
stress/death precedence, protected/unknown crops, empty trays and nonmutating
display reads. Native-definition checks require all 12 rack color/normal pairs
at exactly 64 × 64. Export inspection checks alpha, composition and registration
at native size and integer enlargement. Builds do not establish Unity behavior.

Later owner gameplay checks: place/rotate both machines; compare panel and world
stages; inspect under native light/occlusion; pause, harvest/clear, damage/repair,
reload and switch ships. Look for stale imagery, obscured plants, bad alignment
or altered fit. Supplies still use runtime native art and machinery has no bespoke
damaged drawing yet. These remain honest limits, not blockers on further work.
