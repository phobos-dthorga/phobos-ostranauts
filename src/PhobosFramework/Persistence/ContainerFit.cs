using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Ostranauts.Inventory;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Registration;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>Keeps saved contents in view when a Phobos inventory becomes smaller (Framework 0.70.0; owner direction,
/// 1 October 2026: size every inventory to its job, and migrate saves automatically). The game rebuilds a container
/// from its definition's current grid and re-adds each saved item at its saved cell without a fit check, so an item
/// saved outside a smaller grid loads as cargo that holds no cell: it still weighs and still blocks Uninstall, and no
/// window shows it. Once a ship of the player's has loaded, every container with a declared
/// <see cref="EquipmentInventory"/> role is checked against its live grid:
/// <list type="number">
/// <item>Items that lie inside the grid stay where they are. Items a saved job names (content registers how to tell,
/// <see cref="KeepFirst"/>) are placed before the rest.</item>
/// <item>Items outside the grid move to the first free cells, in place: only their cell changes.</item>
/// <item>What finds no cell goes to the deck beside the equipment through the game's own drop, as ordinary cargo. A
/// legacy receptacle (equipment that no longer has an inventory) puts everything on the deck.</item>
/// <item>The player is told once per ship, in the crew log.</item>
/// </list>
/// Nothing is destroyed or hidden, no save file is edited, and a second pass changes nothing. A locked container, or
/// one whose window is open, is left for a later pass.</summary>
public static class ContainerFit
{
    /// <summary>What one pass over one ship did.</summary>
    public sealed class Report
    {
        public int Moved, OnDeck, Deferred;
        public readonly List<string> Equipment = new();
        public bool Any => Moved + OnDeck > 0;
    }

    public const double RetrySeconds = 15;
    private static readonly List<(string Prefix, Func<CondOwner, CondOwner, bool> Named)> keepers = new();
    private static readonly Cadence cadence = new(RetrySeconds);
    // Ships already brought into line since they loaded; a reloaded save makes new ship objects.
    private static ConditionalWeakTable<Ship, object> settled = new();
    internal static void Reset() { keepers.Clear(); settled = new ConditionalWeakTable<Ship, object>(); cadence.Invalidate(); }

    /// <summary>Says which contents of a family a saved job names (a bound input, a dose in hand), so they keep their
    /// place inside the machine before anything else is fitted.</summary>
    public static void KeepFirst(string prefix, Func<CondOwner, CondOwner, bool> named)
    {
        if (string.IsNullOrEmpty(prefix) || named == null) throw new ArgumentException("A keep-first rule needs a family prefix and a test.");
        keepers.Add((prefix, named));
    }

    internal static void Poll()
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.system?.dictShips == null || !cadence.Due()) return;
        foreach (var ship in CrewSim.system.dictShips.Values.ToArray())
        {
            if (ship == null || ship.bDestroyed || (int)ship.LoadState < 2 || CrewSim.system.GetShipOwner(ship.strRegID) != CrewSim.coPlayer?.strID) continue;
            if (settled.TryGetValue(ship, out _)) continue;
            try
            {
                var report = Sweep(ship);
                if (report.Deferred == 0) settled.Add(ship, new object());
                if (report.Moved > 0) FrameworkLifecycle.Log(Text.Get("ContainerFit.moved_log", ship.strRegID, report.Moved));
                if (report.OnDeck == 0) continue;
                string names = string.Join(", ", report.Equipment.Distinct().OrderBy(n => n, StringComparer.CurrentCulture));
                FrameworkLifecycle.Log(Text.Get("ContainerFit.deck_log", ship.strRegID, report.OnDeck, names));
                Notices.PlayerNotices.Post(ship, "ContainerFit", Notices.NoticeLevel.Info, Text.Get("ContainerFit.notice", report.OnDeck, names));
            }
            catch (Exception e)
            {
                // A fault here must not repeat every pass: the ship is left as it loaded.
                settled.Add(ship, new object());
                FrameworkLifecycle.Log(Text.Get("ContainerFit.failed", ship.strRegID, e.Message));
            }
        }
    }

    /// <summary>Brings every declared container aboard one loaded ship into line with its live grid.</summary>
    internal static Report Sweep(Ship ship)
    {
        var report = new Report();
        if (ship == null) return report;
        foreach (var co in ship.GetCOs(null, true, false, true))
        {
            if (co == null || co.bDestroyed || co.ship != ship || co.objContainer == null) continue;
            var spec = EquipmentInventory.Of(co.strCODef);
            if (spec == null || co.HasCond("IsInfiniteContainer")) continue;
            Fit(ship, co, spec.Value, report);
        }
        return report;
    }

    private static void Fit(Ship ship, CondOwner co, InventorySpec spec, Report report)
    {
        var container = co.objContainer; var grid = container.gridLayout;
        var heads = container.ContainedCOs.Where(c => c != null && !c.bDestroyed && c.coStackHead == null).OrderBy(c => c.strID, StringComparer.Ordinal).ToArray();
        if (heads.Length == 0) return;
        var moves = new Dictionary<string, (int X, int Y)>(StringComparer.Ordinal);
        var overflow = new List<CondOwner>();
        if (spec.Role == InventoryRole.LegacyReceptacle) overflow.AddRange(heads);
        else
        {
            // An item with no saved cell at all was not put out of place by a smaller grid; it is left alone.
            var placed = heads.Where(c => c.pairInventoryXY.IsValid()).ToArray();
            var named = keepers.Where(k => EquipmentIdentity.IsFamily(co.strCODef, k.Prefix)).Select(k => k.Named).ToArray();
            var plan = GridFit.Fit(grid.gridMaxX, grid.gridMaxY, placed.Select(c =>
            {
                var size = GUIInventoryItem.GetWidthHeightForCO(c);
                return new GridFit.Item(c.strID, c.pairInventoryXY.x, c.pairInventoryXY.y, Math.Max(1, size.x), Math.Max(1, size.y), named.Any(n => n(Root(co), c)));
            }).ToArray());
            if (!plan.Changes) return;
            foreach (var move in plan.Moves) moves[move.Key] = move.Value;
            overflow.AddRange(plan.Overflow.Select(id => placed.First(c => c.strID == id)));
        }
        // A window on this container shows the old cells, and a locked container is not opened for the player.
        if (container.InventoryWindow != null || overflow.Count > 0 && (co.HasCond("IsLocked") || Root(co).HasCond("IsLocked"))) { report.Deferred++; return; }
        foreach (var head in heads)
        {
            if (!moves.TryGetValue(head.strID, out var cell)) continue;
            Move(container, head, cell.X, cell.Y);
            report.Moved++;
        }
        if (overflow.Count == 0) return;
        // Units with no cell left join stacks of their own kind inside the grid first (Framework 0.71.0): a tray saved
        // full of single items, as trays were before products were delivered into stacks, re-packs instead of spilling.
        var leaving = new List<CondOwner>();
        foreach (var head in overflow)
        {
            if (spec.Role == InventoryRole.LegacyReceptacle) { leaving.Add(head); continue; }
            leaving.AddRange(Absorb(container, head, out int absorbed));
            report.Moved += absorbed;
        }
        if (leaving.Count == 0) return;
        var anchor = LegacyItemConversions.Anchor(co);
        foreach (var head in leaving)
        {
            if (head.objCOParent != null || head.ship != null) head.RemoveFromCurrentHome(true);
            try { LegacyItemConversions.Drop(ship, head, anchor); }
            catch { if (!head.bDestroyed && head.ship == null) ship.AddCO(head, true); throw; }
            report.OnDeck += Math.Max(1, head.StackCount);
        }
        report.Equipment.Add(Controls.ObjectPresentation.Name(Root(co)));
    }

    // Takes a stack that has no cell out of the container and gives its units, one at a time, to the stacks of their
    // kind that have room and to any free cell. Returns what found no place, re-stacked, for the deck. A stack of
    // mixed or recorded-unlike units is returned untouched.
    private static List<CondOwner> Absorb(Container container, CondOwner head, out int absorbed)
    {
        absorbed = 0;
        var units = head.StackAsList;
        string? kind = StackUnits.Kind(head);
        if (!container.bAllowStacking || kind == null || units.Any(u => StackUnits.Kind(u) != kind)) return new List<CondOwner> { head };
        head.RemoveFromCurrentHome(true);
        if (head.objCOParent != null) return new List<CondOwner> { head };
        foreach (var unit in units) { unit.aStack.Clear(); unit.coStackHead = null; unit.tf.SetParent(null, true); }
        var left = new List<CondOwner>();
        foreach (var unit in units)
        {
            TrayDelivery? delivery = null;
            try
            {
                delivery = TrayDelivery.Plan(container, new[] { unit });
                if (delivery != null) { delivery.Place(); absorbed++; continue; }
            }
            catch (Exception e) { delivery?.Rollback(); FrameworkLifecycle.Log(e.Message); }
            left.Add(unit);
        }
        var stacks = new List<CondOwner>();
        int limit = Math.Max(1, head.nStackLimit);
        for (int i = 0; i < left.Count; i += limit)
        {
            var chunk = left.GetRange(i, Math.Min(limit, left.Count - i));
            var top = chunk.Count == 1 ? chunk[0] : CondOwner.StackFromList(chunk);
            top.Visible = true;
            stacks.Add(top);
        }
        return stacks;
    }

    // Only the item's cell changes: it stays contained, so mass, parent and identity are untouched.
    private static void Move(Container container, CondOwner head, int x, int y)
    {
        var grid = container.gridLayout;
        grid.Remove(head.strID);
        var cell = new PairXY(x, y);
        head.pairInventoryXY = cell;
        foreach (var member in head.aStack) member.pairInventoryXY = cell;
        var size = GUIInventoryItem.GetWidthHeightForCO(head);
        for (int row = y; row < y + Math.Max(1, size.y); row++)
            for (int column = x; column < x + Math.Max(1, size.x); column++)
                if (column >= 0 && row >= 0 && column < grid.gridMaxX && row < grid.gridMaxY) grid.gridID[column, row] = head.strID;
    }

    // The machine a hidden bin belongs to (a feed bin sits in its machine's slot); the object itself otherwise.
    private static CondOwner Root(CondOwner co)
    {
        var root = co;
        while (root.objCOParent != null && !root.objCOParent.HasCond("IsHuman") && !root.objCOParent.HasCond("IsRobot")) root = root.objCOParent;
        return root;
    }
}
