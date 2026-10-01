using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;

/// <summary>Framework 0.70.0: saved contents against a smaller grid (the automatic save migration behind the fitted
/// inventory sizes), and the declared inventory roles.</summary>
internal static class GridFitChecks
{
    internal static void Run(Action<bool, string> check)
    {
        GridFit.Item One(string id, int x, int y, bool keep = false) => new(id, x, y, 1, 1, keep);
        // An old 8 x 8 tray saved with thirty single-cell items in its first rows, loaded into a 2 x 2 rack.
        var thirty = Enumerable.Range(0, 30).Select(n => One("item" + n.ToString("00"), n % 8, n / 8)).ToArray();
        var rack = GridFit.Fit(2, 2, thirty);
        check(rack.Overflow.Count == 26 && rack.Moves.Count == 0 && new[] { "item00", "item01", "item08", "item09" }.All(id => !rack.Overflow.Contains(id)),
            "The four items already inside the smaller grid stay put and the rest go to the deck");
        var spread = GridFit.Fit(2, 2, new[] { One("a", 0, 0), One("b", 5, 5), One("c", 6, 6) });
        check(spread.Overflow.Count == 0 && spread.Moves["b"] == (1, 0) && spread.Moves["c"] == (0, 1), "Free cells are taken row by row from the top-left, as the game does");
        // Applying the plan and planning again changes nothing.
        var after = thirty.Where(i => !rack.Overflow.Contains(i.Id)).Select(i => rack.Moves.TryGetValue(i.Id, out var m) ? One(i.Id, m.X, m.Y) : i).ToArray();
        check(!GridFit.Fit(2, 2, after).Changes, "A second pass over a fitted container changes nothing");
        check(!GridFit.Fit(8, 8, thirty).Changes, "Contents of an unchanged grid are never touched");
        // Twenty charge pieces saved across a 10 x 8 chamber all fit a 5 x 4 one.
        var charge = Enumerable.Range(0, 20).Select(n => One("piece" + n.ToString("00"), n % 10, n / 10)).ToArray();
        var chamber = GridFit.Fit(5, 4, charge);
        check(chamber.Overflow.Count == 0 && chamber.Moves.Count == 10, "A full furnace charge re-packs into the smaller chamber with nothing left over");
        // A job's bound input keeps a place even when everything else was saved ahead of it.
        var crowded = Enumerable.Range(0, 4).Select(n => One("loose" + n, n % 2, n / 2)).Concat(new[] { One("bound", 7, 7, keep: true) }).ToArray();
        var bound = GridFit.Fit(2, 2, crowded);
        check(bound.Moves.ContainsKey("bound") && bound.Overflow.Count == 1 && !bound.Overflow.Contains("bound"), "An item a saved job names is placed before the rest; one loose item gives way");
        // A 2 x 2 piece half outside the grid moves as a whole; one with no room overflows.
        var wide = GridFit.Fit(3, 2, new[] { new GridFit.Item("cake", 2, 0, 2, 2), One("a", 0, 0) });
        check(wide.Moves.TryGetValue("cake", out var cake) && cake == (1, 0) && wide.Overflow.Count == 0, "A piece that sticks out of the grid moves to where it fits whole");
        check(GridFit.Fit(1, 1, new[] { new GridFit.Item("cake", 0, 0, 2, 2) }).Overflow.SequenceEqual(new[] { "cake" }), "A piece larger than the grid overflows");
        check(GridFit.Fit(0, 0, new[] { One("a", 0, 0) }).Overflow.Count == 1, "A grid with no cells keeps nothing");
        bool refused = false; try { GridFit.Fit(2, 2, new[] { One("a", 0, 0), One("a", 1, 0) }); } catch (ArgumentException) { refused = true; }
        check(refused, "Two items with one id are refused");

        // Roles: a role needs a grid and a trigger; none has neither.
        var tray = InventorySpec.ProductTray(4, 3);
        check(tray.Cells == 12 && tray.Trigger == EquipmentInventory.Solid && tray.Sized(2, 2).Role == InventoryRole.ProductTray && tray.Sized(2, 2).Cells == 4,
            "A product tray takes ordinary solid cargo and can be resized without changing what it is");
        check(InventorySpec.None.Cells == 0 && InventorySpec.None.Trigger == null && InventorySpec.LegacyReceptacle(8, 8).Trigger == EquipmentInventory.NoNewCargo,
            "No inventory has no grid; a legacy receptacle admits nothing new");
        bool empty = false; try { InventorySpec.ServiceRack(0, 2); } catch (ArgumentException) { empty = true; }
        check(empty, "A rack without cells is refused");
    }
}
