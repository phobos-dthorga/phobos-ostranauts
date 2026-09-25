using System;
using System.Globalization;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed partial class NavigationService
{
    internal const string ModuleId = "PhobosNavModAutoNav";
    internal const string DamagedId = ModuleId + "Dmg";
    internal const string PursuitId = "PhobosNavModPursuit";
    internal const string PursuitDamagedId = PursuitId + "Dmg";
    internal const string FireControlId = "PhobosNavModFireControl", FireControlDamagedId = FireControlId + "Dmg";
    internal FireControlController Fire { get; } = new();
    private readonly Action<string> log;
    private CondOwner? console;
    private bool issuing;
    internal TorchDriveController Torch { get; } = new();
    private string status = Text.Get("NavigationService.idle");
    private static CondOwner? OpenConsole => GUIOrbitDraw.IsOpen() ? GUIOrbitDraw.Instance.COSelfBase() : null;
    internal NavigationService(Action<string> log) { this.log = log; }

    internal string ArrivalCueStatus => Text.Get(arrivalWatch.Armed ? "Cue.watching" : arrivalWatch.Completed ? "Cue.completed" : "Cue.off");
    internal bool WatchArrival(CondOwner? co, bool enable)
    {
        if (!enable) { arrivalWatch.Cancel(); return true; }
        if (co == null || co != console || HardwareProblem(co) != null || !AutoNavCore.Engaged ||
            savedFlight == null || (savedFlight.Mode != SavedFlightMode.Active && savedFlight.Mode != SavedFlightMode.Rendezvous))
        { status = Text.Get("Cue.start_first"); return false; }
        arrivalWatch.Arm(savedFlight.PlayerId, savedFlight.ShipId); return true;
    }

    internal float Throttle => ReadThrottle(console);

    private static bool HasId(CondOwner co, string id) => !co.bDestroyed && (co.strName == id || co.strCODef == id);
    private static bool PropOn(CondOwner co, string key) => co.mapGUIPropMaps.TryGetValue("Panel A", out var props)
        && props.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    private static string? HardwareProblem(CondOwner? co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled")) return Text.Get("NavigationService.installed_nav_console_required");
        if (co.HasCond("IsOff") || !co.HasCond("IsPowered") || co.HasCond("IsDamaged")) return Text.Get("NavigationService.console_is_off_unpowered_or_damaged");
        if (!co.GetCOsSafe(true).Any(item => (HasId(item, ModuleId) || HasId(item, PursuitId)) && !item.HasCond("IsDamaged"))) return Text.Get("NavigationService.working_phobos_auto_nav_module_required");
        if (co.ship == null || co.ship.bDestroyed || CrewSim.coPlayer == null || CrewSim.coPlayer.ship != co.ship) return Text.Get("NavigationService.player_must_be_aboard_the_controlled_ship");
        if (co.ship.IsDocked()) return Text.Get("NavigationService.undock_before_engagement");
        // Pending power/sensor refresh is a contact suspension, not a discarded
        // destination. Every guidance entry point checks NativeContactReader.
        if ((TorchDriveController.ThrustRequested(co.ship) && !Plugin.Service.Torch.Owns(co.ship)) || co.ship.shipStationKeepingTarget != null || (co.ship.aWPs != null && co.ship.aWPs.Count > 0)
            || PropOn(co, "chkStationKeeping") || PropOn(co, "chkHoldThrust") || PropOn(co, "chkEngage")
            || AIShipManager.GetAIShipByRegID(co.ship.strRegID) != null) return Text.Get("NavigationService.disengage_other_flight_automation_first");
        if (CrewSim.system == null || CrewSim.system.IsInAtmo(co.ship)) return Text.Get("NavigationService.free_space_flight_only");
        if (co.ship.RCSCount <= 0 || co.ship.GetRCSRemain() <= 0) return Text.Get("NavigationService.working_rcs_and_fuel_required");
        if (co.ship.objSS == null || !ArrivalBrake.Finite(co.ship.RCSAccelMax) || co.ship.RCSAccelMax <= 0) return Text.Get("NavigationService.rcs_acceleration_unavailable");
        return null;
    }

    internal void Engage(CondOwner? co, float? arrivalKM = null, SavedFlightMode mode = SavedFlightMode.Active)
    {
        if (AutoNavCore.Engaged) { status = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return; }
        try
        {
            if (!Plugin.Enabled.Value) { status = Text.Get("NavigationService.mod_disabled_in_settings"); return; }
            if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading) { status = Text.Get("NavigationService.world_is_loading"); return; }
            // No upstream dependency. Refuse this prototype alongside the original flight plugin.
            if (Chainloader.PluginInfos.ContainsKey("com.mrkmg.ostranauts.autonavigate"))
            { status = Text.Get("NavigationService.disable_original_auto_navigate_and_restart_before"); return; }
            string? problem = HardwareProblem(co);
            if (problem == null && mode != SavedFlightMode.Active && !HasPursuit(co)) problem = Text.Get("Pursuit.module_required");
            if (problem != null) { status = problem; return; }
            if (!CanReplaceFlight(co!)) return;
            if (DisplaySnapshot(co) != null) { status = Text.Get("Preferences.captured"); return; }
            console = co;
            savedFlight = null;
            if (Throttle <= 0) { status = Text.Get("NavigationService.set_the_nav_console_throttle_above_zero"); return; }
            var contact = GUIOrbitDraw.CrossHairTarget;
            if (contact?.Ship == null || contact.Ship == co!.ship || contact.Ship.bDestroyed || contact.Ship.HideFromSystem || contact.Ship.IsStationHidden())
            { status = Text.Get("NavigationService.select_another_ship_or_station_planetary_travel"); return; }
            if (OtherControllerBusy()) { status = Text.Get("NavigationService.disengage_other_flight_automation_first"); return; }
            var target = TargetRef.FromCrossHair();
            if (target == null) { status = Text.Get("NavigationService.target_unavailable"); return; }
            var sensing = ReadContact(co, target);
            if (!sensing.Usable) { status = Text.Get(sensing.MessageKey); return; }
            if (!ReadPreferences(co!, out var preferences)) { status = Text.Get("Preferences.invalid"); return; }
            double cruise = preferences.CruiseMS, arrival = mode == SavedFlightMode.Active ? preferences.ArrivalMS : 0,
                distance = arrivalKM ?? preferences.ArrivalKM;
            var coastSettings = Plugin.ReadCoastSettings();
            if (!ArrivalBrake.Finite(cruise) || !ArrivalBrake.Finite(arrival) || !ApproachRules.ValidArrival(distance) || !coastSettings.IsValid)
            { status = Text.Get("NavigationService.invalid_flight_settings"); return; }
            if (!target.Resolve(out _, out _, out _, out _)
                || !AutoNavCore.TryReadApproach(co!.ship, target, distance, out _, out _))
            { status = Text.Get("NavigationService.approach_data_unavailable"); return; }
            problem = AdmissionProblem(co!, target, distance, arrival);
            if (problem != null) { status = problem; return; }
            AutoNavCore.CruiseAU = cruise * AutoNavCore.M_TO_AU;
            AutoNavCore.ArrSpdAU = Math.Min(arrival, cruise) * AutoNavCore.M_TO_AU;
            AutoNavCore.ArriveAU = distance * AutoNavCore.KM_TO_AU;
            if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co!.ship, target))
            { status = Text.Get("NavigationService.insufficient_estimated_delta_v"); return; }
            if (!PreferenceStore(co!).TryWrite(preferences.Encode())) { status = Text.Get("Preferences.invalid"); return; }
            CeaseFire();
            issuing = true;
            try { AutoNavCore.BeginFlight(co!.ship, target, coastSettings, Plugin.PreferTorch.Value); } finally { issuing = false; }
            savedFlight = CaptureFlight(co!, target, cruise, Math.Min(arrival, cruise), distance, mode);
            AutoNavCore.Following = mode == SavedFlightMode.Following;
            PersistProgress();
            if (AutoNavCore.Engaged) status = Text.Get("NavigationService.flight_engaged");
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("NavigationService.engagement_failed_see_log")); }
        log(status);
    }

    internal void Tick(ShipSitu situ, double dt, bool ignoreAcceleration)
    {
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading || CrewSim.Paused) return;
        if (!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != situ || ignoreAcceleration || dt == 0) return;
        if (DockingActive) return; // Docking samples both ships at the system boundary.
        using var measurement = Phobos.Ostranauts.Framework.Diagnostics.Performance.Measure(PerformanceMetrics.Guidance);
        try
        {
            string? problem = !Plugin.Enabled.Value ? Text.Get("NavigationService.mod_disabled") : HardwareProblem(console);
            if (problem == null && !FlightBindingValid()) problem = Text.Get("Persistence.binding_changed");
            if (problem == null && Torch.ControlsChanged) problem = Text.Get("Torch.manual");
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt < 0 || dt > Plugin.MaximumStepSeconds.Value)) problem = Text.Get("NavigationService.simulation_step_too_large_or_invalid_reduce");
            if (problem == null && Throttle <= 0) problem = Text.Get("NavigationService.throttle_zero_or_unavailable");
            if (problem == null && AutoNavCore.AutoDockBusy()) problem = Text.Get("NavigationService.auto_dock_took_control");
            if (problem == null && (!ArrivalBrake.Finite(Plugin.ArrivalSpeedTolerance.Value) || !ArrivalBrake.Finite(Plugin.MaximumStepSeconds.Value))) problem = Text.Get("NavigationService.invalid_safety_settings");
            if (problem != null) { Disengage(problem); return; }
            var sensing = Torch.ContactLoss ?? ReadContact(console, AutoNavCore.EngagedTarget);
            if (!sensing.Usable) { SuspendForContact(sensing); return; }
            issuing = true;
            try { AutoNavCore.SteerFlight(AutoNavCore.EngagedPlayer, AutoNavCore.EngagedTarget, dt); }
            finally { issuing = false; }
            if (QueueDockingHandoff()) return;
            status = AutoNavCore.Engaged ? AutoNavCore.PhaseName : DescribeResult(AutoNavCore.LastResult);
            if (AutoNavCore.ControlLimited) status = Text.Get("Pursuit.control_limited");
            PersistProgress();
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("NavigationService.flight_error_see_log")); }
    }

    private static string DescribeResult(string? result) => result switch
    {
        "ARRIVED" => Text.Get("Flight.result.ARRIVED"),
        "ABORTED" => Text.Get("Flight.result.ABORTED"),
        "DOCKED" => Text.Get("Flight.result.DOCKED"),
        "MANUAL" => Text.Get("Flight.result.MANUAL"),
        "TIMEOUT" => Text.Get("Flight.result.TIMEOUT"),
        "TGT LOST" => Text.Get("Flight.result.TGT_LOST"),
        "NO FUEL" => Text.Get("Flight.result.NO_FUEL"),
        "INVALID FLIGHT DATA" => Text.Get("Flight.result.INVALID_FLIGHT_DATA"),
        "BRAKE UNAVAILABLE" => Text.Get("Flight.result.BRAKE_UNAVAILABLE"),
        "THROTTLE ZERO" => Text.Get("Flight.result.THROTTLE_ZERO"),
        _ => result ?? Text.Get("NavigationService.stopped")
    };

    internal void ExternalControl(Ship ship, float x, float y, float rotation)
    {
        if (!issuing && industrial?.Carrier == ship && (x != 0 || y != 0 || rotation != 0))
            EndIndustrial(Text.Get("NavigationService.external_maneuver_command_pilot_other_controller_has"));
        bool wasAiming = autoAim;
        if (!issuing && autoAim && fireConsole?.ship == ship && (x != 0 || y != 0 || rotation != 0)) CeaseFire();
        // Closing the native console emits a zero command; it must not cancel off-console travel.
        if (!issuing && (x != 0 || y != 0 || rotation != 0) && AutoNavCore.Engaged && AutoNavCore.EngagedPlayer == ship)
            Disengage(Text.Get("NavigationService.external_maneuver_command_pilot_other_controller_has"), keepWeapons: !wasAiming);
    }

    internal void ExternalReactorControl(Ship ship, string key, string value)
    {
        if (!issuing && industrial?.Carrier == ship) EndIndustrial(Text.Get("Torch.manual"));
        if (!issuing && autoAim && fireConsole?.ship == ship) CeaseFire();
        if (AutoNavCore.Engaged && Torch.ChangedByPilot(ship, key, value))
        {
            Torch.YieldToPilot();
            Disengage(Text.Get("Torch.manual"));
        }
    }

    internal void Disengage(string reason, bool keepWeapons = false)
    {
        EndIndustrial(reason);
        if (!keepWeapons) CeaseFire();
        FinishSavedFlight(SavedFlightMode.Stopped);
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, reason); }
        catch (Exception ex) { log(Text.Get("NavigationService.stop_failed", ex)); }
        finally { AutoNavCore.ResetStatics(); issuing = false; status = reason; }
        log(reason);
    }

    private string ApproachSummary(CondOwner? co, bool detailed)
    {
        co ??= console;
        double defaultDistance = co != null && ReadPreferences(co, out var defaults) ? defaults.ArrivalKM : Plugin.DefaultArriveKM.Value;
        var snapshot = DisplaySnapshot(co);
        double requested = AutoNavCore.Engaged ? AutoNavCore.ArriveAU / AutoNavCore.KM_TO_AU : snapshot?.ArrivalKM ?? defaultDistance;
        var target = AutoNavCore.Engaged ? AutoNavCore.EngagedTarget :
            snapshot != null ? TargetRef.FromShipId(snapshot.TargetId) :
            GUIOrbitDraw.IsOpen() && GUIOrbitDraw.CrossHairTarget?.Ship != null ? TargetRef.FromCrossHair() : null;
        var ship = AutoNavCore.Engaged ? AutoNavCore.EngagedPlayer : co?.ship;
        var sensing = NativeContactReader.Read(ship, target?.ShipId);
        if (snapshot?.IsDocking == true) return Text.Get("Docking.binding", snapshot.OwnPort, snapshot.TargetPort)
            + "\n" + Text.Get(sensing.MessageKey);
        if (!sensing.Usable) return Text.Get(sensing.MessageKey);
        if (ship != null && target != null && AutoNavCore.TryReadApproach(ship, target, requested, out var plan, out var speed))
            return Text.Get(detailed ? "NavigationService.approach_details" : "NavigationService.approach_range",
                plan.RangeKM, plan.EffectiveArrivalKM, speed, plan.RequestedArrivalKM) + "\n" + Text.Get(sensing.MessageKey);
        return Text.Get("NavigationService.arrival_default", requested);
    }

    internal bool Command(string[] words, out string response)
    {
        try
        {
            string verb = words.Length == 1 ? "help" : words[1].ToLowerInvariant();
            if (words.Length > (verb == "fly" || verb == "arrival" || verb == "cruise" || verb == "arrivalspeed" || verb == "torch" || verb == "weapons" || verb == "rendezvous" || verb == "follow" ? 3 : 2))
            { response = Text.Get("NavigationService.no_extra_arguments_accepted_use_phobosnav_help"); return false; }
            switch (verb)
            {
                case "help": response = Text.Get("NavigationService.phobosnav_help_status_settings_fly_stop_spawn"); return true;
                case "status": response = Text.Get("NavigationService.phobos_auto_nav_engaged", Plugin.Version, AutoNavCore.Engaged, status, EquipmentContent.Status)
                    + "\n" + ApproachSummary(OpenConsole, detailed: true) + "\n" + Text.Get(Torch.Reason); return true;
                case "settings":
                    var snapshot = DisplaySnapshot(OpenConsole ?? console);
                    var settingsConsole = OpenConsole ?? console;
                    if (settingsConsole == null || !IsLocalConsole(settingsConsole))
                    { response = Text.Get("Preferences.console_required"); return false; }
                    if (!ReadPreferences(settingsConsole, out var preferences) && snapshot == null)
                    { response = Text.Get("Preferences.invalid"); return false; }
                    var coast = snapshot?.Coast ?? Plugin.ReadCoastSettings();
                    response = Text.Get("NavigationService.cruise_m_s_arrival_m_s_at", snapshot?.CruiseMS ?? preferences.CruiseMS, snapshot?.ArrivalMS ?? preferences.ArrivalMS, snapshot?.ArrivalKM ?? preferences.ArrivalKM, Plugin.ArrivalSpeedTolerance.Value, Plugin.MaximumStepSeconds.Value, Plugin.Id)
                        + "\n" + Text.Get("NavigationService.coast_settings", coast.MinimumToleranceMS, coast.SpeedTolerancePercent,
                            coast.EnterFraction * 100, coast.BurnHeadingToleranceDegrees, CoastRules.DriftLookaheadSeconds,
                            CoastRules.DriftRadiusFraction * 100,
                            Text.Get(snapshot != null ? "Persistence.saved_profile" : "NavigationService.next_flight"))
                        + "\n" + Text.Get("Persistence.settings", Plugin.ResumeAfterLoad.Value)
                        + "\n" + Text.Get("Torch.settings", snapshot?.PreferTorch ?? Plugin.PreferTorch.Value,
                            Plugin.TorchMaximumG.Value, Plugin.TorchMinimumCorrectionMS.Value);
                    return true;
                case "fly":
                    float? requested = null;
                    if (words.Length == 3)
                    {
                        if (!ApproachRules.TryParseArrival(words[2], out float km)) { response = ArrivalUsage(); return false; }
                        requested = km;
                    }
                    Engage(OpenConsole, requested);
                    response = status + "\n" + ApproachSummary(OpenConsole, detailed: true);
                    return AutoNavCore.Engaged;
                case "rendezvous":
                case "follow":
                    float? separation = null;
                    if (words.Length == 3)
                    {
                        if (!ApproachRules.TryParseArrival(words[2], out float km)) { response = ArrivalUsage(); return false; }
                        separation = km;
                    }
                    StartPursuit(OpenConsole, verb == "follow", separation); response = status; return AutoNavCore.Engaged;
                case "weapons":
                    if (words.Length != 3 || !int.TryParse(words[2], out int group) || group < 1 || group > 9)
                    { response = Text.Get("Pursuit.weapon_usage"); return false; }
                    SelectWeapons(OpenConsole, group); response = status; return true;
                case "firetarget": SelectFireTarget(OpenConsole); response = status; return true;
                case "engage": EngageWeapons(OpenConsole); response = status; return Fire.Permitted;
                case "ceasefire": CeaseFire(); response = status; return true;
                case "autoaim": ToggleAutoAim(OpenConsole); response = status; return autoAim;
                case "nativefire": ReturnFireToNative(OpenConsole); response = status; return true;
                case "volley": StepVolleys(OpenConsole); response = status; return true;
                case "fireweapon": BrowseWeapon(OpenConsole); response = status; return true;
                case "aimweapon": UseAimReference(OpenConsole); response = status; return true;
                case "spawnpursuit": return Spawn(out response, PursuitId);
                case "dock": Dock(OpenConsole); response = status; return AutoNavCore.Engaged;
                case "approachdock": ApproachDock(OpenConsole); response = status; return AutoNavCore.Engaged;
                case "arrival":
                case "cruise":
                case "arrivalspeed":
                    if (words.Length != 3 || !double.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double requestedSetting))
                    { response = Text.Get("Preferences.usage"); return false; }
                    bool changed = SetFlightSetting(OpenConsole, verb == "arrival" ? FlightSetting.ArrivalDistance :
                        verb == "cruise" ? FlightSetting.Cruise : FlightSetting.ArrivalSpeed, requestedSetting);
                    response = status; return changed;
                case "defaults":
                    bool reset = ResetPreferences(OpenConsole); response = status; return reset;
                case "torch":
                    if (words.Length != 3 || (words[2] != "on" && words[2] != "off"))
                    { response = Text.Get("Torch.usage"); return false; }
                    SetTorchPreference(words[2] == "on");
                    response = status;
                    return true;
                case "watch": bool watching = WatchArrival(OpenConsole ?? console, true); response = watching ? ArrivalCueStatus : status; return watching;
                case "unwatch": WatchArrival(OpenConsole ?? console, false); response = ArrivalCueStatus; return true;
                case "cue-volume": Phobos.Ostranauts.Framework.Audio.CompletionCues.CycleVolume(); response = Phobos.Ostranauts.Framework.Audio.CompletionCues.VolumeLabel; return true;
                case "stop": Stop(OpenConsole ?? console, Text.Get("NavigationService.stopped_by_pilot_coasting")); response = status; return true;
                case "resume": ResumeSaved(OpenConsole ?? console); response = status; return AutoNavCore.Engaged;
                case "forget": ForgetSaved(OpenConsole ?? console); response = status; return true;
                case "spawn": return Spawn(out response);
                default: response = Text.Get("NavigationService.unknown_command_use_phobosnav_help"); return false;
            }
        }
        catch (Exception ex) { log(ex.ToString()); response = Text.Get("NavigationService.command_failed_see_bepinex_log"); return false; }
    }

    private static string ArrivalUsage() => Text.Get("NavigationService.arrival_usage",
        ApproachRules.MinimumArrivalKM, ApproachRules.MaximumArrivalKM);

    private bool Spawn(out string response, string module = ModuleId)
    {
        var co = OpenConsole;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { response = Text.Get("NavigationService.finish_loading_a_game_first"); return false; }
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled") || co.HasCond("IsLocked") || co.ship != CrewSim.coPlayer?.ship)
        { response = Text.Get("NavigationService.open_an_installed_unlocked_nav_console_aboard"); return false; }
        if (co.GetCOsSafe(true).Any(item => HasId(item, module))) { response = Text.Get("NavigationService.module_already_present"); return false; }
        if (DataHandler.GetCOOverlay(module) == null) { response = Text.Get("NavigationService.native_phobos_auto_nav_package_is_not"); return false; }
        var item = DataHandler.GetCondOwner(module);
        if (item == null) { response = Text.Get("NavigationService.could_not_create_module"); return false; }
        try
        {
            var remainder = co.AddCO(item, bEquip: false, bOverflow: true, bIgnoreLocks: false);
            if (remainder != null) { item.Destroy(); response = Text.Get("NavigationService.console_has_no_compatible_free_capacity"); return false; }
        }
        catch
        {
            if (!item.bDestroyed && item.objCOParent == null && item.ship == null) item.Destroy();
            throw;
        }
        response = Text.Get("NavigationService.module_added_reopen_the_console_and_place");
        return true;
    }
}
