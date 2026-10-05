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
    private static FrameMeasurements? frames;
    internal static void Initialize(Action<string> log)
    {
        try
        {
            Performance.Session = new PerformanceSession(Path.Combine(Paths.BepInExRootPath, "captures", "PhobosScope"),
                () => CrewSim.objInstance != null && CrewSim.objInstance.FinishedLoading && CrewSim.system != null,
                Metadata, log);
            Performance.RoomAlarmRead = Performance.RegisterOperation("framework.room_alarm.read", "observations");
            Performance.CrewDiscovery = Performance.RegisterOperation("framework.crew.discovery", "discovery");
            Performance.ShipCandidates = Performance.RegisterIncrement("framework.equipment.scan_objects", "discovery", "items");
            Performance.FluidRouteFind = Performance.RegisterOperation("framework.fluid_route.find", "routing");
            Performance.FluidRouteObjects = Performance.RegisterIncrement("framework.fluid_route.scan_objects", "routing", "items");
            Performance.FluidRouteInvalidations = Performance.RegisterIncrement("framework.fluid_route.invalidations", "routing", "items");
            Performance.FluidRouteRecheck = Performance.RegisterOperation("framework.fluid_route.recheck", "routing");
            Performance.CrewTaskFilter = Performance.RegisterOperation("framework.crew.task_filter", "discovery");
            Performance.CrewPathChecks = Performance.RegisterIncrement("framework.crew.path_checks", "discovery", "searches");
            Performance.RcsCollect = Performance.RegisterOperation("framework.rcs.collect", "navigation");
            Performance.StateWrite = Performance.RegisterOperation("framework.state.write", "persistence");
            Performance.StateWritesSkipped = Performance.RegisterIncrement("framework.state.writes_skipped", "persistence", "writes");
            Performance.WaterRefill = Performance.RegisterOperation("framework.water_supply.refill", "processing");
            Performance.SkipMachineStep = Performance.RegisterOperation("framework.skip.machine_step", "processing");
            Performance.WorldSweep = Performance.RegisterOperation("framework.world.sweep", "discovery");
            Performance.WorldSweepObjects = Performance.RegisterIncrement("framework.world.sweep_objects", "discovery", "items");
            Performance.LineContentsMaintain = Performance.RegisterOperation("framework.line_contents.maintain", "processing");
            Performance.StoryCheck = Performance.RegisterOperation("framework.story.check", "story");
            CaptureProbes.Initialize(log);
            frames = new FrameMeasurements();
            MemoryMeasurements.Register();
            Footprints();
            Performance.RegisterContext("game.allocations.available", () => frames.AllocationSupported ? "true" : "false");
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
        foreach (var pair in new[] { ("phobosgekko.ostranauts.autonav", "autonav"), ("phobosgekko.ostranauts.shipbreaker", "shipbreaker"),
            ("phobosgekko.ostranauts.agriculture", "agriculture"), ("phobosgekko.ostranauts.manufacturing", "manufacturing"),
            ("phobosgekko.ostranauts.medical", "medical"), ("phobosgekko.ostranauts.wardeclared", "wardeclared") })
            if (Chainloader.PluginInfos.TryGetValue(pair.Item1, out var plugin))
            {
                values[pair.Item2 + "_version"] = plugin.Metadata.Version.ToString();
                values[pair.Item2 + "_build"] = plugin.Instance.GetType().Module.ModuleVersionId.ToString();
            }
        values["framework_build"] = typeof(NativePerformance).Module.ModuleVersionId.ToString();
        values["allocation_measurement"] = frames?.AllocationSupported == true ? "main_thread_bytes" : "unavailable";
        values["memory_sources"] = MemoryMeasurements.Available;
        values["memory_sources_unavailable"] = MemoryMeasurements.Unavailable;
        return values;
    }
    /// <summary>What Framework keeps alive by key, the collections that would grow if a key were never let go.</summary>
    private static void Footprints()
    {
        Performance.RegisterFootprint("framework.crew.orders", "footprint", () => Crew.CrewWork.OrderCount);
        Performance.RegisterFootprint("framework.crew.jobs", "footprint", () => Crew.CrewWork.Jobs.Count);
        Performance.RegisterFootprint("framework.crew.retry_records", "footprint", () => Crew.CrewWork.RetryRecords);
        Performance.RegisterFootprint("framework.crew_skip.records", "footprint", () => Crew.CrewSkip.Records);
        Performance.RegisterFootprint("framework.fluid_route.ships", "footprint", () => Liquids.FluidRouteCache.ShipCount);
        Performance.RegisterFootprint("framework.buffered_drains.entries", "footprint", () => Liquids.BufferedDrains.EntryCount);
    }
    internal static void Poll()
    {
        Performance.Session?.Poll();
        frames?.Poll();
        CaptureProbes.Poll(Performance.IsRecording);
    }
    internal static void WorldChanging() => Performance.Session?.Stop(StopReason.WorldChange);
    internal static void Shutdown() { Performance.Session?.Stop(StopReason.ApplicationExit); CaptureProbes.Poll(false); }
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
