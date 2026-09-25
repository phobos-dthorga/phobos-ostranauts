using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Durable interruption evidence around a measured transfer. Not a crash-atomic transaction.
/// An unresolved journal blocks both endpoints until separately investigated; never automatically retry.</summary>
public sealed class LiquidTransferGuard
{
    private readonly ObjectStateStore store;
    public LiquidTransferGuard(Dictionary<string, Dictionary<string, string>> maps, string name, string owner)
        => store = new ObjectStateStore(maps, name, owner, 1);
    public bool Protected
    {
        get
        {
            var status = store.Read(out var fields);
            return status != SavedStateStatus.Missing && (status != SavedStateStatus.Ready ||
                fields.Count != 1 || !fields.TryGetValue("state", out var state) || state != "clear");
        }
    }
    private void Pending(ILiquidReservoir self, ILiquidReservoir peer, double request)
    {
        if (!store.TryWrite(new Dictionary<string, string> { ["state"] = "pending", ["peer"] = peer.Identity,
            ["commodity"] = self.Commodity, ["beforeKg"] = self.QuantityKg.ToString("R", CultureInfo.InvariantCulture),
            ["peerBeforeKg"] = peer.QuantityKg.ToString("R", CultureInfo.InvariantCulture),
            ["requestedKg"] = request.ToString("R", CultureInfo.InvariantCulture) }))
            throw new InvalidOperationException("Liquid transfer journal is protected.");
    }
    private void Complete()
    {
        if (!store.TryWrite(new Dictionary<string, string> { ["state"] = "clear" }))
            throw new InvalidOperationException("Liquid transfer journal completion failed.");
    }
    public static LiquidReceipt Commit(ILiquidReservoir source, ILiquidReservoir destination, double requestedKg,
        LiquidTransferGuard sourceGuard, LiquidTransferGuard destinationGuard)
    {
        if (ReferenceEquals(sourceGuard, destinationGuard) || sourceGuard.Protected || destinationGuard.Protected)
            throw new InvalidOperationException("Liquid transfer requires two clear endpoint journals.");
        // Validate non-mutating admission before recording evidence.
        if (source.Identity == destination.Identity || string.IsNullOrEmpty(source.ShipId) || source.ShipId != destination.ShipId ||
            source.Commodity != destination.Commodity || !Finite(requestedKg) || requestedKg <= 0)
            throw new ArgumentException("Invalid guarded liquid transfer.");
        sourceGuard.Pending(source, destination, requestedKg);
        destinationGuard.Pending(destination, source, requestedKg);
        var receipt = FiniteLiquidTransfer.Commit(source, destination, requestedKg, 0);
        if (!receipt.Reconciled) throw new InvalidOperationException("Liquid receipt did not reconcile.");
        destinationGuard.Complete(); sourceGuard.Complete();
        return receipt;
    }
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
