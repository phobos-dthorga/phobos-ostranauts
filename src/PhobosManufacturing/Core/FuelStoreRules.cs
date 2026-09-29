using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Liquids;
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

/// <summary>One pressurised fuel store family: a Framework bulk vessel with the Leak damage policy, its
/// combustion stoichiometry and heating value, and where a leak goes (space, or the room as a native species).
/// Text keys specific to the fuel live under <see cref="TextPrefix"/>; shared ones under "Store".</summary>
public sealed class FuelStore
{
    public string Prefix { get; }
    public string Installed => Prefix + "Installed";
    public string Commodity { get; }
    public string TextPrefix { get; }
    public double CapacityKg { get; }
    public double DryKg { get; }
    public double Price { get; }
    public double LeakKgPerHour { get; }
    /// <summary>Higher heating value per kilogram of fuel burned (liquid water formed).</summary>
    public double HeatingKJPerKg { get; }
    public double OxygenPerFuel { get; }
    /// <summary>Native room species formed per kilogram of fuel burned.</summary>
    public IReadOnlyDictionary<string, double> RoomProductsPerKg { get; }
    /// <summary>The native species a damaged store leaks into its room, or null when the leak goes to space.</summary>
    public string? LeakSpecies { get; }
    public BulkVesselSpec Spec { get; }
    public FuelStore(string prefix, string commodity, string textPrefix, double capacityKg, double dryKg, double price, double leakKgPerHour,
        double heatingKJPerKg, double oxygenPerFuel, IReadOnlyDictionary<string, double> roomProductsPerKg, string? leakSpecies,
        string record, string journal, string guard)
    {
        if (!(heatingKJPerKg > 0) || !(oxygenPerFuel > 0) || roomProductsPerKg.Values.Any(v => !ManufacturingRules.Finite(v) || v < 0))
            throw new ArgumentException("Invalid fuel store.");
        Prefix = prefix; Commodity = commodity; TextPrefix = textPrefix; CapacityKg = capacityKg; DryKg = dryKg; Price = price; LeakKgPerHour = leakKgPerHour;
        HeatingKJPerKg = heatingKJPerKg; OxygenPerFuel = oxygenPerFuel; RoomProductsPerKg = roomProductsPerKg; LeakSpecies = leakSpecies;
        Spec = new BulkVesselSpec(prefix, commodity, capacityKg, dryKg, ManufacturingRules.Owner, record, journal, guard, VesselDamagePolicy.Leak, leakKgPerHour);
    }
    public bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    public Deflagration Burn(double fuelKg, double oxygenAvailableKg)
    {
        if (!ManufacturingRules.Finite(fuelKg) || !ManufacturingRules.Finite(oxygenAvailableKg) || fuelKg < 0 || oxygenAvailableKg < 0) throw new ArgumentException("Invalid deflagration inputs.");
        double burned = Math.Min(fuelKg, oxygenAvailableKg / OxygenPerFuel), energy = burned * HeatingKJPerKg;
        return new Deflagration(burned, burned * OxygenPerFuel, energy, fuelKg - burned, FuelStores.SizeFor(energy),
            RoomProductsPerKg.ToDictionary(p => p.Key, p => p.Value * burned, StringComparer.Ordinal));
    }
}

/// <summary>The two fuel stores and the rules they share: when a release ignites, and how big the blast is.</summary>
public static class FuelStores
{
    /// <summary>Oxygen partial pressure below which stored fuel will not deflagrate in this model (authored).</summary>
    public const double IgnitionOxygenKPa = 5;
    /// <summary>Blast sizes by energy released: the energy of 2 kg and 8 kg of hydrogen.</summary>
    public const double SmallKJ = 2 * HydrogenRules.HHVKJPerKg, MediumKJ = 8 * HydrogenRules.HHVKJPerKg;
    public const string DeflagrationPrefix = "SysPhobosDeflagration";
    public static FuelStore Hydrogen => HydrogenRules.Store;
    public static FuelStore Methane => MethaneRules.Store;
    public static IReadOnlyList<FuelStore> All => new[] { Hydrogen, Methane };
    public static FuelStore? For(string? definition) => All.FirstOrDefault(f => f.IsFamily(definition));
    public static bool IsFamily(string? definition) => For(definition) != null;
    public static string SizeFor(double energyKJ) => energyKJ < SmallKJ ? "Small" : energyKJ < MediumKJ ? "Medium" : "Large";
    /// <summary>Whether a release ignites: enough oxygen and something to light it (a fire in the room, a working
    /// refinery hearth, or a powered device sparking at the game's own half-damage rule).</summary>
    public static HydrogenFate Fate(double oxygenKPa, bool fireInRoom, bool hearthWorking, bool sparkingDevice) =>
        ManufacturingRules.Finite(oxygenKPa) && oxygenKPa >= IgnitionOxygenKPa && (fireInRoom || hearthWorking || sparkingDevice) ? HydrogenFate.Deflagrate : HydrogenFate.Leak;
    public static string DeflagrationDefinition(Deflagration d) => DeflagrationPrefix + d.Size;
}

/// <summary>The M2 Methane Store: a 2 x 2 pressurised vessel for Sabatier methane, kept as a kilogram record
/// until a consumer exists (owner decision, 29 September 2026). Methane is a native gas: a damaged store leaks
/// it into the room, and with oxygen and an ignition source the contents burn, CH4 + 2 O2 -> CO2 + 2 H2O.</summary>
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
    internal static double KgPerMol(string species) => Phobos.Ostranauts.Framework.Processing.NativeGasCanister.KgPerMol[species];
    public static readonly FuelStore Store = new(Prefix, ManufacturingRules.Methane, "Methane", CapacityKg, DryKg, Price, LeakKgPerHour, HHVKJPerKg, OxygenPerMethane,
        new Dictionary<string, double>(StringComparer.Ordinal) { ["CO2"] = CarbonDioxidePerMethane }, "CH4", Record, Journal, Guard);
    public static bool IsFamily(string? id) => Store.IsFamily(id);
}
