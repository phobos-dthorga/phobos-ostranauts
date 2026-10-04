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
    internal static readonly ChargeMachine Leach = new(LeachSpec());
    internal static readonly ChargeMachine AcidPlant = new(AcidPlantSpec());
    internal static readonly ChargeMachine Fermenter = new(FermenterSpec());
    internal static readonly ChargeMachine ElectrolysisCell = new(ElectrolysisCellSpec());
    internal static readonly ChargeMachine Carbothermal = new(CarbothermalSpec());
    internal static readonly IReadOnlyList<ChargeMachine> All = new[] { Refinery, Leach, AcidPlant, Fermenter, ElectrolysisCell, Carbothermal };
    private static readonly Dictionary<string, ChargeMachine?> byDefinition = new(StringComparer.Ordinal);
    internal static ChargeMachine? For(string? id)
    {
        if (id == null) return null;
        if (byDefinition.TryGetValue(id, out var known)) return known;
        var machine = All.FirstOrDefault(m => m.IsFamily(id));
        if (byDefinition.Count < 65536) byDefinition[id] = machine;
        return machine;
    }
    // Called for every container admission and stack test in the game: one string comparison per charge machine, no allocation.
    internal static ChargeMachine? ForBin(string? binId)
    {
        if (binId == null) return null;
        for (int i = 0; i < All.Count; i++) if (All[i].Spec.InputBin == binId) return All[i];
        return null;
    }
    internal static bool IsBin(string? id) => ForBin(id) != null;
    internal static void Reset() { byDefinition.Clear(); foreach (var machine in All) machine.Reset(); }

    /// <summary>The V4: automatic best-match charges (nickel steel supersedes the plain steel charge from 0.26.0, which stays
    /// gated on Shipbreaker's stock so a charge already bound to it settles), melts that freeze to
    /// slag, a hearth that can ignite a leaking store, water and stored-gas outlets. Keys, ports and texts are the
    /// ones every saved V4 already carries.</summary>
    private static ChargeMachineSpec RefinerySpec() => new()
    {
        Prefix = RefineryRules.Prefix, StockTrigger = RefineryRules.StockTrigger, StockFeed = RefineryRules.StockFeed, AdmitsOre = true, FeedConditions = RefineryRules.FeedConditions,
        Record = RefineryRules.Record, MachineKey = ChargeCatalog.Refinery, TextPrefix = "Refinery", HeatNote = true, SnapshotKind = "refinery", Art = Definitions.RefineryArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = true,
        Met = key => key == ChargeCatalog.SteelStockRequirement && ShipbreakerStock.Available || key == ChargeCatalog.StrawRequirement && AgricultureStock.Straw,
        Spoiled = RefineryRecipes.Spoiled, SpoiledProducts = RefineryRecipes.SpoiledProducts,
        Links = () => new[] { RefineryWater() }.Concat(RefineryRules.GasFamilies.Select(RefineryGas)).ToArray()
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
        () => Text.Get("Refinery.linked"), () => Text.Get("Refinery.unlinked"), () => Text.Get("Refinery.link_missing"), deposit: true);
    /// <summary>One link per gas family a charge stores or (Manufacturing 0.27.0) draws: methane and oxygen come from
    /// their stores the way ammonia and carbon dioxide go into theirs.</summary>
    private static ChargeLinkSpec RefineryGas(GasFamily family)
    {
        string Gas() => Text.Get(family.TextPrefix + ".gas");
        bool deposit = RefineryRules.Stores(family);
        return new(family.Commodity, "gas-link:" + family.SmallPrefix + ":", RefineryRules.GasOutPort(family), RefineryRules.GasInPort,
            v => GasStores.Holds(v.strCODef, family.Commodity), () => Text.Get(deposit ? "Provider.gas_field" : "Provider.gas_source_field", Gas()), alwaysShow: false,
            (problem, have, need) => problem switch
            {
                LinkProblem.None => Text.Get("Refinery.no_store", Gas()),
                LinkProblem.NotReady => Text.Get("Refinery.store_not_ready", Gas()),
                LinkProblem.Protected or LinkProblem.Busy => Text.Get("Refinery.store_protected", Gas()),
                LinkProblem.Catch => Text.Get("Refinery.store_catch", Gas()),
                LinkProblem.Short => Text.Get("Refinery.store_short", Gas(), have, need),
                _ => Text.Get("Refinery.store_full", Gas(), have, need)
            },
            () => Text.Get(deposit ? "Refinery.store_linked" : "Refinery.source_linked", Gas()), () => Text.Get("Refinery.unlinked"), () => Text.Get("Refinery.store_link_missing", Gas()), deposit: deposit);
    }

    /// <summary>The Lixivar LC-3 (Manufacturing 0.18.0): the crew selects the recipe; nothing melts or spoils and it
    /// is no ignition source. Water circulates through, and is drawn from, a linked water vessel; the struvite steps
    /// draw ammonia from a linked ammonia store; the makeup formulation needs Agriculture's makeup packet. From 0.20.0
    /// the game's olivine enters its feed (the container rule admits only the selected recipe's exact charge), the acid
    /// steps draw from a linked acid tank and the complete formulation deposits into a linked Agriculture hopper.</summary>
    private static ChargeMachineSpec LeachSpec() => new()
    {
        Prefix = LeachRules.Prefix, StockTrigger = LeachRules.StockTrigger, StockFeed = LeachRules.StockFeed, AdmitsOre = true, FeedConditions = LeachRules.FeedConditions,
        Record = LeachRules.Record, MachineKey = ChargeCatalog.Leach, TextPrefix = "Leach", SnapshotKind = "leach", Art = Definitions.LeachArt,
        Selection = RecipeSelection.Explicit, IgnitionSource = false,
        Met = key => LeachRecipes.IsMet(key, AgricultureStock.Available, AgricultureStock.Hoppers),
        ExtraStatus = _ => !AgricultureStock.Available ? Text.Get("Leach.no_agriculture") : AgricultureStock.Hoppers ? null : Text.Get("Leach.no_hoppers"),
        MaintenanceChargeKey = "Maintenance.leach_charge",
        Links = () => new[] { LeachWater(), LeachAmmonia(), LeachAcid(), LeachNutrients(), LeachHydrogen() }
    };
    private static ChargeLinkSpec LeachWater() => new(ManufacturingRules.Water, "link:", LeachRules.WaterPort, LeachRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true,
        (problem, have, need) => problem switch
        {
            LinkProblem.None => Text.Get("Leach.no_vessel"),
            LinkProblem.NotReady => Text.Get("Leach.vessel_not_ready"),
            LinkProblem.Protected or LinkProblem.Busy => Text.Get("Leach.vessel_protected"),
            LinkProblem.Catch => Text.Get("Leach.vessel_catch"),
            LinkProblem.Short => Text.Get("Leach.vessel_short", have, need),
            _ => Text.Get("Leach.vessel_full", have, need)
        },
        () => Text.Get("Leach.linked"), () => Text.Get("Leach.unlinked"), () => Text.Get("Leach.link_missing"));
    private static ChargeLinkSpec LeachAmmonia() => new(ManufacturingRules.Ammonia, "ammonia:", LeachRules.AmmoniaPort, LeachRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Ammonia), () => Text.Get("Provider.ammonia_field"), alwaysShow: false,
        (problem, have, need) => problem switch
        {
            LinkProblem.None => Text.Get("Leach.no_ammonia"),
            LinkProblem.NotReady => Text.Get("Leach.ammonia_not_ready"),
            LinkProblem.Protected or LinkProblem.Busy => Text.Get("Leach.ammonia_protected"),
            LinkProblem.Catch => Text.Get("Leach.ammonia_catch"),
            LinkProblem.Short => Text.Get("Leach.ammonia_short", have, need),
            _ => Text.Get("Leach.ammonia_full", have, need)
        },
        () => Text.Get("Leach.ammonia_linked"), () => Text.Get("Leach.ammonia_unlinked"), () => Text.Get("Leach.ammonia_link_missing"));
    private static ChargeLinkSpec LeachAcid() => new(LiquidStores.SulfuricAcid, "acid:", LeachRules.AcidPort, LeachRules.VesselPort,
        v => LiquidStores.Holds(v.strCODef, LiquidStores.SulfuricAcid), () => Text.Get("Provider.acid_field"), alwaysShow: false, Reasons("Leach", "acid"),
        () => Text.Get("Leach.acid_linked"), () => Text.Get("Leach.acid_unlinked"), () => Text.Get("Leach.acid_link_missing"));
    /// <summary>Any registered vessel of Agriculture's crop nutrients is a hopper (the vessel list is by commodity).</summary>
    private static ChargeLinkSpec LeachNutrients() => new(ManufacturingRules.CropNutrients, "nutrients:", LeachRules.NutrientPort, LeachRules.VesselPort,
        _ => true, () => Text.Get("Provider.nutrients_field"), alwaysShow: false, Reasons("Leach", "nutrients"),
        () => Text.Get("Leach.nutrients_linked"), () => Text.Get("Leach.nutrients_unlinked"), () => Text.Get("Leach.nutrients_link_missing"), deposit: true);

    /// <summary>The ferrosilicon charge's hydrogen goes to a linked hydrogen store (Manufacturing 0.52.0).</summary>
    private static ChargeLinkSpec LeachHydrogen() => new(ManufacturingRules.Hydrogen, "hydrogen:", LeachRules.HydrogenPort, LeachRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Hydrogen), () => Text.Get("Provider.hydrogen_out_field"), alwaysShow: false, Reasons("Leach", "hydrogen"),
        () => Text.Get("Leach.hydrogen_linked"), () => Text.Get("Leach.hydrogen_unlinked"), () => Text.Get("Leach.hydrogen_link_missing"), deposit: true);

    /// <summary>The Oxsmith EC-4 (Manufacturing 0.52.0): a lump of regolith or a chunk of Silicates ore, chosen
    /// automatically from the feed; oxygen to a linked oxygen store, a lump's bound water to a linked water vessel,
    /// ferrosilicon and slag to the tray. Nothing spoils: a cell left waiting for a cooler room freezes around its
    /// electrodes and melts again. Its molten pool is an ignition source while it works.</summary>
    private static ChargeMachineSpec ElectrolysisCellSpec() => new()
    {
        Prefix = ElectrolysisRules.Prefix, StockTrigger = ElectrolysisRules.StockTrigger, AdmitsOre = true, FeedConditions = ElectrolysisRules.FeedConditions,
        Record = ElectrolysisRules.Record, MachineKey = ChargeCatalog.ElectrolysisCell, TextPrefix = "Cell", HeatNote = true, SnapshotKind = "electrolysis-cell", Art = Definitions.ElectrolysisCellArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = true,
        MaintenanceChargeKey = "Maintenance.cell_charge",
        Links = () => new[] { CellOxygen(), CellWater() }
    };
    private static ChargeLinkSpec CellOxygen() => new(ManufacturingRules.Oxygen, "oxygen:", ElectrolysisRules.OxygenPort, ElectrolysisRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Oxygen), () => Text.Get("Provider.oxygen_store_field"), alwaysShow: true, Reasons("Cell", "oxygen"),
        () => Text.Get("Cell.oxygen_linked"), () => Text.Get("Cell.oxygen_unlinked"), () => Text.Get("Cell.oxygen_link_missing"), deposit: true);
    private static ChargeLinkSpec CellWater() => new(ManufacturingRules.Water, "link:", ElectrolysisRules.WaterPort, ElectrolysisRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true, Reasons("Cell", "vessel"),
        () => Text.Get("Cell.linked"), () => Text.Get("Cell.unlinked"), () => Text.Get("Cell.link_missing"), deposit: true);

    /// <summary>The Oxsmith CR-4 (Manufacturing 0.53.0): a lump of regolith or a chunk of Silicates ore, chosen
    /// automatically from the feed, reduced with methane drawn from a linked methane store. Carbon monoxide and hydrogen
    /// go to their linked stores, a lump's bound water to a linked water vessel, ferrosilicon and slag to the tray. A
    /// charge binds only once its methane store is linked (the engine's rule for a drawing charge). Nothing spoils, and
    /// its hot bed is an ignition source while it works.</summary>
    private static ChargeMachineSpec CarbothermalSpec() => new()
    {
        Prefix = CarbothermalRules.Prefix, StockTrigger = CarbothermalRules.StockTrigger, AdmitsOre = true, FeedConditions = CarbothermalRules.FeedConditions,
        Record = CarbothermalRules.Record, MachineKey = ChargeCatalog.CarbothermalReactor, TextPrefix = "Carbothermal", HeatNote = true, SnapshotKind = "carbothermal-reactor", Art = Definitions.CarbothermalArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = true,
        MaintenanceChargeKey = "Maintenance.carbothermal_charge",
        Links = () => new[] { CarbothermalMethane(), CarbothermalMonoxide(), CarbothermalHydrogen(), CarbothermalWater() }
    };
    private static ChargeLinkSpec CarbothermalMethane() => new(ManufacturingRules.Methane, "methane:", CarbothermalRules.MethanePort, CarbothermalRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Methane), () => Text.Get("Provider.methane_from_field"), alwaysShow: true, Reasons("Carbothermal", "methane"),
        () => Text.Get("Carbothermal.methane_linked"), () => Text.Get("Carbothermal.methane_unlinked"), () => Text.Get("Carbothermal.methane_link_missing"));
    private static ChargeLinkSpec CarbothermalMonoxide() => new(ManufacturingRules.CarbonMonoxide, "monoxide:", CarbothermalRules.MonoxidePort, CarbothermalRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.CarbonMonoxide), () => Text.Get("Provider.monoxide_out_field"), alwaysShow: true, Reasons("Carbothermal", "monoxide"),
        () => Text.Get("Carbothermal.monoxide_linked"), () => Text.Get("Carbothermal.monoxide_unlinked"), () => Text.Get("Carbothermal.monoxide_link_missing"), deposit: true);
    private static ChargeLinkSpec CarbothermalHydrogen() => new(ManufacturingRules.Hydrogen, "hydrogen:", CarbothermalRules.HydrogenPort, CarbothermalRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Hydrogen), () => Text.Get("Provider.hydrogen_out_field"), alwaysShow: true, Reasons("Carbothermal", "hydrogen"),
        () => Text.Get("Carbothermal.hydrogen_linked"), () => Text.Get("Carbothermal.hydrogen_unlinked"), () => Text.Get("Carbothermal.hydrogen_link_missing"), deposit: true);
    private static ChargeLinkSpec CarbothermalWater() => new(ManufacturingRules.Water, "link:", CarbothermalRules.WaterPort, CarbothermalRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true, Reasons("Carbothermal", "vessel"),
        () => Text.Get("Carbothermal.linked"), () => Text.Get("Carbothermal.unlinked"), () => Text.Get("Carbothermal.link_missing"), deposit: true);

    /// <summary>The Lixivar SA-3 (Manufacturing 0.19.0): one recipe, chosen automatically; oxygen and water drawn from
    /// linked vessels, sulfuric acid deposited into a linked acid tank, the reactions' heat into the room. Nothing melts
    /// or spoils; it is no ignition source (the roaster is enclosed).</summary>
    private static ChargeMachineSpec AcidPlantSpec() => new()
    {
        Prefix = AcidPlantRules.Prefix, StockTrigger = AcidPlantRules.StockTrigger, StockFeed = AcidPlantRules.StockFeed, AdmitsOre = false,
        Record = AcidPlantRules.Record, MachineKey = ChargeCatalog.AcidPlant, TextPrefix = "AcidPlant", HeatNote = true, SnapshotKind = "acid-plant", Art = Definitions.AcidPlantArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = false,
        MaintenanceChargeKey = "Maintenance.acid_plant_charge",
        Links = () => new[] { AcidPlantOxygen(), AcidPlantWater(), AcidPlantAcid() }
    };
    /// <summary>The Alembrine Copperhead-3 (Manufacturing 0.39.0): beets or sugar, chosen automatically from the feed;
    /// ethanol to a linked cask, carbon dioxide to a linked store, stillage water to (and wash water from) a linked water
    /// vessel. It is an ignition source while working: its boiler and hot ethanol vapour light a spill in its room.</summary>
    private static ChargeMachineSpec FermenterSpec() => new()
    {
        Prefix = FermenterRules.Prefix, StockTrigger = FermenterRules.StockTrigger, StockFeed = FermenterRules.StockFeed, AdmitsOre = false,
        Record = FermenterRules.Record, MachineKey = ChargeCatalog.Fermenter, TextPrefix = "Fermenter", SnapshotKind = "fermenter", Art = Definitions.FermenterArt,
        Selection = RecipeSelection.Automatic, IgnitionSource = true,
        Met = key => key == ChargeCatalog.SugarCropsRequirement && AgricultureStock.SugarCrops,
        ExtraStatus = _ => AgricultureStock.SugarCrops ? null : Text.Get("Fermenter.no_agriculture"),
        MaintenanceChargeKey = "Maintenance.fermenter_charge",
        Links = () => new[] { FermenterWater(), FermenterCarbonDioxide(), FermenterEthanol() }
    };
    private static ChargeLinkSpec FermenterWater() => new(ManufacturingRules.Water, "link:", FermenterRules.WaterPort, FermenterRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true, Reasons("Fermenter", "vessel"),
        () => Text.Get("Fermenter.linked"), () => Text.Get("Fermenter.unlinked"), () => Text.Get("Fermenter.link_missing"), deposit: true);
    private static ChargeLinkSpec FermenterCarbonDioxide() => new(ManufacturingRules.CarbonDioxide, "co2:", FermenterRules.CarbonDioxidePort, FermenterRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.CarbonDioxide), () => Text.Get("Provider.co2_store_field"), alwaysShow: true, Reasons("Fermenter", "co2"),
        () => Text.Get("Fermenter.co2_linked"), () => Text.Get("Fermenter.co2_unlinked"), () => Text.Get("Fermenter.co2_link_missing"), deposit: true);
    private static ChargeLinkSpec FermenterEthanol() => new(LiquidStores.Ethanol, "ethanol:", FermenterRules.EthanolPort, FermenterRules.VesselPort,
        v => LiquidStores.Holds(v.strCODef, LiquidStores.Ethanol), () => Text.Get("Provider.ethanol_field"), alwaysShow: true, Reasons("Fermenter", "ethanol"),
        () => Text.Get("Fermenter.ethanol_linked"), () => Text.Get("Fermenter.ethanol_unlinked"), () => Text.Get("Fermenter.ethanol_link_missing"), deposit: true);
    private static Func<LinkProblem, double, double, string> Reasons(string prefix, string what) => (problem, have, need) => problem switch
    {
        LinkProblem.None => Text.Get(prefix + ".no_" + what),
        LinkProblem.NotReady => Text.Get(prefix + "." + what + "_not_ready"),
        LinkProblem.Protected or LinkProblem.Busy => Text.Get(prefix + "." + what + "_protected"),
        LinkProblem.Catch => Text.Get(prefix + "." + what + "_catch"),
        LinkProblem.Short => Text.Get(prefix + "." + what + "_short", have, need),
        _ => Text.Get(prefix + "." + what + "_full", have, need)
    };
    private static ChargeLinkSpec AcidPlantOxygen() => new(ManufacturingRules.Oxygen, "oxygen:", AcidPlantRules.OxygenPort, AcidPlantRules.VesselPort,
        v => GasStores.Holds(v.strCODef, ManufacturingRules.Oxygen), () => Text.Get("Provider.oxygen_field"), alwaysShow: true, Reasons("AcidPlant", "oxygen"),
        () => Text.Get("AcidPlant.oxygen_linked"), () => Text.Get("AcidPlant.oxygen_unlinked"), () => Text.Get("AcidPlant.oxygen_link_missing"));
    private static ChargeLinkSpec AcidPlantWater() => new(ManufacturingRules.Water, "link:", AcidPlantRules.WaterPort, AcidPlantRules.VesselPort, _ => true,
        () => Text.Get("Provider.vessel_field"), alwaysShow: true,
        (problem, have, need) => problem switch
        {
            LinkProblem.None => Text.Get("AcidPlant.no_vessel"),
            LinkProblem.NotReady => Text.Get("AcidPlant.vessel_not_ready"),
            LinkProblem.Protected or LinkProblem.Busy => Text.Get("AcidPlant.vessel_protected"),
            LinkProblem.Catch => Text.Get("AcidPlant.vessel_catch"),
            LinkProblem.Short => Text.Get("AcidPlant.vessel_short", have, need),
            _ => Text.Get("AcidPlant.vessel_full", have, need)
        },
        () => Text.Get("AcidPlant.linked"), () => Text.Get("AcidPlant.unlinked"), () => Text.Get("AcidPlant.link_missing"));
    private static ChargeLinkSpec AcidPlantAcid() => new(LiquidStores.SulfuricAcid, "acid:", AcidPlantRules.AcidPort, AcidPlantRules.VesselPort,
        v => LiquidStores.Holds(v.strCODef, LiquidStores.SulfuricAcid), () => Text.Get("Provider.acid_field"), alwaysShow: true, Reasons("AcidPlant", "acid"),
        () => Text.Get("AcidPlant.acid_linked"), () => Text.Get("AcidPlant.acid_unlinked"), () => Text.Get("AcidPlant.acid_link_missing"), deposit: true);
}
