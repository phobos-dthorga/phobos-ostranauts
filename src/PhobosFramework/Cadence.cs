using System;

namespace Phobos.Ostranauts.Framework;

/// <summary>A real-time gate for work that need not follow game speed: topology rechecks, discovery, candidate
/// lists, record settlement. The first call is due at once; after that one pass per interval, and a stall
/// produces one pass with no catch-up burst. Conserved accounting (power receipts, transfers, thermal and crop
/// steps) stays on game time inside the native hooks and never uses this.</summary>
public sealed class Cadence
{
    public double Seconds { get; }
    private double next = double.NegativeInfinity;
    public Cadence(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Seconds = seconds;
    }
    /// <summary>Whether a pass is due at <paramref name="now"/> (seconds on any monotonic clock); schedules the next one.</summary>
    public bool Due(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now) || now < next) return false;
        next = now + Seconds; return true;
    }
    public bool Due() => Due(RealTime);
    /// <summary>The next call is due at once.</summary>
    public void Invalidate() => next = double.NegativeInfinity;
    /// <summary>Unscaled real time: unaffected by pause and fast-forward. A time-skip is one frame of real time but
    /// hours of machine steps, so each stepped second of a skip counts here as a second (Framework 0.99.0): a machine
    /// that waits five seconds before it looks at a full store again waits five skipped seconds, not the whole skip.
    /// The clock only ever moves forward.</summary>
    public static double RealTime => UnityEngine.Time.unscaledTime + skipped;
    private static double skipped;
    /// <summary>Counts stepped time-skip seconds into <see cref="RealTime"/>.</summary>
    internal static void AdvanceSkip(double seconds) => skipped = Skipped(skipped, seconds);
    /// <summary>The skip clock's rule, pure: only finite, positive steps are added.</summary>
    public static double Skipped(double total, double seconds) =>
        double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0 ? total : total + seconds;
}
