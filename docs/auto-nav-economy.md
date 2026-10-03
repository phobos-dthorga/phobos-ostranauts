# Phobos' Asterel N1 Polaris Auto Nav Module: acquisition and servicing

Current stock quantities: [bulk merchant lots](development/merchant-stock.md) supersede the older single-item offers below. Use the [current dependency requirements](installing-mods.md); older version floors below describe their original releases.

Regional acquisition now covers the current vanilla solar system: see the
[solar-system economy guide](solar-system-economy.md) for availability, native
price factors and limits. The regional builds require Framework 0.23.0+.

Current follow-up: [N2 pursuit](auto-nav-pursuit.md) retains navigation/docking
and pursuit. [N3 Fire Control System](auto-nav-fire-control.md) now owns firing
independently. N3's authored 0.4 kg, $5,400/$1,350 values, two-electronics bill,
30-minute construction, native maintenance and offcut/residue outputs match N2.
Its own IDs preserve existing N1/N2 items and jobs. Fire permission is never saved;
FCS ownership restores as Hold.

Original economy audit prepared with **Auto Nav 0.4.3**, Ostranauts **1.0.1.5**, BepInEx **5.4.23.5**
and Framework **0.10.0**. That build required Framework 0.7.0 or newer. The economy
already existed before this round; 0.4.0 names the equipment for its Polaris
navigation-station use, centralizes its balance constants and expands the native
trade/maintenance checks. Versions 0.4.1–0.4.3 fix panel dragging/layout and improve coasting; prices, mass
and existing work thresholds are retained.
No game session or live shop quote was used for this audit.

Current merchant/salvage policy: [expanded availability](development/merchant-stock.md), Auto Nav 0.22.4 with Framework 0.30.3. Earlier build evidence below is historical. The equipment naming
introduced in 0.8.1 is unchanged.
The item and its damaged form are **Phobos' Asterel N1 Polaris Auto Nav Module** and
**Phobos' Asterel N1 Polaris Auto Nav Module (Damaged)**. The mod manager/package remains
**Phobos Auto Nav**. Internal item IDs, module slots, load-order folder, console
commands and configuration filename are unchanged. Existing items are not
replaced by the naming change. User-assigned names may remain user-assigned.

## Buying and selling

All values are whole-object native valuation baselines, before merchant markup,
negotiation and market/category modifiers; they are not guaranteed purchase quotes.

| State | Definition value at that condition |
|---|---:|
| Functional, fully restored | $3,600 |
| Pristine retail | $4,500 |
| Lightly worn retail (15% wear) | $2,700 |
| Refurbished (restored, without pristine premium) | $3,600 |
| Broken, before additional wear discounts | $900 |

| Existing merchant | Offer | Chance per shop stock generation |
|---|---|---:|
| San Diego Polaris electronics dealer | Pristine | 85% |
| K-Leg fixer | Lightly worn | 85% |
| K-Leg scrap supplies | Broken | 85% |
| Venus orbital scrap kiosk | Refurbished | 85% |

Each successful current offer supplies the board lot in the stock guide. Framework's stock-availability setting
can scale the chances. Merchants must restock normally; we do not replace their
inventories, force refreshes or guarantee availability after restarting. No new
merchant or geographic distribution is introduced. Since 0.26.0 the N2 and N3
have the same fixer, K-Leg supplies and Venus offers as the N1; the Polaris dealer
sells all three new.

Native filters accept both forms for buying/selling at the Polaris dealer,
K-Leg supplies and Venus scrap kiosk. The fixer can sell its worn offer; do not
assume its selective buying rules accept this module in return. Remove a fitted
module before trading or servicing it; native ownership, slot and action rules
still apply. Shipbreaker's high-value industrial classification is not copied
onto this small control-system board.

## Build, repair, Restore and dismantle

Current [native module salvage](auto-nav-flight-profiles.md) includes N1, N2 and N3: 30% total per eligible leaf roll for new configurations, biased toward damaged boards. Existing saved chance settings remain authoritative. Some shared pools feed merchants too. Existing inventories are retained;
this adds future loot choices rather than rewriting ship layouts or guaranteeing
an item aboard every derelict. The salvage chance has its own configuration.

| Action | Materials and result | Baseline work |
|---|---|---:|
| Construct | 2 small electronic parts (1 kg) → module (0.4 kg) + offcuts (0.6 kg) | 30 min |
| Repair broken module | 2 small electronic parts (1 kg) → functional module; the parts are used up | 10.8 min |
| Restore functional module | Reduce wear in place; no replacement materials | Depends on wear; 5.76 min for the lightly worn shop offer |
| Dismantle either form | 0.4 kg module → 0.4 kg retained board residue | 6 min |

Construction uses a native Bar/Dining Table, or a detected supported workbench.
Mortorq and soldering tools are required for construction and service; they are
reusable tools, not ingredients. Times exclude hauling and assume unit work/tool
multipliers; skills and tool condition can change them. Module fitting uses the
native navigation-station module system, not a large-machine installation job.

Repair follows native behaviour and leaves substantial wear (approximately 90%);
Restore then improves its condition. At unit multipliers, restoring that much
wear takes about 35 more minutes. Like the game's own repairs, it uses up its parts
and leaves nothing else behind (Framework 0.74.0). Existing repair progress/identity
and legacy finish mappings remain protected by Framework.

Board residue and assembly offcuts have a nominal $0.01 value each and no current
refining recipe. Zero would invoke the game's mass-based price fallback. A
standard electronic-parts item weighs 0.5 kg, so returning one from this 0.4 kg
module would create mass. Dismantling does not provide that output. Taking an
intact module from salvage and reusing/repairing it is the useful recovery path;
the native loot registration above supplies a rare future-generation path.

## Balance evidence and checks

The installed vanilla navigation motherboard weighs 0.4 kg and has a $767 base
value ($159 for its damaged definition). Its dismantling recipe produces 3 kg
of outputs; Phobos deliberately preserves mass instead. Our $3,600 price remains
the existing authored guidance/calibration premium, not a price supplied by the
game. Construction/repair electronics have a combined $29 native base value;
labour creates the functional product's value. Repair/refurbishment for resale
is intended, while dismantling for immediate profit is not.

The refreshed [equipment value audit](development/equipment-value-audit.md) checks both forms,
wear/pristine tiers, material mass and adverse Venus buyer multipliers. Expanded
native checks cover Polaris categories, actual buy/sell filters, slotted-stock
restrictions, additive/idempotent stock, repair output, Restore and guarded
dismantling. These checks do not replace owner testing of shop restocks, actual
quotes, native service actions or saved modules.

The approved faceplate and intact/damaged item sprites need no raster changes:
the Polaris title and item labels are live localized text. No game artwork is
copied and no label is baked into the images. See [Auto Nav operation](development/auto-navigate-adaptation.md)
and the shared [equipment economy](equipment-economy.md).
