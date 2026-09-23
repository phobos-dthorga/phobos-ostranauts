# Phobos Framework provenance

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

The upstream licence covers newly authored framework code/documentation only;
it excludes game-derived definitions, artwork and binaries. No game source,
extracted artwork, game definitions, upstream DLLs, BepInEx or Harmony binaries
are distributed. Ostranauts/Unity, Newtonsoft.Json, BepInEx and Harmony remain
separately supplied by the user's installation under their respective terms.
