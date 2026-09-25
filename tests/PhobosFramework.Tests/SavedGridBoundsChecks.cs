using System;
using Phobos.Ostranauts.Framework.Persistence;

internal static class SavedGridBoundsChecks
{
    internal static void Run(Action<bool, string> check)
    {
        check(SavedGridBounds.TryPlan(62, 44, -32, -14, 63, 44, -32, -14, 4, out var p) &&
            p.Left == 0 && p.Right == 1 && p.Top == 0 && p.Bottom == 0,
            "G4 pending placement restores the missing column from the reported save");
        // Native flat indices from four distinct compartments in the report.
        foreach (var pair in new[] { (196, 193), (1157, 1139), (1410, 1388), (2297, 2261) })
        {
            int row = pair.Item1 / 63, col = pair.Item1 % 63;
            check(row * 62 + col == pair.Item2, "Uncorrected stride reproduces a reported wrong room lookup");
            check(row * (62 + p.Left + p.Right) + col == pair.Item1,
                "Padding preserves the compartment's saved lookup");
        }
        // Exercise each edge, combinations and every tile, including origin shifts.
        for (int left = 0; left <= 2; left++)
        for (int right = 0; right <= 2; right++)
        for (int top = 0; top <= 2; top++)
        for (int bottom = 0; bottom <= 2; bottom++)
        {
            int cols = 10 - left - right, rows = 10 - top - bottom;
            check(SavedGridBounds.TryPlan(cols, rows, -8 + left, 3 - top, 10, 10, -8, 3, 2, out p),
                "Contained grid can expand on any edge");
            for (int i = 0; i < cols * rows; i++)
            {
                int x = -8 + left + i % cols, y = 3 - top - i / cols;
                int padded = (i / cols + p.Top) * (cols + p.Left + p.Right) + i % cols + p.Left;
                check(padded == (3 - y) * 10 + x + 8, "Existing tiles keep their world positions and saved indices");
            }
        }
        check(SavedGridBounds.TryPlan(63, 44, -32, -14, 63, 44, -32, -14, 4, out p) && !p.Required,
            "Already matching grids are an idempotent no-op");
        check(!SavedGridBounds.TryPlan(64, 44, -32, -14, 63, 44, -32, -14, 4, out p), "Never shrink loaded geometry");
        check(!SavedGridBounds.TryPlan(62, 44, -33, -14, 63, 44, -32, -14, 4, out p), "Reject geometry outside saved bounds");
        check(!SavedGridBounds.TryPlan(62, 44, -31.5, -14, 63, 44, -32, -14, 4, out p), "Reject fractional tile shifts");
        check(!SavedGridBounds.TryPlan(62, 44, double.NaN, -14, 63, 44, -32, -14, 4, out p), "Reject invalid origins");
        check(!SavedGridBounds.TryPlan(62, 44, -32, -14, 5000, 44, -32, -14, 4, out p), "Bound growth by relevant equipment footprint");
        check(!SavedGridBounds.TryPlan(0, 0, 0, 0, 63, 44, -32, -14, 4, out p), "Do not invent an empty ship");
        check(!SavedGridBounds.TryPlan(1, 1, 0, 0, int.MaxValue, 2, 0, 0, int.MaxValue, out p), "Reject overflowing tile counts");
    }
}
