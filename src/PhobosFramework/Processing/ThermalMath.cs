using System;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Shared unit-explicit thermal arithmetic. Content owns all material and equipment parameters.</summary>
public static class ThermalMath
{
    public const double StefanBoltzmannKW = 5.670374419e-11;
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    public static double RadiationKW(double kelvin, double backgroundKelvin, double areaM2, double emissivity)
    {
        if (!Finite(kelvin) || !Finite(backgroundKelvin) || !Finite(areaM2) || !Finite(emissivity) ||
            kelvin < 0 || backgroundKelvin < 0 || areaM2 < 0 || emissivity < 0 || emissivity > 1)
            throw new ArgumentOutOfRangeException(nameof(kelvin));
        return StefanBoltzmannKW * areaM2 * emissivity * (Math.Pow(kelvin, 4) - Math.Pow(backgroundKelvin, 4));
    }
    public static double SensibleKJ(double capacityKJPerK, double kelvin, double referenceKelvin) => capacityKJPerK * (kelvin - referenceKelvin);
}
