using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Phobos.Ostranauts.Framework.Data;

namespace Phobos.Ostranauts.Framework.Crew;

/// <summary>The <c>upkeep</c> schema (Framework 0.111.0; owner request, 6 October 2026): the figures behind crew
/// upkeep that are not ordinary player settings. Framework ships the pack; players and add-ons tune it.</summary>
public sealed class UpkeepPack : DataPack
{
    /// <summary>How much one tuning session raises a machine's tune (a share of a full tune), unskilled and skilled.</summary>
    public double tuneStep = 0.2, tuneStepSkilled = 0.3;
    /// <summary>Game hours an inspection stays good for.</summary>
    public double inspectionValidHours = 24;
    /// <summary>Share of the ordinary fade an inspected machine's tune takes.</summary>
    public double inspectedFadeShare = 0.5;
    /// <summary>A machine family's own share of the maximum gain, by family key; 1 when absent.</summary>
    public Dictionary<string, UpkeepFamilyEntry> families = new(StringComparer.Ordinal);
}

public sealed class UpkeepFamilyEntry
{
    public string? notes;
    /// <summary>This family's share of the player's maximum tuning gain (0 to 1); 0 leaves it untunable.</summary>
    public double gainShare = 1;
}

public static class UpkeepSchema
{
    public const string Name = "upkeep";
    public static void Validate(UpkeepPack pack)
    {
        if (pack == null) throw new ArgumentNullException(nameof(pack));
        Range(pack.tuneStep, 0.01, 1, "tuneStep"); Range(pack.tuneStepSkilled, 0.01, 1, "tuneStepSkilled");
        if (pack.tuneStepSkilled < pack.tuneStep) throw new ArgumentException(Text.Get("UpkeepSchema.skilled_less"));
        Range(pack.inspectionValidHours, 1, 240, "inspectionValidHours");
        Range(pack.inspectedFadeShare, 0, 1, "inspectedFadeShare");
        foreach (var pair in pack.families)
        {
            if (string.IsNullOrEmpty(pair.Key) || pair.Key.Length > 64 || pair.Value == null) throw new ArgumentException(Text.Get("UpkeepSchema.family", pair.Key ?? ""));
            Range(pair.Value.gainShare, 0, 1, "families." + pair.Key + ".gainShare");
        }
    }
    private static void Range(double value, double min, double max, string where)
    {
        if (double.IsNaN(value) || value < min || value > max) throw new ArgumentException(Text.Get("UpkeepSchema.range", where, min, max));
    }
}

/// <summary>The player's upkeep settings (BepInEx, section Upkeep) with their ranges. The inspection length is the
/// owner's choice; the other defaults are agent choices.</summary>
public sealed class UpkeepSettings
{
    public const double DefaultInspectionMinutes = 5, MinInspectionMinutes = 1, MaxInspectionMinutes = 30;
    public const double DefaultTuningMinutes = 10, MinTuningMinutes = 2, MaxTuningMinutes = 60;
    public const double DefaultMaxTuningGain = 0.10, MinMaxTuningGain = 0, MaxMaxTuningGain = 0.25;
    public const double DefaultTuneFadeHours = 36, MinTuneFadeHours = 6, MaxTuneFadeHours = 240;
    public double InspectionMinutes = DefaultInspectionMinutes, TuningMinutes = DefaultTuningMinutes,
        MaxTuningGain = DefaultMaxTuningGain, TuneFadeHours = DefaultTuneFadeHours;
    /// <summary>The settings held to their ranges; a value that is not a number falls back to its default.</summary>
    public UpkeepSettings Clamped() => new()
    {
        InspectionMinutes = Clamp(InspectionMinutes, MinInspectionMinutes, MaxInspectionMinutes, DefaultInspectionMinutes),
        TuningMinutes = Clamp(TuningMinutes, MinTuningMinutes, MaxTuningMinutes, DefaultTuningMinutes),
        MaxTuningGain = Clamp(MaxTuningGain, MinMaxTuningGain, MaxMaxTuningGain, DefaultMaxTuningGain),
        TuneFadeHours = Clamp(TuneFadeHours, MinTuneFadeHours, MaxTuneFadeHours, DefaultTuneFadeHours)
    };
    private static double Clamp(double value, double min, double max, double fallback) =>
        double.IsNaN(value) || double.IsInfinity(value) ? fallback : Math.Min(max, Math.Max(min, value));
}

/// <summary>What one machine remembers of its upkeep: its tune, as a share of a full tune, and when it was last
/// inspected (game time in seconds; 0 for never).</summary>
public sealed class UpkeepState
{
    public double Level;
    public double Inspected;
    /// <summary>The level last written to the machine's record, so a running machine writes only on a whole percent.</summary>
    public double SavedLevel;
    public double SavedInspected;
    /// <summary>The record on the machine could not be read safely: never tuned, never written.</summary>
    public bool Protected;
}

public enum UpkeepKind { Tune, Inspect }

/// <summary>The upkeep rules with no game types (Framework 0.111.0). A tuned machine's work counts for more; nothing
/// else about its job changes, so yields, masses and energy per job stay exactly as authored.</summary>
public static class UpkeepRules
{
    /// <summary>The smallest change of level worth writing to the machine's record.</summary>
    public const double SaveQuantum = 0.01;

    /// <summary>How much faster a machine works at this tune: 1 untuned, 1 + gain x share when fully tuned.</summary>
    public static double Rate(double level, double maxGain, double gainShare) =>
        1 + Math.Max(0, maxGain) * Math.Min(1, Math.Max(0, gainShare)) * Math.Min(1, Math.Max(0, level));

    /// <summary>The tune left after a machine has worked for some seconds: it fades evenly over the fade hours, and at
    /// the inspected share while its inspection is still good. A machine that does not work does not fade.</summary>
    public static double Fade(double level, double workedSeconds, double fadeHours, bool inspected, double inspectedShare)
    {
        if (!(level > 0) || !(workedSeconds > 0) || !(fadeHours > 0)) return Math.Max(0, Math.Min(1, level));
        double fade = workedSeconds / (fadeHours * 3600) * (inspected ? Math.Min(1, Math.Max(0, inspectedShare)) : 1);
        return Math.Max(0, Math.Min(1, level) - fade);
    }

    public static bool InspectionGood(double inspected, double epoch, double validHours) => inspected > 0 && epoch - inspected < validHours * 3600;

    /// <summary>A tuning session is worth offering only when a whole unskilled step still fits.</summary>
    public static bool TuneDue(double level, UpkeepPack pack) => level <= 1 - pack.tuneStep + 1e-9;

    public static double Tuned(double level, bool skilled, UpkeepPack pack) => Math.Min(1, Math.Max(0, level) + (skilled ? pack.tuneStepSkilled : pack.tuneStep));

    /// <summary>Whether a level has moved far enough from the saved one to be written, or has just run out.</summary>
    public static bool WorthSaving(double level, double saved) => Math.Abs(level - saved) >= SaveQuantum || level <= 0 && saved > 0;

    /// <summary>One candidate for the planner: a machine, what could be done at it, and how urgent it is.</summary>
    public readonly struct Candidate
    {
        public readonly string Id; public readonly UpkeepKind Kind; public readonly double Urgency;
        public Candidate(string id, UpkeepKind kind, double urgency) { Id = id; Kind = kind; Urgency = urgency; }
    }

    /// <summary>What to offer next, most urgent first: tuning before inspection, the least tuned machine first, then
    /// the longest uninspected; ties by id so the order is stable.</summary>
    public static List<Candidate> Plan(IEnumerable<(string Id, UpkeepState State, bool Tunable)> machines, bool tune, bool inspect, double epoch, UpkeepPack pack)
    {
        var result = new List<Candidate>();
        foreach (var (id, state, tunable) in machines)
        {
            if (state.Protected) continue;
            if (tune && tunable && TuneDue(state.Level, pack)) result.Add(new Candidate(id, UpkeepKind.Tune, 2 - state.Level));
            else if (inspect && !InspectionGood(state.Inspected, epoch, pack.inspectionValidHours))
                result.Add(new Candidate(id, UpkeepKind.Inspect, state.Inspected <= 0 ? 1 : Math.Min(1, (epoch - state.Inspected) / (pack.inspectionValidHours * 3600 * 10))));
        }
        return result.OrderByDescending(c => c.Urgency).ThenBy(c => c.Id, StringComparer.Ordinal).ToList();
    }

    public static Dictionary<string, string> Encode(UpkeepState state) => new(StringComparer.Ordinal)
    {
        ["level"] = state.Level.ToString("R", CultureInfo.InvariantCulture), ["inspected"] = state.Inspected.ToString("R", CultureInfo.InvariantCulture)
    };
    public static UpkeepState Decode(IReadOnlyDictionary<string, string> fields)
    {
        var state = new UpkeepState();
        if (fields.TryGetValue("level", out var level) && double.TryParse(level, NumberStyles.Float, CultureInfo.InvariantCulture, out double l) && !double.IsNaN(l)) state.Level = Math.Min(1, Math.Max(0, l));
        if (fields.TryGetValue("inspected", out var at) && double.TryParse(at, NumberStyles.Float, CultureInfo.InvariantCulture, out double i) && !double.IsNaN(i) && !double.IsInfinity(i)) state.Inspected = Math.Max(0, i);
        state.SavedLevel = state.Level; state.SavedInspected = state.Inspected;
        return state;
    }

    /// <summary>The crew time banked during a time-skip buys whole sessions: how many, and what is left.</summary>
    public static int Sessions(ref double bankedSeconds, double sessionSeconds, double travelSeconds)
    {
        double cost = sessionSeconds + Math.Max(0, travelSeconds);
        if (!(cost > 0) || !(bankedSeconds >= cost)) return 0;
        bankedSeconds -= cost; return 1;
    }
}
