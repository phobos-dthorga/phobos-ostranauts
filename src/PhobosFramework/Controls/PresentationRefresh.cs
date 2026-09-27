using System;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Real-time presentation pacing. Never use this to pace gameplay or authorise a command.</summary>
public sealed class PresentationRefresh
{
    private readonly double interval;
    private double next = double.NegativeInfinity, last = double.NegativeInfinity;
    private object? subject, actor, target;
    private string? language;
    public PresentationRefresh(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        interval = seconds;
    }
    public void Invalidate() => next = double.NegativeInfinity;
    public void Bind(object? subject, object? actor, object? target, string? language)
    {
        if (ReferenceEquals(this.subject, subject) && ReferenceEquals(this.actor, actor) &&
            ReferenceEquals(this.target, target) && this.language == language) return;
        this.subject = subject; this.actor = actor; this.target = target; this.language = language;
        Invalidate();
    }
    public bool Due(double now)
    {
        if (double.IsNaN(now) || double.IsInfinity(now)) return false;
        if (now < last) Invalidate();
        last = now;
        if (now < next) return false;
        next = now + interval; // No catch-up bursts after a stall, pause or hidden panel.
        return true;
    }
}
