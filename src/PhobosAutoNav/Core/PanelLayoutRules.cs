using System;

namespace PhobosAutoNav.Core;

/// <summary>One standard native navigation column and row.</summary>
internal static class PanelLayoutRules
{
    public const float RowHeight = 0.2f;
    public const float ColumnWidth = 0.25f;
    public const float DefaultLeft = 0.35f;
    public const float DefaultTop = 0.25f;

    // Match Time/Zoom and Display Controls, independently of the source bitmap's
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
