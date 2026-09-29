using System;
using Phobos.Ostranauts.Framework.Liquids;
using Phobos.Ostranauts.Framework.Registration;

namespace PhobosManufacturing.Core;

/// <summary>What a damaged hydrogen store does with its contents, decided once from the room around it.</summary>
public enum HydrogenFate { Leak, Deflagrate }

/// <summary>The outcome of hydrogen burning in a room's air: 2 H2 + O2 -> 2 H2O, 8 kg of oxygen per kilogram of
/// hydrogen, bounded by whichever runs out first; the water vapour has no game species and is gone.</summary>
public sealed class Deflagration
{
    public double BurnedKg { get; }
    public double OxygenKg { get; }
    public double EnergyKJ { get; }
    public double LostKg { get; }
    public string Size { get; }
    internal Deflagration(double burnedKg, double oxygenKg, double energyKJ, double lostKg, string size)
    { BurnedKg = burnedKg; OxygenKg = oxygenKg; EnergyKJ = energyKJ; LostKg = lostKg; Size = size; }
}

/// <summary>The H2 Hydrogen Store: a 2 x 2 pressurised vessel holding electrolysis hydrogen as a kilogram record
/// (hydrogen has no game species and never enters a room as gas). A damaged store leaks to space, or, with
/// oxygen in the room and an ignition source, deflagrates through the game's own explosion machinery.</summary>
public static class HydrogenRules
{
    public const string Prefix = "PhobosHydrogenStore", Installed = Prefix + "Installed";
    public const string Record = "ManufacturingHydrogen", Journal = "ManufacturingHydrogenWork", Guard = "ManufacturingHydrogenTransfer";
    public const int Footprint = 2;
    /// <summary>The native RTA canister volume (0.787 m3) at its rated 41.4 MPa and 293 K holds ~27 kg of ideal-gas
    /// hydrogen; authored 24 kg of usable capacity in a 160 kg housing.</summary>
    public const double CapacityKg = 24, DryKg = 160, Price = 5200, LeakKgPerHour = 2;
    /// <summary>Higher heating value of hydrogen, 285.83 kJ/mol of water formed (NIST), 141.9 MJ per kilogram.</summary>
    public const double HHVKJPerKg = 141900, OxygenPerHydrogen = 8;
    /// <summary>Oxygen partial pressure below which hydrogen will not deflagrate in this model (authored).</summary>
    public const double IgnitionOxygenKPa = 5;
    public const double SmallKg = 2, MediumKg = 8;
    public const string DeflagrationPrefix = "SysPhobosDeflagration";
    public static readonly BulkVesselSpec Spec = new(Prefix, ManufacturingRules.Hydrogen, CapacityKg, DryKg, ManufacturingRules.Owner, Record, Journal, Guard, VesselDamagePolicy.Leak, LeakKgPerHour);
    public static bool IsFamily(string? id) => EquipmentIdentity.IsFamily(id, Prefix);
    /// <summary>Whether a release ignites: enough oxygen and something to light it (a fire in the room, a working
    /// refinery hearth, or a powered device sparking at the game's own half-damage rule).</summary>
    public static HydrogenFate Fate(double oxygenKPa, bool fireInRoom, bool hearthWorking, bool sparkingDevice) =>
        ManufacturingRules.Finite(oxygenKPa) && oxygenKPa >= IgnitionOxygenKPa && (fireInRoom || hearthWorking || sparkingDevice) ? HydrogenFate.Deflagrate : HydrogenFate.Leak;
    public static Deflagration Burn(double hydrogenKg, double oxygenAvailableKg)
    {
        if (!ManufacturingRules.Finite(hydrogenKg) || !ManufacturingRules.Finite(oxygenAvailableKg) || hydrogenKg < 0 || oxygenAvailableKg < 0) throw new ArgumentException("Invalid deflagration inputs.");
        double burned = Math.Min(hydrogenKg, oxygenAvailableKg / OxygenPerHydrogen);
        string size = burned < SmallKg ? "Small" : burned < MediumKg ? "Medium" : "Large";
        return new Deflagration(burned, burned * OxygenPerHydrogen, burned * HHVKJPerKg, hydrogenKg - burned, size);
    }
    public static string DeflagrationDefinition(Deflagration d) => DeflagrationPrefix + d.Size;
}
