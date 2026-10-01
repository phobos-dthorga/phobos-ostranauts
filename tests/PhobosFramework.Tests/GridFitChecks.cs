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

        // Framework 0.71.0 stacked placement: a batch tops up the stacks of its kind already in the tray, then forms new
        // stacks up to the stack limit; what never stacks takes its own cells.
        var one = new ItemSize(1, 1);
        StackItem[] Batch(params (string? Kind, int Count, int Limit)[] parts) => parts.SelectMany(p => Enumerable.Repeat(new StackItem(p.Kind, one, p.Limit), p.Count)).ToArray();
        // Applies a plan to a model tray (occupied cells and stack counts by kind) and returns the cells in use.
        int Deliver(bool[,] cells, List<(string Kind, int Count, int Limit)> stacks, StackItem[] batch, out bool fits)
        {
            var room = stacks.Select(s => new StackRoom(s.Kind, s.Limit - s.Count)).ToArray();
            var plan = BatchPlacement.PlanStacked(cells, room, batch); fits = plan != null;
            if (plan == null) return cells.Cast<bool>().Count(c => c);
            for (int i = 0; i < batch.Length; i++) if (plan.Existing[i] >= 0) { var s = stacks[plan.Existing[i]]; stacks[plan.Existing[i]] = (s.Kind, s.Count + 1, s.Limit); }
            for (int n = 0; n < plan.NewStacks; n++)
            {
                cells[plan.Positions[n].X, plan.Positions[n].Y] = true;
                int count = Enumerable.Range(0, batch.Length).Count(i => plan.New[i] == n); var first = batch[Array.IndexOf(plan.New, n)];
                if (first.Stacks) stacks.Add((first.Kind!, count, first.Limit));
            }
            return cells.Cast<bool>().Count(c => c);
        }
        // The R4's packet (three steel, one aluminium, one reject that never stacks) four times over into 3 x 2 cells.
        var trayCells = new bool[3, 2]; var trayStacks = new List<(string, int, int)>(); bool ok = true; int used = 0;
        for (int packet = 0; packet < 4 && ok; packet++) used = Deliver(trayCells, trayStacks, Batch(("steel", 3, 15), ("aluminium", 1, 15), (null, 1, 1)), out ok);
        check(ok && used == 6 && trayStacks.Count == 2 && trayStacks[0] == ("steel", 12, 15) && trayStacks[1] == ("aluminium", 4, 15),
            "Four reclaimer packets fill a six-cell tray: one steel stack, one aluminium stack and four rejects");
        Deliver(trayCells, trayStacks, Batch(("steel", 3, 15), ("aluminium", 1, 15), (null, 1, 1)), out bool fifth);
        check(!fifth && trayStacks[0] == ("steel", 12, 15), "A fifth packet waits: its reject has no cell, and nothing of it is placed");
        // The heaviest wall's thirty-seven products twice over into 4 x 3 cells.
        var wall = Batch(("steel", 30, 15), ("aluminium", 4, 15), ("parts", 2, 20), (null, 1, 1));
        var d4 = new bool[4, 3]; var d4Stacks = new List<(string, int, int)>();
        int firstWall = Deliver(d4, d4Stacks, wall, out bool wallOne), secondWall = Deliver(d4, d4Stacks, wall, out bool wallTwo);
        check(wallOne && firstWall == 5 && wallTwo && secondWall == 8, "Thirty-seven products take five cells, and a second batch tops up the open stacks before taking three more");
        var unstacked = BatchPlacement.PlanStacked(new bool[2, 1], Array.Empty<StackRoom>(), Batch((null, 2, 15)));
        check(unstacked != null && unstacked.NewStacks == 2 && unstacked.Existing.All(e => e < 0), "Products without a kind never share a cell");
        check(BatchPlacement.PlanStacked(new bool[1, 1], new[] { new StackRoom("steel", 2) }, Batch(("steel", 2, 15))) is { NewStacks: 0 } &&
              BatchPlacement.PlanStacked(new bool[0, 0], new[] { new StackRoom("steel", 1) }, Batch(("steel", 2, 15))) == null,
            "A batch that fits wholly into open stacks needs no free cell; one unit over needs one");
        check(BatchPlacement.PlanStacked(new bool[2, 2], new[] { new StackRoom("aluminium", 9) }, Batch(("steel", 16, 15))) is { NewStacks: 2 },
            "Room in a stack of another kind is never used, and a stack never exceeds its limit");

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
