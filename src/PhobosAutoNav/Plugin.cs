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
[BepInDependency(FrameworkInfo.PluginId, "0.7.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.autonav";
    public const string Version = "0.4.0";
    internal static NavigationService Service { get; private set; } = null!;
    internal static ConfigEntry<bool> Enabled = null!, VerboseLogging = null!, FuelCheck = null!,
        AbortOnManualThrust = null!, UseThrusterRotation = null!;
    internal static ConfigEntry<float> DefaultCruiseMS = null!, DefaultArriveSpeedMS = null!,
        DefaultArriveKM = null!, ArrivalSpeedTolerance = null!, MaxFlightSimHours = null!,
        CoastTolerance = null!, RotAccelMax = null!, RotSpeedMax = null!, MaximumStepSeconds = null!;
    private Harmony? harmony;
    private static Action<string>? log;

    private void Awake()
    {
        log = message => Logger.LogInfo(message);
        Enabled = Config.Bind("General", "Enabled", true, Text.Get("Plugin.master_switch_switching_off_aborts_an_active"));
        VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", false, Text.Get("Plugin.detailed_flight_diagnostics"));
        DefaultCruiseMS = Number("Flight", "CruiseMS", 100, 10, 5000, Text.Get("Plugin.cruise_speed_relative_to_target_captured_on"));
        DefaultArriveSpeedMS = Number("Flight", "ArrivalMS", 0, 0, 1000, Text.Get("Plugin.arrival_relative_speed_zero_requests_braking_to"));
        DefaultArriveKM = Number("Flight", "ArrivalKM", ApproachRules.DefaultArrivalKM,
            (float)ApproachRules.MinimumArrivalKM, (float)ApproachRules.MaximumArrivalKM,
            Text.Get("Plugin.centre_to_centre_arrival_distance_also_floored"));
        ArrivalSpeedTolerance = Number("Flight", "ArrivalToleranceMS", 0.5f, 0.05f, 5, Text.Get("Plugin.allowed_speed_error_at_arrival_not_a"));
        MaxFlightSimHours = Number("Limits", "MaximumFlightHours", 48, 0.01f, 168, Text.Get("Plugin.simulation_time_timeout_aborts_and_coasts"));
        MaximumStepSeconds = Number("Limits", "MaximumStepSeconds", 10, 0.1f, 60, Text.Get("Plugin.abort_on_larger_simulation_updates_avoids_commanding"));
        CoastTolerance = Number("Flight", "CoastToleranceMS", 3, 0.1f, 20, Text.Get("Plugin.velocity_error_before_a_cruise_correction"));
        RotAccelMax = Number("Flight", "RotationAcceleration", 0.5f, 0.01f, 0.5f, Text.Get("Plugin.maximum_rotation_command"));
        RotSpeedMax = Number("Flight", "RotationSpeed", 0.6f, 0.01f, 0.6f, Text.Get("Plugin.maximum_requested_spin_rate"));
        FuelCheck = Config.Bind("Flight", "FuelCheck", true, Text.Get("Plugin.check_the_inherited_approximate_delta_v_budget"));
        AbortOnManualThrust = Config.Bind("Flight", "AbortOnManualThrust", true, Text.Get("Plugin.legacy_guidance_check_phobos_always_yields_to"));
        UseThrusterRotation = Config.Bind("Flight", "UseThrusterRotation", true, Text.Get("Plugin.use_rcs_turning_false_retains_upstream_s"));
        Service = new NavigationService(log);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        FrameworkLifecycle.ContentLoading += EquipmentContent.Register;
        Logger.LogInfo(Text.Get("Plugin.adaptation_of_auto_navigate_by_gravy_mrkmg"));
    }

    private ConfigEntry<float> Number(string section, string key, float value, float min, float max, string description) =>
        Config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
    internal static void Verbose(string message) { if (VerboseLogging.Value) log?.Invoke(message); }
    private void OnDestroy() { FrameworkLifecycle.ContentLoading -= EquipmentContent.Register; Service?.Disengage(Text.Get("Plugin.plugin_unloaded")); harmony?.UnpatchSelf(); }
}

[HarmonyPatch(typeof(GUIOrbitDraw), "LoadModules")]
internal static class PanelPatch
{
    private static void Prefix(GUIOrbitDraw __instance) => AutoNavPanel.Ensure(__instance);
}

[HarmonyPatch(typeof(ShipSitu), "TimeAdvance")]
internal static class NavigationTickPatch
{
    private static void Prefix(ShipSitu __instance, double fTime, bool bIgnoreAccel) =>
        Plugin.Service.Tick(__instance, fTime, bIgnoreAccel);
    private static Exception? Finalizer(Exception? __exception)
    {
        if (__exception != null) Plugin.Service.Disengage(Text.Get("Plugin.physics_interrupted"));
        return __exception;
    }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.Maneuver))]
internal static class ExternalControlPatch
{
    private static void Prefix(Ship __instance, float fX, float fY, float fR) => Plugin.Service.ExternalControl(__instance, fX, fY, fR);
}

[HarmonyPatch]
internal static class LifecyclePatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods()
        .Where(method => method.Name == nameof(CrewSim.LoadGame) || method.Name == nameof(CrewSim.NewGame));
    private static void Prefix() => Plugin.Service.Disengage(Text.Get("Plugin.world_change_rearm_explicitly"));
}

[HarmonyPatch]
internal static class OrbitLockPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => new[]
    {
        AccessTools.Method(typeof(ShipSitu), "LockToBO", new[] { typeof(BodyOrbit), typeof(double) }),
        AccessTools.Method(typeof(ShipSitu), "LockToOrbit", new[] { typeof(BodyOrbit), typeof(double) })
    };
    private static bool Prefix(ShipSitu __instance) => !AutoNavCore.Engaged || AutoNavCore.EngagedPlayer?.objSS != __instance;
}

[HarmonyPatch(typeof(Ship), "LockToOrbit")]
internal static class ShipOrbitLockPatch
{
    private static bool Prefix(Ship __instance, ref BodyOrbit? __result)
    {
        if (!AutoNavCore.Engaged || AutoNavCore.EngagedPlayer != __instance) return true;
        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(ConsoleResolver), nameof(ConsoleResolver.ResolveString))]
internal static class ConsolePatch
{
    private static bool Prefix(ref string strInput, ref bool __result)
    {
        string[] words = strInput.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0 || !words[0].Equals("phobosnav", StringComparison.OrdinalIgnoreCase)) return true;
        __result = Plugin.Service.Command(words, out string response);
        strInput += "\n" + response;
        return false;
    }
}
