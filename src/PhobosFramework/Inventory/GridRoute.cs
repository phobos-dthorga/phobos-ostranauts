using System;
using System.Collections.Generic;

namespace Phobos.Ostranauts.Framework.Inventory;

/// <summary>Bounded cardinal route on a rectangular grid. Caller owns topology and revalidation.</summary>
public static class GridRoute
{
    public static int[]? Find(int columns, int rows, IEnumerable<int> starts, ISet<int> goals,
        Func<int, bool> allowed, int visitLimit = 4096)
    {
        if (columns <= 0 || rows <= 0 || (long)columns * rows > int.MaxValue || visitLimit < 1)
            throw new ArgumentException("Invalid route bounds.");
        int count = columns * rows;
        var parents = new Dictionary<int, int>();
        var queue = new Queue<int>();
        bool Valid(int index) => index >= 0 && index < count && allowed(index);
        foreach (int start in starts)
        {
            if (!Valid(start) || parents.ContainsKey(start)) continue;
            if (parents.Count >= visitLimit) break;
            parents.Add(start, -1); queue.Enqueue(start);
        }
        while (queue.Count > 0)
        {
            int cell = queue.Dequeue();
            if (goals.Contains(cell))
            {
                var path = new List<int>();
                for (int n = cell; n != -1; n = parents[n]) path.Add(n);
                path.Reverse(); return path.ToArray();
            }
            void Visit(int next)
            {
                if (parents.Count >= visitLimit || parents.ContainsKey(next) || !Valid(next)) return;
                parents.Add(next, cell); queue.Enqueue(next);
            }
            if (cell % columns > 0) Visit(cell - 1);
            if (cell % columns + 1 < columns) Visit(cell + 1);
            if (cell >= columns) Visit(cell - columns);
            if (cell < count - columns) Visit(cell + columns);
        }
        return null;
    }
}
