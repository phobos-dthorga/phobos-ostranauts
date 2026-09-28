using System;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>One synchronous pump interval. Consumed electricity is never banked for later transfers.
/// The interval may be long (a time-skip or a reload gap): the received electricity bounds the
/// delivery, as it does for the game's own machines catching up.</summary>
public static class LiquidDeliveryBudget
{
    public static double Kilograms(double elapsedSeconds, double receivedKWh, double kgPerSecond, double kWhPerKg)
    {
        if (!Positive(elapsedSeconds) || !Positive(receivedKWh) || !Positive(kgPerSecond) || !Positive(kWhPerKg)) return 0;
        return Math.Min(elapsedSeconds * kgPerSecond, receivedKWh / kWhPerKg);
    }
    private static bool Positive(double value) => value > 0 && !double.IsNaN(value) && !double.IsInfinity(value);
}
