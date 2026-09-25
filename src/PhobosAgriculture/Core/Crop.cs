using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosAgriculture.Core;

public sealed class Crop
{
    public readonly string Id;
    public readonly double Hours, KW, Seed, Final, Carbon, Nutrient, Water, Vapour, SeedCarbon;
    public Crop(string id, double hours, double kw, double seed, double final, double carbon, double nutrient, double water, double vapour, double seedCarbon)
    { Id = id; Hours = hours; KW = kw; Seed = seed; Final = final; Carbon = carbon; Nutrient = nutrient; Water = water; Vapour = vapour; SeedCarbon = seedCarbon; }
    public static Crop Get(string id) => id == "potato" ? Potato : id == "lettuce" ? Lettuce : id == "lettuce-seed" ? LettuceSeed : throw new ArgumentException("Unknown crop profile.");
    public static readonly Crop Potato = new("potato", 96, .75, .2, 5, .84, .04, 4.624, .2, .04);
    // Authored seed-production cycle: separate from the historic food cohort.
    public static readonly Crop LettuceSeed = new("lettuce-seed", 96, .4, .005, 1.2, .0725, .01, 1.356, .2, .0045);
    public static readonly Crop Lettuce = new("lettuce", 48, .4, .005, 1.2, .0605, .005, 1.2658, .1, .0045);
}

public sealed class CropState
{
    public string CropId = "", Cohort = "", CookerInput = "";
    public double Progress, Health = 1, Water, Nutrients, Biomass, Carbon, Pace = 1, DarkHours, CookerProgress;
    public bool Running, Receiving;
    public int RecoveryRevision; // Zero preserves pre-recovery cohorts without assigning them an assay.
    public const double ReservoirKg = 20, NutrientCapacityKg = .5, HeatPerCarbonKWh = 17 / 3.6;
    public double ContentsMass => Water + Nutrients + Biomass;
    public bool Ready => CropId.Length > 0 && Progress >= 1 - 1e-10 && Health > 0;
    public double DemandKW => Running && CropId.Length > 0 && Progress < 1 && Health > 0 ? Math.Min(1.5, Crop.Get(CropId).KW / Pace) : .02;
    public CropState Copy() => (CropState)MemberwiseClone();
    public HarvestBudget Harvest(bool clear = false)
    {
        if (CropId.Length == 0 || !clear && !Ready) throw new InvalidOperationException("No harvestable cohort.");
        bool potato = CropId == "potato";
        double edible = clear ? 0 : Biomass * (potato ? 4.2 / 5 : CropId == "lettuce-seed" ? .02 / 1.2 : 1 / 1.2) * Health;
        double seed = potato && edible >= .2 ? .2 : 0;
        double portion = potato ? .4 : CropId == "lettuce-seed" ? Crop.LettuceSeed.Seed : .25;
        int count = (int)Math.Floor((edible - seed + 1e-8) / portion);
        return new HarvestBudget(seed, count, portion, Math.Max(0, Biomass - seed - count * portion));
    }
    public void Plant(Crop profile, double pace)
    {
        if (CropId.Length > 0 || !Finite(pace) || pace < .5 || pace > 2) throw new InvalidOperationException("Rack is occupied or pace is invalid.");
        RecoveryRevision = 1; CropId = profile.Id; Cohort = Guid.NewGuid().ToString("N"); Progress = 0; Health = 1; DarkHours = 0;
        Biomass = profile.Seed; Carbon = profile.SeedCarbon; Pace = pace; Running = true;
    }
    public void ClearCrop() { RecoveryRevision = 0; CropId = Cohort = ""; Progress = Biomass = Carbon = DarkHours = 0; Health = 1; Running = false; }

    // Effective continuously lit, NET healthy-growth surrogate. Dark/blocked time respires
    // explicitly; no oxygen timer and no bank of power credit. All quantities are kg/kWh.
    public Exchange Step(double hours, double electricKWh, double co2Kg, double oxygenKg, bool habitable, NutrientSolution? solution = null)
    {
        foreach (double x in new[] { hours, electricKWh, co2Kg, oxygenKg }) if (!Finite(x) || x < 0) throw new ArgumentException("Invalid crop step.");
        if (hours > 1) throw new ArgumentException("Settle cultivation in at most one-hour steps.");
        var exchange = new Exchange { RoomHeatKWh = electricKWh };
        solution?.Validate(this);
        if (CropId.Length == 0 || hours == 0) return exchange;
        var c = Crop.Get(CropId);
        double grow = Running && Health > 0 && habitable ? Math.Min(1 - Progress, Math.Min(hours / (c.Hours * Pace), electricKWh / (c.Hours * c.KW))) : 0;
        double solutionGrowth = solution == null ? 0 : Math.Min(solution.Quantity.CarrierKg / c.Water, solution.Quantity.SoluteKg / c.Nutrient);
        grow = Math.Max(0, Math.Min(grow, Math.Min(solutionGrowth + Math.Min(Water / c.Water, Nutrients / c.Nutrient), co2Kg / (c.Carbon * 44 / 30))));
        double mixedGrowth = Math.Min(grow, solutionGrowth);
        if (solution != null) solution.Quantity = new(Math.Max(0, solution.Quantity.CarrierKg - mixedGrowth * c.Water), Math.Max(0, solution.Quantity.SoluteKg - mixedGrowth * c.Nutrient));
        Water -= (grow - mixedGrowth) * c.Water; Nutrients -= (grow - mixedGrowth) * c.Nutrient; Biomass += grow * (c.Final - c.Seed); Carbon += grow * c.Carbon; Progress = Math.Min(1, Progress + grow);
        exchange.CO2Kg = -grow * c.Carbon * 44 / 30; exchange.OxygenKg = grow * c.Carbon * 32 / 30; exchange.VapourKg = grow * c.Vapour;
        exchange.RoomHeatKWh -= grow * (c.Carbon * HeatPerCarbonKWh + c.Vapour * 2.45 / 3.6);
        double dark = Math.Max(0, hours - grow * c.Hours * Pace);
        double respired = Math.Min(Carbon, Math.Min(oxygenKg * 30 / 32, Carbon * (1 - Math.Exp(-dark * .0005))));
        Carbon -= respired; Biomass -= respired;
        double returnedWater = respired * 18 / 30;
        double retained = Math.Min(returnedWater, Math.Max(0, ReservoirKg - Water - (solution?.TotalKg ?? 0))); Water += retained;
        exchange.VapourKg += returnedWater - retained;
        exchange.CO2Kg += respired * 44 / 30; exchange.OxygenKg -= respired * 32 / 30;
        exchange.RoomHeatKWh += respired * HeatPerCarbonKWh - (returnedWater - retained) * 2.45 / 3.6;
        // Harvest-ready crops still respire, but ordinary retention does not count as an irrigation failure.
        bool stressed = !habitable || Health <= 0 || (Progress < 1 && dark > hours * .5) || Water + (solution?.Quantity.CarrierKg ?? 0) < .01;
        double previousStress = DarkHours;
        DarkHours = stressed ? DarkHours + hours : Math.Max(0, DarkHours - hours);
        double damagingHours = Math.Max(0, Math.Max(0, DarkHours - 2) - Math.Max(0, previousStress - 2));
        Health = Math.Max(0, Health - damagingHours * (habitable ? .01 : .1));
        if (Health == 0) Running = false;
        Validate(); solution?.Validate(this); return exchange;
    }
    public void Validate()
    {
        foreach (double x in new[] { Progress, Health, Water, Nutrients, Biomass, Carbon, Pace, DarkHours, CookerProgress })
            if (!Finite(x) || x < -1e-9) throw new ArgumentException("Invalid saved crop quantity.");
        if (RecoveryRevision < 0 || RecoveryRevision > 1) throw new ArgumentException("Unknown crop recovery revision.");
        if (Progress > 1 || Health > 1 || Water > ReservoirKg + 1e-9 || Nutrients > NutrientCapacityKg + 1e-9 || Pace < .5 || Pace > 2 || Carbon > Biomass + 1e-9 || CookerProgress > .05 + 1e-9)
            throw new ArgumentException("Saved crop quantity exceeds its bound.");
        if (CropId.Length > 0) { var c = Crop.Get(CropId); if (Cohort.Length != 32 || Biomass > c.Final + 1e-8) throw new ArgumentException("Invalid saved cohort."); }
        else if (Biomass > 1e-9 || Carbon > 1e-9 || Progress > 0 || Cohort.Length > 0) throw new ArgumentException("Unbound biomass.");
    }
    public Dictionary<string, string> Save()
    {
        Validate(); var d = new Dictionary<string, string> { ["crop"] = CropId.Length == 0 ? "empty" : CropId, ["cohort"] = Cohort.Length == 0 ? "none" : Cohort, ["cookerInput"] = CookerInput.Length == 0 ? "none" : CookerInput };
        foreach (var p in new Dictionary<string, double> { ["progress"] = Progress, ["health"] = Health, ["water"] = Water, ["nutrients"] = Nutrients, ["biomass"] = Biomass, ["carbon"] = Carbon, ["pace"] = Pace, ["dark"] = DarkHours, ["cooker"] = CookerProgress }) d[p.Key] = p.Value.ToString("R", CultureInfo.InvariantCulture);
        d["recoveryRevision"] = RecoveryRevision.ToString(CultureInfo.InvariantCulture);
        return d; // Run/receive permission deliberately does not survive reload.
    }
    public static CropState Read(IReadOnlyDictionary<string, string> d)
    {
        double N(string k) => double.Parse(d[k], CultureInfo.InvariantCulture);
        var s = new CropState { CropId = d["crop"] == "empty" ? "" : d["crop"], Cohort = d["cohort"] == "none" ? "" : d["cohort"], Progress = N("progress"), Health = N("health"), Water = N("water"), Nutrients = N("nutrients"), Biomass = N("biomass"), Carbon = N("carbon"), Pace = N("pace"), DarkHours = N("dark"), CookerProgress = N("cooker") };
        s.CookerInput = d["cookerInput"] == "none" ? "" : d["cookerInput"];
        s.RecoveryRevision = d.TryGetValue("recoveryRevision", out var revision) ? int.Parse(revision, CultureInfo.InvariantCulture) : 0;
        if (d.Count != (d.ContainsKey("recoveryRevision") ? 13 : 12)) throw new ArgumentException("Unknown crop fields.");
        s.Validate(); return s;
    }
    public static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
}
public sealed class Exchange { public double CO2Kg, OxygenKg, VapourKg, RoomHeatKWh; }
public readonly struct HarvestBudget
{
    public readonly double SeedKg, PortionKg, ResidueKg;
    public readonly int Portions;
    public HarvestBudget(double seed, int portions, double portion, double residue) { SeedKg = seed; Portions = portions; PortionKg = portion; ResidueKg = residue; }
}
