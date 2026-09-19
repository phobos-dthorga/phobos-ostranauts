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

## Environment recheck: 2026-09-20

Read-only inspection while the owner's game was already running. No game files,
load order, plugin configuration or saves were changed. No gameplay experiment
was performed.

- Local `main` and the live remote `main` both resolved to
  `9c383c1c3df090dab383d4c90dd8df4188b68d0f`; the checkout was initially clean.
  GitHub reported `PRIVATE` visibility.
- The current session's `Player.log` reported `Release Build: 1.0.1.4`.
  The executable's product version is Unity's `6000.3.23f1`, not the game version.
- `BepInEx/LogOutput.log` reported BepInEx **5.4.23.5**, Configuration Manager
  **18.4.1**, the BepInEx Configuration Manager button plugin **1.0.0**, and
  Ship's Water **0.16.1** loading. Ship's Water also reported matching plugin
  and native-data versions.
- The startup log and patcher folder confirm an
  `OstranautsWorkshopBepInExBridge.Preloader` assembly, reported as **1.0.0.0**.
  It scans the configured load order and copies Workshop plugin files into
  BepInEx. This is additional installed machinery, not proof that the base
  game's Workshop loader itself runs DLLs.

The configured native load order, after core, contains the following packages.
Versions below come from their installed `mod_info.json` files; configuration
alone does not verify every package's gameplay behaviour.

| Package | Mod version | Declared target game | Workshop ID |
| --- | --- | --- | --- |
| Ithalan's Additional Ships | 1.5.1 | 1.0.0.7 | 3739343366 |
| BepInEx Configuration Manager | 1.0.0 | 0.15.1.0 | 3745492729 |
| BepInEx Mod Loader | 0.1.2 | 1.0.0.7 | 3741030124 |
| Ship's Water | 0.16.1 | 1.0.1.3 | 3757331189 |
| EVArything Suit Unlocked | 1.1.1 | 1.0.0.17 | 3800275558 |

Ship's Water's installed metadata describes water recycling, hygiene and infection
recovery changes, and the startup log reports its hydration multiplier. Any later
water, hygiene or infection experiment needs to account for this existing mod.
Its described behaviour has not been independently tested in this project.

Both sample archives remain present. Their metadata targets old game versions:
`SampleMod` specifies `0.8.0.0`, and `SampleWorkshopMod` specifies `0.15.0.34`.
They demonstrate package layout and asset overrides, not a newly functioning
machine. Their sample load orders also ignore the core full-name data; do not
copy that unrelated example exclusion into a new mod.

The official modding guide was reopened on this date. Its description of local
mod folders, metadata and later-definition replacement agrees with the supplied
examples. Keep new content under unique `Phobos` identifiers and reference core
resources by identifier where appropriate, without distributing extracted assets.

## Concrete native references inspected on 2026-09-20

Paths here are relative to `Ostranauts_Data/StreamingAssets/data/`. These are
observations of definitions and supplied schema descriptions, not tested engine
semantics for a Phobos object.

| File | Useful entries and findings |
| --- | --- |
| `schemas/interactions-schema.json` | Documents durations in hours, actor/target condition tests, chained interactions, cancellation callbacks, item operations and log descriptions. Explicit `,[us],[them]` suffixes preserve roles in a chain. It does not establish when resource changes commit or whether power is continuously rechecked. |
| `condowners/condowners.json` | `ItmBedMedical01` has power points, electrical update handling and a power ticker. `ItmChair02` offers sitting and relaxation. `ItmWorkbench01` has no direct interactions in its inspected definition; it is not evidence of a working crafting station. |
| `interactions/interactions.json` | `SeekSleepSimpleLieDownMedical`, `ACTChairSitShim`, `ACTSeekSecurityChair`, `ACTExcerciseTreadmillDo` and `SeekDrugAntibioticPrescDirectAllow` show existing care, recreation and consumption paths. |
| `conditions/conditions.json` | Defines blood loss, pain, infection, dehydration and hypoxia, plus recovery-rate fields. `StatBlood` describes loss, rather than blood remaining. Visible severity and bleeding conditions already exist, so repeating those alone has limited diagnostic value. |
| `condrules/condrules.json` | `DcBlood` maps `StatBlood` to severity conditions. Threshold entries include transition adjustments; do not assume simple boundary comparisons reproduce runtime behaviour. |
| `loot/loot.json` and `tickers/tickers.json` | Separate blood-loss and blood-recovery ticks use `StatBloodRate` and `StatBloodHealRate`. A positive loss rate alone does not establish the net trend. Medical sleep modifies wound, blood and infection recovery and other conditions. |
| `condtrigs/condtrigs.json` | `TIsBedMedical` checks installed, undamaged and not-off state; `TIsPowered` is a separate condition test. `TIsBleeding` refers to wound bleeding conditions. Do not assume a wound-local condition appears on the patient or that this trigger is a read-only patient test. |

The [first furniture investigation](first-furniture-experiment.md) is historical.
The current [medical and portable-power evidence map](medical-research.md),
[runtime findings](medical-runtime-findings.md) and
[next-step decision brief](medical-next-steps.md) reflect the broader direction.
