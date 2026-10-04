using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Oxsmith EC-4 Electrolysis Cell (Manufacturing 0.52.0; owner decisions of 5 October 2026, set 4 of the
/// regolith programme): a 4 x 4 molten-rock electrolysis cell on the shared charge engine. One lump of the game's loose
/// regolith or one chunk of its Silicates ore at a time: the cell melts it and splits its iron and silicon oxides with
/// current into oxygen for a linked oxygen store and ferrosilicon for the tray, leaving slag. A lump's bound water goes
/// to a linked water vessel. The splitting absorbs most of what the cell draws; the rest warms the room. Identities,
/// ports and records stay here; shape and recipes are in the packs.</summary>
public static class ElectrolysisRules
{
    public const string Prefix = "PhobosElectrolysisCell", Installed = Prefix + "Installed";
    public const string StockTrigger = Prefix + "TStock", Record = "ManufacturingElectrolysisCell";
    public const string OxygenPort = "PhobosManufacturing.CellLink.oxygen", WaterPort = "PhobosManufacturing.CellLink.water", VesselPort = "PhobosManufacturing.CellIn";
    /// <summary>The game's own Silicates ore (10 kg), the cell's second feed; loose regolith is <see cref="RefineryRules.Regolith"/>.</summary>
    public const string Silicates = "ItmMineral04";
    public const double SilicatesKg = 10;
    /// <summary>Native conditions the feed admits beside the ore rule: loose regolith is a mineral, not an ore, in the
    /// game's data, and carries this condition alone. The recipe rule then admits only a lump or a Silicates chunk.</summary>
    public static readonly string[] FeedConditions = { RefineryRules.RegolithCondition };
    /// <summary>What one 2.2 kg unit of ferrosilicon is (authored): iron and silicon, 35.7 percent silicon by mass.</summary>
    public const double FerrosiliconIronKg = 1.414, FerrosiliconSiliconKg = 0.786;
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
}

/// <summary>The EC-4 catalog: the <c>electrolysis-cell</c> recipes of the shared charge catalog.</summary>
public static class ElectrolysisRecipes
{
    public const string Machine = ChargeCatalog.ElectrolysisCell;
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe Regolith => View.ById("regolith-electrolysis")!;
    public static ChargeRecipe Silicates => View.ById("silicates-electrolysis")!;
    public static ChargeRecipe? Match(IEnumerable<string> feedIds) => View.Match(feedIds, _ => false);
}
