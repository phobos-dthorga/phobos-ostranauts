# N2 pursuit faceplate — 25 September 2026

Original project derivative produced with one built-in ChatGPT `image_gen` call.
The sole reference is our retained N1 instrument faceplate, not extracted game
art. Model revision, seed and monetary usage were not disclosed by the tool.
No API fallback or additional PixelLab generations were used.

The unchanged generated output is retained as
`mods/PhobosAutoNav/images/phobos/autonav/PhobosPursuitInstruments.png`.
Dimensions: 1942 x 809 RGBA, compared with a 600 x 250 reference display.
SHA-256: `9B6C07990EAA2C2AC4800D1F1B73994B0E0BCE018F998824CE017799A5A00FC8`.
This is a development candidate; owner in-game visual review remains pending.

## Exact generation prompt

Use case: precise-object-edit. Asset: new N2 Polaris Pursuit instrument faceplate, derivative of our supplied original N1 panel. Preserve EXACT proportions and normalized locations of ALL recesses, rotary discs, buttons, blank header and corner screws in reference; output at least 1942 x 809 landscape 2.4:1. Change only surface design to distinguish a matching Asterel pursuit instrument: dark desaturated graphite-blue casing, narrow muted amber inset line running inside outer perimeter (outside all controls), two short parallel amber identification strips in the blank upper left header margin before x=9%, restrained amber edges around the two rotary bezels. Retain broad flat surfaces and understated slate edges. Large left black display completely blank, both knobs blank with NO pointers or ticks, all three lower button wells blank. No extra controls, no imagery on screen, no wording, no numbers, no insignia, no crosshairs, no glowing lights, no bevel extravagance, no grunge. Orthographic front face, not product perspective. Suitable production UI texture for live overlaid localized labels and moving controls; right and bottom edges align with reference. Do not reposition or enlarge any controls. Transparent outside rounded corners only.

## Native asset reuse

N2 uses Blue Bottle Games' Ostranauts navigation module art by runtime reference:
`navmod/ItmNavMod01`, `navmod/ItmNavMod01Dmg` and their matching `n` normal maps.
The matching portraits reference the same native colour sprites. Inspected in
Ostranauts 1.0.1.5. Those native PNGs are neither copied into this repository nor
submitted to an image-generation provider. Game art is not covered by our MIT
licence. The N1 item and panel artwork remains unchanged.

Live localized text, controls and placement use the existing Auto Nav panel,
native navigation-module lifecycle and native font. This change does not claim
that the existing custom rotary selector is a cloned vanilla widget.
The fire-permission switch uses Framework's audited isolated
`GUIShip/GUIReactor` donor `pnlPower/chkThrustSafety`. Pursuit buttons share only
the sprite/state artwork of `GUIShip/GUIAirPump/pnlInside/btnDone`. Original
native actions are never bound to pursuit controls. Both art sources belong to
[Blue Bottle Games](https://bluebottlegames.com/), inspected in Ostranauts 1.0.1.5.

Built-in output provenance follows [OpenAI's provider terms](https://openai.com/policies/terms-of-use/);
this link is not a claim of exclusive rights in generated imagery or a licence
to redistribute Blue Bottle Games' assets. No NASA/ESA imagery is used.
