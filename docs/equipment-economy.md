# Equipment economy and maintenance

Current stock quantities: [bulk merchant lots](development/merchant-stock.md) supersede the older single-item offers below. Use the [current dependency requirements](installing-mods.md); older version floors below describe their original releases.

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
filters, materials, timing and naming compatibility. Display names follow the [equipment brand directory](development/equipment-branding.md);
role labels below are shorthand. This naming pass changes no prices or bills.
The full value audit is
regenerated from the current definitions below.

## F6 electrical casting candidate (25 September 2026)

Agriculture 0.3.0 implements its separate economic pass: see the
[current service and supply guide](agriculture-player-guide.md#economy-and-maintenance-030),
[economic review](development/agriculture-economy-review.md) and
[generated evidence](development/agriculture-economy-evidence.md). Its prices, repairs, salvage
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
stock roll. Existing shops update through ordinary normal restocking; no inventories
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
financial result clear. The [generated equipment audit](development/equipment-value-audit.md)
now calculates every row using the game's own data-only price evaluator, including
wear. The separate [vanilla comparison](development/vanilla-economy-audit.md) records native
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
exploit. The [residue contract](development/residue-material-contract.md) keeps this recipe
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
| Fixture assembly section (retired 0.60.0) | 80 kg | $4,800 | — | — | — |
| Scrap reclaimer | 180 kg | $14,800 | $18,500 | $11,100 | $3,700 |
| Reclaimer assembly section (retired 0.60.0) | 90 kg | $6,000 | — | — | — |
| Auto Nav module | 0.4 kg | $3,600 | $4,500 | $2,700 | $900 |
| Process water silo (0.37.0) | 240 kg empty | $4,800 | $6,000 | $3,600 | $1,200 |
| Ice thaw unit (0.37.0) | 120 kg | $3,200 | $4,000 | $2,400 | $800 |
| ML-2 mining laser (0.59.0) | 120 kg | $9,600 | $12,000 | $7,200 | $2,400 |
| Aluminium ingot (0.38.0) | 4 kg | $12 | $15 | $9 | — |
| Steel ingot (0.38.0) | 4 kg | $25 | $31.25 | $18.75 | — |

Installed and loose forms have the same base price. Uninstall before trading.
Sections are unfinished construction stock with no separate wear/broken family.
Since Shipbreaker 0.60.0 they are retired: never sold or found, and the D4, R4 and
F6 come whole. The rows remain for copies still held, which convert automatically.

Mixed panel residue (13 kg), Auto Nav board residue (0.4 kg), and Auto Nav
assembly offcuts (0.6 kg) each have a nominal **$0.01** base value. They are
outputs, never retail offers. Zero is deliberately avoided: native `GetBasePrice`
substitutes mass when the price stat is zero. Terminal remainders carry the game's
Trash market category: board residue,
offcuts, feed-family rejects, retained coolant and, since Shipbreaker 0.39.0, the
R4 reject and both melt remainders. None is the game's own Trash item, and none
has a refining recipe yet. Mixed panel residue is R4 feed and stays unclassified.
Native scrap and useful parts produced by the processor retain native prices.

**Repair and Restore follow the game** (owner direction, 3 October 2026; Framework
0.74.0). A repair uses up its parts and gives back only the repaired machine, as
every one of the game's own repairs does; Restore removes wear and leaves nothing
behind. Older Phobos repairs also returned the used parts as Spent Service Parts
(0.5 kg each). Those are removed from a save as each ship loads, with one crew-log
line, and nothing makes new ones.
The hidden zero-mass feed is an internal system, not an item for sale.

The combined scrap reclaimer is implemented in 0.8.0. Research-only ore and
chemical systems are not advertised as purchasable equipment.

## Merchants and rarity

Each percentage is an independent chance of **one** item per shop stock
generation. It is not a promise that the item will be present on every visit.
An item may also fail to appear if shop stock placement has no usable space.

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
required. Venus gains an additional second-hand route. Character-creation-only supply
lists are left alone. The CCRE and GalCon faction kiosks were also left alone
here; since 30 September 2026 they sell everything for scrip (see
[Faction kiosks (scrip)](#faction-kiosks-scrip) below).

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

## Faction kiosks (scrip)

Every Phobos machine, section, board, supply, crop and meal is also sold at the
CCRE faction kiosks (Zhonghuamen Terminal; Port Yangshan, Mars) and the GalCon
faction kiosk (Port Mojave, Ceres). These kiosks take only faction scrip, which
you earn by selling them mining output.

- **Price:** the usual credit price at the faction's rate, one scrip per 20
  credits, like everything else they sell. A 24,000 credit F6 costs 1,200 scrip
  before the kiosk's own adjustments.
- **Standing:** the kiosk lists what you can't buy yet as locked rows.

| Standing needed | What you can buy |
| --- | --- |
| Neutral | Supplies, pipe and line, ingots, coolant and nutrient charges, seeds, crops and meals |
| Warm (25) | Agriculture machines and nutrient hoppers; H4 chute, C2 collector, Y bins, T2, S silos, C1 console, F6-R and F6-P; N1 board |
| Friendly (50) | D4 and R4, G4 grabber, ML-2 mining laser; N2 and N3 boards; every Manufacturing gas store and acid tank, the A2, P1 and L2 |
| Trusted (75) | F6 furnace; X2, AX-2, K2, V4, LC-3, SA-3 and EC-4 |

Nothing needs Honored. Buying at a kiosk also raises your standing with that
faction a little, as it does for vanilla goods. Stock arrives in the usual lots
at the kiosk's next normal restock; kiosks already stocked are not refilled. The
full table, vanilla comparisons and reasoning are in the
[faction kiosk record](development/faction-kiosk-stock.md).

## Construction, repair and restoration

Smaller equipment recipes use an installed native Bar/Dining Table or a supported optional workbench. Since Shipbreaker 0.60.0 the D4, R4 and F6 are not built at all: they are bought or found whole and installed directly (see [installing machines](section-assembly-and-maintenance.md)). A Mortorq tool and soldering tool are required through native
tool selection/fetching. Tools are used, not consumed as ingredients. Skills,
tool condition, travel, materials fetching and interruptions can alter observed
job duration. Construction work below is the configured action duration; install,
repair and dismantle values are normalized to **unit work/tool multipliers**.

| Equipment | Assembly work | Install / uninstall | Repair broken | Dismantle |
|---|---:|---:|---:|---:|
| Assembly section (retired 0.60.0) | n/a | n/a | n/a | 24 min |
| Processor | Not built (bought or found whole) | 18 / 12 min | 43.2 min | 60 min |
| Grabber | 60 min | 12 / 9.6 min | 28.8 min | 39 min |
| Chute | 30 min | 6 / 6 min | 18 min | 18 min |
| Collector | 40 min | 7.2 / 6 min | 21.6 min | 21 min |
| Auto Nav | 30 min | native module placement | 10.8 min | 6 min |
| Process water silo | purchase only | 14.4 / 10.8 min | 28.8 min | 48 min |
| Ice thaw unit | purchase only | 9.6 / 7.2 min | 24 min | 30 min |
| ML-2 mining laser | purchase only | 12 / 9.6 min | 28.8 min | 39 min |

The S3 and T2 stay purchase-only by owner decision (29 September 2026):
construction of anything beyond semi-advanced equipment waits for the Phobos
Manufacturing mod to decide where and when it belongs. The S2 to S5 silos, now
Framework's, are likewise sold, not built. The Ablatine ML-2 mining laser
(Shipbreaker 0.59.0) is purchase-only on the same decision.

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
| S3 silo | 30 min | 27 min | 55.8 min |
| T2 thaw unit | 25 min | 22.5 min | 46.5 min |
| ML-2 mining laser | 30 min | 27 min | 55.8 min |

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
| One 80 kg section (retired 0.60.0; a leftover converts back to this bill) | 50 | 24 | 10 | 2 |
| 160 kg processor | Not built since 0.60.0 (bought or found whole) | — | — | — |
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
| R4 reclaimer | 4 | 2 | 6 | 4 | 11 kg |
| C1 console | 1 | 1 | 2 | 4 | 5 kg |
| F6 furnace | 4 | 4 | 8 | 6 | 15 kg |
| F6-R radiator / F6-P port | 2 | 4 | 4 | 0 | 8 kg |
| S3 silo | 2 | 2 | 4 | 0 | 6 kg |
| T2 thaw unit | 2 | 2 | 4 | 2 | 7 kg |
| ML-2 mining laser | 2 | 2 | 2 | 6 | 8 kg |
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
| R4 reclaimer intact | 104 | 42 | 20 | 8 | 20 | 180 kg |
| R4 reclaimer broken | 92 | 34 | 10 | 2 | 48 | 180 kg |
| C1 console intact | 16 | 8 | 8 | 4 | 10 | 40 kg |
| C1 console broken | 12 | 6 | 4 | 0 | 20 | 40 kg |
| F6 furnace intact | 140 | 50 | 24 | 12 | 32 | 240 kg |
| F6 furnace broken | 120 | 40 | 12 | 4 | 72 | 240 kg |
| F6-R radiator / F6-P port intact | 28 | 50 | 8 | 0 | 18 | 100 kg |
| F6-R radiator / F6-P port broken | 20 | 38 | 4 | 0 | 40 | 100 kg |
| S3 silo intact | 170 | 40 | 20 | 4 | 18 | 240 kg |
| S3 silo broken | 40 | 10 | 4 | 0 | 188 | 240 kg |
| T2 thaw unit intact | 70 | 24 | 20 | 8 | 12 | 120 kg |
| T2 thaw unit broken | 30 | 8 | 4 | 0 | 80 | 120 kg |
| ML-2 mining laser intact | 56 | 26 | 20 | 24 | 16 | 120 kg |
| ML-2 mining laser broken | 34 | 14 | 8 | 0 | 68 | 120 kg |
| Assembly section | 46 | 20 | 8 | 2 | 9 | 80 kg |
| R4-S assembly section | 52 | 21 | 10 | 4 | 10 | 90 kg |
| F6-S assembly section | 44 | 20 | 12 | 4 | 8 | 80 kg |

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
differences justify the small shared stock-condition hook. The repair-remainder hook
was removed in Framework 0.74.0, so repairs now finish exactly as the game's do.
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
2. Repair broken equipment; confirm functionality and that nothing but the repaired
   machine appears. Use Restore on worn equipment. Check that stored products and
   routing survive.
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

## Manufacturing 0.1.1: late-game plant

Owner direction, 29 September 2026: Phobos Manufacturing's equipment is meant
for the middle-to-late game and is priced, stocked and maintained like the
game's own late-game kit rather than like Shipbreaker's working machinery.
Vanilla anchors, from the installed 1.0.1.5 definitions: IC fusion reactor
$141,000, active stabilizer $156,774, missile launchers $86,000–93,000, heavy
lift rotor $56,774, Zhuangzi and NASA radars $42,000 and $30,000, towing brace
$23,944. These are authored balance choices, not measured gameplay outcomes.

| Equipment | Mass | Base price | Pristine reference | Lightly worn reference | Broken base |
|---|---:|---:|---:|---:|---:|
| Fennmark V4 volatiles refinery | 180 kg | $64,000 | $80,000 | $48,000 | $16,000 |
| Fennmark X2 chemical processor | 130 kg | $38,000 | $47,500 | $28,500 | $9,500 |
| Fennmark H2 hydrogen store | 160 kg empty | $22,000 | $27,500 | $16,500 | $5,500 |
| Fennmark K2 Sabatier reactor (0.2.0) | 150 kg | $44,000 | $55,000 | $33,000 | $11,000 |
| Fennmark M2 methane store (0.2.0) | 160 kg empty | $21,000 | $26,250 | $15,750 | $5,250 |
| Fennmark P1 RCS propellant manifold (0.3.0) | 10 kg | $24,000 | $30,000 | $18,000 | $6,000 |
| Fennmark gas line (0.3.0, ordinary supply; Framework's since Framework 0.57.0) | 1 kg | $3 | — | — | — |
| Process water line (Framework 0.57.0, ordinary supply) | 1 kg | $3 | — | — | — |
| Nickel-iron ingot | 4 kg | $220 (0.26.0; $20 from 0.15.0, $24 from 0.1.1 to 0.14.0) | — | — | — |
| Carbon stock | 1 kg | $38 (0.26.0; $10 before) | — | — | — |
| Nickel steel ingot (0.26.0) | 4 kg | $260 | — | — | — |

San Diego's traders add their own markup (sell 2–3×, per the
[vanilla audit](development/vanilla-economy-audit.md#merchant-multipliers)).
Only the machinery is late-game priced (owner correction, 29 September 2026).
Since Manufacturing 0.26.0 refining is a business (owner decision, 1 October
2026): a charge from mined ore earns 1.5 to 2.5 times the ore at base prices, and
the native checks verify it from live definitions. See
[Refining as a business](#refining-as-a-business-framework-0680-manufacturing-0260-shipbreaker-0620)
below; the 30 September guardrails (half again the inputs, stock below its ore)
are superseded.

**Where to buy and sell.** The same routes as the Shipbreaker S3/T2, in lots of
eight: K-Leg supplies (broken), the K-Leg fixer (worn), San Diego Halvorson
(new), the Venus scrap kiosk (broken and refurbished), and the fifteen regional
supply kiosks, each at the 85% equipment floor. Every form carries the game's
own high-salvage mark (`IsSalvageValueHigh`), as every native loose item above
$20,000 does, so the machines sell back like the game's high-value salvage: the
K-Leg fixer buys them intact, the Venus scrap kiosk buys intact or broken, and
the K-Leg supplies kiosk does not buy them.

**World finds** are rare: one engineering-loot roll in twenty
(`Manufacturing.machinerySalvageChance`, 0.05) yields a machine, three times in
four a broken one, split evenly across the three families. Manufacturing 0.1.0
had a 40% chance, which at these prices would have been a money printer.

**Work** (unit tool and skill multipliers; progress thresholds in brackets):

| Equipment | Install / uninstall | Repair broken | Dismantle | Full wear-bar Restore | Repair + Restore |
|---|---:|---:|---:|---:|---:|
| V4 refinery | 24 / 19.2 min (2000/1600) | 72 min (6000) | 96 min (1600) | 150 min | 207 min |
| X2 processor | 14.4 / 12 min (1200/1000) | 43.2 min (3600) | 54 min (900) | 90 min | 124.2 min |
| H2 store | 14.4 / 12 min (1200/1000) | 36 min (3000) | 54 min (900) | 60 min | 90 min |
| K2 reactor | 14.4 / 12 min (1200/1000) | 43.2 min (3600) | 54 min (900) | 90 min | 124.2 min |
| M2 store | 14.4 / 12 min (1200/1000) | 36 min (3000) | 54 min (900) | 60 min | 90 min |
| P1 manifold | 7.2 / 6 min (600/500) | 21.6 min (1800) | 18 min (300) | 30 min | 48.6 min |

The IC fusion reactor's thresholds are 2000/2000 with a 6000 repair; the radars
1000/1000 with 1600; the heavy lift rotor's repair is 5000.

**Repair bills** use the game's own components, as its late-game kit does (the IC
fusion reactor needs a motor, a screen, two heat sinks, two mainboards, seven
electronic and five mechanical parts and three steel):

| Repaired equipment | Steel | Aluminium | Mechanical | Electronic | Motor | Mainboard | Heat sink | Screen | Base value | Spent material returned |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V4 refinery | 4 | 2 | 6 | 8 | 2 | 2 | 2 | 1 | $572.60 | 28 kg |
| X2 processor | 2 | 2 | 3 | 8 | 1 | 3 | 2 | 0 | $285.40 | 16.5 kg |
| H2 store | 6 | 2 | 6 | 2 | 0 | 1 | 0 | 0 | $100.30 | 12.5 kg |
| K2 reactor | 3 | 2 | 4 | 6 | 1 | 2 | 2 | 0 | $247.50 | 16.5 kg |
| M2 store | 6 | 2 | 6 | 2 | 0 | 1 | 0 | 0 | $100.30 | 12.5 kg |
| P1 manifold | 1 | 1 | 2 | 2 | 0 | 1 | 0 | 0 | $61.20 | 4.5 kg |

**Dismantling** conserves mass and returns components too (motor 2.5 kg,
mainboard 0.5 kg, heat sink 1.5 kg, screen 6 kg; other units as above):

| Equipment/state | Steel | Aluminium | Mechanical | Electronic | Motor | Mainboard | Heat sink | Screen | Trash | Total | Value / whole |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| V4 intact | 100 | 40 | 20 | 10 | 2 | 2 | 2 | 1 | 10 | 180 kg | $1,059.50 / 1.7% |
| V4 broken | 90 | 32 | 10 | 4 | 1 | 0 | 1 | 0 | 47 | 180 kg | $534.55 / 3.3% |
| X2 intact | 70 | 26 | 16 | 12 | 1 | 3 | 2 | 0 | 13 | 130 kg | $680.25 / 1.8% |
| X2 broken | 34 | 10 | 4 | 0 | 0 | 1 | 1 | 0 | 82 | 130 kg | $202.50 / 2.1% |
| H2 intact | 110 | 30 | 12 | 3 | 0 | 1 | 0 | 0 | 12 | 160 kg | $550.60 / 2.5% |
| H2 broken | 40 | 10 | 4 | 0 | 0 | 0 | 0 | 0 | 108 | 160 kg | $180.40 / 3.3% |
| K2 intact | 80 | 30 | 16 | 11 | 1 | 2 | 2 | 0 | 20 | 150 kg | $689.00 / 1.6% |
| K2 broken | 36 | 12 | 4 | 0 | 0 | 1 | 1 | 0 | 98 | 150 kg | $212.70 / 1.9% |
| M2 intact / broken | as the H2 store | | | | | | | | | 160 kg | $550.60 / 2.6%, $180.40 / 3.4% |
| P1 intact | 4 | 2 | 2 | 2 | 0 | 0 | 0 | 0 | 2 | 10 kg | $55.70 / 0.2% |
| P1 broken | 3 | 1 | 2 | 0 | 0 | 0 | 0 | 0 | 5 | 10 kg | $22.15 / 0.4% |

Recovery sits within the vanilla range (RCS cluster 1.0%, battery 2.5%, towing
brace 2.6%). Empty the V4's feed and tray, the X2's hold and the H2 store before
dismantling; the shared guards refuse otherwise. Existing saved machines keep
their identities and state; the new prices and bills apply to definitions, and
merchants update at their normal restock.

## Manufacturing 0.4.0: store sizes and the L2

Every gas store comes in three sizes through Framework's shared ladder (see
[the refinery record](development/manufacturing-refinery-and-chemistry.md)): the
medium and large sizes hold 2.475 and 4.8 times the small one, weigh 1.9 and 2.8
times as much, and cost about 1.63 and 2.3 times as much. Their broken price is a
quarter. Work, repair bills and salvage grow with the footprint:

| Store size | Install / uninstall | Repair broken | Dismantle | Restore | Repair bill (St Al Me El Mb) |
|---|---:|---:|---:|---:|---|
| Small (2 x 2) | 1200 / 1000 | 3000 | 900 | 60 min | 6 2 6 2 1 |
| Medium (3 x 3) | 1600 / 1300 | 3900 | 1200 | 75 min | 8 3 8 3 1 |
| Large (4 x 4) | 2000 / 1600 | 4800 | 1500 | 90 min | 10 4 10 4 2 |

Salvage keeps the small stores' fittings (12 mechanical, 3 electronic parts and a
mainboard intact; 4 mechanical parts broken) and fills the rest of the dry mass
with steel, aluminium and retained trash in the small stores' proportions, so
every size conserves mass. The medium and large sizes are sold through the same
merchant and regional routes but are never salvage loot; the one-in-twenty
engineering find is split across the machines and the small stores.

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Fennmark L2 canister filling station | 120 kg | $26,000 | $6,500 | 1200 / 1000 | 3000 | 900 | 60 min |

The L2's repair bill is 3 steel, 2 aluminium, 4 mechanical and 4 electronic
parts, a motor, a mainboard and a heat sink. It dismantles to 70 steel, 20
aluminium, 11 mechanical and 6 electronic parts, a motor, a mainboard, a heat
sink and 17 kg of retained trash (120 kg); broken, to 40 steel, 10 aluminium, 4
mechanical parts and 68 kg of trash.

Bulk oxygen, nitrogen and carbon dioxide are sold into installed stores through
the station Bulk supplies view at the game's own gas price per kilogram, in steps
of 10 kg. Since Framework 0.68.0 the kiosk buys any stored gas back at 45% of
that price.

## Shipbreaker 0.40.0: S4 and S5 silos

Framework's since 0.58.0, with these figures unchanged; see the last section.

The S4 and S5 scale from the S3 through Framework's shared size ladder (the same
rule as the gas stores): capacity 1,960 and 3,330 kg, dry mass 365 and 465 kg,
price $6,780 and $8,860 (broken a quarter). Work grows with the footprint: install
1600 and 2000, uninstall 1200 and 1500, repair 3000 and 3600, dismantle 1000 and
1200, Restore 40 and 50 minutes. Repair takes 3/3/6 and 4/4/8 steel, aluminium and
mechanical parts. Salvage keeps the S3's fittings (20 mechanical, 4 electronic
parts intact; 4 mechanical broken) and fills the rest of the dry mass with steel,
aluminium and retained trash in the S3's proportions. They are sold on the S3's
routes and never appear in salvage loot.

## Shipbreaker 0.43.0: Y2, Y3 and Y4 material bins

The Y2 costs $2,400 (broken $600): about half the game's own Storage Bay per
grid cell ($150 against $263), because a bin only takes mined material. The Y3
and Y4 scale from it through Framework's shared size ladder: 115 and 170 kg
empty, $3,900 and $5,510 (broken a quarter), $108 and $86 per cell. Work: install
800, 1100 and 1400; uninstall 600, 800 and 1000; repair 1500, 2000 and 2500;
dismantle 400, 550 and 700; Restore 15, 20 and 25 minutes. Repair takes 1/1/2,
2/2/4 and 3/3/6 steel, aluminium and mechanical parts. Intact salvage returns
40/8/8 (Y2), 79/16/8 and 119/24/8 steel, aluminium and mechanical parts with
the rest of the housing as retained trash; broken salvage keeps two mechanical
parts and returns mostly trash. Salvage stays below a quarter of the price. The
Y2 is sold on the silos' routes and may turn up in engineering salvage; the Y3
and Y4 never appear in salvage loot.

## Agriculture 0.20.0: R4 and R5 reservoirs

Historical: the reservoirs retired in Agriculture 0.31.0 and convert to silos on load.

The R4 and R5 scale from the R3 through Framework's shared size ladder: 235 and
400 kg of water, 38 and 49 kg empty, $635 and $830 (broken a fifth, as the R3).
Repair needs 2 and 3 small mechanical parts and aluminium scraps plus one
electrical part, over 2400 and 3000 progress; dismantling returns 8 and 12 steel
scraps (2 and 3 broken) with the rest of the housing as waste, over 800 and 1000.
They are sold on the R3's routes and never appear in salvage loot.

## Manufacturing 0.5.0: the A2 cabin air regulator

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Fennmark A2 cabin air regulator | 60 kg | $23,000 | $5,750 | 1000 / 800 | 2400 | 600 | 45 min |

Its repair bill is 2 steel, 1 aluminium, 3 mechanical and 3 electronic parts, a
motor and a mainboard. It dismantles to 30 steel, 12 aluminium, 6 mechanical and
5 electronic parts, a motor, a mainboard, a heat sink and 8 kg of retained trash
(60 kg); broken, to 20 steel, 6 aluminium, 2 mechanical parts and 33 kg of trash.
It is sold on the other Fennmark machines' routes, carries the high-salvage mark
and shares their one-in-twenty engineering find.

## Medical 0.1.0: the Halewright Ward-3 medical bed

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Halewright Ward-3 medical bed | 92 kg | $24,000 | $6,000 | 1300 / 1300 | 2000 | 1500 | 45 min |

Priced above the game's Infirmaway ($18,570) for its powered care, patient handling
and drawer, with the Infirmaway's own work figures. Its repair bill follows the
Infirmaway's: 2 steel, 3 aluminium, 3 mechanical and 3 electronic parts, a motor, a
mainboard and 6 clean scrap cloth for the upholstery. It dismantles to 30 steel, 30
aluminium, 8 mechanical and 8 electronic parts, 2 motors, 2 mainboards and 18 kg of
retained trash (92 kg); broken, to 26 steel, 24 aluminium, 4 mechanical and 2
electronic parts and 39 kg of trash. Sold new at the K-Leg furnishings kiosk (where the
game sells its own medical bed), worn by the K-Leg fixer, broken at the K-Leg supply
kiosk, refurbished at the Venus scrap kiosk, at the regional supply kiosks and for scrip
at Friendly standing; one engineering-loot roll in fifty finds one, usually broken. It
carries the high-salvage mark. The figures are in `mods/PhobosMedical/framework/economy.json`.

## Medical 0.3.0: the Halewright Vigil-2 patient monitor

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Halewright Vigil-2 patient monitor | 34 kg | $9,500 | $2,375 | 600 / 600 | 1200 | 600 | 30 min |

A bedside instrument cart priced under the bed and under 10,000 credits, so Warm at the
faction kiosks. Its repair bill is 1 aluminium, 2 mechanical and 3 electronic parts, a
mainboard and a screen. It dismantles to 10 steel, 10 aluminium, 4 mechanical and 8
electronic parts, 2 mainboards, a screen and 1 kg of trash (34 kg); broken, to 10 steel,
8 aluminium, 2 mechanical and 4 electronic parts and 13 kg of trash. Sold on the Ward-3's
routes and shares its rare engineering find.

## Manufacturing 0.9.0: Q2, Q3 and Q4 ammonia stores

The ammonia stores follow the other gas stores exactly: $20,000, $32,530 and
$45,950 (broken a quarter), 160, 305 and 450 kg empty, with the same work, repair
bills, salvage and routes as the other store sizes above. The small size shares
the one-in-twenty engineering find; the medium and large sizes are never salvage
loot. No station sells ammonia: it comes only from the V4's salt crust charge.

The ammonium salt crust is ore (mined, never sold by merchants; the government
kiosks buy it at 150 cr, the game's hydrates price). Refining it is for the
nitrogen, not the money: 150 cr of crust becomes about 3.25 cr of ammonia at the
game's own NH3 price, 5.05 cr of water and a spent salt cake at the technical minimum.

## Manufacturing 0.10.0: the Tolvane AX-2 ammonia cracker

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Tolvane AX-2 ammonia cracker | 150 kg | $42,000 | $10,500 | 1200 / 1000 | 3600 | 900 | 90 min |

The AX-2 is built like the K2 (catalyst bed, heat exchanger, controls) and shares
its repair bill and mass-balanced salvage: 3 steel, 2 aluminium, 4 mechanical and 6
electronic parts, a motor, two mainboards and two heat sinks to repair; intact
salvage 80 steel, 30 aluminium, 16 mechanical and 11 electronic parts, a motor, two
mainboards, two heat sinks and 20 kg of retained trash (150 kg). It is sold on the
other Manufacturing machines' routes, carries the high-salvage mark and shares
their one-in-twenty engineering find.

## Manufacturing 0.18.0: the Lixivar LC-3 leach and crystallise unit

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Lixivar LC-3 leach and crystallise unit | 220 kg | $48,000 | $12,000 | 1600 / 1300 | 4800 | 1200 | 120 min |

The LC-3 sits between the AX-2 and the V4 in price. Repair takes 3 steel, 2
aluminium, 5 mechanical and 6 electronic parts, two motors, a mainboard, a heat
sink and a screen; intact salvage is 110 steel, 40 aluminium, 20 mechanical and 12
electronic parts, two motors, two mainboards, two heat sinks, a screen and 39 kg of
retained trash (220 kg). It is sold on the other Manufacturing machines' routes and
at the faction kiosks for Trusted standing, carries the high-salvage mark and
shares their one-in-twenty engineering find.

Its products are never sold by merchants: potassium sulfate 190 cr (0.70 kg),
struvite 125 cr (0.43 kg), phosphate concentrate 110 cr (0.25 kg), and the leached
residue, brine salt cake, caustic remainder and calcined residue at the technical
minimum (prices from Manufacturing 0.26.0; 42, 17 and 12 cr before). The evaporite
crust is ore (mined, never sold; the government kiosks buy it at 150 cr). Leaching
one earns 300 cr of salts, twice the crust; struvite gains a little on its
concentrate. The makeup formulation is the owner's exception (30 September 2026,
reaffirmed 1 October because fertiliser is rare in the game's world): its 39
packets carry Agriculture's own 30 cr price, 1,170 cr from about 440 cr of salts,
and no merchant sells the salts, so no trade loop pays.

## Agriculture 0.27.0: Groundwork nutrient hoppers

| Equipment | Mass | Base price | Broken base | Repair | Dismantle |
|---|---:|---:|---:|---:|---:|
| Groundwork E2 nutrient hopper | 15 kg | $300 | $60 | 1200 | 400 |
| Groundwork E3 nutrient hopper | 29 kg | $490 | $98 | 1800 | 600 |
| Groundwork E4 nutrient hopper | 42 kg | $690 | $138 | 2400 | 800 |

The hoppers follow the reservoir ladder: a component repair bill (one small
mechanical part, one small electronic part, one aluminium scrap, growing with the
step), steel scrap salvage with the rest of the housing retained, four to a lot on
the reservoirs' routes, only the E2 in salvage loot, and the faction kiosks at Warm
standing. The E2 joins Agriculture's engineering-salvage share, which is split one
more way: each machine falls from 3% to 2.5% of a roll, the same total. Station kiosks fill them with crop nutrients at 1,500 cr/kg under Bulk
supplies, the same per kilogram as the 500 g bulk charge (750 cr) and the 40 g
packet (60 cr), so no route is cheaper and nothing sells back.

## Manufacturing 0.19.0: the SA-3 acid plant and AT acid tanks

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Lixivar SA-3 sulfuric acid plant | 260 kg | $56,000 | $14,000 | 1800 / 1400 | 5400 | 1400 | 135 min |
| Lixivar AT-2 sulfuric acid tank | 240 kg | $16,000 | $4,000 | 1200 / 1000 | 3000 | 900 | 75 min |

The SA-3 is billed like the LC-3 with a second heat sink and more steel (intact
salvage 140 steel, 40 aluminium, 20 mechanical and 12 electronic parts, two
motors, two mainboards, two heat sinks, a screen and 49 kg of retained trash,
260 kg). The AT-2 is billed like a gas store (intact salvage 180 steel, 30
aluminium, 12 mechanical and 3 electronic parts, a mainboard and 22 kg of trash,
240 kg); the AT-3 and AT-4 follow the store size ladder, purchase-only. Both carry
the high-salvage mark and share the other machines' routes, the SA-3 at Trusted
and the tanks at Friendly faction-kiosk standing. Stations sell sulfuric acid into
a tank at the game's own 3.1 cr/kg, and since Framework 0.68.0 buy it back at
45% of that. The phosphoric acid flask is 300 cr (30 cr before Manufacturing
0.26.0) and the roasted calcine is trash; neither is sold by merchants.

## Manufacturing 0.20.0: the LC-3's acid recipes

No new equipment. The LC-3 gains three recipes and three materials, none sold by
merchants:

| Material | Unit | Base price | Per kg |
|---|---:|---:|---:|
| Epsom salt | 0.432 kg | $13 (0.26.0; $7 before) | about $30 |
| Ammonium sulfate | 0.232 kg | $20 (0.26.0; $8 before) | about $86 |
| Olivine leach cake (terminal) | 14.33 kg | $0.01 | trash |

The olivine charge's 32 Epsom salt ($416) earn 2.3 times the $180 ore, with
$26.80 of acid and $95.20 of water at station prices besides; the acid-route
struvite's three struvite and three ammonium sulfate ($435) gain about 28% on the
$300 flask and three Epsom salt ($339). Crop nutrients made aboard go straight
into a hopper at Agriculture's own 1,500 cr/kg (the owner's formulation
decision). Bagged into bulk charges they sell like any other; every salt in the
blend is made aboard from mined feed, so bought stock alone never pays. At about
4,150 cr of nutrients a charge from about 75 cr of salts, the formulation is a
strong earner.

## Framework 0.58.0: one water silo ladder

The Rivetline S3 to S5 silos moved from Shipbreaker to Framework with every
price, work figure, bill and salvage unchanged, and Framework adds a smaller S2
one tile narrower than the S3 on the same size ladder:

| Silo | Empty | Price | Broken | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| S2 (2 x 2, 400 kg of water) | 125 kg | $2,950 | $737 | 800 / 600 | 1800 | 600 | 20 min |
| S3 (3 x 3, 1,000 kg) | 240 kg | $4,800 | $1,200 | 1200 / 900 | 2400 | 800 | 30 min |

The S2's repair takes one steel scrap, one aluminium scrap and two small
mechanical parts. Its salvage keeps the S3's fittings (20 mechanical and 4
electronic parts intact, 4 mechanical broken) and fills the rest of the housing
with steel, aluminium and retained trash in the S3's proportions; intact salvage
is worth about $480 against the silo's $2,950. Every size sells on the silos'
routes in lots of eight, and only the S3 turns up in engineering salvage: its
share moved with it (3.3% of a roll, half of it broken), and Shipbreaker's
machinery roll fell by the same amount, so no family's odds changed.

Agriculture 0.31.0 retires the R3, R4 and R5 reservoirs. Saved ones convert to
the S3, S4 and S5 on load; the heavier housing raises the converted item's mass
and base value to the silo's. Merchants no longer offer reservoirs, and
Agriculture's machinery salvage roll fell from 30% to 25% with the R3's share,
leaving every remaining family's chance as it was.

## Manufacturing 0.24.0: the Lixivar acid line

| Item | Mass | Price | Repair | Dismantle |
|---|---:|---:|---|---|
| Lixivar acid line segment | 1 kg | $6 | 120 work, one steel scrap | 120 work, 1 kg retained waste |

Twice the plain gas or process-water line ($3) for its lining. It sells at the
same four merchants in lots of 128, at the supplies floor, and for scrip at the
faction kiosks at any standing. Dismantling returns no clean metal, because
acid-wetted lining is not recovered as scrap. Since Manufacturing 0.25.0 a segment
holding acid must be drained before it can be dismantled.

## Framework 0.61.0: the Rivetline conveyor belt

| Item | Mass | Price | Repair | Dismantle |
|---|---:|---:|---|---|
| Rivetline conveyor belt segment | 4 kg | $24 | 120 work, one steel scrap | 180 work, 4 kg retained waste |

Priced above the steel in it (scrap steel is 3.6 cr/kg) and sold with the lines at
the same four merchants in lots of 128 and for scrip at the faction kiosks at any
standing. Belts use no power of their own: the machine that sends or receives an
item pays for its transfer, as before.

## Framework 0.63.0: the Rivetline D20 drain canister

| Item | Mass | Price | Repair | Dismantle |
|---|---:|---:|---|---|
| Rivetline D20 drain canister | 3 kg empty | $40 | none | none |

A 20 litre lined steel can, priced above its 3 kg of steel (scrap steel is 3.6
cr/kg). It sells at every general market and the four expanded merchants in lots
of 16, and for scrip at the faction kiosks at any standing. It never stacks, since
each canister records the liquid it holds. What it holds is the line's own water
or acid, carried back to a tank, so it creates no value. See
[draining and venting](lines-and-draining.md).

## Refining as a business (Framework 0.68.0, Manufacturing 0.26.0, Shipbreaker 0.62.0)

Owner decision, 1 October 2026: refining is to be a reasonably profitable
business, retroactively. The rules and the worked chains are in
[the design record](development/refining-business-and-interdependencies.md).

| Chain | Ore | Products | Ratio to ore |
|---|---:|---:|---:|
| Meteoric iron to nickel-iron ingots | $450 | $882 | 1.96 |
| Iron and carbon ore to nickel steel | $470 | $1,042 | 2.22 |
| Carbon ore to carbon stock | $99 | $202 | 2.04 |
| Evaporite crust to salts | $150 | $300 | 2.00 |
| Olivine to Epsom salt (plus $122 of acid and water) | $180 | $416 | 2.31 |
| Sulfide nodule to acid and a flask (plus $99 of oxygen and water) | $150 | $324 | 2.16 |
| Methane ice thawed in a T2 | $100 | $205 | 2.05 |

- Water ice, hydrates, clay and the salt crust's gases stay supply: their bulk is
  worth less than the ore, at the game's gas prices and our 10 cr/kg water.
- Fertiliser (makeup packets, crop nutrients) stays above the band because it is
  rare in the game's world.
- The refuelling kiosk buys bulk back from installed stores (every Manufacturing
  gas store, the acid tanks and Framework's water silos) at 45% of its own
  selling price, inside the game's own 0.4 to 0.5 kiosk range. Hopper nutrients
  are not bought back. Buying and selling straight back loses 55%.
- A conversion fed only by bought stock earns at most 1.25 times its inputs; no
  current charge is fed that way.
- The game's methane ice is corrected in place from 20 to 100 cr.
- Saved materials and methane ice take their current prices when a save loads
  (Framework's load-time price refresh); nothing else in a save changes.
- Payback is long for the small chains: a machine's price divided by its gain per
  charge is 112 to 742 charges, 99 to 742 hours of running. The owner will judge
  this in play.

## Manufacturing 0.52.0: the Oxsmith EC-4 electrolysis cell

| Equipment | Mass | Base price | Broken base | Install / uninstall | Repair | Dismantle | Restore |
|---|---:|---:|---:|---:|---:|---:|---:|
| Oxsmith EC-4 electrolysis cell | 420 kg | $96,000 | $24,000 | 2600 / 2000 | 7800 | 2000 | 180 min |

The EC-4 is the dearest Manufacturing machine, half again the V4 (agent default, open to
owner revision), and billed above it: repair takes 6 steel, 3 aluminium, 8 mechanical and
10 electronic parts, three motors, two mainboards, four heat sinks and a screen. Intact
salvage is 260 steel, 60 aluminium, 30 mechanical and 20 electronic parts, four motors, two
mainboards, six heat sinks, a screen and 49 kg of retained trash (420 kg); broken salvage is
230 steel, 50 aluminium, 14 mechanical and 6 electronic parts, two motors, two heat sinks and
122 kg of trash. It carries the high-salvage mark, shares the other machines' routes and
sells at Trusted faction-kiosk standing. Ferrosilicon is 2 cr a 2.2 kg unit and is not sold
by any merchant; spent ferrosilicon is a terminal remainder at the technical minimum.
