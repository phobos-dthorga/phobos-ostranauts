# Phobos Medical provenance

Phobos Medical is original Phobos code under the repository licence. It uses
Phobos Framework as a separately supplied dependency; Framework retains its own
upstream adaptation notices.

Native bed, sleep, wound, drag and time-skip behaviour was inspected locally in
[Blue Bottle Games' Ostranauts](https://store.steampowered.com/developer/bluebottlegames/)
1.0.1.5. The mod reuses the game's own sleep chain, Recuperating condition,
chair and sleep actions (cloned at load) and wound and drag systems at runtime by
reference. Until the Halewright artwork exists, the Ward-3 shows the game's own
Infirmaway images, referenced by name at runtime; no game asset, definition or code
is copied into the package. Game assemblies and decompiled source are excluded
from distribution. BepInEx and Unity references are resolved from the owner's
installation and are not bundled.

The shipped data packs in `framework/` are original data under the repository
licence; players may copy and adapt them freely. Admission thresholds are authored
gameplay balance on the game's own scales, not clinical values.
