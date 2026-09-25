using System;
using System.Collections.Generic;
using System.Globalization;
using Phobos.Ostranauts.Framework.Liquids;

namespace PhobosAgriculture.Core;

/// <summary>Authored fresh-feed profiles, not real hydroponic concentrations or chemical assays.</summary>
public sealed class NutrientSolution
{
    public const string None = "water", Potato = "potato-v1", Lettuce = "lettuce-v1";
    public const double Tolerance = 1e-9;
    public string Profile = None;
    public LiquidMixture Quantity;
    public bool Enabled => Profile != None;
    public double TotalKg => Quantity.TotalKg;
    public NutrientSolution Copy() => (NutrientSolution)MemberwiseClone();
    public static string CropId(string profile) => profile == Potato ? "potato" : profile == Lettuce ? "lettuce" : profile == None ? "" : throw new ArgumentException("Unknown solution profile.");
    public static LiquidMixture Ratio(string profile) => profile == Potato ? new(4.624, .04) : profile == Lettuce ? new(1.2658, .005) : throw new ArgumentException("Water-only mode has no nutrient formulation.");
    public double PlainWaterCapacity => Math.Max(0, CropState.ReservoirKg - TotalKg);
    public double DryCapacity => Math.Max(0, CropState.NutrientCapacityKg - Quantity.SoluteKg);
    public bool CanPlant(string crop) => TotalKg <= Tolerance || CropId(Profile) == crop;
    public void Validate(CropState state)
    {
        _ = CropId(Profile);
        if (!CropState.Finite(Quantity.CarrierKg) || !CropState.Finite(Quantity.SoluteKg) || Quantity.CarrierKg < 0 || Quantity.SoluteKg < 0 ||
            state.Water + TotalKg > CropState.ReservoirKg + Tolerance || state.Nutrients + Quantity.SoluteKg > CropState.NutrientCapacityKg + Tolerance ||
            !Enabled && TotalKg > 0 || state.CropId.Length > 0 && !CanPlant(state.CropId)) throw new ArgumentException("Invalid solution contents or crop compatibility.");
        if (Enabled && !MixtureTransfer.Same(Quantity, Ratio(Profile).Scale(TotalKg / Ratio(Profile).TotalKg)))
            throw new ArgumentException("Saved formulation differs from its declared composition.");
    }
    public double BlendAllowance(CropState state, double budgetKg)
    {
        Validate(state);
        if (!Enabled || !CropState.Finite(budgetKg) || budgetKg <= 0) return 0;
        var unit = Ratio(Profile).Scale(1 / Ratio(Profile).TotalKg);
        // Moving water into the mixture changes no liquid mass; dissolving stock does.
        return Math.Max(0, Math.Min(budgetKg, Math.Min(state.Water / unit.CarrierKg,
            Math.Min(state.Nutrients / unit.SoluteKg, (CropState.ReservoirKg - state.Water - TotalKg) / unit.SoluteKg))));
    }
    public double Blend(CropState state, double budgetKg)
    {
        double amount = BlendAllowance(state, budgetKg);
        if (amount <= 0) return 0;
        var portion = Ratio(Profile).Scale(amount / Ratio(Profile).TotalKg);
        state.Water = Math.Max(0, state.Water - portion.CarrierKg); state.Nutrients = Math.Max(0, state.Nutrients - portion.SoluteKg);
        Quantity += portion; Validate(state); return amount;
    }
    public Dictionary<string, string> Save(CropState state)
    {
        Validate(state);
        return new() { ["profile"] = Profile, ["water"] = Quantity.CarrierKg.ToString("R", CultureInfo.InvariantCulture), ["nutrients"] = Quantity.SoluteKg.ToString("R", CultureInfo.InvariantCulture) };
    }
    public static NutrientSolution Read(IReadOnlyDictionary<string, string> fields, CropState state)
    {
        if (fields.Count != 3) throw new ArgumentException("Unknown solution fields.");
        var result = new NutrientSolution { Profile = fields["profile"], Quantity = new(double.Parse(fields["water"], CultureInfo.InvariantCulture), double.Parse(fields["nutrients"], CultureInfo.InvariantCulture)) };
        result.Validate(state); return result;
    }
}
