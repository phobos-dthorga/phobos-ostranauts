using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace PhobosAutoNav;

[BepInPlugin(Id, "Phobos Auto Nav (prototype)", Version)]
[BepInProcess("Ostranauts.exe")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "phobosgekko.ostranauts.autonav";
    public const string Version = "0.1.1";
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
        Enabled = Config.Bind("General", "Enabled", true, "Master switch; switching off aborts an active flight.");
        VerboseLogging = Config.Bind("Diagnostics", "VerboseLogging", false, "Detailed flight diagnostics.");
        DefaultCruiseMS = Number("Flight", "CruiseMS", 100, 10, 5000, "Cruise speed relative to target. Captured on engagement.");
        DefaultArriveSpeedMS = Number("Flight", "ArrivalMS", 0, 0, 1000, "Arrival relative speed. Zero requests braking to a stop. Captured on engagement.");
        DefaultArriveKM = Number("Flight", "ArrivalKM", 5, 1, 100, "Centre-to-centre arrival distance, also floored by hull size. Captured on engagement.");
        ArrivalSpeedTolerance = Number("Flight", "ArrivalToleranceMS", 0.5f, 0.05f, 5, "Allowed speed error at arrival; not a station-keeping guarantee.");
        MaxFlightSimHours = Number("Limits", "MaximumFlightHours", 48, 0.01f, 168, "Simulation-time timeout; aborts and coasts.");
        MaximumStepSeconds = Number("Limits", "MaximumStepSeconds", 10, 0.1f, 60, "Abort on larger simulation updates; avoids commanding a long unobserved burn.");
        CoastTolerance = Number("Flight", "CoastToleranceMS", 3, 0.1f, 20, "Velocity error before a cruise correction.");
        RotAccelMax = Number("Flight", "RotationAcceleration", 0.5f, 0.01f, 0.5f, "Maximum rotation command.");
        RotSpeedMax = Number("Flight", "RotationSpeed", 0.6f, 0.01f, 0.6f, "Maximum requested spin rate.");
        FuelCheck = Config.Bind("Flight", "FuelCheck", true, "Check the inherited approximate delta-v budget before departure; not a fuel guarantee.");
        AbortOnManualThrust = Config.Bind("Flight", "AbortOnManualThrust", true, "Legacy guidance check. Phobos always yields to external maneuver commands regardless of this value.");
        UseThrusterRotation = Config.Bind("Flight", "UseThrusterRotation", true, "Use RCS turning. False retains upstream's instantaneous heading change for comparisons.");
        Service = new NavigationService(log);
        harmony = new Harmony(Id);
        harmony.PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo("Standalone adaptation of Auto Navigate by Gravy/mrkmg. F3: phobosnav help. Test saves only; no upstream mod dependency.");
    }

    private ConfigEntry<float> Number(string section, string key, float value, float min, float max, string description) =>
        Config.Bind(section, key, value, new ConfigDescription(description, new AcceptableValueRange<float>(min, max)));
    internal static void Verbose(string message) { if (VerboseLogging.Value) log?.Invoke(message); }
    private void OnDestroy() { Service?.Disengage("Plugin unloaded"); harmony?.UnpatchSelf(); }
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
        if (__exception != null) Plugin.Service.Disengage("Physics interrupted");
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
    private static void Prefix() => Plugin.Service.Disengage("World change; rearm explicitly");
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
