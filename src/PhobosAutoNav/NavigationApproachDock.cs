using System;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    internal const double DockStagingMarginKM = 1;

    internal void ApproachDock(CondOwner? co)
    {
        if (AutoNavCore.Engaged) { status = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return; }
        try
        {
            if (!Plugin.Enabled.Value || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
            { status = Text.Get("Docking.unavailable"); return; }
            string? problem = HardwareProblem(co);
            if (problem != null) { status = problem; return; }
            if (OtherControllerBusy()) { status = Text.Get("NavigationService.disengage_other_flight_automation_first"); return; }
            if (!CanReplaceFlight(co!) || DisplaySnapshot(co) != null) { status = Text.Get("Docking.stop_first"); return; }
            var selected = GUIOrbitDraw.CrossHairTarget?.Ship;
            var target = selected == null ? null : TargetRef.FromShipId(selected.strRegID);
            if (target == null || selected == co!.ship) { status = Text.Get("Docking.select"); return; }
            var sensing = ReadContact(co, target);
            if (!sensing.Usable) { status = Text.Get(sensing.MessageKey); return; }
            problem = DockingAdapter.SelectPorts(co!.ship, selected!, out string ownPort, out string targetPort);
            if (problem != null) { status = Text.Get(problem); return; }
            if (!ReadPreferences(co, out var preferences)) { status = Text.Get("Preferences.invalid"); return; }
            double staging = StagingDistance(co.ship, selected!);
            if (!ApproachRules.ValidArrival(staging)) { status = Text.Get("Hub.staging_unavailable"); return; }
            console = co;
            var flight = CaptureFlight(co, target, preferences.CruiseMS, 0, staging, SavedFlightMode.ApproachDock);
            flight.Coast = Plugin.ReadCoastSettings(); flight.PreferTorch = Plugin.PreferTorch.Value;
            flight.OwnPort = ownPort; flight.TargetPort = targetPort;
            savedFlight = flight;
            ResumeApproachDock(co, target, flight);
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Docking.error")); }
    }

    private static double StagingDistance(Ship own, Ship target) =>
        CollisionManager.GetCollisionDistanceAU(own, target) / AutoNavCore.M_TO_AU / 1000 *
        ApproachRules.HullClearanceMultiplier + DockStagingMarginKM;

    private string? CombinedProblem(CondOwner co, FlightSnapshot flight, bool checkFit = true)
    {
        if (co.HasCond("IsDamagedSoftware")) return Text.Get("Docking.software");
        if (!FlightBindingValid()) return Text.Get("Persistence.binding_changed");
        var sensing = NativeContactReader.Read(co.ship, flight.TargetId);
        if (!sensing.Usable) return Text.Get(sensing.MessageKey);
        string? key = DockingAdapter.Check(co.ship, CrewSim.system?.GetShipByRegID(flight.TargetId),
            flight.OwnPort, flight.TargetPort, checkFit);
        return key == null ? null : Text.Get(key);
    }

    private void ResumeApproachDock(CondOwner co, TargetRef target, FlightSnapshot flight)
    {
        combinedHandoffPending = false;
        string? problem = CombinedProblem(co, flight);
        if (problem != null) { SuspendCombined(problem); return; }
        if (!AutoNavCore.TryReadApproach(co.ship, target, flight.ArrivalKM, out var plan, out _))
        { SuspendCombined(Text.Get("NavigationService.approach_data_unavailable")); return; }
        // Inside staging, use the terminal controller's own admission. Never burn outward and restart.
        if (plan.RangeKM <= flight.ArrivalKM) { HandoffDocking(co, target, flight); return; }
        problem = AdmissionProblem(co, target, flight.ArrivalKM, 0);
        if (problem != null) { SuspendCombined(problem); return; }
        AutoNavCore.RestoreFlight(co.ship, target, flight);
        if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co.ship, target, readOnly: true))
        { SuspendCombined(Text.Get("NavigationService.insufficient_estimated_delta_v")); return; }
        Fire.Cease(); co.ship.UnlockFromOrbit(); co.ship.objSS.ResetNavData();
        flight.Mode = SavedFlightMode.ApproachDock; nextDockFitCheck = AutoNavCore.ElapsedSeconds + FitCheckSeconds;
        CrewSim.ResetTimeScale(); PersistProgress();
        if (AutoNavCore.Engaged) status = Text.Get("Hub.approach_stage");
    }

    // The ordinary controller finishes inside a ship update. Defer terminal admission until
    // every ship has advanced, so the handoff never compares different simulation timestamps.
    private bool QueueDockingHandoff()
    {
        if (savedFlight?.Mode != SavedFlightMode.ApproachDock || AutoNavCore.Engaged || AutoNavCore.LastResult != "ARRIVED") return false;
        combinedHandoffPending = true; Fire.Cease();
        if (!FinishSavedFlight(SavedFlightMode.ApproachDockSuspended))
        { combinedHandoffPending = false; status = Text.Get("Persistence.write_failed"); return true; }
        status = Text.Get("Hub.handoff");
        return true;
    }

    private void TickCombinedDocking(bool afterPhysics)
    {
        if (console == null || savedFlight == null) { combinedHandoffPending = false; return; }
        if (!savedFlight.IsCombinedApproach) { combinedHandoffPending = false; return; }
        if (!combinedHandoffPending && (!AutoNavCore.Engaged || savedFlight.Mode != SavedFlightMode.ApproachDock)) return;
        string? problem = !Plugin.Enabled.Value ? Text.Get("Docking.unavailable") : HardwareProblem(console);
        bool checkFit = !afterPhysics && AutoNavCore.ElapsedSeconds >= nextDockFitCheck;
        if (problem == null) problem = CombinedProblem(console, savedFlight, checkFit);
        if (checkFit) nextDockFitCheck = AutoNavCore.ElapsedSeconds + FitCheckSeconds;
        if (problem != null) { SuspendCombined(problem); return; }
        if (!combinedHandoffPending || !afterPhysics) return;
        combinedHandoffPending = false;
        var target = TargetRef.FromShipId(savedFlight.TargetId);
        if (target == null) { SuspendCombined(Text.Get("Docking.target_lost")); return; }
        HandoffDocking(console, target, savedFlight);
    }

    private void HandoffDocking(CondOwner co, TargetRef target, FlightSnapshot flight)
    {
        // Require the native attachment interface before handing over, not merely at clamp range.
        if (!DockingAdapter.ConsoleOpen(co)) { SuspendCombined(Text.Get("Docking.open_console")); return; }
        string? problem = CombinedProblem(co, flight);
        var ship = CrewSim.system?.GetShipByRegID(flight.TargetId);
        if (problem == null && (ship == null || !DockingAdapter.Read(co.ship, ship, Throttle, DockingRules.MaximumStep, out _)))
            problem = Text.Get("Docking.unsafe");
        if (problem != null) { SuspendCombined(problem); return; }
        // Preserve exact hardware/target/ports. The terminal phase has its own elapsed-time budget.
        flight.Mode = SavedFlightMode.DockingSuspended; flight.CruiseMS = DockingRules.CruiseMS;
        flight.ArrivalMS = 0; flight.PreferTorch = false; flight.Coasting = false; flight.ElapsedSeconds = 0;
        ResumeDocking(co, target, flight);
    }

    private void SuspendCombined(string reason)
    {
        combinedHandoffPending = false; Fire.Cease();
        FinishSavedFlight(SavedFlightMode.ApproachDockSuspended);
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "ABORTED"); }
        finally { AutoNavCore.ResetStatics(); issuing = false; }
        status = reason;
    }
}
