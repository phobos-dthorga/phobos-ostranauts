# Halewright Ward-3 artwork handoff

Prepared 4 October 2026 for Phobos Medical 0.1.0. The initial handoff below is
retained as history; the owner changed the art direction later the same day.

## Current direction: 2075 nanomedical bed concept

The owner requested an original infirmary bed in Blue Bottle Games' Ostranauts
vanilla art style, imagined in 2075 with nanomachines able to repair moderate to
somewhat severe wounds. This is fictional artwork intent, not a clinical finding
or implemented treatment. The existing mod's care rules are unchanged.

The built-in Imagegen tool was available in this session. It generated the
[retained concept](../../assets/phobos-medical/source/ward3-nanomedical-chatgpt.png)
from original text, after local inspection of the vanilla Infirmaway and the
[equipment-art study](ship-equipment-art-study.md). No game textures were sent to
either provider. The [request record](../../assets/phobos-medical/requests.json)
retains exact prompts, provider usage, hashes and decisions; the
[preview](../../assets/phobos-medical/ward3-nanomedical-preview.png) shows the
whole source reduced to 48 x 80 and enlarged four times without smoothing.

The concept has treatment cartridges, parked manipulators and an unobstructed
patient cushion. Its 971 x 1619 untouched source has white margins and a narrower
silhouette than the full 3 x 5 footprint.

**Selected and exported, Medical 0.1.1.** The owner chose this concept the same day.
[register-ward3.py](../../assets/phobos-medical/register-ward3.py) registers it as the
96 x 160 working master: margin removed by edge flood fill, widened about a fifth to 44
native px (the vanilla Infirmaway image is opaque over 44 x 80), 96-colour palette with no
dithering. Trials at 73 to 94 px wide and 32 to 96 colours were compared at native size;
narrower beds looked thin on their tiles, wider ones flattened the cartridges, and smaller
palettes lost the teal. The completion exporter writes the 48 x 80 image and a neutral
normal (manifest key `ward3-medical-bed`); every form uses it, damaged forms with the game's
damage tint. No separate damaged art and no PixelLab layer were made. How it looks lit and
rotated in the game is still for the owner to check. The original hospital-bed base and the completed one-generation
PixelLab fitting were superseded before composition. Their originals are verified
in local archive commit `fd98de450ee10d346a041cfea4a928737b848ffc` on
`codex/rejected-artwork`; that commit has not been pushed.

## Vigil-1 patient monitor (set 3, owner-run ChatGPT request)

Prepared 4 October 2026 for Medical 0.3.0. The monitor stands beside a Ward-3, pairs
with the bed it touches, and shows readings and alerts on its panel. It never heals.
Readings stay live text on the panel and consoles, so the sprite carries no screen,
numbers or lamps (the owner's 29 September ruling on painted instruments).

| | Value |
| --- | --- |
| Footprint | 1 x 1 tile (agent choice, open to revision): a floor cart at the bed's side |
| Native world sprite | 16 x 16 px |
| Registered master | 64 x 64 px (4x; the short side is 16) |
| ChatGPT source | square canvas, 1024 x 1024 or larger |
| Orientation | back edge at the **top**: it takes power from the wall row behind it; its sensor arm reaches toward the patient on the **left** |

Attach the selected Ward-3 concept, `assets/phobos-medical/source/ward3-nanomedical-chatgpt.png`,
as a style reference (original Phobos art, so it may be uploaded), then paste exactly:

> ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight
> down at the object's TOP SURFACE ONLY. Rectangular edges run horizontally and
> vertically on the canvas. Show no vertical front or side faces.
>
> Create an ORIGINAL game equipment sprite concept for Phobos' Halewright Vigil-1: a
> compact bedside patient-monitoring cart for a spacecraft sickbay in 2075, made by the
> same maker as the attached Ward-3 medical bed and matching it exactly in style:
> off-white enamel housing, mid-grey structural trim, dark grey recesses, a few visible
> fasteners and one quiet muted blue-teal accent. Art direction informed by Ostranauts'
> vanilla ship-equipment sprites: coarse deliberate pixel clusters, practical industrial
> modules, muted material colours, sparse mechanical detail, very restrained flat shading.
>
> Square canvas, one square floor tile seen from directly above, the cart filling almost
> the whole square with a small margin. Show only its top: a squared enamel lid with a
> vent grille and a recessed carry handle near the top edge; a short folded articulated
> sensor arm parked along the LEFT edge, ending in a small round sensor head with a
> teal ring; a neat coiled lead stowed in a shallow recess; two small locking caster
> covers at the bottom corners. The display faces sideways toward the bed and is NOT
> visible from above.
>
> PIXEL ART, not a photograph or smooth illustration. Design the image to read as a
> coarse 16 x 16 logical pixel sprite enlarged cleanly, with crisp square pixel
> clusters, stepped outlines, a restricted palette of about 16 colours, no
> anti-aliasing and no gradients, so the shapes stay readable when reduced to 16 x 16.
> Flat diffuse illumination, minimal local shading only.
>
> No screen, numbers, waveforms, text, logos, medical crosses, gauges, status lights,
> readouts, people, cables leaving the tile, floor, backdrop, drop shadow, isometric
> projection, perspective or visible side faces. White background.

Save the untouched output as `assets/phobos-medical/source/vigil1-monitor-chatgpt.png`
and tell the agent the date; the agent records the prompt and hashes in
`assets/phobos-medical/requests.json`, registers it to 64 x 64 the way the Ward-3 was
registered, inspects it at 16 x 16 and exports it.

## Initial handoff (superseded)

No image had been generated when the initial handoff was prepared.
Owner decision at that point: a ChatGPT high-resolution base with PixelLab layers, the
route Agriculture's Firstlight rack and Hearth counter took. The agent cannot run
ChatGPT image generation from this session, so the base is generated by the owner in
ChatGPT from the prompt below and saved into the repository; the agent then adds the
PixelLab layer, composes, exports and records provenance.

Rules that apply: the [asset generation policy](asset-generation-policy.md) (overhead
first, one pilot before a family, no painted live-state instruments), the
[resolution memorandum](artwork-resolution-policy.md) and
[equipment branding](equipment-branding.md) (Halewright: no painted text or logos).

## Geometry

| | Value |
| --- | --- |
| Footprint | 3 tiles wide x 5 tiles deep (as the Infirmaway) |
| Native world sprite | 48 x 80 px, 16 px per tile, opaque to the footprint edge |
| Exported master | 96 x 160 px (2x; the short side 48 is above 32) |
| ChatGPT source | any 3:5 portrait canvas, 1024 x 1707 or larger |
| Orientation | head of the bed at the **top** edge: the back edge, against the wall row where its power point sits |
| Patient point | centre of the mattress; the game draws the patient there, so the mattress centre stays clear |

The world sprite is also the portrait on every form, as for the Rivetline silos.

## Halewright colours

Clean off-white enamel frame, pale mint-grey mattress and sheet, mid-grey rails and
one muted teal accent (head panel and drawer pull), apart from Fennmark's graphite and
orange, Lixivar's sage and slate, Tolvane's teal-and-yellow and Ablatine's red collar.
The teal is quieter and bluer than Tolvane's deep teal.

## Request 1: Ward-3 base (ChatGPT, owner-run)

Paste exactly:

> ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight
> down at the object's TOP SURFACE ONLY. Rectangular edges run horizontally and
> vertically on the canvas. Show no vertical front or side faces.
> A spacecraft medical bed seen from directly above, filling a tall portrait canvas
> with a 3 : 5 width-to-height ratio, the bed's outline touching all four canvas
> edges. Head of the bed at the TOP edge. An off-white enamel frame with rounded
> corners and thin mid-grey side rails along both long edges. A pale mint-grey
> mattress with a folded sheet across the lower third and a flat pillow near the
> head. Across the top edge, a narrow head panel of darker grey with one muted teal
> stripe. The middle of the mattress is plain and uncluttered. Clean, utilitarian,
> worn-in industrial spaceship furniture; flat even lighting; restrained shading.
> No people, no text, no numbers, no logos, no screens, no gauges, no cables beyond
> the frame, no floor, no shadow, no perspective.

Save the untouched output as `assets/phobos-medical/source/ward3-base-chatgpt.png`
and paste the exact prompt used and the date into
`assets/phobos-medical/requests.json` (the agent can do this from your note).

## Request 2: drawer and service fitting (PixelLab, agent-run)

After the base is in the repository: one `create_image_pixflux` call with
`view="high top-down"`, `isometric=false`, the overhead prefix above, for a small
bedside drawer unit and power socket plate seen from above, sized to sit at the head
end (native 16 x 16, 64 x 64 master), off-white with the teal pull. Check the PixelLab
balance and the quoted cost first; stop and report if it needs paid credit.

## Composition and export

`assets/phobos-medical/layers.json` registers the base and the fitting in one output
coordinate system. The agent reduces the base to the 96 x 160 master
(nearest-neighbour after a deliberate palette pass, so it does not look smooth beside
the PixelLab fitting), composes, then exports with the existing completion exporter:

- `mods/PhobosMedical/images/phobos/medical/PhobosMedicalBed.png` and `...Normal.png`
- `...Damaged.png` and `...LooseDamaged.png`: the same composite with scorched and
  torn patches, derived mechanically from the selected master as the Rivetline
  silos were, unless a derived version reads poorly at native size.

Inspect at 48 x 80 and at an integer enlargement before anything else is made.

## Until then

Medical 0.1.0 shows the vanilla Infirmaway's own images on each Ward-3 form,
referenced by name at run time (nothing of the game's is copied into the package),
so the bed is playable now. When the Halewright art is exported, the definitions
switch to `phobos/medical/PhobosMedicalBed` and the completion manifest binds it.
