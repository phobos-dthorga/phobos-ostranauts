using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Processing;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>What a damaged fuel store does with its contents, decided once from the room around it.</summary>
public enum HydrogenFate { Leak, Deflagrate }

/// <summary>The outcome of a stored fuel burning in a room's air, bounded by whichever of fuel and oxygen runs out
/// first. Products with a game species (carbon dioxide from methane) go into the room; water vapour has no species
/// and leaves with the blast.</summary>
public sealed class Deflagration
{
    public double BurnedKg { get; }
    public double OxygenKg { get; }
    public double EnergyKJ { get; }
    public double LostKg { get; }
    public string Size { get; }
    public IReadOnlyDictionary<string, double> RoomProductsKg { get; }
    internal Deflagration(double burnedKg, double oxygenKg, double energyKJ, double lostKg, string size, IReadOnlyDictionary<string, double> products)
    { BurnedKg = burnedKg; OxygenKg = oxygenKg; EnergyKJ = energyKJ; LostKg = lostKg; Size = size; RoomProductsKg = products; }
}

/// <summary>How a fuel gas burns: higher heating value, oxygen used and native room products per kilogram.</summary>
public sealed class Combustion
{
    public double HeatingKJPerKg { get; }
    public double OxygenPerFuel { get; }
    public IReadOnlyDictionary<string, double> RoomProductsPerKg { get; }
    public Combustion(double heatingKJPerKg, double oxygenPerFuel, IReadOnlyDictionary<string, double> roomProductsPerKg)
    {
        if (!(heatingKJPerKg > 0) || !(oxygenPerFuel > 0) || roomProductsPerKg.Values.Any(v => !ManufacturingRules.Finite(v) || v < 0)) throw new ArgumentException("Invalid combustion.");
        HeatingKJPerKg = heatingKJPerKg; OxygenPerFuel = oxygenPerFuel; RoomProductsPerKg = roomProductsPerKg;
    }
    /// <summary>A burn of <paramref name="fuelKg"/> limited by the oxygen the room holds: what burns, the oxygen it takes, the
    /// heat, what is left unburned, the blast size and the native room products. Shared by the gas stores and, since
    /// Manufacturing 0.38.0, the ethanol tanks and line.</summary>
    public Deflagration Burn(double fuelKg, double oxygenAvailableKg)
    {
        if (!ManufacturingRules.Finite(fuelKg) || !ManufacturingRules.Finite(oxygenAvailableKg) || fuelKg < 0 || oxygenAvailableKg < 0) throw new ArgumentException("Invalid deflagration inputs.");
        double burned = Math.Min(fuelKg, oxygenAvailableKg / OxygenPerFuel), energy = burned * HeatingKJPerKg;
        return new Deflagration(burned, burned * OxygenPerFuel, energy, fuelKg - burned, GasStores.SizeFor(energy),
            RoomProductsPerKg.ToDictionary(p => p.Key, p => p.Value * burned, StringComparer.Ordinal));
    }
}

/// <summary>One gas store family: the small size's identities and ratings, the gas, and whether it burns. The
/// medium and large sizes follow Framework's shared size ladder (one tile wider per step, capacity with floor area
/// plus 10% per step). Text keys specific to the gas live under <see cref="TextPrefix"/>; shared ones under
/// "Store". Model letters: H hydrogen, M methane, O oxygen, N nitrogen, C carbon dioxide, Q ammonia; digit = footprint.</summary>
public sealed class GasFamily
{
    public string SmallPrefix { get; }
    public string Commodity { get; }
    /// <summary>The gas's formula, for its RCS worth and molar mass (H2, CH4, O2, N2, CO2).</summary>
    public string Species { get; }
    public string TextPrefix { get; }
    public string Model { get; }
    public int SmallFootprint => 2;
    /// <summary>The small size's ratings, from the vessels data pack (Manufacturing 0.13.0); larger sizes scale through the ladder.</summary>
    public double SmallCapacityKg => Vessels.Entry(SmallPrefix).capacityKg ?? 0;
    public double SmallDryKg => Vessels.Entry(SmallPrefix).dryKg;
    /// <summary>The small size's price, from the economy data pack; larger sizes scale through the ladder.</summary>
    public double SmallPrice => Economy.Price(SmallPrefix);
    public double SmallLeakKgPerHour => Vessels.Entry(SmallPrefix).leakKgPerHour;
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    public Combustion? Fuel { get; }
    /// <summary>Whether leaks go into the room: only gases the game has a room species for (never hydrogen).</summary>
    public bool LeaksIntoRoom => NativeGasCanister.IsRoomSpecies(Species);
    public IReadOnlyList<GasStore> Sizes { get; }
    public GasFamily(string smallPrefix, string commodity, string species, string textPrefix, string model, string record, string journal, string guard, Combustion? fuel)
    {
        SmallPrefix = smallPrefix; Commodity = commodity; Species = species; TextPrefix = textPrefix; Model = model; Record = record; Journal = journal; Guard = guard; Fuel = fuel;
        Sizes = BulkVesselSizes.All.Select(s => new GasStore(this, s)).ToArray();
    }
    public GasStore Small => Sizes[0];
    public bool InFamily(string? definition) => BulkVesselSizes.InLadder(definition, SmallPrefix);
}

/// <summary>One size of one gas store family: a Framework bulk vessel with the Leak damage policy.</summary>
public sealed class GasStore
{
    public GasFamily Family { get; }
    public VesselSize Size { get; }
    public int Footprint { get; }
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public string Commodity => Family.Commodity;
    public string TextPrefix => Family.TextPrefix;
    public double CapacityKg => Spec.CapacityKg;
    public double DryKg => Spec.DryKg;
    /// <summary>The economy pack's small-store price scaled by floor area to the power 0.6 (Framework ladder).</summary>
    public double Price => BulkVesselSizes.Scale(Family.SmallPrice, BulkVesselSizes.PriceFactor(Family.SmallFootprint, Size));
    public double LeakKgPerHour => Spec.LeakKgPerHour;
    /// <summary>The native species a damaged store leaks into its room, or null when the leak goes to space.</summary>
    public string? LeakSpecies => Family.LeaksIntoRoom ? Family.Species : null;
    public bool IsFuel => Family.Fuel != null;
    private BulkVesselSpec? spec; private VesselPack? specFrom;
    /// <summary>The Framework vessel spec, built from the vessels data pack and rebuilt if that pack is reloaded.</summary>
    public BulkVesselSpec Spec
    {
        get
        {
            var pack = Vessels.Pack;
            if (spec == null || !ReferenceEquals(specFrom, pack))
            {
                // A bigger vessel leaks faster through the same kind of damage: the rate grows with its side.
                double leak = Family.SmallLeakKgPerHour * Footprint / Family.SmallFootprint;
                spec = BulkVesselSizes.Spec(Family.SmallPrefix, Family.SmallFootprint, Size, Family.Commodity, Family.SmallCapacityKg, Family.SmallDryKg, ManufacturingRules.Owner,
                    Family.Record, Family.Journal, Family.Guard, VesselDamagePolicy.Leak, leak);
                specFrom = pack;
            }
            return spec;
        }
    }
    /// <summary>The translation key of this size's name (its own Fennmark model in the naming map).</summary>
    public string NameKey => TextPrefix + ".name" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    /// <summary>The art of this size, by family art name and size.</summary>
    public string ArtSuffix => Size == VesselSize.Small ? "" : Size.ToString();
    internal GasStore(GasFamily family, VesselSize size)
    {
        Family = family; Size = size; Footprint = BulkVesselSizes.Footprint(family.SmallFootprint, size); Prefix = BulkVesselSizes.Prefix(family.SmallPrefix, size);
    }
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>The line port on the neighbouring tile of the store's local +X side, in the middle row (the upper of
    /// the two middle rows for an even footprint), with the index of the footprint tile that shows the pipe joint.</summary>
    public (int X, int Y, int SocketIndex) Outlet
    {
        get
        {
            int row = Footprint % 2 == 0 ? Footprint / 2 - 1 : (Footprint - 1) / 2;
            return (8 * Footprint + 8, Footprint % 2 == 0 ? 8 : 0, row * Footprint + Footprint - 1);
        }
    }
    public Deflagration Burn(double fuelKg, double oxygenAvailableKg) => (Family.Fuel ?? throw new InvalidOperationException("Not a fuel: " + Commodity)).Burn(fuelKg, oxygenAvailableKg);
}

/// <summary>The gas store families, every size of them, and the rules they share: when a released fuel ignites,
/// and how big the blast is.</summary>
public static class GasStores
{
    /// <summary>Oxygen partial pressure below which stored fuel will not deflagrate in this model (authored).</summary>
    public const double IgnitionOxygenKPa = 5;
    /// <summary>Blast sizes by energy released: the energy of 2 kg and 8 kg of hydrogen.</summary>
    public const double SmallKJ = 2 * HydrogenRules.HHVKJPerKg, MediumKJ = 8 * HydrogenRules.HHVKJPerKg;
    public const string DeflagrationPrefix = "SysPhobosDeflagration";
    /// <summary>Oxygen, nitrogen and carbon dioxide stores use the same vessel as the fuel stores: the native RTA canister
    /// volume (0.787 m3) at its rated 41.4 MPa and 293 K holds about 13,400 mol of ideal gas; authored at 80%, about
    /// 10,700 mol (340 kg of oxygen, 300 kg of nitrogen, 470 kg of carbon dioxide), in the same 160 kg housing. The
    /// authored figures live in the vessels data pack (framework/vessels.json).</summary>
    public const double UsableMolesFraction = 0.8;
    public static readonly GasFamily HydrogenFamily = new(HydrogenRules.Prefix, ManufacturingRules.Hydrogen, "H2", "Store", "H", HydrogenRules.Record, HydrogenRules.Journal, HydrogenRules.Guard,
        new Combustion(HydrogenRules.HHVKJPerKg, HydrogenRules.OxygenPerHydrogen, new Dictionary<string, double>(StringComparer.Ordinal)));
    public static readonly GasFamily MethaneFamily = new(MethaneRules.Prefix, ManufacturingRules.Methane, "CH4", "Methane", "M", MethaneRules.Record, MethaneRules.Journal, MethaneRules.Guard,
        new Combustion(MethaneRules.HHVKJPerKg, MethaneRules.OxygenPerMethane, new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = MethaneRules.CarbonDioxidePerMethane }));
    public static readonly GasFamily OxygenFamily = new("PhobosOxygenStore", ManufacturingRules.Oxygen, "O2", "Oxygen", "O",
        "ManufacturingOxygen", "ManufacturingOxygenWork", "ManufacturingOxygenTransfer", null);
    public static readonly GasFamily NitrogenFamily = new("PhobosNitrogenStore", ManufacturingRules.Nitrogen, "N2", "Nitrogen", "N",
        "ManufacturingNitrogen", "ManufacturingNitrogenWork", "ManufacturingNitrogenTransfer", null);
    public static readonly GasFamily CarbonDioxideFamily = new("PhobosCarbonDioxideStore", ManufacturingRules.CarbonDioxide, "CO2", "CarbonDioxide", "C",
        "ManufacturingCarbonDioxide", "ManufacturingCarbonDioxideWork", "ManufacturingCarbonDioxideTransfer", null);
    /// <summary>Ammonia is kept liquefied, as industry keeps it: it condenses at about 0.86 MPa at 20 C, far below
    /// the vessel's rating, and the saturated liquid is 609 kg/m3 at 20 C (Engineering ToolBox tables after NIST).
    /// The same 0.787 m3 vessel at an 80% fill holds 383 kg; authored 380 kg in the vessels data pack. The game treats
    /// the gas as ideal everywhere else; the capacity is the only place the liquid matters. Model letter Q (unused by
    /// every brand). Ammonia is a game gas species (it poisons the crew in bands), so a damaged store leaks into the room.</summary>
    public static readonly GasFamily AmmoniaFamily = new("PhobosAmmoniaStore", ManufacturingRules.Ammonia, "NH3", "Ammonia", "Q",
        "ManufacturingAmmonia", "ManufacturingAmmoniaWork", "ManufacturingAmmoniaTransfer", null);
    public static readonly IReadOnlyList<GasFamily> Families = new[] { HydrogenFamily, MethaneFamily, OxygenFamily, NitrogenFamily, CarbonDioxideFamily, AmmoniaFamily };
    public static readonly IReadOnlyList<GasStore> All = Families.SelectMany(f => f.Sizes).ToArray();
    // One dictionary probe per definition on the hot paths (every powered object, every destroyed object, the
    // two-second world scan), instead of a query over fifteen sizes with string comparisons.
    private static readonly DefinitionIndex<GasStore> index = BuildIndex();
    private static DefinitionIndex<GasStore> BuildIndex() { var i = new DefinitionIndex<GasStore>(); foreach (var store in All) i.Add(store.Prefix, store); return i; }
    /// <summary>The original small stores, by their historic names.</summary>
    public static GasStore Hydrogen => HydrogenFamily.Small;
    public static GasStore Methane => MethaneFamily.Small;
    public static GasStore? For(string? definition) => index.Get(definition);
    public static bool IsFamily(string? definition) => For(definition) != null;
    public static GasFamily? FamilyOf(string? commodity)
    {
        for (int i = 0; i < Families.Count; i++) if (Families[i].Commodity == commodity) return Families[i];
        return null;
    }
    /// <summary>Whether a definition is any size of the store family holding <paramref name="commodity"/>.</summary>
    public static bool Holds(string? definition, string commodity) => For(definition)?.Commodity == commodity;
    public static string SizeFor(double energyKJ) => energyKJ < SmallKJ ? "Small" : energyKJ < MediumKJ ? "Medium" : "Large";
    /// <summary>Whether a release ignites: enough oxygen and something to light it (a fire in the room, a working
    /// refinery hearth, or a powered device sparking at the game's own half-damage rule).</summary>
    public static HydrogenFate Fate(double oxygenKPa, bool fireInRoom, bool hearthWorking, bool sparkingDevice) =>
        ManufacturingRules.Finite(oxygenKPa) && oxygenKPa >= IgnitionOxygenKPa && (fireInRoom || hearthWorking || sparkingDevice) ? HydrogenFate.Deflagrate : HydrogenFate.Leak;
    public static string DeflagrationDefinition(Deflagration d) => DeflagrationPrefix + d.Size;
}

/// <summary>The M2 Methane Store: a 2 x 2 pressurised vessel for Sabatier methane, kept as a kilogram record
/// (owner decision, 29 September 2026). Methane is a native gas: a damaged store leaks it into the room, and with
/// oxygen and an ignition source the contents burn, CH4 + 2 O2 -> CO2 + 2 H2O. Larger sizes are in
/// <see cref="GasStores"/>.</summary>
public static class MethaneRules
{
    public const string Prefix = "PhobosMethaneStore", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingMethane", Journal = "ManufacturingMethaneWork", Guard = "ManufacturingMethaneTransfer";
    public const int Footprint = 2;
    /// <summary>The native RTA canister volume (0.787 m3) at its rated 41.4 MPa holds roughly 200 kg of compressed
    /// methane; authored 160 kg of usable capacity in a 160 kg housing (vessels data pack).</summary>
    public static double CapacityKg => GasStores.MethaneFamily.SmallCapacityKg;
    public static double DryKg => GasStores.MethaneFamily.SmallDryKg;
    public static double LeakKgPerHour => GasStores.MethaneFamily.SmallLeakKgPerHour;
    /// <summary>Methane's higher heating value, 890.6 kJ/mol (NIST Chemistry WebBook), per kilogram.</summary>
    public const double HHVKJPerMol = 890.6;
    public static double HHVKJPerKg => HHVKJPerMol / KgPerMol("CH4");
    public static double OxygenPerMethane => 2 * KgPerMol("O2") / KgPerMol("CH4");
    public static double CarbonDioxidePerMethane => KgPerMol("CO2") / KgPerMol("CH4");
    internal static double KgPerMol(string species) => NativeGasCanister.KgPerMol[species];
    public static GasStore Store => GasStores.Methane;
    public static bool IsFamily(string? id) => Store.IsFamily(id);
}
