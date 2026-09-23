# Dependency maintenance and fallback plan

Owner preference, **2026-09-23**: prepare for dependencies that stay out-of-date,
using documentation instead of speculative replacement code where sufficient.
Replacement and migration options below remain plans. Shipbreaker **0.1.2** now
implements the early compatibility checks and staged registration described below;
it does not implement a missing-dependency save-recovery mode.

## When to act

An old release that still works is usable. Release age alone must not disable
features or trigger a fork. Investigate when a game/mod update breaks something
we use, a required download disappears, or an unsupported interface blocks a
concrete feature or release.

Record the first confirmed blocking failure. If unresolved for **30 days**, review
the options at the next relevant development session. This is a planning
checkpoint, not an automatic deadline, a declaration of abandonment, or a reason
to delay a useful fix. Review sooner for a release blocker or an announced end
of support. Check for upstream fixes or a maintained continuation during that
review; no recurring release-date monitor is needed now.

## Preferred response, in increasing maintenance cost

1. **Retain a working combination where available.** Record game, loader, Phobos
   and dependency versions/hashes, configuration and native load order. Keep
   permitted private copies of mod packages outside Git where available. Record
   whether restoration is actually possible; do not promise access to old game
   builds or packages. Workshop required items do not pin a dependency version.
   Call a combination "known working" only after the relevant gameplay check.
2. **Fix our integration narrowly.** Adapt a recipe limit, API call or template
   reference. Scope changes to Phobos content and document the supported
   combinations. Avoid rebuilding the dependency's whole system or bypassing
   missing functionality with free inputs, production or unsafe control.
3. **Use a maintained successor or manual fallback.** Verify the actual interfaces
   and saved identities. Manual inventory handling is sufficient if an optional
   hauling helper fails. A required content provider needs compatible definitions
   or a tested migration before replacement.
4. **Consider a small compatibility fork.** Check for an existing continuation
   first. Preserve authorship, provenance, notices and the record of our changes.
   Keep the owner's permissive working assumption separate from verified terms;
   follow explicit restrictions and game-asset exclusions. Do not silently bundle
   dependency DLLs or load competing original/fork providers of the same IDs.
5. **Reduce scope or retire the feature** if upkeep outweighs gameplay value.
   Preserve a usable release or a tested transition out instead of maintaining an
   entire abandoned framework for one machine.

This policy performs no installations, backups, automatic downgrades, upstream
contact or public releases. Those remain separate actions under existing project
instructions.

## Current dependencies and specific alternatives

Reference versions: Ostranauts **1.0.1.4**, BepInEx **5.4.23.5**, Crafting Framework
and Salvage Workshop **0.8.71**. Phobos Auto Nav **0.1.1** and Shipbreaker **0.1.2** have build
and offline-check evidence, **not an in-game-verified rollback baseline**.

| Dependency | What we need | Contingency if the relevant functionality remains broken |
| --- | --- | --- |
| Crafting Framework: required by Shipbreaker | Recipe registration, ingredient handling and construction outputs | Adapt our recipe/API use first, then prefer a compatible continuation. If construction is the only irreparable dependency, assess a small native construction path for our sections and fixture. Deleting the current hard dependency alone would not provide construction. |
| Salvage Workshop: required by Shipbreaker | Workbench and runtime-cloned sorter, slots, power, installation and repair templates | Adapt the changed templates first. If necessary, author only the fixture definitions and construction-station integration we need, keeping Phobos IDs stable. Do not reproduce its wider sorting, crafting or battery systems. |
| Common Sense hauling/manifest: optional companions | Crew transport and inventory visibility; no Phobos assembly dependency | Retain manual loading, unloading and native inventory access. Disable or revise a failing optional integration when one exists; no replacement hauling framework is required. |
| Ship's Water: optional upstream Workshop integration | Existing water-consuming workshop recipes; our panel process uses no water | Retain the upstream water-ration path where supported. Do not treat an absent provider as free water/cooling. Any future Phobos coolant feature needs its own dependency decision. |
| Auto Navigate: adapted provenance, **not a runtime dependency** | Selected guidance already belongs to our standalone adaptation | Upstream inactivity does not prevent our package loading. Maintain the adapted code against game changes, track useful upstream fixes and preserve attribution. Existing terms uncertainty remains in `THIRD_PARTY_NOTICES.md`; no new fork is needed solely because upstream is inactive. |
| Game APIs and BepInEx/Harmony: required foundations | Loading, patches, inventory, power and navigation | Address the specific changed signatures/behaviour, or retain a verified compatible installation where possible. Suspend an unsupported feature release if necessary; changing a version requirement alone cannot restore functionality. |

## Existing safeguards and their limits

- Shipbreaker declares Crafting Framework **0.8.71 minimum**. This is not an upper
  compatibility bound or certification of every later version. If the loader
  rejects the dependency, our plugin and its diagnostic UI may never start;
  the loader log is then the available diagnostic.
- `WorkshopAdapter` is the explicit dependency boundary: it identifies the loaded
  providers, checks required definitions and the machine/storage/power relationships
  used by this version, and translates private copies into Phobos definitions.
  `Content.Register` modifies those copies before a single publication transaction.
  A preparation failure changes no game dictionary; a publication failure restores
  old entries and removes new entries. Recovery errors are surfaced explicitly and
  processing remains disabled. This does not roll back the game's subsequent
  native generation or arbitrary changes made by another mod.
- Both Phobos construction recipes must be present after the Framework/native
  loading phase before `Content.Ready` allows processing. Complete item definitions
  remain registered if this later recipe check fails. That check does not remove
  partially registered upstream crafting actions or implement alternative crafting.
- `phobosshipbreaker dependencies` reports the startup version/data/template and
  recipe checks without selecting a machine or mutating game state. It does not
  hot-reload dependencies. Offline checks cover changed/missing contracts, partial
  recipe registration and publication rollback; live compatibility is still pending.
  Definition checks cannot certify every behavioural change in a future release.
- An independent inert-content loader, automatic dependency substitution and
  missing-dependency save recovery **do not exist**. Our runtime-generated fixture
  definitions need the plugin and Workshop. Preventing processing does not ensure
  an existing save can load safely without its content providers.

## Preserve saved ships during a transition

Do not recommend removing a required provider and loading an affected save as a
generic remedy. Keep the compatible content set until a transition is ready.
Restoring old binaries onto a save written by a newer combination is unverified;
use a matching earlier save or a deliberately tested migration. The owner retains
control of real-save operations.

A replacement release must preserve, or explicitly migrate, Phobos fixture,
assembly-section and residue IDs; feed/output slots and contents; physical sizes
and masses; and saved panel progress, duration and recipe revision. Transition
with processing paused and navigation disarmed. Never silently discard objects,
refund spent energy, finish unearned work or resume thrust.

If full functionality cannot be retained, consider an inert compatibility package
that preserves saved definitions and lets items be stored or recovered through
ordinary gameplay. This is a possible future migration, not a current rescue tool.

Use a separate test save for changed contracts and save transitions. Focus checks
on the concrete failure, item/inventory survival, material accounting and paused
or disarmed state as relevant. Do not repeat established upstream power/hauling
proof tests solely because a dependency changed version.

## Record only what helps the next decision

For a real blocker, note the dependency/version/hash, game/Phobos versions, first
observed date, concrete failure, affected feature, last verified working combination
(or "none yet"), upstream/successor status, chosen workaround, save impact and
focused verification. Add the next decision trigger. A short entry in the relevant
build guide or issue is enough; no separate tracking service or speculative
replacement framework is warranted now.
