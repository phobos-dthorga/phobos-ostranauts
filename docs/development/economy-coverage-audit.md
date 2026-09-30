# Economy coverage audit — 29 September 2026

Owner request, 29 September 2026: check that merchants across the solar system
buy and sell the new bulk-silo items, that loot tables and the salvage, repair and
Restore costs follow the principles already in place, and audit every item across
the Phobos Ostranauts mods the same way, using vanilla examples where ours are weak.
Shipbreaker 0.39.0, Agriculture 0.19.0 and Auto Nav 0.26.0 implement the
findings. These are authored balance choices checked against native definitions;
they are not measured shop behaviour or gameplay validation.

## Method

- **Coverage.** The generated [item-reference data](../item-reference-data.json)
  lists every Phobos item with its value, mass, native jobs (repair, Restore,
  dismantle, install) and every merchant or loot source with its condition, chance
  and lot. Each machine family was compared with its siblings on price, repair bill,
  dismantle yield and mass balance, construction, and new/used/refurbished/broken
  offers.
- **Native buyers and sellers.** Blue Bottle Games' installed Ostranauts 1.0.1.5
  decides what a trader buys and sells through the `strLootCTsBuy` and
  `strLootCTsSell` trigger lists in `data/guipropmaps`. The native checks evaluate
  those triggers with the game's own `CondTrigger.TriggeredDataCO`.
- **Vanilla precedent.** Native loot, trade categories and prices come from the
  same installation's `data/loot`, `data/condowners` and `market/CoCollections`.
  Proprietary data stays local; only conclusions are recorded here. Primary
  product attribution: [Blue Bottle Games — Ostranauts](https://bluebottlegames.com/games/ostranauts).

## What the game does with trade

| Native trader | Buys from the player | Evidence |
| --- | --- | --- |
| Regional supply kiosks (all 15 placed settlements) | Nothing: their buy list is the game's never-true trigger | `TraderCTsBuyBartender` → `TNever` |
| Venus Orbital scrap kiosk | Any tradeable item | `TIsBarterVORBScrapKiosk` |
| K-Leg supply kiosk | Any tradeable item without the high-salvage mark | `TIsBarterOKLGSupplyKiosk` |
| K-Leg fixer | Intact high-salvage equipment, with chances | `TIsBarterOKLGFixerBuy` |
| Furnishings kiosks | Any undamaged tradeable item | `TIsBarterChargen` |
| Flotilla and generic scrap kiosks | Walls, floors, scrap, conduit, small parts, hull patches | `TIsBarterFlotillaScrapKiosk` |
| San Diego brand traders | Only their own brand (`IsHalvorson`, `IsPolaris`, …) | `TIsBarterSanDiego*Buy` |

Regional kiosks selling without buying back is the game's rule for all goods,
so it was kept. Every Phobos retail identity passes the Venus buyer, now checked
for each regional offer.

## Findings and changes

| Finding | Evidence | Change |
| --- | --- | --- |
| W2, B2 and R3 had no used, refurbished or broken offers; the Firstlight-4 and Hearth-2 did | Item-reference sources | Agriculture 0.19.0 gives all five the same fixer/Venus routes, from one shared family list |
| N2 and N3 were sold new only; the N1 had used, broken and refurbished offers | Item-reference sources | Auto Nav 0.26.0 gives the N2 and N3 the N1's routes |
| Ingots had no world finds | The game's engineering loot already carries loose metal (`ItmRandomPartsScrap`: aluminium, steel, carbon fibre, parts) | Shipbreaker 0.39.0 adds one bounded choice: aluminium ingot 6%, steel ingot 4% per engineering roll, one ingot at most |
| The R4 reject and both melt remainders lacked the Trash market category that the feed-family rejects, retained coolant and every other remainder carry | `IsCategoryTrash` is only a market/cargo collection (`AnyTrash`) in vanilla | Shipbreaker 0.39.0 marks them; price, mass and identity unchanged |
| The economy guide lacked S3/T2 Restore, repair and dismantle rows, and several older rows | `docs/equipment-economy.md` | Rows added from the definitions |
| No check confirmed a native buyer for every retail item | Native checks | Added, with categories for ingots (`AnyMetal`), the S3/T2 (`AnyIndustrialProducts`) and remainders (`AnyTrash`) |

## Manufacturing 0.1.0 (29 September 2026)

The three Fennmark machines follow the audited pattern from the start: the same
five merchant routes as the S3/T2 (K-Leg supply broken, fixer used, Halvorson
new, Venus broken and refurbished), regional supply kiosks, one bounded
engineering-loot choice over the six loose forms, mass-balanced dismantling
below whole value, Restore rates and `EquipmentSaveUpgrade`.

Manufacturing 0.1.1 (owner direction, 29 September 2026: mid-to-late-game
equipment, rather expensive) re-anchors them on vanilla's late-game kit instead
of Shipbreaker's: V4 $64,000, X2 $38,000, H2 $22,000 (IC fusion reactor
$141,000, heavy lift rotor $56,774, radars $30,000–42,000, towing brace
$23,944). Findings and changes:

| Finding | Evidence | Change |
| --- | --- | --- |
| At 0.1.0 prices the machines read as mid-game Shipbreaker plant | Vanilla loose equipment above $20,000 is sensors, weapons, rotors, reactors | Prices above; install/repair/dismantle work and Restore times lengthened toward the reactor and radar thresholds |
| Repair bills were scrap only | Every vanilla late-game repair uses motors, mainboards, heat sinks and screens | Component bills ($100–573 of parts), component-bearing dismantling, still mass-balanced and 1.7–3.3% of whole value |
| No high-salvage mark | Every native loose item above $20,000 carries `IsSalvageValueHigh`, all forms | Added to all forms: the K-Leg fixer now buys them intact, the supplies kiosk no longer does, Venus buys either |
| A 40% engineering-loot chance | At late-game prices that is a money printer | 5%, three in four broken, a registered constant capped by a native check |
| The steel charge gained value (0.1.0: 4 x $20 + $10 in, 4 x $25 out) | Live-price computation | Nickel-iron ingot $24 (the smallest fix; carbon stays $10); a native check then proved every charge lost value at live prices. Superseded 30 September 2026: the ingot returns to $20 and the checks prove the refining value guardrails instead (sellable products within 1.5 x inputs, a quarter when every input is bought stock). Stock stays ordinary-priced: only the machinery is late-game (owner correction) |

Lots stay at eight and the 85% equipment floor stays, per the stock memoranda;
price, not scarcity, is the late-game gate. Manufacturing 0.2.0 adds the K2 Sabatier
reactor ($44,000) and M2 methane store ($21,000) on the same terms: component
repair bills, the high-salvage mark, the same routes, and a share of the same 5%
engineering-loot chance, now split across five machines.
Nickel-iron ingots and carbon stock are `AnyMetal` / `AnyIndustrialProducts`
so the Venus, K-Leg supply and furnishings buyers accept them; scrap kiosks do
not (the ingot rule above). Slag and anhydrous residue carry the Trash category.
Clay hydrates are ore: a bounded mining-table choice, bought by the government
kiosks, sold by nobody. Every refinery charge loses money against selling its
input whole; the table is in [the refinery record](manufacturing-refinery-and-chemistry.md).

## Held deliberately

- **S3, T2 and R3 stay purchase-only** (owner decision, 29 September 2026).
  Construction of anything beyond semi-advanced equipment waits for Phobos
  Manufacturing. The Framework recipe limit of 100 input units would have required
  ingots or new assembly sections for the 240 kg S3 and 120 kg T2 anyway.
- **S3 and T2 dismantling returns 15–17% of whole value**, against 3–9% for other
  machines, because they are mostly steel. Every row still loses value against
  selling the machine whole, conserves mass, and passes the native Venus
  adverse-multiplier comparison in the [equipment value audit](equipment-value-audit.md).
  Their prices sit within vanilla range: the native 1,500 kg fusion-fuel canister
  is $6,542, the 70 kg gas canister $410.
- **Ingots are not bought by the scrap kiosks.** Those buy `IsScrap`, which would
  also make ingots furnace and processor feed. The Venus, K-Leg supply and
  furnishings buyers accept them.
- **Broken conduit** (coolant and irrigation) has no shop offer in either mod,
  consistently; loose pipe is sold new in large lots and repairs cheaply.
- **Water ice** is valued at $1,200 per 24.7 kg block by the game itself, far above
  the 10 cr/kg bulk water price. Thawing mined ice therefore trades value for
  water aboard; that is the game's valuation and the T2's purpose, not an exploit.
  Ice is acquired by native mining; no ice offers are added.
- **Recorded process intermediates** (Agriculture residues, solutions, mixtures,
  panel residue feed) keep their process identity with no market category, as
  before.

## Coverage after the change

| Family | Used / refurbished / broken offers | Engineering or module salvage | Repair, Restore, dismantle |
| --- | --- | --- | --- |
| Shipbreaker machines (11, including S3 and T2) | All three | Intact and damaged | All present |
| Agriculture machines (5, including R3) | All three | Intact and damaged | All present |
| Auto Nav boards (N1, N2, N3) | All three | Intact and damaged | All present |
| Ingots | New at every general market | Single ingot, 10% | Table recovery to scrap (loses value) |
| Terminal remainders | Never sold | Never generated | None; trash category |

Later addition (Shipbreaker 0.43.0, 30 September 2026): the Y2, Y3 and Y4
material bins join the Shipbreaker machine family at parity, through the same
economy table: all three offer conditions on the silos' routes, the Y2 in
engineering salvage (the Y3 and Y4 are purchase-only by size, like the S4 and
S5), a native buyer under industrial products, and full repair, Restore and
dismantle coverage. The same native economy checks cover them.

Later addition (Manufacturing 0.18.0, 30 September 2026): the Lixivar LC-3 joins
the Manufacturing machines at parity through the same economy table: broken, worn,
new and refurbished offers on the other machines' routes and the regional supply
kiosks, the one-in-twenty engineering find (three in four broken), the high-salvage
mark, component repair, Restore and mass-balanced dismantling, and the faction
kiosks at Trusted standing. Its salts, intermediates and remainders and the mined
evaporite crust are never sold. The same native economy, stock, faction-kiosk and
buyer checks cover it.

Later addition (Manufacturing 0.19.0, 30 September 2026): the Lixivar SA-3 joins
the Manufacturing machines and the AT-2, AT-3 and AT-4 acid tanks join the store
ladder at parity (routes, lots, the small tank in engineering salvage, component
repair, Restore, mass-balanced dismantling, the high-salvage mark, Trusted and
Friendly faction tiers). The mined sulfide nodule and the SA-3's flask and calcine
are never sold.

Later addition (Agriculture 0.27.0, 30 September 2026): the Groundwork E2, E3 and
E4 nutrient hoppers join the Agriculture ladder families at the reservoirs' parity:
the same routes and lot of four, only the small size in engineering salvage (the
share split one more way, 3% to 2.5% per machine), component repair, Restore and
mass-balanced dismantling with a retained housing remainder, and the faction kiosks
at Warm standing.

Across the three content mods the regenerated references record 746 merchant
offers (738 before), 56 retail identities (51: the five newly sold broken forms)
and 54 identities in world-loot choices (52: the two ingots). Used and refurbished
offers replace the matching pristine gap-fill at the fixer and Venus, so offers
grow by less than the new branches. The native definition checks rose from
15,080 to 15,717 with this change, almost
all from the per-offer buyer check. Owner checks remain: a normally restocked K-Leg fixer and Venus
scrap kiosk offering the new used and broken forms, a newly generated engineering
roll with an ingot, and selling an ingot and a remainder at Venus.
