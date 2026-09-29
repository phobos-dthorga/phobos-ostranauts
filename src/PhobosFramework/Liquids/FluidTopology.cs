using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>The cells one segment family may carry fluid through on one ship grid, grouped into connected
/// components. Pure: the native adapter decides which cells qualify (an intact installed segment over sound
/// structural floor) and how long a snapshot may be reused. A route between two cells exists exactly when the old
/// per-call breadth-first search would have found one: both cells lie in the same component and the bounded search
/// reaches the goal, so <see cref="Path"/> gives the same cells as <see cref="GridRoute.Find"/> did.</summary>
public sealed class FluidTopology
{
    public int Columns { get; }
    public int Rows { get; }
    /// <summary>More segment cells than the visit limit: no route is trusted, as before (a bounded search must never
    /// hide a remote second source behind an unvisited tail).</summary>
    public bool Overflow { get; }
    private readonly HashSet<int> allowed;
    private readonly Dictionary<int, int> component;
    private readonly int[] start = new int[1];
    private readonly HashSet<int> goal = new();

    private FluidTopology(int columns, int rows, HashSet<int> allowed, Dictionary<int, int> component, bool overflow)
    { Columns = columns; Rows = rows; this.allowed = allowed; this.component = component; Overflow = overflow; }

    /// <summary>Builds the snapshot. <paramref name="segmentCount"/> is every segment cell found before floor checks,
    /// the count the old search bounded; <paramref name="allowedCells"/> are the ones over sound floor.</summary>
    public static FluidTopology Build(int columns, int rows, int segmentCount, IEnumerable<int> allowedCells, int visitLimit = GridRoute.DefaultVisitLimit)
    {
        if (columns <= 0 || rows <= 0 || (long)columns * rows > int.MaxValue || visitLimit < 1) throw new ArgumentException(Text.Get("GridRoute.invalid_route_bounds"));
        int count = columns * rows;
        var allowed = new HashSet<int>();
        foreach (int cell in allowedCells) if (cell >= 0 && cell < count) allowed.Add(cell);
        var component = new Dictionary<int, int>();
        var queue = new Queue<int>();
        int next = 0;
        foreach (int seed in allowed)
        {
            if (component.ContainsKey(seed)) continue;
            int id = next++;
            component[seed] = id; queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                void Visit(int n) { if (allowed.Contains(n) && !component.ContainsKey(n)) { component[n] = id; queue.Enqueue(n); } }
                if (cell % columns > 0) Visit(cell - 1);
                if (cell % columns + 1 < columns) Visit(cell + 1);
                if (cell >= columns) Visit(cell - columns);
                if (cell < count - columns) Visit(cell + columns);
            }
        }
        return new FluidTopology(columns, rows, allowed, component, segmentCount > visitLimit);
    }
    public int AllowedCount => allowed.Count;
    public int ComponentCount { get { var seen = new HashSet<int>(component.Values); return seen.Count; } }
    public bool Allowed(int cell) => allowed.Contains(cell);
    /// <summary>The component a cell belongs to, or -1 when it carries no fluid.</summary>
    public int ComponentOf(int cell) => component.TryGetValue(cell, out int id) ? id : -1;
    /// <summary>Whether two cells share one connected run of segments (a second source on the same run would
    /// share the circuit).</summary>
    public bool Connected(int a, int b) => !Overflow && a >= 0 && b >= 0 && component.TryGetValue(a, out int x) && component.TryGetValue(b, out int y) && x == y;
    /// <summary>The bounded cardinal route between two cells, or null; identical to the former per-call search.</summary>
    public int[]? Path(int startCell, int goalCell, int visitLimit = GridRoute.DefaultVisitLimit)
    {
        if (!Connected(startCell, goalCell)) return null;
        start[0] = startCell; goal.Clear(); goal.Add(goalCell);
        return GridRoute.Find(Columns, Rows, start, goal, Allowed, visitLimit);
    }
}
