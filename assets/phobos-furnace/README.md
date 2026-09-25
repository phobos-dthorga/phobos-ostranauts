# F6 furnace research assets

The owner has since [selected electrical heating](../../docs/furnace-electrical-direction.md).
The installation's direct-fusion coupler is a historical concept, not required
artwork for the electrical build. The instrument layout remains the design baseline.

25 September 2026. Research and layout work only; no production furnace assets.

Open [the interactive layout study](research/layouts.html) in a browser for the
tile-scale installation, full instrument panel and compact panel. Select a sample
state to inspect partial heat delivery, cooling, blocked output or missing probes.
Scroll the instrument area; machine identity, alarm and stop controls remain fixed.
All readings and gestures are illustrative and have no game connection.

The HTML uses original geometric placeholders and system fonts. It contains no
extracted vanilla pixels. The intended native donors and remaining original art
are specified in [the reuse/graphics brief](../../docs/furnace-ui-and-art.md).
The physical design and assumptions are in [the cycle report](../../docs/furnace-first-cycle.md).
No image-generation prompts, image masters or approval claims exist for this round.

Follow the [approach for inexpensive revisions](../../docs/furnace-ui-and-art.md#minimize-the-work-needed-for-later-changes):
separate native controls/live labels from panel framing; retain editable registered
machine layers; export ordinary flattened native assets with stable paths and one
manifest. Create the manifest/exporter with the first actual masters, not empty
placeholder art. The electrical build needs 12 colour forms, plus their derived
normals/portraits; no reactor-coupler artwork is required.

Reproduce the research checks from the repository root:

```text
python scripts/calculate-furnace-cycle.py --output docs/research/furnace-cycle-calculations.json
node scripts/verify-furnace-study.cjs
python scripts/inspect-furnace-ui.py --game <local-game-directory> --output .local/research/furnace/ui-metadata.json
```

The calculation has no external dependencies. Browser verification requires
Playwright and local Microsoft Edge; screenshots remain in ignored
`.local/art-review/furnace/`. Native inspection requires UnityPy and the exact
documented game assembly/serialization version. Supply local paths at invocation,
never in committed configuration. Inspection exports metadata only, under `.local`.

Preserve Blue Bottle Games' ownership of native assets and source. Future mods
reference suitable installed resources at runtime and distribute Phobos code and
original artwork only. Native compatibility/isolation still needs in-game checking.
