# Agriculture maintenance and treatment economics — 0.7.0

## Maintenance

Blue Bottle Games' installed Ostranauts 1.0.1.5 native definitions supply the
material prices and work mechanism inspected by our [generated audit](agriculture-economy-evidence.md).
[Ostranauts](https://bluebottlegames.com/ostranauts) is the game whose mechanisms
we reuse. The following repair bills and durations are our authored balance;
merchant condition/market adjustments and crew/tool modifiers still apply.

| Equipment | New repair bill | Base material value | Unmodified repair time |
| --- | --- | ---: | ---: |
| Groundwork W2 | 1 small mechanical + 1 small electrical + 1 aluminium | 20.60 cr | 21.6 minutes |
| Irrigation conduit | 1 aluminium | 1.10 cr | 1.44 minutes |

The 2 cr conduit previously inherited 6.10 cr of repair inputs and 28.8 minutes
of work. It now has a simple metal repair. W2 servicing accounts for its pump
and controls. Rack/cooker bills, purchase prices, construction recipes and
salvage outputs are unchanged. Actual replaced material returns as spent waste.
Native saved work maxima can retain their old timing; this change does not edit
saved progress or reset an ongoing repair. New damaged forms use the new times.
The audit now compares W2 and conduits alongside the rack and cooker.

## Finite treatment medium

For newly queued jobs, one fresh **Groundwork Treatment Cartridge** costs 25 cr
at base value, contains 0.05 kg medium and treats **25 kg recorded drainage**.
It spends 0.002 kg medium per kg treated. Unused medium returns as a physical
cartridge with saved remaining capacity, proportional mass and proportional base
value. A whole drainage packet must fit one cartridge; partial cartridges are
not automatically combined. Read the bound-job W2 panel for remaining capacity.

| Drainage batch | Medium spent | Base capacity cost | Electricity |
| --- | ---: | ---: | ---: |
| 0.25 kg | 0.0005 kg | 0.25 cr | 0.0025 kWh |
| 5 kg | 0.0100 kg | 5.00 cr | 0.0500 kWh |
| 20 kg | 0.0400 kg | 20.00 cr | 0.2000 kWh |

Each batch still needs 15 minutes of crew setup. Larger combined batches remain
more labour-efficient. Treatment recovers 90% of recorded water and 80% of
recorded nutrients, retaining unrecovered matter and spent medium as terminal
rejects. No drinking water, fresh nutrient manufacturing or perfect recycling
is implied. Power remains finite at up to 0.5 kW. All numbers are authored
balance, not a measured treatment technology; [existing scientific attribution](fluid-network-operations.md#evidence-and-owner-checks)
continues to explain the separation/recovery concept and its limits.

Jobs already bound under 0.6.x retain their whole-cartridge contract, including
saved electrical progress and exact input IDs. New jobs capture `metered-v1`.
Cancel a paused job to keep its physical supplies; spent electrical work is not
refunded. Missing, corrupt or future cartridge records cannot recreate capacity;
a recordless item is accepted as new only at the original full cartridge mass.

Completion preflights all output space, including the returned partial cartridge,
and balances recovered tank stock, reject mass and remaining medium. Inputs stay
bound when capacity or placement blocks delivery. Existing commit journals guard
interrupted settlement; readouts do not mutate capacity. Stock/loot only spawn
fresh cartridges; existing IDs and saved inventories are preserved.

Offline checks cover repeated partial batches through exhaustion, proportional
value, save/reload, malformed capacity/mass, oversize batches and old/new job
contracts. Owner checks: treat a small packet, inspect the returned cartridge,
reload and reuse it; test insufficient capacity and blocked tank/output space;
finish or cancel an older job. Native gameplay remains owner-tested work.
