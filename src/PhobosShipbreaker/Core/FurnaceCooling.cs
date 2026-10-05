using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Two physical installations, one finite cooling budget and unchanged saved sink format.</summary>
public static class FurnaceCooling
{
    public enum Socket { None, Left, Right, Rear }
    public const double SideX = 3.5, SideY = .5;
    public const double PumpKW = 1;
    public const int RouteLimit = 64;
    public const string Conduit = "PhobosFurnaceCoolantConduit";
    /// <summary>The fittings of the first conduit rule (until Shipbreaker 0.80.0): the furnace's side fittings and the
    /// radiator's one service point. Kept for the joints drawn there and for checks that old layouts still join.</summary>
    public static (double X, double Y) CoolantOffset(bool furnace, string mode) => furnace
        ? mode == "left" ? (-SideX, SideY) : mode == "right" ? (SideX, SideY) : throw new ArgumentException("Unknown coolant fitting.")
        : (.5, -3.5);
    public const string Direct = "direct", Piped = "piped";
    /// <summary>Reads a saved cooling mode. Since Shipbreaker 0.80.0 there is one piped mode: conduit under or right
    /// beside the furnace joins it on any side, so a record saved as the left or right fitting reads as piped.</summary>
    public static bool TryReadMode(IReadOnlyDictionary<string, string> fields, out string mode)
    {
        mode = Direct;
        if (fields.Count != 1 || !fields.TryGetValue("mode", out var value) || (value != Direct && value != Piped && value != "left" && value != "right")) return false;
        mode = value == Direct ? Direct : Piped; return true;
    }
    /// <summary>Where conduit joins an exterior radiator (Shipbreaker 0.80.0), in its own tile coordinates: any tile of
    /// its mounting wall row, where conduit may run inside the wall, or of the row one tile further inside the ship. The
    /// old single service point is one of them.</summary>
    public static IEnumerable<(double X, double Y)> RadiatorJoinOffsets()
    {
        for (int x = 0; x < FurnaceRules.Footprint; x++)
        {
            yield return (x - (FurnaceRules.Footprint - 1) / 2.0, -2.5);
            yield return (x - (FurnaceRules.Footprint - 1) / 2.0, -3.5);
        }
    }
    /// <summary>Whether a point in the furnace's own tile coordinates lies under it or right beside it (corners excluded).</summary>
    public static bool OnOrBesideFurnace(double x, double y)
    {
        double half = FurnaceRules.Footprint / 2.0, ax = Math.Abs(x), ay = Math.Abs(y);
        return ax < half && ay < half || ax < half && ay < half + 1 || ay < half && ax < half + 1;
    }
    public static (double X, double Y) Offset(Socket socket) => socket switch
    {
        Socket.Left => (-SideX, SideY), Socket.Right => (SideX, SideY),
        Socket.Rear => (0, FurnaceRules.PairSpacingTiles), _ => throw new ArgumentOutOfRangeException(nameof(socket))
    };
    public static Socket AtSocket(bool underside, double fx, double fy, double fa, double cx, double cy)
    {
        foreach (var socket in underside ? new[] { Socket.Left, Socket.Right } : new[] { Socket.Rear })
        {
            var offset = Offset(socket); var p = IntakeRules.Rotate(offset.X, offset.Y, fa);
            if (IntakeRules.Near(cx, cy, fx + p.X, fy + p.Y)) return socket;
        }
        return Socket.None;
    }
    public static bool Aligned(bool underside, double fx, double fy, double fa, double cx, double cy, double ca)
    {
        return IntakeRules.SameAngle(fa, ca) && AtSocket(underside, fx, fy, fa, cx, cy) != Socket.None;
    }
    public static bool FloorSupport(bool floor, bool sealedFloor, bool wall, bool eva, bool intactInstalledFloor) =>
        floor && sealedFloor && !wall && !eva && intactInstalledFloor;
    public static bool CanChange(bool protectedState, bool idle, double temperature, int captiveItems, int outputItems) =>
        !protectedState && idle && ThermalMath.Finite(temperature) && temperature <= FurnaceRules.ReleaseK && captiveItems == 0 && outputItems == 0;
    public static bool TryRead(IReadOnlyDictionary<string, string> fields, out double sinkKJ)
    {
        sinkKJ = 0;
        return fields.Count == 1 && fields.TryGetValue("sink", out var raw) &&
            double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out sinkKJ) && ThermalMath.Finite(sinkKJ) &&
            sinkKJ > -FurnaceRules.ReferenceK * FurnaceRules.SinkCapacity && sinkKJ <= 1000000;
    }
    public static Dictionary<string, string> Save(double sinkKJ) => new() { ["sink"] = sinkKJ.ToString("R", CultureInfo.InvariantCulture) };
    // The caller bounds elapsed time, and checks support separately from instrumentation.
    public static double Radiate(ref double sinkKJ, double seconds, bool available, bool damaged)
    {
        if (!ThermalMath.Finite(seconds) || seconds < 0 || seconds > FurnaceRules.MaxIntervalSeconds) throw new ArgumentOutOfRangeException(nameof(seconds));
        double before = sinkKJ;
        if (available)
            while (seconds > 1e-9)
            {
                double step = Math.Min(seconds, FurnaceRules.MaxStepSeconds); seconds -= step;
                sinkKJ -= FurnaceRules.Radiation(FurnaceRules.ReferenceK + sinkKJ / FurnaceRules.SinkCapacity) * step *
                    (damaged ? FurnaceRules.DamagedRadiatorFraction : 1);
            }
        return before - sinkKJ;
    }
}
