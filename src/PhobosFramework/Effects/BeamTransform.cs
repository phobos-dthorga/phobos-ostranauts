using System;

namespace Phobos.Ostranauts.Framework.Effects;

/// <summary>Where a beam quad sits under the item it is drawn from. The game scales an item's transform by its size
/// in tiles, so a child's local unit is that many tiles; the emitter is given in the game's own light-point
/// convention (pixels from the item centre, sixteen to a tile, +Y up). A rotated child only stays unsheared under a
/// uniform parent scale, so a non-square item is refused.</summary>
public static class BeamTransform
{
    public const double PixelsPerTile = 16;

    public readonly struct Pose
    {
        public readonly double X, Y, AngleDegrees, ScaleX, ScaleY, LengthTiles;
        public Pose(double x, double y, double angleDegrees, double scaleX, double scaleY, double lengthTiles)
        { X = x; Y = y; AngleDegrees = angleDegrees; ScaleX = scaleX; ScaleY = scaleY; LengthTiles = lengthTiles; }
    }

    /// <summary>The local position, Z rotation and scale of a unit quad (its length along local X) stretched from
    /// the emitter to the target, both given in the parent's local space.</summary>
    public static bool Solve(double emitterPixelsX, double emitterPixelsY, double parentScaleX, double parentScaleY,
        double targetLocalX, double targetLocalY, double thicknessTiles, out Pose pose)
    {
        pose = default;
        foreach (double value in new[] { emitterPixelsX, emitterPixelsY, parentScaleX, parentScaleY, targetLocalX, targetLocalY, thicknessTiles })
            if (double.IsNaN(value) || double.IsInfinity(value)) return false;
        if (parentScaleX <= 0 || thicknessTiles <= 0 || Math.Abs(parentScaleX - parentScaleY) > 1e-4) return false;
        double emitterX = emitterPixelsX / PixelsPerTile / parentScaleX, emitterY = emitterPixelsY / PixelsPerTile / parentScaleY;
        double dx = targetLocalX - emitterX, dy = targetLocalY - emitterY;
        double lengthTiles = Math.Sqrt(dx * dx + dy * dy) * parentScaleX;
        if (lengthTiles < 1e-4) return false;
        pose = new Pose((emitterX + targetLocalX) / 2, (emitterY + targetLocalY) / 2, Math.Atan2(dy, dx) * 180 / Math.PI,
            lengthTiles / parentScaleX, thicknessTiles / parentScaleY, lengthTiles);
        return true;
    }
}
