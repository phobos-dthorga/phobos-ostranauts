using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosManufacturing.Core;

/// <summary>The AX-2's saved record (schema 1). Ammonia held for the current cycle, products made and not yet
/// delivered (nitrogen and hydrogen), the energy credited to the cycle and totals. A completed cycle turns its
/// ammonia hold into its product hold in one saved step, so a reload can never deliver twice. Running permission
/// is not saved; a reload waits for Start.</summary>
public sealed class CrackerState
{
    public double AmmoniaKg, NitrogenKg, HydrogenKg, CycleKWh, ProducedNitrogenKg, ProducedHydrogenKg, ConsumedAmmoniaKg;
    public long Cycles;
    public double HeldKg => AmmoniaKg + NitrogenKg + HydrogenKg;
    public bool HoldsProducts => NitrogenKg > 1e-9 || HydrogenKg > 1e-9;
    public bool Charged => AmmoniaKg + 1e-9 >= CrackerRules.AmmoniaKgPerCycle;
    private static readonly string[] Keys = { "nh3", "n2", "h2", "cycle", "made_n2", "made_h2", "used_nh3", "cycles" };
    public static CrackerState Read(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0)) throw new FormatException("Unknown cracker record field.");
        var s = new CrackerState
        {
            AmmoniaKg = Number(fields, "nh3"), NitrogenKg = Number(fields, "n2"), HydrogenKg = Number(fields, "h2"), CycleKWh = Number(fields, "cycle"),
            ProducedNitrogenKg = Number(fields, "made_n2"), ProducedHydrogenKg = Number(fields, "made_h2"), ConsumedAmmoniaKg = Number(fields, "used_nh3"), Cycles = Int(fields, "cycles")
        };
        if (s.AmmoniaKg < 0 || s.AmmoniaKg > CrackerRules.AmmoniaKgPerCycle + 1e-9 || s.NitrogenKg < 0 || s.NitrogenKg > CrackerRules.NitrogenKgPerCycle + 1e-9 ||
            s.HydrogenKg < 0 || s.HydrogenKg > CrackerRules.HydrogenKgPerCycle + 1e-9 || s.CycleKWh < 0 || s.CycleKWh >= CrackerRules.CycleKWh ||
            s.ProducedNitrogenKg < 0 || s.ProducedHydrogenKg < 0 || s.ConsumedAmmoniaKg < 0 || s.Cycles < 0 || (s.CycleKWh > 0 && s.HoldsProducts))
            throw new FormatException("Invalid cracker record.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["nh3"] = N(AmmoniaKg), ["n2"] = N(NitrogenKg), ["h2"] = N(HydrogenKg), ["cycle"] = N(CycleKWh),
        ["made_n2"] = N(ProducedNitrogenKg), ["made_h2"] = N(ProducedHydrogenKg), ["used_nh3"] = N(ConsumedAmmoniaKg),
        ["cycles"] = Cycles.ToString(CultureInfo.InvariantCulture)
    };
    /// <summary>A completed cycle: the ammonia hold becomes the product hold, mass for mass, and the cycle closes.</summary>
    public void Convert()
    {
        if (!Charged || HoldsProducts) throw new InvalidOperationException("The cracker has no complete charge to convert.");
        AmmoniaKg = 0; CycleKWh = 0;
        NitrogenKg = CrackerRules.NitrogenKgPerCycle; HydrogenKg = CrackerRules.HydrogenKgPerCycle;
        ConsumedAmmoniaKg += CrackerRules.AmmoniaKgPerCycle; Cycles++;
    }
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static double Number(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && ManufacturingRules.Finite(d) ? d : throw new FormatException("Invalid number: " + key);
    private static long Int(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i) ? i : throw new FormatException("Invalid integer: " + key);
}
