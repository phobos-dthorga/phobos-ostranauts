# PDA locator and installed sensors: research

**Status: set aside by the owner on 2026-09-20.** The owner clarified that the
existing cargo manifest is available immediately and is already being used.
That immediate benefit outweighs developing our sensor-based alternative; overlap
alone is not a universal rejection rule. Preserve the evidence for reference; do not treat the
recommendations below as active work. See [cartridge alternatives](pda-cartridge-ideas.md).

Research date: 2026-09-20. The owner proposed an additional app in the existing
PDA, supported by installed sensors with limited detection radii. This document
records feasibility and overlap; the [experiment brief](locator-next-steps.md)
contains proposed behaviour and acceptance checks. No locator has been built.

## Assessment

An extra PDA app and a limited-area sensor network are technically plausible.
The app shell has a concrete extension route, while spatial queries, power and
line-of-sight helpers supply useful components. The detection and information
rules would require C#; a new JSON sensor name cannot implement them by itself.

However, **a general searchable cargo manifest already exists as another mod**.
The justification for this project would be deploying and maintaining useful
sensor coverage, not claiming to invent PDA item search. If the owner mainly
wants a convenient cargo list, evaluate that existing option before duplicating it.

## Existing functionality and competing mods

| Source | Verified scope of the evidence | Implication |
| --- | --- | --- |
| Installed `PDAVisualisers` code and `conditions/conditions_pda.json` | Vizor supports power, damage, mass, price, heat and pressure presets. Its assembly path checks wrist equipment and cartridge conditions. | A generic condition/price overlay overlaps existing capability. Searchable, coverage-limited observations need their own purpose. |
| Installed `GUIPDA.OpenApp` inventory branch and selected inventory classes | ITEMS opens the existing inventory interface; examined inventory code includes local ground queries. | Do not describe ITEMS as a new ship-wide search app. Full current UI behaviour still needs a gameplay comparison. |
| [Common Sense Cargo Manifest, by LOGUSS](https://steamcommunity.com/sharedfiles/filedetails/?id=3790481501) | Author advertises searchable cargo across nested unlocked containers, carried equipment and the ship interior, with grouping, sorting and pickup. The page reports version 0.12.10 tested with game 1.0.0.13. | Substantial overlap with a convenience locator. This is an author description, not a test against our installed 1.0.1.4. |
| [Common Sense collection](https://steamcommunity.com/sharedfiles/filedetails/?id=3789049955) | Author describes PDA HOME > MANIFEST, owned-ship selection, quantities, condition, value, location and access to storage/pickup. | Our proposed coverage constraints are not advertised in that description. This does not establish that no such option exists in its implementation. |
| [PDA Torch Maneuver Planning System, by Diced](https://steamcommunity.com/sharedfiles/filedetails/?id=3788309223) | Author describes a cartridge-based PDA application requiring BepInEx Mod Loader. | External precedent for an additional PDA application. Its implementation and compatibility were not audited or adopted. |

The [official starting guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3347080066)
also documents the PDA home/app workflow and Power Viz. Web listings and isolated
forum reports do not prove current compatibility or a complete inventory of mods.
No additional mod was subscribed to, downloaded into the game or enabled during
this research. No dependency on Common Sense is proposed.

## Local evidence and reproduction

The current session log still reports **1.0.1.4**. Assembly SHA-256 still matches
`1DC1858A8EDC514EC089F2FD7C55932C7F9B62B0A96201C15E2B2720122A03A7` for
`Ostranauts_Data/Managed/Assembly-CSharp.dll`. Startup logs continue to report
BepInEx 5.4.23.5, Configuration Manager 18.4.1, its button plugin 1.0.0 and
Ship's Water 0.16.1. See the full [environment record](modding-notes.md#environment-recheck-2026-09-20).

Evidence below is **data** (installed definitions), **code** (selected local
implementation), **external** (author/standards references), or **proposal**.
There is no gameplay verification of a Phobos sensor, app or network.
Selective ILSpyCmd 11.0.0.9375 output and metadata research remain in ignored
`.local/`; no extracted assets, assemblies or decompiled source belong in Git.
No game files, player saves or active configuration were changed.

## App integration

Data paths in this document are relative to
`Ostranauts_Data/StreamingAssets/data/`.

| Evidence | Result |
| --- | --- |
| `pda_apps/pda_apps.json` supplies app name, friendly label, icon path and hidden flag. | There is an existing data format for another home icon. Use a unique `Phobos` identifier and original art. |
| `GUIPDAHomepage._GenerateAppIcons` enumerates `DataHandler.dictPDAAppIcons`, instantiates the existing app-button prefab and calls `GUIPDAApp.UpdateInfo`. | A data entry can participate in the native home layout, subject to actual mod loading and UI testing. Avoid editing core JSON. |
| `GUIPDAApp.Toggle` calls `GUIPDA.OpenApp(strName)`. Tooltip keys are derived from the uppercase app name. | Supply title/description strings as well as an icon. A missing icon can fail during texture setup; it is not just cosmetic. |
| `GUIPDA.OpenApp(string)` switches over known names; unknown names return to the home screen. `GUIPDA.UIState` and its state handling are fixed code. | Adding JSON alone does not create a working app. A narrow C# dispatch hook and custom panel/lifecycle are required. Do not invent an enum value and expect native state handling to support it. |

Proposed route: register the app definition; intercept only its exact app name;
show a panel integrated with the PDA; let all other app calls follow their normal
path. Closing, Home/Escape, switching apps, switching crew and changing saves must
close/reset our panel and markers correctly. Hook timing, panel parenting, input
focus, UI scale and hotbar support remain untested. An app appearing in the home
menu does not itself establish PDA possession or a physical data connection.

`PDAVisualisers._Assemble` is a useful possession example: `TIsWristSlotted`
requires wrist-slot state and forbids locked/damaged equipment, then cartridge
conditions enable controls. Our app should also verify it is the intended PDA,
not merely any matching wrist object. The examined `ItmWristPDA01` definition has
no power-info entry or power ticker. Do not promise native PDA battery drain or
modify every PDA's energy behaviour as an incidental part of this project.

## What existing sensors actually do

The `Sensor` component's `Run` queries objects at a named point with
`Ship.GetCOsAtWorldCoords1`, tests them and queues configured interactions. Native
temperature alarms (`ItmAlarmTempOnB/OnR/OnW`) use `Sensor,AlarmTemp`. This is a
point-based detector/interaction path, not a radius-based inventory scanner.

The separate `Ostranauts.Ships.Sensors.ShipSensor` family represents ship sensing
such as radar, optical, IR and lidar, with ranges expressed in kilometres. It is
not an established API for identifying loose items inside a ship. Neither family
provides evidence of a ready-made local tracking network.

Temperature alarms nevertheless offer a small installed-device reference:
loose/installed/damaged variants, electrical handling and power input. Their
`AlarmPressureTemp` power info links on/off interactions and external supply.
Its coefficient is `6.25e-8` kWh per game second in the inspected `Powered` path
(0.225 W by conversion). That is an example's value, not a selected scanner load.
New sensor definitions must have their own identifiers and appropriate effects;
copying alarm transitions would retain unrelated alarm behaviour.

## Finding items is not the same as detecting them

| Code entry point | Useful capability | Required boundary |
| --- | --- | --- |
| `Ship.GetCOs(CondTrigger, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)` and `VisitCOs` | Enumerate top-level or nested objects, optionally including docked ships. Results are collected through a set. | They do not themselves apply sensor coverage, player knowledge or access policy. Rooms and other non-cargo objects can also appear. Use an explicit eligible-item filter. |
| `Container.VisitCOs` | Recursively visits contents; `bAllowLocked=false` skips a container marked `IsLocked`. | Unlocked does not mean optically visible, open, previously inspected or owned. A lock flag is not a complete detection rule. |
| `CondOwner.GetPos()` | Normally delegates position to the object's parent, giving contained items their container/carrier location. | A hit inside a bag should lead to that bag, not imply a separate physical spot for its contents. Parent chains and stacked objects need tests. |
| `Ship.GetTileIndexAtWorldCoords` / `GetWorldCoordsAtTileIndex1` | Convert between the loaded interior grid and world positions. The inspected mapping uses one coordinate unit per tile. | Specify radius in tiles, not an invented physical unit. Check invalid indices, docking, ship changes and reloading. Do not use orbital coordinates for interior distance. |
| `Visibility.IsCondOwnerLOSVisible` | Ray-based test rejects installed walls and closed portals on the relevant layer. | It is not a general RF propagation model or a container-visibility check. Its collider and endpoint behaviour need tests for wall-mounted sensors. |
| `Visibility.IsCondOwnerLOSVisibleBlocks` | Alternative block-based test with glass and endpoint options. | Different semantics and cost; choose deliberately. Do not treat the two helpers as interchangeable. |
| `CondOwner.Visible` | Reads renderer-enabled state. | It is not a reliable record of what the player has discovered or what a sensor knows. Setting it changes presentation/colliders; do not reveal items this way. |

For the first test, one explicitly selected test ship, no docked-ship expansion
and top-level eligible equipment sharply reduce accidental disclosure. Product
access rules still need a verified owned/authorised ship check; being loaded or
occupied is not sufficient authority for a network. Crew inventories, closed
containers and off-ship items should be outside the first detection contract.

## Locating a result

`CrewSim.CamCenter(CondOwner)` starts following an object. It is a possible route
for a current valid result, but continuous follow is different from a one-time
camera jump. Test how to exit it without disrupting normal camera behaviour.

`CrewSim.HighlightCOs` dims objects across loaded ships and uses shared highlight
state. It is not an isolated marker API. A small temporary locator marker may be
less invasive; its rendering and cleanup still need a prototype. Do not clear
other systems' highlights or change global Vizor settings to show one result.

A stale result must point to its recorded location, not dereference the item's
current position. Otherwise an out-of-coverage item would still be tracked.
Navigating to a result must not pick up, teleport, open or issue work on it unless
that separate action is explicitly designed and chosen by the player.

## Power, observation time and persistence

Use the native installed power path; see [power findings](medical-runtime-findings.md).
`TIsPowered` only checks a condition. Installation, operational/damage state,
manual-off state and available supply all matter. Verify transitions rather than
assuming an `IsPowered` flag is current immediately after disconnection or reload.
The sensor must stop producing fresh observations when inactive.

Sample at a bounded interval in game time through one service. Enumerate eligible
items once per sample for the target ship, then test against active sensors.
Deduplicate by stable object ID, prefilter distance before LOS, and keep Unity/game
API access on the main thread. Measure work before adding spatial indexing. Do not
scan every loaded ship for every sensor on every frame or every search keystroke.

At acceleration, produce an actual present observation when due; do not replay
thousands of missed samples. While a ship is unloaded, we cannot reconstruct where
items were at intermediate times from its present state. Do not manufacture a
historical path during catch-up. UI refresh must not perform an additional scan.

The simplest prototype keeps observations in memory, clears them on save changes
and reload, and explicitly waits for a new scan. Native state can preserve the
physical sensor; that still needs testing. Persistent last-known observations are
a later feature requiring save identity, item ID, ship ID, ship-relative position,
sample time and data version. Do not store live object references across loads or
put per-save history in a global plugin config.

## Plausible detection mechanisms

| Proposed mechanism | Merits | Limits / additional work |
| --- | --- | --- |
| Optical survey sensor | Detects exposed recognisable equipment; walls and closed doors make placement matter. Fits existing LOS helpers. | Cannot inspect opaque storage or infer battery charge merely from an item's appearance. Geometry and mount points need validation. |
| Tag reader / tracking transponder | Gives a clear reason to identify particular tools and containers without direct sight. | Requires a tag/registration model, compatible items, attachment persistence and explicit shielding rules. Reading identity alone does not establish charge telemetry or exact position. |
| Paired equipment telemetry | Could report charge/status for compatible devices and support later machinery or medical equipment. | No universal native equipment-broadcast protocol was established. An electronics category flag is not proof that an object transmits its state. |

[GS1's RFID range explanation](https://support.gs1.org/support/solutions/articles/43000734166-what-is-the-read-range-for-a-typical-rfid-tag-)
distinguishes active/passive systems and notes that read range depends on equipment
and operating conditions. Its [RFID overview](https://www.gs1.org/standards/rfid)
describes identification standards for tagged objects. These support the reader/tag
concept, not a universal radius or a claim that one reader yields precise location.
An exact tile from a simple tag reader would be a deliberate gameplay abstraction;
a reader/compartment-level result is another option. No radio standard is being
implemented here.

## Feasibility boundary

Proceed only if building coverage and choosing what to track sound worthwhile
even with a general manifest available. The smallest plausible proof is one
powered optical sensor, one eligible exposed item type and a PDA result within
radius and LOS. A tag-based version is a separate meaningful design choice.

Set this aside or redirect it if sensors become busywork around an existing
convenience feature, if reliable app lifecycle/markers require broad intrusive
patches, or if the desired detection would require a much larger simulation than
its gameplay warrants. No framework, exact radius, price, wattage, refresh period,
dependency, release date or first-feature selection follows from this research.
