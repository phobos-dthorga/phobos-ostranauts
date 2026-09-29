using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Timings of the game's own main loop, installed only while a capture records and removed when it stops, so
/// ordinary play pays nothing (29 September 2026 fast-forward pass, stage 8). They show whether long frames sit inside
/// the game's simulation, ship update, appliance updates or crew offer checks, and how often those run. Every Phobos
/// hook on those methods runs inside these timings, so they bound what our hooks can cost there.</summary>
internal static class CaptureProbes
{
    private const string HarmonyId = FrameworkInfo.PluginId + ".capture-probes";
    // Prefixes first and closing postfixes last, so other mods' patches on the same methods are inside the timing.
    // Harmony runs higher priorities first and reads a negative priority as unset, so these stay within First..Last.
    private const int Open = Priority.First, SpanStart = Priority.First, SpanEnd = Priority.Last + 1, Close = Priority.Last;
    private static Harmony? harmony;
    private static bool applied, failed;
    private static long postfixStart;
    private static double millisecondsPerTick = 1000d / Stopwatch.Frequency;
    internal static PerformanceMetric? GameUpdate, SimAdvance, SystemUpdate, PoweredUpdate, OfferCheck, OfferPostfixes, TriggerCalls;
    private static Action<string> log = _ => { };

    internal static void Initialize(Action<string> logger)
    {
        log = logger;
        GameUpdate = Performance.RegisterOperation("game.crewsim.update", "game");
        SimAdvance = Performance.RegisterOperation("game.sim.advance", "game");
        SystemUpdate = Performance.RegisterOperation("game.starsystem.update", "game");
        PoweredUpdate = Performance.RegisterOperation("game.powered.update", "game");
        OfferCheck = Performance.RegisterOperation("game.interaction.offer_check", "game");
        OfferPostfixes = Performance.RegisterIncrement("game.interaction.offer_postfixes", "game", "ms");
        TriggerCalls = Performance.RegisterIncrement("game.condtrigger.calls", "game", "calls");
    }

    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static MethodBase? Method(Type type, string name, params Type[] arguments) => type.GetMethod(name, Declared, null, arguments, null);

    /// <summary>The game methods the probes time, resolved by exact signature. Checked offline by the native suite.</summary>
    internal static IReadOnlyList<(string Name, MethodBase? Target)> Targets() => new (string, MethodBase?)[]
    {
        ("CrewSim.Update", Method(typeof(CrewSim), "Update")),
        ("CrewSim.AdvanceSim", Method(typeof(CrewSim), "AdvanceSim", typeof(float))),
        ("StarSystem.Update", Method(typeof(StarSystem), "Update", typeof(double))),
        ("Powered.Update", Method(typeof(Powered), "Update")),
        ("Interaction.TriggeredInternal", Method(typeof(Interaction), "TriggeredInternal", typeof(CondOwner), typeof(CondOwner), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(List<string>))),
        ("CondTrigger.Triggered", Method(typeof(CondTrigger), "Triggered", typeof(CondOwner), typeof(string), typeof(bool)))
    };

    /// <summary>Framework Update: installs the probes when a capture starts recording and removes them when it stops.
    /// A failure removes whatever was installed and disables the probes for the rest of the session.</summary>
    internal static void Poll(bool recording)
    {
        if (failed || recording == applied) return;
        try
        {
            if (recording) Apply(); else Remove();
        }
        catch (Exception ex)
        {
            failed = true;
            try { harmony?.UnpatchSelf(); } catch { }
            applied = false;
            log(Text.Get("Performance.probes_failed", ex.GetType().Name));
        }
    }

    private static void Apply()
    {
        harmony ??= new Harmony(HarmonyId);
        var targets = Targets();
        foreach (var (name, target) in targets) if (target == null) throw new MissingMethodException(name);
        Timed(targets[0].Target!, nameof(GameUpdatePrefix));
        Timed(targets[1].Target!, nameof(SimAdvancePrefix));
        Timed(targets[2].Target!, nameof(SystemUpdatePrefix));
        Timed(targets[3].Target!, nameof(PoweredUpdatePrefix));
        Timed(targets[4].Target!, nameof(OfferCheckPrefix));
        harmony.Patch(targets[4].Target!, postfix: Hook(nameof(PostfixSpanStart), SpanStart));
        harmony.Patch(targets[4].Target!, postfix: Hook(nameof(PostfixSpanEnd), SpanEnd));
        harmony.Patch(targets[5].Target!, prefix: Hook(nameof(TriggerPrefix), Open));
        applied = true;
    }

    private static void Remove() { harmony?.UnpatchSelf(); applied = false; }

    private static HarmonyMethod Hook(string name, int priority) =>
        new(typeof(CaptureProbes).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)) { priority = priority };
    private static void Timed(MethodBase target, string prefix) =>
        harmony!.Patch(target, prefix: Hook(prefix, Open), postfix: Hook(nameof(Stop), Close));

    private static void GameUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(GameUpdate);
    private static void SimAdvancePrefix(out PerformanceScope __state) => __state = Performance.Measure(SimAdvance);
    private static void SystemUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(SystemUpdate);
    private static void PoweredUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(PoweredUpdate);
    private static void OfferCheckPrefix(out PerformanceScope __state) => __state = Performance.Measure(OfferCheck);
    private static void Stop(PerformanceScope __state) => __state.Dispose();
    // Everything between these two runs every postfix on the offer check: Phobos mods' and any other mod's.
    private static void PostfixSpanStart() => postfixStart = Stopwatch.GetTimestamp();
    private static void PostfixSpanEnd() => Performance.Increment(OfferPostfixes, (Stopwatch.GetTimestamp() - postfixStart) * millisecondsPerTick);
    private static void TriggerPrefix() => Performance.Increment(TriggerCalls);
}
