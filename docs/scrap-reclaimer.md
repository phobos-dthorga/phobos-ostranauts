# Scrap reclaimer — prepared 0.8.0 candidate

Framework and Shipbreaker **0.8.0**, built against the installed Ostranauts
1.0.1.4 baseline. This is implemented and checked offline, not yet tested in a
game session. Auto Nav remains 0.3.0 and is optional for this processing chain.

## Buying and building

The **Phobos Scrap Reclaimer** has a $14,800 refurbished base value; broken
equipment is $3,700. A pristine offer has the native 25% premium ($18,500), and
lightly worn stock uses the native 75% tier ($11,100). Actual merchant quotes
also depend on market and trading modifiers.

| Seller | Per-restock offers |
| --- | --- |
| K-Leg scrap supplies | 20% chance of one broken machine; independent 20% chance of one assembly section |
| K-Leg fixer | 10% worn and independent 10% refurbished machine |
| San Diego Halvorson industrial trader | 40% pristine machine; independent 40% assembly section |
| Venus orbital scrap kiosk | 15% broken machine |

These are normal additive stock entries. Existing merchant inventory is not
replaced when loading a save. Stock space, native restocking and Framework's
availability setting still apply. Arriving at a station does not guarantee a
unit is for sale. Existing Shipbreaker equipment retains its offers.

Construction uses an installed Bar/Dining Table, or a supported optional
workbench, with a Mortorq tool and soldering tool. Build two **90 kg reclaimer
assembly sections**, then assemble the **180 kg machine**.

| One section's bill | Quantity | Mass |
| --- | ---: | ---: |
| Steel scrap | 56 | 56 kg |
| Aluminium scrap | 26 | 26 kg |
| Small mechanical parts | 12 | 6 kg |
| Small electronic parts | 4 | 2 kg |
| **Total** | | **90 kg** |

Each section takes 75 minutes of configured assembly work; final assembly takes
45 minutes: **195 minutes** before fetching, skills and interruptions. Sections
cost $6,000 each at base value. Final assembly adds no hidden materials.

## Installation and use

Install on **4 x 4 interior floor tiles**, with the lower feed/service edge
accessible. Build electrical conduit separately. It has one active packet within
a **four-packet / 52 kg feed**, and a separate **8 x 8 output inventory**.

1. Right-click the reclaimer and choose **Control Panel**.
2. Choose **Reclaimer feed** and load separate, empty **Identified panel residue
   R2 (13 kg)** packets, produced by newly started wall jobs in this version.
3. Choose **Start / resume reclaimer**. Default operation takes **120 powered
   seconds at 12 kW**, nominally **0.4 kWh** per batch. Native power ticks can
   overrun the final fraction of a cycle; their delivered energy still makes heat.
4. Collect products through ordinary **Inventory** or the panel's product button.
   Each packet gives **3 kg steel + 1 kg aluminium + 9 kg terminal rejects**.
5. Store or haul the rejects, or pair the reclaimer's output to a collector through
   the existing routing controls. One sender per collector remains the rule.

Manual hauling supplies the reclaimer in this first version. There is no automatic
collector-to-reclaimer feed route. The collector now explicitly accepts legacy
13 kg residue, identified 13 kg residue and 9 kg terminal rejects. It retains its
four-slot limit and 52 kg maximum; existing pair IDs are unchanged. Collection
does not eject material or reduce ship mass.

Old **Mixed panel residue** is still unclassified and is not accepted as feed.
Terminal rejects cannot be fed back for another yield. New recipes do not rewrite
old cargo or partially completed wall jobs. See [saved-job compatibility](processing-job-compatibility.md).

F3 commands use the same service and access checks:

```text
phobosreclaimer help
phobosreclaimer status
phobosreclaimer controls [full machine ID]
phobosreclaimer start|pause|cancel|feed|products [full machine ID]
```

Supply a full ID when several reclaimers are present. Pause retains work; Cancel
retains the input but discards credited work, with no energy or cooling refund.
Reload retains recipe, duration, input and progress and waits for manual Start.
Full output, changed input or unavailable machinery stops without deleting feed.
There is no processing while the ship is unloaded.

## Heat and operating cost

This first appliance is **air cooled into the surrounding room**, not vacuum
cooled. It needs at least **10 kPa** at its service edge and a room with enough
thermal capacity to accept the next step without reaching **40 °C**. Failure
pauses work before requesting power. Cool/repressurize the room or reduce time
acceleration, then resume. Native ship cooling handles the room afterwards.

The electrical path uses native `Powered.UsePower` and its `GatherPower` remaining
demand result. Actual supplied energy, including partial brownouts, enters the
native gas temperature accumulator. No progress is credited during a brownout.
Idle draw is **0.1 kW** where cooling is available; blocked operation stops its
power request. Heat already delivered remains in the native room, including
after cancellation. This is not a new ship-wide thermal network.

Local inspection found that `Heater` uses **20.7 J/(mol K)** and
`GasContainer.fDGasTemp` for room heat. We reuse that native convention and its
saved accumulator. The pressure/temperature limits, power and cycle time are
Phobos balance choices, not manufacturer data. No new gases are introduced.
Native mixing, environmental loss, cooling equipment and accidents remain native.
Live timing, room selection and interaction with other thermal mods need gameplay
verification; offline arithmetic is not a claim of complete thermodynamic realism.

The nominal full cycle adds **1.44 MJ** to the room. For example, 10,000 mol of
gas gains about **6.96 K** before native cooling. Several appliances share the
pending heat budget, so one cannot ignore another's unprocessed temperature rise.

Recovered metal has **$11.90 base value** per packet. The complete new wall chain
produces $45.94 of useful stock plus a $0.01 reject, compared with a $21 wall
definition. Processing deliberately adds value; acquisition, capital, labour,
electricity and cooling still cost resources. At $11.90 per batch it takes more
than 1,240 batches to equal the reclaimer's base capital value even before those
costs. This is an illustrative gross comparison, not guaranteed trading profit.
Do not invent an electricity tariff or claim every live market is identical.

Reclaimer settings in the existing Shipbreaker config:

| Section/key | Default | Bounds | Meaning |
| --- | ---: | ---: | --- |
| `Reclaimer/CycleSeconds` | 120 | 30–3600 | New jobs only; saved durations persist |
| `Reclaimer/WorkingKilowatts` | 12 | 1–100 | Startup setting; affects resumed jobs and room heat |
| `Processing/ContinueQueue` | true | true/false | Shared queue continuation preference |

Dimensions, material identities/yields and thermal limits are fixed contracts.
Restart after configuration changes. Different power/duration settings change
energy per batch; they are not free speed upgrades.

## Maintenance and material accounting

| Work | Native progress target | Time at unit native multipliers |
| --- | ---: | ---: |
| Install | 1800 | 21.6 min |
| Uninstall | 1200 | 14.4 min |
| Repair | 4200 | 50.4 min |
| Dismantle machine | 1200 | 72 min |
| Dismantle section | 500 | 30 min |

Restore uses existing native wear maintenance, as for the processor. Repair
requires **4 steel, 2 aluminium, 6 mechanical parts and 4 electronic parts**;
11 kg of replaced material returns through Framework's service-waste mechanism.
Tools are reused. Native Repair's remaining wear still requires Restore.

| Dismantled object | Steel | Aluminium | Mechanical | Electronic | Trash | Total mass / base output value |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| Functional machine | 104 | 42 | 20 | 8 | 20 | 180 kg / $637.60 |
| Broken machine | 92 | 34 | 10 | 2 | 48 | 180 kg / $450.00 |
| Section | 52 | 21 | 10 | 4 | 10 | 90 kg / $318.80 |

Part units are 0.5 kg; scrap/trash units are 1 kg. Recovery loses monetary value
against selling the complete equipment, including tested native worn/broken
tiers. Both sections' raw material bill is $696.40, greater than the machine's
$637.60 dismantling yield. The [native value audit](equipment-value-audit.md)
checks these comparisons. Empty both feed and output before dismantling.

## Framework, assets and verification

Framework 0.8.0 now owns immutable `ProcessRecipe`/`ProcessRecipeCatalog`, saved
`ProcessJob` binding and material accounting in `Phobos.Ostranauts.Framework.Processing`.
Both actual processors use these, plus existing native registration, construction,
maintenance, stock and staged output delivery. Shipbreaker owns feed eligibility,
machine definitions, thermal adapter, art, prices and recipe identities.

The original Imagegen master and exact prompt are under `assets/phobos-reclaimer`.
The new machine uses a 64 x 64 sprite, neutral normal map and 256 x 256 portrait.
Closed installed/transport forms share that sprite and native damage tint; separate
state artwork and sculpted normals remain visual refinements. Packets and sections
reuse our existing original art, with distinct names and IDs. No game art is bundled.

Offline checks cover native construction/maintenance and stock eligibility, gross
and constituent budgets, legacy continuation, native job-save round trips,
output space, shared rollback/duplicate delivery rules, exact feed exclusions,
heat headroom, invalid intervals, brownout accounting hooks and saved heat fields.
Installer checks require the new art and Framework 0.8.0 before copying files.
The owner supplies the real gameplay check; no running-game or save manipulation
is part of preparing this build.

Inspected game assembly SHA-256:
`91b50f45cacd64de39b9bcc30ec7b4542f3e3976ac3bc5589b346976a262425e`.
