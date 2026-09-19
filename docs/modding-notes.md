# Initial modding findings

Recorded 2026-09-19 from the local installation and the references below.
These are starting points, not a compatibility certification or a working mod.

## Native content

The installed game exposes linked JSON definitions for objects (`condowners`),
visuals and grid footprints (`items`), interactions, conditions, condition tests,
threshold rules, timers, installation and repair, power and gas handling.

New interactions can combine existing tests, timed actions, effects and state
changes. Engine update commands select existing C# implementations; a new JSON
property or command name does not automatically implement new engine behaviour.

The medical bed is a useful example: its object definition supplies interactions,
power connection points and state flags; the item supplies graphics and placement
requirements; power and medical effects are separate definitions.

Native mods support local development and Steam Workshop distribution. Later
definitions with matching identifiers can override earlier ones. Prefer unique
identifiers for new content and keep overrides narrowly scoped.

## Code extensions

BepInEx and Harmony-style runtime patches are an established C# route. Custom UI,
automated processing, detailed diagnostics and new AI behaviour may need this
layer. Select exact runtime versions when the first code feature is implemented.
Do not assume native Workshop subscription alone loads arbitrary plugin DLLs.

Persistence and simulation time need explicit design: ordinary ticks, fast-forward,
unloaded ships and save/reload may follow different execution paths.

## Artwork

Inspected vanilla PNG dimensions: sink 32x16, treadmill 32x48, medical bed 48x80.
These examples use 16 pixels per tile and separate normal-map textures; some
also have damaged variants. Use local originals as reference, not repository assets.

Generate concepts or base artwork, then check silhouette, transparency, native-size
readability, grid alignment, lighting and state variants. Record asset origin,
AI assistance, edits and applicable distribution terms alongside final assets.

## References

- [Official Ostranauts modding guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946)
- `SampleMod.zip` and `SampleWorkshopMod.zip`, supplied in the game installation.
- [BepInEx runtime patching](https://docs.bepinex.dev/articles/dev_guide/runtime_patching.html)
- [Room Effects: an existing code-mod example](https://github.com/Kriil/ostranauts/blob/main/RoomEffects/README.md)

The repository setup follows [Republic Observatory](https://github.com/phobos-dthorga/soviet-republic-observatory).
Its application stack and save-observer architecture are not dependencies here.
