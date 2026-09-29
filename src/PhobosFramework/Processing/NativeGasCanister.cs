using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>What an installed native gas canister holds: one species, its moles, and the capacity the game's
/// own refuelling arithmetic gives it (volume times rated pressure over R T).</summary>
public sealed class CanisterReading
{
    public string Species { get; }
    public double Moles { get; }
    public double CapacityMoles { get; }
    public double PressureKPa { get; }
    public double MaxPressureKPa { get; }
    public double VolumeM3 { get; }
    public double Kelvin { get; }
    public double HeadroomMoles => Math.Max(0, CapacityMoles - Moles);
    public CanisterReading(string species, double moles, double capacityMoles, double pressureKPa, double maxPressureKPa, double volumeM3, double kelvin)
    { Species = species; Moles = moles; CapacityMoles = capacityMoles; PressureKPa = pressureKPa; MaxPressureKPa = maxPressureKPa; VolumeM3 = volumeM3; Kelvin = kelvin; }
}

/// <summary>The game's gas species by name, their molar masses (the constants in the game's own
/// <c>GasContainer.GetGasMass</c>), and guarded reads and writes on an installed native canister. The game
/// caps nothing when moles are added, so the cap is applied here from the canister's rated pressure; removals
/// are clamped to what the canister holds, because a negative total corrupts the game's internal count.
/// Species the game has no condition for are never created.</summary>
public static class NativeGasCanister
{
    /// <summary>The game's R, in kJ per mol K, as its refuelling and pressure code use it.</summary>
    public const double GasConstantKJPerMolK = 0.008314000442624092;
    public const double ReferenceKelvin = 293;
    public static readonly IReadOnlyDictionary<string, double> KgPerMol = new Dictionary<string, double>(StringComparer.Ordinal)
    {
        ["H2"] = 0.0020159, ["He2"] = 0.008005204, ["CH4"] = 0.016043, ["NH3"] = 0.017031, ["H2O"] = 0.0180153,
        ["N2"] = 0.0280134, ["O2"] = 0.0319988, ["CO2"] = 0.04401, ["H2SO4"] = 0.0980785, ["CO"] = 0.02801, ["Smoke"] = 0.0980785
    };
    /// <summary>Species the game gives a room and canister condition; only these may be added to any container.</summary>
    public static readonly IReadOnlyList<string> RoomSpecies = new[] { "CH4", "CO", "CO2", "H2SO4", "N2", "NH3", "O2", "Smoke" };
    private static readonly (string Condition, string Species)[] vesselConditions = { ("IsVesselO2", "O2"), ("IsVesselCO2", "CO2"), ("IsVesselN2", "N2") };

    public static double CapacityMoles(double volumeM3, double maxPressureKPa, double kelvin)
    {
        if (!Finite(volumeM3) || !Finite(maxPressureKPa) || !Finite(kelvin) || volumeM3 <= 0 || maxPressureKPa <= 0 || kelvin <= 0)
            throw new ArgumentException("Invalid canister rating.");
        return volumeM3 * maxPressureKPa / GasConstantKJPerMolK / kelvin;
    }
    public static double Moles(string species, double kg) => Check(kg) / MolarMass(species);
    public static double Kilograms(string species, double moles) => Check(moles) * MolarMass(species);
    public static bool IsRoomSpecies(string? species) => species != null && RoomSpecies.Contains(species, StringComparer.Ordinal);
    private static double MolarMass(string species) =>
        IsRoomSpecies(species) && KgPerMol.TryGetValue(species!, out double kgPerMol) ? kgPerMol : throw new ArgumentException("Not a native gas species: " + species);
    private static double Check(double amount) => Finite(amount) && amount >= 0 ? amount : throw new ArgumentException("Invalid gas amount.");
    private static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

    /// <summary>The species a native vessel is rated for (O2, CO2 or N2 canisters), or null.</summary>
    public static string? Species(CondOwner? co)
    {
        if (co == null) return null;
        foreach (var pair in vesselConditions) if (co.HasCond(pair.Condition)) return pair.Species;
        return null;
    }
    /// <summary>Reads an installed native canister: committed plus pending moles of its species and its rated
    /// capacity. False for anything that is not an installed rated canister with a gas container.</summary>
    public static bool TryRead(CondOwner? co, out CanisterReading reading)
    {
        reading = null!;
        string? species = Species(co);
        var gas = co?.GasContainer;
        if (species == null || gas == null || co == null || co.bDestroyed || !co.HasCond("IsInstalled") || !co.HasCond("IsRTA")) return false;
        double volume = co.GetCondAmount("StatVolume"), max = co.GetCondAmount("StatGasPressureMax");
        double kelvin = co.GetCondAmount("StatGasTemp"); if (!Finite(kelvin) || kelvin <= 0) kelvin = ReferenceKelvin;
        if (!Finite(volume) || volume <= 0 || !Finite(max) || max <= 0) return false;
        reading = new CanisterReading(species, HeldMoles(gas, species), CapacityMoles(volume, max, kelvin), co.GetCondAmount("StatGasPressure"), max, volume, kelvin);
        return true;
    }
    internal static double HeldMoles(GasContainer gas, string species)
    {
        string key = "StatGasMol" + species;
        double moles = gas.mapGasMols1.TryGetValue(key, out double committed) ? committed : 0;
        if (gas.mapDGasMols != null && gas.mapDGasMols.TryGetValue(key, out double pending)) moles += pending;
        return Finite(moles) ? Math.Max(0, moles) : 0;
    }
    /// <summary>Adds up to the canister's headroom and returns the moles actually added (zero when full or
    /// when the canister is not rated for that species).</summary>
    public static double TryAdd(CondOwner co, string species, double moles)
    {
        Check(moles);
        if (!TryRead(co, out var reading) || reading.Species != species) return 0;
        double added = Math.Min(moles, reading.HeadroomMoles);
        if (added <= 0) return 0;
        co.GasContainer.AddGasMols(species, added, true);
        return added;
    }
    /// <summary>Removes up to what the canister holds and returns the moles actually taken.</summary>
    public static double TryTake(CondOwner co, string species, double moles)
    {
        Check(moles);
        if (!TryRead(co, out var reading) || reading.Species != species) return 0;
        double taken = Math.Min(moles, reading.Moles);
        if (taken <= 0) return 0;
        co.GasContainer.AddGasMols(species, -taken, true);
        return taken;
    }
}
