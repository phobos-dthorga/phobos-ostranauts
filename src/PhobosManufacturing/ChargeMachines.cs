using System;
using System.Collections.Generic;
using System.Linq;
using PhobosManufacturing.Core;

namespace PhobosManufacturing;

/// <summary>Manufacturing's charge machines and their specs: the lookup the power hooks, feed patches, panel and
/// maintenance checks use instead of naming each machine. One dictionary probe per definition id on hot paths.</summary>
internal static class ChargeMachines
{
    internal static readonly ChargeMachine Refinery = new(RefinerySpec());
    internal static readonly IReadOnlyList<ChargeMachine> All = new[] { Refinery };
    private static readonly Dictionary<string, ChargeMachine?> byDefinition = new(StringComparer.Ordinal);
    internal static ChargeMachine? For(string? id)
    {
        if (id == null) return null;
        if (byDefinition.TryGetValue(id, out var known)) return known;
        var machine = All.FirstOrDefault(m => m.IsFamily(id));
        if (byDefinition.Count < 65536) byDefinition[id] = machine;
        return machine;
    }
    internal static ChargeMachine? ForBin(string? binId) => binId == null ? null : All.FirstOrDefault(m => m.Spec.InputBin == binId);
    internal static bool IsBin(string? id) => ForBin(id) != null;
    internal static void Reset() { byDefinition.Clear(); foreach (var machine in All) machine.Reset(); }

    /// <summary>The V4: automatic best-match charges, the steel recipe gated on Shipbreaker's stock, melts that freeze to
    /// slag, a hearth that can ignite a leaking store, water and stored-gas outlets. Keys, ports and texts are the
    /// ones every saved V4 already carries.</summary>
    private static ChargeMachineSpec RefinerySpec() => new()
    {
        Prefix = RefineryRules.Prefix, StockTrigger = RefineryRules.StockTrigger, StockFeed = RefineryRules.StockFeed, AdmitsOre = true,
        Record = RefineryRules.Record, MachineKey = ChargeCatalog.Refinery, TextPrefix = "Refinery", SnapshotKind = "refinery", Art = Definitions.RefineryArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = true,
        Met = key => key == ChargeCatalog.SteelStockRequirement && ShipbreakerStock.Available,
        Spoiled = RefineryRecipes.Spoiled, SpoiledProducts = RefineryRecipes.SpoiledProducts,
        ExtraStatus = _ => ShipbreakerStock.Available ? null : Text.Get("Refinery.no_steel"),
        Links = () => new[] { RefineryWater() }.Concat(RefineryRules.StoredGasFamilies.Select(RefineryGas)).ToArray()
    };
    private static ChargeLinkSpec RefineryWater() => new(ManufacturingRules.Water, "link:", RefineryRules.OutPort, RefineryRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true,
        (problem, have, need) => problem switch
        {
            LinkProblem.None => Text.Get("Refinery.no_vessel"),
            LinkProblem.NotReady => Text.Get("Refinery.vessel_not_ready"),
            LinkProblem.Protected or LinkProblem.Busy => Text.Get("Refinery.vessel_protected"),
            LinkProblem.Catch => Text.Get("Refinery.vessel_catch"),
            _ => Text.Get("Refinery.vessel_full", have, need)
        },
        () => Text.Get("Refinery.linked"), () => Text.Get("Refinery.unlinked"), () => Text.Get("Refinery.link_missing"));
    private static ChargeLinkSpec RefineryGas(GasFamily family)
    {
        string Gas() => Text.Get(family.TextPrefix + ".gas");
        return new(family.Commodity, "gas-link:" + family.SmallPrefix + ":", RefineryRules.GasOutPort(family), RefineryRules.GasInPort,
            v => GasStores.Holds(v.strCODef, family.Commodity), () => Text.Get("Provider.gas_field", Gas()), alwaysShow: false,
            (problem, have, need) => problem switch
            {
                LinkProblem.None => Text.Get("Refinery.no_store", Gas()),
                LinkProblem.NotReady => Text.Get("Refinery.store_not_ready", Gas()),
                LinkProblem.Protected or LinkProblem.Busy => Text.Get("Refinery.store_protected", Gas()),
                LinkProblem.Catch => Text.Get("Refinery.store_catch", Gas()),
                _ => Text.Get("Refinery.store_full", Gas(), have, need)
            },
            () => Text.Get("Refinery.store_linked", Gas()), () => Text.Get("Refinery.unlinked"), () => Text.Get("Refinery.store_link_missing", Gas()));
    }
}
