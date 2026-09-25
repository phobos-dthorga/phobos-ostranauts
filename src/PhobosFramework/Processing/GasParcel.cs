using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Processing;

/// <summary>Finite captured native species; energy is absolute sensible internal energy in kJ.
/// This does not simulate rooms or create species. Native adapters supply validated identities.</summary>
public sealed class GasParcel
{
    private readonly Dictionary<string, double> species = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, double> Species => species;
    public double Moles => species.Values.Sum();
    public double EnergyKJ { get; private set; }
    public double CapacityPerMole { get; }
    public GasParcel(double capacityPerMole)
    {
        if (!ThermalMath.Finite(capacityPerMole) || capacityPerMole <= 0) throw new ArgumentOutOfRangeException(nameof(capacityPerMole));
        CapacityPerMole = capacityPerMole;
    }
    public double TemperatureK => Moles > 0 ? EnergyKJ / (Moles * CapacityPerMole) : 0;
    public void Add(string id, double moles, double kelvin)
    {
        if (string.IsNullOrWhiteSpace(id) || !ThermalMath.Finite(moles) || moles < 0 || !ThermalMath.Finite(kelvin) || kelvin <= 0)
            throw new ArgumentOutOfRangeException(nameof(moles));
        if (moles == 0) return;
        species[id] = (species.TryGetValue(id, out var old) ? old : 0) + moles;
        EnergyKJ += moles * CapacityPerMole * kelvin;
    }
    public GasParcel Take(double moles)
    {
        if (!ThermalMath.Finite(moles) || moles < 0 || moles > Moles + 1e-10) throw new ArgumentOutOfRangeException(nameof(moles));
        var result = new GasParcel(CapacityPerMole);
        double total = Moles;
        if (total == 0) return result;
        double ratio = Math.Min(1, moles / total), temp = TemperatureK;
        foreach (var id in species.Keys.ToArray())
        {
            double moved = species[id] * ratio;
            result.Add(id, moved, temp); species[id] -= moved;
            if (species[id] == 0) species.Remove(id);
        }
        EnergyKJ -= result.EnergyKJ;
        if (species.Count == 0) EnergyKJ = 0;
        return result;
    }
    public void Add(GasParcel other)
    {
        if (other.CapacityPerMole != CapacityPerMole || ReferenceEquals(other, this)) throw new ArgumentException("Incompatible gas parcel.");
        foreach (var pair in other.Species) Add(pair.Key, pair.Value, other.TemperatureK);
    }
    public double SetTemperature(double kelvin)
    {
        if (!ThermalMath.Finite(kelvin) || kelvin <= 0) throw new ArgumentOutOfRangeException(nameof(kelvin));
        double delta = Moles * CapacityPerMole * kelvin - EnergyKJ;
        EnergyKJ += delta; return delta;
    }
}
