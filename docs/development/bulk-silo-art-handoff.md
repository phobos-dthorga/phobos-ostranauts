# Bulk silo, thaw unit and ingot artwork handoff

Prepared 29 September 2026 for Shipbreaker 0.37.0 (S3 silo, T2 thaw unit) and
the planned 0.38.0 ingots. No paid generation was run in this round (owner
direction: create the handoff; PixelLab's MCP follows). Until the requests
below are produced and reviewed, the runtime uses **procedural placeholder
sprites** from `scripts/export-silo-placeholder-art.py`:

| Runtime image | Native size | Placeholder master (4x) |
| --- | --- | --- |
| `mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosProcessSilo.png` (+ `Normal`, `Portrait`) | 48 x 48 (3 tiles) | `assets/phobos-shipbreaker/placeholders/source/PhobosProcessSilo-placeholder-4x.png` |
| `mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosIceThaw.png` (+ `Normal`, `Portrait`) | 32 x 32 (2 tiles) | `assets/phobos-shipbreaker/placeholders/source/PhobosIceThaw-placeholder-4x.png` |

The placeholders are flat overhead drawings (rim, lid, hatch, gauge; flat
normal maps), deterministic and re-exportable with `--check`. They are not
reviewed art and are **not** listed as selected in
`assets/artwork-completion/manifest.json`. Both loose and damaged forms reuse
the one sprite with the game's damage tint, as the R4 does. Replace them by
producing the requests below, exporting through the same script pattern
(master kept, nearest-neighbour native export, neutral or authored normal map,
256 px portrait), recording provenance in the manifest, and deleting the
placeholder masters in the same change.

## Rules that apply

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

## Prompt prefix (verbatim, first in every request)

ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking
straight down at the object's TOP SURFACE ONLY. Rectangular edges run
horizontally and vertically on the canvas. Show no vertical front or side faces.

## Request 1: S3 process water silo (pilot)

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

## Request 2: T2 ice thaw unit

- Canvas: square, transparent, 2 x 2 footprint, centred pivot.
- Subject: the top of a boxy enclosed thaw cabinet: a lid with a square
  frosted inspection window over the ice feed (pale blue-white), a row of
  short vent slots, a low hinged tray hatch along one edge, a small teal water
  outlet stub on one side, corner fasteners. Same palette and pixel scale as
  the S3 so the pair reads as one Rivetline line.
- Exports: 32 x 32 colour and normal, 256 x 256 portrait.

## Request 3 (round 3, pending): ingots and steel remainder

- Phobos' Rivetline aluminium ingot: one-cell inventory sprite, 16 x 16 native,
  64 x 64 master; a single cast bar with a chamfered top face seen from above,
  bright silver-grey, one stamped recess (no legible text).
- Phobos' Rivetline steel ingot: same layout, darker blue-grey with a mill
  scale edge, so the two are distinct at 16 px.
- Steel melt remainder: a small irregular grey-black slag packet, 16 x 16,
  distinct from the existing aluminium melt remainder (`StockMeltRemainder`).
- These follow the existing Shipbreaker stock exports (`StockCoolantCharge`
  pattern) and join `assets/artwork-completion/manifest.json` when selected.

## Review checklist

- Overhead only: no visible vertical faces, no isometric diamond.
- Native-size inspection (48, 32, 16 px) before any family expansion.
- Masters retained, hashes and prompts recorded, rejected attempts archived.
- Placeholders removed from `assets/phobos-shipbreaker/placeholders/` and the
  export script retired or repointed in the same change.
