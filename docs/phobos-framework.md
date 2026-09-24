# Phobos Framework: independent foundation

Decision: 24 September 2026. The owner selected our own shareable framework
**instead of Ostranauts Crafting Framework**, and authorised full independence.
The purpose is reusable services for equipment and future material transport,
not reproducing every feature of OCF or turning Ostranauts into a factory game.

## Current candidate: Framework 0.13.0, Shipbreaker 0.11.0

Framework 0.13.0 adds [shared observations](shared-console-observations.md):
an immutable evidence/validity contract and a narrow native room-alarm adapter.
Shipbreaker 0.11.0 uses them for the C1 console, built-in R4 cooling probes and
session stop evidence. Furnace instruments and specialist packages remain future
work. No saved schema or material/heat accounting changes are introduced.

Framework 0.12.0 adds shared [equipment name formatting](equipment-branding.md),
with brands/models owned by content and type/variant descriptions translated.
Auto Nav 0.8.1 and Shipbreaker 0.10.1 use it without changing saved IDs.

The [sensor report](sensor-integration-research.md) remains the wider research
direction. Extract further shared services as concrete consumers need them;
content owns instruments and process responses. Numeric process telemetry is
not inferred from native alarm outputs.

Framework 0.11.0 adds shared versioned object-state envelopes, detached snapshots,
owner checks and protection of unreadable/unsupported state. Auto Nav 0.5.0 uses
them for [saved flights](auto-nav-persistence.md). Gameplay authority and resumption
remain content-owned. Existing transport records and pause policies are unchanged.

The [residue collector](residue-collector.md) adds a finite wall-mounted receiving
chamber linked to one processor through structural flooring. Framework owns the
existing same-object move plus small filter, clock, grid-search and saved-pair helpers;
Shipbreaker owns physical rules and machinery. Eight construction recipes register, including the industrial console.
Current Shipbreaker requires Framework 0.13.0; Auto Nav requires 0.12.0 or later.
The installer enforces these requirements and verifies all console/intake artwork.
Version 0.9.0 adds [automatic reclaimer feeding](automatic-material-routing.md),
collector buffer outputs and shared saved exact-ID filters. Players choose one
sender/receiver pair per logical port from either endpoint's controls. Pairing
uses full object IDs in native saved property maps, following signal connections.
Offline checks pass; gameplay and old-save loading remain owner tests. The earlier
[connected intake](shipbreaker-hull-intake.md) is included unchanged in scope.

Auto Nav 0.3.0 is another consumer. Shared merchant offers, stock condition,
maintenance definitions, actual repair-lot residue and equipment save upgrades are
implemented in 0.6.0; recipe packs can require reusable tools. See the current
[economy and maintenance report](equipment-economy.md). Historical timings below
describe the earlier prototype; the current report supersedes them.

## Earlier independent construction baseline

Framework packaging hotfix **0.2.1** (2026-09-24): the first game startup loaded
Framework 0.2.0 and registered both Shipbreaker construction recipes, but the
native mod menu reported Framework as Missing because its package had no `data`
directory. Native `DataHandler.LoadMod` requires that directory even for a library
with runtime registration. A tracked empty conditions array now ensures the folder
survives packaging and installation. Installer preflight requires that file, with
a regression check for rejection before any copying. Shipbreaker remains 0.2.0;
no saved identities or gameplay rules changed. The owner confirmed the Missing
status was fixed after restarting with 0.2.1. This verifies the menu correction,
not construction, processing or saved-job migration.

The standalone replacement is implemented and packaged for owner testing:

- Phobos Framework owns opt-in construction registration, exact material
  selection, mass checks, completion gating, legacy Phobos action lookup,
  definition publication and the existing inventory/production helpers.
- Shipbreaker authors its machine states, feed, slots, power, install/uninstall,
  repair and damage definitions against native game interfaces. It no longer
  clones SWB machinery or requires OCF/SWB at startup.
- Native installed **Bar Table** (`ItmTable01`) and **Dining Table** (`ItmTable02`)
  provide construction. Their loose forms occur in native stock/loot. The native
  **Workbench** (`ItmWorkbench01`) is defined but was found only in object/item
  data, so normal acquisition is unconfirmed. It is an optional surface. An
  installed `SWB_WorkbenchInstalled` is also supported when present.
- Phobos Framework is the only mod dependency of this Shipbreaker candidate;
  BepInEx and Ostranauts are required. Auto Nav remains optional for the current
  onboard processor; future positioning will have a separate integration.
- The installer selects one shared provider automatically, requires Framework
  0.2.0+, and leaves unrelated Workshop packages and load-order entries intact.

Build/offline evidence is not gameplay verification. The owner's installed
Shipbreaker was updated to 0.2.0 alongside Framework 0.2.0 and Auto Nav 0.1.1 on
2026-09-24. The shared installer verified 39 matching files and enabled native
entries; the game was not launched and no saves or player settings were accessed.
New construction, optional-bench coexistence and loading old Phobos
objects/queues still need owner-run gameplay evaluation. The current suite uses
ordinary saves; the historical test-save wording is not a current restriction.

## Saved identity and construction ownership

Fixture variants, section/residue IDs, feed slot/bin IDs, six installable IDs,
footprints, storage sizes, masses, damage limits and processing revision/progress
remain stable. The input bill is unchanged. The original independent prototype
used 120 seconds per section and 60 seconds for assembly; **0.6.0 supersedes those
timings with 60 minutes per section and 30 minutes for final assembly**. See
[current equipment work times](equipment-economy.md) and the
[player guide](player-guide.md).

Active recipes live in `framework/recipes.json`, explicitly loaded by Shipbreaker.
The former `crafting/recipes.json` remains as an **empty migration stub**. The
installer backs up and replaces that owned file through its ordinary update path.
An OCF installation scanning it sees no Phobos recipes; no file deletion is needed.
Do not restore the old recipe contents into a 0.2.0 installation.

Only these historical actions translate during native interaction lookup:

| Historical action | Active action |
| --- | --- |
| `OCF_Craft_PhobosBuildShipbreakerSection` | `PhobosCraft_PhobosBuildShipbreakerSection` |
| `OCF_Craft_PhobosBuildShipbreaker` | `PhobosCraft_PhobosBuildShipbreaker` |

Lookup creates a private copy of the saved interaction DTO, changing the action
and chain-start names while retaining actors, targets and material references.
It does not rewrite files. No active alias retains OCF's prefix, so OCF's completion
hook does not take ownership. An old provider registering either action before
or after our pack blocks our construction readiness rather than permitting two
providers. Other authors' recipes and queues are not translated.

A queued job still needs its original station. This migration does not preserve
foreign Workshop objects if their mod is removed. Keep OCF/SWB installed for
existing saves containing their equipment or other consumers. Test independence
in a new separate save, not by removing those mods from the player's ship.

## Scope and limits

Construction uses the native fetching/action/output path, with exact identities,
arrived-material validation and explicit mass checks. Its completion gate blocks
repeated effects on the same live interaction after effects begin. Native effects
are **not transactional**: a crash or exception after material removal may leave
partial results. The gate is session state, not a persistent crash journal. Stop
and inspect the reported state on a native completion fault; do not claim
crash-safe construction or missing-provider recovery.

Processing delivery remains Shipbreaker's existing staged output/input handling,
using the shared `BatchDelivery` service. Do not conflate that service with native
construction's completion semantics.

Licensed OCF construction patterns were adapted with attribution and the complete
notice in the Framework package. No upstream binary, game-derived definition blob
or extracted artwork is distributed. See the [author guide](framework-author-guide.md)
and the packages' `THIRD-PARTY.md` files.

## Current material transport boundary

The collector implements one explicitly selected output-to-port connection.
Actual residue remains in the sender during its short clock; the checked move
preserves identity and mass. Full storage waits and a broken route pauses.
Competing crew removal invalidates the pending item, rather than imposing a
global crew reservation. Reload retains physical cargo and endpoint pairs, clears
the short timer and requires the player to press Start. Invalid/missing endpoints
pause the route without reassignment. Explicit Unlink is available at either end.
See [saved material-port pairing](material-port-pairing.md).

The structural-floor route checks continuity across the ship; walls, gaps and
exterior webbing cannot bridge it. This is an abstract underfloor service route,
not placed belt segments or a general conveyor network. Collection remains ship
cargo. Persistent release into space is still research. See
[underfloor transport](underfloor-material-transport.md).

Version 0.8.0 also supplies shared immutable processing recipes and saved-job
binding for the wall fixture and [scrap reclaimer](scrap-reclaimer.md). Content
keeps material identities, heat handling, artwork and balance.

Version 0.10.0 supplies `Controls.ConsoleBinding`, typed `EquipmentActivity`
and `PanelWidgets` for the [industrial console](industrial-console-player-guide.md)
and local equipment panels. Binding checks use freshly resolved console, ship,
operator and ownership facts on each command. Movement or changed operator ends
the session; temporarily unavailable power does not grant permission or erase work.
Shipbreaker owns native access resolution, snapshots, discovery and command rules.
No generic third-party equipment registry or new control network is claimed.
Physical inventories remain local. PDA/visor connections are documented only.
