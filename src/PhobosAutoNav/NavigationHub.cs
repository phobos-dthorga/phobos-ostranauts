using System;
using System.Globalization;
using System.Linq;
using Ostranauts.ShipGUIs.NavStation;
using PhobosAutoNav.Core;
using Phobos.Ostranauts.Framework.Persistence;

namespace PhobosAutoNav;

internal enum DockProgress { None, Approach, Suspended, Hold, Capture }
internal enum ManualPropulsion { Flow, Cycle, CycleEnabled, Safety, Shutdown }

// An ephemeral read model. Nullable measurements mean unavailable; no display string
// controls gameplay, and neither this snapshot nor weapon permission is persisted.
internal sealed class HubSnapshot
{
    internal InstrumentSnapshot Navigation = new();
    internal string CompletionCue = "";
    internal bool Combat, CanCombat;
    internal string Movement = "";
    internal double? EffectiveSeparationKM;
    internal SavedFlightMode? Operation;
    internal ContactReading Contact, FireContact;
    internal string? TargetId;
    internal bool WorkingFire, AutoAiming, FireHeld, CanReturnFire, CanAim, CanChangeGroup;
    internal int Volleys = 1, Remaining;
    internal string WeaponCard = "", WeaponLabel = "", Ownership = "";
    internal bool WorkingNavigation, WorkingPursuit, Active, CanApproachDock, CanEngage, CanSelectWeapons, FirePermitted;
    /// <summary>A flight, departure or positioning job holds this console, or it keeps a suspended flight (Auto Nav 0.35.0):
    /// a new flight's first press offers to stop it.</summary>
    internal bool FlightHolds;
    internal string Restriction = "", OffensiveTarget = "", OwnPort = "", TargetPort = "", Clearance = "", FireReason = "";
    internal int WeaponGroup;
    internal WeaponGroupReading[] Groups = Array.Empty<WeaponGroupReading>();
    internal DockProgress DockProgress;
    internal double? RangeKM, ClosingMS, RelativeMS, AlignmentDegrees, RcsAuthorityMS2, RcsFuelKG,
        TorchHours, ConnectedKWh, DeliveredMS2, CoreMK, Flow, Cycle;
    internal bool? Safety, CycleEnabled, NoWake;
    internal bool CanManual, CanBurn, CanShutdown;
    internal float CycleLimit = 1, FlowLimit = 1;
}

internal sealed partial class NavigationService
{
    internal void HubLoaded(CondOwner co)
    {
        if (!IsLocalConsole(co) || !co.GetCOsSafe(true).Any(item => HasId(item, ModuleId) || HasId(item, PursuitId) || HasId(item, DamagedId) || HasId(item, PursuitDamagedId) || HasId(item, FireControlId) || HasId(item, FireControlDamagedId))) return;
        var notice = new Phobos.Ostranauts.Framework.Persistence.ObjectStateStore(co.mapGUIPropMaps, "AutoNav.N3Notice", co.strID, 1);
        if (HasPursuit(co) && notice.Read(out _) == SavedStateStatus.Missing)
        { CrewSim.coPlayer.LogMessage(Text.Get("FCS.migration"), "Neutral", "Game"); notice.TryWrite(new System.Collections.Generic.Dictionary<string,string> { ["shown"] = "1" }); }
        var store = new Phobos.Ostranauts.Framework.Persistence.ObjectStateStore(co.mapGUIPropMaps, "AutoNav.HubPlacement", co.strID, 1);
        if (store.Read(out _) != Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Missing) return;
        // Called at native module loading, never from rendering. Keep legacy coordinates and
        // every other instrument untouched; native Edit decides placement and overlap.
        CrewSim.coPlayer.LogMessage(Text.Get("Hub.placement_notice"), "Neutral", "Game");
        store.TryWrite(new System.Collections.Generic.Dictionary<string, string> { ["shown"] = "1" });
    }

    internal HubSnapshot ReadHub(CondOwner? co, string? page = null)
    {
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.PanelRead);
        var read = IsLocalConsole(co) && CrewSim.objInstance?.FinishedLoading == true ? new PresentationRead(co!, AutoNavCore.Engaged && console == co) : null;
        var view = new HubSnapshot { Navigation = ReadInstruments(co, read: read, details: page == null || page == "details"), CompletionCue = co == console ? ArrivalCueStatus : Text.Get("Cue.off"), OffensiveTarget = Text.Get("Instruments.no_target"),
            FireReason = Text.Get("Pursuit.ceased"), Clearance = Text.Get("Hub.unavailable") };
        view.Restriction = view.Navigation.Warning || view.Navigation.Resumable ||
            page != "fire" && Fire.State == FireState.Fault && status == Text.Get(Fire.Reason) ? view.Navigation.Notice : status;
        if (read == null) return view;
        view.FlightHolds = FlightHolds(co);
        bool powered = !co!.HasCond("IsOff") && co.HasCond("IsPowered") && !co.HasCond("IsDamaged");
        view.WorkingNavigation = powered && read.Navigation;
        view.WorkingPursuit = powered && read.Pursuit;
        view.WorkingFire = powered && read.FireModule != null;
        view.Combat = combatActive && console == co;
        view.CanCombat = view.WorkingPursuit && view.WorkingFire && CoordinatedFlight(co);
        view.Movement = AutoNavCore.Engaged && console == co ? MovementReason() : "";
        var flight = combatActive && console == co ? savedFlight : read.Flight;
        view.Active = AutoNavCore.Engaged && console == co;
        view.Operation = flight?.Mode;
        var target = read.Target;
        view.Contact = read.Contact(target);
        view.TargetId = target?.ShipId;
        var own = co.ship;
        var other = target == null ? null : CrewSim.system?.GetShipByRegID(target.ShipId);
        // An asteroid marker has live geometry but no ship: ports, clearance and docking stay ship-only.
        var situ = other?.objSS ?? (target?.IsStellar == true ? target.TargetSitu : null);
        view.EffectiveSeparationKM = view.Navigation.EffectiveArrivalKM;
        if (powered && view.Contact.Usable && situ != null && own.objSS != null)
        {
            var offset = new NavVector((situ.vPosx - own.objSS.vPosx) / AutoNavCore.M_TO_AU,
                (situ.vPosy - own.objSS.vPosy) / AutoNavCore.M_TO_AU);
            var velocity = new NavVector((own.objSS.vVelX - situ.vVelX) / AutoNavCore.M_TO_AU,
                (own.objSS.vVelY - situ.vVelY) / AutoNavCore.M_TO_AU);
            view.RangeKM = Known(offset.Length / 1000); view.RelativeMS = Known(velocity.Length);
            view.ClosingMS = offset.Length > 0 ? Known(velocity.Dot(offset) / offset.Length, signed: true) : null;
            view.AlignmentDegrees = Known(DockingRules.Wrap(-Math.Atan2(offset.X, offset.Y) - own.objSS.fRot) * 180 / Math.PI, signed: true);
            string? dockingProblem;
            if (other == null) dockingProblem = "Docking.asteroid";
            else if (flight?.IsDocking == true || flight?.IsCombinedApproach == true)
            {
                view.OwnPort = flight.OwnPort; view.TargetPort = flight.TargetPort;
                dockingProblem = DockingAdapter.Check(own, other, flight.OwnPort, flight.TargetPort, checkFit: false);
                if (dockingProblem != null)
                {
                    // Current admission is distinct from the earlier event retained in Info.
                    view.Restriction = view.Navigation.Notice = Text.Get(dockingProblem);
                    view.Navigation.Warning = true;
                }
                view.DockProgress = !view.Active ? DockProgress.Suspended : flight.IsCombinedApproach ? DockProgress.Approach :
                    dockHolding ? DockProgress.Hold : DockProgress.Capture;
            }
            else dockingProblem = page == null || page == "navigation" ? DockingAdapter.ReadAvailablePorts(own, other, out view.OwnPort, out view.TargetPort) : "Instruments.clearance_unknown";
            view.Clearance = Text.Get(dockingProblem ?? (flight == null ? "Hub.clearance_pending_fit" : "Hub.clearance_valid"));
            view.CanApproachDock = view.WorkingNavigation && !view.Active && flight == null &&
                !AutoNavCore.Engaged && read.Hardware == null && !OtherControllerBusy() &&
                dockingProblem == null && !co.HasCond("IsDamagedSoftware");
        }
        else if (flight?.IsDocking == true || flight?.IsCombinedApproach == true)
        {
            view.OwnPort = flight.OwnPort; view.TargetPort = flight.TargetPort;
            view.DockProgress = DockProgress.Suspended;
        }
        bool boundFire = fireConsole == co;
        bool freshFire = boundFire && powered && ArrivalBrake.Finite(Fire.SampleEpoch) && StarSystem.fEpoch >= Fire.SampleEpoch && StarSystem.fEpoch - Fire.SampleEpoch <= FireRules.MaximumStep;
        view.FirePermitted = boundFire && Fire.Permitted;
        view.AutoAiming = boundFire && autoAim;
        if (boundFire && fireTargetId != null) view.OffensiveTarget = FireTarget?.DisplayName ?? fireTargetId;
        view.FireContact = read.Contact(boundFire ? FireTarget : null);
        bool validPreferences = FirePreferences(co, out int group, out int volleys, out bool held);
        view.WeaponGroup = group; view.Volleys = volleys; view.Remaining = boundFire ? Fire.Remaining : 0;
        view.FireHeld = held || Fire.Owns(co.strID);
        view.CanSelectWeapons = view.WorkingFire && validPreferences && !Fire.OtherOwner(co.strID);
        view.CanChangeGroup = view.CanSelectWeapons;
        view.CanReturnFire = validPreferences && view.FireHeld;
        view.CanEngage = (page == null || page == "fire") && view.CanSelectWeapons && boundFire && view.FireContact.Usable && FireHardwareProblem(co, read) == null &&
            !AttachedFireTarget(co, FireTarget) && CoordinatedFlight(co) && freshFire && Fire.Weapons.Any(w => w.Eligible && w.Loaded);
        view.CanAim = view.CanEngage && AimProblem(co) == null;
        view.Ownership = Text.Get("FCS.state." + (boundFire ? Fire.State : view.FireHeld ? FireState.Hold : FireState.Native));
        view.FireReason = !view.WorkingFire ? Text.Get("FCS.module_required") : Text.Get("FCS.readiness", boundFire ? Text.Get(Fire.Reason) : view.Ownership,
            freshFire ? Fire.ReadyCount?.ToString() ?? "—" : "—", freshFire ? Fire.Weapons.Count.ToString() : "—");
        if (view.WorkingFire && !view.Navigation.Warning && (!view.WorkingNavigation || page == "fire" && boundFire && Fire.State == FireState.Fault)) view.Restriction = view.FireReason;
        // Steady reminder while sensors Auto Nav switched on stay on; faults keep the first line.
        string sensorsInUse = ""; ReadSensorsInUse(co, ref sensorsInUse);
        if (sensorsInUse.Length > 0)
            view.Restriction = string.IsNullOrWhiteSpace(view.Restriction) ? sensorsInUse :
                view.Navigation.Warning ? view.Restriction + "\n" + sensorsInUse : sensorsInUse + "\n" + view.Restriction;
        if (page == null || page == "fire")
        {
            ReadWeaponInventory(co, view, freshFire);
        }
        if (page != null && page != "systems") return view;
        if (!powered || own.bCheckPower || own.objSS == null) return view;
        float? throttle = ReadThrottleReading(co);
        view.RcsAuthorityMS2 = throttle.HasValue ? Known(own.RCSAccelMax / AutoNavCore.M_TO_AU * throttle.Value) : null;
        view.RcsFuelKG = Known(read.Fuel);
        view.ConnectedKWh = co.Pwr == null ? null : Known(co.Pwr.PowerConnected);
        view.DeliveredMS2 = Known(own.objSS.vAccIn.magnitude / AutoNavCore.M_TO_AU);
        var core = own.Reactor;
        if (core == null || core.bDestroyed) return view;
        view.TorchHours = Known(own.fShallowFusionRemain / 3600);
        view.CoreMK = Known(core.GetCondAmount("StatICCoreTemp"));
        view.Flow = ReactorNumber(own, "slidFlow"); view.Cycle = ReactorNumber(own, "slidCycle");
        double? ratio = ReactorNumber(own, "knobRatio");
        view.CycleEnabled = ratio == 1 ? true : ratio == 0 ? false : (bool?)null;
        view.NoWake = bool.TryParse(own.GetReactorGPMValue("bNWZ"), out bool noWake) ? noWake : (bool?)null;
        view.Safety = !co.mapGUIPropMaps.TryGetValue("Panel A", out var map) || !map.TryGetValue("bTorchSafety", out var safe) ?
            true : bool.TryParse(safe, out bool safety) ? safety : (bool?)null;
        view.CycleLimit = TorchCycle.Limit(own, view.Safety == true);
        view.FlowLimit = Math.Min(1, view.CycleLimit * 2);
        view.CanManual = (view.WorkingNavigation || view.WorkingFire) && (!AutoNavCore.Engaged || console == co) &&
            !core.HasCond("IsDamaged") && TowFlight.Problem(own) == null && CrewSim.system != null && !CrewSim.system.IsInAtmo(own);
        view.CanShutdown = (view.WorkingNavigation || view.WorkingFire) && (!AutoNavCore.Engaged || console == co);
        view.CanBurn = view.CanManual && view.Safety.HasValue && view.Flow.HasValue && view.Cycle.HasValue &&
            view.CycleEnabled.HasValue && !OtherControllerBusy() && own.shipStationKeepingTarget == null &&
            (own.aWPs == null || own.aWPs.Count == 0) && !PropOn(co, "chkStationKeeping") && !PropOn(co, "chkHoldThrust") &&
            AIShipManager.GetAIShipByRegID(own.strRegID) == null && TorchDriveController.Ready(core) && view.NoWake == false &&
            CrewSim.system != null && !CrewSim.system.IsWithinNoWakeRangeOfAnyStation(own.objSS, StarSystem.fEpoch);
        return view;
    }

    private static double? Known(double value, bool signed = false) => ArrivalBrake.Finite(value) && (signed || value >= 0) ? value : (double?)null;
    private static double? ReactorNumber(Ship ship, string key) => double.TryParse(ship.GetReactorGPMValue(key),
        NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value >= 0 && value <= 1 ? value : (double?)null;

    internal void ManualPropulsionAction(CondOwner? co, ManualPropulsion action, double value)
    {
        if (!ArrivalBrake.Finite(value) || value < 0 || value > 1) return;
        var view = ReadHub(co);
        bool permitted = action == ManualPropulsion.Shutdown ? view.CanShutdown : view.CanManual;
        if (!permitted || co?.ship.Reactor == null) { status = Text.Get("Hub.manual_unavailable"); return; }
        if ((action == ManualPropulsion.Flow || action == ManualPropulsion.Cycle || action == ManualPropulsion.CycleEnabled) && value > 0 && !view.CanBurn)
        { status = Text.Get("Hub.burn_restricted"); return; }
        if (action == ManualPropulsion.Flow && value > view.FlowLimit || action == ManualPropulsion.Cycle && value > view.CycleLimit) return;
        // Release automatic ownership before any pilot actuator write. Do not restore its
        // captured reactor values over the new manual command.
        if (AutoNavCore.Engaged || DisplaySnapshot(co) != null) Stop(co, Text.Get("Torch.manual"));
        CeaseFire();
        var ship = co.ship;
        if (!co.mapGUIPropMaps.TryGetValue("Panel A", out var props))
            co.mapGUIPropMaps["Panel A"] = props = new System.Collections.Generic.Dictionary<string, string>();
        props["chkEngage"] = "false";
        string number = value.ToString("R", CultureInfo.InvariantCulture);
        switch (action)
        {
            case ManualPropulsion.Flow:
                ship.SetReactorGPMValue("slidFlow", number);
                ship.SetReactorGPMValue("fFlowEpochResume", (StarSystem.fEpoch + 2).ToString("R", CultureInfo.InvariantCulture)); break;
            case ManualPropulsion.Cycle: ship.SetReactorGPMValue("slidCycle", number); break;
            case ManualPropulsion.CycleEnabled: ship.SetReactorGPMValue("knobRatio", value > 0 ? "1" : "0"); break;
            case ManualPropulsion.Safety:
                props["bTorchSafety"] = value > 0 ? "true" : "false";
                float limit = value > 0 ? NavModTorchDrive.GetLimiterSafetyMax(ship) : 1;
                props["fCrsLimMax"] = limit.ToString("R", CultureInfo.InvariantCulture);
                if (ReactorNumber(ship, "slidCycle") > limit) ship.SetReactorGPMValue("slidCycle", limit.ToString("R", CultureInfo.InvariantCulture));
                if (ReactorNumber(ship, "slidFlow") > Math.Min(1, limit * 2)) ship.SetReactorGPMValue("slidFlow", Math.Min(1, limit * 2).ToString("R", CultureInfo.InvariantCulture));
                break;
            case ManualPropulsion.Shutdown: ship.Reactor.AddCondAmount("IsOverrideOff", 1); break;
        }
        status = Text.Get("Hub.manual_taken");
    }
}
