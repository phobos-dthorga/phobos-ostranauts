using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Where saved contents go when a container's grid is smaller than the one they were saved in (Framework
/// 0.70.0). Pure. Items a saved job names keep or get a place first; everything else that still lies wholly inside
/// the grid on free cells keeps its place; the rest takes the first free place in the game's own order (row by row
/// from the top-left); what finds none is overflow, for the deck. Planning twice changes nothing.</summary>
public static class GridFit
{
    public readonly struct Item
    {
        public string Id { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        /// <summary>Named by a saved job on the machine: placed before anything else.</summary>
        public bool KeepFirst { get; }
        public Item(string id, int x, int y, int width, int height, bool keepFirst = false)
        {
            if (string.IsNullOrEmpty(id) || width < 1 || height < 1) throw new ArgumentException("A grid item needs an id and a size of at least one cell.");
            Id = id; X = x; Y = y; Width = width; Height = height; KeepFirst = keepFirst;
        }
    }
    public sealed class Plan
    {
        /// <summary>Items that move inside the grid, with their new top-left cell.</summary>
        public Dictionary<string, (int X, int Y)> Moves { get; } = new(StringComparer.Ordinal);
        /// <summary>Items with no place left, in the order they were tried.</summary>
        public List<string> Overflow { get; } = new();
        public bool Changes => Moves.Count > 0 || Overflow.Count > 0;
    }

    public static Plan Fit(int width, int height, IReadOnlyList<Item> items)
    {
        if (width < 0 || height < 0) throw new ArgumentException("Invalid grid.");
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (items.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != items.Count) throw new ArgumentException("Grid items need distinct ids.");
        var plan = new Plan();
        var taken = new bool[Math.Max(width, 1), Math.Max(height, 1)];
        bool Inside(Item i) => i.X >= 0 && i.Y >= 0 && i.X + i.Width <= width && i.Y + i.Height <= height;
        bool Free(int x, int y, int w, int h)
        {
            for (int row = y; row < y + h; row++) for (int column = x; column < x + w; column++) if (taken[column, row]) return false;
            return true;
        }
        void Take(int x, int y, int w, int h) { for (int row = y; row < y + h; row++) for (int column = x; column < x + w; column++) taken[column, row] = true; }
        bool Stay(Item i) { if (!Inside(i) || !Free(i.X, i.Y, i.Width, i.Height)) return false; Take(i.X, i.Y, i.Width, i.Height); return true; }
        void Place(Item i)
        {
            for (int y = 0; y + i.Height <= height; y++)
                for (int x = 0; x + i.Width <= width; x++)
                    if (Free(x, y, i.Width, i.Height)) { Take(x, y, i.Width, i.Height); plan.Moves[i.Id] = (x, y); return; }
            plan.Overflow.Add(i.Id);
        }
        // Larger pieces first within each pass, then by id, so the result does not depend on the order given.
        IEnumerable<Item> Ordered(IEnumerable<Item> source) => source.OrderByDescending(i => i.Width * i.Height).ThenBy(i => i.Id, StringComparer.Ordinal);
        var pending = new List<Item>();
        foreach (var item in Ordered(items.Where(i => i.KeepFirst))) if (!Stay(item)) pending.Add(item);
        foreach (var item in pending) Place(item);
        pending.Clear();
        foreach (var item in Ordered(items.Where(i => !i.KeepFirst))) if (!Stay(item)) pending.Add(item);
        foreach (var item in pending) Place(item);
        return plan;
    }
}
