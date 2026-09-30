using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The Lixivar LC-3 Leach and Crystallise Unit (feedstock round three, Manufacturing 0.18.0): a 3 x 3
/// hydrometallurgy machine on the shared charge engine. The crew selects its recipe (evaporite leach, struvite,
/// makeup formulation, and from 0.20.0 Epsom salt from olivine, acid-route struvite and the complete crop nutrient
/// formulation); water circulates through, and is drawn from, a linked water vessel within one tile, the struvite
/// steps draw ammonia from a linked ammonia store, the acid steps draw from a linked acid tank and the complete
/// formulation deposits into a linked Agriculture hopper. Its shape is in the <c>equipment</c> pack, its recipes in
/// the <c>process-recipes</c> pack under machine <c>leach</c>. Identities, ports and records stay here.</summary>
public static class LeachRules
{
    public const string Prefix = "PhobosLeachUnit", Installed = Prefix + "Installed";
    public const string StockTrigger = Prefix + "TStock", Record = "ManufacturingLeach";
    /// <summary>The LC-3's own link ports: pairing is one-to-one per port id, so a vessel can serve a V4 and an LC-3
    /// at once through different ports.</summary>
    public const string WaterPort = "PhobosManufacturing.LeachLink.water", AmmoniaPort = "PhobosManufacturing.LeachLink.ammonia", VesselPort = "PhobosManufacturing.LeachIn";
    /// <summary>The acid and hopper links (Manufacturing 0.20.0), on the same vessel-side port.</summary>
    public const string AcidPort = "PhobosManufacturing.LeachLink.acid", NutrientPort = "PhobosManufacturing.LeachLink.nutrients";
    /// <summary>The game's own olivine ore (10 kg, 180 cr), the Epsom salt charge's feed.</summary>
    public const string Olivine = "ItmMineral02";
    /// <summary>The formulation's product: Agriculture's Verdemorrow Groundwork makeup packet, named as a string only.</summary>
    public const string MakeupPacket = "PhobosVerdemorrowGroundworkMakeup";
    public const double MakeupPacketKg = .04;
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>Own identities the feed admits at the game level, beside the game's ore rule (for the olivine); the
    /// container rule then admits only the selected recipe's exact feed.</summary>
    public static readonly string[] StockFeed = { Materials.EvaporiteCrust, Materials.PhosphateConcentrate, Materials.PotassiumSulfate, Materials.Struvite,
        Materials.PhosphoricAcidFlask, Materials.EpsomSalt, Materials.AmmoniumSulfate };
    /// <summary>Every item identity the LC-3's recipes take: its own stock and the game's olivine.</summary>
    public static IEnumerable<string> FeedIds => StockFeed.Concat(new[] { Olivine });
}

/// <summary>The LC-3 catalog: the <c>leach</c> recipes of the shared charge catalog, with the named charges the
/// checks and the panel describe.</summary>
public static class LeachRecipes
{
    public const string Machine = ChargeCatalog.Leach, MakeupRequirement = ChargeCatalog.MakeupRequirement, CropNutrientsRequirement = ChargeCatalog.CropNutrientsRequirement;
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    /// <summary>The LC-3's requirement gate: the makeup formulation needs Agriculture's packet identity, the complete
    /// formulation Agriculture's hoppers (0.27.0).</summary>
    public static Func<string, bool> Met(bool agriculture, bool hoppers = false) => key => IsMet(key, agriculture, hoppers);
    public static bool IsMet(string key, bool agriculture, bool hoppers) => key == MakeupRequirement && agriculture || key == CropNutrientsRequirement && hoppers;
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe Leach => ById("evaporite-leach")!;
    public static ChargeRecipe Struvite => ById("struvite")!;
    public static ChargeRecipe Makeup => ById("makeup-formulation")!;
    public static ChargeRecipe Epsom => ById("olivine-epsom")!;
    public static ChargeRecipe AcidStruvite => ById("struvite-acid")!;
    public static ChargeRecipe CropNutrients => ById("crop-nutrients")!;
    public static ChargeRecipe? ByRevision(int revision) => View.ByRevision(revision);
    public static ChargeRecipe? ById(string? id) => View.ById(id);
    public static IEnumerable<ChargeRecipe> Available(bool agriculture, bool hoppers = false) => View.Available(Met(agriculture, hoppers));
    /// <summary>The unit mass a feed identity must carry for the selected recipe, or null when it is not that recipe's feed.</summary>
    public static double? FeedKg(string? id, ChargeRecipe selected, bool agriculture, bool hoppers = false) => View.FeedKg(id, Met(agriculture, hoppers), selected);
}
