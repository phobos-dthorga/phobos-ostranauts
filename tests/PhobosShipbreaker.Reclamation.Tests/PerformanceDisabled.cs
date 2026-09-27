namespace PhobosShipbreaker;

// This state-machine fixture does not load Unity or start a capture. The real
// recorder and its failure isolation are covered by PhobosPerformance.Tests.
internal static class PerformanceMetrics
{
    internal static Phobos.Ostranauts.Framework.Diagnostics.PerformanceMetric? Reclamation = null;
}
