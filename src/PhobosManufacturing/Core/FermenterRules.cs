using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Alembrine Copperhead-3 Fermenter-Still (Manufacturing 0.39.0; owner decisions of 4 October 2026): a 3 x 3
/// fermenter and pot still on the shared charge engine. It takes Phobos Agriculture's sugar beets as mash or its beet sugar
/// as a wash, ferments the sucrose (C12H22O11 + H2O -> 4 C2H5OH + 4 CO2) and distils the ethanol into a linked ethanol cask;
/// the carbon dioxide goes to a linked carbon dioxide store and the stillage water to a linked water vessel, the spent mash
/// to its tray. A working still is an ignition source for ethanol spills in its room (owner decision). Identities, ports
/// and records stay here; shape and recipes are in the packs.</summary>
public static class FermenterRules
{
    public const string Prefix = "PhobosFermenterStill", Installed = Prefix + "Installed";
    public const string StockTrigger = Prefix + "TStock", Record = "ManufacturingFermenter";
    public const string WaterPort = "PhobosManufacturing.FermenterLink.water", CarbonDioxidePort = "PhobosManufacturing.FermenterLink.co2",
        EthanolPort = "PhobosManufacturing.FermenterLink.ethanol", VesselPort = "PhobosManufacturing.FermenterIn";
    /// <summary>Phobos Agriculture's sugar crops (0.46.0), named as strings only: a 0.5 kg beet and a 70 g sugar packet.</summary>
    public const string Beet = "PhobosVerdemorrowSugarBeets", Sugar = "PhobosVerdemorrowBeetSugar";
    public const double BeetKg = 0.5, SugarKg = 0.07;
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public static readonly string[] StockFeed = { Beet, Sugar };
    /// <summary>The share of the sucrose that ferments to ethanol and carbon dioxide (authored, within the 90 to 95% of
    /// theoretical industrial fermentations reach); the rest goes to yeast growth and by-products in the spent mash.</summary>
    public const double FermentedShare = 0.92;
}

/// <summary>The Copperhead-3 catalog: the <c>fermenter</c> recipes of the shared charge catalog.</summary>
public static class FermenterRecipes
{
    public const string Machine = ChargeCatalog.Fermenter;
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe BeetMash => View.ById("beet-mash")!;
    public static ChargeRecipe SugarWash => View.ById("sugar-wash")!;
    public static Func<string, bool> Met(bool sugarCrops) => key => key == ChargeCatalog.SugarCropsRequirement && sugarCrops;
    public static ChargeRecipe? Match(IEnumerable<string> feedIds, bool sugarCrops) => View.Match(feedIds, Met(sugarCrops));
    public static IEnumerable<ChargeRecipe> Available(bool sugarCrops) => View.Available(Met(sugarCrops));
}
