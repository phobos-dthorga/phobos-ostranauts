# Underfloor material transport concept prompts

24 September 2026. ChatGPT built-in Imagegen, one call per asset.
The sender is a new image from a written brief informed by the approved Phobos
Shipbreaker palette and pixel scale. No game pixels or screenshots were supplied.
The receiver edits that original sender, retaining the housing and framing.

## Sending port v1

Output: `source/PhobosTransportSending-concept-v1.png`

Image references: none.

```text
Use case: stylized-concept.
Asset type: original top-down pixel-art machinery sprite concept for the Phobos Ostranauts mod, a compact UNDERFLOOR CONVEYOR SENDING PORT.
Primary request: one square industrial loading terminal, sized to occupy TWO BY TWO floor tiles and read at 32 x 32 actual game pixels. Cargo placed on its small open tray moves along a very short captive conveyor and disappears underneath a covered opening at the back, implying enclosed transport below the deck.
Subject layout: directly overhead orthographic, north at top. Rectangular muted industrial blue perimeter frame with four small mounting tabs; a broad low cream-coloured service hood across the top third; beneath its bottom lip, a deep charcoal rectangular transport mouth; a short dark grey ribbed belt down the middle extending toward the bottom/front of the device; two simple grey guide rails visibly holding cargo captive in microgravity; a low open loading tray at the bottom. The belt must visibly terminate under the hood, with two or three chunky transverse ribs, not a long external conveyor. A single small ochre triangular arrow on the tray points toward the top mouth. One small dark service panel on the right rim and one unconnected electrical socket, no connecting wire. The tray is empty. Retain enough dark space to read the mechanism at small size.
Style: match coarse low-resolution industrial pixel-art game equipment. Bold simple shapes, logical square pixel clusters on a 32 x 32 grid, stepped outlines, only 2–3 shades per material, modest edge relief. Muted blue steel, off-white/cream cover, charcoal belt, dull grey rails, very restrained ochre safety accents. No smooth gradients, polished metal, tiny screws, noisy grime, painterly rendering or high-definition detailing.
Composition: one complete isolated square object, centered, narrow equal transparent margins equivalent to one logical pixel, same intended registration for a later receiving variant. Strict overhead view, no perspective/isometric view and no visible tall side walls. Genuine transparent background with opaque object surfaces. No surrounding floor, no cutaway basement, no additional machinery, no cargo, no text, letters, numbers, labels, watermarks, UI, conduit loops, strong lighting, cast shadow or glow.
```

## Receiving port v1

Output: `source/PhobosTransportReceiving-concept-v1.png`

Edit target: `source/PhobosTransportSending-concept-v1.png`

```text
Use case: precise-object-edit.
Asset type: matching top-down pixel-art UNDERFLOOR CONVEYOR RECEIVING PORT, the companion to the supplied sending port.
Image 1 is the edit target. Preserve its exact square canvas registration, outer blue frame, four mounting tabs, cream rear hood, recessed black transport mouth, grey captive guides, right-side service panel and unplugged electrical socket. Same top-down angle, palette and scale.
Change only the front half of the conveyor: materials now emerge FROM beneath the rear cream hood and arrive in a clearly wider shallow receiving tray at the bottom/front. Keep a short central ribbed belt coming from the rear mouth. Make the near tray a dark empty collection pocket, slightly wider than the belt but INSIDE the existing outer bounds, with two small ochre retaining stops on its front corners. Reverse the single ochre triangular direction marker so it points toward the bottom/front, away from the hood. Keep the tray empty and visually accessible from the front; no loose crate or added cargo.
Coarse 32 x 32 logical pixel scale, hard square clusters and stepped edges, simple two-to-three shades per material. Simplify rather than add tiny details. No smooth gradients, glow, painted scenery, realistic reflections, noisy texture or blurred edges. One complete isolated square object on a genuinely transparent background, opaque surfaces. No floor, external conveyor length, pipework, conduit, text, labels, watermark or new equipment. Both ports must look like versions of the same piece of hardware.
```
