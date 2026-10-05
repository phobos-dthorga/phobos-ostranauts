# Dedicated item and equipment artwork

Prepared 27 September 2026 for Agriculture 0.15.0, Shipbreaker 0.28.0 and
Framework 0.28.0. This is original AI-assisted artwork with offline validation;
owner gameplay and visual approval remain pending. No game installation or
Steam publication was performed.

## Coverage

Current chemical-machinery update, 5 October 2026: after approving the Oxsmith
designs, the owner requested the same finish for V4, X2, K2, AX-2, LC-3, SA-3 and
Copperhead-3. Their selected OpenAI built-in Imagegen originals and larger working
masters retain each maker's colours, existing footprints and game image identities.
That reactor pass left silos/tanks and support equipment unchanged. See the
[production record](../../assets/phobos-manufacturing/README.md),
[exact prompts](../../assets/phobos-manufacturing/chemical-reactor-requests.json)
and [native-size review](../../assets/phobos-manufacturing/previews/chemical-reactor-restyle-review.png).
The older generation details below retain their original dates and providers.
The subsequent [support-machine update](../../assets/phobos-manufacturing/manufacturing-restyle-requests.json)
selects new L2, A2 and Corker-2 artwork in the same finish. The owner excluded the
really small sprites; materials, pipes, one-tile equipment, floor and all silos
retain their previous artwork. The three support-machine exports keep their
32 x 32 dimensions and two-by-two footprints.

The owner's subsequent acid/ethanol exception replaces only **Lixivar AT-2/3/4**
and **Alembrine Cask-2/3/4**. Plain sealed lids and bund rims distinguish the two
families through sage/slate and steel/copper/brass; no readings, gauges, fill strips
or instruments are painted onto them. Other bulk stores and small sprites keep
their earlier artwork. The [tank request record](../../assets/phobos-manufacturing/liquid-tank-requests.json)
and [native/four-times review](../../assets/phobos-manufacturing/previews/liquid-tank-restyle-review.png)
retain the sources, trials and unchanged 32/48/64-pixel footprints.

| Area | Dedicated additions |
| --- | --- |
| Agriculture supplies (7) | Recorded crop residue, recovered concentrate, makeup salts, progressive mixture, spent biomass, Recycler wet rejects, bulk nutrient charge |
| Shipbreaker supplies/intermediates (8) | Fresh coolant, retained coolant, classified reclaimer feed, terminal reclaimer rejects, melt remainder, housing blank, R4 section, F6 section |
| Agriculture equipment (4 families) | Firstlight-4 rack, Hearth-2 cooker, W2 supply, B2 workup bench |
| Shipbreaker equipment (7 families) | Hull chute, exterior grabber, residue collector, R4 reclaimer, F6 furnace, F6-R radiator, F6-P underside thermal port |

Each equipment family has three new registered states: damaged installed,
packed intact, and packed damaged. Its existing approved installed chassis is
retained. **48 selected provider masters** produce **136 native PNGs**, including
matching neutral normal maps, eighteen damaged crop-stage compositions and two
damaged directional thermal-port compositions. Neutral maps do not provide
authored surface relief. Protective covers and straps are visual state cues;
they do not introduce new inventory items or material outputs.

The cooked Hearth potato portion already had dedicated artwork before this
pass. Existing food, seed and other approved stock art remains intact. R3 already
has dedicated intact/damaged masters and intentionally shares its chassis with
the loose forms. Native N2/N3 board art and Framework maintenance waste remain
intentional reuse of game resources at runtime, with no game textures bundled.
Manufacturing M4, proposed machining products and underfloor terminal concepts
remain design-only; their unsettled equipment contracts are not converted into
production assets by this pass.

## Review and provenance

The [provenance notes](../../assets/artwork-completion/README.md),
[selected master manifest](../../assets/artwork-completion/manifest.json) and
[runtime hashes](../../assets/artwork-completion/runtime-hashes.json) retain origins
and exact export identities. Preview sheets show native sprites beside integer
enlargements: [supplies](../../assets/artwork-completion/preview-1.png),
[equipment states](../../assets/artwork-completion/preview-2.png),
[packed states](../../assets/artwork-completion/preview-3.png).

PixelLab generated the new details. Only original Phobos art or text was supplied;
no Blue Bottle Games artwork was uploaded. PixelLab's
[terms of service](https://www.pixellab.ai/termsofservice) are recorded separately
from code licensing. The account allowance decreased from 1,952 to 1,894 included
generations during this pass (58), including rejected candidates and retries.
No credits were purchased or charged; the credit balance remained $0. One B2
damage request failed with a provider timeout before a successful retry.

The [overhead-first policy](asset-generation-policy.md#overhead-first-pixellab-rule--owner-direction-27-september-2026)
requires a vertical orthographic prompt, top surfaces only, no visible side
faces, high top-down/non-isometric settings where supported, and a reviewed
overhead reference for variants. Inspect the first result before expanding a
batch. Prompt controls reduce wasted attempts but cannot guarantee projection.

## Integration and verification

Native footprints, pivots, sockets, item IDs, recipe outputs and contents are
unchanged. The shared Framework binding only assigns image and portrait fields.
Agriculture retains saved crop-stage overlays when installed machinery is damaged;
F6-P retains its existing left/right coupling inserts. Registration references
restore the exact existing silhouette, and selected damage patches retain the
original chassis outside the authored damaged regions.

In a source checkout with Python and Pillow, run
`python scripts/export-completion-art.py --check` for byte-for-byte verification
of retained masters, source resolution and all direct/composed native exports.
Ordinary builds package committed PNGs and provenance; they never generate art.
The native checks load the real game definitions to verify every selected item
binding, dimensions and presentation-only state binding. Existing crop checks
also cover the eighteen damaged stages. These checks do not render Unity or
demonstrate save/load, lighting, wear blending or the owner's in-game approval.

## Bulk silo pass, 29 September 2026

Shipbreaker 0.38.1 adds five selected masters (53 in the manifest, 146 native
PNGs): the S3 process water silo (48 px, 96 px master), the T2 ice thaw unit
(32 px, 128 px master), the aluminium and steel ingots and the steel melt
remainder (16 px, 64 px masters). They replace procedural placeholders and are
shown on the [fourth preview sheet](../../assets/artwork-completion/preview-4.png).
The [handoff](bulk-silo-art-handoff.md) records the requests, results and
deviations; [bulk-silo-requests.json](../../assets/artwork-completion/bulk-silo-requests.json)
keeps every prompt, seed, job ID and review.

- The S3 and T2 fill their whole deck footprint, like the game's own square-deck
  machinery, so the exporter's `fullFootprint` rule requires them to be opaque
  edge to edge instead of padded. Their world sprite also serves as the
  inventory portrait for every form.
- The machines were generated by Pixflux from original start drawings in the
  approved Shipbreaker/Rivetline palette; the steel melt remainder likewise. The
  aluminium ingot is a Pixen result. The steel ingot is a deterministic,
  recorded luminance recolour of the aluminium ingot, so the pair differs only
  by metal tone.
- The allowance went from 1,890 to 1,875 included generations (15), with the
  credit balance at $0 throughout and no purchases. Rejected and superseded
  outputs are on the archive branch at commit
  [1df5784](https://github.com/phobos-dthorga/phobos-ostranauts/tree/1df5784356d8ee2f583032c42ce14032d73b5df0/assets/artwork-completion);
  see the [archive inventory](../../assets/rejected-artwork-archive.md).
