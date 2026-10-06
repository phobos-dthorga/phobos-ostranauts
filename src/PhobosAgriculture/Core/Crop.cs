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
    /// <param name="stress">The stress rules (Agriculture 0.55.0); the crops pack's own when left out.</param>
    /// <param name="outsideDamageScale">The share of out-of-room damage that applies (Agriculture 0.59.0): misting a crop in a
    /// room too hot for it slows the loss. 1 is the full rate.</param>
    /// <param name="tune">Crew upkeep (Agriculture 0.63.0): how much faster a tuned rack grows per hour. Its lamps drew
    /// that much more power for the step, so the energy and everything else a unit of growth takes are unchanged. 1 is untuned.</param>
    /// <param name="carbonShortfallHarmless">Agriculture 0.65.0, for time skips: a step held back only by the room's
    /// carbon dioxide grows what that CO2 allows and no more, but does not count as stress. The game pauses breathing
    /// during a skip, so a crop fed by the crew's breath would otherwise be harmed by every skip. No mass is created.</param>
    public Exchange Step(double hours, double electricKWh, double co2Kg, double oxygenKg, bool habitable, double co2Factor = 1, StressRules? stress = null, double outsideDamageScale = 1, double tune = 1, bool carbonShortfallHarmless = false)
    {
        if (!Finite(tune) || tune < 1 || tune > MaxTune) throw new ArgumentException("Invalid tune.");
        var rules = stress ?? Growth.Stress;
        if (!Finite(outsideDamageScale) || outsideDamageScale < 0 || outsideDamageScale > 1) throw new ArgumentException("Invalid damage scale.");
        foreach (double x in new[] { hours, electricKWh, co2Kg, oxygenKg }) if (!Finite(x) || x < 0) throw new ArgumentException("Invalid crop step.");
        if (!Finite(co2Factor) || co2Factor < Co2Response.MinFactor || co2Factor > Co2Response.MaxFactor) throw new ArgumentException("Invalid carbon dioxide response.");
        if (hours > 1) throw new ArgumentException("Settle cultivation in at most one-hour steps.");
        var exchange = new Exchange { RoomHeatKWh = electricKWh };
        if (CropId.Length == 0 || hours == 0) return exchange;
        var c = Crop.Get(CropId);
        double timeCap = hours * co2Factor * tune / (c.Hours * Pace), energyCap = electricKWh * co2Factor / (c.Hours * c.KW);
        double waterCap = Water / c.Water, nutrientCap = Nutrients / c.Nutrient, carbonCap = co2Kg / (c.Carbon * 44 / 30);
        double grow = Running && Health > 0 && habitable ? Math.Min(1 - Progress, Math.Min(timeCap, energyCap)) : 0;
        // One water store and one nutrient store since Agriculture 0.55.0 (the per-crop feed is gone): each crop draws
        // its own amounts of both.
        grow = Math.Max(0, Math.Min(grow, Math.Min(Math.Min(waterCap, nutrientCap), carbonCap)));
        // What held this step below the crop's own rate (Agriculture 0.65.0): the room, a pause, or the scarcest supply.
        var limit = !habitable ? GrowthLimit.Room : !Running ? GrowthLimit.Paused : GrowthLimit.None;
        if (limit == GrowthLimit.None && Health > 0 && Progress < 1 && grow < Math.Min(1 - Progress, timeCap) * (1 - 1e-9))
        {
            double least = Math.Min(Math.Min(energyCap, waterCap), Math.Min(nutrientCap, carbonCap));
            limit = least == energyCap ? GrowthLimit.Power : least == waterCap ? GrowthLimit.Water : least == nutrientCap ? GrowthLimit.Nutrients : GrowthLimit.CarbonDioxide;
        }
        Water = Math.Max(0, Water - grow * c.Water); Nutrients = Math.Max(0, Nutrients - grow * c.Nutrient); Biomass += grow * (c.Final - c.Seed); Carbon += grow * c.Carbon; Progress = Math.Min(1, Progress + grow);
        // Transpired water condenses inside the closed rack and returns to the plain-water reservoir while it
        // has room: the game's atmosphere has no water vapour to receive it, and the latent heat of what
        // condenses nets to zero. Only an overfull reservoir leaves any vapour.
        double transpired = grow * c.Vapour, condensed = Math.Min(transpired, Math.Max(0, ReservoirKg - Water));
        Water += condensed;
        exchange.CO2Kg = -grow * c.Carbon * 44 / 30; exchange.OxygenKg = grow * c.Carbon * 32 / 30; exchange.VapourKg = transpired - condensed;
        exchange.RoomHeatKWh -= grow * c.Carbon * HeatPerCarbonKWh + (transpired - condensed) * LatentKWhPerKg;
        double dark = Math.Max(0, hours - grow * c.Hours * Pace / (co2Factor * tune));
        double respired = Math.Min(Carbon, Math.Min(oxygenKg * 30 / 32, Carbon * (1 - Math.Exp(-dark * .0005))));
        Carbon -= respired; Biomass -= respired;
        double returnedWater = respired * 18 / 30;
        double retained = Math.Min(returnedWater, Math.Max(0, ReservoirKg - Water)); Water += retained;
        exchange.VapourKg += returnedWater - retained;
        exchange.CO2Kg += respired * 44 / 30; exchange.OxygenKg -= respired * 32 / 30;
        exchange.RoomHeatKWh += respired * HeatPerCarbonKWh - (returnedWater - retained) * LatentKWhPerKg;
        // Harvest-ready crops still respire, but ordinary retention does not count as an irrigation failure.
        bool carbonStall = carbonShortfallHarmless && limit == GrowthLimit.CarbonDioxide;
        bool stressed = !habitable || Health <= 0 || (Progress < 1 && dark > hours * .5 && !carbonStall) || Water < .01;
        exchange.Limit = stressed && limit == GrowthLimit.None && Water < .01 ? GrowthLimit.Water : limit; exchange.Stressed = stressed;
        double previousStress = DarkHours;
        DarkHours = stressed ? DarkHours + hours : Math.Max(0, DarkHours - hours);
        double damagingHours = Math.Max(0, Math.Max(0, DarkHours - rules.GraceHours) - Math.Max(0, previousStress - rules.GraceHours));
        Health = Math.Max(0, Health - damagingHours * (habitable ? rules.HealthLossPerHour : rules.HealthLossPerHourOutside * outsideDamageScale));
        if (Health == 0) Running = false;
        Validate(); return exchange;
    }
    /// <summary>The most a tune may raise the growth rate: the top of the player's setting.</summary>
    public const double MaxTune = 1.25;
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
    /// <summary>Latent heat of evaporating water, 2.45 MJ per kg, in kWh per kg.</summary>
    public const double LatentKWhPerKg = 2.45 / 3.6;
}

public sealed class Exchange
{
    public double CO2Kg, OxygenKg, VapourKg, RoomHeatKWh;
    /// <summary>What held the step below the crop's own rate (Agriculture 0.65.0); None when it grew at its full rate.</summary>
    public GrowthLimit Limit;
    /// <summary>Whether the step counted as poor growing conditions.</summary>
    public bool Stressed;
}
/// <summary>What held a crop's growth back in a step, in the order the panel names it (Agriculture 0.65.0).</summary>
public enum GrowthLimit { None, Paused, Power, Water, Nutrients, CarbonDioxide, Room }
/// <summary>How Agriculture treats a room while a time skip steps its machines (Agriculture 0.65.0; owner decision,
/// 6 October 2026). The game pauses breathing, scrubbers, coolers and the air through open doors during a skip, so by
/// default Agriculture machines neither heat their room nor are limited by its temperature then, and a crop short of
/// carbon dioxide only waits. The player's setting keeps room conditions on in skips despite the risks.</summary>
public static class SkipRoom
{
    /// <summary>Whether the lenient skip rules apply to this step.</summary>
    public static bool Lenient(bool skipping, bool roomConditionsInSkips) => skipping && !roomConditionsInSkips;
}
public readonly struct HarvestBudget
{
    public readonly double SeedKg, PortionKg, ResidueKg;
    public readonly int Portions;
    public HarvestBudget(double seed, int portions, double portion, double residue) { SeedKg = seed; Portions = portions; PortionKg = portion; ResidueKg = residue; }
}
