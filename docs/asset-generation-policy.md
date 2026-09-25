# Asset generation: PixelLab first for simpler pixel art

Owner memorandum, **25 September 2026**, effective immediately across all Phobos
Ostranauts mods. Its purpose is to reduce generation costs while preserving the
game's visual style and making later changes inexpensive.

## Choosing the tool

Reuse suitable vanilla controls and existing approved Phobos assets first.
Use deterministic layout, live localized labels and mechanical exports for work
that does not need new authored imagery.

For new simpler raster artwork, use **PixelLab**: small machinery fittings,
materials, icons, uncomplicated props and suitable state variants. Reserve
ChatGPT's image generation for complex imagery where its additional capability
materially improves the result. Choose the operation for the actual asset; do
not infer price or quality from the provider name alone. This changes future
provider selection, not the provenance or approval of existing artwork.

The configured PixelLab MCP connection was verified through read-only balance
and capability calls. No generation was submitted for this memorandum. Reuse
that connection rather than copying credentials or building another integration.

## Small, cost-conscious workflow

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
