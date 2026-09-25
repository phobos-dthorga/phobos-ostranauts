using System;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed class InstrumentSnapshot
{
    internal string Heading = "", Target = "", Range = "", RelativeSpeed = "", Notice = "", Details = "";
    internal double ArrivalKM, CruiseMS, ArrivalMS;
    internal bool TorchPreferred, CanFly, CanStop, CanAdjustArrival, CanAdjustPropulsion, Resumable, Warning, CanDock, Docking;
}

internal sealed partial class NavigationService
{
    private static bool IsLocalConsole(CondOwner? co) => co != null && !co.bDestroyed &&
        co.HasCond("IsInstalled") && !co.HasCond("IsLocked") && co.ship != null && co.ship == CrewSim.coPlayer?.ship;

    // UI reads only. Never Resolve a target or touch actuator/save state when drawing a panel.
    internal InstrumentSnapshot ReadInstruments(CondOwner? co, bool matchMotion = false)
    {
        var view = new InstrumentSnapshot { ArrivalKM = Plugin.DefaultArriveKM.Value,
            Heading = Text.Get("Instruments.waiting"), Target = Text.Get("Instruments.no_target"),
            Range = Text.Get("Instruments.range_unknown"), RelativeSpeed = Text.Get("Instruments.speed_unknown") };
        if (!IsLocalConsole(co) || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { view.Notice = view.Details = Text.Get("Persistence.open_console"); return view; }
        if (industrial != null)
        {
            view.Heading = Text.Get("Industrial.approaching"); view.Target = industrial.Target;
            view.Notice = view.Details = industrialNotice; view.CanStop = industrial.Console == co;
            return view;
        }
        bool ownsFlight = AutoNavCore.Engaged && console == co;
        bool otherFlight = AutoNavCore.Engaged && !ownsFlight;
        var snapshot = DisplaySnapshot(co);
        bool validPreferences = ReadPreferences(co!, out var preferences);
        view.Resumable = !AutoNavCore.Engaged && snapshot != null;
        bool captured = ownsFlight || snapshot != null;
        bool permitsTorch = ownsFlight ? AutoNavCore.FlightPrefersTorch : snapshot?.PreferTorch ?? Plugin.PreferTorch.Value;
        view.ArrivalKM = ownsFlight ? AutoNavCore.ArriveAU / AutoNavCore.KM_TO_AU : snapshot?.ArrivalKM ?? preferences.ArrivalKM;
        view.CruiseMS = snapshot?.CruiseMS ?? preferences.CruiseMS;
        view.ArrivalMS = snapshot?.ArrivalMS ?? (matchMotion ? 0 : preferences.ArrivalMS);
        view.TorchPreferred = permitsTorch && Plugin.PreferTorch.Value;
        view.CanAdjustArrival = validPreferences && SettingsHardwareReady(co) &&
            !otherFlight && InstrumentRules.CanSetArrival(AutoNavCore.Engaged, snapshot != null);
        view.CanAdjustPropulsion = !otherFlight && InstrumentRules.CanEnableTorch(captured, permitsTorch);
        var target = ownsFlight ? AutoNavCore.EngagedTarget : snapshot != null ? TargetRef.FromShipId(snapshot.TargetId) :
            GUIOrbitDraw.IsOpen() && GUIOrbitDraw.CrossHairTarget?.Ship != null && GUIOrbitDraw.CrossHairTarget.Ship != co!.ship
                ? TargetRef.FromShipId(GUIOrbitDraw.CrossHairTarget.Ship.strRegID) : null;
        if (target != null) view.Target = target.DisplayName;
        bool approachReady = false;
        var sensing = ReadContact(co, target);
        string clearance = Text.Get("Instruments.clearance_unknown");
        if (sensing.Usable && target != null && AutoNavCore.TryReadApproach(co!.ship, target, view.ArrivalKM, out var plan, out var speed))
        {
            approachReady = true;
            view.Range = Text.Get("Instruments.range", plan.RangeKM);
            view.RelativeSpeed = Text.Get("Instruments.relative_speed", speed);
            clearance = Text.Get("Instruments.clearance", view.ArrivalKM, plan.EffectiveArrivalKM);
        }
        string? problem = otherFlight ? Text.Get("Instruments.other_console") :
            !Plugin.Enabled.Value ? Text.Get("NavigationService.mod_disabled") : HardwareProblem(co);
        var recordStatus = Store(co!).Read(out var fields);
        FlightSnapshot? stored = null;
        bool validRecord = recordStatus == Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Missing ||
            recordStatus == Phobos.Ostranauts.Framework.Persistence.SavedStateStatus.Ready &&
            FlightSnapshot.TryDecode(fields, out stored) && stored.ConsoleId == co!.strID;
        if (!validRecord) problem = Text.Get("Persistence.invalid_state");
        view.CanAdjustArrival &= validRecord;
        if (!captured && !validPreferences) problem = Text.Get("Preferences.invalid");
        if (problem == null && OtherControllerBusy()) problem = Text.Get("NavigationService.disengage_other_flight_automation_first");
        if (problem == null && (target != null || captured) && !sensing.Usable) problem = Text.Get(sensing.MessageKey);
        // Dock retains its own terminal admission policy and must not inherit Fly's arrival settings.
        view.CanDock = !AutoNavCore.Engaged && problem == null && approachReady && !view.Resumable;
        if (problem == null && !AutoNavCore.Engaged && target != null && snapshot?.IsDocking != true)
            problem = AdmissionProblem(co!, target, view.ArrivalKM, view.ArrivalMS);
        view.Warning = problem != null;
        view.Heading = ownsFlight ? Text.Get("Instruments.phase." + AutoNavCore.CurrentPhase) :
            problem != null ? Text.Get("Instruments.blocked") : view.Resumable ? Text.Get("Instruments.suspended") :
            target != null ? Text.Get("Instruments.ready") : Text.Get("Instruments.select_target");
        bool previousDestination = !AutoNavCore.Engaged && validRecord && stored != null && target?.ShipId == stored.TargetId;
        if (previousDestination && problem == null && !view.Resumable)
            view.Heading = Text.Get(stored!.Mode == SavedFlightMode.Arrived ? "Instruments.arrived" : "Instruments.stopped");
        view.CanFly = !AutoNavCore.Engaged && problem == null && approachReady;
        view.CanStop = !otherFlight && (ownsFlight || view.Resumable);
        view.Notice = problem ?? (ownsFlight ? Text.Get(AutoNavCore.CurrentPhase == AutoNavCore.Phase.Coast ? "Instruments.coast_hint" : Torch.Reason) :
            view.Resumable ? Text.Get("Instruments.resume_hint") : Text.Get(target != null ? "Instruments.ready_hint" : "Instruments.select_hint"));
        if (previousDestination && problem == null && !view.Resumable) view.Notice = Text.Get("Instruments.guidance_off");
        view.Details = Text.Get("Instruments.details", view.Target, status, view.Notice, clearance, view.CruiseMS,
            Plugin.TorchMaximumG.Value, Plugin.ResumeAfterLoad.Value ? Text.Get("Instruments.on") : Text.Get("Instruments.off")) +
            "\n\n" + Text.Get(captured ? "Instruments.captured_hint" : "Instruments.default_hint") +
            "\n\n" + Text.Get("Instruments.help");
        view.Details += "\n\n" + Text.Get("Preferences.profile", view.CruiseMS, view.ArrivalMS);
        view.Details += "\n\n" + Text.Get("Docking.help");
        if (snapshot?.IsDocking == true)
        {
            view.Docking = true;
            view.Heading = Text.Get(problem != null ? "Instruments.blocked" : ownsFlight ? "Docking.heading" : "Docking.suspended_heading");
            view.Notice = problem ?? (ownsFlight ? status : Text.Get("Docking.resume_hint"));
            view.Details = Text.Get("Docking.details", view.Target, status, view.Notice, view.Range, view.RelativeSpeed) +
                "\n\n" + Text.Get("Docking.binding", snapshot.OwnPort, snapshot.TargetPort) +
                "\n\n" + Text.Get("Docking.help");
        }
        if (ownsFlight && snapshot?.IsPursuit == true)
        {
            view.Heading = Text.Get(Fire.Permitted ? "Pursuit.engaging" : snapshot.IsFollowing ? "Pursuit.following" : "Pursuit.rendezvous");
            view.Notice = Text.Get(Fire.Reason);
        }
        if (ownsFlight && AutoNavCore.ControlLimited)
        { view.Notice = Text.Get("Pursuit.control_limited"); view.Warning = true; }
        view.Details += "\n\n" + Text.Get(sensing.MessageKey) + "\n" + Text.Get("Sensors.help");
        return view;
    }

    internal void StepPanelArrival(CondOwner? co, int direction)
    {
        if (!CanChangePreferences(co)) return;
        if (!ReadPreferences(co!, out var preferences)) { status = Text.Get("Preferences.invalid"); return; }
        SetFlightSetting(co, FlightSetting.ArrivalDistance, InstrumentRules.StepArrival(preferences.ArrivalKM, direction));
    }

    internal void SetPanelTorch(CondOwner? co, bool preferred)
    {
        if (!IsLocalConsole(co) || CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) return;
        if (AutoNavCore.Engaged && console != co) { status = Text.Get("Instruments.other_console"); return; }
        var saved = DisplaySnapshot(co);
        bool captured = AutoNavCore.Engaged ? AutoNavCore.FlightPrefersTorch : saved?.PreferTorch ?? false;
        if (preferred && !InstrumentRules.CanEnableTorch(AutoNavCore.Engaged || saved != null, captured))
        { status = Text.Get("Instruments.torch_locked"); return; }
        SetTorchPreference(preferred);
    }

    private void SetTorchPreference(bool preferred)
    {
        bool changed = Plugin.PreferTorch.Value != preferred;
        Plugin.PreferTorch.Value = preferred;
        if (!preferred) Torch.Cut();
        if (changed) Plugin.PreferTorch.ConfigFile.Save();
        status = Text.Get("Torch.preference_saved", preferred);
    }
}
