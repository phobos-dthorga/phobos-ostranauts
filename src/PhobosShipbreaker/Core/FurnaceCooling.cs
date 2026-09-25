using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Processing;

namespace PhobosShipbreaker.Core;

/// <summary>Two physical installations, one finite cooling budget and unchanged saved sink format.</summary>
public static class FurnaceCooling
{
    public const double SideX = 3.5, SideY = .5;
    public static bool Aligned(bool underside, double fx, double fy, double fa, double cx, double cy, double ca)
    {
        if (!IntakeRules.SameAngle(fa, ca)) return false;
        if (!underside) return At(0, FurnaceRules.PairSpacingTiles);
        return At(-SideX, SideY) || At(SideX, SideY);
        bool At(double x, double y)
        {
            var p = IntakeRules.Rotate(x, y, fa);
            return IntakeRules.Near(cx, cy, fx + p.X, fy + p.Y);
        }
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
