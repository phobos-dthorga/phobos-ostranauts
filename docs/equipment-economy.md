# Equipment economy and maintenance

Current stock quantities: [bulk merchant lots](merchant-stock.md) supersede the older single-item offers below. These content versions require Framework 0.24.0+.

Regional acquisition now covers the current vanilla solar system: see the
[solar-system economy guide](solar-system-economy.md) for availability, native
price factors and limits. The regional builds require Framework 0.23.0+.

Research and implementation baseline: **2026-09-24**, Ostranauts **1.0.1.4**,
BepInEx **5.4.23.5**. Candidate: Framework **0.8.0**, Shipbreaker **0.8.0**, Auto Nav **0.3.0**.
These are implemented balance choices informed by local game/mod definitions;
they are not measured gameplay outcomes. Gameplay validation remains pending.

Auto Nav 0.4.0 rechecked the retained balance against **1.0.1.5**. Version 0.8.1
now names the equipment **Phobos' Asterel N1 Polaris Auto Nav Module**. Its dedicated
[acquisition and service guide](auto-nav-economy.md) covers native buy/sell
filters, materials, timing and naming compatibility. Display names follow the [equipment brand directory](equipment-branding.md);
role labels below are shorthand. This naming pass changes no prices or bills.
The full value audit is
regenerated from the current definitions below.

## F6 electrical casting candidate (25 September 2026)

Agriculture 0.3.0 implements its separate economic pass: see the
[current service and supply guide](agriculture-player-guide.md#economy-and-maintenance-030),
[economic review](agriculture-economy-review.md) and
[generated evidence](agriculture-economy-evidence.md). Its prices, repairs, salvage
and finite irrigation supplies are authored balance awaiting owner gameplay checks.

Shipbreaker 0.12.0 adds the 240 kg F6 furnace ($24,000 functional / $6,000
broken), 100 kg F6-R radiator ($7,200 / $1,800), and 80 kg F6-S construction
sections ($6,500). Native maintenance and merchant routes reuse the existing
economy services. Three sections build the furnace. The radiator uses 40 steel
and 60 aluminium; its 100-unit bill stays within the construction limit.

The [F6 guide](furnace-player-guide.md) describes the mass-balanced casting and
optional D4/R4 section recipes. Existing recipes and historic residue retain their
meaning. New furnace/radiator/section dismantling is included in the generated
value audit, including broken condition and native wear/merchant multipliers.

## Shipbreaker 0.15.0 availability and casting recovery

F6-S sections now receive the same industrial/high-value trade categories as D4-S
and R4-S. Old explicitly saved F6-S sections gain only those missing categories;
IDs, cargo references, mass and dismantling progress remain intact. K-Leg supplies
have a 15% offer chance and San Diego Halvorson 30%, each for one section per native
stock roll. Existing shops update through ordinary native restocking; no inventories
are repopulated on loading.

The native `ItmLootSpawnEngineering` table now adds one mutually exclusive section
choice: D4-S 1%, R4-S 1%, F6-S 1% (3% total per eligible engineering roll). This is
not a per-ship discovery chance. Native engineering loot remains intact, and repeat
registration adds no duplicate branch. Native ship layouts including `02Indy.json`,
`Babak.json` and `Bulk Lifter.json` use this route. Their loot placement and available
space still determine whether an item appears; no whole working furnace is spawned.
These observations come from Blue Bottle Games' installed Ostranauts 1.0.1.5
`data/loot/loot.json` and ship definitions; see the
[developer's game page](https://bluebottlegames.com/games/ostranauts). The chances
are Phobos balance choices, not developer recommendations. Extracted data stays local.

Two explicit table recipes cut an unused released 19 kg housing blank into 19
one-kg aluminium scraps, or an 18 kg finished housing into 18 scraps. Each takes
600 seconds of configured work with Mortorq and welding tools. All cutting material
is retained: no extra slag or disappearing mass. Base recovery values are $20.90
and $19.80, below the respective $55 and $60 whole-item values. Exact casting
identities are required; melt remainder and historic residue cannot be recycled
through these recipes. Existing finishing and D4/R4 recipes keep their IDs and bills.

Machine purchase prices, repair bills, salvage yields and construction bills are
unchanged. Future Manufacturing outputs/prices remain provisional until its own
machining process is established. Runtime merchant placement, observed work duration
and owner gameplay evaluation remain pending.

## Dismantling value audit (owner clarification, 2026-09-24)

The owner clarified that the concern was **dismantling our machines**, based on
this document before gameplay testing. Material counts alone did not make the
financial result clear. The [generated equipment audit](equipment-value-audit.md)
now calculates every row using the game's own data-only price evaluator, including
wear. The separate [vanilla comparison](vanilla-economy-audit.md) records native
equipment, salvage, repair inputs and merchant multipliers.
Rerun both with `scripts/audit-economy.ps1 -OstranautsPath <game directory>`
(`-PythonPath` accepts a Python executable when it is not on PATH). This reads
definitions and runs offline checks; it neither launches nor modifies the game.

| Machine | Functional whole / recovered parts | Broken whole / recovered parts |
|---|---:|---:|
| Industrial console (0.10.0) | $5,200 / $164.90 | $1,300 / $70.80 |
| Processor | $12,000 / $514.10 | $3,000 / $365.40 |
| Exterior grabber | $6,400 / $258.55 | $1,600 / $167.15 |
| Hull chute | $1,800 / $150.15 | $450 / $85.00 |
| Residue collector | $2,400 / $88.50 | $600 / $41.45 |
| Scrap reclaimer | $14,800 / $637.60 | $3,700 / $450.00 |
| Auto Nav | $3,600 / $0.01 | $900 / $0.01 |

These are **base values per complete object/batch**, not merchant quotes and not
prices per kilogram. The assembly section is $4,800 whole versus $257.05 recovered.
For example, the processor's 92 steel scraps contribute $331.20, 40 aluminium
$44, 16 mechanical parts $80, four electronic parts $58, and 18 trash $0.90:
**$514.10 in total**. Retaining all 160 kg does not retain the machine's monetary
value.

This recheck does **not** reproduce a machine-dismantling profit at vanilla base
values. All machine rows also lose value at the lowest native wear tier, with
fresh recovery. The narrowest margin is a heavily worn broken chute: $112.50
whole versus $85 recovered. VORB's native buyer accepts both; even comparing its
low 0.4 multiplier for the whole chute ($45) with its high 0.5 multiplier for
scrap ($42.50) retains a loss. A single actual buyer normally applies one current
multiplier to both. Commodity supply/demand can differ, so this is not a promise
across all live markets, negotiations, regions or economy-changing mods.

No machine price or yield has been changed merely to correct a profit that these
numbers do not demonstrate. The implementation now has native valuation checks
for intact/broken equipment, wear thresholds, pristine flags, buyer eligibility,
purchase/dismantle/resale and construction/dismantle loops. All construction
recipes return less scrap value than their inputs; processor recovery is also
below the $570.80 raw-material bill for its two sections.

The original work times, stock chances and custom equipment prices remain
**Phobos balance choices**, not exact vanilla recipes or measured play balance.
Vanilla examples support the order of magnitude; they do not prove a unique fair
price or duration for a new machine. In particular, the $3,600 Auto Nav guidance
premium is our choice above a $767 native bare board. No further vanilla-only
precision can establish that gameplay valuation before use.

An unrelated issue found during this audit is the processor's **wall-input**
recipe: its $21 wall becomes $34.05 of current-base-value outputs. Earlier residue
definitions with zero price also invoke the game's mass-as-price fallback. That
recipe is distinct from dismantling the processor. It has **not** been silently
changed in this machine-focused review: changing its output mass distribution
requires a versioned recipe and preservation of existing 13 kg residue/jobs.
Native wall salvage also sometimes increases value, so copying it is insufficient
as an economic justification. This remains a separate processing-balance decision:
powered processing may add value, but that is not evidence of a machine-dismantling
exploit. The [residue contract](residue-material-contract.md) keeps this recipe
unchanged and states the additional value of its future recovery design explicitly.

## Acquisition and prices

Prices below are definition values before native condition, merchant, market and
negotiation adjustments. A pristine item receives the native 25% premium. A fully
restored/refurbished item uses base value. Our lightly worn stock starts at 85%
condition, which falls in the native 75%-value tier. Broken equipment uses its
real damaged definition, priced at one quarter of the functional base value.
Repair restores functionality, not the pristine designation.

| Item | Mass | Refurbished base | Pristine reference | Lightly worn reference | Broken base |
|---|---:|---:|---:|---:|---:|
| Powered dismantling fixture | 160 kg | $12,000 | $15,000 | $9,000 | $3,000 |
| Exterior panel grabber | 80 kg | $6,400 | $8,000 | $4,800 | $1,600 |
| Hull chute | 40 kg | $1,800 | $2,250 | $1,350 | $450 |
| Residue collector | 20 kg | $2,400 | $3,000 | $1,800 | $600 |
| Fixture assembly section | 80 kg | $4,800 | — | — | — |
| Scrap reclaimer | 180 kg | $14,800 | $18,500 | $11,100 | $3,700 |
| Reclaimer assembly section | 90 kg | $6,000 | — | — | — |
| Auto Nav module | 0.4 kg | $3,600 | $4,500 | $2,700 | $900 |

Installed and loose forms have the same base price. Uninstall before trading.
Sections are unfinished construction stock with no separate wear/broken family.
The processor's two sections cost $9,600 before assembly labour adds value.

Mixed panel residue (13 kg), spent service parts (0.5 kg), Auto Nav board residue
(0.4 kg), and Auto Nav assembly offcuts (0.6 kg) each have a nominal **$0.01** base
value. They are outputs, never retail offers. Zero is deliberately avoided:
native `GetBasePrice` substitutes mass when the price stat is zero. These materials
are not tagged as ordinary sortable trash and have no refining recipes yet.
Native scrap and useful parts produced by the processor retain native prices.
The hidden zero-mass feed is an internal system, not an item for sale.

The legacy pulse-only Approach Assist stays an opt-in historical prototype; it
does not get duplicate retail offers. The combined scrap reclaimer is implemented in 0.8.0. Research-only ore and
chemical systems are not advertised as purchasable equipment.

## Merchants and rarity

Each percentage is an independent chance of **one** item per native stock
generation. It is not a promise that the item will be present on every visit.
An item may also fail to appear if native stock placement has no usable space.

| Merchant stock | Processor | Grabber | Chute | Collector | Section | Auto Nav |
|---|---|---|---|---|---|---|
| K-Leg scrap supplies (`ItmOKLGSupplyKioskInv`) | 20% broken | 20% broken | 30% broken | 30% broken | 30% unfinished | 25% broken |
| K-Leg fixer (`ItmOKLGFixer`) | 10% worn + independent 10% refurbished | 10% worn | 15% worn | 15% worn | — | 30% worn |
| San Diego Halvorson industrial trader | 40% pristine | 40% pristine | 60% pristine | 60% pristine | 50% unfinished | — |
| San Diego Polaris electronics trader | — | — | — | — | — | 60% pristine |
| Venus orbital scrap kiosk (`ItmVORBScrapKioskInv`) | 15% broken | 15% broken | 22.5% broken | 22.5% broken | — | 20% refurbished |

The San Diego entries use the game's existing named industrial/electronics
merchant definitions. We do not spawn a new merchant or claim they exist at every
station. K-Leg offers and construction mean no journey to a particular region is
required. Venus gains an additional second-hand route. Ceres/Callisto faction
shops and character-creation-only supply lists are left alone: a mining-themed
table name alone is not enough evidence of an ordinary industrial retailer.

The native sales filters permit these offers. Intact Shipbreaker machinery and
sections retain `IsSalvageValueHigh`, so the fixer is a resale route while K-Leg's
ordinary supplies buyer retains its normal restriction. We do not bypass salvage
permits, ownership, locked/slotted items or a merchant's buying preferences.
Auto Nav remains control-system equipment using the native board's categories.
Repairing broken salvage for profit takes materials and labour; it is an intended
activity. Buying equipment simply to dismantle it yields much less base value
than even the broken equipment costs. This is not a guarantee against every
market fluctuation or third-party trade multiplier.

The shared implementation appends namespaced loot branches without replacing
native/foreign stock. It reapplies the chosen condition after the native trader's
automatic pristine flag, only for newly generated registered offers. It does not
retag items the player already owns, repopulate shops on load, or duplicate stock.

**Existing merchants update at their normal restock.** Native supplies kiosks
use a 24-hour restock ticker; the fixer uses the NPC trader ticker (1.5 hours),
with native away/visit checks still in charge. Restarting alone does not promise
new stock. No forced refresh command is added, since it could remove merchant
inventory the player expects to remain present.

`BepInEx/config/phobosgekko.ostranauts.framework.cfg` exposes
`[Economy] StockAvailabilityMultiplier`, default **1**, range **0.25–4**.
It multiplies chances, capped at 100%, without changing the quantity per successful offer. Change
with the game closed; restart and allow a normal restock. Prices, dimensions and
material identities remain a coherent fixed baseline in this version.

## Construction, repair and restoration

All assembly recipes use an installed native Bar/Dining Table or a supported
optional workbench. A Mortorq tool and soldering tool are required through native
tool selection/fetching. Tools are used, not consumed as ingredients. Skills,
tool condition, travel, materials fetching and interruptions can alter observed
job duration. Construction work below is the configured action duration; install,
repair and dismantle values are normalized to **unit work/tool multipliers**.

| Equipment | Assembly work | Install / uninstall | Repair broken | Dismantle |
|---|---:|---:|---:|---:|
| Assembly section | 60 min | n/a | n/a | 24 min |
| Processor | 30 min final assembly; **150 min including two sections** | 18 / 12 min | 43.2 min | 60 min |
| Grabber | 60 min | 12 / 9.6 min | 28.8 min | 39 min |
| Chute | 30 min | 6 / 6 min | 18 min | 18 min |
| Collector | 40 min | 7.2 / 6 min | 21.6 min | 21 min |
| Auto Nav | 30 min | native module placement | 10.8 min | 6 min |

Native work ticks are 0.001 hours (3.6 seconds). Install/uninstall/repair apply
five progress units per unmodified tick; dismantle applies one. Chosen progress
thresholds, in that order, are processor 1500/1000/3600/1000, grabber
1000/800/2400/650, chute 500/500/1500/300, collector 600/500/1800/350.
Auto Nav repair is 900 and dismantle 100; section dismantle is 400.

**Restore** is native wear maintenance on functional equipment, using tools with
no replacement-material bill. Shipbreaker **0.15.0** gives each equipment family
its own wear-removal rate, while retaining native durability, tool/skill modifiers,
current wear, action IDs and saved work. The native global Restore effect and
Auto Nav remain unchanged. These are authored labour choices, not measured playtimes.

| Equipment | Full wear-bar Restore equivalent | Restore after native Repair (90% wear) | Repair + Restore |
|---|---:|---:|---:|
| F6 furnace | 80 min | 72 min | 129.6 min |
| F6-R radiator / F6-P underside assembly | 20 min | 18 min | 54 min |
| C1 industrial console | 30 min | 27 min | 55.8 min |
| D4 processor | 60 min | 54 min | 97.2 min |
| G4 grabber | 30 min | 27 min | 55.8 min |
| H4 chute | 10 min | 9 min | 27 min |
| R4 reclaimer | 75 min | 67.5 min | 117.9 min |
| C2 collector | 15 min | 13.5 min | 35.1 min |

Figures use unit multipliers and exclude fetching, interruptions and tick rounding.
A full wear bar is a rate comparison, not a functional item at destruction threshold.
Lightly worn merchant stock needs 15% of the full-bar time. Cooling assembly
repair plus Restore previously took about 208.8 minutes versus 60 minutes to build;
it now takes 54 minutes. Repair bills, spent-material returns and durability stay
unchanged. Restore does not grant pristine status or reset cargo, routing, heat or
processing progress. An already queued Restore keeps its accumulated wear reduction;
subsequent work uses the current equipment rate.

Repairing a broken object still uses native replacement mode switching, preserving
its identity and applicable state, and leaves approximately 90% wear. Auto Nav
retains its native 0.00625 damage reduction per unmodified tick: roughly 5.76 minutes
for lightly worn stock and 34.56 minutes after Repair. Repair/Restore/Dismantle use
Mortorq and soldering tools; installation/removal use Mortorq. F6 construction and
casting recovery use Mortorq and welding tools.

The existing Shipbreaker construction bills remain unchanged:

| Output | Steel, 1 kg units | Aluminium, 1 kg units | Mechanical parts, 0.5 kg units | Electronic parts, 0.5 kg units |
|---|---:|---:|---:|---:|
| One 80 kg section | 50 | 24 | 10 | 2 |
| 160 kg processor | Two complete sections | — | — | — |
| 80 kg grabber | 50 | 20 | 16 | 4 |
| 40 kg chute | 24 | 10 | 10 | 2 |
| 20 kg collector | 12 | 4 | 6 | 2 |
| 0.4 kg Auto Nav + 0.6 kg offcuts | 0 | 0 | 0 | 2 |

Repair replacement bills:

| Repaired equipment | Steel | Aluminium | Mechanical | Electronic | Spent material returned |
|---|---:|---:|---:|---:|---:|
| Processor | 4 | 2 | 4 | 4 | 10 kg |
| Grabber | 2 | 1 | 4 | 2 | 6 kg |
| Chute | 2 | 1 | 2 | 0 | 4 kg |
| Collector | 0 | 1 | 2 | 2 | 3 kg |
| Auto Nav | 0 | 0 | 0 | 2 | 1 kg |

Repair leaves equipment mass unchanged and returns the replaced mass as separate
0.5 kg spent-service packs. Quantity is calculated from the **actual native repair
lot** at completion; an older 1.5 kg repair bill therefore returns 1.5 kg, not the
new recipe's larger amount. Native repair gathering, progress and cancellation
remain native. An unrepresentable fractional or excessively large lot blocks
completion and logs the reason; service material containing other cargo is also
rejected before it can be consumed. Physical completion/drop placement still needs
in-game verification. Used tools may incur their normal native costs.

## Dismantling yields

Counts use the same native units as above; trash is **1 kg** per unit. Every row
sums to the full equipment mass. Broken machines yield fewer useful components.
No money, lost material or unspawned abstract output completes the mass balance.

| Equipment/state | Steel | Aluminium | Mechanical | Electronic | Trash | Total |
|---|---:|---:|---:|---:|---:|---:|
| Processor intact | 92 | 40 | 16 | 4 | 18 | 160 kg |
| Processor broken | 80 | 32 | 8 | 0 | 44 | 160 kg |
| Grabber intact | 42 | 16 | 12 | 2 | 15 | 80 kg |
| Grabber broken | 34 | 12 | 6 | 0 | 31 | 80 kg |
| Chute intact | 20 | 8 | 8 | 2 | 7 | 40 kg |
| Chute broken | 16 | 6 | 4 | 0 | 16 | 40 kg |
| Collector intact | 10 | 3 | 4 | 2 | 4 | 20 kg |
| Collector broken | 8 | 2 | 2 | 0 | 9 | 20 kg |
| Assembly section | 46 | 20 | 8 | 2 | 9 | 80 kg |

Either Auto Nav state yields one **0.4 kg board-residue** item. A module cannot
honestly yield a native 0.5 kg electronics bundle, much less the native generic
board's motherboard-plus-parts output. The custom residue retains the mass for
future refining without inventing immediately reusable material. Offcuts, service
packs and panel residue are already final remainders and have no recursive
dismantling or Restore actions.

Empty equipment and its feed before dismantling. Shared guards check both job
eligibility and the final effect, including repair lots. Cargo inserted mid-job
blocks completion. An empty zero-mass internal processor feed is removed rather
than being transferred to the first scrap item. Dismantling a paired endpoint
leaves the other end disconnected; unlink/reassign it normally. It never chooses
another destination automatically.

## Evidence and rationale

The official [modding guide](https://steamcommunity.com/sharedfiles/filedetails/?id=3748342946)
documents the native data categories, including equipment, installables, overlays
and loot. The developer's [modding discussion](https://steamcommunity.com/app/1022980/discussions/3/3185736852419933798/)
provides additional native-data context. The current installed definitions and
read-only local engine inspection supply the actual numbers/semantics below;
neither web page is used as evidence for our proposed prices. No extracted game
data or decompiled source is distributed with this report/package.

| Comparison | Native/mod base price | Relevant work thresholds | Interpretation |
|---|---:|---|---|
| Native complete towing brace, 180 kg | $23,944 | install 1500, uninstall 1000, dismantle 1300 | External industrial mounting/work scale |
| Native RCS cluster, 28 kg | $5,520; broken $1,112 | install/uninstall 1000, repair 3000 | Small precision propulsion costs more than passive steelwork |
| Native Hydra RCS distributor, 28 kg | $32,266; broken $7,330 | install/uninstall 1200, repair 3600 | Specialized high-end ceiling, not our baseline |
| Native battery, 45 kg | $2,623; broken $524 | install/uninstall 1000, repair 3000 | Useful lower industrial anchor |
| Native pump, 11 kg | $2,050; broken $510 | install/uninstall 500, repair 1500 | Small service-port/utility scale |
| Native generic motherboard nav module | $767; broken $159 | repair 480, dismantle 100 | Physical module and native maintenance precedent |
| Salvage Workshop 0.8.71 sorter | $2,400 | install 100 | Small sorting equipment, not a full industrial dismantler |
| Salvage Workshop workbench | $1,500 | install 100 | Work surface rather than powered heavy plant |
| Ship's Water 0.16.1 recycler, 85 kg | $7,000 | install 800, dismantle 300 | Particularly useful survival-machinery comparison |
| G-Immersion Pods 0.8.3 couch/nav pod | $300,000 / $400,000 | install 600 / 1800 | Premium niche outlier; unsuitable price anchor here |

The processor's price recognizes its current limited wall-processing capability;
it is below the brace and far below specialized propulsion. The grabber is priced
for detached-panel loading, not future autonomous cutting. The chute is mostly
passive structure; the smaller collector costs more for its powered transfer and
control capability. Auto Nav carries a guidance premium over a generic bare board.
Construction adds substantial labour value above cheap raw scrap, as elsewhere
in the native economy. Prices and supply rates remain playtest balance choices.

Local engine findings: native prices use `IsPristine` and damage tiers at 95%, 66%
and 33% condition; zero price falls back to mass. `Trader.AddNewItems` grants
pristine to functional stock. `Installables.Create` generates work, tools,
progress and replacement effects; `CondOwner.ModeSwitch` destroys the attached
repair lot, preserves the object ID and transfers persistent properties. These
differences justify the small shared stock-condition and repair-remainder hooks.
No isolated proof test of established power-consumption patterns is requested.

## Upgrade and verification

Existing IDs, footprint, machine mass, construction material bills, panel yields,
processing duration/progress and port pairings remain stable. The first economy
load clones each relevant saved object DTO in memory, refreshes its price and
adds missing maintenance limits. In-progress native work retains its previous
finish threshold, including DEFAULT-compressed saves. Full-stat saves gain the
new dismantle limit. The upgrade is marked once; no save file is edited directly.
New jobs on a newly installed/repaired replacement use the current definitions.
Already-queued construction retains its native saved duration and material
contract; assembly now also requires the listed reusable tools at completion.

Auto Nav's saved overlay IDs are unchanged. They now refer to private Phobos
board definitions so our repair/salvage changes cannot affect vanilla modules.
An old queued generic board repair/dismantle finish is redirected only when its
actual object is a Phobos Auto Nav module, retaining its real repair-lot mass or
0.4 kg salvage limit. Vanilla modules using those actions are unaffected.
Ordinary saves are now the baseline, with no name gate.
`phobosnav spawn` remains an explicit debug grant; merchants and assembly are
normal acquisition. Flight limitations and manual engagement still apply.

Offline checks cover real native seller/buyer filters, mass and scrap-value
comparisons, native action generation, additive/idempotent stock registration,
old repair bill accounting, full/compressed save migration, overlay construction,
and existing processing/routing/persistence rules. Build success is not an
in-game test. No public release or full compatibility claim is made.

Useful owner checks, during ordinary play:

1. After a normal merchant restock, purchase one available Phobos item. Check the
   displayed condition and that it can be collected and installed normally.
2. Repair broken equipment; confirm functionality and separate spent parts. Use
   Restore on worn equipment. Check that stored products and routing survive.
3. Dismantle one empty spare item and compare total output mass with this table.
   Confirm a loaded machine refuses dismantling. Reload once around an interrupted
   job to exercise native job/lot integration.

Remaining acquisition improvements are blueprint/progression gating and an
in-game catalogue explaining stock locations and bills. We deliberately do not
invent a second currency, universal retailer or new faction unlock for this first
ordinary-play slice. Actual shop delivery space, repair-lot handling and crowded
salvage output placement are the specific integration uncertainties to watch.

## Reclaimer addition (0.8.0)

The [reclaimer guide](scrap-reclaimer.md) contains its complete station offers,
section/construction bills, 195-minute assembly work, service times, repair bill
and mass-balanced dismantling rows. The generated value audit now includes this
machine and its 90 kg sections. Its operating budget is a nominal 0.4 kWh / batch
at default settings with 1.44 MJ delivered into native room atmosphere.

New wall jobs produce identified R2 residue; old jobs and unclassified residue
keep their original meaning. Identified residue and terminal rejects each cost
$0.01 at definition level and are not retail products. The reclaimer recovers
$11.90 of native metal per packet; process value addition is separate from the
machine-dismantling loss requirement. Full details are in the material contract.

### F6-P alternative cooling assembly (0.13.0)

The 1 x 1 F6-P head includes the complete 100 kg underside assembly. It uses the
same bill as the F6-R: 40 kg steel + 60 kg aluminium, 3,600 native work-progress
seconds with Mortorq and welding tools at supported tables. Whole base value is
$7,200 intact / $1,800 damaged; installation 1,200, uninstallation 900, repair
3,000 and dismantling 800 work-progress units. Repair and retained salvage bills
match the F6-R exactly. Native value audits include both forms. The choice changes
installation geometry, not capacity, construction mass or operating yield.

## N2 Polaris Pursuit (25 September 2026)

The [N2 pursuit module](auto-nav-pursuit.md) has an authored $5,400 pristine base
value, $1,350 damaged value and 0.4 kg mass. Its Polaris pristine merchant offer is
60%. Construction uses two 0.5 kg small electronics parts over 30 minutes and retains
0.6 kg existing board offcuts; repair consumes two electronics parts and dismantling
retains 0.4 kg existing board residue. N1 stock, salvage and old saved IDs are unchanged.
These are game balance choices, not actual electronics manufacturing yields.
