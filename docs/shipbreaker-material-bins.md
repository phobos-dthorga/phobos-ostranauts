# Material bins

Shipbreaker 0.43.0. Implemented and checked offline; owner gameplay checks are
pending, including how the bins' artwork looks in play. Use the
[current dependency requirements](installing-mods.md).

## Equipment

| Item | Size and mass | Holds | Base price | Where |
| --- | --- | --- | --- | --- |
| Phobos' Rivetline Y2 Material Bin | 2 x 2 tiles; 60 kg empty | 4 x 4 grid: 96 ore blocks | 2,400 cr, broken 600 cr | K-Leg supply kiosk and fixer, San Diego Halvorson, the Venus scrap kiosk and regional markets, in lots of eight; INSTALL > FURN. Also rare engineering salvage. |
| Phobos' Rivetline Y3 Material Bin | 3 x 3 tiles; 115 kg empty | 6 x 6 grid: 216 ore blocks | 3,900 cr, broken 975 cr | The same sellers; INSTALL > FURN. Too big to turn up in salvage. |
| Phobos' Rivetline Y4 Material Bin | 4 x 4 tiles; 170 kg empty | 8 x 8 grid: 384 ore blocks | 5,510 cr, broken 1,377 cr | The same sellers; INSTALL > FURN. Too big to turn up in salvage. |

No fabrication recipe. A bin is a sealed box with hinged lids for what the crew
mine: ore, loose regolith, gangue, water ice, methane ice, ice gangue and mined
chunks such as clay hydrates. It takes nothing else, so a bin never fills up
with spare parts and scrap. Ore stacks six to a grid cell, so a Y2 holds about a
tonne of 10 kg ore blocks, or more than two tonnes of water ice. Every block
stays itself: a clay chunk is still a clay chunk when it comes out.

## Use

1. Install the bin on intact floor from the **FURN** tab, next to the game's own
   Storage Bay. It needs no electricity.
2. Right-click it and choose **Inventory**. Drag mined material in, or drop a
   stack in one go.
3. Your crew treat an installed bin like any unlocked container:
   - **Load feed by crew** on a machine (for example the T2 ice thaw unit) fetches
     from bins when its source is the whole ship, or you can pin one bin as its
     source in [crew standing orders](crew-automation.md).
   - Choose a bin as a machine's output store to have the crew clear gangue and
     other mined remainders into it.
4. The D4 and R4 storage outputs do not list bins: those machines send scrap
   metal and parts, which a bin refuses. Use a crate or the Storage Bay there.

## Moving, damage and repair

- Uninstalling a bin carries its contents with it, as the game's own Storage Bay
  does. A full Y4 of water ice weighs several tonnes: empty it first if the crew
  have to drag it.
- A damaged bin keeps its contents and its lid still opens. Repair it like other
  Rivetline equipment; Restore treats wear.
- Dismantling is refused until the bin is empty. Nothing inside is ever deleted.

## Limits

- A bin is storage only. It does not feed a machine by itself; the crew move
  material between bins and machines.
- Stacks follow the game's own limits: six ore blocks or five loose regolith per
  cell. A bin's mass is its housing plus everything inside, as for any
  container.
- Offline checks are not gameplay validation. How the grids read on screen, and
  how heavy bins feel to move, still need checking in play.
