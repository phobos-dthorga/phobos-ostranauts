namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    // One movement-authority decision at the shared native physics boundary.
    // Kept outside the Harmony shim so coupled regressions execute this exact ordering.
    internal void BeforeNavigationPhysics(StarSystem system, double dt)
    {
        if (system != CrewSim.system) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Background);
        // Contact readings, hardware verdicts and docked partners are shared by every controller below for this
        // step only; the step closes before native physics moves anything.
        BeginStep();
        try
        {
            TickDeparture(dt);
            ValidateCombat();
            if (GuardNavigation(dt)) return;
            TickIndustrial(dt, false);
            TickFire(dt, false);
            TickDocking(system, dt, false);
            if (AutoNavCore.EngagedPlayer?.objSS != null) Tick(AutoNavCore.EngagedPlayer.objSS, dt, false);
            TickFire(dt, true);
        }
        finally { EndStep(); }
    }

    internal void AfterNavigationPhysics(StarSystem system, double dt)
    {
        if (avoidanceActive) return;
        TickDocking(system, dt, true);
        if (system == CrewSim.system) TickIndustrial(dt, true);
    }
}
