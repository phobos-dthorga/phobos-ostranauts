using System;
using System.Collections.Generic;
using System.Linq;
using Ostranauts.Inventory;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Puts a batch's products into a machine's tray as stacks (Framework 0.71.0; owner direction, 1 October
/// 2026: a tray holds about one to two batches, so products are delivered into stacks). A plain product
/// (<see cref="StackUnits.Plain"/>) first tops up the plain stacks of its definition already in the tray, then forms
/// new stacks up to the game's stack limit; anything else takes its own cells, as before. Only the game's own calls
/// are used, in the order the game uses them: a new stack is built with <c>CondOwner.StackFromList</c> and added at
/// its cell with <c>Container.AddCOSimple</c> (how a saved stack is restored), and a stack that takes more units is
/// taken out, rebuilt with its old head still on top and put back at the same cell (how the game pops a stack's
/// head). Planning reserves nothing; <see cref="Place"/> throws when the tray no longer matches the plan, and
/// <see cref="Rollback"/> takes every placed product back out as a single loose unit and leaves the stacks that were
/// there as they were. Masses, identities and counts of the products are never changed.</summary>
public sealed class TrayDelivery
{
    private readonly Container tray;
    private readonly IReadOnlyList<CondOwner> products;
    private readonly StackedPlan plan;
    private readonly CondOwner[] stacks;
    private readonly List<CondOwner> placed = new();
    private TrayDelivery(Container tray, IReadOnlyList<CondOwner> products, StackedPlan plan, CondOwner[] stacks)
    { this.tray = tray; this.products = products; this.plan = plan; this.stacks = stacks; }

    /// <summary>The cells a tray's contents hold, as the game's own free-cell search sees them.</summary>
    public static bool[,] Occupancy(Container tray)
    {
        var grid = tray.gridLayout;
        var result = new bool[grid.gridMaxX, grid.gridMaxY];
        for (int x = 0; x < grid.gridMaxX; x++)
            for (int y = 0; y < grid.gridMaxY; y++)
                result[x, y] = grid.gridID[x, y] != null || grid.gridInventoryItem[x, y] != null;
        return result;
    }
    // The plain stacks in the tray, in id order, with the room each has.
    private static (CondOwner[] Heads, StackRoom[] Room) Stacks(Container tray)
    {
        var heads = new List<CondOwner>(); var room = new List<StackRoom>();
        if (!tray.bAllowStacking) return (heads.ToArray(), room.ToArray());
        foreach (var head in tray.ContainedCOs.Where(c => c != null && !c.bDestroyed && c.coStackHead == null).OrderBy(c => c.strID, StringComparer.Ordinal))
        {
            string? kind = StackUnits.Kind(head);
            int free = kind == null ? 0 : StackUnits.Room(head, kind);
            if (free > 0) { heads.Add(head); room.Add(new StackRoom(kind!, free)); }
        }
        return (heads.ToArray(), room.ToArray());
    }

    /// <summary>Plans where freshly made products go, or null when the tray has no room for them. The products must
    /// be loose single units that the tray admits; nothing is moved.</summary>
    public static TrayDelivery? Plan(Container tray, IReadOnlyList<CondOwner> products)
    {
        if (tray == null || products == null) throw new ArgumentNullException(tray == null ? nameof(tray) : nameof(products));
        var (heads, room) = Stacks(tray);
        var items = products.Select(p =>
        {
            var size = GUIInventoryItem.GetWidthHeightForCO(p);
            return new StackItem(tray.bAllowStacking ? StackUnits.Kind(p) : null, new ItemSize(size.x, size.y), p.nStackLimit);
        }).ToArray();
        var plan = BatchPlacement.PlanStacked(Occupancy(tray), room, items);
        return plan == null ? null : new TrayDelivery(tray, products, plan, heads);
    }

    /// <summary>Whether a batch described by definitions alone would find room now: the check a machine makes before
    /// it spends power on a job. Each product counts as a fresh item of its definition (plain when the game stacks it).</summary>
    public static bool Fits(Container? tray, IEnumerable<(string Definition, int Count)> products)
    {
        if (tray == null || products == null) return false;
        var (_, room) = Stacks(tray);
        var items = new List<StackItem>();
        foreach (var (definition, count) in products)
        {
            var co = DataHandler.GetCondOwnerDef(definition);
            if (co == null || count < 0 || !DataHandler.dictItemDefs.TryGetValue(co.strItemDef, out var item) || item.nCols <= 0) return false;
            int width = co.inventoryWidth != 0 ? co.inventoryWidth : item.nCols;
            int height = co.inventoryHeight != 0 ? co.inventoryHeight : item.aSocketAdds.Length / item.nCols;
            var entry = new StackItem(tray.bAllowStacking && co.nStackLimit > 1 ? definition : null, new ItemSize(width, height), co.nStackLimit);
            for (int n = 0; n < count; n++) items.Add(entry);
        }
        return BatchPlacement.PlanStacked(Occupancy(tray), room, items) != null;
    }

    /// <summary>Carries out the plan. Throws when a stack or a cell changed since planning; the caller then rolls back.</summary>
    public void Place()
    {
        var owner = tray.CO;
        for (int e = 0; e < stacks.Length; e++)
        {
            var units = Enumerable.Range(0, products.Count).Where(i => plan.Existing[i] == e).Select(i => products[i]).ToList();
            if (units.Count == 0) continue;
            var head = stacks[e];
            if (head.bDestroyed || head.objCOParent != owner || head.coStackHead != null || StackUnits.Kind(units[0]) is not string kind || StackUnits.Room(head, kind) < units.Count)
                throw new InvalidOperationException("A stack in the tray changed before delivery.");
            var cell = head.pairInventoryXY;
            double before = head.GetTotalMass(), added = units.Sum(u => u.GetTotalMass());
            // Out, rebuilt with the old head still on top, and back at its cell: the game's own stack pop, in reverse.
            var members = head.StackAsList;
            head.RemoveFromCurrentHome(true);
            if (head.objCOParent != null) throw new InvalidOperationException("A stack could not be taken out of the tray.");
            int fresh = units.Count;
            units.AddRange(members);
            try
            {
                if (CondOwner.StackFromList(units) != head) throw new InvalidOperationException("A rebuilt stack lost its head.");
                placed.AddRange(units.Take(fresh));
                tray.AddCOSimple(head, cell);
            }
            catch
            {
                // The stack that was in the tray goes back as it was before anything else is undone.
                if (head.objCOParent == null && !head.bDestroyed)
                {
                    foreach (var unit in units.Take(fresh)) unit.coStackHead = null;
                    CondOwner.StackFromList(members);
                    tray.AddCOSimple(head, cell);
                }
                throw;
            }
            if (head.objCOParent != owner || !tray.ContainedCOs.Contains(head) || Math.Abs(head.GetTotalMass() - before - added) > 1e-7)
                throw new InvalidOperationException("A stack did not return to the tray with its new units.");
        }
        for (int n = 0; n < plan.NewStacks; n++)
        {
            var units = Enumerable.Range(0, products.Count).Where(i => plan.New[i] == n).Select(i => products[i]).ToList();
            if (units.Count == 0) continue;
            placed.AddRange(units);
            var head = units.Count == 1 ? units[0] : CondOwner.StackFromList(units);
            if (head == null) throw new InvalidOperationException("A new stack could not be formed.");
            tray.AddCOSimple(head, new PairXY(plan.Positions[n].X, plan.Positions[n].Y));
            if (head.objCOParent != owner || !tray.ContainedCOs.Contains(head) || head.StackCount != units.Count)
                throw new InvalidOperationException("A new stack was not placed in the tray.");
        }
    }

    /// <summary>Takes every product this delivery placed back out, one unit at a time, so each is a loose single item
    /// again (the caller destroys them). Stacks that were in the tray before keep their own units and their cell.</summary>
    public void Rollback()
    {
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            var unit = placed[i];
            if (unit == null || unit.bDestroyed) continue;
            if (unit.objCOParent != null || unit.ship != null || unit.coStackHead != null || unit.aStack.Count > 0) StackUnits.Detach(unit);
            // A unit formed into a stack that never reached the tray is still tied to its head.
            if (unit.coStackHead != null) { unit.coStackHead.aStack.Remove(unit); unit.coStackHead = null; }
            if (unit.aStack.Count > 0) { foreach (var member in unit.aStack.ToArray()) member.coStackHead = null; unit.aStack.Clear(); }
        }
        placed.Clear();
    }
}
