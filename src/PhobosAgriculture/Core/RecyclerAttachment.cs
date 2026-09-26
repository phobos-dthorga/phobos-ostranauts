using System;

namespace PhobosAgriculture.Core;

public static class RecyclerAttachment
{
    public const double CenterSpacingTiles = 1.5, PositionToleranceTiles = .06, AngleToleranceDegrees = .1;

    // The 2 x 1 collector's pocket (+Y) touches a complete edge of the 2 x 2 Recycler.
    // Its service face (-Y) remains on the opposite side, clear of the Recycler.
    public static bool Aligned(double rx, double ry, double ra, double cx, double cy, double ca)
    {
        if (Math.Abs(Math.IEEERemainder(ra, 90)) >= AngleToleranceDegrees ||
            Math.Abs(Math.IEEERemainder(ca, 90)) >= AngleToleranceDegrees) return false;
        double radians = ca * Math.PI / 180;
        double dx = rx - cx + CenterSpacingTiles * Math.Sin(radians);
        double dy = ry - cy - CenterSpacingTiles * Math.Cos(radians);
        return Math.Abs(dx) < PositionToleranceTiles && Math.Abs(dy) < PositionToleranceTiles;
    }
}
