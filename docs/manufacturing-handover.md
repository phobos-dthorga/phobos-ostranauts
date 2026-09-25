# Phobos Manufacturing: handover for a separate task

Owner decision: 25 September 2026. **Approved direction; research and mod creation
are authorized. No Manufacturing runtime, equipment or finished artwork exists yet.**
This handover is the first deliverable; continue the work in a separate task in
this repository. Do not create another repository or change its private visibility.

## Request to carry into the new task

Create **Phobos Manufacturing**, a separate Ostranauts content mod for dedicated
machining equipment and finished replacement parts. Begin with research for one
enclosed milling machine/machining centre and a useful heat-sink finishing job.
Use **Phobos Framework as the only required mod dependency**, in addition to the
normal game/BepInEx loader prerequisites. **Do not depend on Ostranauts Crafting
Framework, Salvage Workshop or their associated content.** The owner explicitly
chose an independent direction. Shipbreaker integration is optional, as are
integrations with other Phobos content mods.

Research the first useful slice, resolve its material/energy/equipment contracts,
and establish the mod using our existing conventions. Do not substitute an
ordinary table recipe for dedicated machining. Avoid building a catalogue of
machines or a second general-purpose framework before the first job is defined.
Save owner-only visual approval and gameplay checks for the owner.

## Agreed division of responsibility

| Owner | Responsibility |
| --- | --- |
| Shipbreaker | Salvage, recovery, furnace operation and rough casting processes |
| Manufacturing | Machine tools, machining/finishing recipes, tooling requirements and finished components |
| Phobos Framework | Shared power accounting, physical inventories/transfers, persistent jobs, controls, access and integration services when concretely needed |

The intended chain is **recovered metal → furnace casting → machining → usable
replacement part**. Manufacturing must also have a credible standalone source of
suitable cold stock, such as purchased stock or an audited salvage input. Do not
make ownership of an F6 furnace a hidden prerequisite. The exact stock source is
research to resolve, not an already implemented native supply route.

Settle one authoritative provider for each new stock/blank/finished-item ID before
registration. If Shipbreaker supplies a blank, enable its Manufacturing recipe
only when that provider is present. If Manufacturing owns a common stock item,
Shipbreaker's optional casting adapter may produce it only when registered.
Do not declare the same item twice, create circular hard dependencies, or leave
recipes with missing output/input definitions when a mod is absent. Do not put
content-specific commodities into Framework merely to bypass ownership decisions.

The first machine should face mounting surfaces, drill mounting holes, separate
pieces and perform the finishing needed for the selected heat sink. An enclosure
gives chips/swarf a contained destination aboard a spacecraft. Later lathes need
their own useful turned outputs, such as a shaft, sleeve or bush; none of those
outputs has been researched or authorized as the first slice.

Detailed CNC programming can remain abstracted. Material consumption, crew
setup/handling, power, retained waste, tool maintenance, blocked outputs and
interruption should have meaningful effects. Do not impose a complex coolant
network before deciding whether the first operation needs one.

## What already exists

Read `AGENTS.md` first, followed by:

- [Framework architecture](phobos-framework.md) and [author guide](framework-author-guide.md).
- [F6 operating guide](furnace-player-guide.md), [repair-casting research](furnace-repair-castings.md)
  and [future material-routing design](furnace-material-routing.md).
- [Equipment economics](equipment-economy.md), [branding](equipment-branding.md),
  [localization](localization.md), [asset-generation policy](asset-generation-policy.md)
  and [resolution policy](artwork-resolution-policy.md).
- [Residue contract](residue-material-contract.md) and
  [saved processing jobs](processing-job-compatibility.md).

Installed and file-verified in the owner's game on 25 September:
**Framework 0.17.0, Shipbreaker 0.14.0, Auto Nav 0.10.1**. This was an installation
check of 94 files plus load order, not an in-game test. Agriculture was explicitly
excluded from installation; its separate task is still preparing changes.

The F6 is implemented at 6 × 6 tiles, 240 kg empty, a 50 kg chamber rating and
250 kW delivered heat. Its only current batch is twenty exact 1 kg aluminium
scrap objects, producing a 19 kg housing blank and 1 kg terminal melt remainder.
Existing bench finishing produces an 18 kg D4/R4 construction housing plus a
1 kg offcut. Preserve that already delivered recipe and its saved identities;
this decision changes the **new heat-sink direction**, not existing player cargo.

F6-R and F6-P are alternative finite cooling assemblies. The cycle retains heat,
chamber gas and exact feed IDs, pauses heating after reload, and requires explicit
handling/release. Do not change those contracts to accommodate machining.

The two latest studies are research only: replacement heat sinks are not yet
produced, and furnace feed/output automation is not implemented. No manufacturing
capability should be inferred from their diagrams or proposed IDs.

## Useful findings to carry forward

Blue Bottle Games' native `ItmHeatSink01` is a **1.5 kg**, **$27.50 base-value**
passive aluminium-or-copper heat exchanger whose description explicitly mentions
an integrally cast standardized mounting pattern. `TIsHeatSink` tests `IsHeatSink`.
The read-only audit found 68 heat-sink-consuming definitions among 252 definitions
using the standard broken-item Repair templates; these include installed/loose
variants, not 68 distinct machine families.

Priority consumer: native CO₂ scrubber repair. `AtmoScrubber01DmgRepair` requires
one motor, one sink, one motherboard, two small mechanical parts, one small
electronic part and two steel scrap units: **8 kg / $114.20 native base value**.
Other direct consumers include cabin coolers, heaters and maintenance tools.
A replacement sink does not remove the need for motors, electronics, sorbents
or other inputs. Native Restore is separate from broken-item Repair.

The previous proposed budget was:

| Operation | Proposed accounting |
| --- | --- |
| Cast | 20 kg aluminium → 19 kg rough cluster + 1 kg terminal remainder |
| Finish | 19 kg cluster → twelve 1.5 kg sinks + 1 kg recoverable machining material |

That is **authored preliminary balance**, not verified machining yield. Reassess
clamping stock, gates, kerf, mounting faces, chip collection and any lubricant
contamination. Separate recoverable clean metal from contaminated waste. Do not
silently turn oily swarf into clean native scrap. Revisit price, work, tooling and
energy alongside the chosen machine. Existing 20 kg housing outputs stay fixed.

The former proposal for 1,800 work-seconds at Bar/Dining Tables, its ordinary
Mortorq/welding finishing assumptions and optional Workshop station support is
**superseded for the new heat sinks**. It must not become the implementation by
default. Previously suggested `PhobosFurnaceHeatSink*` IDs were never registered;
choose IDs reflecting their agreed owning mod before implementing them.

Sources are local installed game data at `data/condowners/condowners.json`,
`data/condtrigs/condtrigs.json`, and `data/installables/installables_repair.json`.
Attribute the game to [Blue Bottle Games](https://store.steampowered.com/developer/bluebottlegames/).
The audited `Assembly-CSharp.dll` SHA-256 is
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
Reproduce the focused audit with repository script `scripts/audit-furnace-repair.py`
and a locally resolved `--game-root`; retain generated evidence in ignored `.local/`.

NASA's [On-Demand Additive Manufacturing for Deep Space brief](https://www.nasa.gov/wp-content/uploads/2024/09/27-on-demand-manufacturing-spec-sheet-508.pdf)
(hosted September 2024, prepared by Secor Strategies) supports treating fabrication,
machining/joining and inspection as distinct requirements for usable spares. It
does not establish our casting yield, machine dimensions, power, work time or
part qualification. Name NASA/ESA/original authors explicitly beside supporting
claims; distinguish scientific evidence, observed game behavior and authored
balance. No institutional endorsement is implied.

Salvage Workshop findings in the earlier study are comparative history only.
The new mod must neither require that provider nor copy its code/artwork or
commandeer its machinery. Do not uninstall or modify another author's installed
mods as part of establishing our independence.

## Research deliverables for the first round

1. Recommend one machine and operation sequence. Audit real machining precedents,
   stock workholding, microgravity containment, crew access and footprint. Compare
   an enclosed mill/compact machining centre against a lathe for the actual part.
2. Define standalone stock acquisition and optional F6 casting input, one finished
   native-compatible sink, every remainder and the limits of alloy/fit assumptions.
   Audit actual tools, prices, work and downstream native repair acceptance.
3. Budget electrical demand, motor/spindle heat, cutting work, chip/swarf storage
   and tool wear. Resolve dry machining versus finite lubricant/coolant for this
   operation; record a justified first approximation rather than inventing fluids.
4. Specify load/clamp → setup → machine → inspect/deburr → release, with physical
   workpiece retention, partial power, loss of access, damaged equipment, output
   blocking, save/reload and explicit resume. No saved permission grants free work.
5. Map concrete reuse from Framework: `Processing`, `Inventory`, `Persistence`,
   `Controls`, `Registration`, measured electrical receipts and equipment providers.
   Determine whether its fixed-job limits fit; do not alter those limits merely
   to accommodate an unchosen machine. Optional C1 access uses checked providers.
6. Prepare an installation layout, native-widget panel sketch and small asset
   brief. Keep labels/readings localized and live. Plan machine/workpiece layers
   and registered exports so later recipes do not require repainting the machine.
7. Establish the `PhobosManufacturing` mod scaffold using existing build/package
   conventions once the owning namespaces and first slice are clear. Keep research
   status explicit; a scaffold is not operational machining. Record the proposed
   version, dependencies, acquisition, construction and validation plan.

Research first, then implement the defined useful slice under the owner's creation
authorization where evidence supports it. Surface material design decisions or
unsupported native connections before committing to costly additions. Do not
build every potential machine, crop tie-in or pipe system mentioned as a future use.

## Delivery and working constraints

- Keep costs proportionate. Use one task, existing scripts and focused inspection;
  no subagents unless explicitly authorized. No speculative shared framework.
- Use the owner's complementary artwork workflow: ChatGPT may create a
  high-resolution machine/furniture base while PixelLab supplies separate simple
  workpiece, fitting and state sprites. Keep masters, attachment positions and
  native exports registered; match the composite's pixel density and lighting.
  Check allowance/cost before generation, use the smallest useful pilot, preserve
  prompts/IDs/masters, and avoid paid-credit purchases or costlier fallback without
  owner input. No mandatory two-provider pass; research sketches need not trigger
  raster generation. Follow `docs/asset-generation-policy.md`.
- Reuse native UI at runtime. Never commit extracted game assets, assemblies,
  decompiled source, saves, credentials or personal machine paths. Keep source
  attribution separate from art provenance and licensing.
- Preserve the `Phobos'` equipment-name prefix and original industrial branding.
  Rivetline is an available family; no Manufacturing machine model is approved yet.
- UI only presents state and dispatches to checked services. Missing instrument
  readings are Unknown; physical heat/material persists independently of probes.
- The repository has concurrent Agriculture edits and furnace research changes.
  Inspect current status, preserve other work, and avoid indiscriminate staging.
  The last all-mod checkpoint was `e9d993c`; later research/branding may be local.
- Use ordinary private-repository commits/direct pushes only when requested;
  no PR or visibility change. Do not assume this handover authorizes another
  all-work checkpoint, installation, or migration of already installed content.
- Installation uses `scripts/install-mods.ps1`, only with the game closed, after
  explicit selection and prepared packages. Leave interactive gameplay and visual
  evaluation to the owner. The owner's latest instruction excludes Agriculture.

Completion for the research handoff: one bounded machine recommendation, stock and
repair integration, material/energy budgets, state/permission table, optional
provider plan, modular graphics brief and concrete implementation sequence, with
verified findings separated from proposals and unresolved runtime checks.
