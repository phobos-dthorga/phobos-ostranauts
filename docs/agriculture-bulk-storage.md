# R3, R4 and R5 agricultural water and bulk station supplies

Agriculture 0.14.0 / Framework 0.27.0 implement the first bulk-storage slice.
Shipbreaker 0.27.0 adds the corresponding compact selectors to C1. These are
source/build features; automated checks are not owner-run Unity evaluation.
No Steam publication is implied.

## Equipment and supplies

| Item | Capacity / mass | Acquisition and base price |
|---|---|---|
| Phobos' Verdemorrow Groundwork R3 Agricultural Water Reservoir | 3 x 3 tiles; 120 kg water; 25 kg empty, 145 kg full, plus physical inventory cargo | Empty loose hardware, 450 cr; Agriculture merchants, four per successful offer; INSTALL → APPS |
| Phobos' Verdemorrow Groundwork R4 Agricultural Water Reservoir | 4 x 4 tiles; 235 kg water; 38 kg empty | 635 cr; the same merchants; INSTALL → APPS; never found in salvage |
| Phobos' Verdemorrow Groundwork R5 Agricultural Water Reservoir | 5 x 5 tiles; 400 kg water; 49 kg empty | 830 cr; the same merchants; INSTALL → APPS; never found in salvage |
| Phobos' Verdemorrow Groundwork Bulk Nutrient Charge | One inventory slot; 0.5 kg dry formulated nutrient stock | 750 cr; eight per merchant offer, or one per station purchase |

The R4 and R5 work exactly like the R3 and hold more for less per kilogram of
capacity; everything below applies to every size. The R3's capacity and empty
weight are in the mod's framework/vessels.json and can be overridden (see
[editing the data files](editing-data-files.md)); the larger sizes follow. R3 is optional. Existing racks, W2, B2, small packets and Ship's Water retain
their roles. R3 holds clean agricultural water only. It is neither a potable
tank nor a nutrient-solution/recovery receiver. No new chemical assay, crop
yield, growth speed or pump power is introduced. The 500 g charge uses the same
aggregate formulation and value per kilogram as the existing selected charges.

## Set up a reservoir

1. Install the reservoir on intact floor and put the W2 within one tile of it:
   touching or with one tile between them, on any side, diagonals included.
   A Shipbreaker S3, S4 or S5 process water silo can feed a W2 the same way.
   The original R3 layout (W2 against its right edge) still works.
2. Pause W2 operation and receiving. Open either local Supplies panel, choose
   **Reservoir / supply connection**, select the other machine, and Apply.
   C1 exposes the same checked connection and reserve/target choices.
3. Fill R3 using station **Bulk supplies**, or put ordinary 5 kg irrigation
   charges in its inventory and choose **Load one 5 kg irrigation charge**.
   Each successful local load requires ten seconds before skill modifiers.
4. Select how much water to keep in the reservoir (0 to all of it, in steps of
   its size; 0–120 kg for the R3). Choose W2's refill target
   (5, 10, 15 or 19.5 kg; default 19.5). Live W2 headroom can reduce it further.
5. Explicitly enable W2 receiving and Resume distribution. Existing rack pairs,
   conduits and receiving permissions still apply.

The selected R3 replaces the optional Ship's Water *inlet* only. It never
silently falls back to drinking supplies if the R3 is empty, blocked, damaged,
missing or below reserve. Clear the selection while paused to restore the
previous intake option, including its crew-water reserve.

R3 uses W2's existing received-electricity and throughput budget: output,
blending and intake compete for that one budget. R3 adds storage, not a second pump. A 120 kg intake requires 0.12 kWh of the existing
0.001 kWh/kg transfer budget. Room heat follows the existing W2 accounting.
W2 treatment still suspends distribution; storage does not bypass it.

## Nutrient charges and crew

Put the new charge in W2's inventory, pause, select that exact charge under
Supplies and Resume. Its mass and value fall as nutrients are used. Empty charges disappear.
Repair and Restore cannot refill them.

Orders begin disabled. R3's **Maintain R3 water stock** order hauls only ordinary
5 kg irrigation charges from the approved input store and loads them when a
whole charge fits. It never drains or recovers catch water automatically.
W2's **Distribute feed and replace selected charges** explicitly authorizes
replacement with a physical 500 g, 40 g or finished B2 charge from its approved
store. The ordinary distribution order retains its previous no-replacement
meaning. Permissions, AutoTask, roster, needs, pathfinding and actual worker
access remain authoritative; changing configuration suspends enabled orders.

The same W2 pump and resource rules run during supported bounded time-skips.
R3 load and charge-selection work use the existing crew handling/travel budget;
they grant no elapsed-time pumping or duplicate training. Station purchases,
manual draining and catch recovery are not automated skipped work. Reload
retains identities/contents but W2 intake remains paused for explicit Resume.

## Station purchasing

Open the native refuelling interface at a serviced dock, then **Bulk supplies**.
Choose agricultural water or a nutrient charge, the exact destination and
quantity. Review the quote and use its separate **Buy quoted quantity** button.
Water is 10 cr/kg in 0.25 kg steps; one quote can fill the chosen reservoir. Nutrients
are one 500 g charge at 750 cr into an accessible W2 inventory per purchase.
These are limits chosen for gameplay; station stock is not simulated as a finite supply.

The actual terminal user pays. Ownership, docking, terminal access, the quote,
destination revision and capacity are checked again on Buy. Closing before Buy
spends nothing. A known partial delivery refunds the undelivered portion and
records only the amount delivered. A completed quote cannot be replayed.
Choose a new quantity/destination for another purchase.

If a purchase cannot be confirmed, further bulk purchases are blocked and its
payment/delivery records are kept. Keep the save and report the fault; do not
erase those records to force another attempt. Packaged supplies and the usual
fuel and water services remain available. Recovery after a crash is not guaranteed.

The station entry is gated to the locally audited native assembly. Unsupported
versions receive no added entry and retain ordinary packaged supply. Blue Bottle
Games' native fuel rows/OnSubmit and [Valtora's Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
0.16.1 potable row are not replaced or called by this purchase. Other station
adapters and in-game coexistence still require owner evaluation.

## Damage and recovery

Damage isolates service water in a catch chamber; both share the reservoir's
capacity. Repeated damage cannot create another catch. Repair uses the W2-style
bill (one mechanical small part, one electrical small part, one aluminium
scrap), retains actual spent repair material, and does not recover or resume
water. After repair, explicitly **Recover isolated water** locally (60 seconds).

Explicit **Drain** withdraws up to 20 kg as the existing recorded process-solution
item with exactly zero nutrients. It requires room in the inventory and uses
60 seconds. W2 treatment is lossy and still requires a finite cartridge, power
and output space. No nutrient value appears merely from storing water.

Uninstall/dismantle require empty, unpaired storage and clear journals. Filled or
uncertain individual reservoirs block native detach/destruction instead of
silently dropping their water. This conservative first slice does **not** add
rupture drops or atmospheric spills. Native whole-ship destruction/despawn
retains its own semantics; this is not insurance against losing a ship.
Dismantling takes 600 seconds and returns 4 kg steel + 21 kg housing waste
(damaged: 1 + 24 kg). The R4 and R5 return twice and three times the steel
with the rest of their housing as waste, and take longer to repair and dismantle.
No reservoir fabrication recipe is added.

## Shared vessels since Agriculture 0.18.0

Agriculture 0.18.0 keeps the R3's records, journals and guard under their
existing names but moves their custody into Framework 0.39.0's shared bulk
vessel service, the same one Shipbreaker's S3 silo uses. A saved R3 reads
unchanged. A Shipbreaker T2 ice thaw unit installed within one tile of an R3
can deliver its thaw water into the reservoir: link it from the T2's panel.
See the [process water silo and ice thaw unit](shipbreaker-bulk-silos.md).

## Design basis and verification

The [research report](development/agriculture-bulk-storage-research.md) separates science
from authored game balance. Bruce Dunn's [Oklahoma State University hydroponics
guide](https://extension.okstate.edu/fact-sheets/hydroponics), [NASA's porous-tube
nutrient-delivery research](https://technology.nasa.gov/patent/ksc-tops-73), and
Jay Garland of Bionetics' [NASA TM-107557](https://ntrs.nasa.gov/citations/19930008922)
inform the reservoir/managed-recovery distinction, not these capacities, yields
or safety assurances. No institutional endorsement is implied.

Automated checks cover storage conservation, reserve/catch round-trips,
competing reservations, partial and failed settlement, replay/unknown saves,
native definitions, economics, installation forms, localization and native-size
art exports. Existing crop/power/crew/skip tests remain applicable. Owner Unity
checks still include all rotated placements, actual service walking, C1/local
authority, station purchases with/without Ship's Water, save/load, damage,
one-/six-hour skips, shared electrical demand, lighting and scaled/translated UI.
