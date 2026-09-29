using System;
using System.Collections.Generic;
using System.Linq;
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
}

/// <summary>One gas store family: the small size's identities and ratings, the gas, and whether it burns. The
/// medium and large sizes follow Framework's shared size ladder (one tile wider per step, capacity with floor area
/// plus 10% per step). Text keys specific to the gas live under <see cref="TextPrefix"/>; shared ones under
/// "Store". Model letters: H hydrogen, M methane, O oxygen, N nitrogen, C carbon dioxide; digit = footprint.</summary>
public sealed class GasFamily
{
    public string SmallPrefix { get; }
    public string Commodity { get; }
    /// <summary>The gas's formula, for its RCS worth and molar mass (H2, CH4, O2, N2, CO2).</summary>
    public string Species { get; }
    public string TextPrefix { get; }
    public string Model { get; }
    public int SmallFootprint => 2;
    public double SmallCapacityKg { get; }
    public double SmallDryKg { get; }
    public double SmallPrice { get; }
    public double SmallLeakKgPerHour { get; }
    public string Record { get; }
    public string Journal { get; }
    public string Guard { get; }
    public Combustion? Fuel { get; }
    /// <summary>Whether leaks go into the room: only gases the game has a room species for (never hydrogen).</summary>
    public bool LeaksIntoRoom => NativeGasCanister.IsRoomSpecies(Species);
    public IReadOnlyList<GasStore> Sizes { get; }
    public GasFamily(string smallPrefix, string commodity, string species, string textPrefix, string model, double smallCapacityKg, double smallDryKg, double smallPrice,
        double smallLeakKgPerHour, string record, string journal, string guard, Combustion? fuel)
    {
        SmallPrefix = smallPrefix; Commodity = commodity; Species = species; TextPrefix = textPrefix; Model = model; SmallCapacityKg = smallCapacityKg; SmallDryKg = smallDryKg;
        SmallPrice = smallPrice; SmallLeakKgPerHour = smallLeakKgPerHour; Record = record; Journal = journal; Guard = guard; Fuel = fuel;
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
    public double Price { get; }
    public double LeakKgPerHour => Spec.LeakKgPerHour;
    /// <summary>The native species a damaged store leaks into its room, or null when the leak goes to space.</summary>
    public string? LeakSpecies => Family.LeaksIntoRoom ? Family.Species : null;
    public bool IsFuel => Family.Fuel != null;
    public BulkVesselSpec Spec { get; }
    /// <summary>The translation key of this size's name (its own Fennmark model in the naming map).</summary>
    public string NameKey => TextPrefix + ".name" + (Size == VesselSize.Small ? "" : "_" + Size.ToString().ToLowerInvariant());
    /// <summary>The art of this size, by family art name and size.</summary>
    public string ArtSuffix => Size == VesselSize.Small ? "" : Size.ToString();
    internal GasStore(GasFamily family, VesselSize size)
    {
        Family = family; Size = size; Footprint = BulkVesselSizes.Footprint(family.SmallFootprint, size); Prefix = BulkVesselSizes.Prefix(family.SmallPrefix, size);
        // A bigger vessel leaks faster through the same kind of damage: the rate grows with its side.
        double leak = family.SmallLeakKgPerHour * Footprint / family.SmallFootprint;
        Spec = BulkVesselSizes.Spec(family.SmallPrefix, family.SmallFootprint, size, family.Commodity, family.SmallCapacityKg, family.SmallDryKg, ManufacturingRules.Owner,
            family.Record, family.Journal, family.Guard, VesselDamagePolicy.Leak, leak);
        Price = BulkVesselSizes.Scale(family.SmallPrice, BulkVesselSizes.PriceFactor(family.SmallFootprint, size));
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
    public Deflagration Burn(double fuelKg, double oxygenAvailableKg)
    {
        if (!ManufacturingRules.Finite(fuelKg) || !ManufacturingRules.Finite(oxygenAvailableKg) || fuelKg < 0 || oxygenAvailableKg < 0) throw new ArgumentException("Invalid deflagration inputs.");
        var fuel = Family.Fuel ?? throw new InvalidOperationException("Not a fuel: " + Commodity);
        double burned = Math.Min(fuelKg, oxygenAvailableKg / fuel.OxygenPerFuel), energy = burned * fuel.HeatingKJPerKg;
        return new Deflagration(burned, burned * fuel.OxygenPerFuel, energy, fuelKg - burned, GasStores.SizeFor(energy),
            fuel.RoomProductsPerKg.ToDictionary(p => p.Key, p => p.Value * burned, StringComparer.Ordinal));
    }
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
    /// 10,700 mol (340 kg of oxygen, 300 kg of nitrogen, 470 kg of carbon dioxide), in the same 160 kg housing.</summary>
    public const double UsableMolesFraction = 0.8, InertDryKg = 160;
    public const double OxygenCapacityKg = 340, NitrogenCapacityKg = 300, CarbonDioxideCapacityKg = 470;
    public const double OxygenPrice = 21000, NitrogenPrice = 20000, CarbonDioxidePrice = 20000;
    public static readonly GasFamily HydrogenFamily = new(HydrogenRules.Prefix, ManufacturingRules.Hydrogen, "H2", "Store", "H", HydrogenRules.CapacityKg, HydrogenRules.DryKg,
        HydrogenRules.Price, HydrogenRules.LeakKgPerHour, HydrogenRules.Record, HydrogenRules.Journal, HydrogenRules.Guard,
        new Combustion(HydrogenRules.HHVKJPerKg, HydrogenRules.OxygenPerHydrogen, new Dictionary<string, double>(StringComparer.Ordinal)));
    public static readonly GasFamily MethaneFamily = new(MethaneRules.Prefix, ManufacturingRules.Methane, "CH4", "Methane", "M", MethaneRules.CapacityKg, MethaneRules.DryKg,
        MethaneRules.Price, MethaneRules.LeakKgPerHour, MethaneRules.Record, MethaneRules.Journal, MethaneRules.Guard,
        new Combustion(MethaneRules.HHVKJPerKg, MethaneRules.OxygenPerMethane, new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = MethaneRules.CarbonDioxidePerMethane }));
    public static readonly GasFamily OxygenFamily = new("PhobosOxygenStore", ManufacturingRules.Oxygen, "O2", "Oxygen", "O", OxygenCapacityKg, InertDryKg, OxygenPrice, 2,
        "ManufacturingOxygen", "ManufacturingOxygenWork", "ManufacturingOxygenTransfer", null);
    public static readonly GasFamily NitrogenFamily = new("PhobosNitrogenStore", ManufacturingRules.Nitrogen, "N2", "Nitrogen", "N", NitrogenCapacityKg, InertDryKg, NitrogenPrice, 2,
        "ManufacturingNitrogen", "ManufacturingNitrogenWork", "ManufacturingNitrogenTransfer", null);
    public static readonly GasFamily CarbonDioxideFamily = new("PhobosCarbonDioxideStore", ManufacturingRules.CarbonDioxide, "CO2", "CarbonDioxide", "C", CarbonDioxideCapacityKg, InertDryKg,
        CarbonDioxidePrice, 2, "ManufacturingCarbonDioxide", "ManufacturingCarbonDioxideWork", "ManufacturingCarbonDioxideTransfer", null);
    public static readonly IReadOnlyList<GasFamily> Families = new[] { HydrogenFamily, MethaneFamily, OxygenFamily, NitrogenFamily, CarbonDioxideFamily };
    public static readonly IReadOnlyList<GasStore> All = Families.SelectMany(f => f.Sizes).ToArray();
    /// <summary>The original small stores, by their historic names.</summary>
    public static GasStore Hydrogen => HydrogenFamily.Small;
    public static GasStore Methane => MethaneFamily.Small;
    public static GasStore? For(string? definition) => All.FirstOrDefault(f => f.IsFamily(definition));
    public static bool IsFamily(string? definition) => For(definition) != null;
    public static GasFamily? FamilyOf(string? commodity) => Families.FirstOrDefault(f => f.Commodity == commodity);
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
    /// methane; authored 160 kg of usable capacity in a 160 kg housing.</summary>
    public const double CapacityKg = 160, DryKg = 160, Price = 21000, LeakKgPerHour = 2;
    /// <summary>Methane's higher heating value, 890.6 kJ/mol (NIST Chemistry WebBook), per kilogram.</summary>
    public const double HHVKJPerMol = 890.6;
    public static double HHVKJPerKg => HHVKJPerMol / KgPerMol("CH4");
    public static double OxygenPerMethane => 2 * KgPerMol("O2") / KgPerMol("CH4");
    public static double CarbonDioxidePerMethane => KgPerMol("CO2") / KgPerMol("CH4");
    internal static double KgPerMol(string species) => NativeGasCanister.KgPerMol[species];
    public static GasStore Store => GasStores.Methane;
    public static bool IsFamily(string? id) => Store.IsFamily(id);
}
