# Phobos Exchange artwork

| File | Use | Source |
| --- | --- | --- |
| [Shares-1024.png](Shares-1024.png) | Master of the EXCHANGE PDA app icon | Drawn by `scripts/export-exchange-art.py` |
| `mods/PhobosExchange/images/phobos/exchange/Shares.png` | The icon the game loads, 256 px | Lanczos reduction of the master |

No image generator was used: the script draws every shape from fixed coordinates
and is the source of record. [exports.json](exports.json) records each output's
hash; `python scripts/export-exchange-art.py --check` verifies them. The icon follows
the game's own PDA icons, a white disc with a black glyph that the game tints, at
the game's 256 px; the master is four times that. The glyph is a price chart: two
axes and a line that dips, then climbs through two runs to an arrowhead (agent
choice, 7 October 2026).
