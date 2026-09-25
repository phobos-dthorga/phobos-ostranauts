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
    private FlightSnapshot? savedFlight;
    private bool restorePending;
    private bool combinedHandoffPending;
    private static ObjectStateStore Store(CondOwner co) => new ObjectStateStore(co.mapGUIPropMaps,
        FlightSnapshot.StoreName, co.strID, FlightSnapshot.Schema);

    // Load/NewGame may be nested. Drop references only: never erase the old
    // world's snapshot or send a maneuver into a world being torn down.
    internal void WorldChanging()
    {
        ResetPursuit(); Fire.Reset(); Torch.Reset(); AutoNavCore.ResetStatics(); console = null; savedFlight = null;
        issuing = false; restorePending = false; combinedHandoffPending = false; status = Text.Get("Persistence.loading");
    }
    internal void WorldLoaded() => restorePending = true;

    internal static void PrepareSavedPhysics(ShipSitu situ, JsonShipSitu saved)
    {
        if (!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != situ) return;
        saved.vAccRCS = UnityEngine.Vector2.zero;
        saved.vAccIn = UnityEngine.Vector2.zero;
        saved.fA = 0;
    }

    internal void UpdatePersistence()
    {
        if (!restorePending || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading ||
            CrewSim.coPlayer?.ship == null) return;
        restorePending = false;
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
            if (snapshot.Mode == SavedFlightMode.Active && Plugin.ResumeAfterLoad.Value) ResumeSaved(selected);
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

    private void PersistProgress()
    {
        if (savedFlight == null || console == null || console.bDestroyed || savedFlight.ConsoleId != console.strID) return;
        savedFlight.ElapsedSeconds = AutoNavCore.ElapsedSeconds;
        savedFlight.Coasting = AutoNavCore.Coasting;
        if (!AutoNavCore.Engaged) savedFlight.Mode = AutoNavCore.LastResult == "ARRIVED" ? SavedFlightMode.Arrived : SavedFlightMode.Stopped;
        if (!savedFlight.Valid || !Store(console).TryWrite(savedFlight.Encode()))
        {
            // A persistence failure cannot leave unrecorded automation running.
            Fire.Cease(); issuing = true;
            try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, "ABORTED"); }
            finally { AutoNavCore.ResetStatics(); issuing = false; }
            status = Text.Get("Persistence.write_failed");
        }
    }

    private bool FinishSavedFlight(SavedFlightMode mode)
    {
        if (mode == SavedFlightMode.Stopped) combinedHandoffPending = false;
        try
        {
            if (savedFlight == null || console == null || console.bDestroyed || savedFlight.ConsoleId != console.strID) return false;
            if (AutoNavCore.Engaged)
            { savedFlight.ElapsedSeconds = AutoNavCore.ElapsedSeconds; savedFlight.Coasting = AutoNavCore.Coasting; }
            savedFlight.Mode = mode;
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
            if (problem == null && (!ArrivalBrake.Finite(Plugin.MaxFlightSimHours.Value) ||
                snapshot.ElapsedSeconds >= Plugin.MaxFlightSimHours.Value * Phobos.Ostranauts.Framework.Units.SecondsPerHour))
                problem = Text.Get("Flight.result.TIMEOUT");
            var target = TargetRef.FromShipId(snapshot.TargetId);
            if (problem == null)
            {
                var sensing = ReadContact(co, target);
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
            savedFlight.Mode = snapshot.ActiveMode; Fire.Reset(); PersistProgress();
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
    {
        if (Chainloader.PluginInfos.ContainsKey("com.mrkmg.ostranauts.autonavigate") || AutoNavCore.AutoDockBusy()) return true;
        Type? type = AccessTools.TypeByName("PhobosApproachAssist.Plugin");
        object? service = type == null ? null : AccessTools.Property(type, "Service")?.GetValue(null);
        return service != null && (bool)(AccessTools.Property(service.GetType(), "Active")?.GetValue(service) ?? false);
    }

    internal bool HasResumableFlight(CondOwner co) => !AutoNavCore.Engaged && DisplaySnapshot(co) != null;
    internal void FlyOrResume(CondOwner co) { if (HasResumableFlight(co)) ResumeSaved(co); else Engage(co); }
    internal void Stop(CondOwner? co, string reason)
    {
        if (!AutoNavCore.Engaged)
        {
            if (co == null || co.bDestroyed || co.ship != CrewSim.coPlayer?.ship)
            { status = Text.Get("Persistence.open_console"); return; }
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
