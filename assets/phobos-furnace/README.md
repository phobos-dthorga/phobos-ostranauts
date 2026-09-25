# F6 furnace artwork and layout study

**25 September 2026 — implementation candidates, owner review pending.**
The [electrical direction](../../docs/furnace-electrical-direction.md) supersedes
the earlier reactor-side coupler. Use the [current operating guide](../../docs/furnace-player-guide.md)
for the implemented tile layout and first-cycle controls.

Four original masters are retained unchanged in `source/`:

| Master | World export | Uses |
|---|---|---|
| `PhobosFurnace-v1.png` — 1254 x 1254 | 96 x 96 | F6 installed/loose; native damage tint |
| `PhobosFurnaceRadiator-v1.png` — 1536 x 1024 | 96 x 64 | Separate radiator installed/loose; native damage tint |
| `PhobosFurnaceHousing-v1.png` — 1254 x 1254 | 32 x 32 | Rough/finished casting, with live item names |
| `PhobosFurnaceThermalPort-v1.png` — 1254 x 1254 | 16 x 16 | F6-P sealed mounting head; underside assembly represented by the equipment |

[Full prompts and provenance](prompts.md) record four built-in image_gen calls.
The [export manifest](exports.json) pins source hashes, crops and sizes.
Run `scripts/export-furnace-art.ps1` to regenerate twelve runtime colour/flat-normal/
portrait files and native-size/4x previews. Exports use nearest-neighbour sampling
and a shared alpha threshold; portraits retain integer scaling and padding.
This creates no enlargement of the physical footprint.

The F6 section reuses the existing original D4 section graphic. Terminal melt
remainder reuses original Phobos residue artwork with a distinct saved identity,
name and description. Construction uses the native unfinished-equipment display.
There is no separately represented transport cassette or reactor coupler.

This small first set follows the existing reclaimer/intake practice. Distinct
transport/damage silhouettes, richer normal maps and a dedicated F6 section sprite
can follow owner feedback. No new sprite is claimed approved merely because the
earlier layout study was liked.

## Keeping changes inexpensive

- Controls, measurements, labels, units, language and sequence are code/catalog
  data, separate from artwork. No number or process state is painted into a panel.
- The panel reuses installed native knobs, meters, lamps and font resources plus
  the existing Phobos frame; no generated UI plate or native texture export.
- The furnace, radiator, housing and thermal port are independent transparent masters. A change
  to one does not require regenerating the others.
- Paths and world bounds remain stable. Revise the retained master, its provenance
  and hash, and only if needed its manifest crop; then run the exporter. Derived
  files are never edited independently.
- These are flat transparent images, not layered source files. Future moving
  covers or damage overlays should become separate registered layers only when
  a concrete visual change requires them.

The F6-P master was generated in one built-in call for Shipbreaker 0.13.0.
It has no baked label, heat state or exhaust plume. Its four bolts, pale insulation
and capped service connection remain separate from the unchanged furnace art.
World dimensions, alpha, normal and portrait alignment are exported together.
The 1 x 1 head does not imply a one-square-metre radiator: the complete underside
assembly has the same finite 12 m² effective area as the exterior option.

## Earlier research

The [interactive layout study](research/layouts.html) uses original geometric
placeholders and system fonts; its sample states have no game connection. Its
direct-fusion coupler installation is explicitly historical. The panel grouping
informed the runtime view, but the study is not a screenshot of it.

The [UI reuse brief](../../docs/furnace-ui-and-art.md) records native donor paths.
The implementation uses isolated knob, LED and lamp donors; guarded-toggle,
seven-segment and slider adapters remain follow-up work. In-game isolation, focus,
scaling and appearance await owner checks.

Research tools remain `scripts/calculate-furnace-cycle.py`,
`scripts/verify-furnace-study.cjs` and `scripts/inspect-furnace-ui.py`. Game-derived
metadata and screenshots stay in ignored `.local/`. Blue Bottle Games retains
ownership of native resources; only Phobos code and original assets are distributed.
