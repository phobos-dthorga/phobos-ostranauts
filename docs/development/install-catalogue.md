# Equipment in the native INSTALL catalogue

Open **INSTALL** in the PDA and select the following existing tab. Entries use
our equipment names and artwork, with separate intact and damaged placement
forms. These changes are prepared and checked offline; owner gameplay review
is still pending.

| Mod | Tab | Equipment |
| --- | --- | --- |
| Phobos Shipbreaker | APPS | D4 dismantling fixture, exterior grabber, intake chute, floor/hull collector, R4 scrap reclaimer, F6 furnace, S3, S4 and S5 process water silos and T2 ice thaw unit |
| Phobos Shipbreaker | HVAC | F6-R exterior radiator, F6-P underside cooling head and F6-C coolant conduit |
| Phobos Shipbreaker | CTRL | C1 industrial control console |
| Phobos Shipbreaker | FURN | Y2, Y3 and Y4 material bins (beside the game's own Storage Bay) |
| Phobos Agriculture | APPS | Firstlight-4 cultivation rack, Hearth-2 portion cooker, Groundwork W2 supply, B2 workup bench, R3, R4 and R5 reservoirs and E2, E3 and E4 nutrient hoppers |
| Phobos Agriculture | MISC | Irrigation conduit |
| Phobos Manufacturing | APPS | Fennmark V4 volatiles refinery, X2 chemical processor, K2 Sabatier reactor, Tolvane AX-2 ammonia cracker, Lixivar LC-3 leach and crystallise unit, Lixivar SA-3 acid plant, the AT-2, AT-3 and AT-4 acid tanks, and the hydrogen, methane, oxygen, nitrogen, carbon dioxide and ammonia stores in all three sizes |
| Phobos Manufacturing | HVAC | Fennmark P1 RCS propellant manifold, L2 canister filling station, A2 cabin air regulator and gas line |

The native coverage checks include every implemented intact/damaged placement family. The R3, R4 and R5 have no fabrication recipe: buy the loose hardware before installation.
D4, R4 and F6 entries now consume two D4-S, two R4-S or three F6-S sections at the site. Native hauling stages them separately. Complete loose machinery still has its direct Install action; damaged placement keeps its existing loose input. Other entries consume existing loose equipment and retain their work, placement and access requirements. See [section assembly](../section-assembly-and-maintenance.md). Obtain or construct the equipment first;
selecting a catalogue entry does not create a free machine or replace the
[construction recipes and equipment economy](../equipment-economy.md). Pipe entries
use native placement; continuous drag-laying behaviour has not been verified.

## Other mods and equipment

Auto Nav's N1 and N2 boards are inserted in Polaris module slots through the
existing [Auto Nav workflow](auto-navigate-adaptation.md). They have no standalone
floor-installed form and are deliberately absent from this placement catalogue.
Framework adds shared services. Manufacturing's machines and stores are
purchase-only APPS entries (the P1, L2 and A2 are HVAC); its proposed M4 machining centre must receive a
catalogue entry when it becomes operational. Supplies, produce, ore, ingots,
castings and waste remain cargo. Assembly sections are also cargo, but their Install action starts construction of the complete machine rather than installing a section as furniture.

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
