# Equipment in the native INSTALL catalogue

Open **INSTALL** in the PDA and select the following existing tab. Entries use
our equipment names and artwork, with separate intact and damaged placement
forms. These changes are prepared and checked offline; owner gameplay review
is still pending.

| Mod | Tab | Equipment |
| --- | --- | --- |
| Phobos Shipbreaker | APPS | D4 dismantling fixture, exterior grabber, intake chute, hull collector, R4 scrap reclaimer and F6 furnace |
| Phobos Shipbreaker | HVAC | F6-R exterior radiator, F6-P underside cooling head and F6-C coolant conduit |
| Phobos Shipbreaker | CTRL | C1 industrial control console |
| Phobos Agriculture | APPS | Firstlight-4 cultivation rack, Hearth-2 portion cooker and Groundwork W2 supply |
| Phobos Agriculture | MISC | Irrigation conduit |

This covers 14 equipment families and 28 intact/damaged placement entries.
Installation consumes the existing loose equipment and uses its existing work,
placement and access requirements. Obtain or construct the equipment first;
selecting a catalogue entry does not create a free machine or replace the
[construction recipes and equipment economy](equipment-economy.md). Pipe entries
use native placement; continuous drag-laying behaviour has not been verified.

## Other mods and equipment

Auto Nav's N1 and N2 boards are inserted in Polaris module slots through the
existing [Auto Nav workflow](auto-navigate-adaptation.md). They have no standalone
floor-installed form and are deliberately absent from this placement catalogue.
Framework adds shared services, Approach Assist adds no placeable fixture, and
Manufacturing currently has no implemented machinery. Manufacturing's proposed
M4 must receive a catalogue entry when it becomes operational. Supplies, produce,
castings, waste and assembly sections are cargo rather than installed furniture.

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
