using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Inventory;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>The cells one segment family may carry fluid through on one ship grid, grouped into connected
/// components. Pure: the native adapter decides which cells qualify (an intact installed segment over sound
/// structural floor) and how long a snapshot may be reused. A route between two cells exists exactly when the old
/// per-call breadth-first search would have found one: both cells lie in the same component and the bounded search
/// reaches the goal, so <see cref="Path"/> gives the same cells as <see cref="GridRoute.Find"/> did.
///
/// A network family (Framework 0.56.0) also has participants: machines and vessels with a port of the family. A
/// participant joins the component of the segment at each of its ports, and, where the family allows it, the
/// component of every participant it touches (the owner's rule of 30 September 2026: touching equipment joins as if
/// piped, and joins chain). Participants are indices 0..P-1 in the order the adapter gave them.</summary>
public sealed class FluidTopology
{
    public int Columns { get; }
    public int Rows { get; }
    /// <summary>More segment cells than the visit limit: no route is trusted, as before (a bounded search must never
    /// hide a remote second source behind an unvisited tail).</summary>
    public bool Overflow { get; }
    private readonly HashSet<int> allowed;
    private readonly Dictionary<int, int> component;
    private readonly int[] participantComponent;
    private readonly int[][] participantPorts, participantJoins;
    private readonly Dictionary<int, List<int>> cellParticipants;
    private readonly Dictionary<int, int[]> hopMemo = new();
    private readonly int[] start = new int[1];
    private readonly HashSet<int> goal = new();

    private FluidTopology(int columns, int rows, HashSet<int> allowed, Dictionary<int, int> component, bool overflow,
        int[] participantComponent, int[][] participantPorts, int[][] participantJoins, Dictionary<int, List<int>> cellParticipants)
    {
        Columns = columns; Rows = rows; this.allowed = allowed; this.component = component; Overflow = overflow;
        this.participantComponent = participantComponent; this.participantPorts = participantPorts; this.participantJoins = participantJoins;
        this.cellParticipants = cellParticipants;
    }

    /// <summary>Builds the snapshot. <paramref name="segmentCount"/> is every segment cell found before floor checks,
    /// the count the old search bounded; <paramref name="allowedCells"/> are the ones over sound floor.
    /// <paramref name="participantPortCells"/> gives each participant's port cells (any cell; only segment cells
    /// connect), and <paramref name="joins"/> the participant pairs that touch.</summary>
    public static FluidTopology Build(int columns, int rows, int segmentCount, IEnumerable<int> allowedCells, int visitLimit = GridRoute.DefaultVisitLimit,
        IReadOnlyList<IReadOnlyList<int>>? participantPortCells = null, IEnumerable<(int A, int B)>? joins = null)
    {
        if (columns <= 0 || rows <= 0 || (long)columns * rows > int.MaxValue || visitLimit < 1) throw new ArgumentException(Text.Get("GridRoute.invalid_route_bounds"));
        int count = columns * rows, participants = participantPortCells?.Count ?? 0;
        var allowed = new HashSet<int>();
        foreach (int cell in allowedCells) if (cell >= 0 && cell < count) allowed.Add(cell);

        // Union-find over segment cells and participants; participants sit after the grid.
        var parent = new Dictionary<int, int>();
        int Find(int x) { while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; } return x; }
        void Union(int a, int b) { int ra = Find(a), rb = Find(b); if (ra != rb) parent[Math.Max(ra, rb)] = Math.Min(ra, rb); }
        foreach (int cell in allowed) parent[cell] = cell;
        for (int k = 0; k < participants; k++) parent[count + k] = count + k;
        foreach (int cell in allowed)
        {
            if (cell % columns + 1 < columns && allowed.Contains(cell + 1)) Union(cell, cell + 1);
            if (cell < count - columns && allowed.Contains(cell + columns)) Union(cell, cell + columns);
        }
        var ports = new int[participants][];
        var cellParticipants = new Dictionary<int, List<int>>();
        for (int k = 0; k < participants; k++)
        {
            var list = new List<int>();
            foreach (int cell in participantPortCells![k])
                if (allowed.Contains(cell) && !list.Contains(cell))
                {
                    list.Add(cell); Union(count + k, cell);
                    if (!cellParticipants.TryGetValue(cell, out var at)) cellParticipants[cell] = at = new List<int>();
                    at.Add(k);
                }
            ports[k] = list.ToArray();
        }
        var joinLists = new List<int>[participants];
        for (int k = 0; k < participants; k++) joinLists[k] = new List<int>();
        if (joins != null)
            foreach (var (a, b) in joins)
            {
                if (a < 0 || b < 0 || a >= participants || b >= participants || a == b || joinLists[a].Contains(b)) continue;
                joinLists[a].Add(b); joinLists[b].Add(a); Union(count + a, count + b);
            }

        // Compact component ids in first-seen order.
        var ids = new Dictionary<int, int>();
        int Id(int node) { int root = Find(node); if (!ids.TryGetValue(root, out int id)) ids[root] = id = ids.Count; return id; }
        var component = new Dictionary<int, int>();
        foreach (int cell in allowed) component[cell] = Id(cell);
        var participantComponent = new int[participants];
        for (int k = 0; k < participants; k++) participantComponent[k] = Id(count + k);
        var joined = new int[participants][];
        for (int k = 0; k < participants; k++) joined[k] = joinLists[k].ToArray();
        return new FluidTopology(columns, rows, allowed, component, segmentCount > visitLimit, participantComponent, ports, joined, cellParticipants);
    }
    public int AllowedCount => allowed.Count;
    public int ParticipantCount => participantComponent.Length;
    public int ComponentCount
    {
        get { var seen = new HashSet<int>(component.Values); foreach (int c in participantComponent) seen.Add(c); return seen.Count; }
    }
    public bool Allowed(int cell) => allowed.Contains(cell);
    /// <summary>The component a cell belongs to, or -1 when it carries no fluid.</summary>
    public int ComponentOf(int cell) => component.TryGetValue(cell, out int id) ? id : -1;
    /// <summary>The component a participant belongs to (its own when it touches nothing), or -1 when out of range.</summary>
    public int ParticipantComponentOf(int participant) => participant >= 0 && participant < participantComponent.Length ? participantComponent[participant] : -1;
    /// <summary>Whether two cells share one connected run of segments (a second source on the same run would
    /// share the circuit).</summary>
    public bool Connected(int a, int b) => !Overflow && a >= 0 && b >= 0 && component.TryGetValue(a, out int x) && component.TryGetValue(b, out int y) && x == y;
    /// <summary>Whether two participants share one network: through segments at their ports, through touching
    /// participants, or both.</summary>
    public bool ParticipantsConnected(int a, int b) => !Overflow && a != b && ParticipantComponentOf(a) >= 0 && ParticipantComponentOf(a) == ParticipantComponentOf(b);
    /// <summary>Whether one connected run of segments has a cell on or beside (north, south, east or west of) one of the
    /// <paramref name="starts"/> and a cell on or beside one of the <paramref name="goals"/> (Framework 0.61.0): how a
    /// conveyor belt joins two pieces of equipment by their own tiles. False on an overflowing layout.</summary>
    public bool JoinsNear(IEnumerable<int> starts, IEnumerable<int> goals)
    {
        if (Overflow) return false;
        var near = new HashSet<int>(ComponentsNear(starts));
        if (near.Count == 0) return false;
        foreach (int id in ComponentsNear(goals)) if (near.Contains(id)) return true;
        return false;
    }
    private IEnumerable<int> ComponentsNear(IEnumerable<int> cells)
    {
        int count = Columns * Rows;
        foreach (int cell in cells)
        {
            if (cell < 0 || cell >= count) continue;
            int column = cell % Columns;
            foreach (int c in new[] { cell, column > 0 ? cell - 1 : -1, column + 1 < Columns ? cell + 1 : -1, cell - Columns, cell + Columns })
                if (c >= 0 && c < count && component.TryGetValue(c, out int id)) yield return id;
        }
    }
    /// <summary>Every participant on the network that runs through a segment cell (Framework 0.60.0), in index order;
    /// none when the cell carries no fluid or the snapshot overflowed.</summary>
    public IEnumerable<int> ParticipantsOn(int cell)
    {
        int id = Overflow ? -1 : ComponentOf(cell);
        if (id < 0) yield break;
        for (int k = 0; k < participantComponent.Length; k++) if (participantComponent[k] == id) yield return k;
    }
    /// <summary>The bounded cardinal route between two cells, or null; identical to the former per-call search.</summary>
    public int[]? Path(int startCell, int goalCell, int visitLimit = GridRoute.DefaultVisitLimit)
    {
        if (!Connected(startCell, goalCell)) return null;
        start[0] = startCell; goal.Clear(); goal.Add(goalCell);
        return GridRoute.Find(Columns, Rows, start, goal, Allowed, visitLimit);
    }
    /// <summary>Steps between two participants over the network (segment cells and touching participants each count
    /// one), or -1 when they share no network. One search per source participant per snapshot, remembered.</summary>
    public int Hops(int from, int to)
    {
        if (!ParticipantsConnected(from, to)) return -1;
        if (!hopMemo.TryGetValue(from, out var distances)) hopMemo[from] = distances = Distances(from);
        return distances[to];
    }
    private int[] Distances(int from)
    {
        int count = Columns * Rows, participants = participantComponent.Length;
        var result = new int[participants];
        for (int k = 0; k < participants; k++) result[k] = -1;
        var cellDistance = new Dictionary<int, int>();
        var queue = new Queue<(bool Cell, int Node, int Distance)>();
        result[from] = 0; queue.Enqueue((false, from, 0));
        while (queue.Count > 0)
        {
            var (isCell, node, distance) = queue.Dequeue();
            void Cell(int c) { if (allowed.Contains(c) && !cellDistance.ContainsKey(c)) { cellDistance[c] = distance + 1; queue.Enqueue((true, c, distance + 1)); } }
            void Participant(int p) { if (result[p] < 0) { result[p] = distance + 1; queue.Enqueue((false, p, distance + 1)); } }
            if (isCell)
            {
                if (node % Columns > 0) Cell(node - 1);
                if (node % Columns + 1 < Columns) Cell(node + 1);
                if (node >= Columns) Cell(node - Columns);
                if (node < count - Columns) Cell(node + Columns);
                if (cellParticipants.TryGetValue(node, out var at)) foreach (int p in at) Participant(p);
            }
            else
            {
                foreach (int c in participantPorts[node]) Cell(c);
                foreach (int p in participantJoins[node]) Participant(p);
            }
        }
        return result;
    }
}
