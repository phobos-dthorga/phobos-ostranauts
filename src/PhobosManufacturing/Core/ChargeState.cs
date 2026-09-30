using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosManufacturing.Core;

/// <summary>A charge machine's saved record (schema 1): the bound charge, its recipe revision, powered progress, the
/// heat-wait time it has accumulated, the off-gas already released, and the cycle count. A machine that binds an
/// explicitly selected recipe also saves that selection. A charge whose recipe is not available on this installation
/// is retained and reported, never overwritten.</summary>
public class ChargeState
{
    public string RecipeId = "";
    public int Revision;
    public double ProgressSeconds, WaitSeconds, EmittedKg;
    public long Cycles;
    public bool Running;
    /// <summary>The recipe revision the next charge binds, for explicit-selection machines (0 when none is chosen).</summary>
    public int Selected;
    /// <summary>The exact bound feed units, by object id.</summary>
    public List<string> Charge = new();
    public bool Bound => Revision > 0 && Charge.Count > 0;
    /// <summary>Whether the record carries the selection field (a V4 record never does).</summary>
    public bool ExplicitSelection { get; }
    public ChargeState(bool explicitSelection) { ExplicitSelection = explicitSelection; }
    private static readonly string[] Keys = { "recipe", "revision", "progress", "wait", "emitted", "cycles", "running", "charge" };
    private const string SelectedKey = "selected";
    public static ChargeState Read(IReadOnlyDictionary<string, string> fields, bool explicitSelection) => Fill(new ChargeState(explicitSelection), fields);
    protected static T Fill<T>(T s, IReadOnlyDictionary<string, string> fields) where T : ChargeState
    {
        if (fields.Keys.Any(k => Array.IndexOf(Keys, k) < 0 && !(s.ExplicitSelection && k == SelectedKey))) throw new FormatException("Unknown charge record field.");
        s.RecipeId = fields.TryGetValue("recipe", out var recipe) && recipe != "-" ? recipe : "";
        s.Revision = Int(fields, "revision"); s.ProgressSeconds = Number(fields, "progress"); s.WaitSeconds = Number(fields, "wait");
        s.EmittedKg = Number(fields, "emitted"); s.Cycles = Int(fields, "cycles"); s.Running = fields.TryGetValue("running", out var r) && r == "1";
        s.Charge = fields.TryGetValue("charge", out var charge) && charge != "-" ? charge.Split(';').Where(x => x.Length > 0).ToList() : new List<string>();
        s.Selected = s.ExplicitSelection ? Int(fields, SelectedKey) : 0;
        if (s.Revision < 0 || s.Selected < 0 || s.ProgressSeconds < 0 || s.WaitSeconds < 0 || s.EmittedKg < 0 || s.Cycles < 0 || (s.Revision == 0) != (s.Charge.Count == 0))
            throw new FormatException("Invalid charge record.");
        return s;
    }
    public IReadOnlyDictionary<string, string> Save()
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["recipe"] = RecipeId.Length == 0 ? "-" : RecipeId, ["revision"] = Revision.ToString(CultureInfo.InvariantCulture),
            ["progress"] = N(ProgressSeconds), ["wait"] = N(WaitSeconds), ["emitted"] = N(EmittedKg),
            ["cycles"] = Cycles.ToString(CultureInfo.InvariantCulture), ["running"] = Running ? "1" : "0",
            ["charge"] = Charge.Count == 0 ? "-" : string.Join(";", Charge)
        };
        if (ExplicitSelection) fields[SelectedKey] = Selected.ToString(CultureInfo.InvariantCulture);
        return fields;
    }
    /// <summary>Releases the bound charge; the selection, if any, stays for the next charge.</summary>
    public void Clear() { RecipeId = ""; Revision = 0; ProgressSeconds = 0; WaitSeconds = 0; EmittedKg = 0; Running = false; Charge.Clear(); }
    private static string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
    private static double Number(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out double d) && ManufacturingRules.Finite(d) ? d : throw new FormatException("Invalid number: " + key);
    private static int Int(IReadOnlyDictionary<string, string> f, string key) =>
        !f.TryGetValue(key, out var v) ? 0 : int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i) ? i : throw new FormatException("Invalid integer: " + key);
    public static bool SafeId(string id) => ObjectStateStore.SafeValue(id) && !id.Contains(';');
}

/// <summary>The V4's record: the charge record without a selection, byte-identical to Manufacturing 0.1.0's.</summary>
public sealed class RefineryState : ChargeState
{
    public RefineryState() : base(false) { }
    public static RefineryState Read(IReadOnlyDictionary<string, string> fields) => Fill(new RefineryState(), fields);
}
