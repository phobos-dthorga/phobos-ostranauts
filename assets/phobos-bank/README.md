# Phobos Banking artwork

| File | Use | Source |
| --- | --- | --- |
| [Credit-1024.png](Credit-1024.png) | Master of the CREDIT PDA app icon | Drawn by `scripts/export-bank-art.py` |
| `mods/PhobosBank/images/phobos/bank/Credit.png` | The icon the game loads, 256 px | Lanczos reduction of the master |
| `assets/workshop/sources/PhobosBank-scene.png` | Scene layer of the held Workshop cover | Drawn by the same script at 244 x 170 |

No image generator was used: the script draws every shape from fixed coordinates
and is the source of record. [exports.json](exports.json) records each output's
hash; `python scripts/export-bank-art.py --check` verifies them. The icon follows
the game's own PDA icons, a white disc with a black glyph that the game tints, at
the game's 256 px; the master is four times that.

The cover scene is a placeholder (agent choice, 6 October 2026, while the mod is a
held draft): a finance kiosk showing ledger rows, one of them late, and a credit
chit in its reader. A generated scene can replace it before publication if the
owner wants one; the cover frame and lettering come from
`scripts/compose-workshop-cover.py` as for the other composed covers.
