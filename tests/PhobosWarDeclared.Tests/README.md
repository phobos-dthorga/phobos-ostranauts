# War Has Been Declared checks

`dotnet run --project tests/PhobosWarDeclared.Tests -c Release` compiles the
mod's `Core/*.cs` sources directly and checks the pure rules: the battle-stations
window opens on a hostile lock, your own lock or fresh damage, holds for one quiet
period after the last activity, stays open under a manual order, and after Stand
down ignores a fight the game still lists until that listing clears; every shipped
schematic parses and decides floors, walls, doors, machinery and unknown parts as
documented; custom schematics use first-match rules, all-fields matching and refuse
misspelt fields, empty lists and unknown actions; the ledger records each part ID
once, orders floors first, holds parts that do not fit or keep failing, retries when
the player is holding an item, and round-trips through the Framework state store's
value rules, refusing malformed saved entries so the record can be kept intact.

`tests/PhobosNative.Tests/WarDeclaredNativeChecks.cs` runs with the local game:
it runs the game's own `Installables.Create` over every native install job, then
checks damaged walls and conduit (a cosmetic overlay) resolve to their intact
forms, closed and locked doors to the door's install job through its uninstall
job, scrap to nothing, at least nine in ten uninstallable native parts to a tabbed
intact install, wall and door build sites blocking walking while floors and conduit
do not, every Phobos damaged form resolving to its intact form, the safe schematic
on real parts, and the three orders appearing once on every installed navigation
station and nowhere else. No game session is run by either suite.
