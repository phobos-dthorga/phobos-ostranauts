using System;
using PhobosAgriculture.Core;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Inventory;

namespace PhobosAgriculture;

internal static partial class Service
{
    // Additive schema migration: absence means zero dissolved inventory, never reinterpret
    // the original Agriculture/schema-1 water or dry nutrient fields. Reads do not write.
    private static ObjectStateStore SolutionStore(CondOwner co) => new(co.mapGUIPropMaps, "AgricultureSolution", Plugin.Id, 1);
    private static double ProviderHeadroom(Session s) => Math.Max(0, s.Solution.PlainWaterCapacity - s.State.Water -
        (s.Solution.Enabled ? CropState.NutrientCapacityKg : 0));
    private static bool CompatibleSolution(Session source, Session target) => source.Solution.Profile == target.Solution.Profile &&
        (!source.Solution.Enabled || target.State.CropId.Length == 0 || NutrientSolution.CropId(source.Solution.Profile) == target.State.CropId);
    private static bool? SolutionCommand(Session s, string action, out string message)
    {
        message = "";
        if (action != "mix-potato" && action != "mix-lettuce" && action != "water-only") return null;
        if (!IrrigationDefinitions.IsSupply(s.Object) || !Paused(s) || !NativeFluidRoute.EndpointReady(s.Object) ||
            WaterBank(s.Object).Occupied || s.Solution.TotalKg > NutrientSolution.Tolerance)
        { message = Text.Get("solution_switch"); return false; }
        string profile = action == "mix-potato" ? NutrientSolution.Potato : action == "mix-lettuce" ? NutrientSolution.Lettuce : NutrientSolution.None;
        if (profile != NutrientSolution.None && s.State.Water > CropState.ReservoirKg - CropState.NutrientCapacityKg)
        { message = Text.Get("solution_headroom"); return false; }
        s.Solution.Profile = profile; Save(s); message = Describe(s.Object); return true;
    }
    private static bool CanDeliver(Session source, Session target)
    {
        if (!CompatibleSolution(source, target)) return false;
        return source.Solution.Enabled
            ? MixtureTransfer.Allowance(new SolutionReservoir(source), new SolutionReservoir(target), double.MaxValue) > NutrientSolution.Tolerance
            : source.State.Water > 0 && target.State.Water < target.Solution.PlainWaterCapacity;
    }
    private static double DeliverLiquid(Session source, Session target, double budget)
    {
        if (source.Solution.Enabled)
        {
            var receipt = LiquidTransferGuard.Commit(new SolutionReservoir(source), new SolutionReservoir(target), budget, WaterGuard(source.Object), WaterGuard(target.Object));
            source.Notice = Text.Get("solution_receipt", receipt.Received.CarrierKg, receipt.Received.SoluteKg);
            return receipt.Received.TotalKg;
        }
        var water = LiquidTransferGuard.Commit(new Reservoir(source), new Reservoir(target), budget, WaterGuard(source.Object), WaterGuard(target.Object));
        source.Notice = Text.Get("water_receipt", water.ReceivedKg); return water.ReceivedKg;
    }
    private static string DescribeSolution(Session s) => Text.Get("solution_status", Text.Get("solution_" + s.Solution.Profile),
        s.Solution.Quantity.CarrierKg, s.Solution.Quantity.SoluteKg, s.State.Nutrients, s.State.Water + s.Solution.TotalKg, CropState.ReservoirKg);

    private sealed class SolutionReservoir : IMixtureReservoir
    {
        private readonly Session s;
        internal SolutionReservoir(Session session) { s = session; }
        public string Identity => s.Object.strID;
        public string ShipId => s.Object.ship.strRegID;
        public string Profile => "phobos.agriculture.solution." + s.Solution.Profile;
        public LiquidMixture Quantity => s.Solution.Quantity;
        public LiquidMixture ComponentCapacity => new(Math.Max(0, CropState.ReservoirKg - s.State.Water), Math.Max(0, CropState.NutrientCapacityKg - s.State.Nutrients));
        public double TotalCapacityKg => Math.Max(0, CropState.ReservoirKg - s.State.Water);
        public void SetQuantity(LiquidMixture quantity) { s.Solution.Quantity = quantity; Save(s); }
    }
}
