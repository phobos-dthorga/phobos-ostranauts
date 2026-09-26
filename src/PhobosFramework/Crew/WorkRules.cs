using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Phobos.Ostranauts.Framework.Crew;

public enum CrewRole { Agriculture, Cooking, Industry, Exterior }
public enum WorkPermission { Disabled, Enabled, Stopped, Suspended }

/// <summary>Authored training balance, independent of recipes and machine power.</summary>
public static class CrewBalance
{
    public const double PracticeHours = 20;
    public const double StudyHours = 10;
    public const double SkilledDurationFraction = 0.8;
    public const double HandlingSeconds = 10;
    public const double DiscoverySeconds = 2;
    public const double SkipStepSeconds = 1;
    public const double WalkSecondsPerTile = 2;
    public static double UntilHour(double epoch) => 3600 - ((epoch % 3600 + 3600) % 3600);
    public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
    public static double Credit(double progress, double productiveSeconds, bool study) =>
        !Finite(progress) || progress < 0 || !Finite(productiveSeconds) || productiveSeconds <= 0 ? progress :
        Math.Min(100, progress + productiveSeconds / (3600 * (study ? StudyHours : PracticeHours)) * 100);
    public static double Duration(double seconds, bool skilled) => seconds * (skilled ? SkilledDurationFraction : 1);
    public static string Binding(IEnumerable<string> fields)
    {
        using var hash=SHA256.Create();
        var encoded=string.Join("|",fields.Select(v=>v.Length.ToString(CultureInfo.InvariantCulture)+":"+v));
        return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(encoded))).Replace("-","");
    }
}

/// <summary>Only explicit user edits change intent. A blocked job does not erase its order.</summary>
public sealed class StandingOrder
{
    public WorkPermission Permission;
    public string Recipe = "default", Source = "none", Destination = "none", Target = "none", Binding = "";
    public int Stock = 4;
    public bool Hazardous, ClearCrops, Drain;
    public bool ResumeRoutine = true;
    public string StopReason = "";
    public bool Protected;
    public Dictionary<string, string> Save() => new(StringComparer.Ordinal) {
        ["permission"] = Permission.ToString(), ["recipe"] = Recipe, ["source"] = Source,
        ["destination"] = Destination, ["target"] = Target, ["stock"] = Stock.ToString(CultureInfo.InvariantCulture),
        ["hazardous"] = Hazardous ? "1" : "0", ["clear"] = ClearCrops ? "1" : "0", ["drain"] = Drain ? "1" : "0",
        ["resumeRoutine"] = ResumeRoutine ? "1" : "0", ["reason"] = StopReason.Length==0?"none":StopReason,
        ["binding"] = Binding.Length==0?"none":Binding
    };
    public static StandingOrder Read(IReadOnlyDictionary<string,string> fields)
    {
        var r = new StandingOrder();
        if (!fields.TryGetValue("permission", out var p) || !Enum.TryParse(p, out r.Permission) || !Enum.IsDefined(typeof(WorkPermission), r.Permission) ||
            !fields.TryGetValue("stock", out var stock) || !int.TryParse(stock, NumberStyles.None, CultureInfo.InvariantCulture, out r.Stock) || r.Stock < 1 || r.Stock > 256 ||
            !fields.TryGetValue("recipe", out r.Recipe) || !fields.TryGetValue("source", out r.Source) ||
            !fields.TryGetValue("destination", out r.Destination) || !fields.TryGetValue("target", out r.Target) ||
            !ReadBool(fields, "hazardous", out r.Hazardous) || !ReadBool(fields, "clear", out r.ClearCrops) || !ReadBool(fields, "drain", out r.Drain) ||
            !ReadBool(fields,"resumeRoutine",out r.ResumeRoutine) || !fields.TryGetValue("reason",out r.StopReason) || !fields.TryGetValue("binding",out r.Binding) ||
            new[]{r.Recipe,r.Source,r.Destination,r.Target}.Any(string.IsNullOrWhiteSpace)) r.Protected = true;
        if(r.StopReason=="none")r.StopReason="";
        if(r.Binding=="none")r.Binding="";
        return r;
    }
    private static bool ReadBool(IReadOnlyDictionary<string,string> f, string k, out bool b)
    { b = f.TryGetValue(k, out var v) && v == "1"; return v == "0" || v == "1"; }
    public void Reload(bool routine)
    { if (Permission == WorkPermission.Enabled && (!routine || Hazardous || !ResumeRoutine)) { Permission = WorkPermission.Suspended; StopReason="reload"; } }
}

/// <summary>Single-owner leases cover shared equipment, cargo and destination capacity.</summary>
public sealed class WorkReservations
{
    private readonly Dictionary<string,string> owners = new(StringComparer.Ordinal);
    public bool Available(string key, string owner) => !owners.TryGetValue(key, out var current) || current == owner;
    public bool Acquire(string owner, IEnumerable<string> keys)
    {
        var all = keys.Distinct(StringComparer.Ordinal).ToArray();
        if (all.Any(k => !Available(k, owner))) return false;
        foreach (var k in all) owners[k] = owner;
        return true;
    }
    public void Release(string owner)
    { foreach (var k in owners.Where(p => p.Value == owner).Select(p => p.Key).ToArray()) owners.Remove(k); }
    public void Clear() => owners.Clear();
}

/// <summary>One budget per worker: no overlapping sleep, labour, study or native repair credit.</summary>
public sealed class CrewTimeBudget
{
    public double Available { get; private set; }
    public double Worked { get; private set; }
    public CrewTimeBudget(double seconds)
    { if (!CrewBalance.Finite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds)); Available = seconds; }
    public bool TrySpend(double seconds, bool work)
    {
        if (!CrewBalance.Finite(seconds) || seconds <= 0 || seconds > Available) return false;
        Available -= seconds; if (work) Worked += seconds; return true;
    }
}
