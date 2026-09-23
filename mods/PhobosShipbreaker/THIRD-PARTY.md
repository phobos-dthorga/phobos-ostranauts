# Dependencies and asset provenance

Phobos Shipbreaker's original code and documentation use the repository's MIT
licence. The distribution contains Phobos code, metadata and its construction
recipe; no game or dependency DLLs, extracted sprites or copied native definitions.

- **Phobos Framework 0.2.0+**: required separate shared provider for construction,
  definition publication, inventory planning and production delivery. Its notices
  include the credited OCF construction-code adaptation; install one provider.
- [Ostranauts Crafting Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573443)
  and [Salvage Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3798573453),
  inspected at 0.8.71: optional companions and earlier implementation references.
  Shipbreaker 0.2.0 replaces the previous runtime sorter-template adapter with
  Phobos-authored native machinery definitions, preserving our saved identities.
  No SWB definitions are looked up for the standalone path. An existing Workshop
  bench is an optional construction surface; its original definitions stay owned
  by Workshop. Native power/installation patterns informed this implementation.
- **Ostranauts**, Blue Bottle Games: native object/material behaviours are
  referenced from the user's installation at runtime. No game artwork is bundled.
- **Phobos artwork**, generated with ChatGPT's built-in Imagegen tool: original
  installed, damaged, transport, unfinished-section and mixed-residue colour art.
  The owner approved the installed v2 pixel-art direction on 23 September 2026.
  A user-supplied game screenshot informed that v2 edit as a style reference;
  it is not bundled. Derivatives, mechanical exports and original technical normal
  maps are included under the repository's MIT scope, without claiming exclusive
  rights in generated imagery or rights over Ostranauts art. Masters, exact prompts
  and hashes are preserved in the repository's `assets/phobos-shipbreaker/` folder.
- **BepInEx 5 / Harmony** and the game's **Newtonsoft.Json**: used from the existing
  installation; none is redistributed in the package.

The installed framework/workshop licences grant MIT terms for newly authored
framework code/documentation and explicitly exclude game-derived definitions,
artwork, binaries and other third-party components. Runtime dependency references
do not extend those licences to the excluded material. Preserve these notices if
the integration changes to include any upstream source or assets.

Common Sense Salvage and Storage is an optional hauling companion; this package
does not copy its code or promise untested compatibility.

Shipbreaker 0.4.0 adds an original Imagegen residue-collector sprite, using our own
receiving-port concept as the style reference. The unchanged master, exact prompt,
hash and mechanical exports are documented in `assets/phobos-residue-collector/`.
No community art or game textures were copied for this family. The new collector
art is prepared for owner testing, not yet owner-approved.

Shipbreaker 0.3.0 also includes the owner-approved original hull chute and exterior
grabber generated with built-in Imagegen. Masters, prompts, SHA-256 hashes and crop
coordinates are in `assets/phobos-hull-intake/`. Runtime colour exports use mechanical
nearest-neighbour sampling and binary alpha; flat normals are technical shader data.
The same provenance and MIT-scope qualifications above apply. No game artwork is included.
