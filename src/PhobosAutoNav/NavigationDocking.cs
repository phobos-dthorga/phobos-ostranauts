using System;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private bool DockingActive => AutoNavCore.Engaged && savedFlight?.Mode == SavedFlightMode.Docking;
    private double nextDockFitCheck;
    private bool dockingAttachmentPending;
    private const double FitCheckSeconds = 2;

    internal void Dock(CondOwner? co)
    {
        if (AutoNavCore.Engaged) { status = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return; }
        try
        {
            if (!Plugin.Enabled.Value || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
            { status = Text.Get("Docking.unavailable"); return; }
            string? problem = HardwareProblem(co);
            if (problem == null && co!.HasCond("IsDamagedSoftware")) problem = Text.Get("Docking.software");
            if (problem != null) { status = problem; return; }
            if (OtherControllerBusy()) { status = Text.Get("NavigationService.disengage_other_flight_automation_first"); return; }
            if (!CanReplaceFlight(co!)) return;
            if (DisplaySnapshot(co) != null) { status = Text.Get("Docking.stop_first"); return; }
            var target = GUIOrbitDraw.CrossHairTarget?.Ship;
            if (target == null || target == co!.ship) { status = Text.Get("Docking.select"); return; }
            problem = DockingAdapter.SelectPorts(co!.ship, target, out string ownPort, out string targetPort);
            if (problem != null) { status = Text.Get(problem); return; }
            console = co;
            if (!DockingAdapter.Read(co.ship, target, Throttle, DockingRules.MaximumStep, out _))
            { status = Text.Get("Docking.unsafe"); return; }
            var targetRef = TargetRef.FromShipId(target.strRegID);
            if (targetRef == null) { status = Text.Get("Docking.target_lost"); return; }
            // Use the existing versioned Framework store and identity bindings. No new generic framework is needed.
            savedFlight = CaptureFlight(co, targetRef, DockingRules.CruiseMS, 0, ApproachRules.DefaultArrivalKM);
            savedFlight.Coast = Plugin.ReadCoastSettings(); savedFlight.PreferTorch = false;
            savedFlight.Mode = SavedFlightMode.Docking; savedFlight.OwnPort = ownPort; savedFlight.TargetPort = targetPort;
            ResumeDocking(co, targetRef, savedFlight);
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Docking.error")); }
    }

    private string? DockingResumeProblem(CondOwner co, FlightSnapshot snapshot)
    {
        if (co.HasCond("IsDamagedSoftware")) return Text.Get("Docking.software");
        var target = CrewSim.system?.GetShipByRegID(snapshot.TargetId);
        var problem = DockingAdapter.Check(co.ship, target, snapshot.OwnPort, snapshot.TargetPort, checkFit: true);
        if (problem != null) return Text.Get(problem);
        if (snapshot.ElapsedSeconds >= DockingRules.MaximumSeconds) return Text.Get("Docking.timeout");
        if (!DockingAdapter.Read(co.ship, target!, Throttle, DockingRules.MaximumStep, out _)) return Text.Get("Docking.unsafe");
        return null;
    }

    private void ResumeDocking(CondOwner co, TargetRef target, FlightSnapshot snapshot)
    {
        string? problem = DockingResumeProblem(co, snapshot);
        if (problem != null) { FinishSavedFlight(SavedFlightMode.DockingSuspended); status = problem; return; }
        AutoNavCore.RestoreFlight(co.ship, target, snapshot);
        if (Plugin.FuelCheck.Value && !DockingAdapter.HasFuel(co.ship, CrewSim.system.GetShipByRegID(snapshot.TargetId)!, Throttle))
        { AutoNavCore.ResetStatics(); FinishSavedFlight(SavedFlightMode.DockingSuspended); status = Text.Get("NavigationService.insufficient_estimated_delta_v"); return; }
        co.ship.UnlockFromOrbit(); co.ship.objSS.ResetNavData(); Torch.Release();
        snapshot.Mode = SavedFlightMode.Docking; nextDockFitCheck = 0;
        CrewSim.ResetTimeScale(); PersistProgress();
        if (AutoNavCore.Engaged) status = Text.Get("Docking.engaged");
    }

    // Both endpoints are sampled before native Update advances any ship. A per-ship hook
    // would mix timestamps and mistake orbital motion for a large docking error.
    internal void TickDocking(StarSystem system, double dt, bool afterPhysics)
    {
        if (dockingAttachmentPending || !DockingActive || system != CrewSim.system || CrewSim.objInstance == null ||
            !CrewSim.objInstance.FinishedLoading || CrewSim.Paused || dt == 0) return;
        try
        {
            var own = console!.ship; var flight = savedFlight!;
            var target = system.GetShipByRegID(flight.TargetId);
            if (target != null && own.IsDockedWith(target))
            { CompleteDocking(true); return; }
            string? problem = !Plugin.Enabled.Value ? Text.Get("Docking.unavailable") : HardwareProblem(console);
            if (problem == null && console.HasCond("IsDamagedSoftware")) problem = Text.Get("Docking.software");
            if (problem == null && (!FlightBindingValid() || OtherControllerBusy())) problem = Text.Get("Persistence.binding_changed");
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt <= 0 || dt > DockingRules.MaximumStep)) problem = Text.Get("Docking.step");
            if (problem == null && AutoNavCore.ElapsedSeconds >= DockingRules.MaximumSeconds) problem = Text.Get("Docking.timeout");
            if (problem == null)
            {
                bool checkFit = !afterPhysics && AutoNavCore.ElapsedSeconds >= nextDockFitCheck;
                string? key = DockingAdapter.Check(own, target, flight.OwnPort, flight.TargetPort, checkFit);
                if (key != null) problem = Text.Get(key);
                if (checkFit) nextDockFitCheck = AutoNavCore.ElapsedSeconds + FitCheckSeconds;
            }
            if (problem != null) { Disengage(problem); return; }
            if (!DockingAdapter.Read(own, target!, Throttle, dt, out var command))
            { Disengage(Text.Get("Docking.unsafe")); return; }
            if (afterPhysics)
            {
                if (!command.Ready) return;
                if (!DockingAdapter.ConsoleOpen(console)) { status = Text.Get("Docking.open_console"); return; }
                // Never attach during a ship's physics iteration. Recheck actual motion after all ships advance.
                issuing = true; dockingAttachmentPending = true;
                try
                {
                    Torch.Release(); own.Maneuver(0, 0, 0, 0, (float)dt);
                    bool success = DockingAdapter.Attach(console, target!, flight.OwnPort, flight.TargetPort, Throttle, dt);
                    CompleteDocking(success);
                    if (success)
                    {
                        // A UI/notification failure must not erase an already completed docking record.
                        try { DockingAdapter.NotifyAttached(target!); } catch (Exception ex) { log(ex.ToString()); }
                    }
                }
                finally { issuing = false; dockingAttachmentPending = false; }
                return;
            }
            issuing = true;
            try { own.Maneuver((float)command.X, (float)command.Y, (float)command.Turn, 0, (float)dt); }
            finally { issuing = false; }
            AutoNavCore.AdvanceDockingClock(dt);
            status = Text.Get("Docking.progress", command.HullGapM, command.SpeedMS);
            PersistProgress();
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Docking.error")); }
    }

    private void CompleteDocking(bool success)
    {
        // The native attachment can throw or refuse after changing state; do not retry or forcibly undock.
        AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, success ? "DOCKED" : "ABORTED");
        FinishSavedFlight(success ? SavedFlightMode.Docked : SavedFlightMode.Stopped);
        status = Text.Get(success ? "Docking.complete" : "Docking.attach_failed");
    }
}
