using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace PhobosMedical.Core;

/// <summary>The <c>care</c> schema (Phobos Medical 0.1.0; owner decision, 4 October 2026: authored tables are data
/// from the start). What a player may tune lives here: each station's electrical demand and the thresholds that decide
/// who counts as injured and when a resting patient has recovered. The rules that act on them stay in code
/// (<see cref="BedRules"/>). Later sets add sections (care levels, treatments); a player file written for this one
/// stays valid.</summary>
public sealed class CarePack : DataPack
{
    /// <summary>Electrical demand by station: <c>bed</c>.</summary>
    public Dictionary<string, StationEntry> stations = new(StringComparer.Ordinal);
    public AdmissionEntry admission = new();
    /// <summary>What each station adds to the game's own care (Medical 0.2.0); absent means nothing added.</summary>
    public Dictionary<string, LevelEntry>? levels;
    /// <summary>When the Vigil-2 warns (Medical 0.3.0); absent means the shipped defaults.</summary>
    public AlertEntry? alerts;
    /// <summary>What a medic does for a patient in a Ward-3 (Medical 0.4.0), by name; absent means nothing. Players may
    /// tune or add entries; code acts on the fixed tests and effects in <see cref="TreatmentRules"/>, never on a name.</summary>
    public Dictionary<string, TreatmentEntry>? treatments;
}

/// <summary>One treatment: which wounds it is for (<see cref="test"/>), what is done (<see cref="effect"/>), the one
/// real item it uses up, how long a medic takes, the skill that makes it quicker and its place in the order of care.</summary>
public sealed class TreatmentEntry
{
    public string? notes;
    /// <summary><c>bleeding</c>, <c>fracture</c> or <c>spent-dressing</c>.</summary>
    public string test = "";
    /// <summary><c>slot-item</c>: the item goes onto the wound the game's own way.</summary>
    public string effect = TreatmentRules.SlotItem;
    /// <summary>The game definition of the item used up, one unit per treatment.</summary>
    public string item = "";
    public double medicSeconds;
    /// <summary>A skill condition (the game's own, such as <c>SkillMedicalTrauma</c>); crew who have it work faster and
    /// are asked first. Empty means anyone, at the ordinary pace.</summary>
    public string? skill;
    /// <summary>Lower comes first; ties go by name.</summary>
    public int order;
}

/// <summary>The figures at which a Vigil-2 posts an alert, on the same scales as admission. Each must sit below the
/// game's fatal or knock-out level. A wound that starts bleeding always alerts.</summary>
public sealed class AlertEntry
{
    public string? notes;
    public double bloodLost = 15, infection = 35, pain = 50;
}

/// <summary>The bed's additions to the game's Recuperating.</summary>
public sealed class LevelEntry
{
    public string? notes;
    /// <summary>Share of normal wound healing a weightless patient keeps while under care, from the game's own 0.05
    /// (no lift) to 1 (no weightless penalty at all). Needs Framework's wound-gravity patch.</summary>
    public double weightlessHealing = Phobos.Ostranauts.Framework.Health.WoundGravity.NativeFactor;
}

public sealed class StationEntry
{
    public string? notes;
    /// <summary>Demand when switched on with nobody under care, and while giving care, in kW.</summary>
    public double idleKW, workingKW;
}

/// <summary>Who counts as injured. A person is injured when any figure reaches its threshold, a wound is bleeding, or
/// a fracture is unsplinted; a resting patient has recovered when every figure is under its threshold times
/// <see cref="dischargeShare"/> and nothing bleeds. The figures are the game's own scales: blood lost (the game's
/// shock bands start at 15 and 30, and 40 is fatal), infection (35, 65, 95 fatal), pain (25, 50, 75 knocks out) and the
/// worst wound's cut or blunt damage (0 to 1).</summary>
public sealed class AdmissionEntry
{
    public string? notes;
    public double bloodLost, infection, pain, wound, dischargeShare;
}

public static class CareSchema
{
    public const string Name = "care";
    public const string Bed = "bed";
    public const string Monitor = "monitor";
    public static readonly IReadOnlyList<string> Stations = new[] { Bed, Monitor };
    public const double MaxKW = 2;
    /// <summary>Stations that can add to the game's care: the bed (Medical 0.2.0).</summary>
    public static readonly IReadOnlyList<string> Levels = new[] { Bed };
    public const double MinWeightless = Phobos.Ostranauts.Framework.Health.WoundGravity.NativeFactor;
    /// <summary>The game's fatal or knock-out levels: an admission threshold must sit below them to mean anything.</summary>
    public const double FatalBloodLost = 40, FatalInfection = 95, KnockoutPain = 75;

    /// <summary>The checks every file passes, shipped or player. Pricing and balance are authoring rules for shipped
    /// data and are not enforced here.</summary>
    public static void Validate(CarePack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        foreach (string station in Stations)
            if (!pack.stations.ContainsKey(station)) throw new ArgumentException(Text.Get("care_station_missing", station));
        foreach (var pair in pack.stations)
        {
            if (!Stations.Contains(pair.Key)) throw new ArgumentException(Text.Get("care_station_unknown", pair.Key, string.Join(", ", Stations)));
            var s = pair.Value;
            if (!Finite(s.idleKW) || !Finite(s.workingKW) || s.idleKW < 0 || s.workingKW <= 0 || s.workingKW > MaxKW || s.idleKW > s.workingKW)
                throw new ArgumentException(Text.Get("care_station_kw", pair.Key, MaxKW));
        }
        var a = pack.admission ?? throw new ArgumentException(Text.Get("care_admission_missing"));
        Between(a.bloodLost, FatalBloodLost, "bloodLost");
        Between(a.infection, FatalInfection, "infection");
        Between(a.pain, KnockoutPain, "pain");
        Between(a.wound, 1, "wound");
        if (!Finite(a.dischargeShare) || a.dischargeShare < 0 || a.dischargeShare >= 1) throw new ArgumentException(Text.Get("care_discharge"));
        if (pack.alerts is { } alerts)
        {
            Between(alerts.bloodLost, FatalBloodLost, "alerts/bloodLost");
            Between(alerts.infection, FatalInfection, "alerts/infection");
            Between(alerts.pain, KnockoutPain, "alerts/pain");
        }
        foreach (var pair in pack.treatments ?? new Dictionary<string, TreatmentEntry>())
            TreatmentRules.Validate(pair.Key, pair.Value);
        foreach (var pair in pack.levels ?? new Dictionary<string, LevelEntry>())
        {
            if (!Levels.Contains(pair.Key)) throw new ArgumentException(Text.Get("care_level_unknown", pair.Key, string.Join(", ", Levels)));
            double w = pair.Value?.weightlessHealing ?? double.NaN;
            if (!Finite(w) || w < MinWeightless || w > 1) throw new ArgumentException(Text.Get("care_weightless", pair.Key, MinWeightless));
        }
    }
    private static void Between(double value, double below, string field)
    {
        if (!Finite(value) || value <= 0 || value >= below) throw new ArgumentException(Text.Get("care_admission_range", field, below));
    }
    internal static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
}

/// <summary>Phobos Medical's care pack, from <c>framework/care.json</c> and player files in
/// <c>BepInEx/config/PhobosMedical/care</c>. Loaded on first use (offline checks) and again at each content load.</summary>
public static class Care
{
    public const string Resource = "PhobosMedical.care.json";
    private static CarePack? pack;
    public static CarePack Pack => pack ??= Load();
    public static DataPackSource Source => new(MedicalRules.Owner, MedicalRules.ModFolder, CareSchema.Name, typeof(Care).Assembly, Resource);
    public static CarePack Load() => pack = DataPacks.Load<CarePack>(Source, CareSchema.Validate);
    /// <summary>Adopts a pack built from text (offline checks).</summary>
    public static void Use(CarePack loaded) { CareSchema.Validate(loaded); pack = loaded; }
    public static StationEntry Station(string station) => Pack.stations.TryGetValue(station, out var s) ? s : throw new InvalidOperationException("No care station " + station);
    public static AdmissionEntry Admission => Pack.admission;
    /// <summary>The share of normal healing a weightless patient keeps in this station's care; the game's own 0.05 when the pack says nothing.</summary>
    public static AlertEntry Alerts => Pack.alerts ?? new AlertEntry();
    /// <summary>The treatments in the order of care: by <c>order</c>, then by name.</summary>
    public static IReadOnlyList<KeyValuePair<string, TreatmentEntry>> Treatments =>
        (Pack.treatments ?? new Dictionary<string, TreatmentEntry>()).OrderBy(p => p.Value.order).ThenBy(p => p.Key, StringComparer.Ordinal).ToList();
    public static double WeightlessHealing(string station) => Pack.levels != null && Pack.levels.TryGetValue(station, out var l) ? l.weightlessHealing : CareSchema.MinWeightless;
}
