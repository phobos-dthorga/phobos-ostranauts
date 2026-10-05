using System;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosAgriculture;

internal static partial class Service
{
    // The old per-crop feed record. Since 0.55.0 it is read once to fold an old feed into the plain stores
    // (LegacyFeed) and is saved empty; reads do not write.
    private static ObjectStateStore SolutionStore(CondOwner co) => new(co.mapGUIPropMaps, "AgricultureSolution", Plugin.Id, 1);
    private static double ProviderHeadroom(Session s) => Math.Max(0, CropState.ReservoirKg - s.State.Water);
    /// <summary>Whether a W2 can send a rack water now.</summary>
    private static bool CanDeliver(Session source, Session target) => source.State.Water > 0 && target.State.Water < CropState.ReservoirKg;
    /// <summary>The nutrient a rack still wants from this W2: up to the pack's target, while the W2 holds any.</summary>
    private static double NutrientNeed(Session source, Session target) =>
        source.State.Nutrients <= LegacyFeed.Tolerance ? 0 : NutrientFeed.Need(target.State.Nutrients, Growth.NutrientTargetKg);
    /// <summary>Water from a W2 into a rack, through the guarded transfer.</summary>
    private static double DeliverWater(Session source, Session target, double budget)
    {
        if (budget <= 0 || !CanDeliver(source, target)) return 0;
        var water = LiquidTransferGuard.Commit(new Reservoir(source), new Reservoir(target), budget, WaterGuard(source.Object), WaterGuard(target.Object));
        if (water.ReceivedKg > 0) source.Notice = Text.Get("water_receipt", water.ReceivedKg);
        return water.ReceivedKg;
    }
    /// <summary>Nutrients ride with the water the pump moves (Agriculture 0.55.0): up to the pack's dose for every
    /// kilogram, fresh or recirculated. When the fresh water alone cannot carry what the rack needs, the pump
    /// recirculates the rack's own water round the loop, which nets to no water moved. Returns the kilograms
    /// recirculated, which the pump pays for from the same budget.</summary>
    private static double FeedNutrients(Session source, Session target, double freshKg, double budget)
    {
        double need = NutrientNeed(source, target);
        if (need <= LegacyFeed.Tolerance) return 0;
        double strength = Growth.FeedStrength;
        double recirculated = NutrientFeed.Recirculate(need, source.State.Nutrients, freshKg, strength, Math.Max(0, budget));
        double send = NutrientFeed.Send(need, source.State.Nutrients, freshKg + recirculated, strength);
        if (send <= LegacyFeed.Tolerance) return 0;
        var receipt = LiquidTransferGuard.Commit(new DryNutrients(source), new DryNutrients(target), send, WaterGuard(source.Object), WaterGuard(target.Object));
        if (receipt.ReceivedKg <= 0) return 0;
        target.Notice = Text.Get(recirculated > LegacyFeed.Tolerance ? "feed_recirculating" : "feed_receipt", receipt.ReceivedKg * 1000, Phobos.Ostranauts.Framework.Controls.ObjectPresentation.Name(source.Object));
        if (recirculated > LegacyFeed.Tolerance) source.Notice = Text.Get("feed_recirculating_supply", Phobos.Ostranauts.Framework.Controls.ObjectPresentation.Name(target.Object));
        return recirculated;
    }
    private static string DescribeSolution(Session s) => Text.Get("stores_status", s.State.Water, CropState.ReservoirKg, s.State.Nutrients, CropState.NutrientCapacityKg);
}
