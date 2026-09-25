using System;

namespace Phobos.Ostranauts.Framework.Persistence;

/// <summary>Plans expansion back to a serialized tile grid before native room/zone loading.
/// Does not shrink a grid, move objects, remap saved records or grant floor tiles.</summary>
public readonly struct SavedGridBounds
{
    private const double TileTolerance = 0.0001;
    public readonly int Left, Right, Top, Bottom;
    public bool Required => Left != 0 || Right != 0 || Top != 0 || Bottom != 0;
    private SavedGridBounds(int left, int right, int top, int bottom)
    { Left = left; Right = right; Top = top; Bottom = bottom; }

    public static bool TryPlan(int columns, int rows, double x, double y,
        int savedColumns, int savedRows, double savedX, double savedY,
        int maximumPaddingPerEdge, out SavedGridBounds padding)
    {
        padding = default;
        if (columns <= 0 || rows <= 0 || savedColumns <= 0 || savedRows <= 0 ||
            (long)savedColumns * savedRows > int.MaxValue || maximumPaddingPerEdge < 0)
            return false;
        double left = x - savedX, top = savedY - y;
        double right = savedColumns - (double)columns - left;
        double bottom = savedRows - (double)rows - top;
        if (!Valid(left, maximumPaddingPerEdge) || !Valid(right, maximumPaddingPerEdge) ||
            !Valid(top, maximumPaddingPerEdge) || !Valid(bottom, maximumPaddingPerEdge)) return false;
        padding = new SavedGridBounds((int)Math.Round(left), (int)Math.Round(right),
            (int)Math.Round(top), (int)Math.Round(bottom));
        return true;
    }

    private static bool Valid(double value, int maximum) => !double.IsNaN(value) &&
        !double.IsInfinity(value) && value >= 0 && value <= maximum &&
        Math.Abs(value - Math.Round(value)) <= TileTolerance;
}
