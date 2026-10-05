using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Phobos.Ostranauts.Framework.Crew;

public enum CrewRole { Agriculture, Cooking, Industry, Exterior, Medical }
public enum WorkPermission { Disabled, Enabled, Stopped, Suspended }

/// <summary>Authored training balance, independent of recipes and machine power.</summary>
public static class CrewBalance
{
    public const double PracticeHours = 20;
    public const double StudyHours = 10;
    public const double SkilledDurationFraction = 0.8;
    public const double HandlingSeconds = 10;
    public const double DiscoverySeconds = 2;
    // Before Framework 0.112.0 a skip with crew orders stepped one second at a time (SkipStepSeconds = 1) and one with
    // only running machines ten seconds (MachineSkipStepSeconds, 0.99.0); both now use the player's step below.
    /// <summary>The game seconds a stepped time-skip takes at a time, the player's <c>TimeSkip/StepSeconds</c>
    /// (Framework 0.112.0; owner report, 6 October 2026). A six-hour skip with crew orders on used to take one-second
    /// steps and froze the game for minutes; the owner put a playable skip ahead of its detail. Each step costs about
    /// the same whatever its length, so the freeze shrinks with the step. The ceiling is the heat check: a machine
    /// refuses a step whose heat would take its room past the limit, so a step much longer than a minute would stall
    /// hot machines in small rooms for the whole skip, and that check is never relaxed.</summary>
    public const double DefaultSkipStepSeconds = 30, MinSkipStepSeconds = 1, MaxSkipStepSeconds = 60;
    /// <summary>The setting held to its range; a value that is not a number takes the default.</summary>
    public static double ClampSkipStep(double seconds) => !Finite(seconds) ? DefaultSkipStepSeconds : Math.Min(MaxSkipStepSeconds, Math.Max(MinSkipStepSeconds, seconds));
    /// <summary>How a time-skip advances the clock, pure: in the configured step while crew orders are enabled or any
    /// machine's Start stands, and otherwise 0, the game's own single jump.</summary>
    public static double SkipStep(bool crewOrders, bool runningMachines, double configured) => crewOrders || runningMachines ? ClampSkipStep(configured) : 0;
    /// <summary>The game's own powered fittings (lights, doors, life support) are stepped every fourth skip step;
    /// Phobos machines, crew-ordered equipment, rooms and the objects that charge batteries every step.</summary>
    public const int FixtureStepMultiple = 4;
    /// <summary>Whether a fitting last stepped at <paramref name="last"/> is due again, pure.</summary>
    public static bool FixtureDue(double last, double now, double step) => !Finite(last) || now - last >= FixtureStepMultiple * step - 1e-6;
    /// <summary>The turn a fitting is treated as last stepped at the start of a skip, pure: the fittings are spread over
    /// the four steps by their order, so no one step carries them all.</summary>
    public static double FixtureStart(int index, double now, double step) => now - (Math.Max(0, index) % FixtureStepMultiple) * step;
    /// <summary>The game charges a battery by a fixed share of what it lacks at each power step, not per second
    /// (<c>Powered.Recharge</c>, 0.1% per call), so the reactor and battery chargers take a long skip step in
    /// one-second slices, as they would in play. At most this many slices per step.</summary>
    public const int MaxRechargeSlices = 60;
    /// <summary>How many slices a recharging object's step of <paramref name="elapsed"/> seconds takes, pure.</summary>
    public static int RechargeSlices(double elapsed) => !Finite(elapsed) || elapsed < 2 ? 1 : (int)Math.Min(MaxRechargeSlices, Math.Floor(elapsed));
    /// <summary>A crew job in a stepped skip ends with the step it finishes in; the seconds of that step left over are
    /// a head start on the worker's next job, so longer steps do not cost crew time, pure.</summary>
    public static double JobSeconds(double seconds, double headStart) => Math.Max(0, seconds - (Finite(headStart) && headStart > 0 ? headStart : 0));
    public const double WalkSecondsPerTile = 2;
    public static double UntilHour(double epoch) => 3600 - ((epoch % 3600 + 3600) % 3600);
    public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
    public static double Credit(double progress, double productiveSeconds, bool study) =>
        !Finite(progress) || progress < 0 || !Finite(productiveSeconds) || productiveSeconds <= 0 ? progress :
        Math.Min(100, progress + productiveSeconds / (3600 * (study ? StudyHours : PracticeHours)) * 100);
    public static double Duration(double seconds, bool skilled) => seconds * (skilled ? SkilledDurationFraction : 1);
    /// <summary>Share of on-shift crew time a time-skip left for the game's own repairs: 1 when no
    /// Phobos work was done, 0 when every available crew-second went to Phobos jobs.</summary>
    public static double RepairShare(double workedSeconds, double availableSeconds) =>
        !Finite(workedSeconds) || !Finite(availableSeconds) || availableSeconds <= 0 ? 1 :
        Math.Min(1, Math.Max(0, 1 - Math.Max(0, workedSeconds) / availableSeconds));
    // After a work step fails, wait before offering it again so the worker is free for native
    // tasks, study and rest. The order stays enabled; the wait grows to the last step.
    public static readonly double[] RetryDelaySeconds = { 30, 60, 120, 300, 600 };
    public static double RetryDelay(int failures) => failures <= 0 ? 0 : RetryDelaySeconds[Math.Min(failures, RetryDelaySeconds.Length) - 1];
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
    /// <summary>A source that means anywhere aboard: the deck, unlocked containers and other machines'
    /// trays on the same ship, as the game's own Reload job searches. Saved like a store ID.</summary>
    public const string ShipWide = "ship";
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
    public bool Available(string owner, IEnumerable<string> keys) => keys.All(k => Available(k, owner));
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

/// <summary>Which study actions to add to a crew member's AI history so the idle picker can choose
/// them like vanilla study. Additive only: an entry is planned where the vanilla template entry
/// exists for that need and ours is absent. Nothing is ever changed or removed.</summary>
public static class StudyHistorySeed
{
    public static IReadOnlyList<(string Need, string Opener)> Plan(IEnumerable<(string Need, IReadOnlyCollection<string> Entries)> histories,
        string templateOpener, IEnumerable<string> openers)
    {
        var wanted = openers.Where(o => !string.IsNullOrEmpty(o)).Distinct(StringComparer.Ordinal).ToArray();
        var plan = new List<(string, string)>();
        foreach (var (need, entries) in histories)
        {
            if (entries == null || !entries.Contains(templateOpener)) continue;
            foreach (string opener in wanted) if (!entries.Contains(opener)) plan.Add((need, opener));
        }
        return plan;
    }
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
