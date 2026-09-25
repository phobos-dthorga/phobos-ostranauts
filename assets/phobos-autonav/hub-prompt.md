Polaris shared flight hub faceplate — final generation prompt
Provider: built-in ChatGPT image_gen. Reference: approved original PhobosPursuitInstruments.png (0.11.1), style reference only.

Create one production game UI faceplate bitmap, a tall portrait instrument panel in the same convincing utilitarian Ostranauts-like graphite/slate painted metal art style as the attached approved original Phobos N2 panel. Reference image is ONLY a material, bevel, screw and restrained warm metal trim reference. Redesign its proportions completely as a tall 5:8 portrait panel, minimum 1200 pixels wide by 1920 pixels high (prefer exactly 1200 x 1920 or a larger exact 5:8 canvas). Straight-on orthographic view, no perspective, no scene, no surrounding desk, no shadow outside the object. Panel fills the canvas with only genuinely transparent rounded outside corners. Retain subtle slate-blue/graphite finish, worn edge highlights, dark bolted corners and a very thin muted brass perimeter accent. Restrained, practical, factory-made hardware.

This is the EMPTY REUSABLE FACEPLATE ONLY. No writing, no letters, no numbers, no logos, no lights, no lamps, no knobs, no button images, no switches, no toggle wells, no grilles, no fake readings, no UI mockup. Every control and all text will be real game widgets added above the image. Large quiet uninterrupted surfaces are essential. Do not reproduce the two large circular control wells or three button recesses in the reference.

Registration in a 600 x 960 reference design (scale uniformly to output): hardware frame and screws must remain within the outer 14-unit margin. Keep all of x=20..580 clear. A subtly recessed dark graphite display field may occupy x=18..582, y=48..302 for the live header. At y=306 and y=372, use a delicate shallow seam to suggest the live tab strip between them. The main universal control field x=18..582,y=380..832 must remain flat slate metal with no fixed divisions; every page uses it differently. A delicate horizontal seam at y=838 separates the bottom action strip. Keep y=846..906 unobstructed for three live buttons and y=912..944 unobstructed for the live coasting legend. At the top y=16..46 keep a quiet slate surface for live branding. No thick borders encroaching on these clear areas. Corner screws small enough to remain outside all live content. Low-contrast fine texture; no overly busy scratches or glowing futuristic ornament.

The output is a single finished reusable bitmap asset with transparency only outside the metal silhouette, suitable for downscaling to 300 x 480 while retaining readable live UI over it. Preserve the material character of the approved reference, but do not carry over its wide-format layout.


## Resolution retry and selected export

The first generation returned 992 × 1586. A second built-in imagegen edit requested
an exact 2400 × 3840 enlargement preserving the complete silhouette and registered
fields; it again returned 992 × 1586. Both exact returned images are retained as
`source/PhobosFlightHub-generated.png` and `source/PhobosFlightHub-resolution-attempt.png`.
The first is the selected source. Generated pixels do not imply the requested
resolution was delivered. The retry was not selected for production.

The owner explicitly approved **local Real-ESRGAN** on 25 September 2026. The
reusable `scripts/upscale-ui-art.py` invokes the official ncnn Vulkan executable
with the `realesrgan-x4plus` model, 4× RGB inference, tile 256 and jobs 1:2:2.
It downsamples to a retained 1984 × 3172 master, resamples only the original alpha
with Lanczos, then uniformly fits to 1200 × 1920 with a centred subpixel crop
(less than 0.1%). There is no nonuniform stretch. Live controls were reflowed after
inspection to clear the resulting fasteners and bottom wells.

[Xintao Wang, Liangbin Xie, Chao Dong and Ying Shan's Real-ESRGAN](https://github.com/xinntao/Real-ESRGAN)
provides the restoration model; see their [2021 paper](https://arxiv.org/abs/2107.10833).
The project credits Tencent ARC Lab and Shenzhen Institutes of Advanced Technology,
Chinese Academy of Sciences. Its output infers detail; this use does not claim
lossless recovery or endorsement. Executable: official
[ncnn Vulkan v0.2.0 release](https://github.com/xinntao/Real-ESRGAN-ncnn-vulkan/releases/tag/v0.2.0).
Model files: official
[20220424 Windows bundle](https://github.com/xinntao/Real-ESRGAN/releases/tag/v0.2.5.0).
Tool/model binaries and licences remain local and are not redistributed. The
[manifest](hub-upscale-provenance.json) records exact source, binary, model and
selected output hashes. Ordinary builds verify exports and never run or download AI.

At equal production size, visual inspection found crisper fastener rims and trim
than Lanczos-only enlargement, with smoother surface grain. The selected derivative
preserves the visible panel geometry; its texture is AI inferred. Original and
enhanced images remain independently identifiable. Offline checks cover the four
routine pages at all three requested sizes. Native Unity appearance awaits owner
gameplay review. The previous approved N1/N2 masters remain untouched.
