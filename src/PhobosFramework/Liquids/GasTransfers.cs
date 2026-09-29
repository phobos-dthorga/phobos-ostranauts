using System;
using Phobos.Ostranauts.Framework.Processing;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Gas moved between a bulk vessel's kilogram record and the game's own gas vessels (canisters and suit
/// bottles). Every move is bounded by the store's service contents above its reserve, the native vessel's safe fill
/// or contents, and the store's headroom; it happens under the store's conversion journal, so an interrupted move
/// leaves the store Protected for the owner to accept rather than silently creating or losing gas. The record changes
/// first when gas leaves a store and last when gas enters one: an interruption can lose gas, never create it.
/// Content decides which species a commodity is; Framework never assumes one.</summary>
public static class GasTransfers
{
    /// <summary>Moves up to <paramref name="kg"/> from the store into <paramref name="vessel"/>, never past
    /// <paramref name="fraction"/> of the vessel's rating; returns the kilograms that arrived.</summary>
    public static double StoreToVessel(CondOwner store, CondOwner vessel, string species, double kg, double fraction = NativeGasVessel.SafeFillFraction)
    {
        if (!Ready(store, kg) || vessel.HasCond("IsDamaged") || !NativeGasVessel.TryRead(vessel, out var reading) || reading.Species != species) return 0;
        var spec = BulkVessel.Spec(store);
        var state = BulkVessel.Read(store, spec);
        double planned = Math.Min(kg, Math.Min(Math.Max(0, state.ServiceKg - state.ReserveKg), NativeGasCanister.Kilograms(species, reading.HeadroomMoles(fraction))));
        if (planned <= 1e-9) return 0;
        BulkVessel.BeginConversion(store, vessel.strID, state.ServiceKg);
        state.SetService(state.ServiceKg - planned); BulkVessel.Save(store, spec, state);
        double added = NativeGasCanister.Kilograms(species, NativeGasVessel.TryFill(vessel, species, NativeGasCanister.Moles(species, planned), fraction));
        if (planned - added > 1e-12) { state = BulkVessel.Read(store, spec); state.SetService(state.ServiceKg + planned - added); BulkVessel.Save(store, spec, state); }
        BulkVessel.EndConversion(store);
        return added;
    }
    /// <summary>Empties up to <paramref name="kg"/> of the vessel's species into the store, bounded by the store's
    /// headroom; returns the kilograms that arrived.</summary>
    public static double VesselToStore(CondOwner vessel, CondOwner store, string species, double kg)
    {
        if (!Ready(store, kg) || !NativeGasVessel.TryRead(vessel, out var reading) || reading.Species != species) return 0;
        var spec = BulkVessel.Spec(store);
        var state = BulkVessel.Read(store, spec);
        double headroom = Math.Max(0, spec.CapacityKg - state.ServiceKg - state.CatchKg);
        double planned = Math.Min(kg, Math.Min(headroom, NativeGasCanister.Kilograms(species, reading.Moles)));
        if (planned <= 1e-9) return 0;
        BulkVessel.BeginConversion(store, vessel.strID, state.ServiceKg);
        double taken = NativeGasCanister.Kilograms(species, NativeGasVessel.TryDrain(vessel, species, NativeGasCanister.Moles(species, planned)));
        state = BulkVessel.Read(store, spec); state.SetService(state.ServiceKg + taken); BulkVessel.Save(store, spec, state);
        BulkVessel.EndConversion(store);
        return taken;
    }
    /// <summary>Decants up to <paramref name="kg"/> from one native vessel into another of the same species; whatever
    /// the target cannot take goes straight back. Returns the kilograms that arrived.</summary>
    public static double VesselToVessel(CondOwner source, CondOwner target, string species, double kg, double fraction = NativeGasVessel.SafeFillFraction)
    {
        if (!BulkVesselSpec.Finite(kg) || kg <= 0 || source == target || target.HasCond("IsDamaged") ||
            !NativeGasVessel.TryRead(source, out var from) || !NativeGasVessel.TryRead(target, out var to) || from.Species != species || to.Species != species) return 0;
        double moles = Math.Min(NativeGasCanister.Moles(species, kg), Math.Min(from.Moles, to.HeadroomMoles(fraction)));
        if (moles <= 1e-12) return 0;
        double taken = NativeGasVessel.TryDrain(source, species, moles);
        double added = NativeGasVessel.TryFill(target, species, taken, fraction);
        if (taken - added > 1e-12) NativeGasVessel.TryFill(source, species, taken - added, 1);
        return NativeGasCanister.Kilograms(species, added);
    }
    private static bool Ready(CondOwner store, double kg)
    {
        if (!BulkVesselSpec.Finite(kg) || kg <= 0 || store == null || store.bDestroyed || !BulkVessels.IsVessel(store) || BulkVessel.Protected(store) || CommodityReservations.Held(store.strID)) return false;
        // Buffered draws (the RCS manifold) land first, so the record read here is current.
        BufferedDrains.Settle(store);
        return !BulkVessel.Protected(store);
    }
}
