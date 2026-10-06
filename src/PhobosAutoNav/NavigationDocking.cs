using System;
using PhobosAutoNav.Core;
using static PhobosAutoNav.Core.DockingRules;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    private bool DockingActive => AutoNavCore.Engaged && savedFlight?.Mode == SavedFlightMode.Docking;
    private double nextDockFitCheck;
    private readonly MotionTrack dockTrack = new();
    private double dockStableSeconds;
    private bool dockHolding;
    private bool dockingAttachmentPending;
    private const double FitCheckSeconds = 2;

    internal void Dock(CondOwner? co, string? boundTarget = null)
    {
        try
        {
            if (!Plugin.Enabled.Value || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
            { status = Text.Get("Docking.unavailable"); return; }
            string? problem = HardwareProblem(co);
            if (problem == null && co!.HasCond("IsDamagedSoftware")) problem = Text.Get("Docking.software");
            if (problem != null) { status = problem; return; }
            if (OtherControllerBusy()) { status = Text.Get("NavigationService.disengage_other_flight_automation_first"); return; }
            if (!CanReplaceFlight(co!)) { status = Text.Get("Persistence.invalid_state"); return; }
            var selected = boundTarget == null ? GUIOrbitDraw.CrossHairTarget?.Ship : CrewSim.system.GetShipByRegID(boundTarget);
            var target = selected == null ? null : CrewSim.system?.GetShipByRegID(selected.strRegID);
            if (target == null || target == co!.ship) { status = Text.Get("Docking.select"); return; }
            // A flight holding this console is stopped on the second press (Auto Nav 0.35.0).
            if (!ClearFlightFor(co)) return;
            var sensing = SenseTarget(co, target.strRegID);
            if (!sensing.Usable) { status = Text.Get(sensing.MessageKey); return; }
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
        var sensing = SenseTarget(co, snapshot.TargetId);
        if (!sensing.Usable) return Text.Get(sensing.MessageKey);
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
        CeaseFire(); dockTrack.Reset(); dockStableSeconds = 0; dockHolding = true; attachDeadline = 0;
        AutoNavCore.RestoreFlight(co.ship, target, snapshot);
        if (Plugin.FuelCheck.Value && !DockingAdapter.HasFuel(co.ship, CrewSim.system.GetShipByRegID(snapshot.TargetId)!, Throttle))
        {
            FinishSavedFlight(SavedFlightMode.DockingSuspended);
            issuing = true;
            try { AutoNavCore.EndFlight(co.ship, "NO FUEL"); }
            finally { AutoNavCore.ResetStatics(); issuing = false; }
            status = Text.Get("NavigationService.insufficient_estimated_delta_v"); return;
        }
        co.ship.UnlockFromOrbit(); co.ship.objSS.ResetNavData(); Torch.Release();
        snapshot.Mode = SavedFlightMode.Docking; nextDockFitCheck = 0;
        CrewSim.ResetTimeScale(); PersistProgress(force: true);
        if (AutoNavCore.Engaged) status = Text.Get("Docking.engaged");
    }

    // Both endpoints are sampled before native Update advances any ship. A per-ship hook
    // would mix timestamps and mistake orbital motion for a large docking error.
    internal void TickDocking(StarSystem system, double dt, bool afterPhysics)
    {
        try
        {
            if (system == CrewSim.system && CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && !CrewSim.Paused && dt > 0)
                TickCombinedDocking(afterPhysics);
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Docking.error")); return; }
        if (dockingAttachmentPending || !DockingActive || system != CrewSim.system || CrewSim.objInstance == null ||
            !CrewSim.objInstance.FinishedLoading || CrewSim.Paused || dt == 0) return;
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Docking);
        try
        {
            var own = console!.ship; var flight = savedFlight!;
            var target = system.GetShipByRegID(flight.TargetId);
            if (target != null && own.IsDockedWith(target))
            { CompleteDocking(true); return; }
            string? problem = !Plugin.Enabled.Value ? Text.Get("Docking.unavailable") : HardwareProblemNow(console);
            if (problem == null && console.HasCond("IsDamagedSoftware")) problem = Text.Get("Docking.software");
            if (problem == null && (!FlightBindingValidNow() || OtherControllerBusy())) problem = Text.Get("Persistence.binding_changed");
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt <= 0)) problem = Text.Get("Docking.step");
            if (problem == null && AutoNavCore.ElapsedSeconds >= DockingRules.MaximumSeconds) problem = Text.Get("Docking.timeout");
            // An oversized step (heavy time compression) is held, not a reason to abandon the docking.
            if (problem == null && dt > DockingRules.MaximumStep) { if (!afterPhysics) HoldThrust(own, Text.Get("NavigationService.step_hold")); return; }
            if (problem == null)
            {
                var sensing = SenseTarget(console, flight.TargetId);
                if (!sensing.Usable) { if (!SensorsSettling(own, sensing)) SuspendForContact(sensing); return; }
            }
            if (problem == null)
            {
                bool checkFit = !afterPhysics && AutoNavCore.ElapsedSeconds >= nextDockFitCheck;
                string? key = DockingAdapter.Check(own, target, flight.OwnPort, flight.TargetPort, checkFit);
                if (key != null) problem = Text.Get(key);
                if (checkFit) nextDockFitCheck = AutoNavCore.ElapsedSeconds + FitCheckSeconds;
            }
            if (problem != null) { Disengage(problem); return; }
            // Relative samples cancel common orbital acceleration; compensate only our known RCS input.
            dockTrack.Observe(new NavVector((target!.objSS.vVelX - own.objSS.vVelX) / AutoNavCore.M_TO_AU,
                (target.objSS.vVelY - own.objSS.vVelY) / AutoNavCore.M_TO_AU), StarSystem.fEpoch,
                new NavVector(own.objSS.vAccRCS.x / AutoNavCore.M_TO_AU, own.objSS.vAccRCS.y / AutoNavCore.M_TO_AU));
            double authority = own.RCSAccelMax / AutoNavCore.M_TO_AU * Throttle * TerminalAuthorityShare;
            // The target's own spin is no gate: the game's CanDock has none, and derelicts spawn tumbling.
            bool stable = dockTrack.Samples >= 2 && dockTrack.Acceleration.Length < authority && dockTrack.ErrorMS < MaximumResidualMS;
            if (!stable) dockStableSeconds = 0;
            else if (!afterPhysics) dockStableSeconds += dt;
            dockHolding = dockStableSeconds < StableMotionSeconds;
            if (!DockingAdapter.Read(own, target!, Throttle, dt, out var command, dockTrack.Acceleration, dockHolding))
            {
                // Keep braking an already running manoeuvre; preflight admission remains conservative.
                dockHolding = true; dockStableSeconds = 0;
                if (!DockingAdapter.Read(own, target!, Throttle, dt, out command, dockTrack.Acceleration, true))
                { Disengage(Text.Get("Docking.unsafe")); return; }
            }
            if (afterPhysics)
            {
                if (attachDeadline > 0)
                {
                    // The game's own clamp sequence runs over the next frames; success shows as a docked ship above.
                    if (AutoNavCore.ElapsedSeconds < attachDeadline) return;
                    CompleteDocking(false); return;
                }
                if (dockHolding || !command.Ready) return;
                if (!DockingAdapter.ConsoleOpen(console)) { status = Text.Get("Docking.open_console"); return; }
                // Never attach during a ship's physics iteration. Recheck actual motion after all ships advance.
                // Admission and attachment are the game's own: its CanDock geometry and its clamp button.
                issuing = true; dockingAttachmentPending = true;
                try
                {
                    Torch.Release(); own.Maneuver(0, 0, 0, 0, (float)dt);
                    switch (DockingAdapter.Clamp(console, target!, flight.OwnPort, flight.TargetPort, Throttle, dt))
                    {
                        case DockingAdapter.ClampResult.Started: attachDeadline = AutoNavCore.ElapsedSeconds + ClampSettleSeconds; status = Text.Get("Docking.clamping"); break;
                        case DockingAdapter.ClampResult.Refused: CompleteDocking(false); break;
                        default: status = Text.Get("Docking.aligning"); break;
                    }
                }
                finally { issuing = false; dockingAttachmentPending = false; }
                return;
            }
            issuing = true;
            try { own.Maneuver((float)command.X, (float)command.Y, (float)command.Turn, 0, (float)dt); }
            finally { issuing = false; }
            AutoNavCore.AdvanceDockingClock(dt);
            status = Text.Get(dockHolding ? "Docking.holding" : "Docking.progress", command.HullGapM, command.SpeedMS);
            PersistProgress();
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("Docking.error")); }
    }

    // Game seconds the game's own clamp sequence may take before the attempt counts as failed.
    private const double ClampSettleSeconds = 5;
    private double attachDeadline;
    private void CompleteDocking(bool success)
    {
        attachDeadline = 0;
        // The native attachment can throw or refuse after changing state; do not retry or forcibly undock.
        AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, success ? "DOCKED" : "ABORTED");
        FinishSavedFlight(success ? SavedFlightMode.Docked : SavedFlightMode.Stopped);
        status = Text.Get(success ? "Docking.complete" : "Docking.attach_failed");
    }
}
