using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Timings of the game's own main loop, installed only while a capture records and removed when it stops, so
/// ordinary play pays nothing (29 September 2026 fast-forward pass, stage 8). They show whether long frames sit inside
/// the game's simulation, ship update, appliance updates or crew offer checks, and how often those run. Every Phobos
/// hook on those methods runs inside these timings, so they bound what our hooks can cost there.
///
/// Framework 0.133.0 (L102, owner request after the 8 October 2026 capture): the time every mod's patches take on the
/// interaction effects and the trigger check, which no timing covered, and the main-thread part of the game's save.
/// The trigger check runs about 590,000 times a second, so only one call in <see cref="TriggerStride"/> is timed
/// (owner choice, 8 October 2026); every call is still counted, and <see cref="EmptySpanNanoseconds"/> records what the
/// timing itself adds, so the report can scale and correct the estimate and say that it is one.</summary>
internal static class CaptureProbes
{
    private const string HarmonyId = FrameworkInfo.PluginId + ".capture-probes";
    // Probe prefixes and span starts run before every other patch, closing patches after them, so other mods' patches on
    // the same methods are inside the timing. Harmony runs higher priorities first and ties in registration order, and
    // the probes are registered last, so they start one above First; a negative priority reads as unset, so the closing
    // patches stay at Last and Last + 1. Apply checks the order it got and logs any patch left outside a span.
    private const int Open = Priority.First + 1, SpanStart = Priority.First + 1, SpanEnd = Priority.Last + 1, Close = Priority.Last;
    internal const int TriggerStride = 16;
    private static Harmony? harmony;
    private static bool applied, failed;
    private static readonly HookSpan offers = new(Stopwatch.GetTimestamp), effects = new(Stopwatch.GetTimestamp),
        triggers = new(Stopwatch.GetTimestamp, TriggerStride);
    private static readonly double millisecondsPerTick = 1000d / Stopwatch.Frequency;
    internal static PerformanceMetric? GameUpdate, SimAdvance, SystemUpdate, PoweredUpdate, OfferCheck, OfferPostfixes, TriggerCalls,
        ApplyEffects, EffectHooks, TriggerSampledMs, TriggerSampledCalls, SaveBegin, SaveSerialise;
    /// <summary>What an empty timed span reads, in nanoseconds, measured once at start-up; null before then.</summary>
    internal static double? EmptySpanNanoseconds { get; private set; }
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
        ApplyEffects = Performance.RegisterOperation("game.interaction.apply_effects", "game");
        EffectHooks = Performance.RegisterIncrement("game.interaction.effect_hooks", "game", "ms");
        TriggerSampledMs = Performance.RegisterIncrement("game.condtrigger.postfix_sampled_ms", "game", "ms");
        TriggerSampledCalls = Performance.RegisterIncrement("game.condtrigger.sampled_calls", "game", "calls");
        SaveBegin = Performance.RegisterOperation("game.save.begin", "game");
        SaveSerialise = Performance.RegisterOperation("game.save.serialise", "game");
        EmptySpanNanoseconds = HookSpan.EmptyTicks(Stopwatch.GetTimestamp) * 1e9 / Stopwatch.Frequency;
    }

    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private static MethodBase? Method(Type type, string name, params Type[] arguments) => type.GetMethod(name, Declared, null, arguments, null);

    /// <summary>The game methods the probes time, resolved by exact signature. Checked offline by the native suite.
    /// <see cref="Apply"/> reads them by position: add new ones at the end.</summary>
    internal static IReadOnlyList<(string Name, MethodBase? Target)> Targets() => new (string, MethodBase?)[]
    {
        ("CrewSim.Update", Method(typeof(CrewSim), "Update")),
        ("CrewSim.AdvanceSim", Method(typeof(CrewSim), "AdvanceSim", typeof(float))),
        ("StarSystem.Update", Method(typeof(StarSystem), "Update", typeof(double))),
        ("Powered.Update", Method(typeof(Powered), "Update")),
        ("Interaction.TriggeredInternal", Method(typeof(Interaction), "TriggeredInternal", typeof(CondOwner), typeof(CondOwner), typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(List<string>))),
        ("CondTrigger.Triggered", Method(typeof(CondTrigger), "Triggered", typeof(CondOwner), typeof(string), typeof(bool))),
        ("Interaction.ApplyEffects", Method(typeof(Interaction), "ApplyEffects", typeof(List<string>), typeof(bool))),
        ("LoadManager.SaveGame", Method(typeof(global::Ostranauts.Core.LoadManager), "SaveGame", typeof(string), typeof(int), typeof(bool))),
        ("LoadManager.SaveGameData", Method(typeof(global::Ostranauts.Core.LoadManager), "SaveGameData", typeof(string)))
    };

    /// <summary>Framework Update: installs the probes when a capture starts recording and removes them when it stops.
    /// A failure removes whatever was installed and disables the probes for the rest of the session.</summary>
    internal static void Poll(bool recording)
    {
        // Per-call counts are summed in memory and recorded once per frame: a record per call (400,000 trigger checks a
        // second) filled the capture record limit within a second and pushed out the frame samples.
        if (applied) Flush();
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
        harmony.Patch(targets[4].Target!, postfix: Hook(nameof(OfferSpanStart), SpanStart));
        harmony.Patch(targets[4].Target!, postfix: Hook(nameof(OfferSpanEnd), SpanEnd));
        harmony.Patch(targets[5].Target!, postfix: Hook(nameof(TriggerSpanStart), SpanStart));
        harmony.Patch(targets[5].Target!, postfix: Hook(nameof(TriggerSpanEnd), SpanEnd));
        // The effects can throw and can apply other interactions: the timing closes in a finalizer, and each call
        // brackets its own prefix and postfix spans.
        harmony.Patch(targets[6].Target!, prefix: Hook(nameof(EffectsPrefixStart), SpanStart), finalizer: Hook(nameof(EffectsLeave), Close));
        TimedToEnd(targets[6].Target!, nameof(ApplyEffectsPrefix));
        harmony.Patch(targets[6].Target!, prefix: Hook(nameof(EffectsPrefixEnd), SpanEnd));
        harmony.Patch(targets[6].Target!, postfix: Hook(nameof(EffectsPostfixStart), SpanStart));
        harmony.Patch(targets[6].Target!, postfix: Hook(nameof(EffectsPostfixEnd), SpanEnd));
        TimedToEnd(targets[7].Target!, nameof(SaveBeginPrefix));
        TimedToEnd(targets[8].Target!, nameof(SaveSerialisePrefix));
        applied = true;
        ReportOutside(targets[4].Name, targets[4].Target!, prefixes: false);
        ReportOutside(targets[5].Name, targets[5].Target!, prefixes: false);
        ReportOutside(targets[6].Name, targets[6].Target!, prefixes: true);
    }

    private static void Remove()
    {
        harmony?.UnpatchSelf(); applied = false;
        foreach (var span in new[] { offers, effects, triggers }) { span.Reset(); span.Take(); }
    }
    private static void Flush()
    {
        var offer = offers.Take(); var effect = effects.Take(); var trigger = triggers.Take();
        // The frame has ended, so every call has returned: a span still open was left by an exception.
        offers.Reset(); effects.Reset(); triggers.Reset();
        if (trigger.Calls > 0) Performance.Increment(TriggerCalls, trigger.Calls);
        if (trigger.Timed > 0) { Performance.Increment(TriggerSampledCalls, trigger.Timed); Performance.Increment(TriggerSampledMs, trigger.Ticks * millisecondsPerTick); }
        if (offer.Ticks > 0) Performance.Increment(OfferPostfixes, offer.Ticks * millisecondsPerTick);
        if (effect.Ticks > 0) Performance.Increment(EffectHooks, effect.Ticks * millisecondsPerTick);
    }

    // A patch another mod registered with a priority the probes cannot outrank (First + 1 or above, or Last) runs
    // outside the span: the measurement then leaves it out, which the log says once per capture start.
    private static void ReportOutside(string name, MethodBase target, bool prefixes)
    {
        var info = Harmony.GetPatchInfo(target);
        if (info == null) return;
        var outside = (prefixes ? info.Prefixes.Concat(info.Postfixes) : info.Postfixes)
            .Where(p => p.owner != HarmonyId && (p.priority >= SpanStart || p.priority < SpanEnd))
            .Select(p => p.owner).Distinct().ToArray();
        if (outside.Length > 0) log(Text.Get("Performance.probe_outside_span", name, string.Join(", ", outside)));
    }

    private static HarmonyMethod Hook(string name, int priority) =>
        new(typeof(CaptureProbes).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)) { priority = priority };
    private static void Timed(MethodBase target, string prefix) =>
        harmony!.Patch(target, prefix: Hook(prefix, Open), postfix: Hook(nameof(Stop), Close));
    // A timing that must end even when the method throws, or a scope left open would stop the capture.
    private static void TimedToEnd(MethodBase target, string prefix) =>
        harmony!.Patch(target, prefix: Hook(prefix, Open), finalizer: Hook(nameof(Stop), Close));

    private static void GameUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(GameUpdate);
    private static void SimAdvancePrefix(out PerformanceScope __state) => __state = Performance.Measure(SimAdvance);
    private static void SystemUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(SystemUpdate);
    private static void PoweredUpdatePrefix(out PerformanceScope __state) => __state = Performance.Measure(PoweredUpdate);
    private static void OfferCheckPrefix(out PerformanceScope __state) => __state = Performance.Measure(OfferCheck);
    private static void ApplyEffectsPrefix(out PerformanceScope __state) => __state = Performance.Measure(ApplyEffects);
    private static void SaveBeginPrefix(out PerformanceScope __state) => __state = Performance.Measure(SaveBegin);
    private static void SaveSerialisePrefix(out PerformanceScope __state) => __state = Performance.Measure(SaveSerialise);
    private static void Stop(PerformanceScope __state) => __state.Dispose();
    // Everything between a span's start and end runs every patch on the method: Phobos mods' and any other mod's.
    private static void OfferSpanStart() => offers.Open();
    private static void OfferSpanEnd() => offers.Close();
    private static void TriggerSpanStart() => triggers.Open();
    private static void TriggerSpanEnd() => triggers.Close();
    private static void EffectsPrefixStart() { effects.Enter(); effects.Open(); }
    private static void EffectsPrefixEnd() => effects.Close();
    // A prefix that skipped the original may also have left the end-of-prefixes probe unrun: close the call's prefix span.
    private static void EffectsPostfixStart() { effects.Close(); effects.Open(); }
    private static void EffectsPostfixEnd() => effects.Close();
    private static void EffectsLeave() => effects.Leave();
}
