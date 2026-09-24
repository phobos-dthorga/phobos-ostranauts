using System;
using System.Globalization;
using System.Linq;
using BepInEx.Bootstrap;
using HarmonyLib;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal sealed class NavigationService
{
    internal const string ModuleId = "PhobosNavModAutoNav";
    internal const string DamagedId = ModuleId + "Dmg";
    private readonly Action<string> log;
    private CondOwner? console;
    private bool issuing;
    private string status = Text.Get("NavigationService.idle");
    private static CondOwner? OpenConsole => GUIOrbitDraw.IsOpen() ? GUIOrbitDraw.Instance.COSelfBase() : null;
    internal NavigationService(Action<string> log) { this.log = log; }

    internal float Throttle
    {
        get
        {
            if (console != null && console.mapGUIPropMaps.TryGetValue("Panel A", out var props)
                && props.TryGetValue("slidThrottle", out string value)
                && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float throttle)
                && ArrivalBrake.Finite(throttle)) return Math.Max(0, Math.Min(1, throttle));
            return 0; // Missing throttle state must not silently become a burn.
        }
    }

    private static bool HasId(CondOwner co, string id) => !co.bDestroyed && (co.strName == id || co.strCODef == id);
    private static bool PropOn(CondOwner co, string key) => co.mapGUIPropMaps.TryGetValue("Panel A", out var props)
        && props.TryGetValue(key, out var value) && string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    private static string? HardwareProblem(CondOwner? co)
    {
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled")) return Text.Get("NavigationService.installed_nav_console_required");
        if (co.HasCond("IsOff") || !co.HasCond("IsPowered") || co.HasCond("IsDamaged")) return Text.Get("NavigationService.console_is_off_unpowered_or_damaged");
        if (!co.GetCOsSafe(true).Any(item => HasId(item, ModuleId) && !item.HasCond("IsDamaged"))) return Text.Get("NavigationService.working_phobos_auto_nav_module_required");
        if (co.ship == null || co.ship.bDestroyed || CrewSim.coPlayer == null || CrewSim.coPlayer.ship != co.ship) return Text.Get("NavigationService.player_must_be_aboard_the_controlled_ship");
        if (co.ship.IsDocked()) return Text.Get("NavigationService.undock_before_engagement");
        if (co.ship.bCheckPower) return Text.Get("NavigationService.power_network_is_updating");
        if (co.ship.IsUsingTorchDrive || co.ship.shipStationKeepingTarget != null || (co.ship.aWPs != null && co.ship.aWPs.Count > 0)
            || PropOn(co, "chkStationKeeping") || PropOn(co, "chkHoldThrust") || PropOn(co, "chkEngage")
            || AIShipManager.GetAIShipByRegID(co.ship.strRegID) != null) return Text.Get("NavigationService.disengage_other_flight_automation_first");
        if (CrewSim.system == null || CrewSim.system.IsInAtmo(co.ship)) return Text.Get("NavigationService.free_space_flight_only");
        if (co.ship.RCSCount <= 0 || co.ship.GetRCSRemain() <= 0) return Text.Get("NavigationService.working_rcs_and_fuel_required");
        if (co.ship.objSS == null || !ArrivalBrake.Finite(co.ship.RCSAccelMax) || co.ship.RCSAccelMax <= 0) return Text.Get("NavigationService.rcs_acceleration_unavailable");
        return null;
    }

    internal void Engage(CondOwner? co, float? arrivalKM = null)
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
            if (problem != null) { status = problem; return; }
            console = co;
            if (Throttle <= 0) { status = Text.Get("NavigationService.set_the_nav_console_throttle_above_zero"); return; }
            var contact = GUIOrbitDraw.CrossHairTarget;
            if (contact?.Ship == null || contact.Ship == co!.ship || contact.Ship.bDestroyed || contact.Ship.HideFromSystem || contact.Ship.IsStationHidden())
            { status = Text.Get("NavigationService.select_another_ship_or_station_planetary_travel"); return; }
            if (AutoNavCore.AutoDockBusy()) { status = Text.Get("NavigationService.auto_dock_already_controls_flight"); return; }
            Type? approach = AccessTools.TypeByName("PhobosApproachAssist.Plugin");
            object? approachService = approach == null ? null : AccessTools.Property(approach, "Service")?.GetValue(null);
            if (approachService != null && (bool)(AccessTools.Property(approachService.GetType(), "Active")?.GetValue(approachService) ?? false))
            { status = Text.Get("NavigationService.stop_approach_assist_s_test_pulse_first"); return; }
            var target = TargetRef.FromCrossHair();
            if (target == null) { status = Text.Get("NavigationService.target_unavailable"); return; }
            float cruise = Plugin.DefaultCruiseMS.Value, arrival = Plugin.DefaultArriveSpeedMS.Value,
                distance = arrivalKM ?? Plugin.DefaultArriveKM.Value;
            var coastSettings = Plugin.ReadCoastSettings();
            if (!ArrivalBrake.Finite(cruise) || !ArrivalBrake.Finite(arrival) || !ApproachRules.ValidArrival(distance) || !coastSettings.IsValid)
            { status = Text.Get("NavigationService.invalid_flight_settings"); return; }
            if (!target.Resolve(out _, out _, out _, out _)
                || !AutoNavCore.TryReadApproach(co!.ship, target, distance, out _, out _))
            { status = Text.Get("NavigationService.approach_data_unavailable"); return; }
            AutoNavCore.CruiseAU = cruise * AutoNavCore.M_TO_AU;
            AutoNavCore.ArrSpdAU = Math.Min(arrival, cruise) * AutoNavCore.M_TO_AU;
            AutoNavCore.ArriveAU = distance * AutoNavCore.KM_TO_AU;
            if (Plugin.FuelCheck.Value && !AutoNavCore.HasFuelForFlight(co!.ship, target))
            { status = Text.Get("NavigationService.insufficient_estimated_delta_v"); return; }
            issuing = true;
            try { AutoNavCore.BeginFlight(co!.ship, target, coastSettings); } finally { issuing = false; }
            status = Text.Get("NavigationService.flight_engaged");
        }
        catch (Exception ex) { log(ex.ToString()); Disengage(Text.Get("NavigationService.engagement_failed_see_log")); }
        log(status);
    }

    internal void Tick(ShipSitu situ, double dt, bool ignoreAcceleration)
    {
        if (!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != situ || ignoreAcceleration || dt == 0) return;
        try
        {
            string? problem = !Plugin.Enabled.Value ? Text.Get("NavigationService.mod_disabled") : HardwareProblem(console);
            if (problem == null && (!ArrivalBrake.Finite(dt) || dt < 0 || dt > Plugin.MaximumStepSeconds.Value)) problem = Text.Get("NavigationService.simulation_step_too_large_or_invalid_reduce");
            if (problem == null && Throttle <= 0) problem = Text.Get("NavigationService.throttle_zero_or_unavailable");
            if (problem == null && AutoNavCore.AutoDockBusy()) problem = Text.Get("NavigationService.auto_dock_took_control");
            if (problem == null && (!ArrivalBrake.Finite(Plugin.ArrivalSpeedTolerance.Value) || !ArrivalBrake.Finite(Plugin.MaximumStepSeconds.Value))) problem = Text.Get("NavigationService.invalid_safety_settings");
            if (problem != null) { Disengage(problem); return; }
            issuing = true;
            try { AutoNavCore.SteerFlight(AutoNavCore.EngagedPlayer, AutoNavCore.EngagedTarget, dt); }
            finally { issuing = false; }
            status = AutoNavCore.Engaged ? AutoNavCore.PhaseName : DescribeResult(AutoNavCore.LastResult);
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
        // Closing the native console emits a zero command; it must not cancel off-console travel.
        if (!issuing && (x != 0 || y != 0 || rotation != 0) && AutoNavCore.Engaged && AutoNavCore.EngagedPlayer == ship)
            Disengage(Text.Get("NavigationService.external_maneuver_command_pilot_other_controller_has"));
    }

    internal void Disengage(string reason)
    {
        issuing = true;
        try { if (AutoNavCore.Engaged) AutoNavCore.EndFlight(AutoNavCore.EngagedPlayer, reason); }
        catch (Exception ex) { log(Text.Get("NavigationService.stop_failed", ex)); }
        finally { AutoNavCore.ResetStatics(); issuing = false; console = null; status = reason; }
        log(reason);
    }

    internal string ReadPanel(CondOwner co) => status + "\n" +
        (AutoNavCore.Engaged ? Text.Get("NavigationService.target", AutoNavCore.EngagedTarget.DisplayName) : HardwareProblem(co) ?? Text.Get("NavigationService.ready_to_select_target"))
        + "\n" + ApproachSummary(co, detailed: false);

    private static string ApproachSummary(CondOwner? co, bool detailed)
    {
        double requested = AutoNavCore.Engaged ? AutoNavCore.ArriveAU / AutoNavCore.KM_TO_AU : Plugin.DefaultArriveKM.Value;
        var target = AutoNavCore.Engaged ? AutoNavCore.EngagedTarget :
            GUIOrbitDraw.IsOpen() && GUIOrbitDraw.CrossHairTarget?.Ship != null ? TargetRef.FromCrossHair() : null;
        var ship = AutoNavCore.Engaged ? AutoNavCore.EngagedPlayer : co?.ship;
        if (ship != null && target != null && AutoNavCore.TryReadApproach(ship, target, requested, out var plan, out var speed))
            return Text.Get(detailed ? "NavigationService.approach_details" : "NavigationService.approach_range",
                plan.RangeKM, plan.EffectiveArrivalKM, speed, plan.RequestedArrivalKM);
        return Text.Get("NavigationService.arrival_default", requested);
    }

    internal bool Command(string[] words, out string response)
    {
        try
        {
            string verb = words.Length == 1 ? "help" : words[1].ToLowerInvariant();
            if (words.Length > (verb == "fly" || verb == "arrival" ? 3 : 2))
            { response = Text.Get("NavigationService.no_extra_arguments_accepted_use_phobosnav_help"); return false; }
            switch (verb)
            {
                case "help": response = Text.Get("NavigationService.phobosnav_help_status_settings_fly_stop_spawn"); return true;
                case "status": response = Text.Get("NavigationService.phobos_auto_nav_engaged", Plugin.Version, AutoNavCore.Engaged, status, EquipmentContent.Status)
                    + "\n" + ApproachSummary(OpenConsole, detailed: true); return true;
                case "settings":
                    var coast = AutoNavCore.Engaged ? AutoNavCore.FlightCoastSettings : Plugin.ReadCoastSettings();
                    response = Text.Get("NavigationService.cruise_m_s_arrival_m_s_at", Plugin.DefaultCruiseMS.Value, Plugin.DefaultArriveSpeedMS.Value, Plugin.DefaultArriveKM.Value, Plugin.ArrivalSpeedTolerance.Value, Plugin.MaximumStepSeconds.Value, Plugin.Id)
                        + "\n" + Text.Get("NavigationService.coast_settings", coast.MinimumToleranceMS, coast.SpeedTolerancePercent,
                            coast.EnterFraction * 100, coast.BurnHeadingToleranceDegrees, CoastRules.DriftLookaheadSeconds,
                            CoastRules.DriftRadiusFraction * 100,
                            Text.Get(AutoNavCore.Engaged ? "NavigationService.active_flight" : "NavigationService.next_flight"));
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
                case "arrival":
                    if (words.Length != 3 || !ApproachRules.TryParseArrival(words[2], out float newDefault))
                    { response = ArrivalUsage(); return false; }
                    if (AutoNavCore.Engaged)
                    { response = Text.Get("NavigationService.already_engaged_stop_before_changing_the_flight"); return false; }
                    Plugin.DefaultArriveKM.Value = newDefault;
                    Plugin.DefaultArriveKM.ConfigFile.Save();
                    response = Text.Get("NavigationService.arrival_default_saved", newDefault);
                    return true;
                case "stop": Disengage(Text.Get("NavigationService.stopped_by_pilot_coasting")); response = status; return true;
                case "spawn": return Spawn(out response);
                default: response = Text.Get("NavigationService.unknown_command_use_phobosnav_help"); return false;
            }
        }
        catch (Exception ex) { log(ex.ToString()); response = Text.Get("NavigationService.command_failed_see_bepinex_log"); return false; }
    }

    private static string ArrivalUsage() => Text.Get("NavigationService.arrival_usage",
        ApproachRules.MinimumArrivalKM, ApproachRules.MaximumArrivalKM);

    private bool Spawn(out string response)
    {
        var co = OpenConsole;
        if (CrewSim.objInstance == null || !CrewSim.objInstance.FinishedLoading)
        { response = Text.Get("NavigationService.finish_loading_a_game_first"); return false; }
        if (co == null || co.bDestroyed || !co.HasCond("IsInstalled") || co.HasCond("IsLocked") || co.ship != CrewSim.coPlayer?.ship)
        { response = Text.Get("NavigationService.open_an_installed_unlocked_nav_console_aboard"); return false; }
        if (co.GetCOsSafe(true).Any(item => HasId(item, ModuleId))) { response = Text.Get("NavigationService.module_already_present"); return false; }
        if (DataHandler.GetCOOverlay(ModuleId) == null) { response = Text.Get("NavigationService.native_phobos_auto_nav_package_is_not"); return false; }
        var item = DataHandler.GetCondOwner(ModuleId);
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
