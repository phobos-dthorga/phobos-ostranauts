using System;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Two independently conserved masses. Content owns chemistry, profile and density.</summary>
public readonly struct LiquidMixture
{
    public readonly double CarrierKg, SoluteKg;
    public double TotalKg => CarrierKg + SoluteKg;
    public LiquidMixture(double carrier, double solute) { CarrierKg = carrier; SoluteKg = solute; }
    public static LiquidMixture operator +(LiquidMixture a, LiquidMixture b) => new(a.CarrierKg + b.CarrierKg, a.SoluteKg + b.SoluteKg);
    public static LiquidMixture operator -(LiquidMixture a, LiquidMixture b) => new(a.CarrierKg - b.CarrierKg, a.SoluteKg - b.SoluteKg);
    public LiquidMixture Scale(double fraction) => new(CarrierKg * fraction, SoluteKg * fraction);
}
public interface IMixtureReservoir
{
    string Identity { get; }
    string ShipId { get; }
    string Profile { get; }
    LiquidMixture Quantity { get; }
    LiquidMixture ComponentCapacity { get; }
    double TotalCapacityKg { get; }
    void SetQuantity(LiquidMixture quantity);
}
public readonly struct MixtureReceipt
{
    public readonly LiquidMixture Debited, Received;
    public MixtureReceipt(LiquidMixture debited, LiquidMixture received) { Debited = debited; Received = received; }
    public bool Reconciled => MixtureTransfer.Same(Debited, Received);
}
public static class MixtureTransfer
{
    public const double ToleranceKg = 1e-9;
    public static bool Same(LiquidMixture a, LiquidMixture b) => Math.Abs(a.CarrierKg - b.CarrierKg) <= ToleranceKg && Math.Abs(a.SoluteKg - b.SoluteKg) <= ToleranceKg;
    private static bool Valid(double x) => FiniteLiquidTransfer.Finite(x) && x >= 0;
    private static bool Valid(LiquidMixture x) => Valid(x.CarrierKg) && Valid(x.SoluteKg) && Valid(x.TotalKg);
    private static void Validate(IMixtureReservoir r)
    {
        var q = r.Quantity; var cap = r.ComponentCapacity;
        if (!Valid(q) || !Valid(cap) || !Valid(r.TotalCapacityKg) || q.CarrierKg > cap.CarrierKg + ToleranceKg ||
            q.SoluteKg > cap.SoluteKg + ToleranceKg || q.TotalKg > r.TotalCapacityKg + ToleranceKg)
            throw new ArgumentException("Invalid mixture reservoir.");
    }
    public static double Allowance(IMixtureReservoir source, IMixtureReservoir destination, double requestedKg)
    {
        Validate(source); Validate(destination);
        if (string.IsNullOrEmpty(source.Identity) || string.IsNullOrEmpty(destination.Identity) || source.Identity == destination.Identity || string.IsNullOrEmpty(source.ShipId) || source.ShipId != destination.ShipId ||
            string.IsNullOrEmpty(source.Profile) || source.Profile != destination.Profile || !Valid(requestedKg))
            throw new ArgumentException("Incompatible mixture transfer.");
        var q = source.Quantity; var space = destination.ComponentCapacity - destination.Quantity;
        if (q.TotalKg == 0) return 0;
        double fraction = Math.Min(1, Math.Min(requestedKg / q.TotalKg, Math.Max(0, destination.TotalCapacityKg - destination.Quantity.TotalKg) / q.TotalKg));
        if (q.CarrierKg > 0) fraction = Math.Min(fraction, Math.Max(0, space.CarrierKg) / q.CarrierKg);
        if (q.SoluteKg > 0) fraction = Math.Min(fraction, Math.Max(0, space.SoluteKg) / q.SoluteKg);
        return fraction * q.TotalKg;
    }
    // Only uniform partial acceptance is refundable. An adapter that changes the composition
    // leaves its evidence for reconciliation; never synthesize one component to balance total mass.
    private static bool Portion(LiquidMixture part, LiquidMixture whole)
    {
        if (!Valid(part) || part.TotalKg > whole.TotalKg + ToleranceKg) return false;
        return whole.TotalKg == 0 ? Same(part, whole) : Same(part, whole.Scale(part.TotalKg / whole.TotalKg));
    }
    internal static MixtureReceipt Commit(IMixtureReservoir source, IMixtureReservoir destination, double requestedKg)
    {
        double kg = Allowance(source, destination, requestedKg);
        if (kg <= 0) return new MixtureReceipt(default, default);
        var before = source.Quantity; var target = destination.Quantity;
        var intended = before.Scale(kg / before.TotalKg);
        source.SetQuantity(before - intended);
        var debit = before - source.Quantity;
        if (!Portion(debit, intended)) throw new InvalidOperationException("Unexpected mixture debit.");
        try { destination.SetQuantity(target + debit); }
        catch
        {
            var retained = destination.Quantity - target;
            if (Portion(retained, debit)) source.SetQuantity(source.Quantity + debit - retained);
            throw;
        }
        var received = destination.Quantity - target;
        if (!Portion(received, debit)) throw new InvalidOperationException("Mixture composition changed during receipt.");
        if (!Same(received, debit)) source.SetQuantity(source.Quantity + debit - received);
        var receipt = new MixtureReceipt(before - source.Quantity, received);
        if (!receipt.Reconciled) throw new InvalidOperationException("Mixture components failed reconciliation.");
        return receipt;
    }
}
