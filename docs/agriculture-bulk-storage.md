# R3 agricultural water and bulk station supplies

Agriculture 0.14.0 / Framework 0.27.0 implement the first bulk-storage slice.
Shipbreaker 0.27.0 adds the corresponding compact selectors to C1. These are
source/build features; automated checks are not owner-run Unity evaluation.
No Steam publication is implied.

## Equipment and supplies

| Item | Capacity / mass | Acquisition and authored base price |
|---|---|---|
| Phobos' Verdemorrow Groundwork R3 Agricultural Water Reservoir | 3 x 3 tiles; 120 kg water; 25 kg empty, 145 kg full, plus physical inventory cargo | Empty loose hardware, 450 cr; Agriculture merchants, four per successful offer; INSTALL → APPS |
| Phobos' Verdemorrow Groundwork Bulk Nutrient Charge | One inventory slot; 0.5 kg dry formulated nutrient stock | 750 cr; eight per merchant offer, or one per station purchase |

R3 is optional. Existing racks, W2, B2, small packets and Ship's Water retain
their roles. R3 holds clean agricultural water only. It is neither a potable
tank nor a nutrient-solution/recovery receiver. No new chemical assay, crop
yield, growth speed or pump power is introduced. The 500 g charge uses the same
aggregate formulation and value per kilogram as the existing selected charges.

## Set up a reservoir

1. Install R3 on intact floor. Place W2 on its right side, facing the same way,
   with its entire two-tile edge against R3. The two allowed W2 centre offsets
   are R3-local `(2.5, +0.5)` and `(2.5, -0.5)` tiles. Rotate both together.
   Leave the front (-Y) service rows of both machines clear and walkable.
2. Pause W2 operation and receiving. Open either local Supplies panel, choose
   **Reservoir / supply connection**, select the other machine, and Apply.
   C1 exposes the same checked connection and reserve/target choices.
3. Fill R3 using station **Bulk supplies**, or put ordinary 5 kg irrigation
   charges in its inventory and choose **Load one 5 kg irrigation charge**.
   Each successful local load requires ten seconds before skill modifiers.
4. Select how much water to keep in R3 (0–120 kg). Choose W2's refill target
   (5, 10, 15 or 19.5 kg; default 19.5). Live W2 headroom can reduce it further.
5. Explicitly enable W2 receiving and Resume distribution. Existing rack pairs,
   conduits and receiving permissions still apply.

The selected R3 replaces the optional Ship's Water *inlet* only. It never
silently falls back to drinking supplies if the R3 is empty, blocked, damaged,
missing or below reserve. Clear the selection while paused to restore the
previous intake option, including its crew-water reserve.

R3 uses W2's existing received-electricity and throughput budget: output,
blending and intake compete for that one budget. There is no R3 ticker or
extra pump allowance. A 120 kg intake requires 0.12 kWh of the existing
0.001 kWh/kg transfer budget. Room heat follows the existing W2 accounting.
W2 treatment still suspends distribution; storage does not bypass it.

## Nutrient charges and crew

Put the new charge in W2's inventory, pause, select that exact charge under
Supplies and Resume. Its actual remaining mass/value decrease through the
existing gradual dosing service. Empty charges disappear; no packaging mass,
water carrier, Repair or Restore is invented.

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
Water is 10 cr/kg in 0.25 kg steps, up to 120 kg per quote into one R3. Nutrients
are one 500 g charge at 750 cr into an accessible W2 inventory per purchase.
These are authored transaction caps, not finite station stock simulation.

The actual terminal user pays. Ownership, docking, terminal access, the quote,
destination revision and capacity are checked again on Buy. Closing before Buy
spends nothing. A known partial delivery refunds the undelivered portion and
records only the amount delivered. A completed quote cannot be replayed.
Choose a new quantity/destination for another purchase.

Interrupted or uncertain settlement keeps a protected journal on the payer and
blocks new bulk purchases; it does not guess a refund from a wallet balance.
Keep the save and report the blocker for evidence-based reconciliation. There
is deliberately no “clear journal and retry” button. Ordinary supply packages
and native services remain available. Native save writes are not a database
transaction, so crash-atomic payment/delivery is not promised.

The station entry is gated to the locally audited native assembly. Unsupported
versions receive no added entry and retain ordinary packaged supply. Blue Bottle
Games' native fuel rows/OnSubmit and [Valtora's Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
0.16.1 potable row are not replaced or called by this purchase. Other station
adapters and in-game coexistence still require owner evaluation.

## Damage and recovery

Damage isolates service water in a catch chamber; both share the same 120 kg
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
(damaged: 1 + 24 kg). No R3 fabrication recipe is added.

## Design basis and verification

The [research report](agriculture-bulk-storage-research.md) separates science
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
