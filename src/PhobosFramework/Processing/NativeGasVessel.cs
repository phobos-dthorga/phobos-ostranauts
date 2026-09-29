using System;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>What any native gas vessel holds: an installed or loose canister, or a suit bottle. <see cref="Moles"/>
/// is its rated species; <see cref="TotalMoles"/> counts every species it holds, because pressure (and bursting)
/// follows the whole content.</summary>
public sealed class GasVesselReading
{
    public string Species { get; }
    public double Moles { get; }
    public double TotalMoles { get; }
    public double CapacityMoles { get; }
    public double MaxPressureKPa { get; }
    public double VolumeM3 { get; }
    public double Kelvin { get; }
    public bool Installed { get; }
    public bool Bottle { get; }
    public GasVesselReading(string species, double moles, double totalMoles, double capacityMoles, double maxPressureKPa, double volumeM3, double kelvin, bool installed, bool bottle)
    { Species = species; Moles = moles; TotalMoles = totalMoles; CapacityMoles = capacityMoles; MaxPressureKPa = maxPressureKPa; VolumeM3 = volumeM3; Kelvin = kelvin; Installed = installed; Bottle = bottle; }
    /// <summary>Moles that may still go in before the whole content reaches <paramref name="fraction"/> of rating.</summary>
    public double HeadroomMoles(double fraction) => NativeGasVessel.Headroom(TotalMoles, CapacityMoles, fraction);
    public double FillFraction => CapacityMoles > 0 ? TotalMoles / CapacityMoles : 0;
}

/// <summary>Guarded reads and writes on any native gas vessel the game rates for O2, CO2 or N2: installed or loose
/// canisters and the suit's O2 bottle. The game's air pump fills without a cut-off and a vessel bursts when its
/// pressure difference to the room passes its rating by 150 kPa (<c>GasContainer.CheckPressureDifference</c>), so
/// every fill here stops at a fraction of the rating, counting all the species inside. Removals are clamped to
/// what is held. Species the game has no condition for are never created.</summary>
public static class NativeGasVessel
{
    /// <summary>The default safe fill: 99% of the rated pressure, below the burst margin even in vacuum.</summary>
    public const double SafeFillFraction = 0.99;
    public static double Headroom(double totalMoles, double capacityMoles, double fraction)
    {
        if (!Finite(totalMoles) || !Finite(capacityMoles) || !Finite(fraction) || totalMoles < 0 || capacityMoles <= 0 || fraction <= 0 || fraction > 1)
            throw new ArgumentException("Invalid gas vessel headroom.");
        return Math.Max(0, capacityMoles * fraction - totalMoles);
    }
    /// <summary>The suit bottle: the game's handheld O2 vessel (<c>IsHandheld</c> with <c>IsVesselO2</c>).</summary>
    public static bool IsBottle(CondOwner? co) => co != null && co.HasCond("IsHandheld") && NativeGasCanister.Species(co) != null;
    public static bool TryRead(CondOwner? co, out GasVesselReading reading)
    {
        reading = null!;
        string? species = NativeGasCanister.Species(co);
        var gas = co?.GasContainer;
        if (species == null || gas == null || co == null || co.bDestroyed) return false;
        double volume = co.GetCondAmount("StatVolume"), max = co.GetCondAmount("StatGasPressureMax");
        double kelvin = co.GetCondAmount("StatGasTemp"); if (!Finite(kelvin) || kelvin <= 0) kelvin = NativeGasCanister.ReferenceKelvin;
        if (!Finite(volume) || volume <= 0 || !Finite(max) || max <= 0) return false;
        double total = NativeGasCanister.RoomSpecies.Sum(s => NativeGasCanister.HeldMoles(gas, s));
        reading = new GasVesselReading(species, NativeGasCanister.HeldMoles(gas, species), total, NativeGasCanister.CapacityMoles(volume, max, kelvin), max, volume, kelvin,
            co.HasCond("IsInstalled"), IsBottle(co));
        return true;
    }
    /// <summary>Adds up to the vessel's headroom below <paramref name="fraction"/> of its rating and returns the moles
    /// actually added; zero for a damaged vessel or one rated for another species.</summary>
    public static double TryFill(CondOwner co, string species, double moles, double fraction = SafeFillFraction)
    {
        if (!Finite(moles) || moles < 0) throw new ArgumentException("Invalid gas amount.");
        if (co.HasCond("IsDamaged") || !TryRead(co, out var reading) || reading.Species != species) return 0;
        double added = Math.Min(moles, reading.HeadroomMoles(fraction));
        if (added <= 0) return 0;
        co.GasContainer.AddGasMols(species, added, true);
        return added;
    }
    /// <summary>Removes up to what the vessel holds of its species and returns the moles actually taken.</summary>
    public static double TryDrain(CondOwner co, string species, double moles)
    {
        if (!Finite(moles) || moles < 0) throw new ArgumentException("Invalid gas amount.");
        if (!TryRead(co, out var reading) || reading.Species != species) return 0;
        double taken = Math.Min(moles, reading.Moles);
        if (taken <= 0) return 0;
        co.GasContainer.AddGasMols(species, -taken, true);
        return taken;
    }
    /// <summary>The game's own price per kilogram for a gas (its GasPrices table, as the refuelling kiosk charges).</summary>
    public static double PricePerKg(string species) => GasContainer.GetGasPrice(species);
    private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
}
