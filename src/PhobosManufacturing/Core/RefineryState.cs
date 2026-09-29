using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosManufacturing.Core;

/// <summary>The V4's saved record (schema 1): the bound charge, its recipe revision, powered progress, the heat-wait
/// time it has accumulated, the off-gas already released, and the cycle count. A charge whose recipe is not
/// available on this installation is retained and reported, never overwritten.</summary>
public sealed class RefineryState
{
    public string RecipeId = "";
    public int Revision;
    public double ProgressSeconds, WaitSeconds, EmittedKg;
    public long Cycles;
    public bool Running;
    /// <summary>The exact bound feed units, by object id.</summary>
    public List<string> Charge = new();
    public bool Bound => Revision > 0 && Charge.Count > 0;
    private static readonly string[] Keys = { "recipe", "revision", "progress", "wait", "emitted", "cycles", "running", "charge" };
    public static RefineryState Read(IReadOnlyDictionary<string, string> fields)
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0)) throw new FormatException("Unknown refinery record field.");
        var s = new RefineryState
        {
            RecipeId = fields.TryGetValue("recipe", out var recipe) && recipe != "-" ? recipe : "",
            Revision = Int(fields, "revision"), ProgressSeconds = Number(fields, "progress"), WaitSeconds = Number(fields, "wait"),
            EmittedKg = Number(fields, "emitted"), Cycles = Int(fields, "cycles"), Running = fields.TryGetValue("running", out var r) && r == "1",
            Charge = fields.TryGetValue("charge", out var charge) && charge != "-" ? charge.Split(';').Where(x => x.Length > 0).ToList() : new List<string>()
        };
        if (s.Revision < 0 || s.ProgressSeconds < 0 || s.WaitSeconds < 0 || s.EmittedKg < 0 || s.Cycles < 0 || (s.Revision == 0) != (s.Charge.Count == 0)) throw new FormatException("Invalid refinery record.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save() => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["recipe"] = RecipeId.Length == 0 ? "-" : RecipeId, ["revision"] = Revision.ToString(CultureInfo.InvariantCulture),
        ["progress"] = N(ProgressSeconds), ["wait"] = N(WaitSeconds), ["emitted"] = N(EmittedKg),
        ["cycles"] = Cycles.ToString(CultureInfo.InvariantCulture), ["running"] = Running ? "1" : "0",
        ["charge"] = Charge.Count == 0 ? "-" : string.Join(";", Charge)
    };
    public void Clear() { RecipeId = ""; Revision = 0; ProgressSeconds = 0; WaitSeconds = 0; EmittedKg = 0; Running = false; Charge.Clear(); }
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static double Number(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && ManufacturingRules.Finite(d) ? d : throw new FormatException("Invalid number: " + key);
    private static int Int(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : throw new FormatException("Invalid integer: " + key);
    public static bool SafeId(string id) => ObjectStateStore.SafeValue(id) && !id.Contains(';');
}
