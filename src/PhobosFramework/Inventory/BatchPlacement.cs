using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Inventory;

public readonly struct ItemSize
{
    public readonly int Width, Height;
    public ItemSize(int width, int height) { Width = width; Height = height; }
}

public readonly struct Position
{
    public readonly int X, Y;
    public Position(int x, int y) { X = x; Y = y; }
}

/// <summary>One product for stacked placement: the kind it stacks with (null: it never stacks), its cells and its
/// stack limit.</summary>
public readonly struct StackItem
{
    public readonly string? Kind; public readonly ItemSize Size; public readonly int Limit;
    public StackItem(string? kind, ItemSize size, int limit) { Kind = kind; Size = size; Limit = limit; }
    public bool Stacks => Kind != null && Limit > 1;
}

/// <summary>A stack already in the tray that takes more of a kind.</summary>
public readonly struct StackRoom
{
    public readonly string Kind; public readonly int Room;
    public StackRoom(string kind, int room) { Kind = kind; Room = room; }
}

/// <summary>Where each product of a batch goes: into an existing stack, or into a new stack with its own cells.</summary>
public sealed class StackedPlan
{
    /// <summary>Per product: the index of the existing stack it joins, or -1.</summary>
    public int[] Existing { get; }
    /// <summary>Per product: the index of the new stack it joins, or -1.</summary>
    public int[] New { get; }
    /// <summary>Per new stack: its top-left cell.</summary>
    public Position[] Positions { get; }
    public int NewStacks => Positions.Length;
    internal StackedPlan(int[] existing, int[] @new, Position[] positions) { Existing = existing; New = @new; Positions = positions; }
}

public static class BatchPlacement
{
    /// <summary>Stacked placement (Framework 0.71.0; owner direction, 1 October 2026: trays sized to the job): each
    /// product that stacks first tops up the existing stacks of its kind, in the order given, then joins a new stack
    /// of its kind up to the stack limit; a product that never stacks takes its own cells. New stacks are placed like
    /// single items (<see cref="Plan"/>). Null when the new stacks find no room; nothing is reserved then.</summary>
    public static StackedPlan? PlanStacked(bool[,] occupied, IReadOnlyList<StackRoom> existing, IReadOnlyList<StackItem> items)
    {
        var room = new int[existing.Count];
        for (int e = 0; e < room.Length; e++) room[e] = Math.Max(0, existing[e].Room);
        var joins = new int[items.Count]; var starts = new int[items.Count];
        var stacks = new List<(string? Kind, ItemSize Size, int Limit, int Count)>();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i]; joins[i] = starts[i] = -1;
            if (item.Stacks)
            {
                for (int e = 0; e < room.Length && joins[i] < 0; e++)
                    if (room[e] > 0 && existing[e].Kind == item.Kind) { room[e]--; joins[i] = e; }
                if (joins[i] >= 0) continue;
                for (int n = 0; n < stacks.Count && starts[i] < 0; n++)
                    if (stacks[n].Kind == item.Kind && stacks[n].Count < stacks[n].Limit) { stacks[n] = (stacks[n].Kind, stacks[n].Size, stacks[n].Limit, stacks[n].Count + 1); starts[i] = n; }
                if (starts[i] >= 0) continue;
            }
            stacks.Add((item.Stacks ? item.Kind : null, item.Size, item.Limit, 1)); starts[i] = stacks.Count - 1;
        }
        // Larger pieces take their cells first (in the order made, among equals), so two single cells never split the
        // space a 2 x 2 residue needs in a small tray.
        var order = new int[stacks.Count];
        for (int n = 0; n < order.Length; n++) order[n] = n;
        Array.Sort(order, (a, b) =>
        {
            int area = stacks[b].Size.Width * stacks[b].Size.Height - stacks[a].Size.Width * stacks[a].Size.Height;
            return area != 0 ? area : a.CompareTo(b);
        });
        var sizes = new ItemSize[stacks.Count];
        for (int n = 0; n < sizes.Length; n++) sizes[n] = stacks[order[n]].Size;
        var placed = Plan(occupied, sizes);
        if (placed == null) return null;
        var positions = new Position[stacks.Count];
        for (int n = 0; n < order.Length; n++) positions[order[n]] = placed[n];
        return new StackedPlan(joins, starts, positions);
    }

    // Reserve every new item's cells in a private copy. Never merge existing stacks.
    public static Position[]? Plan(bool[,] occupied, IReadOnlyList<ItemSize> sizes)
    {
        var cells = (bool[,])occupied.Clone();
        int width = cells.GetLength(0), height = cells.GetLength(1);
        var result = new Position[sizes.Count];
        for (int i = 0; i < sizes.Count; i++)
        {
            var size = sizes[i];
            if (size.Width <= 0 || size.Height <= 0) return null;
            bool placed = false;
            for (int y = 0; y <= height - size.Height && !placed; y++)
            for (int x = 0; x <= width - size.Width && !placed; x++)
            {
                bool free = true;
                for (int dy = 0; dy < size.Height; dy++)
                for (int dx = 0; dx < size.Width; dx++) free &= !cells[x + dx, y + dy];
                if (!free) continue;
                result[i] = new Position(x, y);
                for (int dy = 0; dy < size.Height; dy++)
                for (int dx = 0; dx < size.Width; dx++) cells[x + dx, y + dy] = true;
                placed = true;
            }
            if (!placed) return null;
        }
        return result;
    }
}

public interface IBatchDelivery
{
    bool Prepare();
    void PlaceProducts();
    void ConsumeInput();
    bool InputConsumed { get; }
    void RollbackProducts();
}

public enum DeliveryResult { Blocked, Completed }

public static class BatchDelivery
{
    public static DeliveryResult Commit(IBatchDelivery delivery)
    {
        if (delivery.InputConsumed) return DeliveryResult.Completed;
        try
        {
            if (!delivery.Prepare())
            { delivery.RollbackProducts(); return DeliveryResult.Blocked; }
            delivery.PlaceProducts();
            delivery.ConsumeInput();
            if (!delivery.InputConsumed) throw new InvalidOperationException(Text.Get("BatchPlacement.input_was_not_consumed"));
            return DeliveryResult.Completed;
        }
        catch
        {
            // Once destruction took effect, products are the only surviving material.
            // The caller still receives the exception and must pause, not retry blindly.
            if (!delivery.InputConsumed) delivery.RollbackProducts();
            throw;
        }
    }
}
