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
    /// <summary>Unscaled real time: unaffected by pause and fast-forward.</summary>
    public static double RealTime => UnityEngine.Time.unscaledTime;
}
