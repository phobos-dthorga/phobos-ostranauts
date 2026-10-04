using System;

namespace Phobos.Ostranauts.Framework.Observations;

/// <summary>Which installed pieces of equipment touch (Framework 0.86.0, first used by Phobos Medical's Vigil-2
/// monitor and Ward-3 bed). The shared link rule is that footprints touch or stand one tile apart without overlapping;
/// <c>BulkVessels.WithinOneTile</c> applies it to squares. This applies it to rectangles turned on the deck: a quarter
/// turn swaps an item's width and depth. Shipbreaker's mining laser has its own copy of the same rule
/// (<c>LaserRules.Touching</c>), which can move here when Shipbreaker next changes.</summary>
public static class Footprints
{
    /// <summary>Two footprints, as centres and sizes in tiles, that touch or stand one tile apart and do not overlap.</summary>
    public static bool Touching(double ax, double ay, double aw, double ah, double bx, double by, double bw, double bh)
    {
        foreach (double v in new[] { ax, ay, aw, ah, bx, by, bw, bh }) if (double.IsNaN(v) || double.IsInfinity(v)) return false;
        if (aw <= 0 || ah <= 0 || bw <= 0 || bh <= 0) return false;
        double gapX = Math.Abs(ax - bx) - (aw + bw) / 2, gapY = Math.Abs(ay - by) - (ah + bh) / 2;
        const double Epsilon = 1e-6;
        // Overlapping on both axes is one object on top of another, not touching.
        if (gapX < -Epsilon && gapY < -Epsilon) return false;
        return Math.Max(gapX, gapY) <= 1 + Epsilon;
    }

    /// <summary>Width and depth on the deck in tiles, given the item's own size and its rotation in degrees.</summary>
    public static (double Width, double Depth) OnDeck(double width, double depth, double rotationDegrees)
    {
        bool turned = Math.Abs(Math.IEEERemainder(rotationDegrees, 180)) > 45;
        return turned ? (depth, width) : (width, depth);
    }

    /// <summary>Whether two installed objects on the same ship touch, from their items' sizes and rotations.</summary>
    public static bool Touching(CondOwner? a, CondOwner? b)
    {
        if (a == null || b == null || a == b || a.bDestroyed || b.bDestroyed || a.ship == null || a.ship != b.ship || a.Item == null || b.Item == null) return false;
        var pa = a.GetPos(); var pb = b.GetPos();
        var da = OnDeck(a.Item.nWidthInTiles, a.Item.nHeightInTiles, a.tf.eulerAngles.z);
        var db = OnDeck(b.Item.nWidthInTiles, b.Item.nHeightInTiles, b.tf.eulerAngles.z);
        return Touching(pa.x, pa.y, da.Width, da.Depth, pb.x, pb.y, db.Width, db.Depth);
    }
}
