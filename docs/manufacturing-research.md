# Phobos Manufacturing: first machining research

25 September 2026. **First-round research and proposed balance; no operational
machining is implemented.** The companion [implementation record](manufacturing-implementation.md)
describes the buildable 0.0.1 scaffold. This follows the
[owner's handover](manufacturing-handover.md); it does not revise shipped housing
recipes, saved furnace jobs or residue identities.

## Recommendation and limits

Begin with one enclosed three-axis mill for **two replacement heat sinks per
batch**, using one purchased, near-net aluminium preform. Provisional equipment
name: **Phobos' Rivetline M4 Enclosed Machining Centre**. Rivetline is our fictional
industrial brand; M4 and its appearance are proposals, not owner-approved art.
Require Framework 0.17.0 only, plus the game and BepInEx. Buying stock and a finite
machining cartridge must suffice without Shipbreaker, OCF or Salvage Workshop.

This deliberately narrows the earlier twelve-sink casting idea. A smaller part
carrier simplifies positive clamping, crew handling and finite output storage.
It leaves a larger allowance for gates, mounting faces and separation. This is
our design inference, not a measured yield. A lathe is a poor first fit for our
proposed flat mounting faces, hole pattern and finned rectangular stock: turning
would still need another operation for those features. No shaft/bush catalogue
or multi-machine production chain is justified yet.

The main endurance benefit is replacing a part in existing life-support repairs.
The machine will still depend on replacement tools/lubricant, power, heat
rejection and other repair components. It cannot establish indefinite survival
or perfect recycling. At only $27.50 native base value per sink, buying spares
remains attractive; fabrication earns its place when resupply is inconvenient.

## Primary evidence and its application

Sources accessed 25 September 2026. Each application below is a Phobos inference
or proposal. No cited organization has evaluated or endorsed this mod.

| Primary source and attribution | Supported finding | Application and limit |
| --- | --- | --- |
| **Tormach**, [PCNC 440 operator manual: machine specifications](https://knowledgebase.tormach.com/pcnc-440/machine-specifications), undated live manual | A compact metalworking mill is rated at 0.56 kW spindle power, 254 × 159 × 254 mm travel, 68 kg table load and about 272 kg system mass, with a 1.1 × 0.9 m footprint. Tormach stresses rigid workholding and respecting spindle power. | Supports substantial machine mass and positive fixtures even for small parts. Spindle power is not total electrical demand; terrestrial dimensions do not establish game tiles or spacecraft qualification. |
| **Made In Space, Inc., development team led by Brandon Kirkland**, [VULCAN parabolic-flight summary](https://flightopportunities.ndc.nasa.gov/media/technology/334/331-Summary-Chart.pdf), 14 August 2020, hosted by **NASA Flight Opportunities** | The proposed apparatus combines CNC milling, a tool changer, an iris clamp, verification cameras and an environmental-control unit. A stated flight objective is testing machining-debris capture in microgravity. | Direct precedent for treating fixturing and chip capture as integral equipment. This is a test plan, not evidence of successful flight qualification or an operational ISS mill. We are not copying its hybrid additive process or vacuum system. |
| **NASA Marshall / ESA**, [Microgravity Science Glovebox account by Wayne Smith](https://www.nasa.gov/missions/station/microgravity-science-glovebox-celebrates-20-years-of-success/), 8 July 2022 | ESA developed the facility jointly with NASA. Its enclosed work volume and filtration contain experimental particles and aerosols. | Supports contained handling and filtration aboard a crewed spacecraft. The MSG is a research facility, not a metal-cutting enclosure rating or proof that ordinary cabin vacuum cleaners handle metal chips. |
| **Sandvik Coromant**, [Milling different materials: non-ferrous materials](https://www.sandvik.coromant.com/en-gb/knowledge/milling/milling-different-materials), undated live guidance | Aluminium alloy composition affects machinability. Edge buildup, burrs and evacuation matter; the guidance recommends cutting fluid for aluminium and sharp, positive cutting edges. | Generic scrap is not an alloy certificate. Prefer purchased specified preforms and a finite lubricated finishing process; do not cite steel dry-roughing advice as validation of dry aluminium finishing. |
| **Sandvik Coromant**, [Dry milling or with cutting fluid](https://www.sandvik.coromant.com/en-gb/knowledge/milling/dry-milling-or-with-cutting-fluid), undated live guidance | Aluminium finishing is an exception to general dry-milling advice; the page also describes small-dose lubrication with filtered evacuation. | Supports considering limited lubricant rather than starting with a ship-wide coolant network. Our sealed cartridge, dosage and capture efficiency remain authored simplifications, not a reproduced industrial process. |
| **NASA** opportunity brief, prepared by **Secor Strategies**, [On-Demand Additive Manufacturing for Deep Space](https://www.nasa.gov/wp-content/uploads/2024/09/27-on-demand-manufacturing-spec-sheet-508.pdf), hosted September 2024 | Separates fabrication, machining/joining and inspection needs for usable spare parts. | Include crew setup and inspection. Its sub-2 kW additive-system target does not determine our mill's power rating, accuracy, footprint or production time. |

## Reproduced native evidence

**Blue Bottle Games**, [Ostranauts developer](https://store.steampowered.com/developer/bluebottlegames/):
local `data/condowners`, `data/condtrigs`, `data/installables` and `data/loot`
are the primary game sources. Relative data paths are under
`Ostranauts_Data/StreamingAssets/`. The audit was rerun on 25 September; the
assembly SHA-256 matches the handover:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
The handover identifies this installation as 1.0.1.5; no game session was opened
to independently read its running version.

Reproduce with `scripts/audit-manufacturing.py --game-root <local game folder>`.
It reuses `scripts/audit-furnace-repair.py`, drops that study's superseded balance
fields, and adds focused tool/merchant evidence. Results stay in ignored
`.local/manufacturing-research/`; no game definition files are distributed.

| Native finding | Consequence |
| --- | --- |
| `ItmHeatSink01`: 1.5 kg, $27.50 base value; description identifies aluminium/copper and a cast mounting pattern | Target one native-compatible finished identity, retaining mass and `IsHeatSink`; do not promise a real alloy, fin geometry or certified thermal resistance |
| `TIsHeatSink` requires `IsHeatSink`, with no forbids/nested triggers | A branded Manufacturing sink can plausibly satisfy native repair gathering; actual gathering still needs verification with the new definition |
| 68 of 252 broken-item Repair definitions require a sink | Counts include loose/installed variants, not 68 machine families; Restore is separate |
| `AtmoScrubber01DmgRepair`: motor ×1, sink ×1, motherboard ×1, small mechanical ×2, small electronic ×1, steel ×2 | Native bill remains 8 kg / $114.20; the new sink substitutes only one input |
| Scrubber Repair requires `TIsToolMortorq` and `TIsToolSoldering`; native progress maximum 2,000 | Do not describe progress units as seconds or substitute milling for the final repair |
| `ItmToolScrewDriver01`: 2 kg / $15; `ItmToolDrill01`: 2 kg / $150; both carry `IsToolMortorq` | Suitable native clamp/assembly-tool candidates; a powered drill is not a machine spindle or a precision fixture |
| `ItmToolSolderingIron01`: 2 kg / $90; `ItmToolWelder01`: 2 kg / $165 | These exist in Ostranauts. The Project Zomboid no-soldering-iron rule does not apply. Soldering belongs to repair/electronics, not cutting the sink |
| `ItmOKLGSupplyKioskInv`, `ItmOKLGFixer`, `ItmTraderSanDiegoHalvorsonInv`, `ItmVORBScrapKioskInv`: existing item-loot tables | Concrete additive stock targets; their presence alone does not mean Manufacturing stock already exists or is guaranteed on every visit |

Damaged tools can carry the same type flags. Runtime work must use native
usability/access checks as well as matching the flag. The audit enumerates
candidate definitions, not usable tools in a player's inventory. Base values
are definition values, not merchant quotations.

## Machine and operation proposal

Use a **4 × 4 tile, 280 kg** anchored enclosed machine, plus an unobstructed
front operator aisle. Those are authored game dimensions; there is no asserted
metres-to-tiles conversion. Proposed working envelope is roughly 300 × 200 mm
in-plane, with a stock carrier no larger than about 200 × 120 mm. Geometry,
clamp clearance and usable Z travel need a registered layout before art or
definitions are finalized; mass alone does not establish fit.

Use one near-net, two-part finned preform with sacrificial clamp lugs and a
connecting web. Positive jaws restrain the web, with secondary retention holding
each separated part. Cutting a fully finned sink from a solid billet is a
different recipe and must not inherit this time or yield.

1. **Load/clamp, local crew:** load one exact 4 kg preform and one intact 1 kg
   machining cartridge; check empty contents, stock identity and both fixtures.
2. **Setup, local crew:** establish datum, tool offsets and mounting pattern;
   close enclosure and verify capture path. Proposed 180 crew-work seconds for
   load/setup, with a usable native Mortorq tool. This is gameplay pacing.
3. **Machine:** face the contact surface, drill mounting holes, separate the
   two parts while restrained, and chamfer accessible edges. Captive tooling
   abstracts CNC programming and tool changes. Proposed 900 powered-equivalent
   seconds, not a manufacturer's cycle time.
4. **Inspect/deburr, local crew:** stop spindle, finish capture, inspect fit and
   deburr inside the contained workspace. Proposed 120 crew-work seconds, using
   the cartridge's dedicated gauge/deburring kit. No invented native caliper ID.
5. **Release, local crew:** release two sinks and one closed spent cartridge
   only when all products fit. No outside loose chip pile or instant output drop.

The cartridge bundles cutting edges, a contained lubricant supply, capture media
and handling kit for one batch. Its exhausted identity cannot satisfy a fresh
cartridge check. Native machine wear/Restore remains separate; replacing the
cartridge does not heal the motor or restore a damaged machine. One cartridge
per batch is a conservative service abstraction, not a measured cutter life.

## Material ownership, standalone supply and optional casting

**All following `PhobosManufacturing...` IDs are unregistered proposals.**
Manufacturing owns every new stock and finished-item definition. Framework owns
none of these commodities. The finished sink alone carries `IsHeatSink`.

| Proposed ID | Meaning / mass | First acquisition or destination |
| --- | --- | --- |
| `PhobosManufacturingMill` + installed/loose/damaged suffixes | M4 equipment / 280 kg empty | Additive industrial/fixer machine offers; native install and maintenance |
| `PhobosManufacturingHeatSinkStock` | Near-net two-sink preform / 4 kg | New Manufacturing stock offers at K-Leg supplies and Halvorson |
| `PhobosManufacturingMachiningCartridge` | Sealed fresh tooling/lubrication/capture kit / 1 kg | New Manufacturing stock offers at the same merchants |
| `PhobosManufacturingHeatSink` | Branded replacement sink / 1.5 kg | Dedicated machining only; native repair/trade acceptance to check |
| `PhobosManufacturingSpentCartridge` | Captured machining material plus spent kit / 2 kg | Retained physical waste; no first-slice recovery recipe |

Proposed metal budget: **4 kg preform → 2 × 1.5 kg sinks + 1 kg machining
material**. Proposed complete job: **4 + 1 = 3 + 2 kg**. The fresh kit's notional
0.75 kg hardware/tooling/media and 0.25 kg contained process liquid remain in
the 2 kg spent kit along with the 1 kg removed metal. These fractions describe
our simplified sealed consumable; they are not chemical assays or measured
lubricant usage. Tool wear and fines stay inside the retained material budget.

Use finite lubricant with contained recirculation/capture as the first design.
Do not enable arbitrary dry aluminium work, infer free oil from electricity,
vent cabin gas, or require Ship's Water. The cartridge's liquid is an internal
accounted quantity, not a new atmosphere species or generally refillable tank.
Swarf exposed to it is **contaminated**, even if much of its mass is aluminium.
It does not become native `ItmScrapAluminum`; stock, spent kits, historic residue
and terminal rejects stay distinct. A future cleaning process would need its
own inputs, actual recovery evidence and retained contaminants.

The standalone path is **buy preform + kit → machine → native repair**. Current
Framework `Trading.MarketStock` / `Registration.AdditiveLoot` can append bounded
offers without replacing foreign stock or refreshing existing inventories. The
mod must publish its definitions before adding offers. Seed stock chances as
documented settings when implemented; no console-spawn-only prerequisite.

Optional **later F6 recipe**, owned by Shipbreaker: **20 kg native aluminium →
four 4 kg preforms + three 1 kg clean solid gate/offcut units + existing 1 kg
terminal melt remainder**. The clean gate route assumes separation before any
lubricant contact; it is an authored casting simplification, not a scrap assay.
If that separation cannot be represented, retain the 3 kg as a distinct casting
remainder instead of labelling it clean scrap. The approved 19 kg housing output
and its bench-finishing recipe remain unchanged.

Four machining jobs would add four 1 kg kits and produce **12 kg sinks + 8 kg
spent kits**, with **3 kg gates + 1 kg melt remainder**: **24 kg in = 24 kg out**.
This means eight sinks per future 20 kg F6 batch, replacing the *unimplemented*
twelve-sink proposal only. F6 access to generic aluminium does not establish
alloy quality; standardized useful fit remains an explicit gameplay assumption.

Shipbreaker may register that recipe only after discovering the Manufacturing
provider and verifying exact registered output definitions/masses. Manufacturing
does not depend on Shipbreaker or register a duplicate casting. Old F6 state lacks
a recipe identity, so introducing selection requires an explicit saved-contract
extension mapping historical jobs to housing. This is separate implementation
work, not a shortcut into the scaffold. Output packing for four preforms and
remainders also needs checking in the actual F6 tray.

## Energy, heat and finite storage proposal

| Quantity | First approximation and meaning |
| --- | --- |
| Active received electrical load | 2.0 kW total: 0.3 kW controls/capture and 1.7 kW spindle/axes budget |
| Loaded idle instruments | 0.02 kW while actually supplied; heat still accounted |
| Powered machining duration | 900 s; intended configurable range 300–3,600 s, captured on the job |
| Full-supply active energy | 1,800 kJ = 0.5 kWh; includes auxiliaries, excludes setup/idle and any tick overrun |
| Thermal accounting | Every actually consumed kJ enters one finite machine/process store before transfer to room gas; no second allocation of cutting heat |
| Effective machine heat capacity | 140 kJ/K, authored lumped model; the adiabatic 1,800 kJ rise would be about 12.9 K |
| Heat transfer candidate | Up to 0.1 kW/K × positive machine/room temperature difference, bounded by stored energy and native receiving-room headroom |
| Operating limits | Pause at a 60°C machine limit; room acceptance ceiling 50°C; values are gameplay settings/model limits, not material certifications |
| Physical inventories | One 4 kg stock fixture; one 1 kg fresh-kit slot with 2 kg spent capacity; proposed 4 × 4-cell / 5 kg released-product tray |

These are not industrial cutting-force or cycle-time measurements. No cutting
coefficient, alloy specification or toolpath has yet justified a precise removal
rate. Commissioning must evaluate useful throughput, room heating and crew
handling rather than imply the machine is a Tormach or a NASA system.

For a measured interval `dt` seconds and native receipt `E` kJ, service overhead
is `min(E, 0.3 × dt)`. Credit at most
`min(dt, remainingJobSeconds, max(0, E - overhead) / 1.7)` machining seconds.
This is a gameplay approximation of reduced operation, not a motor torque law.
Below the operating overhead, pause and require explicit Resume; no banked
electricity receipt survives reload. Cap work by time as well as energy. Excess
actual native consumption still becomes heat, including final-tick overrun.

Prevent native donor heat and custom store heat from counting the same receipt
twice; inspect the selected powered-appliance donor before adding its adapter.
Retain physical heat when a thermometer fails. Cooling needs actual adjacent
room gas heat capacity: vacuum provides no free convection, and a sealed cold
room is not an infinite heat sink. No first-slice exterior radiator or hull hole.
Pause cuts during propulsion commands; do not alter flight controls.

## State, permissions and interruption contract

The named states below are proposed stable logic, never parsed from translated
labels. Reads return snapshots; they do not advance work, write defaults or Resume.
Local/C1/F3 commands all reach the same checked content service.

| State / event | Required action and authority | Material/progress result |
| --- | --- | --- |
| Empty / load | Local accessible crew; intact installed machine; correct stock, kit and free fixtures | Physically move original objects; no virtual inventory or nearest-item substitution |
| Loaded / setup | Local crew + usable Mortorq; positive clamp and enclosure checks | Bind full machine/ship/stock/kit IDs and recipe revision; capture duration/rating |
| Ready / start | Explicit local or checked same-ship console command; sensors, heat and power valid | Grant session permission; stock remains captive |
| Running / partial supply | Single measured native electrical receipt, bounded elapsed time | Credit only paid work; retain all heat and captured matter |
| Brownout, lost authority, damage, flight or open enclosure | Pause; resume only after fresh checks and explicit command | No reset, free work or material refund; failed probes read Unknown |
| Machined / inspect | Local crew; stopped spindle and contained debris; completed inspection work | No usable heat sink exists before inspection |
| Ready to release / output full | Local release with space for every sink and spent kit | Retain captive stock/kit and completed progress until whole batch fits |
| Save/reload or unloaded time | Restore validated recipe/IDs/progress/heat; clear permission and timestamps | Never apply wall-clock catch-up; require explicit Resume |
| Cancel before first cut | Local crew, cool/stopped; no cut credited | Unlock original stock and fresh kit; no energy refund |
| Cancel after cutting begins | Treat as Pause in the first slice | No pristine-stock refund or restart. Retain marked stock and kit for later completion; a safe destructive-abort route is future work |
| Damaged/repair mode change | Native repair with cold, stopped machine; preserve object maps and captive objects | No swapping a charged fixture for empty/new stock; refuse uninstall/dismantle while loaded |
| Unknown/corrupt saved revision or ambiguous output commit | Protected stop, preserve evidence | Never clear fields automatically, duplicate outputs or retry consumption blindly |

During processing, the 4 kg stock object represents the complete captive metal
lot, including already cut pieces/chips, until the output commit. That is a
coarse physical-accounting abstraction. Its reserved/started state must block
ordinary hauling, stacking, sale, native crafting and F6 admission. The cartridge
is likewise captive. Two-object input consumption and irreversible faults need
the guarded multi-input pattern already used by the furnace; `ProcessJob`
alone does not provide it. No claim of crash-atomic native saves is justified.

## Framework reuse and concrete gaps

Inspected repository code is the source for this map; it is not engine evidence.
See the [Framework author guide](framework-author-guide.md).

| Existing service / source | Manufacturing use and limit |
| --- | --- |
| `Processing/ProcessJob.cs` | Immutable recipe revisions and bounded progress; 1–3,600 s fits a 900 s powered phase. `ProcessRecipe.InputKg` can account for the combined 5 kg lot, but job binding has only one input ID. A content state record must additionally bind the cartridge. `ProcessJob` starts Running on construction, so explicitly pause restored jobs before publishing a session |
| `Processing/NativeEnergyReceipts.cs`, `EnergyReceipt.cs` | Measure existing `Powered.UsePower` consumption once, including partial supply/storage; no additional generator or free reactor heat |
| `Processing/ThermalMath.cs` | Shared sensible-energy/radiation arithmetic only; content must enforce finite stores, bounded exchange, capacity, limits and native room acceptance |
| `Inventory/NativeItemTransfer.cs`, `BatchPlacement.cs`, shared `BatchDelivery` | Original physical stock movement, complete placement planning and staged release. No reservation authority, persistent transaction or automatic multi-input safety is supplied |
| `Persistence/ObjectStateStore.cs` | Namespaced versioned record, full object ownership and protected unreadable envelopes. Content validates phase, quantities, recipe and exact additional IDs |
| `Registration/ApplianceDefinitions.cs`, `NativeDefinitions.cs`, `MaintenanceDefinitions.cs` | Installed/loose/damaged skeleton and private publication, plus native servicing. Add machining-specific inventory/retention checks without copying a furnace or foreign machine |
| `Trading/MarketStock.cs`, `Registration/AdditiveLoot.cs` | Standalone new stock and machine offers; retain foreign entries and existing inventories |
| `Controls/ConsoleBinding.cs`, `EquipmentProviders.cs`, `NativeInstruments.cs`, `PanelWidgets.cs` | Optional C1 snapshot/dispatch and runtime widgets; provider registration grants no command authority and no remote hauling |
| `Localization/TranslationCatalog.cs`, `EquipmentNames.cs` | Content-owned English fallbacks and original brand/model metadata; displayed units and limits derive from captured rules |

Keep the one-hour fixed-job limit. Crew load/setup/inspection are separate local
interactions, not a reason to extend it. New work belongs in a small Manufacturing
service/state/definition layer. Extract shared native authority or guarded
delivery code only where the actual implementation would otherwise duplicate it.
No new general scheduler, fluid network or speculative framework is needed.

## Economy, construction and next decision

Provisional base values: M4 **$18,000 intact / $4,500 damaged**, preform **$32**,
fresh kit **$8**, sink **$27.50**, spent kit **$0.01 technical minimum**. A purchased
batch uses $40 of stock and produces $55 of useful parts plus retained waste;
the $15 spread excludes machinery, crew, electricity and merchant multipliers.
Waste's nonzero technical value does not establish a buyer. These are authored
trial values, not a demonstrated profitable business or final balance.

For the first operational slice, buy the complete machine through existing
native traders. **Do not offer a scrap-only recipe for a precision spindle.**
If construction is added, assemble four purchased 70 kg factory modules (frame,
spindle, motion/fixture, controls/enclosure), retaining 280 kg with no hidden
material. This is an acquisition/construction plan, not four registered products;
choose exact bills, work and dismantling outputs before shipping it. Ordinary
tables may support assembly of those modules, never the heat-sink machining job.

Dismantling needs a native price/wear audit and explicit mass-balanced outputs
below whole-equipment resale value before it can be enabled. Loaded machinery
cannot be dismantled. Do not reuse another machine's salvage bill or promise
precision tool fabrication from generic mechanical parts.

Proceed next with native machine/inventory retention, a two-input saved-state
model and measured power/thermal adapter. The material design choices surfaced
by this round are **two sinks per 4 kg preform, one finite cartridge per batch,
and retained contaminated swarf**. They remain reversible proposals; no paid
artwork, installation, F6 migration or gameplay balance has been committed.
The [implementation record](manufacturing-implementation.md) gives the concrete
order, panel sketch, validation boundaries and artifact layout.
