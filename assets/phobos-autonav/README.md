# Phobos Auto Nav artwork

## Current flight hub (0.12.0)

One reusable tall faceplate serves both N1 and N2. Live Navigation/Pursuit labels
and accents distinguish capabilities. Intended reference: **600 × 960**; runtime
PNG: **1200 × 1920**; retained AI-upscaled master: **1984 × 3172**. Controls, text,
numeric displays and hit areas remain separate. World-item footprints are unchanged.
The corrected live layout registers six safe fields to the actual raster recesses.
Readouts have code-rendered inset bezels and padded content; all labels remain live.
This changes registration/presentation only, retaining the selected image hashes.

- [Exact generation prompt and upscale provenance](hub-prompt.md)
- [Processing manifest and hashes](hub-upscale-provenance.json)
- [Shared live-control registration](hub-layout.json)
- [Offline layout preview](previews/flight-hub.html)
- [Current controls and migration](../../docs/auto-nav-instruments.md)

The two untouched Imagegen sources in `source/PhobosFlightHub-generated.png` and
`source/PhobosFlightHub-resolution-attempt.png` are both 992 × 1586. Explicit
owner-approved local Real-ESRGAN supplies inferred sharper texture, not lossless
reconstruction. The source alpha is resampled separately. The enlarged master is
`source/PhobosFlightHub-ai-master.png`; production is
`mods/PhobosAutoNav/images/phobos/autonav/PhobosFlightHub.png` at repository root.
Earlier approved masters, exports and the 0.11.1 N2 work below are retained as
historical provenance. They are not the current hub's control layout.

Blue Bottle Games' native button, knob, slider and guarded-switch sprites are
shared by runtime reference through Framework's isolated native adapters. No
reactor panel/controller or game-derived bitmap is distributed with this art.
The original Phobos N2 generated artwork is the style reference for the new plate.

## Earlier artwork records

New artwork and visual revisions follow the
[2x/4x resolution policy](../../docs/artwork-resolution-policy.md). The approved
masters and existing export dimensions recorded below remain unchanged.

## Current instrument panel — 0.7.0

The owner requested a substantial redesign with rotary controls on 24 September
2026. `PhobosAutoNavInstruments.png` is the new runtime faceplate: **1942 x 809**,
exceeding 2x the **600 x 250** reference display. Its controls and text are live;
the entire layout scales into the existing native 25%-column / 20%-row bounds.
The pickup sprites and former approved faceplate below remain unchanged.
This candidate needs owner visual and interaction testing in-game.

See the [instrument guide](../../docs/auto-nav-instruments.md),
[exact built-in Imagegen prompt and hash](instruments-prompt.md), and
[interactive browser preview](previews/instruments.html). The preview uses sample
data and a system font; it is not a Unity or gameplay test.

The owner approved the neutral slate-grey faceplate and both item states on
2026-09-23. These are original images generated for this project with ChatGPT's
built-in Imagegen. The game supplied a visual reference, not source pixels for
redistribution. No Auto Navigate or extracted Ostranauts artwork is included.

## Approved masters

| Asset | Unchanged master in this repository | Dimensions | SHA-256 |
| --- | --- | --- | --- |
| Faceplate | `mods/PhobosAutoNav/images/phobos/autonav/PhobosAutoNavPanel.png` | 1774 x 887 | `A5D5A01FA3460A255D7E7A8830495141E8EBC02168683749D23B7E882252185E` |
| Intact item | `assets/phobos-autonav/source/PhobosAutoNavModule-approved.png` | 1254 x 1254 RGBA | `3608E4815DDB7FD6637BEAF7357812802FBF6A415BA13110227DF4481ADE2478` |
| Damaged item | `assets/phobos-autonav/source/PhobosAutoNavModuleDmg-approved.png` | 1254 x 1254 RGBA | `A1800D144178D76F6998903991B0B42ED399F99CE9CE6FE131FEDF554074B6E9` |

The faceplate is the exact attachment approved after the owner clarified crossed
replies. It is intentionally restrained, with slate-grey paint, shallow edges,
corner fasteners and blank dark controls. Do not substitute the earlier weathered
concept or flatten it into a different design. Its text and button behaviour are
live UI; no labels are painted into the texture.

The item states share blue-grey casing, an ochre accent and brass contacts.
Damage adds the approved crack, dead display, scorching and bent contacts.
The two `*-v1.png` source files are earlier drafts and are not packaged.

## Game exports

Run `scripts/export-autonav-art.ps1` from PowerShell 7 on Windows. The build also
runs it. It performs only mechanical cropping/downsampling of the approved item
masters and creates neutral shader data; it does not repaint their design.

- Shared crop: x=367, y=404, width=521, height=480. This contains both states'
  visible pixels (alpha above 16), including damaged contacts, and keeps their
  components registered. Source hashes prevent silent use of an obsolete crop.
- World/inventory images: 16 x 16 RGBA, one-pixel nominal margin, aspect preserved.
  This matters because the inspected game item path derives physical rendering
  scale from texture dimensions at 16 pixels per tile. Shipping the large master
  as `strImg` would give the module the wrong visual size.
- Inspection portraits: 256 x 256 RGBA, using the same crop and proportional
  margin. These retain the finer detail that cannot survive a tiny ground icon.
- Normal map: one shared 16 x 16 flat tangent-space image, RGB 128,128,255.
  It is generated technical data, with no borrowed normal-map artwork.
- Faceplate: unchanged 1774 x 887 texture. Auto Nav 0.5.0 uses sliced rendering
  in a native 25%-wide, 20%-high panel, following the owner's screenshot correction.
  The centre widens; corner/screw regions keep their height-based uniform scale.
  Labels and hit areas share the visible panel. No bitmap pixels are changed.

Exports live under `mods/PhobosAutoNav/images/phobos/autonav/`. Native module
definitions point to the intact/damaged images and portraits. The native
motherboard bases still provide game behaviour and repair transitions.

The export images were visually inspected and the package verifies their paths,
dimensions, transparency and faceplate hash. This is asset/build verification,
not an in-game lighting, layout or interaction test; those checks remain with
the owner in the separate test save.

## Provenance and terms

[Recorded generation prompts](prompts.md) include the superseded faceplate and
its neutral revision. The revision used the original generated panel as an edit
target and the owner's game screenshot as a style reference. The damaged item
used the generated intact item as its edit target. The approved attachments are
the authoritative masters, even where attachment encoding differs from the
generator's initial output file. The user's screenshot is not stored here.

These Phobos-created assets and export code follow the repository's MIT scope;
no exclusivity in generated imagery or rights over the game's art are claimed.
The separate uncertainty about Auto Navigate-derived code is documented in the
repository/package's `THIRD_PARTY_NOTICES.md` and is not an artwork dependency.

## N2 pursuit artwork — 0.11.1 (25 September 2026)

The initial 0.11.0 N2 reused N1 art. Following the owner's request, N2 now uses
its own 1942 x 809 graphite-blue/amber instrument faceplate, retaining the same
600 x 250 reference layout and native placement bounds (over 2x both axes).
The untouched built-in generated master is also the runtime texture:
`mods/PhobosAutoNav/images/phobos/autonav/PhobosPursuitInstruments.png`.
SHA-256: `9B6C07990EAA2C2AC4800D1F1B73994B0E0BCE018F998824CE017799A5A00FC8`.
See [exact prompt and provenance](pursuit-prompt.md).

Blue Bottle Games' native `navmod/ItmNavMod01` / `ItmNavMod01Dmg` sprites and
matching normals are runtime references for the N2 item/portrait. They remain
16 x 16 native assets with native detail, not generated or enlarged production
masters. No game pixels are redistributed or uploaded to an image provider.
The panel uses Framework's isolated reactor safety toggle and references the
native air-pump Done button's artwork, with fresh Phobos actions and live labels.
No native reactor or air-pump controller is instantiated. N1 masters remain
unchanged. The new panel and controls require owner in-game evaluation.
