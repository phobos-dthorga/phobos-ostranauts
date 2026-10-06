# Phobos Banking provenance

Phobos Banking is original Phobos code under the repository licence. It uses
Phobos Framework as a separately supplied dependency; Framework retains its own
upstream adaptation notices.

Native ledger, mortgage, late-fee, Finances window and PDA behaviour was
inspected locally in [Blue Bottle Games' Ostranauts](https://store.steampowered.com/developer/bluebottlegames/)
1.0.1.5. The mod reads the game's own ledger and opens the game's own Finances
window at runtime by reference; it mirrors the game's mortgage instalment formula
and late-fee share as named figures so the panel can show them, and checks the
mirror against the installed game. No game asset, definition or code is copied
into the package. Game assemblies and decompiled source are excluded from
distribution. BepInEx and Unity references are resolved from the owner's
installation and are not bundled.

The way a PDA app is added (an icon entry, two tooltip strings and a prefix on the
game's app switch) follows the pattern LOGUSS's Common Sense Cargo Manifest
(Steam Workshop item 3790481501) uses; no code from it is copied. Phobos Framework
implements it independently.

## Artwork

The CREDIT icon (`images/phobos/bank/Credit.png`) and the Workshop cover's scene
are original, drawn from fixed shapes by `scripts/export-bank-art.py`, which is
their source of record; no image generator and no game image was used. The icon
follows the look of the game's own PDA icons (a white disc with a black glyph)
without copying any of them. The cover reuses the Phobos cover frame from the
Shipbreaker cover, as the Manufacturing, War Has Been Declared and Medical covers
do (`assets/workshop/composed.json`).
