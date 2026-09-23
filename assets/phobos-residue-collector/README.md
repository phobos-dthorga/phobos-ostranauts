# Residue Collector artwork

Original Phobos artwork generated with built-in ChatGPT Imagegen, 24 September
2026, for the implemented 2 x 1 hull-mounted collector. The reference was our own
[receiving-port concept](../phobos-material-transport/README.md), not an extracted
game asset. This newly generated sprite has been inspected at runtime pixel scale;
it has not yet been approved by the owner or tested in game.

- [Unchanged generated master](source/PhobosResidueCollector-v1.png)
- [Source SHA-256 and crop](sources.json)
- [Exact generation prompt](prompts.md)
- [32 x 16 runtime preview](previews/collector-v1-world.png)
- [Nearest-neighbour enlarged preview](previews/collector-v1-zoom.png)

Run `scripts/export-collector-art.ps1` to reproduce the colour sprite, flat normal
map and padded 256-pixel portrait in `mods/PhobosShipbreaker/images/phobos/shipbreaker`.
The script crops the retained source to its opaque bounds, downsamples with nearest
neighbour sampling, and uses the established binary-alpha export. The generated
master is never overwritten. Its 1176 x 608 crop is fitted to the 32 x 16 footprint.

Intact, loose and damaged definitions share this static body, with native damaged
tint/name. Cream service housing faces inboard (bottom); the dark pocket faces
outward (top). Native conduit is separate. No movement animation or ejection effect
is implied. The drawing and its mechanical exports remain under the project's MIT
scope; no exclusive rights in AI-generated imagery are claimed.
