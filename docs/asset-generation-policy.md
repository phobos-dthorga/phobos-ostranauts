# Asset generation: layered ChatGPT bases and PixelLab sprites

Owner memorandum, **25 September 2026**, effective immediately across all Phobos
Ostranauts mods. Its purpose is to reduce generation costs while preserving the
game's visual style and making later changes inexpensive.

## Choosing the tool

Reuse suitable vanilla controls and existing approved Phobos assets first.
Use deterministic layout, live localized labels and mechanical exports for work
that does not need new authored imagery.

For possible future talking portraits and restrained, useful sound cues, see
the [animation and sound direction](animation-and-sound-direction.md). PixelLab
mouth-shape animation is a visual capability, separate from audio synthesis.
That memorandum records the owner's interest in brief, unobtrusive sound
retrofits with HIGH or MEDIUM-HIGH value; it does not add sounds or animations
to current packages.

For new simpler raster artwork, prefer **PixelLab**: small machinery fittings,
materials, icons, uncomplicated props and suitable state variants. The owner's
subsequent memorandum on **25 September 2026 explicitly permits ChatGPT image
generation for high-resolution equipment/furniture bases**, with separately
registered PixelLab sprites layered onto them. This applies across all Phobos
Ostranauts mods, including future Manufacturing equipment. ChatGPT can also supply
other complex imagery where it materially improves the result. Choose by the
actual layer's needs; neither both providers nor a new base are required for
every asset. Existing approved art needs no migration or regeneration.

The configured PixelLab MCP connection was verified through read-only balance
and capability calls. No generation was submitted for this memorandum. Reuse
that connection rather than copying credentials or building another integration.

## Layered production memorandum

Use each provider where its output is useful, preserving independent source
layers and repeatable composition:

| Layer | Preferred source / role |
| --- | --- |
| Equipment chassis, grow-rack, galley furniture, machine enclosure | ChatGPT high-resolution original master when useful for the base design |
| Plants and growth stages, stove insert, workpieces, small fittings, simple variants | PixelLab sprites, kept separate from the base |
| Labels, readings, buttons and instruments | Live localized text and isolated native controls; original raster UI only for a documented gap |
| Game-ready image/state | Deterministic composition/export from the retained layers, or a supported native visual overlay where runtime change requires it |

The current reference is the owner's **Research Ostranauts agriculture mod** task
and its `assets/phobos-agriculture/layers.json`: separate Firstlight rack and
Hearth counter masters, registered plant positions, a separate cooking appliance
insert, and potato/lettuce state sprites. The task reported those masters and
sprite families prepared; runtime composition and owner evaluation were still
in progress when this memorandum was recorded. This is a production precedent,
not a claim that the new visuals have been approved in-game.

Recommendations adopted for this workflow:

- Establish transparent canvases, overhead projection, native footprint, crop,
  pivot, layer order and named attachment positions before requesting variants.
  Record coordinate units and intended native dimensions in a small manifest.
  Different source resolutions are acceptable; alignment is defined in one
  shared output coordinate system, not guessed for each export.
- A high-resolution base is a **source master**, not permission to ship a larger
  native footprint. Apply the existing world export/scaling rules. Inspect the
  composite at actual game size: palette, pixel clusters, outline strength and
  shading must agree. A detailed master should not leave a smooth-looking base
  underneath conspicuously coarse sprites. Nearest-neighbour reduction preserves
  sampling; it does not by itself create a coherent pixel-art design.
- Keep replaceable content out of the permanent base: plants, movable workpieces,
  changeable appliances and status indicators should be their own layers.
  Foreground lips/guards may need a separate occlusion layer so contents sit
  inside equipment correctly rather than painting over its edges.
- Authoring layers do not require one game object per layer. Flatten static
  combinations with the existing exporter. Change only the necessary visual
  state at runtime using the native rendering path; preserve common rotation,
  lighting, visibility and damage treatment. Keep colour/normal/state alignment,
  and do not turn a visual insert into extra physical cargo or simulated equipment.
- Preserve each untouched master, layer provenance, generation requests and
  hashes, plus the composition manifest and exporter. Regenerate only the layer
  whose design changed. Validate one representative base-plus-sprite composite
  before expanding an entire state family.

The earlier cost-conscious direction remains: no mandatory dual-provider pass,
bulk remake of approved art, silent paid-credit purchase or assumption that this
workflow is automatically cheaper. Record observed generation usage separately
for each provider when available. This memorandum permits the complementary
workflow; it is not a request to generate additional artwork immediately.

## Small, cost-conscious workflow

### Rejected artwork archive

Owner direction, 27 September 2026: keep rejected and unselected image binaries
on [codex/rejected-artwork](https://github.com/phobos-dthorga/phobos-ostranauts/tree/codex/rejected-artwork).
Verify the archived originals before removing them from the current main tree.
Keep selected masters, required inputs and runtime exports on main, together
with the original requests, decisions, hashes and an
[archive inventory](https://github.com/phobos-dthorga/phobos-ostranauts/blob/main/assets/rejected-artwork-archive.md). Future additions must
preserve existing archive contents and record their exact archive commit; do not
reset the archive branch or rewrite main history. This does not change provider
licensing or turn discarded candidates into approved assets.

### Overhead-first PixelLab rule — owner direction, 27 September 2026

All Ostranauts **world sprites and inventory-item sprites** must request the
correct projection on the first submission. This also applies to machinery,
commodities and their damaged/loose variants. UI faceplates are flat front-facing
interfaces and are outside this world-camera rule.

- Begin the prompt with: **"ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera
  directly above, looking straight down at the object's TOP SURFACE ONLY.
  Rectangular edges run horizontally and vertically on the canvas. Show no
  vertical front or side faces."** Put this before the object description.
- For `create_image_pixflux`, explicitly pass `view="high top-down"` and
  `isometric=false`. Do not rely on defaults or the ambiguous word "top-down"
  alone. When an operation has no camera fields, retain the full prompt prefix.
- Describe the visible lid, top panel, rim, recesses and overhead silhouette.
  Avoid product-photography wording that invites a standing container or a
  three-quarter view. Express height/capacity in game definitions and live text,
  not through visible side walls. Do not use an isometric image as a style input.
- For a related state, prefer editing an already verified **overhead Phobos
  reference**, preserving canvas, footprint, pivot and attachments, instead of
  asking the generator to rediscover the camera. Only original project imagery
  may be uploaded; native game references stay local.
- Validate one overhead pilot at native scale before expanding that family.
  A diamond-shaped footprint or visible front/side faces is a projection failure;
  retain and label the rejected candidate. Do not export it merely because its
  palette or detail is attractive. Make one targeted correction before considering
  another approach; continue to follow the existing cost/fallback rules.
- Record requested camera settings and the actual projection review beside the
  generation request. Tool camera settings are weak guidance, **not a guarantee**;
  prompt compliance never substitutes for inspecting the output.

Reusable first-request wording for a small supply item:

> ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking
> straight down at the object's TOP SURFACE ONLY. Rectangular edges run
> horizontally and vertically on the canvas. Show no vertical front or side
> faces. Draw one sealed spacecraft supply canister as its rectangular grey TOP
> PANEL with a teal inset, circular filler cap and flush yellow handle. Original
> coarse pixel art, restrained shading. Centre the complete silhouette on a
> transparent 64 × 64 canvas with 8-pixel margins; intended native export 16 × 16.
> No perspective, scene, ground shadow, text or logos.

This rule follows projection failures observed in the coolant pilot and earlier
reservoir work. It aims to reduce avoidable retries; it does not claim a measured
success rate or guaranteed provider behaviour.

1. Establish the native footprint, projection, visible connection positions and
   master dimensions before generating. Retain the [resolution policy](artwork-resolution-policy.md):
   at least 2x per axis, or 4x when the intended short side is at most 32 pixels.
   A 16 x 16 port therefore needs at least a 64 x 64 master; enlargement alone
   does not create additional detail.
2. Read the current tool description and account allowance. For a simple image,
   start with `create_image_pixflux` when it meets the requirements. Its inspected
   tool description quotes one generation; the legacy one-direction object tool
   quotes 20–40 and can return many candidates even when only one was described.
   Treat these as inspection-time observations, not permanent prices. For Pro
   Flash, use its free capabilities/quote lookup before choosing dimensions.
3. Generate one independently useful object first. Use transparent backgrounds,
   deliberate margins and Ostranauts' overhead projection. Use a seed where
   supported. Request extra directions or animation only when the task needs
   them; a flat sprite's ordinary 90-degree game rotations do not require eight
   newly generated views.
4. Retain the untouched output, exact prompt, operation/model if disclosed, seed,
   job/asset ID, input provenance, hashes, returned dimensions and generation cost
   in the asset family's existing notes/manifest. Poll the original job after an
   uncertain response instead of submitting a duplicate charged request.
5. Inspect transparency, clipping, pixel scale, facing and attachment locations
   at native size and an integer enlargement. Correct crop, padding or export
   problems with existing deterministic tools where sufficient. Keep colour,
   normal and damage forms registered. Do not reroll merely to change a crop.
6. Inspect the first result before expanding the set. Keep retries proportional
   to a specific defect. If included allowance is unavailable or an operation
   unexpectedly needs additional paid credit, report that gap before proceeding.
   Never silently purchase credit or escalate to a costlier provider.

## Lessons taken from Codename Gekko

The following are workflow lessons from its local provenance and rejection
records; its project artwork and project-specific approval rules are not imported.

- **Independent objects:** the rejected construction-prop sheet crossed provider
  slicing boundaries and clipped props. Generate production props independently;
  treat a mixed sheet as ideation rather than a source of supposedly complete
  cropped equipment.
- **Actual output matters:** the municipal vehicle request returned 68 x 68
  images for a requested 64 x 64 canvas. Preserve complete silhouettes and inspect
  returned bounds before selecting a shared crop or pivot.
- **Avoid unnecessary rerolls:** the toolbox proof retained a larger original
  and used an explicit nearest-neighbour export after a smaller retry clipped
  the object. The grass study also identified deterministic palette changes as
  preferable to regenerating geometry for a palette-only adjustment.
- **Keep UI native:** the Civic Field Desk raster UI was rejected after resizing
  distorted painted surfaces and crossed control boundaries. Continue our vanilla
  widget preference and live text; PixelLab does not replace functioning controls.
- **Keep our own projection:** Gekko's fixed isometric requirement does not apply
  to Ostranauts' overhead machinery. A compatible palette is insufficient when
  the projection, scale or visible connections disagree with gameplay geometry.

Keep extracted game art local, preserve original authorship and record applicable
provider terms alongside generated assets. Do not send game textures or Gekko
assets as generation inputs merely because they were inspected for research.

## Polaris interface follow-up — 27 September 2026

The owner permits replacement interface artwork where suitability is MEDIUM-HIGH
or HIGH. Prefer Blue Bottle Games' native widget/artwork references at runtime,
then assess remaining gaps in readability, control boundaries and reusable
framing. Ratings are design judgements, not measured usability gains. Generate
one pilot only for a documented qualifying gap; keep all labels and states live.
Preserve previous selected masters and archive rejected outputs under the existing
policy. Flat UI artwork remains exempt from the world-sprite overhead camera rule.
See [the implemented interface review](polaris-interface-refresh.md).
