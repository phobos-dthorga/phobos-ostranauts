# Dependency maintenance and fallback plan

**25 September autonomous-reclamation direction:** the owner requires Auto Nav
for the future Shipbreaker implementation, using existing N1/N2 hardware. The
[handover](shipbreaker-autopilot-handover.md#mandatory-dependency-delivery) covers
loader/native enablement, package order, installer preflight and historical
package compatibility. This research round changes no installed/current dependency.
Do not build a second flight controller if Auto Nav is unavailable, and do not
remove required providers from saves. Auto Nav's existing upstream provenance
hold remains applicable to the combined release.

**24 September 2026:** Framework and Shipbreaker 0.2.0 implement the authorised
independent construction/machinery candidate. OCF/SWB are no longer required by
this version. See [migration and verification limits](phobos-framework.md).

Owner preference: plan for extended dependency incompatibility, documenting
inexpensive fallbacks before implementing speculative replacement systems. The
0.2.0 architecture change was explicitly requested, not triggered by release age.
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

Reference build: Ostranauts **1.0.1.4**, BepInEx **5.4.23.5**. Current candidate:
Phobos Framework/Shipbreaker **0.2.0**; owner-installed Shipbreaker **0.1.4** had
successful startup/art feedback. Neither is an in-game-verified processing or
rollback baseline. OCF/SWB **0.8.71** supplied inspected precedent and licensed
construction code, but are optional companions now.

| Dependency | Current role | Concrete fallback |
| --- | --- | --- |
| Phobos Framework 0.2.0+ | Required: construction, definition publication, inventory/delivery helpers | Maintain a single compatible provider, preserve APIs or migrate consumers together. No silent downgrade. Fix common faults once. |
| OCF / Salvage Workshop | Optional bench and other installed content; required by our legacy pre-0.2 versions | Our independent path uses native tables and Phobos definitions. Keep these providers for foreign objects/other consumers in existing saves. Do not broadly remap foreign IDs. |
| Common Sense hauling/manifest | Optional crew handling/visibility | Manual loading and native inventories remain available. |
| Ship's Water | Optional upstream Workshop recipes; unused by this panel process | Any future Phobos coolant path must define real inputs, not assume free water. |
| Auto Navigate | Provenance for our separate adaptation, not a runtime requirement | Maintain our adapted guidance against native changes and preserve attribution. |
| Game/BepInEx/Harmony | Required native APIs and loading | Repair the changed integration or retain an available verified combination. Changing version declarations does not restore functionality. |

## Existing safeguards and limits

- BepInEx enforces Phobos Framework's minimum version. If dependency loading fails,
  the loader log is the diagnostic path; Shipbreaker's commands may not start.
- `NativeAdapter` / `DependencyContract` check the native interfaces used, while
  `MachineDefinitions` authors our content with stable IDs. Private preparation
  precedes publication; definition transactions restore prior entries on failure
  and report recovery faults. They cannot roll back arbitrary later game/mod work.
- Construction packs publish atomically, validate masses and exact items, retain
  optional station menus, and reject competing ownership of our historical actions.
  Native crafting effects themselves are not atomic; replay protection is in memory.
  A partial native failure or crash requires inspection, not an automatic retry.
- Both construction stages and provider readiness must pass before processing is
  enabled. If recipe registration fails, complete item definitions stay available,
  but this is not a missing-provider save-recovery guarantee.
- `phobosshipbreaker dependencies` reports provider/native/recipe status without
  mutating game state. It does not hot-reload mods or certify future versions.
- Machinery still needs our plugin and shared provider to recreate its native
  definitions. An inert compatibility loader does not exist. Removing required
  content from a save is not supported.
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
