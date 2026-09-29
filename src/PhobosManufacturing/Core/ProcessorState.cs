using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosManufacturing.Core;

/// <summary>The X2's saved record (schema 1): water held for the current cycle, energy credited to it, totals
/// produced, and the linked native oxygen canister (a one-sided saved id: nothing is written into a vanilla
/// object). Running permission is not saved; a reload waits for Resume.</summary>
public sealed class ProcessorState
{
    public double HoldKg, CycleKWh, ProducedO2Kg, ProducedH2Kg, CabinO2Kg;
    public long Cycles;
    public string Canister = "";
    private static readonly string[] Keys = { "hold", "cycle", "o2", "h2", "cabin", "cycles", "canister" };
    public static ProcessorState Read(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0)) throw new FormatException("Unknown processor record field.");
        var s = new ProcessorState
        {
            HoldKg = Number(fields, "hold"), CycleKWh = Number(fields, "cycle"), ProducedO2Kg = Number(fields, "o2"), ProducedH2Kg = Number(fields, "h2"),
            CabinO2Kg = Number(fields, "cabin"), Cycles = Int(fields, "cycles"), Canister = fields.TryGetValue("canister", out var c) && c != "-" ? c : ""
        };
        if (s.HoldKg < 0 || s.HoldKg > ProcessorRules.WaterKgPerCycle + 1e-9 || s.CycleKWh < 0 || s.CycleKWh >= ProcessorRules.CycleKWh ||
            s.ProducedO2Kg < 0 || s.ProducedH2Kg < 0 || s.CabinO2Kg < 0 || s.Cycles < 0) throw new FormatException("Invalid processor record.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["hold"] = N(HoldKg), ["cycle"] = N(CycleKWh), ["o2"] = N(ProducedO2Kg), ["h2"] = N(ProducedH2Kg), ["cabin"] = N(CabinO2Kg),
        ["cycles"] = Cycles.ToString(CultureInfo.InvariantCulture), ["canister"] = Canister.Length == 0 ? "-" : Canister
    };
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static double Number(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && ManufacturingRules.Finite(d) ? d : throw new FormatException("Invalid number: " + key);
    private static long Int(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long i) ? i : throw new FormatException("Invalid integer: " + key);
    public static bool SafeId(string id) => ObjectStateStore.SafeValue(id);
}
