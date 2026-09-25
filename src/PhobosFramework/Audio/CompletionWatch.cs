using System;

namespace Phobos.Ostranauts.Framework.Audio;

/// <summary>Transient, explicitly requested one-result notification. Never persisted.</summary>
public sealed class CompletionWatch
{
    private string actor = "", ship = "";
    public bool Armed { get; private set; }
    public bool Completed { get; private set; }
    public void Arm(string actorId, string shipId)
    {
        actor = actorId; ship = shipId;
        Armed = actor.Length > 0 && ship.Length > 0; Completed = false;
    }
    public void Cancel() { Armed = false; Completed = false; }
    public bool Commit(string actorId, string shipId, bool advanced)
    {
        bool watched = Armed;
        Armed = false;
        Completed |= watched;
        return watched && advanced && actor == actorId && ship == shipId;
    }
}

/// <summary>Real-time spacing: drop bursts rather than scheduling stale sounds.</summary>
internal sealed class CompletionCueGate
{
    internal const double QuietSeconds = 3;
    private double last = double.NegativeInfinity;
    internal bool Take(double now, double volume, bool playing)
    {
        if (double.IsNaN(now) || double.IsInfinity(now) || double.IsNaN(volume) ||
            double.IsInfinity(volume) || volume <= 0 || playing) return false;
        if (now < last) { last = now; return false; }
        if (now - last < QuietSeconds) return false;
        last = now;
        return true;
    }
}
