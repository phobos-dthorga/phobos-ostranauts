# Water silos for the W2, E2 to E4 nutrient hoppers and bulk station supplies

A W2 can draw its irrigation water from a Rivetline process water silo, and dose
nutrients from a Groundwork hopper, so the growing shift runs for days without a
crew member swapping charges. The silos are Phobos Framework's shared water
tanks (see [the process water silo guide](shipbreaker-bulk-silos.md)); every
Phobos mod that uses process water draws from the same ones. These are source
and build features; automated checks are not owner-run Unity evaluation.

## Your old reservoirs (Agriculture 0.31.0)

The Groundwork R3, R4 and R5 agricultural water reservoirs are retired. When
you load a save, every one of them becomes the Rivetline silo of the same size:
R3 to S3, R4 to S4 and R5 to S5, in whatever state it was in (installed, loose,
damaged). It keeps its place, rotation, water, trapped water, reserve, inventory
and W2 links, and the game posts one notice listing what changed. The only
honest difference is the housing: a silo's shell is heavier and worth more, so
the item's mass and value rise to the silo's. Nothing needs doing by hand.
Merchants no longer sell reservoirs, and they have left the INSTALL menu.

## Equipment and supplies

| Item | Capacity / mass | Acquisition and base price |
|---|---|---|
| Phobos' Rivetline S2 Process Water Silo | 2 x 2 tiles; 400 kg water; 125 kg empty | 2,950 cr; general markets; INSTALL → APPS |
| Phobos' Rivetline S3 Process Water Silo | 3 x 3 tiles; 1,000 kg water; 240 kg empty | 4,800 cr; the same merchants; INSTALL → APPS |
| Phobos' Rivetline S4 and S5 Process Water Silos | 4 x 4 and 5 x 5 tiles; 1,960 and 3,330 kg water; 365 and 465 kg empty | 6,780 and 8,860 cr; the same merchants; INSTALL → APPS |
| Phobos' Verdemorrow Groundwork Bulk Nutrient Charge | One inventory slot; 0.5 kg dry formulated nutrient stock | 750 cr; eight per merchant offer, or one per station purchase |
| Phobos' Verdemorrow Groundwork E2, E3 and E4 Nutrient Hoppers | 2 x 2, 3 x 3 and 4 x 4 tiles; 10, 25 and 48 kg of crop nutrients; 15, 29 and 42 kg empty | 300, 490 and 690 cr; Agriculture merchants, four per successful offer; INSTALL → APPS; only the E2 turns up in salvage |

A silo holds clean process water only. It is not a drinking-water tank and not
a nutrient-solution or recovery receiver. Existing racks, W2, B2, small packets
and Ship's Water keep their roles. No new chemical assay, crop yield, growth
speed or pump power comes with the silos. The 500 g charge uses the same
aggregate formulation and value per kilogram as the other selected charges.

## Feed a W2 from a silo

1. Install the silo on intact floor and put the W2 within one tile of it
   (touching or with one tile between them, on any side, diagonals included),
   or anywhere aboard with Framework's **process-water line** laid so it runs
   under or right beside both the silo and the W2, on any side.
2. Pause W2 operation and receiving. On the W2's Supplies page choose **Water
   silo connection**, pick the silo and Apply. The list says how each silo is
   reached and marks empty ones. One silo can feed several W2s and other
   machines at once; its own Control Panel lists every machine linked to it.
   C1 offers the same W2 choices.
3. Fill the silo using station **Bulk supplies**, or put ordinary 5 kg
   irrigation charges in its inventory, right-click it and choose **Load one
   5 kg irrigation charge**. Each load takes ten seconds before skill modifiers.
4. On the silo's Control Panel, choose how much to **Keep in reserve** (none, a
   tenth, a quarter, a half or all of it). The W2 draws only above that. On the
   W2, choose the refill target (5, 10, 15 or 19.5 kg; default 19.5). Live W2
   headroom can reduce it further.
5. Enable W2 receiving and Resume distribution. Existing rack pairs, conduits
   and receiving permissions still apply.

The linked silo replaces the optional Ship's Water *inlet* only. If the silo is
empty, blocked, damaged, missing or down to its reserve, the W2 waits; it never
falls back to drinking water on its own. Disconnect the silo while paused to
restore the previous intake option, including its crew-water reserve.

The silo is storage, not a second pump: intake shares the W2's received
electricity and throughput budget with output and blending. A 120 kg intake uses
0.12 kWh of the existing 0.001 kWh/kg transfer budget. Room heat follows the
existing W2 accounting, and W2 treatment still suspends distribution.

## Nutrient hoppers (0.27.0)

A Groundwork nutrient hopper keeps formulated crop nutrients by the kilogram, so a
W2 can mix for many cohorts without a crew member swapping charges.

1. Install the hopper within one tile of the W2 (touching or one tile between
   them, diagonals included).
2. Fill it at a station: the refuelling kiosk's **Bulk supplies** view offers
   **Crop nutrients (Groundwork hoppers)** by the kilogram at 1,500 cr/kg, the same
   as a bulk charge or a 40 g packet. Choose the hopper as the destination and
   review the quote. Nothing sells back. With Phobos Manufacturing 0.20.0 or newer,
   a Lixivar LC-3 within one tile can also fill it: link the hopper on the LC-3's
   panel and run its crop nutrient recipe (see
   [the Manufacturing guide](manufacturing-player-guide.md#the-leach-unit)).
3. Pause the W2, open its Supplies page and pick the hopper as its nutrient source
   (the same field as a charge in its inventory). Resume. While mixing, the W2
   takes only what each step needs, through the same guarded transfer it uses for
   water, and the panel shows how much the hopper still holds.

A damaged hopper traps its nutrients in a catch chamber: nothing leaks, but it
cannot dose or be filled until it is repaired and a crew member chooses
**Recover trapped nutrients after repair**. To empty a hopper before moving or
dismantling it, choose **Bag up to 500 g as a bulk nutrient charge**: each job
packs up to 500 g into an ordinary bulk charge in the hopper's four-cell rack, at
the same value per kilogram. Full bags stack three to a cell (since Agriculture
0.37.0), so the rack holds twelve. When it is full the crew log says so: take the
charges out of its Inventory and carry on. A hopper that still holds nutrients refuses to be moved or
dismantled. The contents are the crop model's single aggregate nutrient figure,
the same as every charge and packet.

## Nutrient charges and crew

Put the new charge in W2's inventory, pause, select that exact charge under
Supplies and Resume. Its mass and value fall as nutrients are used. Empty charges disappear.
Repair and Restore cannot refill them.

Orders begin disabled. A silo's **Keep the silo stocked with irrigation charges**
order (its Control Panel's crew settings) hauls only ordinary 5 kg irrigation
charges from the approved input store and loads them when a whole charge fits.
It never drains or recovers trapped water automatically. W2's **Distribute feed
and replace selected charges** explicitly authorizes replacement with a physical
500 g, 40 g or finished B2 charge from its approved store. The ordinary
distribution order keeps its no-replacement meaning. Permissions, AutoTask,
roster, needs, pathfinding and actual worker access remain authoritative;
changing configuration suspends enabled orders.

The same W2 pump and resource rules run during supported bounded time-skips.
Charge loading and charge selection use the existing crew handling and travel
budget; they grant no elapsed-time pumping or duplicate training. Station
purchases, manual draining and trapped-water recovery are not automated skipped
work. Reload keeps identities and contents, but W2 intake stays paused until you
Resume.

## Station purchasing

Open the native refuelling interface at a serviced dock, then **Bulk supplies**.
Choose process water, crop nutrients or a nutrient charge, the exact destination
and quantity. Review the quote and use its separate **Buy quoted quantity**
button. Process water is 10 cr/kg in 10 kg steps into any Rivetline silo; one
quote can fill the chosen silo. Nutrient charges are one 500 g charge at 750 cr
into an accessible W2 inventory per purchase. These are limits chosen for
gameplay; station stock is not simulated as a finite supply.

The actual terminal user pays. Ownership, docking, terminal access, the quote,
destination revision and capacity are checked again on Buy. Closing before Buy
spends nothing. A known partial delivery refunds the undelivered portion and
records only the amount delivered. A completed quote cannot be replayed.
Choose a new quantity or destination for another purchase.

If a purchase cannot be confirmed, further bulk purchases are blocked and its
payment and delivery records are kept. Keep the save and report the fault; do not
erase those records to force another attempt. Packaged supplies and the usual
fuel and water services remain available. Recovery after a crash is not guaranteed.

The station entry is gated to the locally audited native assembly. Unsupported
versions receive no added entry and retain ordinary packaged supply. Blue Bottle
Games' native fuel rows/OnSubmit and [Valtora's Ship's Water](https://steamcommunity.com/sharedfiles/filedetails/?id=3757331189)
0.16.1 potable row are not replaced or called by this purchase. Other station
adapters and in-game coexistence still require owner evaluation.

## Damage, draining and removal

Damage traps a silo's water in a catch chamber; both share the silo's capacity,
and repeated damage cannot create another catch. After repair, choose **Recover
trapped water** on its Control Panel, or right-click **Recover trapped water
after repair** (60 seconds).

**Drain up to 20 kg for treatment** withdraws water as the existing recorded
process-solution item with exactly zero nutrients. It needs room in the silo's
inventory and takes 60 seconds. W2 treatment is lossy and still requires a
finite cartridge, power and output space. No nutrient value appears merely from
storing water.

A silo that holds water or has uncertain records refuses to be dismantled
instead of silently dropping its water; uninstalling keeps the water in the
loose silo. Repair bills, dismantling returns and times are in the
[silo guide](shipbreaker-bulk-silos.md). No rupture drops or atmospheric spills
are modelled. Native whole-ship destruction or despawn keeps its own rules; this
is not insurance against losing a ship.

## Design basis and verification

The [research report](development/agriculture-bulk-storage-research.md) separates science
from authored game balance. Bruce Dunn's [Oklahoma State University hydroponics
guide](https://extension.okstate.edu/fact-sheets/hydroponics), [NASA's porous-tube
nutrient-delivery research](https://technology.nasa.gov/patent/ksc-tops-73), and
Jay Garland of Bionetics' [NASA TM-107557](https://ntrs.nasa.gov/citations/19930008922)
inform the storage and managed-recovery distinction, not these capacities, yields
or safety assurances. No institutional endorsement is implied.

Automated checks cover storage conservation, reserve and catch round-trips,
competing reservations, partial and failed settlement, replay and unknown saves,
the reservoir-to-silo conversion (position, water, records and mass), native
definitions, economics, installation forms, localization and native-size art
exports. Owner Unity checks still include rotated placements, actual service
walking, C1 and local authority, station purchases with and without Ship's
Water, loading a save with R3 reservoirs, damage, one- and six-hour skips,
shared electrical demand, lighting and scaled or translated UI.
