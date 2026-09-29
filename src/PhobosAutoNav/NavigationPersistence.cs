using System;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using Phobos.Ostranauts.Framework.Persistence;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    partial void ResetPursuit();
    partial void HoldFireForGuidance();
    partial void ResetCombatSession();
    partial void ReadExclusiveMovement(ref bool exclusive);
    partial void ResetExtended();
    partial void StopExtended(string reason);
    partial void CrewResumePolicy(CondOwner co, ref bool permitted);
    partial void CrewManualStop(CondOwner co);
    partial void ExtendedCommand(CondOwner? co, string action, ref bool handled);
    partial void ReadIndustrialRouteCost(CondOwner co,string module,string targetId,double bearing,double gap,ref bool valid,ref double cost);
    internal bool IndustrialRouteCost(CondOwner co,string module,string target,double bearing,double gap,out double cost)
    { bool valid=false;cost=double.PositiveInfinity;ReadIndustrialRouteCost(co,module,target,bearing,gap,ref valid,ref cost);return valid; }
    internal bool avoidanceActive, avoidanceBlocked;
    internal string avoidanceNotice = "";
    private FlightSnapshot? savedFlight;
    // Combat never replaces the saved mission. Only this session owns its movement intent.
    private bool combatActive;
    private FlightSnapshot? combatPrevious;
    internal bool CombatActive => combatActive;
    internal string ControlDiagnostic => $"controller={(combatActive ? "Combat" : industrial != null ? "Industrial" : savedFlight?.Mode.ToString() ?? "None")} console={console?.strID ?? "none"} reason={status}";
    private void DropCombat()
    {
        if (!combatActive) return;
        combatActive = false; savedFlight = combatPrevious; combatPrevious = null;
        ResetCombatSession();
    }
    private readonly Phobos.Ostranauts.Framework.Audio.CompletionWatch arrivalWatch = new();
    private bool restorePending;
    private bool combinedHandoffPending;
    private static ObjectStateStore Store(CondOwner co) => new ObjectStateStore(co.mapGUIPropMaps,
        FlightSnapshot.StoreName, co.strID, FlightSnapshot.Schema);

    // Load/NewGame may be nested. Drop references only: never erase the old
    // world's snapshot or send a maneuver into a world being torn down.
    internal void WorldChanging()
    {
        DropCombat();
        ResetExtended(); avoidanceActive = avoidanceBlocked = false;
        industrial = null; industrialNotice = Text.Get("Persistence.loading");
        arrivalWatch.Cancel(); ResetPursuit(); Fire.Reset(); Torch.Reset(); AutoNavCore.ResetStatics(); console = null; savedFlight = null;
        persistedFlight = null; persistCadence.Invalidate();
        ForgetSensorWork();
        issuing = false; restorePending = false; combinedHandoffPending = false; status = Text.Get("Persistence.loading");
    }
    internal void WorldLoaded() => restorePending = true;

    internal static void PrepareSavedPhysics(ShipSitu situ, JsonShipSitu saved)
    {
        if ((!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != situ) && Plugin.Service?.StandaloneAimFor(situ) != true && Plugin.Service?.IndustrialOwns(situ) != true) return;
        saved.vAccRCS = UnityEngine.Vector2.zero;
        saved.vAccIn = UnityEngine.Vector2.zero;
        saved.fA = 0;
    }

    internal void UpdatePersistence()
    {
        ReconcileSensors();
        if (!restorePending || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ||
            CrewSim.coPlayer?.ship == null) return;
        restorePending = false;
        DiscoverSensorHolders(CrewSim.coPlayer.ship);
        try
        {
            // Exclude docked neighbours and cargo. Restoring another ship's
            // console is never implied by sharing a dock group or display name.
            var candidates = CrewSim.coPlayer.ship.GetCOs(null, bSubObjects: false, bAllowDocked: false, bAllowLocked: true)
                .Where(co => !co.bDestroyed && Store(co).Read(out _) != SavedStateStatus.Missing)
                .OrderBy(co => co.strID, StringComparer.Ordinal).ToArray();
            var resumable = candidates.Where(co => Store(co).Read(out var fields) == SavedStateStatus.Ready &&
                FlightSnapshot.TryDecode(fields, out var flight) && flight.IsActive).ToArray();
            if (resumable.Length > 1)
            {
                // Persist the suspension so another reload cannot silently pick a winner.
                foreach (var co in resumable)
                {
                    Store(co).Read(out var fields); FlightSnapshot.TryDecode(fields, out var flight);
                    flight.Mode = flight.SuspendedMode; Store(co).TryWrite(flight.Encode());
                }
                status = Text.Get("Persistence.multiple_flights"); return;
            }
            var selected = resumable.FirstOrDefault() ?? candidates.FirstOrDefault(HasResumableFlight) ?? candidates.FirstOrDefault();
            if (selected == null) { status = Text.Get("NavigationService.idle"); return; }
            console = selected;
            if (!ReadSaved(selected, out var snapshot)) return;
            savedFlight = snapshot;
            bool resumePermitted=Plugin.ResumeAfterLoad.Value;
            CrewResumePolicy(selected,ref resumePermitted);
            if (snapshot.Mode == SavedFlightMode.Active && resumePermitted) ResumeSaved(selected);
            else
            {
                if (snapshot.IsActive) FinishSavedFlight(snapshot.SuspendedMode);
                status = SavedDescription(snapshot);
            }
        }
        catch (Exception ex) { log(ex.ToString()); AutoNavCore.ResetStatics(); status = Text.Get("Persistence.restore_error"); }
    }

    private bool ReadSaved(CondOwner co, out FlightSnapshot snapshot)
    {
        snapshot = null!;
        var state = Store(co).Read(out var fields);
        if (state == SavedStateStatus.Missing) { status = Text.Get("Persistence.no_flight"); return false; }
        if (state != SavedStateStatus.Ready || !FlightSnapshot.TryDecode(fields, out snapshot) || snapshot.ConsoleId != co.strID)
        { status = Text.Get("Persistence.invalid_state"); return false; }
        return true;
    }

    private bool CanReplaceFlight(CondOwner co) => Store(co).Read(out _) == SavedStateStatus.Missing || ReadSaved(co, out _);

    private FlightSnapshot? DisplaySnapshot(CondOwner? co) => co != null && Store(co).Read(out var fields) == SavedStateStatus.Ready &&
        FlightSnapshot.TryDecode(fields, out var snapshot) && snapshot.ConsoleId == co.strID &&
        snapshot.IsResumable ? snapshot : null;

    private FlightSnapshot CaptureFlight(CondOwner co, TargetRef target, double cruise, double arrival, double distance, SavedFlightMode mode = SavedFlightMode.Active) => new FlightSnapshot
    {
        ConsoleId = co.strID, ModuleId = co.GetCOsSafe(true).First(item => (mode == SavedFlightMode.Rendezvous || mode == SavedFlightMode.Following ? HasId(item, PursuitId) : (HasId(item, ModuleId) || HasId(item, PursuitId))) && !item.HasCond("IsDamaged")).strID,
        ShipId = co.ship.strRegID, PlayerId = CrewSim.coPlayer.strID, TargetId = target.ShipId,
        CruiseMS = cruise, ArrivalMS = arrival,
        ArrivalKM = distance, Coast = AutoNavCore.FlightCoastSettings, PreferTorch = AutoNavCore.FlightPrefersTorch, Mode = mode
    };

    private bool FlightBindingValid() => savedFlight != null && console != null &&
        savedFlight.Matches(console.strID, console.GetCOsSafe(true).FirstOrDefault(item => item.strID == savedFlight.ModuleId &&
            (savedFlight.IsPursuit ? HasId(item, PursuitId) : (HasId(item, ModuleId) || HasId(item, PursuitId))) && !item.HasCond("IsDamaged"))?.strID ?? "", console.ship?.strRegID ?? "", CrewSim.coPlayer?.strID ?? "");

    // 29 September 2026 pass (FF6): the flight record settles on real time. The in-memory snapshot follows every
    // step; the native map is written every PersistSeconds, on every mode or engagement change, at explicit commit
    // points, and before every native save (FlushProgress on the Framework save boundary). The snapshot's own
    // validity and the record's envelope are still checked every step, so a corrupt record aborts at once, as it
    // did when every step wrote. A crash (never a save) can lose up to PersistSeconds of elapsed budget.
    internal const double PersistSeconds = 2;
    private static readonly System.Diagnostics.Stopwatch realClock = System.Diagnostics.Stopwatch.StartNew();
    /// <summary>Real seconds on a monotonic clock, unaffected by pause and fast-forward; the test suites substitute it.</summary>
    internal static Func<double> RealClock = () => realClock.Elapsed.TotalSeconds;
    private readonly Phobos.Ostranauts.Framework.Cadence persistCadence = new(PersistSeconds);
    private FlightSnapshot? persistedFlight;
    private SavedFlightMode persistedMode;
    private bool persistedEngaged;
    internal int ProgressWrites { get; private set; }

    /// <summary>Framework save boundary: an engaged flight's latest progress reaches the record before the game serialises its ship.</summary>
    internal void FlushProgress(Ship ship)
    {
        if (AutoNavCore.Engaged && console != null && console.ship == ship) PersistProgress(force: true);
    }

    private void PersistProgress(bool force = false)
    {
        if (combatActive) { if (!AutoNavCore.Engaged) { CeaseFire(); DropCombat(); } return; }
        if (savedFlight == null || console == null || console.bDestroyed || savedFlight.ConsoleId != console.strID) return;
        savedFlight.ElapsedSeconds = AutoNavCore.ElapsedSeconds;
        savedFlight.Coasting = AutoNavCore.Coasting;
        if (!AutoNavCore.Engaged) savedFlight.Mode = AutoNavCore.LastResult == "ARRIVED" ? SavedFlightMode.Arrived : SavedFlightMode.Stopped;
        bool transition = !ReferenceEquals(persistedFlight, savedFlight) || persistedMode != savedFlight.Mode || persistedEngaged != AutoNavCore.Engaged;
        var store = Store(console);
        var envelope = store.Status();
        bool failed = !savedFlight.Valid || envelope != SavedStateStatus.Ready && envelope != SavedStateStatus.Missing;
        if (!failed && (force || transition || persistCadence.Due(RealClock())))
        {
            using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Persist);
            failed = !store.TryWriteIfChanged(savedFlight.Encode());
            if (!failed) { ProgressWrites++; persistedFlight = savedFlight; persistedMode = savedFlight.Mode; persistedEngaged = AutoNavCore.Engaged; }
        }
        if (failed)
        {
            // A persistence failure cannot leave unrecorded automation running.
            persistedFlight = null;
            CeaseFire(); issuing = true;
            try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "ABORTED"); }
            finally { AutoNavCore.ResetStatics(); issuing = false; }
            arrivalWatch.Cancel(); status = Text.Get("Persistence.write_failed");
        }
        else if (!AutoNavCore.Engaged)
        {
            if (savedFlight.Mode == SavedFlightMode.Arrived && arrivalWatch.Armed)
                Phobos.Ostranauts.Framework.Audio.CompletionCues.Complete(arrivalWatch, savedFlight.PlayerId, savedFlight.ShipId);
            else if (savedFlight.Mode != SavedFlightMode.Arrived) arrivalWatch.Cancel();
        }
    }

    private bool FinishSavedFlight(SavedFlightMode mode)
    {
        if (combatActive) return true; // Previous mission was suspended before control transferred.
        arrivalWatch.Cancel();
        if (mode == SavedFlightMode.Stopped) combinedHandoffPending = false;
        try
        {
            if (savedFlight == null || console == null || console.bDestroyed || savedFlight.ConsoleId != console.strID) return false;
            if (AutoNavCore.Engaged)
            { savedFlight.ElapsedSeconds = AutoNavCore.ElapsedSeconds; savedFlight.Coasting = AutoNavCore.Coasting; }
            savedFlight.Mode = mode;
            persistedFlight = null; // The next progress write is a transition, whatever the cadence says.
            return savedFlight.Valid && Store(console).TryWrite(savedFlight.Encode());
        }
        catch (Exception ex)
        {
            // Losing the save boundary must never skip the following actuator stop.
            log(ex.ToString()); return false;
        }
    }

    internal void ResumeSaved(CondOwner? co)
    {
        if (AutoNavCore.Engaged) { status = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return; }
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || co == null || co.bDestroyed || co.ship != CrewSim.coPlayer?.ship)
        { status = Text.Get("Persistence.open_console"); return; }
        try
        {
            if (!ReadSaved(co, out var snapshot)) return;
            if (!snapshot.IsResumable)
            { status = SavedDescription(snapshot); return; }
            console = co; savedFlight = snapshot;
            string? problem = !Plugin.Enabled.Value ? Text.Get("NavigationService.mod_disabled") : HardwareProblem(co);
            if (problem == null && !FlightBindingValid()) problem = Text.Get("Persistence.binding_changed");
            if (problem == null && Throttle <= 0) problem = Text.Get("NavigationService.throttle_zero_or_unavailable");
            if (problem == null && OtherControllerBusy()) problem = Text.Get("NavigationService.disengage_other_flight_automation_first");
            // The saved elapsed budget is preserved across suspension and reload (owner direction); a used-up budget is refused here.
            if (problem == null && (!ArrivalBrake.Finite(Plugin.MaxFlightSimHours.Value) ||
                snapshot.ElapsedSeconds >= Plugin.MaxFlightSimHours.Value * Phobos.Ostranauts.Framework.Units.SecondsPerHour))
                problem = Text.Get("Flight.result.TIMEOUT");
            var target = TargetRef.FromShipId(snapshot.TargetId);
            if (problem == null)
            {
                var sensing = SenseTarget(co, target?.ShipId);
                if (!sensing.Usable) problem = Text.Get(sensing.MessageKey);
            }
            if (problem == null && (target == null || !AutoNavCore.TryReadApproach(co.ship, target, snapshot.ArrivalKM, out _, out _)))
                problem = Text.Get("NavigationService.target_unavailable");
            if (problem == null && !snapshot.IsDocking && !snapshot.IsCombinedApproach)
                problem = AdmissionProblem(co, target!, snapshot.ArrivalKM, snapshot.ArrivalMS);
            if (problem != null)
            {
                FinishSavedFlight(snapshot.SuspendedMode);
                status = Text.Get("Persistence.suspended_reason", problem); log(status); return;
            }
            if (snapshot.IsDocking) { ResumeDocking(co, target!, snapshot); return; }
            if (snapshot.IsCombinedApproach) { ResumeApproachDock(co, target!, snapshot); return; }
            // No Resolve/BeginFlight here: those can advance target physics or
            // reset native navigation. All actual guidance waits for TimeAdvance.
            AutoNavCore.RestoreFlight(co.ship, target!, snapshot);
            if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co.ship, target!, readOnly: true))
            {
                FinishSavedFlight(snapshot.SuspendedMode); AutoNavCore.ResetStatics();
                status = Text.Get("Persistence.suspended_reason", Text.Get("NavigationService.insufficient_estimated_delta_v")); return;
            }
            savedFlight.Mode = snapshot.ActiveMode; CeaseFire(); PersistProgress();
            if (AutoNavCore.Engaged) status = Text.Get("Persistence.resumed", target!.DisplayName);
            log(status);
        }
        catch (Exception ex)
        {
            log(ex.ToString()); FinishSavedFlight(savedFlight?.SuspendedMode ?? SavedFlightMode.Suspended);
            AutoNavCore.ResetStatics(); status = Text.Get("Persistence.restore_error");
        }
    }

    private static bool OtherControllerBusy()
        => Plugin.Service?.industrial != null || OtherControllerBusyExceptIndustrial();
    // 29 September 2026 pass (FF6): the retired Approach Assist prototype is looked up once per session (its
    // type and Service property), not on every panel read and admission check; a controller loaded after
    // startup was never supported. The live Active value is still read on every call.
    private static bool foreignControllersResolved;
    private static System.Reflection.PropertyInfo? approachAssistService, approachAssistActive;
    private static Type? approachAssistServiceType;
    private static bool OtherControllerBusyExceptIndustrial()
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.ForeignController);
        if (Chainloader.PluginInfos.ContainsKey("com.mrkmg.ostranauts.autonavigate") || AutoNavCore.AutoDockBusy()) return true;
        if (!foreignControllersResolved)
        {
            foreignControllersResolved = true;
            Type? type = AccessTools.TypeByName("PhobosApproachAssist.Plugin");
            approachAssistService = type == null ? null : AccessTools.Property(type, "Service");
        }
        object? service = approachAssistService?.GetValue(null);
        if (service == null) return false;
        if (approachAssistServiceType != service.GetType())
        { approachAssistServiceType = service.GetType(); approachAssistActive = AccessTools.Property(approachAssistServiceType, "Active"); }
        return (bool)(approachAssistActive?.GetValue(service) ?? false);
    }

    internal bool HasResumableFlight(CondOwner co) => !AutoNavCore.Engaged && DisplaySnapshot(co) != null;
    internal void FlyOrResume(CondOwner co) { if (HasResumableFlight(co)) ResumeSaved(co); else Engage(co); }
    internal void Stop(CondOwner? co, string reason)
    {
        if(co!=null)CrewManualStop(co);
        if (industrial != null) { EndIndustrial(reason); return; }
        if (!AutoNavCore.Engaged)
        {
            if (co == null || co.bDestroyed || co.ship != CrewSim.coPlayer?.ship)
            { status = Text.Get("Persistence.open_console"); return; }
            CeaseFire();
            ReleaseNativeControls(co);
            if (!ReadSaved(co, out var snapshot)) return;
            console = co; savedFlight = snapshot;
        }
        Disengage(reason);
    }
    private static string SavedDescription(FlightSnapshot snapshot) => Text.Get("Persistence.state." + snapshot.Mode,
        snapshot.TargetId, snapshot.ElapsedSeconds, snapshot.ArrivalKM);

    private void ForgetSaved(CondOwner? co)
    {
        if (co == null || co.bDestroyed || co.ship != CrewSim.coPlayer?.ship || !co.HasCond("IsInstalled"))
        { status = Text.Get("Persistence.open_console"); return; }
        if (AutoNavCore.Engaged && co != console) { status = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return; }
        if (AutoNavCore.Engaged) Disengage(Text.Get("NavigationService.stopped_by_pilot_coasting"));
        Store(co).Clear(); console = co; savedFlight = null; status = Text.Get("Persistence.forgotten");
    }
}
