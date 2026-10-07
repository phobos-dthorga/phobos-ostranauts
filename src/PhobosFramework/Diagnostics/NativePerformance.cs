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
            Performance.SkipStep = Performance.RegisterOperation("framework.skip.step", "processing");
            Performance.SkipCrew = Performance.RegisterOperation("framework.skip.crew", "discovery");
            Performance.WorldSweep = Performance.RegisterOperation("framework.world.sweep", "discovery");
            Performance.WorldSweepObjects = Performance.RegisterIncrement("framework.world.sweep_objects", "discovery", "items");
            Performance.LineContentsMaintain = Performance.RegisterOperation("framework.line_contents.maintain", "processing");
            Performance.StoryCheck = Performance.RegisterOperation("framework.story.check", "story");
            Performance.StoryChatter = Performance.RegisterOperation("framework.story.chatter", "story");
            Performance.UpkeepPlan = Performance.RegisterOperation("framework.upkeep.plan", "discovery");
            CaptureProbes.Initialize(log);
            frames = new FrameMeasurements();
            MemoryMeasurements.Register();
            Footprints();
            Performance.RegisterContext("game.allocations.available", () => frames.AllocationSupported ? "true" : "false");
            Performance.RegisterContext("game.speed_multiplier", () => Time.timeScale.ToString("R", CultureInfo.InvariantCulture));
            Performance.RegisterContext("game.paused", () => CrewSim.Paused ? "true" : "false");
            Performance.RegisterContext("game.navigation_console_visible", () => GUIOrbitDraw.IsOpen() ? "true" : "false");
            // Framework 0.133.0: whether the game's save job is still writing, so a capture shows when each save ran.
            if (SaveWatch() is Func<bool> saving) Performance.RegisterContext("game.saving", () => saving() ? "true" : "false");
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
        values["framework_build"] = typeof(NativePerformance).Module.ModuleVersionId.ToString();
        values["allocation_measurement"] = frames?.AllocationSupported == true ? "main_thread_bytes" : "unavailable";
        values["memory_sources"] = MemoryMeasurements.Available;
        values["memory_sources_unavailable"] = MemoryMeasurements.Unavailable;
        // Framework 0.133.0: whether the game's collector works in slices, which decides how a collection shows in the
        // frame times, and what an empty timed span reads, which the report takes off the sampled trigger-check time.
        values["gc_incremental"] = GcIncremental();
        if (CaptureProbes.EmptySpanNanoseconds is double empty)
            values["timer_overhead_ns"] = empty.ToString("0.0", CultureInfo.InvariantCulture);
        // Every loaded Phobos mod names itself (Framework 0.133.0); a fixed list had missed Banking and Exchange.
        var mods = new List<(string, string, string)>();
        foreach (var plugin in Chainloader.PluginInfos.Values)
            if (CaptureMetadata.Stem(plugin?.Metadata?.GUID, FrameworkInfo.PluginId) is string stem && plugin!.Instance != null)
                mods.Add((stem, plugin.Metadata.Version.ToString(), plugin.Instance.GetType().Module.ModuleVersionId.ToString()));
        return CaptureMetadata.Plan(values, mods, reserved: 2);
    }
    /// <summary>Reads whether the game's save job is still running: its saving manager's private job, which says when it
    /// is done (a save writes its archive on a thread while play goes on). Null when the game's layout differs.</summary>
    internal static Func<bool>? SaveWatch()
    {
        try
        {
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            var instance = typeof(global::Ostranauts.Core.MonoSingleton<global::Ostranauts.Core.LoadManager>).GetField("_instance", any);
            var job = typeof(global::Ostranauts.Core.LoadManager).GetField("_saveJob", any);
            if (instance == null || !instance.IsStatic || job == null || job.FieldType != typeof(global::Ostranauts.Core.Models.SavingJob)) return null;
            return () => instance.GetValue(null) is global::Ostranauts.Core.LoadManager manager &&
                job.GetValue(manager) is global::Ostranauts.Core.Models.SavingJob running && !running.IsDone;
        }
        catch { return null; }
    }
    private static string GcIncremental()
    {
        try { return UnityEngine.Scripting.GarbageCollector.isIncremental ? "true" : "false"; }
        catch { return "unknown"; }
    }
    /// <summary>What Framework keeps alive by key, the collections that would grow if a key were never let go.</summary>
    private static void Footprints()
    {
        Performance.RegisterFootprint("framework.crew.orders", "footprint", () => Crew.CrewWork.OrderCount);
        Performance.RegisterFootprint("framework.crew.jobs", "footprint", () => Crew.CrewWork.Jobs.Count);
        Performance.RegisterFootprint("framework.crew.retry_records", "footprint", () => Crew.CrewWork.RetryRecords);
        Performance.RegisterFootprint("framework.crew_skip.records", "footprint", () => Crew.CrewSkip.Records);
        Performance.RegisterFootprint("framework.upkeep.states", "footprint", () => Crew.Upkeep.StateCount);
        // Framework 0.132.0 (L101): the last variant picked per news item, advert and small-talk line, cleared on reload.
        Performance.RegisterFootprint("framework.story.variant_picks", "footprint", () => Story.VariantPicks.Count);
        Performance.RegisterFootprint("framework.text.variant_picks", "footprint", () => Localization.Translations.VariantPickCount);
        Performance.RegisterFootprint("framework.fluid_route.ships", "footprint", () => Liquids.FluidRouteCache.ShipCount);
        Performance.RegisterFootprint("framework.buffered_drains.entries", "footprint", () => Liquids.BufferedDrains.EntryCount);
        // Framework 0.133.0 (L103): the line segments whose run the two-second top-up remembers.
        Performance.RegisterFootprint("framework.line_contents.segments", "footprint", () => Liquids.LineContents.RememberedSegments);
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
