using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

internal static class SavedRoomGridChecks
{
    internal static SavedRoomShape[] Rooms(int width, int height)
    {
        int a = 2 * width + 2, b = (height - 3) * width + width - 3;
        return new[] {
            new SavedRoomShape("outside", true, Enumerable.Range(0, width * height).Where(t => t != a && t != b).ToArray(), -10, 20),
            new SavedRoomShape("first", false, new[] { a }, -8, 18),
            new SavedRoomShape("second", false, new[] { b }, -10 + width - 3, 20 - (height - 3))
        };
    }

    internal static void Run(Action<bool, string> check)
    {
        bool Resolve(SavedRoomShape[] rooms, int width = 9, int height = 7, int[]? zones = null) =>
            SavedRoomGrid.TryResolve(width, height, -10, 20, rooms, zones ?? Array.Empty<int>(), out _, out _);
        var original = Rooms(8, 6);
        var before = original.Select(r => string.Join(",", r.Tiles)).ToArray();
        foreach (var header in new[] { (8, 6), (9, 6), (8, 7), (9, 7), (12, 12) })
            check(SavedRoomGrid.TryResolve(header.Item1, header.Item2, -10, 20, original, new[] { 18, 19 }, out int w, out int h) && w == 8 && h == 6,
                "Consistent room/zone geometry resolves through column, row or combined trimming");
        check(original.Select((r, i) => string.Join(",", r.Tiles) == before[i]).All(x => x), "Resolving dimensions never rewrites room indices");
        check(!Resolve(original, 7, 6), "Never expand undersized headers or truncate tile data");
        check(!Resolve(original, int.MaxValue, 6), "Overflowing headers are rejected");
        check(!Resolve(original, zones: new[] { 48 }), "Out-of-bounds zone tiles block recovery");
        check(!Resolve(original, zones: new[] { -1 }), "Negative zone tiles block recovery");
        var altered = Rooms(8, 6);
        altered[1] = new SavedRoomShape("first", false, new[] { 18, 18 }, -8, 18);
        check(Resolve(altered), "Native repeated tiles within the same room retain their meaning");
        altered = Rooms(8, 6); altered[2] = new SavedRoomShape("second", false, new[] { 18, 29 }, -5, 17);
        check(!Resolve(altered), "Tiles assigned to conflicting room IDs are rejected");
        altered = Rooms(8, 6); altered[2] = altered[1];
        check(!Resolve(altered), "Duplicate room IDs and overlapping rooms are rejected");
        altered = Rooms(8, 6); altered[0] = new SavedRoomShape("outside", false, altered[0].Tiles, -10, 20);
        check(!Resolve(altered), "A real recorded exterior is required");
        altered = Rooms(8, 6); altered[0] = new SavedRoomShape("outside", true, altered[0].Tiles.Where(t => t != 7).ToArray(), -10, 20);
        check(!Resolve(altered), "Incomplete exterior border is not guessed");
        foreach (double x in new[] { double.NaN, double.PositiveInfinity, -8.5, -9 })
        {
            altered = Rooms(8, 6); altered[1] = new SavedRoomShape("first", false, new[] { 18 }, x, 18);
            check(!Resolve(altered), "Nonfinite, fractional or mismatched room world positions are rejected");
        }
        var ambiguous = new[] {
            new SavedRoomShape("outside", true, Enumerable.Range(0, 60).Where(t => t != 14 && t != 22).ToArray(), -10, 20),
            new SavedRoomShape("inside", false, new[] { 14, 22 }, -8, 18)
        };
        check(!Resolve(ambiguous, 10, 10), "Two equally plausible grids are rejected instead of choosing one");
        // The exact failing native lookups from autosave_293_pg17: a stale
        // 64-wide header reinterprets intact room indices stored at width 63.
        foreach (var pair in new[] { (1111, 1128), (1120, 1137), (1410, 1432), (2297, 2333) })
        {
            int row = pair.Item1 / 63, col = pair.Item1 % 63;
            check(row * 64 + col == pair.Item2, "Stale width reproduces an observed duplicate-room lookup");
            check(row * 63 + col == pair.Item1, "Corrected width preserves the saved room lookup");
        }
    }
}
