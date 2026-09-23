# Phobos Framework: independent foundation

Decision: 24 September 2026. The owner selected our own shareable framework
**instead of Ostranauts Crafting Framework**, and authorised full independence.
The purpose is reusable services for equipment and future material transport,
not reproducing every feature of OCF or turning Ostranauts into a factory game.

## Current candidate: Framework and Shipbreaker 0.2.0

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
objects/queues still need owner-run testing in a separate save.

## Saved identity and construction ownership

Fixture variants, section/residue IDs, feed slot/bin IDs, six installable IDs,
footprints, storage sizes, masses, damage limits and processing revision/progress
remain stable. The two construction stages retain their input bill and work
requirements (120 seconds per section, then 60 seconds for assembly).

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
and inspect the test save on a reported native completion fault; do not claim
crash-safe construction or missing-provider recovery.

Processing delivery remains Shipbreaker's existing staged output/input handling,
using the shared `BatchDelivery` service. Do not conflate that service with native
construction's completion semantics.

Licensed OCF construction patterns were adapted with attribution and the complete
notice in the Framework package. No upstream binary, game-derived definition blob
or extracted artwork is distributed. See the [author guide](framework-author-guide.md)
and the packages' `THIRD-PARTY.md` files.

## Next shared feature: physical material transport

Conveyor/underfloor transport is **not implemented**. The first useful connection
is one Shipbreaker output and one receiving port on the same ship. Move the
actual item, retaining identity, mass, conditions and contents; do not destroy it
and spawn a replacement. Keep pending cargo in the sender, reserve against crew
handling, revalidate route/power/capacity at completion, and release reservations
on interruption or reload. Full storage and invalid links must pause safely.

Matching ship IDs alone does not establish structural continuity across severed
hull sections. Define that rule and finite port capacity/transfer rate before
implementation. Port concept artwork does not establish a pressure boundary or
an implemented conveyor network. See [underfloor transport](underfloor-material-transport.md).
