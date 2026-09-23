# Dependencies and asset provenance

Phobos Shipbreaker's original code and documentation use the repository's MIT
licence. The distribution contains Phobos code, metadata and its construction
recipe; no game or dependency DLLs, extracted sprites or copied native definitions.

- [Ostranauts Crafting Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573443),
  inspected version 0.8.71: required plugin/data dependency. Phobos uses its JSON
  recipe interface and follows its native powered-tick integration pattern.
- [Salvage Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573453),
  inspected version 0.8.71: required native dependency. Phobos makes private runtime
  copies of sorter machine/container, installation, repair and power templates,
  then changes IDs, physical footprint, capacity, labels and processing demand.
  It does not replace the original sorter or its automation recipe.
- **Ostranauts**, Blue Bottle Games: native object/material definitions and a
  `ItmFloorGrate4x401` work-deck appearance are referenced from the user's installation
  at runtime. Mixed residue uses the native trash item's appearance with its own
  definition, mass and eligibility. These references are temporary artwork, not
  redistributed sprites or a claim of ownership over game content.
- **BepInEx 5 / Harmony** and the game's **Newtonsoft.Json**: used from the existing
  installation; none is redistributed in the package.

The installed framework/workshop licences grant MIT terms for newly authored
framework code/documentation and explicitly exclude game-derived definitions,
artwork, binaries and other third-party components. Runtime dependency references
do not extend those licences to the excluded material. Preserve these notices if
the integration changes to include any upstream source or assets.

Common Sense Salvage and Storage is an optional hauling companion; this package
does not copy its code or promise untested compatibility.
