# Phobos Framework provenance

Framework 0.15.0 also bundles the separately maintained Phobos-authored
`Phobos.Scope.Recording.dll` from the pinned `external/phobos-scope` submodule.
Phobos Scope's project licence remains undecided; the repository MIT grant does
not cover it. See `licenses/PhobosScope-LICENSING.md` in the native mod folder.

Phobos Framework 0.2.0 is distributed under the repository MIT licence, with the
upstream notice below retained for adapted portions.

Construction recipe DTOs, native interaction/loot registration, exact ingredient
selection, native ingredient fetching and completion gating were adapted from
Ostranauts Crafting Framework 0.8.71. Original credit: **Crafting Framework
contributors**. Source/reference: [Steam Workshop item 3798573443](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573443).
The complete upstream notice is in
[licenses/CraftingFramework-MIT.md](licenses/CraftingFramework-MIT.md) within the
Phobos Framework native mod folder. In the unpacked package this is
`Mods/PhobosFramework/licenses/CraftingFramework-MIT.md`.

Phobos changes include an explicit consumer registration API, atomic pack
publication, defensive recipe snapshots, mass validation, optional work surfaces,
Phobos action ownership and narrowly scoped migration aliases. This is a separate
provider; it neither scans OCF packs nor replaces OCF's registry. Random salvage,
blueprints, water adapters and OCF automation are not included.

Inventory planning, production delivery and definition rollback services are
Phobos-authored, moved from Shipbreaker into the shared provider. The native
registration helper references the player's game at runtime.

The observation value contract and native room-alarm adapter added in 0.13.0
are Phobos-authored. Native sensor/room API and data were inspected locally;
no decompiled source, alarm artwork or game definitions are distributed here.

The upstream licence covers newly authored framework code/documentation only;
it excludes game-derived definitions, artwork and binaries. No game source,
extracted artwork, game definitions, upstream DLLs, BepInEx or Harmony binaries
are distributed. Ostranauts/Unity, Newtonsoft.Json, BepInEx and Harmony remain
separately supplied by the user's installation under their respective terms.

Version 0.5.0 material-port pairing is independently authored Phobos code. Its
native property-map persistence approach was checked against the locally installed
game's electrical connection and item-save behaviour. It contains no copied game
source and does not replace or modify the native electrical connection records.
