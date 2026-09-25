using System;
using System.Globalization;
using System.Linq;
using Ostranauts.ShipGUIs.NavStation;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal enum DockProgress { None, Approach, Suspended, Hold, Capture }
internal enum ManualPropulsion { Flow, Cycle, CycleEnabled, Safety, Shutdown }

// An ephemeral read model. Nullable measurements mean unavailable; no display string
// controls gameplay, and neither this snapshot nor weapon permission is persisted.
internal sealed class HubSnapshot
{
    internal InstrumentSnapshot Navigation = new();
    internal SavedFlightMode? Operation;
    internal ContactReading Contact;
    internal bool WorkingNavigation, WorkingPursuit, Active, CanApproachDock, CanEngage, CanSelectWeapons, FirePermitted;
    internal string Restriction = "", OffensiveTarget = "", OwnPort = "", TargetPort = "", Clearance = "", FireReason = "";
    internal int WeaponGroup;
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
        if (!IsLocalConsole(co) || !co.GetCOsSafe(true).Any(item => HasId(item, ModuleId) || HasId(item, PursuitId) || HasId(item, DamagedId) || HasId(item, PursuitDamagedId))) return;
        var store = new Phobos.Ostranauts.Framework.Persistence.ObjectStateStore(co.mapGUIPropMaps, "AutoNav.HubPlacement", co.strID, 1);
        if (store.Read(out _) != Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Missing) return;
        // Called at native module loading, never from rendering. Keep legacy coordinates and
        // every other instrument untouched; native Edit decides placement and overlap.
        CrewSim.coPlayer.LogMessage(Text.Get("Hub.placement_notice"), "Neutral", "Game");
        store.TryWrite(new System.Collections.Generic.Dictionary<string, string> { ["shown"] = "1" });
    }

    internal HubSnapshot ReadHub(CondOwner? co)
    {
        var view = new HubSnapshot { Navigation = ReadInstruments(co), OffensiveTarget = Text.Get("Instruments.no_target"),
            FireReason = Text.Get("Pursuit.ceased"), Clearance = Text.Get("Hub.unavailable") };
        view.Restriction = view.Navigation.Warning ? view.Navigation.Notice : status;
        if (!IsLocalConsole(co) || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return view;
        bool powered = !co!.HasCond("IsOff") && co.HasCond("IsPowered") && !co.HasCond("IsDamaged");
        view.WorkingNavigation = powered && co.GetCOsSafe(true).Any(item =>
            (HasId(item, ModuleId) || HasId(item, PursuitId)) && !item.HasCond("IsDamaged"));
        view.WorkingPursuit = powered && HasPursuit(co);
        var flight = DisplaySnapshot(co);
        view.Active = AutoNavCore.Engaged && console == co;
        view.Operation = flight?.Mode;
        var target = view.Active ? AutoNavCore.EngagedTarget : flight != null ? TargetRef.FromShipId(flight.TargetId) :
            GUIOrbitDraw.IsOpen() ? TargetRef.FromShipId(GUIOrbitDraw.CrossHairTarget?.Ship?.strRegID ?? "") : null;
        view.Contact = ReadContact(co, target);
        var own = co.ship;
        var other = target == null ? null : CrewSim.system?.GetShipByRegID(target.ShipId);
        if (powered && view.Contact.Usable && other?.objSS != null && own.objSS != null)
        {
            var offset = new NavVector((other.objSS.vPosx - own.objSS.vPosx) / AutoNavCore.M_TO_AU,
                (other.objSS.vPosy - own.objSS.vPosy) / AutoNavCore.M_TO_AU);
            var velocity = new NavVector((own.objSS.vVelX - other.objSS.vVelX) / AutoNavCore.M_TO_AU,
                (own.objSS.vVelY - other.objSS.vVelY) / AutoNavCore.M_TO_AU);
            view.RangeKM = Known(offset.Length / 1000); view.RelativeMS = Known(velocity.Length);
            view.ClosingMS = offset.Length > 0 ? Known(velocity.Dot(offset) / offset.Length, signed: true) : null;
            view.AlignmentDegrees = Known(DockingRules.Wrap(-Math.Atan2(offset.X, offset.Y) - own.objSS.fRot) * 180 / Math.PI, signed: true);
            string? dockingProblem;
            if (flight?.IsDocking == true || flight?.IsCombinedApproach == true)
            {
                view.OwnPort = flight.OwnPort; view.TargetPort = flight.TargetPort;
                dockingProblem = DockingAdapter.Check(own, other, flight.OwnPort, flight.TargetPort, checkFit: false);
                view.DockProgress = !view.Active ? DockProgress.Suspended : flight.IsCombinedApproach ? DockProgress.Approach :
                    dockHolding ? DockProgress.Hold : DockProgress.Capture;
            }
            else dockingProblem = DockingAdapter.SelectPorts(own, other, out view.OwnPort, out view.TargetPort);
            view.Clearance = Text.Get(dockingProblem ?? "Hub.clearance_valid");
            view.CanApproachDock = view.WorkingNavigation && !view.Active && flight == null &&
                !AutoNavCore.Engaged && HardwareProblem(co) == null && !OtherControllerBusy() &&
                dockingProblem == null && !co.HasCond("IsDamagedSoftware");
        }
        else if (flight?.IsDocking == true || flight?.IsCombinedApproach == true)
        {
            view.OwnPort = flight.OwnPort; view.TargetPort = flight.TargetPort;
            view.DockProgress = DockProgress.Suspended;
        }
        bool boundFire = view.Active && view.WorkingPursuit;
        view.FirePermitted = boundFire && Fire.Permitted;
        if (fireSelectionConsole == co && fireTargetId != null)
            view.OffensiveTarget = TargetRef.FromShipId(fireTargetId)?.DisplayName ?? fireTargetId;
        view.WeaponGroup = WeaponGroup(co);
        view.CanSelectWeapons = view.WorkingPursuit && (!AutoNavCore.Engaged || console == co);
        var offensive = fireSelectionConsole == co && fireTargetId != null ? TargetRef.FromShipId(fireTargetId) : null;
        view.CanEngage = boundFire && flight?.IsFollowing == true && view.Contact.Usable &&
            offensive != null && ReadContact(co, offensive).Usable && view.WeaponGroup > 0;
        bool freshFire = boundFire && view.FirePermitted && view.Contact.Usable && ArrivalBrake.Finite(Fire.SampleEpoch) &&
            StarSystem.fEpoch >= Fire.SampleEpoch && StarSystem.fEpoch - Fire.SampleEpoch <= FireRules.MaximumStep;
        string shortFire = Fire.Reason switch { "Pursuit.firing" => "firing", "Pursuit.aiming" => "aiming", "Pursuit.armed" => "armed",
            "Pursuit.no_weapons" => "empty", "Pursuit.fire_held" => "held", _ => "off" };
        view.FireReason = Text.Get("Hub.fire_readiness", Text.Get("Hub.fire." + (boundFire ? shortFire : "off")),
            freshFire ? Fire.InArcCount?.ToString() ?? Text.Get("Hub.unavailable") : Text.Get("Hub.unavailable"),
            freshFire ? Fire.AmmoCount?.ToString() ?? Text.Get("Hub.unavailable") : Text.Get("Hub.unavailable"),
            freshFire ? Fire.ReadyCount?.ToString() ?? Text.Get("Hub.unavailable") : Text.Get("Hub.unavailable"));
        if (!powered || own.bCheckPower || own.objSS == null) return view;
        float? throttle = ReadThrottleReading(co);
        view.RcsAuthorityMS2 = throttle.HasValue ? Known(own.RCSAccelMax / AutoNavCore.M_TO_AU * throttle.Value) : null;
        view.RcsFuelKG = Known(own.GetRCSRemain());
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
        view.CycleLimit = view.Safety == true ? NavModTorchDrive.GetLimiterSafetyMax(own) : 1;
        view.FlowLimit = Math.Min(1, view.CycleLimit * 2);
        view.CanManual = view.WorkingNavigation && (!AutoNavCore.Engaged || console == co) &&
            !core.HasCond("IsDamaged") && !own.IsDocked() && !own.IsMoored() && CrewSim.system != null && !CrewSim.system.IsInAtmo(own);
        view.CanShutdown = view.WorkingNavigation && (!AutoNavCore.Engaged || console == co);
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
