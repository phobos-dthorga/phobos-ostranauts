using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>The V4 Volatiles Refinery: a 4 x 4 electric hearth and drying retort that takes one exact charge from
/// its feed bin at a time (the F6's charge model, so a two-input carburising charge is possible), pays a share
/// of its electricity into the room, and delivers solids to its tray and water to a linked vessel.</summary>
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
    public const int Footprint = 4, FeedCapacity = 6;
    public const double MachineKg = 180, Price = 64000;
    public const double WorkingKW = 24, IdleKW = 0.1, RoomHeatFraction = 0.15;
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
    /// <summary>The unit mass a feed identity must carry, or null when it is not feed.</summary>
    public static double? FeedKg(string? id, bool steelStock) => id switch
    {
        Hydrates => HydratesKg, Iron => IronKg, Carbides => CarbidesKg, Materials.ClayHydrates => Materials.ClayKg, Materials.AmmoniumSaltCrust => Materials.CrustKg,
        Materials.NickelIronIngot when steelStock => Materials.IngotKg, Materials.CarbonStock when steelStock => Materials.CarbonKg, _ => null
    };
    /// <summary>Feed identities the bin admits at the game level beyond the native TIsOre rule: our own stock.</summary>
    public static readonly string[] StockFeed = { Materials.NickelIronIngot, Materials.CarbonStock };
}

/// <summary>One input line of a charge: an exact identity, how many units, and each unit's mass.</summary>
public sealed class ChargeInput
{
    public string Id { get; }
    public int Count { get; }
    public double Kg { get; }
    public ChargeInput(string id, int count, double kg)
    {
        if (string.IsNullOrWhiteSpace(id) || count < 1 || !ManufacturingRules.Finite(kg) || kg <= 0) throw new ArgumentException("Invalid charge input.");
        Id = id; Count = count; Kg = kg;
    }
}

/// <summary>One immutable refinery recipe: its exact charge, the products it releases (solids to the tray, water to
/// a vessel), the native gas it breathes into the room, and its powered duration. Charge mass equals product mass
/// plus off-gas mass exactly. The revision is the saved identity of a bound charge.</summary>
public sealed class ChargeRecipe
{
    public string Id { get; }
    public int Revision { get; }
    public IReadOnlyList<ChargeInput> Inputs { get; }
    public IReadOnlyList<ProductSpec> Products { get; }
    /// <summary>Native room species and the kilograms of each released over the whole job.</summary>
    public IReadOnlyDictionary<string, double> OffGas { get; }
    public double Seconds { get; }
    /// <summary>A melt: a long interruption freezes it and the charge is lost to slag.</summary>
    public bool Melt { get; }
    public bool NeedsSteelStock { get; }
    public double ChargeKg => Inputs.Sum(i => i.Count * i.Kg);
    public double OffGasKg => OffGas.Values.Sum();
    public double EnergyKWh => RefineryRules.WorkingKW * Seconds / 3600;
    public bool NeedsVessel => Products.Any(p => p.Id == ManufacturingRules.Water);
    /// <summary>Products that go to a gas store: any Manufacturing gas commodity (ammonia). Never vented.</summary>
    public IReadOnlyList<ProductSpec> StoredGases => Products.Where(p => GasStores.FamilyOf(p.Id) != null).ToArray();
    /// <summary>Products that are items in the tray: everything but water and stored gases.</summary>
    public IEnumerable<ProductSpec> Solids(IEnumerable<ProductSpec> products) => products.Where(p => p.Id != ManufacturingRules.Water && GasStores.FamilyOf(p.Id) == null);
    public int Units => Inputs.Sum(i => i.Count);
    public ChargeRecipe(string id, int revision, IEnumerable<ChargeInput> inputs, IEnumerable<ProductSpec> products, IReadOnlyDictionary<string, double>? offGas, double seconds, bool melt, bool needsSteelStock)
    {
        var ins = inputs.ToArray(); var outs = products.ToArray(); var gas = new Dictionary<string, double>(offGas ?? new Dictionary<string, double>(), StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(id) || revision < 1 || ins.Length == 0 || outs.Length == 0 || !ManufacturingRules.Finite(seconds) ||
            seconds < ProcessJob.MinSeconds || seconds > ProcessJob.MaxSeconds ||
            ins.Select(i => i.Id).Distinct(StringComparer.Ordinal).Count() != ins.Length ||
            outs.Any(p => p == null || string.IsNullOrWhiteSpace(p.Id) || p.Count <= 0 || !ManufacturingRules.Finite(p.Kg) || p.Kg <= 0) ||
            outs.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != outs.Length ||
            gas.Any(g => !NativeGasCanister.IsRoomSpecies(g.Key) || !ManufacturingRules.Finite(g.Value) || g.Value <= 0))
            throw new ArgumentException("Invalid charge recipe.");
        Inputs = Array.AsReadOnly(ins); Products = Array.AsReadOnly(outs); OffGas = gas; Seconds = seconds; Melt = melt; NeedsSteelStock = needsSteelStock;
        Id = id; Revision = revision;
        if (!ProcessMaterial.Balanced(ChargeKg, Products.Select(p => p.Kg * p.Count).Concat(OffGas.Values))) throw new ArgumentException("Charge recipe does not conserve mass: " + id);
    }
    /// <summary>Off-gas due at a progress fraction, given what the job has already released: never more than the
    /// recipe's share, never negative, and nothing at all for a recipe without off-gas.</summary>
    public double OffGasDueKg(double progressFraction, double emittedKg)
    {
        if (!ManufacturingRules.Finite(progressFraction) || !ManufacturingRules.Finite(emittedKg) || emittedKg < 0) throw new ArgumentException("Invalid off-gas progress.");
        double target = OffGasKg * Math.Max(0, Math.Min(1, progressFraction));
        return Math.Max(0, target - emittedKg);
    }
    /// <summary>Splits an amount of off-gas across the recipe's species in their declared proportions.</summary>
    public IEnumerable<KeyValuePair<string, double>> Split(double kg)
    {
        if (!ManufacturingRules.Finite(kg) || kg < 0) throw new ArgumentException("Invalid off-gas amount.");
        double total = OffGasKg;
        foreach (var pair in OffGas) yield return new KeyValuePair<string, double>(pair.Key, total <= 0 ? 0 : kg * pair.Value / total);
    }
}

/// <summary>The V4 catalog (owner chemistry decisions, 29 September 2026; sources in the refinery design
/// record). Every recipe conserves mass to the gram of its item units; the rounding each accepts is stated
/// beside it.</summary>
public static class RefineryRecipes
{
    // Hydrates: CM-chondrite bound water 9-13 wt% (authored 10%); the dehydrated silicate/oxide is native gangue.
    public static readonly ChargeRecipe Hydrates = new("hydrates", 1,
        new[] { new ChargeInput(RefineryRules.Hydrates, 1, RefineryRules.HydratesKg) },
        new[] { new ProductSpec(ManufacturingRules.Water, 1, 1), new ProductSpec(RefineryRules.Gangue, 3, RefineryRules.GangueKg) }, null, 600, false, false);
    // Clay hydrates: CI-chondrite phyllosilicate water 18-22 wt% (authored 20%); 8 kg of anhydrous residue is no gangue multiple.
    public static readonly ChargeRecipe Clay = new("clay", 2,
        new[] { new ChargeInput(Materials.ClayHydrates, 1, Materials.ClayKg) },
        new[] { new ProductSpec(ManufacturingRules.Water, 1, 2), new ProductSpec(Materials.AnhydrousResidue, 1, Materials.ResidueKg) }, null, 900, false, false);
    // Carbon/carbides ("often hydrated"): pyrolysis; authored 50% carbon, 10% water, 30% silicate, 10% gas (CO2, CO, smoke).
    public static readonly IReadOnlyDictionary<string, double> CarbonOffGas = new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = .6, ["CO"] = .3, ["Smoke"] = .1 };
    public static readonly ChargeRecipe Carbon = new("carbon", 3,
        new[] { new ChargeInput(RefineryRules.Carbides, 1, RefineryRules.CarbidesKg) },
        new[] { new ProductSpec(Materials.CarbonStock, 5, Materials.CarbonKg), new ProductSpec(ManufacturingRules.Water, 1, 1), new ProductSpec(RefineryRules.Gangue, 1, RefineryRules.GangueKg) },
        CarbonOffGas, 1800, false, false);
    // Meteoric iron: Fe-Ni metal (kamacite/taenite) with adhering rock and troilite/schreibersite/oxide inclusions;
    // authored 80% metal, 15% rock (native gangue), 5% slag. Cast as it comes: nickel-iron, not steel.
    public static readonly ChargeRecipe NickelIron = new("nickel-iron", 4,
        new[] { new ChargeInput(RefineryRules.Iron, 1, RefineryRules.IronKg) },
        new[] { new ProductSpec(Materials.NickelIronIngot, 4, Materials.IngotKg), new ProductSpec(RefineryRules.Gangue, 1, RefineryRules.GangueKg), new ProductSpec(Materials.RefinerySlag, 1, Materials.SlagKg) },
        null, 2400, true, false);
    // Carburised (nickel) steel: four nickel-iron ingots melted with one kilogram of carbon in excess; the steel
    // takes 0.2-2 wt% carbon within its 4 kg unit, the unreacted carbon and oxide leave in Shipbreaker's remainder.
    public static readonly ChargeRecipe Steel = new("steel", 5,
        new[] { new ChargeInput(Materials.NickelIronIngot, 4, Materials.IngotKg), new ChargeInput(Materials.CarbonStock, 1, Materials.CarbonKg) },
        new[] { new ProductSpec(RefineryRules.SteelIngot, 4, RefineryRules.SteelIngotKg), new ProductSpec(RefineryRules.SteelRemainder, 1, RefineryRules.SteelRemainderKg) },
        null, 2000, true, true);
    // Ammonium salt crust (Manufacturing 0.9.0): ammonium chloride with its own sodium carbonate, the bright-area salts NASA's
    // Dawn mission found on Ceres (Raponi et al. 2019; De Sanctis et al. 2024). Heated: 2 NH4Cl + Na2CO3 -> 2 NH3 + CO2 + H2O
    // + 2 NaCl. Authored crust: 3.000 kg NH4Cl (56.08 mol), 2.972 kg Na2CO3 (stoichiometric), 4.028 kg clay and hydrohalite.
    // Products: 0.955 kg NH3 to an ammonia store (never vented), 1.235 kg CO2 breathed into the room as the carbon charge does,
    // 0.505 kg water to the vessel, and a 7.305 kg cake of 3.278 kg NaCl plus the remainder. Rounded to the gram.
    // Energy: about +210 kJ per mole of reaction from standard enthalpies of formation, 1.6 kWh, plus 0.8 kWh to heat the
    // crust; 900 s at 24 kW is 6 kWh.
    public const double CrustAmmoniaKg = 0.955, CrustCarbonDioxideKg = 1.235, CrustWaterKg = 0.505;
    public static readonly IReadOnlyDictionary<string, double> CrustOffGas = new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = CrustCarbonDioxideKg };
    public static readonly ChargeRecipe Ammonium = new("ammonium", 6,
        new[] { new ChargeInput(Materials.AmmoniumSaltCrust, 1, Materials.CrustKg) },
        new[] { new ProductSpec(Materials.SpentSaltCake, 1, Materials.SaltCakeKg), new ProductSpec(ManufacturingRules.Water, 1, CrustWaterKg), new ProductSpec(ManufacturingRules.Ammonia, 1, CrustAmmoniaKg) },
        CrustOffGas, 900, false, false);
    public static readonly IReadOnlyList<ChargeRecipe> All = Array.AsReadOnly(new[] { Hydrates, Clay, Carbon, NickelIron, Steel, Ammonium });
    public static ChargeRecipe? ByRevision(int revision) => All.FirstOrDefault(r => r.Revision == revision);
    public static ChargeRecipe? ById(string? id) => id == null ? null : All.FirstOrDefault(r => r.Id == id);
    /// <summary>Recipes available now: the steel recipe needs Shipbreaker's stock identities.</summary>
    public static IEnumerable<ChargeRecipe> Available(bool steelStock) => All.Where(r => steelStock || !r.NeedsSteelStock);
    /// <summary>The recipe whose whole charge is present among the feed identities, preferring the largest charge,
    /// or null. The caller binds the exact units.</summary>
    public static ChargeRecipe? Match(IEnumerable<string> feedIds, bool steelStock)
    {
        var counts = feedIds.GroupBy(id => id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        return Available(steelStock).OrderByDescending(r => r.Units).ThenBy(r => r.Revision)
            .FirstOrDefault(r => r.Inputs.All(i => counts.TryGetValue(i.Id, out int n) && n >= i.Count));
    }
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
