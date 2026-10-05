# Pipe and link audit — 5 October 2026

Owner request, 5 October 2026: "the Cultivation Racks need the same treatment as the W2's
received", and "a greater audit across anywhere pipes of all kinds are used". The W2's treatment
was the process-water line's rules: any pipe under or right beside a machine joins it (Framework
0.69.0), touching machines join without pipe, and a pipe laid in a wall counts (Framework
0.100.0, the `lines` data pack).

This record covers what the owner's save showed, what Framework 0.103.0, Agriculture 0.53.0 and
Manufacturing 0.54.0 change, every pipe family and link checked, and what remains for the owner
to decide. Nothing here is gameplay validation.

## What the save showed

A read-only copy of save `pg11` (5 October 2026, 19:22) and Player.log were read; the copy was
deleted afterwards. On H-5YJG the farm block has two Firstlight-4 racks side by side and four
W2s side by side below them, all rotated so their fittings face along the row. One irrigation
run lies along the row between them, directly beside every machine, with short stubs under each.
The process-water line runs beside it.

Until Agriculture 0.53.0 the irrigation pipe joined a W2 only at one outlet tile beside its upper
right and a rack only at one inlet tile beside its upper left. In this layout three of the four W2
outlet tiles and one of the two rack inlet tiles lie inside the neighbouring machine, and the two
free ones have no pipe on them. No rack could link. A second rule would then have stopped all four
W2s: two W2 outlets on one pipe blocked pumping. Player.log showed the current builds loaded and no
pipe errors; the game's "No such Interaction: ACTFeedItem…" lines are its own and appear for its
power conduit too.

## What changed

**Irrigation pipe (Agriculture 0.53.0).** The pipe is now a network family like the water, gas,
acid and ethanol lines. Every W2 and rack form has an irrigation port.
- A pipe under or right beside a W2 or rack joins it, on any side. The old outlet and inlet tiles
  lie in that ring, so every layout that linked before still links.
- A W2 within one tile of a rack feeds it directly, with no pipe, and fills no pipe.
- Touching machines do not chain separate pipe runs. This is unlike the shared lines: two W2s side
  by side, each on its own run with its own feed, keep their runs apart as before.
- Several W2s may share one pipe while they mix the same feed (agent choice; until now one W2 per
  connected run). A W2 that would pump a different feed into a pipe another running W2 pumps into
  waits, and its panel says why. Two feeds in one pipe would only flush each other back.
- The 64-tile limit and the flow estimate count tiles along the network. The pump fills the W2's
  whole network before feed reaches a piped rack, as it filled the whole run before.
- The rack's and W2's link picker now says why each W2 or rack aboard is not offered (the shared
  link note), instead of an empty list.
- Saved links, pipe contents and the family id are unchanged. No save needs a manual step.

**Framework 0.103.0.**
- `LineContents.Declare(..., pumped)`: a holding line its content mod fills is never topped up from
  stores, even when it is a network (irrigation).
- `LineContents.Circuit(participant, family)`: the segments on a participant's network.
- The shared link note names the end that has no fitting for a line that does carry the cargo (a
  game canister on the gas line), instead of saying no line carries it.
- Ship's Water tanks whose own fittings refuse the water-line port are named in the log (they then
  join only by touching); this used to pass silently.

**Agriculture 0.53.0 and Manufacturing 0.54.0: pickers that went quiet now explain.**
- The water tank's list of W2s.
- The W2's nutrient hopper list: crop nutrients travel on no line, so a hopper has to stand within
  one tile.
- The X2's oxygen and K2's carbon-source fields: they now name the game's canisters, and the K2's
  carbon monoxide stores, that are not offered.
- The L2's canister field.

## Pipe families

| Line | Owner | Joins equipment | Touching joins | Filled by | Notes |
| --- | --- | --- | --- | --- | --- |
| Process water | Framework | under or beside, any side | yes, and chains | its stores, Ship's Water tanks | |
| Gas | Framework | under or beside | yes, and chains | its stores | |
| Acid, ethanol | Manufacturing | under or beside | yes, and chains | their tanks | |
| Irrigation | Agriculture | under or beside (since 0.53.0) | W2 to rack only, no chaining | the W2's pump | was two fixed tiles |
| F6-C coolant | Shipbreaker | two fixed points | no | the furnace pump | see recommendation 1 |
| Conveyor belt | Framework | on or beside, by the route's own cells | yes | — | see recommendation 2 |

Every line counts on a floor or inside a wall, never on flex floor or outside, by the shipped
`lines` rule. No Phobos machine links a commodity a line carries without having a port on that
line; the inventory checked every port registration against every vessel link.

## Links checked

Machine-to-store links in every mod were inventoried: Framework (water tanks and Ship's Water,
line top-up, drain canisters, feed and product stores, the RCS regulator), Manufacturing (the six
charge machines, X2, K2, AX-2, P1, L2, A2, Corker-2, RM-1, store transfers), Shipbreaker (T2, item
routes, storage, ML-2, F6 cooling, grabber and chute) and Agriculture (W2, racks, B2, hoppers,
recycler capture). Medical, War Declared and Auto Nav have no material links. The water, gas,
acid and ethanol links all follow the network rule and explain refusals. The exceptions are in the
recommendations below.

## Recommendations for the owner

1. **F6-C coolant conduit: give it the network rule, with care.** It still joins only at the
   furnace's chosen side fitting and at the radiator's service point, one tile inside its mounting
   wall. A plain "under or beside" rule would cut every existing piped furnace, because that
   service point lies outside the ring around the radiator. The route also deliberately survives a
   damaged or locked furnace or radiator so a hot loop keeps its heat, which the shared participant
   rule does not. A safe change: the network rule plus the old service point as an extra join, the
   coolant family keeping damaged and locked ends, and one "Piped cooling" choice in place of left
   and right fittings (saved left/right records read as piped). Recommended, as its own release,
   after the owner's say on the panel change.
2. **Conveyor belts at the F6 and the collector.** A belt joins the F6 only at one tile per side
   (its front input and output points) and the residue collector only along its service row, where
   other equipment joins anywhere on or beside its footprint. The F6's two points keep input and
   output apart; a decision is needed on whether that distinction should stay.
3. **Shipbreaker pickers list everything aboard.** Item-route sources and destinations, the D4/R4
   storage destination and F6 cooling offer every compatible object, and the reason appears only
   after a failed Apply; `Storage.no_candidates` describes a reach filter the list does not apply.
   Recommended: filter by reach and add the shared note, as the provider panels do.
4. **Exact placements kept on purpose.** The P1 and RM-1 on the game's RCS regulator's gas input
   tile, the recycler capture attachment, the grabber-chute-D4 alignment and F6 direct mounting are
   physical assemblies, not links. No change recommended.
5. **Touching only, by design.** Nutrient hoppers (dry nutrients travel on no line), the game's own
   canisters (no line fitting), the ML-2 to its cooling assembly, and the RM-1 feed. Now explained
   where the pickers had no note, apart from the ML-2 cooling field, which is part of
   recommendation 3's pass.
6. **Smaller items.** Three touching tests exist (square footprints for lines and belts, rectangles
   for the Medical monitor and the laser); the feed-store note gives one generic reason and lists at
   most eight stores; an obsolete fixed-point `LineReach.Of` overload and an unused
   `ManifoldRules.RouteTileLimit` remain for binary compatibility.

## Verification

All seven mods' unit and native checks, including new native checks that every W2 and rack form
carries the irrigation port, that the irrigation pipe is a pumped network that touching does not
chain, that its joints are drawn on the shared port pattern, and Framework checks for the new link
reasons and the network circuit. Offline only; the owner's farm block has not been run in the game.
