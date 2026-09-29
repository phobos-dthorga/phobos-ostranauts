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

Across the three content mods the regenerated references record 746 merchant
offers (738 before), 56 retail identities (51: the five newly sold broken forms)
and 54 identities in world-loot choices (52: the two ingots). Used and refurbished
offers replace the matching pristine gap-fill at the fixer and Venus, so offers
grow by less than the new branches. The native definition checks rose from
15,080 to 15,717 with this change, almost
all from the per-offer buyer check. Owner checks remain: a normally restocked K-Leg fixer and Venus
scrap kiosk offering the new used and broken forms, a newly generated engineering
roll with an ingot, and selling an ingot and a remainder at Venus.
