using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAgriculture.Core;

// Authored aggregate nutrient accounting, not a chemical assay or complete mineral model.
public static class NutrientRecovery
{
    public const double Fraction = .6, PowerKW = .5, KWhPerKg = .02;
    // Economy audit (Agriculture 0.52.0): crop nutrients at 150 a kilogram, makeup salts at half that.
    public const double MakeupKg = .04, MakeupPrice = 3;
    public static double Allocation(CropState state, double residue)
    {
        if (state.RecoveryRevision != 1 || state.CropId.Length == 0 || state.Biomass <= 0) return 0;
        if (!CropState.Finite(residue) || residue < 0 || residue > state.Biomass + 1e-8) throw new ArgumentException("Invalid residue allocation.");
        return Math.Min(residue, state.Progress * Crop.Get(state.CropId).Nutrient * Math.Min(1, residue / state.Biomass));
    }
    /// <summary>The organic matter (the crop model's CH2O-equivalent fixed carbon) in a harvest's residue: the residue's
    /// share of the plant's mass (Agriculture 0.44.0).</summary>
    public static double Organic(CropState state, double residue) =>
        state.Biomass <= 0 || residue <= 0 ? 0 : Math.Min(residue, state.Carbon * Math.Min(1, residue / state.Biomass));
    /// <summary>A residue record. Since Agriculture 0.44.0 it may also name the organic matter, which the straw press
    /// needs; records without it (all residue made before) still recover nutrients.</summary>
    public static Dictionary<string,string> Record(double mass, double nutrient, double? organic = null)
    {
        Validate(mass, nutrient, organic);
        var d = new Dictionary<string,string> { ["mass"] = mass.ToString("R", CultureInfo.InvariantCulture), ["nutrient"] = nutrient.ToString("R", CultureInfo.InvariantCulture) };
        if (organic is double o) d["organic"] = o.ToString("R", CultureInfo.InvariantCulture);
        return d;
    }
    public static double Read(IReadOnlyDictionary<string,string> fields, double physicalMass) => ReadFull(fields, physicalMass).Nutrient;
    /// <summary>The record's nutrient and, when it names one, its organic matter.</summary>
    public static (double Nutrient, double? Organic) ReadFull(IReadOnlyDictionary<string,string> fields, double physicalMass)
    {
        bool organicField = fields.ContainsKey("organic");
        if (fields.Count != (organicField ? 3 : 2)) throw new ArgumentException("Unknown residue record.");
        double mass = double.Parse(fields["mass"], CultureInfo.InvariantCulture), nutrient = double.Parse(fields["nutrient"], CultureInfo.InvariantCulture);
        double? organic = organicField ? double.Parse(fields["organic"], CultureInfo.InvariantCulture) : null;
        Validate(mass, nutrient, organic);
        if (!CropState.Finite(physicalMass) || Math.Abs(mass - physicalMass) > 1e-8) throw new ArgumentException("Residue mass does not match its record.");
        return (nutrient, organic);
    }
    private static void Validate(double mass, double nutrient, double? organic = null)
    {
        if (!CropState.Finite(mass) || !CropState.Finite(nutrient) || mass <= 0 || nutrient < 0 || nutrient > mass) throw new ArgumentException("Invalid residue budget.");
        if (organic is double o && (!CropState.Finite(o) || o < 0 || nutrient + o > mass + 1e-9)) throw new ArgumentException("Invalid residue budget.");
    }
}

public sealed class NutrientCharge
{
    public static double DoseAllowance(CropState state,NutrientSolution solution,double poweredBlendBudget)
    {
        if(!state.Running || !solution.Enabled || !CropState.Finite(poweredBlendBudget) || poweredBlendBudget<=0)return 0;
        var supplied=state.Copy();supplied.Nutrients=solution.DryCapacity;
        double blend=solution.BlendAllowance(supplied,poweredBlendBudget);
        var ratio=NutrientSolution.Ratio(solution.Profile);
        return Math.Max(0,blend*ratio.SoluteKg/ratio.TotalKg-state.Nutrients);
    }
    public readonly double Initial, Remaining;
    public NutrientCharge(double initial, double remaining)
    {
        if (!CropState.Finite(initial) || !CropState.Finite(remaining) || initial <= 0 || initial > CropState.NutrientCapacityKg || remaining < 0 || remaining > initial)
            throw new ArgumentException("Invalid nutrient charge.");
        Initial = initial; Remaining = remaining;
    }
    public NutrientCharge Spend(double kg)
    {
        if (!CropState.Finite(kg) || kg < 0 || kg > Remaining) throw new ArgumentException("Invalid charge debit.");
        return new(Initial, Remaining - kg);
    }
    public Dictionary<string,string> Save() => new() { ["initial"] = Initial.ToString("R", CultureInfo.InvariantCulture), ["remaining"] = Remaining.ToString("R", CultureInfo.InvariantCulture) };
    public static NutrientCharge Read(IReadOnlyDictionary<string,string> d, double mass)
    {
        if (d.Count != 2) throw new ArgumentException("Unknown charge record.");
        var charge = new NutrientCharge(double.Parse(d["initial"], CultureInfo.InvariantCulture), double.Parse(d["remaining"], CultureInfo.InvariantCulture));
        if (!CropState.Finite(mass) || Math.Abs(charge.Remaining - mass) > 1e-8) throw new ArgumentException("Charge mass mismatch.");
        return charge;
    }
}

public sealed class WorkupJob
{
    public string Mode = "", Input = "", Supplement = "";
    public double Energy;
    public WorkupJob Copy() => (WorkupJob)MemberwiseClone();
    public Dictionary<string,string> Save()
    {
        if (Input == null || Supplement == null || Input == "none" || Supplement == "none" ||
            (Mode != "" && Mode != "recover" && Mode != "formulate" && BenchConversions.ForMode(Mode) == null) || !CropState.Finite(Energy) || Energy < 0 || Energy > 100 ||
            (Mode.Length == 0 && (Input.Length > 0 || Supplement.Length > 0 || Energy != 0)) || (Mode.Length > 0 && Input.Length == 0) ||
            (Mode == "formulate" && Supplement.Length == 0) || (Mode != "formulate" && Supplement.Length > 0)) throw new ArgumentException("Invalid workup job.");
        var fields = new Dictionary<string,string> { ["mode"] = Mode.Length == 0 ? "none" : Mode,
            ["input"] = Input.Length == 0 ? "none" : Input, ["supplement"] = Supplement.Length == 0 ? "none" : Supplement,
            ["energy"] = Energy.ToString("R", CultureInfo.InvariantCulture) };
        foreach(var value in fields.Values) if(!ObjectStateStore.SafeValue(value)) throw new ArgumentException("Unsafe workup identity.");
        return fields;
    }
    public static WorkupJob Read(IReadOnlyDictionary<string,string> d)
    {
        if (d.Count != 4) throw new ArgumentException("Unknown workup job fields.");
        foreach(var value in d.Values) if(!ObjectStateStore.SafeValue(value)) throw new ArgumentException("Unsafe workup record.");
        var job = new WorkupJob { Mode=d["mode"]=="none"?"":d["mode"], Input=d["input"]=="none"?"":d["input"],
            Supplement=d["supplement"]=="none"?"":d["supplement"], Energy=double.Parse(d["energy"],CultureInfo.InvariantCulture) };
        job.Save(); return job;
    }
}
