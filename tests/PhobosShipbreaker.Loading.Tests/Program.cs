using System;
using System.Collections.Generic;
using PhobosShipbreaker;

int checks = 0;
void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks++; }
var spawned = new Dictionary<string, CondOwner> { ["marker"] = new CondOwner() };
var parent = new UnityEngine.GameObject();
void Apply(Ship ship, bool template = false, Ship.Loaded requested = Ship.Loaded.Full,
    Dictionary<string, CondOwner>? markers = null) => PlaceholderLoadRepair.Postfix(ship, template, requested, markers ?? spawned, parent);

var ship = new Ship(); var saved = ship.json; var rooms = saved.aRooms; var markers = saved.aPlaceholders;
Apply(ship);
Check(TileUtils.Calls == 1 && ship.nCols == 63 && ship.nRows == 44, "Owned pending G4 expands native grid before room restoration");
Check(ReferenceEquals(saved, ship.json) && ReferenceEquals(rooms, saved.aRooms) && ReferenceEquals(markers, saved.aPlaceholders),
    "Saved records retain their identities and are never rewritten");
Apply(ship);
Check(TileUtils.Calls == 1, "Repeated hook is a no-op once the saved bounds match");
foreach (Action<Ship> alter in new Action<Ship>[] {
    s => s.json.aPlaceholders[0].strInstalledCO = "Foreign",
    s => s.json.aPlaceholders[0].strName = "not-spawned",
    s => s.json.aPlaceholders = Array.Empty<Marker>(),
    s => s.json.aRooms = Array.Empty<object>(),
    s => s.LoadState = Ship.Loaded.Full,
    s => s.json.nCols = 61,
    s => s.json.nCols = 5000,
    s => s.json.vShipPos = new Point(-31.5f, -14),
    s => s.aTiles.RemoveAt(0)
})
{
    ship = new Ship(); alter(ship); Apply(ship);
    Check(TileUtils.Calls == 1 && ship.nCols == 62, "Unrelated, incomplete or unsupported loads cannot change geometry");
}
Apply(new Ship(), template: true);
Apply(new Ship(), requested: Ship.Loaded.Shallow);
Content.Ready = false; Apply(new Ship()); Content.Ready = true;
spawned["marker"].Placeholder = false; Apply(new Ship()); spawned["marker"].Placeholder = true;
Apply(new Ship(), markers: new Dictionary<string, CondOwner>());
Check(TileUtils.Calls == 1, "Templates, shallow loads, inactive content and non-markers stay native");
ship = new Ship { LoadState = Ship.Loaded.Shallow }; Apply(ship);
Check(TileUtils.Calls == 2 && ship.nCols == 63, "Shallow-to-full promotion also receives the saved grid");
Check(Plugin.Messages.Contains("PlaceholderLoadRepair.unsupported"), "Unsupported geometry leaves an explicit diagnostic");
Console.WriteLine($"PASS: {checks} saved-grid loader boundary checks. No Unity session was run.");
