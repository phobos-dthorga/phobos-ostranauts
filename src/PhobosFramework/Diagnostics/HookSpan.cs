using System;

namespace Phobos.Ostranauts.Framework.Diagnostics;

/// <summary>The time every mod's patches on one game method take (Framework 0.133.0, L102): a span opened by a probe
/// patch that runs before all of them and closed by one that runs after all of them. Pure: the probes supply the clock.
///
/// Spans nest the way the game's calls do. A trigger check runs other trigger checks, and an interaction's effects can
/// apply another interaction, sometimes from inside a patch. A span opened while another is open is counted as a call
/// but not timed again, because the outer span's time already holds it. A method whose patches split into a prefix and
/// a postfix span brackets each call with <see cref="Enter"/> and <see cref="Leave"/>, so a span is closed only by the
/// call that opened it: a prefix that skips the original can leave the end-of-prefixes probe unrun, and the call's
/// first postfix then closes its prefix span. <see cref="Leave"/>, run from a finalizer, closes whatever an exception
/// left open, and <see cref="Reset"/> drops anything still open at the end of a frame.
///
/// With a stride above one only every stride-th outermost span reads the clock: about 590,000 trigger checks a second
/// would otherwise cost more in clock reads than the patches being measured (owner choice, 8 October 2026: one call
/// in 16, calibrated). Every call is still counted, so the report scales the timed share up and labels it an estimate.</summary>
internal sealed class HookSpan
{
    /// <summary>Deeper nesting than this is counted but neither opened nor timed.</summary>
    internal const int MaximumDepth = 64;
    private readonly Func<long> clock;
    private readonly int stride;
    private readonly int[] open = new int[MaximumDepth];
    private int count, depth, overflow;
    private long start, outermost;
    private bool timing;
    /// <summary>Spans opened, nested or not: for a trigger check, every call that reached its postfixes.</summary>
    internal long Calls { get; private set; }
    /// <summary>Outermost spans that were timed.</summary>
    internal long Timed { get; private set; }
    /// <summary>Clock ticks inside timed spans.</summary>
    internal long Ticks { get; private set; }

    internal HookSpan(Func<long> clock, int stride = 1)
    {
        if (stride < 1) throw new ArgumentOutOfRangeException(nameof(stride));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock)); this.stride = stride;
    }

    /// <summary>A call to the bracketed method begins (its first prefix).</summary>
    internal void Enter() => depth++;
    /// <summary>The call ends (its finalizer): closes any span it left open.</summary>
    internal void Leave()
    {
        while (count > 0 && open[count - 1] == depth) Close();
        if (depth > 0) depth--;
    }
    /// <summary>Opens a span for the current call.</summary>
    internal void Open()
    {
        Calls++;
        if (count == MaximumDepth) { overflow++; return; }
        open[count++] = depth;
        if (count > 1) return;
        timing = stride == 1 || ++outermost % stride == 0;
        if (timing) start = clock();
    }
    /// <summary>Closes the innermost open span when it belongs to the current call; otherwise does nothing.</summary>
    internal void Close()
    {
        if (overflow > 0) { overflow--; return; }
        if (count == 0 || open[count - 1] != depth) return;
        if (--count > 0 || !timing) return;
        long end = clock();
        if (end > start) Ticks += end - start;
        Timed++; timing = false;
    }
    /// <summary>The frame ended: every call has returned, so anything still open was left by an exception and is dropped.</summary>
    internal void Reset() { count = depth = overflow = 0; timing = false; }
    /// <summary>Hands over the sums gathered since the last take and starts again from zero.</summary>
    internal (long Calls, long Timed, long Ticks) Take()
    {
        var sums = (Calls, Timed, Ticks);
        Calls = Timed = Ticks = 0;
        return sums;
    }

    /// <summary>What reading the clock twice costs a span with nothing in it, in clock ticks: the lowest mean of
    /// <paramref name="batches"/> batches of <paramref name="pairs"/> empty spans. A timed span overstates its patches
    /// by about this much; the report subtracts it.</summary>
    internal static double EmptyTicks(Func<long> clock, int batches = 8, int pairs = 512)
    {
        double lowest = double.PositiveInfinity;
        for (int b = 0; b < batches; b++)
        {
            var span = new HookSpan(clock);
            for (int i = 0; i < pairs; i++) { span.Open(); span.Close(); }
            lowest = Math.Min(lowest, (double)span.Ticks / pairs);
        }
        return lowest;
    }
}
