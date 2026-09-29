# Bulk silo, thaw unit and ingot artwork handoff

Prepared 29 September 2026 for Shipbreaker 0.37.0 (S3 silo, T2 thaw unit) and
0.38.0 (ingots), when both shipped procedural placeholders. **Produced the same
day with PixelLab** and integrated as Shipbreaker 0.38.1: the placeholders,
their masters and `scripts/export-silo-placeholder-art.py` are retired. Owner
review of the art in play (scale, lighting, damage tint) is pending.

## Result

| Asset | Native / master | Operation and selected job | Notes |
| --- | --- | --- | --- |
| S3 process water silo (`PhobosProcessSilo`) | 48 / 96 px | Pixflux from an original Rivetline-coloured start drawing, strength 90, job `3553be2c` | Blue frame, yellow clamps, cream tank, teal band, hatch |
| T2 ice thaw unit (`PhobosIceThaw`) | 32 / 128 px | Pixflux second pass over the first pass, strength 120, job `de7fe18b` | Same family as the S3; frosted window, vents, tray hatch |
| Aluminium ingot (`StockAluminiumIngot`) | 16 / 64 px | Pixen, job `e52b5cea` | Overhead bar with bevel rim and stamped recess |
| Steel ingot (`StockSteelIngot`) | 16 / 64 px | Derived: per-pixel luminance recolour of the aluminium master | Same drawing, darker steel tones; no generation |
| Steel melt remainder (`StockSteelMeltRemainder`) | 16 / 64 px | Pixflux from an original start drawing, strength 60, job `5e842dea` | Dark lump with a rust streak, distinct from `StockMeltRemainder` |

Masters, hashes and exports are registered in
[the artwork-completion manifest](../../assets/artwork-completion/manifest.json);
every prompt, seed, setting, job ID, cost and review, including the rejected
attempts, is in
[bulk-silo-requests.json](../../assets/artwork-completion/bulk-silo-requests.json).
The pass used 15 included generations (allowance 1,890 to 1,875), with $0
credit and no purchases. Rejected and superseded outputs are archived on
`codex/rejected-artwork` ([inventory](../../assets/rejected-artwork-archive.md)).
Only original Phobos drawings were uploaded as start images; no game art was.

Deviations from the requests below, and why:

- **No level-gauge strip on the silo, no separate layers.** Pixflux returns one
  flattened image; the gauge did not survive as a readable element at 48 px. The
  water level is live text on the Control Panel and the C1, which is where the
  player reads it anyway. A filled-state overlay would be a new request.
- **No separate 256 px portraits.** The S3 and T2 use their world sprite as the
  inventory portrait on every form (`strPortraitImg`), like the other
  artwork-completion equipment; the placeholder portraits were deleted.
- **Full-footprint masters.** Background removal made the silo's light dome and
  frame transparent twice, so the machines were generated as opaque squares that
  fill their whole footprint, like the game's square-deck machinery. The exporter
  checks this with its `fullFootprint` rule instead of requiring a padded
  silhouette.
- **Rivetline colours instead of worn grey-blue.** A monochrome grey silo passed
  projection but read as off-family; the selected pass uses the approved
  Shipbreaker/Rivetline colours (blue frame, yellow clamps, cream housing).
- **Steel ingot derived, not generated.** A deterministic recolour keeps the two
  ingots registered and distinct by tone alone; it cost nothing and cannot drift.
  The requested mill-scale edge was dropped as unreadable at 16 px.

## Original requests

The requests below are kept as issued. The placeholder table they referred to
was removed with the placeholders.

### Rules that apply

- [Asset generation policy](asset-generation-policy.md): overhead-first prompt
  prefix on every world/inventory sprite, `view="high top-down"` and
  `isometric=false` for `create_image_pixflux`, one pilot per family before
  expanding, rejected candidates archived on `codex/rejected-artwork`.
- [Resolution memorandum](artwork-resolution-policy.md): masters at 2x for the
  48 px silo (96 px or larger), 4x for the 32 px thaw unit (128 px) and 4x for
  16 px ingots (64 px); integer nearest-neighbour exports only.
- [Equipment branding](equipment-branding.md): Rivetline S3 and T2. Names are
  live text; do not paint text, numbers or logos into the sprites.
- Provider choice per the owner memoranda: a ChatGPT high-resolution base is
  permitted for machinery, with PixelLab for the pixel pass or as the sole
  generator. Check the allowance and the operation's cost first; record
  prompt, seed or job id, cost, allowance before and after, and the review.

### Prompt prefix (verbatim, first in every request)

ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking
straight down at the object's TOP SURFACE ONLY. Rectangular edges run
horizontally and vertically on the canvas. Show no vertical front or side faces.

### Request 1: S3 process water silo (pilot)

- Canvas: square, transparent, whole canvas is the 3 x 3 footprint; centred pivot.
- Subject: the top of a sealed industrial water silo in a square deck frame:
  a circular pressure dome filling the frame, a bolted rim, a central round
  inspection hatch with a single lift handle, one thin teal service band around
  the dome, a narrow level-gauge strip along one edge, four corner fasteners.
  Worn grey-blue painted steel, restrained contrast, coarse pixel clusters like
  the approved Shipbreaker v2 family. No pipes leaving the frame (conduit and
  links are separate objects and live text).
- Layers (authoring only, flattened for export): frame and rim; dome and hatch;
  gauge strip. Keep the gauge strip separable so a filled state can be composed
  later without regenerating the sprite.
- Exports: 48 x 48 colour, 48 x 48 normal (neutral unless authored), 256 x 256
  portrait. Inspect at native size for a visible side face or a diamond
  silhouette before accepting.

### Request 2: T2 ice thaw unit

- Canvas: square, transparent, 2 x 2 footprint, centred pivot.
- Subject: the top of a boxy enclosed thaw cabinet: a lid with a square
  frosted inspection window over the ice feed (pale blue-white), a row of
  short vent slots, a low hinged tray hatch along one edge, a small teal water
  outlet stub on one side, corner fasteners. Same palette and pixel scale as
  the S3 so the pair reads as one Rivetline line.
- Exports: 32 x 32 colour and normal, 256 x 256 portrait.

### Request 3 (Shipbreaker 0.38.0): ingots and steel remainder

- Phobos' Rivetline aluminium ingot: one-cell inventory sprite, 16 x 16 native,
  64 x 64 master; a single cast bar with a chamfered top face seen from above,
  bright silver-grey, one stamped recess (no legible text).
- Phobos' Rivetline steel ingot: same layout, darker blue-grey with a mill
  scale edge, so the two are distinct at 16 px.
- Steel melt remainder: a small irregular grey-black slag packet, 16 x 16,
  distinct from the existing aluminium melt remainder (`StockMeltRemainder`).
- These follow the existing Shipbreaker stock exports (`StockCoolantCharge`
  pattern) and join `assets/artwork-completion/manifest.json` when selected.

### Review checklist (completed 29 September 2026)

- Overhead only: no visible vertical faces, no isometric diamond. Checked on
  every selected master and at native size.
- Native-size inspection (48, 32, 16 px) before any family expansion. Done
  against the approved Shipbreaker equipment and stock sprites.
- Masters retained, hashes and prompts recorded, rejected attempts archived.
  Done: manifest, request record and archive commit `1df5784`.
- Placeholders removed from `assets/phobos-shipbreaker/placeholders/` and the
  export script retired or repointed in the same change. Done.
