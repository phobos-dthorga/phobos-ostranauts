using System;

namespace PhobosAutoNav.Core;

/// <summary>Native navigation row height with the approved faceplate's proportions.</summary>
internal static class PanelLayoutRules
{
    public const float RowHeight = 0.2f;
    public const float FaceplateAspect = 2f;
    public const float DefaultLeft = 0.35f;
    public const float DefaultTop = 0.25f;

    // Native layout saves round anchors to two decimal places. Recompute the
    // physical width after loading them rather than stretching the artwork.
    // Keep the top-left placement; never silently move other panels or force a fit.
    public static bool TryBounds(double boardWidth, double boardHeight, float left, float top,
        out (float Left, float Bottom, float Right, float Top) bounds)
    {
        bounds = default;
        if (!Finite(left) || !Finite(top)) return false;
        if (!Finite(boardWidth) || !Finite(boardHeight) || boardWidth <= 0 || boardHeight <= 0) return false;
        double proposedWidth = RowHeight * FaceplateAspect * boardHeight / boardWidth;
        if (!Finite(proposedWidth) || proposedWidth <= 0 || proposedWidth > 1) return false;
        float width = (float)proposedWidth;
        if (width <= 0) return false;
        bounds = (left, top - RowHeight, left + width, top);
        return true;
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
