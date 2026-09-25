# Original furnace candidate prompts — 25 September 2026

**0.14.0 addition:** one small original coupling was generated through PixelLab
`create_image_pixflux`, costing one included generation. Its full request, seed,
provider job/asset ID and hash are in [coupling-provenance.json](coupling-provenance.json).
It is a separate insert; none of the four masters below was regenerated.

Generated with the **built-in image_gen tool**, one call per asset: three calls
for the initial furnace set, then one F6-P call for 0.13.0, with no iterative
generations. No game images or extracted assets were
passed as references. These are original Phobos candidate masters; owner approval
and appearance in the running game remain pending. `exports.json` records hashes,
the shared crop/registration per form and native output sizes.

## PhobosFurnace-v1.png

Use case: stylized-concept. Asset type: original overhead pixel-art sprite for an Ostranauts-style industrial spacecraft game. Create ONE intact Phobos Rivetline F6 electric furnace, no words or logo. Sealed batch induction furnace with a strong round dark central pressure-latched lid/crucible vessel inside a square utilitarian chassis, a small cream electrical cabinet and blue-grey cooling fittings, a short captive front loading cassette, four compact mounting feet. Distinct power and cooling connection sockets, but NO attached pipes, conduit loop, reactor coupler, radiator, floor, people or background scene. True orthographic top-down view, no isometric perspective. Muted worn slate steel and warm grey paint, small restrained ochre safety accents, broad readable shapes and coarse deliberate pixel clusters, stepped edges, very limited shading, no bloom, no dramatic shadows, no tiny text or photoreal detail. Design must remain recognizable at a 96 by 96 pixel world export for a 6 by 6 tile footprint. Produce a large square 1024 by 1024 master with generous transparent margins, the entire machine centered and visible, actual transparent alpha background. Preserve crisp pixel-art language, not smooth 3D rendering. This is one sprite, not a contact sheet.

Tool output: 1254 x 1254 RGBA; retained unchanged.

## PhobosFurnaceRadiator-v1.png

Use case: stylized-concept. Asset type: one original top-down pixel-art equipment sprite for an Ostranauts-style spacecraft game. Subject: Phobos Rivetline F6-R fixed exterior heat-rejection radiator. A wide rectangular fin pack, aspect ratio 3:2, with six broad dark graphite fin banks, restrained grey metal perimeter and four compact mounting brackets, two blue-grey sealed coolant fittings along the lower edge. Fixed passive radiator, no deployment hinges or wings. Utilitarian worn steel, muted grey, dark graphite, small cream fittings and sparse ochre caution accents. True orthographic overhead view, no perspective, no floor, no ship hull, no piping network, no furnace, no text or logo, no glow or space scene. Broad simple readable shapes, coarse deliberate pixel clusters and stepped edges; this must read clearly at 96 by 64 native pixels for a 6 by 4 tile footprint. Minimal baked shading, no long shadows. Entire object visible with generous transparent margins, actual transparent alpha background. Large landscape 1536 by 1024 master. One sprite, not a sheet of variants.

Tool output: 1536 x 1024 RGBA; retained unchanged. The four visible brackets are
artwork; the six tile-wide hull support requirement is a native placement rule.

## PhobosFurnaceHousing-v1.png

Use case: stylized-concept. Create ONE original transparent-background overhead pixel-art inventory sprite: a plain rough-cast aluminium industrial machinery housing for a utilitarian spacecraft game. A squat rectangular hollow housing with two thick mounting flanges, four bolt bosses and a broad central opening, dull light grey aluminium, simple coarse pixel clusters, stepped edges, only a few dark cavities. Top-down orthographic view, no perspective. It must remain readable as a 32 by 32 pixel icon, so very simple silhouette and sparse details. No machine, cables, text, logo, floor, tools, grid, glow or background. Whole object centered with generous transparent margins. Large square master, actual transparent alpha. This is a single material item, not a contact sheet. The same casting will be shown for rough and finished stock, with live item names identifying the processing state.

Tool output: 1254 x 1254 RGBA; retained unchanged.
## F6-P thermal exhaust port — 25 September 2026

Original intact master: `source/PhobosFurnaceThermalPort-v1.png` (1254 × 1254).
Generated with one built-in image_gen call; no external API or regeneration rounds.
The export manifest retains its hash and common colour/normal/portrait crop.
Owner visual approval and in-game evaluation remain pending.

```text
Use case: stylized-concept. Create one original pixel-art game equipment sprite for Phobos' Rivetline F6-P Thermal Exhaust Port, a one-tile sealed through-deck thermal fitting connected to an underside radiator assembly. This is the visible small mounting head only, not the large underside radiator. Strict orthographic top-down view, square composition, isolated on a genuinely transparent background. A compact square bolted charcoal-grey flange, four broad readable corner bolts, pale ceramic insulation around a dark sealed circular centre, and one short blue-grey capped service coupling at the right edge. A restrained ochre mark is acceptable. Match utilitarian space-salvage machinery with strong coarse pixel clusters, crisp stepped edges, limited shades, no fine grime. Design must remain readable as only 16 by 16 world pixels: use about 16 by 16 logical pixel cells enlarged crisply into a high-resolution original master, no smoothing. Leave one logical pixel of transparent margin around the entire silhouette, including the coupling. No text, letters, numbers, logos, labels, UI, background floor, flames, plume, glowing exhaust, open air hole, perspective, cast shadow, or surrounding conduits. It is a sealed solid fitting, not an air vent or fan. One intact sprite only, no contact sheet or variants.
```
