using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The V4 Volatiles Refinery: a 4 x 4 electric hearth and drying retort that takes one exact charge from
/// its feed bin at a time (the F6's charge model, so a two-input carburising charge is possible), pays a share
/// of its electricity into the room, and delivers solids to its tray and water to a linked vessel. It runs on the
/// shared charge machine (Manufacturing 0.17.0); its shape is in the <c>equipment</c> pack, its recipes in the
/// <c>process-recipes</c> pack under machine <c>refinery</c>. Identities, ports and records stay here.</summary>
public static class RefineryRules
{
    public const string Prefix = "PhobosVolatilesRefinery", Installed = Prefix + "Installed";
    public const string InputBin = Prefix + "InputBin", InputSlot = Prefix + "Input", FeedTrigger = Prefix + "TFeed", StockTrigger = "PhobosManufacturingTStock";
    public const string Record = "ManufacturingRefinery";
    public const string OutPort = "PhobosManufacturing.RefineryOut", VesselPort = "PhobosManufacturing.VesselIn";
    /// <summary>Stored gases (Manufacturing 0.9.0): one V4 outlet per gas family, pairing with this port on a store of that gas.</summary>
    public const string GasInPort = "PhobosManufacturing.RefineryGasIn";
    public static string GasOutPort(GasFamily family) => "PhobosManufacturing.RefineryGasOut." + family.SmallPrefix;
    /// <summary>The gas families any charge keeps in a store, for the V4's links.</summary>
    public static IEnumerable<GasFamily> StoredGasFamilies => RefineryRecipes.All.SelectMany(r => r.StoredGases).Select(p => GasStores.FamilyOf(p.Id)!).Distinct();
    /// <summary>Native feed identities and their masses (items_mining.json): hydrates 10 kg, meteoric iron 20 kg,
    /// carbon/carbides 10 kg, gangue 3 kg. Steel identities are Shipbreaker's, named as strings only.</summary>
    public const string Hydrates = "ItmMineral11", Iron = "ItmMineral01", Carbides = "ItmMineral03", Gangue = "ItmMiningTrash";
    public const string SteelIngot = "PhobosSteelIngot", SteelRemainder = "PhobosSteelMeltRemainder";
    public const double HydratesKg = 10, IronKg = 20, CarbidesKg = 10, GangueKg = 3, SteelIngotKg = 4, SteelRemainderKg = 1;
    private static EquipmentEntry Shape => Equipment.Entry(Prefix);
    public static int Footprint => Shape.footprint;
    public static int FeedCapacity => Shape.feedCells;
    public static double MachineKg => Shape.massKg;
    public static double WorkingKW => Shape.workingKW;
    public static double IdleKW => Shape.idleKW;
    public static double RoomHeatFraction => Shape.roomHeatFraction;
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>Electricity that warms the room: the authored fraction while working, all of the idle draw.</summary>
    public static double RoomHeatKW(bool working) => working ? WorkingKW * RoomHeatFraction : IdleKW;
    /// <summary>Whether one loose unit may enter the feed: an exact feed identity at its unit mass, detached,
    /// empty and unstacked. Wear is ignored: the game's ores spawn with random damage. Steel-bound identities
    /// enter only while Shipbreaker's stock exists.</summary>
    public static bool ValidFeed(string? id, double kg, bool detached, bool empty, bool unstacked, bool steelStock)
    {
        if (!detached || !empty || !unstacked || id == null) return false;
        double? unit = FeedKg(id, steelStock);
        return unit.HasValue && ProcessMaterial.MassMatches(kg, unit.Value);
    }
    /// <summary>The unit mass a feed identity must carry, or null when it is not feed: from the available recipes.</summary>
    public static double? FeedKg(string? id, bool steelStock) => ChargeCatalog.For(ChargeCatalog.Refinery).FeedKg(id, RefineryRecipes.Met(steelStock));
    /// <summary>Feed identities the bin admits at the game level beyond the native TIsOre rule: our own stock.</summary>
    public static readonly string[] StockFeed = { Materials.NickelIronIngot, Materials.CarbonStock, Materials.LeachedResidue };
}

/// <summary>The V4 catalog (owner chemistry decisions, 29 September 2026; sources in the refinery design record): the
/// <c>refinery</c> recipes of the shared charge catalog. Every recipe conserves mass to the gram of its item units;
/// the loader enforces that and the game's gas species on every file, and the frozen-revision file keeps every
/// published revision exactly as a saved charge expects.</summary>
public static class RefineryRecipes
{
    public const string Schema = ChargeCatalog.Schema, Resource = ChargeCatalog.Resource, FrozenResource = ChargeCatalog.FrozenResource;
    public const string Machine = ChargeCatalog.Refinery, SteelStockRequirement = ChargeCatalog.SteelStockRequirement;
    public static RecipePack Pack => ChargeCatalog.Pack;
    public static DataPackSource Source => ChargeCatalog.Source;
    public static RecipePack Load(Func<string, double?>? nativeMass = null) => ChargeCatalog.Load(nativeMass);
    private static ChargeRecipeView View => ChargeCatalog.For(Machine);
    /// <summary>The V4's requirement gate: the steel recipe needs Shipbreaker's stock identities.</summary>
    public static Func<string, bool> Met(bool steelStock) => key => key == SteelStockRequirement && steelStock;
    /// <summary>Every charge, by revision.</summary>
    public static IReadOnlyList<ChargeRecipe> All => View.All;
    public static ChargeRecipe Hydrates => ById("hydrates")!;
    public static ChargeRecipe Clay => ById("clay")!;
    public static ChargeRecipe Carbon => ById("carbon")!;
    public static ChargeRecipe NickelIron => ById("nickel-iron")!;
    /// <summary>The plain steel charge (revision 5), superseded for new charges by <see cref="NickelSteel"/>; kept so a
    /// charge bound to it settles.</summary>
    public static ChargeRecipe Steel => ById("steel")!;
    /// <summary>Nickel steel (revision 8, Manufacturing 0.26.0): the mined iron chain's own end product, needing no other mod.</summary>
    public static ChargeRecipe NickelSteel => ById("nickel-steel")!;
    public static ChargeRecipe Ammonium => ById("ammonium")!;
    /// <summary>Calcining the LC-3's leached residue: its magnesite's CO2 to a linked carbon dioxide store (Manufacturing 0.18.0).</summary>
    public static ChargeRecipe Calcine => ById("calcine")!;
    /// <summary>The salt crust's ammonia yield, as the pack states it (0.955 kg on 56.08 mol of ammonium chloride).</summary>
    public static double CrustAmmoniaKg => Ammonium.Products.Single(p => p.Id == ManufacturingRules.Ammonia).Kg;
    public static ChargeRecipe? ByRevision(int revision) => View.ByRevision(revision);
    public static ChargeRecipe? ById(string? id) => View.ById(id);
    /// <summary>Recipes available now, less superseded ones: the plain steel recipe needs Shipbreaker's stock identities and
    /// is superseded by nickel steel, so new charges never bind it.</summary>
    public static IEnumerable<ChargeRecipe> Available(bool steelStock) => View.Available(Met(steelStock));
    /// <summary>The recipe whose whole charge is present among the feed identities, preferring the largest charge,
    /// or null. The caller binds the exact units.</summary>
    public static ChargeRecipe? Match(IEnumerable<string> feedIds, bool steelStock) => View.Match(feedIds, Met(steelStock));
    /// <summary>Whether a saved charge froze: a melt interrupted by heat waits for longer than its own duration.
    /// A drying or roasting charge simply resumes.</summary>
    public static bool Spoiled(ChargeRecipe recipe, double waitSeconds) =>
        recipe.Melt && ManufacturingRules.Finite(waitSeconds) && waitSeconds > recipe.Seconds;
    /// <summary>What a frozen melt yields instead: the native rock share stays gangue, every other kilogram is
    /// refinery slag in one-kilogram units. Mass is conserved; nothing is refined.</summary>
    public static IReadOnlyList<ProductSpec> SpoiledProducts(ChargeRecipe recipe)
    {
        if (!recipe.Melt) throw new ArgumentException("Only a melt can spoil.");
        var gangue = recipe.Products.FirstOrDefault(p => p.Id == RefineryRules.Gangue);
        double slagKg = recipe.ChargeKg - (gangue == null ? 0 : gangue.Count * gangue.Kg) - recipe.OffGasKg;
        int units = (int)Math.Round(slagKg / Materials.SlagKg);
        if (Math.Abs(units * Materials.SlagKg - slagKg) > 1e-9 || units < 1) throw new InvalidOperationException("Slag does not divide the charge.");
        var list = new List<ProductSpec> { new(Materials.RefinerySlag, units, Materials.SlagKg) };
        if (gangue != null) list.Add(gangue);
        return list.AsReadOnly();
    }
}
