using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Lixivar SA-3 Sulfuric Acid Plant (Manufacturing 0.19.0): a 3 x 3 roaster, converter and absorber on
/// the shared charge engine. One sulfide-phosphide nodule at a time: its troilite roasts to SO2, which is converted
/// and absorbed into sulfuric acid for a linked acid tank; its schreibersite's phosphorus is caught as phosphoric
/// acid in a flask. Oxygen comes from a linked oxygen store and water from a linked water vessel, each within one
/// tile. The reactions release far more heat than the plant draws; the room must take it. Identities, ports and
/// records stay here; shape and recipes are in the packs.</summary>
public static class AcidPlantRules
{
    public const string Prefix = "PhobosAcidPlant", Installed = Prefix + "Installed";
    public const string StockTrigger = Prefix + "TStock", Record = "ManufacturingAcidPlant";
    public const string OxygenPort = "PhobosManufacturing.AcidPlantLink.oxygen", WaterPort = "PhobosManufacturing.AcidPlantLink.water",
        AcidPort = "PhobosManufacturing.AcidPlantLink.acid", VesselPort = "PhobosManufacturing.AcidPlantIn";
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static readonly string[] StockFeed = { Materials.SulfideNodule };
}

/// <summary>The SA-3 catalog: the <c>acid-plant</c> recipes of the shared charge catalog.</summary>
public static class AcidPlantRecipes
{
    public const string Machine = ChargeCatalog.AcidPlant;
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe Roast => View.ById("sulfide-roast")!;
    public static ChargeRecipe? Match(IEnumerable<string> feedIds) => View.Match(feedIds, _ => false);
}
