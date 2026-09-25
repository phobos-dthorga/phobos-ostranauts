using System;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>One native consumption call, including partial external supply and local storage.
/// Receipts are session objects, never saved permissions or a second power-source budget.</summary>
public sealed class EnergyReceipt
{
    public double RequestedKWh { get; }
    private readonly double before;
    private double gathered;
    private bool consumed;
    public EnergyReceipt(double requestedKWh, double storedBeforeKWh)
    {
        Require(requestedKWh); Require(storedBeforeKWh);
        RequestedKWh = requestedKWh; before = storedBeforeKWh;
    }
    public void Gather(double requestedKWh, double remainingKWh)
    {
        if (consumed) throw new InvalidOperationException("Energy receipt already consumed.");
        Require(requestedKWh); Require(remainingKWh);
        if (remainingKWh > requestedKWh) throw new ArgumentException("Remaining demand exceeds request.");
        gathered += requestedKWh - remainingKWh;
    }
    public double Consume(double storedAfterKWh)
    {
        if (consumed) throw new InvalidOperationException("Energy receipt already consumed.");
        Require(storedAfterKWh); consumed = true;
        double used = gathered + before - storedAfterKWh;
        if (used < -1e-9 || double.IsInfinity(used)) throw new InvalidOperationException("Invalid consumed electrical energy.");
        // Do not clamp a real native debit to the request: excess consumption is still heat.
        return Math.Max(0, used);
    }
    private static void Require(double x)
    { if (double.IsNaN(x) || double.IsInfinity(x) || x < 0) throw new ArgumentOutOfRangeException(nameof(x)); }
}
