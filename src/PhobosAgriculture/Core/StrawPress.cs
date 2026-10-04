using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhobosAgriculture.Core;

/// <summary>The B2's straw press (Agriculture 0.44.0): recorded crop residue and spent biomass go in whole, and the
/// press keeps one record of what they held, as organic matter (the crop model's CH2O-equivalent fixed carbon), mineral
/// nutrients and water. Drying boils off the water above the bale's share; the press then packs fixed one-kilogram
/// straw bales, whose composition is fixed so a refinery charge can bind them by mass. What no whole bale can take stays
/// in the press until more straw arrives, or is emptied back out as one recorded residue.</summary>
public sealed class StrawPress
{
    /// <summary>One straw bale: 0.87 kg organic, 0.03 kg minerals and 0.10 kg water (authored; about 10% moisture, as
    /// baled straw is stored). The minerals sit between fresh residue (about 4%) and spent biomass (about 2%).</summary>
    public const double BaleKg = 1, BaleOrganicKg = .87, BaleMineralsKg = .03, BaleWaterKg = .1;
    /// <summary>The most the press holds, wet.</summary>
    public const double CapacityKg = 12;
    /// <summary>Electricity per kilogram of water boiled off: water's latent heat of vaporisation at 100 C, 2,257 kJ/kg
    /// (NIST Chemistry WebBook), as kWh. Heating the straw itself is left out.</summary>
    public const double DryKWhPerKg = 2.257 / 3.6;
    /// <summary>The dryer's draw (authored): a kilogram of water takes about 38 minutes.</summary>
    public const double PowerKW = 1;
    public double Organic, Minerals, Water, Energy;
    public double TotalKg => Organic + Minerals + Water;
    public bool Empty => TotalKg <= 1e-9;
    public StrawPress Copy() => (StrawPress)MemberwiseClone();
    /// <summary>The water the press keeps after drying: the bale's share of the organic matter it holds.</summary>
    public double KeptWater => Math.Min(Water, Organic * BaleWaterKg / BaleOrganicKg);
    /// <summary>Water drying would boil off now.</summary>
    public double Surplus => Math.Max(0, Water - KeptWater);
    public double DryingKWh => Surplus * DryKWhPerKg;
    /// <summary>Whole bales the press could pack once dried.</summary>
    public int Bales => (int)Math.Floor(Math.Min(Organic / BaleOrganicKg, Math.Min(Minerals / BaleMineralsKg, KeptWater / BaleWaterKg)) + 1e-9);
    /// <summary>Takes in one recorded item, whole. Water is what its record does not name.</summary>
    public void Absorb(double mass, double nutrient, double organic)
    {
        if (!CropState.Finite(mass) || !CropState.Finite(nutrient) || !CropState.Finite(organic) || mass <= 0 || nutrient < 0 || organic <= 0 || nutrient + organic > mass + 1e-9)
            throw new ArgumentException("Invalid straw input.");
        if (TotalKg + mass > CapacityKg + 1e-9) throw new ArgumentException("The press is full.");
        Organic += organic; Minerals += nutrient; Water += Math.Max(0, mass - nutrient - organic);
    }
    /// <summary>Boils off the surplus water and returns its mass.</summary>
    public double Dry() { double kg = Surplus; Water -= kg; Energy = 0; return kg; }
    /// <summary>Packs whole bales out of the press.</summary>
    public void Pack(int bales)
    {
        if (bales < 1 || bales > Bales) throw new ArgumentException("Invalid bale count.");
        Organic = Clamp(Organic - bales * BaleOrganicKg); Minerals = Clamp(Minerals - bales * BaleMineralsKg); Water = Clamp(Water - bales * BaleWaterKg);
    }
    private static double Clamp(double kg) => Math.Abs(kg) < 1e-9 ? 0 : kg;
    public Dictionary<string, string> Save()
    {
        Validate();
        return new() { ["organic"] = N(Organic), ["minerals"] = N(Minerals), ["water"] = N(Water), ["energy"] = N(Energy) };
    }
    public static StrawPress Read(IReadOnlyDictionary<string, string> d)
    {
        if (d.Count != 4) throw new ArgumentException("Unknown straw press record.");
        double P(string key) => double.Parse(d[key], CultureInfo.InvariantCulture);
        var press = new StrawPress { Organic = P("organic"), Minerals = P("minerals"), Water = P("water"), Energy = P("energy") };
        press.Validate(); return press;
    }
    private void Validate()
    {
        foreach (double x in new[] { Organic, Minerals, Water, Energy })
            if (!CropState.Finite(x) || x < 0) throw new ArgumentException("Invalid straw press record.");
        if (TotalKg > CapacityKg + 1e-6 || Energy > 100 || Empty && Energy > 0) throw new ArgumentException("Invalid straw press record.");
    }
    private static string N(double x) => x.ToString("R", CultureInfo.InvariantCulture);
}
