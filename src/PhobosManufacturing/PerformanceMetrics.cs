using Phobos.Ostranauts.Framework.Diagnostics;

namespace PhobosManufacturing;

/// <summary>Opt-in recorder scopes (29 September 2026 performance pass): the two-second world scan, the power hooks
/// on our own machines, each machine's pre-power step, manifold rechecks and regulator ticks.</summary>
internal static class PerformanceMetrics
{
    internal static PerformanceMetric? Scan, PowerHook, MachineStep, ManifoldRefresh, RegulatorTick;
    internal static void Initialize()
    {
        Scan = Performance.RegisterOperation("manufacturing.scan", "discovery");
        PowerHook = Performance.RegisterOperation("manufacturing.power.hook", "processing");
        MachineStep = Performance.RegisterOperation("manufacturing.machine.step", "processing");
        ManifoldRefresh = Performance.RegisterOperation("manufacturing.manifold.refresh", "routing");
        RegulatorTick = Performance.RegisterOperation("manufacturing.regulator.tick", "processing");
    }
}
