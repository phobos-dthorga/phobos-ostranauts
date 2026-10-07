# Phobos Exchange artwork

| File | Use | Source |
| --- | --- | --- |
| [Shares-1024.png](Shares-1024.png) | Master of the EXCHANGE PDA app icon | Drawn by `scripts/export-exchange-art.py` |
| `mods/PhobosExchange/images/phobos/exchange/Shares.png` | The icon the game loads, 256 px | Lanczos reduction of the master |
| `assets/workshop/sources/PhobosExchange-scene.png` | Scene layer of the held Workshop cover, 244 x 170 | Drawn by the same script; composed by `scripts/compose-workshop-cover.py` |

No image generator was used: the script draws every shape from fixed coordinates
and is the source of record. [exports.json](exports.json) records each output's
hash; `python scripts/export-exchange-art.py --check` verifies them. The icon follows
the game's own PDA icons, a white disc with a black glyph that the game tints, at
the game's 256 px; the master is four times that. The glyph is a price chart: two
axes and a line that dips, then climbs through two runs to an arrowhead (agent
choice, 7 October 2026).

The cover scene is a placeholder (agent choice, 7 October 2026, while the mod is a held
draft): an overhead cabin nook where a spacer checks the exchange board on the bulkhead,
with a desk terminal and two crates. No image generator was used, to avoid an unasked
cost; a generated scene can replace it before publication if the owner wants one.
