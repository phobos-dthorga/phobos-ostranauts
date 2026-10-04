using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhobosAgriculture.Core;

/// <summary>One crop from the <c>crops</c> data pack (<see cref="Crops"/>): the budgets a planting is settled by, what
/// its harvest gives and the names it is planted, fed and drawn under. A saved planting stores only <see cref="Id"/>.</summary>
public sealed class Crop
{
    public readonly string Id, Stock, Produce, Feed, FeedCommodity, Art;
    private readonly string? plainName;
    /// <summary>The crop as players read it: the catalogue's wording, or the plain name a player file gave a crop it added.</summary>
    public string Name => Text.Has(Id) ? Text.Get(Id) : plainName ?? Id;
    public readonly double Hours, KW, Seed, Final, Carbon, Nutrient, Water, Vapour, SeedCarbon, EdibleKg, KeptStockKg, PortionKg, PickKg;
    /// <summary>Picks a ripe plant allows before its final harvest; zero for a crop harvested once.</summary>
    public readonly int Picks;
    public Crop(string id, CropEntry e)
    {
        Id = id; Hours = e.hours; KW = e.kw; Seed = e.seedKg; Final = e.finalKg; Carbon = e.carbonKg; Nutrient = e.nutrientKg; Water = e.waterKg; Vapour = e.vapourKg;
        SeedCarbon = e.seedCarbonKg; EdibleKg = e.edibleKg; KeptStockKg = e.keptStockKg; PortionKg = e.portionKg; Picks = e.picks; PickKg = e.pickKg;
        Stock = e.stock; Produce = e.produce; Feed = e.feed; FeedCommodity = e.feedCommodity; Art = e.art; plainName = e.name;
    }
    public static Crop Get(string id) => Crops.Find(id) ?? throw new ArgumentException("Unknown crop profile.");
}

public sealed class CropState
{
    public string CropId = "", Cohort = "", CookerInput = "";
    public double Progress, Health = 1, Water, Nutrients, Biomass, Carbon, Pace = 1, DarkHours, CookerProgress;
    public bool Running, Receiving;
    public int RecoveryRevision; // Zero preserves pre-recovery cohorts without assigning them an assay.
    /// <summary>Picks taken from this planting (Agriculture 0.42.0); saved only when not zero, so older records read as none.</summary>
    public int Picks;
    public const double ReservoirKg = 20, NutrientCapacityKg = .5, HeatPerCarbonKWh = 17 / 3.6;
    public double ContentsMass => Water + Nutrients + Biomass;
    public bool Ready => CropId.Length > 0 && Progress >= 1 - 1e-10 && Health > 0;
    public double DemandKW => Running && CropId.Length > 0 && Progress < 1 && Health > 0 ? Math.Min(1.5, Crop.Get(CropId).KW / Pace) : .02;
    public CropState Copy() => (CropState)MemberwiseClone();
    public HarvestBudget Harvest(bool clear = false)
    {
        if (CropId.Length == 0 || !clear && !Ready) throw new InvalidOperationException("No harvestable cohort.");
        var crop = Crop.Get(CropId);
        double edible = clear ? 0 : Biomass * (crop.EdibleKg / crop.Final) * Health;
        double seed = crop.KeptStockKg > 0 && edible >= crop.KeptStockKg ? crop.KeptStockKg : 0;
        double portion = crop.PortionKg;
        int count = (int)Math.Floor((edible - seed + 1e-8) / portion);
        return new HarvestBudget(seed, count, portion, Math.Max(0, Biomass - seed - count * portion));
    }
    /// <summary>Whole portions a pick would take now: up to the crop's pick mass of its edible share, from a ripe
    /// plant with picks left. Zero when nothing can be picked.</summary>
    public int PickPortions()
    {
        if (CropId.Length == 0 || !Ready) return 0;
        var crop = Crop.Get(CropId);
        if (Picks >= crop.Picks) return 0;
        double edible = Math.Min(crop.PickKg, Biomass * (crop.EdibleKg / crop.Final) * Health);
        return Math.Max(0, (int)Math.Floor((edible + 1e-8) / crop.PortionKg));
    }
    /// <summary>Takes <paramref name="portions"/> picked portions from the plant and sets its growth back by exactly the
    /// share of a cycle they were: regrowing them uses the same water, nutrient and carbon budget per unit of progress
    /// that grew them, so a healthy plant returns to the state it was picked from.</summary>
    public void Pick(int portions)
    {
        var crop = Crop.Get(CropId);
        if (portions < 1 || portions > PickPortions()) throw new InvalidOperationException("Nothing to pick.");
        double kg = portions * crop.PortionKg, grown = crop.Final - crop.Seed;
        Biomass -= kg;
        Carbon = Math.Max(0, Math.Min(Biomass, Carbon - kg * crop.Carbon / grown));
        Progress = Math.Max(0, Progress - kg / grown);
        Picks++;
        Validate();
    }
    public void Plant(Crop profile, double pace)
    {
        if (CropId.Length > 0 || !Finite(pace) || pace < .5 || pace > 2) throw new InvalidOperationException("Rack is occupied or pace is invalid.");
        RecoveryRevision = 1; CropId = profile.Id; Cohort = Guid.NewGuid().ToString("N"); Progress = 0; Health = 1; DarkHours = 0; Picks = 0;
        Biomass = profile.Seed; Carbon = profile.SeedCarbon; Pace = pace; Running = true;
    }
    public void ClearCrop() { RecoveryRevision = 0; CropId = Cohort = ""; Progress = Biomass = Carbon = DarkHours = 0; Health = 1; Running = false; Picks = 0; }

    // Effective continuously lit, NET healthy-growth surrogate. Dark/blocked time respires
    // explicitly; no oxygen timer and no bank of power credit. All quantities are kg/kWh.
    /// <param name="co2Factor">How much faster the crop grows per hour and per kWh in its room's carbon dioxide
    /// (Agriculture 0.43.0, <see cref="Co2Response"/>); 1 is the historic rate. Budgets per unit of growth are
    /// unchanged, so enrichment shortens the cycle and its energy without changing what the crop takes or gives.</param>
    public Exchange Step(double hours, double electricKWh, double co2Kg, double oxygenKg, bool habitable, NutrientSolution? solution = null, double co2Factor = 1)
    {
        foreach (double x in new[] { hours, electricKWh, co2Kg, oxygenKg }) if (!Finite(x) || x < 0) throw new ArgumentException("Invalid crop step.");
        if (!Finite(co2Factor) || co2Factor < Co2Response.MinFactor || co2Factor > Co2Response.MaxFactor) throw new ArgumentException("Invalid carbon dioxide response.");
        if (hours > 1) throw new ArgumentException("Settle cultivation in at most one-hour steps.");
        var exchange = new Exchange { RoomHeatKWh = electricKWh };
        solution?.Validate(this);
        if (CropId.Length == 0 || hours == 0) return exchange;
        var c = Crop.Get(CropId);
        double grow = Running && Health > 0 && habitable ? Math.Min(1 - Progress, Math.Min(hours * co2Factor / (c.Hours * Pace), electricKWh * co2Factor / (c.Hours * c.KW))) : 0;
        double solutionGrowth = solution == null ? 0 : Math.Min(solution.Quantity.CarrierKg / c.Water, solution.Quantity.SoluteKg / c.Nutrient);
        grow = Math.Max(0, Math.Min(grow, Math.Min(solutionGrowth + Math.Min(Water / c.Water, Nutrients / c.Nutrient), co2Kg / (c.Carbon * 44 / 30))));
        double mixedGrowth = Math.Min(grow, solutionGrowth);
        if (solution != null) solution.Quantity = new(Math.Max(0, solution.Quantity.CarrierKg - mixedGrowth * c.Water), Math.Max(0, solution.Quantity.SoluteKg - mixedGrowth * c.Nutrient));
        Water -= (grow - mixedGrowth) * c.Water; Nutrients -= (grow - mixedGrowth) * c.Nutrient; Biomass += grow * (c.Final - c.Seed); Carbon += grow * c.Carbon; Progress = Math.Min(1, Progress + grow);
        // Transpired water condenses inside the closed rack and returns to the plain-water reservoir while it
        // has room: the game's atmosphere has no water vapour to receive it, and the latent heat of what
        // condenses nets to zero. Only an overfull reservoir leaves any vapour.
        double transpired = grow * c.Vapour, condensed = Math.Min(transpired, Math.Max(0, ReservoirKg - Water - (solution?.TotalKg ?? 0)));
        Water += condensed;
        exchange.CO2Kg = -grow * c.Carbon * 44 / 30; exchange.OxygenKg = grow * c.Carbon * 32 / 30; exchange.VapourKg = transpired - condensed;
        exchange.RoomHeatKWh -= grow * c.Carbon * HeatPerCarbonKWh + (transpired - condensed) * 2.45 / 3.6;
        double dark = Math.Max(0, hours - grow * c.Hours * Pace / co2Factor);
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
        if (Progress > 1 || Health > 1 || Water > ReservoirKg + 1e-9 || Nutrients > NutrientCapacityKg + 1e-9 || Pace < .5 || Pace > 2 || Carbon > Biomass + 1e-9 || CookerProgress > HearthRecipes.MaxKWh + 1e-9)
            throw new ArgumentException("Saved crop quantity exceeds its bound.");
        if (CropId.Length > 0) { var c = Crop.Get(CropId); if (Cohort.Length != 32 || Biomass > c.Final + 1e-8 || Picks < 0 || Picks > c.Picks) throw new ArgumentException("Invalid saved cohort."); }
        else if (Biomass > 1e-9 || Carbon > 1e-9 || Progress > 0 || Cohort.Length > 0 || Picks != 0) throw new ArgumentException("Unbound biomass.");
    }
    public Dictionary<string, string> Save()
    {
        Validate(); var d = new Dictionary<string, string> { ["crop"] = CropId.Length == 0 ? "empty" : CropId, ["cohort"] = Cohort.Length == 0 ? "none" : Cohort, ["cookerInput"] = CookerInput.Length == 0 ? "none" : CookerInput };
        string N(double v) => v.ToString("R", CultureInfo.InvariantCulture);
        d["progress"] = N(Progress); d["health"] = N(Health); d["water"] = N(Water); d["nutrients"] = N(Nutrients); d["biomass"] = N(Biomass);
        d["carbon"] = N(Carbon); d["pace"] = N(Pace); d["dark"] = N(DarkHours); d["cooker"] = N(CookerProgress);
        d["recoveryRevision"] = RecoveryRevision.ToString(CultureInfo.InvariantCulture);
        if (Picks > 0) d["picks"] = Picks.ToString(CultureInfo.InvariantCulture);
        return d; // Run/receive permission deliberately does not survive reload.
    }
    public static CropState Read(IReadOnlyDictionary<string, string> d)
    {
        double N(string k) => double.Parse(d[k], CultureInfo.InvariantCulture);
        var s = new CropState { CropId = d["crop"] == "empty" ? "" : d["crop"], Cohort = d["cohort"] == "none" ? "" : d["cohort"], Progress = N("progress"), Health = N("health"), Water = N("water"), Nutrients = N("nutrients"), Biomass = N("biomass"), Carbon = N("carbon"), Pace = N("pace"), DarkHours = N("dark"), CookerProgress = N("cooker") };
        s.CookerInput = d["cookerInput"] == "none" ? "" : d["cookerInput"];
        s.RecoveryRevision = d.TryGetValue("recoveryRevision", out var revision) ? int.Parse(revision, CultureInfo.InvariantCulture) : 0;
        s.Picks = d.TryGetValue("picks", out var picks) ? int.Parse(picks, CultureInfo.InvariantCulture) : 0;
        if (d.ContainsKey("picks") && (!d.ContainsKey("recoveryRevision") || s.Picks < 1)) throw new ArgumentException("Unknown crop fields.");
        if (d.Count != 12 + (d.ContainsKey("recoveryRevision") ? 1 : 0) + (d.ContainsKey("picks") ? 1 : 0)) throw new ArgumentException("Unknown crop fields.");
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
