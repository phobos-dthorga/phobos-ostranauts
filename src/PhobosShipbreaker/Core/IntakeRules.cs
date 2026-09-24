using System;

namespace PhobosShipbreaker.Core;

public static class IntakeRules
{
    public const string Chute = "PhobosHullChute", Grabber = "PhobosExteriorGrabber";
    public const string Working = "PhobosShipbreakerIntakeWorking";
    public const double ChuteKg = 40, GrabberKg = 80, TransferSeconds = 5, WorkingKW = 2, IdleKW = 0.05;
    public const int Width = 4, ChuteDepth = 1, GrabberDepth = 3;
    public const double PositionToleranceTiles = 0.06, AngleToleranceDegrees = 0.1;
    public static bool IsHardware(string? id) => id == Chute + "Installed" || id == Chute + "Loose" ||
        id == Chute + "InstalledDmg" || id == Chute + "LooseDmg" ||
        id == Grabber + "Installed" || id == Grabber + "Loose" ||
        id == Grabber + "InstalledDmg" || id == Grabber + "LooseDmg";

    // Broad 2D bound; render depth must not affect physical adjacency.
    public static bool CouldReachProcessor(double gx, double gy, double px, double py)
    {
        double radius = GrabberDepth / 2.0 + ChuteDepth + ProcessRules.Footprint / 2.0 + 2 * PositionToleranceTiles;
        double dx = gx - px, dy = gy - py;
        return dx * dx + dy * dy <= radius * radius;
    }

    // Positive angle follows Unity's counter-clockwise Z rotation. One world unit
    // is one native tile. An outward-facing grabber has its arms at local +Y.
    public static (double X, double Y) Rotate(double x, double y, double degrees)
    {
        double radians = degrees * Math.PI / 180;
        return (x * Math.Cos(radians) - y * Math.Sin(radians), x * Math.Sin(radians) + y * Math.Cos(radians));
    }
    public static bool Near(double x, double y, double expectedX, double expectedY) =>
        Math.Abs(x - expectedX) < PositionToleranceTiles && Math.Abs(y - expectedY) < PositionToleranceTiles;
    public static bool SameAngle(double a, double b, double period = 360) =>
        Math.Abs(Math.IEEERemainder(a - b, period)) < AngleToleranceDegrees;
    public static bool Connected(double gx, double gy, double ga, double cx, double cy, double ca,
        double px, double py, double pa)
    {
        var chute = Rotate(0, -(GrabberDepth + ChuteDepth) / 2.0, ga);
        var processor = Rotate(0, -(GrabberDepth / 2.0 + ChuteDepth + ProcessRules.Footprint / 2.0), ga);
        return SameAngle(ga, ca, 180) && SameAngle(ga + 180, pa) &&
            Near(cx, cy, gx + chute.X, gy + chute.Y) && Near(px, py, gx + processor.X, gy + processor.Y);
    }
}
