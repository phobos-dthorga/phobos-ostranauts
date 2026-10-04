using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Oxsmith CR-4 Carbothermal Reactor (Manufacturing 0.53.0; owner decisions of 5 October 2026, set 5 of the
/// regolith programme): a 4 x 4 reactor on the shared charge engine that reduces molten rock with methane. One lump of
/// the game's loose regolith or one chunk of its Silicates ore at a time, with methane drawn from a linked methane
/// store: the rock's oxygen leaves as carbon monoxide for a linked carbon monoxide store, the methane's hydrogen goes
/// to a linked hydrogen store, and the same ferrosilicon and slag as the EC-4 land in the tray. A K2 turns the carbon
/// monoxide and hydrogen back into methane and water, and an X2 splits that water into the oxygen. Identities, ports
/// and records stay here; shape and recipes are in the packs.</summary>
public static class CarbothermalRules
{
    public const string Prefix = "PhobosCarbothermalReactor", Installed = Prefix + "Installed";
    public const string StockTrigger = Prefix + "TStock", Record = "ManufacturingCarbothermalReactor";
    public const string MethanePort = "PhobosManufacturing.CarbothermalLink.methane", MonoxidePort = "PhobosManufacturing.CarbothermalLink.monoxide",
        HydrogenPort = "PhobosManufacturing.CarbothermalLink.hydrogen", WaterPort = "PhobosManufacturing.CarbothermalLink.water", VesselPort = "PhobosManufacturing.CarbothermalIn";
    /// <summary>Native conditions the feed admits beside the ore rule: loose regolith's own condition, as on the EC-4.</summary>
    public static readonly string[] FeedConditions = { RefineryRules.RegolithCondition };
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
}

/// <summary>The CR-4 catalog: the <c>carbothermal-reactor</c> recipes of the shared charge catalog.</summary>
public static class CarbothermalRecipes
{
    public const string Machine = ChargeCatalog.CarbothermalReactor;
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe Regolith => View.ById("regolith-carbothermal")!;
    public static ChargeRecipe Silicates => View.ById("silicates-carbothermal")!;
    public static ChargeRecipe? Match(IEnumerable<string> feedIds) => View.Match(feedIds, _ => false);
}
