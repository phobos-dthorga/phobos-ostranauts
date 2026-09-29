using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosManufacturing.Core;

/// <summary>The K2's saved record (schema 1). Reactants held for the current cycle (hydrogen and carbon dioxide),
/// products made and not yet delivered (water and methane), the energy credited to the cycle, totals, and the
/// linked native CO2 canister (a one-sided saved id: nothing is written into a vanilla object). A completed
/// cycle turns its reactant hold into its product hold in one saved step, so a reload can never deliver twice.
/// Running permission is not saved; a reload waits for Start.</summary>
public sealed class SabatierState
{
    public double HydrogenKg, CarbonDioxideKg, WaterKg, MethaneKg, CycleKWh, ProducedWaterKg, ProducedMethaneKg, ConsumedCarbonDioxideKg;
    public long Cycles;
    public string Canister = "";
    public double HeldKg => HydrogenKg + CarbonDioxideKg + WaterKg + MethaneKg;
    public bool HoldsProducts => WaterKg > 1e-9 || MethaneKg > 1e-9;
    public bool Charged => HydrogenKg + 1e-9 >= SabatierRules.HydrogenKgPerCycle && CarbonDioxideKg + 1e-9 >= SabatierRules.CarbonDioxideKgPerCycle;
    private static readonly string[] Keys = { "h2", "co2", "water", "ch4", "cycle", "made_water", "made_ch4", "used_co2", "cycles", "canister" };
    public static SabatierState Read(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0)) throw new FormatException("Unknown reactor record field.");
        var s = new SabatierState
        {
            HydrogenKg = Number(fields, "h2"), CarbonDioxideKg = Number(fields, "co2"), WaterKg = Number(fields, "water"), MethaneKg = Number(fields, "ch4"),
            CycleKWh = Number(fields, "cycle"), ProducedWaterKg = Number(fields, "made_water"), ProducedMethaneKg = Number(fields, "made_ch4"),
            ConsumedCarbonDioxideKg = Number(fields, "used_co2"), Cycles = Int(fields, "cycles"),
            Canister = fields.TryGetValue("canister", out var c) && c != "-" ? c : ""
        };
        if (s.HydrogenKg < 0 || s.HydrogenKg > SabatierRules.HydrogenKgPerCycle + 1e-9 || s.CarbonDioxideKg < 0 || s.CarbonDioxideKg > SabatierRules.CarbonDioxideKgPerCycle + 1e-9 ||
            s.WaterKg < 0 || s.WaterKg > SabatierRules.WaterKgPerCycle + 1e-9 || s.MethaneKg < 0 || s.MethaneKg > SabatierRules.MethaneKgPerCycle + 1e-9 ||
            s.CycleKWh < 0 || s.CycleKWh >= SabatierRules.CycleKWh || s.ProducedWaterKg < 0 || s.ProducedMethaneKg < 0 || s.ConsumedCarbonDioxideKg < 0 || s.Cycles < 0 ||
            (s.CycleKWh > 0 && s.HoldsProducts)) throw new FormatException("Invalid reactor record.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["h2"] = N(HydrogenKg), ["co2"] = N(CarbonDioxideKg), ["water"] = N(WaterKg), ["ch4"] = N(MethaneKg), ["cycle"] = N(CycleKWh),
        ["made_water"] = N(ProducedWaterKg), ["made_ch4"] = N(ProducedMethaneKg), ["used_co2"] = N(ConsumedCarbonDioxideKg),
        ["cycles"] = Cycles.ToString(CultureInfo.InvariantCulture), ["canister"] = Canister.Length == 0 ? "-" : Canister
    };
    /// <summary>A completed cycle: the reactant hold becomes the product hold, mass for mass, and the cycle closes.</summary>
    public void Convert()
    {
        if (!Charged || HoldsProducts) throw new InvalidOperationException("The reactor has no complete charge to convert.");
        HydrogenKg = 0; CarbonDioxideKg = 0; CycleKWh = 0;
        WaterKg = SabatierRules.WaterKgPerCycle; MethaneKg = SabatierRules.MethaneKgPerCycle;
        ConsumedCarbonDioxideKg += SabatierRules.CarbonDioxideKgPerCycle; Cycles++;
    }
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static double Number(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && ManufacturingRules.Finite(d) ? d : throw new FormatException("Invalid number: " + key);
    private static long Int(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i) ? i : throw new FormatException("Invalid integer: " + key);
}
