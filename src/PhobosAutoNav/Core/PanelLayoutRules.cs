using System;

namespace PhobosAutoNav.Core;

/// <summary>One native navigation column, four standard rows. New layout identity only.</summary>
internal static class PanelLayoutRules
{
    public const float RowHeight = 0.8f;
    public const float ColumnWidth = 0.25f;
    public const float DefaultLeft = 0f;
    public const float DefaultTop = 0.8f;

    // Match the native column grid, independently of the source bitmap's
    // aspect ratio. Both dimensions lie on the native two-decimal anchor grid.
    // Keep the top-left placement; never silently move other panels or force a fit.
    public static bool TryBounds(double boardWidth, double boardHeight, float left, float top,
        out (float Left, float Bottom, float Right, float Top) bounds)
    {
        bounds = default;
        if (!Finite(left) || !Finite(top)) return false;
        if (!Finite(boardWidth) || !Finite(boardHeight) || boardWidth <= 0 || boardHeight <= 0) return false;
        bounds = (left, top - RowHeight, left + ColumnWidth, top);
        return true;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
