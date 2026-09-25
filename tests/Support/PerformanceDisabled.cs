using System;

// Isolated gameplay-double suites keep diagnostics disabled. The real adapter and
// recorder are exercised separately by PhobosPerformance.Tests and the Scope suite.
namespace Phobos.Ostranauts.Framework.Diagnostics;
public sealed class PerformanceMetric { }
public static class Performance
{
    public static PerformanceMetric? RoomAlarmRead => null;
    public static PerformanceMetric? RegisterOperation(string name, string category) => null;
    public static DisabledScope Measure(PerformanceMetric? operation) => default;
}
public readonly struct DisabledScope : IDisposable { public void Dispose() { } }
