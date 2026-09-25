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
