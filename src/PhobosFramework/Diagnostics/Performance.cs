using System;
using Phobos.Scope.Recording;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Opaque registered operation/counter; content never owns the recorder.</summary>
public sealed class PerformanceMetric
{
    internal readonly PerformanceSession Session;
    internal readonly Metric Metric;
    internal PerformanceMetric(PerformanceSession session, Metric metric) { Session = session; Metric = metric; }
}

public readonly struct PerformanceScope : IDisposable
{
    private readonly PerformanceSession? session;
    private readonly TimingScope scope;
    internal PerformanceScope(PerformanceSession session, TimingScope scope) { this.session = session; this.scope = scope; }
    public void Dispose()
    {
        try { scope.Dispose(); }
        catch (Exception ex) { session?.Fault(ex); }
    }
}

/// <summary>Main-thread, opt-in diagnostics shared by Framework and content mods.</summary>
public static class Performance
{
    internal static PerformanceSession? Session;
    internal static PerformanceMetric? RoomAlarmRead;
    internal static PerformanceMetric? CrewDiscovery = null;
    internal static PerformanceMetric? ShipCandidates = null;
    // 29 September 2026 pass: the Framework hot paths the fast-forward captures attribute.
    internal static PerformanceMetric? FluidRouteFind = null, FluidRouteObjects = null, CrewTaskFilter = null, CrewPathChecks = null, RcsCollect = null,
        StateWrite = null, StateWritesSkipped = null, WaterRefill = null, SkipMachineStep = null;
    // Stage 8: the shared world sweep that replaced each mod's full pass.
    internal static PerformanceMetric? WorldSweep = null, WorldSweepObjects = null;
    // Framework 0.63.0: lines that hold their contents (the two-second top-up and canister pours).
    internal static PerformanceMetric? LineContentsMaintain = null;
    // Framework 0.72.0: how often a ship's cached line layout is actually thrown away.
    internal static PerformanceMetric? FluidRouteInvalidations = null;
    public static bool IsRecording => Session?.IsRecording == true;
    public static PerformanceMetric? RegisterOperation(string name, string category)
    {
        try { return Session?.RegisterOperation(name, category); }
        catch (Exception ex) { Session?.Fault(ex); return null; }
    }
    public static PerformanceMetric? RegisterIncrement(string name, string category, string unit)
    {
        try { return Session?.RegisterIncrement(name, category, unit); }
        catch (Exception ex) { Session?.Fault(ex); return null; }
    }
    /// <summary>A count a mod keeps alive (sessions, records, cached entries), read once a real second while recording
    /// (Framework 0.104.0). Counts show growth that never comes back down; they are not bytes. Register at start-up, like
    /// every metric, and keep the read cheap: it runs on the main thread.</summary>
    public static void RegisterFootprint(string name, string category, Func<int> count)
    {
        try { Session?.RegisterPeriodic(name, category, "entries", () => count()); }
        catch (Exception ex) { Session?.Fault(ex); }
    }
    internal static void RegisterPeriodic(string name, string category, string unit, Func<double?> read)
    {
        try { Session?.RegisterPeriodic(name, category, unit, read); }
        catch (Exception ex) { Session?.Fault(ex); }
    }
    public static void RegisterContext(string name, Func<string> read)
    {
        try { Session?.RegisterContext(name, read); }
        catch (Exception ex) { Session?.Fault(ex); }
    }
    public static PerformanceScope Measure(PerformanceMetric? operation)
    {
        if (!IsRecording || operation == null || operation.Session != Session) return default;
        return operation.Session.Measure(operation);
    }
    public static void Increment(PerformanceMetric? counter, double value = 1)
    {
        if (!IsRecording || counter == null || counter.Session != Session) return;
        counter.Session.Increment(counter, value);
    }
}
