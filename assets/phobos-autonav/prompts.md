# Phobos Auto Nav generation record

Generated with ChatGPT's built-in Imagegen on 2026-09-23. The exact model revision
and seed were not exposed by the tool. These are the recorded prompts, not future
instructions. The approved masters and their hashes are listed in README.md.

## Initial faceplate concept (superseded)

```text
Use case: stylized-concept
Asset type: production texture for the faceplate of a small spaceship navigation instrument, a 2:1 wide rectangular user-interface panel.
Create original Phobos spacecraft equipment artwork visually compatible with Ostranauts' grimy, utilitarian, analog-industrial ship instruments. Orthographic flat front view, absolutely no perspective. Entire rectangular faceplate fills the canvas edge to edge, no scene or surrounding background.
Design language: weathered charcoal gunmetal chassis; worn, desaturated blue-green paint; sparse muted ochre maker stripe; exposed recessed screws; slightly uneven stamped metal and fine grime in seams. Credible serviceable equipment, restrained wear, readable at small scale. A modest unique twin-notch motif worked into the upper left metal edge; no existing logos.
Practical layout: a shallow plain metal header strip occupying the top 16% with space for live text; a single nearly black recessed instrument glass window spanning x=5%..95%, y=24%..66%, entirely blank inside; two blank rectangular physical button faces at the bottom, x=5%..53% and x=60%..95%, y=76%..94%. Left button face is faded desaturated blue-green; right is muted rust-ochre. Four small corner fasteners, inset far enough not to touch controls. No artwork details inside the blank screen. The live labels and status will be drawn by the game.
Textures should have hand-painted game-art character rather than crisp modern vector UI or glossy product photography. Modest fine grain, subtle raised-edge highlights, no dramatic lighting. Design must work against dark grey ship console backgrounds.
Text: none. No lettering, numbers, logos, icons, readouts or simulated navigation traces.
Avoid: neon, holograms, futuristic glowing trim, chrome, glossy black, cyberpunk gradients, hazard-stripe clutter, detached parts, floating objects, watermark.
Output a single complete faceplate texture, landscape 2:1 aspect ratio, ideally 1536 x 768 pixels.
```

## Intact item

```text
Use case: stylized-concept
Asset type: a single tiny pixel-art inventory sprite for a Phobos automatic navigation module in Ostranauts.
Create original artwork matching the game's top-down, low-resolution industrial item sprites. Straight-on orthographic top view, no perspective, no scene. Genuinely transparent background, alpha zero outside the object.
The item is a compact rectangular electronics cassette: charcoal outline, muted dusty blue-grey metal casing, very dark inset central navigation display with two simple dull green instrument bars, a small ochre strip, two dark notches at the top edge, and a few exposed brass contact pins along the bottom. Keep the original Phobos identity seen in the worn blue-grey and ochre navigation faceplate. Worn but serviceable. Distinct silhouette and bold colour blocks.
CRITICAL pixel-art scale: design on an effective 16 by 16 pixel grid, enlarged by nearest-neighbour. Each design pixel is a large uniform square. No subpixel detail, antialiasing, blur, texture grain or fine scratches. Use at most 18 muted colours. Entire object within a one-grid-pixel transparent margin; roughly square overall occupied footprint. Light from upper left with very restrained highlights. Only large components which survive a 16x16 game sprite. Do not draw text, tiny symbols, screws smaller than one design pixel, floating detached pieces or drop shadows.
Output one centered sprite on a transparent square canvas, ideally 1024x1024 with a strict 16x16 effective pixel grid. No border or checkerboard painted into the image, no sample sheet, no title, no watermark.
```

## Damaged item edit

```text
Use case: precise-object-edit
Input image 1: edit target, the new original Phobos Auto Nav module sprite.
Create the damaged version of this same module. Preserve the exact camera, canvas dimensions, transparent background, object placement, overall silhouette, two top notches, connector-pin layout and restrained pixel-art style. The damaged item must remain instantly recognizable as the same design.
Change only these damage details: darken both green display bars until the display is dead; give the glass a single coarse diagonal crack; add a modest blackened patch at the upper right, revealing a little brown circuit-board substrate; bend or darken two existing bottom contact pins. Keep the blue-grey and ochre identity readable and damage legible at a final 16x16 game size.
No fire, sparks, glow, smoke, debris, detached pieces, added background, text or labels. Keep genuine transparency. Do not turn the silhouette into a different item. This is one repairable broken navigation cassette.
```

## Approved neutral faceplate revision

```text
Use case: precise-object-edit
Input image 1: edit target, our generated Phobos instrument faceplate. Input image 2: style reference only, an Ostranauts navigation-console screenshot supplied by the user.
Redesign ONLY the finish and visual treatment of image 1 so that the resulting faceplate belongs seamlessly beside the neutral flat panels in image 2. The user's correction is decisive: neutral, flat, monotone. Do not preserve the weathering or coloured styling of image 1.
Keep a 2:1 landscape faceplate with a blank header area, one blank dark rectangular display occupying the middle and two blank button faces along the bottom. Flat orthographic front view, full panel fills the image.
Surface colour: essentially uniform muted slate blue-grey, RGB 64,79,98 / #404F62, matched to the large plain panel areas in reference image 2. Remove every scratch, stain, rust patch, grain, chipped edge, decorative coloured stripe, ochre accent and blue-green accent. No metallic reflections, glossy glare or dramatic lighting. Very restrained thin grey edge outlines only, like the game screenshot; shallow black corner fasteners, small and unobtrusive.
The display is plain near-black with a thin dark grey inset border; no glass sheen or reflections. Both button faces are neutral dark charcoal, with thin muted grey bevel edges consistent with the screenshot's rectangular controls. Keep large blank surfaces for the game to render live text. No twin-notch decorative signature; standard restrained mechanical mounting corners.
No words, numbers, logos, symbols, status lights, coloured controls or simulated readout. No surrounding screenshot, existing game text, player data, map, background scene, watermark or branding copied from the screenshot.
The result should read like flat 2D game interface art, not a photograph of hardware. Preserve generous empty display space and crisp, low-contrast simple surfaces.
```
