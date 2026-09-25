using System;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>A quantity-backed reservoir. Owned vessels update native mass; foreign adapters retain their provider's mass policy.</summary>
public interface ILiquidReservoir
{
    string Identity { get; }
    string ShipId { get; }
    string Commodity { get; }
    double QuantityKg { get; }
    double CapacityKg { get; }
    void SetQuantity(double kg);
}

public readonly struct LiquidReceipt
{
    public readonly double DebitedKg, ReceivedKg;
    public LiquidReceipt(double debit, double received) { DebitedKg = debit; ReceivedKg = received; }
    public bool Reconciled => Math.Abs(DebitedKg - ReceivedKg) < 1e-7;
}

public static class FiniteLiquidTransfer
{
    public static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    // Synchronous, fresh checks. No saved request can replay a receipt.
    public static LiquidReceipt Commit(ILiquidReservoir source, ILiquidReservoir destination, double requestedKg, double reserveKg)
    {
        double before = source.QuantityKg, target = destination.QuantityKg;
        if (source.Identity == destination.Identity || string.IsNullOrEmpty(source.ShipId) || source.ShipId != destination.ShipId ||
            source.Commodity != destination.Commodity || !Finite(requestedKg) || requestedKg < 0 || !Finite(reserveKg) || reserveKg < 0 ||
            !Finite(before) || before < 0 || !Finite(target) || target < 0 || !Finite(destination.CapacityKg) || target > destination.CapacityKg)
            throw new ArgumentException("Invalid finite liquid transfer.");
        double amount = Math.Max(0, Math.Min(requestedKg, Math.Min(before - reserveKg, destination.CapacityKg - target)));
        if (amount == 0) return new LiquidReceipt(0, 0);
        source.SetQuantity(before - amount);
        double debit = before - source.QuantityKg;
        if (!Finite(debit) || debit < 0 || debit > amount + 1e-7) throw new InvalidOperationException("Unexpected liquid source debit; reconcile before retry.");
        try { destination.SetQuantity(target + debit); }
        catch
        {
            double retained = destination.QuantityKg - target;
            if (Finite(retained) && retained >= 0 && retained <= debit) source.SetQuantity(source.QuantityKg + debit - retained);
            throw;
        }
        double received = destination.QuantityKg - target;
        if (!Finite(received) || received < 0 || received > debit + 1e-7) throw new InvalidOperationException("Unexpected liquid destination receipt; reconcile before retry.");
        if (received < debit) source.SetQuantity(source.QuantityKg + debit - received);
        var receipt = new LiquidReceipt(before - source.QuantityKg, received);
        if (!receipt.Reconciled) throw new InvalidOperationException("Liquid receipt is unbalanced; reconcile before retry.");
        return receipt;
    }
}
