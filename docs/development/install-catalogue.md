# Equipment in the native INSTALL catalogue

Open **INSTALL** in the PDA and select the following existing tab. Entries use
our equipment names and artwork, with separate intact and damaged placement
forms. These changes are prepared and checked offline; owner gameplay review
is still pending.

| Mod | Tab | Equipment |
| --- | --- | --- |
| Phobos Shipbreaker | APPS | D4 dismantling fixture, exterior grabber, intake chute, floor/hull collector, R4 scrap reclaimer, F6 furnace, T2 ice thaw unit and Ablatine ML-2 mining laser |
| Phobos Shipbreaker | HVAC | F6-R exterior radiator, F6-P underside cooling head and F6-C coolant conduit |
| Phobos Shipbreaker | CTRL | C1 industrial control console |
| Phobos Shipbreaker | FURN | Y2, Y3 and Y4 material bins (beside the game's own Storage Bay) |
| Phobos Agriculture | APPS | Firstlight-4 cultivation rack, Hearth-2 portion cooker, Groundwork W2 supply, B2 workup bench and E2, E3 and E4 nutrient hoppers |
| Phobos Agriculture | MISC | Irrigation conduit |
| Phobos Manufacturing | APPS | Fennmark V4 volatiles refinery, X2 chemical processor, K2 Sabatier reactor, Tolvane AX-2 ammonia cracker, Lixivar LC-3 leach and crystallise unit, Lixivar SA-3 acid plant, the Oxsmith EC-4 electrolysis cell (0.52.0) and CR-4 carbothermal reactor (0.53.0), the AT-2, AT-3 and AT-4 acid tanks, the Alembrine Cask-2, Cask-3 and Cask-4 ethanol tanks (0.38.0), the Alembrine Copperhead-3 fermenter-still (0.39.0), the Alembrine Corker-2 bottling unit (0.40.0), and the hydrogen, methane, oxygen, nitrogen, carbon dioxide, ammonia and (0.53.0) carbon monoxide stores in all three sizes |
| Phobos Manufacturing | HVAC | Fennmark P1 RCS propellant manifold, Slingwright RM-1 reaction mass feeder (0.43.0), L2 canister filling station and A2 cabin air regulator; Lixivar acid line (0.24.0); Alembrine ethanol line (0.38.0) |
| Phobos Manufacturing | HULL | Fennmark sintered regolith floor (0.51.0), laid from one regolith paver beside the game's own floors; our twin of the game's Polished Regolith Floor |
| Phobos Framework | HVAC | Fennmark gas line (moved from Manufacturing in Framework 0.57.0) and process water line |
| Phobos Framework | MISC | Rivetline conveyor belt (Framework 0.61.0) |
| Phobos Framework | APPS | Rivetline S2, S3, S4 and S5 process water silos (the S3 to S5 moved from Shipbreaker in Framework 0.58.0) |
| Phobos Medical | FURN | Halewright Ward-3 medical bed (0.1.0), beside the game's own beds and medical bed |
| Phobos Medical | APPS | Halewright Vigil-2 patient monitor (0.3.0) |

The native coverage checks include every implemented intact/damaged placement family. The silos have no fabrication recipe: buy the loose hardware before installation. Recorded exception: Agriculture's retired R3, R4 and R5 reservoirs (Agriculture 0.31.0) convert to the S3, S4 and S5 on load and are no longer offered in INSTALL; their definitions and jobs remain only for jobs saved against them.
Since Shipbreaker 0.60.0 the D4, R4 and F6 entries consume the whole loose machine, like every other entry (owner direction, 1 October 2026); their section jobs remain registered only for build sites saved by earlier versions. Damaged placement keeps its existing loose input. All entries retain their work, placement and access requirements. See [installing machines](../section-assembly-and-maintenance.md). Obtain or construct the equipment first;
selecting a catalogue entry does not create a free machine or replace the
[construction recipes and equipment economy](../equipment-economy.md). Pipe entries
use native placement; continuous drag-laying behaviour has not been verified.

## Other mods and equipment

Auto Nav's N1 and N2 boards are inserted in Polaris module slots through the
existing [Auto Nav workflow](auto-navigate-adaptation.md). They have no standalone
floor-installed form and are deliberately absent from this placement catalogue.
Framework adds shared services and, since 0.57.0, the two shared lines listed above. Manufacturing's machines and stores are
purchase-only APPS entries (the P1, L2 and A2 are HVAC); its proposed M4 machining centre must receive a
catalogue entry when it becomes operational. Supplies (including Framework's drain canisters), produce, ore, ingots,
castings and waste remain cargo. The retired assembly sections are cargo with no Install action; saved copies convert automatically.

## Implementation evidence and maintenance

**Observed game implementation:** Blue Bottle Games' Ostranauts 1.0.1.5 local
`GUIPDA.ShowJobPaintUI` / `ShowJobOptions` and `Installables.Create` select the
fixed categories HULL, HVAC, POWR, SENS, CTRL, FURN, APPS and MISC. Primary product
source: [Blue Bottle Games — Ostranauts](https://bluebottlegames.com/ostranauts).
The method observations come from the locally inspected game assembly, not a
claim made by that web page. Proprietary implementation files remain local.
Our previous `MIS` category was registered but unreachable from these tabs.

**Our design choice:** use the existing functional tabs above. Framework shares
category constants and rejects unreachable visible categories or missing
placement targets before publishing a definition batch. Content mods choose the
tab; menu categories do not change work-rate categories or existing job IDs.
No game UI replacement or new dependency is required.

The native-definition suite runs the actual native installable generator and
checks every installed content form for one visible entry, the expected tab,
physical inputs, output identity and resolvable source/art definition. It also
checks rejection of misspelled categories and preservation of another provider's
entry. Both content build scripts run that suite. Add future placeable forms to
their owning definitions and keep this inventory, changelog and Workshop draft
current in the same change, as required by the owner memorandum in AGENTS.md.

Owner check after installing updated packages: open each listed tab, select an
intact and damaged machine, rotate/place a valid outline, and let crew install
it from matching loose stock. Check pipe placement on its supported floor,
normal uninstall/reinstall and save/reload. Offline checks are not evidence of
successful in-game rendering or crew pathfinding.

## Groundwork B2 — Agriculture 0.9.0

The implemented B2 Workup Bench joins APPS with intact and damaged placement
forms. Recorded residue, concentrate, makeup salts, mixtures, spent biomass and
wet rejects are loose supplies/byproducts, not floor fixtures.
