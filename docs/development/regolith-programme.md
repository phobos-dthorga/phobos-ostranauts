# Uses for regolith: programme record

Opened 5 October 2026. The owner asked what could be done with the game's unused
**Regolith (Loose)**, all options open, and chose every one offered. This record holds
the decisions, the order of work and what is still unverified. Each release set adds its
own recipe record to the [refinery and chemistry record](manufacturing-refinery-and-chemistry.md).

## What regolith is, from the game's data (Ostranauts 1.0.1.5)

`ItmMineralStone01`, "Regolith (Loose)": 20 kg, base price 35, stacks six, a mineral in the
ore market category but not an ore to the game's ore rule, and not mineable. It comes from
most rock-wall salvage tables (30 to 50 percent shares), cracked walls and S-class deposits.
Damaged far enough it turns into gangue. Kiosks buy it; nothing in the game uses it. The
game's own cargo text says regolith is "heated to emit chemically bonded hydrogen, water,
He4". The Rivetline material bins already hold it; no Phobos machine took it before this
programme.

## Owner decisions (5 October 2026)

- **All five uses:** an LC-3 acid leach, a V4 volatile bake, sintered floor pavers, and
  oxygen from rock by both routes, molten regolith electrolysis and carbothermal reduction
  with methane, "maximum player choice".
- **Reduced metal:** a new stock item, ferrosilicon, with a use shipped in the same release:
  the LC-3 reacts it with water into hydrogen (the silicol process).
- **Floor:** a Phobos-owned twin of the game's Polished Regolith Floor, so it can be lifted
  again. The game's own tile has no uninstall and cannot safely be given one.
- **Brand:** one new brand, Oxsmith, for both oxygen machines (EC-4 and CR-4). Carbon
  monoxide stores stay Fennmark.
- **Methanation:** a second mode on the existing K2, fed from a new carbon monoxide store.
  This amends the earlier rule to keep the K2's saved holds and one-step conversion: old
  records must still read unchanged.
- **Belts:** reassessed at the owner's question. Feed stores already bring lumps to the
  machines. Two gaps are closed in this programme: products could not leave a Manufacturing
  machine by belt, and a started V4 on a shared bin would have baked every lump in it.
- **Artwork:** ChatGPT for the two machine masters while the owner's plan lasts, PixelLab
  and derivation for the rest. The [handoff](oxsmith-art-handoff.md) was written first.

Agent defaults, open to owner revision: leach odds 72/22/4/2 (first proposed as 65/25/7/3, then
leaned because a K-Leg prospector sells regolith, which brings in the bought-stock rule); the bake gives 0.4 kg of
water and 0.1 kg of CO2, leaving 19.5 kg, exactly three 6.5 kg pavers; the V4 leaves
regolith alone unless told to bake or sinter it; machine prices about 96,000 cr (EC-4),
72,000 cr (CR-4) and 20,000 cr (carbon monoxide store).

## Order of work

| Set | What | Status |
| --- | --- | --- |
| Art | ChatGPT handoff for the EC-4 and CR-4 | Written 5 October 2026; awaiting the owner's run |
| 0 | Products out by belt: an optional "Send products to" store for Manufacturing machines | Done: Framework 0.98.0, Manufacturing 0.49.0 |
| 1 | Regolith leach on the LC-3, with an outcome table | Done: Manufacturing 0.50.0, odds 72/22/4/2 |
| 2 | Volatile bake on the V4; baked regolith as a declared remainder | Not started |
| 3 | Sintered pavers, the V4's regolith choice, the regolith floor twin | Not started |
| 4 | Oxsmith EC-4, ferrosilicon and its silicol use | Not started; opens with a design record |
| 5 | Carbon monoxide stores, the K2's second mode, Oxsmith CR-4 | Not started; opens with a design record |

## Unverified, and not to be cited until checked

These figures came from memory or from planning notes with no recorded derivation. Each is
checked against a primary source, or labelled authored, before any player text cites it:

- 3.9 kg of oxygen from a 20 kg lump by molten regolith electrolysis (NASA Kennedy Space
  Center work is named in the business record as "to cite").
- The lump's assumed composition (bound water, silica, magnesia, iron oxide, metal grains,
  sulfide), described as chondrite-like.
- The carbothermal extraction share and its loop figures (NASA Carbothermal Reduction
  Demonstration reports are linked in the feedstock gaps record but were not re-read).
- Sintering temperature, the share of metal grains an acid leach frees, the silicol
  reaction's yield, and the reaction enthalpies used for energy budgets.

## Known consequences

- Baked regolith is the first remainder worth making on purpose: one 35 cr lump gives
  19.5 kg of RM-1 reaction mass.
- The Silicates ore sells for more raw than its electrolysis products are worth; both oxygen
  routes are supply chains at the game's gas price, with no profit claim.
- A save with the regolith floor laid needs Manufacturing kept installed; removing the mod
  would leave holes where it lay.
