# Phobos Auto Nav artwork

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
- Faceplate: unchanged 1774 x 887 texture, fitted at its original 2:1 ratio inside
  the native draggable panel. Labels and hit areas follow that fitted texture.

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
