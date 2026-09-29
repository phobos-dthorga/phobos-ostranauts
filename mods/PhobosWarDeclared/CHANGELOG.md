# Phobos' War Has Been Declared changelog

Maintained from 29 September 2026. Dates on Draft entries record preparation,
not Steam publication.

## [Unreleased]

## [0.1.1] - 2026-09-29 - Draft

### Changed

- Performance pass, stage 6 (the game crawls at fast-forward). The two-second check over your ships no longer allocates lists of ships and expired damage marks each time, and a part's schematic facts (its conditions, INSTALL tab and footprint) are worked out once per part instead of on every sort comparison and evaluation while build sites are being laid.
- Build sites that could not be laid because you were holding an item, or because the ship was not yet editable, are tried again every ten real seconds rather than at every check. They stay pending; standing down, a new loss during combat or Lay held build sites tries at once.
- Recorder scopes war.poll and war.lay_pending.

### Compatibility and limits

- Requires Phobos Framework 0.45.1 or newer. Saved battle logs, schematics and orders are unchanged. Offline checks are not gameplay validation.

## [0.1.0] - 2026-09-29 - Draft

### Added

- Battle stations for ships you own. They start on their own when the game has an enemy engaged with your ship, your weapons lock on, or your hull takes damage, and end after 5 quiet minutes of game time (a setting). A Battle stations order at any intact navigation station holds them until you choose Stand down.
- While at battle stations, every installed part destroyed on that ship is logged where it stood: walls, floors, doors, conduit, machinery, vanilla and modded alike. Parts that are only damaged are left to the game's own repair jobs.
- When you stand down, the game's own build sites are laid where the parts stood, floors first. The crew build them through the usual Construct duty and need matching replacement parts; nothing is created for free. A door, alarm or machine is rebuilt in the state its install job gives it.
- Rebuild schematics decide what gets a build site: Safe (the default) lays only build sites crew can walk through and holds walls, doors and bulky machinery; Safe plus walls adds walls, window walls and temporary blister seals to what Safe lays, keeping doors, hatches, docking ports and bulky machinery held; Everything lays them all; Hull only is an example to copy. Players can write their own schematic files in BepInEx/config/PhobosWarDeclared/schematics, matching parts by ID, game condition, INSTALL tab or whether they block walking.
- Lay held build sites, at the navigation station or through F3, lays every held part when you are ready.
- An after-action line in the crew log: build sites laid, parts held, parts left for you, parts the game cannot rebuild, and loose items destroyed.
- F3 console: phoboswar status, battle, standdown, lay, schematics, schematic name and reload.
- A mod-menu and Workshop cover, matching the other Phobos covers: a PixelLab scene of build sites laid over a torn hull, composed into the suite's cover frame. It illustrates the idea and is not a gameplay screenshot.
- A player guide with flowcharts of the whole fight, of what happens to each destroyed part, of battle stations and of how schematic rules are chosen, plus a table of what each shipped schematic does with each kind of part and a troubleshooting table.

### Compatibility and limits

- Requires Phobos Framework 0.43.0, which adds the shared build-site and combat-observation services.
- The battle log and held parts are saved with each ship; laid build sites are saved by the game. Loose-item counts in the after-action line cover the current session only.
- Build sites carry their part's full footprint: an unbuilt wall or machine blocks walking exactly as the finished part would. That is why Safe holds them, and why Safe plus walls seals the hull at the cost of blocking those tiles until the crew finish.
- A part with no install job in the game (some unique fixtures) cannot get a build site and is listed instead. A build site that cannot fit where the part stood (for example with the floor beneath it gone) is held and tried again at Lay held.
- Hull damage from any cause starts battle stations unless the DamageStartsBattle setting is off. Owner gameplay checks remain pending.
