using System;
using System.Collections.Generic;
using System.Globalization;

namespace PhobosAgriculture.Core;

// Authored consumable-medium budget, not a claim about real treatment chemistry.
public static class TreatmentCartridge
{
    public const double CapacityKg = 25, FullPrice = 25;
    public static double Mass(double remaining) => DrainageRecovery.CartridgeKg * remaining / CapacityKg;
    public static double Price(double remaining) => FullPrice * remaining / CapacityKg;
    public static double Spend(double remaining, double drainage)
    {
        Validate(remaining, Mass(remaining));
        if (!CropState.Finite(drainage) || drainage <= 0 || drainage > remaining)
            throw new ArgumentException("Insufficient treatment capacity.");
        return remaining - drainage;
    }
    public static Dictionary<string,string> Save(double remaining)
    {
        Validate(remaining, Mass(remaining));
        return new() { ["remaining"] = remaining.ToString("R", CultureInfo.InvariantCulture) };
    }
    public static double Read(IReadOnlyDictionary<string,string> fields, double mass)
    {
        if (fields.Count != 1) throw new ArgumentException("Unknown cartridge fields.");
        double remaining = double.Parse(fields["remaining"], CultureInfo.InvariantCulture);
        Validate(remaining, mass); return remaining;
    }
    private static void Validate(double remaining, double mass)
    {
        if (!CropState.Finite(remaining) || remaining <= 0 || remaining > CapacityKg || !CropState.Finite(mass) || Math.Abs(mass - Mass(remaining)) > 1e-7)
            throw new ArgumentException("Invalid cartridge capacity or physical mass.");
    }
}
