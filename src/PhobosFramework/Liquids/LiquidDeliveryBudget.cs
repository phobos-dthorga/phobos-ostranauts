using System;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One synchronous pump interval. Consumed electricity is never banked for later transfers.</summary>
public static class LiquidDeliveryBudget
{
    public static double Kilograms(double elapsedSeconds, double receivedKWh, double kgPerSecond, double kWhPerKg)
    {
        if (!Positive(elapsedSeconds) || elapsedSeconds > 3600 || !Positive(receivedKWh) ||
            !Positive(kgPerSecond) || !Positive(kWhPerKg)) return 0;
        return Math.Min(elapsedSeconds * kgPerSecond, receivedKWh / kWhPerKg);
    }
    private static bool Positive(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
}
