# Phobos Manufacturing: scaffold and implementation plan

25 September 2026. **Version 0.0.1 is a research scaffold, not a usable machine.**
It supplies native mod metadata, a BepInEx entry point and embedded English
localization through Framework. Startup reports that no equipment, stock or
recipes are registered. It has no simulation tick, game patches, equipment
provider or persistent state. The first operational version is proposed as 0.1.0.

Read [first-round research](manufacturing-research.md) for primary sources,
stock/energy budgets and the proposed state contract, and the
[original handover](manufacturing-handover.md) for owner direction. The research
recommends one enclosed mill and two heat sinks per batch; these are designs,
not already available equipment.

## Established layout

| Repository location | Purpose now |
| --- | --- |
| `src/PhobosManufacturing/` | Buildable `netstandard2.1` plugin; only Framework is a required mod dependency |
| `mods/PhobosManufacturing/` | Version/author metadata and separate third-party provenance |
| `mods/PhobosManufacturing/data/conditions/` | Tracked empty native array preserving the required `data` directory |
| `mods/PhobosManufacturing/framework/` | Registration notes; no dummy recipes or prematurely registered item IDs |
| `translations/PhobosManufacturing/` | UTF-8 English catalog, also embedded in the assembly |
| `assets/phobos-manufacturing/` | Machine/workpiece brief and separate provenance policy |
| `assets/phobos-manufacturing/source/` | Reserved for original registered masters; no commissioned artwork |
| `assets/phobos-manufacturing/previews/` | Reserved for native-scale original-art reviews |
| `tests/PhobosManufacturing.Tests/` | Focused acceptance plan; no empty test runner pretending to test machinery |
| `scripts/build-manufacturing.ps1` | Existing Framework build/checks plus scaffold build and shared packaging |
| `scripts/audit-manufacturing.py` | Repeatable read-only native repair/tool/stock audit |
| `.local/manufacturing-research/` | Ignored generated local audit evidence; never packaged |

Create native images and definition subfolders when their first real assets
exist. No independent repository, loader, generic framework or OCF migration
stub is needed. Stable plugin/package identity is
`phobosgekko.ostranauts.manufacturing` / `PhobosManufacturing`; proposed equipment
IDs remain unregistered pending the actual useful slice.

## Build and package

From the repository root, with PowerShell 7 and the local game/loader available:

```powershell
./scripts/build-manufacturing.ps1 -OstranautsPath '<local game folder>'
python ./scripts/audit-manufacturing.py --game-root '<local game folder>'
```

Use the same Python interpreter as the other repository audits. Redirect the
second command to an ignored `.local/manufacturing-research/` file to retain
evidence. Machine-specific paths are never committed.

The script prepares `dist/PhobosManufacturing-P0.zip` using
`New-PhobosPackage`, including the shared player guides, this record, research,
handover and asset brief. The package layout is:

```text
BepInEx/plugins/PhobosManufacturing/
  PhobosManufacturing.dll
  translations/en.json
Mods/PhobosManufacturing/
  mod_info.json
  data/conditions/phobos_manufacturing.json
  framework/README.md
  THIRD-PARTY.md
README.md
manufacturing-implementation.md
manufacturing-research.md
manufacturing-handover.md
manufacturing-art-brief.md
player-guide.md and shared guide files
LICENSE / THIRD-PARTY.md
```

Framework is built/prepared separately; its DLL is not copied into this content
package. Game, Unity, BepInEx and foreign mod binaries are not distributed.
Framework's own adaptation/licensing notices remain with that dependency.

**No installation is part of this round.** The current shared installer does not
select Manufacturing. Before delivering operational content, add an explicit
`Manufacturing` selection with Framework 0.17.0 minimum, required-file checks,
backup/load-order handling and installer tests. Keep existing defaults and the
game-closed guard. Use `install-mods.ps1` for that later delivery; do not copy
the scaffold into the game manually. Agriculture remains excluded from installation.

## Physical layout proposal

The M4 has a 4 × 4 floor footprint, intact/loose/damaged forms and a front aisle.
The design does not require a hull breach, exterior radiator or a pipe network.
Position and dimensions are authored gameplay choices and need native placement
checks before the artwork brief becomes a production manifest.

```text
             rear / local +Y
      +-----------------------+
      | spindle / axes        |
      | captive twin preform  |  4 tiles
      | jaws / capture path   |
      | kit door / controls   |
      +-----------------------+
          4 tiles wide
          front / local -Y
       clear crew access aisle
```

Show one native electrical connection and a clearly distinct service-cartridge
door. Keep incoming stock, captive process contents and released products
separate even if the exterior uses one loading hatch. The 4 × 4-cell output
tray and mass limits are inventory proposals, not assumed placement success.
Inspect exact output sizes with `BatchPlacement` and native admission before
committing them. All rotations share the same operator/collision layout.

## Native-widget panel sketch

Use Framework's existing runtime native instrument adapters, with plain controls
as the supported fallback. Do not copy extracted reactor artwork into the mod
or clone its controller. Full and compact panels present the same snapshot;
all labels/messages are localized, and the machinery art contains no labels.

```text
Phobos' Rivetline M4 Enclosed Machining Centre
<name / full ID / authorized host ship>

Status: Paused — explicit resume required
Job: Replacement heat sinks      Revision: 1
Stock: <bound ID / retained mass> Cartridge: <fresh / bound / spent>
Enclosure: <checked state>        Fixture: <checked state>

Received power: <kW / Unknown>   Machine temperature: <°C / Unknown>
Cut progress: <paid s / captured duration>
Cooling: <room scope / waiting reason>
Outputs: 2 heat sinks + 1 spent cartridge
Next crew step: <load / setup / inspect / release>

[Load / setup locally] [Inspect locally] [Release locally]
[Resume] [Pause] [Details]
                         [STOP / ISOLATE MOTION — always visible]
```

Power is an observed receipt-derived rate, not the configured rating; no fake
spindle RPM or precision gauge. The built-in temperature sensor reads the
content-owned process store only after a valid update. A failed/stale probe
shows Unknown while heat remains physical. Mark source, subject, ship and time
through Framework observation conventions. Physical local buttons can navigate
to ordinary crew actions; they must not instantly execute crew work.

Proposed F3 namespace: `phobosmanufacturing`, with `help`, `list`, `status`,
`controls`, `resume` and `pause` targeting full IDs. These commands are **not
implemented by 0.0.1**. A later optional C1 provider uses the same checked
service; local handling remains local and Shipbreaker remains optional.

## Concrete implementation order

1. **Define one real machine and standalone supply.** Finalize registered
   stock geometry, protected native containers, fresh/spent kit identities,
   finished-sink flag and additive merchant entries. Require Framework only.
   Use content-owned `EquipmentNames` metadata for every variant. Keep native
   acquisition/maintenance present; machine assembly can wait for justified
   precision subassemblies.
2. **Add content rules and state.** Implement the research phase table with
   captured recipe/rating/duration, two exact input IDs, finite heat and explicit
   Resume. Reuse fixed-job and object-store helpers without increasing their
   limits. Started stock and kit cannot escape via hauling, native crafting,
   trade, damage switching or uninstall. Code UI separately from mutations.
3. **Connect measured power and heat.** Adapt the existing native receipt path
   and prove there is only one debit/heat allocation per interval. Handle partial
   supply, last-tick excess, finite room heat, stale probes, power loss and flight
   preemption. Preserve heat independently of sensors and permission.
4. **Complete native crew setup/inspection and guarded delivery.** Use native
   reachable crew/tool work. Plan every output before consumption, record an
   in-progress commit guard and protect ambiguous multi-input consumption faults.
   A completed job with blocked output stays complete and cannot consume again.
5. **Expose local/F3 controls, then optional C1.** Register an equipment provider
   for Manufacturing definitions; read-only snapshots and fresh authority checks
   on all commands. No Shipbreaker assembly reference or hard dependency.
6. **Produce the smallest useful original-art pilot.** Follow the asset brief
   and updated layered-art policy: a ChatGPT base when useful, PixelLab for
   simpler workpieces/state layers, and current allowance/cost checks. Preserve registered masters, crop/pivot,
   normals/damage alignment and deterministic native-size derivatives. Owner
   visual review remains separate from code checks.
7. **Prepare explicit delivery and owner checks.** Add installer support and
   a genuine player guide once the machine is operational. Verify independent
   acquisition and native scrubber repair gathering, blocked release, pause/reload,
   damages/maintenance and actual room heating. Build is not gameplay validation.
8. **Add optional F6 stock only afterward.** Shipbreaker owns the casting recipe
   and historic housing migration; Manufacturing owns stock. Verify provider
   presence, exact masses, all output packing and an unchanged old housing job.

## Verification record and remaining uncertainty

The read-only native audit reproduced the handover's assembly fingerprint,
252 Repair definitions, 68 sink-consuming definitions and the 8 kg / $114.20
scrubber bill. Native tool flags and all four existing merchant item tables
were checked. This establishes definition evidence, not live repair gathering.

The 0.0.1 scaffold and Framework built with **zero warnings/errors**. The existing
Framework runner passed **1,954 public-assembly/recovery checks**, and the
performance adapter runner passed **35 checks**. The Manufacturing package was
prepared through the shared helper; these results do not test machining or prove
game startup. The current repository has unrelated Agriculture and furnace
research edits; those remain in place and are not a Manufacturing checkpoint or
installation request.

There is no operational machining test to run yet. Key unresolved runtime
connections are protected two-input native inventory, fixture geometry, native
crew interactions, one-time thermal accounting and native acceptance of the new
sink. The research choices concerning cartridge consumption, batch size, price
and speed remain candidates for owner gameplay feedback.
