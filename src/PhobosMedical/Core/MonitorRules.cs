using System;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Health;

namespace PhobosMedical.Core;

/// <summary>What the monitor warns about.</summary>
public enum MonitorAlert { BloodLost, Infection, Pain, Bleeding }

/// <summary>One reading the monitor keeps for its trend: the time and the patient's figures.</summary>
public readonly struct MonitorSample
{
    public double Epoch { get; }
    public double BloodLost { get; }
    public double Infection { get; }
    public double Pain { get; }
    public double WorstWound { get; }
    public MonitorSample(double epoch, PatientFacts f) : this(epoch, f.BloodLost, f.Infection, f.Pain, f.WorstWound) { }
    public MonitorSample(double epoch, double bloodLost, double infection, double pain, double worstWound)
    { Epoch = epoch; BloodLost = bloodLost; Infection = infection; Pain = pain; WorstWound = worstWound; }
}

/// <summary>The Vigil-2's decisions, apart from the game objects. The monitor observes and never changes the patient.</summary>
public static class MonitorRules
{
    /// <summary>How often a trend sample is kept, and how far back the trend looks, in game seconds.</summary>
    public const double SampleSeconds = 600, TrendSeconds = 3600;
    /// <summary>An alert re-arms once its figure falls back below this share of its threshold.</summary>
    public const double Rearm = 0.8;

    /// <summary>Adds a sample when the last one is at least <see cref="SampleSeconds"/> old, and drops samples older
    /// than the trend window (keeping the newest that is older, so a full hour can be compared).</summary>
    public static void Record(List<MonitorSample> samples, MonitorSample now)
    {
        if (samples.Count > 0 && now.Epoch < samples[samples.Count - 1].Epoch) samples.Clear();
        if (samples.Count == 0 || now.Epoch - samples[samples.Count - 1].Epoch >= SampleSeconds) samples.Add(now);
        while (samples.Count > 1 && now.Epoch - samples[1].Epoch >= TrendSeconds) samples.RemoveAt(0);
    }

    /// <summary>The change from the oldest kept sample to <paramref name="now"/>, and the hours it spans; null until a
    /// sample at least one sampling step old exists. A trend restarts after a reload: it is not saved.</summary>
    public static (MonitorSample Change, double Hours)? Trend(IReadOnlyList<MonitorSample> samples, MonitorSample now)
    {
        if (samples.Count == 0) return null;
        var old = samples[0];
        double seconds = now.Epoch - old.Epoch;
        if (seconds < SampleSeconds) return null;
        return (new MonitorSample(seconds, now.BloodLost - old.BloodLost, now.Infection - old.Infection, now.Pain - old.Pain, now.WorstWound - old.WorstWound), seconds / 3600);
    }

    /// <summary>The alerts that newly fire, given the patient's figures and the alerts already armed off. A figure fires
    /// when it reaches its threshold and was not already announced; it re-arms below <see cref="Rearm"/> of the
    /// threshold. A wound that starts bleeding fires once until nothing bleeds.</summary>
    public static List<MonitorAlert> Fire(PatientFacts f, AlertEntry a, ISet<MonitorAlert> announced)
    {
        var fired = new List<MonitorAlert>();
        void Figure(MonitorAlert alert, double value, double threshold)
        {
            if (value >= threshold) { if (announced.Add(alert)) fired.Add(alert); }
            else if (value < threshold * Rearm) announced.Remove(alert);
        }
        Figure(MonitorAlert.BloodLost, f.BloodLost, a.bloodLost);
        Figure(MonitorAlert.Infection, f.Infection, a.infection);
        Figure(MonitorAlert.Pain, f.Pain, a.pain);
        if (f.Bleeding > 0) { if (announced.Add(MonitorAlert.Bleeding)) fired.Add(MonitorAlert.Bleeding); }
        else announced.Remove(MonitorAlert.Bleeding);
        return fired;
    }
}

/// <summary>The Vigil-2's saved record: the bed it watches (or none, to watch whichever Ward-3 it touches) and
/// whether it posts alerts.</summary>
public sealed class MonitorState
{
    public string Bed { get; set; } = "";
    public bool Alerts { get; set; } = true;
    public const int Fields = 2;
    public Dictionary<string, string> Save() => new() { ["bed"] = Bed.Length == 0 ? BedState.Nobody : Bed, ["alerts"] = Alerts ? "1" : "0" };
    public static MonitorState Read(IReadOnlyDictionary<string, string> f)
    {
        if (f.Count != Fields) throw new FormatException("Invalid monitor record.");
        if (!f.TryGetValue("bed", out var bed) || !BedState.SafeId(bed)) throw new FormatException("Invalid monitor bed.");
        if (!f.TryGetValue("alerts", out var alerts) || (alerts != "0" && alerts != "1")) throw new FormatException("Invalid monitor flag.");
        return new MonitorState { Bed = bed == BedState.Nobody ? "" : bed, Alerts = alerts == "1" };
    }
}
