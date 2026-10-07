using System;
using System.Collections.Generic;
using System.Linq;
using Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>Framework 0.133.0 (L102): the hook spans the capture probes time patches with, and the capture metadata that
/// names every loaded Phobos mod within the recorder's limit.</summary>
internal static class ProbeChecks
{
    internal static void Run(Action<bool, string> check)
    {
        long now = 0;
        long Clock() => now;

        // A postfix-only span (offer check, trigger check): nested calls are counted, timed once by the outer span.
        var span = new HookSpan(Clock);
        span.Open(); now += 5; span.Open(); now += 3; span.Close(); now += 2; span.Close();
        var sums = span.Take();
        check(sums.Calls == 2 && sums.Timed == 1 && sums.Ticks == 10, "A nested call inside an open span is counted but not timed twice");
        check(span.Take() == (0, 0, 0), "Take hands the sums over once");

        // A stride times one outermost span in N; every call is still counted.
        var sampled = new HookSpan(Clock, 4);
        for (int i = 0; i < 8; i++) { sampled.Open(); now += 1; sampled.Close(); }
        sums = sampled.Take();
        check(sums.Calls == 8 && sums.Timed == 2 && sums.Ticks == 2, "With a stride of four, two of eight spans are timed and all eight counted");

        // Prefix and postfix spans of one call (interaction effects).
        var effects = new HookSpan(Clock);
        effects.Enter(); effects.Open(); now += 2; effects.Close();   // prefixes
        now += 100;                                                   // the game's own body: not hook time
        effects.Close(); effects.Open(); now += 3; effects.Close();   // postfixes (the stale-close at their start is a no-op)
        effects.Leave();
        sums = effects.Take();
        check(sums.Timed == 2 && sums.Ticks == 5, "Only the prefix and postfix spans are hook time, not the method's body");

        // A prefix that skips the original can leave the end-of-prefixes probe unrun: the first postfix closes it.
        effects.Enter(); effects.Open(); now += 4;
        effects.Close(); effects.Open(); now += 1; effects.Close(); effects.Leave();
        sums = effects.Take();
        check(sums.Timed == 2 && sums.Ticks == 5, "A prefix span left open by a skipping prefix is closed by the call's first postfix");

        // A call applied from inside an outer call's prefix span is inside that span already and must not close it.
        effects.Enter(); effects.Open(); now += 1;
        effects.Enter(); effects.Open(); now += 2; effects.Close(); now += 10; effects.Close(); effects.Open(); now += 2; effects.Close(); effects.Leave();
        now += 1; effects.Close();
        now += 50; effects.Close(); effects.Open(); now += 1; effects.Close(); effects.Leave();
        sums = effects.Take();
        check(sums.Timed == 2 && sums.Ticks == 17, "An inner call's spans neither close nor double the outer call's prefix span");

        // An exception skips the closing probes: the finalizer closes the call's spans; a frame reset drops the rest.
        effects.Enter(); effects.Open(); now += 7; effects.Leave();
        sums = effects.Take();
        check(sums.Timed == 1 && sums.Ticks == 7, "A call that threw still closes its spans in its finalizer");
        span.Open(); span.Open(); span.Reset(); span.Open(); now += 2; span.Close();
        sums = span.Take();
        check(sums.Timed == 1 && sums.Ticks == 2, "A frame reset drops spans an exception left open");
        var deep = new HookSpan(Clock);
        for (int i = 0; i < HookSpan.MaximumDepth + 3; i++) deep.Open();
        now += 1;
        for (int i = 0; i < HookSpan.MaximumDepth + 3; i++) deep.Close();
        check(deep.Take().Timed == 1, "Nesting deeper than the limit still closes back to the outermost span");
        check(HookSpan.EmptyTicks(() => now++, 2, 10) == 1, "The empty-span calibration reads what two clock reads cost a span");

        // Metadata: every Phobos plugin names itself, Framework does not, and the recorder's limit holds.
        const string framework = "phobosgekko.ostranauts.framework";
        check(CaptureMetadata.Stem("phobosgekko.ostranauts.bank", framework) == "bank" &&
            CaptureMetadata.Stem("phobosgekko.ostranauts.exchange", framework) == "exchange" &&
            CaptureMetadata.Stem("phobosgekko.ostranauts.wardeclared", framework) == "wardeclared", "Banking, Exchange and the existing keys keep plain stems");
        check(CaptureMetadata.Stem(framework, framework) == null && CaptureMetadata.Stem("com.other.mod", framework) == null &&
            CaptureMetadata.Stem("phobosgekko.ostranauts.", framework) == null && CaptureMetadata.Stem("phobosgekko.ostranauts.Bad Id", framework) == null,
            "Framework, other authors' plugins and unsafe ids are not listed");
        var fixedEntries = new Dictionary<string, string> { ["game"] = "Ostranauts", ["framework_version"] = "0.133.0" };
        var mods = new[] { ("exchange", "0.5.1", "b-e"), ("bank", "0.8.0", "b-b"), ("autonav", "0.35.1", "b-a") };
        var plan = CaptureMetadata.Plan(fixedEntries, mods, reserved: 2);
        check(plan.Count == 8 && plan["bank_version"] == "0.8.0" && plan["exchange_build"] == "b-e" && !plan.ContainsKey(CaptureMetadata.Truncated),
            "Every mod's version and build are listed when they fit");
        var many = Enumerable.Range(0, 20).Select(i => ("mod" + i.ToString("00"), "1." + i, "build" + i)).ToArray();
        plan = CaptureMetadata.Plan(fixedEntries, many, reserved: 2);
        check(plan.Count == CaptureMetadata.Limit - 2 && plan[CaptureMetadata.Truncated] == "true",
            "Too many mods: the metadata stops at the recorder's limit, less the session's own entries, and says it was cut");
        check(Enumerable.Range(0, 20).All(i => plan.ContainsKey("mod" + i.ToString("00") + "_version")) && plan.Keys.Count(k => k.EndsWith("_build")) == 7,
            "Builds give way before versions");
    }
}
