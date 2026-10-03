using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Phobos.Ostranauts.Framework;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

[BepInPlugin(Id, "Phobos Auto Nav", Version)]
[BepInProcess("Ostranauts.exe")]
[BepInDependency(FrameworkInfo.PluginId, "0.74.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.autonav";
    public const string Version = "0.32.0";
    internal static NavigationService Service { get; private set; } = null!;
    internal static ConfigEntry<bool> Enabled = null!, VerboseLogging = null!, FuelCheck = null!,
        AbortOnManualThrust = null!, UseThrusterRotation = null!, ResumeAfterLoad = null!, PreferTorch = null!, SalvageEnabled = null!;
    internal static ConfigEntry<SensorAutoEngage> AutoEngageSensors = null!;
    internal static ConfigEntry<float> DefaultCruiseMS = null!, DefaultArriveSpeedMS = null!,
        DefaultArriveKM = null!, ArrivalSpeedTolerance = null!, MaxFlightSimHours = null!,
        CoastTolerance = null!, CoastSpeedTolerancePercent = null!, CoastEnterFraction = null!,
        BurnHeadingToleranceDegrees = null!, RotAccelMax = null!, RotSpeedMax = null!, MaximumStepSeconds = null!,
        TorchMaximumG = null!, TorchMinimumCorrectionMS = null!, SalvageChance = null!;
    private Harmony? harmony;
    private static Action<string>? log;

    private void Awake()
    {
        log = message => Logger.LogInfo(message);
        Enabled = Config.Bind("General", "Enabled", true, Text.Get("Plugin.master_switch_switching_off_aborts_an_active"));
        VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", false, Text.Get("Plugin.detailed_flight_diagnostics"));
        DefaultCruiseMS = Number("Flight", "CruiseMS", 100, FlightPreferences.MinimumCruiseMS, FlightPreferences.MaximumCruiseMS, Text.Get("Plugin.cruise_speed_relative_to_target_captured_on"));
        DefaultArriveSpeedMS = Number("Flight", "ArrivalMS", 0, 0, FlightPreferences.MaximumArrivalMS, Text.Get("Plugin.arrival_relative_speed_zero_requests_braking_to"));
        DefaultArriveKM = Number("Flight", "ArrivalKM", ApproachRules.DefaultArrivalKM,
            (float)ApproachRules.MinimumArrivalKM, (float)ApproachRules.MaximumArrivalKM,
            Text.Get("Plugin.centre_to_centre_arrival_distance_also_floored"));
        ArrivalSpeedTolerance = Number("Flight", "ArrivalToleranceMS", 0.5f, 0.05f, 5, Text.Get("Plugin.allowed_speed_error_at_arrival_not_a"));
        MaxFlightSimHours = Number("Limits", "MaximumFlightHours", 48, 0.01f, 168, Text.Get("Plugin.simulation_time_timeout_aborts_and_coasts"));
        MaximumStepSeconds = Number("Limits", "MaximumStepSeconds", 10, 0.1f, 60, Text.Get("Plugin.abort_on_larger_simulation_updates_avoids_commanding"));
        CoastTolerance = Number("Flight", "CoastToleranceMS", 3, 0.1f, 20, Text.Get("Plugin.velocity_error_before_a_cruise_correction"));
        CoastSpeedTolerancePercent = Number("Flight", "CoastSpeedTolerancePercent", CoastRules.DefaultSpeedTolerancePercent,
            0, 25, Text.Get("Plugin.coast_speed_tolerance_percent"));
        CoastEnterFraction = Number("Flight", "CoastEnterFraction", CoastRules.DefaultEnterFraction,
            0.2f, 0.9f, Text.Get("Plugin.coast_enter_fraction"));
        BurnHeadingToleranceDegrees = Number("Flight", "BurnHeadingToleranceDegrees", CoastRules.DefaultBurnHeadingToleranceDegrees,
            0.1f, 10, Text.Get("Plugin.burn_heading_tolerance_degrees"));
        RotAccelMax = Number("Flight", "RotationAcceleration", 0.5f, 0.01f, 0.5f, Text.Get("Plugin.maximum_rotation_command"));
        RotSpeedMax = Number("Flight", "RotationSpeed", 0.6f, 0.01f, 0.6f, Text.Get("Plugin.maximum_requested_spin_rate"));
        FuelCheck = Config.Bind("Flight", "FuelCheck", true, Text.Get("Plugin.check_the_inherited_approximate_delta_v_budget"));
        AbortOnManualThrust = Config.Bind("Flight", "AbortOnManualThrust", true, Text.Get("Plugin.legacy_guidance_check_phobos_always_yields_to"));
        UseThrusterRotation = Config.Bind("Flight", "UseThrusterRotation", true, Text.Get("Plugin.use_rcs_turning_false_retains_upstream_s"));
        PreferTorch = Config.Bind("Torch", "PreferTorch", true, Text.Get("Torch.setting_prefer"));
        TorchMaximumG = Number("Torch", "MaximumAccelerationG", 1, 0.05f, 2, Text.Get("Torch.setting_acceleration"));
        TorchMinimumCorrectionMS = Number("Torch", "MinimumCorrectionMS", 5, 0.5f, 100, Text.Get("Torch.setting_correction"));
        PerformanceMetrics.Initialize();
        Service = new NavigationService(log);
        Phobos.Ostranauts.Framework.Crew.CrewWork.Register(new NavigationCrewProvider());
        Phobos.Ostranauts.Framework.Crew.CrewWork.SkipStarting += Service.SuspendForSkip;
        SalvageEnabled = Config.Bind("Salvage", "Enabled", true, Text.Get("Salvage.enabled"));
        SalvageChance = Number("Salvage", "NavModuleChance", AutoNavEconomy.SalvageChance, 0, 1, Text.Get("Salvage.chance"));
        ResumeAfterLoad = Config.Bind("Persistence", "ResumeAfterLoad", true, Text.Get("Persistence.resume_setting"));
        AutoEngageSensors = Config.Bind("Sensors", "AutoEngage", SensorAutoEngage.All, Text.Get("SensorAssist.setting"));
        TorchDriveController.SensorSettling = Service.SensorsSettlingForTorch;
        TorchDriveController.Notify = (ship, key, text) =>
            Phobos.Ostranauts.Framework.Notices.PlayerNotices.Post(ship, key, Phobos.Ostranauts.Framework.Notices.NoticeLevel.Caution, text, text);
        Phobos.Ostranauts.Framework.Sensors.SensorLeases.SwitchedOffByOthers += Service.SensorSwitchedOff;
        // Flight records settle on a real-time cadence; every native save first receives the latest progress.
        Phobos.Ostranauts.Framework.Persistence.SaveBoundary.BeforeShipSave += Service.FlushProgress;
        CrewSim.OnGameFinishedLoading.AddListener(Service.WorldLoaded);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += EquipmentContent.Register;
        Logger.LogInfo(Text.Get("Plugin.adaptation_of_auto_navigate_by_gravy_mrkmg"));
    }

    private ConfigEntry<float> Number(string section, string key, float value, float min, float max, string description) =>
        Config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
    internal static CoastSettings ReadCoastSettings() => new CoastSettings(CoastTolerance.Value,
        CoastSpeedTolerancePercent.Value, CoastEnterFraction.Value, BurnHeadingToleranceDegrees.Value);
    internal static void Verbose(string message) { if (VerboseLogging.Value) log?.Invoke(message); }
    private void Update() { Service?.RestoreFireOwnership(); Service?.UpdatePersistence(); }
    private void OnDestroy()
    {
        Phobos.Ostranauts.Framework.Crew.CrewWork.SkipStarting -= Service.SuspendForSkip;
        Phobos.Ostranauts.Framework.Sensors.SensorLeases.SwitchedOffByOthers -= Service.SensorSwitchedOff;
        Phobos.Ostranauts.Framework.Persistence.SaveBoundary.BeforeShipSave -= Service.FlushProgress;
        TorchDriveController.SensorSettling = null;
        TorchDriveController.Notify = null;
        FrameworkLifecycle.ContentLoading -= EquipmentContent.Register;
        if (Service != null) CrewSim.OnGameFinishedLoading.RemoveListener(Service.WorldLoaded);
        Service?.Disengage(Text.Get("Plugin.plugin_unloaded")); harmony?.UnpatchSelf();
    }
}

[HarmonyPatch(typeof(StarSystem), nameof(StarSystem.Update))]
internal static class DockingTickPatch
{
    private static void Prefix(StarSystem __instance, double fTimeDelta)
    {
        Plugin.Service.BeforeNavigationPhysics(__instance, fTimeDelta);
    }
    private static void Postfix(StarSystem __instance, double fTimeDelta)
    {
        Plugin.Service.AfterNavigationPhysics(__instance, fTimeDelta);
    }
    private static Exception? Finalizer(Exception? __exception)
    {
        // The game's own update can throw (for example while it spawns an NPC ship); Auto Nav rides that out.
        if (__exception != null) Plugin.Service.PhysicsInterrupted(__exception);
        return __exception;
    }
}

[HarmonyPatch(typeof(GUIOrbitDraw), "LoadModules")]
internal static class PanelPatch
{
    private static void Prefix(GUIOrbitDraw __instance) => AutoNavPanel.Ensure(__instance);
    private static void Postfix(CondOwner coNav) => Plugin.Service.HubLoaded(coNav);
}

// Switching crew with the nav screen open keeps its module panels and swaps the console under them;
// rebind the hub at once, including while it is hidden for a station without a Phobos module.
[HarmonyPatch(typeof(GUIOrbitDraw), nameof(GUIOrbitDraw.CrewSwitch))]
internal static class CrewSwitchPatch
{
    private static void Postfix(GUIOrbitDraw __instance)
    {
        foreach (var panel in __instance.GetComponentsInChildren<AutoNavPanel>(true)) panel.FollowConsole();
    }
}

// Observe native aim normally; remove only leased offensive weapons from its queue.
[HarmonyPatch(typeof(Ostranauts.ShipGUIs.NavStation.NavModWeaponsControl), "TargetLockHandler")]
internal static class PursuitWeaponAimPatch
{
    private static void Postfix(List<CondOwner> ____weaponsToFire, CondOwner ___COSelf)
    {
        Plugin.Service.RestoreFireOwnership();
        var ship = ___COSelf?.ship;
        if (ship != null) Plugin.Service.Fire.FilterNative(ship.WeaponsSystem, ____weaponsToFire, ship.shipCombatTarget?.objSS);
    }
}
[HarmonyPatch(typeof(Ostranauts.ShipGUIs.NavStation.NavModWeaponsControl), "KeyHandler")]
internal static class PursuitWeaponQueuePatch
{
    private static void Prefix(List<CondOwner> ____weaponsToFire, CondOwner ___COSelf)
    {
        Plugin.Service.RestoreFireOwnership();
        var ship = ___COSelf?.ship;
        if (ship != null) Plugin.Service.Fire.FilterNative(ship.WeaponsSystem, ____weaponsToFire, ship.shipCombatTarget?.objSS);
    }
}
[HarmonyPatch(typeof(Ostranauts.Ships.WeaponsSystem), nameof(Ostranauts.Ships.WeaponsSystem.ShootAuto))]
internal static class PursuitWeaponFirePatch
{
    private static bool Prefix(Ostranauts.Ships.WeaponsSystem __instance, ref List<CondOwner> weaponsToFire, ShipSitu target, ref bool __result)
    {
        Plugin.Service.RestoreFireOwnership();
        // Without a lease nothing is filtered: the game's own list goes through untouched and uncopied.
        if (weaponsToFire == null || Plugin.Service.Fire.Idle) return true;
        weaponsToFire = new List<CondOwner>(weaponsToFire);
        Plugin.Service.Fire.FilterNative(__instance, weaponsToFire, target);
        if (weaponsToFire.Count > 0) return true;
        __result = false; return false;
    }
}

// LoadModules applies stored/default anchors before asking whether each panel
// fits. Normalize only our rectangle at that shared boundary, including native
// drag validation and saving; do not patch the game's global placement rules.
[HarmonyPatch(typeof(Ostranauts.ShipGUIs.NavStation.NavModBase), "GetRoundedAnchors")]
internal static class PanelBoundsPatch
{
    private static void Prefix(Ostranauts.ShipGUIs.NavStation.NavModBase __instance)
    {
        if (__instance is AutoNavPanel panel) panel.NormalizePlacementBounds();
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.Maneuver))]
internal static class ExternalControlPatch
{
    private static void Prefix(Ship __instance, ref float fX, ref float fY, ref float fR, float __4)
    { Plugin.Service.ExternalControl(__instance,fX,fY,fR); Plugin.Service.FilterAvoidanceCommand(__instance,ref fX,ref fY,ref fR,__4); }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.SetReactorGPMValue))]
internal static class ReactorControlPatch
{
    private static void Prefix(Ship __instance, string strName, string strValue) =>
        Plugin.Service.ExternalReactorControl(__instance, strName, strValue);
}

[HarmonyPatch(typeof(Ship), nameof(Ship.SetThrust))]
internal static class TorchThrustPatch
{
    private static void Prefix(Ship __instance, ref double fAmount)
    {
        if(fAmount>0&&!Plugin.Service.Torch.Owns(__instance)) IndustrialNavigation.Yield(__instance);
        Plugin.Service.Torch.FilterThrust(__instance, ref fAmount);
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.GetJsonItem))]
internal static class SavedReactorPatch
{
    private static void Postfix(CondOwner co, JsonItem __result) => Plugin.Service.Torch.PrepareSavedControls(co, __result);
}

// Native ShipSitu saves include the last torch/RCS acceleration. That is an actuator
// command, not flight intent: omit it in the fresh DTO for our active ship only.
// Preserve velocity, angular momentum, gravity and other controllers' saves.
[HarmonyPatch(typeof(ShipSitu), nameof(ShipSitu.GetJSON))]
internal static class SavedThrustPatch
{
    private static void Postfix(ShipSitu __instance, JsonShipSitu __result) => NavigationService.PrepareSavedPhysics(__instance, __result);
}

[HarmonyPatch]
internal static class LifecyclePatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods()
        .Where(method => method.Name == nameof(CrewSim.LoadGame) || method.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Plugin.Service.WorldChanging();
}

// A native orbit lock on the engaged ship (the pilot's orbital mode, or the game's own anchoring) is a
// takeover: Auto Nav steps aside and the game's lock proceeds, instead of being silently swallowed.
[HarmonyPatch]
internal static class OrbitLockPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(ShipSitu), "LockToBO", new[] { typeof(BodyOrbit), typeof(double) }),
        AccessTools.Method(typeof(ShipSitu), "LockToOrbit", new[] { typeof(BodyOrbit), typeof(double) })
    };
    private static void Prefix(ShipSitu __instance)
    {
        if (AutoNavCore.Engaged && AutoNavCore.EngagedPlayer?.objSS == __instance) Plugin.Service.Disengage(Text.Get("NavigationService.stopped_by_pilot_coasting"));
    }
}

[HarmonyPatch(typeof(Ship), "LockToOrbit")]
internal static class ShipOrbitLockPatch
{
    private static void Prefix(Ship __instance)
    {
        if (AutoNavCore.Engaged && AutoNavCore.EngagedPlayer == __instance) Plugin.Service.Disengage(Text.Get("NavigationService.stopped_by_pilot_coasting"));
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        string[] words = strInput.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosnav", StringComparison.OrdinalIgnoreCase)) return true;
        string response;
        // The sensor suite is a ship-wide native read/toggle, separate from flight commands.
        if (words.Length >= 2 && words[1].Equals("sensors", StringComparison.OrdinalIgnoreCase)) __result = NativeSensorSuite.Command(words, out response);
        else __result = Plugin.Service.Command(words, out response);
        strInput += "\n" + response;
        return false;
    }
}
