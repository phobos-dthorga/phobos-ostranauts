# Oxsmith artwork handoff: EC-4 and CR-4

Prepared 5 October 2026, ahead of any code, because the owner's ChatGPT plan runs for
only a few more days. Both machines belong to the regolith programme the owner approved
the same day: oxygen from rock by two routes. **No machine exists yet**; this record
holds the requests so the high-resolution masters can be made in time. Nothing here has
been generated, and no placeholder stands in for reviewed art.

## What the owner runs, and what comes back

1. Open ChatGPT and paste **Request 1** below as one message. Attaching a style
   reference is optional; if you attach one, use our own
   `mods/PhobosManufacturing/images/phobos/manufacturing/PhobosVolatilesRefinery.png`
   (original Phobos art), never a game texture.
2. Save the image it returns, untouched, as
   `assets/phobos-manufacturing/source/oxsmith-ec4-chatgpt.png`.
3. Do the same with **Request 2**, saved as
   `assets/phobos-manufacturing/source/oxsmith-cr4-chatgpt.png`.
4. If a result shows side or front faces, a slanted (diamond) layout, text, numbers,
   screens or gauges, ask for it again with the same request; keep the rejected file
   beside the others with `-rejected-1` in its name so it can be archived with its reason.
5. Tell the agent the files are in place. Registration (crop, palette, 64 x 64 export,
   normal map, previews, hashes and the request record) is then done by script.

Costs are the owner's ChatGPT plan only. No PixelLab generation is requested here.

## Specifications

| | Oxsmith EC-4 Electrolysis Cell | Oxsmith CR-4 Carbothermal Reactor |
| --- | --- | --- |
| Footprint | 4 x 4 tiles | 4 x 4 tiles |
| Native world sprite | 64 x 64 px (16 px a tile) | 64 x 64 px |
| Registered master | 128 x 128 px or larger (2x; the short side is over 32) | the same |
| ChatGPT source | square, 1024 x 1024 or larger | the same |
| Coverage | opaque, the whole footprint edge to edge | the same |
| Orientation | back edge at the **top** (power comes from the wall row behind it); crew stand at the **bottom** edge | the same |

Rules that apply: the [asset generation policy](asset-generation-policy.md) (overhead
first, one pilot inspected before a family grows, prompts and rejected attempts kept),
the [resolution memorandum](artwork-resolution-policy.md), and the owner's ruling of
29 September 2026 that world sprites carry no painted live-state instruments. Pipes,
conduit and belts are separate objects, so neither sprite shows connections running off
its edge.

## Oxsmith's look

One family for both machines, apart from every other Phobos maker: **deep oxide-red
enamel frames** (the red of iron oxide), **pale ceramic refractory plates**, **blackened
steel** vessels and lids, and **one ice-blue accent** kept for oxygen fittings. Fennmark is
graphite and burnt orange, Lixivar sage and slate, Tolvane teal and yellow, Alembrine
copper and brass; Oxsmith must not be mistaken for any of them.

## Request 1: EC-4 Electrolysis Cell (pilot)

> ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight
> down at the object's TOP SURFACE ONLY. Rectangular edges run horizontally and
> vertically on the canvas. Show no vertical front or side faces.
>
> Create an ORIGINAL game equipment sprite concept for the Oxsmith EC-4: a molten-rock
> electrolysis cell for a working spacecraft, which melts crushed asteroid rock and
> splits it with electricity into oxygen and metal. Art direction informed by the
> ship-equipment sprites of the game Ostranauts: coarse deliberate pixel clusters,
> practical industrial modules, muted material colours, sparse mechanical detail, very
> restrained flat shading, stepped pixel edges, no smooth gradients.
>
> Square canvas. The machine is a square floor unit seen from directly above and fills
> the whole canvas edge to edge, with no background and no margin. Its top is divided
> into clear blocks: a deep oxide-red enamel frame around the edge with four heavy
> corner clamps; pale ceramic refractory plates as the deck; a large round blackened
> steel cell lid in the upper middle, with a ring of bolts and three thick anode rods
> standing through it, joined by two broad dark busbars that run to the TOP edge; a
> small ice-blue oxygen offtake dome to the upper right of the lid; a square charge
> hatch with a red handle bar at the lower left; a shallow dark tapping trough with
> three ingot moulds along the lower right; a narrow cooling grille along the left side.
> A faint warm glow shows only through one small sight port in the cell lid.
>
> No text, letters, numbers, logos, screens, dials, gauges or indicator lamps anywhere.
> No pipes or cables leaving the edges of the machine. No shadow outside the machine.
> No people. Not isometric, not three-quarter view, not a product photograph.

## Request 2: CR-4 Carbothermal Reactor

> ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight
> down at the object's TOP SURFACE ONLY. Rectangular edges run horizontally and
> vertically on the canvas. Show no vertical front or side faces.
>
> Create an ORIGINAL game equipment sprite concept for the Oxsmith CR-4: a carbothermal
> reduction reactor for a working spacecraft, which heats crushed asteroid rock in
> methane to pull the oxygen out of it as gas. It is made by the same maker as the
> Oxsmith EC-4 electrolysis cell and matches it in style and colour: deep oxide-red
> enamel frame, pale ceramic refractory deck plates, blackened steel vessels, and one
> ice-blue accent. Art direction informed by the ship-equipment sprites of the game
> Ostranauts: coarse deliberate pixel clusters, practical industrial modules, muted
> material colours, sparse mechanical detail, very restrained flat shading, stepped
> pixel edges, no smooth gradients.
>
> Square canvas. The machine is a square floor unit seen from directly above and fills
> the whole canvas edge to edge, with no background and no margin. Its top is divided
> into clear blocks: a deep oxide-red enamel frame around the edge with four heavy
> corner clamps; pale ceramic refractory deck plates; a tall reactor vessel seen from
> above as a large round blackened steel flanged lid at the upper left, with a ring of
> bolts and a ring of six small gas injector nozzles around it; two round gas domes
> side by side at the upper right, one dark grey and one pale, joined to the vessel by
> a short thick manifold; a square charge hatch with a red handle bar at the lower
> left; a shallow dark slag trough with a grille along the lower right; one small
> ice-blue valve wheel beside the gas domes. A faint warm glow shows only through one
> small sight port in the vessel lid.
>
> No text, letters, numbers, logos, screens, dials, gauges or indicator lamps anywhere.
> No pipes or cables leaving the edges of the machine. No shadow outside the machine.
> No people. Not isometric, not three-quarter view, not a product photograph.

## What does not need ChatGPT

- **Fennmark carbon monoxide stores** (three sizes): derived by a recorded recolour of the
  existing Fennmark dome stores, as the steel ingot was derived from the aluminium ingot.
  No generation.
- **Regolith floor**: the Phobos floor twin uses the game's own Polished Regolith Floor
  picture at runtime. Nothing is copied or drawn.
- **Small items** (baked regolith, regolith paver, ferrosilicon, and the damaged and loose
  machine forms): PixelLab or derivation from the selected masters, in their own release
  sets, under the usual one-pilot rule.

## Review checklist, for when the files arrive

- Overhead only: no side or front faces, no diamond projection.
- No text, numbers, screens, gauges or lamps.
- Reads at 64 x 64 beside the V4, LC-3 and SA-3 without being mistaken for them.
- The two machines are clearly one family and clearly two different machines.
- Back edge at the top, charge hatch and trough toward the bottom edge.
- Untouched sources kept; prompts, hashes and any rejected attempts recorded.
