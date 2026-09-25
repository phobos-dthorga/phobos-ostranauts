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
