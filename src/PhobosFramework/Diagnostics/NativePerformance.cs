using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Phobos.Scope.Recording;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Diagnostics;

internal static class NativePerformance
{
    internal static void Initialize(Action<string> log)
    {
        try
        {
            Performance.Session = new PerformanceSession(Path.Combine(Paths.BepInExRootPath, "captures", "PhobosScope"),
                () => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && CrewSim.system != null,
                Metadata, log);
            Performance.RoomAlarmRead = Performance.RegisterOperation("framework.room_alarm.read", "observations");
            Performance.RegisterContext("game.speed_multiplier", () => Time.timeScale.ToString("R", CultureInfo.InvariantCulture));
            Performance.RegisterContext("game.paused", () => CrewSim.Paused ? "true" : "false");
            Performance.RegisterContext("game.navigation_console_visible", () => GUIOrbitDraw.IsOpen() ? "true" : "false");
        }
        catch (Exception ex)
        {
            Performance.Session = null;
            try { log(Text.Get("Performance.fault", ex.GetType().Name)); } catch { }
        }
    }
    private static IReadOnlyDictionary<string, string> Metadata()
    {
        var values = new Dictionary<string, string> {
            ["game"] = "Ostranauts", ["game_version"] = string.IsNullOrEmpty(Application.version) ? "unknown" : Application.version,
            ["framework_version"] = FrameworkInfo.Version,
            ["bepinex_version"] = typeof(BaseUnityPlugin).Assembly.GetName().Version.ToString()
        };
        foreach (var pair in new[] { ("phobosgekko.ostranauts.autonav", "autonav_version"), ("phobosgekko.ostranauts.shipbreaker", "shipbreaker_version") })
            if (Chainloader.PluginInfos.TryGetValue(pair.Item1, out var plugin)) values[pair.Item2] = plugin.Metadata.Version.ToString();
        return values;
    }
    internal static void Poll() => Performance.Session?.Poll();
    internal static void WorldChanging() => Performance.Session?.Stop(StopReason.WorldChange);
    internal static void Shutdown() => Performance.Session?.Stop(StopReason.ApplicationExit);
    internal static bool Command(string[] words, out string response)
    {
        if (Performance.Session != null) return Performance.Session.Command(words, out response);
        response = Text.Get("Performance.unavailable"); return false;
    }
}

[HarmonyPatch]
internal static class PerformanceWorldPatch
{
    private static IEnumerable<MethodBase> TargetMethods() => typeof(CrewSim).GetMethods()
        .Where(m => m.Name == nameof(CrewSim.LoadGame) || m.Name == nameof(CrewSim.NewGame));
    [HarmonyPriority(Priority.First)]
    private static void Prefix() => NativePerformance.WorldChanging();
}
